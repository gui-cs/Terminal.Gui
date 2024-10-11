#nullable enable

using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     ContextMenuv2 provides a Popover menu that can be positioned anywhere within a <see cref="View"/>.
///     <para>
///         To show the ContextMenu, set <see cref="Application.Popover"/> to the ContextMenu object and set
///         <see cref="View.Visible"/> property to <see langword="true"/>.
///     </para>
///     <para>
///         The menu will be hidden when the user clicks outside the menu or when the user presses <see cref="Application.QuitKey"/>.
///     </para>
///     <para>
///         To explicitly hide the menu, set <see cref="View.Visible"/> property to <see langword="false"/>.
///     </para>
///     <para>
///         <see cref="Key"/> is the key used to activate the ContextMenus (<c>Shift+F10</c> by default). Callers can use this in
///         their keyboard handling code.
///     </para>
///     <para>The menu will be displayed at the current mouse coordinates.</para>
/// </summary>
public class ContextMenuv2 : Menuv2
{
    private Key _key = DefaultKey;

    /// <summary>
    ///     The mouse flags that will trigger the context menu. The default is <see cref="MouseFlags.Button3Clicked"/> which is typically the right mouse button.
    /// </summary>
    public MouseFlags MouseFlags { get; set; } = MouseFlags.Button3Clicked;

    /// <summary>Initializes a context menu with no menu items.</summary>
    public ContextMenuv2 () : this ([]) { }

    /// <inheritdoc/>
    public ContextMenuv2 (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        Visible = false;
        VisibleChanged += OnVisibleChanged;
        Key = DefaultKey;
        AddCommand (Command.Context,
                    () =>
                    {
                        if (!Enabled)
                        {
                            return false;
                        }
                        Application.Popover = this;
                        SetPosition (Application.GetLastMousePosition ());
                        Visible = !Visible;

                        return true;
                    });
        return;

    }

    private void OnVisibleChanged (object? sender, EventArgs _)
    {
        if (Visible && Subviews.Count > 0)
        {
            Subviews [0].SetFocus ();
        }
    }

    /// <summary>The default key for activating the context menu.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key DefaultKey { get; set; } = Key.F10.WithShift;

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

    /// <summary>
    ///     Sets the position of the ContextMenu. The actual position of the menu will be adjusted to
    ///     ensure the menu fully fits on the screen, and the mouse cursor is over the first call of the
    ///     first Shortcut.
    /// </summary>
    /// <param name="screenPosition"></param>
    public void SetPosition (Point? screenPosition)
    {
        if (screenPosition is { })
        {
            Frame = Frame with
            {
                X = screenPosition.Value.X - GetViewportOffsetFromFrame ().X,
                Y = screenPosition.Value.Y - GetViewportOffsetFromFrame ().Y,
            };
        }
    }

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            Application.KeyBindings.Remove (Key, this);
        }
        base.Dispose (disposing);
    }
}
