namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    /// <inheritdoc/>
    public event EventHandler<EventArgs<Rectangle>>? ScreenChanged;

    /// <inheritdoc/>
    public Rectangle Screen
    {
        get => Driver?.Screen ?? new (new (0, 0), new (2048, 2048));
        set
        {
            if (value is { } && (value.X != 0 || value.Y != 0))
            {
                throw new NotImplementedException ("Screen locations other than 0, 0 are not yet supported");
            }

            Driver?.SetScreenSize (value.Width, value.Height);
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
    ///     INTERNAL: Called when the application's screen has changed.
    ///     Raises the <see cref="ScreenChanged"/> event.
    /// </summary>
    /// <param name="screen">The new screen size and position.</param>
    private void RaiseScreenChangedEvent (Rectangle screen)
    {
        //Screen = new (Point.Empty, screen.Size);

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
        if (ClearScreenNextIteration)
        {
            forceRedraw = true;
            ClearScreenNextIteration = false;
        }

        if (forceRedraw)
        {
            Driver?.ClearContents ();
        }

        List<View?> views = [.. SessionStack!.Select (r => r.Runnable! as View)!];

        if (Popover?.GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            visiblePopover.SetNeedsDraw ();
            visiblePopover.SetNeedsLayout ();
            views.Insert (0, visiblePopover);
        }

        // Layout
        bool neededLayout = View.Layout (views.ToArray ().Reverse ()!, Screen.Size);

        // Draw
        bool needsDraw = forceRedraw || views.Any (v => v is { NeedsDraw: true } or { SubViewNeedsDraw: true });

        if (Driver is { } && (neededLayout || needsDraw))
        {
            Logging.Redraws.Add (1);

            Driver.Clip = new (Screen);

            // Only force a complete redraw if needed (needsLayout or forceRedraw).
            // Otherwise, just redraw views that need it.
            View.Draw (views: views.ToArray ().Cast<View> (), neededLayout || forceRedraw);

            //Driver.Clip = new (Screen);

            // Cause the driver to flush any pending updates to the terminal
            Driver?.Refresh ();
        }
    }
}
