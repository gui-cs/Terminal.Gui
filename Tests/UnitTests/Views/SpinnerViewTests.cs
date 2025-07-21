using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class SpinnerViewTests (ITestOutputHelper output)
{
    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void TestSpinnerView_AutoSpin (bool callStop)
    {
        ConsoleDriver.RunningUnitTests = true;

        SpinnerView view = GetSpinnerView ();

        Assert.Empty (Application.MainLoop.TimedEvents.Timeouts);
        view.AutoSpin = true;
        Assert.NotEmpty (Application.MainLoop.TimedEvents.Timeouts);
        Assert.True (view.AutoSpin);

        //More calls to AutoSpin do not add more timeouts
        Assert.Single (Application.MainLoop.TimedEvents.Timeouts);
        view.AutoSpin = true;
        view.AutoSpin = true;
        view.AutoSpin = true;
        Assert.True (view.AutoSpin);
        Assert.Single (Application.MainLoop.TimedEvents.Timeouts);

        if (callStop)
        {
            view.AutoSpin = false;
            Assert.Empty (Application.MainLoop.TimedEvents.Timeouts);
            Assert.False (view.AutoSpin);
        }
        else
        {
            Assert.NotEmpty (Application.MainLoop.TimedEvents.Timeouts);
        }

        // Dispose clears timeout
        view.Dispose ();
        Assert.Empty (Application.MainLoop.TimedEvents.Timeouts);
        Application.Top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestSpinnerView_NoThrottle ()
    {
        SpinnerView view = GetSpinnerView ();
        view.SpinDelay = 0;

        view.AdvanceAnimation ();
        view.Draw ();

        var expected = "|";
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        view.AdvanceAnimation ();
        View.SetClipToScreen ();
        view.Draw ();

        expected = "/";
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Application.Top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestSpinnerView_ThrottlesAnimation ()
    {
        SpinnerView view = GetSpinnerView ();
        view.Draw ();

        var expected = @"\";
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        view.AdvanceAnimation ();
        view.Draw ();

        expected = @"\";
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        view.AdvanceAnimation ();
        view.Draw ();

        expected = @"\";
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        // BUGBUG: Disabled due to xunit error
        //Task.Delay (400).Wait ();

        //view.AdvanceAnimation ();
        //view.Draw ();

        //expected = "|";
        //DriverAsserts.AssertDriverContentsWithFrameAre (expected, output);
        Application.Top.Dispose ();
    }

    private SpinnerView GetSpinnerView ()
    {
        var view = new SpinnerView ();

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        // Required to clear the initial 'Invoke nothing' that Begin does
        Application.MainLoop.TimedEvents.Timeouts.Clear ();

        Assert.Equal (1, view.Frame.Width);
        Assert.Equal (1, view.Frame.Height);

        return view;
    }
}
