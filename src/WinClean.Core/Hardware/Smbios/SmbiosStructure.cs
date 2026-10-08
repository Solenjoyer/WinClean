using System.Buffers.Binary;

namespace WinClean.Core.Hardware.Smbios;

/// <summary>
/// One raw SMBIOS structure: the formatted area plus its string set. Field accessors are bounds
/// checked and return zero or an empty string for fields that an older table does not carry.
/// </summary>
public sealed class SmbiosStructure
{
    public SmbiosStructure(byte type, ushort handle, ReadOnlyMemory<byte> formatted, IReadOnlyList<string> strings)
    {
        Type = type;
        Handle = handle;
        Formatted = formatted;
        Strings = strings;
    }

    public byte Type { get; }

    public ushort Handle { get; }

    public ReadOnlyMemory<byte> Formatted { get; }

    public IReadOnlyList<string> Strings { get; }

    public int Length => Formatted.Length;

    public bool Has(int offset, int size) => offset >= 0 && offset + size <= Formatted.Length;

    public byte ByteAt(int offset) => Has(offset, 1) ? Formatted.Span[offset] : (byte)0;

    public ushort WordAt(int offset) => Has(offset, 2) ? BinaryPrimitives.ReadUInt16LittleEndian(Formatted.Span[offset..]) : (ushort)0;

    public uint DwordAt(int offset) => Has(offset, 4) ? BinaryPrimitives.ReadUInt32LittleEndian(Formatted.Span[offset..]) : 0;

    public ulong QwordAt(int offset) => Has(offset, 8) ? BinaryPrimitives.ReadUInt64LittleEndian(Formatted.Span[offset..]) : 0;

    /// <summary>String fields hold a 1-based index into the string set; 0 and out-of-range mean "none".</summary>
    public string StringAt(int offset)
    {
        var index = ByteAt(offset);
        return index == 0 || index > Strings.Count ? string.Empty : Strings[index - 1].Trim();
    }
}
