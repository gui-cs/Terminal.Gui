namespace Terminal.Gui.Drivers;

/// <summary>
///     INTERNAL: Tracks the state of a single mouse button to detect multi-click patterns (single, double, triple clicks).
///     Manages button press/release state, timing, and position tracking to determine when consecutive clicks should be
///     counted together.
/// </summary>
/// <remarks>
///     <para>
///         This class is used by <see cref="MouseInterpreter"/> to implement click detection for each mouse button
///         independently. It uses event timestamps and position matching to determine whether consecutive button
///         presses should be counted as multi-clicks (e.g., double-click, triple-click).
///     </para>
    ///     <para>
    ///         Click detection uses a **deferred click** approach: when a button is released, the click event
    ///         is deferred to allow time for potential multi-clicks. If no additional click occurs within
    ///         the repeat-click threshold, the pending click is emitted via <see cref="CheckForExpiredClicks"/>.
    ///         This applies to ALL clicks (single, double, triple, etc.) to allow applications to distinguish
    ///         between different user intentions without receiving intermediate events.
    ///     </para>
///     <para>
///         Not to be confused with <c>NetEvents.MouseButtonState</c>.
///     </para>
/// </remarks>
/// <param name="_now">Function to get the current time, allowing for time injection in tests.</param>
/// <param name="_repeatClickThreshold">Maximum time between clicks to count as consecutive (e.g., double-click timeout).</param>
/// <param name="_buttonIdx">
///     Zero-based index of the button being tracked (0=Button1/Left, 1=Button2/Middle,
///     2=Button3/Right, 3=Button4).
/// </param>

// ReSharper disable InconsistentNaming
internal class MouseButtonClickTracker (Func<DateTime> _now, TimeSpan _repeatClickThreshold, int _buttonIdx)
{
    // ReSharper enable InconsistentNaming
    private int _consecutiveClicks;
    private Point _lastPosition;

    // Deferred click tracking
    private int? _pendingClickCount;
    private Point _pendingClickPosition;
    private DateTime _pendingClickTime;

    /// <summary>
    ///     Gets or sets the timestamp when the button last changed state (pressed or released).
    /// </summary>
    /// <value>The <see cref="DateTime"/> when the button entered its current state.</value>
    public DateTime At { get; set; }

    /// <summary>
    ///     Gets or sets whether the button is currently in a pressed state.
    /// </summary>
    /// <value><see langword="true"/> if the button is currently down; <see langword="false"/> if released.</value>
    public bool Pressed { get; set; }

    /// <summary>
    ///     Updates the button state based on a new mouse event and determines if a multi-click occurred.
    /// </summary>
    /// <param name="mouse">The mouse event arguments containing button flags, position, and timestamp.</param>
    /// <param name="numClicks">
    ///     Output parameter indicating the number of consecutive clicks detected. Returns:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see langword="null"/> - No click event (button state unchanged or click deferred)</description>
    ///         </item>
    ///         <item>
    ///             <description>2 - Double click (second consecutive release within threshold)</description>
    ///         </item>
    ///         <item>
    ///             <description>3 - Triple click (third consecutive release within threshold)</description>
    ///         </item>
    ///         <item>
    ///             <description>4+ - Additional consecutive clicks</description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method implements timestamp-based multi-click detection with deferred single clicks:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 Check if time since last state change exceeds threshold or position changed.
    ///                 If so, reset consecutive click counter.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If button state hasn't changed (still pressed or still released), return null.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 On button press: cancel any pending single click (double-click sequence detected).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 On button release: defer the click (any count) to allow further multi-click detection.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         ALL clicks are deferred to allow applications to distinguish user intentions. Use <see cref="CheckForExpiredClicks"/>
    ///         to retrieve deferred clicks after the threshold expires.
    ///     </para>
    /// </remarks>
    public void UpdateState (Mouse mouse, out int? numClicks)
    {
        bool isPressedNow = IsPressed (_buttonIdx, mouse.Flags);
        bool isSamePosition = _lastPosition == mouse.ScreenPosition;

        TimeSpan elapsed = _now () - At;

        numClicks = null; // Default to no click

        // Check if threshold exceeded or position changed
        if (elapsed > _repeatClickThreshold || !isSamePosition)
        {
            // Reset consecutive click counter
            _consecutiveClicks = 0;
            _pendingClickCount = null; // Clear any pending click
        }

        // Check if button state changed
        if (isPressedNow == Pressed)
        {
            // No state change - do nothing
            return;
        }

        // State changed - update tracking
        if (Pressed)
        {
            // Button was pressed, now released - this is a click!
            ++_consecutiveClicks;

            // Defer ALL clicks - wait to see if more clicks follow
            // This allows applications to distinguish between single, double, and triple clicks
            _pendingClickCount = _consecutiveClicks;
            _pendingClickPosition = mouse.ScreenPosition;
            _pendingClickTime = _now ();
            numClicks = null; // Don't emit yet - always defer
        }
        else
        {
            // Button was released, now pressed
            // Cancel any pending click - this is start of next click in sequence
            _pendingClickCount = null;
        }

        // Record new state
        OverwriteState (mouse);
    }

