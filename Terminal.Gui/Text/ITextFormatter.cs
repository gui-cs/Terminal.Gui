#nullable enable
using System.Drawing;

namespace Terminal.Gui.Text;

/// <summary>
///     Interface for text formatting. Separates formatting concerns from rendering.
/// </summary>
public interface ITextFormatter
{
    /// <summary>Gets or sets the text to be formatted.</summary>
    string Text { get; set; }

    /// <summary>Gets or sets the size constraint for formatting.</summary>
    Size? ConstrainToSize { get; set; }

    /// <summary>Gets or sets the horizontal text alignment.</summary>
    Alignment Alignment { get; set; }

    /// <summary>Gets or sets the vertical text alignment.</summary>
    Alignment VerticalAlignment { get; set; }

    /// <summary>Gets or sets the text direction.</summary>
    TextDirection Direction { get; set; }

    /// <summary>Gets or sets whether word wrap is enabled.</summary>
    bool WordWrap { get; set; }

    /// <summary>Gets or sets whether multi-line text is allowed.</summary>
    bool MultiLine { get; set; }

    /// <summary>Gets or sets the HotKey specifier character.</summary>
    Rune HotKeySpecifier { get; set; }

    /// <summary>Gets or sets the tab width.</summary>
    int TabWidth { get; set; }

    /// <summary>Gets or sets whether trailing spaces are preserved in word-wrapped lines.</summary>
    bool PreserveTrailingSpaces { get; set; }

    /// <summary>
    ///     Formats the text and returns the formatted result.
    /// </summary>
    /// <returns>The formatted text result containing lines, size, and metadata.</returns>
    FormattedText Format();

    /// <summary>
    ///     Gets the size required to display the formatted text.
    /// </summary>
    /// <returns>The size required for the formatted text.</returns>
    Size GetFormattedSize();
}