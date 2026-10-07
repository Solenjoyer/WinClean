namespace WinClean.ViewModels;

/// <summary>Stable page identifiers used by --page, the tray menu and the activation message between instances.</summary>
public static class PageKeys
{
    public const string Overview = "overview";

    public const string Processes = "processes";

    public const string Storage = "storage";

    public const string Cleanup = "cleanup";

    public const string Health = "health";

    public const string Hardware = "hardware";

    public const string Settings = "settings";

    private static readonly string[] Ordered = [Overview, Processes, Storage, Cleanup, Health, Hardware, Settings];

    public static IReadOnlyList<string> All => Ordered;

    public static int IndexOf(string? key)
    {
        return key is null ? -1 : Array.FindIndex(Ordered, candidate => string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase));
    }

    public static string? FromIndex(int index) => index >= 0 && index < Ordered.Length ? Ordered[index] : null;
}
