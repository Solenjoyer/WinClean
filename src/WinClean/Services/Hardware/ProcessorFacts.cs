using WinClean.Core.Hardware;

namespace WinClean.Services.Hardware;

public sealed record ProcessorFacts(
    string Name,
    string? Identifier,
    int BaseMHz,
    string Architecture,
    ProcessorTopology Topology,
    bool Avx2,
    bool Avx512,
    bool VirtualizationEnabledInFirmware);
