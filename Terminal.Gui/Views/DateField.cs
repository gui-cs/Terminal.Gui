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
using System.Text;

namespace Terminal.Gui; 

/// <summary>
///   Simple Date editing <see cref="View"/>
/// </summary>
/// <remarks>
///   The <see cref="DateField"/> <see cref="View"/> provides date editing functionality with mouse support.
/// </remarks>
public class DateField : TextField {
	DateTime date;
	bool isShort;
	int longFieldLen = 10;
	int shortFieldLen = 8;
	string sepChar;
	string longFormat;
	string shortFormat;

	int fieldLen => isShort ? shortFieldLen : longFieldLen;

	string format => isShort ? shortFormat : longFormat;

	/// <summary>
	///   DateChanged event, raised when the <see cref="Date"/> property has changed.
	/// </summary>
	/// <remarks>
	///   This event is raised when the <see cref="Date"/> property changes.
	/// </remarks>
	/// <remarks>
	///   The passed event arguments containing the old value, new value, and format string.
	/// </remarks>
	public event EventHandler<DateTimeEventArgs<DateTime>> DateChanged;

	/// <summary>
	///    Initializes a new instance of <see cref="DateField"/> using <see cref="LayoutStyle.Absolute"/> layout.
	/// </summary>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="date">Initial date contents.</param>
	/// <param name="isShort">If true, shows only two digits for the year.</param>
	public DateField (int x, int y, DateTime date, bool isShort = false) : base (x, y, isShort ? 10 : 12, "") => Initialize (date, isShort);

	/// <summary>
	///  Initializes a new instance of <see cref="DateField"/> using <see cref="LayoutStyle.Computed"/> layout.
	/// </summary>
	public DateField () : this (DateTime.MinValue) { }

	/// <summary>
	///  Initializes a new instance of <see cref="DateField"/> using <see cref="LayoutStyle.Computed"/> layout.
	/// </summary>
	/// <param name="date"></param>
	public DateField (DateTime date) : base ("")
	{
		Width = fieldLen + 2;
		Initialize (date);
	}

	void Initialize (DateTime date, bool isShort = false)
	{
		var cultureInfo = CultureInfo.CurrentCulture;
		sepChar = cultureInfo.DateTimeFormat.DateSeparator;
		longFormat = GetLongFormat (cultureInfo.DateTimeFormat.ShortDatePattern);
		shortFormat = GetShortFormat (longFormat);
		this.isShort = isShort;
		Date = date;
		CursorPosition = 1;
		TextChanged += DateField_Changed;

		// Things this view knows how to do
		AddCommand (Command.DeleteCharRight, () => {
			DeleteCharRight ();
			return true;
		});
		AddCommand (Command.DeleteCharLeft, () => {
			DeleteCharLeft (false);
			return true;
		});
		AddCommand (Command.LeftHome, () => MoveHome ());
		AddCommand (Command.Left, () => MoveLeft ());
		AddCommand (Command.RightEnd, () => MoveEnd ());
		AddCommand (Command.Right, () => MoveRight ());

		// Default keybindings for this view
		KeyBindings.Add (KeyCode.Delete, Command.DeleteCharRight);
		KeyBindings.Add (Key.D.WithCtrl, Command.DeleteCharRight);

		KeyBindings.Add (Key.Delete, Command.DeleteCharLeft);
		KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);

		KeyBindings.Add (Key.Home, Command.LeftHome);
		KeyBindings.Add (Key.A.WithCtrl, Command.LeftHome);

		KeyBindings.Add (Key.CursorLeft, Command.Left);
		KeyBindings.Add (Key.B.WithCtrl, Command.Left);

		KeyBindings.Add (Key.End, Command.RightEnd);
		KeyBindings.Add (Key.E.WithCtrl, Command.RightEnd);

