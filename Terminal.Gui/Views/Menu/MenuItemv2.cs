#nullable enable

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     A <see cref="MenuItemv2"/> has title, an associated help text, and an action to execute on activation. MenuItems
///     can also have a checked indicator (see <see cref="Checked"/>).
/// </summary>
public class MenuItemv2 : Shortcut
{
    /// <summary>
    ///     Creates a new instance of <see cref="MenuItemv2"/>.
    /// </summary>
    public MenuItemv2 () : base (Key.Empty, null, null, null)
    {
    }

    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>, binding it to <paramref name="targetView"/> and
    ///     <paramref name="command"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper API that simplifies creation of multiple Shortcuts when adding them to <see cref="Bar"/>-based
    ///         objects, like <see cref="MenuBarv2"/>.
    ///     </para>
    /// </remarks>
    /// <param name="targetView">
    ///     The View that <paramref name="command"/> will be invoked on when user does something that causes the Shortcut's Accept
    ///     event to be raised.
    /// </param>
    /// <param name="command">
    ///     The Command to invoke on <paramref name="targetView"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>
    /// </param>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="helpText">The help text to display.</param>
    /// <param name="subMenu"></param>
    public MenuItemv2 (View targetView, Command command, string commandText, string? helpText = null, Menuv2? subMenu = null)
        : base (
                targetView?.HotKeyBindings.GetFirstFromCommands (command)!,
                commandText,
                null,
                helpText)
    {
        TargetView = targetView;
        Command = command;

        if (subMenu is { })
        {
            // TODO: This is a temporary hack - add a flag or something instead
            KeyView.Text = $"{Glyphs.RightArrow}";
            subMenu.SuperMenuItem = this;
        }

        SubMenu = subMenu;
    }

    /// <summary>
    ///     Gets the target <see cref="View"/> that the <see cref="Command"/> will be invoked on.
    /// </summary>
    public View? TargetView { get; set; }

    /// <summary>
    ///     Gets the <see cref="Command"/> that will be invoked on <see cref="TargetView"/> when the Shortcut is activated.
    /// </summary>
    public Command Command { get; set; }


    internal override bool? DispatchCommand (ICommandContext? commandContext)
    {
        bool? ret = null;

        if (commandContext is { Command: not Command.HotKey })
        {

            if (TargetView is { })
            {
                if (commandContext is null)
                {
                    commandContext = new CommandContext<KeyBinding> ();
                }
                commandContext.Command = Command;
                ret = TargetView.InvokeCommand (Command, commandContext);
            }
        }

        if (ret is not true)
        {
            ret = base.DispatchCommand (commandContext);
        }

        Logging.Trace ($"{commandContext?.Source?.Title}");

        RaiseAccepted (commandContext);

        return ret;
    }

    /// <summary>
    /// 
    /// </summary>
    public Menuv2? SubMenu { get; set; }

    /// <inheritdoc />
    protected override bool OnMouseEnter (CancelEventArgs eventArgs)
    {
        // Logging.Trace($"OnEnter {Title}");
        SetFocus ();
        return base.OnMouseEnter (eventArgs);
    }


    /// <summary>
    ///     Riases the <see cref="OnAccepted"/>/<see cref="Accepted"/> event indicating this item (or submenu)
    ///     was accepted. This is used to determine when to hide the menu.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected bool? RaiseAccepted (ICommandContext? ctx)
    {
        Logging.Trace ($"RaiseAccepted: {ctx}");
        CommandEventArgs args = new () { Context = ctx };

        OnAccepted (args);
        Accepted?.Invoke (this, args);

        return true;
    }

    /// <summary>
    ///     Called when the user has accepted an item in this menu (or submenu. This is used to determine when to hide the menu.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="args"></param>
    protected virtual void OnAccepted (CommandEventArgs args) { }

    /// <summary>
    ///     Raised when the user has accepted an item in this menu (or submenu. This is used to determine when to hide the menu.
    /// </summary>
    /// <remarks>
    /// <para>
    ///    See <see cref="RaiseAccepted"/> for more information.
    /// </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepted;



    ///// <inheritdoc />
    //public override Attribute GetNormalColor ()
    //{
    //    if (HasFocus || SubMenu is { Visible: true })
    //    {
    //        return base.GetFocusColor ();
    //    }

    //    return base.GetNormalColor ();

    //}

    ///// <inheritdoc />
    //public override Attribute GetHotNormalColor ()
    //{
    //    if (HasFocus || SubMenu is { Visible: true })
    //    {
    //        return base.GetHotFocusColor ();
    //    }

    //    return base.GetHotNormalColor ();

    //}

    /// <inheritdoc />
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
