namespace WinClean.Core.Applications;

public enum ProtectionLevel
{
    Allowed,

    /// <summary>Possible, but the user must confirm with an explicit acknowledgement.</summary>
    Confirm,

    Blocked,
}
