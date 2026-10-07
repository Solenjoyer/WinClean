namespace WinClean.Core.Applications;

/// <summary>Case-insensitive matching with * as the only wildcard: enough for "ServiceHub.*.exe".</summary>
public static class Wildcard
{
    public static bool IsMatch(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);

        return IsMatch(text.AsSpan(), pattern.AsSpan());
    }

    private static bool IsMatch(ReadOnlySpan<char> text, ReadOnlySpan<char> pattern)
    {
        while (true)
        {
            if (pattern.IsEmpty)
            {
                return text.IsEmpty;
            }

            if (pattern[0] == '*')
            {
                pattern = pattern[1..];

                if (pattern.IsEmpty)
                {
                    return true;
                }

                for (var skip = 0; skip <= text.Length; skip++)
                {
                    if (IsMatch(text[skip..], pattern))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (text.IsEmpty || char.ToUpperInvariant(text[0]) != char.ToUpperInvariant(pattern[0]))
            {
                return false;
            }

            text = text[1..];
            pattern = pattern[1..];
        }
    }
}
