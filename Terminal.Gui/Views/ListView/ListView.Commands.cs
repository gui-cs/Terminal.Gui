namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="ListView"/>. Override via <c>config.json</c>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Dictionary<string, string []>? DefaultKeyBindings { get; set; } = new ()
    {
        { "Up", ["CursorUp", "Ctrl+P"] },
        { "Down", ["CursorDown", "Ctrl+N"] },
        { "PageUp", ["PageUp"] },
        { "PageDown", ["PageDown", "Ctrl+V"] },
        { "Start", ["Home"] },
        { "End", ["End"] },
        { "UpExtend", ["Shift+CursorUp", "Ctrl+Shift+P"] },
        { "DownExtend", ["Shift+CursorDown", "Ctrl+Shift+N"] },
        { "PageUpExtend", ["Shift+PageUp"] },
        { "PageDownExtend", ["Shift+PageDown"] },
        { "StartExtend", ["Shift+Home"] },
        { "EndExtend", ["Shift+End"] }
    };

    /// <summary>
    ///     Gets or sets the platform-override key bindings for <see cref="ListView"/> on Unix. Override via
    ///     <c>config.json</c>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Dictionary<string, string []>? DefaultKeyBindingsUnix { get; set; }

    private void SetupBindingsAndCommands ()
    {
        // Things this view knows how to do
        // 
        AddCommand (Command.Up, ctx => RaiseActivating (ctx) == true || MoveUp ());
        AddCommand (Command.Down, ctx => RaiseActivating (ctx) == true || MoveDown ());

        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.PageUp, () => MovePageUp ());
        AddCommand (Command.PageDown, () => MovePageDown ());
        AddCommand (Command.Start, () => MoveHome ());
        AddCommand (Command.End, () => MoveEnd ());
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));

        // Extend commands for multi-selection
        AddCommand (Command.UpExtend, ctx => RaiseActivating (ctx) == true || MoveUp (true));
        AddCommand (Command.DownExtend, ctx => RaiseActivating (ctx) == true || MoveDown (true));
        AddCommand (Command.PageUpExtend, () => MovePageUp (true));
        AddCommand (Command.PageDownExtend, () => MovePageDown (true));
        AddCommand (Command.StartExtend, () => MoveHome (true));
        AddCommand (Command.EndExtend, () => MoveEnd (true));

        AddCommand (Command.SelectAll, HandleSelectAll);

        // Default keybindings for all ListViews
        KeyBindingConfigHelper.Apply (this, DefaultKeyBindings, DefaultKeyBindingsUnix);

        // Key.Space is already bound to Command.Activate; this gives us activate then move down
        KeyBindings.Add (Key.Space.WithShift, Command.Activate, Command.Down);

        // Use the form of Add that lets us pass data with the binding
        KeyBindings.Add (Key.A.WithCtrl, new KeyBinding ([Command.SelectAll], true));
        KeyBindings.Add (Key.U.WithCtrl, new KeyBinding ([Command.SelectAll], false));

        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonDoubleClicked, Command.Accept);

        // Mouse click bindings for selection and marking
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate); // Normal click
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Shift, Command.Activate); // Shift+Click

        MouseBindings.ReplaceCommands (MouseFlags.RightButtonClicked | MouseFlags.Ctrl,
                                       Command.Activate); // Alternative to Shift+Click for terminals like WT that us Shift-Click for selection
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Ctrl, Command.Activate); // Ctrl+Click

        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledRight, Command.ScrollRight);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledLeft, Command.ScrollLeft);
    }

    /// <inheritdoc />
    protected override void OnHotKeyCommand (ICommandContext? ctx)
    {
        base.OnHotKeyCommand (ctx);

        SelectedItem ??= 0;
    }

    /// <inheritdoc />
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);

        // Handle keyboard (Space key) - mark item when marking is enabled
        if (ctx?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            // Allow marking if: ShowMarks=true OR MarkMultiple=true
            // Only disallow if both are false (Combination 1: standard selection mode)
            if ((ShowMarks || MarkMultiple) && SelectedItem.HasValue)
            {
                MarkUnmarkSelectedItem ();
            }

            return;
        }

        // Handle mouse clicks
        Point position = mouse.Position!.Value;
        int index = Viewport.Y + position.Y;

        if (Source is null || index >= Source.Count)
        {
            return;
        }
        bool shift = mouse.Flags.HasFlag (MouseFlags.Shift);
        bool ctrl = mouse.Flags.HasFlag (MouseFlags.Ctrl);
        bool leftButton = mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked);
        bool rightButton = mouse.Flags.HasFlag (MouseFlags.RightButtonClicked);

        // Allow marking if: ShowMarks=true OR MarkMultiple=true
        bool allowMarking = ShowMarks || MarkMultiple;

        if (ctrl && leftButton && MarkMultiple && allowMarking)
        {
            // Ctrl+LeftClick: Toggle mark state directly
            Source.SetMark (index, !Source.IsMarked (index));

            // Update SelectedItem to clicked item
            SelectedItem = index;
            SetNeedsDraw ();
        }
        else if ((shift || (ctrl && rightButton)) && MarkMultiple && allowMarking)
        {
            // Shift+Click or Ctrl+RightClick: Extend marking from anchor
            SetSelection (index, true);
        }
        else
        {
            // Normal click: Select item (SetSelection handles marking in radio button mode)
            SetSelection (index, false);

            // In checkbox/hidden marks mode, toggle the mark for the clicked item
            // In radio button mode, SetSelection already marked it, so skip MarkUnmarkSelectedItem
            // Mark item only on Clicked (not Pressed) to avoid double-toggle
            if (allowMarking && MarkMultiple && mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked))
            {
                MarkUnmarkSelectedItem ();
            }
        }
    }
}