		KeyBindings.Add (Key.CursorRight, Command.Right);
		KeyBindings.Add (Key.F.WithCtrl, Command.Right);

	}

	/// <inheritdoc />
	public override bool OnProcessKeyDown (Key a)
	{
		// Ignore non-numeric characters.
		if (a >= Key.D0 && a <= Key.D9) {
			if (!ReadOnly) {
				if (SetText ((Rune)a)) {
					IncCursorPosition ();
				}
			}
			return true;
		}
		return false;
	}

	void DateField_Changed (object sender, TextChangedEventArgs e)
	{
		try {
			if (!DateTime.TryParseExact (GetDate (Text), GetInvarianteFormat (), CultureInfo.CurrentCulture, DateTimeStyles.None, out var result)) {
				Text = e.OldValue;
			}
		} catch (Exception) {
			Text = e.OldValue;
		}
	}

	string GetInvarianteFormat () => $"MM{sepChar}dd{sepChar}yyyy";

	string GetLongFormat (string lf)
	{
		string [] frm = lf.Split (sepChar);
		for (int i = 0; i < frm.Length; i++) {
			if (frm [i].Contains ("M") && frm [i].GetRuneCount () < 2) {
				lf = lf.Replace ("M", "MM");
			}
			if (frm [i].Contains ("d") && frm [i].GetRuneCount () < 2) {
				lf = lf.Replace ("d", "dd");
			}
			if (frm [i].Contains ("y") && frm [i].GetRuneCount () < 4) {
				lf = lf.Replace ("yy", "yyyy");
			}
		}
		return $" {lf}";
	}

	string GetShortFormat (string lf) => lf.Replace ("yyyy", "yy");

	/// <summary>
	///   Gets or sets the date of the <see cref="DateField"/>.
	/// </summary>
	/// <remarks>
	/// </remarks>
	public DateTime Date {
		get => date;
		set {
			if (ReadOnly) {
				return;
			}

			var oldData = date;
			date = value;
			Text = value.ToString (format);
			var args = new DateTimeEventArgs<DateTime> (oldData, value, format);
			if (oldData != value) {
				OnDateChanged (args);
			}
		}
	}

		/// <summary>
		/// Get or set the date format for the widget.
		/// </summary>
		public bool IsShortFormat {
			get => isShort;
			set {
				isShort = value;
				if (isShort) {
					if (LayoutStyle == LayoutStyle.Absolute) {
						Frame = new Rect (Frame.Location, new Size (10, Frame.Height));
					} else {
						Width = 10;
					}
				} else {
					if (LayoutStyle == LayoutStyle.Absolute) {
						Frame = new Rect (Frame.Location, new Size (12, Frame.Height));
					} else {
						Width = 12;
					}
				}
				var ro = ReadOnly;
				if (ro)
					ReadOnly = false;
				SetText (Text);
				ReadOnly = ro;
				SetNeedsDisplay ();
			}
		}

	/// <inheritdoc/>
	public override int CursorPosition {
		get => base.CursorPosition;
		set => base.CursorPosition = Math.Max (Math.Min (value, fieldLen), 1);
	}

	bool SetText (Rune key)
	{
		var text = Text.EnumerateRunes ().ToList ();
		var newText = text.GetRange (0, CursorPosition);
		newText.Add (key);
		if (CursorPosition < fieldLen) {
			newText = newText.Concat (text.GetRange (CursorPosition + 1, text.Count - (CursorPosition + 1))).ToList ();
		}
		return SetText (StringExtensions.ToString (newText));
	}

	bool SetText (string text)
	{
		if (string.IsNullOrEmpty (text)) {
			return false;
		}

		string [] vals = text.Split (sepChar);
		string [] frm = format.Split (sepChar);
		bool isValidDate = true;
		int idx = GetFormatIndex (frm, "y");
		int year = Int32.Parse (vals [idx]);
		int month;
		int day;
		idx = GetFormatIndex (frm, "M");
		if (Int32.Parse (vals [idx]) < 1) {
			isValidDate = false;
			month = 1;
			vals [idx] = "1";
		} else if (Int32.Parse (vals [idx]) > 12) {
			isValidDate = false;
			month = 12;
			vals [idx] = "12";
		} else {
			month = Int32.Parse (vals [idx]);
		}
		idx = GetFormatIndex (frm, "d");
		if (Int32.Parse (vals [idx]) < 1) {
			isValidDate = false;
			day = 1;
			vals [idx] = "1";
		} else if (Int32.Parse (vals [idx]) > 31) {
			isValidDate = false;
			day = DateTime.DaysInMonth (year, month);
			vals [idx] = day.ToString ();
		} else {
			day = Int32.Parse (vals [idx]);
		}
		string d = GetDate (month, day, year, frm);

		if (!DateTime.TryParseExact (d, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out var result) ||
		!isValidDate) {
			return false;
		}
		Date = result;
		return true;
	}

	string GetDate (int month, int day, int year, string [] fm)
	{
		string date = " ";
		for (int i = 0; i < fm.Length; i++) {
			if (fm [i].Contains ("M")) {
				date += $"{month,2:00}";
			} else if (fm [i].Contains ("d")) {
				date += $"{day,2:00}";
			} else {
				if (!isShort && year.ToString ().Length == 2) {
					string y = DateTime.Now.Year.ToString ();
					date += y.Substring (0, 2) + year.ToString ();
				} else if (isShort && year.ToString ().Length == 4) {
					date += $"{year.ToString ().Substring (2, 2)}";
				} else {
					date += $"{year,2:00}";
				}
			}
			if (i < 2) {
				date += $"{sepChar}";
			}
		}
		return date;
	}

	string GetDate (string text)
	{
		string [] vals = text.Split (sepChar);
		string [] frm = format.Split (sepChar);
		string [] date = { null, null, null };

		for (int i = 0; i < frm.Length; i++) {
			if (frm [i].Contains ("M")) {
				date [0] = vals [i].Trim ();
			} else if (frm [i].Contains ("d")) {
				date [1] = vals [i].Trim ();
			} else {
				string year = vals [i].Trim ();
				if (year.GetRuneCount () == 2) {
					string y = DateTime.Now.Year.ToString ();
					date [2] = y.Substring (0, 2) + year.ToString ();
				} else {
					date [2] = vals [i].Trim ();
				}
			}
		}
		return date [0] + sepChar + date [1] + sepChar + date [2];
	}

	int GetFormatIndex (string [] fm, string t)
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
		if (CursorPosition == fieldLen) {
			return;
		}
		if (Text [++CursorPosition] == sepChar.ToCharArray () [0]) {
			CursorPosition++;
		}
	}

	void DecCursorPosition ()
	{
		if (CursorPosition == 1) {
			return;
		}
		if (Text [--CursorPosition] == sepChar.ToCharArray () [0]) {
			CursorPosition--;
		}
	}

	void AdjCursorPosition ()
	{
		if (Text [CursorPosition] == sepChar.ToCharArray () [0]) {
			CursorPosition++;
		}
	}

	bool MoveRight ()
	{
		IncCursorPosition ();
		return true;
	}

	new bool MoveEnd ()
	{
		CursorPosition = fieldLen;
		return true;
	}

	bool MoveLeft ()
	{
		DecCursorPosition ();
		return true;
	}

	bool MoveHome ()
	{
		// Home, C-A
		CursorPosition = 1;
		return true;
	}

	/// <inheritdoc/>
	public override void DeleteCharLeft (bool useOldCursorPos = true)
	{
		if (ReadOnly) {
			return;
		}

		SetText ((Rune)'0');
		DecCursorPosition ();
		return;
	}

	/// <inheritdoc/>
	public override void DeleteCharRight ()
	{
		if (ReadOnly) {
			return;
		}

		SetText ((Rune)'0');
		return;
	}

	/// <inheritdoc/>
	public override bool MouseEvent (MouseEvent ev)
	{
		if (!ev.Flags.HasFlag (MouseFlags.Button1Clicked)) {
			return false;
		}
		if (!HasFocus) {
			SetFocus ();
		}

		int point = ev.X;
		if (point > fieldLen) {
			point = fieldLen;
		}
		if (point < 1) {
			point = 1;
		}
		CursorPosition = point;
		AdjCursorPosition ();
		return true;
	}

	/// <summary>
	/// Event firing method for the <see cref="DateChanged"/> event.
	/// </summary>
	/// <param name="args">Event arguments</param>
	public virtual void OnDateChanged (DateTimeEventArgs<DateTime> args) => DateChanged?.Invoke (this, args);
}