#nullable enable

using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Shortcut"/>-derived object to be used as a menu item in a <see cref="Menuv2"/>. Has title, an
///     A <see cref="Shortcut"/>-derived object to be used as a menu item in a <see cref="Menuv2"/>. Has title, an
///     associated help text, and an action to execute on activation.
/// </summary>
public class MenuItemv2 : Shortcut
{
    /// <summary>
    ///     Creates a new instance of <see cref="MenuItemv2"/>.
    /// </summary>
    public MenuItemv2 () : base (Key.Empty, null, null) { }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuItemv2"/>, binding it to <paramref name="targetView"/> and
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
    public MenuItemv2 (View? targetView, Command command, string? commandText = null, string? helpText = null, Menuv2? subMenu = null)
        : base (
                targetView?.HotKeyBindings.GetFirstFromCommands (command)!,
                string.IsNullOrEmpty (commandText) ? GlobalResources.GetString ($"cmd.{command}") : commandText,
                null,
                string.IsNullOrEmpty (helpText) ? GlobalResources.GetString ($"cmd.{command}.Help") : helpText
               )
    {
        TargetView = targetView;
        Command = command;
        SubMenu = subMenu;
    }

    /// <inheritdoc/>
    public MenuItemv2 (string? commandText = null, string? helpText = null, Action? action = null, Key? key = null)
        : base (key ?? Key.Empty, commandText, action, helpText)
    { }

    /// <inheritdoc/>
    public MenuItemv2 (string commandText, Key key, Action? action = null)
        : base (key ?? Key.Empty, commandText, action, null)
    { }

    /// <inheritdoc/>
    public MenuItemv2 (string? commandText = null, string? helpText = null, Menuv2? subMenu = null)
        : base (Key.Empty, commandText, null, helpText)
    {
        SubMenu = subMenu;
    }

    // TODO: Consider moving TargetView and Command to Shortcut?

    /// <summary>
    ///     Gets the target <see cref="View"/> that the <see cref="Command"/> will be invoked on.
    /// </summary>
    public View? TargetView { get; set; }

    private Command _command;

    /// <summary>
    ///     Gets the <see cref="Command"/> that will be invoked on <see cref="TargetView"/> when the MenuItem is selected.
    /// </summary>
    public Command Command
    {
        get => _command;
        set
        {
            if (_command == value)
            {
                return;
            }

            _command = value;

            if (string.IsNullOrEmpty (Title))
            {
                Title = GlobalResources.GetString ($"cmd.{_command}") ?? string.Empty;
            }

            if (string.IsNullOrEmpty (HelpText))
            {
                HelpText = GlobalResources.GetString ($"cmd.{_command}.Help") ?? string.Empty;
            }
        }
    }

    internal override bool? DispatchCommand (ICommandContext? commandContext)
    {
        // Logging.Debug ($"{Title} - {commandContext?.Source?.Title} Command: {commandContext?.Command}");
        bool? ret = null;

        bool quit = false;

        if (commandContext is CommandContext<KeyBinding> keyCommandContext)
        {
            if (keyCommandContext.Binding.Key is { } && keyCommandContext.Binding.Key == Application.QuitKey && SuperView is { Visible: true })
            {
                // This supports a MenuItem with Key = Application.QuitKey/Command = Command.Quit
                // Logging.Debug ($"{Title} - Ignoring Key = Application.QuitKey/Command = Command.Quit");
                quit = true;
                //ret = true;
            }
        }

        // Translate the incoming command to Command
        if (Command != Command.NotBound && commandContext is { })
        {
            commandContext.Command = Command;
        }

        if (!quit)
        {
            if (TargetView is { })
            {
                // Logging.Debug ($"{Title} - InvokeCommand on TargetView ({TargetView.Title})...");
                ret = TargetView.InvokeCommand (Command, commandContext);
            }
            else
            {
                // Is this an Application-bound command?
                // Logging.Debug ($"{Title} - Application.InvokeCommandsBoundToKey ({Key})...");
                ret = Application.InvokeCommandsBoundToKey (Key);
            }
        }

        if (ret is not true)
        {
            // Logging.Debug ($"{Title} - calling base.DispatchCommand...");
            // Base will Raise Selected, then Accepting, then invoke the Action, if any
            ret = base.DispatchCommand (commandContext);
        }

        if (ret is true)
        {
            // Logging.Debug ($"{Title} - Calling RaiseAccepted");
            RaiseAccepted (commandContext);
        }

        return ret;
    }

    ///// <inheritdoc />
    //protected override bool OnAccepting (CommandEventArgs e)
    //{
    //    // Logging.Debug ($"{Title} - calling base.OnAccepting: {e.Context?.Command}");
    //    bool? ret = base.OnAccepting (e);

    //    if (ret is true || e.Cancel)
    //    {
    //        return true;
    //    }

    //    //RaiseAccepted (e.Context);

    //    return ret is true;
    //}

    private Menuv2? _subMenu;

    /// <summary>
    ///     The submenu to display when the user selects this menu item.
    /// </summary>
    public Menuv2? SubMenu
    {
        get => _subMenu;
        set
        {
            _subMenu = value;

            if (_subMenu is { })
            {
                SubMenu!.Visible = false;
                // TODO: This is a temporary hack - add a flag or something instead
                KeyView.Text = $"{Glyphs.RightArrow}";
                _subMenu.SuperMenuItem = this;
            }
        }
    }

    /// <inheritdoc/>
    protected override bool OnMouseEnter (CancelEventArgs eventArgs)
    {
        // When the mouse enters a menuitem, we set focus to it automatically.

        // Logging.Trace($"OnEnter {Title}");
        SetFocus ();

        return base.OnMouseEnter (eventArgs);
    }

    // TODO: Consider moving Accepted to Shortcut?

    /// <summary>
    ///     Raises the <see cref="OnAccepted"/>/<see cref="Accepted"/> event indicating this item (or submenu)
    ///     was accepted. This is used to determine when to hide the menu.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected void RaiseAccepted (ICommandContext? ctx)
    {
        //Logging.Trace ($"RaiseAccepted: {ctx}");
        CommandEventArgs args = new () { Context = ctx };

        OnAccepted (args);
        Accepted?.Invoke (this, args);
    }

    /// <summary>
    ///     Called when the user has accepted an item in this menu (or submenu). This is used to determine when to hide the
    ///     menu.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="args"></param>
    protected virtual void OnAccepted (CommandEventArgs args) { }

    /// <summary>
    ///     Raised when the user has accepted an item in this menu (or submenu). This is used to determine when to hide the
    ///     menu.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="RaiseAccepted"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepted;

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            SubMenu?.Dispose ();
            SubMenu = null;
        }

        base.Dispose (disposing);
    }
}
