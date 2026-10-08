namespace WinClean.Core.Health;

/// <summary>
/// Servicing dates as published on Microsoft's release-health pages. The table is data bundled with
/// a release of WinClean, so the UI always says when it was last updated instead of claiming to know.
/// </summary>
public static class ServicingTable
{
    public static DateOnly DataAsOf { get; } = new(2026, 9, 29);

    public static IReadOnlyList<WindowsRelease> Releases { get; } =
    [
        new("Windows 11", "25H2", 26200, false, new(2025, 9, 30), new(2027, 10, 12), new(2028, 10, 10)),
        new("Windows 11", "24H2", 26100, false, new(2024, 10, 1), new(2026, 10, 13), new(2027, 10, 12)),
        new("Windows 11", "Enterprise LTSC 2024", 26100, true, new(2024, 10, 1), null, new(2029, 10, 9), "IoT Enterprise LTSC 2024 is serviced until 10 Oct 2034."),
        new("Windows 11", "23H2", 22631, false, new(2023, 10, 31), new(2025, 11, 11), new(2026, 11, 10)),
        new("Windows 11", "22H2", 22621, false, new(2022, 9, 20), new(2024, 10, 8), new(2025, 10, 14)),
        new("Windows 11", "21H2", 22000, false, new(2021, 10, 4), new(2023, 10, 10), new(2024, 10, 8)),
        new("Windows 10", "22H2", 19045, false, new(2022, 10, 18), new(2025, 10, 14), new(2025, 10, 14), "Extended Security Updates run until 13 Oct 2026 for consumers and up to 10 Oct 2028 for organisations."),
        new("Windows 10", "21H2", 19044, false, new(2021, 11, 16), new(2023, 6, 13), new(2024, 6, 11)),
        new("Windows 10", "Enterprise LTSC 2021", 19044, true, new(2021, 11, 16), null, new(2027, 1, 12), "IoT Enterprise LTSC 2021 is serviced until 13 Jan 2032."),
        new("Windows 10", "21H1", 19043, false, new(2021, 5, 18), new(2022, 12, 13), new(2022, 12, 13)),
        new("Windows 10", "20H2", 19042, false, new(2020, 10, 20), new(2022, 5, 10), new(2023, 5, 9)),
        new("Windows 10", "2004", 19041, false, new(2020, 5, 27), new(2021, 12, 14), new(2021, 12, 14)),
        new("Windows 10", "1909", 18363, false, new(2019, 11, 12), new(2021, 5, 11), new(2022, 5, 10)),
        new("Windows 10", "1903", 18362, false, new(2019, 5, 21), new(2020, 12, 8), new(2020, 12, 8)),
        new("Windows 10", "1809", 17763, false, new(2018, 11, 13), new(2020, 11, 10), new(2021, 5, 11)),
        new("Windows 10", "Enterprise LTSC 2019", 17763, true, new(2018, 11, 13), null, new(2029, 1, 9)),
        new("Windows 10", "Enterprise LTSB 2016", 14393, true, new(2016, 8, 2), null, new(2026, 10, 13)),
    ];

    public static WindowsRelease? Find(int build, string? editionId)
    {
        var longTerm = IsLongTermServicingEdition(editionId);

        return Releases.FirstOrDefault(release => release.Build == build && release.LongTermServicing == longTerm)
            ?? Releases.FirstOrDefault(release => release.Build == build);
    }

    public static ServicingStatus Evaluate(int build, string? editionId, DateOnly today)
    {
        var release = Find(build, editionId);

        if (release is null)
        {
            return new ServicingStatus(ServicingState.Unknown, null, null, false);
        }

        var enterprise = release.LongTermServicing || IsEnterpriseEdition(editionId);
        var end = enterprise ? release.EnterpriseEnd ?? release.ConsumerEnd : release.ConsumerEnd ?? release.EnterpriseEnd;
        var state = end is null || end.Value >= today ? ServicingState.Supported : ServicingState.Ended;

        return new ServicingStatus(state, release, end, enterprise);
    }

    /// <summary>Editions that follow the Enterprise and Education column of Microsoft's tables.</summary>
    public static bool IsEnterpriseEdition(string? editionId)
    {
        if (string.IsNullOrEmpty(editionId))
        {
            return false;
        }

        return editionId.StartsWith("Enterprise", StringComparison.OrdinalIgnoreCase)
            || editionId.StartsWith("Education", StringComparison.OrdinalIgnoreCase)
            || editionId.StartsWith("IoTEnterprise", StringComparison.OrdinalIgnoreCase)
            || editionId.StartsWith("ServerRdsh", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>LTSC and LTSB editions carry "EnterpriseS" in their edition id.</summary>
    public static bool IsLongTermServicingEdition(string? editionId)
    {
        return editionId is not null && editionId.Contains("EnterpriseS", StringComparison.OrdinalIgnoreCase);
    }
}
