﻿namespace Terminal.Gui;

/// <summary>
///     ContextMenu provides a pop-up menu that can be positioned anywhere within a <see cref="View"/>. ContextMenu is
///     analogous to <see cref="MenuBar"/> and, once activated, works like a sub-menu of a <see cref="MenuBarItem"/> (but
///     can be positioned anywhere).
///     <para>
///         By default, a ContextMenu with sub-menus is displayed in a cascading manner, where each sub-menu pops out of
///         the ContextMenu frame (either to the right or left, depending on where the ContextMenu is relative to the edge
///         of the screen). By setting <see cref="UseSubMenusSingleFrame"/> to <see langword="true"/>, this behavior can be
///         changed such that all sub-menus are drawn within the ContextMenu frame.
///     </para>
///     <para>
///         ContextMenus can be activated using the Shift-F10 key (by default; use the <see cref="Key"/> to change to
///         another key).
///     </para>
///     <para>
///         Callers can cause the ContextMenu to be activated on a right-mouse click (or other interaction) by calling
///         <see cref="Show()"/>.
///     </para>
///     <para>ContextMenus are located using screen using screen coordinates and appear above all other Views.</para>
/// </summary>
public sealed class ContextMenu : IDisposable
{
    private static MenuBar _menuBar;

    /// <summary>Initializes a context menu with no menu items.</summary>
    public ContextMenu ()
    {
        if (IsShow)
        {
            if (_menuBar.SuperView != null)
            {
                Hide ();
            }

            IsShow = false;
        }

        MenuItems = new MenuBarItem ();
    }

    private Toplevel _container;
    private Key _key = DefaultKey;
    private MouseFlags _mouseFlags = MouseFlags.Button3Clicked;

    /// <summary>The default shortcut key for activating the context menu.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key DefaultKey { get; set; } = Key.F10.WithShift;

    /// <summary>
    ///     Sets or gets whether the context menu be forced to the right, ensuring it is not clipped, if the x position is
    ///     less than zero. The default is <see langword="true"/> which means the context menu will be forced to the right. If
    ///     set to <see langword="false"/>, the context menu will be clipped on the left if x is less than zero.
    /// </summary>
    public bool ForceMinimumPosToZero { get; set; } = true;

    /// <summary>The host <see cref="View "/> which position will be used, otherwise if it's null the container will be used.</summary>
    public View Host { get; set; }

    /// <summary>Gets whether the ContextMenu is showing or not.</summary>
    public static bool IsShow { get; private set; }

    /// <summary>Specifies the key that will activate the context menu.</summary>
    public Key Key
    {
        get => _key;
        set
        {
            Key oldKey = _key;
            _key = value;
            KeyChanged?.Invoke (this, new KeyChangedEventArgs (oldKey, _key));
        }
    }

    /// <summary>Gets the <see cref="MenuBar"/> that is hosting this context menu.</summary>
    public MenuBar MenuBar => _menuBar;

    /// <summary>Gets or sets the menu items for this context menu.</summary>
    public MenuBarItem MenuItems { get; set; }

    /// <summary><see cref="Gui.MouseFlags"/> specifies the mouse action used to activate the context menu by mouse.</summary>
    public MouseFlags MouseFlags
    {
        get => _mouseFlags;
        set
        {
            MouseFlags oldFlags = _mouseFlags;
            _mouseFlags = value;
            MouseFlagsChanged?.Invoke (this, new MouseFlagsChangedEventArgs (oldFlags, value));
        }
    }

    /// <summary>Gets or sets the menu position.</summary>
    public Point Position { get; set; }

    /// <summary>
    ///     Gets or sets if sub-menus will be displayed using a "single frame" menu style. If <see langword="true"/>, the
    ///     ContextMenu and any sub-menus that would normally cascade will be displayed within a single frame. If
    ///     <see langword="false"/> (the default), sub-menus will cascade using separate frames for each level of the menu
    ///     hierarchy.
    /// </summary>
    public bool UseSubMenusSingleFrame { get; set; }

    /// <summary>Disposes the context menu object.</summary>
    public void Dispose ()
    {
        if (IsShow)
        {
            _menuBar.MenuAllClosed -= MenuBar_MenuAllClosed;
            _menuBar.Dispose ();
            _menuBar = null;
            IsShow = false;
        }

        if (_container != null)
        {
            _container.Closing -= Container_Closing;
        }
    }

    /// <summary>Hides (closes) the ContextMenu.</summary>
    public void Hide ()
    {
        _menuBar?.CleanUp ();
        Dispose ();
    }

    /// <summary>Event invoked when the <see cref="ContextMenu.Key"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs> KeyChanged;

    /// <summary>Event invoked when the <see cref="ContextMenu.MouseFlags"/> is changed.</summary>
    public event EventHandler<MouseFlagsChangedEventArgs> MouseFlagsChanged;

    /// <summary>Shows (opens) the ContextMenu, displaying the <see cref="MenuItem"/>s it contains.</summary>
    public void Show ()
    {
        if (_menuBar != null)
        {
            Hide ();
        }

        _container = Application.Current;
        _container.Closing += Container_Closing;
        Rect frame = Application.Driver.Bounds;
        Point position = Position;

        if (Host != null)
        {
            Host.BoundsToScreen (frame.X, frame.Y, out int x, out int y);
            var pos = new Point (x, y);
            pos.Y += Host.Frame.Height - 1;

            if (position != pos)
            {
                Position = position = pos;
            }
        }

        Rect rect = Menu.MakeFrame (position.X, position.Y, MenuItems.Children);

        if (rect.Right >= frame.Right)
        {
            if ((frame.Right - rect.Width >= 0) || !ForceMinimumPosToZero)
            {
                position.X = frame.Right - rect.Width;
            }
            else if (ForceMinimumPosToZero)
            {
                position.X = 0;
            }
        }
        else if (ForceMinimumPosToZero && position.X < 0)
        {
            position.X = 0;
        }

        if (rect.Bottom >= frame.Bottom)
        {
            if ((frame.Bottom - rect.Height - 1 >= 0) || !ForceMinimumPosToZero)
            {
                if (Host == null)
                {
                    position.Y = frame.Bottom - rect.Height - 1;
                }
                else
                {
                    Host.BoundsToScreen (frame.X, frame.Y, out int x, out int y);
                    var pos = new Point (x, y);
                    position.Y = pos.Y - rect.Height - 1;
                }
            }
            else if (ForceMinimumPosToZero)
            {
                position.Y = 0;
            }
        }
        else if (ForceMinimumPosToZero && position.Y < 0)
        {
            position.Y = 0;
        }

        _menuBar = new MenuBar
        {
            X = position.X,
            Y = position.Y,
            Width = 0,
            Height = 0,
            UseSubMenusSingleFrame = UseSubMenusSingleFrame,
            Key = Key,
            Menus = [MenuItems]
        };

        _menuBar._isContextMenuLoading = true;
        _menuBar.MenuAllClosed += MenuBar_MenuAllClosed;
        _menuBar.BeginInit ();
        _menuBar.EndInit ();
        IsShow = true;
        _menuBar.OpenMenu ();
    }

    private void Container_Closing (object sender, ToplevelClosingEventArgs obj) { Hide (); }
    private void MenuBar_MenuAllClosed (object sender, EventArgs e) { Dispose (); }
}
