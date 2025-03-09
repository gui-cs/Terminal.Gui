#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui;

/// <summary>
///     Parser for SS3 terminal escape sequences. These describe specific keys e.g.
///     <c>EscOP</c> is F1.
/// </summary>
public class Ss3Pattern : AnsiKeyboardParserPattern
{
    private static readonly Regex _pattern = new (@"^\u001bO([PQRStDCAB])$");

    /// <inheritdoc/>
    public override bool IsMatch (string input) { return _pattern.IsMatch (input); }

    /// <summary>
    ///     Returns the ss3 key that corresponds to the provided input escape sequence
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    protected override Key? GetKeyImpl (string input)
    {
        Match match = _pattern.Match (input);

        if (!match.Success)
        {
            return null;
        }

        return match.Groups [1].Value.Single () switch
               {
                   'P' => Key.F1,
                   'Q' => Key.F2,
                   'R' => Key.F3,
                   'S' => Key.F4,
                   't' => Key.F5,
                   'D' => Key.CursorLeft,
                   'C' => Key.CursorRight,
                   'A' => Key.CursorUp,
                   'B' => Key.CursorDown,
                   _ => null
               };
    }
}
