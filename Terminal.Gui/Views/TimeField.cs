//
// TimeField.cs: text entry for time
//
// Author: Jörg Preiß
//
// Licensed under the MIT license
using System;
using System.Globalization;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	///   Time editing <see cref="View"/>
	/// </summary>
	/// <remarks>
	///   The <see cref="TimeField"/> <see cref="View"/> provides time editing functionality with mouse support.
	/// </remarks>
	public class TimeField : TextField {
		bool isShort;

		int longFieldLen = 8;
		int shortFieldLen = 5;
		string sepChar;
		string longFormat;
		string shortFormat;

		int FieldLen { get { return isShort ? shortFieldLen : longFieldLen; } }
		string Format { get { return isShort ? shortFormat : longFormat; } }


		/// <summary>
		///    Initializes a new instance of <see cref="TimeField"/> at an absolute position and fixed size.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="time">Initial time contents.</param>
		/// <param name="isShort">If true, the seconds are hidden.</param>
		public TimeField (int x, int y, DateTime time, bool isShort = false) : base (x, y, isShort ? 7 : 10, "")
		{
			this.isShort = isShort;
			Initialize (time);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TimeField"/> 
		/// </summary>
		/// <param name="time"></param>
		public TimeField (DateTime time) : base ("")
		{
			this.isShort = true;
			Width = FieldLen + 2;
			Initialize (time);
		}

		void Initialize (DateTime time)
		{
			CultureInfo cultureInfo = CultureInfo.CurrentCulture;
			sepChar = cultureInfo.DateTimeFormat.TimeSeparator;
			longFormat = $" HH{sepChar}mm{sepChar}ss";
			shortFormat = $" HH{sepChar}mm";
			CursorPosition = 1;
			Time = time;
			Changed += TimeField_Changed;
		}

		void TimeField_Changed (object sender, ustring e)
		{
			if (!DateTime.TryParseExact (Text.ToString (), Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime result))
				Text = e;
		}

		/// <summary>
		///   Gets or sets the time of the <see cref="TimeField"/>.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public DateTime Time {
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
					Width = 7;
				else
					Width = 10;
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
			bool isValidTime = true;
			int hour = Int32.Parse (vals [0].ToString ());
			int minute = Int32.Parse (vals [1].ToString ());
			int second = isShort ? 0 : vals.Length > 2 ? Int32.Parse (vals [2].ToString ()) : 0;
			if (hour < 0) {
				isValidTime = false;
				hour = 0;
				vals [0] = "0";
			} else if (hour > 23) {
				isValidTime = false;
				hour = 23;
				vals [0] = "23";
			}
			if (minute < 0) {
				isValidTime = false;
				minute = 0;
				vals [1] = "0";
			} else if (minute > 59) {
				isValidTime = false;
				minute = 59;
				vals [1] = "59";
			}
			if (second < 0) {
				isValidTime = false;
				second = 0;
				vals [2] = "0";
			} else if (second > 59) {
				isValidTime = false;
				second = 59;
				vals [2] = "59";
			}
			string time = isShort ? $" {hour,2:00}{sepChar}{minute,2:00}" : $" {hour,2:00}{sepChar}{minute,2:00}{sepChar}{second,2:00}";
			Text = time;

			if (!DateTime.TryParseExact (text.ToString (), Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime result) ||
				!isValidTime)
				return false;
			return true;
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

		///<inheritdoc cref="MouseEvent(Gui.MouseEvent)"/>
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