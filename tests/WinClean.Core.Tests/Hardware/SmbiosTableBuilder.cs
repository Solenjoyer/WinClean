using System.Buffers.Binary;
using System.Text;

namespace WinClean.Core.Tests.Hardware;

/// <summary>Builds SMBIOS tables byte by byte so the parser can be tested without real firmware.</summary>
internal sealed class SmbiosTableBuilder
{
    private readonly List<byte> _bytes = [];

    public SmbiosStructureBuilder Structure(byte type, int length, ushort handle = 0)
    {
        return new SmbiosStructureBuilder(this, type, length, handle);
    }

    public SmbiosTableBuilder EndOfTable()
    {
        _bytes.AddRange([127, 4, 0xFF, 0xFF, 0, 0]);
        return this;
    }

    public SmbiosTableBuilder Raw(params byte[] bytes)
    {
        _bytes.AddRange(bytes);
        return this;
    }

    public byte[] ToTable() => [.. _bytes];

    /// <summary>Wraps the table in the RawSMBIOSData header GetSystemFirmwareTable returns.</summary>
    public byte[] ToFirmwareTable(byte major = 3, byte minor = 4)
    {
        var header = new byte[8];
        header[1] = major;
        header[2] = minor;
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), (uint)_bytes.Count);
        return [.. header, .. _bytes];
    }

    internal void Append(IEnumerable<byte> bytes) => _bytes.AddRange(bytes);

    internal sealed class SmbiosStructureBuilder
    {
        private readonly SmbiosTableBuilder _table;

        private readonly byte[] _formatted;

        private readonly List<string> _strings = [];

        public SmbiosStructureBuilder(SmbiosTableBuilder table, byte type, int length, ushort handle)
        {
            _table = table;
            _formatted = new byte[length];
            _formatted[0] = type;
            _formatted[1] = (byte)length;
            BinaryPrimitives.WriteUInt16LittleEndian(_formatted.AsSpan(2), handle);
        }

        public SmbiosStructureBuilder Byte(int offset, byte value)
        {
            _formatted[offset] = value;
            return this;
        }

        public SmbiosStructureBuilder Word(int offset, ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_formatted.AsSpan(offset), value);
            return this;
        }

        public SmbiosStructureBuilder Dword(int offset, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_formatted.AsSpan(offset), value);
            return this;
        }

        public SmbiosStructureBuilder Qword(int offset, ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_formatted.AsSpan(offset), value);
            return this;
        }

        public SmbiosStructureBuilder Text(int offset, string value)
        {
            _strings.Add(value);
            _formatted[offset] = (byte)_strings.Count;
            return this;
        }

        public SmbiosStructureBuilder Bytes(int offset, params byte[] value)
        {
            value.CopyTo(_formatted, offset);
            return this;
        }

        public SmbiosTableBuilder End()
        {
            var bytes = new List<byte>(_formatted);

            foreach (var text in _strings)
            {
                bytes.AddRange(Encoding.UTF8.GetBytes(text));
                bytes.Add(0);
            }

            bytes.Add(0);

            if (_strings.Count == 0)
            {
                bytes.Add(0);
            }

            _table.Append(bytes);
            return _table;
        }
    }
}
