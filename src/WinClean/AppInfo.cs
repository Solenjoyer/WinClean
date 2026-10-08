using System.Reflection;

namespace WinClean;

internal static class AppInfo
{
    public const string Name = "WinClean";

    public const string RepositoryUrl = "https://github.com/Solenjoyer/WinClean";

    public const string ReleasesUrl = RepositoryUrl + "/releases";

    public const string IssuesUrl = RepositoryUrl + "/issues/new/choose";

    /// <summary>The version as shown to users: the informational version without the source revision suffix.</summary>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var informational = typeof(AppInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrEmpty(informational))
        {
            return typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        }

        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? informational : informational[..plus];
    }
}
