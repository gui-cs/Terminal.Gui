namespace Terminal.Gui;

/// <summary>
///     <see cref="StatusItem"/> objects are contained by <see cref="StatusBar"/> <see cref="View"/>s. Each
///     <see cref="StatusItem"/> has a title, a shortcut (hotkey), and an <see cref="Command"/> that will be invoked when
///     the <see cref="StatusItem.Shortcut"/> is pressed. The <see cref="StatusItem.Shortcut"/> will be a global hotkey for
///     the application in the current context of the screen. The color of the <see cref="StatusItem.Title"/> will be
///     changed after each ~. A <see cref="StatusItem.Title"/> set to `~F1~ Help` will render as *F1* using
///     <see cref="ColorScheme.HotNormal"/> and *Help* as <see cref="ColorScheme.HotNormal"/>.
/// </summary>
public class StatusItem {
    /// <summary>Initializes a new <see cref="StatusItem"/>.</summary>
    /// <param name="shortcut">Shortcut to activate the <see cref="StatusItem"/>.</param>
    /// <param name="title">Title for the <see cref="StatusItem"/>.</param>
    /// <param name="action">Action to invoke when the <see cref="StatusItem"/> is activated.</param>
    /// <param name="canExecute">Function to determine if the action can currently be executed.</param>
    public StatusItem (Key shortcut, string title, Action action, Func<bool> canExecute = null) {
        Title = title ?? "";
        Shortcut = shortcut;
        Action = action;
        CanExecute = canExecute;
    }

    /// <summary>Gets or sets the action to be invoked when the statusbar item is triggered</summary>
    /// <value>Action to invoke.</value>
    public Action Action { get; set; }

    /// <summary>
    ///     Gets or sets the action to be invoked to determine if the <see cref="StatusItem"/> can be triggered. If
    ///     <see cref="CanExecute"/> returns <see langword="true"/> the status item will be enabled. Otherwise, it will be
    ///     disabled.
    /// </summary>
    /// <value>Function to determine if the action is can be executed or not.</value>
    public Func<bool> CanExecute { get; set; }

    /// <summary>Gets the global shortcut to invoke the action on the menu.</summary>
    public Key Shortcut { get; set; }

    /// <summary>Gets or sets arbitrary data for the status item.</summary>
    /// <remarks>This property is not used internally.</remarks>
    public object Data { get; set; }

    /// <summary>Gets or sets the title.</summary>
    /// <value>The title.</value>
    /// <remarks>
    ///     The colour of the <see cref="StatusItem.Title"/> will be changed after each ~. A <see cref="StatusItem.Title"/> set
    ///     to `~F1~ Help` will render as *F1* using <see cref="ColorScheme.HotNormal"/> and *Help* as
    ///     <see cref="ColorScheme.HotNormal"/>.
    /// </remarks>
    public string Title { get; set; }

    /// <summary>
    ///     Returns <see langword="true"/> if the status item is enabled. This method is a wrapper around
    ///     <see cref="CanExecute"/>.
    /// </summary>
    public bool IsEnabled () => CanExecute?.Invoke () ?? true;
}

/// <summary>
///     A status bar is a <see cref="View"/> that snaps to the bottom of a <see cref="Toplevel"/> displaying set of
///     <see cref="StatusItem"/>s. The <see cref="StatusBar"/> should be context sensitive. This means, if the main menu
///     and an open text editor are visible, the items probably shown will be ~F1~ Help ~F2~ Save ~F3~ Load. While a dialog
///     to ask a file to load is executed, the remaining commands will probably be ~F1~ Help. So for each context must be a
///     new instance of a status bar.
/// </summary>
public class StatusBar : View {
    /// <summary>Initializes a new instance of the <see cref="StatusBar"/> class.</summary>
    public StatusBar () : this (new StatusItem[] { }) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="StatusBar"/> class with the specified set of <see cref="StatusItem"/>
    ///     s. The <see cref="StatusBar"/> will be drawn on the lowest line of the terminal or <see cref="View.SuperView"/> (if
    ///     not null).
    /// </summary>
    /// <param name="items">A list of status bar items.</param>
    public StatusBar (StatusItem[] items) {
        if (items != null) {
            Items = items;
        }

        CanFocus = false;
        ColorScheme = Colors.ColorSchemes["Menu"];
        X = 0;
        Y = Pos.AnchorEnd (1);
        Width = Dim.Fill ();
        Height = 1;
        AddCommand (Command.Accept, InvokeItem);
    }

    private static Rune _shortcutDelimiter = (Rune)'=';
    private StatusItem _itemToInvoke;
    private StatusItem[] _items = { };

    /// <summary>Gets or sets shortcut delimiter separator. The default is "-".</summary>
    public static Rune ShortcutDelimiter {
        get => _shortcutDelimiter;
        set {
            if (_shortcutDelimiter != value) {
                _shortcutDelimiter = value == default (Rune) ? (Rune)'=' : value;
            }
        }
    }

