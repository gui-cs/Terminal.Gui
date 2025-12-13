using UnitTests;
using Xunit.Abstractions;

namespace ViewBaseTests.Adornments;

[Collection ("Global Test Setup")]

public class ShadowTests (ITestOutputHelper output)
{
    [Fact]
    public void Default_None ()
    {
        var view = new View ();
        Assert.Equal (ShadowStyle.None, view.ShadowStyle);
        Assert.Equal (ShadowStyle.None, view.Margin!.ShadowStyle);
        view.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyle.None)]
    [InlineData (ShadowStyle.Opaque)]
    [InlineData (ShadowStyle.Transparent)]
    public void Set_View_Sets_Margin (ShadowStyle style)
    {
        var view = new View ();

        view.ShadowStyle = style;
        Assert.Equal (style, view.ShadowStyle);
        Assert.Equal (style, view.Margin!.ShadowStyle);
        view.Dispose ();
    }


    [Theory]
    [InlineData (ShadowStyle.None, 0, 0, 0, 0)]
    [InlineData (ShadowStyle.Opaque, 0, 0, 1, 1)]
    [InlineData (ShadowStyle.Transparent, 0, 0, 1, 1)]
    public void ShadowStyle_Margin_Thickness (ShadowStyle style, int expectedLeft, int expectedTop, int expectedRight, int expectedBottom)
    {
        var superView = new View
        {
            Height = 10, Width = 10
        };

        View view = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "0123",
            HighlightStates = MouseState.Pressed,
            ShadowStyle = style,
            CanFocus = true
        };

        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (new (expectedLeft, expectedTop, expectedRight, expectedBottom), view.Margin!.Thickness);
    }


    [Theory]
    [InlineData (ShadowStyle.None, 3)]
    [InlineData (ShadowStyle.Opaque, 4)]
    [InlineData (ShadowStyle.Transparent, 4)]
    public void Style_Changes_Margin_Thickness (ShadowStyle style, int expected)
    {
        var view = new View ();
        view.Margin!.Thickness = new (3);
        view.ShadowStyle = style;
        Assert.Equal (new (3, 3, expected, expected), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyle.None;
        Assert.Equal (new (3), view.Margin.Thickness);
        view.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyle.None)]
    [InlineData (ShadowStyle.Opaque)]
    [InlineData (ShadowStyle.Transparent)]
    public void ShadowWidth_ShadowHeight_Defaults_To_One (ShadowStyle style)
    {
        View view = new () { ShadowStyle = style };

        Assert.Equal (1, view.ShadowWidth);
        Assert.Equal (1, view.ShadowHeight);
    }

    [Fact]
    public void ShadowStyle_Opaque_Margin_ShadowWidth_ShadowHeight_Cannot_Be_Set_Different_Of_One ()
    {
        View view = new () { ShadowStyle = ShadowStyle.Opaque, ShadowWidth = 3, ShadowHeight = 4 };
        Assert.Equal (3, view.ShadowWidth);
        Assert.Equal (4, view.ShadowHeight);
        Assert.Equal (1, view.Margin!.ShadowWidth);
        Assert.Equal (1, view.Margin.ShadowHeight);
    }

    [Theory]
    [InlineData (ShadowStyle.None)]
    [InlineData (ShadowStyle.Opaque)]
    [InlineData (ShadowStyle.Transparent)]
    public void Margin_ShadowWidth_ShadowHeight_Cannot_Be_Set_Less_Than_One (ShadowStyle style)
    {
        View view = new () { ShadowStyle = style, ShadowWidth = 0, ShadowHeight = -1 };
        Assert.Equal (0, view.ShadowWidth);
        Assert.Equal (-1, view.ShadowHeight);
        Assert.Equal (1, view.Margin!.ShadowWidth);
        Assert.Equal (1, view.Margin.ShadowHeight);
    }

    [Fact]
    public void Changing_ShadowStyle_Correctly_Set_ShadowWidth_ShadowHeight_Thickness ()
    {
        View view = new () { ShadowStyle = ShadowStyle.Transparent, ShadowWidth = 2, ShadowHeight = 2 };
        Assert.Equal (2, view.ShadowWidth);
        Assert.Equal (2, view.ShadowHeight);
        Assert.Equal (2, view.Margin!.ShadowWidth);
        Assert.Equal (2, view.Margin.ShadowHeight);
        Assert.Equal (new (0, 0, 2, 2), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyle.None;
        Assert.Equal (2, view.ShadowWidth);
        Assert.Equal (2, view.ShadowHeight);
        Assert.Equal (2, view.Margin!.ShadowWidth);
        Assert.Equal (2, view.Margin.ShadowHeight);
        Assert.Equal (new (0, 0, 0, 0), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyle.Opaque;
        Assert.Equal (2, view.ShadowWidth);
        Assert.Equal (2, view.ShadowHeight);
        Assert.Equal (1, view.Margin!.ShadowWidth);
        Assert.Equal (1, view.Margin.ShadowHeight);
        Assert.Equal (new (0, 0, 1, 1), view.Margin.Thickness);
    }

    [Fact]
    public void ShadowStyle_Transparent_Handles_Wide_Glyphs_Correctly ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        app.Driver?.SetScreenSize (6, 5);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        superview.Text = """
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         """;

        View view = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single, ShadowStyle = ShadowStyle.Transparent };
        view.ShadowWidth = 2;
        superview.Add (view);

        app.Begin (superview);

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              ┌──┐🍎
                                              │  │🍎
                                              │  │🍎
                                              └──┘🍎
                                              � 🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        view.ShadowWidth = 1;
        view.ShadowHeight = 2;

        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              ┌──┐🍎
                                              │  │�
                                              └──┘�
                                              � 🍎🍎
                                              � 🍎🍎
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void ShadowStyle_Opaque_Change_Thickness_On_Mouse_Pressed_Released ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        app.Driver?.SetScreenSize (10, 4);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        View view = new () { Width = 7, Height = 2, ShadowStyle = ShadowStyle.Opaque, Text = "| Hi |", HighlightStates = MouseState.Pressed};
        superview.Add (view);

        app.Begin (superview);

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              | Hi |▖
                                              ▝▀▀▀▀▀▘
                                              """,
                                              output,
                                              app.Driver);

        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 0), Flags = MouseFlags.Button1Pressed});
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              | Hi |
                                              """,
                                              output,
                                              app.Driver);

        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 0), Flags = MouseFlags.Button1Released });
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              | Hi |▖
                                              ▝▀▀▀▀▀▘
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void ShadowStyle_Transparent_Never_Throws_Navigating_Outside_Bounds ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        app.Driver?.SetScreenSize (6, 5);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        superview.Text = """
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         """;

        View view = new ()
        {
            Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single, ShadowStyle = ShadowStyle.Transparent, ShadowWidth = 2,
            Arrangement = ViewArrangement.Movable, CanFocus = true
        };
        superview.Add (view);

        app.Begin (superview);

        Assert.Equal (new (0, 0), view.Frame.Location);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.F5.WithCtrl));

        int i = 0;
        DecrementValue (-10, Key.CursorLeft);
        Assert.Equal (-10, i);

        IncrementValue (0, Key.CursorRight);
        Assert.Equal (0, i);

        DecrementValue (-10, Key.CursorUp);
        Assert.Equal (-10, i);

        IncrementValue (20, Key.CursorDown);
        Assert.Equal (20, i);

        DecrementValue (0, Key.CursorUp);
        Assert.Equal (0, i);

        IncrementValue (20, Key.CursorRight);
        Assert.Equal (20, i);

        return;

        void DecrementValue (int count, Key key)
        {
            for (; i > count; i--)
            {
                Assert.True (app.Keyboard.RaiseKeyDownEvent (key));
                app.LayoutAndDraw ();

                CheckAssertion (new (i - 1, 0), new (0, i - 1), key);
            }
        }

        void IncrementValue (int count, Key key)
        {
            for (; i < count; i++)
            {
                Assert.True (app.Keyboard.RaiseKeyDownEvent (key));
                app.LayoutAndDraw ();

                CheckAssertion (new (i + 1, 0), new (0, i + 1), key);
            }
        }

        bool? IsColumn (Key key)
        {
            if (key == Key.CursorLeft || key == Key.CursorRight)
            {
                return true;
            }

            if (key == Key.CursorUp || key == Key.CursorDown)
            {
                return false;
            }

            return null;
        }

        void CheckAssertion (Point colLocation, Point rowLocation, Key key)
        {
            bool? isCol = IsColumn (key);

            switch (isCol)
            {
                case true:
                    Assert.Equal (colLocation, view.Frame.Location);

                    break;
                case false:
                    Assert.Equal (rowLocation, view.Frame.Location);

                    break;
                default:
                    throw new InvalidOperationException ();
            }
        }
    }

    [Theory]
    [InlineData (ShadowStyle.None, 3)]
    [InlineData (ShadowStyle.Opaque, 4)]
    [InlineData (ShadowStyle.Transparent, 4)]
    public void Margin_Thickness_Changes_Adjust_Correctly (ShadowStyle style, int expected)
    {
        var view = new View ();
        view.Margin!.Thickness = new (3);
        view.ShadowStyle = style;
        Assert.Equal (new (3, 3, expected, expected), view.Margin.Thickness);

        view.Margin.Thickness = new (3, 3, expected + 1, expected + 1);
        Assert.Equal (new (3, 3, expected + 1, expected + 1), view.Margin.Thickness);
        view.ShadowStyle = ShadowStyle.None;
        Assert.Equal (new (3, 3, 4, 4), view.Margin.Thickness);
        view.Dispose ();
    }

    [Fact]
    public void TransparentShadow_Draws_Transparent_At_Driver_Output ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (5, 3);

        // Force 16-bit colors off to get predictable RGB output
        app.Driver.Force16Colors = false;

        var superView = new Runnable
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = "ABC".Repeat (40)!
        };
        superView.SetScheme (new (new Attribute (Color.White, Color.Blue)));
        superView.TextFormatter.WordWrap = true;

        // Create an overlapped view with transparent shadow
        var overlappedView = new View
        {
            Width = 4,
            Height = 2,
            Text = "123",
            Arrangement = ViewArrangement.Overlapped,
            ShadowStyle = ShadowStyle.Transparent
        };
        overlappedView.SetScheme (new (new Attribute (Color.Black, Color.Green)));

        superView.Add (overlappedView);

        // Act
        SessionToken? token = app.Begin (superView);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        // Assert
        output.WriteLine ("Actual driver contents:");
        output.WriteLine (app.Driver.ToString ());
        output.WriteLine ("\nActual driver output:");
        string? output1 = app.Driver.GetOutput ().GetLastOutput ();
        output.WriteLine (output1);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[38;2;0;0;0m\x1b[48;2;0;128;0m123\x1b[38;2;0;0;0m\x1b[48;2;56;56;56mA\x1b[38;2;0;0;255m\x1b[48;2;255;255;255mBC\x1b[38;2;0;0;0m\x1b[48;2;56;56;56mABC\x1b[38;2;0;0;255m\x1b[48;2;255;255;255mABCABC
                                           """, output, app.Driver);

        // The output should contain ANSI color codes for the transparent shadow
        // which will have dimmed colors compared to the original
        Assert.Contains ("\x1b[38;2;", output1); // Should have RGB foreground color codes
        Assert.Contains ("\x1b[48;2;", output1); // Should have RGB background color codes

        // Verify driver contents show the background text in shadow areas
        int shadowX = overlappedView.Frame.X + overlappedView.Frame.Width;
        int shadowY = overlappedView.Frame.Y + overlappedView.Frame.Height;

        Cell shadowCell = app.Driver.Contents! [shadowY, shadowX];
        output.WriteLine ($"\nShadow cell at [{shadowY},{shadowX}]: Grapheme='{shadowCell.Grapheme}', Attr={shadowCell.Attribute}");

        // The grapheme should be from background text
        Assert.NotEqual (string.Empty, shadowCell.Grapheme);
        Assert.Contains (shadowCell.Grapheme, "ABC"); // Should be one of the background characters

        // Cleanup
        if (token is { })
        {
            app.End (token);
        }

        superView.Dispose ();
        app.Dispose ();
    }
}
