#nullable enable

namespace Terminal.Gui;

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
///         <see cref="Show"/>.
///     </para>
///     <para>ContextMenus are located using screen coordinates and appear above all other Views.</para>
/// </summary>
public class ContextMenuv2 : Menuv2
{
    private Key _key = DefaultKey;

    /// <summary>Initializes a context menu with no menu items.</summary>
    public ContextMenuv2 ()
    {
        VisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged (object? sender, EventArgs e)
    {
        if (Visible)
        {

        }
        else
        {
            if (Application.MouseGrabView == this)
            {
                Application.UngrabMouse ();
            }
        }
    }

    /// <summary>The default key for activating the context menu.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key DefaultKey { get; set; } = Key.F10.WithShift;

    /// <summary>
    ///     Sets or gets whether the context menu be forced to the right, ensuring it is not clipped, if the x position is
    ///     less than zero. The default is <see langword="true"/> which means the context menu will be forced to the right. If
    ///     set to <see langword="false"/>, the context menu will be clipped on the left if x is less than zero.
    /// </summary>
    public bool ForceMinimumPosToZero { get; set; } = true;

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

    /// <summary>Event raised when the <see cref="ContextMenu.Key"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs>? KeyChanged;
}
