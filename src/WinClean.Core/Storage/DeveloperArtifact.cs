namespace WinClean.Core.Storage;

/// <summary>A developer folder found during a scan, with the last time its project changed outside of it.</summary>
public sealed record DeveloperArtifact(int Node, DeveloperArtifactRule Rule, long ProjectNewestWriteTicks);
