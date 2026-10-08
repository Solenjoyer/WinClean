using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WinClean.Core.Cleanup;

namespace WinClean.Services.Cleanup;

/// <summary>Talks to the local Docker daemon through docker.exe, never to anything remote.</summary>
public sealed class DockerCli
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan PruneTimeout = TimeSpan.FromMinutes(5);

    private readonly ILogger<DockerCli> _logger;

    public DockerCli(ILogger<DockerCli> logger)
    {
        _logger = logger;
    }

    public static string? Locate()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (var directory in path.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim(), "docker.exe");

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "resources", "bin", "docker.exe");
        return File.Exists(defaultPath) ? defaultPath : null;
    }

    /// <summary>Reclaimable space per kind, as docker system df reports it; null when the daemon does not answer.</summary>
    public IReadOnlyList<(string Type, long Reclaimable)>? Usage(string docker)
    {
        var output = Run(docker, "system df --format \"{{json .}}\"", QueryTimeout);

        if (output is null)
        {
            return null;
        }

        var rows = new List<(string, long)>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                var element = document.RootElement;

                if (element.TryGetProperty("Type", out var type) && element.TryGetProperty("Reclaimable", out var reclaimable)
                    && DockerSizes.TryParse(reclaimable.GetString(), out var bytes))
                {
                    rows.Add((type.GetString() ?? string.Empty, bytes));
                }
            }
            catch (JsonException)
            {
                // Not a JSON line: an older client prints a table.
            }
        }

        return rows;
    }

    public bool Prune(string docker, string arguments, out string output)
    {
        var result = Run(docker, arguments, PruneTimeout);
        output = result ?? string.Empty;
        return result is not null;
    }

    private string? Run(string docker, string arguments, TimeSpan timeout)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(docker, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            process.Start();
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                _logger.LogWarning("docker {Arguments} did not finish within {Seconds} s.", arguments, timeout.TotalSeconds);
                return null;
            }

            if (process.ExitCode != 0)
            {
                _logger.LogInformation("docker {Arguments} exited with {Code}: {Error}", arguments, process.ExitCode, error.Result.Trim());
                return null;
            }

            return output.Result;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _logger.LogWarning(exception, "docker {Arguments} could not be run.", arguments);
            return null;
        }
    }
}
