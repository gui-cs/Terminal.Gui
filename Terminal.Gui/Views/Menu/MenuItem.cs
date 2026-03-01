using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Shortcut"/>-derived object to be used as a menu item in a <see cref="Menu"/>. Has title, an
///     associated help text, and an action to execute on activation.
/// </summary>
public class MenuItem : Shortcut, IValue
{
    /// <summary>
    ///     Creates a new instance of <see cref="MenuItem"/>.
    /// </summary>
    public MenuItem () : base (Key.Empty, null, null) { }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuItem"/>, binding it to <paramref name="targetView"/> and
    ///     <paramref name="command"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="targetView">
    ///     The View that <paramref name="command"/> will be invoked on when user does something that causes the Shortcut's
    ///     Accept
    ///     event to be raised.
    /// </param>
    /// <param name="command">
    ///     The Command to invoke on <paramref name="targetView"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>
    /// </param>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="helpText">The help text to display.</param>
    /// <param name="subMenu">The submenu to display when the user selects this menu item.</param>
    public MenuItem (View? targetView, Command command, string? commandText = null, string? helpText = null, Menu? subMenu = null) :
        base (targetView?.HotKeyBindings.GetFirstFromCommands (command)!,
              string.IsNullOrEmpty (commandText) ? GlobalResources.GetString ($"cmd{command}") : commandText,
              null,
              string.IsNullOrEmpty (helpText) ? GlobalResources.GetString ($"cmd{command}_Help") : helpText)
    {
        TargetView = targetView;
        Command = command;
        SubMenu = subMenu;
    }

    /// <inheritdoc/>
    public MenuItem (string? commandText = null, string? helpText = null, Action? action = null, Key? key = null) : base (key ?? Key.Empty,
        commandText,
        action,
        helpText)
    { }

    /// <inheritdoc/>
    public MenuItem (string commandText, Key key, Action? action = null) : base (key ?? Key.Empty, commandText, action) { }

    /// <inheritdoc/>
    public MenuItem (string? commandText = null, string? helpText = null, Menu? subMenu = null) : base (Key.Empty, commandText, null, helpText) =>
        SubMenu = subMenu;

    private CommandBridge? _subMenuBridge;

    /// <summary>
    ///     The submenu to display when the user selects this menu item.
    /// </summary>
    public Menu? SubMenu
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            // Tear down old bridge
            _subMenuBridge?.Dispose ();
            _subMenuBridge = null;

            field = value;

            if (field is null)
            {
                return;
            }

            field!.App ??= App;
            field!.Visible = false;

            // TODO: This is a temporary hack - add a flag or something instead
            KeyView.Text = $"{Glyphs.RightArrow}";
            field.SuperMenuItem = this;

            // Bridge Activate and Accept from SubMenu → this MenuItem across the
            // non-containment boundary. SubMenu is not a SubView of this MenuItem,
            // so commands can't bubble naturally; the bridge relays completion events.
            _subMenuBridge = CommandBridge.Connect (this, field, Command.Activate, Command.Accept);
        }
    }

    /// <inheritdoc/>
    public object? GetValue () => Title;

    /// <inheritdoc/>
    event EventHandler<ValueChangedEventArgs<object?>>? IValue.ValueChangedUntyped
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    protected override bool OnMouseEnter (CancelEventArgs eventArgs)
    {
        // When the mouse enters a menuitem, we set focus to it automatically.

        // Logging.Trace($"OnEnter {this.ToIdentifyingString ()}");
        SetFocus ();

        return base.OnMouseEnter (eventArgs);
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _subMenuBridge?.Dispose ();
            _subMenuBridge = null;

            SubMenu?.Dispose ();
            SubMenu = null;
        }

        base.Dispose (disposing);
    }
}
