using Terminal.Gui.App;

namespace Terminal.Gui;

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
/// </remarks>
/// <seealso cref="CWPPropertyHelper"/>
/// <seealso cref="ValueChangingEventArgs{T}"/>
/// <seealso cref="ValueChangedEventArgs{T}"/>
public interface IValue<TValue>
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
}
