#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui.Drivers;

internal class EscAsAltPattern : AnsiKeyboardParserPattern
{
    public EscAsAltPattern () { IsLastMinute = true; }

#pragma warning disable IDE1006 // Naming Styles
    private static readonly Regex _pattern = new (@"^\u001b([a-zA-Z0-9_])$");
#pragma warning restore IDE1006 // Naming Styles

    public override bool IsMatch (string? input) { return _pattern.IsMatch (input!); }

    protected override Key? GetKeyImpl (string? input)
    {
        Match match = _pattern.Match (input!);

        if (!match.Success)
        {
            return null;
        }

        char key = match.Groups [1].Value [0];

        return new Key (key).WithAlt;
    }
}
