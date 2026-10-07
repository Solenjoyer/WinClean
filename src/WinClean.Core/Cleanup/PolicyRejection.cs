namespace WinClean.Core.Cleanup;

public enum PolicyRejection
{
    None,
    UnsupportedPath,
    NotAbsolute,
    OutsideRoot,
    IsRoot,
    ProtectedLocation,
    TooDeep,
    Directory,
    ReparsePoint,
    SystemFile,
    ReadOnlyFile,
    TooNew,
}
