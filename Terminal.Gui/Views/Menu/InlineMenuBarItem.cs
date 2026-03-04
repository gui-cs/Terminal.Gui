using Terminal.Gui.Tracing;

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="MenuItem"/>-derived item for use in a <see cref="MenuBar"/>. Unlike
///     <see cref="MenuBarItem"/>, which uses a modal <see cref="PopoverMenu"/>, this class uses
///     the inherited <see cref="MenuItem.SubMenu"/> mechanism to display a non-modal, inline
///     drop-down menu.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="InlineMenuBarItem"/> is designed for scenarios that need non-modal menus —
///         for example, the Terminal.Gui Designer (TGD), where menus must stay open for extended
///         editing, allow simultaneous visibility, and support add/remove/reorder while open.
///     </para>
///     <para>
///         <b>SubMenu Integration:</b> The <see cref="MenuItem.SubMenu"/> is displayed as a
///         drop-down below this item. A <see cref="Glyphs.DownArrow"/> glyph is shown in the
///         <see cref="Shortcut.KeyView"/> instead of the default right-arrow.
///     </para>
///     <para>
///         <b>Activation:</b> Overrides <see cref="View.OnActivating"/> to toggle
///         <see cref="IMenuBarEntry.IsMenuOpen"/>. Bridged commands (originating from within the
///         SubMenu) are ignored to avoid unintended toggling.
///     </para>
///     <para>
///         See <see href="https://gui-cs.github.io/Terminal.Gui/docs/menus.html">Menus Deep Dive</see>
///         for the full menu system architecture.
///     </para>
/// </remarks>
public class InlineMenuBarItem : MenuItem, IMenuBarEntry, IDesignable
{
    /// <summary>
    ///     Creates a new instance of <see cref="InlineMenuBarItem"/>.
    /// </summary>
    public InlineMenuBarItem () : base (null, Command.NotBound) => SetupCommands ();

    /// <summary>
    ///     Creates a new instance of <see cref="InlineMenuBarItem"/> with the specified command text and SubMenu.
    /// </summary>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="subMenu">The SubMenu that will be displayed when this item is selected.</param>
    public InlineMenuBarItem (string commandText, Menu? subMenu = null) : base (commandText, null, subMenu)
    {
        SetupCommands ();
    }

    /// <summary>
    ///     Creates a new instance of <see cref="InlineMenuBarItem"/> with the specified command text and
    ///     menu items automatically added to a new <see cref="Menu"/>.
    /// </summary>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="menuItems">
    ///     The menu items that will be added to the SubMenu displayed when this item is selected.
    /// </param>
    public InlineMenuBarItem (string commandText, IEnumerable<View> menuItems) : base (commandText, null,
        new Menu (menuItems)
        {
#if DEBUG
            Id = $"InlineMenu ({commandText})"
#endif
        })
    {
        SetupCommands ();
    }

    /// <inheritdoc/>
    protected override Rune SubMenuGlyph => Glyphs.DownArrow;

    /// <summary>
    ///     Initializes <see cref="InlineMenuBarItem"/>-specific command handlers.
    /// </summary>
    private void SetupCommands () =>

        // Override the default HotKey handler to skip SetFocus before InvokeCommand(Activate).
        // Same pattern as MenuBarItem: prevents premature menu opening when switching
        // between items via HotKey.
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

                        // ShowEntry/HideEntry in MenuBar.OnActivating handles focus.
                        InvokeCommand (Command.Activate, ctx?.Binding);

