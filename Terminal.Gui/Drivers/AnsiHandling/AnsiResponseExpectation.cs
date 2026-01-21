namespace Terminal.Gui.Drivers;

/// <summary>
///     Expectation record supporting an optional Value token used for disambiguation.
/// </summary>
internal record AnsiResponseExpectation (string? Terminator, string? Value, Action<IHeld> Response, Action? Abandoned)
{
    public bool Matches (string? cur)
    {
        if (string.IsNullOrEmpty (cur) || string.IsNullOrEmpty (Terminator))
        {
            return false;
        }

        // Must end with the terminator
        if (!cur.EndsWith (Terminator!, StringComparison.Ordinal))
        {
            return false;
        }

        // If no specific value requested, any response ending with terminator matches
        if (string.IsNullOrEmpty (Value))
        {
            return true;
        }

        // Remove leading ESC if present to simplify parsing
        string s = cur;
        if (s.Length > 0 && s[0] == '\x1B')
        {
            s = s.Substring (1);
        }

        // Extract the first numeric token after '[' (e.g. "[8;..." -> "8", "[6;..." -> "6")
        // This matches typical CSI reply formats used here.
        var m = System.Text.RegularExpressions.Regex.Match (s, @"^\[(\d+);");
        if (m.Success)
        {
            return string.Equals (m.Groups[1].Value, Value, StringComparison.Ordinal);
        }

        // Fallback: conservative contains check (rare)
        return s.Contains ($"[{Value};", StringComparison.Ordinal);
    }
}
