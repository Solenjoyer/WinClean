namespace WinClean.Core.Applications;

/// <summary>
/// One way to recognise an application: executable names (with * wildcards), optional path fragments
/// and optional command-line fragments. All given parts must match.
/// </summary>
public sealed record ProcessSignature(
    IReadOnlyList<string> ExeNames,
    IReadOnlyList<string>? PathContains = null,
    IReadOnlyList<string>? CommandLineContains = null)
{
    public const int CommandLineScore = 300;

    public const int PathScore = 200;

    public const int ExeScore = 100;

    /// <summary>0 when the signature does not match, otherwise the specificity of the match.</summary>
    public int Score(ProcessFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        if (!ExeNames.Any(pattern => Wildcard.IsMatch(facts.Name, pattern)))
        {
            return 0;
        }

        if (PathContains is { Count: > 0 })
        {
            var path = Normalize(facts.Path);

            if (path is null || !PathContains.Any(fragment => path.Contains(Normalize(fragment)!, StringComparison.Ordinal)))
            {
                return 0;
            }
        }

        if (CommandLineContains is { Count: > 0 })
        {
            var commandLine = Normalize(facts.CommandLine);

            if (commandLine is null || !CommandLineContains.Any(fragment => commandLine.Contains(Normalize(fragment)!, StringComparison.Ordinal)))
            {
                return 0;
            }

            return CommandLineScore;
        }

        return PathContains is { Count: > 0 } ? PathScore : ExeScore;
    }

    /// <summary>Lower-case with backslashes, so fragments match whichever slash npm or Windows produced.</summary>
    private static string? Normalize(string? text) => text?.Replace('/', '\\').ToLowerInvariant();
}
