

//
// DateField.cs: text entry for date
//
// Author: Barry Nolte
//
// Licensed under the MIT license
//

using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>Provides date editing functionality with mouse support.</summary>
public class DateField : TextField
{
    private const string RIGHT_TO_LEFT_MARK = "\u200f";

    private readonly int _dateFieldLength = 12;
    private DateTime? _date;
    private string? _format;
    private string? _separator;

    /// <summary>Initializes a new instance of <see cref="DateField"/>.</summary>
    public DateField () : this (DateTime.MinValue) { }

    /// <summary>Initializes a new instance of <see cref="DateField"/>.</summary>
    /// <param name="date"></param>
    public DateField (DateTime date)
    {
        Width = _dateFieldLength;
        SetInitialProperties (date);
    }

    private CultureInfo _culture = CultureInfo.CurrentCulture;

    /// <summary>CultureInfo for date. The default is CultureInfo.CurrentCulture.</summary>
    public CultureInfo? Culture
    {
        get => _culture;
        set
        {
            _culture = value ?? CultureInfo.CurrentCulture;
            _separator = GetDataSeparator (_culture.DateTimeFormat.DateSeparator);
            _format = " " + StandardizeDateFormat (_culture.DateTimeFormat.ShortDatePattern);
            Text = Date?.ToString (_format).Replace (RIGHT_TO_LEFT_MARK, "");
        }
    }

    /// <inheritdoc/>
    public override int CursorPos
    {
        get => base.CursorPos;
        set => base.CursorPos = Math.Max (Math.Min (value, FormatLength), 1);
    }

    /// <summary>Gets or sets the date of the <see cref="DateField"/>.</summary>
    /// <remarks></remarks>
    public DateTime? Date
    {
        get => _date;
        set
        {
            if (ReadOnly)
            {
                return;
            }

            DateTime? oldData = _date;
            _date = value;

            if (_format is { })
            {
                Text = value?.ToString (" " + StandardizeDateFormat (_format.Trim ()))
                            .Replace (RIGHT_TO_LEFT_MARK, "");
                EventArgs<DateTime> args = new (value!.Value);

                if (oldData != value)
                {
                    OnDateChanged (args);
                    DateChanged?.Invoke (this, args);
                }
            }
        }
    }

    private int FormatLength => StandardizeDateFormat (_format).Trim ().Length;

    /// <summary>DateChanged event, raised when the <see cref="Date"/> property has changed.</summary>
    /// <remarks>This event is raised when the <see cref="Date"/> property changes.</remarks>
    /// <remarks>The passed event arguments containing the old value, new value, and format string.</remarks>
    public event EventHandler<EventArgs<DateTime>>? DateChanged;

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
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (base.OnMouseEvent (mouse) || mouse.Handled)
        {
            return true;
        }

        if (SelectedLength == 0 && mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
        {
            AdjCursorPosition (mouse.Position!.Value.X);
        }

        return mouse.Handled;
    }

    /// <summary>Event firing method for the <see cref="DateChanged"/> event.</summary>
    /// <param name="args">Event arguments</param>
    protected virtual void OnDateChanged (EventArgs<DateTime> args) {  }

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
            base.CursorPos = newPoint;
        }

        while (base.CursorPos < Text.GetColumns () - 1 && Text [base.CursorPos].ToString () == _separator)
        {
            if (increment)
            {
                base.CursorPos++;
            }
            else
            {
                base.CursorPos--;
            }
        }
    }

    private void OnTextChanging (object? sender, ResultEventArgs<string> e)
    {
        if (e.Result is null)
        {
            return;
        }

        try
        {
            var spaces = 0;

            for (var i = 0; i < e.Result.Length; i++)
            {
                if (e.Result [i] == ' ')
                {
                    spaces++;
                }
                else
                {
                    break;
                }
            }

            spaces += FormatLength;
            string trimmedText = e.Result [..spaces];
            spaces -= FormatLength;
            trimmedText = trimmedText.Replace (new (' ', spaces), " ");
            var date = Convert.ToDateTime (trimmedText).ToString (_format!.Trim ());

            if ($" {date}" != e.Result)
            {
                // Change the date format to match the current culture
                e.Result = $" {date}".Replace (RIGHT_TO_LEFT_MARK, "");
            }

            AdjCursorPosition (base.CursorPos);
        }
        catch (Exception)
        {
            e.Handled = true;
        }
    }

    private void DecCursorPosition ()
    {
        if (base.CursorPos <= 1)
        {
            base.CursorPos = 1;

            return;
        }

        base.CursorPos--;
        AdjCursorPosition (base.CursorPos, false);
    }

    private string GetDataSeparator (string separator)
    {
        string sepChar = separator.Trim ();

        if (sepChar.Length > 1 && sepChar.Contains (RIGHT_TO_LEFT_MARK))
        {
            sepChar = sepChar.Replace (RIGHT_TO_LEFT_MARK, "");
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
        if (base.CursorPos >= FormatLength)
        {
            base.CursorPos = FormatLength;

            return;
        }

        base.CursorPos++;
        AdjCursorPosition (base.CursorPos);
    }

    private new bool MoveEnd ()
    {
        ClearAllSelection ();
        base.CursorPos = FormatLength;

        return true;
    }

    private bool MoveHome ()
    {
        // Home, C-A
        ClearAllSelection ();
        base.CursorPos = 1;

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

    private string NormalizeFormat (string text, string? fmt = null, string? sepChar = null)
    {
        if (string.IsNullOrEmpty (fmt))
        {
            fmt = _format;
        }

        if (string.IsNullOrEmpty (sepChar))
        {
            sepChar = _separator;
        }

        if (fmt is null || fmt.Length != text.Length)
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

        return new (fmtText);
    }

    private void SetInitialProperties (DateTime date)
    {
        _format = $" {StandardizeDateFormat (Culture!.DateTimeFormat.ShortDatePattern)}";
        _separator = GetDataSeparator (Culture.DateTimeFormat.DateSeparator);
        Date = date;
        base.CursorPos = 1;
        TextChanging += OnTextChanging;

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
        if (base.CursorPos > FormatLength)
        {
            base.CursorPos = FormatLength;

            return false;
        }

        if (base.CursorPos < 1)
        {
            base.CursorPos = 1;

            return false;
        }

        List<Rune> text = Text.EnumerateRunes ().ToList ();
        List<Rune> newText = text.GetRange (0, base.CursorPos);
        newText.Add (key);

        if (base.CursorPos < FormatLength)
        {
            newText =
            [
                .. newText,
                .. text.GetRange (base.CursorPos + 1, text.Count - (base.CursorPos + 1))
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
            if (vals [i].Contains (RIGHT_TO_LEFT_MARK))
            {
                vals [i] = vals [i].Replace (RIGHT_TO_LEFT_MARK, "");
            }
        }

        string [] frm = _format!.Split (_separator);
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
    private static string StandardizeDateFormat (string? format)
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
