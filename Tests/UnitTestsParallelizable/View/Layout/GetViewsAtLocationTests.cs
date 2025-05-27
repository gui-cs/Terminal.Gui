#nullable enable

namespace Terminal.Gui.ViewMouseTests;

[Trait ("Category", "Layout")]
public class GetViewsAtLocationTests
{
    private class TestView : View
    {
        public TestView (int x, int y, int w, int h, bool visible = true)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            base.Visible = visible;
        }
    }

    [Fact]
    public void ReturnsEmpty_WhenRootIsNull ()
    {
        var result = View.GetViewsAtLocation (null, new Point (0, 0));
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsEmpty_WhenRootIsNotVisible ()
    {
        var root = new TestView (0, 0, 10, 10, visible: false);
        var result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsEmpty_WhenPointOutsideRoot ()
    {
        var root = new TestView (0, 0, 10, 10);
        var result = View.GetViewsAtLocation (root, new Point (20, 20));
        Assert.Empty (result);
    }


    [Fact]
    public void ReturnsEmpty_WhenPointOutsideRoot_AndSubview ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub = new TestView (5, 5, 2, 2);
        root.Add (sub);
        var result = View.GetViewsAtLocation (root, new Point (20, 20));
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsRoot_WhenPointInsideRoot_NoSubviews ()
    {
        var root = new TestView (0, 0, 10, 10);
        var result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }


    [Fact]
    public void ReturnsRoot_And_Subview_WhenPointInsideRootMargin ()
    {
        var root = new TestView (0, 0, 10, 10);
        root.Margin.Thickness = new (1);
        var sub = new TestView (2, 2, 5, 5);
        root.Add (sub);
        var result = View.GetViewsAtLocation (root, new Point (3, 3));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
    }


    [Fact]
    public void ReturnsRoot_And_Margin_WhenPointInside_With_Margin ()
    {
        var root = new TestView (0, 0, 10, 10);
        root.Margin.Thickness = new (1);
        var result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Margin, result [1]);
    }

    [Fact]
    public void ReturnsRoot_WhenPointOutsideSubview_With_Margin ()
    {
        var root = new TestView (0, 0, 10, 10);
        root.Margin.Thickness = new (1);
        var sub = new TestView (2, 2, 5, 5);
        root.Add (sub);
        var result = View.GetViewsAtLocation (root, new Point (2, 2));
        Assert.Equal (1, result.Count);
        Assert.Equal (root, result [0]);

        result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Margin, result [1]);

        result = View.GetViewsAtLocation (root, new Point (1, 1));
        Assert.Equal (1, result.Count);
        Assert.Equal (root, result [0]);

        result = View.GetViewsAtLocation (root, new Point (8, 8));
        Assert.Equal (1, result.Count);
        Assert.Equal (root, result [0]);
    }


    [Fact]
    public void ReturnsRoot_And_Border_WhenPointInside_With_Border ()
    {
        var root = new TestView (0, 0, 10, 10);
        root.Border.Thickness = new (1);
        var result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Border, result [1]);
    }

    [Fact]
    public void ReturnsRoot_WhenPointOutsideSubview_With_Border ()
    {
        var root = new TestView (0, 0, 10, 10);
        root.Border.Thickness = new (1);
        var sub = new TestView (2, 2, 5, 5);
        root.Add (sub);
        var result = View.GetViewsAtLocation (root, new Point (2, 2));
        Assert.Equal (1, result.Count);
        Assert.Equal (root, result [0]);

        result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Border, result [1]);

        result = View.GetViewsAtLocation (root, new Point (1, 1));
        Assert.Equal (1, result.Count);
        Assert.Equal (root, result [0]);

        result = View.GetViewsAtLocation (root, new Point (8, 8));
        Assert.Equal (1, result.Count);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsRoot_And_Border_WhenPointInsideRootBorder ()
    {
        var root = new TestView (0, 0, 10, 10);
        root.Border.Thickness = new (1);
        var result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Border, result [1]);
    }

    [Fact]
    public void ReturnsRoot_And_Padding_WhenPointInsideRootPadding ()
    {
        var root = new TestView (0, 0, 10, 10);
        root.Padding.Thickness = new (1);
        var result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (root.Padding, result [1]);
    }

    [Fact]
    public void ReturnsRootAndSubview_WhenPointInsideSubview ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub = new TestView (2, 2, 5, 5);
        root.Add (sub);

        var result = View.GetViewsAtLocation (root, new Point (3, 3));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndMargin_WhenPointInsideSubviewMargin ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub = new TestView (2, 2, 5, 5);
        sub.Margin!.Thickness = new (1);
        root.Add (sub);

        var result = View.GetViewsAtLocation (root, new Point (6, 6));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Margin, result [2]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndBorder_WhenPointInsideSubviewBorder ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub = new TestView (2, 2, 5, 5);
        sub.Border!.Thickness = new (1);
        root.Add (sub);

        var result = View.GetViewsAtLocation (root, new Point (2, 2));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Border, result [2]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndSubviewAndBorder_WhenPointInsideSubviewBorder ()
    {
        var root = new TestView (2, 2, 10, 10);
        var sub = new TestView (2, 2, 5, 5);
        sub.Border!.Thickness = new (1);
        root.Add (sub);

        var result = View.GetViewsAtLocation (root, new Point (4, 4));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Border, result [2]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndBorder_WhenPointInsideSubviewPadding ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub = new TestView (2, 2, 5, 5);
        sub.Padding!.Thickness = new (1);
        root.Add (sub);

        var result = View.GetViewsAtLocation (root, new Point (2, 2));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Padding, result [2]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndMarginAndShadowView_WhenPointInsideSubviewMargin ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub = new TestView (2, 2, 5, 5);
        sub.ShadowStyle = ShadowStyle.Opaque;
        root.Add (sub);

        root.Layout ();

        var result = View.GetViewsAtLocation (root, new Point (6, 6));
        Assert.Equal (5, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Margin, result [2]);
        Assert.Equal (sub.Margin!.SubViews.ElementAt (0), result [3]);
        Assert.Equal (sub.Margin.SubViews.ElementAt (1), result [4]);
    }

    [Fact]
    public void ReturnsRootAndSubviewAndBorderAndButton_WhenPointInsideSubviewBorder ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub = new TestView (2, 2, 5, 5);
        sub.Border!.Thickness = new (1);

        Button closeButton = new Button ()
        {
            NoDecorations = true,
            NoPadding = true,
            Title = "X",
            Width = 1,
            Height = 1,
            X = Pos.AnchorEnd (),
            Y= 0,
            ShadowStyle = ShadowStyle.None
        };
        sub.Border.Add (closeButton);
        root.Add (sub);

        root.Layout ();

        var result = View.GetViewsAtLocation (root, new Point (6, 2));
        Assert.Equal (4, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub, result [1]);
        Assert.Equal (sub.Border, result [2]);
        Assert.Equal (closeButton, result [3]);
    }

    [Fact]
    public void ReturnsDeepestSubview_WhenNested ()
    {
        var root = new TestView (0, 0, 20, 20);
        var sub1 = new TestView (2, 2, 16, 16);
        var sub2 = new TestView (3, 3, 10, 10);
        var sub3 = new TestView (1, 1, 5, 5);
        root.Add (sub1);
        sub1.Add (sub2);
        sub2.Add (sub3);

        // Point inside all
        var result = View.GetViewsAtLocation (root, new Point (7, 7));
        Assert.Equal (4, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub1, result [1]);
        Assert.Equal (sub2, result [2]);
        Assert.Equal (sub3, result [3]);
    }

    [Fact]
    public void ReturnsTopmostSubview_WhenOverlapping ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub1 = new TestView (2, 2, 6, 6);
        var sub2 = new TestView (4, 4, 6, 6);
        root.Add (sub1);
        root.Add (sub2); // sub2 is on top

        var result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Equal (3, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub1, result [1]);
        Assert.Equal (sub2, result [2]);
    }

    [Fact]
    public void ReturnsTopmostSubview_WhenNotOverlapping ()
    {
        var root = new TestView (0, 0, 10, 10);// under 5,5,
        var sub1 = new TestView (10, 10, 6, 6); // not under location 5,5
        var sub2 = new TestView (4, 4, 6, 6); // under 5,5,
        root.Add (sub1);
        root.Add (sub2); // sub2 is on top

        var result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub2, result [1]);
    }

    [Fact]
    public void SkipsInvisibleSubviews ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub1 = new TestView (2, 2, 6, 6, visible: false);
        var sub2 = new TestView (4, 4, 6, 6);
        root.Add (sub1);
        root.Add (sub2);

        var result = View.GetViewsAtLocation (root, new Point (5, 5));
        Assert.Equal (2, result.Count);
        Assert.Equal (root, result [0]);
        Assert.Equal (sub2, result [1]);
    }

    [Fact]
    public void ReturnsRoot_WhenPointOnEdge ()
    {
        var root = new TestView (0, 0, 10, 10);
        var result = View.GetViewsAtLocation (root, new Point (0, 0));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsRoot_WhenPointOnBottomRightCorner ()
    {
        var root = new TestView (0, 0, 10, 10);
        var result = View.GetViewsAtLocation (root, new Point (9, 9));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }

    [Fact]
    public void ReturnsEmpty_WhenAllSubviewsInvisible ()
    {
        var root = new TestView (0, 0, 10, 10);
        var sub1 = new TestView (2, 2, 6, 6, visible: false);
        root.Add (sub1);

        var result = View.GetViewsAtLocation (root, new Point (3, 3));
        Assert.Single (result);
        Assert.Equal (root, result [0]);
    }
}

