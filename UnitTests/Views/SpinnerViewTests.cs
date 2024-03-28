using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class SpinnerViewTests
{
    private readonly ITestOutputHelper output;
    public SpinnerViewTests (ITestOutputHelper output) { this.output = output; }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void TestSpinnerView_AutoSpin (bool callStop)
    {
        SpinnerView view = GetSpinnerView ();

        Assert.Empty (Application.MainLoop._timeouts);
        view.AutoSpin = true;
        Assert.NotEmpty (Application.MainLoop._timeouts);
        Assert.True (view.AutoSpin);

        //More calls to AutoSpin do not add more timeouts
        Assert.Single (Application.MainLoop._timeouts);
        view.AutoSpin = true;
        view.AutoSpin = true;
        view.AutoSpin = true;
        Assert.True (view.AutoSpin);
        Assert.Single (Application.MainLoop._timeouts);

        if (callStop)
        {
            view.AutoSpin = false;
            Assert.Empty (Application.MainLoop._timeouts);
            Assert.False (view.AutoSpin);
        }
        else
        {
            Assert.NotEmpty (Application.MainLoop._timeouts);
        }

        // Dispose clears timeout
        view.Dispose ();
        Assert.Empty (Application.MainLoop._timeouts);
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
        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.AdvanceAnimation ();
        view.Draw ();

        expected = "/";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestSpinnerView_ThrottlesAnimation ()
    {
        SpinnerView view = GetSpinnerView ();
        view.Draw ();

        var expected = @"\";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.AdvanceAnimation ();
        view.Draw ();

        expected = @"\";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.AdvanceAnimation ();
        view.Draw ();

        expected = @"\";
        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        // BUGBUG: Disabled due to xunit error
        //Task.Delay (400).Wait ();

        //view.AdvanceAnimation ();
        //view.Draw ();

        //expected = "|";
        //TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }

    private SpinnerView GetSpinnerView ()
    {
        var view = new SpinnerView ();

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Assert.Equal (1, view.Width);
        Assert.Equal (1, view.Height);

        return view;
    }
}
