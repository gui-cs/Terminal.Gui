using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace ViewBaseTests.Adornments;

[Collection ("Global Test Setup")]

public class ShadowTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

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

        Assert.Equal (new (1, 1), view.Margin!.ShadowSize);
    }

    [Fact]
    public void ShadowStyle_Opaque_Margin_ShadowWidth_ShadowHeight_Cannot_Be_Set_Different_Of_One ()
    {
        View view = new () { ShadowStyle = ShadowStyle.Opaque, ShadowWidth = 3, ShadowHeight = 4 };
        Assert.Equal (1, view.ShadowWidth);
        Assert.Equal (1, view.ShadowHeight);
        Assert.Equal (1, view.Margin!.ShadowWidth);
        Assert.Equal (1, view.Margin.ShadowHeight);
    }

    [Theory]
    [InlineData (ShadowStyle.None)]
    [InlineData (ShadowStyle.Opaque)]
    [InlineData (ShadowStyle.Transparent)]
    public void Margin_ShadowWidth_ShadowHeight_Cannot_Be_Set_Less_Than_One (ShadowStyle style)
    {
        View view = new () { ShadowStyle = style };
        view.Margin!.ShadowSize = new (-1, -1);
        Assert.Equal (expectedLength, view.Margin!.ShadowSize.Width);
        Assert.Equal (expectedLength, view.Margin!.ShadowSize.Height);
    }

    [Fact]
    public void Changing_ShadowStyle_Correctly_Set_ShadowWidth_ShadowHeight_Thickness ()
    {
        View view = new () { ShadowStyle = ShadowStyle.Transparent };
        view.Margin!.ShadowSize = new (2, 2);

        Assert.Equal (new (2, 2), view.Margin!.ShadowSize);
        Assert.Equal (new (0, 0, 2, 2), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyle.None;
        Assert.Equal (new (2, 2), view.Margin!.ShadowSize);
        Assert.Equal (new (0, 0, 0, 0), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyle.Opaque;
        Assert.Equal (new (1, 1), view.Margin!.ShadowSize);
        Assert.Equal (new (0, 0, 1, 1), view.Margin.Thickness);
    }

    [Fact]
    public void ShadowStyle_Transparent_Handles_Wide_Glyphs_Correctly ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        app.Driver?.SetScreenSize (6, 5);

        // Using a replacement char to make sure wide glyphs are handled correctly
        // in the shadow area, to not confusing with a space char.
        app.Driver?.GetOutputBuffer ().SetWideGlyphReplacement (Rune.ReplacementChar);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        superview.Text = """
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         """;

        View view = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single, ShadowStyle = ShadowStyle.Transparent };
        view.Margin!.ShadowSize = view.Margin!.ShadowSize with { Width = 2 };
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

        view.Margin!.ShadowSize = new (1, 2);

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
        View view = new () { Width = 7, Height = 2, ShadowStyle = ShadowStyle.Opaque, Text = "| Hi |", HighlightStates = MouseState.Pressed };
        superview.Add (view);

        app.Begin (superview);

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              | Hi |▖
                                              ▝▀▀▀▀▀▘
                                              """,
                                              output,
                                              app.Driver);

        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 0), Flags = MouseFlags.Button1Pressed });
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
            Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single, ShadowStyle = ShadowStyle.Transparent,
            Arrangement = ViewArrangement.Movable, CanFocus = true
        };
        view.Margin!.ShadowSize = view.Margin!.ShadowSize with { Width = 2 };
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
    public void Runnable_View_Overlap_Other_Runnables ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        app.Driver?.SetScreenSize (10, 5);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill (), Text = "🍎".Repeat (25)! };
        View view = new () { Width = 7, Height = 2, ShadowStyle = ShadowStyle.Opaque, Text = "| Hi |" };
        superview.Add (view);

        app.Begin (superview);

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              | Hi |▖ 🍎
                                              ▝▀▀▀▀▀▘ 🍎
                                              🍎🍎🍎🍎🍎
                                              🍎🍎🍎🍎🍎
                                              🍎🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        Runnable modalSuperview = new () { Y = 1, Width = Dim.Fill (), Height = 4, BorderStyle = LineStyle.Single };
        View view1 = new () { Width = 8, Height = 2, ShadowStyle = ShadowStyle.Opaque, Text = "| Hey |" };
        modalSuperview.Add (view1);

        app.Begin (modalSuperview);

        Assert.True (modalSuperview.IsModal);

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              | Hi |▖ 🍎
                                              ┌────────┐
                                              │| Hey |▖│
                                              │▝▀▀▀▀▀▀▘│
                                              └────────┘
                                              """,
                                              output,
                                              app.Driver);


        app.Dispose ();
    }

    [Fact]
    public void TransparentShadow_Draws_Transparent_At_Driver_Output ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (2, 1);
        app.Driver.Force16Colors = true;

        using Runnable superView = new ();
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.Text = "AB";
        superView.TextFormatter.WordWrap = true;
        superView.SetScheme (new (new Attribute (Color.Black, Color.White)));

        // Create view with transparent shadow
        View viewWithShadow = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "*",
            ShadowStyle = ShadowStyle.Transparent
        };
        // Make it so the margin is only on the right for simplicity
        viewWithShadow.Margin!.Thickness = new (0, 0, 1, 0);
        viewWithShadow.SetScheme (new (new Attribute (Color.Black, Color.White)));

        superView.Add (viewWithShadow);

        // Act
        app.Begin (superView);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        // Assert
        _output.WriteLine ("Actual driver contents:");
        _output.WriteLine (app.Driver.ToString ());
        _output.WriteLine ("\nActual driver output:");
        string? output = app.Driver.GetOutput ().GetLastOutput ();
        _output.WriteLine (output);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[30m\x1b[107m*\x1b[90m\x1b[100mB
                                           """, _output, app.Driver);
    }

    [Fact]
    public void TransparentShadow_OverWide_Draws_Transparent_At_Driver_Output ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (2, 3);
        app.Driver.Force16Colors = true;

        using Runnable superView = new ();
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.Text = "🍎🍎🍎🍎";
        superView.TextFormatter.WordWrap = true;
        superView.SetScheme (new (new Attribute (Color.Black, Color.White)));

        // Create view with transparent shadow
        View viewWithShadow = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "*",
            ShadowStyle = ShadowStyle.Transparent
        };
        // Make it so the margin is only on the bottom for simplicity
        viewWithShadow.Margin!.Thickness = new (0, 0, 0, 1);
        viewWithShadow.SetScheme (new (new Attribute (Color.Black, Color.White)));

        superView.Add (viewWithShadow);

        // Act
        app.Begin (superView);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        // Assert
        _output.WriteLine ("Actual driver contents:");
        _output.WriteLine (app.Driver.ToString ());
        _output.WriteLine ("\nActual driver output:");
        string? output = app.Driver.GetOutput ().GetLastOutput ();
        _output.WriteLine (output);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[30m\x1b[107m*\x1b[90m\x1b[103m \x1b[97m\x1b[40m \x1b[90m\x1b[100m \x1b[97m\x1b[40m🍎
                                           """, _output, app.Driver);
    }
}
