using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides time editing functionality with specialized cursor behavior for time entry.
/// </summary>
/// <remarks>
///     <para>
///         TimeField extends <see cref="TextField"/> with time-specific cursor behavior:
///         <list type="bullet">
///             <item>
///                 <description>Cursor positions are constrained to valid digit positions (skipping separators)</description>
///             </item>
///             <item>
///                 <description>Position 0 is reserved for a leading space; valid cursor range is [1, FieldLength]</description>
///             </item>
///             <item>
///                 <description>Numeric input replaces characters in-place rather than inserting</description>
///             </item>
///             <item>
///                 <description>Delete operations replace digits with '0' rather than removing characters</description>
///             </item>
///             <item>
///                 <description>Supports both short (HH:mm) and long (HH:mm:ss) formats</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Cursor Position Model:</b>
///         <list type="bullet">
///             <item>
///                 <description>
///                     <see cref="TextField.InsertionPoint"/>: Inherited, but constrained by the override to [1,
///                     FieldLength]
///                 </description>
///             </item>
///             <item>
///                 <description><see cref="AdjustInsertionPoint"/>: Adjusts cursor to skip over time separator characters</description>
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
///         <b>Example:</b> For long format "HH:mm:ss" with text " 14:30:45":
///         <list type="bullet">
///             <item>
///                 <description>Position 0: Leading space (not user-accessible)</description>
///             </item>
///             <item>
///                 <description>Positions 1-2: Hour digits (14)</description>
///             </item>
///             <item>
///                 <description>Position 3: Separator ':' (cursor skips over)</description>
///             </item>
///             <item>
///                 <description>Positions 4-5: Minute digits (30)</description>
///             </item>
///             <item>
///                 <description>Position 6: Separator ':' (cursor skips over)</description>
///             </item>
///             <item>
///                 <description>Positions 7-8: Second digits (45)</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public class TimeField : TextField, IValue<TimeSpan>
{
    /// <summary>
    ///     The field length for long format (HH:mm:ss) = 8 characters.
    /// </summary>
    private const int LONG_FIELD_LEN = 8;

    /// <summary>
    ///     The format string for long time format with escaped separators (e.g., " hh\:mm\:ss").
    ///     The leading space provides a visual buffer and keeps cursor position 0 inaccessible.
    /// </summary>
    private readonly string _longFormat;

    /// <summary>
    ///     The time separator character for the current culture (typically ':').
    ///     The cursor automatically skips over these positions during navigation.
    /// </summary>
    private readonly string _sepChar;

    /// <summary>
    ///     The field length for short format (HH:mm) = 5 characters.
    /// </summary>
    private const int SHORT_FIELD_LEN = 5;

    /// <summary>
    ///     The format string for short time format with escaped separators (e.g., " hh\:mm").
    /// </summary>
    private readonly string _shortFormat;

    /// <summary>
    ///     Indicates whether the short format (HH:mm) is being used instead of long format (HH:mm:ss).
    /// </summary>
    private bool _isShort;

    /// <summary>
    ///     The current time value being edited.
    /// </summary>
    private TimeSpan _time;

    /// <summary>Initializes a new instance of <see cref="TimeField"/>.</summary>
    public TimeField ()
    {
        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
        _sepChar = cultureInfo.DateTimeFormat.TimeSeparator;
        _longFormat = $" hh\\{_sepChar}mm\\{_sepChar}ss";
        _shortFormat = $" hh\\{_sepChar}mm";
        Width = FieldLength + 2;
        Value = TimeSpan.MinValue;
        base.InsertionPoint = 1;
        TextChanging += TextField_TextChanging;

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

    /// <summary>
    ///     Gets or sets the cursor position within the time field, constrained to valid digit positions.
    /// </summary>
    /// <value>
    ///     The cursor position, clamped to the range [1, FieldLength]. Unlike <see cref="TextField.InsertionPoint"/>,
    ///     position 0 is not accessible because it contains a leading space.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This override constrains the cursor to valid editing positions within the time format:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Minimum position is 1 (first digit of hours)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Maximum position is FieldLength (5 for short format, 8 for long format)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Note:</b> This property only enforces bounds; it does not skip separator characters.
    ///         Use <see cref="AdjustInsertionPoint"/> after setting to ensure the cursor is on a digit position.
    ///     </para>
    /// </remarks>
    /// <seealso cref="AdjustInsertionPoint"/>
    /// <seealso cref="FieldLength"/>
    public override int InsertionPoint
    {
        get => Math.Max (Math.Min (base.InsertionPoint, FieldLength), 1);
        set => base.InsertionPoint = Math.Max (Math.Min (value, FieldLength), 1);
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
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets the length of the time format string (excluding the leading space), which represents
    ///     the maximum valid cursor position.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Returns 5 for short format (HH:mm) or 8 for long format (HH:mm:ss).
    ///         The valid cursor range is [1, FieldLength], where position 1 is the first digit
    ///         and FieldLength is the last digit.
    ///     </para>
    /// </remarks>
    private int FieldLength => _isShort ? SHORT_FIELD_LEN : LONG_FIELD_LEN;

    /// <summary>
    ///     Gets the current time format string based on <see cref="IsShortFormat"/>.
    /// </summary>
    private string Format => _isShort ? _shortFormat : _longFormat;

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
            int point = mouse.Position!.Value.X;
            AdjustInsertionPoint (point);
        }

        return mouse.Handled;
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
                    IncrementInsertionPoint ();
                }
            }

            return true;
        }

        return false;
    }

    #region IValue<TimeSpan> Implementation

    /// <summary>Gets or sets the time value of the <see cref="TimeField"/>.</summary>
    public new TimeSpan Value
    {
        get => _time;
        set
        {
            if (ReadOnly)
            {
                return;
            }

            TimeSpan oldValue = _time;

            if (oldValue == value)
            {
                return;
            }

            ValueChangingEventArgs<TimeSpan> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            _time = value;
            Text = " " + value.ToString (Format.Trim ());

            ValueChangedEventArgs<TimeSpan> changedArgs = new (oldValue, _time);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
        }
    }

    /// <inheritdoc/>
    object? IValue.GetValue () => _time;

    /// <summary>
    ///     Called when the <see cref="TimeField"/> <see cref="Value"/> is changing.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<TimeSpan> args) => false;

    /// <inheritdoc/>
    public new event EventHandler<ValueChangingEventArgs<TimeSpan>>? ValueChanging;

    /// <summary>
    ///     Called when the <see cref="TimeField"/> <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<TimeSpan> args) { }

    /// <inheritdoc/>
    public new event EventHandler<ValueChangedEventArgs<TimeSpan>>? ValueChanged;

    #endregion

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
    ///                 <description>Clamps <paramref name="point"/> to valid bounds [1, FieldLength]</description>
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
    ///         <b>Example:</b> For time " 14:30:45" with separator ':':
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>AdjustInsertionPoint(3, true) → cursor moves to position 4 (first digit of minutes)</description>
    ///             </item>
    ///             <item>
    ///                 <description>AdjustInsertionPoint(3, false) → cursor moves to position 2 (last digit of hours)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    private void AdjustInsertionPoint (int point, bool increment = true)
    {
        int newPoint = point;

        // Clamp to valid bounds
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
            InsertionPoint = newPoint;
        }

        // Skip over separator characters in the specified direction
        while (InsertionPoint < Text.GetColumns () - 1 && Text [InsertionPoint] == _sepChar [0])
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

    /// <summary>
    ///     Increments the cursor position by one, skipping over separator characters.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method moves the cursor right by one position, then calls <see cref="AdjustInsertionPoint"/>
    ///         with <c>increment=true</c> to skip over any separator that might be at the new position.
    ///     </para>
    ///     <para>
    ///         The cursor will not move beyond FieldLength (the last digit position).
    ///     </para>
    /// </remarks>
    private void IncrementInsertionPoint ()
    {
        if (InsertionPoint >= FieldLength)
        {
            InsertionPoint = FieldLength;

            return;
        }

        InsertionPoint++;
        AdjustInsertionPoint (InsertionPoint);
    }

    private new bool MoveEnd ()
    {
        ClearAllSelection ();
        InsertionPoint = FieldLength;

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
        string [] vals = text.Split (_sepChar);
        var isValidTime = true;
        int hour = int.Parse (vals [0]);
        int minute = int.Parse (vals [1]);

        int second = _isShort ? 0 : vals.Length > 2 ? int.Parse (vals [2]) : 0;

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

        string t = _isShort ? $" {hour,2:00}{_sepChar}{minute,2:00}" : $" {hour,2:00}{_sepChar}{minute,2:00}{_sepChar}{second,2:00}";

        if (!TimeSpan.TryParseExact (t.Trim (), Format.Trim (), CultureInfo.CurrentCulture, TimeSpanStyles.None, out TimeSpan result) || !isValidTime)
        {
            return false;
        }

        if (IsInitialized)
        {
            Value = result;
        }

        return true;
    }

    private void TextField_TextChanging (object? sender, ResultEventArgs<string> e)
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

            spaces += FieldLength;
            string trimmedText = e.Result [..spaces];
            spaces -= FieldLength;
            trimmedText = trimmedText.Replace (new string (' ', spaces), " ");

            if (trimmedText != e.Result)
            {
                e.Result = trimmedText;
            }

            if (!TimeSpan.TryParseExact (e.Result.Trim (), Format.Trim (), CultureInfo.CurrentCulture, TimeSpanStyles.None, out TimeSpan _))
            {
                e.Handled = true;
            }

            AdjustInsertionPoint (InsertionPoint);
        }
        catch (Exception)
        {
            e.Handled = true;
        }
    }
}
