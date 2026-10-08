using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinClean.Native;

/// <summary>The two DXGI calls needed to list adapters, through raw vtables rather than a generated interop layer.</summary>
[SupportedOSPlatform("windows")]
internal static unsafe partial class Dxgi
{
    internal static readonly Guid IID_IDXGIFactory1 = new("770aae78-f26f-4dba-a829-253c83d1b387");

    internal const int DXGI_ERROR_NOT_FOUND = unchecked((int)0x887A0002);

    internal const uint DXGI_ADAPTER_FLAG_SOFTWARE = 2;

    private const int ReleaseSlot = 2;

    private const int EnumAdapters1Slot = 12;

    private const int GetDesc1Slot = 10;

    [LibraryImport("dxgi.dll")]
    internal static partial int CreateDXGIFactory1(in Guid riid, out nint ppFactory);

    internal static int EnumAdapters1(nint factory, uint index, out nint adapter)
    {
        fixed (nint* result = &adapter)
        {
            return ((delegate* unmanaged<nint, uint, nint*, int>)(*(void***)factory)[EnumAdapters1Slot])(factory, index, result);
        }
    }

    internal static int GetDesc1(nint adapter, Span<byte> description)
    {
        fixed (byte* buffer = description)
        {
            return ((delegate* unmanaged<nint, byte*, int>)(*(void***)adapter)[GetDesc1Slot])(adapter, buffer);
        }
    }

    internal static void Release(nint unknown)
    {
        ((delegate* unmanaged<nint, uint>)(*(void***)unknown)[ReleaseSlot])(unknown);
    }
}
