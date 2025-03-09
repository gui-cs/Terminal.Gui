#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Not to be confused with <see cref="NetEvents.MouseButtonState"/>
/// </summary>
internal class MouseButtonStateEx
{
    private readonly Func<DateTime> _now;
    private readonly TimeSpan _repeatClickThreshold;
    private readonly int _buttonIdx;
    private int _consecutiveClicks;
    private Point _lastPosition;

    /// <summary>
    ///     When the button entered its current state.
    /// </summary>
    public DateTime At { get; set; }

    /// <summary>
    ///     <see langword="true"/> if the button is currently down
    /// </summary>
    public bool Pressed { get; set; }

    public MouseButtonStateEx (Func<DateTime> now, TimeSpan repeatClickThreshold, int buttonIdx)
    {
        _now = now;
        _repeatClickThreshold = repeatClickThreshold;
        _buttonIdx = buttonIdx;
    }

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

    private void OverwriteState (MouseEventArgs e)
    {
        Pressed = IsPressed (_buttonIdx, e.Flags);
        At = _now ();
        _lastPosition = e.Position;
    }

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
