using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides date editing functionality with specialized cursor behavior for date entry.
/// </summary>
/// <remarks>
///     <para>
///         DateField extends <see cref="TextField"/> with date-specific cursor behavior:
///         <list type="bullet">
///             <item>
///                 <description>Cursor positions are constrained to valid digit positions (skipping separators)</description>
///             </item>
///             <item>
///                 <description>Position 0 is reserved for a leading space; valid cursor range is [1, FormatLength]</description>
///             </item>
///             <item>
///                 <description>Numeric input replaces characters in-place rather than inserting</description>
///             </item>
///             <item>
///                 <description>Delete operations replace digits with '0' rather than removing characters</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Cursor Position Model:</b>
///         <list type="bullet">
///             <item>
///                 <description>
///                     <see cref="TextField.InsertionPoint"/>: Inherited, but constrained by the override to [1,
///                     FormatLength]
///                 </description>
///             </item>
///             <item>
///                 <description><see cref="AdjustInsertionPoint"/>: Adjusts cursor to skip over date separator characters</description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="IncrementInsertionPoint"/>/<see cref="DecrementInsertionPoint"/>: Move cursor while
///                     respecting separator positions
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Example:</b> For format "MM/dd/yyyy" with text " 01/15/2024":
///         <list type="bullet">
///             <item>
///                 <description>Position 0: Leading space (not user-accessible)</description>
///             </item>
///             <item>
///                 <description>Positions 1-2: Month digits (01)</description>
///             </item>
///             <item>
///                 <description>Position 3: Separator '/' (cursor skips over)</description>
///             </item>
///             <item>
///                 <description>Positions 4-5: Day digits (15)</description>
///             </item>
///             <item>
///                 <description>Position 6: Separator '/' (cursor skips over)</description>
///             </item>
///             <item>
///                 <description>Positions 7-10: Year digits (2024)</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public class DateField : TextField, IValue<DateTime?>
{
    /// <summary>
    ///     Unicode Right-to-Left Mark character, used to handle RTL date formats in some cultures.
    ///     This character is stripped from display text to ensure consistent cursor positioning.
    /// </summary>
    private const string RIGHT_TO_LEFT_MARK = "\u200f";

    /// <summary>
    ///     The fixed width of the date field (12 characters: 1 leading space + 10 date characters + 1 trailing).
    /// </summary>
    private readonly int _dateFieldLength = 12;

    /// <summary>
    ///     The current date value being edited. Setting this updates the display text.
    /// </summary>
    private DateTime? _date;

    /// <summary>
    ///     The date format string with a leading space (e.g., " MM/dd/yyyy").
    ///     The leading space provides a visual buffer and keeps cursor position 0 inaccessible.
    /// </summary>
    private string? _format;

    /// <summary>
    ///     The date separator character for the current culture (e.g., "/", "-", or ".").
    ///     The cursor automatically skips over these positions during navigation.
    /// </summary>
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
            Text = Value?.ToString (_format).Replace (RIGHT_TO_LEFT_MARK, "") ?? string.Empty;
        }
    }

    /// <summary>
    ///     Gets or sets the cursor position within the date field, constrained to valid digit positions.
    /// </summary>
    /// <value>
    ///     The cursor position, clamped to the range [1, FormatLength]. Unlike <see cref="TextField.InsertionPoint"/>,
    ///     position 0 is not accessible because it contains a leading space.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This override constrains the cursor to valid editing positions within the date format:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Minimum position is 1 (first digit of the date)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Maximum position is FormatLength (last digit of the year)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Note:</b> This property only enforces bounds; it does not skip separator characters.
    ///         Use <see cref="AdjustInsertionPoint"/> after setting to ensure the cursor is on a digit position.
    ///     </para>
    /// </remarks>
    /// <seealso cref="AdjustInsertionPoint"/>
    public override int InsertionPoint
    {
        get => Math.Max (Math.Min (base.InsertionPoint, FormatLength + 1), 1);
        set => base.InsertionPoint = Math.Max (Math.Min (value, FormatLength + 1), 1);
    }

    /// <summary>
    ///     >
    ///     Gets the length of the date format string (excluding the leading space), which represents
    ///     the maximum valid cursor position.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For a standard 10-character date format like "MM/dd/yyyy", this returns 10.
    ///         The valid cursor range is [1, FormatLength], where position 1 is the first digit
    ///         and FormatLength is the last digit.
    ///     </para>
    /// </remarks>
    private int FormatLength => StandardizeDateFormat (_format).Trim ().Length;

    /// <summary>Gets or sets the date value of the <see cref="DateField"/>.</summary>
    public new DateTime? Value
    {
        get => _date;
        set
        {
            if (ReadOnly)
            {
                return;
            }

            DateTime? oldValue = _date;

            if (oldValue == value)
            {
                return;
            }

            ValueChangingEventArgs<DateTime?> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            _date = value;

            if (_format is null)
            {
                return;
            }

            Text = value?.ToString (" " + StandardizeDateFormat (_format.Trim ())).Replace (RIGHT_TO_LEFT_MARK, "") ?? string.Empty;

            ValueChangedEventArgs<DateTime?> changedArgs = new (oldValue, _date);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
        }
    }

    object? IValue.GetValue () => _date;

    /// <summary>
    ///     Called when the <see cref="DateField"/> <see cref="Value"/> is changing.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<DateTime?> args) => false;

    /// <inheritdoc/>
    public new event EventHandler<ValueChangingEventArgs<DateTime?>>? ValueChanging;

    /// <summary>
    ///     Called when the <see cref="DateField"/> <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<DateTime?> args) { }

    /// <inheritdoc/>
    public new event EventHandler<ValueChangedEventArgs<DateTime?>>? ValueChanged;

    /// <inheritdoc/>
    public override bool DeleteCharLeft (bool useOldCursorPos)
    {
        if (ReadOnly)
        {
            return false;
        }

        ClearAllSelection ();
        SetText ((Rune)'0');
        DecrementInsertionPoint ();

        return true;
    }

    /// <inheritdoc/>
    public override bool DeleteCharRight ()
    {
        if (ReadOnly)
        {
            return false;
        }

        ClearAllSelection ();
        SetText ((Rune)'0');

        return true;
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
            AdjustInsertionPoint (mouse.Position!.Value.X);
        }

        return mouse.Handled;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key a)
    {
        // Ignore non-numeric characters.
        if (a < Key.D0 || a > Key.D9)
        {
            return false;
        }

        if (ReadOnly)
        {
            return true;
        }

        if (SetText ((Rune)a))
        {
            IncrementInsertionPoint ();
        }

        return true;

    }

    /// <summary>
    ///     Adjusts the cursor position to ensure it lands on a valid digit position, skipping separator characters.
    /// </summary>
    /// <param name="point">The desired cursor position.</param>
    /// <param name="increment">
    ///     If true, skip separators by moving right; if false, skip by moving left.
    ///     This determines the direction of adjustment when the cursor lands on a separator.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method performs two adjustments:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Clamps <paramref name="point"/> to valid bounds [1, FormatLength]</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     If the cursor is on a separator character, moves it in the specified direction until it
    ///                     reaches a digit
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> For date " 01/15/2024" with separator '/':
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>AdjustInsertionPoint(3, true) → cursor moves to position 4 (first digit of day)</description>
    ///             </item>
    ///             <item>
    ///                 <description>AdjustInsertionPoint(3, false) → cursor moves to position 2 (last digit of month)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    private void AdjustInsertionPoint (int point, bool increment = true)
    {
        int newPoint = point;

        // Clamp to valid bounds
        if (point > FormatLength + 1)
        {
            newPoint = FormatLength + 1;
        }

        if (point < 1)
        {
            newPoint = 1;
        }

        if (newPoint != point)
        {
            InsertionPoint = newPoint;
        }

        // Skip over separator characters in the specified direction
        while (InsertionPoint < Text.GetColumns () - 1 && Text [InsertionPoint].ToString () == _separator)
        {
            if (increment)
            {
                InsertionPoint++;
            }
            else
            {
                InsertionPoint--;
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

            foreach (char t in e.Result)
            {
                if (t == ' ')
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
            trimmedText = trimmedText.Replace (new string (' ', spaces), " ");
            var date = Convert.ToDateTime (trimmedText).ToString (_format!.Trim ());

            if ($" {date}" != e.Result)
            {
                // Change the date format to match the current culture
                e.Result = $" {date}".Replace (RIGHT_TO_LEFT_MARK, "");
            }

            AdjustInsertionPoint (InsertionPoint);
        }
        catch (Exception)
        {
            e.Handled = true;
        }
    }

    /// <summary>
    ///     Decrements the cursor position by one, skipping over separator characters.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method moves the cursor left by one position, then calls <see cref="AdjustInsertionPoint"/>
    ///         with <c>increment=false</c> to skip over any separator that might be at the new position.
    ///     </para>
    ///     <para>
    ///         The cursor will not move below position 1 (the first digit position).
    ///     </para>
    /// </remarks>
    private void DecrementInsertionPoint ()
    {
        if (InsertionPoint <= 1)
        {
            InsertionPoint = 1;

            return;
        }

        InsertionPoint--;
        AdjustInsertionPoint (InsertionPoint, false);
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
            if (!fm [i].Contains (t))
            {
                continue;
            }
            idx = i;

            break;
        }

        return idx;
    }

    /// <summary>
    ///     Increments the cursor position by one, skipping over separator characters.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method moves the cursor right by one position, then calls <see cref="AdjustInsertionPoint"/>
    ///         with <c>increment=true</c> to skip over any separator that might be at the new position.
    ///     </para>
    ///     <para>
    ///         The cursor will not move beyond FormatLength (the last digit position).
    ///     </para>
    /// </remarks>
    private void IncrementInsertionPoint ()
    {
        if (InsertionPoint >= FormatLength + 1)
        {
            InsertionPoint = FormatLength + 1;

            return;
        }

        InsertionPoint++;
        AdjustInsertionPoint (InsertionPoint);
    }

    private new bool MoveEnd ()
    {
        ClearAllSelection ();
        InsertionPoint = FormatLength + 1;

        return true;
    }

    private bool MoveHome ()
    {
        // Home, C-A
        ClearAllSelection ();
        InsertionPoint = 1;

        return true;
    }

    private bool MoveLeft ()
    {
        ClearAllSelection ();
        DecrementInsertionPoint ();

        return true;
    }

    private bool MoveRight ()
    {
        ClearAllSelection ();
        IncrementInsertionPoint ();

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

        return new string (fmtText);
    }

    private void SetInitialProperties (DateTime date)
    {
        _format = $" {StandardizeDateFormat (Culture!.DateTimeFormat.ShortDatePattern)}";
        _separator = GetDataSeparator (Culture.DateTimeFormat.DateSeparator);
        Value = date;
        InsertionPoint = 1;
        TextChanging += OnTextChanging;

        // Things this view knows how to do
        AddCommand (Command.DeleteCharRight,
                    () =>
                    {
                        DeleteCharRight ();

                        return true;
                    });

        AddCommand (Command.DeleteCharLeft,
                    () =>
                    {
                        DeleteCharLeft (false);

                        return true;
                    });
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
        if (InsertionPoint > FormatLength)
        {
            InsertionPoint = FormatLength;

            return false;
        }

        if (InsertionPoint < 1)
        {
            InsertionPoint = 1;

            return false;
        }

        List<Rune> text = Text.EnumerateRunes ().ToList ();
        List<Rune> newText = text.GetRange (0, InsertionPoint);
        newText.Add (key);

        if (InsertionPoint + 1 < text.Count)
        {
            newText = [.. newText, .. text.GetRange (InsertionPoint + 1, text.Count - (InsertionPoint + 1))];
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

        Value = date;

        return true;
    }

    // Converts various date formats to a uniform 10-character format.
    // This aids in simplifying the handling of single-digit months and days,
    // and reduces the number of distinct date formats to maintain.
    private static string StandardizeDateFormat (string? format) =>
        format switch
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
