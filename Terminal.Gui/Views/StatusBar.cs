namespace Terminal.Gui;

/// <summary>
///     A status bar is a <see cref="View"/> that snaps to the bottom of a <see cref="Toplevel"/> displaying set of
///     <see cref="StatusItem"/>s. The <see cref="StatusBar"/> should be context sensitive. This means, if the main menu
///     and an open text editor are visible, the items probably shown will be ~F1~ Help ~F2~ Save ~F3~ Load. While a dialog
///     to ask a file to load is executed, the remaining commands will probably be ~F1~ Help. So for each context must be a
///     new instance of a status bar.
/// </summary>
public class StatusBar : View
{
    private static Rune _shortcutDelimiter = (Rune)'=';

    private StatusItem [] _items = [];

    /// <summary>Initializes a new instance of the <see cref="StatusBar"/> class.</summary>
    public StatusBar () : this (new StatusItem [] { }) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="StatusBar"/> class with the specified set of
    ///     <see cref="StatusItem"/> s. The <see cref="StatusBar"/> will be drawn on the lowest line of the terminal or
    ///     <see cref="View.SuperView"/> (if not null).
    /// </summary>
    /// <param name="items">A list of status bar items.</param>
    public StatusBar (StatusItem [] items)
    {
        if (items is { })
        {
            Items = items;
        }

        CanFocus = false;
        ColorScheme = Colors.ColorSchemes ["Menu"];
        X = 0;
        Y = Pos.AnchorEnd ();
        Width = Dim.Fill ();
        Height = 1; // BUGBUG: Views should avoid setting Height as doing so implies Frame.Size == GetContentSize ().

        AddCommand (Command.Accept, ctx => InvokeItem ((StatusItem)ctx.KeyBinding?.Context));
    }

    /// <summary>The items that compose the <see cref="StatusBar"/></summary>
    public StatusItem [] Items
    {
        get => _items;
        set
        {
            foreach (StatusItem item in _items)
            {
                KeyBindings.Remove (item.Shortcut);
            }

            _items = value;

            foreach (StatusItem item in _items.Where (i => i.Shortcut != Key.Empty))
            {
                KeyBinding keyBinding = new (new [] { Command.Accept }, KeyBindingScope.HotKey, item);
                KeyBindings.Add (item.Shortcut, keyBinding);
            }
        }
    }

    /// <summary>Gets or sets shortcut delimiter separator. The default is "-".</summary>
    public static Rune ShortcutDelimiter
    {
        get => _shortcutDelimiter;
        set
        {
            if (_shortcutDelimiter != value)
            {
                _shortcutDelimiter = value == default (Rune) ? (Rune)'=' : value;
            }
        }
    }

    /// <summary>Inserts a <see cref="StatusItem"/> in the specified index of <see cref="Items"/>.</summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void AddItemAt (int index, StatusItem item)
    {
        List<StatusItem> itemsList = new (Items);
        itemsList.Insert (index, item);
        Items = itemsList.ToArray ();
        SetNeedsDisplay ();
    }

    ///<inheritdoc/>
    protected internal override bool OnMouseEvent (MouseEvent me)
    {
        if (me.Flags != MouseFlags.Button1Clicked)
        {
            return false;
        }

        var pos = 1;

        for (var i = 0; i < Items.Length; i++)
        {
            if (me.Position.X >= pos && me.Position.X < pos + GetItemTitleLength (Items [i].Title))
            {
                StatusItem item = Items [i];

                if (item.IsEnabled ())
                {
                    Run (item.Action);
                }

                break;
            }

            pos += GetItemTitleLength (Items [i].Title) + 3;
        }

        return true;
    }

    ///<inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        Move (0, 0);
        Driver.SetAttribute (GetNormalColor ());

        for (var i = 0; i < Frame.Width; i++)
        {
            Driver.AddRune ((Rune)' ');
        }

        Move (1, 0);
        Attribute scheme = GetNormalColor ();
        Driver.SetAttribute (scheme);

        for (var i = 0; i < Items.Length; i++)
        {
            string title = Items [i].Title;
            Driver.SetAttribute (DetermineColorSchemeFor (Items [i]));

            for (var n = 0; n < Items [i].Title.GetRuneCount (); n++)
            {
                if (title [n] == '~')
                {
                    if (Items [i].IsEnabled ())
                    {
                        scheme = ToggleScheme (scheme);
                    }

                    continue;
                }

                Driver.AddRune ((Rune)title [n]);
            }

            if (i + 1 < Items.Length)
            {
                Driver.AddRune ((Rune)' ');
                Driver.AddRune (Glyphs.VLine);
                Driver.AddRune ((Rune)' ');
            }
        }
    }

    /// <summary>Removes a <see cref="StatusItem"/> at specified index of <see cref="Items"/>.</summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <returns>The <see cref="StatusItem"/> removed.</returns>
    public StatusItem RemoveItem (int index)
    {
        List<StatusItem> itemsList = new (Items);
        StatusItem item = itemsList [index];
        itemsList.RemoveAt (index);
        Items = itemsList.ToArray ();
        SetNeedsDisplay ();

        return item;
    }

    private Attribute DetermineColorSchemeFor (StatusItem item)
    {
        if (item is { })
        {
            if (item.IsEnabled ())
            {
                return GetNormalColor ();
            }

            return ColorScheme.Disabled;
        }

        return GetNormalColor ();
    }

    private int GetItemTitleLength (string title)
    {
        var len = 0;

        foreach (char ch in title)
        {
            if (ch == '~')
            {
                continue;
            }

            len++;
        }

        return len;
    }

    private bool? InvokeItem (StatusItem itemToInvoke)
    {
        if (itemToInvoke is { Action: { } })
        {
            itemToInvoke.Action.Invoke ();

            return true;
        }

        return false;
    }

    private void Run (Action action)
    {
        if (action is null)
        {
            return;
        }

        Application.MainLoop.AddIdle (
                                      () =>
                                      {
                                          action ();

                                          return false;
                                      }
                                     );
    }

    private Attribute ToggleScheme (Attribute scheme)
    {
        Attribute result = scheme == ColorScheme.Normal ? ColorScheme.HotNormal : ColorScheme.Normal;
        Driver.SetAttribute (result);

        return result;
    }
}