                        return true;
                    });

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        Trace.Command (this, args.Context, "Entry", $"IsMenuOpen={((IMenuBarEntry)this).IsMenuOpen}");

        if (base.OnActivating (args))
        {
            return true;
        }

        // Bridged commands come FROM the SubMenu (e.g., a MenuItem was activated inside it).
        // This is a notification — do not toggle the SubMenu open/closed.
        if (args.Context?.Routing == CommandRouting.Bridged)
        {
            Trace.Command (this, args.Context, "Bridged", "Ignoring bridged command — no toggle");

            return false;
        }

        IMenuBarEntry entry = this;

        // When closing the menu, guard against reentrant reopening:
        // HideMenu() → Visible=false → focus returns to MenuBar → OnFocusedChanged →
        // OnSelectedMenuItemChanged sees _popoverBrowsingMode=true + IsMenuOpen=false → ShowEntry.
        // Setting IsSwitchingItem prevents OnSelectedMenuItemChanged from auto-opening.
        if (entry.IsMenuOpen && SuperView is MenuBar menuBar)
        {
            menuBar.IsSwitchingItem = true;

            try
            {
                entry.IsMenuOpen = false;
            }
            finally
            {
                menuBar.IsSwitchingItem = false;
            }
        }
        else
        {
            entry.IsMenuOpen = !entry.IsMenuOpen;
        }

        return false;
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        if (SubMenu is { } && !SubMenu.IsInitialized)
        {
            SubMenu.App ??= App;
            SubMenu.BeginInit ();
            SubMenu.EndInit ();
        }

        SetupSubMenuNavigation ();
    }

    private bool _subMenuNavigationSetup;

    /// <summary>
    ///     Subscribes to the SubMenu's <see cref="View.KeyDown"/> event to forward
    ///     Left/Right arrow keys to the parent <see cref="MenuBar"/> for entry switching,
    ///     and Escape for closing the menu.
    /// </summary>
    private void SetupSubMenuNavigation ()
    {
        if (SubMenu is null || _subMenuNavigationSetup)
        {
            return;
        }

        _subMenuNavigationSetup = true;

        SubMenu.KeyDown += OnSubMenuKeyDown;
    }

    private void OnSubMenuKeyDown (object? sender, Key e)
    {
        if (SuperView is not MenuBar menuBar)
        {
            return;
        }

        if (e == Key.CursorRight)
        {
            e.Handled = true;
            menuBar.InvokeCommand (Command.Right);
        }
        else if (e == Key.CursorLeft)
        {
            e.Handled = true;
            menuBar.InvokeCommand (Command.Left);
        }
        else if (e == Application.QuitKey)
        {
            e.Handled = true;
            menuBar.HideActiveItem ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        Trace.Keyboard (this, key, "Entry", $"SubMenuVisible={SubMenu is { Visible: true }}");

        if (SubMenu is not { Visible: true } || !HotKeyBindings.TryGet (key, out _))
        {
            return false;
        }

        Trace.Keyboard (this, key, "HotKeyMatch", "SubMenu visible + HotKey match — hiding");

        // If the user presses the hotkey for a menu item that is already open,
        // it should close the menu item.
        if (SuperView is MenuBar menuBar)
        {
            menuBar.HideActiveItem ();
        }

        return true;
    }

    #region IMenuBarEntry

    /// <inheritdoc/>
    bool IMenuBarEntry.IsMenuOpen
    {
        get => SubMenu?.Visible ?? false;
        set
        {
            if (SubMenu is null)
            {
                return;
            }

            if (value)
            {
                SubMenu.ShowMenu ();
            }
            else
            {
                SubMenu.HideMenu ();
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<bool>>? MenuOpenChanged;

    /// <inheritdoc/>
    Menu? IMenuBarEntry.RootMenu => SubMenu;

    #endregion IMenuBarEntry

    /// <inheritdoc/>
    protected override void OnVisibleChanged ()
    {
        base.OnVisibleChanged ();

        // Relay SubMenu visibility changes to MenuOpenChanged for MenuBar consumption.
        // We subscribe to our own SubMenu's VisibleChanged indirectly through EndInit wiring.
    }

    /// <summary>
    ///     Subscribes to SubMenu visibility changes to relay them through <see cref="MenuOpenChanged"/>.
    /// </summary>
    internal void SubscribeToSubMenuVisibility ()
    {
        if (SubMenu is { })
        {
            SubMenu.VisibleChanged += OnSubMenuVisibleChanged;
        }
    }

    /// <summary>
    ///     Unsubscribes from SubMenu visibility changes.
    /// </summary>
    internal void UnsubscribeFromSubMenuVisibility ()
    {
        if (SubMenu is { })
        {
            SubMenu.VisibleChanged -= OnSubMenuVisibleChanged;
        }
    }

    private void OnSubMenuVisibleChanged (object? sender, EventArgs e)
    {
        bool isOpen = SubMenu?.Visible ?? false;
        MenuOpenChanged?.Invoke (this, new ValueChangedEventArgs<bool> (!isOpen, isOpen));
    }

    /// <inheritdoc/>
    public new bool EnableForDesign ()
    {
        SubMenu = new Menu ();
        SubMenu.EnableForDesign ();

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            UnsubscribeFromSubMenuVisibility ();

            if (SubMenu is { })
            {
                SubMenu.KeyDown -= OnSubMenuKeyDown;
            }
        }

        base.Dispose (disposing);
    }
}
