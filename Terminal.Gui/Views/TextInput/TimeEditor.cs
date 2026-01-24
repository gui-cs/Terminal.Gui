using System.Globalization;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides time editing functionality using <see cref="TextValidateField"/> with culture-aware formatting.
/// </summary>
/// <remarks>
///     <para>
///         TimeEditor extends <see cref="TextValidateField"/> with time-specific functionality:
///         <list type="bullet">
///             <item><description>Uses <see cref="TimeTextProvider"/> for validation and formatting</description></item>
///             <item><description>Supports both 12-hour and 24-hour formats via <see cref="DateTimeFormatInfo"/></description></item>
///             <item><description>Cursor automatically skips over separator characters</description></item>
///             <item><description>Supports AM/PM toggling for 12-hour formats</description></item>
///             <item><description>Auto-adjusts width based on time pattern</description></item>
///         </list>
///     </para>
///     <para>
///         <b>Usage Examples:</b>
///         <code>
///         // Use default (current culture's long time pattern)
///         TimeEditor timeEditor = new () { Value = TimeSpan.FromHours (14.5) };
///         // en-US displays: " 2:30:00 PM"
///         // en-GB displays: " 14:30:00"
///         
///         // Use specific culture's format
///         timeEditor.Format = CultureInfo.GetCultureInfo ("de-DE").DateTimeFormat;
///         // Displays: " 14:30:00"
///         
///         // Want short time? Modify the LongTimePattern
///         DateTimeFormatInfo format = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
///         format.LongTimePattern = format.ShortTimePattern;
///         timeEditor.Format = format;
///         // en-US displays: " 2:30 PM"
///         
///         // Custom pattern with milliseconds
///         DateTimeFormatInfo customFormat = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
///         customFormat.LongTimePattern = "HH:mm:ss.fff";
///         timeEditor.Format = customFormat;
///         // Displays: " 14:30:00.000"
///         </code>
///     </para>
/// </remarks>
public class TimeEditor : TextValidateField, IValue<TimeSpan>
{
    private TimeTextProvider TimeProvider => (TimeTextProvider)Provider!;
    private TimeSpan _lastKnownValue = TimeSpan.Zero;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TimeEditor"/> class.
    /// </summary>
    public TimeEditor ()
    {
        Provider = new TimeTextProvider ();
        Width = Dim.Auto (minimumContentDim: 10);
        
        // Subscribe to provider's text changed to raise our value events
        TimeProvider.TextChanged += (_, _) => RaiseValueChangedEvents ();
        
        // Initialize last known value
        _lastKnownValue = TimeProvider.TimeValue;
    }

    /// <summary>
    ///     Gets or sets the <see cref="DateTimeFormatInfo"/> used for time formatting.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The editor uses <see cref="DateTimeFormatInfo.LongTimePattern"/> to determine the display format.
    ///         To use a short time format, clone the DateTimeFormatInfo and set LongTimePattern to ShortTimePattern.
    ///     </para>
    ///     <para>
    ///         The width automatically adjusts when the format changes to accommodate the new pattern.
    ///     </para>
    /// </remarks>
    public DateTimeFormatInfo Format
    {
        get => TimeProvider.Format;
        set
        {
            TimeProvider.Format = value;
            Width = TimeProvider.DisplayText.Length + 2;
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets or sets the current time value.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property raises <see cref="ValueChanging"/> (cancellable) and <see cref="ValueChanged"/> events.
    ///         The change can be prevented by handling <see cref="ValueChanging"/> and setting
    ///         <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/>.
    ///     </para>
    /// </remarks>
    public TimeSpan Value
    {
        get => TimeProvider.TimeValue;
        set
        {
            TimeSpan oldValue = TimeProvider.TimeValue;

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

            TimeProvider.TimeValue = value;
            Text = TimeProvider.Text;

            ValueChangedEventArgs<TimeSpan> changedArgs = new (oldValue, value);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);

            SetNeedsDraw ();
        }
    }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<TimeSpan>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<TimeSpan>>? ValueChanged;

    /// <inheritdoc/>
    object? IValue.GetValue () => Value;

    /// <summary>
    ///     Called when the <see cref="Value"/> is changing.
    ///     Allows derived classes to cancel the change.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<TimeSpan> args)
    {
        return false;
    }

    /// <summary>
    ///     Called when the <see cref="Value"/> has changed.
    ///     Allows derived classes to react to value changes.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<TimeSpan> args)
    {
    }

    /// <summary>
    ///     Raises value events when the text changes through user input.
    /// </summary>
    private void RaiseValueChangedEvents ()
    {
        TimeSpan currentValue = TimeProvider.TimeValue;
        
        if (_lastKnownValue == currentValue)
        {
            return;
        }
        
        ValueChangedEventArgs<TimeSpan> changedArgs = new (_lastKnownValue, currentValue);
        _lastKnownValue = currentValue;
        OnValueChanged (changedArgs);
        ValueChanged?.Invoke (this, changedArgs);
    }
}
