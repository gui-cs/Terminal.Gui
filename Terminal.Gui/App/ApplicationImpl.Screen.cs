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
        Trace.Draw ("ApplicationImpl", "Start", $"forceRedraw={forceRedraw}, Screen={Screen}, _inlineScreenSized={_inlineScreenSized}");

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

        // Inline mode: on the first draw, ensure enough vertical space exists for the view,
        // scrolling the terminal if the cursor is near the bottom. Then resize Screen and set
        // the rendering row offset. This is the technique used by fzf, atuin, etc.
        if (Application.AppModel == AppModel.Inline && !_inlineScreenSized && Driver is { })
        {
            // Get the view's desired height from the initial full-terminal layout
            View? topView = views.LastOrDefault (v => v is { });
            int viewHeight = topView?.Frame.Height ?? 0;

            InlineState state = Driver.InlineState;
            int cursorRow = state.InlineCursorRow;
            int termHeight = Screen.Height;

            if (viewHeight > 0)
            {
                // If the view doesn't fit below the cursor, scroll the terminal up
                int overflow = cursorRow + viewHeight - termHeight;

                if (overflow > 0)
                {
                    // Emit newlines to force the terminal to scroll
                    Driver.WriteRaw (new string ('\n', overflow));
                    Driver.WriteRaw ($"{EscSeqUtils.CSI}{overflow}A");

                    // The scroll shifted everything up — adjust the cursor row
                    cursorRow -= overflow;
                }

                // Reserve the vertical space for the inline region
                Driver.WriteRaw (new string ('\n', viewHeight));
                Driver.WriteRaw ($"{EscSeqUtils.CSI}{viewHeight}A");
            }

            int availableHeight = termHeight - cursorRow;

            if (availableHeight > 0 && cursorRow > 0)
            {
                state.InlineRowOffset = cursorRow;
                state.InlineContentHeight = viewHeight;
                Driver.InlineState = state;

                // Resize Screen to only the available rows below the cursor
                Screen = new Rectangle (0, 0, Screen.Width, availableHeight);

                // Re-layout with the new (smaller) Screen size
                neededLayout = View.Layout (views.ToArray ().Reverse ()!, Screen.Size);
            }

            _inlineScreenSized = true;
        }

        // Inline mode: after initial setup, handle dynamic growth. If the view's desired height
        // exceeds Screen.Height, scroll the terminal to make room and grow Screen.
        if (Application.AppModel == AppModel.Inline && _inlineScreenSized && Driver is { })
        {
            View? topView = views.LastOrDefault (v => v is { });
            int viewHeight = topView?.Frame.Height ?? 0;

            if (viewHeight > Screen.Height)
            {
                InlineState state = Driver.InlineState;
                int extraRows = viewHeight - Screen.Height;

                // We can only gain rows by decreasing InlineRowOffset (scrolling terminal up).
                int canScroll = Math.Min (extraRows, state.InlineRowOffset);

                if (canScroll > 0)
                {
                    // Scroll the terminal up to make room below
                    Driver.WriteRaw (new string ('\n', canScroll));
                    Driver.WriteRaw ($"{EscSeqUtils.CSI}{canScroll}A");

                    state.InlineRowOffset -= canScroll;
                }

                // Grow screen by however many rows we gained (may be less than extraRows if at top)
                int newHeight = Screen.Width > 0
                                    ? Math.Min (viewHeight, Screen.Height + canScroll)
                                    : Screen.Height;

                if (newHeight != Screen.Height)
                {
                    state.InlineContentHeight = newHeight;
                    Driver.InlineState = state;
                    Screen = new Rectangle (0, 0, Screen.Width, newHeight);
                    neededLayout = View.Layout (views.ToArray ().Reverse ()!, Screen.Size);
                }
                else
                {
                    Driver.InlineState = state;
                }
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
