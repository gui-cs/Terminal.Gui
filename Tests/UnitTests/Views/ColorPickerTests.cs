using UnitTests;

namespace Terminal.Gui.ViewsTests;

public class ColorPickerTests
{
    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_ChangedEvent_Fires ()
    {
        Color newColor = default;
        var count = 0;

        var cp = new ColorPicker ();

        cp.ColorChanged += (s, e) =>
                           {
                               count++;
                               newColor = e.Result;

                               Assert.Equal (cp.SelectedColor, e.Result);
                           };

        cp.SelectedColor = new (1, 2, 3);
        Assert.Equal (1, count);
        Assert.Equal (new (1, 2, 3), newColor);

        cp.SelectedColor = new (2, 3, 4);

        Assert.Equal (2, count);
        Assert.Equal (new (2, 3, 4), newColor);

        // Set to same value
        cp.SelectedColor = new (2, 3, 4);

        // Should have no effect
        Assert.Equal (2, count);
    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_ChangeValueOnUI_UpdatesAllUIElements ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true);

        var otherView = new View { CanFocus = true };

        Application.Top?.Add (otherView); // thi sets focus to otherView
        Assert.True (otherView.HasFocus);

        cp.SetFocus ();
        Assert.False (otherView.HasFocus);

        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);
        TextField rTextField = GetTextField (cp, ColorPickerPart.Bar1);
        TextField gTextField = GetTextField (cp, ColorPickerPart.Bar2);
        TextField bTextField = GetTextField (cp, ColorPickerPart.Bar3);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.Equal ("0", rTextField.Text);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("0", gTextField.Text);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("0", bTextField.Text);
        Assert.Equal ("#000000", hex.Text);

        // Change value using text field
        TextField rBarTextField = cp.SubViews.OfType<TextField> ().First (tf => tf.Text == "0");

        rBarTextField.SetFocus ();
        rBarTextField.Text = "128";

        otherView.SetFocus ();
        Assert.True (otherView.HasFocus);

        cp.Draw ();

        Assert.Equal ("R:", r.Text);
        Assert.Equal (9, r.TrianglePosition);
        Assert.Equal ("128", rTextField.Text);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("0", gTextField.Text);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("0", bTextField.Text);
        Assert.Equal ("#800000", hex.Text);

        Application.Top?.Dispose ();
        Application.ResetState();
    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_ClickingAtEndOfBar_SetsMaxValue ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);

        cp.Draw ();

        // Click at the end of the Red bar
        cp.Focused.RaiseMouseEvent (
                                    new ()
                                    {
                                        Flags = MouseFlags.Button1Pressed,
                                        Position = new (19, 0) // Assuming 0-based indexing
                                    });

        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        Application.Top?.Dispose ();
        Application.ResetState ();

    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_ClickingBeyondBar_ChangesToMaxValue ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);

        cp.Draw ();

        // Click beyond the bar
        cp.Focused.RaiseMouseEvent (
                                    new ()
                                    {
                                        Flags = MouseFlags.Button1Pressed,
                                        Position = new (21, 0) // Beyond the bar
                                    });

        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        Application.Top?.Dispose ();
        Application.ResetState ();

    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_ClickingDifferentBars_ChangesFocus ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);

        cp.Draw ();

        // Click on Green bar
        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         Flags = MouseFlags.Button1Pressed,
                                         ScreenPosition = new (0, 1)
                                     });

        //cp.SubViews.OfType<GBar> ()
        //  .Single ()
        //  .OnMouseEvent (
        //                 new ()
        //                 {
        //                     Flags = MouseFlags.Button1Pressed,
        //                     Position = new (0, 1)
        //                 });

        cp.Draw ();

        Assert.IsAssignableFrom<GBar> (cp.Focused);

        // Click on Blue bar
        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         Flags = MouseFlags.Button1Pressed,
                                         ScreenPosition = new (0, 2)
                                     });

        //cp.SubViews.OfType<BBar> ()
        //  .Single ()
        //  .OnMouseEvent (
        //                 new ()
        //                 {
        //                     Flags = MouseFlags.Button1Pressed,
        //                     Position = new (0, 2)
        //                 });

        cp.Draw ();

        Assert.IsAssignableFrom<BBar> (cp.Focused);

        Application.Top?.Dispose ();
        Application.ResetState ();

    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_Construct_DefaultValue ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.HSV, false);

        // Should be only a single text field (Hex) because ShowTextFields is false
        Assert.Single (cp.SubViews.OfType<TextField> ());

        cp.Draw ();

        // All bars should be at 0 with the triangle at 0 (+2 because of "H:", "S:" etc)
        ColorBar h = GetColorBar (cp, ColorPickerPart.Bar1);
        Assert.Equal ("H:", h.Text);
        Assert.Equal (2, h.TrianglePosition);
        Assert.IsType<HueBar> (h);

        ColorBar s = GetColorBar (cp, ColorPickerPart.Bar2);
        Assert.Equal ("S:", s.Text);
        Assert.Equal (2, s.TrianglePosition);
        Assert.IsType<SaturationBar> (s);

        ColorBar v = GetColorBar (cp, ColorPickerPart.Bar3);
        Assert.Equal ("V:", v.Text);
        Assert.Equal (2, v.TrianglePosition);
        Assert.IsType<ValueBar> (v);

        TextField hex = GetTextField (cp, ColorPickerPart.Hex);
        Assert.Equal ("#000000", hex.Text);
    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_DisposesOldViews_OnModelChange ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.HSL, true);

        ColorBar b1 = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar b2 = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b3 = GetColorBar (cp, ColorPickerPart.Bar3);

        TextField tf1 = GetTextField (cp, ColorPickerPart.Bar1);
        TextField tf2 = GetTextField (cp, ColorPickerPart.Bar2);
        TextField tf3 = GetTextField (cp, ColorPickerPart.Bar3);

        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

