namespace WinClean.Services.Diagnostics;

internal enum CheckStatus
{
    Ok,

    /// <summary>The feature works but something on this machine limits it, such as missing counters or rights.</summary>
    Degraded,

    Failed,
}
