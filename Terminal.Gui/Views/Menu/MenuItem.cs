namespace Terminal.Gui;

/// <summary>
///     A <see cref="MenuItem"/> has title, an associated help text, and an action to execute on activation. MenuItems
///     can also have a checked indicator (see <see cref="Checked"/>).
/// </summary>
public class MenuItem
{
    private readonly ShortcutHelper _shortcutHelper;
    private bool _allowNullChecked;
    private MenuItemCheckStyle _checkType;

    private string _title;

    // TODO: Update to use Key instead of KeyCode
    /// <summary>Initializes a new instance of <see cref="MenuItem"/></summary>
    public MenuItem (KeyCode shortcut = KeyCode.Null) : this ("", "", null, null, null, shortcut) { }

    // TODO: Update to use Key instead of KeyCode
    /// <summary>Initializes a new instance of <see cref="MenuItem"/>.</summary>
    /// <param name="title">Title for the menu item.</param>
    /// <param name="help">Help text to display.</param>
    /// <param name="action">Action to invoke when the menu item is activated.</param>
    /// <param name="canExecute">Function to determine if the action can currently be executed.</param>
    /// <param name="parent">The <see cref="Parent"/> of this menu item.</param>
    /// <param name="shortcut">The <see cref="Shortcut"/> keystroke combination.</param>
    public MenuItem (
        string title,
        string help,
        Action action,
        Func<bool> canExecute = null,
        MenuItem parent = null,
        KeyCode shortcut = KeyCode.Null
    )
    {
        Title = title ?? "";
        Help = help ?? "";
        Action = action;
        CanExecute = canExecute;
        Parent = parent;
        _shortcutHelper = new ();

        if (shortcut != KeyCode.Null)
        {
            Shortcut = shortcut;
        }
    }

    /// <summary>Gets or sets the action to be invoked when the menu item is triggered.</summary>
    /// <value>Method to invoke.</value>
    public Action Action { get; set; }

    /// <summary>
    ///     Used only if <see cref="CheckType"/> is of <see cref="MenuItemCheckStyle.Checked"/> type. If
    ///     <see langword="true"/> allows <see cref="Checked"/> to be null, true or false. If <see langword="false"/> only
    ///     allows <see cref="Checked"/> to be true or false.
    /// </summary>
    public bool AllowNullChecked
    {
        get => _allowNullChecked;
        set
        {
            _allowNullChecked = value;
            Checked ??= false;
        }
    }

    /// <summary>
    ///     Gets or sets the action to be invoked to determine if the menu can be triggered. If <see cref="CanExecute"/>
    ///     returns <see langword="true"/> the menu item will be enabled. Otherwise, it will be disabled.
    /// </summary>
    /// <value>Function to determine if the action is can be executed or not.</value>
    public Func<bool> CanExecute { get; set; }

    /// <summary>
    ///     Sets or gets whether the <see cref="MenuItem"/> shows a check indicator or not. See
    ///     <see cref="MenuItemCheckStyle"/>.
    /// </summary>
    public bool? Checked { set; get; }

    /// <summary>
    ///     Sets or gets the <see cref="MenuItemCheckStyle"/> of a menu item where <see cref="Checked"/> is set to
    ///     <see langword="true"/>.
    /// </summary>
    public MenuItemCheckStyle CheckType
    {
        get => _checkType;
        set
        {
            _checkType = value;

            if (_checkType == MenuItemCheckStyle.Checked && !_allowNullChecked && Checked is null)
            {
                Checked = false;
            }
        }
    }

    /// <summary>Gets or sets arbitrary data for the menu item.</summary>
    /// <remarks>This property is not used internally.</remarks>
    public object Data { get; set; }

    /// <summary>Gets or sets the help text for the menu item. The help text is drawn to the right of the <see cref="Title"/>.</summary>
    /// <value>The help text.</value>
    public string Help { get; set; }

    /// <summary>Gets the parent for this <see cref="MenuItem"/>.</summary>
    /// <value>The parent.</value>
    public MenuItem Parent { get; set; }

    /// <summary>Gets or sets the title of the menu item .</summary>
    /// <value>The title.</value>
    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
            {
                return;
            }

