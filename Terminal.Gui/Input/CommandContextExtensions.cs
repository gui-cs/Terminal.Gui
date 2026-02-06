namespace Terminal.Gui.Input;

/// <summary>
///     Extension methods for <see cref="ICommandContext"/>.
/// </summary>
public static class CommandContextExtensions
{
    /// <summary>
    ///     Tries to get the source <see cref="View"/> from a command context.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="source">
    ///     When this method returns, contains the source View if the context is not null and the source weak reference
    ///     target is still alive; otherwise, null.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the context is not null and the source weak reference target is still alive;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This is a convenience method to simplify accessing the source view from a command context.
    ///         It combines null-checking the context and retrieving the weak reference target in one call.
    ///     </para>
    ///     <para>
    ///         Example usage:
    ///         <code>
    ///         if (commandContext.TryGetSource(out View? view))
    ///         {
    ///             // use view
    ///         }
    ///         </code>
    ///     </para>
    /// </remarks>
    public static bool TryGetSource (this ICommandContext? context, out View? source)
    {
        source = null;
        return context?.Source?.TryGetTarget (out source) == true;
    }
}
