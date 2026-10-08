namespace WinClean.Core.Applications;

public enum RuleStrength
{
    /// <summary>Always starts its own group: an application the user launched or would recognise.</summary>
    Strong,

    /// <summary>Joins whatever started it when that is an application; stands alone otherwise.</summary>
    Weak,
}
