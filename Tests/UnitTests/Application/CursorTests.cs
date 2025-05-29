using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests;

public class CursorTests
{
    private readonly ITestOutputHelper _output;

    public CursorTests (ITestOutputHelper output)
    {
        _output = output;
        ConsoleDriver.RunningUnitTests = true;
    }

    private class TestView : View
    {
        public Point? TestLocation { get; set; }

        /// <inheritdoc />
        public override Point? PositionCursor ()
        {
            if (TestLocation.HasValue && HasFocus)
            {
                Driver.SetCursorVisibility (CursorVisibility.Default);
            }
            return TestLocation;
        }
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_No_Focus_Returns_False ()
    {
        Application.Navigation = new ();
        Application.Navigation.SetFocused (null);

        Assert.False (Application.PositionCursor ());

        TestView view = new ()
        {
            CanFocus = false,
            Width = 1,
            Height = 1,
        };
        view.TestLocation = new Point (0, 0);
        Assert.False (Application.PositionCursor ());
        Application.ResetState (true);

    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_No_Position_Returns_False ()
    {
        Application.Navigation = new ();
        TestView view = new ()
        {
            CanFocus = false,
            Width = 1,
            Height = 1,
        };

        view.CanFocus = true;
        view.SetFocus ();
        Assert.False (Application.PositionCursor ());
        Application.ResetState (true);
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_No_IntersectSuperView_Returns_False ()
    {
        Application.Navigation = new ();
        View superView = new ()
        {
            Width = 1,
            Height = 1,
        };

        TestView view = new ()
        {
            CanFocus = false,
            X = 1,
            Y = 1,
            Width = 1,
            Height = 1,
        };
        superView.Add (view);

        view.CanFocus = true;
        view.SetFocus ();
        view.TestLocation = new Point (0, 0);
        Assert.False (Application.PositionCursor ());
        Application.ResetState (true);
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_Position_OutSide_SuperView_Returns_False ()
    {
        Application.Navigation = new ();
        View superView = new ()
        {
            Width = 1,
            Height = 1,
        };

        TestView view = new ()
        {
            CanFocus = false,
            X = 0,
            Y = 0,
            Width = 2,
            Height = 2,
        };
        superView.Add (view);

        view.CanFocus = true;
        view.SetFocus ();
        view.TestLocation = new Point (1, 1);
        Assert.False (Application.PositionCursor ());
        Application.ResetState (true);
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_Focused_With_Position_Returns_True ()
    {
        Application.Navigation = new ();
        TestView view = new ()
        {
            CanFocus = false,
            Width = 1,
            Height = 1,
        };
        view.CanFocus = true;
        view.SetFocus ();
        view.TestLocation = new Point (0, 0);
        Assert.True (Application.PositionCursor ());
        Application.ResetState (true);
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_Defaults_Invisible ()
    {
        Application.Navigation = new ();
        View view = new ()
        {
            CanFocus = true,
            Width = 1,
            Height = 1,
        };
        view.SetFocus ();

        Assert.True (view.HasFocus);
        Assert.False (Application.PositionCursor ());

        if (Application.Driver?.GetCursorVisibility (out CursorVisibility cursor) ?? false)
        {
            Assert.Equal (CursorVisibility.Invisible, cursor);
        }

        Application.ResetState (true);
    }
}
