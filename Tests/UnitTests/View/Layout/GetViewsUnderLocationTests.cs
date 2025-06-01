#nullable enable

namespace Terminal.Gui.ViewMouseTests;

[Trait ("Category", "Input")]
public class GetViewsUnderLocationTests
{
    [Theory]
    [InlineData (0, 0, 0, 0, 0, -1, -1, new string [] { })]
    [InlineData (0, 0, 0, 0, 0, 0, 0, new [] { "Top" })]
    [InlineData (0, 0, 0, 0, 0, 1, 1, new [] { "Top" })]
    [InlineData (0, 0, 0, 0, 0, 4, 4, new [] { "Top" })]
    [InlineData (0, 0, 0, 0, 0, 9, 9, new [] { "Top" })]
    [InlineData (0, 0, 0, 0, 0, 10, 10, new string [] { })]
    [InlineData (1, 1, 0, 0, 0, -1, -1, new string [] { })]
    [InlineData (1, 1, 0, 0, 0, 0, 0, new string [] { })]
    [InlineData (1, 1, 0, 0, 0, 1, 1, new [] { "Top" })]
    [InlineData (1, 1, 0, 0, 0, 4, 4, new [] { "Top" })]
    [InlineData (1, 1, 0, 0, 0, 9, 9, new [] { "Top" })]
    [InlineData (1, 1, 0, 0, 0, 10, 10, new [] { "Top" })]
    [InlineData (0, 0, 1, 0, 0, -1, -1, new string [] { })]
    [InlineData (0, 0, 1, 0, 0, 0, 0, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 0, 0, 1, 1, new [] { "Top" })]
    [InlineData (0, 0, 1, 0, 0, 4, 4, new [] { "Top" })]
    [InlineData (0, 0, 1, 0, 0, 9, 9, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 0, 0, 10, 10, new string [] { })]
    [InlineData (0, 0, 1, 1, 0, -1, -1, new string [] { })]
    [InlineData (0, 0, 1, 1, 0, 0, 0, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 1, 0, 1, 1, new [] { "Top", "Border" })]
    [InlineData (0, 0, 1, 1, 0, 4, 4, new [] { "Top" })]
    [InlineData (0, 0, 1, 1, 0, 9, 9, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 1, 0, 10, 10, new string [] { })]
    [InlineData (0, 0, 1, 1, 1, -1, -1, new string [] { })]
    [InlineData (0, 0, 1, 1, 1, 0, 0, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 1, 1, 1, 1, new [] { "Top", "Border" })]
    [InlineData (0, 0, 1, 1, 1, 2, 2, new [] { "Top", "Padding" })]
    [InlineData (0, 0, 1, 1, 1, 4, 4, new [] { "Top" })]
    [InlineData (0, 0, 1, 1, 1, 9, 9, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (0, 0, 1, 1, 1, 10, 10, new string [] { })]
    [InlineData (1, 1, 1, 0, 0, -1, -1, new string [] { })]
    [InlineData (1, 1, 1, 0, 0, 0, 0, new string [] { })]
    [InlineData (1, 1, 1, 0, 0, 1, 1, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 0, 0, 4, 4, new [] { "Top" })]
    [InlineData (1, 1, 1, 0, 0, 9, 9, new [] { "Top" })]
    [InlineData (1, 1, 1, 0, 0, 10, 10, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 1, 0, -1, -1, new string [] { })]
    [InlineData (1, 1, 1, 1, 0, 0, 0, new string [] { })]
    [InlineData (1, 1, 1, 1, 0, 1, 1, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 1, 0, 4, 4, new [] { "Top" })]
    [InlineData (1, 1, 1, 1, 0, 9, 9, new [] { "Top", "Border" })]
    [InlineData (1, 1, 1, 1, 0, 10, 10, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 1, 1, -1, -1, new string [] { })]
    [InlineData (1, 1, 1, 1, 1, 0, 0, new string [] { })]
    [InlineData (1, 1, 1, 1, 1, 1, 1, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    [InlineData (1, 1, 1, 1, 1, 2, 2, new [] { "Top", "Border" })]
    [InlineData (1, 1, 1, 1, 1, 3, 3, new [] { "Top", "Padding" })]
    [InlineData (1, 1, 1, 1, 1, 4, 4, new [] { "Top" })]
    [InlineData (1, 1, 1, 1, 1, 8, 8, new [] { "Top", "Padding" })]
    [InlineData (1, 1, 1, 1, 1, 9, 9, new [] { "Top", "Border" })]
    [InlineData (1, 1, 1, 1, 1, 10, 10, new string [] { })] //margin is ViewportSettings.TransparentToMouse
    public void Top_Adornments_Returns_Correct_View (
        int frameX,
        int frameY,
        int marginThickness,
        int borderThickness,
        int paddingThickness,
        int testX,
        int testY,
        string [] expectedViewsFound
    )
    {
        // Arrange
        Application.Top = new ()
        {
            Id = "Top",
            Frame = new (frameX, frameY, 10, 10)
        };
        Application.Top.Margin!.Thickness = new (marginThickness);
        Application.Top.Margin.Id = "Margin";
        Application.Top.Border!.Thickness = new (borderThickness);
        Application.Top.Border.Id = "Border";
        Application.Top.Padding!.Thickness = new (paddingThickness);
        Application.Top.Padding.Id = "Padding";

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = View.GetViewsUnderLocation (location, ViewportSettingsFlags.TransparentMouse);

        // Assert
        if (expectedViewsFound.Length == 0)
        {
            Assert.Empty (viewsUnderMouse);
        }
        else
        {
            string [] foundIds = viewsUnderMouse.Select (v => v!.Id).ToArray ();
            Assert.Equal (expectedViewsFound, foundIds);
        }

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (2, 2)]
    public void Returns_Top_If_No_SubViews (int testX, int testY)
    {
        // Arrange
        Application.Top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = View.GetViewsUnderLocation (location, ViewportSettingsFlags.TransparentMouse);

        // Assert
        Assert.Contains (viewsUnderMouse, v => v == Application.Top);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderLocation returns the correct view if the start view has no subviews
    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (2, 2)]
    public void Returns_Start_If_No_SubViews (int testX, int testY)
    {
        Application.ResetState (true);

        Application.Top = new ()
        {
            Width = 10, Height = 10
        };

        Assert.Same (Application.Top, View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ());
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderLocation returns the correct view if the start view has subviews
    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, true)]
    [InlineData (5, 6, true)]
    public void Returns_Correct_If_SubViews (int testX, int testY, bool expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };

        var subview = new View
        {
            X = 1, Y = 2,
            Width = 5, Height = 5
        };
        Application.Top.Add (subview);

        View? found = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, false)]
    [InlineData (5, 6, false)]
    public void Returns_Null_If_SubView_NotVisible (int testX, int testY, bool expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };

