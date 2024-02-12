using Xunit;

namespace Terminal.Gui.ViewsTests;

public class ColorPickerTests {
	[Fact]
	public void Constructors ()
	{
		var colorPicker = new ColorPicker ();
		Assert.Equal (new Color (ColorName.Black), colorPicker.SelectedColor);
		Assert.Equal (new Point (0, 0), colorPicker.Cursor);
		Assert.True (colorPicker.CanFocus);

        colorPicker.BeginInit ();
        colorPicker.EndInit ();
        colorPicker.LayoutSubviews ();
        Assert.Equal (new Rect (0, 0, 32, 4), colorPicker.Frame);
    }

	[Fact]
	[AutoInitShutdown]
	public void KeyBindings_Command ()
	{
		var colorPicker = new ColorPicker ();
		Assert.Equal (Color.Black, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorRight));
		Assert.Equal (Color.Blue, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorDown));
		Assert.Equal (Color.BrightBlue, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorLeft));
		Assert.Equal (Color.DarkGray, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp));
		Assert.Equal (Color.Black, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorLeft)); // stay
		Assert.Equal (Color.Black, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp)); // stay
		Assert.Equal (Color.Black, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.End));
		Assert.Equal (Color.White, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorDown)); // stay
		Assert.Equal (Color.White, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorRight)); // stay
		Assert.Equal (Color.White, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorUp));
		Assert.Equal (Color.Gray, colorPicker.SelectedColor);

		Assert.True (colorPicker.NewKeyDownEvent (Key.CursorRight)); // wrap
		Assert.Equal (Color.DarkGray, colorPicker.SelectedColor);
		
		Assert.True (colorPicker.NewKeyDownEvent (Key.Home));
		Assert.Equal (Color.Black, colorPicker.SelectedColor);
		
		Assert.True (colorPicker.NewKeyDownEvent (Key.End));
		Assert.Equal (Color.White, colorPicker.SelectedColor);
	}

	[Fact]
	[AutoInitShutdown]
	public void MouseEvents ()
	{
		var colorPicker = new ColorPicker () {
			X = 0,
			Y = 0,
			Height = 4,
			Width = 32
		};
		Assert.Equal (new Color (ColorName.Black), colorPicker.SelectedColor);
		Application.Top.Add (colorPicker);
		Application.Begin (Application.Top);

        Assert.False (colorPicker.MouseEvent (new MouseEvent ()));

		Assert.True (colorPicker.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Clicked, X = 4, Y = 1 }));
		Assert.Equal (Color.Blue, colorPicker.SelectedColor);
	}

	[Fact]
	[AutoInitShutdown]
	public void SelectedColorAndCursor ()
	{
		var colorPicker = new ColorPicker ();
		colorPicker.SelectedColor = new Color (ColorName.White);
		Assert.Equal (7, colorPicker.Cursor.X);
		Assert.Equal (1, colorPicker.Cursor.Y);

		colorPicker.SelectedColor = new Color (Color.Black);
		Assert.Equal (0, colorPicker.Cursor.X);
		Assert.Equal (0, colorPicker.Cursor.Y);

		colorPicker.Cursor = new Point (7, 1);
		Assert.Equal (new Color (ColorName.White), colorPicker.SelectedColor);

		colorPicker.Cursor = new Point (0, 0);
		Assert.Equal (new Color (ColorName.Black), colorPicker.SelectedColor);
	}
}