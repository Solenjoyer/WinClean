namespace WinClean.Core.Health;

/// <summary>An official download page for a vendor's drivers.</summary>
public sealed record DriverSource(string Vendor, Uri Url);
