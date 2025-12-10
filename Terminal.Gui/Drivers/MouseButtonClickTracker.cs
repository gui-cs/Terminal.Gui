namespace Terminal.Gui.Drivers;

/// <summary>
///     INTERNAL: Tracks the state of a single mouse button to detect multi-click patterns (single, double, triple clicks).
///     Manages button press/release state, timing, and position tracking to determine when consecutive clicks should be
///     counted together.
/// </summary>
/// <remarks>
///     <para>
///         This class is used by <see cref="MouseInterpreter"/> to implement click detection for each mouse button
///         independently. It uses a time threshold and position matching to determine whether consecutive button
///         presses should be counted as multi-clicks (e.g., double-click, triple-click).
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
    /// <param name="e">The mouse event arguments containing button flags and position.</param>
    /// <param name="numClicks">
    ///     Output parameter indicating the number of consecutive clicks detected. Returns:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see langword="null"/> - No click event (button state unchanged or expired)</description>
    ///         </item>
    ///         <item>
    ///             <description>1 - Single click (button released after press)</description>
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
    ///         This method implements the following logic:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 If time since last state change exceeds _repeatClickThreshold or position changed,
    ///                 reset consecutive click counter.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If button state hasn't changed (still pressed or still released), do nothing.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If button was pressed and is now released (within threshold), increment and return consecutive click
    ///                 count.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If button was released and is now pressed, prepare for potential next click but don't return a count
    ///                 yet.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Click counting occurs on button **release**, not press, following standard UI conventions.
    ///     </para>
    /// </remarks>
    public void UpdateState (MouseEventArgs e, out int? numClicks)
    {
        bool isPressedNow = IsPressed (_buttonIdx, e.Flags);
        bool isSamePosition = _lastPosition == e.Position;

        TimeSpan elapsed = _now () - At;

        if (elapsed > _repeatClickThreshold || !isSamePosition)
        {
            // Expired
            OverwriteState (e);
            _consecutiveClicks = 0;
            numClicks = null;
        }
        else
        {
            if (isPressedNow == Pressed)
            {
                // No change in button state so do nothing
                numClicks = null;

                return;
            }

            if (Pressed)
            {
                // Click released
                numClicks = ++_consecutiveClicks;
            }
            else
            {
                numClicks = null;
            }

            // Record new state
            OverwriteState (e);
        }
    }

    /// <summary>
    ///     Overwrites the current state with values from a new mouse event.
    /// </summary>
    /// <param name="e">The mouse event containing the new state to record.</param>
    /// <remarks>
    ///     Updates <see cref="Pressed"/>, <see cref="At"/>, and the last known position to match the current event.
    /// </remarks>
    private void OverwriteState (MouseEventArgs e)
    {
        Pressed = IsPressed (_buttonIdx, e.Flags);
        At = _now ();
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
