#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui;

/// <summary>
///     Detects ansi escape sequences in strings that have been read from
///     the terminal (see <see cref="IAnsiResponseParser"/>). This pattern
///     handles keys that begin <c>Esc[</c> e.g. <c>Esc[A</c> - cursor up
/// </summary>
public class CsiKeyPattern : AnsiKeyboardParserPattern
{
    private readonly Dictionary<string, Key> _terminators = new()
    {
        { "A", Key.CursorUp },
        { "B", Key.CursorDown },
        { "C", Key.CursorRight },
        { "D", Key.CursorLeft },
        { "H", Key.Home }, // Home (older variant)
        { "F", Key.End }, // End (older variant)
        { "1~", Key.Home }, // Home (modern variant)
        { "4~", Key.End }, // End (modern variant)
        { "5~", Key.PageUp },
        { "6~", Key.PageDown },
        { "2~", Key.InsertChar },
        { "3~", Key.Delete },
        { "11~", Key.F1 },
        { "12~", Key.F2 },
        { "13~", Key.F3 },
        { "14~", Key.F4 },
        { "15~", Key.F5 },
        { "17~", Key.F6 },
        { "18~", Key.F7 },
        { "19~", Key.F8 },
        { "20~", Key.F9 },
        { "21~", Key.F10 },
        { "23~", Key.F11 },
        { "24~", Key.F12 }
    };

    private readonly Regex _pattern;

    /// <inheritdoc/>
    public override bool IsMatch (string? input) { return _pattern.IsMatch (input!); }

    /// <summary>
    ///     Creates a new instance of the <see cref="CsiKeyPattern"/> class.
    /// </summary>
    public CsiKeyPattern ()
    {
        var terms = new string (_terminators.Select (k => k.Key [0]).Where (k => !char.IsDigit (k)).ToArray ());
        _pattern = new (@$"^\u001b\[(1;(\d+))?([{terms}]|\d+~)$");
    }

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

        string terminator = match.Groups [3].Value;
        string modifierGroup = match.Groups [2].Value;

        Key? key = _terminators.GetValueOrDefault (terminator);

        if (key is {} && int.TryParse (modifierGroup, out int modifier))
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
