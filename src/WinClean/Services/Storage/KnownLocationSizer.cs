using System.IO;
using WinClean.Core.Storage;

namespace WinClean.Services.Storage;

/// <summary>Resolves the known locations on this machine and measures them on request, without a full scan.</summary>
public sealed class KnownLocationSizer
{
    private readonly IReadOnlyDictionary<string, string> _variables = BuildVariables();

    public IReadOnlyList<string> Expand(KnownLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        var paths = KnownLocations.Expand(location, _variables, ListDirectories).ToList();

        switch (location.Id)
        {
            case "wsl-disks":
                foreach (var distribution in WslDistributions.Read())
                {
                    paths.Add(Path.Combine(distribution.BasePath, "ext4.vhdx"));
                }

                break;

            case "docker-data":
                if (DockerDesktopData.ConfiguredDataFolder() is { } folder)
                {
                    paths.Add(folder);
                }

                break;
        }

        return paths
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public LocationMeasurement Measure(KnownLocation location, CancellationToken cancellationToken)
    {
        var paths = Expand(location);
        long bytes = 0;
        var files = 0;

        foreach (var path in paths)
        {
            var (pathBytes, pathFiles) = DirectorySizes.Measure(path, location.FilePatterns, cancellationToken);
            bytes += pathBytes;
            files += pathFiles;
        }

        return new LocationMeasurement(location, paths, bytes, files);
    }

    public Task<IReadOnlyList<LocationMeasurement>> MeasureAllAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        return Task.Run<IReadOnlyList<LocationMeasurement>>(() =>
        {
            var results = new List<LocationMeasurement>();

            foreach (var location in KnownLocations.All)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(location.DisplayName);
                results.Add(Measure(location, cancellationToken));
            }

            return results;
        }, cancellationToken);
    }

    private static IEnumerable<string> ListDirectories(string directory)
    {
        try
        {
            return Directory.Exists(directory) ? Directory.EnumerateDirectories(directory).Select(path => Path.GetFileName(path) ?? string.Empty) : [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static Dictionary<string, string> BuildVariables()
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void Add(string name, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                variables[name] = value.TrimEnd('\\');
            }
        }

        Add("LOCALAPPDATA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        Add("APPDATA", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        Add("USERPROFILE", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Add("PROGRAMDATA", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        Add("SYSTEMROOT", Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        Add("SYSTEMDRIVE", Path.GetPathRoot(Environment.SystemDirectory));
        return variables;
    }
}
