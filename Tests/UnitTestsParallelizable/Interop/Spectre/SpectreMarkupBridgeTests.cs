// Copilot

using Spectre.Console;
using Terminal.Gui.Drawing;
using Terminal.Gui.Interop.Spectre;
using Terminal.Gui.Views;
using TgAttribute = Terminal.Gui.Drawing.Attribute;
using TgColor = Terminal.Gui.Drawing.Color;

namespace InteropTests;

public class SpectreMarkupBridgeTests
{
    [Fact]
    public void ParseMarkup_Parses_Named_Color_And_Decoration ()
    {
        IReadOnlyList<StyledSegment> segments = SpectreMarkupBridge.ParseMarkup ("[bold red]Hello[/]");

        Assert.Single (segments);
        Assert.Equal ("Hello", segments [0].Text);
        Assert.Equal (new TgColor (255, 0, 0), segments [0].Attribute!.Value.Foreground);
        Assert.True (segments [0].Attribute!.Value.Style.HasFlag (TextStyle.Bold));
    }

    [Fact]
    public void ParseMarkup_Parses_Rgb_Color ()
    {
        IReadOnlyList<StyledSegment> segments = SpectreMarkupBridge.ParseMarkup ("[#0a2030]RGB[/]");

        Assert.Single (segments);
        Assert.Equal ("RGB", segments [0].Text);
        Assert.Equal (new TgColor (0x0a, 0x20, 0x30), segments [0].Attribute!.Value.Foreground);
    }

    [Fact]
    public void ParseMarkup_Parses_Nested_Markup ()
    {
        IReadOnlyList<StyledSegment> segments = SpectreMarkupBridge.ParseMarkup ("[bold]A [underline]B[/] C[/]");

        Assert.Equal (3, segments.Count);
        Assert.Equal ("A ", segments [0].Text);
        Assert.Equal ("B", segments [1].Text);
        Assert.Equal (" C", segments [2].Text);
        Assert.True (segments [1].Attribute!.Value.Style.HasFlag (TextStyle.Bold));
        Assert.True (segments [1].Attribute!.Value.Style.HasFlag (TextStyle.Underline));
    }

    [Fact]
    public void ParseMarkup_Parses_Link_Markup ()
    {
        IReadOnlyList<StyledSegment> segments = SpectreMarkupBridge.ParseMarkup ("[link=https://example.com]Go[/]");

        Assert.Single (segments);
        Assert.Equal ("Go", segments [0].Text);
        Assert.Equal ("https://example.com", segments [0].Url);
    }

    [Fact]
    public void ToAttribute_Maps_Decoration_Flags ()
    {
        Style style = new (Spectre.Console.Color.Red, Spectre.Console.Color.Blue, Decoration.Bold | Decoration.Italic | Decoration.Underline | Decoration.SlowBlink | Decoration.Invert | Decoration.Strikethrough, null);
        TgAttribute attribute = SpectreMarkupBridge.ToAttribute (style);

        Assert.Equal (new TgColor (255, 0, 0), attribute.Foreground);
        Assert.Equal (new TgColor (0, 0, 255), attribute.Background);
        Assert.True (attribute.Style.HasFlag (TextStyle.Bold));
        Assert.True (attribute.Style.HasFlag (TextStyle.Italic));
        Assert.True (attribute.Style.HasFlag (TextStyle.Underline));
        Assert.True (attribute.Style.HasFlag (TextStyle.Blink));
        Assert.True (attribute.Style.HasFlag (TextStyle.Reverse));
        Assert.True (attribute.Style.HasFlag (TextStyle.Strikethrough));
    }

    [Fact]
    public void ToSpectreStyle_Round_Trip_Preserves_Color_And_TextStyle ()
    {
        TgAttribute source = new (new TgColor (1, 2, 3), new TgColor (4, 5, 6), TextStyle.Bold | TextStyle.Faint | TextStyle.Italic | TextStyle.Underline | TextStyle.Blink | TextStyle.Reverse | TextStyle.Strikethrough);

        Style spectre = SpectreMarkupBridge.ToSpectreStyle (source);
        TgAttribute roundTrip = SpectreMarkupBridge.ToAttribute (spectre);

        Assert.Equal (source.Foreground, roundTrip.Foreground);
        Assert.Equal (source.Background, roundTrip.Background);
        Assert.Equal (source.Style, roundTrip.Style);
    }

    [Fact]
    public void SetMarkup_Sets_Plain_Text ()
    {
        Label label = new ();

        label.SetMarkup ("[green]OK[/]");

        Assert.Equal ("OK", label.Text);
    }
}
