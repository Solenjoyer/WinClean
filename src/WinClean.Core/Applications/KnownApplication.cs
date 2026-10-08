namespace WinClean.Core.Applications;

/// <summary>An application WinClean recognises, with the signatures that identify its processes.</summary>
public sealed record KnownApplication(
    string Id,
    string DisplayName,
    ApplicationCategory Category,
    RuleStrength Strength,
    IReadOnlyList<ProcessSignature> Signatures,
    bool InheritanceBarrier = false,
    int Priority = 0,
    string? Hint = null)
{
    public static KnownApplication Strong(string id, string displayName, ApplicationCategory category, params string[] exeNames)
    {
        return new KnownApplication(id, displayName, category, RuleStrength.Strong, [new ProcessSignature(exeNames)]);
    }

    public static KnownApplication Weak(string id, string displayName, ApplicationCategory category, params string[] exeNames)
    {
        return new KnownApplication(id, displayName, category, RuleStrength.Weak, [new ProcessSignature(exeNames)]);
    }

    public int Score(ProcessFacts facts)
    {
        var best = 0;

        foreach (var signature in Signatures)
        {
            best = Math.Max(best, signature.Score(facts));
        }

        return best == 0 ? 0 : best + Priority;
    }
}
