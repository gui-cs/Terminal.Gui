
namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    /// <inheritdoc/>
    public event EventHandler<EventArgs<Rectangle>>? ScreenChanged;

    private readonly object _lockScreen = new ();
    private Rectangle? _screen;

    /// <inheritdoc/>
    public Rectangle Screen
    {
        get
        {
            lock (_lockScreen)
            {
                if (_screen == null)
                {
                    _screen = Driver?.Screen ?? new (new (0, 0), new (2048, 2048));
                }

                return _screen.Value;
            }
        }
        set
        {
            if (value is { } && (value.X != 0 || value.Y != 0))
            {
                throw new NotImplementedException ("Screen locations other than 0, 0 are not yet supported");
            }

            lock (_lockScreen)
            {
                _screen = value;
            }
        }
    }

    /// <inheritdoc/>
    public bool ClearScreenNextIteration { get; set; }

    /// <inheritdoc/>
    public bool PositionCursor ()
    {
        if (Driver is null)
        {
            return false;
        }

        // Find the most focused view and position the cursor there.
        View? mostFocused = Navigation?.GetFocused ();

        // If the view is not visible or enabled, don't position the cursor
        if (mostFocused is null || !mostFocused.Visible || !mostFocused.Enabled)
        {
            var current = CursorVisibility.Invisible;
            Driver?.GetCursorVisibility (out current);

            if (current != CursorVisibility.Invisible)
            {
                Driver?.SetCursorVisibility (CursorVisibility.Invisible);
            }

            return false;
        }

        // If the view is not visible within it's superview, don't position the cursor
        Rectangle mostFocusedViewport = mostFocused.ViewportToScreen (mostFocused.Viewport with { Location = Point.Empty });

        Rectangle superViewViewport =
            mostFocused.SuperView?.ViewportToScreen (mostFocused.SuperView.Viewport with { Location = Point.Empty }) ?? Driver.Screen;

        if (!superViewViewport.IntersectsWith (mostFocusedViewport))
        {
            return false;
        }

        Point? cursor = mostFocused.PositionCursor ();

        Driver!.GetCursorVisibility (out CursorVisibility currentCursorVisibility);

        if (cursor is { })
        {
            // Convert cursor to screen coords
            cursor = mostFocused.ViewportToScreen (mostFocused.Viewport with { Location = cursor.Value }).Location;

            // If the cursor is not in a visible location in the SuperView, hide it
            if (!superViewViewport.Contains (cursor.Value))
            {
                if (currentCursorVisibility != CursorVisibility.Invisible)
                {
                    Driver.SetCursorVisibility (CursorVisibility.Invisible);
                }

                return false;
            }

            // Show it
            if (currentCursorVisibility == CursorVisibility.Invisible)
            {
                Driver.SetCursorVisibility (mostFocused.CursorVisibility);
            }

            return true;
        }

        if (currentCursorVisibility != CursorVisibility.Invisible)
        {
            Driver.SetCursorVisibility (CursorVisibility.Invisible);
        }

        return false;
    }

    /// <summary>
    ///     INTERNAL: Resets the Screen field to null so it will be recalculated on next access.
    /// </summary>
    private void ResetScreen ()
    {
        lock (_lockScreen)
        {
            _screen = null;
        }
    }

    /// <summary>
    ///     INTERNAL: Called when the application's screen has changed.
    ///     Raises the <see cref="ScreenChanged"/> event.
    /// </summary>
    /// <param name="screen">The new screen size and position.</param>
    private void RaiseScreenChangedEvent (Rectangle screen)
    {
        Screen = new (Point.Empty, screen.Size);

        ScreenChanged?.Invoke (this, new (screen));

        foreach (SessionToken t in SessionStack!)
        {
            if (t.Runnable is View runnableView)
            {
                runnableView.SetNeedsLayout ();
            }
        }
    }

    private void Driver_SizeChanged (object? sender, SizeChangedEventArgs e) { RaiseScreenChangedEvent (new (new (0, 0), e.Size!.Value)); }

    /// <inheritdoc/>
    public void LayoutAndDraw (bool forceRedraw = false)
    {
        List<View?> tops = [.. SessionStack!.Select(r => r.Runnable! as View)!];

        if (Popover?.GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            visiblePopover.SetNeedsDraw ();
            visiblePopover.SetNeedsLayout ();
            tops.Insert (0, visiblePopover);
        }

        bool neededLayout = View.Layout (tops.ToArray ().Reverse ()!, Screen.Size);

        if (ClearScreenNextIteration)
        {
            forceRedraw = true;
            ClearScreenNextIteration = false;
        }

        if (forceRedraw)
        {
            Driver?.ClearContents ();
        }

        if (Driver is { })
        {
            Driver.Clip = new (Screen);

            View.Draw (views: tops!, neededLayout || forceRedraw);
            Driver.Clip = new (Screen);
            Driver?.Refresh ();
        }
    }
}
