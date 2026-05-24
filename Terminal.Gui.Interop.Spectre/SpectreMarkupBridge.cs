using System.IO;
using Spectre.Console;
using Spectre.Console.Rendering;
using Terminal.Gui.Drawing;
using TgAttribute = Terminal.Gui.Drawing.Attribute;
using TgColor = Terminal.Gui.Drawing.Color;
using SpectreColor = global::Spectre.Console.Color;

namespace Terminal.Gui.Interop.Spectre;

/// <summary>Converts Spectre markup and style primitives into Terminal.Gui equivalents.</summary>
public static class SpectreMarkupBridge
{
    private static readonly IAnsiConsole _spectreConsole = CreateSpectreConsole ();

    /// <summary>Parses Spectre markup and returns styled text segments.</summary>
    /// <param name="markup">The Spectre markup string.</param>
    /// <returns>Styled segments in render order.</returns>
    public static IReadOnlyList<StyledSegment> ParseMarkup (string markup)
    {
        ArgumentNullException.ThrowIfNull (markup);

        Markup renderable = new (markup);
        List<StyledSegment> segments = [];

        foreach (Segment segment in RenderableExtensions.GetSegments (renderable, _spectreConsole))
        {
            if (segment.IsControlCode || string.IsNullOrEmpty (segment.Text))
            {
                continue;
            }

            TgAttribute attribute = ToAttribute (segment.Style);
            string? url = string.IsNullOrEmpty (segment.Style.Link) ? null : segment.Style.Link;
            segments.Add (new StyledSegment (segment.Text, MarkdownStyleRole.Normal, url: url, attribute: attribute));
        }

        return segments;
    }

    /// <summary>Converts a Spectre style to a Terminal.Gui attribute.</summary>
    /// <param name="spectreStyle">The Spectre style.</param>
    /// <returns>The converted Terminal.Gui attribute.</returns>
    public static TgAttribute ToAttribute (Style spectreStyle)
    {
        TextStyle textStyle = ToTextStyle (spectreStyle.Decoration);
        TgColor foreground = ToTgColor (spectreStyle.Foreground);
        TgColor background = ToTgColor (spectreStyle.Background);

        return new TgAttribute (foreground, background, textStyle);
    }

    /// <summary>Converts a Terminal.Gui attribute to a Spectre style.</summary>
    /// <param name="attribute">The Terminal.Gui attribute.</param>
    /// <returns>The converted Spectre style.</returns>
    public static Style ToSpectreStyle (TgAttribute attribute)
    {
        SpectreColor? foreground = ToSpectreColor (attribute.Foreground);
        SpectreColor? background = ToSpectreColor (attribute.Background);
        Decoration decoration = ToDecoration (attribute.Style);

        return new Style (foreground, background, decoration == Decoration.None ? null : decoration, null);
    }

    private static IAnsiConsole CreateSpectreConsole ()
    {
        AnsiConsoleSettings settings = new ()
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Interactive = InteractionSupport.No,
            Out = new AnsiConsoleOutput (TextWriter.Null)
        };

        return AnsiConsole.Create (settings);
    }

    private static TgColor ToTgColor (SpectreColor color)
    {
        if (color == SpectreColor.Default)
        {
            return TgColor.None;
        }

        return new TgColor (color.R, color.G, color.B);
    }

    private static SpectreColor? ToSpectreColor (TgColor color)
    {
        if (color == TgColor.None)
        {
            return null;
        }

        return new SpectreColor (color.R, color.G, color.B);
    }

    private static TextStyle ToTextStyle (Decoration decoration)
    {
        TextStyle textStyle = TextStyle.None;

        if (decoration.HasFlag (Decoration.Bold))
        {
            textStyle |= TextStyle.Bold;
        }

        if (decoration.HasFlag (Decoration.Dim))
        {
            textStyle |= TextStyle.Faint;
        }

        if (decoration.HasFlag (Decoration.Italic))
        {
            textStyle |= TextStyle.Italic;
        }

        if (decoration.HasFlag (Decoration.Underline))
        {
            textStyle |= TextStyle.Underline;
        }

        if (decoration.HasFlag (Decoration.SlowBlink) || decoration.HasFlag (Decoration.RapidBlink))
        {
            textStyle |= TextStyle.Blink;
        }

        if (decoration.HasFlag (Decoration.Invert))
        {
            textStyle |= TextStyle.Reverse;
        }

        if (decoration.HasFlag (Decoration.Strikethrough))
        {
            textStyle |= TextStyle.Strikethrough;
        }

        return textStyle;
    }

    private static Decoration ToDecoration (TextStyle style)
    {
        Decoration decoration = Decoration.None;

        if (style.HasFlag (TextStyle.Bold))
        {
            decoration |= Decoration.Bold;
        }

        if (style.HasFlag (TextStyle.Faint))
        {
            decoration |= Decoration.Dim;
        }

        if (style.HasFlag (TextStyle.Italic))
        {
            decoration |= Decoration.Italic;
        }

        if (style.HasFlag (TextStyle.Underline))
        {
            decoration |= Decoration.Underline;
        }

        if (style.HasFlag (TextStyle.Blink))
        {
            decoration |= Decoration.SlowBlink;
        }

        if (style.HasFlag (TextStyle.Reverse))
        {
            decoration |= Decoration.Invert;
        }

        if (style.HasFlag (TextStyle.Strikethrough))
        {
            decoration |= Decoration.Strikethrough;
        }

        return decoration;
    }
}
