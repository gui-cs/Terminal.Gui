//
// DateField.cs: text entry for date
//
// Author: Barry Nolte
//
// Licensed under the MIT license
//

using System.Globalization;

namespace Terminal.Gui;

/// <summary>Simple Date editing <see cref="View"/></summary>
/// <remarks>The <see cref="DateField"/> <see cref="View"/> provides date editing functionality with mouse support.</remarks>
public class DateField : TextField
{
    private const string RightToLeftMark = "\u200f";

    private readonly int _dateFieldLength = 12;
    private DateTime _date;
    private string _format;
    private string _separator;

    /// <summary>Initializes a new instance of <see cref="DateField"/>.</summary>
    public DateField () : this (DateTime.MinValue) { }

    /// <summary>Initializes a new instance of <see cref="DateField"/>.</summary>
    /// <param name="date"></param>
    public DateField (DateTime date)
    {
        Width = _dateFieldLength;
        SetInitialProperties (date);
    }

    /// <summary>CultureInfo for date. The default is CultureInfo.CurrentCulture.</summary>
    public CultureInfo Culture
    {
        get => CultureInfo.CurrentCulture;
        set
        {
            if (value is { })
            {
                CultureInfo.CurrentCulture = value;
                _separator = GetDataSeparator (value.DateTimeFormat.DateSeparator);
                _format = " " + StandardizeDateFormat (value.DateTimeFormat.ShortDatePattern);
                Text = Date.ToString (_format).Replace (RightToLeftMark, "");
            }
        }
    }

    /// <inheritdoc/>
    public override int CursorPosition
    {
        get => base.CursorPosition;
        set => base.CursorPosition = Math.Max (Math.Min (value, FormatLength), 1);
    }

    /// <summary>Gets or sets the date of the <see cref="DateField"/>.</summary>
    /// <remarks></remarks>
    public DateTime Date
    {
        get => _date;
        set
        {
            if (ReadOnly)
            {
                return;
            }

            DateTime oldData = _date;
            _date = value;

            Text = value.ToString (" " + StandardizeDateFormat (_format.Trim ()))
                        .Replace (RightToLeftMark, "");
            DateTimeEventArgs<DateTime> args = new (oldData, value, _format);

            if (oldData != value)
            {
                OnDateChanged (args);
            }
        }
    }

    private int FormatLength => StandardizeDateFormat (_format).Trim ().Length;

    /// <summary>DateChanged event, raised when the <see cref="Date"/> property has changed.</summary>
    /// <remarks>This event is raised when the <see cref="Date"/> property changes.</remarks>
    /// <remarks>The passed event arguments containing the old value, new value, and format string.</remarks>
    public event EventHandler<DateTimeEventArgs<DateTime>> DateChanged;

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
    protected override bool OnMouseEvent (MouseEventArgs ev)
    {
        if (base.OnMouseEvent (ev) || ev.Handled)
        {
            return true;
        }

        if (SelectedLength == 0 && ev.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            AdjCursorPosition (ev.Position.X);
        }

        return ev.Handled;
    }

