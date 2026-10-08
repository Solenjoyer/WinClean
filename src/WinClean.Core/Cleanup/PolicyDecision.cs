namespace WinClean.Core.Cleanup;

public sealed record PolicyDecision(bool Allowed, PolicyRejection Rejection)
{
    public static PolicyDecision Yes { get; } = new(true, PolicyRejection.None);

    public static PolicyDecision No(PolicyRejection rejection) => new(false, rejection);
}
