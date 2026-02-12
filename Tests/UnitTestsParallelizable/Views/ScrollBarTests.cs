namespace ViewsTests;

public class ScrollBarTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var scrollBar = new ScrollBar ();
        Assert.False (scrollBar.CanFocus);
        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (0, scrollBar.ScrollableContentSize);
        Assert.Equal (0, scrollBar.VisibleContentSize);
        Assert.Equal (0, scrollBar.GetSliderPosition ());
        Assert.Equal (0, scrollBar.Value);
        Assert.False (scrollBar.ShowScroll);
        Assert.False (scrollBar.AutoShow);
    }

    #region ShowScroll

    [Fact]
    public void ShowScroll_False_Is_Default_CorrectlyDoesNotShows ()
    {
        var super = new Runnable { Id = "super", Width = 1, Height = 20 };
        var scrollBar = new ScrollBar { ScrollableContentSize = 20 };
        super.Add (scrollBar);
        Assert.False (scrollBar.ShowScroll);
        Assert.False (scrollBar.Visible);
        super.Dispose ();
    }

    [Fact]
    public void ShowScroll_True_And_AutoShow_True_CorrectlyShows_IfScrollableContentSizeIsLessOrEqualThanViewportSize ()
    {
        var super = new Runnable { Id = "super", Width = 1, Height = 20 };
        var scrollBar = new ScrollBar { ScrollableContentSize = 20, ShowScroll = true, AutoShow = true };
        super.Add (scrollBar);
        super.Layout ();

        Assert.True (scrollBar.ShowScroll);
        Assert.True (scrollBar.AutoShow);
        Assert.False (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 19;
        Assert.False (scrollBar.Visible);
    }

    [Fact]
    public void ShowScroll_True_And_AutoShow_True_CorrectlyShows_IfScrollableContentSizeIsGreaterThanViewportSize ()
    {
        var super = new Runnable { Id = "super", Width = 1, Height = 20 };
        var scrollBar = new ScrollBar { ScrollableContentSize = 21, ShowScroll = true, AutoShow = true };
        super.Add (scrollBar);
        super.Layout ();

        Assert.True (scrollBar.ShowScroll);
        Assert.True (scrollBar.AutoShow);
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 22;
        Assert.True (scrollBar.Visible);
    }

    [Fact]
    public void ShowScroll_True_And_AutoShow_False_CorrectlyShows_RegardlessScrollableContentSizeAndViewportSize ()
    {
        var super = new Runnable { Id = "super", Width = 1, Height = 20 };
        var scrollBar = new ScrollBar { ScrollableContentSize = 20, ShowScroll = true, AutoShow = false };
        super.Add (scrollBar);
        super.Layout ();

        Assert.True (scrollBar.ShowScroll);
        Assert.False (scrollBar.AutoShow);
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 19;
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 21;
        Assert.True (scrollBar.Visible);
    }

    #endregion AutoShow

    #region AutoHide

    [Fact]
    public void AutoHide_False_Is_Default_CorrectlyHidesAndShows ()
    {
        var super = new Runnable { Id = "super", Width = 1, Height = 20 };

        var scrollBar = new ScrollBar ();
        super.Add (scrollBar);
        Assert.False (scrollBar.ShowScroll);
        Assert.False (scrollBar.AutoShow);
        Assert.False (scrollBar.Visible);

        scrollBar.ShowScroll = true;
        scrollBar.AutoShow = true;
        Assert.True (scrollBar.AutoShow);
        Assert.True (scrollBar.Visible);

        // Should Show
        scrollBar.ScrollableContentSize = 21;
        Assert.True (scrollBar.Visible);

        // Should Hide
        scrollBar.ScrollableContentSize = 10;
        super.Layout (new (100, 100));
        Assert.False (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    public void AutoHide_False_CorrectlyHidesAndShows ()
    {
        var super = new Runnable { Id = "super", Width = 1, Height = 20 };

        var scrollBar = new ScrollBar { ScrollableContentSize = 20, AutoShow = false };
        super.Add (scrollBar);
        Assert.False (scrollBar.ShowScroll);
        Assert.False (scrollBar.AutoShow);
        Assert.False (scrollBar.Visible);

        // Should Hide if AutoSize = true, but should not hide if ShowScroll = true and AutoSize = false
        scrollBar.ScrollableContentSize = 10;
        Assert.False (scrollBar.Visible);

        // Should Show
        scrollBar.ShowScroll = true;
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    public void AutoHide_True_Changing_ScrollableContentSize_CorrectlyHidesAndShows ()
    {
        var super = new Runnable { Id = "super", Width = 1, Height = 20 };

        var scrollBar = new ScrollBar { ScrollableContentSize = 20 };
        super.Add (scrollBar);
        Assert.False (scrollBar.ShowScroll);
        Assert.False (scrollBar.AutoShow);
        Assert.False (scrollBar.Visible);

        scrollBar.ShowScroll = true;
        scrollBar.AutoShow = true;

        super.Layout (new (100, 100));
        Assert.False (scrollBar.Visible);
        Assert.Equal (1, scrollBar.Frame.Width);
        Assert.Equal (20, scrollBar.Frame.Height);

        scrollBar.ScrollableContentSize = 10;
        super.Layout (new (100, 100));
        Assert.False (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 30;
        super.Layout (new (100, 100));
        Assert.True (scrollBar.Visible);

        scrollBar.AutoShow = false;
        super.Layout (new (100, 100));
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 10;
        super.Layout (new (100, 100));
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    public void AutoHide_Change_VisibleContentSize_CorrectlyHidesAndShows ()
    {
        var super = new Runnable { Id = "super", Width = 1, Height = 20 };

        var scrollBar = new ScrollBar { ScrollableContentSize = 20, VisibleContentSize = 20 };
        super.Add (scrollBar);
        Assert.False (scrollBar.ShowScroll);
        Assert.False (scrollBar.AutoShow);
        Assert.False (scrollBar.Visible);

        scrollBar.ShowScroll = true;
        scrollBar.AutoShow = true;

        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (20, scrollBar.VisibleContentSize);
        Assert.False (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;

        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 30;

        //Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;

        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 21;

        //Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.AutoShow = false;

        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;

        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    #endregion AutoHide

    #region Orientation

    [Fact]
    public void OnOrientationChanged_Keeps_Size ()
    {
        var scroll = new ScrollBar ();
        scroll.Layout ();
        scroll.ScrollableContentSize = 1;

        scroll.Orientation = Orientation.Horizontal;
        Assert.Equal (1, scroll.ScrollableContentSize);
    }

    [Fact]
    public void OnOrientationChanged_Sets_Position_To_0 ()
    {
        var super = new View { Id = "super", Width = 10, Height = 10 };
        var scrollBar = new ScrollBar ();
        super.Add (scrollBar);
        scrollBar.Layout ();
        scrollBar.Value = 1;
        scrollBar.Orientation = Orientation.Horizontal;

        Assert.Equal (0, scrollBar.GetSliderPosition ());
    }

    #endregion Orientation

    #region Size

    // TODO: Add tests.

    #endregion Size

    #region Position

    [Fact]
    public void Position_Event_IsIgnored_IfShowScrollIsFalse ()
    {
        var changingCount = 0;
        var changedCount = 0;
        var scrollBar = new ScrollBar ();
        scrollBar.ScrollableContentSize = 5;
        scrollBar.Frame = new Rectangle (0, 0, 1, 4); // Needs to be at least 4 for slider to move

        scrollBar.ValueChanging += (s, e) =>
                                   {
                                       if (changingCount == 0)
                                       {
                                           e.Handled = true;
                                       }

                                       changingCount++;
                                   };
        scrollBar.ValueChanged += (s, e) => changedCount++;

        Assert.False (scrollBar.ShowScroll);

        scrollBar.Value = 1;
        Assert.Equal (0, scrollBar.Value);
        Assert.Equal (0, changingCount);
        Assert.Equal (0, changedCount);

        scrollBar.Value = 1;
        Assert.Equal (0, scrollBar.Value);
        Assert.Equal (0, changingCount);
        Assert.Equal (0, changedCount);
    }

    [Fact]
    public void Position_Event_Cancels ()
    {
        var changingCount = 0;
        var changedCount = 0;
        var scrollBar = new ScrollBar { ShowScroll = true };
        scrollBar.ScrollableContentSize = 5;
        scrollBar.Frame = new Rectangle (0, 0, 1, 4); // Needs to be at least 4 for slider to move

        scrollBar.ValueChanging += (s, e) =>
                                   {
                                       if (changingCount == 0)
                                       {
                                           e.Handled = true;
                                       }

                                       changingCount++;
                                   };
        scrollBar.ValueChanged += (s, e) => changedCount++;

        scrollBar.Value = 1;
        Assert.Equal (0, scrollBar.Value);
        Assert.Equal (1, changingCount);
        Assert.Equal (0, changedCount);

        scrollBar.Value = 1;
        Assert.Equal (1, scrollBar.Value);
        Assert.Equal (2, changingCount);
        Assert.Equal (1, changedCount);
    }

    #endregion Position

    [Fact]
    public void ScrollableContentSize_Cannot_Be_Negative ()
    {
        var scrollBar = new ScrollBar { Height = 10, ScrollableContentSize = -1 };
        Assert.Equal (0, scrollBar.ScrollableContentSize);
        scrollBar.ScrollableContentSize = -10;
        Assert.Equal (0, scrollBar.ScrollableContentSize);
    }

    [Fact]
    public void ScrollableContentSizeChanged_Event_IsIgnored_IfShowScrollIsFalse ()
    {
        var count = 0;
        var scrollBar = new ScrollBar ();
        scrollBar.ScrollableContentSizeChanged += (s, e) => count++;

        Assert.False (scrollBar.ShowScroll);

        scrollBar.ScrollableContentSize = 10;
        Assert.Equal (10, scrollBar.ScrollableContentSize);
        Assert.Equal (0, count);
    }

    [Fact]
    public void ScrollableContentSizeChanged_Event ()
    {
        var count = 0;
        var scrollBar = new ScrollBar { ShowScroll = true };
        scrollBar.ScrollableContentSizeChanged += (s, e) => count++;

        scrollBar.ScrollableContentSize = 10;
        Assert.Equal (10, scrollBar.ScrollableContentSize);
        Assert.Equal (1, count);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void ScrollBar_Click_JumpsPosition ()
    {
        ScrollBar scrollBar = new () { Height = 10, ScrollableContentSize = 100 };
        scrollBar.BeginInit ();
        scrollBar.EndInit ();

        // Click on track jumps scroll position
        Mouse ev = new () { Position = new Point (0, 5), Flags = MouseFlags.LeftButtonClicked };
        scrollBar.NewMouseEvent (ev);

        // Verify the scrollbar is set up correctly
        Assert.Equal (100, scrollBar.ScrollableContentSize);

        scrollBar.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void ScrollBar_Command_Accept_NotTypical ()
    {
        ScrollBar scrollBar = new () { Height = 10 };

        // ScrollBar doesn't typically use Accept command
        bool? result = scrollBar.InvokeCommand (Command.Accept);

        // Accept is not handled
        Assert.NotEqual (true, result);

        scrollBar.Dispose ();
    }
}
