namespace Terminal.Gui.Views;

public partial class MarkdownView
{
    /// <summary>A contiguous link span on a single rendered line, built during layout.</summary>
    internal sealed class MarkdownLinkRegion
    {
        public int Line { get; init; }
        public int StartX { get; init; }
        public int EndXExclusive { get; set; }
        public string Url { get; init; } = "";
    }

    private void SetupBindingsAndCommands ()
    {
        AddCommand (Command.Up, () => ScrollVertical (-1));
        AddCommand (Command.Down, () => ScrollVertical (1));
        AddCommand (Command.PageUp, () => ScrollVertical (-Math.Max (Viewport.Height - 1, 1)));
        AddCommand (Command.PageDown, () => ScrollVertical (Math.Max (Viewport.Height - 1, 1)));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
        AddCommand (Command.Start, () => { Viewport = Viewport with { Y = 0 }; return true; });
        AddCommand (Command.End, () => { Viewport = Viewport with { Y = Math.Max (GetContentSize ().Height - Viewport.Height, 0) }; return true; });
        AddCommand (Command.Accept, () => ActivateCurrentLink ());
        AddCommand (Command.NextTabStop, () => NavigateLink (1));
        AddCommand (Command.PreviousTabStop, () => NavigateLink (-1));
        AddCommand (Command.Activate, HandleActivateCommand);

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.CursorLeft, Command.ScrollLeft);
        KeyBindings.Add (Key.CursorRight, Command.ScrollRight);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.ReplaceCommands (Key.Enter, Command.Accept);
        KeyBindings.ReplaceCommands (Key.Tab, Command.NextTabStop);
        KeyBindings.ReplaceCommands (Key.Tab.WithShift, Command.PreviousTabStop);

        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledRight, Command.ScrollRight);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledLeft, Command.ScrollLeft);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate);
    }

    /// <summary>Handles the <see cref="Command.Activate"/> command, dispatched by mouse click bindings.</summary>
    private bool? HandleActivateCommand (ICommandContext? ctx)
    {
        EnsureLayout ();

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        if (ctx?.Binding is not MouseBinding { MouseEvent: { Position: { } pos } })
        {
            return true;
        }

        int contentX = Viewport.X + pos.X;
        int contentY = Viewport.Y + pos.Y;

        for (var i = 0; i < _linkRegions.Count; i++)
        {
            MarkdownLinkRegion region = _linkRegions [i];

            if (region.Line != contentY)
            {
                continue;
            }

            if (contentX < region.StartX || contentX >= region.EndXExclusive)
            {
                continue;
            }

            _activeLinkIndex = i;
            ActivateLink (region);

            return true;
        }

        return true;
    }

    /// <summary>
    ///     Builds the deduplicated list of link regions by scanning rendered lines.
    ///     Called at the end of <see cref="BuildRenderedLines"/>.
    /// </summary>
    private void BuildLinkRegions ()
    {
        _linkRegions.Clear ();
        _activeLinkIndex = -1;

        for (var lineIdx = 0; lineIdx < _renderedLines.Count; lineIdx++)
        {
            RenderedLine line = _renderedLines [lineIdx];
            var x = 0;
            string? currentUrl = null;
            MarkdownLinkRegion? currentRegion = null;

            foreach (StyledSegment segment in line.Segments)
            {
                int segWidth = segment.Text.GetColumns ();

                if (!string.IsNullOrWhiteSpace (segment.Url))
                {
                    if (currentUrl == segment.Url && currentRegion is { })
                    {
                        currentRegion.EndXExclusive = x + segWidth;
                    }
                    else
                    {
                        currentRegion = new MarkdownLinkRegion
                        {
                            Line = lineIdx,
                            StartX = x,
                            EndXExclusive = x + segWidth,
                            Url = segment.Url!
                        };

                        _linkRegions.Add (currentRegion);
                        currentUrl = segment.Url;
                    }
                }
                else
                {
                    currentUrl = null;
                    currentRegion = null;
                }

                x += segWidth;
            }
        }
    }

    /// <summary>Moves the active link index by <paramref name="delta"/> and scrolls to show it.</summary>
    private bool NavigateLink (int delta)
    {
        EnsureLayout ();

        if (_linkRegions.Count == 0)
        {
            return false;
        }

        if (_activeLinkIndex < 0)
        {
            _activeLinkIndex = delta > 0 ? 0 : _linkRegions.Count - 1;
        }
        else
        {
            _activeLinkIndex += delta;
        }

        if (_activeLinkIndex >= _linkRegions.Count)
        {
            _activeLinkIndex = 0;
        }
        else if (_activeLinkIndex < 0)
        {
            _activeLinkIndex = _linkRegions.Count - 1;
        }

        ScrollToLinkRegion (_linkRegions [_activeLinkIndex]);
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Activates the currently highlighted link (Enter key).</summary>
    private bool ActivateCurrentLink ()
    {
        if (_activeLinkIndex < 0 || _activeLinkIndex >= _linkRegions.Count)
        {
            return false;
        }

        ActivateLink (_linkRegions [_activeLinkIndex]);

        return true;
    }

    /// <summary>Activates a link region: scrolls for anchors, opens URL otherwise.</summary>
    private void ActivateLink (MarkdownLinkRegion region)
    {
        if (region.Url.StartsWith ('#'))
        {
            ScrollToAnchor (region.Url);
            RaiseLinkClicked (region.Url);

            return;
        }

        bool handled = RaiseLinkClicked (region.Url);

        if (!handled)
        {
            Link.OpenUrl (region.Url);
        }
    }

    /// <summary>Scrolls the viewport so that the given link region is visible.</summary>
    private void ScrollToLinkRegion (MarkdownLinkRegion region)
    {
        int lineY = region.Line;

        if (lineY < Viewport.Y)
        {
            Viewport = Viewport with { Y = lineY };
        }
        else if (lineY >= Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = lineY - Viewport.Height + 1 };
        }
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the segment at position (<paramref name="contentX"/>,
    ///     <paramref name="lineIdx"/>) belongs to the currently active (focused) link.
    /// </summary>
    internal bool IsActiveLinkAt (int lineIdx, int contentX)
    {
        if (_activeLinkIndex < 0 || _activeLinkIndex >= _linkRegions.Count)
        {
            return false;
        }

        MarkdownLinkRegion active = _linkRegions [_activeLinkIndex];

        return active.Line == lineIdx && contentX >= active.StartX && contentX < active.EndXExclusive;
    }
}
