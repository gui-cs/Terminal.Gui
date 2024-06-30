#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

/// <summary><see cref="EventArgs"/> for events that convey state changes to a <see cref="View"/> class.</summary>
/// <remarks>
/// Events that use this class can be cancellable. The <see cref="CancelEventArgs.Cancel"/> property should be set to
/// <see langword="true"/> to prevent the state change from occurring.
/// </remarks>
public class StateEventArgs<T> : CancelEventArgs
{
    /// <summary>Creates a new instance of the <see cref="StateEventArgs{T}"/> class.</summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    public StateEventArgs (T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>The new state</summary>
    public T NewValue { get; set; }

    /// <summary>The previous state</summary>
    public T OldValue { get; }
}
