using System.ComponentModel;

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
                        if (RaiseHandlingHotKey (ctx) is true)
                        {
                            return false;
                        }

                        RaiseHotKeyCommand (ctx);

                        // ShowItem/HideItem in MenuBar.OnActivating handles focus.
                        InvokeCommand (Command.Activate, ctx?.Binding);

                        return true;
                    });

    /// <inheritdoc />
    protected override bool OnActivating (CommandEventArgs args)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} {args}");

        if (base.OnActivating (args))
        {
            return true;
        }

        // Bridged commands come FROM the PopoverMenu (e.g., a MenuItem was activated inside it).
        // This is a notification — do not toggle the PopoverMenu open/closed.
        if (args.Context?.Routing == CommandRouting.Bridged)
        {
            return false;
        }

        if (PopoverMenuOpen)
        {
            PopoverMenuOpen = false;
        }
        else
        {
            RegisterPopover ();

            PopoverMenuOpen = true;
        }

        return false;
    }

    private void RegisterPopover ()
    {
        if (App is { Popovers: { } } && !App.Popovers.IsRegistered (PopoverMenu))
        {
            App.Popovers.Register (PopoverMenu);
        }
    }

    /// <inheritdoc />
    protected override void OnAccepted (ICommandContext? ctx)
    {
        base.OnAccepted (ctx);

    }

    /// <inheritdoc />
    public override void EndInit ()
    {
        base.EndInit ();

        if (PopoverMenu?.IsInitialized is true)
        {
            return;
        }
        PopoverMenu?.BeginInit ();
        PopoverMenu?.EndInit ();
        PopoverMenu?.App = App;
        RegisterPopover ();
    }

    /// <summary>
    ///     Do not use this property. MenuBarItem does not support SubMenu. Use <see cref="PopoverMenu"/> instead.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public new Menu? SubMenu { get => null; set => throw new InvalidOperationException ("MenuBarItem does not support SubMenu. Use PopoverMenu instead."); }

    private CommandBridge? _popoverBridge;

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
                field.VisibleChanged -= OnPopoverVisibleChanged;
                _popoverBridge?.Dispose ();
                _popoverBridge = null;
            }

            field = value;

            if (field is { })
            {
#if DEBUG
                Id = $"{Id}.{field.Id}";
#endif

                RegisterPopover ();
                PopoverMenuOpen = field.Visible;
                field.VisibleChanged += OnPopoverVisibleChanged;

                // Bridge Activate from PopoverMenu → MenuBarItem across the non-containment boundary.
                _popoverBridge = CommandBridge.Connect (this, field, Command.Activate);
            }

            return;

            void OnPopoverVisibleChanged (object? sender, EventArgs args) =>

                // Logging.Debug ($"OnPopoverVisibleChanged - {this.ToIdentifyingString ()} - Visible = {_popoverMenu?.Visible} ");
                PopoverMenuOpen = field?.Visible ?? false;
        }
    }

    /// <summary>
    ///     Gets whether the PopoverMenu is open and visible or not.
    /// </summary>
    public bool PopoverMenuOpen
    {
        get; 
        set
        {
            if (field == value)
            {
                return;
            }

            CWPPropertyHelper.ChangeProperty (this,
                                              ref field,
                                              value,
                                              OnPopoverMenuOpenChanging,
                                              PopoverMenuOpenChanging,
                                              newValue =>
                                              {
                                                  field = newValue;

                                                  if (field)
                                                  {
                                                      // MakeVisible requires the Application's popover infrastructure.
                                                      // Guard against calls when App is not available (e.g., in design mode
                                                      // or unit tests without Application.Init).
                                                      if (PopoverMenu is { } && IsInitialized)
                                                      {
                                                          PopoverMenu.MakeVisible (new Point (FrameToScreen ().X, FrameToScreen ().Bottom));
                                                      }
                                                  }
                                                  else
                                                  {
                                                      PopoverMenu?.Visible = false;
                                                  }
                                              },
                                              OnPopoverMenuOpenChanged,
                                              PopoverMenuOpenChanged,
                                              out _);
        }
    }

    /// <summary>
    /// </summary>
    protected virtual bool OnPopoverMenuOpenChanging (ValueChangingEventArgs<bool> args) => false;

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<bool>>? PopoverMenuOpenChanging;

    /// <summary>
    /// </summary>
    protected virtual void OnPopoverMenuOpenChanged (ValueChangedEventArgs<bool> args) { }

    /// <summary>
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? PopoverMenuOpenChanged;

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({key})");

        if (PopoverMenu is { Visible: true } && HotKeyBindings.TryGet (key, out _))
        {
            // If the user presses the hotkey for a menu item that is already open,
            // it should close the menu item (Test: MenuBarItem_HotKey_DeActivates)
            if (SuperView is MenuBar { } menuBar)
            {
                menuBar.HideActiveItem ();
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        // Logging.Debug ($"CanFocus = {CanFocus}, HasFocus = {HasFocus}");

        if (newHasFocus)
        {
            return;
        }

        PopoverMenuOpen = false;
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
            if (App is { Popovers: { } } && !App.Popovers.IsRegistered (PopoverMenu))
            {
                App.Popovers.DeRegister (PopoverMenu);
            }

            PopoverMenu?.Dispose ();
            PopoverMenu = null;
        }

        base.Dispose (disposing);
    }
}
