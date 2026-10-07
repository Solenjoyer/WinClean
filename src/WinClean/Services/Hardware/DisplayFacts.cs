namespace WinClean.Services.Hardware;

public sealed record DisplayFacts(string Name, string Adapter, int Width, int Height, int RefreshHz, bool IsPrimary);
