using Microsoft.Extensions.Logging;

namespace WinClean.Services.Logging;

/// <summary>Plain text logging to the data folder. There is no other sink: nothing leaves the machine.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly LogFile _file;

    private volatile LogLevel _minimumLevel = LogLevel.Information;

    public FileLoggerProvider(string directory)
    {
        _file = new LogFile(directory);
    }

    public LogLevel MinimumLevel
    {
        get => _minimumLevel;
        set => _minimumLevel = value;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this);

    public void Dispose() => _file.Dispose();

    internal void Write(string line) => _file.Append(line);
}
