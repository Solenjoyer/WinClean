using System.Globalization;

namespace WinClean.Core.Formatting;

public static class Durations
{
    /// <summary>The two largest units: "3 days, 4 hours", "4 hours, 12 minutes", "45 seconds".</summary>
    public static string Format(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        if (duration.TotalDays >= 1)
        {
            return Join(Unit((int)duration.TotalDays, "day"), Unit(duration.Hours, "hour"));
        }

        if (duration.TotalHours >= 1)
        {
            return Join(Unit(duration.Hours, "hour"), Unit(duration.Minutes, "minute"));
        }

        if (duration.TotalMinutes >= 1)
        {
            return Unit(duration.Minutes, "minute");
        }

        return Unit(duration.Seconds, "second");
    }

    /// <summary>How long ago something happened, coarse on purpose: "3 weeks ago", "14 months ago".</summary>
    public static string FormatRelative(DateTimeOffset moment, DateTimeOffset now)
    {
        var elapsed = now - moment;

        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return Unit((int)elapsed.TotalMinutes, "minute") + " ago";
        }

        if (elapsed < TimeSpan.FromHours(24))
        {
            return Unit((int)elapsed.TotalHours, "hour") + " ago";
        }

        var days = (int)elapsed.TotalDays;

        if (days == 1)
        {
            return "yesterday";
        }

        if (days < 14)
        {
            return Unit(days, "day") + " ago";
        }

        if (days < 60)
        {
            return Unit(days / 7, "week") + " ago";
        }

        var months = (int)(days / 30.44);

        if (months < 24)
        {
            return Unit(months, "month") + " ago";
        }

        return Unit(months / 12, "year") + " ago";
    }

    private static string Unit(int count, string singular)
    {
        return count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? singular : singular + "s");
    }

    private static string Join(string larger, string smaller)
    {
        return smaller.StartsWith("0 ", StringComparison.Ordinal) ? larger : larger + ", " + smaller;
    }
}
