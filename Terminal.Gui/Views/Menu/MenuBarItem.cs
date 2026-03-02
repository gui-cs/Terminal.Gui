using Terminal.Gui.Tracing;

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Shortcut"/>-derived object to be used as items in a <see cref="MenuBar"/>.
///     MenuBarItems hold a <see cref="PopoverMenu"/> instead of a <see cref="SubMenu"/>.
/// </summary>
public class MenuBarItem : MenuItem, IDesignable
{
    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItem"/>.
    /// </summary>
    public MenuBarItem () : base (null, Command.NotBound) => SetupCommands ();

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItem"/>. Each MenuBarItem typically has a <see cref="PopoverMenu"/>
    ///     that is
    ///     shown when the item is selected.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="targetView">
    ///     The View that <paramref name="command"/> will be invoked on when user does something that causes the MenuBarItems's
    ///     Accept event to be raised.
    /// </param>
    /// <param name="command">
    ///     The Command to invoke on <paramref name="targetView"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>
    /// </param>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="popoverMenu">The Popover Menu that will be displayed when this item is selected.</param>
    public MenuBarItem (View? targetView, Command command, string? commandText, PopoverMenu? popoverMenu = null) : base (targetView, command, commandText)
    {
        TargetView = targetView;
        Command = command;
        PopoverMenu = popoverMenu;
        SetupCommands ();
    }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItem"/> with the specified <paramref name="popoverMenu"/>. This is a
    ///     helper for the most common MenuBar use-cases.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="popoverMenu">The Popover Menu that will be displayed when this item is selected.</param>
    public MenuBarItem (string commandText, PopoverMenu? popoverMenu = null) : this (null, Command.NotBound, commandText, popoverMenu) { }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItem"/> with the <paramref name="menuItems"/> automatcialy added to a
    ///     <see cref="PopoverMenu"/>.
    ///     This is a helper for the most common MenuBar use-cases.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="menuItems">
    ///     The menu items that will be added to the Popover Menu that will be displayed when this item is
    ///     selected.
    /// </param>
    public MenuBarItem (string commandText, IEnumerable<View> menuItems) : this (null,
                                                                                 Command.NotBound,
                                                                                 commandText,
                                                                                 new PopoverMenu (menuItems)
                                                                                 {
#if DEBUG
                                                                                     Id = $"PopoverMenu ({commandText})"
#endif
                                                                                 })
    { }

    /// <summary>
    ///     Initializes <see cref="MenuBarItem"/>-specific command handlers.
    /// </summary>
    private void SetupCommands () =>

        // Override the default HotKey handler to skip SetFocus before InvokeCommand(Activate).
        // DefaultHotKeyHandler calls SetFocus() which triggers OnSelectedMenuItemChanged on MenuBar,
        // opening the popover BEFORE Activate can toggle it — causing it to open then immediately close
        // when switching between MenuBarItems via HotKey.
        AddCommand (Command.HotKey,
                    ctx =>
                    {
                        Trace.Command (this, ctx, "Entry", "HotKey handler");

                        if (RaiseHandlingHotKey (ctx) is true)
                        {
                            Trace.Command (this, ctx, "Handled", "RaiseHandlingHotKey returned true");

                            return false;
                        }

                        RaiseHotKeyCommand (ctx);

                        Trace.Command (this, ctx, "InvokeActivate", "Before InvokeCommand(Activate)");

                        // ShowItem/HideItem in MenuBar.OnActivating handles focus.
                        InvokeCommand (Command.Activate, ctx?.Binding);

                        return true;
                    });

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        Trace.Command (this, args.Context, "Entry", $"PopoverMenuOpen={PopoverMenuOpen} Routing={args.Context?.Routing}");

        if (base.OnActivating (args))
        {
            Trace.Command (this, args.Context, "BaseHandled", "base.OnActivating returned true");

            return true;
        }

        // Bridged commands come FROM the PopoverMenu (e.g., a MenuItem was activated inside it).
        // This is a notification — do not toggle the PopoverMenu open/closed.
        if (args.Context?.Routing == CommandRouting.Bridged)
        {
            Trace.Command (this, args.Context, "Bridged", "Ignoring bridged command — no toggle");

            return false;
        }

        PopoverMenuOpen = !PopoverMenuOpen;

        return false;
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        if (PopoverMenu is null || PopoverMenu.IsInitialized)
        {
            return;
        }

        PopoverMenu.App ??= App;
        PopoverMenu.BeginInit ();
        PopoverMenu.EndInit ();
    }

    /// <summary>
    ///     Do not use this property. MenuBarItem does not support SubMenu. Use <see cref="PopoverMenu"/> instead.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public new Menu? SubMenu { get => null; set => throw new InvalidOperationException ("MenuBarItem does not support SubMenu. Use PopoverMenu instead."); }

    /// <summary>
    ///     The Popover Menu that will be displayed when this item is selected.
    /// </summary>
    public PopoverMenu? PopoverMenu
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            if (field is { })
            {
                field.VisibleChanged -= OnPopoverMenuVisibleChanged;
                field.Target = null;
            }

            field = value;

            if (field is null)
            {
                return;
            }

            Trace.Command (this, "PopoverMenuSet", $"PopoverMenu={field.ToIdentifyingString ()}");

            // Set Target for base class bridge + focus tracking
            field.Target = new WeakReference<View> (this);

            // Set Anchor for positioning below MenuBarItem
            field.Anchor = () => FrameToScreen ();

            // Relay VisibleChanged to PopoverMenuOpenChanged for consumers (e.g. MenuBar)
            field.VisibleChanged += OnPopoverMenuVisibleChanged;
        }
    }

    /// <summary>
    ///     Gets or sets whether the PopoverMenu is open and visible or not.
    ///     Delegates to <see cref="PopoverMenu"/>.<see cref="View.Visible"/>.
    /// </summary>
    public bool PopoverMenuOpen
    {
        get => PopoverMenu?.Visible ?? false;
        set
        {
            if (PopoverMenu is null)
            {
                return;
            }

            if (value)
            {
                PopoverMenu.MakeVisible ();
            }
            else
            {
                PopoverMenu.Visible = false;
            }
        }
    }

    /// <summary>
    ///     Raised when <see cref="PopoverMenuOpen"/> has changed. Relayed from
    ///     <see cref="View.VisibleChanged"/>.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? PopoverMenuOpenChanged;

    private void OnPopoverMenuVisibleChanged (object? sender, EventArgs e)
    {
        bool isOpen = PopoverMenu?.Visible ?? false;
        PopoverMenuOpenChanged?.Invoke (this, new ValueChangedEventArgs<bool> (!isOpen, isOpen));
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        Trace.Keyboard (this, key, "Entry", $"PopoverMenuVisible={PopoverMenu is { Visible: true }}");

        if (PopoverMenu is not { Visible: true } || !HotKeyBindings.TryGet (key, out _))
        {
            return false;
        }

        Trace.Keyboard (this, key, "HotKeyMatch", "Popover visible + HotKey match — hiding");

        // If the user presses the hotkey for a menu item that is already open,
        // it should close the menu item (Test: MenuBarItem_HotKey_DeActivates)
        if (SuperView is MenuBar menuBar)
        {
            menuBar.HideActiveItem ();
        }

        return true;
    }

    /// <inheritdoc/>
    public new bool EnableForDesign ()
    {
        PopoverMenu = new PopoverMenu ();
        PopoverMenu.Root = new Menu ();
        PopoverMenu.Root.EnableForDesign ();

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            PopoverMenu?.Dispose ();
            PopoverMenu = null;
        }

        base.Dispose (disposing);
    }
}
