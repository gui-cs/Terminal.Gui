﻿using System;
using System.Globalization;
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
			Assert.Equal (LayoutStyle.Absolute, df.LayoutStyle);

			df.IsShortFormat = false;
			Assert.Equal (new Rect (3, 4, 12, 1), df.Frame);
            Assert.Equal(12, df.Width);
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
		public void CursorPosition_Min_Is_Always_One_Max_Is_Always_Max_Format_After_Selection ()
		{
			var df = new DateField ();
			// Start selection
			Assert.True (df.NewKeyDownEvent (new (KeyCode.CursorLeft | KeyCode.ShiftMask)));
			Assert.Equal (1, df.SelectedStart);
			Assert.Equal (1, df.SelectedLength);
			Assert.Equal (0, df.CursorPosition);
			// Without selection
			Assert.True (df.NewKeyDownEvent (new (KeyCode.CursorLeft)));
			Assert.Equal (-1, df.SelectedStart);
			Assert.Equal (0, df.SelectedLength);
			Assert.Equal (1, df.CursorPosition);
			df.CursorPosition = 10;
			Assert.True (df.NewKeyDownEvent (new (KeyCode.CursorRight | KeyCode.ShiftMask)));
			Assert.Equal (10, df.SelectedStart);
			Assert.Equal (1, df.SelectedLength);
			Assert.Equal (11, df.CursorPosition);
			Assert.True (df.NewKeyDownEvent (new (KeyCode.CursorRight)));
			Assert.Equal (-1, df.SelectedStart);
			Assert.Equal (0, df.SelectedLength);
			Assert.Equal (10, df.CursorPosition);
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			CultureInfo cultureBackup = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			DateField df = new DateField (DateTime.Parse ("12/12/1971"));
			df.ReadOnly = true;
			Assert.True (df.NewKeyDownEvent (new (KeyCode.Delete)));
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
			Assert.Equal (2, df.CursorPosition);
			Assert.True (df.NewKeyDownEvent (new (KeyCode.D | KeyCode.AltMask)));
			Assert.Equal (" 10/02/1971", df.Text);
			CultureInfo.CurrentCulture = cultureBackup;
		}

		[Fact]
		public void Typing_With_Selection_Normalize_Format ()
		{
			CultureInfo cultureBackup = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			DateField df = new DateField (DateTime.Parse ("12/12/1971"));
			// Start selection at before the first separator /
			df.CursorPosition = 2;
			// Now select the separator /
			Assert.True (df.NewKeyDownEvent (new (KeyCode.CursorRight | KeyCode.ShiftMask)));
			Assert.Equal (2, df.SelectedStart);
			Assert.Equal (1, df.SelectedLength);
			Assert.Equal (3, df.CursorPosition);
			// Type 3 over the separator
			Assert.True (df.NewKeyDownEvent (new (KeyCode.D3)));
			// The format was normalized and replaced again with /
			Assert.Equal (" 12/12/1971", df.Text);
			Assert.Equal (4, df.CursorPosition);
			CultureInfo.CurrentCulture = cultureBackup;
		}

		[Fact, AutoInitShutdown]
		public void Copy_Paste ()
		{
			CultureInfo cultureBackup = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			DateField df1 = new DateField (DateTime.Parse ("12/12/1971"));
			DateField df2 = new DateField (DateTime.Parse ("12/31/2023"));
			// Select all text
			Assert.True (df2.NewKeyDownEvent (new (KeyCode.End | KeyCode.ShiftMask)));
			Assert.Equal (1, df2.SelectedStart);
			Assert.Equal (10, df2.SelectedLength);
			Assert.Equal (11, df2.CursorPosition);
			// Copy from df2
			Assert.True (df2.NewKeyDownEvent (new (KeyCode.C | KeyCode.CtrlMask)));
			// Paste into df1
			Assert.True (df1.NewKeyDownEvent (new (KeyCode.V | KeyCode.CtrlMask)));
			Assert.Equal (" 12/31/2023", df1.Text);
			Assert.Equal (11, df1.CursorPosition);
			CultureInfo.CurrentCulture = cultureBackup;
		}

		[Fact]
		public void Date_Start_From_01_01_0001_And_End_At_12_31_9999 ()
		{
			CultureInfo cultureBackup = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			DateField df = new DateField (DateTime.Parse ("01/01/0001"));
			Assert.Equal (" 01/01/0001", df.Text);
			df.Date = DateTime.Parse ("12/31/9999");
			Assert.Equal (" 12/31/9999", df.Text);
			CultureInfo.CurrentCulture = cultureBackup;
		}
	}
}
