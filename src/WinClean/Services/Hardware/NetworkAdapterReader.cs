using System.Net.NetworkInformation;

namespace WinClean.Services.Hardware;

internal static class NetworkAdapterReader
{
    public static IReadOnlyList<NetworkAdapterFacts> Read()
    {
        var adapters = new List<NetworkAdapterFacts>();

        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            var type = adapter.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.FastEthernetT or NetworkInterfaceType.FastEthernetFx => "Ethernet",
                NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                NetworkInterfaceType.Wwanpp or NetworkInterfaceType.Wwanpp2 => "Mobile broadband",
                NetworkInterfaceType.Ppp => "PPP",
                _ => adapter.NetworkInterfaceType.ToString(),
            };

            var mac = adapter.GetPhysicalAddress().GetAddressBytes();
            adapters.Add(new NetworkAdapterFacts(
                adapter.Name,
                adapter.Description,
                type,
                adapter.Speed,
                mac.Length == 0 ? string.Empty : string.Join(':', mac.Select(part => part.ToString("X2", System.Globalization.CultureInfo.InvariantCulture))),
                adapter.OperationalStatus == OperationalStatus.Up));
        }

        return adapters;
    }
}
