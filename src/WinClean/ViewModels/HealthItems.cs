namespace WinClean.ViewModels;

public sealed record UpdateRow(string Title, string DateText, bool Failed);

public sealed record DriverRow(string Device, string Group, string Provider, string Version, string DateText, string? Problem, string? SourceLabel, string? SourceTooltip, Uri? SourceUrl)
{
    public bool HasSource => SourceUrl is not null;

    public bool HasProblem => Problem is not null;
}
