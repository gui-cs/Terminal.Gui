using System.ComponentModel;

namespace Terminal.Gui.Drivers;

/// <summary>
///     INTERNAL: Processes raw mouse events from drivers and generates synthetic click events (single, double, triple clicks)
///     based on button state tracking and timing analysis.
/// </summary>
/// <remarks>
///     <para>
///         This class acts as a middleware between the driver layer and the application layer, transforming low-level
///         pressed/released events into higher-level click events. It maintains state for all four mouse buttons
///         independently using <see cref="MouseButtonClickTracker"/> instances.
///     </para>
///     <para>
///         For each incoming <see cref="MouseEventArgs"/>, the interpreter:
///     </para>
///     <list type="number">
///         <item><description>Yields the original event unchanged (for low-level handling)</description></item>
///         <item><description>Updates the state of all four button trackers</description></item>
///         <item><description>Yields synthetic click events when button releases are detected within the threshold</description></item>
///     </list>
///     <para>
///         Click detection follows standard UI conventions: clicks are counted on button **release**, not press,
///         and consecutive clicks must occur within <see cref="RepeatedClickThreshold"/> milliseconds at the same
///         position to be counted as multi-clicks.
///     </para>
/// </remarks>
internal class MouseInterpreter
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseInterpreter"/> class.
    /// </summary>
    /// <param name="doubleClickThreshold">
    ///     Optional threshold for multi-click detection. If <see langword="null"/>, defaults to 500 milliseconds.
    ///     This value determines how quickly consecutive clicks must occur to be counted together.
    /// </param>
    /// <remarks>
    ///     Creates four <see cref="MouseButtonClickTracker"/> instances, one for each supported mouse button
    ///     (Button1/Left, Button2/Middle, Button3/Right, Button4), all using the same threshold.
    ///     Event timestamps from <see cref="MouseEventArgs.Timestamp"/> are used for timing calculations.
    /// </remarks>
    public MouseInterpreter (TimeSpan? doubleClickThreshold = null)
    {
        RepeatedClickThreshold = doubleClickThreshold ?? TimeSpan.FromMilliseconds (500);

        _mouseButtonClickTracker =
        [
            new (RepeatedClickThreshold, 0),
            new (RepeatedClickThreshold, 1),
            new (RepeatedClickThreshold, 2),
            new (RepeatedClickThreshold, 3)
        ];
    }

    /// <summary>
    ///     Gets or sets the maximum time allowed between consecutive clicks for them to be counted as a multi-click
    ///     (double-click, triple-click).
    /// </summary>
    /// <value>
    ///     A <see cref="TimeSpan"/> representing the click threshold. Defaults to 500 milliseconds.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         If the time between button releases exceeds this threshold, the click counter resets to 1.
    ///         This value applies to all mouse buttons tracked by this interpreter.
    ///     </para>
    ///     <para>
    ///         Standard double-click thresholds typically range from 300-500ms depending on the operating system.
    ///     </para>
    /// </remarks>
    public TimeSpan RepeatedClickThreshold { get; set; }

    private readonly MouseButtonClickTracker [] _mouseButtonClickTracker;

    /// <summary>
    ///     Processes a raw mouse event and generates both the original event and any synthetic click events.
    /// </summary>
    /// <param name="mouseEvent">The mouse event to process, typically from a driver layer.</param>
    /// <returns>
    ///     An enumerable sequence of <see cref="MouseEventArgs"/>:
    ///     <list type="bullet">
    ///         <item><description>Zero or more expired pending click events (if threshold exceeded)</description></item>
    ///         <item><description>The original input event (always yielded)</description></item>
    ///         <item><description>Zero or more synthetic click events from the current action</description></item>
    ///     </list>
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method uses timestamp-based click detection with pending clicks:
    ///     </para>
    ///     <list type="number">
    ///         <item><description>Check all trackers for expired pending clicks using event timestamp and yield them first</description></item>
    ///         <item><description>Yield the original input event for low-level handling</description></item>
    ///         <item><description>Update button state trackers and yield any clicks triggered by the current action</description></item>
    ///     </list>
    ///     <para>
    ///         Example sequence for a double click:
    ///     </para>
    ///     <code>
    ///         Input: LeftButtonPressed  → Yields: LeftButtonPressed
    ///         Input: LeftButtonReleased → Yields: LeftButtonReleased (click pending)
    ///         Input: LeftButtonPressed  → Yields: LeftButtonClicked (pending), LeftButtonPressed
    ///         Input: LeftButtonReleased → Yields: LeftButtonReleased (double-click pending)
    ///         [wait &gt; threshold]
    ///         Next input              → Yields: LeftButtonDoubleClicked (expired pending), [original event]
    ///     </code>
    ///     <para>
    ///         For isolated single-clicks with no follow-up action, the pending click will be yielded
    ///         on the next mouse event OR when the application calls <see cref="CheckForPendingClicks"/>.
    ///     </para>
    /// </remarks>
    public IEnumerable<MouseEventArgs> Process (MouseEventArgs mouseEvent)
    {
        // First, check all trackers for expired pending clicks using event timestamp
        for (var i = 0; i < 4; i++)
        {
            if (_mouseButtonClickTracker [i].CheckForExpiredClicks (mouseEvent.Timestamp, out int? expiredClicks, out Point position))
            {
                yield return CreateClickEvent (i, expiredClicks!.Value, mouseEvent, position);
            }
        }

        // Then yield the original event
        yield return mouseEvent;

        // Finally, process the current event and yield any new clicks
        for (var i = 0; i < 4; i++)
        {
            _mouseButtonClickTracker [i].UpdateState (mouseEvent, out int? numClicks);

            if (numClicks.HasValue)
            {
                yield return CreateClickEvent (i, numClicks.Value, mouseEvent, mouseEvent.Position);
            }
        }
    }

    /// <summary>
    ///     Checks all button trackers for pending clicks that have exceeded the threshold.
    /// </summary>
    /// <param name="now">The current time to use for checking expired clicks. Typically <see cref="DateTime.Now"/>.</param>
    /// <returns>
    ///     An enumerable sequence of <see cref="MouseEventArgs"/> for any expired pending clicks.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method should be called periodically by <see cref="InputProcessorImpl{TInputRecord}.ProcessQueue"/>
    ///         to ensure isolated single-clicks (where no follow-up mouse action occurs) are eventually yielded
    ///         to the application.
    ///     </para>
    ///     <para>
    ///         In normal operation, pending clicks are typically yielded on the next mouse action via <see cref="Process"/>.
    ///         This method handles the edge case where the user performs a single click and then doesn't move the mouse.
    ///     </para>
    /// </remarks>
    public IEnumerable<MouseEventArgs> CheckForPendingClicks (DateTime now)
    {
        for (var i = 0; i < 4; i++)
        {
            if (_mouseButtonClickTracker [i].CheckForExpiredClicks (now, out int? expiredClicks, out Point position))
            {
                // Create a synthetic event with the stored position
                yield return CreateClickEvent (i, expiredClicks!.Value, null, position);
            }
        }
    }

    /// <summary>
    ///     Creates a synthetic click event based on button index and click count.
    /// </summary>
    /// <param name="button">The zero-based button index (0=Button1/Left, 1=Button2/Middle, 2=Button3/Right, 3=Button4).</param>
    /// <param name="numberOfClicks">The number of consecutive clicks detected (1=single, 2=double, 3+=triple).</param>
    /// <param name="mouseEventArgs">The original mouse event to copy position and view information from, or null for expired pending clicks.</param>
    /// <param name="position">The position to use for the click event (overrides mouseEventArgs.Position if provided).</param>
    /// <returns>
    ///     A new <see cref="MouseEventArgs"/> with the appropriate click flag (LeftButtonClicked, LeftButtonDoubleClicked,
    ///     LeftButtonTripleClicked, etc.) and position/view information.
    /// </returns>
    /// <remarks>
    ///     The returned event has <see cref="HandledEventArgs.Handled"/> set to <see langword="false"/> to allow
    ///     propagation through the event system. Logs a trace message when raising the click event.
    /// </remarks>
    private MouseEventArgs CreateClickEvent (int button, int numberOfClicks, MouseEventArgs? mouseEventArgs, Point position)
    {
        var newClick = new MouseEventArgs
        {
            Handled = false,
            Flags = ToClicks (button, numberOfClicks),
            ScreenPosition = position,
            View = mouseEventArgs?.View,
            Position = position
        };
        Logging.Trace ($"Raising click event:{newClick.Flags} at screen {newClick.ScreenPosition}");

        return newClick;
    }

    /// <summary>
    ///     Converts a button index and click count into the appropriate <see cref="MouseFlags"/> click flag.
    /// </summary>
    /// <param name="buttonIdx">The zero-based button index (0=Button1/Left, 1=Button2/Middle, 2=Button3/Right, 3=Button4).</param>
    /// <param name="numberOfClicks">
    ///     The number of consecutive clicks detected:
    ///     <list type="bullet">
    ///         <item><description>1 - Single click (Button*Clicked)</description></item>
    ///         <item><description>2 - Double click (Button*DoubleClicked)</description></item>
    ///         <item><description>3+ - Triple click (Button*TripleClicked)</description></item>
    ///     </list>
    /// </param>
    /// <returns>The corresponding <see cref="MouseFlags"/> value for the button and click count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="numberOfClicks"/> is zero or <paramref name="buttonIdx"/> is not 0-3.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method maps button indices and click counts to specific <see cref="MouseFlags"/> values.
    ///         Four or more consecutive clicks are reported as triple-clicks.
    ///     </para>
    ///     <para>
    ///         Button index mapping:
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Index</term>
    ///             <description>Button / Flags</description>
    ///         </listheader>
    ///         <item>
    ///             <term>0</term>
    ///             <description>Button1 (Left) → LeftButtonClicked, LeftButtonDoubleClicked, LeftButtonTripleClicked</description>
    ///         </item>
    ///         <item>
    ///             <term>1</term>
    ///             <description>Button2 (Middle) → MiddleButtonClicked, MiddleButtonDoubleClicked, MiddleButtonTripleClicked</description>
    ///         </item>
    ///         <item>
    ///             <term>2</term>
    ///             <description>Button3 (Right) → RightButtonClicked, RightButtonDoubleClicked, RightButtonTripleClicked</description>
    ///         </item>
    ///         <item>
    ///             <term>3</term>
    ///             <description>Button4 → Button4Clicked, Button4DoubleClicked, Button4TripleClicked</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private MouseFlags ToClicks (int buttonIdx, int numberOfClicks)
    {
        if (numberOfClicks == 0)
        {
            throw new ArgumentOutOfRangeException (nameof (numberOfClicks), @"Zero clicks are not valid.");
        }

        return buttonIdx switch
               {
                   0 => numberOfClicks switch
                        {
                            1 => MouseFlags.LeftButtonClicked,
                            2 => MouseFlags.LeftButtonDoubleClicked,
                            _ => MouseFlags.LeftButtonTripleClicked
                        },
                   1 => numberOfClicks switch
                        {
                            1 => MouseFlags.MiddleButtonClicked,
                            2 => MouseFlags.MiddleButtonDoubleClicked,
                            _ => MouseFlags.MiddleButtonTripleClicked
                        },
                   2 => numberOfClicks switch
                        {
                            1 => MouseFlags.RightButtonClicked,
                            2 => MouseFlags.RightButtonDoubleClicked,
                            _ => MouseFlags.RightButtonTripleClicked
                        },
                   3 => numberOfClicks switch
                        {
                            1 => MouseFlags.Button4Clicked,
                            2 => MouseFlags.Button4DoubleClicked,
                            _ => MouseFlags.Button4TripleClicked
                        },
                   _ => throw new ArgumentOutOfRangeException (nameof (buttonIdx), @"Unsupported button index")
               };
    }
}
