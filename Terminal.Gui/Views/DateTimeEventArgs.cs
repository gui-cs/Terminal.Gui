//
// DateField.cs: text entry for date
//
// Author: Barry Nolte
//
// Licensed under the MIT license
//

namespace Terminal.Gui.Views;

/// <summary>
///     Defines the event arguments for <see cref="DateField.DateChanged"/> and <see cref="TimeField.TimeChanged"/>
///     events.
/// </summary>
public class DateTimeEventArgs<T> : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="DateTimeEventArgs{T}"/></summary>
    /// <param name="oldValue">The old <see cref="DateField"/> or <see cref="TimeField"/> value.</param>
    /// <param name="newValue">The new <see cref="DateField"/> or <see cref="TimeField"/> value.</param>
    /// <param name="format">The <see cref="DateField"/> or <see cref="TimeField"/> format string.</param>
    public DateTimeEventArgs (T oldValue, T newValue, string format)
    {
        OldValue = oldValue;
        NewValue = newValue;
        Format = format;
    }

    /// <summary>The <see cref="DateField"/> or <see cref="TimeField"/> format.</summary>
    public string Format { get; }

    /// <summary>The new <see cref="DateField"/> or <see cref="TimeField"/> value.</summary>
    public T NewValue { get; }

    /// <summary>The old <see cref="DateField"/> or <see cref="TimeField"/> value.</summary>
    public T OldValue { get; }
}
