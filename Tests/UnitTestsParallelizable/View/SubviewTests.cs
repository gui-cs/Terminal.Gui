using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class SubviewTests
{
    private readonly ITestOutputHelper _output;
    public SubviewTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void Added_Removed ()
    {
        var v = new View { Frame = new Rectangle (0, 0, 10, 24) };
        var t = new View ();

        v.Added += (s, e) =>
                   {
                       Assert.Same (v.SuperView, e.SuperView);
                       Assert.Same (t, e.SuperView);
                       Assert.Same (v, e.SubView);
                   };

        v.Removed += (s, e) =>
                     {
                         Assert.Same (t, e.SuperView);
                         Assert.Same (v, e.SubView);
                         Assert.True (v.SuperView == null);
                     };

        t.Add (v);
        Assert.True (t.Subviews.Count == 1);

        t.Remove (v);
        Assert.True (t.Subviews.Count == 0);

        t.Dispose ();
        v.Dispose ();
    }


    [Fact]
    public void IsAdded_Added_Removed ()
    {
        var top = new Toplevel ();
        var view = new View ();
        Assert.False (view.IsAdded);
        top.Add (view);
        Assert.True (view.IsAdded);
        top.Remove (view);
        Assert.False (view.IsAdded);

        top.Dispose ();
        view.Dispose ();
    }

    // TODO: Consider a feature that will change the ContentSize to fit the subviews.
    [Fact]
    public void Add_Does_Not_Impact_ContentSize ()
    {
        var view = new View ();
        view.SetContentSize (new Size (1, 1));

        var subview = new View ()
        {
            X = 10,
            Y = 10
        };

        Assert.Equal (new Size (1, 1), view.GetContentSize ());
        view.Add (subview);
        Assert.Equal (new Size (1, 1), view.GetContentSize ());
    }

    [Fact]
    public void Remove_Does_Not_Impact_ContentSize ()
    {
        var view = new View ();
        view.SetContentSize (new Size (1, 1));

        var subview = new View ()
        {
            X = 10,
            Y = 10
        };

        Assert.Equal (new Size (1, 1), view.GetContentSize ());
        view.Add (subview);
        Assert.Equal (new Size (1, 1), view.GetContentSize ());

        view.SetContentSize (new Size (5, 5));
        Assert.Equal (new Size (5, 5), view.GetContentSize ());

        view.Remove (subview);
        Assert.Equal (new Size (5, 5), view.GetContentSize ());
    }

    [Fact]
    public void MoveSubviewToStart ()
    {
        View superView = new ();

        View subview1 = new View ()
        {
            Id = "subview1"
        };

        View subview2 = new View ()
        {
            Id = "subview2"
        };

        View subview3 = new View ()
        {
            Id = "subview3"
        };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubviewToStart (subview2);
        Assert.Equal(subview2, superView.Subviews [0]);

        superView.MoveSubviewToStart (subview3);
        Assert.Equal (subview3, superView.Subviews [0]);
    }


    [Fact]
    public void MoveSubviewTowardsFront ()
    {
        View superView = new ();

        View subview1 = new View ()
        {
            Id = "subview1"
        };

        View subview2 = new View ()
        {
            Id = "subview2"
        };

        View subview3 = new View ()
        {
            Id = "subview3"
        };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubviewTowardsStart (subview2);
        Assert.Equal (subview2, superView.Subviews [0]);

        superView.MoveSubviewTowardsStart (subview3);
        Assert.Equal (subview3, superView.Subviews [1]);

        // Already at front, what happens?
        superView.MoveSubviewTowardsStart (subview2);
        Assert.Equal (subview2, superView.Subviews [0]);
    }

    [Fact]
    public void MoveSubviewToEnd ()
    {
        View superView = new ();

        View subview1 = new View ()
        {
            Id = "subview1"
        };

        View subview2 = new View ()
        {
            Id = "subview2"
        };

        View subview3 = new View ()
        {
            Id = "subview3"
        };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubviewToEnd (subview1);
        Assert.Equal (subview1, superView.Subviews [^1]);

        superView.MoveSubviewToEnd (subview2);
        Assert.Equal (subview2, superView.Subviews [^1]);
    }


    [Fact]
    public void MoveSubviewTowardsEnd ()
    {
        View superView = new ();

        View subview1 = new View ()
        {
            Id = "subview1"
        };

        View subview2 = new View ()
        {
            Id = "subview2"
        };

        View subview3 = new View ()
        {
            Id = "subview3"
        };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubviewTowardsEnd (subview2);
        Assert.Equal (subview2, superView.Subviews [^1]);

        superView.MoveSubviewTowardsEnd (subview1);
        Assert.Equal (subview1, superView.Subviews [1]);

        // Already at end, what happens?
        superView.MoveSubviewTowardsEnd (subview2);
        Assert.Equal (subview2, superView.Subviews [^1]);
    }

    [Fact]
    public void IsInHierarchy_ViewIsNull_ReturnsFalse ()
    {
        // Arrange
        var start = new View ();

        // Act
        var result = View.IsInHierarchy (start, null);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void IsInHierarchy_StartIsNull_ReturnsFalse ()
    {
        // Arrange
        var view = new View ();

        // Act
        var result = View.IsInHierarchy (null, view);

        // Assert
        Assert.False (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsStart_ReturnsTrue ()
    {
        // Arrange
        var start = new View ();

        // Act
        var result = View.IsInHierarchy (start, start);

        // Assert
        Assert.True (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsDirectSubview_ReturnsTrue ()
    {
        // Arrange
        var start = new View ();
        var subview = new View ();
        start.Add (subview);

        // Act
        var result = View.IsInHierarchy (start, subview);

        // Assert
        Assert.True (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsNestedSubview_ReturnsTrue ()
    {
        // Arrange
        var start = new View ();
        var subview = new View ();
        var nestedSubview = new View ();
        start.Add (subview);
        subview.Add (nestedSubview);

        // Act
        var result = View.IsInHierarchy (start, nestedSubview);

        // Assert
        Assert.True (result);
    }

    [Fact]
    public void IsInHierarchy_ViewIsNotInHierarchy_ReturnsFalse ()
    {
        // Arrange
        var start = new View ();
        var subview = new View ();

        // Act
        var result = View.IsInHierarchy (start, subview);

        // Assert
        Assert.False (result);
    }

    [Theory]
    [CombinatorialData]
    public void IsInHierarchy_ViewIsInAdornments_ReturnsTrue (bool includeAdornments)
    {
        // Arrange
        var start = new View ()
        {
            Id = "start"
        };
        var inPadding = new View ()
        {
            Id = "inPadding"
        };

        start.Padding.Add (inPadding);

        // Act
        var result = View.IsInHierarchy (start, inPadding, includeAdornments: includeAdornments);

        // Assert
        Assert.Equal(includeAdornments, result);
    }
}
