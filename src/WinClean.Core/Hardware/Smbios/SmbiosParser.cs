using System.Buffers.Binary;
using System.Text;

namespace WinClean.Core.Hardware.Smbios;

/// <summary>
/// Decodes the raw SMBIOS table that GetSystemFirmwareTable('RSMB') returns. Every accessor is bounds
/// checked, so a truncated or odd table yields a partial result instead of an exception.
/// </summary>
public static class SmbiosParser
{
    private const byte BiosInformationType = 0;
    private const byte SystemInformationType = 1;
    private const byte BaseboardInformationType = 2;
    private const byte ProcessorInformationType = 4;
    private const byte PhysicalMemoryArrayType = 16;
    private const byte MemoryDeviceType = 17;
    private const byte EndOfTableType = 127;

    private const int FirmwareTableHeaderLength = 8;

    /// <summary>Takes the RawSMBIOSData buffer: an 8-byte header (calling method, version, revision, length) and the table.</summary>
    public static SmbiosInfo ParseFirmwareTable(ReadOnlySpan<byte> rawSmbiosData)
    {
        if (rawSmbiosData.Length < FirmwareTableHeaderLength)
        {
            return SmbiosInfo.Empty;
        }

        var major = rawSmbiosData[1];
        var minor = rawSmbiosData[2];
        var length = BinaryPrimitives.ReadUInt32LittleEndian(rawSmbiosData[4..]);
        var table = rawSmbiosData[FirmwareTableHeaderLength..];

        if (length < (uint)table.Length)
        {
            table = table[..(int)length];
        }

        return Parse(table, major, minor);
    }

    public static SmbiosInfo Parse(ReadOnlySpan<byte> table, int major, int minor)
    {
        BiosInformation? bios = null;
        SystemInformation? system = null;
        BaseboardInformation? baseboard = null;
        var processors = new List<ProcessorInformation>();
        var arrays = new List<PhysicalMemoryArray>();
        var devices = new List<MemoryDevice>();

        foreach (var structure in ReadStructures(table))
        {
            switch (structure.Type)
            {
                case BiosInformationType:
                    bios ??= ReadBios(structure);
                    break;
                case SystemInformationType:
                    system ??= ReadSystem(structure);
                    break;
                case BaseboardInformationType:
                    baseboard ??= ReadBaseboard(structure);
                    break;
                case ProcessorInformationType:
                    processors.Add(ReadProcessor(structure));
                    break;
                case PhysicalMemoryArrayType:
                    arrays.Add(ReadMemoryArray(structure));
                    break;
                case MemoryDeviceType:
                    devices.Add(ReadMemoryDevice(structure));
                    break;
            }
        }

        return new SmbiosInfo(new Version(major, minor), bios, system, baseboard, processors, arrays, devices);
    }

    /// <summary>Splits the table into structures: a 4-byte header, a formatted area, then NUL-terminated strings ended by an empty one.</summary>
    public static IReadOnlyList<SmbiosStructure> ReadStructures(ReadOnlySpan<byte> table)
    {
        var structures = new List<SmbiosStructure>();
        var offset = 0;

        while (offset + 4 <= table.Length)
        {
            var type = table[offset];
            var length = table[offset + 1];
            var handle = BinaryPrimitives.ReadUInt16LittleEndian(table[(offset + 2)..]);

            if (length < 4 || offset + length > table.Length)
            {
                break;
            }

            var formatted = table.Slice(offset, length).ToArray();
            var cursor = offset + length;
            var strings = new List<string>();

            while (cursor < table.Length)
            {
                var terminator = table[cursor..].IndexOf((byte)0);

                if (terminator < 0)
                {
                    cursor = table.Length;
                    break;
                }

                if (terminator == 0)
                {
                    cursor++;
                    break;
                }

                strings.Add(Encoding.UTF8.GetString(table.Slice(cursor, terminator)));
                cursor += terminator + 1;
            }

            // A structure without strings ends with two NULs; the loop above consumed only the first.
            if (strings.Count == 0 && cursor < table.Length && table[cursor] == 0)
            {
                cursor++;
            }

            structures.Add(new SmbiosStructure(type, handle, formatted, strings));

            if (type == EndOfTableType)
            {
                break;
            }

            offset = cursor;
        }

        return structures;
    }

