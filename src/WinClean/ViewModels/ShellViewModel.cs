using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinClean.Resources;

namespace WinClean.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    public ShellViewModel(
        OverviewViewModel overview,
        ProcessesViewModel processes,
        StorageViewModel storage,
        CleanupViewModel cleanup,
        HealthViewModel health,
        HardwareViewModel hardware,
        SettingsViewModel settings)
    {
        Items =
        [
            new NavigationItem(PageKeys.Overview, Strings.Nav_Overview, Glyphs.Home, overview),
            new NavigationItem(PageKeys.Processes, Strings.Nav_Processes, Glyphs.TaskView, processes),
            new NavigationItem(PageKeys.Storage, Strings.Nav_Storage, Glyphs.HardDrive, storage),
            new NavigationItem(PageKeys.Cleanup, Strings.Nav_Cleanup, Glyphs.Broom, cleanup),
            new NavigationItem(PageKeys.Health, Strings.Nav_Health, Glyphs.Health, health),
            new NavigationItem(PageKeys.Hardware, Strings.Nav_Hardware, Glyphs.Component, hardware),
            new NavigationItem(PageKeys.Settings, Strings.Nav_Settings, Glyphs.Setting, settings),
        ];

        SelectedItem = Items[0];
    }

    public IReadOnlyList<NavigationItem> Items { get; }

    [ObservableProperty]
    public partial NavigationItem SelectedItem { get; set; }

    [ObservableProperty]
    public partial bool IsCompact { get; set; }

    public void NavigateTo(string? key)
    {
        if (key is null)
        {
            return;
        }

        var item = Items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));

        if (item is not null)
        {
            SelectedItem = item;
        }
    }

    [RelayCommand]
    private void ToggleCompact() => IsCompact = !IsCompact;

    [RelayCommand]
    private void Navigate(string key) => NavigateTo(key);
}