    /// <summary>The items that compose the <see cref="StatusBar"/></summary>
    public StatusItem[] Items {
        get => _items;
        set {
            foreach (StatusItem item in _items) {
                KeyBindings.Remove ((KeyCode)item.Shortcut);
            }

            _items = value;
            foreach (StatusItem item in _items) {
                KeyBindings.Add ((KeyCode)item.Shortcut, KeyBindingScope.HotKey, Command.Accept);
            }
        }
    }

    /// <summary>Inserts a <see cref="StatusItem"/> in the specified index of <see cref="Items"/>.</summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void AddItemAt (int index, StatusItem item) {
        List<StatusItem> itemsList = new (Items);
        itemsList.Insert (index, item);
        Items = itemsList.ToArray ();
        SetNeedsDisplay ();
    }

    ///<inheritdoc/>
    public override bool MouseEvent (MouseEvent me) {
        if (me.Flags != MouseFlags.Button1Clicked) {
            return false;
        }

        var pos = 1;
        for (var i = 0; i < Items.Length; i++) {
            if (me.X >= pos && me.X < pos + GetItemTitleLength (Items[i].Title)) {
                StatusItem item = Items[i];
                if (item.IsEnabled ()) {
                    Run (item.Action);
                }

                break;
            }

            pos += GetItemTitleLength (Items[i].Title) + 3;
        }

        return true;
    }

    ///<inheritdoc/>
    public override void OnDrawContent (Rect contentArea) {
        Move (0, 0);
        Driver.SetAttribute (GetNormalColor ());
        for (var i = 0; i < Frame.Width; i++) {
            Driver.AddRune ((Rune)' ');
        }

        Move (1, 0);
        Attribute scheme = GetNormalColor ();
        Driver.SetAttribute (scheme);
        for (var i = 0; i < Items.Length; i++) {
            string title = Items[i].Title;
            Driver.SetAttribute (DetermineColorSchemeFor (Items[i]));
            for (var n = 0; n < Items[i].Title.GetRuneCount (); n++) {
                if (title[n] == '~') {
                    if (Items[i].IsEnabled ()) {
                        scheme = ToggleScheme (scheme);
                    }

                    continue;
                }

                Driver.AddRune ((Rune)title[n]);
            }

            if (i + 1 < Items.Length) {
                Driver.AddRune ((Rune)' ');
                Driver.AddRune (Glyphs.VLine);
                Driver.AddRune ((Rune)' ');
            }
        }
    }

    ///<inheritdoc/>
    public override bool OnEnter (View view) {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnEnter (view);
    }

    /// <inheritdoc/>
    public override bool? OnInvokingKeyBindings (Key keyEvent) {
        // This is a bit of a hack. We want to handle the key bindings for status bar but
        // InvokeKeyBindings doesn't pass any context so we can't tell which item it is for.
        // So before we call the base class we set SelectedItem appropriately.
        KeyCode key = keyEvent.KeyCode;
        if (KeyBindings.TryGet (key, out _)) {
            // Search RadioLabels 
            foreach (StatusItem item in Items) {
                if (item.Shortcut == key) {
                    _itemToInvoke = item;
                    keyEvent.Scope = KeyBindingScope.HotKey;

                    break;
                }
            }
        }

        return base.OnInvokingKeyBindings (keyEvent);
    }

    /// <summary>Removes a <see cref="StatusItem"/> at specified index of <see cref="Items"/>.</summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <returns>The <see cref="StatusItem"/> removed.</returns>
    public StatusItem RemoveItem (int index) {
        List<StatusItem> itemsList = new (Items);
        StatusItem item = itemsList[index];
        itemsList.RemoveAt (index);
        Items = itemsList.ToArray ();
        SetNeedsDisplay ();

        return item;
    }

    private Attribute DetermineColorSchemeFor (StatusItem item) {
        if (item != null) {
            if (item.IsEnabled ()) {
                return GetNormalColor ();
            }

            return ColorScheme.Disabled;
        }

        return GetNormalColor ();
    }

    private int GetItemTitleLength (string title) {
        var len = 0;
        foreach (char ch in title) {
            if (ch == '~') {
                continue;
            }

            len++;
        }

        return len;
    }

    private bool? InvokeItem () {
        if (_itemToInvoke is { Action: { } }) {
            _itemToInvoke.Action.Invoke ();

            return true;
        }

        return false;
    }

    private void Run (Action action) {
        if (action == null) {
            return;
        }

        Application.MainLoop.AddIdle (
            () => {
                action ();

                return false;
            }
        );
    }

    private Attribute ToggleScheme (Attribute scheme) {
        Attribute result = scheme == ColorScheme.Normal ? ColorScheme.HotNormal : ColorScheme.Normal;
        Driver.SetAttribute (result);

        return result;
    }
}
