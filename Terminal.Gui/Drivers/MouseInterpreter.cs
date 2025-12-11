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
///         For each incoming <see cref="Mouse"/>, the interpreter:
///     </para>
///     <list type="number">
///         <item><description>Yields the original event unchanged (for low-level handling)</description></item>
///         <item><description>Updates the state of all four button trackers</description></item>
///         <item><description>Defers all click events to allow complete multi-click sequences to be detected</description></item>
///     </list>
///     <para>
///         Click detection follows standard UI conventions: clicks are counted on button **release**, not press,
///         and consecutive clicks must occur within <see cref="RepeatedClickThreshold"/> milliseconds at the same
///         position to be counted as multi-clicks. ALL clicks (single, double, triple, etc.) are deferred and
///         emitted via <see cref="CheckForExpiredClicks"/> after the threshold expires, ensuring applications
///         only receive the final click type without intermediate events.
///     </para>
/// </remarks>
internal class MouseInterpreter
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseInterpreter"/> class.
    /// </summary>
    /// <param name="now">
    ///     Optional function to get the current time. If <see langword="null"/>, defaults to <c>() => DateTime.Now</c>.
    ///     Useful for unit tests to inject controlled time values.
    /// </param>
    /// <param name="doubleClickThreshold">
    ///     Optional threshold for multi-click detection. If <see langword="null"/>, defaults to 500 milliseconds.
    ///     This value determines how quickly consecutive clicks must occur to be counted together.
    /// </param>
    /// <remarks>
    ///     Creates four <see cref="MouseButtonClickTracker"/> instances, one for each supported mouse button
    ///     (Button1/Left, Button2/Middle, Button3/Right, Button4), all using the same time function and threshold.
    /// </remarks>
    public MouseInterpreter (
        Func<DateTime>? now = null,
        TimeSpan? doubleClickThreshold = null
    )
    {
        Now = now ?? (() => DateTime.Now);
        RepeatedClickThreshold = doubleClickThreshold ?? TimeSpan.FromMilliseconds (500);

        _mouseButtonClickTracker =
        [
            new (Now, RepeatedClickThreshold, 0),
            new (Now, RepeatedClickThreshold, 1),
            new (Now, RepeatedClickThreshold, 2),
            new (Now, RepeatedClickThreshold, 3)
        ];
    }

    /// <summary>
    ///     Gets or sets the function for returning the current time.
    /// </summary>
    /// <value>A function that returns the current <see cref="DateTime"/>.</value>
    /// <remarks>
    ///     This property enables time injection for unit tests, ensuring repeatable and deterministic test behavior.
    ///     In production, this defaults to <c>() => DateTime.Now</c>.
    /// </remarks>
    public Func<DateTime> Now { get; set; }

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
    /// <param name="mouse">The mouse event to process, typically from a driver layer.</param>
    /// <returns>
    ///     An enumerable sequence of <see cref="Mouse"/>:
    ///     <list type="bullet">
    ///         <item><description>The original input event (always yielded first)</description></item>
    ///         <item><description>No click events are yielded immediately - all are deferred for multi-click detection</description></item>
    ///     </list>
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method uses a generator pattern (yield return) to produce events from input.
    ///         The original event is always yielded first to allow low-level handling. Click events
    ///         are NOT yielded from this method - they are deferred and must be retrieved via
    ///         <see cref="CheckForExpiredClicks"/>.
    ///     </para>
    ///     <para>
    ///         Single clicks are deferred to allow double-click detection.
    ///     </para>
    ///     <para>
    ///         Example sequence for a double click:
    ///     </para>
    ///     <code>
    ///         Input: LeftButtonPressed  → Yields: LeftButtonPressed
    ///         Input: LeftButtonReleased → Yields: LeftButtonReleased (click count=1 deferred)
    ///         Input: LeftButtonPressed  → Yields: LeftButtonPressed (deferred click cancelled)
    ///         Input: LeftButtonReleased → Yields: LeftButtonReleased (click count=2 deferred)
    ///         [After threshold expires]
    ///         CheckForExpiredClicks()   → Yields: LeftButtonDoubleClicked
    ///     </code>
    /// </remarks>
    public IEnumerable<Mouse> Process (Mouse mouse)
    {
        yield return mouse;

        // For each mouse button
        for (int i = 0; i < 4; i++)
        {
            _mouseButtonClickTracker [i].UpdateState (mouse, out int? numClicks);

            if (numClicks.HasValue)
            {
                yield return CreateClickEvent (i, numClicks.Value, mouse);
            }
        }
    }

    /// <summary>
    ///     Checks all button trackers for expired pending clicks and returns them as synthetic click events.
    /// </summary>
    /// <returns>
    ///     An enumerable sequence of <see cref="Mouse"/> containing expired single-click events that were deferred
    ///     waiting for potential multi-clicks.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method should be called periodically (e.g., during input processing) to ensure deferred
    ///         single-click events are eventually emitted after the double-click threshold expires.
    ///     </para>
    /// </remarks>
    public IEnumerable<Mouse> CheckForExpiredClicks ()
    {
        // Check each button tracker for expired clicks
        for (int i = 0; i < 4; i++)
        {
            if (_mouseButtonClickTracker [i].CheckForExpiredClicks (out int? numClicks, out Point position))
            {
                Mouse expiredClick = new ()
                {
                    Timestamp = Now (),
                    Handled = false,
                    Flags = ToClicks (i, numClicks!.Value),
                    ScreenPosition = position
                    // View and Position intentionally NOT set - set by MouseImpl/View.Mouse
                };
                
                Logging.Trace ($"Raising expired click event:{expiredClick.Flags} at screen {expiredClick.ScreenPosition}");
                
                yield return expiredClick;
            }
        }
    }

    /// <summary>
    ///     Creates a synthetic click event based on button index and click count.
    /// </summary>
    /// <param name="button">The zero-based button index (0=Button1/Left, 1=Button2/Middle, 2=Button3/Right, 3=Button4).</param>
    /// <param name="numberOfClicks">The number of consecutive clicks detected (1=single, 2=double, 3+=triple).</param>
    /// <param name="mouseEventArgs">The original mouse event to copy screen position and view information from.</param>
    /// <returns>
    ///     A new <see cref="Mouse"/> with the appropriate click flag (LeftButtonClicked, LeftButtonDoubleClicked,
    ///     LeftButtonTripleClicked, etc.) and screen position/view copied from the input event.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The returned event has <see cref="HandledEventArgs.Handled"/> set to <see langword="false"/> to allow
    ///         propagation through the event system. Logs a trace message when raising the click event.
    ///     </para>
    /// </remarks>
    private Mouse CreateClickEvent (int button, int numberOfClicks, Mouse mouseEventArgs)
    {
        var newClick = new Mouse
        {
            Timestamp = Now (),
            Handled = false,
            Flags = ToClicks (button, numberOfClicks),
            ScreenPosition = mouseEventArgs.ScreenPosition,
            // View is intentionally NOT copied - it's View-relative and set by MouseImpl/View.Mouse
            // Position is intentionally NOT copied - it's View-relative and set by MouseImpl/View.Mouse
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
