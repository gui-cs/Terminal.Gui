#nullable enable
namespace Terminal.Gui.App;

/// <summary>
///     Provides data for events that allow modification of a value in a cancellable workflow,
///     such as property changes in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     Used for workflows where an existing value (e.g., property) is being modified or cancelled, such for property
///     changes like <see cref="Orientation"/>.
/// </remarks>
/// <typeparam name="T">The type of the value being changed.</typeparam>
/// <seealso cref="ResultEventArgs{T}"/>
public class ValueChangingEventArgs<T>
{
    /// <summary>
    ///     Gets the current value before the change.
    /// </summary>
    public T CurrentValue { get; }

    /// <summary>
    ///     Gets or sets the proposed new value, which can be modified by event handlers.
    /// </summary>
    public T NewValue { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the change has been handled.
    ///     If true, the change is considered complete or cancelled, per the CWP.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValueChangingEventArgs{T}"/> class.
    /// </summary>
    /// <param name="currentValue">The current value before the change.</param>
    /// <param name="newValue">The proposed new value.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="currentValue"/> or <paramref name="newValue"/> is
    ///     null for non-nullable reference types.
    /// </exception>
    public ValueChangingEventArgs (T currentValue, T newValue)
    {
        if (currentValue is null && !typeof (T).IsValueType)
        {
            throw new ArgumentNullException (nameof (currentValue));
        }

        if (newValue is null && !typeof (T).IsValueType)
        {
            throw new ArgumentNullException (nameof (newValue));
        }

        CurrentValue = currentValue;
        NewValue = newValue;
    }
}
