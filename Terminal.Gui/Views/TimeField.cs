//
// TimeField.cs: text entry for time
//
// Author: Jörg Preiß
//
// Licensed under the MIT license

using System.Globalization;

namespace Terminal.Gui;

/// <summary>
///         Time editing <see cref="View" />
/// </summary>
/// <remarks>
///         The <see cref="TimeField" /> <see cref="View" /> provides time editing functionality with mouse support.
/// </remarks>
public class TimeField : TextField {
	readonly int _longFieldLen = 8;
	readonly string _longFormat;
	readonly string _sepChar;
	readonly int _shortFieldLen = 5;
	readonly string _shortFormat;
	bool _isShort;
	TimeSpan _time;

	/// <summary>
	///         Initializes a new instance of <see cref="TimeField" /> using <see cref="LayoutStyle.Computed" /> positioning.
	/// </summary>
	public TimeField ()
	{
		var cultureInfo = CultureInfo.CurrentCulture;
		_sepChar = cultureInfo.DateTimeFormat.TimeSeparator;
		_longFormat = $" hh\\{_sepChar}mm\\{_sepChar}ss";
		_shortFormat = $" hh\\{_sepChar}mm";
		Width = FieldLength + 2;
		Time = TimeSpan.MinValue;
		CursorPosition = 1;
		TextChanging += TextField_TextChanging;

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

		KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);
		KeyBindings.Add (Key.D.WithAlt, Command.DeleteCharLeft);

		KeyBindings.Add (Key.Home, Command.LeftHome);
		KeyBindings.Add (Key.A.WithCtrl, Command.LeftHome);

		KeyBindings.Add (Key.CursorLeft, Command.Left);
		KeyBindings.Add (Key.B.WithCtrl, Command.Left);

		KeyBindings.Add (Key.End, Command.RightEnd);
		KeyBindings.Add (Key.E.WithCtrl, Command.RightEnd);

