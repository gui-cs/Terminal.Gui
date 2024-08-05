﻿using Xunit.Abstractions;

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
        Assert.False (Application.PositionCursor (null));

        TestView view = new ()
        {
            CanFocus = false,
            Width = 1,
            Height = 1,
        };
        view.TestLocation = new Point (0, 0);
        Assert.False (Application.PositionCursor (view));
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_No_Position_Returns_False ()
    {
        TestView view = new ()
        {
            CanFocus = false,
            Width = 1,
            Height = 1,
        };

        view.CanFocus = true;
        view.SetFocus();
        Assert.False (Application.PositionCursor (view));
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_No_IntersectSuperView_Returns_False ()
    {
        View superView = new ()
        {
            Width = 1,
            Height = 1,
        };

        TestView view = new ()
        {
            CanFocus = false,
            X = 1,
            Y =1,
            Width = 1,
            Height = 1,
        };
        superView.Add (view);

        view.CanFocus = true;
        view.SetFocus ();
        view.TestLocation = new Point (0, 0);
        Assert.False (Application.PositionCursor (view));
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_Position_OutSide_SuperView_Returns_False ()
    {
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
        Assert.False (Application.PositionCursor (view));
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_Focused_With_Position_Returns_True ()
    {
        TestView view = new ()
        {
            CanFocus = false,
            Width = 1,
            Height = 1,
        };
        view.CanFocus = true;
        view.SetFocus ();
        view.TestLocation = new Point (0, 0);
        Assert.True (Application.PositionCursor (view));
    }

    [Fact]
    [SetupFakeDriver]
    public void PositionCursor_Defaults_Invisible ()
    {
        View view = new ()
        {
            CanFocus = true,
            Width = 1,
            Height = 1,
        };
        view.SetFocus();

        Assert.True (view.HasFocus);
        Assert.False (Application.PositionCursor (view));

        if (Application.Driver?.GetCursorVisibility (out CursorVisibility cursor) ?? false)
        {
            Assert.Equal (CursorVisibility.Invisible, cursor);
        }
    }
}
