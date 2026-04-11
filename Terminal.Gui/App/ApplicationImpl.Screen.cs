namespace Terminal.Gui.App;

using Trace = Terminal.Gui.Tracing.Trace;

internal partial class ApplicationImpl
{
    /// <inheritdoc/>
    public event EventHandler<EventArgs<Rectangle>>? ScreenChanged;

    /// <inheritdoc/>
    public Rectangle Screen
    {
        get => Driver?.Screen ?? new Rectangle (new Point (0, 0), new Size (2048, 2048));
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

    /// <summary>
    ///     Tracks whether the inline-mode Screen has been sized from the first layout pass.
    /// </summary>
    private bool _inlineScreenSized;

    /// <summary>
    ///     INTERNAL: Called when the application's screen has changed.
    ///     Raises the <see cref="ScreenChanged"/> event.
    /// </summary>
    /// <param name="screen">The new screen size and position.</param>
    private void RaiseScreenChangedEvent (Rectangle screen)
    {
        //Screen = new (Point.Empty, screen.Size);

        ScreenChanged?.Invoke (this, new EventArgs<Rectangle> (screen));

        foreach (SessionToken t in SessionStack!)
        {
            if (t.Runnable is View runnableView)
            {
                runnableView.SetNeedsLayout ();
            }
        }
    }

    private void Driver_SizeChanged (object? sender, SizeChangedEventArgs e) => RaiseScreenChangedEvent (new Rectangle (new Point (0, 0), e.Size ?? Size.Empty));

    /// <inheritdoc/>
    public void LayoutAndDraw (bool forceRedraw = false)
    {
        Trace.Draw ("ApplicationImpl", "Start", $"forceRedraw={forceRedraw}");

        if (ClearScreenNextIteration)
        {
            forceRedraw = true;
            ClearScreenNextIteration = false;
        }

        if (forceRedraw)
        {
            Driver?.ClearContents ();
        }

        if (SessionStack is null)
        {
            return;
        }

        List<View?> views = [.. SessionStack.Select (r => r.Runnable! as View)!];

        if (Popovers?.GetActivePopover () is { Visible: true } visiblePopover)
        {
            visiblePopover.SetNeedsDraw ();
            visiblePopover.SetNeedsLayout ();

            // Need View for views.Insert
            if (visiblePopover is View popoverView)
            {
                views.Insert (0, popoverView);
            }
        }

        // Layout
        bool neededLayout = View.Layout (views.ToArray ().Reverse ()!, Screen.Size);

        // Inline mode: after the first layout pass, resize Screen to match the runnable view's
        // computed Frame.Height. This prevents the inline region from occupying the entire terminal.
        if (Application.AppModel == AppModel.Inline && !_inlineScreenSized && Driver is { })
        {
            View? topView = views.LastOrDefault (v => v is { });

            if (topView is { })
            {
                int inlineHeight = topView.Frame.Height;

                if (inlineHeight > 0 && inlineHeight < Screen.Height)
                {
                    Screen = new Rectangle (0, 0, Screen.Width, inlineHeight);

                    // Re-layout with the resized Screen so the view fills the inline region.
                    View.Layout (views.ToArray ().Reverse ()!, Screen.Size);
                    neededLayout = true;
                }

                _inlineScreenSized = true;
            }
        }

        // Draw
        bool needsDraw = forceRedraw || views.Any (v => v is { NeedsDraw: true } or { SubViewNeedsDraw: true });

        if (Driver is { } && (neededLayout || needsDraw))
        {
            Logging.Redraws.Add (1);

            Driver.Clip = new Region (Screen);

            // Only force a complete redraw if needed (needsLayout or forceRedraw).
            // Otherwise, just redraw views that need it.
            View.Draw (views: views.ToArray ().Cast<View> (), neededLayout || forceRedraw);

            Driver.Clip = new Region (Screen);

            // Cause the driver to flush any pending updates to the terminal
            Driver?.Refresh ();
        }

        Trace.Draw ("ApplicationImpl", "End", $"neededLayout={neededLayout}, needsDraw={needsDraw}");
    }
}
