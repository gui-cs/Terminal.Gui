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
    ///     The terminal row (0-indexed) where the cursor was when the inline-mode app started.
    ///     Set by the main loop when the ANSI cursor position response is received.
    /// </summary>
    internal int InlineCursorRow { get; set; }

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

        // Inline mode: on the first draw, resize Screen so its height equals only the rows
        // from the cursor's starting position to the bottom of the terminal. Set the
        // InlineRowOffset on AnsiOutput so that Screen row 0 maps to the cursor's
        // terminal row. Then emit newlines to reserve vertical space (scrolling the terminal
        // if the cursor is near the bottom). This is the technique used by fzf, atuin, etc.
        if (Application.AppModel == AppModel.Inline && !_inlineScreenSized && Driver is { })
        {
            int cursorRow = InlineCursorRow;
            int termHeight = Screen.Height;
            int availableHeight = termHeight - cursorRow;

            if (availableHeight > 0 && availableHeight != termHeight)
            {
                // Set the row offset so SetCursorPositionImpl adds cursorRow to all row positions
                if (Driver.GetOutput () is AnsiOutput ansiOutput)
                {
                    ansiOutput.InlineRowOffset = cursorRow;
                }

                // Resize Screen to only the available rows below the cursor
                Screen = new Rectangle (0, 0, Screen.Width, availableHeight);

                // Re-layout with the new (smaller) Screen size
                neededLayout = View.Layout (views.ToArray ().Reverse ()!, Screen.Size);
            }

            View? topView = views.LastOrDefault (v => v is { });

            if (topView is { })
            {
                int inlineHeight = topView.Frame.Height;

                if (inlineHeight > 0)
                {
                    // Emit newlines to reserve space, then cursor-up to reclaim the region
                    Driver.WriteRaw (new string ('\n', inlineHeight));
                    Driver.WriteRaw ($"{EscSeqUtils.CSI}{inlineHeight}A");
                }
            }

            _inlineScreenSized = true;
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
