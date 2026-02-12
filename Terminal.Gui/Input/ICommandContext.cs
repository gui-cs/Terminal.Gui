namespace Terminal.Gui.Input;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Describes the context in which a <see cref="Command"/> is being invoked.
///     When a <see cref="Command"/> is invoked via <see cref="View.InvokeCommand(Command)"/>
///     a context object is passed to Command handlers as an <see cref="ICommandContext"/> reference.
/// </summary>
/// <seealso cref="View.AddCommand(Command)"/>
/// <seealso cref="View.InvokeCommand(Command)"/>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
public interface ICommandContext
{
    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; set; }

    /// <summary>
    ///     A weak reference to the View that was the source of the command invocation, if any.
    ///     (e.g. the view the user clicked on or the view that had focus when a key was pressed).
    ///     Use <c>Source?.TryGetTarget(out View? view)</c> to safely access the source view.
    /// </summary>
    /// <remarks>
    ///     Uses WeakReference to prevent memory leaks and access to disposed views when views are disposed during command
    ///     propagation.
    /// </remarks>
    public WeakReference<View>? Source { get; set; }

    /// <summary>
    ///     The binding that triggered the command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use pattern matching to access specific binding types:
    ///         <code>
    ///         if (ctx.Binding is KeyBinding kb) { /* key binding */ }
    ///         else if (ctx.Binding is MouseBinding mb) { /* mouse binding */ }
    ///         else if (ctx.Binding is CommandBinding ib) { /* programmatic */ }
    ///         </code>
    ///     </para>
    /// </remarks>
    public ICommandBinding? Binding { get; }

    /// <summary>
    ///     Gets whether this command is being dispatched downward to a SubView. When <see langword="true"/>,
    ///     <see cref="View.TryBubbleToSuperView"/> will skip bubbling, preventing re-entry.
    /// </summary>
    public bool IsBubblingDown { get; }
}
