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
public class TimeField : TextField
{
    private readonly int _longFieldLen = 8;
    private readonly string _longFormat;
    private readonly string _sepChar;
    private readonly int _shortFieldLen = 5;
    private readonly string _shortFormat;
    private bool _isShort;
    private TimeSpan _time;

    /// <summary>Initializes a new instance of <see cref="TimeField"/>.</summary>
    public TimeField ()
    {
        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
        _sepChar = cultureInfo.DateTimeFormat.TimeSeparator;
        _longFormat = $" hh\\{_sepChar}mm\\{_sepChar}ss";
        _shortFormat = $" hh\\{_sepChar}mm";
        Width = FieldLength + 2;
        Time = TimeSpan.MinValue;
        CursorPosition = 1;
        TextChanging += TextField_TextChanging;

        // Things this view knows how to do
        AddCommand (
                    Command.DeleteCharRight,
                    () =>
                    {
                        DeleteCharRight ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteCharLeft,
                    () =>
                    {
                        DeleteCharLeft (false);

                        return true;
                    }
                   );
        AddCommand (Command.LeftStart, () => MoveHome ());
        AddCommand (Command.Left, () => MoveLeft ());
        AddCommand (Command.RightEnd, () => MoveEnd ());
        AddCommand (Command.Right, () => MoveRight ());

        // Replace the key bindings defined in TextField
        KeyBindings.ReplaceCommands (Key.Delete, Command.DeleteCharRight);
        KeyBindings.ReplaceCommands (Key.D.WithCtrl, Command.DeleteCharRight);

        KeyBindings.ReplaceCommands (Key.Backspace, Command.DeleteCharLeft);

        KeyBindings.ReplaceCommands (Key.Home, Command.LeftStart);
        KeyBindings.ReplaceCommands (Key.A.WithCtrl, Command.LeftStart);

        KeyBindings.ReplaceCommands (Key.CursorLeft, Command.Left);
        KeyBindings.ReplaceCommands (Key.B.WithCtrl, Command.Left);

        KeyBindings.ReplaceCommands (Key.End, Command.RightEnd);
        KeyBindings.ReplaceCommands (Key.E.WithCtrl, Command.RightEnd);

        KeyBindings.ReplaceCommands (Key.CursorRight, Command.Right);
        KeyBindings.ReplaceCommands (Key.F.WithCtrl, Command.Right);

#if UNIX_KEY_BINDINGS
        KeyBindings.ReplaceCommands (Key.D.WithAlt, Command.DeleteCharLeft);
#endif
    }

    /// <inheritdoc/>
    public override int CursorPosition
    {
        get => base.CursorPosition;
        set => base.CursorPosition = Math.Max (Math.Min (value, FieldLength), 1);
    }

    /// <summary>Get or sets whether <see cref="TimeField"/> uses the short or long time format.</summary>
    public bool IsShortFormat
    {
        get => _isShort;
        set
        {
            _isShort = value;
            Width = FieldLength + 2;

            bool ro = ReadOnly;

            if (ro)
            {
                ReadOnly = false;
            }

            SetText (Text);
            ReadOnly = ro;
            SetNeedsDisplay ();
        }
    }

    /// <summary>Gets or sets the time of the <see cref="TimeField"/>.</summary>
    /// <remarks></remarks>
    public TimeSpan Time
    {
        get => _time;
        set
        {
            if (ReadOnly)
            {
                return;
            }

            TimeSpan oldTime = _time;
            _time = value;
            Text = " " + value.ToString (Format.Trim ());
            DateTimeEventArgs<TimeSpan> args = new (oldTime, value, Format);

            if (oldTime != value)
            {
                OnTimeChanged (args);
            }
        }
    }

    private int FieldLength => _isShort ? _shortFieldLen : _longFieldLen;
    private string Format => _isShort ? _shortFormat : _longFormat;

    /// <inheritdoc/>
    public override void DeleteCharLeft (bool useOldCursorPos = true)
    {
        if (ReadOnly)
        {
            return;
        }

        ClearAllSelection ();
        SetText ((Rune)'0');
        DecCursorPosition ();
    }

    /// <inheritdoc/>
    public override void DeleteCharRight ()
    {
        if (ReadOnly)
        {
            return;
        }

        ClearAllSelection ();
        SetText ((Rune)'0');
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent  (MouseEventArgs ev)
    {
        if (base.OnMouseEvent (ev) || ev.Handled)
        {
            return true;
        }

        if (SelectedLength == 0 && ev.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            int point = ev.Position.X;
            AdjCursorPosition (point);
        }

        return ev.Handled;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key a)
    {
        // Ignore non-numeric characters.
        if (a.KeyCode is >= (KeyCode)(int)KeyCode.D0 and <= (KeyCode)(int)KeyCode.D9)
        {
            if (!ReadOnly)
            {
                if (SetText ((Rune)a))
                {
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

    private void AdjCursorPosition (int point, bool increment = true)
    {
        int newPoint = point;

        if (point > FieldLength)
        {
            newPoint = FieldLength;
        }

        if (point < 1)
        {
            newPoint = 1;
        }

        if (newPoint != point)
        {
            CursorPosition = newPoint;
        }

        while (CursorPosition < Text.GetColumns() -1 && Text [CursorPosition] == _sepChar [0])
        {
            if (increment)
            {
                CursorPosition++;
            }
            else
            {
                CursorPosition--;
            }
        }
    }

    private void DecCursorPosition ()
    {
        if (CursorPosition <= 1)
        {
            CursorPosition = 1;

            return;
        }

        CursorPosition--;
        AdjCursorPosition (CursorPosition, false);
    }

    private void IncCursorPosition ()
    {
        if (CursorPosition >= FieldLength)
        {
            CursorPosition = FieldLength;

            return;
        }

        CursorPosition++;
        AdjCursorPosition (CursorPosition);
    }

    private new bool MoveEnd ()
    {
        ClearAllSelection ();
        CursorPosition = FieldLength;

        return true;
    }

    private bool MoveHome ()
    {
        // Home, C-A
        ClearAllSelection ();
        CursorPosition = 1;

        return true;
    }

    private bool MoveLeft ()
    {
        ClearAllSelection ();
        DecCursorPosition ();

        return true;
    }

    private bool MoveRight ()
    {
        ClearAllSelection ();
        IncCursorPosition ();

        return true;
    }

    private string NormalizeFormat (string text, string fmt = null, string sepChar = null)
    {
        if (string.IsNullOrEmpty (fmt))
        {
            fmt = Format;
        }

        fmt = fmt.Replace ("\\", "");

        if (string.IsNullOrEmpty (sepChar))
        {
            sepChar = _sepChar;
        }

        if (fmt.Length != text.Length)
        {
            return text;
        }

        char [] fmtText = text.ToCharArray ();

        for (var i = 0; i < text.Length; i++)
        {
            char c = fmt [i];

            if (c.ToString () == sepChar && text [i].ToString () != sepChar)
            {
                fmtText [i] = c;
            }
        }

        return new string (fmtText);
    }

    private bool SetText (Rune key)
    {
        List<Rune> text = Text.EnumerateRunes ().ToList ();
        List<Rune> newText = text.GetRange (0, CursorPosition);
        newText.Add (key);

        if (CursorPosition < FieldLength)
        {
            newText =
            [
                .. newText,
                .. text.GetRange (CursorPosition + 1, text.Count - (CursorPosition + 1))
            ];
        }

        return SetText (StringExtensions.ToString (newText));
    }

    private bool SetText (string text)
    {
        if (string.IsNullOrEmpty (text))
        {
            return false;
        }

        text = NormalizeFormat (text);
        string [] vals = text.Split (_sepChar);
        var isValidTime = true;
        int hour = int.Parse (vals [0]);
        int minute = int.Parse (vals [1]);

        int second = _isShort ? 0 :
                     vals.Length > 2 ? int.Parse (vals [2]) : 0;

        if (hour < 0)
        {
            isValidTime = false;
            hour = 0;
            vals [0] = "0";
        }
        else if (hour > 23)
        {
            isValidTime = false;
            hour = 23;
            vals [0] = "23";
        }

        if (minute < 0)
        {
            isValidTime = false;
            minute = 0;
            vals [1] = "0";
        }
        else if (minute > 59)
        {
            isValidTime = false;
            minute = 59;
            vals [1] = "59";
        }

        if (second < 0)
        {
            isValidTime = false;
            second = 0;
            vals [2] = "0";
        }
        else if (second > 59)
        {
            isValidTime = false;
            second = 59;
            vals [2] = "59";
        }

        string t = _isShort
                       ? $" {hour,2:00}{_sepChar}{minute,2:00}"
                       : $" {hour,2:00}{_sepChar}{minute,2:00}{_sepChar}{second,2:00}";

        if (!TimeSpan.TryParseExact (
                                     t.Trim (),
                                     Format.Trim (),
                                     CultureInfo.CurrentCulture,
                                     TimeSpanStyles.None,
                                     out TimeSpan result
                                    )
            || !isValidTime)
        {
            return false;
        }

        if (IsInitialized)
        {
            Time = result;
        }

        return true;
    }

    private void TextField_TextChanging (object sender, CancelEventArgs<string> e)
    {
        try
        {
            var spaces = 0;

            for (var i = 0; i < e.NewValue.Length; i++)
            {
                if (e.NewValue [i] == ' ')
                {
                    spaces++;
                }
                else
                {
                    break;
                }
            }

            spaces += FieldLength;
            string trimmedText = e.NewValue [..spaces];
            spaces -= FieldLength;
            trimmedText = trimmedText.Replace (new string (' ', spaces), " ");

            if (trimmedText != e.NewValue)
            {
                e.NewValue = trimmedText;
            }

            if (!TimeSpan.TryParseExact (
                                         e.NewValue.Trim (),
                                         Format.Trim (),
                                         CultureInfo.CurrentCulture,
                                         TimeSpanStyles.None,
                                         out TimeSpan result
                                        ))
            {
                e.Cancel = true;
            }

            AdjCursorPosition (CursorPosition);
        }
        catch (Exception)
        {
            e.Cancel = true;
        }
    }
}