#if DEBUG_IDISPOSABLE
        Assert.All (new View [] { b1, b2, b3, tf1, tf2, tf3, hex }, b => Assert.False (b.WasDisposed));
#endif
        cp.Style.ColorModel = ColorModel.RGB;
        cp.ApplyStyleChanges ();

        ColorBar b1After = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar b2After = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b3After = GetColorBar (cp, ColorPickerPart.Bar3);

        TextField tf1After = GetTextField (cp, ColorPickerPart.Bar1);
        TextField tf2After = GetTextField (cp, ColorPickerPart.Bar2);
        TextField tf3After = GetTextField (cp, ColorPickerPart.Bar3);

        TextField hexAfter = GetTextField (cp, ColorPickerPart.Hex);

        // Old bars should be disposed
#if DEBUG_IDISPOSABLE
        Assert.All (new View [] { b1, b2, b3, tf1, tf2, tf3, hex }, b => Assert.True (b.WasDisposed));
#endif
        Assert.NotSame (hex, hexAfter);

        Assert.NotSame (b1, b1After);
        Assert.NotSame (b2, b2After);
        Assert.NotSame (b3, b3After);

        Assert.NotSame (tf1, tf1After);
        Assert.NotSame (tf2, tf2After);
        Assert.NotSame (tf3, tf3After);
    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_EnterHexFor_ColorName ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true, true);

        cp.Draw ();

        TextField name = GetTextField (cp, ColorPickerPart.ColorName);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        hex.SetFocus ();

        Assert.True (hex.HasFocus);
        Assert.Same (hex, cp.Focused);

        hex.Text = "";
        name.Text = "";

        Assert.Empty (hex.Text);
        Assert.Empty (name.Text);

        Application.RaiseKeyDownEvent ('#');
        Assert.Empty (name.Text);

        //7FFFD4

        Assert.Equal ("#", hex.Text);
        Application.RaiseKeyDownEvent ('7');
        Application.RaiseKeyDownEvent ('F');
        Application.RaiseKeyDownEvent ('F');
        Application.RaiseKeyDownEvent ('F');
        Application.RaiseKeyDownEvent ('D');
        Assert.Empty (name.Text);

        Application.RaiseKeyDownEvent ('4');

        Assert.True (hex.HasFocus);

        // Tab out of the hex field - should wrap to first focusable subview 
        Application.RaiseKeyDownEvent (Key.Tab);
        Assert.False (hex.HasFocus);
        Assert.NotSame (hex, cp.Focused);

        // Color name should be recognised as a known string and populated
        Assert.Equal ("#7FFFD4", hex.Text);
        Assert.Equal ("Aquamarine", name.Text);

        Application.Top?.Dispose ();
        Application.ResetState (true);
    }

    /// <summary>
    ///     In this version we use the Enter button to accept the typed text instead
    ///     of tabbing to the next view.
    /// </summary>
    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_EnterHexFor_ColorName_AcceptVariation ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true, true);

        cp.Draw ();

        TextField name = GetTextField (cp, ColorPickerPart.ColorName);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        hex.SetFocus ();

        Assert.True (hex.HasFocus);
        Assert.Same (hex, cp.Focused);

        hex.Text = "";
        name.Text = "";

        Assert.Empty (hex.Text);
        Assert.Empty (name.Text);

        Application.RaiseKeyDownEvent ('#');
        Assert.Empty (name.Text);

        //7FFFD4

        Assert.Equal ("#", hex.Text);
        Application.RaiseKeyDownEvent ('7');
        Application.RaiseKeyDownEvent ('F');
        Application.RaiseKeyDownEvent ('F');
        Application.RaiseKeyDownEvent ('F');
        Application.RaiseKeyDownEvent ('D');
        Assert.Empty (name.Text);

        Application.RaiseKeyDownEvent ('4');

        Assert.True (hex.HasFocus);

        // Should stay in the hex field (because accept not tab)
        Application.RaiseKeyDownEvent (Key.Enter);
        Assert.True (hex.HasFocus);
        Assert.Same (hex, cp.Focused);

        // But still, Color name should be recognised as a known string and populated
        Assert.Equal ("#7FFFD4", hex.Text);
        Assert.Equal ("Aquamarine", name.Text);

        Application.Top?.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_InvalidHexInput_DoesNotChangeColor ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true);

        cp.Draw ();

        // Enter invalid hex value
        TextField hexField = cp.SubViews.OfType<TextField> ().First (tf => tf.Text == "#000000");
        hexField.SetFocus ();
        hexField.Text = "#ZZZZZZ";
        Assert.True (hexField.HasFocus);
        Assert.Equal ("#ZZZZZZ", hexField.Text);

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("#ZZZZZZ", hex.Text);

        // Advance away from hexField to cause validation
        cp.AdvanceFocus (NavigationDirection.Forward, null);

        cp.Draw ();

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#000000", hex.Text);

        Application.Top?.Dispose ();
        Application.ResetState (true);

    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_RGB_KeyboardNavigation ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);
        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.IsType<RBar> (r);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.IsType<GBar> (g);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.IsType<BBar> (b);
        Assert.Equal ("#000000", hex.Text);

        Assert.IsAssignableFrom<IColorBar> (cp.Focused);

        cp.Draw ();

        Application.RaiseKeyDownEvent (Key.CursorRight);

        cp.Draw ();

        Assert.Equal (3, r.TrianglePosition);
        Assert.Equal ("#0F0000", hex.Text);

        Application.RaiseKeyDownEvent (Key.CursorRight);

        cp.Draw ();

        Assert.Equal (4, r.TrianglePosition);
        Assert.Equal ("#1E0000", hex.Text);

        // Use cursor to move the triangle all the way to the right
        for (var i = 0; i < 1000; i++)
        {
            Application.RaiseKeyDownEvent (Key.CursorRight);
        }

        cp.Draw ();

        // 20 width and TrianglePosition is 0 indexed
        // Meaning we are asserting that triangle is at end
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        Application.Top.Dispose ();
        Application.ResetState (true);

    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_RGB_MouseNavigation ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);

        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.IsType<RBar> (r);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.IsType<GBar> (g);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.IsType<BBar> (b);
        Assert.Equal ("#000000", hex.Text);

        Assert.IsAssignableFrom<IColorBar> (cp.Focused);

        cp.Focused.RaiseMouseEvent (
                                    new ()
                                    {
                                        Flags = MouseFlags.Button1Pressed,
                                        Position = new (3, 0)
                                    });

        cp.Draw ();

        Assert.Equal (3, r.TrianglePosition);
        Assert.Equal ("#0F0000", hex.Text);

        cp.Focused.RaiseMouseEvent (
                                    new ()
                                    {
                                        Flags = MouseFlags.Button1Pressed,
                                        Position = new (4, 0)
                                    });

        cp.Draw ();

        Assert.Equal (4, r.TrianglePosition);
        Assert.Equal ("#1E0000", hex.Text);

        Application.Top?.Dispose ();
        Application.ResetState (true);

    }

    [Theory]
    [SetupFakeDriver]
    [MemberData (nameof (ColorPickerTestData))]
    public void ColorPicker_RGB_NoText (
        Color c,
        string expectedR,
        int expectedRTriangle,
        string expectedG,
        int expectedGTriangle,
        string expectedB,
        int expectedBTriangle,
        string expectedHex
    )
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);
        cp.SelectedColor = c;

        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal (expectedR, r.Text);
        Assert.Equal (expectedRTriangle, r.TrianglePosition);
        Assert.Equal (expectedG, g.Text);
        Assert.Equal (expectedGTriangle, g.TrianglePosition);
        Assert.Equal (expectedB, b.Text);
        Assert.Equal (expectedBTriangle, b.TrianglePosition);
        Assert.Equal (expectedHex, hex.Text);

        Application.Top.Dispose ();
        Application.ResetState (true);

    }

    [Theory]
    [SetupFakeDriver]
    [MemberData (nameof (ColorPickerTestData_WithTextFields))]
    public void ColorPicker_RGB_NoText_WithTextFields (
        Color c,
        string expectedR,
        int expectedRTriangle,
        int expectedRValue,
        string expectedG,
        int expectedGTriangle,
        int expectedGValue,
        string expectedB,
        int expectedBTriangle,
        int expectedBValue,
        string expectedHex
    )
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true);
        cp.SelectedColor = c;

        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);
        TextField rTextField = GetTextField (cp, ColorPickerPart.Bar1);
        TextField gTextField = GetTextField (cp, ColorPickerPart.Bar2);
        TextField bTextField = GetTextField (cp, ColorPickerPart.Bar3);

        Assert.Equal (expectedR, r.Text);
        Assert.Equal (expectedRTriangle, r.TrianglePosition);
        Assert.Equal (expectedRValue.ToString (), rTextField.Text);
        Assert.Equal (expectedG, g.Text);
        Assert.Equal (expectedGTriangle, g.TrianglePosition);
        Assert.Equal (expectedGValue.ToString (), gTextField.Text);
        Assert.Equal (expectedB, b.Text);
        Assert.Equal (expectedBTriangle, b.TrianglePosition);
        Assert.Equal (expectedBValue.ToString (), bTextField.Text);
        Assert.Equal (expectedHex, hex.Text);

        Application.Top?.Dispose ();
        Application.ResetState (true);

    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_SwitchingColorModels_ResetsBars ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, false);
        cp.BeginInit ();
        cp.EndInit ();
        cp.SelectedColor = new (255, 0);

        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        // Switch to HSV
        cp.Style.ColorModel = ColorModel.HSV;
        cp.ApplyStyleChanges ();

        cp.Draw ();

        ColorBar h = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar s = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar v = GetColorBar (cp, ColorPickerPart.Bar3);

        Assert.Equal ("H:", h.Text);
        Assert.Equal (2, h.TrianglePosition);
        Assert.Equal ("S:", s.Text);
        Assert.Equal (19, s.TrianglePosition);
        Assert.Equal ("V:", v.Text);
        Assert.Equal (19, v.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        Application.Top!.Dispose ();
        Application.ResetState (true);

    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_SyncBetweenTextFieldAndBars ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true);

        cp.Draw ();

        // Change value using the bar
        RBar rBar = cp.SubViews.OfType<RBar> ().First ();
        rBar.Value = 128;

        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);
        TextField rTextField = GetTextField (cp, ColorPickerPart.Bar1);
        TextField gTextField = GetTextField (cp, ColorPickerPart.Bar2);
        TextField bTextField = GetTextField (cp, ColorPickerPart.Bar3);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (9, r.TrianglePosition);
        Assert.Equal ("128", rTextField.Text);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("0", gTextField.Text);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("0", bTextField.Text);
        Assert.Equal ("#800000", hex.Text);

        Application.Top?.Dispose ();
        Application.ResetState (true);

    }

    [Fact]
    [SetupFakeDriver]
    public void ColorPicker_TabCompleteColorName ()
    {
        ColorPicker cp = GetColorPicker (ColorModel.RGB, true, true);

        cp.Draw ();

        ColorBar r = GetColorBar (cp, ColorPickerPart.Bar1);
        ColorBar g = GetColorBar (cp, ColorPickerPart.Bar2);
        ColorBar b = GetColorBar (cp, ColorPickerPart.Bar3);
        TextField name = GetTextField (cp, ColorPickerPart.ColorName);
        TextField hex = GetTextField (cp, ColorPickerPart.Hex);

        name.SetFocus ();

        Assert.True (name.HasFocus);
        Assert.Same (name, cp.Focused);

        name.Text = "";
        Assert.Empty (name.Text);

        Application.RaiseKeyDownEvent (Key.A);
        Application.RaiseKeyDownEvent (Key.Q);

        Assert.Equal ("aq", name.Text);

        // Auto complete the color name
        Application.RaiseKeyDownEvent (Key.Tab);

        // Match cyan alternative name
        Assert.Equal ("Aqua", name.Text);

        Assert.True (name.HasFocus);

        Application.RaiseKeyDownEvent (Key.Tab);

        // Resolves to cyan color
        Assert.Equal ("Aqua", name.Text);

        // Tab out of the text field
        Application.RaiseKeyDownEvent (Key.Tab);

        Assert.False (name.HasFocus);
        Assert.NotSame (name, cp.Focused);

        Assert.Equal ("#00FFFF", hex.Text);

        Application.Top?.Dispose ();
        Application.ResetState (true);
    }

    public static IEnumerable<object []> ColorPickerTestData ()
    {
        yield return new object []
        {
            new Color (255, 0),
            "R:", 19, "G:", 2, "B:", 2, "#FF0000"
        };

        yield return new object []
        {
            new Color (0, 255),
            "R:", 2, "G:", 19, "B:", 2, "#00FF00"
        };

        yield return new object []
        {
            new Color (0, 0, 255),
            "R:", 2, "G:", 2, "B:", 19, "#0000FF"
        };

        yield return new object []
        {
            new Color (125, 125, 125),
            "R:", 11, "G:", 11, "B:", 11, "#7D7D7D"
        };
    }

    public static IEnumerable<object []> ColorPickerTestData_WithTextFields ()
    {
        yield return new object []
        {
            new Color (255, 0),
            "R:", 15, 255, "G:", 2, 0, "B:", 2, 0, "#FF0000"
        };

        yield return new object []
        {
            new Color (0, 255),
            "R:", 2, 0, "G:", 15, 255, "B:", 2, 0, "#00FF00"
        };

        yield return new object []
        {
            new Color (0, 0, 255),
            "R:", 2, 0, "G:", 2, 0, "B:", 15, 255, "#0000FF"
        };

        yield return new object []
        {
            new Color (125, 125, 125),
            "R:", 9, 125, "G:", 9, 125, "B:", 9, 125, "#7D7D7D"
        };
    }

    private ColorBar GetColorBar (ColorPicker cp, ColorPickerPart toGet)
    {
        if (toGet <= ColorPickerPart.Bar3)
        {
            return cp.SubViews.OfType<ColorBar> ().ElementAt ((int)toGet);
        }

        throw new NotSupportedException ("ColorPickerPart must be a bar");
    }

    private ColorPicker GetColorPicker (ColorModel colorModel, bool showTextFields, bool showName = false)
    {
        var cp = new ColorPicker { Width = 20, SelectedColor = new (0, 0) };
        cp.Style.ColorModel = colorModel;
        cp.Style.ShowTextFields = showTextFields;
        cp.Style.ShowColorName = showName;
        cp.ApplyStyleChanges ();

        Application.Navigation = new ();

        Application.Top = new () { Width = 20, Height = 5 };
        Application.Top.Add (cp);

        Application.Top.LayoutSubViews ();
        Application.Top.SetFocus ();

        return cp;
    }

    private TextField GetTextField (ColorPicker cp, ColorPickerPart toGet)
    {
        bool hasBarValueTextFields = cp.Style.ShowTextFields;
        bool hasColorNameTextField = cp.Style.ShowColorName;

        switch (toGet)
        {
            case ColorPickerPart.Bar1:
            case ColorPickerPart.Bar2:
            case ColorPickerPart.Bar3:
                if (!hasBarValueTextFields)
                {
                    throw new NotSupportedException ("Corresponding Style option is not enabled");
                }

                return cp.SubViews.OfType<TextField> ().ElementAt ((int)toGet);
            case ColorPickerPart.ColorName:
                if (!hasColorNameTextField)
                {
                    throw new NotSupportedException ("Corresponding Style option is not enabled");
                }

                return cp.SubViews.OfType<TextField> ().ElementAt (hasBarValueTextFields ? (int)toGet : (int)toGet - 3);
            case ColorPickerPart.Hex:

                int offset = hasBarValueTextFields ? 0 : 3;
                offset += hasColorNameTextField ? 0 : 1;

                return cp.SubViews.OfType<TextField> ().ElementAt ((int)toGet - offset);
            default:
                throw new ArgumentOutOfRangeException (nameof (toGet), toGet, null);
        }
    }

    private enum ColorPickerPart
    {
        Bar1 = 0,
        Bar2 = 1,
        Bar3 = 2,
        ColorName = 3,
        Hex = 4
    }
}
