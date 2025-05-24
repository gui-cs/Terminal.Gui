#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui;

/// <summary>
/// Detects ansi escape sequences in strings that have been read from
/// the terminal (see <see cref="IAnsiResponseParser"/>).
/// Handles navigation CSI key parsing such as <c>\x1b[A</c> (Cursor up)
/// and <c>\x1b[1;5A</c> (Cursor up with Ctrl)
/// </summary>
public class CsiCursorPattern : AnsiKeyboardParserPattern
{
    private readonly Regex _pattern = new Regex (@"^\u001b\[(?:1;(\d+))?([A-DHF])$");

    private readonly Dictionary<char, Key> _cursorMap = new ()
    {
        { 'A', Key.CursorUp },
        { 'B', Key.CursorDown },
        { 'C', Key.CursorRight },
        { 'D', Key.CursorLeft },
        { 'H', Key.Home },
        { 'F', Key.End }
    };

    /// <inheritdoc/>
    public override bool IsMatch (string? input) { return _pattern.IsMatch (input!); }

    /// <summary>
    ///     Called by the base class to determine the key that matches the input.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    protected override Key? GetKeyImpl (string? input)
    {
        Match match = _pattern.Match (input!);
        if (!match.Success)
        {
            return null;
        }

        string modifierGroup = match.Groups [1].Value;
        char terminator = match.Groups [2].Value [0];

        if (!_cursorMap.TryGetValue (terminator, out var key))
        {
            return null;
        }

        if (string.IsNullOrEmpty (modifierGroup))
        {
            return key;
        }

        if (int.TryParse (modifierGroup, out int modifier))
        {
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
        }

        return key;
    }
}


/// <summary>
/// Detects ansi escape sequences in strings that have been read from
/// the terminal (see <see cref="IAnsiResponseParser"/>).
/// Handles CSI key parsing such as <c>\x1b[3;5~</c> (Delete with Ctrl)
/// </summary>
public class CsiKeyPattern : AnsiKeyboardParserPattern
{
    private readonly Regex _pattern = new Regex (@"^\u001b\[(\d+)(?:;(\d+))?~$");

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

        if (!_keyCodeMap.TryGetValue (keyCode, out var key))
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