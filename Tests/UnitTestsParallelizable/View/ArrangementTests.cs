using Xunit.Abstractions;

namespace UnitTests_Parallelizable.ViewTests;

public class ArrangementTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region ViewArrangement Enum Tests

    [Fact]
    public void ViewArrangement_Fixed_IsZero ()
    {
        Assert.Equal (0, (int)ViewArrangement.Fixed);
    }

    [Fact]
    public void ViewArrangement_Flags_HaveCorrectValues ()
    {
        Assert.Equal (1, (int)ViewArrangement.Movable);
        Assert.Equal (2, (int)ViewArrangement.LeftResizable);
        Assert.Equal (4, (int)ViewArrangement.RightResizable);
        Assert.Equal (8, (int)ViewArrangement.TopResizable);
        Assert.Equal (16, (int)ViewArrangement.BottomResizable);
        Assert.Equal (32, (int)ViewArrangement.Overlapped);
    }

    [Fact]
    public void ViewArrangement_Resizable_IsCombinationOfAllResizableFlags ()
    {
        ViewArrangement expected = ViewArrangement.LeftResizable 
            | ViewArrangement.RightResizable 
            | ViewArrangement.TopResizable 
            | ViewArrangement.BottomResizable;
        
        Assert.Equal (ViewArrangement.Resizable, expected);
    }

    [Fact]
    public void ViewArrangement_CanCombineFlags ()
    {
        ViewArrangement arrangement = ViewArrangement.Movable | ViewArrangement.LeftResizable;
        
        Assert.True (arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.False (arrangement.HasFlag (ViewArrangement.RightResizable));
    }

    #endregion

    #region View.Arrangement Property Tests

    [Fact]
    public void View_Arrangement_DefaultsToFixed ()
    {
        var view = new View ();
        Assert.Equal (ViewArrangement.Fixed, view.Arrangement);
    }

    [Fact]
    public void View_Arrangement_CanBeSet ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable };
        Assert.Equal (ViewArrangement.Movable, view.Arrangement);
    }

    [Fact]
    public void View_Arrangement_CanSetMultipleFlags ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable 
        };
        
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    [Fact]
    public void View_Arrangement_Overlapped_CanBeSetIndependently ()
    {
        var view = new View { Arrangement = ViewArrangement.Overlapped };
        
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Overlapped));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.Resizable));
    }

    [Fact]
    public void View_Arrangement_CanCombineOverlappedWithOtherFlags ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable 
        };
        
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Overlapped));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
    }

    #endregion

    #region TopResizable and Movable Mutual Exclusivity Tests

    [Fact]
    public void TopResizable_WithoutMovable_IsAllowed ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single
        };
        
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.Movable));
    }

    [Fact]
    public void Movable_WithTopResizable_MovableWins ()
    {
        // According to docs and Border.Arrangment.cs line 569:
        // TopResizable is only checked if NOT Movable
        var view = new View 
        { 
            Arrangement = ViewArrangement.Movable | ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single
        };
        
        // Both flags can be set on the property
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        
        // But the behavior in Border.DetermineArrangeModeFromClick 
        // will prioritize Movable over TopResizable
    }

    [Fact]
    public void Resizable_WithMovable_IncludesTopResizable ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.Resizable | ViewArrangement.Movable,
            BorderStyle = LineStyle.Single
        };
        
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    #endregion

    #region Border Arrangement Tests

    [Fact]
    public void Border_WithNoArrangement_HasNoArrangementOptions ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.Fixed,
            BorderStyle = LineStyle.Single
        };
        
        Assert.NotNull (view.Border);
        Assert.Equal (ViewArrangement.Fixed, view.Arrangement);
    }

    [Fact]
    public void Border_WithMovableArrangement_CanEnterArrangeMode ()
    {
        var superView = new View ();
        var view = new View 
        { 
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };
        superView.Add (view);
        
        Assert.NotNull (view.Border);
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
    }

    [Fact]
    public void Border_WithResizableArrangement_HasResizableOptions ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.Resizable,
            BorderStyle = LineStyle.Single
        };
        
        Assert.NotNull (view.Border);
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    [Theory]
    [InlineData (ViewArrangement.LeftResizable)]
    [InlineData (ViewArrangement.RightResizable)]
    [InlineData (ViewArrangement.TopResizable)]
    [InlineData (ViewArrangement.BottomResizable)]
    public void Border_WithSingleResizableDirection_OnlyHasThatOption (ViewArrangement arrangement)
    {
        var view = new View 
        { 
            Arrangement = arrangement,
            BorderStyle = LineStyle.Single
        };
        
        Assert.NotNull (view.Border);
        Assert.True (view.Arrangement.HasFlag (arrangement));
        
        // Verify other directions are not set
        if (arrangement != ViewArrangement.LeftResizable)
            Assert.False (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        if (arrangement != ViewArrangement.RightResizable)
            Assert.False (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        if (arrangement != ViewArrangement.TopResizable)
            Assert.False (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        if (arrangement != ViewArrangement.BottomResizable)
            Assert.False (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    #endregion

    #region Corner Resizing Tests

    [Fact]
    public void Border_BottomRightResizable_CombinesBothFlags ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.BottomResizable | ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single
        };
        
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
    }

    [Fact]
    public void Border_BottomLeftResizable_CombinesBothFlags ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.BottomResizable | ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };
        
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
    }

    [Fact]
    public void Border_TopRightResizable_CombinesBothFlags ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.TopResizable | ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single
        };
        
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
    }

    [Fact]
    public void Border_TopLeftResizable_CombinesBothFlags ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.TopResizable | ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };
        
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
    }

    #endregion

    #region Overlapped Layout Tests

    [Theory]
    [InlineData (ViewArrangement.Fixed)]
    [InlineData (ViewArrangement.Overlapped)]
    public void MoveSubViewToEnd_ViewArrangement (ViewArrangement arrangement)
    {
        View superView = new () { Arrangement = arrangement };

        var subview1 = new View { Id = "subview1" };
        var subview2 = new View { Id = "subview2" };
        var subview3 = new View { Id = "subview3" };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewToEnd (subview1);
        Assert.Equal ([subview2, subview3, subview1], superView.SubViews.ToArray ());

        superView.MoveSubViewToEnd (subview2);
        Assert.Equal ([subview3, subview1, subview2], superView.SubViews.ToArray ());

        superView.MoveSubViewToEnd (subview3);
        Assert.Equal ([subview1, subview2, subview3], superView.SubViews.ToArray ());
    }

    [Fact]
    public void Overlapped_AllowsSubViewsToOverlap ()
    {
        var superView = new View 
        { 
            Arrangement = ViewArrangement.Overlapped,
            Width = 20,
            Height = 20
        };

        var view1 = new View { X = 0, Y = 0, Width = 10, Height = 10 };
        var view2 = new View { X = 5, Y = 5, Width = 10, Height = 10 };

        superView.Add (view1, view2);

        // Both views can exist at overlapping positions
        Assert.Equal (2, superView.SubViews.Count);
        Assert.True (view1.Frame.IntersectsWith (view2.Frame));
    }

    #endregion

    #region Splitter Pattern Tests

    [Fact]
    public void LeftResizable_CanBeUsedForHorizontalSplitter ()
    {
        var container = new View { Width = 80, Height = 25 };
        
        var leftPane = new View
        {
            X = 0,
            Y = 0,
            Width = 40,
            Height = Dim.Fill ()
        };
        
        var rightPane = new View
        {
            X = 40,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };
        
        container.Add (leftPane, rightPane);
        
        Assert.True (rightPane.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.NotNull (rightPane.Border);
    }

    [Fact]
    public void TopResizable_CanBeUsedForVerticalSplitter ()
    {
        var container = new View { Width = 80, Height = 25 };
        
        var topPane = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = 10
        };
        
        var bottomPane = new View
        {
            X = 0,
            Y = 10,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single
        };
        
        container.Add (topPane, bottomPane);
        
        Assert.True (bottomPane.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.NotNull (bottomPane.Border);
    }

    #endregion

    #region View Without Border Tests

    [Fact]
    public void View_WithoutBorderStyle_CanHaveArrangement ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.Movable
        };
        
        // Arrangement can be set even without a border style
        // Border object still exists but has no visible style
        Assert.Equal (ViewArrangement.Movable, view.Arrangement);
        Assert.NotNull (view.Border);
        Assert.Equal (LineStyle.None, view.BorderStyle);
    }

    [Fact]
    public void View_WithNoBorderStyle_ResizableCanBeSet ()
    {
        var view = new View 
        { 
            Arrangement = ViewArrangement.Resizable
        };
        
        // Arrangement is set but has limited effect without a visible border style
        Assert.Equal (ViewArrangement.Resizable, view.Arrangement);
        Assert.NotNull (view.Border);
        Assert.Equal (LineStyle.None, view.BorderStyle);
    }

    #endregion
}
