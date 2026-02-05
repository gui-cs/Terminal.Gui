namespace Terminal.Gui.ViewBase;

/// <summary>
///     Non-generic interface for accessing a View's value as a boxed object.
/// </summary>
/// <remarks>
///     <para>
///         This interface enables command propagation to carry values without knowing the generic type.
///         When a command is invoked, the source View's value can be captured via <see cref="GetValue"/>
///         and stored in <c>CommandContext.Value</c> for handlers up the hierarchy.
///     </para>
///     <para>
///         Views should implement <see cref="IValue{TValue}"/> rather than this interface directly.
///         The generic interface provides a default implementation of <see cref="GetValue"/>.
///     </para>
/// </remarks>
/// <seealso cref="IValue{TValue}"/>
public interface IValue
{
    /// <summary>
    ///     Gets the value as a boxed object.
    /// </summary>
    /// <returns>The current value, or <see langword="null"/> if no value is set.</returns>
    object? GetValue ();
    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

}

/// <summary>
///     Interface for views that provide a strongly-typed value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <remarks>
///     <para>
///         Views implementing this interface can be used with <c>Prompt&lt;TView, TResult&gt;</c>
///         for automatic result extraction without requiring an explicit <c>resultExtractor</c>.
///     </para>
///     <para>
///         Implementers should use <see cref="CWPPropertyHelper.ChangeProperty{T}"/> to implement
///         the <see cref="Value"/> property setter, which follows the Cancellable Work Pattern (CWP).
///     </para>
///     <para>
///         This interface inherits from <see cref="IValue"/> and provides a default implementation
///         of <see cref="IValue.GetValue"/> that returns the boxed <see cref="Value"/>.
///     </para>
/// </remarks>
/// <seealso cref="IValue"/>
/// <seealso cref="CWPPropertyHelper"/>
/// <seealso cref="ValueChangingEventArgs{T}"/>
/// <seealso cref="ValueChangedEventArgs{T}"/>
public interface IValue<TValue> : IValue
{
    /// <summary>
    ///     Gets or sets the value.
    /// </summary>
    TValue? Value { get; set; }

    /// <summary>
    ///     Raised when <see cref="Value"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    event EventHandler<ValueChangingEventArgs<TValue?>>? ValueChanging;

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    event EventHandler<ValueChangedEventArgs<TValue?>>? ValueChanged;

    /// <inheritdoc/>
    object? IValue.GetValue () => Value;
}
