using WinClean.Core.Monitoring;

namespace WinClean.Core.Tests.Monitoring;

public class GpuCounterInstanceTests
{
    [Fact]
    public void TryParse_EngineInstance_ReadsEveryPart()
    {
        Assert.True(GpuCounterInstance.TryParse("pid_18204_luid_0x00000000_0x0000F1C9_phys_0_eng_3_engtype_3D", out var instance));

        Assert.Equal(18204, instance.ProcessId);
        Assert.Equal(0xF1C9, instance.Luid);
        Assert.Equal(0, instance.PhysicalAdapter);
        Assert.Equal(3, instance.Engine);
        Assert.Equal("3D", instance.EngineType);
    }

    [Fact]
    public void TryParse_AdapterMemoryInstance_HasNoProcess()
    {
        Assert.True(GpuCounterInstance.TryParse("luid_0x00000001_0x0000C6B3_phys_1", out var instance));

        Assert.Null(instance.ProcessId);
        Assert.Equal(0x1_0000_C6B3L, instance.Luid);
        Assert.Equal(1, instance.PhysicalAdapter);
        Assert.Null(instance.Engine);
        Assert.Null(instance.EngineType);
    }

    [Fact]
    public void TryParse_ProcessMemoryInstance_HasNoEngine()
    {
        Assert.True(GpuCounterInstance.TryParse("pid_4_luid_0x00000000_0x0000F1C9_phys_0", out var instance));

        Assert.Equal(4, instance.ProcessId);
        Assert.Null(instance.Engine);
    }

    [Fact]
    public void TryParse_EngineTypeWithUnderscores_IsKeptWhole()
    {
        Assert.True(GpuCounterInstance.TryParse("pid_1_luid_0x00000000_0x00000001_phys_0_eng_7_engtype_Video_Decode", out var instance));

        Assert.Equal("Video_Decode", instance.EngineType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pid_12_phys_0_eng_1_engtype_3D")]
    [InlineData("pid_x_luid_0xZZ_0x1")]
    [InlineData("_Total")]
    public void TryParse_WithoutUsableLuid_Fails(string instance)
    {
        Assert.False(GpuCounterInstance.TryParse(instance, out _));
    }
}