            _title = value;
            GetHotKey ();
        }
    }

    /// <summary>Gets if this <see cref="MenuItem"/> is from a sub-menu.</summary>
    internal bool IsFromSubMenu => Parent != null;

    internal int TitleLength => GetMenuBarItemLength (Title);

    // 
    // ┌─────────────────────────────┐
    // │ Quit  Quit UI Catalog  Ctrl+Q │
    // └─────────────────────────────┘
    // ┌─────────────────┐
    // │ ◌ TopLevel Alt+T │
    // └─────────────────┘
    // TODO: Replace the `2` literals with named constants 
    internal int Width => 1
                          + // space before Title
                          TitleLength
                          + 2
                          + // space after Title - BUGBUG: This should be 1 
                          (Checked == true || CheckType.HasFlag (MenuItemCheckStyle.Checked) || CheckType.HasFlag (MenuItemCheckStyle.Radio)
                               ? 2
                               : 0)
                          + // check glyph + space 
                          (Help.GetColumns () > 0 ? 2 + Help.GetColumns () : 0)
                          + // Two spaces before Help
                          (ShortcutTag.GetColumns () > 0
                               ? 2 + ShortcutTag.GetColumns ()
                               : 0); // Pad two spaces before shortcut tag (which are also aligned right)

    /// <summary>Merely a debugging aid to see the interaction with main.</summary>
    internal bool GetMenuBarItem () { return IsFromSubMenu; }

    /// <summary>Merely a debugging aid to see the interaction with main.</summary>
    internal MenuItem GetMenuItem () { return this; }

    /// <summary>
    ///     Returns <see langword="true"/> if the menu item is enabled. This method is a wrapper around
    ///     <see cref="CanExecute"/>.
    /// </summary>
    public bool IsEnabled () { return CanExecute?.Invoke () ?? true; }

    /// <summary>
    ///     Toggle the <see cref="Checked"/> between three states if <see cref="AllowNullChecked"/> is
    ///     <see langword="true"/> or between two states if <see cref="AllowNullChecked"/> is <see langword="false"/>.
    /// </summary>
    public void ToggleChecked ()
    {
        if (_checkType != MenuItemCheckStyle.Checked)
        {
            throw new InvalidOperationException ("This isn't a Checked MenuItemCheckStyle!");
        }

        bool? previousChecked = Checked;

        if (AllowNullChecked)
        {
            Checked = previousChecked switch
                      {
                          null => true,
                          true => false,
                          false => null
                      };
        }
        else
        {
            Checked = !Checked;
        }
    }

    private static int GetMenuBarItemLength (string title)
    {
        return title.EnumerateRunes ()
                    .Where (ch => ch != MenuBar.HotKeySpecifier)
                    .Sum (ch => Math.Max (ch.GetColumns (), 1));
    }

    #region Keyboard Handling

    // TODO: Update to use Key instead of Rune
    /// <summary>
    ///     The HotKey is used to activate a <see cref="MenuItem"/> with the keyboard. HotKeys are defined by prefixing the
    ///     <see cref="Title"/> of a MenuItem with an underscore ('_').
    ///     <para>
    ///         Pressing Alt-Hotkey for a <see cref="MenuBarItem"/> (menu items on the menu bar) works even if the menu is
    ///         not active). Once a menu has focus and is active, pressing just the HotKey will activate the MenuItem.
    ///     </para>
    ///     <para>
    ///         For example for a MenuBar with a "_File" MenuBarItem that contains a "_New" MenuItem, Alt-F will open the
    ///         File menu. Pressing the N key will then activate the New MenuItem.
    ///     </para>
    ///     <para>See also <see cref="Shortcut"/> which enable global key-bindings to menu items.</para>
    /// </summary>
    public Rune HotKey { get; set; }
    private void GetHotKey ()
    {
        var nextIsHot = false;

        foreach (char x in _title)
        {
            if (x == MenuBar.HotKeySpecifier.Value)
            {
                nextIsHot = true;
            }
            else
            {
                if (nextIsHot)
                {
                    HotKey = (Rune)char.ToUpper (x);

                    break;
                }

                nextIsHot = false;
                HotKey = default (Rune);
            }
        }
    }

    // TODO: Update to use Key instead of KeyCode
    /// <summary>
    ///     Shortcut defines a key binding to the MenuItem that will invoke the MenuItem's action globally for the
    ///     <see cref="View"/> that is the parent of the <see cref="MenuBar"/> or <see cref="ContextMenu"/> this
    ///     <see cref="MenuItem"/>.
    ///     <para>
    ///         The <see cref="KeyCode"/> will be drawn on the MenuItem to the right of the <see cref="Title"/> and
    ///         <see cref="Help"/> text. See <see cref="ShortcutTag"/>.
    ///     </para>
    /// </summary>
    public KeyCode Shortcut
    {
        get => _shortcutHelper.Shortcut;
        set
        {
            if (_shortcutHelper.Shortcut != value && (ShortcutHelper.PostShortcutValidation (value) || value == KeyCode.Null))
            {
                _shortcutHelper.Shortcut = value;
            }
        }
    }

    /// <summary>Gets the text describing the keystroke combination defined by <see cref="Shortcut"/>.</summary>
    public string ShortcutTag => _shortcutHelper.Shortcut == KeyCode.Null
                                     ? string.Empty
                                     : Key.ToString (_shortcutHelper.Shortcut, MenuBar.ShortcutDelimiter);

    #endregion Keyboard Handling
}
