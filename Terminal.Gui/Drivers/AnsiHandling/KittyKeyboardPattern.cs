using System.Globalization;
using System.Text.RegularExpressions;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Parses kitty keyboard protocol CSI <c>u</c> sequences into the current <see cref="Key"/> model.
///     Phase 1 intentionally drops kitty-only event metadata such as press/release/repeat and distinct modifier keys.
/// </summary>
public class KittyKeyboardPattern : AnsiKeyboardParserPattern
{
    private readonly Regex _pattern = new (@"^\u001b\[(\d+)(?:(?::\d+)*)?(?:;([^;u]+)(?:;[^u]*)?)?u$");

    private readonly Dictionary<int, Key> _functionalKeyMap = new ()
    {
        { 27, Key.Esc },
        { 9, Key.Tab },
        { 13, Key.Enter },
        { 127, Key.Backspace },
        { 57344, Key.CursorUp },
        { 57345, Key.CursorDown },
        { 57346, Key.CursorLeft },
        { 57347, Key.CursorRight },
        { 57348, Key.PageUp },
        { 57349, Key.PageDown },
        { 57350, Key.Home },
        { 57351, Key.End },
        { 57352, Key.InsertChar },
        { 57353, Key.Delete },
        { 57354, Key.Clear },
        { 57361, Key.PrintScreen },
        { 57364, Key.F1 },
        { 57365, Key.F2 },
        { 57366, Key.F3 },
        { 57367, Key.F4 },
        { 57368, Key.F5 },
        { 57369, Key.F6 },
        { 57370, Key.F7 },
        { 57371, Key.F8 },
        { 57372, Key.F9 },
        { 57373, Key.F10 },
        { 57374, Key.F11 },
        { 57375, Key.F12 },
        { 57376, Key.F13 },
        { 57377, Key.F14 },
        { 57378, Key.F15 },
        { 57379, Key.F16 },
        { 57380, Key.F17 },
        { 57381, Key.F18 },
        { 57382, Key.F19 },
        { 57383, Key.F20 },
        { 57384, Key.F21 },
        { 57385, Key.F22 },
        { 57386, Key.F23 },
        { 57387, Key.F24 }
    };

    /// <inheritdoc />
    public override bool IsMatch (string? input) => !string.IsNullOrEmpty (input) && _pattern.IsMatch (input);

    /// <inheritdoc />
    protected override Key? GetKeyImpl (string? input)
    {
        Match match = _pattern.Match (input!);

        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse (match.Groups [1].Value, CultureInfo.InvariantCulture, out int kittyCode))
        {
            return null;
        }

        Key? key = MapKey (kittyCode);

        if (key is null)
        {
            return null;
        }

        string modifierField = match.Groups [2].Value;

        if (string.IsNullOrEmpty (modifierField))
        {
            return key;
        }

        string modifierToken = modifierField.Split (':') [0];

        if (!int.TryParse (modifierToken, CultureInfo.InvariantCulture, out int encodedModifiers))
        {
            return key;
        }

        int modifiers = Math.Max (0, encodedModifiers - 1);

        if ((modifiers & 0b1) != 0)
        {
            key = key.WithShift;
        }

        if ((modifiers & 0b10) != 0)
        {
            key = key.WithAlt;
        }

        if ((modifiers & 0b100) != 0)
        {
            key = key.WithCtrl;
        }

        return key;
    }

    private Key? MapKey (int kittyCode)
    {
        if (_functionalKeyMap.TryGetValue (kittyCode, out Key? functionalKey))
        {
            return functionalKey;
        }

        if (!Rune.IsValid (kittyCode))
        {
            return null;
        }

        return new Key (kittyCode);
    }
}