    /// <summary>
    ///     Checks if there's a pending click that has exceeded the threshold and should be yielded.
    /// </summary>
    /// <param name="numClicks">
    ///     Output parameter indicating the number of clicks to yield if expired, or <see langword="null"/> if no expired click.
    /// </param>
    /// <param name="position">
    ///     Output parameter containing the position of the expired click if one exists.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if a pending click has expired and should be emitted; <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method should be called periodically by <see cref="MouseInterpreter"/> to check for deferred single-clicks
    ///         that have not been followed by additional clicks within the threshold time.
    ///     </para>
    /// </remarks>
    public bool CheckForExpiredClicks (out int? numClicks, out Point position)
    {
        if (_pendingClickCount.HasValue)
        {
            TimeSpan elapsed = _now () - _pendingClickTime;

            if (elapsed > _repeatClickThreshold)
            {
                // Pending click has expired - emit it
                numClicks = _pendingClickCount;
                position = _pendingClickPosition;
                _pendingClickCount = null; // Clear pending state
                
                return true;
            }
        }

        // No expired click
        numClicks = null;
        position = Point.Empty;

        return false;
    }

    /// <summary>
    ///     Overwrites the current state with values from a new mouse event.
    /// </summary>
    /// <param name="e">The mouse event containing the new state to record.</param>
    /// <remarks>
    ///     Updates <see cref="Pressed"/>, <see cref="At"/>, and the last known position to match the current event.
    /// </remarks>
    private void OverwriteState (Mouse e)
    {
        Pressed = IsPressed (_buttonIdx, e.Flags);
        At = _now ();
        _lastPosition = e.ScreenPosition;
    }

    /// <summary>
    ///     Determines if a specific button is pressed based on the mouse flags.
    /// </summary>
    /// <param name="btn">The zero-based button index (0=Button1/Left, 1=Button2/Middle, 2=Button3/Right, 3=Button4).</param>
    /// <param name="eFlags">The mouse flags to check for the pressed state.</param>
    /// <returns><see langword="true"/> if the specified button is pressed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="btn"/> is not 0-3.</exception>
    /// <remarks>
    ///     Maps button indices to their corresponding <see cref="MouseFlags"/> pressed flags:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Index</term>
    ///             <description>Button / Flag</description>
    ///         </listheader>
    ///         <item>
    ///             <term>0</term>
    ///             <description>Button1 (Left) / <see cref="MouseFlags.Button1Pressed"/></description>
    ///         </item>
    ///         <item>
    ///             <term>1</term>
    ///             <description>Button2 (Middle) / <see cref="MouseFlags.Button2Pressed"/></description>
    ///         </item>
    ///         <item>
    ///             <term>2</term>
    ///             <description>Button3 (Right) / <see cref="MouseFlags.Button3Pressed"/></description>
    ///         </item>
    ///         <item>
    ///             <term>3</term>
    ///             <description>Button4 / <see cref="MouseFlags.Button4Pressed"/></description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private bool IsPressed (int btn, MouseFlags eFlags)
    {
        return btn switch
               {
                   0 => eFlags.HasFlag (MouseFlags.Button1Pressed),
                   1 => eFlags.HasFlag (MouseFlags.Button2Pressed),
                   2 => eFlags.HasFlag (MouseFlags.Button3Pressed),
                   3 => eFlags.HasFlag (MouseFlags.Button4Pressed),
                   _ => throw new ArgumentOutOfRangeException (nameof (btn))
               };
    }
}
