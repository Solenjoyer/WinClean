namespace WinClean.Core.Health;

/// <summary>Device Manager's problem codes, in the words Device Manager uses.</summary>
public static class CmProblemCodes
{
    public static string? Describe(int code) => code switch
    {
        0 => null,
        1 => "Not configured correctly (code 1)",
        3 => "Driver may be corrupted or the system is low on resources (code 3)",
        10 => "Cannot start (code 10)",
        12 => "Not enough free resources (code 12)",
        14 => "Needs a restart (code 14)",
        18 => "Drivers need to be reinstalled (code 18)",
        19 => "Registry information is damaged (code 19)",
        22 => "Disabled (code 22)",
        24 => "Not present or not working (code 24)",
        28 => "Drivers are not installed (code 28)",
        29 => "Disabled by firmware (code 29)",
        31 => "Driver could not be loaded (code 31)",
        32 => "Driver service is disabled (code 32)",
        33 => "Resources could not be determined (code 33)",
        34 => "Needs manual configuration (code 34)",
        35 => "Firmware does not provide resources (code 35)",
        36 => "IRQ configuration failed (code 36)",
        37 => "Driver initialisation failed (code 37)",
        38 => "A previous driver instance is still loaded (code 38)",
        39 => "Driver is missing or corrupt (code 39)",
        40 => "Service key information is invalid (code 40)",
        41 => "Driver loaded but the device was not found (code 41)",
        42 => "Duplicate device (code 42)",
        43 => "Stopped because it reported problems (code 43)",
        44 => "Stopped by an application or service (code 44)",
        45 => "Not connected (code 45)",
        46 => "Being removed (code 46)",
        47 => "Prepared for safe removal (code 47)",
        48 => "Driver blocked (code 48)",
        49 => "Registry hive is too large (code 49)",
        50 => "Device properties could not be applied (code 50)",
        51 => "Waiting on another device (code 51)",
        52 => "Driver signature could not be verified (code 52)",
        53 => "Reserved by the debugger (code 53)",
        54 => "Failed and is being reset (code 54)",
        _ => $"Problem code {code}",
    };
}
