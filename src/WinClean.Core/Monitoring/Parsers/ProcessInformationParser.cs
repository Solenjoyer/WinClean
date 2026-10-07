using System.Buffers.Binary;
using System.Text;

namespace WinClean.Core.Monitoring.Parsers;

/// <summary>
/// Walks the buffer NtQuerySystemInformation(SystemProcessInformation) fills, using the 64-bit layout.
/// Image names are absolute pointers into the same buffer, so the caller passes the address the buffer
/// had during the call; a pointer outside the buffer is treated as no name.
/// </summary>
public static class ProcessInformationParser
{
    /// <summary>sizeof(SYSTEM_PROCESS_INFORMATION) without the trailing thread array.</summary>
    public const int EntrySize = 256;

    /// <summary>sizeof(SYSTEM_THREAD_INFORMATION).</summary>
    public const int ThreadEntrySize = 80;

    private const int ThreadStateWait = 5;

    private const int WaitReasonSuspended = 5;

    public static void Parse(ReadOnlySpan<byte> buffer, ulong bufferAddress, List<ProcessRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        records.Clear();

        var offset = 0;

        while (offset + EntrySize <= buffer.Length)
        {
            var entry = buffer[offset..];
            var next = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry);
            var entryLength = next == 0 ? buffer.Length - offset : next;
            var threads = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[4..]);
            var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(entry[0x38..]);
            var namePointer = BinaryPrimitives.ReadUInt64LittleEndian(entry[0x40..]);
            var nameOffset = ResolveName(namePointer, bufferAddress, nameLength, buffer.Length);

            records.Add(new ProcessRecord(
                Pid: (int)BinaryPrimitives.ReadInt64LittleEndian(entry[0x50..]),
                ParentPid: (int)BinaryPrimitives.ReadInt64LittleEndian(entry[0x58..]),
                CreateTime: BinaryPrimitives.ReadInt64LittleEndian(entry[0x20..]),
                KernelTime: BinaryPrimitives.ReadInt64LittleEndian(entry[0x30..]),
                UserTime: BinaryPrimitives.ReadInt64LittleEndian(entry[0x28..]),
                PrivateWorkingSet: BinaryPrimitives.ReadInt64LittleEndian(entry[0x08..]),
                WorkingSet: BinaryPrimitives.ReadInt64LittleEndian(entry[0x90..]),
                Commit: BinaryPrimitives.ReadInt64LittleEndian(entry[0xB8..]),
                Threads: threads,
                SuspendedThreads: CountSuspendedThreads(entry[..Math.Min(entryLength, entry.Length)], threads),
                Handles: (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[0x60..]),
                SessionId: (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[0x64..]),
                BasePriority: BinaryPrimitives.ReadInt32LittleEndian(entry[0x48..]),
                ReadTransfer: BinaryPrimitives.ReadInt64LittleEndian(entry[0xE8..]),
                WriteTransfer: BinaryPrimitives.ReadInt64LittleEndian(entry[0xF0..]),
                OtherTransfer: BinaryPrimitives.ReadInt64LittleEndian(entry[0xF8..]),
                NameOffset: nameOffset,
                NameLength: nameOffset < 0 ? 0 : nameLength));

            if (next < EntrySize)
            {
                break;
            }

            offset += next;
        }
    }

    public static string ReadName(ReadOnlySpan<byte> buffer, in ProcessRecord record)
    {
        return record.NameLength == 0 ? string.Empty : Encoding.Unicode.GetString(buffer.Slice(record.NameOffset, record.NameLength));
    }

    private static int ResolveName(ulong pointer, ulong bufferAddress, int length, int bufferLength)
    {
        if (length == 0 || pointer < bufferAddress)
        {
            return -1;
        }

        var offset = pointer - bufferAddress;
        return offset + (ulong)length <= (ulong)bufferLength ? (int)offset : -1;
    }

    private static int CountSuspendedThreads(ReadOnlySpan<byte> entry, int threads)
    {
        var available = (entry.Length - EntrySize) / ThreadEntrySize;
        var count = Math.Min(threads, available);
        var suspended = 0;

        for (var index = 0; index < count; index++)
        {
            var thread = entry[(EntrySize + index * ThreadEntrySize)..];
            var state = BinaryPrimitives.ReadUInt32LittleEndian(thread[0x44..]);
            var reason = BinaryPrimitives.ReadUInt32LittleEndian(thread[0x48..]);

            if (state == ThreadStateWait && reason == WaitReasonSuspended)
            {
                suspended++;
            }
        }

        return suspended;
    }
}
