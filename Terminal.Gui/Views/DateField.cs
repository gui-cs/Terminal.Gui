//
// DateField.cs: text entry for date
//
// Author: Barry Nolte
//
// Licensed under the MIT license
//
using System;
using System.Globalization;
using System.Linq;
using NStack;

namespace Terminal.Gui {

	/// <summary>
	///   Date edit widget
	/// </summary>
	/// <remarks>
	///   This widget provides date editing functionality, and mouse support.
	/// </remarks>
	public class DateField : TextField {
		bool isShort;

		int longFieldLen = 10;
		int shortFieldLen = 8;
		int FieldLen { get { return isShort ? shortFieldLen : longFieldLen; } }
		char sepChar;		// = '/';
		string longFormat;      // = " MM/dd/yyyy";
		string shortFormat;	// = " MM/dd/yy";
		string Format { get { return isShort ? shortFormat : longFormat; } }


		/// <summary>
		///    Public constructor that creates a date edit field at an absolute position and fixed size.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="date">Initial date contents.</param>
		/// <param name="isShort">If true, shows only two digits for the year.</param>
		public DateField (int x, int y, DateTime date, bool isShort = false) : base (x, y, isShort ? 10 : 12, "")
		{
			CultureInfo cultureInfo = CultureInfo.CurrentCulture;
			sepChar = cultureInfo.DateTimeFormat.DateSeparator.ToCharArray () [0];
			longFormat = $" {cultureInfo.DateTimeFormat.ShortDatePattern}";
			shortFormat = GetShortFormat (longFormat);
			this.isShort = isShort;
			CursorPosition = 1;
			Date = date;
		}

		string GetShortFormat (string lf)
		{
			return lf.Replace ("yyyy", "yy");
		}

		/// <summary>
		///   Gets or sets the date in the widget.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public DateTime Date {
			get {
				if (!DateTime.TryParseExact (Text.ToString (), Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime result)) return new DateTime ();
				return result;
			}
			set {
				this.Text = value.ToString (Format);
			}
		}

		bool SetText (Rune key)
		{
			var text = TextModel.ToRunes (Text);
			var newText = text.GetRange (0, CursorPosition);
			newText.Add (key);
			if (CursorPosition < FieldLen)
				newText = newText.Concat (text.GetRange (CursorPosition + 1, text.Count - (CursorPosition + 1))).ToList ();
			return SetText (ustring.Make (newText));
		}

		bool SetText (ustring text)
		{
			// FIXED: This validation could be made better by calculating the actual min/max values
			//        for month/day/year. This has a 'good' chance of keeping things valid
			ustring [] vals = text.Split (ustring.Make (sepChar));
			ustring [] frm = ustring.Make (Format).Split (ustring.Make (sepChar));
			bool isValidDate = true;
			int month;
			int day;
			int idx = GetFormatIndex (frm, "M");
			if (Int32.Parse (vals [idx].ToString ()) < 1) {
				isValidDate = false;
				month = 1;
				vals [idx] = "1";
			} else if (Int32.Parse (vals [idx].ToString ()) > 12) {
				isValidDate = false;
				month = 12;
				vals [idx] = "12";
			} else
				month = Int32.Parse (vals [idx].ToString ());
			idx = GetFormatIndex (frm, "d");
			if (Int32.Parse (vals [idx].ToString ()) < 1) {
				isValidDate = false;
				day = 1;
				vals [idx] = "1";
			} else if (Int32.Parse (vals [idx].ToString ()) > 31) {
				isValidDate = false;
				day = 31;
				vals [idx] = "31";
			} else
				day = Int32.Parse (vals [idx].ToString ());
			idx = GetFormatIndex (frm, "y");
			int year = Int32.Parse (vals [idx].ToString ());
			string date = GetData (month, day, year, frm);
			Text = date;

			if (!DateTime.TryParseExact (date, Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime result)) 
				return false;
			return true;
		}

		string GetData(int month, int day, int year, ustring[] fm)
		{
			string data = " ";
			for (int i = 0; i < fm.Length; i++) {
				if (fm [i].Contains ("M"))
					data += $"{month,2:00}";
				else if (fm [i].Contains ("d"))
					data += $"{day,2:00}";
				else
					data += isShort ? $"{year,2:00}" : $"{year,4:0000}";
				if (i < 2)
					data += $"{sepChar}";
			}
			return data;
		}

		int GetFormatIndex(ustring[] fm, string t)
		{
			int idx = -1;
			for (int i = 0; i < fm.Length; i++) {
				if (fm [i].Contains (t)) {
					idx = i;
					break;
				}					
			}
			return idx;
		}

		void IncCursorPosition ()
		{
			if (CursorPosition == FieldLen) 
				return;
			if (Text [++CursorPosition] == sepChar) 
				CursorPosition++;
		}

		void DecCursorPosition ()
		{
			if (CursorPosition == 1) 
				return;
			if (Text [--CursorPosition] == sepChar) 
				CursorPosition--;
		}

		void AdjCursorPosition ()
		{
			if (Text [CursorPosition] == sepChar) 
				CursorPosition++;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.DeleteChar:
			case Key.ControlD:
				SetText ('0');
				break;

			case Key.Delete:
			case Key.Backspace:
				SetText ('0');
				DecCursorPosition ();
				break;

			// Home, C-A
			case Key.Home:
			case Key.ControlA:
				CursorPosition = 1;
				break;

			case Key.CursorLeft:
			case Key.ControlB:
				DecCursorPosition ();
				break;

			case Key.End:
			case Key.ControlE: // End
				CursorPosition = FieldLen;
				break;

			case Key.CursorRight:
			case Key.ControlF:
				IncCursorPosition ();
				break;

			default:
				// Ignore non-numeric characters.
				if (kb.Key < (Key)((int)'0') || kb.Key > (Key)((int)'9'))
					return false;
				if (SetText (TextModel.ToRunes (ustring.Make ((uint)kb.Key)).First ()))
					IncCursorPosition ();
				return true;
			}
			return true;
		}

		public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Clicked))
				return false;
			if (!HasFocus)
				SuperView.SetFocus (this);

			var point = ev.X;
			if (point > FieldLen)
				point = FieldLen;
			if (point < 1)
				point = 1;
			CursorPosition = point;
			AdjCursorPosition ();
			return true;
		}
	}
}