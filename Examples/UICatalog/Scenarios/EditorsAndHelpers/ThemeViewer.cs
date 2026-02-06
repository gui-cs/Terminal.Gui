#nullable enable

namespace UICatalog.Scenarios;

public class ThemeViewer : FrameView
{
    public ThemeViewer ()
    {
        BorderStyle = LineStyle.Rounded;
        Border!.Thickness = new (0, 1, 0, 0);
        Margin!.Thickness = new (0, 0, 1, 0);
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        Height = Dim.Fill ();
        Width = Dim.Auto ();
        Title = $"{ThemeManager.Theme}";

        VerticalScrollBar.AutoShow = true;
        HorizontalScrollBar.AutoShow = true;

        SubViewsLaidOut += (sender, _) =>
                           {
                               if (sender is View sendingView)
                               {
                                   sendingView.SetContentSize (new Size (sendingView.GetContentSize ().Width, sendingView.GetHeightRequiredForSubViews ()));
                               }
                           };

        AddCommand (Command.Up, () => ScrollVertical (-1));
        AddCommand (Command.Down, () => ScrollVertical (1));

        AddCommand (Command.PageUp, () => ScrollVertical (-SubViews.OfType<SchemeViewer> ().First ().Frame.Height));
        AddCommand (Command.PageDown, () => ScrollVertical (SubViews.OfType<SchemeViewer> ().First ().Frame.Height));

        AddCommand (
                    Command.Start,
                    () =>
                    {
                        Viewport = Viewport with { Y = 0 };

                        return true;
                    });

        AddCommand (
                    Command.End,
                    () =>
                    {
                        Viewport = Viewport with { Y = GetContentSize ().Height };

                        return true;
                    });

        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.Add (PopoverMenu.DefaultKey, Command.Context);

        MouseBindings.Add (MouseFlags.Button1DoubleClicked, Command.Accept);
        MouseBindings.ReplaceCommands (MouseFlags.Button3Clicked, Command.Context);
        MouseBindings.ReplaceCommands (MouseFlags.Button1Clicked | MouseFlags.ButtonCtrl, Command.Context);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledLeft, Command.ScrollLeft);
        MouseBindings.Add (MouseFlags.WheeledRight, Command.ScrollRight);

        SchemeViewer? prevSchemeViewer = null;

        foreach (KeyValuePair<string, Scheme?> kvp in SchemeManager.GetSchemesForCurrentTheme ())
        {
            var schemeViewer = new SchemeViewer
            {
                Id = $"schemeViewer for {kvp.Key}",
                SchemeName = kvp.Key
            };

            if (prevSchemeViewer is { })
            {
                schemeViewer.Y = Pos.Bottom (prevSchemeViewer);
            }

            prevSchemeViewer = schemeViewer;
            base.Add (schemeViewer);
        }

        ThemeManager.ThemeChanged += OnThemeManagerOnThemeChanged;
    }

    /// <inheritdoc/>
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);

        if (focused is { })
        {
            SchemeName = focused.Title;
        }
    }

    private void OnThemeManagerOnThemeChanged (object? _, EventArgs<string> args) { Title = args.Value!; }

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnThemeManagerOnThemeChanged;
        }

        base.Dispose (disposing);
    }
}