    /// <summary>Event firing method for the <see cref="DateChanged"/> event.</summary>
    /// <param name="args">Event arguments</param>
    public virtual void OnDateChanged (DateTimeEventArgs<DateTime> args) { DateChanged?.Invoke (this, args); }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key a)
    {
        // Ignore non-numeric characters.
        if (a >= Key.D0 && a <= Key.D9)
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

    private void AdjCursorPosition (int point, bool increment = true)
    {
        int newPoint = point;

        if (point > FormatLength)
        {
            newPoint = FormatLength;
        }

        if (point < 1)
        {
            newPoint = 1;
        }

        if (newPoint != point)
        {
            CursorPosition = newPoint;
        }

        while (CursorPosition < Text.GetColumns () - 1 && Text [CursorPosition].ToString () == _separator)
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

    private void DateField_Changing (object sender, CancelEventArgs<string> e)
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

            spaces += FormatLength;
            string trimmedText = e.NewValue [..spaces];
            spaces -= FormatLength;
            trimmedText = trimmedText.Replace (new string (' ', spaces), " ");
            var date = Convert.ToDateTime (trimmedText).ToString (_format.Trim ());

            if ($" {date}" != e.NewValue)
            {
                e.NewValue = $" {date}".Replace (RightToLeftMark, "");
            }

            AdjCursorPosition (CursorPosition);
        }
        catch (Exception)
        {
            e.Cancel = true;
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

    private string GetDataSeparator (string separator)
    {
        string sepChar = separator.Trim ();

        if (sepChar.Length > 1 && sepChar.Contains (RightToLeftMark))
        {
            sepChar = sepChar.Replace (RightToLeftMark, "");
        }

        return sepChar;
    }

    private string GetDate (int month, int day, int year, string [] fm)
    {
        var date = " ";

        for (var i = 0; i < fm.Length; i++)
        {
            if (fm [i].Contains ('M'))
            {
                date += $"{month,2:00}";
            }
            else if (fm [i].Contains ('d'))
            {
                date += $"{day,2:00}";
            }
            else
            {
                date += $"{year,4:0000}";
            }

            if (i < 2)
            {
                date += $"{_separator}";
            }
        }

        return date;
    }

    private static int GetFormatIndex (string [] fm, string t)
    {
        int idx = -1;

        for (var i = 0; i < fm.Length; i++)
        {
            if (fm [i].Contains (t))
            {
                idx = i;

                break;
            }
        }

        return idx;
    }

    private void IncCursorPosition ()
    {
        if (CursorPosition >= FormatLength)
        {
            CursorPosition = FormatLength;

            return;
        }

        CursorPosition++;
        AdjCursorPosition (CursorPosition);
    }

    private new bool MoveEnd ()
    {
        ClearAllSelection ();
        CursorPosition = FormatLength;

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
            fmt = _format;
        }

        if (string.IsNullOrEmpty (sepChar))
        {
            sepChar = _separator;
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

    private void SetInitialProperties (DateTime date)
    {
        _format = $" {StandardizeDateFormat (Culture.DateTimeFormat.ShortDatePattern)}";
        _separator = GetDataSeparator (Culture.DateTimeFormat.DateSeparator);
        Date = date;
        CursorPosition = 1;
        TextChanging += DateField_Changing;

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

        // Replace the commands defined in TextField
        KeyBindings.ReplaceCommands (Key.Delete, Command.DeleteCharRight);
        KeyBindings.ReplaceCommands (Key.D.WithCtrl, Command.DeleteCharRight);

        KeyBindings.ReplaceCommands (Key.Backspace, Command.DeleteCharLeft);

        KeyBindings.ReplaceCommands (Key.Home, Command.LeftStart);
        KeyBindings.ReplaceCommands (Key.Home.WithCtrl, Command.LeftStart);

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

    private bool SetText (Rune key)
    {
        if (CursorPosition > FormatLength)
        {
            CursorPosition = FormatLength;

            return false;
        }

        if (CursorPosition < 1)
        {
            CursorPosition = 1;

            return false;
        }

        List<Rune> text = Text.EnumerateRunes ().ToList ();
        List<Rune> newText = text.GetRange (0, CursorPosition);
        newText.Add (key);

        if (CursorPosition < FormatLength)
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
        string [] vals = text.Split (_separator);

        for (var i = 0; i < vals.Length; i++)
        {
            if (vals [i].Contains (RightToLeftMark))
            {
                vals [i] = vals [i].Replace (RightToLeftMark, "");
            }
        }

        string [] frm = _format.Split (_separator);
        int year;
        int month;
        int day;
        int idx = GetFormatIndex (frm, "y");

        if (int.Parse (vals [idx]) < 1)
        {
            year = 1;
            vals [idx] = "1";
        }
        else
        {
            year = int.Parse (vals [idx]);
        }

        idx = GetFormatIndex (frm, "M");

        if (int.Parse (vals [idx]) < 1)
        {
            month = 1;
            vals [idx] = "1";
        }
        else if (int.Parse (vals [idx]) > 12)
        {
            month = 12;
            vals [idx] = "12";
        }
        else
        {
            month = int.Parse (vals [idx]);
        }

        idx = GetFormatIndex (frm, "d");

        if (int.Parse (vals [idx]) < 1)
        {
            day = 1;
            vals [idx] = "1";
        }
        else if (int.Parse (vals [idx]) > 31)
        {
            day = DateTime.DaysInMonth (year, month);
            vals [idx] = day.ToString ();
        }
        else
        {
            day = int.Parse (vals [idx]);
        }

        string d = GetDate (month, day, year, frm);

        DateTime date;

        try
        {
            date = Convert.ToDateTime (d);
        }
        catch (Exception)
        {
            return false;
        }

        Date = date;

        return true;
    }

    // Converts various date formats to a uniform 10-character format.
    // This aids in simplifying the handling of single-digit months and days,
    // and reduces the number of distinct date formats to maintain.
    private static string StandardizeDateFormat (string format)
    {
        return format switch
        {
            "MM/dd/yyyy" => "MM/dd/yyyy",
            "yyyy-MM-dd" => "yyyy-MM-dd",
            "yyyy/MM/dd" => "yyyy/MM/dd",
            "dd/MM/yyyy" => "dd/MM/yyyy",
            "d?/M?/yyyy" => "dd/MM/yyyy",
            "dd.MM.yyyy" => "dd.MM.yyyy",
            "dd-MM-yyyy" => "dd-MM-yyyy",
            "dd/MM yyyy" => "dd/MM/yyyy",
            "d. M. yyyy" => "dd.MM.yyyy",
            "yyyy.MM.dd" => "yyyy.MM.dd",
            "g yyyy/M/d" => "yyyy/MM/dd",
            "d/M/yyyy" => "dd/MM/yyyy",
            "d?/M?/yyyy g" => "dd/MM/yyyy",
            "d-M-yyyy" => "dd-MM-yyyy",
            "d.MM.yyyy" => "dd.MM.yyyy",
            "d.MM.yyyy '?'." => "dd.MM.yyyy",
            "M/d/yyyy" => "MM/dd/yyyy",
            "d. M. yyyy." => "dd.MM.yyyy",
            "d.M.yyyy." => "dd.MM.yyyy",
            "g yyyy-MM-dd" => "yyyy-MM-dd",
            "d.M.yyyy" => "dd.MM.yyyy",
            "d/MM/yyyy" => "dd/MM/yyyy",
            "yyyy/M/d" => "yyyy/MM/dd",
            "dd. MM. yyyy." => "dd.MM.yyyy",
            "yyyy. MM. dd." => "yyyy.MM.dd",
            "yyyy. M. d." => "yyyy.MM.dd",
            "d. MM. yyyy" => "dd.MM.yyyy",
            _ => "dd/MM/yyyy"
        };
    }
}
