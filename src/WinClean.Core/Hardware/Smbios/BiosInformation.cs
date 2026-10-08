namespace WinClean.Core.Hardware.Smbios;

public sealed record BiosInformation(string Vendor, string Version, string ReleaseDate, Version? Revision);
