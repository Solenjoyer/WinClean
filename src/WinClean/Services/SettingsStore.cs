using System.IO;
using Microsoft.Extensions.Logging;
using WinClean.Core.Settings;

namespace WinClean.Services;

/// <summary>Owns the current settings and the file they live in. Writes are atomic: a temp file is swapped in.</summary>
public sealed class SettingsStore
{
    private readonly ILogger<SettingsStore> _logger;

    private readonly object _gate = new();

    public SettingsStore(SettingsLocation location, ILogger<SettingsStore> logger)
    {
        Location = location;
        _logger = logger;
        Current = new AppSettings();
    }

    public event EventHandler<AppSettings>? Changed;

    public SettingsLocation Location { get; }

    public AppSettings Current { get; private set; }

    public void Load()
    {
        string? json = null;

        try
        {
            if (File.Exists(Location.SettingsFile))
            {
                json = File.ReadAllText(Location.SettingsFile);
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "The settings file could not be read; defaults are used.");
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "The settings file could not be read; defaults are used.");
        }

        if (json is null)
        {
            Current = new AppSettings();
            return;
        }

        var result = SettingsSerializer.Deserialize(json);

        if (result.Error is not null)
        {
            _logger.LogWarning("{Error} Defaults are used.", result.Error);
        }

        Current = result.Settings;
    }

    public void Update(Func<AppSettings, AppSettings> change)
    {
        ArgumentNullException.ThrowIfNull(change);

        AppSettings updated;

        lock (_gate)
        {
            updated = change(Current);

            if (updated == Current)
            {
                return;
            }

            Current = updated;
            Save(updated);
        }

        Changed?.Invoke(this, updated);
    }

    private void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Location.DataDirectory);

            var temporary = Location.SettingsFile + ".tmp";
            File.WriteAllText(temporary, SettingsSerializer.Serialize(settings));
            File.Move(temporary, Location.SettingsFile, overwrite: true);
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "Settings could not be saved to {Path}.", Location.SettingsFile);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogError(exception, "Settings could not be saved to {Path}.", Location.SettingsFile);
        }
    }
}
