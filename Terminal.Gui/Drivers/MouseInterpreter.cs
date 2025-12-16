

namespace Terminal.Gui.Drivers;

internal class MouseInterpreter
{
    /// <summary>
    ///     Time provider for getting the current time. Use ITimeProvider in tests to
    ///     ensure repeatable tests with virtual time.
    /// </summary>
    public ITimeProvider TimeProvider { get; set; }

    /// <summary>
    ///     How long to wait for a second, third, fourth click after the first before giving up and
    ///     releasing event as a 'click'
    /// </summary>
    public TimeSpan RepeatedClickThreshold { get; set; }

    private readonly MouseButtonStateEx [] _buttonStates;

    public MouseInterpreter (
        ITimeProvider? timeProvider = null,
        TimeSpan? doubleClickThreshold = null
    )
    {
        TimeProvider = timeProvider ?? new SystemTimeProvider ();
        RepeatedClickThreshold = doubleClickThreshold ?? TimeSpan.FromMilliseconds (500);

        _buttonStates = new []
        {
            new MouseButtonStateEx (TimeProvider, RepeatedClickThreshold, 0),
            new MouseButtonStateEx (TimeProvider, RepeatedClickThreshold, 1),
            new MouseButtonStateEx (TimeProvider, RepeatedClickThreshold, 2),
            new MouseButtonStateEx (TimeProvider, RepeatedClickThreshold, 3)
        };
    }

    public IEnumerable<MouseEventArgs> Process (MouseEventArgs e)
    {
        yield return e;

        // For each mouse button
        for (var i = 0; i < 4; i++)
        {
            _buttonStates [i].UpdateState (e, out int? numClicks);

            if (numClicks.HasValue)
            {
                yield return RaiseClick (i, numClicks.Value, e);
            }
        }
    }

    private MouseEventArgs RaiseClick (int button, int numberOfClicks, MouseEventArgs mouseEventArgs)
    {
        var newClick = new MouseEventArgs
        {
            Handled = false,
            Flags = ToClicks (button, numberOfClicks),
            ScreenPosition = mouseEventArgs.ScreenPosition,
            View = mouseEventArgs.View,
            Position = mouseEventArgs.Position
        };
        Logging.Trace ($"Raising click event:{newClick.Flags} at screen {newClick.ScreenPosition}");

        return newClick;
    }

    private MouseFlags ToClicks (int buttonIdx, int numberOfClicks)
    {
        if (numberOfClicks == 0)
        {
            throw new ArgumentOutOfRangeException (nameof (numberOfClicks), "Zero clicks are not valid.");
        }

        return buttonIdx switch
               {
                   0 => numberOfClicks switch
                        {
                            1 => MouseFlags.Button1Clicked,
                            2 => MouseFlags.Button1DoubleClicked,
                            _ => MouseFlags.Button1TripleClicked
                        },
                   1 => numberOfClicks switch
                        {
                            1 => MouseFlags.Button2Clicked,
                            2 => MouseFlags.Button2DoubleClicked,
                            _ => MouseFlags.Button2TripleClicked
                        },
                   2 => numberOfClicks switch
                        {
                            1 => MouseFlags.Button3Clicked,
                            2 => MouseFlags.Button3DoubleClicked,
                            _ => MouseFlags.Button3TripleClicked
                        },
                   3 => numberOfClicks switch
                        {
                            1 => MouseFlags.Button4Clicked,
                            2 => MouseFlags.Button4DoubleClicked,
                            _ => MouseFlags.Button4TripleClicked
                        },
                   _ => throw new ArgumentOutOfRangeException (nameof (buttonIdx), "Unsupported button index")
               };
    }
}