        var subview = new View
        {
            X = 1, Y = 2,
            Width = 5, Height = 5,
            Visible = false
        };
        Application.Top.Add (subview);

        View? found = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, false)]
    [InlineData (5, 6, false)]
    public void Returns_Null_If_Not_Visible_And_SubView_Visible (int testX, int testY, bool expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10,
            Visible = false
        };

        var subview = new View
        {
            X = 1, Y = 2,
            Width = 5, Height = 5
        };
        Application.Top.Add (subview);
        subview.Visible = true;
        Assert.True (subview.Visible);
        Assert.False (Application.Top.Visible);
        View? found = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderLocation works if the start view has positive Adornments
    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (7, 8, false)]
    [InlineData (1, 2, false)]
    [InlineData (2, 3, true)]
    [InlineData (5, 6, true)]
    [InlineData (6, 7, true)]
    public void Returns_Correct_If_Start_Has_Adornments (int testX, int testY, bool expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };
        Application.Top.Margin!.Thickness = new (1);

        var subview = new View
        {
            X = 1, Y = 2,
            Width = 5, Height = 5
        };
        Application.Top.Add (subview);

        View? found = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderLocation works if the start view has offset Viewport location
    [Theory]
    [InlineData (1, 0, 0, true)]
    [InlineData (1, 1, 1, true)]
    [InlineData (1, 2, 2, false)]
    [InlineData (-1, 3, 3, true)]
    [InlineData (-1, 2, 2, true)]
    [InlineData (-1, 1, 1, false)]
    [InlineData (-1, 0, 0, false)]
    public void Returns_Correct_If_Start_Has_Offset_Viewport (int offset, int testX, int testY, bool expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10,
            ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation
        };
        Application.Top.Viewport = new (offset, offset, 10, 10);

        var subview = new View
        {
            X = 1, Y = 1,
            Width = 2, Height = 2
        };
        Application.Top.Add (subview);

        View? found = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (9, 9, true)]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (10, 10, false)]
    [InlineData (7, 8, false)]
    [InlineData (1, 2, false)]
    [InlineData (2, 3, false)]
    [InlineData (5, 6, false)]
    [InlineData (6, 7, false)]
    public void Returns_Correct_If_Start_Has_Adornment_WithSubView (int testX, int testY, bool expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };
        Application.Top.Padding!.Thickness = new (1);

        var subview = new View
        {
            X = Pos.AnchorEnd (1), Y = Pos.AnchorEnd (1),
            Width = 1, Height = 1
        };
        Application.Top.Padding.Add (subview);
        Application.Top.BeginInit ();
        Application.Top.EndInit ();

        View? found = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0, new string [] { })]
    [InlineData (9, 9, new string [] { })]
    [InlineData (1, 1, new [] { "Top", "Border" })]
    [InlineData (8, 8, new [] { "Top", "Border" })]
    [InlineData (2, 2, new [] { "Top", "Padding" })]
    [InlineData (7, 7, new [] { "Top", "Padding" })]
    [InlineData (5, 5, new [] { "Top" })]
    public void Returns_Adornment_If_Start_Has_Adornments (int testX, int testY, string [] expectedViewsFound)
    {
        Application.ResetState (true);

        Application.Top = new ()
        {
            Id = "Top",
            Width = 10, Height = 10
        };
        Application.Top.Margin!.Thickness = new (1);
        Application.Top.Margin.Id = "Margin";
        Application.Top.Border!.Thickness = new (1);
        Application.Top.Border.Id = "Border";
        Application.Top.Padding!.Thickness = new (1);
        Application.Top.Padding.Id = "Padding";

        var subview = new View
        {
            Id = "SubView",
            X = 1, Y = 1,
            Width = 1, Height = 1
        };
        Application.Top.Add (subview);

        List<View?> viewsUnderMouse = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse);
        string [] foundIds = viewsUnderMouse.Select (v => v!.Id).ToArray ();

        Assert.Equal (expectedViewsFound, foundIds);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderLocation works if the subview has positive Adornments
    [Theory]
    [InlineData (0, 0, new [] { "Top" })]
    [InlineData (1, 1, new [] { "Top" })]
    [InlineData (9, 9, new [] { "Top" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (7, 8, new [] { "Top" })]
    [InlineData (6, 7, new [] { "Top" })]
    [InlineData (1, 2, new [] { "Top", "subview", "border" })]
    [InlineData (5, 6, new [] { "Top", "subview", "border" })]
    [InlineData (2, 3, new [] { "Top", "subview" })]
    public void Returns_Correct_If_SubView_Has_Adornments (int testX, int testY, string [] expectedViewsFound)
    {
        Application.Top = new ()
        {
            Id = "Top",
            Width = 10, Height = 10
        };

        var subview = new View
        {
            Id = "subview",
            X = 1, Y = 2,
            Width = 5, Height = 5
        };
        subview.Border!.Thickness = new (1);
        subview.Border.Id = "border";
        Application.Top.Add (subview);

        List<View?> viewsUnderMouse = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse);
        string [] foundIds = viewsUnderMouse.Select (v => v!.Id).ToArray ();

        Assert.Equal (expectedViewsFound, foundIds);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderLocation works if the subview has positive Adornments
    [Theory]
    [InlineData (0, 0, new [] { "Top" })]
    [InlineData (1, 1, new [] { "Top" })]
    [InlineData (9, 9, new [] { "Top" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (7, 8, new [] { "Top" })]
    [InlineData (6, 7, new [] { "Top" })]
    [InlineData (1, 2, new [] { "Top" })]
    [InlineData (5, 6, new [] { "Top" })]
    [InlineData (2, 3, new [] { "Top", "subview" })]
    public void Returns_Correct_If_SubView_Has_Adornments_With_TransparentMouse (int testX, int testY, string [] expectedViewsFound)
    {
        Application.Top = new ()
        {
            Id = "Top",
            Width = 10, Height = 10
        };

        var subview = new View
        {
            Id = "subview",
            X = 1, Y = 2,
            Width = 5, Height = 5
        };
        subview.Border!.Thickness = new (1);
        subview.Border.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        subview.Border.Id = "border";
        Application.Top.Add (subview);

        List<View?> viewsUnderMouse = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse);
        string [] foundIds = viewsUnderMouse.Select (v => v!.Id).ToArray ();

        Assert.Equal (expectedViewsFound, foundIds);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (7, 8, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, false)]
    [InlineData (5, 6, false)]
    [InlineData (6, 5, false)]
    [InlineData (5, 5, true)]
    public void Returns_Correct_If_SubView_Has_Adornment_WithSubView (int testX, int testY, bool expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };

        // A subview with + Padding
        var subview = new View
        {
            X = 1, Y = 1,
            Width = 5, Height = 5
        };
        subview.Padding!.Thickness = new (1);

        // This subview will be at the bottom-right-corner of subview
        // So screen-relative location will be X + Width - 1 = 5
        var paddingSubView = new View
        {
            X = Pos.AnchorEnd (1),
            Y = Pos.AnchorEnd (1),
            Width = 1,
            Height = 1
        };
        subview.Padding.Add (paddingSubView);
        Application.Top.Add (subview);
        Application.Top.BeginInit ();
        Application.Top.EndInit ();

        View? found = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == paddingSubView);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (7, 8, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, false)]
    [InlineData (5, 6, false)]
    [InlineData (6, 5, false)]
    [InlineData (5, 5, true)]
    public void Returns_Correct_If_SubView_Is_Scrolled_And_Has_Adornment_WithSubView (int testX, int testY, bool expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };

        // A subview with + Padding
        var subview = new View
        {
            X = 1, Y = 1,
            Width = 5, Height = 5
        };
        subview.Padding!.Thickness = new (1);

        // Scroll the subview
        subview.SetContentSize (new (10, 10));
        subview.Viewport = subview.Viewport with { Location = new (1, 1) };

        // This subview will be at the bottom-right-corner of subview
        // So screen-relative location will be X + Width - 1 = 5
        var paddingSubView = new View
        {
            X = Pos.AnchorEnd (1),
            Y = Pos.AnchorEnd (1),
            Width = 1,
            Height = 1
        };
        subview.Padding.Add (paddingSubView);
        Application.Top.Add (subview);
        Application.Top.BeginInit ();
        Application.Top.EndInit ();

        View? found = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == paddingSubView);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderLocation works with nested subviews
    [Theory]
    [InlineData (0, 0, -1)]
    [InlineData (9, 9, -1)]
    [InlineData (10, 10, -1)]
    [InlineData (1, 1, 0)]
    [InlineData (1, 2, 0)]
    [InlineData (2, 2, 1)]
    [InlineData (3, 3, 2)]
    [InlineData (5, 5, 2)]
    public void Returns_Correct_With_NestedSubViews (int testX, int testY, int expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };

        var numSubViews = 3;
        List<View> subviews = new ();

        for (var i = 0; i < numSubViews; i++)
        {
            var subview = new View
            {
                X = 1, Y = 1,
                Width = 5, Height = 5
            };
            subviews.Add (subview);

            if (i > 0)
            {
                subviews [i - 1].Add (subview);
            }
        }

        Application.Top.Add (subviews [0]);

        View? found = View.GetViewsUnderLocation (new (testX, testY), ViewportSettingsFlags.TransparentMouse).LastOrDefault ();
        Assert.Equal (expectedSubViewFound, subviews.IndexOf (found!));
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0, new [] { "top" })]
    [InlineData (9, 9, new [] { "top" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (1, 1, new [] { "top", "view" })]
    [InlineData (1, 2, new [] { "top", "view" })]
    [InlineData (2, 1, new [] { "top", "view" })]
    [InlineData (2, 2, new [] { "top", "view", "subView" })]
    [InlineData (3, 3, new [] { "top" })] // clipped
    [InlineData (2, 3, new [] { "top" })] // clipped
    public void Tiled_SubViews (int mouseX, int mouseY, string [] viewIdStrings)
    {
        // Arrange
        Application.Top = new ()
        {
            Frame = new (0, 0, 10, 10),
            Id = "top"
        };

        var view = new View
        {
            Id = "view",
            X = 1,
            Y = 1,
            Width = 2,
            Height = 2,
            Arrangement = ViewArrangement.Overlapped
        }; // at 1,1 to 3,2 (screen)

        var subView = new View
        {
            Id = "subView",
            X = 1,
            Y = 1,
            Width = 2,
            Height = 2,
            Arrangement = ViewArrangement.Overlapped
        }; // at 2,2 to 4,3 (screen)
        view.Add (subView);
        Application.Top.Add (view);

        List<View?> found = View.GetViewsUnderLocation (new (mouseX, mouseY), ViewportSettingsFlags.TransparentMouse);

        string [] foundIds = found.Select (v => v!.Id).ToArray ();

        Assert.Equal (viewIdStrings, foundIds);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0, new [] { "top" })]
    [InlineData (9, 9, new [] { "top" })]
    [InlineData (10, 10, new string [] { })]
    [InlineData (-1, -1, new string [] { })]
    [InlineData (1, 1, new [] { "top", "view" })]
    [InlineData (1, 2, new [] { "top", "view" })]
    [InlineData (2, 1, new [] { "top", "view" })]
    [InlineData (2, 2, new [] { "top", "view", "popover" })]
    [InlineData (3, 3, new [] { "top" })] // clipped
    [InlineData (2, 3, new [] { "top" })] // clipped
    public void Popover (int mouseX, int mouseY, string [] viewIdStrings)
    {
        // Arrange
        Application.Top = new ()
        {
            Frame = new (0, 0, 10, 10),
            Id = "top"
        };

        var view = new View
        {
            Id = "view",
            X = 1,
            Y = 1,
            Width = 2,
            Height = 2,
            Arrangement = ViewArrangement.Overlapped
        }; // at 1,1 to 3,2 (screen)

        var popOver = new View
        {
            Id = "popover",
            X = 1,
            Y = 1,
            Width = 2,
            Height = 2,
            Arrangement = ViewArrangement.Overlapped
        }; // at 2,2 to 4,3 (screen)

        view.Add (popOver);
        Application.Top.Add (view);

        List<View?> found = View.GetViewsUnderLocation (new (mouseX, mouseY), ViewportSettingsFlags.TransparentMouse);

        string [] foundIds = found.Select (v => v!.Id).ToArray ();

        Assert.Equal (viewIdStrings, foundIds);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    public void Returns_TopToplevel_When_Point_Inside_Only_TopToplevel ()
    {
        Application.ResetState (true);

        Toplevel topToplevel = new ()
        {
            Id = "topToplevel",
            Frame = new (0, 0, 20, 20)
        };

        Toplevel secondaryToplevel = new ()
        {
            Id = "secondaryToplevel",
            Frame = new (5, 5, 10, 10)
        };
        secondaryToplevel.Margin!.Thickness = new (1);
        secondaryToplevel.Layout ();

        Application.TopLevels.Clear ();
        Application.TopLevels.Push (topToplevel);
        Application.TopLevels.Push (secondaryToplevel);
        Application.Top = secondaryToplevel;

        List<View?> found = View.GetViewsUnderLocation (new (2, 2), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (found, v => v?.Id == topToplevel.Id);
        Assert.Contains (found, v => v == topToplevel);

        topToplevel.Dispose ();
        secondaryToplevel.Dispose ();
        Application.TopLevels.Clear ();
        Application.ResetState (true);
    }

    [Fact]
    public void Returns_SecondaryToplevel_When_Point_Inside_Only_SecondaryToplevel ()
    {
        Application.ResetState (true);

        Toplevel topToplevel = new ()
        {
            Id = "topToplevel",
            Frame = new (0, 0, 20, 20)
        };

        Toplevel secondaryToplevel = new ()
        {
            Id = "secondaryToplevel",
            Frame = new (5, 5, 10, 10)
        };
        secondaryToplevel.Margin!.Thickness = new (1);
        secondaryToplevel.Layout ();

        Application.TopLevels.Clear ();
        Application.TopLevels.Push (topToplevel);
        Application.TopLevels.Push (secondaryToplevel);
        Application.Top = secondaryToplevel;

        List<View?> found = View.GetViewsUnderLocation (new (7, 7), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (found, v => v?.Id == secondaryToplevel.Id);
        Assert.DoesNotContain (found, v => v?.Id == topToplevel.Id);

        topToplevel.Dispose ();
        secondaryToplevel.Dispose ();
        Application.TopLevels.Clear ();
        Application.ResetState (true);
    }

    [Fact]
    public void Returns_Depends_On_Margin_ViewportSettings_When_Point_In_Margin_Of_SecondaryToplevel ()
    {
        Application.ResetState (true);

        Toplevel topToplevel = new ()
        {
            Id = "topToplevel",
            Frame = new (0, 0, 20, 20)
        };

        Toplevel secondaryToplevel = new ()
        {
            Id = "secondaryToplevel",
            Frame = new (5, 5, 10, 10)
        };
        secondaryToplevel.Margin!.Thickness = new (1);

        Application.TopLevels.Clear ();
        Application.TopLevels.Push (topToplevel);
        Application.TopLevels.Push (secondaryToplevel);
        Application.Top = secondaryToplevel;

        secondaryToplevel.Margin.ViewportSettings = ViewportSettingsFlags.None;

        List<View?> found = View.GetViewsUnderLocation (new (5, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (found, v => v == secondaryToplevel);
        Assert.Contains (found, v => v == secondaryToplevel.Margin);
        Assert.DoesNotContain (found, v => v?.Id == topToplevel.Id);

        secondaryToplevel.Margin.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        found = View.GetViewsUnderLocation (new (5, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.DoesNotContain (found, v => v == secondaryToplevel);
        Assert.DoesNotContain (found, v => v == secondaryToplevel.Margin);
        Assert.Contains (found, v => v?.Id == topToplevel.Id);

        topToplevel.Dispose ();
        secondaryToplevel.Dispose ();
        Application.TopLevels.Clear ();
        Application.ResetState (true);
    }

    [Fact]
    public void Returns_Empty_When_Point_Outside_All_Toplevels ()
    {
        Application.ResetState (true);

        Toplevel topToplevel = new ()
        {
            Id = "topToplevel",
            Frame = new (0, 0, 20, 20)
        };

        Toplevel secondaryToplevel = new ()
        {
            Id = "secondaryToplevel",
            Frame = new (5, 5, 10, 10)
        };
        secondaryToplevel.Margin!.Thickness = new (1);
        secondaryToplevel.Layout ();

        Application.TopLevels.Clear ();
        Application.TopLevels.Push (topToplevel);
        Application.TopLevels.Push (secondaryToplevel);
        Application.Top = secondaryToplevel;

        List<View?> found = View.GetViewsUnderLocation (new (20, 20), ViewportSettingsFlags.TransparentMouse);
        Assert.Empty (found);

        topToplevel.Dispose ();
        secondaryToplevel.Dispose ();
        Application.TopLevels.Clear ();
        Application.ResetState (true);
    }
}
