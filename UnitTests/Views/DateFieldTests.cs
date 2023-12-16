using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.ViewsTests {
	public class DateFieldTests {
		[Fact]
		public void Constructors_Defaults ()
		{
			var df = new DateField ();
			Assert.False (df.IsShortFormat);
			Assert.Equal (DateTime.MinValue, df.Date);
			Assert.Equal (1, df.CursorPosition);
			Assert.Equal (new Rect (0, 0, 12, 1), df.Frame);

			var date = DateTime.Now;
			df = new DateField (date);
			Assert.False (df.IsShortFormat);
			Assert.Equal (date, df.Date);
			Assert.Equal (1, df.CursorPosition);
			Assert.Equal (new Rect (0, 0, 12, 1), df.Frame);

			df = new DateField (1, 2, date);
			Assert.False (df.IsShortFormat);
			Assert.Equal (date, df.Date);
			Assert.Equal (1, df.CursorPosition);
			Assert.Equal (new Rect (1, 2, 12, 1), df.Frame);

			df = new DateField (3, 4, date, true);
			Assert.True (df.IsShortFormat);
			Assert.Equal (date, df.Date);
			Assert.Equal (1, df.CursorPosition);
			Assert.Equal (new Rect (3, 4, 10, 1), df.Frame);

			df.IsShortFormat = false;
			Assert.Equal (new Rect (3, 4, 12, 1), df.Frame);
			Assert.Equal (12, df.Width);
		}

		[Fact]
		public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format ()
		{
			var df = new DateField ();
			Assert.Equal (1, df.CursorPosition);
			df.CursorPosition = 0;
			Assert.Equal (1, df.CursorPosition);
			df.CursorPosition = 11;
			Assert.Equal (10, df.CursorPosition);
			df.IsShortFormat = true;
			df.CursorPosition = 0;
			Assert.Equal (1, df.CursorPosition);
			df.CursorPosition = 9;
			Assert.Equal (8, df.CursorPosition);
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			CultureInfo cultureBackup = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			DateField df = new DateField (DateTime.Parse ("12/12/1971"));
			df.ReadOnly = true;
			Assert.True (df.NewKeyDownEvent (new (KeyCode.DeleteChar)));
			Assert.Equal (" 12/12/1971", df.Text);
			df.ReadOnly = false;
			Assert.True (df.NewKeyDownEvent (new (KeyCode.D | KeyCode.CtrlMask)));
			Assert.Equal (" 02/12/1971", df.Text);
			df.CursorPosition = 4;
			df.ReadOnly = true;
			Assert.True (df.NewKeyDownEvent (new (KeyCode.Delete)));
			Assert.Equal (" 02/12/1971", df.Text);
			df.ReadOnly = false;
			Assert.True (df.NewKeyDownEvent (new (KeyCode.Backspace)));
			Assert.Equal (" 02/02/1971", df.Text);
			Assert.True (df.NewKeyDownEvent (new (KeyCode.Home)));
			Assert.Equal (1, df.CursorPosition);
			Assert.True (df.NewKeyDownEvent (new (KeyCode.End)));
			Assert.Equal (10, df.CursorPosition);
			Assert.True (df.NewKeyDownEvent (new (KeyCode.A | KeyCode.CtrlMask)));
			Assert.Equal (1, df.CursorPosition);
			Assert.True (df.NewKeyDownEvent (new (KeyCode.E | KeyCode.CtrlMask)));
			Assert.Equal (10, df.CursorPosition);
			Assert.True (df.NewKeyDownEvent (new (KeyCode.CursorLeft)));
			Assert.Equal (9, df.CursorPosition);
			Assert.True (df.NewKeyDownEvent (new (KeyCode.CursorRight)));
			Assert.Equal (10, df.CursorPosition);
			// Non-numerics are ignored
			Assert.False (df.NewKeyDownEvent (new (KeyCode.A)));
			df.ReadOnly = true;
			df.CursorPosition = 1;
			Assert.True (df.NewKeyDownEvent (new (KeyCode.D1)));
			Assert.Equal (" 02/02/1971", df.Text);
			df.ReadOnly = false;
			Assert.True (df.NewKeyDownEvent (new (KeyCode.D1)));
			Assert.Equal (" 12/02/1971", df.Text);
			CultureInfo.CurrentCulture = cultureBackup;
		}
	}
}
