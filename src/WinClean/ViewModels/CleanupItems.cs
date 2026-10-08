using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using WinClean.Core.Cleanup;
using WinClean.Core.Formatting;
using WinClean.Core.Storage;
using WinClean.Resources;
using WinClean.Services.Cleanup;

namespace WinClean.ViewModels;

public sealed partial class CleanupCategoryItem : ObservableObject
{
    public CleanupCategoryItem(CategoryDiscovery discovery, bool elevated)
    {
        Discovery = discovery;
        Name = discovery.DisplayName;
        Description = discovery.Description;
        RequiresElevation = discovery.RequiresElevation;
        NeedsAdministrator = discovery.RequiresElevation && !elevated;
        CanSelect = discovery.Count > 0 && !NeedsAdministrator;
        SizeText = discovery.Unavailable && discovery.Count == 0 ? Strings.NotAvailable : ByteSize.Format(discovery.Bytes);
        CountText = discovery.Count == 0 ? string.Empty : string.Format(CultureInfo.CurrentCulture, Strings.Cleanup_ItemCount, discovery.Count.ToString("N0", CultureInfo.CurrentCulture));
        Risk = discovery.Risk;
        RiskText = discovery.Risk switch
        {
            CleanupRisk.Safe => Strings.Cleanup_RiskSafe,
            CleanupRisk.Caution => Strings.Cleanup_RiskCaution,
            _ => Strings.Cleanup_RiskAdvanced,
        };
        Note = discovery.RunningConflicts.Count > 0
            ? string.Format(CultureInfo.CurrentCulture, Strings.Cleanup_Running, string.Join(", ", discovery.RunningConflicts))
            : NeedsAdministrator ? Strings.Cleanup_NeedsAdministrator : discovery.Note;
        IsSelected = CanSelect && discovery.DefaultSelected;
    }

    public CategoryDiscovery Discovery { get; }

    public string Id => Discovery.Id;

    public string Name { get; }

    public string Description { get; }

    public bool RequiresElevation { get; }

    public bool NeedsAdministrator { get; }

    public bool CanSelect { get; }

    public string SizeText { get; }

    public string CountText { get; }

    public CleanupRisk Risk { get; }

    public string RiskText { get; }

    public string? Note { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public sealed record CleanupGroupItem(string Name, IReadOnlyList<CleanupCategoryItem> Categories);

public sealed partial class CleanupPreviewRow : ObservableObject
{
    public CleanupPreviewRow(CleanupItem item, string category, bool canExclude)
    {
        Item = item;
        Category = category;
        CanExclude = canExclude;
        Name = item.Kind == CleanupItemKind.Command ? item.Path : System.IO.Path.GetFileName(item.Path);
        Detail = item.Kind == CleanupItemKind.Command ? item.Detail ?? string.Empty : item.Path;
        SizeText = ByteSize.Format(item.Size);
        IsIncluded = true;
    }

    public CleanupItem Item { get; }

    public string Category { get; }

    public bool CanExclude { get; }

    public string Name { get; }

    public string Detail { get; }

    public string SizeText { get; }

    [ObservableProperty]
    public partial bool IsIncluded { get; set; }
}

public sealed record CleanupResultRow(string Name, string Path, string SizeText, string Reason);
