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
	///   Date editing <see cref="View"/>
	/// </summary>
	/// <remarks>
	///   The <see cref="DateField"/> <see cref="View"/> provides date editing functionality with mouse support.
	/// </remarks>
	public class DateField : TextField {
		bool isShort;
		int longFieldLen = 10;
		int shortFieldLen = 8;
		string sepChar;
		string longFormat;
		string shortFormat;

		int FieldLen { get { return isShort ? shortFieldLen : longFieldLen; } }
		string Format { get { return isShort ? shortFormat : longFormat; } }

		/// <summary>
		///    Initializes a new instance of <see cref="DateField"/> at an absolute position and fixed size.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="date">Initial date contents.</param>
		/// <param name="isShort">If true, shows only two digits for the year.</param>
		public DateField (int x, int y, DateTime date, bool isShort = false) : base(x, y, isShort ? 10 : 12, "")
		{
			this.isShort = isShort;
			Initialize (date);
		}

		/// <summary>
		///  Initializes a new instance of <see cref="DateField"/> 
		/// </summary>
		/// <param name="date"></param>
		public DateField (DateTime date) : base ("")
		{
			this.isShort = true;
			Width = FieldLen + 2;
			Initialize (date);
		}

		void Initialize (DateTime date)
		{
			CultureInfo cultureInfo = CultureInfo.CurrentCulture;
			sepChar = cultureInfo.DateTimeFormat.DateSeparator;
			longFormat = GetLongFormat (cultureInfo.DateTimeFormat.ShortDatePattern);
			shortFormat = GetShortFormat (longFormat);
			CursorPosition = 1;
			Date = date;
			Changed += DateField_Changed;
		}

		void DateField_Changed (object sender, ustring e)
		{
			if (!DateTime.TryParseExact (Text.ToString (), Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime result))
				Text = e;
		}

		string GetLongFormat (string lf)
		{
			ustring [] frm = ustring.Make (lf).Split (ustring.Make (sepChar));
			for (int i = 0; i < frm.Length; i++) {
				if (frm [i].Contains ("M") && frm [i].Length < 2)
					lf = lf.Replace ("M", "MM");
				if (frm [i].Contains ("d") && frm [i].Length < 2)
					lf = lf.Replace ("d", "dd");
				if (frm [i].Contains ("y") && frm [i].Length < 4)
					lf = lf.Replace ("yy", "yyyy");
			}
			return $" {lf}";
		}

		string GetShortFormat (string lf)
		{
			return lf.Replace ("yyyy", "yy");
		}

		/// <summary>
		///   Gets or sets the date of the <see cref="DateField"/>.
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

		/// <summary>
		/// Get or set the data format for the widget.
		/// </summary>
		public bool IsShortFormat {
			get => isShort;
			set {
				isShort = value;
				if (isShort)
					Width = 10;
				else
					Width = 12;
				var ro = ReadOnly;
				if (ro)
					ReadOnly = false;
				SetText (Text);
				ReadOnly = ro;
				SetNeedsDisplay ();
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
			ustring [] vals = text.Split (ustring.Make (sepChar));
			ustring [] frm = ustring.Make (Format).Split (ustring.Make (sepChar));
			bool isValidDate = true;
			int idx = GetFormatIndex (frm, "y");
			int year = Int32.Parse (vals [idx].ToString ());
			int month;
			int day;
			idx = GetFormatIndex (frm, "M");
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
				day = DateTime.DaysInMonth (year, month);
				vals [idx] = day.ToString ();
			} else
				day = Int32.Parse (vals [idx].ToString ());
			string date = GetDate (month, day, year, frm);
			Text = date;

			if (!DateTime.TryParseExact (date, Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime result) ||
				!isValidDate)
				return false;
			return true;
		}

		string GetDate (int month, int day, int year, ustring [] fm)
		{
			string date = " ";
			for (int i = 0; i < fm.Length; i++) {
				if (fm [i].Contains ("M")) {
					date += $"{month,2:00}";
				} else if (fm [i].Contains ("d")) {
					date += $"{day,2:00}";
				} else {
					if (!isShort && year.ToString ().Length == 2) {
						var y = DateTime.Now.Year.ToString ();
						date += y.Substring (0, 2) + year.ToString ();
					} else {
						date += $"{year,2:00}";
					}
				}
				if (i < 2)
					date += $"{sepChar}";
			}
			return date;
		}

		int GetFormatIndex (ustring [] fm, string t)
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
			if (Text [++CursorPosition] == sepChar.ToCharArray () [0])
				CursorPosition++;
		}

		void DecCursorPosition ()
		{
			if (CursorPosition == 1)
				return;
			if (Text [--CursorPosition] == sepChar.ToCharArray () [0])
				CursorPosition--;
		}

		void AdjCursorPosition ()
		{
			if (Text [CursorPosition] == sepChar.ToCharArray () [0])
				CursorPosition++;
		}

		///<inheritdoc cref="ProcessKey(KeyEvent)"/>
		public override bool ProcessKey(KeyEvent kb)
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

		///<inheritdoc cref="MouseEvent(Gui.MouseEvent)"/>
		public override bool MouseEvent(MouseEvent ev)
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