using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using WinClean.Core.Monitoring;
using WinClean.Native;

namespace WinClean.Services.Processes;

/// <summary>
/// Reads the slow, per-process facts (path, description, command line, owner, icon) on a low-priority
/// thread, one process at a time, and keeps them for as long as the process lives. The snapshot reader
/// asks for identities it has not seen and reads whatever is ready; nothing here blocks a tick.
/// </summary>
public sealed class ProcessDetailsCache : IDisposable
{
    private readonly ConcurrentDictionary<ProcessIdentity, ProcessDetails> _details = new();

    private readonly ConcurrentDictionary<string, string?> _descriptions = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, string> _accounts = new(StringComparer.Ordinal);

    private readonly BlockingCollection<ProcessIdentity> _queue = [];

    private readonly HashSet<ProcessIdentity> _queued = [];

    private readonly ProcessIconCache _icons;

    private readonly ILogger<ProcessDetailsCache> _logger;

    private Thread? _worker;

    public ProcessDetailsCache(ProcessIconCache icons, ILogger<ProcessDetailsCache> logger)
    {
        _icons = icons;
        _logger = logger;
    }

    public ProcessDetails? TryGet(ProcessIdentity identity) => _details.TryGetValue(identity, out var details) ? details : null;

    public void Request(ProcessIdentity identity)
    {
        lock (_queued)
        {
            if (_details.ContainsKey(identity) || !_queued.Add(identity))
            {
                return;
            }
        }

        StartWorker();
        _queue.Add(identity);
    }

    public void Forget(ProcessIdentity identity)
    {
        _details.TryRemove(identity, out _);

        lock (_queued)
        {
            _queued.Remove(identity);
        }
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _worker?.Join(1000);
        _queue.Dispose();
    }

    private void StartWorker()
    {
        if (_worker is not null)
        {
            return;
        }

        // Icon extraction goes through the shell, which wants a single-threaded apartment.
        _worker = new Thread(Work)
        {
            Name = "WinClean.ProcessDetails",
            IsBackground = true,
            Priority = ThreadPriority.Lowest,
        };
        _worker.SetApartmentState(ApartmentState.STA);
        _worker.Start();
    }

    private void Work()
    {
        foreach (var identity in _queue.GetConsumingEnumerable())
        {
            ProcessDetails? details;

            try
            {
                details = Read(identity);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                _logger.LogWarning(exception, "Reading details of process {Pid} failed.", identity.Pid);
                details = ProcessDetails.Unavailable;
            }

            lock (_queued)
            {
                _queued.Remove(identity);
            }

            if (details is not null)
            {
                _details[identity] = details;
            }
        }
    }

    private ProcessDetails? Read(ProcessIdentity identity)
    {
        using var process = Kernel32.OpenProcess(Kernel32.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)identity.Pid);

        if (process.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            return error == Kernel32.ERROR_ACCESS_DENIED ? ProcessDetails.Denied : ProcessDetails.Unavailable;
        }

        // The id may already belong to a different process; that one gets its own entry later.
        if (!Kernel32.GetProcessTimes(process, out var created, out _, out _, out _) || created != identity.CreateTime)
        {
            return null;
        }

        var path = ReadPath(process);
        var (user, systemAccount, elevated) = ReadToken(process);
        var critical = Kernel32.IsProcessCritical(process, out var isCritical) && isCritical;

