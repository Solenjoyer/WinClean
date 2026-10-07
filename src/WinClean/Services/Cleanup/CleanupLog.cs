using System.Globalization;
using System.IO;
using System.Text;
using WinClean.Core.Cleanup;
using WinClean.Core.Settings;

namespace WinClean.Services.Cleanup;

/// <summary>One text file per cleanup run, next to the application logs; the last twenty are kept.</summary>
public sealed class CleanupLog
{
    private const int RunsToKeep = 20;

    private readonly string _directory;

    public CleanupLog(SettingsLocation location)
    {
        _directory = location.LogDirectory;
    }

    public string Write(IReadOnlyList<CleanupItemResult> results, TimeSpan duration)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, $"cleanup-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        var text = new StringBuilder();
        var freed = results.Where(result => result.Removed).Sum(result => result.Item.Size);

        text.AppendLine(CultureInfo.InvariantCulture, $"{AppInfo.Name} {AppInfo.Version} cleanup {DateTimeOffset.Now:u}");
        text.AppendLine(CultureInfo.InvariantCulture, $"{results.Count(result => result.Removed)} removed, {results.Count(result => !result.Removed)} not removed, {freed} bytes freed, {duration.TotalSeconds:F1} s");
        text.AppendLine();

        foreach (var result in results)
        {
            text.Append(result.Outcome.ToString().ToUpperInvariant().PadRight(13));
            text.Append(result.Item.Size.ToString(CultureInfo.InvariantCulture).PadLeft(14));
            text.Append("  ");
            text.Append(result.Item.Path);

            if (result.Detail is not null)
            {
                text.Append("  (").Append(result.Detail).Append(')');
            }

            text.AppendLine();
        }

        File.WriteAllText(path, text.ToString());
        Prune();
        return path;
    }

    private void Prune()
    {
        try
        {
            var old = Directory.EnumerateFiles(_directory, "cleanup-*.log")
                .OrderByDescending(file => file, StringComparer.Ordinal)
                .Skip(RunsToKeep);

            foreach (var file in old)
            {
                File.Delete(file);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
