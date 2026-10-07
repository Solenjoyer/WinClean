using System.Runtime.InteropServices;
using Microsoft.Win32;
using WinClean.Core.Hardware;
using WinClean.Core.Hardware.Parsers;
using WinClean.Native;

namespace WinClean.Services.Hardware;

internal static class ProcessorReader
{
    private const string ProcessorKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

    public static ProcessorFacts Read()
    {
        using var key = Registry.LocalMachine.OpenSubKey(ProcessorKey);
        var name = (key?.GetValue("ProcessorNameString") as string)?.Trim() ?? string.Empty;
        var identifier = key?.GetValue("Identifier") as string;
        var baseMHz = key?.GetValue("~MHz") is int megahertz ? megahertz : 0;
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "ARM64",
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            _ => RuntimeInformation.OSArchitecture.ToString(),
        };

        return new ProcessorFacts(
            name,
            identifier,
            baseMHz,
            architecture,
            ReadTopology(),
            Kernel32.IsProcessorFeaturePresent(Kernel32.PF_AVX2_INSTRUCTIONS_AVAILABLE),
            Kernel32.IsProcessorFeaturePresent(Kernel32.PF_AVX512F_INSTRUCTIONS_AVAILABLE),
            Kernel32.IsProcessorFeaturePresent(Kernel32.PF_VIRT_FIRMWARE_ENABLED));
    }

    private static ProcessorTopology ReadTopology()
    {
        var length = 0u;
        Kernel32.GetLogicalProcessorInformationEx(Kernel32.RelationAll, [], ref length);

        if (length == 0)
        {
            return ProcessorTopology.Empty;
        }

        var buffer = new byte[length];
        return Kernel32.GetLogicalProcessorInformationEx(Kernel32.RelationAll, buffer, ref length)
            ? LogicalProcessorInfoParser.Parse(buffer.AsSpan(0, (int)length))
            : ProcessorTopology.Empty;
    }
}
