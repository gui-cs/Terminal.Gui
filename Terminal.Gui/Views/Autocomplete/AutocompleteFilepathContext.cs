using System.IO.Abstractions;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Views;

internal class AutocompleteFilepathContext (string currentLine, int cursorPosition, FileDialogState state)
    : AutocompleteContext (Cell.ToCellList (currentLine), cursorPosition)
{
    public FileDialogState State { get; set; } = state;
}

internal class FilepathSuggestionGenerator : ISuggestionGenerator
{
    private FileDialogState state;

    public IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context)
    {
        if (context is AutocompleteFilepathContext fileState)
        {
            state = fileState.State;
        }

        if (state is null)
        {
            return Enumerable.Empty<Suggestion> ();
        }

        var path = Cell.ToString (context.CurrentLine);
        int last = path.LastIndexOfAny (FileDialog.Separators);

        if (string.IsNullOrWhiteSpace (path) || !Path.IsPathRooted (path))
        {
            return Enumerable.Empty<Suggestion> ();
        }

        string term = path.Substring (last + 1);

        // If path is /tmp/ then don't just list everything in it
        if (string.IsNullOrWhiteSpace (term))
        {
            return Enumerable.Empty<Suggestion> ();
        }

        if (term.Equals (state?.Directory?.Name))
        {
            // Clear suggestions
            return Enumerable.Empty<Suggestion> ();
        }

        bool isWindows = RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

        string [] suggestions = state.Children.Where (d => !d.IsParent)
                                     .Select (
                                              e => e.FileSystemInfo is IDirectoryInfo d
                                                       ? d.Name + Path.DirectorySeparatorChar
                                                       : e.FileSystemInfo.Name
                                             )
                                     .ToArray ();

        string [] validSuggestions = suggestions
                                     .Where (
                                             s => s.StartsWith (
                                                                term,
                                                                isWindows
                                                                    ? StringComparison.InvariantCultureIgnoreCase
                                                                    : StringComparison.InvariantCulture
                                                               )
                                            )
                                     .OrderBy (m => m.Length)
                                     .ToArray ();

        // nothing to suggest
        if (validSuggestions.Length == 0 || validSuggestions [0].Length == term.Length)
        {
            return Enumerable.Empty<Suggestion> ();
        }

        return validSuggestions.Select (
                                        f => new Suggestion (term.Length, f, f)
                                       )
                               .ToList ();
    }

    public bool IsWordChar (Rune rune)
    {
        if (rune.Value == '\n')
        {
            return false;
        }

        return true;
    }
}
