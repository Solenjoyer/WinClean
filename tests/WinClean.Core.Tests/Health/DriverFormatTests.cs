using WinClean.Core.Health;

namespace WinClean.Core.Tests.Health;

public class DriverFormatTests
{
    [Theory]
    [InlineData("NVIDIA", "32.0.15.6094", "560.94")]
    [InlineData("NVIDIA", "31.0.15.3667", "536.67")]
    [InlineData("Intel Corporation", "32.0.101.6881", "101.6881")]
    [InlineData("Advanced Micro Devices, Inc.", "32.0.12033.1030", "32.0.12033.1030")]
    [InlineData("Microsoft", "10.0.26100.1", "10.0.26100.1")]
    [InlineData(null, "1.2.3.4", "1.2.3.4")]
    [InlineData("NVIDIA", "", "")]
    public void Display_FollowsTheVendorConvention(string? provider, string version, string expected)
    {
        Assert.Equal(expected, DriverVersionFormat.Display(provider, version));
    }

    [Fact]
    public void Sources_PreferTheHardwareVendorId()
    {
        var source = DriverSources.Find([@"PCI\VEN_10DE&DEV_2684&SUBSYS_167F10DE"], "Microsoft");
        Assert.Equal("NVIDIA", source?.Vendor);

        Assert.Equal("Realtek", DriverSources.Find([@"USB\VID_0BDA&PID_8153"], null)?.Vendor);
        Assert.Equal("Intel", DriverSources.Find([], "Intel Corporation")?.Vendor);
        Assert.Null(DriverSources.Find([@"ACPI\PNP0C0C"], "Contoso"));
        Assert.Equal("10DE", DriverSources.VendorId(@"pci\ven_10de&dev_1234"));
    }

    [Fact]
    public void PendingRestart_ReportsEachSignalAndWeighsRenamesLightly()
    {
        var none = PendingRestartEvaluator.Evaluate(new PendingRestartFacts(false, false, false, false, false));
        Assert.Empty(none);
        Assert.False(PendingRestartEvaluator.IsRestartPending(none));

        var renamesOnly = PendingRestartEvaluator.Evaluate(new PendingRestartFacts(false, false, true, false, false));
        Assert.Equal([PendingRestartSignal.FileRenames], renamesOnly);
        Assert.False(PendingRestartEvaluator.IsRestartPending(renamesOnly));

        var update = PendingRestartEvaluator.Evaluate(new PendingRestartFacts(true, true, true, false, false));
        Assert.Equal([PendingRestartSignal.WindowsUpdate, PendingRestartSignal.ComponentServicing, PendingRestartSignal.FileRenames], update);
        Assert.True(PendingRestartEvaluator.IsRestartPending(update));
    }

    [Theory]
    [InlineData("Display", "NVIDIA", DeviceGroup.Display)]
    [InlineData("Net", "Intel", DeviceGroup.Network)]
    [InlineData("MEDIA", "Realtek", DeviceGroup.Audio)]
    [InlineData("HDC", "Microsoft", DeviceGroup.Storage)]
    [InlineData("System", "Intel", DeviceGroup.Chipset)]
    [InlineData("System", "Microsoft", DeviceGroup.Other)]
    [InlineData(null, null, DeviceGroup.Other)]
    public void Classes_MapToGroups(string? className, string? provider, DeviceGroup expected)
    {
        Assert.Equal(expected, DeviceClasses.Classify(className, provider));
    }
}
