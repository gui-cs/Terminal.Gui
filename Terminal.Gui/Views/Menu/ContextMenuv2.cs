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
public class ContextMenuv2 : PopoverMenu, IDesignable
{

    /// <summary>
    ///     The mouse flags that will trigger the context menu. The default is <see cref="MouseFlags.Button3Clicked"/> which is typically the right mouse button.
    /// </summary>
    public MouseFlags MouseFlags { get; set; } = MouseFlags.Button3Clicked;

    /// <summary>Initializes a context menu with no menu items.</summary>
    public ContextMenuv2 () : this ([]) { }

    /// <inheritdoc/>
    public ContextMenuv2 (Menuv2? menu) : base (menu)
    {
        Key = DefaultKey;
    }

    /// <inheritdoc/>
    public ContextMenuv2 (IEnumerable<View>? menuItems) : this (new Menuv2 (menuItems))
    {
    }

    private Key _key = DefaultKey;

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

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        var shortcut = new Shortcut
        {
            Text = "Quit",
            Title = "Q_uit",
            Key = Key.Z.WithCtrl,
        };

        Add (shortcut);

        shortcut = new Shortcut
        {
            Text = "Help Text",
            Title = "Help",
            Key = Key.F1,
        };

        Add (shortcut);

        shortcut = new Shortcut
        {
            Text = "Czech",
            CommandView = new CheckBox ()
            {
                Title = "_Check"
            },
            Key = Key.F9,
            CanFocus = false
        };

        Add (shortcut);

        // HACK: This enables All Views Tester to show the CM if DefaultKey is pressed
        AddCommand (Command.Context, () => Visible = true);
        HotKeyBindings.Add (DefaultKey, Command.Context);

        return true;
    }
}
