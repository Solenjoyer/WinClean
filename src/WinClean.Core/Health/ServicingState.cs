namespace WinClean.Core.Health;

public enum ServicingState
{
    Supported,
    Ended,

    /// <summary>The build is not in the bundled table: a preview, a server, or newer than this release of WinClean.</summary>
    Unknown,
}