		KeyBindings.Add (Key.CursorRight, Command.Right);
		KeyBindings.Add (Key.F.WithCtrl, Command.Right);
	}

	int FieldLength => _isShort ? _shortFieldLen : _longFieldLen;
	string Format => _isShort ? _shortFormat : _longFormat;

	/// <summary>
	///         Gets or sets the time of the <see cref="TimeField" />.
	/// </summary>
	/// <remarks>
	/// </remarks>
	public TimeSpan Time {
		get => _time;
		set {
			if (ReadOnly) {
				return;
			}

			var oldTime = _time;
			_time = value;
			Text = " " + value.ToString (Format.Trim ());
			var args = new DateTimeEventArgs<TimeSpan> (oldTime, value, Format);
			if (oldTime != value) {
				OnTimeChanged (args);
			}
		}
	}

	/// <summary>
	///         Get or sets whether <see cref="TimeField" /> uses the short or long time format.
	/// </summary>
	public bool IsShortFormat {
		get => _isShort;
		set {
			_isShort = value;
			if (_isShort) {
				Width = 7;
			} else {
				Width = 10;
			}

			var ro = ReadOnly;
			if (ro) {
				ReadOnly = false;
			}

			SetText (Text);
			ReadOnly = ro;
			SetNeedsDisplay ();
		}
	}

	/// <inheritdoc />
	public override int CursorPosition {
		get => base.CursorPosition;
		set => base.CursorPosition = Math.Max (Math.Min (value, FieldLength), 1);
	}

	/// <summary>
	///         TimeChanged event, raised when the Date has changed.
	/// </summary>
	/// <remarks>
	///         This event is raised when the <see cref="Time" /> changes.
	/// </remarks>
	/// <remarks>
	///         The passed <see cref="EventArgs" /> is a <see cref="DateTimeEventArgs{T}" /> containing the old value, new
	///         value, and format string.
	/// </remarks>
	public event EventHandler<DateTimeEventArgs<TimeSpan>> TimeChanged;

	//void SetInitialProperties (TimeSpan time, bool isShort = false)
	//{
	//	var cultureInfo = CultureInfo.CurrentCulture;
	//	_sepChar = cultureInfo.DateTimeFormat.TimeSeparator;
	//	_longFormat = $" hh\\{_sepChar}mm\\{_sepChar}ss";
	//	_shortFormat = $" hh\\{_sepChar}mm";
	//	_isShort = isShort;
	//	Width = _fieldLen + 2;
	//	Time = time;
	//	CursorPosition = 1;
	//	TextChanging += TextField_TextChanging;

	//	// Things this view knows how to do
	//	AddCommand (Command.DeleteCharRight, () => {
	//		DeleteCharRight ();
	//		return true;
	//	});
	//	AddCommand (Command.DeleteCharLeft, () => {
	//		DeleteCharLeft (false);
	//		return true;
	//	});
	//	AddCommand (Command.LeftHome, () => MoveHome ());
	//	AddCommand (Command.Left, () => MoveLeft ());
	//	AddCommand (Command.RightEnd, () => MoveEnd ());
	//	AddCommand (Command.Right, () => MoveRight ());

	//	// Default keybindings for this view
	//	KeyBindings.Add (KeyCode.Delete, Command.DeleteCharRight);
	//	KeyBindings.Add (Key.D.WithCtrl, Command.DeleteCharRight);

	//	KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);
	//	KeyBindings.Add (Key.D.WithAlt, Command.DeleteCharLeft);

	//	KeyBindings.Add (Key.Home, Command.LeftHome);
	//	KeyBindings.Add (Key.A.WithCtrl, Command.LeftHome);

	//	KeyBindings.Add (Key.CursorLeft, Command.Left);
	//	KeyBindings.Add (Key.B.WithCtrl, Command.Left);

	//	KeyBindings.Add (Key.End, Command.RightEnd);
	//	KeyBindings.Add (Key.E.WithCtrl, Command.RightEnd);

	//	KeyBindings.Add (Key.CursorRight, Command.Right);
	//	KeyBindings.Add (Key.F.WithCtrl, Command.Right);
	//}

	void TextField_TextChanging (object sender, TextChangingEventArgs e)
	{
		try {
			var spaces = 0;
			for (var i = 0; i < e.NewText.Length; i++) {
				if (e.NewText [i] == ' ') {
					spaces++;
				} else {
					break;
				}
			}

			spaces += FieldLength;
			var trimedText = e.NewText [..spaces];
			spaces -= FieldLength;
			trimedText = trimedText.Replace (new string (' ', spaces), " ");
			if (trimedText != e.NewText) {
				e.NewText = trimedText;
			}

			if (!TimeSpan.TryParseExact (e.NewText.Trim (), Format.Trim (), CultureInfo.CurrentCulture,
				    TimeSpanStyles.None, out var result)) {
				e.Cancel = true;
			}

			AdjCursorPosition (CursorPosition);
		} catch (Exception) {
			e.Cancel = true;
		}
	}

	bool SetText (Rune key)
	{
		var text = Text.EnumerateRunes ().ToList ();
		var newText = text.GetRange (0, CursorPosition);
		newText.Add (key);
		if (CursorPosition < FieldLength) {
			newText = [
				.. newText,
				.. text.GetRange (CursorPosition + 1, text.Count - (CursorPosition + 1))
			];
		}

		return SetText (StringExtensions.ToString (newText));
	}

	bool SetText (string text)
	{
		if (string.IsNullOrEmpty (text)) {
			return false;
		}

		text = NormalizeFormat (text);
		var vals = text.Split (_sepChar);
		var isValidTime = true;
		var hour = Int32.Parse (vals [0]);
		var minute = Int32.Parse (vals [1]);
		var second = _isShort ? 0 : vals.Length > 2 ? Int32.Parse (vals [2]) : 0;
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

		var t = _isShort
			? $" {hour,2:00}{_sepChar}{minute,2:00}"
			: $" {hour,2:00}{_sepChar}{minute,2:00}{_sepChar}{second,2:00}";

		if (!TimeSpan.TryParseExact (t.Trim (), Format.Trim (), CultureInfo.CurrentCulture,
			    TimeSpanStyles.None, out var result) ||
		    !isValidTime) {
			return false;
		}

		Time = result;
		return true;
	}

	string NormalizeFormat (string text, string fmt = null, string sepChar = null)
	{
		if (string.IsNullOrEmpty (fmt)) {
			fmt = Format;
		}

		fmt = fmt.Replace ("\\", "");
		if (string.IsNullOrEmpty (sepChar)) {
			sepChar = _sepChar;
		}

		if (fmt.Length != text.Length) {
			return text;
		}

		var fmtText = text.ToCharArray ();
		for (var i = 0; i < text.Length; i++) {
			var c = fmt [i];
			if (c.ToString () == sepChar && text [i].ToString () != sepChar) {
				fmtText [i] = c;
			}
		}

		return new string (fmtText);
	}

	void IncCursorPosition ()
	{
		if (CursorPosition >= FieldLength) {
			CursorPosition = FieldLength;
			return;
		}

		CursorPosition++;
		AdjCursorPosition (CursorPosition);
	}

	void DecCursorPosition ()
	{
		if (CursorPosition <= 1) {
			CursorPosition = 1;
			return;
		}

		CursorPosition--;
		AdjCursorPosition (CursorPosition, false);
	}

	void AdjCursorPosition (int point, bool increment = true)
	{
		var newPoint = point;
		if (point > FieldLength) {
			newPoint = FieldLength;
		}

		if (point < 1) {
			newPoint = 1;
		}

		if (newPoint != point) {
			CursorPosition = newPoint;
		}

		while (Text [CursorPosition] == _sepChar [0]) {
			if (increment) {
				CursorPosition++;
			} else {
				CursorPosition--;
			}
		}
	}

	/// <inheritdoc />
	public override bool OnProcessKeyDown (Key a)
	{
		// Ignore non-numeric characters.
		if (a.KeyCode is >= (KeyCode)(int)KeyCode.D0 and <= (KeyCode)(int)KeyCode.D9) {
			if (!ReadOnly) {
				if (SetText ((Rune)a)) {
					IncCursorPosition ();
				}
			}

			return true;
		}

		return false;
	}

	bool MoveRight ()
	{
		ClearAllSelection ();
		IncCursorPosition ();
		return true;
	}

	new bool MoveEnd ()
	{
		ClearAllSelection ();
		CursorPosition = FieldLength;
		return true;
	}

	bool MoveLeft ()
	{
		ClearAllSelection ();
		DecCursorPosition ();
		return true;
	}

	bool MoveHome ()
	{
		// Home, C-A
		ClearAllSelection ();
		CursorPosition = 1;
		return true;
	}

	/// <inheritdoc />
	public override void DeleteCharLeft (bool useOldCursorPos = true)
	{
		if (ReadOnly) {
			return;
		}

		ClearAllSelection ();
		SetText ((Rune)'0');
		DecCursorPosition ();
	}

	/// <inheritdoc />
	public override void DeleteCharRight ()
	{
		if (ReadOnly) {
			return;
		}

		ClearAllSelection ();
		SetText ((Rune)'0');
	}

	/// <inheritdoc />
	public override bool MouseEvent (MouseEvent ev)
	{
		var result = base.MouseEvent (ev);

		if (result && SelectedLength == 0 && ev.Flags.HasFlag (MouseFlags.Button1Pressed)) {
			var point = ev.X;
			AdjCursorPosition (point);
		}

		return result;
	}

	/// <summary>
	///         Event firing method that invokes the <see cref="TimeChanged" /> event.
	/// </summary>
	/// <param name="args">The event arguments</param>
	public virtual void OnTimeChanged (DateTimeEventArgs<TimeSpan> args) => TimeChanged?.Invoke (this, args);
}