//
// TimeField.cs: text entry for time
//
// Author: Jörg Preiß
//
// Licensed under the MIT license

using System.Globalization;

namespace Terminal.Gui;

/// <summary>Time editing <see cref="View"/></summary>
/// <remarks>The <see cref="TimeField"/> <see cref="View"/> provides time editing functionality with mouse support.</remarks>
public class TimeField : TextField {
    private bool _isShort;
    private readonly int _longFieldLen = 8;
    private string _longFormat;
    private string _sepChar;
    private readonly int _shortFieldLen = 5;
    private string _shortFormat;
    private TimeSpan _time;

    /// <summary>Initializes a new instance of <see cref="TimeField"/> using <see cref="LayoutStyle.Absolute"/> positioning.</summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="time">Initial time.</param>
    /// <param name="isShort">If true, the seconds are hidden. Sets the <see cref="IsShortFormat"/> property.</param>
    public TimeField (int x, int y, TimeSpan time, bool isShort = false) : base (x, y, isShort ? 7 : 10, "") {
        SetInitialProperties (time, isShort);
    }

    /// <summary>Initializes a new instance of <see cref="TimeField"/> using <see cref="LayoutStyle.Computed"/> positioning.</summary>
    /// <param name="time">Initial time</param>
    public TimeField (TimeSpan time) : base (string.Empty) {
        Width = _fieldLen + 2;
        SetInitialProperties (time);
    }

    /// <summary>Initializes a new instance of <see cref="TimeField"/> using <see cref="LayoutStyle.Computed"/> positioning.</summary>
    public TimeField () : this (TimeSpan.MinValue) { }

    /// <inheritdoc/>
    public override int CursorPosition {
        get => base.CursorPosition;
        set => base.CursorPosition = Math.Max (Math.Min (value, _fieldLen), 1);
    }

    /// <summary>Get or sets whether <see cref="TimeField"/> uses the short or long time format.</summary>
    public bool IsShortFormat {
        get => _isShort;
        set {
            _isShort = value;
            if (_isShort) {
                Width = 7;
            } else {
                Width = 10;
            }

            bool ro = ReadOnly;
            if (ro) {
                ReadOnly = false;
            }

            SetText (Text);
            ReadOnly = ro;
            SetNeedsDisplay ();
        }
    }

    /// <summary>Gets or sets the time of the <see cref="TimeField"/>.</summary>
    /// <remarks></remarks>
    public TimeSpan Time {
        get => _time;
        set {
            if (ReadOnly) {
                return;
            }

            TimeSpan oldTime = _time;
            _time = value;
            Text = " " + value.ToString (_format.Trim ());
            DateTimeEventArgs<TimeSpan> args = new DateTimeEventArgs<TimeSpan> (oldTime, value, _format);
            if (oldTime != value) {
                OnTimeChanged (args);
            }
        }
    }

    private int _fieldLen => _isShort ? _shortFieldLen : _longFieldLen;

    private string _format => _isShort ? _shortFormat : _longFormat;

    /// <inheritdoc/>
    public override void DeleteCharLeft (bool useOldCursorPos = true) {
        if (ReadOnly) {
            return;
        }

        ClearAllSelection ();
        SetText ((Rune)'0');
        DecCursorPosition ();
    }

    /// <inheritdoc/>
    public override void DeleteCharRight () {
        if (ReadOnly) {
            return;
        }

        ClearAllSelection ();
        SetText ((Rune)'0');
    }

    ///<inheritdoc/>
    public override bool MouseEvent (MouseEvent ev) {
        bool result = base.MouseEvent (ev);

        if (result && SelectedLength == 0 && ev.Flags.HasFlag (MouseFlags.Button1Pressed)) {
            int point = ev.X;
            AdjCursorPosition (point);
        }

        return result;
    }

