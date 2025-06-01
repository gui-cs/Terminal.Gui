#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
///     <see cref="EventArgs"/> for events that convey changes to a property of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of the value that was part of the change being canceled.</typeparam>
/// <remarks>
///     Events that use this class can be cancellable. Where applicable, the <see cref="CancelEventArgs.Cancel"/> property
///     should be set to
///     <see langword="true"/> to prevent the state change from occurring.
/// </remarks>
public class CancelEventArgs<TResult> : CancelEventArgs
{
    /// <summary>Initializes a new instance of the <see cref="CancelEventArgs{T}"/> class.</summary>
    /// <param name="currentValue">The current (old) value of the property.</param>
    /// <param name="newValue">The value the property will be set to if the event is not cancelled.</param>
    /// <param name="cancel">Whether the event should be canceled or not.</param>
    /// <typeparam name="T">The type of the value for the change being canceled.</typeparam>
    public CancelEventArgs (ref readonly TResult currentValue, ref TResult newValue, bool cancel = false) : base (cancel)
    {
        CurrentValue = currentValue;
        NewValue = newValue;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CancelEventArgs{T}"/> class.
    /// </summary>
    /// <param name="currentValue">The current (old) value of the property.</param>
    /// <param name="newValue">The value the property will be set to if the event is not cancelled.</param>
    protected CancelEventArgs (TResult currentValue, TResult newValue)
    {
        CurrentValue = currentValue;
        NewValue = newValue;
    }

    /// <summary>
    /// 
    /// </summary>
    public TResult? Result { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="currentValue"></param>
    /// <param name="newValue"></param>
    public CancelEventArgs (TResult? result, TResult currentValue, TResult newValue)
    {
        Result = result;
        CurrentValue = currentValue;
        NewValue = newValue;
    }

    /// <summary>The current value of the property.</summary>
    public TResult CurrentValue { get; }

    /// <summary>The value the property will be set to if the event is not cancelled.</summary>
    public TResult NewValue { get; set; }
}
