namespace WinClean.Core.Applications;

public sealed record ProtectionVerdict(ProtectionLevel Level, ProtectionReason Reason)
{
    public static ProtectionVerdict Allowed { get; } = new(ProtectionLevel.Allowed, ProtectionReason.None);

    public bool IsBlocked => Level == ProtectionLevel.Blocked;
}
