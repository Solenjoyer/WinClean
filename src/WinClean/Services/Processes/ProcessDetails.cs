using System.Windows.Media;

namespace WinClean.Services.Processes;

/// <summary>What is learned about a process once, after it appears; none of it changes for its lifetime.</summary>
public sealed record ProcessDetails(
    string? Path,
    string? Description,
    string? CommandLine,
    string? UserName,
    bool IsSystemAccount,
    bool? IsElevated,
    string? Architecture,
    bool IsCritical,
    bool AccessDenied,
    ImageSource? Icon)
{
    public static ProcessDetails Denied { get; } = new(null, null, null, null, false, null, null, false, AccessDenied: true, null);

    public static ProcessDetails Unavailable { get; } = new(null, null, null, null, false, null, null, false, AccessDenied: false, null);
}
