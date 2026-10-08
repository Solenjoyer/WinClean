namespace WinClean.Core.Storage;

public enum LocationKind
{
    /// <summary>Measured and shown; WinClean never offers to delete it.</summary>
    ReportOnly,

    /// <summary>Can be selected on the Cleanup page.</summary>
    Cleanable,
}
