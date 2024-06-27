#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
/// <see cref="EventArgs"/> for events that convey changes to a property of type `T`.</summary>
/// <remarks>
/// Events that use this class can be cancellable. Where applicable, the <see cref="CancelEventArgs.Cancel"/> property should be set to
/// <see langword="true"/> to prevent the state change from occurring.
/// </remarks>
public class CancelEventArgs<T> : CancelEventArgs
{
    /// <summary>Initializes a new instance of the <see cref="CancelEventArgs{T}"/> class.</summary>
    /// <param name="currentValue">The current (old) value of the property.</param>
    /// <param name="newValue">The value the property will be set to if the event is not cancelled.</param>
    public CancelEventArgs (T currentValue, T newValue)
    {
        CurrentValue = currentValue;
        NewValue = newValue;
    }

    /// <summary>The value the property will be set to if the event is not cancelled.</summary>
    public T NewValue { get; set; }

    /// <summary>The current value of the property.</summary>
    public T CurrentValue { get; }
}
