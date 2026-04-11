using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

using Trace = Trace;

internal partial class ApplicationImpl
{
    /// <inheritdoc/>
    public event EventHandler<EventArgs<Rectangle>>? ScreenChanged;

    /// <summary>
    ///     Backing field for <see cref="Screen"/> in inline mode. When set, <see cref="Screen"/>
    ///     returns this instead of <see cref="IDriver.Screen"/>. In fullscreen mode this is <see langword="null"/>.
    /// </summary>
    private Rectangle? _screen;

    /// <inheritdoc/>
    public Rectangle Screen
    {
        get => _screen ?? Driver?.Screen ?? new Rectangle (new Point (0, 0), new Size (2048, 2048));
        set
        {
            if (Application.AppModel == AppModel.Inline)
            {
                // Inline mode: store the sub-rectangle independently.
                // Resize the output buffer to match the inline region dimensions.
                _screen = value;
                (Driver as DriverImpl)?.ResizeOutputBuffer (value.Width, value.Height);
            }
            else
            {
                // Fullscreen: sync with Driver.Screen (resizes both terminal tracking and buffer).
                _screen = null;
                Driver?.SetScreenSize (value.Width, value.Height);
            }
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

    private void Driver_SizeChanged (object? sender, SizeChangedEventArgs e)
    {
        Size newSize = e.Size ?? Size.Empty;

        if (Application.AppModel == AppModel.Inline && _screen is { } screen)
        {
            // In inline mode, the terminal resized but our inline region keeps its Y offset
            // and height. Only the width needs to update to match the new terminal width.
            _screen = screen with { Width = newSize.Width };
            (Driver as DriverImpl)?.ResizeOutputBuffer (newSize.Width, screen.Height);
            RaiseScreenChangedEvent (_screen.Value);
        }
        else
        {
            RaiseScreenChangedEvent (new Rectangle (new Point (0, 0), newSize));
        }
    }

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

        // Inline mode: on the first draw, position the inline region at the cursor's starting
        // terminal row. If the view doesn't fit, scroll the terminal. Then set App.Screen to
        // the sub-rectangle representing the inline region.
        if (Application.AppModel == AppModel.Inline && !_inlineScreenSized && Driver is { })
        {
            // Get the view's desired height from the initial full-terminal layout
            View? topView = views.LastOrDefault (v => v is { });
            int viewHeight = topView?.Frame.Height ?? 0;

            int cursorRow = Driver.InlineState.InlineCursorRow;
            int termHeight = Driver.Screen.Height;
            int termWidth = Driver.Screen.Width;

            if (viewHeight > 0)
            {
                // If the view doesn't fit below the cursor, scroll the terminal up
                int overflow = cursorRow + viewHeight - termHeight;

                if (overflow > 0)
                {
                    Driver.WriteRaw (new string ('\n', overflow));
                    Driver.WriteRaw ($"{EscSeqUtils.CSI}{overflow}A");
                    cursorRow -= overflow;
                }

                // Reserve the vertical space for the inline region
                Driver.WriteRaw (new string ('\n', viewHeight));
                Driver.WriteRaw ($"{EscSeqUtils.CSI}{viewHeight}A");
            }

            int availableHeight = Math.Min (viewHeight, termHeight - cursorRow);

            if (availableHeight > 0)
            {
                // Set App.Screen to the inline sub-rectangle. This resizes the output buffer
                // to the inline region dimensions and stores the Y offset for cursor positioning.
                Screen = new Rectangle (0, cursorRow, termWidth, availableHeight);

                // Re-layout with the new (smaller) Screen size
                neededLayout = View.Layout (views.ToArray ().Reverse ()!, Screen.Size);
            }

            _inlineScreenSized = true;
        }

        // Inline mode: after initial setup, handle dynamic growth. If the view's desired height
        // exceeds Screen.Height, scroll the terminal to make room and grow App.Screen.
        if (Application.AppModel == AppModel.Inline && _inlineScreenSized && Driver is { })
        {
            View? topView = views.LastOrDefault (v => v is { });
            int viewHeight = topView?.Frame.Height ?? 0;

            if (viewHeight > Screen.Height)
            {
                int extraRows = viewHeight - Screen.Height;

                // We can only gain rows by decreasing Screen.Y (scrolling terminal up).
                int canScroll = Math.Min (extraRows, Screen.Y);

                if (canScroll > 0)
                {
                    Driver.WriteRaw (new string ('\n', canScroll));
                    Driver.WriteRaw ($"{EscSeqUtils.CSI}{canScroll}A");
                }

                // Grow screen: Y decreases, Height increases
                int newY = Screen.Y - canScroll;
                int newHeight = Math.Min (viewHeight, Screen.Height + canScroll);

                if (newHeight != Screen.Height)
                {
                    Screen = new Rectangle (0, newY, Screen.Width, newHeight);
                    neededLayout = View.Layout (views.ToArray ().Reverse ()!, Screen.Size);
                }
            }
        }

        // Draw
        bool needsDraw = forceRedraw || views.Any (v => v is { NeedsDraw: true } or { SubViewNeedsDraw: true });

        if (Driver is { } && (neededLayout || needsDraw))
        {
            Logging.Redraws.Add (1);

            // Clip uses the output buffer dimensions (0-indexed), not the terminal offset.
            Rectangle clipRect = new (0, 0, Screen.Width, Screen.Height);
            Driver.Clip = new Region (clipRect);

            // Only force a complete redraw if needed (needsLayout or forceRedraw).
            // Otherwise, just redraw views that need it.
            View.Draw (views.ToArray ().Cast<View> (), neededLayout || forceRedraw);

            Driver.Clip = new Region (clipRect);

            // Cause the driver to flush any pending updates to the terminal
            Driver?.Refresh ();
        }

        Trace.Draw ("ApplicationImpl", "End", $"neededLayout={neededLayout}, needsDraw={needsDraw}");
    }
}
