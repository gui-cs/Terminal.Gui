#nullable enable

namespace Terminal.Gui;

/// <summary>
///     <see cref="MenuBarItem"/> is a menu item on  <see cref="MenuBar"/>. MenuBarItems do not support
///     <see cref="MenuItem.ShortcutKey"/>.
/// </summary>
public class MenuBarItem : MenuItem
{
    /// <summary>Initializes a new <see cref="MenuBarItem"/> as a <see cref="MenuItem"/>.</summary>
    /// <param name="title">Title for the menu item.</param>
    /// <param name="help">Help text to display. Will be displayed next to the Title surrounded by parentheses.</param>
    /// <param name="action">Action to invoke when the menu item is activated.</param>
    /// <param name="canExecute">Function to determine if the action can currently be executed.</param>
    /// <param name="parent">The parent <see cref="MenuItem"/> of this if any.</param>
    public MenuBarItem (
        string title,
        string help,
        Action action,
        Func<bool>? canExecute = null,
        MenuItem? parent = null
    ) : base (title, help, action, canExecute, parent)
    {
        SetInitialProperties (title, null, null, true);
    }

    /// <summary>Initializes a new <see cref="MenuBarItem"/>.</summary>
    /// <param name="title">Title for the menu item.</param>
    /// <param name="children">The items in the current menu.</param>
    /// <param name="parent">The parent <see cref="MenuItem"/> of this if any.</param>
    public MenuBarItem (string title, MenuItem [] children, MenuItem? parent = null) { SetInitialProperties (title, children, parent); }

    /// <summary>Initializes a new <see cref="MenuBarItem"/> with separate list of items.</summary>
    /// <param name="title">Title for the menu item.</param>
    /// <param name="children">The list of items in the current menu.</param>
    /// <param name="parent">The parent <see cref="MenuItem"/> of this if any.</param>
    public MenuBarItem (string title, List<MenuItem []> children, MenuItem? parent = null) { SetInitialProperties (title, children, parent); }

    /// <summary>Initializes a new <see cref="MenuBarItem"/>.</summary>
    /// <param name="children">The items in the current menu.</param>
    public MenuBarItem (MenuItem [] children) : this ("", children) { }

    /// <summary>Initializes a new <see cref="MenuBarItem"/>.</summary>
    public MenuBarItem () : this ([]) { }

    /// <summary>
    ///     Gets or sets an array of <see cref="MenuItem"/> objects that are the children of this
    ///     <see cref="MenuBarItem"/>
    /// </summary>
    /// <value>The children.</value>
    public MenuItem? []? Children { get; set; }

    internal bool IsTopLevel => Parent is null && (Children is null || Children.Length == 0) && Action != null;

