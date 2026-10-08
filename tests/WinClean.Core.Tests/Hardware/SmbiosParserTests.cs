using WinClean.Core.Hardware.Smbios;

namespace WinClean.Core.Tests.Hardware;

public class SmbiosParserTests
{
    [Fact]
    public void Parse_DecodesBiosSystemAndBaseboardStrings()
    {
        var table = new SmbiosTableBuilder()
            .Structure(0, 0x18).Text(0x04, "American Megatrends International, LLC.").Text(0x05, "F12 ").Text(0x08, "03/14/2025").Byte(0x14, 5).Byte(0x15, 27).End()
            .Structure(1, 0x1B).Text(0x04, "Dell Inc.").Text(0x05, "XPS 15 9530").Text(0x06, "1.0").Text(0x07, "ABC1234")
                .Bytes(0x08, 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF)
                .Text(0x19, "0A7B").Text(0x1A, "XPS").End()
            .Structure(2, 0x0F).Text(0x04, "Dell Inc.").Text(0x05, "0M4N2K").Text(0x06, "A00").Text(0x07, "/ABC1234/CNWS").End()
            .EndOfTable()
            .ToTable();

        var info = SmbiosParser.Parse(table, 3, 4);

        Assert.Equal(new Version(3, 4), info.SpecificationVersion);
        Assert.Equal("American Megatrends International, LLC.", info.Bios!.Vendor);
        Assert.Equal("F12", info.Bios.Version);
        Assert.Equal("03/14/2025", info.Bios.ReleaseDate);
        Assert.Equal(new Version(5, 27), info.Bios.Revision);
        Assert.Equal("XPS 15 9530", info.System!.ProductName);
        Assert.Equal("ABC1234", info.System.SerialNumber);
        Assert.Equal(new Guid("76543210-ba98-fedc-0123-456789abcdef"), info.System.Uuid);
        Assert.Equal("0A7B", info.System.SkuNumber);
        Assert.Equal("0M4N2K", info.Baseboard!.Product);
    }

    [Fact]
    public void Parse_ReadsMemoryDevicesWithEverySizeEncoding()
    {
        var table = new SmbiosTableBuilder()
            .Structure(16, 0x17, handle: 0x1000).Byte(0x05, 0x03).Byte(0x06, 0x03).Dword(0x07, 64 * 1024 * 1024).Word(0x0D, 4).End()
            .Structure(17, 0x5C, handle: 0x1100).Word(0x04, 0x1000).Word(0x0C, 8192).Byte(0x0E, 0x0D).Text(0x10, "DIMM A").Byte(0x12, 0x1A)
                .Word(0x15, 3200).Text(0x17, "Samsung").Text(0x1A, "M471A1K43DB1-CWE").Word(0x20, 2667).Word(0x26, 1200).End()
            .Structure(17, 0x5C, handle: 0x1101).Word(0x04, 0x1000).Word(0x0C, 0x7FFF).Dword(0x1C, 32768).Text(0x10, "DIMM B").Byte(0x12, 0x22)
                .Word(0x15, 0xFFFF).Dword(0x54, 6400).Word(0x20, 0xFFFF).Dword(0x58, 6000).End()
            .Structure(17, 0x28, handle: 0x1102).Word(0x04, 0x1000).Word(0x0C, 0x8200).Text(0x10, "DIMM C").End()
            .Structure(17, 0x28, handle: 0x1103).Word(0x04, 0x1000).Word(0x0C, 0).Text(0x10, "DIMM D").End()
            .Structure(17, 0x28, handle: 0x1104).Word(0x04, 0x1000).Word(0x0C, 0xFFFF).Text(0x10, "DIMM E").End()
            .EndOfTable()
            .ToTable();

        var info = SmbiosParser.Parse(table, 3, 2);

        var array = Assert.Single(info.MemoryArrays);
        Assert.True(array.IsSystemMemory);
        Assert.Equal(64L * 1024 * 1024 * 1024, array.MaximumCapacityBytes);
        Assert.Equal(4, array.DeviceCount);

        Assert.Equal(5, info.MemoryDevices.Count);

        var a = info.MemoryDevices[0];
        Assert.Equal(8192L * 1024 * 1024, a.SizeBytes);
        Assert.Equal("SODIMM", a.FormFactorName);
        Assert.Equal("DDR4", a.TypeName);
        Assert.Equal(3200, a.SpeedMTps);
        Assert.Equal(2667, a.ConfiguredSpeedMTps);
        Assert.Equal(1200, a.ConfiguredVoltageMillivolts);
        Assert.Equal("M471A1K43DB1-CWE", a.PartNumber);
        Assert.True(a.IsPopulated);

        var b = info.MemoryDevices[1];
        Assert.Equal(32768L * 1024 * 1024, b.SizeBytes);
        Assert.Equal("DDR5", b.TypeName);
        Assert.Equal(6400, b.SpeedMTps);
        Assert.Equal(6000, b.ConfiguredSpeedMTps);

        Assert.Equal(512L * 1024, info.MemoryDevices[2].SizeBytes);
        Assert.Equal(0, info.MemoryDevices[3].SizeBytes);
        Assert.False(info.MemoryDevices[3].IsPopulated);
        Assert.Null(info.MemoryDevices[4].SizeBytes);
        Assert.Equal(40L * 1024 * 1024 * 1024 + 512 * 1024, info.InstalledMemoryBytes);
    }