    private static BiosInformation ReadBios(SmbiosStructure s)
    {
        Version? revision = null;

        if (s.Has(0x15, 1) && s.ByteAt(0x14) != 0xFF)
        {
            revision = new Version(s.ByteAt(0x14), s.ByteAt(0x15));
        }

        return new BiosInformation(s.StringAt(0x04), s.StringAt(0x05), s.StringAt(0x08), revision);
    }

    private static SystemInformation ReadSystem(SmbiosStructure s)
    {
        Guid? uuid = null;

        if (s.Has(0x08, 16))
        {
            var bytes = s.Formatted.Span.Slice(0x08, 16);

            if (bytes.ContainsAnyExcept((byte)0x00) && bytes.ContainsAnyExcept((byte)0xFF))
            {
                uuid = new Guid(bytes);
            }
        }

        return new SystemInformation(
            s.StringAt(0x04),
            s.StringAt(0x05),
            s.StringAt(0x06),
            s.StringAt(0x07),
            uuid,
            s.StringAt(0x19),
            s.StringAt(0x1A));
    }

    private static BaseboardInformation ReadBaseboard(SmbiosStructure s)
    {
        return new BaseboardInformation(s.StringAt(0x04), s.StringAt(0x05), s.StringAt(0x06), s.StringAt(0x07));
    }

    private static ProcessorInformation ReadProcessor(SmbiosStructure s)
    {
        int cores = s.ByteAt(0x23);
        int threads = s.ByteAt(0x25);

        // 0xFF means "more than 255, see the 16-bit fields" (SMBIOS 3.0).
        if (cores == 0xFF)
        {
            cores = s.WordAt(0x2A);
        }

        if (threads == 0xFF)
        {
            threads = s.WordAt(0x2E);
        }

        var populated = !s.Has(0x18, 1) || (s.ByteAt(0x18) & 0x40) != 0;

        return new ProcessorInformation(
            s.StringAt(0x04),
            s.StringAt(0x07),
            s.StringAt(0x10),
            s.WordAt(0x12),
            s.WordAt(0x14),
            s.WordAt(0x16),
            cores,
            threads,
            populated);
    }

    private static PhysicalMemoryArray ReadMemoryArray(SmbiosStructure s)
    {
        long? capacity = null;
        var capacityKilobytes = s.DwordAt(0x07);

        if (capacityKilobytes == 0x80000000)
        {
            if (s.Has(0x0F, 8))
            {
                capacity = (long)s.QwordAt(0x0F);
            }
        }
        else if (capacityKilobytes != 0)
        {
            capacity = capacityKilobytes * 1024L;
        }

        var errorCorrection = s.ByteAt(0x06);

        return new PhysicalMemoryArray(
            s.Handle,
            capacity,
            s.WordAt(0x0D),
            IsSystemMemory: s.ByteAt(0x05) == 0x03,
            HasErrorCorrection: errorCorrection is 0x04 or 0x05 or 0x06 or 0x07);
    }

    private static MemoryDevice ReadMemoryDevice(SmbiosStructure s)
    {
        return new MemoryDevice(
            s.Handle,
            s.WordAt(0x04),
            ReadMemorySize(s),
            s.StringAt(0x10),
            s.StringAt(0x11),
            s.ByteAt(0x12),
            s.ByteAt(0x0E),
            ReadSpeed(s, 0x15, 0x54),
            ReadSpeed(s, 0x20, 0x58),
            s.Has(0x26, 2) && s.WordAt(0x26) != 0 ? s.WordAt(0x26) : null,
            s.StringAt(0x17),
            s.StringAt(0x18),
            s.StringAt(0x1A));
    }

    private static long? ReadMemorySize(SmbiosStructure s)
    {
        var size = s.WordAt(0x0C);

        return size switch
        {
            0 => 0,
            0xFFFF => null,
            0x7FFF => (long)(s.DwordAt(0x1C) & 0x7FFFFFFF) * 1024 * 1024,
            _ => (size & 0x8000) != 0 ? (long)(size & 0x7FFF) * 1024 : (long)size * 1024 * 1024,
        };
    }

    private static int? ReadSpeed(SmbiosStructure s, int offset, int extendedOffset)
    {
        var speed = s.WordAt(offset);

        if (speed == 0)
        {
            return null;
        }

        if (speed == 0xFFFF)
        {
            var extended = s.DwordAt(extendedOffset);
            return extended == 0 ? null : (int)Math.Min(extended, int.MaxValue);
        }

        return speed;
    }
}
