using System.IO;
using System.Text;

namespace WinClean.Services.Logging;

/// <summary>
/// Appends to logs\winclean-yyyyMMdd.log. A file that grows past the size cap is moved aside once,
/// and only the newest few files are kept, so the folder never grows without bound.
/// </summary>
internal sealed class LogFile : IDisposable
{
    private const long MaximumFileSize = 5 * 1024 * 1024;

    private const int FilesToKeep = 7;

    private readonly string _directory;

    private readonly object _gate = new();

    private StreamWriter? _writer;

    private DateOnly _writerDate;

    private long _writtenBytes;

    public LogFile(string directory)
    {
        _directory = directory;
    }

    public void Append(string line)
    {
        lock (_gate)
        {
            try
            {
                var writer = Writer(DateOnly.FromDateTime(DateTime.Now));
                writer.WriteLine(line);
                writer.Flush();
                _writtenBytes += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

                if (_writtenBytes > MaximumFileSize)
                {
                    RollOver();
                }
            }
            catch (IOException)
            {
                // Logging must never take the application down. A full or locked disk loses log lines, nothing else.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private StreamWriter Writer(DateOnly today)
    {
        if (_writer is not null && _writerDate == today)
        {
            return _writer;
        }

        _writer?.Dispose();
        Directory.CreateDirectory(_directory);

        var path = PathFor(today);
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writtenBytes = stream.Length;
        _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        _writerDate = today;

        Prune();
        return _writer;
    }

    private void RollOver()
    {
        _writer?.Dispose();
        _writer = null;

        var current = PathFor(_writerDate);
        File.Move(current, Path.ChangeExtension(current, ".1.log"), overwrite: true);
        _writtenBytes = 0;
    }

    private void Prune()
    {
        var files = Directory.GetFiles(_directory, "winclean-*.log");

        if (files.Length <= FilesToKeep)
        {
            return;
        }

        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (var stale in files.Take(files.Length - FilesToKeep))
        {
            File.Delete(stale);
        }
    }

    private string PathFor(DateOnly date) => Path.Combine(_directory, $"winclean-{date:yyyyMMdd}.log");
}