    [Fact]
    public void Parse_ReadsProcessorCountsIncludingTheExtendedFields()
    {
        var table = new SmbiosTableBuilder()
            .Structure(4, 0x30).Text(0x04, "LGA1700").Text(0x07, "Intel(R) Corporation").Text(0x10, "13th Gen Intel(R) Core(TM) i7-13700K")
                .Word(0x12, 100).Word(0x14, 5400).Word(0x16, 3400).Byte(0x18, 0x41).Byte(0x23, 16).Byte(0x25, 24).End()
            .Structure(4, 0x30).Text(0x04, "SP5").Byte(0x18, 0x41).Byte(0x23, 0xFF).Byte(0x25, 0xFF).Word(0x2A, 384).Word(0x2E, 768).End()
            .Structure(4, 0x30).Text(0x04, "CPU 2").Byte(0x18, 0x01).End()
            .EndOfTable()
            .ToTable();

        var info = SmbiosParser.Parse(table, 3, 0);

        Assert.Equal(3, info.Processors.Count);
        Assert.Equal("13th Gen Intel(R) Core(TM) i7-13700K", info.Processors[0].Version);
        Assert.Equal(16, info.Processors[0].CoreCount);
        Assert.Equal(24, info.Processors[0].ThreadCount);
        Assert.Equal(5400, info.Processors[0].MaxSpeedMHz);
        Assert.Equal(384, info.Processors[1].CoreCount);
        Assert.Equal(768, info.Processors[1].ThreadCount);
        Assert.False(info.Processors[2].Populated);
    }

    [Fact]
    public void Parse_StringIndexZeroAndOutOfRange_YieldEmptyStrings()
    {
        var table = new SmbiosTableBuilder()
            .Structure(2, 0x0F).Text(0x04, "Only one string").Byte(0x05, 0).Byte(0x06, 9).End()
            .EndOfTable()
            .ToTable();

        var info = SmbiosParser.Parse(table, 2, 8);

        Assert.Equal("Only one string", info.Baseboard!.Manufacturer);
        Assert.Equal(string.Empty, info.Baseboard.Product);
        Assert.Equal(string.Empty, info.Baseboard.Version);
    }

    [Fact]
    public void Parse_StructuresWithoutStrings_AreSeparatedCorrectly()
    {
        var table = new SmbiosTableBuilder()
            .Structure(16, 0x17, handle: 1).Byte(0x05, 0x03).End()
            .Structure(16, 0x17, handle: 2).Byte(0x05, 0x03).End()
            .Structure(2, 0x0F).Text(0x04, "Board").End()
            .EndOfTable()
            .ToTable();

        var info = SmbiosParser.Parse(table, 2, 8);

        Assert.Equal(2, info.MemoryArrays.Count);
        Assert.Equal("Board", info.Baseboard!.Manufacturer);
    }

    [Fact]
    public void Parse_StopsAtEndOfTableStructure()
    {
        var table = new SmbiosTableBuilder()
            .Structure(2, 0x0F).Text(0x04, "Board").End()
            .EndOfTable()
            .Structure(2, 0x0F).Text(0x04, "Ignored").End()
            .ToTable();

        var info = SmbiosParser.Parse(table, 2, 8);

        Assert.Equal("Board", info.Baseboard!.Manufacturer);
    }

    [Fact]
    public void Parse_TruncatedTable_ReturnsWhatWasReadable()
    {
        var full = new SmbiosTableBuilder()
            .Structure(2, 0x0F).Text(0x04, "Board").End()
            .Structure(17, 0x28).Word(0x0C, 4096).Text(0x10, "DIMM A").End()
            .EndOfTable()
            .ToTable();

        for (var cut = 0; cut < full.Length; cut++)
        {
            var info = SmbiosParser.Parse(full.AsSpan(0, cut), 2, 8);
            Assert.NotNull(info);
        }

        var truncatedInsideSecondStructure = SmbiosParser.Parse(full.AsSpan(0, 22), 2, 8);
        Assert.Equal("Board", truncatedInsideSecondStructure.Baseboard!.Manufacturer);
        Assert.Empty(truncatedInsideSecondStructure.MemoryDevices);
    }

    [Fact]
    public void Parse_GarbageNeverThrows()
    {
        var random = new Random(42);

        for (var attempt = 0; attempt < 200; attempt++)
        {
            var garbage = new byte[random.Next(0, 300)];
            random.NextBytes(garbage);

            var info = SmbiosParser.Parse(garbage, 3, 0);
            Assert.NotNull(info);
        }
    }

    [Fact]
    public void ParseFirmwareTable_HonoursHeaderVersionAndLength()
    {
        var builder = new SmbiosTableBuilder()
            .Structure(2, 0x0F).Text(0x04, "Board").End()
            .EndOfTable();
        var buffer = builder.ToFirmwareTable(major: 3, minor: 6);

        var info = SmbiosParser.ParseFirmwareTable(buffer);

        Assert.Equal(new Version(3, 6), info.SpecificationVersion);
        Assert.Equal("Board", info.Baseboard!.Manufacturer);
        Assert.Equal(SmbiosInfo.Empty, SmbiosParser.ParseFirmwareTable([1, 2, 3]));
    }

    [Fact]
    public void Parse_AllZeroOrAllFfUuid_IsTreatedAsMissing()
    {
        var zero = new SmbiosTableBuilder().Structure(1, 0x1B).Text(0x04, "Vendor").End().EndOfTable().ToTable();
        var ff = new SmbiosTableBuilder().Structure(1, 0x1B).Text(0x04, "Vendor")
            .Bytes(0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF).End()
            .EndOfTable().ToTable();

        Assert.Null(SmbiosParser.Parse(zero, 2, 8).System!.Uuid);
        Assert.Null(SmbiosParser.Parse(ff, 2, 8).System!.Uuid);
    }
}
