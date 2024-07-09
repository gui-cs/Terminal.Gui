﻿
#nullable enable
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Tests View.FindDeepestView
/// </summary>
/// <param name="output"></param>
public class FindDeepestViewTests ()
{
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
    public void Contains (int frameX, int frameY, int marginThickness, int borderThickness, int paddingThickness, int testX, int testY, Type? expectedAdornmentType)
    {
        var view = new View ()
        {
            X = frameX, Y = frameY,
            Width = 10, Height = 10,
        };
        view.Margin.Thickness = new Thickness (marginThickness);
        view.Border.Thickness = new Thickness (borderThickness);
        view.Padding.Thickness = new Thickness (paddingThickness);

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

    // Test that FindDeepestView returns the correct view if the start view has no subviews
    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (2, 2)]
    public void Returns_Start_If_No_SubViews (int testX, int testY)
    {
        var start = new View ()
        {
            Width = 10, Height = 10,
        };

        Assert.Same (start, View.FindDeepestView (start, new (testX, testY)));
    }

    // Test that FindDeepestView returns null if the start view has no subviews and coords are outside the view
    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void Returns_Null_If_No_SubViews_Coords_Outside (int testX, int testY)
    {
        var start = new View ()
        {
            X = 1, Y = 2,
            Width = 10, Height = 10,
        };

        Assert.Null (View.FindDeepestView (start, new (testX, testY)));
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void Returns_Null_If_Start_Not_Visible (int testX, int testY)
    {
        var start = new View ()
        {
            X = 1, Y = 2,
            Width = 10, Height = 10,
            Visible = false,
        };

        Assert.Null (View.FindDeepestView (start, new (testX, testY)));
    }

    // Test that FindDeepestView returns the correct view if the start view has subviews
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
        var start = new View ()
        {
            Width = 10, Height = 10,
        };

        var subview = new View ()
        {
            X = 1, Y = 2,
            Width = 5, Height = 5,
        };
        start.Add (subview);

        var found = View.FindDeepestView (start, new (testX, testY));

        Assert.Equal (expectedSubViewFound, found == subview);
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
        var start = new View ()
        {
            Width = 10, Height = 10,
        };

        var subview = new View ()
        {
            X = 1, Y = 2,
            Width = 5, Height = 5,
            Visible = false
        };
        start.Add (subview);

        var found = View.FindDeepestView (start, new (testX, testY));

        Assert.Equal (expectedSubViewFound, found == subview);
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
        var start = new View ()
        {
            Width = 10, Height = 10,
            Visible = false
        };

        var subview = new View ()
        {
            X = 1, Y = 2,
            Width = 5, Height = 5,
        };
        start.Add (subview);
        subview.Visible = true;
        Assert.True (subview.Visible);
        Assert.False (start.Visible);
        var found = View.FindDeepestView (start, new (testX, testY));

        Assert.Equal (expectedSubViewFound, found == subview);
    }

    // Test that FindDeepestView works if the start view has positive Adornments
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
        var start = new View ()
        {
            Width = 10, Height = 10,
        };
        start.Margin.Thickness = new Thickness (1);

        var subview = new View ()
        {
            X = 1, Y = 2,
            Width = 5, Height = 5,
        };
        start.Add (subview);

        var found = View.FindDeepestView (start, new (testX, testY));

