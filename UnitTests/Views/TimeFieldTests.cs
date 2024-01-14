using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.ViewsTests {
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
			Assert.True (tf.NewKeyDownEvent (new (KeyCode.A)));
			tf.ReadOnly = true;
			tf.CursorPosition = 1;
			Assert.True (tf.NewKeyDownEvent (new (KeyCode.D1)));
			Assert.Equal (" 02:02:19", tf.Text);
			tf.ReadOnly = false;
			Assert.True (tf.NewKeyDownEvent (new (KeyCode.D1)));
			Assert.Equal (" 12:02:19", tf.Text);
		}
	}
}