using System.Buffers.Binary;
using System.Text;
using WinClean.Native;

namespace WinClean.Services.Hardware;

/// <summary>Display adapters as DXGI lists them, without the software renderer.</summary>
internal static class GpuReader
{
    private const int DescriptionLength = 312;

    public static IReadOnlyList<GpuAdapter> Read()
    {
        var adapters = new List<GpuAdapter>();

        if (Dxgi.CreateDXGIFactory1(Dxgi.IID_IDXGIFactory1, out var factory) < 0 || factory == 0)
        {
            return adapters;
        }

        try
        {
            Span<byte> description = stackalloc byte[DescriptionLength];

            for (var index = 0u; Dxgi.EnumAdapters1(factory, index, out var adapter) >= 0; index++)
            {
                try
                {
                    if (Dxgi.GetDesc1(adapter, description) < 0)
                    {
                        continue;
                    }

                    var flags = BinaryPrimitives.ReadUInt32LittleEndian(description[304..]);

                    if ((flags & Dxgi.DXGI_ADAPTER_FLAG_SOFTWARE) != 0)
                    {
                        continue;
                    }

                    var name = Encoding.Unicode.GetString(description[..256]);
                    var end = name.IndexOf('\0', StringComparison.Ordinal);

                    adapters.Add(new GpuAdapter(
                        (end < 0 ? name : name[..end]).Trim(),
                        BinaryPrimitives.ReadUInt32LittleEndian(description[256..]),
                        BinaryPrimitives.ReadUInt32LittleEndian(description[260..]),
                        (long)BinaryPrimitives.ReadUInt64LittleEndian(description[272..]),
                        (long)BinaryPrimitives.ReadUInt64LittleEndian(description[288..]),
                        BinaryPrimitives.ReadInt64LittleEndian(description[296..])));
                }
                finally
                {
                    Dxgi.Release(adapter);
                }
            }
        }
        finally
        {
            Dxgi.Release(factory);
        }

        return adapters;
    }
}