    /// <summary>Get the index of a child <see cref="MenuItem"/>.</summary>
    /// <param name="children"></param>
    /// <returns>Returns a greater than -1 if the <see cref="MenuItem"/> is a child.</returns>
    public int GetChildrenIndex (MenuItem children)
    {
        var i = 0;

        if (Children is null)
        {
            return -1;
        }

        foreach (MenuItem? child in Children)
        {
            if (child == children)
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    /// <summary>Check if a <see cref="MenuItem"/> is a submenu of this MenuBar.</summary>
    /// <param name="menuItem"></param>
    /// <returns>Returns <c>true</c> if it is a submenu. <c>false</c> otherwise.</returns>
    public bool IsSubMenuOf (MenuItem menuItem)
    {
        return Children!.Any (child => child == menuItem && child.Parent == menuItem.Parent);
    }

    /// <summary>Check if a <see cref="MenuItem"/> is a <see cref="MenuBarItem"/>.</summary>
    /// <param name="menuItem"></param>
    /// <returns>Returns a <see cref="MenuBarItem"/> or null otherwise.</returns>
    public MenuBarItem? SubMenu (MenuItem? menuItem) { return menuItem as MenuBarItem; }

    internal void AddShortcutKeyBindings (MenuBar menuBar)
    {
        if (Children is null)
        {
            return;
        }

        _menuBar = menuBar;

        IEnumerable<MenuItem> menuItems = Children.Where (m => m is { })!;

        foreach (MenuItem menuItem in menuItems)
        {
            // Initialize MenuItem _menuBar
            menuItem._menuBar = menuBar;

            // For MenuBar only add shortcuts for submenus
            if (menuItem.ShortcutKey != Key.Empty)
            {
                menuItem.AddShortcutKeyBinding (menuBar, Key.Empty);
            }

            SubMenu (menuItem)?.AddShortcutKeyBindings (menuBar);
        }
    }

    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    private void SetInitialProperties (string title, object? children, MenuItem? parent = null, bool isTopLevel = false)
    {
        if (!isTopLevel && children is null)
        {
            throw new ArgumentNullException (
                                             nameof (children),
                                             @"The parameter cannot be null. Use an empty array instead."
                                            );
        }

        SetTitle (title);

        if (parent is { })
        {
            Parent = parent;
        }

        switch (children)
        {
            case List<MenuItem []> childrenList:
            {
                MenuItem [] newChildren = [];

                foreach (MenuItem [] grandChild in childrenList)
                {
                    foreach (MenuItem child in grandChild)
                    {
                        SetParent (grandChild);
                        Array.Resize (ref newChildren, newChildren.Length + 1);
                        newChildren [^1] = child;
                    }
                }

                Children = newChildren;

                break;
            }
            case MenuItem [] items:
                SetParent (items);
                Children = items;

                break;
            default:
                Children = null;

                break;
        }
    }

    private void SetParent (MenuItem [] children)
    {
        foreach (MenuItem child in children)
        {
            if (child is { Parent: null })
            {
                child.Parent = this;
            }
        }
    }

    private void SetTitle (string? title)
    {
        title ??= string.Empty;
        Title = title;
    }

    /// <summary>
    /// Add a <see cref="MenuBarItem"/> dynamically into the <see cref="MenuBar"/><c>.Menus</c>.
    /// </summary>
    /// <param name="menuBar"></param>
    /// <param name="menuItem"></param>
    public void AddMenuBarItem (MenuBar menuBar, MenuItem? menuItem = null)
    {
        ArgumentNullException.ThrowIfNull (menuBar);

        _menuBar = menuBar;

        if (menuItem is null)
        {
            MenuBarItem [] menus = _menuBar.Menus;
            Array.Resize (ref menus, menus.Length + 1);
            menus [^1] = this;
            _menuBar.Menus = menus;
        }
        else
        {
            MenuItem [] childrens = (Children ?? [])!;
            Array.Resize (ref childrens, childrens.Length + 1);
            menuItem._menuBar = menuBar;
            childrens [^1] = menuItem;
            Children = childrens;
        }
    }

    /// <inheritdoc />
    public override void RemoveMenuItem ()
    {
        if (Children is { })
        {
            foreach (MenuItem? menuItem in Children)
            {
                if (menuItem?.ShortcutKey != Key.Empty)
                {
                    // Remove an existent ShortcutKey
                    _menuBar?.HotKeyBindings.Remove (menuItem?.ShortcutKey!);
                }
            }
        }

        if (ShortcutKey != Key.Empty)
        {
            // Remove an existent ShortcutKey
            _menuBar?.HotKeyBindings.Remove (ShortcutKey!);
        }

        var index = _menuBar!.Menus.IndexOf (this);
        if (index > -1)
        {
            if (_menuBar.Menus [index].HotKey != Key.Empty)
            {
                // Remove an existent HotKey
                _menuBar.HotKeyBindings.Remove (HotKey!.WithAlt);
            }

            _menuBar.Menus [index] = null!;
        }

        var i = 0;

        foreach (MenuBarItem m in _menuBar.Menus)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (m != null)
            {
                _menuBar.Menus [i] = m;
                i++;
            }
        }

        MenuBarItem [] menus = _menuBar.Menus;
        Array.Resize (ref menus, menus.Length - 1);
        _menuBar.Menus = menus;
    }
}
