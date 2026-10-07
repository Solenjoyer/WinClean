namespace WinClean.Services.Hardware;

public sealed record GpuAdapter(string Name, uint VendorId, uint DeviceId, long DedicatedVideoMemory, long SharedSystemMemory, long Luid);
