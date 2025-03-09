#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Parses ANSI escape sequence strings that describe keyboard activity into <see cref="Key"/>.
/// </summary>
public class AnsiKeyboardParser
{
    private readonly List<AnsiKeyboardParserPattern> _patterns = new ()
    {
        new Ss3Pattern (),
        new CsiKeyPattern (),
        new EscAsAltPattern { IsLastMinute = true }
    };

    /// <summary>
    ///     Looks for any pattern that matches the <paramref name="input"/> and returns
    ///     the matching pattern or <see langword="null"/> if no matches.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="isLastMinute"></param>
    /// <returns></returns>
    public AnsiKeyboardParserPattern? IsKeyboard (string input, bool isLastMinute = false)
    {
        return _patterns.FirstOrDefault (pattern => pattern.IsLastMinute == isLastMinute && pattern.IsMatch (input));
    }
}
