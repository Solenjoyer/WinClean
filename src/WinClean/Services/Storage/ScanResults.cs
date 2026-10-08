using WinClean.Core.Storage;

namespace WinClean.Services.Storage;

/// <summary>The most recent scan, shared between the Storage page that made it and the Cleanup page that reads the developer folders from it.</summary>
public sealed class ScanResults
{
    public ScanResult? Latest { get; private set; }

    public event EventHandler? Changed;

    public void Publish(ScanResult result)
    {
        Latest = result;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
