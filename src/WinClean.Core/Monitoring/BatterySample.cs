namespace WinClean.Core.Monitoring;

public sealed record BatterySample(bool OnAcPower, int? Percent, TimeSpan? Remaining, bool Charging, bool SaverOn);
