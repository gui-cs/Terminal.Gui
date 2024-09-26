#nullable enable

namespace Terminal.Gui.ViewMouseTests;

[Trait ("Category", "Input")]
public class GetViewsUnderMouseTests
{
    [Theory]
    [InlineData (0, 0, 0, 0, 0, -1, -1, null)]
    [InlineData (0, 0, 0, 0, 0, 0, 0, typeof (Toplevel))]
    [InlineData (0, 0, 0, 0, 0, 1, 1, typeof (Toplevel))]
    [InlineData (0, 0, 0, 0, 0, 4, 4, typeof (Toplevel))]
    [InlineData (0, 0, 0, 0, 0, 9, 9, typeof (Toplevel))]
    [InlineData (0, 0, 0, 0, 0, 10, 10, null)]
    [InlineData (1, 1, 0, 0, 0, -1, -1, null)]
    [InlineData (1, 1, 0, 0, 0, 0, 0, null)]
    [InlineData (1, 1, 0, 0, 0, 1, 1, typeof (Toplevel))]
    [InlineData (1, 1, 0, 0, 0, 4, 4, typeof (Toplevel))]
    [InlineData (1, 1, 0, 0, 0, 9, 9, typeof (Toplevel))]
    [InlineData (1, 1, 0, 0, 0, 10, 10, typeof (Toplevel))]
    [InlineData (0, 0, 1, 0, 0, -1, -1, null)]
    [InlineData (0, 0, 1, 0, 0, 0, 0, typeof (Margin))]
    [InlineData (0, 0, 1, 0, 0, 1, 1, typeof (Toplevel))]
    [InlineData (0, 0, 1, 0, 0, 4, 4, typeof (Toplevel))]
    [InlineData (0, 0, 1, 0, 0, 9, 9, typeof (Margin))]
    [InlineData (0, 0, 1, 0, 0, 10, 10, null)]
    [InlineData (0, 0, 1, 1, 0, -1, -1, null)]
    [InlineData (0, 0, 1, 1, 0, 0, 0, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 0, 1, 1, typeof (Border))]
    [InlineData (0, 0, 1, 1, 0, 4, 4, typeof (Toplevel))]
    [InlineData (0, 0, 1, 1, 0, 9, 9, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 0, 10, 10, null)]
    [InlineData (0, 0, 1, 1, 1, -1, -1, null)]
    [InlineData (0, 0, 1, 1, 1, 0, 0, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 1, 1, 1, typeof (Border))]
    [InlineData (0, 0, 1, 1, 1, 2, 2, typeof (Padding))]
    [InlineData (0, 0, 1, 1, 1, 4, 4, typeof (Toplevel))]
    [InlineData (0, 0, 1, 1, 1, 9, 9, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 1, 10, 10, null)]
    [InlineData (1, 1, 1, 0, 0, -1, -1, null)]
    [InlineData (1, 1, 1, 0, 0, 0, 0, null)]
    [InlineData (1, 1, 1, 0, 0, 1, 1, typeof (Margin))]
    [InlineData (1, 1, 1, 0, 0, 4, 4, typeof (Toplevel))]
    [InlineData (1, 1, 1, 0, 0, 9, 9, typeof (Toplevel))]
    [InlineData (1, 1, 1, 0, 0, 10, 10, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 0, -1, -1, null)]
    [InlineData (1, 1, 1, 1, 0, 0, 0, null)]
    [InlineData (1, 1, 1, 1, 0, 1, 1, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 0, 4, 4, typeof (Toplevel))]
    [InlineData (1, 1, 1, 1, 0, 9, 9, typeof (Border))]
    [InlineData (1, 1, 1, 1, 0, 10, 10, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 1, -1, -1, null)]
    [InlineData (1, 1, 1, 1, 1, 0, 0, null)]
    [InlineData (1, 1, 1, 1, 1, 1, 1, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 1, 2, 2, typeof (Border))]
    [InlineData (1, 1, 1, 1, 1, 3, 3, typeof (Padding))]
    [InlineData (1, 1, 1, 1, 1, 4, 4, typeof (Toplevel))]
    [InlineData (1, 1, 1, 1, 1, 8, 8, typeof (Padding))]
    [InlineData (1, 1, 1, 1, 1, 9, 9, typeof (Border))]
    [InlineData (1, 1, 1, 1, 1, 10, 10, typeof (Margin))]
    public void GetViewsUnderMouse_Top_Adornments_Returns_Correct_View (
        int frameX,
        int frameY,
        int marginThickness,
        int borderThickness,
        int paddingThickness,
        int testX,
        int testY,
        Type? expectedViewType
    )
    {
        // Arrange
        Application.Top = new ()
        {
            Frame = new (frameX, frameY, 10, 10)
        };
        Application.Top.Margin.Thickness = new (marginThickness);
        Application.Top.Border.Thickness = new (borderThickness);
        Application.Top.Padding.Thickness = new (paddingThickness);

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = View.GetViewsUnderMouse (location);

        // Assert
        if (expectedViewType == null)
        {
            Assert.Empty (viewsUnderMouse);
        }
        else
        {
            Assert.Contains (viewsUnderMouse, v => v?.GetType () == expectedViewType);
        }

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (2, 2)]
    public void GetViewsUnderMouse_Returns_Top_If_No_SubViews (int testX, int testY)
    {
        // Arrange
        Application.Top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = View.GetViewsUnderMouse (location);

        // Assert
        Assert.Contains (viewsUnderMouse, v => v == Application.Top);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void GetViewsUnderMouse_Returns_Null_If_No_SubViews_Coords_Outside (int testX, int testY)
    {
        // Arrange
        var view = new View
        {
            Frame = new (0, 0, 10, 10)
        };

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = View.GetViewsUnderMouse (location);

        // Assert
        Assert.Empty (viewsUnderMouse);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void GetViewsUnderMouse_Returns_Null_If_Start_Not_Visible (int testX, int testY)
    {
        // Arrange
        var view = new View
        {
            Frame = new (0, 0, 10, 10),
            Visible = false
        };

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = View.GetViewsUnderMouse (location);

        // Assert
        Assert.Empty (viewsUnderMouse);
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, true)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (6, 7, true)]
    [InlineData (1, 2, true)]
    [InlineData (5, 6, true)]
    public void GetViewsUnderMouse_Returns_Correct_If_SubViews (int testX, int testY, bool expected)
    {
        // Arrange
        Application.Top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };

        var subView = new View
        {
            Frame = new (1, 1, 8, 8)
        };

        Application.Top.Add (subView);

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = View.GetViewsUnderMouse (location);

        // Assert
        if (expected)
        {
            Assert.Contains (viewsUnderMouse, v => v == subView);
        }
        else
        {
            Assert.DoesNotContain (viewsUnderMouse, v => v == subView);
        }

        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, -1, -1, null)]
    [InlineData (0, 0, 0, 0, 0, 0, 0, typeof (View))]
    [InlineData (0, 0, 0, 0, 0, 1, 1, typeof (View))]
    [InlineData (0, 0, 0, 0, 0, 4, 4, typeof (View))]
    [InlineData (0, 0, 0, 0, 0, 9, 9, typeof (View))]
    [InlineData (0, 0, 0, 0, 0, 10, 10, null)]
    [InlineData (1, 1, 0, 0, 0, -1, -1, null)]
    [InlineData (1, 1, 0, 0, 0, 0, 0, null)]
    [InlineData (1, 1, 0, 0, 0, 1, 1, typeof (View))]
    [InlineData (1, 1, 0, 0, 0, 4, 4, typeof (View))]
    [InlineData (1, 1, 0, 0, 0, 9, 9, typeof (View))]
    [InlineData (1, 1, 0, 0, 0, 10, 10, typeof (View))]
    [InlineData (0, 0, 1, 0, 0, -1, -1, null)]
    [InlineData (0, 0, 1, 0, 0, 0, 0, typeof (Margin))]
    [InlineData (0, 0, 1, 0, 0, 1, 1, typeof (View))]
    [InlineData (0, 0, 1, 0, 0, 4, 4, typeof (View))]
    [InlineData (0, 0, 1, 0, 0, 9, 9, typeof (Margin))]
    [InlineData (0, 0, 1, 0, 0, 10, 10, null)]
    [InlineData (0, 0, 1, 1, 0, -1, -1, null)]
    [InlineData (0, 0, 1, 1, 0, 0, 0, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 0, 1, 1, typeof (Border))]
    [InlineData (0, 0, 1, 1, 0, 4, 4, typeof (View))]
    [InlineData (0, 0, 1, 1, 0, 9, 9, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 0, 10, 10, null)]
    [InlineData (0, 0, 1, 1, 1, -1, -1, null)]
    [InlineData (0, 0, 1, 1, 1, 0, 0, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 1, 1, 1, typeof (Border))]
    [InlineData (0, 0, 1, 1, 1, 2, 2, typeof (Padding))]
    [InlineData (0, 0, 1, 1, 1, 4, 4, typeof (View))]
    [InlineData (0, 0, 1, 1, 1, 9, 9, typeof (Margin))]
    [InlineData (0, 0, 1, 1, 1, 10, 10, null)]
    [InlineData (1, 1, 1, 0, 0, -1, -1, null)]
    [InlineData (1, 1, 1, 0, 0, 0, 0, null)]
    [InlineData (1, 1, 1, 0, 0, 1, 1, typeof (Margin))]
    [InlineData (1, 1, 1, 0, 0, 4, 4, typeof (View))]
    [InlineData (1, 1, 1, 0, 0, 9, 9, typeof (View))]
    [InlineData (1, 1, 1, 0, 0, 10, 10, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 0, -1, -1, null)]
    [InlineData (1, 1, 1, 1, 0, 0, 0, null)]
    [InlineData (1, 1, 1, 1, 0, 1, 1, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 0, 4, 4, typeof (View))]
    [InlineData (1, 1, 1, 1, 0, 9, 9, typeof (Border))]
    [InlineData (1, 1, 1, 1, 0, 10, 10, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 1, -1, -1, null)]
    [InlineData (1, 1, 1, 1, 1, 0, 0, null)]
    [InlineData (1, 1, 1, 1, 1, 1, 1, typeof (Margin))]
    [InlineData (1, 1, 1, 1, 1, 2, 2, typeof (Border))]
    [InlineData (1, 1, 1, 1, 1, 3, 3, typeof (Padding))]
    [InlineData (1, 1, 1, 1, 1, 4, 4, typeof (View))]
    [InlineData (1, 1, 1, 1, 1, 8, 8, typeof (Padding))]
    [InlineData (1, 1, 1, 1, 1, 9, 9, typeof (Border))]
    [InlineData (1, 1, 1, 1, 1, 10, 10, typeof (Margin))]
    public void Contains (
        int frameX,
        int frameY,
        int marginThickness,
        int borderThickness,
        int paddingThickness,
        int testX,
        int testY,
        Type? expectedAdornmentType
    )
    {
        var view = new View
        {
            X = frameX, Y = frameY,
            Width = 10, Height = 10
        };
        view.Margin.Thickness = new (marginThickness);
        view.Border.Thickness = new (borderThickness);
        view.Padding.Thickness = new (paddingThickness);

        Type? containedType = null;

        if (view.Contains (new (testX, testY)))
        {
            containedType = view.GetType ();
        }

        if (view.Margin.Contains (new (testX, testY)))
        {
            containedType = view.Margin.GetType ();
        }

        if (view.Border.Contains (new (testX, testY)))
        {
            containedType = view.Border.GetType ();
        }

        if (view.Padding.Contains (new (testX, testY)))
        {
            containedType = view.Padding.GetType ();
        }

        Assert.Equal (expectedAdornmentType, containedType);
    }

    // Test that GetViewsUnderMouse returns the correct view if the start view has no subviews
    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (2, 2)]
    public void Returns_Start_If_No_SubViews (int testX, int testY)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };

        Assert.Same (Application.Top, View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ());
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderMouse returns null if the start view has no subviews and coords are outside the view
    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void Returns_Null_If_No_SubViews_Coords_Outside (int testX, int testY)
    {
        Application.Top = new ()
        {
            X = 1, Y = 2,
            Width = 10, Height = 10
        };

        Assert.Null (View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ());
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void Returns_Null_If_Start_Not_Visible (int testX, int testY)
    {
        Application.Top = new ()
        {
            X = 1, Y = 2,
            Width = 10, Height = 10,
            Visible = false
        };

        Assert.Null (View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ());
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderMouse returns the correct view if the start view has subviews
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

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();

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

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();

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
        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderMouse works if the start view has positive Adornments
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
        Application.Top.Margin.Thickness = new (1);

        var subview = new View
        {
            X = 1, Y = 2,
            Width = 5, Height = 5
        };
        Application.Top.Add (subview);

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderMouse works if the start view has offset Viewport location
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
            ViewportSettings = ViewportSettings.AllowNegativeLocation
        };
        Application.Top.Viewport = new (offset, offset, 10, 10);

        var subview = new View
        {
            X = 1, Y = 1,
            Width = 2, Height = 2
        };
        Application.Top.Add (subview);

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();

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
    public void Returns_Correct_If_Start_Has_Adornment_WithSubview (int testX, int testY, bool expectedSubViewFound)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };
        Application.Top.Padding.Thickness = new (1);

        var subview = new View
        {
            X = Pos.AnchorEnd (1), Y = Pos.AnchorEnd (1),
            Width = 1, Height = 1
        };
        Application.Top.Padding.Add (subview);
        Application.Top.BeginInit ();
        Application.Top.EndInit ();

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0, 0, typeof (Margin))]
    [InlineData (9, 9, typeof (Margin))]
    [InlineData (1, 1, typeof (Border))]
    [InlineData (8, 8, typeof (Border))]
    [InlineData (2, 2, typeof (Padding))]
    [InlineData (7, 7, typeof (Padding))]
    [InlineData (5, 5, typeof (Toplevel))]
    public void Returns_Adornment_If_Start_Has_Adornments (int testX, int testY, Type expectedAdornmentType)
    {
        Application.Top = new ()
        {
            Width = 10, Height = 10
        };
        Application.Top.Margin.Thickness = new (1);
        Application.Top.Border.Thickness = new (1);
        Application.Top.Padding.Thickness = new (1);

        var subview = new View
        {
            X = 1, Y = 1,
            Width = 1, Height = 1
        };
        Application.Top.Add (subview);

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();
        Assert.Equal (expectedAdornmentType, found!.GetType ());
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderMouse works if the subview has positive Adornments
    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (1, 1, false)]
    [InlineData (9, 9, false)]
    [InlineData (10, 10, false)]
    [InlineData (7, 8, false)]
    [InlineData (6, 7, false)]
    [InlineData (1, 2, false)]
    [InlineData (5, 6, false)]
    [InlineData (2, 3, true)]
    public void Returns_Correct_If_SubView_Has_Adornments (int testX, int testY, bool expectedSubViewFound)
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
        subview.Margin.Thickness = new (1);
        Application.Top.Add (subview);

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == subview);
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
    public void Returns_Correct_If_SubView_Has_Adornment_WithSubview (int testX, int testY, bool expectedSubViewFound)
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
        subview.Padding.Thickness = new (1);

        // This subview will be at the bottom-right-corner of subview
        // So screen-relative location will be X + Width - 1 = 5
        var paddingSubview = new View
        {
            X = Pos.AnchorEnd (1),
            Y = Pos.AnchorEnd (1),
            Width = 1,
            Height = 1
        };
        subview.Padding.Add (paddingSubview);
        Application.Top.Add (subview);
        Application.Top.BeginInit ();
        Application.Top.EndInit ();

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == paddingSubview);
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
    public void Returns_Correct_If_SubView_Is_Scrolled_And_Has_Adornment_WithSubview (int testX, int testY, bool expectedSubViewFound)
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
        subview.Padding.Thickness = new (1);

        // Scroll the subview
        subview.SetContentSize (new (10, 10));
        subview.Viewport = subview.Viewport with { Location = new (1, 1) };

        // This subview will be at the bottom-right-corner of subview
        // So screen-relative location will be X + Width - 1 = 5
        var paddingSubview = new View
        {
            X = Pos.AnchorEnd (1),
            Y = Pos.AnchorEnd (1),
            Width = 1,
            Height = 1
        };
        subview.Padding.Add (paddingSubview);
        Application.Top.Add (subview);
        Application.Top.BeginInit ();
        Application.Top.EndInit ();

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();

        Assert.Equal (expectedSubViewFound, found == paddingSubview);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    // Test that GetViewsUnderMouse works with nested subviews
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

        View? found = View.GetViewsUnderMouse (new (testX, testY)).LastOrDefault ();
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
    public void GetViewsUnderMouse_Tiled_Subviews (int mouseX, int mouseY, string [] viewIdStrings)
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

        List<View?> found = View.GetViewsUnderMouse (new (mouseX, mouseY));

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
    public void GetViewsUnderMouse_Popover (int mouseX, int mouseY, string [] viewIdStrings)
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

        List<View?> found = View.GetViewsUnderMouse (new (mouseX, mouseY));

        string [] foundIds = found.Select (v => v!.Id).ToArray ();

        Assert.Equal (viewIdStrings, foundIds);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }
}
