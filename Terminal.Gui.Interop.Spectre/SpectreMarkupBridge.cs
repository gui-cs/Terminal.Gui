using global::Spectre.Console;
using Terminal.Gui.Drawing;
using TgAttribute = Terminal.Gui.Drawing.Attribute;
using TgColor = Terminal.Gui.Drawing.Color;

namespace Terminal.Gui.Interop.Spectre;

/// <summary>
///     Converts Spectre.Console styling to Terminal.Gui drawing attributes.
/// </summary>
public static class SpectreMarkupBridge
{
    /// <summary>
    ///     Converts a Spectre <see cref="Style"/> to a Terminal.Gui <see cref="TgAttribute"/>.
    /// </summary>
    /// <param name="style">The Spectre style to convert.</param>
    /// <returns>The converted Terminal.Gui attribute.</returns>
    public static TgAttribute ToAttribute (this Style style)
    {
        TgColor foreground = ToColor (style.Foreground);
        TgColor background = ToColor (style.Background);
        TextStyle textStyle = ToTextStyle (style.Decoration);

        return new (foreground, background, textStyle);
    }

    private static TgColor ToColor (global::Spectre.Console.Color? color)
    {
        if (color is null)
        {
            return TgColor.None;
        }

        global::Spectre.Console.Color value = color.Value;

        if (value == global::Spectre.Console.Color.Default)
        {
            return TgColor.None;
        }

        return new TgColor (value.R, value.G, value.B);
    }

    private static TextStyle ToTextStyle (Decoration? decoration)
    {
        if (decoration is null)
        {
            return TextStyle.None;
        }

        Decoration value = decoration.Value;
        TextStyle style = TextStyle.None;

        if ((value & Decoration.Bold) != 0)
        {
            style |= TextStyle.Bold;
        }

        if ((value & Decoration.Dim) != 0)
        {
            style |= TextStyle.Faint;
        }

        if ((value & Decoration.Italic) != 0)
        {
            style |= TextStyle.Italic;
        }

        if ((value & Decoration.Underline) != 0)
        {
            style |= TextStyle.Underline;
        }

        if ((value & Decoration.Invert) != 0)
        {
            style |= TextStyle.Reverse;
        }

        if ((value & (Decoration.SlowBlink | Decoration.RapidBlink)) != 0)
        {
            style |= TextStyle.Blink;
        }

        if ((value & Decoration.Strikethrough) != 0)
        {
            style |= TextStyle.Strikethrough;
        }

        return style;
    }
}
