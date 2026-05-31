using Terminal.Gui.Input;

namespace Terminal.Gui.KeySequences;

/// <summary>Parses compact key sequence pattern strings.</summary>
public static class KeySequenceParser
{
    /// <summary>Parses a sequence pattern.</summary>
    public static KeySequencePattern Parse (string pattern)
    {
        if (string.IsNullOrWhiteSpace (pattern))
        {
            throw new ArgumentException (@"Pattern must not be empty.", nameof (pattern));
        }

        string [] parts = pattern.Split (' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            throw new ArgumentException (@"Pattern must not be empty.", nameof (pattern));
        }

        Key leaderKey = ParseKeyToken (parts [0]);
        KeySequencePattern sequencePattern = KeySequencePattern.Leader (leaderKey);
        bool hasCount = false;

        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts [i];

            if (part.Equals ("<count>", StringComparison.OrdinalIgnoreCase))
            {
                if (hasCount)
                {
                    throw new ArgumentException (@"Pattern can contain only one <count> token.", nameof (pattern));
                }

                hasCount = true;
                sequencePattern.Count ();
                continue;
            }

            if (part.Equals ("<char>", StringComparison.OrdinalIgnoreCase))
            {
                sequencePattern.Char ();
                continue;
            }

            if (part.Equals ("<key>", StringComparison.OrdinalIgnoreCase))
            {
                sequencePattern.AnyKey ();
                continue;
            }

            sequencePattern.Then (ParseKeyToken (part));
        }

        return sequencePattern;
    }

    private static Key ParseKeyToken (string token)
    {
        string keyText = token;

        if (token.StartsWith ('<') && token.EndsWith ('>'))
        {
            keyText = token [1..^1];
        }

        if (keyText.Length == 1)
        {
            return keyText [0];
        }

        if (Key.TryParse (keyText, out Key key))
        {
            return key;
        }

        throw new FormatException ($"Invalid key sequence token: {token}");
    }
}
