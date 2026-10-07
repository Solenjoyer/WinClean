using System.Buffers.Binary;
using System.Text;
using WinClean.Core.Monitoring.Parsers;

namespace WinClean.Core.Tests.Monitoring.Parsers;

/// <summary>Lays out SYSTEM_PROCESS_INFORMATION entries the way the kernel does: header, threads, then the name.</summary>
internal sealed class ProcessInformationBuffer
{
    private readonly List<byte[]> _entries = [];

    public ulong Address { get; init; } = 0x7FF6_0000_0000;

    public ProcessInformationBuffer Add(
        int pid,
        int parentPid,
        string? name,
        long createTime = 0,
        long kernelTime = 0,
        long userTime = 0,
        long privateWorkingSet = 0,
        long workingSet = 0,
        long commit = 0,
        int handles = 0,
        int sessionId = 1,
        long readTransfer = 0,
        long writeTransfer = 0,
        long otherTransfer = 0,
        int runningThreads = 1,
        int suspendedThreads = 0)
    {
        var threads = runningThreads + suspendedThreads;
        var nameBytes = name is null ? [] : Encoding.Unicode.GetBytes(name);
        var entry = new byte[ProcessInformationParser.EntrySize + threads * ProcessInformationParser.ThreadEntrySize + nameBytes.Length];
        var span = entry.AsSpan();

        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], (uint)threads);
        BinaryPrimitives.WriteInt64LittleEndian(span[0x08..], privateWorkingSet);
        BinaryPrimitives.WriteInt64LittleEndian(span[0x20..], createTime);
        BinaryPrimitives.WriteInt64LittleEndian(span[0x28..], userTime);
        BinaryPrimitives.WriteInt64LittleEndian(span[0x30..], kernelTime);
        BinaryPrimitives.WriteInt64LittleEndian(span[0x50..], pid);
        BinaryPrimitives.WriteInt64LittleEndian(span[0x58..], parentPid);
        BinaryPrimitives.WriteUInt32LittleEndian(span[0x60..], (uint)handles);
        BinaryPrimitives.WriteUInt32LittleEndian(span[0x64..], (uint)sessionId);
        BinaryPrimitives.WriteInt64LittleEndian(span[0x90..], workingSet);
        BinaryPrimitives.WriteInt64LittleEndian(span[0xB8..], commit);
        BinaryPrimitives.WriteInt64LittleEndian(span[0xE8..], readTransfer);
        BinaryPrimitives.WriteInt64LittleEndian(span[0xF0..], writeTransfer);
        BinaryPrimitives.WriteInt64LittleEndian(span[0xF8..], otherTransfer);

        for (var index = 0; index < threads; index++)
        {
            var thread = span[(ProcessInformationParser.EntrySize + index * ProcessInformationParser.ThreadEntrySize)..];
            var suspended = index >= runningThreads;
            BinaryPrimitives.WriteUInt32LittleEndian(thread[0x44..], suspended ? 5u : 2u);
            BinaryPrimitives.WriteUInt32LittleEndian(thread[0x48..], suspended ? 5u : 0u);
        }

        if (nameBytes.Length > 0)
        {
            var nameOffset = entry.Length - nameBytes.Length;
            nameBytes.CopyTo(span[nameOffset..]);
            BinaryPrimitives.WriteUInt16LittleEndian(span[0x38..], (ushort)nameBytes.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(span[0x3A..], (ushort)(nameBytes.Length + 2));
            BinaryPrimitives.WriteUInt64LittleEndian(span[0x40..], Address + (ulong)Offset(_entries.Count) + (ulong)nameOffset);
        }

        _entries.Add(entry);
        return this;
    }

    public byte[] Build()
    {
        var buffer = new byte[_entries.Sum(entry => entry.Length)];
        var offset = 0;

        for (var index = 0; index < _entries.Count; index++)
        {
            var entry = _entries[index];
            entry.CopyTo(buffer, offset);

            if (index < _entries.Count - 1)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), (uint)entry.Length);
            }

            offset += entry.Length;
        }

        return buffer;
    }

    private int Offset(int entryIndex) => _entries.Take(entryIndex).Sum(entry => entry.Length);
}
