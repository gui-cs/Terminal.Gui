#nullable enable
using Xunit.Abstractions;

namespace ApplicationTests.Timeout;

/// <summary>
///     Tests for timeout behavior with nested Application.Run() calls.
///     These tests verify that timeouts scheduled in a parent run loop continue to fire
///     correctly when a nested modal dialog is shown via Application.Run().
/// </summary>
public class TimeoutTests (ITestOutputHelper output)
{
    [Fact]
    public void AddTimeout_Fires ()
    {
        using IApplication app = Application.Create ();
        app.Init ("fake");

        uint timeoutTime = 100;
        var timeoutFired = false;

        // Setup a timeout that will fire
        app.AddTimeout (
                        TimeSpan.FromMilliseconds (timeoutTime),
                        () =>
                        {
                            timeoutFired = true;

                            // Return false so the timer does not repeat
                            return false;
                        }
                       );

        // The timeout has not fired yet
        Assert.False (timeoutFired);

        // Block the thread to prove the timeout does not fire on a background thread
        Thread.Sleep ((int)timeoutTime * 2);
        Assert.False (timeoutFired);

        app.StopAfterFirstIteration = true;
        app.Run<Runnable> ();

        // The timeout should have fired
        Assert.True (timeoutFired);
    }

    [Fact]
    public void AddTimeout_TimeSpan_Zero_Fires ()
    {
        using IApplication app = Application.Create ();
        app.Init ("fake");
        var timeoutFired = false;

        app.AddTimeout (TimeSpan.Zero, () =>
                                       {
                                           timeoutFired = true;
                                           return false;
                                       });

        app.StopAfterFirstIteration = true;
        app.Run<Runnable> ();

        Assert.True (timeoutFired);
    }
}
