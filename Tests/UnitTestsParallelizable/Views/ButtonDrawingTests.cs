using UnitTests;

namespace ViewsTests;

public class ButtonDrawingTests (ITestOutputHelper output) : TestDriverBase
{
    // Copilot
    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void FixedWidth_Anchors_Delimiters_To_Edges (bool isDefault)
    {
        const int width = 10;
        string expected = isDefault
                              ? $"{Glyphs.LeftBracket} {Glyphs.LeftDefaultIndicator} OK {Glyphs.RightDefaultIndicator} {Glyphs.RightBracket}"
                              : $"{Glyphs.LeftBracket}   OK   {Glyphs.RightBracket}";

        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, 1);

        Runnable runnable = new () { Width = width, Height = 1 };
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_OK",
            X = 0,
            Y = 0,
            Width = width,
            Height = 1,
            IsDefault = isDefault,
            ShadowStyle = null
        };

        runnable.Add (button);
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (expected, output, app.Driver);
    }
}
