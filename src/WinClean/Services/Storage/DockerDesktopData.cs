using System.IO;
using System.Text.Json;

namespace WinClean.Services.Storage;

/// <summary>Where Docker Desktop keeps its virtual disk when the user moved it away from the default.</summary>
internal static class DockerDesktopData
{
    public static string? ConfiguredDataFolder()
    {
        var docker = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Docker");

        foreach (var (file, property) in new[] { ("settings-store.json", "DataFolder"), ("settings.json", "dataFolder") })
        {
            var path = Path.Combine(docker, file);

            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));

                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty(property, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && value.GetString() is { Length: > 0 } folder)
                {
                    return folder;
                }
            }
            catch (JsonException)
            {
                // A half-written settings file; the default locations still apply.
            }
            catch (IOException)
            {
            }
        }

        return null;
    }
}