        Assert.Equal (expectedSubViewFound, found == subview);
    }

    // Test that FindDeepestView works if the start view has offset Viewport location
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
        var start = new View ()
        {
            Width = 10, Height = 10,
            ViewportSettings = ViewportSettings.AllowNegativeLocation
        };
        start.Viewport = new (offset, offset, 10, 10);

        var subview = new View ()
        {
            X = 1, Y = 1,
            Width = 2, Height = 2,
        };
        start.Add (subview);

        var found = View.FindDeepestView (start, new (testX, testY));

        Assert.Equal (expectedSubViewFound, found == subview);
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
        var start = new View ()
        {
            Width = 10, Height = 10,
        };
        start.Padding.Thickness = new Thickness (1);

        var subview = new View ()
        {
            X = Pos.AnchorEnd(1), Y = Pos.AnchorEnd(1),
            Width = 1, Height = 1,
        };
        start.Padding.Add (subview);
        start.BeginInit();
        start.EndInit();

        var found = View.FindDeepestView (start, new (testX, testY));

        Assert.Equal (expectedSubViewFound, found == subview);
    }


    [Theory]
    [InlineData (0, 0, typeof (Margin))]
    [InlineData (9, 9, typeof (Margin))]

    [InlineData (1, 1, typeof (Border))]
    [InlineData (8, 8, typeof (Border))]

    [InlineData (2, 2, typeof (Padding))]
    [InlineData (7, 7, typeof (Padding))]

    [InlineData (5, 5, typeof (View))]
    public void Returns_Adornment_If_Start_Has_Adornments (int testX, int testY, Type expectedAdornmentType)
    {
        var start = new View ()
        {
            Width = 10, Height = 10,
        };
        start.Margin.Thickness = new Thickness (1);
        start.Border.Thickness = new Thickness (1);
        start.Padding.Thickness = new Thickness (1);

        var subview = new View ()
        {
            X = 1, Y = 1,
            Width = 1, Height = 1,
        };
        start.Add (subview);

        var found = View.FindDeepestView (start, new (testX, testY));
        Assert.Equal (expectedAdornmentType, found!.GetType ());
    }

    // Test that FindDeepestView works if the subview has positive Adornments
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
        var start = new View ()
        {
            Width = 10, Height = 10,
        };

        var subview = new View ()
        {
            X = 1, Y = 2,
            Width = 5, Height = 5,
        };
        subview.Margin.Thickness = new Thickness (1);
        start.Add (subview);

        var found = View.FindDeepestView (start, new (testX, testY));

        Assert.Equal (expectedSubViewFound, found == subview);
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
        var start = new View ()
        {
            Width = 10, Height = 10,
        };

        // A subview with + Padding
        var subview = new View ()
        {
            X = 1, Y = 1,
            Width = 5, Height = 5,
        };
        subview.Padding.Thickness = new (1);

        // This subview will be at the bottom-right-corner of subview
        // So screen-relative location will be X + Width - 1 = 5
        var paddingSubview = new View ()
        {
            X = Pos.AnchorEnd (1),
            Y = Pos.AnchorEnd (1),
            Width = 1,
            Height = 1,
        };
        subview.Padding.Add (paddingSubview);
        start.Add (subview);
        start.BeginInit();
        start.EndInit();

        var found = View.FindDeepestView (start, new (testX, testY));

        Assert.Equal (expectedSubViewFound, found == paddingSubview);
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
        var start = new View ()
        {
            Width = 10, Height = 10,
        };

        // A subview with + Padding
        var subview = new View ()
        {
            X = 1, Y = 1,
            Width = 5, Height = 5,
        };
        subview.Padding.Thickness = new (1);

        // Scroll the subview
        subview.SetContentSize (new (10, 10));
        subview.Viewport = subview.Viewport with { Location = new (1, 1) };

        // This subview will be at the bottom-right-corner of subview
        // So screen-relative location will be X + Width - 1 = 5
        var paddingSubview = new View ()
        {
            X = Pos.AnchorEnd (1),
            Y = Pos.AnchorEnd (1),
            Width = 1,
            Height = 1,
        };
        subview.Padding.Add (paddingSubview);
        start.Add (subview);
        start.BeginInit ();
        start.EndInit ();

        var found = View.FindDeepestView (start, new (testX, testY));

        Assert.Equal (expectedSubViewFound, found == paddingSubview);
    }

    // Test that FindDeepestView works with nested subviews
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
        var start = new View ()
        {
            Width = 10, Height = 10
        };

        int numSubViews = 3;
        List<View> subviews = new List<View> ();
        for (int i = 0; i < numSubViews; i++)
        {
            var subview = new View ()
            {
                X = 1, Y = 1,
                Width = 5, Height = 5,
            };
            subviews.Add (subview);

            if (i > 0)
            {
                subviews [i - 1].Add (subview);
            }
        }

        start.Add (subviews [0]);

        var found = View.FindDeepestView (start, new (testX, testY));
        Assert.Equal (expectedSubViewFound, subviews.IndexOf (found!));
    }
}
