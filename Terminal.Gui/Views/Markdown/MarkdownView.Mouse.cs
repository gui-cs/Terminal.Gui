namespace Terminal.Gui.Views;

public partial class MarkdownView
{
    private struct MarkdownLinkRange
    {
        public int Y { get; init; }
        public int StartX { get; init; }
        public int EndXExclusive { get; init; }
        public string Url { get; init; }
    }

    /// <inheritdoc />
    protected override bool OnMouseEvent (Mouse mouse)
    {
        EnsureLayout ();

        if (mouse.Flags == MouseFlags.WheeledDown)
        {
            ScrollViewportVertical (1);

            return true;
        }

        if (mouse.Flags == MouseFlags.WheeledUp)
        {
            ScrollViewportVertical (-1);

            return true;
        }

        if (mouse.Flags == MouseFlags.WheeledRight)
        {
            ScrollViewportHorizontal (1);

            return true;
        }

        if (mouse.Flags == MouseFlags.WheeledLeft)
        {
            ScrollViewportHorizontal (-1);

            return true;
        }

        if (mouse.Flags != MouseFlags.LeftButtonClicked && mouse.Flags != MouseFlags.LeftButtonReleased)
        {
            return base.OnMouseEvent (mouse);
        }

        if (CanFocus && !HasFocus)
        {
            SetFocus ();
        }

        if (mouse.Position is null)
        {
            return true;
        }

        int contentX = Viewport.X + mouse.Position.Value.X;
        int contentY = Viewport.Y + mouse.Position.Value.Y;

        foreach (MarkdownLinkRange range in _linkRanges)
        {
            if (range.Y != contentY)
            {
                continue;
            }

            if (contentX < range.StartX || contentX >= range.EndXExclusive)
            {
                continue;
            }

            bool handled = RaiseLinkClicked (range.Url);

            if (handled)
            {
                return true;
            }

            // Anchor links scroll to the matching heading
            if (range.Url.StartsWith ('#'))
            {
                ScrollToAnchor (range.Url);
            }
            else
            {
                Link.OpenUrl (range.Url);
            }

            return true;
        }

        return true;
    }

    private void SetupBindingsAndCommands ()
    {
        AddCommand (Command.Up, () => ScrollViewportVertical (-1));
        AddCommand (Command.Down, () => ScrollViewportVertical (1));
        AddCommand (Command.PageUp, () => ScrollViewportVertical (-Math.Max (Viewport.Height - 1, 1)));
        AddCommand (Command.PageDown, () => ScrollViewportVertical (Math.Max (Viewport.Height - 1, 1)));
        AddCommand (Command.ScrollUp, () => ScrollViewportVertical (-1));
        AddCommand (Command.ScrollDown, () => ScrollViewportVertical (1));
        AddCommand (Command.ScrollLeft, () => ScrollViewportHorizontal (-1));
        AddCommand (Command.ScrollRight, () => ScrollViewportHorizontal (1));
        AddCommand (Command.Start, () => ScrollTop ());
        AddCommand (Command.End, () => ScrollBottom ());

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.CursorLeft, Command.ScrollLeft);
        KeyBindings.Add (Key.CursorRight, Command.ScrollRight);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);

        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledRight, Command.ScrollRight);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledLeft, Command.ScrollLeft);
    }

    private bool ScrollViewportVertical (int delta)
    {
        Size content = GetContentSize ();
        int maxY = Math.Max (content.Height - Viewport.Height, 0);
        int newY = Math.Min (Math.Max (Viewport.Y + delta, 0), maxY);

        if (newY == Viewport.Y)
        {
            return true;
        }

        Viewport = Viewport with { Y = newY };
        SetNeedsDraw ();

        return true;
    }

    private bool ScrollViewportHorizontal (int delta)
    {
        Size content = GetContentSize ();
        int maxX = Math.Max (content.Width - Viewport.Width, 0);
        int newX = Math.Min (Math.Max (Viewport.X + delta, 0), maxX);

        if (newX == Viewport.X)
        {
            return true;
        }

        Viewport = Viewport with { X = newX };
        SetNeedsDraw ();

        return true;
    }

    private bool ScrollTop ()
    {
        if (Viewport.Y == 0)
        {
            return true;
        }

        Viewport = Viewport with { Y = 0 };
        SetNeedsDraw ();

        return true;
    }

    private bool ScrollBottom ()
    {
        Size content = GetContentSize ();
        int maxY = Math.Max (content.Height - Viewport.Height, 0);

        if (Viewport.Y == maxY)
        {
            return true;
        }

        Viewport = Viewport with { Y = maxY };
        SetNeedsDraw ();

        return true;
    }
}
