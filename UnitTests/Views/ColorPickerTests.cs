﻿namespace Terminal.Gui.ViewsTests;

public class ColorPickerTests
{
    [Fact]
    public void Constructors ()
    {
        var colorPicker = new ColorPicker ();
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);
        Assert.Equal (Point.Empty, colorPicker.Cursor);
        Assert.True (colorPicker.CanFocus);

        colorPicker.BeginInit ();
        colorPicker.EndInit ();
        colorPicker.LayoutSubviews ();
        Assert.Equal (new (0, 0, 32, 4), colorPicker.Frame);
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        var colorPicker = new ColorPicker ();
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (ColorName.Blue, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (ColorName.BrightBlue, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (ColorName.DarkGray, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseEvents ()
    {
        var colorPicker = new ColorPicker { X = 0, Y = 0, Height = 4, Width = 32 };
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);
        var top = new Toplevel ();
        top.Add (colorPicker);
        Application.Begin (top);

        Assert.False (colorPicker.NewMouseEvent (new ()));

        Assert.True (colorPicker.NewMouseEvent (new() { Position = new (4, 1), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (ColorName.Blue, colorPicker.SelectedColor);
        top.Dispose ();
    }

    [Fact]
    public void SelectedColorAndCursor ()
    {
        var colorPicker = new ColorPicker ();
        colorPicker.SelectedColor = ColorName.White;
        Assert.Equal (7, colorPicker.Cursor.X);
        Assert.Equal (1, colorPicker.Cursor.Y);

        colorPicker.SelectedColor = Color.Black;
        Assert.Equal (0, colorPicker.Cursor.X);
        Assert.Equal (0, colorPicker.Cursor.Y);

        colorPicker.Cursor = new (7, 1);
        Assert.Equal (ColorName.White, colorPicker.SelectedColor);

        colorPicker.Cursor = Point.Empty;
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);
    }
}
