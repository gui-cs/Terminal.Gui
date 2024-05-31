using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui;

/// <summary>
///     Defines the scope of a <see cref="Command"/> that has been bound to a key with
///     <see cref="KeyBindings.Add(Key, Terminal.Gui.Command[])"/>.
/// </summary>
/// <remarks>
///     <para>Key bindings are scoped to the most-focused view (<see cref="Focused"/>) by default.</para>
/// </remarks>
[Flags]
[GenerateEnumExtensionMethods (FastHasFlags = true)]
public enum KeyBindingScope
{
    /// <summary>The key binding is scoped to just the view that has focus.</summary>
    Focused = 1,

    /// <summary>
    ///     The key binding is scoped to the View's Superview hierarchy and will be triggered even when the View does not have
    ///     focus, as
    ///     long as the SuperView does have focus. This is typically used for <see cref="View.HotKey"/>s.
    ///     <remarks>
    ///         <para>
    ///             The View must be visible.
    ///         </para>
    ///         <para>
    ///             HotKey-scoped key bindings are only invoked if the key down event was not handled by the focused view or
    ///             any of its subviews.
    ///         </para>
    ///     </remarks>
    /// </summary>
    HotKey = 2,

    /// <summary>
    ///     The key binding will be triggered regardless of which view has focus. This is typically used for global
    ///     commands, which are called Shortcuts.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Application-scoped key bindings are only invoked if the key down event was not handled by the focused view or
    ///         any of its subviews, and if the key down event was not bound to a <see cref="View.HotKey"/>.
    ///     </para>
    ///     <para>
    ///         <see cref="Shortcut"/> makes it easy to add Application-scoped key bindings with a visual indicator. See also <see cref="Bar"/>.
    ///     </para>
    /// </remarks>
    Application = 4
}
