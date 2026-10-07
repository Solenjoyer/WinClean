namespace WinClean.Core.Monitoring.Parsers;

/// <summary>The fields of a MIB_IF_ROW2 that matter for throughput and for telling real adapters from virtual ones.</summary>
public sealed record NetworkInterfaceRow(
    long Luid,
    int Index,
    string Alias,
    string Description,
    int Type,
    int OperationalStatus,
    int MediaConnectState,
    bool HardwareInterface,
    bool ConnectorPresent,
    ulong TransmitLinkSpeed,
    ulong ReceiveLinkSpeed,
    ulong InOctets,
    ulong OutOctets)
{
    public const int IfTypeEthernet = 6;
    public const int IfTypeWireless80211 = 71;
    public const int IfTypeWwanPp = 243;
    public const int IfTypeWwanPp2 = 244;
    public const int IfOperStatusUp = 1;
    public const int MediaConnectStateConnected = 1;

    /// <summary>Up, carrying real traffic for a physical adapter; VPNs, loopback and virtual switches are left out.</summary>
    public bool CountsTowardsTotals =>
        OperationalStatus == IfOperStatusUp
        && Type is IfTypeEthernet or IfTypeWireless80211 or IfTypeWwanPp or IfTypeWwanPp2
        && (HardwareInterface || ConnectorPresent);
}