        return new ProcessDetails(
            path,
            path is null ? null : _descriptions.GetOrAdd(path, ReadDescription),
            ReadCommandLine(process),
            user,
            systemAccount,
            elevated,
            ReadArchitecture(process),
            critical,
            AccessDenied: false,
            path is null ? null : _icons.Get(path));
    }

    private static string? ReadPath(SafeProcessHandle process)
    {
        Span<char> buffer = stackalloc char[1024];
        var size = (uint)buffer.Length;

        if (Kernel32.QueryFullProcessImageNameW(process, 0, buffer, ref size))
        {
            return new string(buffer[..(int)size]);
        }

        if (Marshal.GetLastPInvokeError() != Kernel32.ERROR_INSUFFICIENT_BUFFER)
        {
            return null;
        }

        var longBuffer = new char[short.MaxValue];
        size = (uint)longBuffer.Length;
        return Kernel32.QueryFullProcessImageNameW(process, 0, longBuffer, ref size) ? new string(longBuffer, 0, (int)size) : null;
    }

    private static string? ReadDescription(string path)
    {
        try
        {
            var description = FileVersionInfo.GetVersionInfo(path).FileDescription?.Trim();
            return string.IsNullOrEmpty(description) ? null : description;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The command line as the process sees it, read by the kernel so it works across architectures and without reading another process's memory.</summary>
    private static string? ReadCommandLine(SafeProcessHandle process)
    {
        var buffer = GC.AllocateUninitializedArray<byte>(4096, pinned: true);

        while (true)
        {
            var status = NtDll.NtQueryInformationProcess(process, NtDll.ProcessCommandLineInformation, buffer, (uint)buffer.Length, out var needed);

            if (status is NtDll.STATUS_INFO_LENGTH_MISMATCH or NtDll.STATUS_BUFFER_TOO_SMALL or NtDll.STATUS_BUFFER_OVERFLOW)
            {
                var size = needed > buffer.Length ? (int)needed : buffer.Length * 2;
                buffer = GC.AllocateUninitializedArray<byte>(size, pinned: true);
                continue;
            }

            if (status != NtDll.STATUS_SUCCESS)
            {
                return null;
            }

            break;
        }

        // A UNICODE_STRING whose buffer follows it in the same allocation; the pointer is absolute.
        var length = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        var pointer = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(8));
        ulong address;

        unsafe
        {
            fixed (byte* start = buffer)
            {
                address = (ulong)start;
            }
        }

        if (length == 0 || pointer < address || pointer + length > address + (ulong)buffer.Length)
        {
            return null;
        }

        return Encoding.Unicode.GetString(buffer, (int)(pointer - address), length);
    }

    private (string? User, bool SystemAccount, bool? Elevated) ReadToken(SafeProcessHandle process)
    {
        if (!Advapi32.OpenProcessToken(process, Advapi32.TOKEN_QUERY, out var token))
        {
            return (null, false, null);
        }

        using (token)
        {
            string? user = null;
            var systemAccount = false;
            Span<byte> userBuffer = stackalloc byte[256];

            if (Advapi32.GetTokenInformation(token, Advapi32.TokenUser, userBuffer, (uint)userBuffer.Length, out _))
            {
                // TOKEN_USER starts with a SID_AND_ATTRIBUTES whose Sid pointer leads into this same buffer.
                nint sidPointer;

                unsafe
                {
                    fixed (byte* start = userBuffer)
                    {
                        sidPointer = *(nint*)start;
                    }
                }

                var sid = new SecurityIdentifier(sidPointer);
                systemAccount = sid.IsWellKnown(WellKnownSidType.LocalSystemSid)
                    || sid.IsWellKnown(WellKnownSidType.LocalServiceSid)
                    || sid.IsWellKnown(WellKnownSidType.NetworkServiceSid);
                user = _accounts.GetOrAdd(sid.Value, _ => AccountName(sid));
            }

            bool? elevated = null;
            Span<byte> elevation = stackalloc byte[4];

            if (Advapi32.GetTokenInformation(token, Advapi32.TokenElevation, elevation, 4, out _))
            {
                elevated = BinaryPrimitives.ReadUInt32LittleEndian(elevation) != 0;
            }

            return (user, systemAccount, elevated);
        }
    }

    private static string AccountName(SecurityIdentifier sid)
    {
        try
        {
            return sid.Translate(typeof(NTAccount)).Value;
        }
        catch (IdentityNotMappedException)
        {
            return sid.Value;
        }
        catch (SystemException)
        {
            return sid.Value;
        }
    }

    private static string? ReadArchitecture(SafeProcessHandle process)
    {
        if (!Kernel32.IsWow64Process2(process, out var processMachine, out var nativeMachine))
        {
            return null;
        }

        var machine = processMachine == Kernel32.IMAGE_FILE_MACHINE_UNKNOWN ? nativeMachine : processMachine;

        return machine switch
        {
            Kernel32.IMAGE_FILE_MACHINE_I386 => "x86",
            Kernel32.IMAGE_FILE_MACHINE_AMD64 => "x64",
            Kernel32.IMAGE_FILE_MACHINE_ARM64 => "ARM64",
            Kernel32.IMAGE_FILE_MACHINE_ARMNT => "ARM",
            _ => null,
        };
    }
}
