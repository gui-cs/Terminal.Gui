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
///         Click detection uses a **deferred/pending click** approach: when a button is released, the click is stored
///         as pending and only yielded on the next mouse action (or after timeout expiration). This timestamp-based
///         approach eliminates the need for timers and provides cleaner multi-click detection.
///     </para>
///     <para>
///         Not to be confused with <c>NetEvents.MouseButtonState</c>.
///     </para>
/// </remarks>
/// <param name="_repeatClickThreshold">Maximum time between clicks to count as consecutive (e.g., double-click timeout).</param>
/// <param name="_buttonIdx">
///     Zero-based index of the button being tracked (0=Button1/Left, 1=Button2/Middle,
///     2=Button3/Right, 3=Button4).
/// </param>

// ReSharper disable InconsistentNaming
internal class MouseButtonClickTracker (TimeSpan _repeatClickThreshold, int _buttonIdx)
{
    // ReSharper enable InconsistentNaming
    private int _consecutiveClicks;
    private Point _lastPosition;
    private int? _pendingClickCount;
    private DateTime? _pendingClickAt;
    private Point _pendingClickPosition;

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
    /// <param name="e">The mouse event arguments containing button flags, position, and timestamp.</param>
    /// <param name="numClicks">
    ///     Output parameter indicating the number of consecutive clicks detected. Returns:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see langword="null"/> - No click event (button state unchanged or pending)</description>
    ///         </item>
    ///         <item>
    ///             <description>1 - Single click (from previous pending or position/timeout reset)</description>
    ///         </item>
    ///         <item>
    ///             <description>2 - Double click (second consecutive click within threshold)</description>
    ///         </item>
    ///         <item>
    ///             <description>3 - Triple click (third consecutive click within threshold)</description>
    ///         </item>
    ///         <item>
    ///             <description>4+ - Additional consecutive clicks (rarely used)</description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method implements **timestamp-based multi-click detection with pending clicks**:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 If there's a pending click and time/position threshold exceeded, yield it and reset.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If button state hasn't changed (still pressed or still released), do nothing.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 On state change within threshold: yield any pending click, then store new click as pending.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Pending clicks are yielded on next mouse action OR when CheckForExpiredClicks detects timeout.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Click counting occurs on button **release**, not press, following standard UI conventions.
    ///         The timestamp-based approach eliminates timers and provides deterministic, testable behavior.
    ///     </para>
    /// </remarks>
    public void UpdateState (MouseEventArgs e, out int? numClicks)
    {
        bool isPressedNow = IsPressed (_buttonIdx, e.Flags);
        bool isSamePosition = _lastPosition == e.Position;

        TimeSpan elapsed = e.Timestamp - At;

        numClicks = null; // Default to no click

        if (elapsed > _repeatClickThreshold || !isSamePosition)
        {
            // Expired or position changed - yield any pending click and reset
            if (_pendingClickCount.HasValue)
            {
                numClicks = _pendingClickCount;
            }

            OverwriteState (e);
            _consecutiveClicks = 0;
            _pendingClickCount = null;
            _pendingClickAt = null;

            return;
        }

        if (isPressedNow == Pressed)
        {
            // No change in button state so do nothing
            // Don't yield pending click - wait for actual state change
            return;
        }

        // State changed - yield any pending click from previous action
        if (_pendingClickCount.HasValue)
        {
            numClicks = _pendingClickCount;
            _pendingClickCount = null;
            _pendingClickAt = null;
        }

        if (Pressed)
        {
            // Click released - store as pending instead of returning immediately
            ++_consecutiveClicks;
            _pendingClickCount = _consecutiveClicks;
            _pendingClickAt = e.Timestamp;
            _pendingClickPosition = e.Position;
        }

        // Record new state
        OverwriteState (e);
    }

    /// <summary>
    ///     Checks if there's a pending click that has exceeded the threshold and should be yielded.
    /// </summary>
    /// <param name="now">The current time to compare against the pending click timestamp.</param>
    /// <param name="numClicks">
    ///     Output parameter indicating the number of clicks to yield if expired, or <see langword="null"/> if no expired click.
    /// </param>
    /// <param name="position">
    ///     Output parameter containing the position of the expired click if one exists.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if a pending click expired and should be yielded; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is called by <see cref="MouseInterpreter"/> to check for isolated single-clicks
    ///         where no follow-up mouse action occurs. If a click has been pending longer than the threshold,
    ///         it's time to yield it to the application.
    ///     </para>
    ///     <para>
    ///         Once a pending click is yielded through this method, it's cleared from the tracker state.
    ///     </para>
    /// </remarks>
    public bool CheckForExpiredClicks (DateTime now, out int? numClicks, out Point position)
    {
        if (_pendingClickCount.HasValue && _pendingClickAt.HasValue)
        {
            TimeSpan elapsed = now - _pendingClickAt.Value;

            if (elapsed > _repeatClickThreshold)
            {
                // Pending click has expired - yield it
                numClicks = _pendingClickCount;
                position = _pendingClickPosition;
                _pendingClickCount = null;
                _pendingClickAt = null;

                return true;
            }
        }

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
    ///     Uses the event's <see cref="MouseEventArgs.Timestamp"/> for timing calculations.
    /// </remarks>
    private void OverwriteState (MouseEventArgs e)
    {
        Pressed = IsPressed (_buttonIdx, e.Flags);
        At = e.Timestamp;
        _lastPosition = e.Position;
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
