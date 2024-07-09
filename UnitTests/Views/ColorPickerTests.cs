namespace Terminal.Gui.ViewsTests;

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
        Assert.Equal (Color.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (ColorName.Blue, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (ColorName.BrightBlue, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (ColorName.DarkGray, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorLeft)); // stay
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp)); // stay
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.End));
        Assert.Equal (ColorName.White, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorDown)); // stay
        Assert.Equal (ColorName.White, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorRight)); // stay
        Assert.Equal (ColorName.White, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (ColorName.Gray, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorRight)); // wrap
        Assert.Equal (ColorName.DarkGray, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.Home));
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.End));
        Assert.Equal (ColorName.White, colorPicker.SelectedColor);
    }

    [Fact]
    public void MouseEvents ()
    {
        var colorPicker = new ColorPicker
        {
            X = 0,
            Y = 0,
            Height = 4,
            Width = 32
        };
        Assert.Equal (new Color (ColorName.Black), colorPicker.SelectedColor);

        Assert.False (colorPicker.NewMouseEvent (new ()));

        Assert.True (colorPicker.NewMouseEvent (new() { Position = new (4, 1), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (ColorName.Blue, colorPicker.SelectedColor);
    }

    [Fact]
    public void SelectedColorAndCursor ()
    {
        var colorPicker = new ColorPicker ();
        colorPicker.SelectedColor = new Color (ColorName.White);
        Assert.Equal (7, colorPicker.Cursor.X);
        Assert.Equal (1, colorPicker.Cursor.Y);

        colorPicker.SelectedColor = new Color (Color.Black);
        Assert.Equal (0, colorPicker.Cursor.X);
        Assert.Equal (0, colorPicker.Cursor.Y);

        colorPicker.Cursor = new (7, 1);
        Assert.Equal (ColorName.White, colorPicker.SelectedColor);

        colorPicker.Cursor = Point.Empty;
        Assert.Equal (ColorName.Black, colorPicker.SelectedColor);
    }
}
