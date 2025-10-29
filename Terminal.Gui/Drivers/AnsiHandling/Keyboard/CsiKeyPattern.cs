#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Detects ansi escape sequences in strings that have been read from
///     the terminal (see <see cref="IAnsiResponseParser"/>).
///     Handles CSI key parsing such as <c>\x1b[3;5~</c> (Delete with Ctrl)
/// </summary>
public class CsiKeyPattern : AnsiKeyboardParserPattern
{
    private readonly Regex _pattern = new (@"^\u001b\[(\d+)(?:;(\d+))?~$");

    private readonly Dictionary<int, Key> _keyCodeMap = new ()
    {
        { 1, Key.Home }, // Home (modern variant)
        { 4, Key.End }, // End (modern variant)
        { 5, Key.PageUp },
        { 6, Key.PageDown },
        { 2, Key.InsertChar },
        { 3, Key.Delete },
        { 11, Key.F1 },
        { 12, Key.F2 },
        { 13, Key.F3 },
        { 14, Key.F4 },
        { 15, Key.F5 },
        { 17, Key.F6 },
        { 18, Key.F7 },
        { 19, Key.F8 },
        { 20, Key.F9 },
        { 21, Key.F10 },
        { 23, Key.F11 },
        { 24, Key.F12 }
    };

    /// <inheritdoc/>
    public override bool IsMatch (string? input) { return _pattern.IsMatch (input!); }

    /// <inheritdoc/>
    protected override Key? GetKeyImpl (string? input)
    {
        Match match = _pattern.Match (input!);

        if (!match.Success)
        {
            return null;
        }

        // Group 1: Key code (e.g. 3, 17, etc.)
        // Group 2: Optional modifier code (e.g. 2 = Shift, 5 = Ctrl)

        if (!int.TryParse (match.Groups [1].Value, out int keyCode))
        {
            return null;
        }

        if (!_keyCodeMap.TryGetValue (keyCode, out Key? key))
        {
            return null;
        }

        // If there's no modifier, just return the key.
        if (!int.TryParse (match.Groups [2].Value, out int modifier))
        {
            return key;
        }

        key = modifier switch
              {
                  2 => key.WithShift,
                  3 => key.WithAlt,
                  4 => key.WithAlt.WithShift,
                  5 => key.WithCtrl,
                  6 => key.WithCtrl.WithShift,
                  7 => key.WithCtrl.WithAlt,
                  8 => key.WithCtrl.WithAlt.WithShift,
                  _ => key
              };

        return key;
    }
}