    ///<inheritdoc/>
    public override bool OnProcessKeyDown (Key a) {
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

    /// <summary>Event firing method that invokes the <see cref="TimeChanged"/> event.</summary>
    /// <param name="args">The event arguments</param>
    public virtual void OnTimeChanged (DateTimeEventArgs<TimeSpan> args) { TimeChanged?.Invoke (this, args); }

    /// <summary>TimeChanged event, raised when the Date has changed.</summary>
    /// <remarks>This event is raised when the <see cref="Time"/> changes.</remarks>
    /// <remarks>
    ///     The passed <see cref="EventArgs"/> is a <see cref="DateTimeEventArgs{T}"/> containing the old value, new
    ///     value, and format string.
    /// </remarks>
    public event EventHandler<DateTimeEventArgs<TimeSpan>> TimeChanged;

    private void AdjCursorPosition (int point, bool increment = true) {
        int newPoint = point;
        if (point > _fieldLen) {
            newPoint = _fieldLen;
        }

        if (point < 1) {
            newPoint = 1;
        }

        if (newPoint != point) {
            CursorPosition = newPoint;
        }

        while (Text[CursorPosition] == _sepChar[0]) {
            if (increment) {
                CursorPosition++;
            } else {
                CursorPosition--;
            }
        }
    }

    private void DecCursorPosition () {
        if (CursorPosition <= 1) {
            CursorPosition = 1;

            return;
        }

        CursorPosition--;
        AdjCursorPosition (CursorPosition, false);
    }

    private void IncCursorPosition () {
        if (CursorPosition >= _fieldLen) {
            CursorPosition = _fieldLen;

            return;
        }

        CursorPosition++;
        AdjCursorPosition (CursorPosition);
    }

    private new bool MoveEnd () {
        ClearAllSelection ();
        CursorPosition = _fieldLen;

        return true;
    }

    private bool MoveHome () {
        // Home, C-A
        ClearAllSelection ();
        CursorPosition = 1;

        return true;
    }

    private bool MoveLeft () {
        ClearAllSelection ();
        DecCursorPosition ();

        return true;
    }

    private bool MoveRight () {
        ClearAllSelection ();
        IncCursorPosition ();

        return true;
    }

    private string NormalizeFormat (string text, string fmt = null, string sepChar = null) {
        if (string.IsNullOrEmpty (fmt)) {
            fmt = _format;
        }

        fmt = fmt.Replace ("\\", "");
        if (string.IsNullOrEmpty (sepChar)) {
            sepChar = _sepChar;
        }

        if (fmt.Length != text.Length) {
            return text;
        }

        char[] fmtText = text.ToCharArray ();
        for (var i = 0; i < text.Length; i++) {
            char c = fmt[i];
            if (c.ToString () == sepChar && text[i].ToString () != sepChar) {
                fmtText[i] = c;
            }
        }

        return new string (fmtText);
    }

    private void SetInitialProperties (TimeSpan time, bool isShort = false) {
        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
        _sepChar = cultureInfo.DateTimeFormat.TimeSeparator;
        _longFormat = $" hh\\{_sepChar}mm\\{_sepChar}ss";
        _shortFormat = $" hh\\{_sepChar}mm";
        _isShort = isShort;
        Time = time;
        CursorPosition = 1;
        TextChanging += TextField_TextChanging;

        // Things this view knows how to do
        AddCommand (
                    Command.DeleteCharRight,
                    () => {
                        DeleteCharRight ();

                        return true;
                    });
        AddCommand (
                    Command.DeleteCharLeft,
                    () => {
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

    private bool SetText (Rune key) {
        List<Rune> text = Text.EnumerateRunes ().ToList ();
        List<Rune> newText = text.GetRange (0, CursorPosition);
        newText.Add (key);
        if (CursorPosition < _fieldLen) {
            newText = 
        }

        [

        .. newText, .. text.GetRange (CursorPosition + 1, text.Count - (CursorPosition + 1))];

        return SetText (StringExtensions.ToString (newText));
    }

    private bool SetText (string text) {
        if (string.IsNullOrEmpty (text)) {
            return false;
        }

        text = NormalizeFormat (text);
        string[] vals = text.Split (_sepChar);
        var isValidTime = true;
        int hour = int.Parse (vals[0]);
        int minute = int.Parse (vals[1]);
        int second = _isShort ? 0 : vals.Length > 2 ? int.Parse (vals[2]) : 0;
        if (hour < 0) {
            isValidTime = false;
            hour = 0;
            vals[0] = "0";
        } else if (hour > 23) {
            isValidTime = false;
            hour = 23;
            vals[0] = "23";
        }

        if (minute < 0) {
            isValidTime = false;
            minute = 0;
            vals[1] = "0";
        } else if (minute > 59) {
            isValidTime = false;
            minute = 59;
            vals[1] = "59";
        }

        if (second < 0) {
            isValidTime = false;
            second = 0;
            vals[2] = "0";
        } else if (second > 59) {
            isValidTime = false;
            second = 59;
            vals[2] = "59";
        }

        string t = _isShort
                       ? $" {hour,2:00}{_sepChar}{minute,2:00}"
                       : $" {hour,2:00}{_sepChar}{minute,2:00}{_sepChar}{second,2:00}";

        if (!TimeSpan.TryParseExact (
                                     t.Trim (),
                                     _format.Trim (),
                                     CultureInfo.CurrentCulture,
                                     TimeSpanStyles.None,
                                     out TimeSpan result) ||
            !isValidTime) {
            return false;
        }

        Time = result;

        return true;
    }

    private void TextField_TextChanging (object sender, TextChangingEventArgs e) {
        try {
            var spaces = 0;
            for (var i = 0; i < e.NewText.Length; i++) {
                if (e.NewText[i] == ' ') {
                    spaces++;
                } else {
                    break;
                }
            }

            spaces += _fieldLen;
            string trimedText = e.NewText[..spaces];
            spaces -= _fieldLen;
            trimedText = trimedText.Replace (new string (' ', spaces), " ");
            if (trimedText != e.NewText) {
                e.NewText = trimedText;
            }

            if (!TimeSpan.TryParseExact (
                                         e.NewText.Trim (),
                                         _format.Trim (),
                                         CultureInfo.CurrentCulture,
                                         TimeSpanStyles.None,
                                         out TimeSpan result)) {
                e.Cancel = true;
            }

            AdjCursorPosition (CursorPosition);
        }
        catch (Exception) {
            e.Cancel = true;
        }
    }
}
