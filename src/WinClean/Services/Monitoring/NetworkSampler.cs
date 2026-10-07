using WinClean.Core.Monitoring;
using WinClean.Core.Monitoring.Parsers;
using WinClean.Native;

namespace WinClean.Services.Monitoring;

/// <summary>Throughput per adapter from GetIfTable2; totals only count physical adapters that are up.</summary>
internal sealed class NetworkSampler
{
    private readonly Dictionary<long, (RateCalculator Receive, RateCalculator Send)> _rates = [];

    public bool IncludeVirtualAdapters { get; set; }

    public string? Reason { get; private set; }

    public unsafe NetworkSample? Sample(long nowTicks)
    {
        var error = IpHlpApi.GetIfTable2(out var table);

        if (error != IpHlpApi.NO_ERROR)
        {
            Reason = $"GetIfTable2 failed ({error})";
            return null;
        }

        IReadOnlyList<NetworkInterfaceRow> rows;

        try
        {
            var count = *(uint*)table;
            var size = NetworkInterfaceTableParser.TableHeaderSize + (int)count * NetworkInterfaceTableParser.RowSize;
            rows = NetworkInterfaceTableParser.Parse(new ReadOnlySpan<byte>((void*)table, size));
        }
        finally
        {
            IpHlpApi.FreeMibTable(table);
        }

        Reason = null;
        var adapters = new List<AdapterSample>(rows.Count);
        var seen = new HashSet<long>();
        double receiveTotal = 0;
        double sendTotal = 0;

        foreach (var row in rows)
        {
            seen.Add(row.Luid);

            if (!_rates.TryGetValue(row.Luid, out var rate))
            {
                rate = (new RateCalculator(), new RateCalculator());
                _rates[row.Luid] = rate;
            }

            var receive = rate.Receive.Update(row.InOctets, nowTicks);
            var send = rate.Send.Update(row.OutOctets, nowTicks);

            if (receive is null || send is null)
            {
                continue;
            }

            var physical = row.CountsTowardsTotals;
            adapters.Add(new AdapterSample(row.Luid, row.Alias, row.Description, receive.Value, send.Value, row.ReceiveLinkSpeed, physical));

            if (physical || (IncludeVirtualAdapters && row.OperationalStatus == NetworkInterfaceRow.IfOperStatusUp))
            {
                receiveTotal += receive.Value;
                sendTotal += send.Value;
            }
        }

        foreach (var stale in _rates.Keys.Where(luid => !seen.Contains(luid)).ToList())
        {
            _rates.Remove(stale);
        }

        return new NetworkSample(receiveTotal, sendTotal, adapters);
    }

    public void Reset()
    {
        foreach (var (receive, send) in _rates.Values)
        {
            receive.Reset();
            send.Reset();
        }
    }
}
