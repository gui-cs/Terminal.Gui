namespace Terminal.Gui.App;

#nullable enable

/// <summary>
///     Provides helper methods for executing property change workflows in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     <para>
///         Used for workflows where a property value is modified, such as in <see cref="OrientationHelper"/> or
///         <see cref="View.SchemeName"/>, allowing pre- and post-change events to customize or cancel the change.
///     </para>
/// </remarks>
/// <seealso cref="ValueChangingEventArgs{T}"/>
/// <seealso cref="ValueChangedEventArgs{T}"/>
public static class CWPPropertyHelper
{
    /// <summary>
    ///     Executes a CWP workflow for a property change, with pre- and post-change events.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the property value, which may be a nullable reference type (e.g., <see cref="string"/>
    ///     ?).
    /// </typeparam>
    /// <param name="currentValue">The current property value, which may be null for nullable types.</param>
    /// <param name="newValue">The proposed new property value, which may be null for nullable types.</param>
    /// <param name="onChanging">The virtual method invoked before the change, returning true to cancel.</param>
    /// <param name="changingEvent">The pre-change event raised to allow modification or cancellation.</param>
    /// <param name="onChanged">The virtual method invoked after the change.</param>
    /// <param name="changedEvent">The post-change event raised to notify of the completed change.</param>
    /// <param name="finalValue">
    ///     The final value after the workflow, reflecting any modifications, which may be null for
    ///     nullable types.
    /// </param>
    /// <returns>True if the property was changed, false if cancelled.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <see cref="ValueChangingEventArgs{T}.NewValue"/> is null for non-nullable reference types after the
    ///     workflow.
    /// </exception>
    /// <example>
    ///     <code>
    ///         string? current = null;
    ///         string? proposed = "Base";
    ///         Func&lt;ValueChangingEventArgs&lt;string?&gt;, bool&gt; onChanging = args =&gt; false;
    ///         EventHandler&lt;ValueChangingEventArgs&lt;string?&gt;&gt;? changingEvent = null;
    ///         Action&lt;ValueChangedEventArgs&lt;string?&gt;&gt;? onChanged = args =&gt;
    ///             Console.WriteLine($"SchemeName changed to {args.NewValue ?? "none"}.");
    ///         EventHandler&lt;ValueChangedEventArgs&lt;string?&gt;&gt;? changedEvent = null;
    ///         bool changed = CWPPropertyHelper.ChangeProperty(
    ///             current, proposed, onChanging, changingEvent, onChanged, changedEvent, out string? final);
    ///     </code>
    /// </example>
    public static bool ChangeProperty<T> (
        T currentValue,
        T newValue,
        Func<ValueChangingEventArgs<T>, bool> onChanging,
        EventHandler<ValueChangingEventArgs<T>>? changingEvent,
        Action<ValueChangedEventArgs<T>>? onChanged,
        EventHandler<ValueChangedEventArgs<T>>? changedEvent,
        out T finalValue
    )
    {
        if (EqualityComparer<T>.Default.Equals (currentValue, newValue))
        {
            finalValue = currentValue;

            return false;
        }

        ValueChangingEventArgs<T> args = new (currentValue, newValue);
        bool cancelled = onChanging (args) || args.Handled;

        if (cancelled)
        {
            finalValue = currentValue;

            return false;
        }

        changingEvent?.Invoke (null, args);

        if (args.Handled)
        {
            finalValue = currentValue;

            return false;
        }

        // Validate NewValue for non-nullable reference types
        if (args.NewValue is null && !typeof (T).IsValueType && !Nullable.GetUnderlyingType (typeof (T))?.IsValueType == true)
        {
            throw new InvalidOperationException ("NewValue cannot be null for non-nullable reference types.");
        }

        finalValue = args.NewValue;
        ValueChangedEventArgs<T> changedArgs = new (currentValue, finalValue);
        onChanged?.Invoke (changedArgs);
        changedEvent?.Invoke (null, changedArgs);

        return true;
    }
}
