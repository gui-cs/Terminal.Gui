using UnitTests;

namespace Terminal.Gui.ViewsTests;

public class ColorPicker16Tests
{
    [Fact]
    public void Constructors ()
    {
        var colorPicker = new ColorPicker16 ();
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);
        Assert.Equal (Point.Empty, colorPicker.Cursor);
        Assert.True (colorPicker.CanFocus);

        colorPicker.BeginInit ();
        colorPicker.EndInit ();
        colorPicker.LayoutSubViews ();
        Assert.Equal (new (0, 0, 32, 4), colorPicker.Frame);
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        var colorPicker = new ColorPicker16 ();
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (ColorName16.Blue, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (ColorName16.BrightBlue, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (ColorName16.DarkGray, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);

        Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseEvents ()
    {
        var colorPicker = new ColorPicker16 { X = 0, Y = 0, Height = 4, Width = 32 };
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);
        var top = new Toplevel ();
        top.Add (colorPicker);
        Application.Begin (top);

        Assert.False (colorPicker.NewMouseEvent (new ()));

        Assert.True (colorPicker.NewMouseEvent (new () { Position = new (4, 1), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (ColorName16.Blue, colorPicker.SelectedColor);
        top.Dispose ();
    }

    [Fact]
    public void SelectedColorAndCursor ()
    {
        var colorPicker = new ColorPicker16 ();
        colorPicker.SelectedColor = ColorName16.White;
        Assert.Equal (7, colorPicker.Cursor.X);
        Assert.Equal (1, colorPicker.Cursor.Y);

        colorPicker.SelectedColor = Color.Black;
        Assert.Equal (0, colorPicker.Cursor.X);
        Assert.Equal (0, colorPicker.Cursor.Y);

        colorPicker.Cursor = new (7, 1);
        Assert.Equal (ColorName16.White, colorPicker.SelectedColor);

        colorPicker.Cursor = Point.Empty;
        Assert.Equal (ColorName16.Black, colorPicker.SelectedColor);
    }
}