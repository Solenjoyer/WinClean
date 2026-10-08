using System.Globalization;

namespace WinClean.Core.Monitoring;

/// <summary>
/// The parts of a "GPU Engine", "GPU Process Memory" or "GPU Adapter Memory" counter instance name,
/// e.g. pid_1234_luid_0x00000000_0x0000F1C9_phys_0_eng_3_engtype_3D.
/// </summary>
public readonly record struct GpuCounterInstance(int? ProcessId, long Luid, int PhysicalAdapter, int? Engine, string? EngineType)
{
    public static bool TryParse(string instance, out GpuCounterInstance result)
    {
        result = default;

        if (string.IsNullOrEmpty(instance))
        {
            return false;
        }

        var tokens = instance.Split('_');
        int? processId = null;
        long? luid = null;
        var physical = 0;
        int? engine = null;
        string? engineType = null;

        for (var index = 0; index < tokens.Length; index++)
        {
            switch (tokens[index])
            {
                case "pid" when index + 1 < tokens.Length && int.TryParse(tokens[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var pid):
                    processId = pid;
                    index++;
                    break;

                case "luid" when index + 2 < tokens.Length && TryParseHex(tokens[index + 1], out var high) && TryParseHex(tokens[index + 2], out var low):
                    luid = ((long)high << 32) | low;
                    index += 2;
                    break;

                case "phys" when index + 1 < tokens.Length && int.TryParse(tokens[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var adapter):
                    physical = adapter;
                    index++;
                    break;

                case "eng" when index + 1 < tokens.Length && int.TryParse(tokens[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var engineIndex):
                    engine = engineIndex;
                    index++;
                    break;

                case "engtype" when index + 1 < tokens.Length:
                    // Engine type names can contain underscores, so the remainder belongs to them.
                    engineType = string.Join('_', tokens, index + 1, tokens.Length - index - 1);
                    index = tokens.Length;
                    break;
            }
        }

        if (luid is null)
        {
            return false;
        }

        result = new GpuCounterInstance(processId, luid.Value, physical, engine, engineType);
        return true;
    }

    private static bool TryParseHex(string token, out uint value)
    {
        value = 0;

        return token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && uint.TryParse(token.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }
}
