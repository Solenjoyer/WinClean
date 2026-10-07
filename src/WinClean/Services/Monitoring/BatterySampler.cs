using WinClean.Core.Monitoring;
using WinClean.Native;

namespace WinClean.Services.Monitoring;

internal sealed class BatterySampler
{
    public string? Reason { get; private set; }

    public BatterySample? Sample()
    {
        if (!Kernel32.GetSystemPowerStatus(out var status))
        {
            Reason = Win32Reason.LastError();
            return null;
        }

        Reason = null;

        if ((status.BatteryFlag & Kernel32.BATTERY_FLAG_NO_BATTERY) != 0)
        {
            return null;
        }

        return new BatterySample(
            OnAcPower: status.ACLineStatus == 1,
            Percent: status.BatteryLifePercent == Kernel32.BATTERY_PERCENTAGE_UNKNOWN ? null : status.BatteryLifePercent,
            Remaining: status.BatteryLifeTime == Kernel32.BATTERY_LIFE_UNKNOWN ? null : TimeSpan.FromSeconds(status.BatteryLifeTime),
            Charging: (status.BatteryFlag & 8) != 0,
            SaverOn: status.SystemStatusFlag == 1);
    }
}
