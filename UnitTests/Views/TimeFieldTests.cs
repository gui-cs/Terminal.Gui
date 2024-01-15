using System;
using Xunit;

namespace Terminal.Gui.ViewsTests;
public class TimeFieldTests {
	[Fact]
	public void Constructors_Defaults ()
	{
		var tf = new TimeField ();
		Assert.False (tf.IsShortFormat);
		Assert.Equal (TimeSpan.MinValue, tf.Time);
		Assert.Equal (1, tf.CursorPosition);
		Assert.Equal (new Rect (0, 0, 10, 1), tf.Frame);

		var time = DateTime.Now.TimeOfDay;
		tf = new TimeField (time);
		Assert.False (tf.IsShortFormat);
		Assert.Equal (time, tf.Time);
		Assert.Equal (1, tf.CursorPosition);
		Assert.Equal (new Rect (0, 0, 10, 1), tf.Frame);

		tf = new TimeField (1, 2, time);
		Assert.False (tf.IsShortFormat);
		Assert.Equal (time, tf.Time);
		Assert.Equal (1, tf.CursorPosition);
		Assert.Equal (new Rect (1, 2, 10, 1), tf.Frame);

		tf = new TimeField (3, 4, time, true);
		Assert.True (tf.IsShortFormat);
		Assert.Equal (time, tf.Time);
		Assert.Equal (1, tf.CursorPosition);
		Assert.Equal (new Rect (3, 4, 7, 1), tf.Frame);

		tf.IsShortFormat = false;
		Assert.Equal (new Rect (3, 4, 10, 1), tf.Frame);
		Assert.Equal (10, tf.Width);
	}

	[Fact]
	public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format ()
	{
		var tf = new TimeField ();
		Assert.Equal (1, tf.CursorPosition);
		tf.CursorPosition = 0;
		Assert.Equal (1, tf.CursorPosition);
		tf.CursorPosition = 9;
		Assert.Equal (8, tf.CursorPosition);
		tf.IsShortFormat = true;
		tf.CursorPosition = 0;
		Assert.Equal (1, tf.CursorPosition);
		tf.CursorPosition = 6;
		Assert.Equal (5, tf.CursorPosition);
	}

	[Fact]
	public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format_After_Selection ()
	{
		var tf = new TimeField ();
		// Start selection
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.CursorLeft | KeyCode.ShiftMask)));
		Assert.Equal (1, tf.SelectedStart);
		Assert.Equal (1, tf.SelectedLength);
		Assert.Equal (0, tf.CursorPosition);
		// Without selection
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.CursorLeft)));
		Assert.Equal (-1, tf.SelectedStart);
		Assert.Equal (0, tf.SelectedLength);
		Assert.Equal (1, tf.CursorPosition);
		tf.CursorPosition = 8;
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.CursorRight | KeyCode.ShiftMask)));
		Assert.Equal (8, tf.SelectedStart);
		Assert.Equal (1, tf.SelectedLength);
		Assert.Equal (9, tf.CursorPosition);
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.CursorRight)));
		Assert.Equal (-1, tf.SelectedStart);
		Assert.Equal (0, tf.SelectedLength);
		Assert.Equal (8, tf.CursorPosition);
		Assert.False (tf.IsShortFormat);
		tf.IsShortFormat = true;
		Assert.Equal (5, tf.CursorPosition);
		// Start selection
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.CursorRight | KeyCode.ShiftMask)));
		Assert.Equal (5, tf.SelectedStart);
		Assert.Equal (1, tf.SelectedLength);
		Assert.Equal (6, tf.CursorPosition);
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.CursorRight)));
		Assert.Equal (-1, tf.SelectedStart);
		Assert.Equal (0, tf.SelectedLength);
		Assert.Equal (5, tf.CursorPosition);
	}

	[Fact]
	public void KeyBindings_Command ()
	{
		TimeField tf = new TimeField (TimeSpan.Parse ("12:12:19"));
		tf.ReadOnly = true;
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.Delete)));
		Assert.Equal (" 12:12:19", tf.Text);
		tf.ReadOnly = false;
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.D | KeyCode.CtrlMask)));
		Assert.Equal (" 02:12:19", tf.Text);
		tf.CursorPosition = 4;
		tf.ReadOnly = true;
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.Delete)));
		Assert.Equal (" 02:12:19", tf.Text);
		tf.ReadOnly = false;
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.Backspace)));
		Assert.Equal (" 02:02:19", tf.Text);
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.Home)));
		Assert.Equal (1, tf.CursorPosition);
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.End)));
		Assert.Equal (8, tf.CursorPosition);
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.A | KeyCode.CtrlMask)));
		Assert.Equal (1, tf.CursorPosition);
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.E | KeyCode.CtrlMask)));
		Assert.Equal (8, tf.CursorPosition);
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.CursorLeft)));
		Assert.Equal (7, tf.CursorPosition);
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.CursorRight)));
		Assert.Equal (8, tf.CursorPosition);
		// Non-numerics are ignored
		Assert.False (tf.NewKeyDownEvent (new (KeyCode.A)));
		tf.ReadOnly = true;
		tf.CursorPosition = 1;
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.D1)));
		Assert.Equal (" 02:02:19", tf.Text);
		tf.ReadOnly = false;
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.D1)));
		Assert.Equal (" 12:02:19", tf.Text);
		Assert.Equal (2, tf.CursorPosition);
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.D | KeyCode.AltMask)));
		Assert.Equal (" 10:02:19", tf.Text);
	}

	[Fact]
	public void Typing_With_Selection_Normalize_Format ()
	{
		TimeField tf = new TimeField (TimeSpan.Parse ("12:12:19"));
		// Start selection at before the first separator :
		tf.CursorPosition = 2;
		// Now select the separator :
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.CursorRight | KeyCode.ShiftMask)));
		Assert.Equal (2, tf.SelectedStart);
		Assert.Equal (1, tf.SelectedLength);
		Assert.Equal (3, tf.CursorPosition);
		// Type 3 over the separator
		Assert.True (tf.NewKeyDownEvent (new (KeyCode.D3)));
		// The format was normalized and replaced again with :
		Assert.Equal (" 12:12:19", tf.Text);
		Assert.Equal (4, tf.CursorPosition);
	}

	[Fact, AutoInitShutdown]
	public void Copy_Paste ()
	{
		TimeField tf1 = new TimeField (TimeSpan.Parse ("12:12:19"));
		TimeField tf2 = new TimeField (TimeSpan.Parse ("12:59:01"));
		// Select all text
		Assert.True (tf2.NewKeyDownEvent (new (KeyCode.End | KeyCode.ShiftMask)));
		Assert.Equal (1, tf2.SelectedStart);
		Assert.Equal (8, tf2.SelectedLength);
		Assert.Equal (9, tf2.CursorPosition);
		// Copy from tf2
		Assert.True (tf2.NewKeyDownEvent (new (KeyCode.C | KeyCode.CtrlMask)));
		// Paste into tf1
		Assert.True (tf1.NewKeyDownEvent (new (KeyCode.V | KeyCode.CtrlMask)));
		Assert.Equal (" 12:59:01", tf1.Text);
		Assert.Equal (9, tf1.CursorPosition);
	}
}
