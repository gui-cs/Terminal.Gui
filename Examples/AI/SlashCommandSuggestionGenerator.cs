using System.Text;
using Terminal.Gui.Views;

namespace AI;

/// <summary>
///     Suggestion generator that provides slash command completions when the input starts with '/'.
/// </summary>
internal sealed class SlashCommandSuggestionGenerator : ISuggestionGenerator
{
    private static readonly List<string> _commands = ["/help", "/clear", "/model", "/compact", "/quit", "/exit"];

    /// <inheritdoc/>
    public IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context)
    {
        List<string> line = context.CurrentLine.Select (c => c.Grapheme).ToList ();
        string lineText = string.Join ("", line).TrimEnd ();

        if (lineText.Length == 0 || lineText [0] != '/')
        {
            return [];
        }

        int typedLength = Math.Min (context.CursorPosition, lineText.Length);
        string typed = lineText [..typedLength];

        context.CursorPosition = 0;

        return _commands.Where (c => c.StartsWith (typed, StringComparison.OrdinalIgnoreCase) && !c.Equals (typed, StringComparison.OrdinalIgnoreCase))
                        .Select (c => new Suggestion (typed.Length, c))
                        .ToList ();
    }

    /// <inheritdoc/>
    public bool IsWordChar (string text)
    {
        if (string.IsNullOrEmpty (text))
        {
            return false;
        }

        Rune r = text.EnumerateRunes ().First ();

        return Rune.IsLetterOrDigit (r) || r == new Rune ('/');
    }
}