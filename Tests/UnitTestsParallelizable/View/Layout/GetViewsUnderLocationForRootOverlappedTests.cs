#nullable enable

namespace Terminal.Gui.ViewMouseTests;

[Trait ("Category", "Input")]
public class GetViewsUnderLocationForRootOverlappedTests
{
    [Fact (Skip = "Overlapped not yet supported.")]
    public void Overlapped_Views_With_Same_ZOrder_Returns_All_Views ()
    {
        // Arrange
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 10, 10)
        };

        // Create two views that completely overlap
        View view1 = new ()
        {
            Id = "view1",
            X = 1,
            Y = 1,
            Width = 4,
            Height = 4,
            Arrangement = ViewArrangement.Overlapped
        };

        View view2 = new ()
        {
            Id = "view2",
            X = 1,
            Y = 1,
            Width = 4,
            Height = 4,
            Arrangement = ViewArrangement.Overlapped
        };

        // Add both to top - view2 should be on top of view1
        top.Add (view1);
        top.Add (view2);

        // Act - click at point (2,2) which is inside both views
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (2, 2), false);

        // Assert
        // Result should contain top, view1, and view2 - with view2 being last (on top)
        Assert.Equal (3, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("view1", result [1]?.Id);
        Assert.Equal ("view2", result [2]?.Id);
    }

    [Fact]
    public void Overlapped_Views_With_Same_ZOrder_But_Not_Overlapped_Flag_Returns_Only_Top_View ()
    {
        // Arrange
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 10, 10)
        };

        // Create two views that completely overlap, but without Overlapped flag
        View view1 = new ()
        {
            Id = "view1",
            X = 1,
            Y = 1,
            Width = 4,
            Height = 4

            // No ViewArrangement.Overlapped
        };

        View view2 = new ()
        {
            Id = "view2",
            X = 1,
            Y = 1,
            Width = 4,
            Height = 4

            // No ViewArrangement.Overlapped
        };

        // Add both to top - view2 should be on top of view1
        top.Add (view1);
        top.Add (view2);

        // Act - click at point (2,2) which is inside both views
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (2, 2), false);

        // Assert
        // Result should contain top and view2 only (not view1, since current implementation only returns one subview)
        Assert.Equal (2, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("view2", result [1]?.Id);
        Assert.DoesNotContain (result, v => v?.Id == "view1");
    }

    [Fact]
    public void Nested_Overlapped_Views_Should_Return_All_Views_In_Path ()
    {
        // Arrange
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 20, 20),
            Arrangement = ViewArrangement.Overlapped
        };

        // Create nested view structure:
        // top -> container (overlapped) -> [view1, view2 (overlapped)]
        View container = new ()
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Overlapped
        };

        View view1 = new ()
        {
            Id = "view1",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5,
            Arrangement = ViewArrangement.Overlapped
        };

        View view2 = new ()
        {
            Id = "view2",
            X = 3,
            Y = 3,
            Width = 5,
            Height = 5,
            Arrangement = ViewArrangement.Overlapped
        };

        // Add views with overlaps
        container.Add (view1);
        container.Add (view2);
        top.Add (container);

        // Act - click at point (5,5) which should be inside all views
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (5, 5), false);

        // Assert
        // We should get all views in the path: top -> container -> view2 (on top of view1)
        Assert.Equal (3, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("container", result [1]?.Id);
        Assert.Equal ("view2", result [2]?.Id);

        // The current implementation only follows one path through the view hierarchy,
        // so view1 is not included even though the point is inside it
        Assert.DoesNotContain (result, v => v?.Id == "view1");
    }

    [Fact (Skip = "Overlapped not yet supported.")]
    public void Multiple_Overlapped_Views_Should_Return_All_Views_In_ZOrder ()
    {
        // Arrange
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 20, 20),
            Arrangement = ViewArrangement.Overlapped
        };

        // Create a setup with multiple overlapping views
        // where all views contain the test point
        View view1 = new ()
        {
            Id = "view1",
            X = 2,
            Y = 2,
            Width = 8,
            Height = 8,
            Arrangement = ViewArrangement.Overlapped
        };

        View view2 = new ()
        {
            Id = "view2",
            X = 3,
            Y = 3,
            Width = 8,
            Height = 8,
            Arrangement = ViewArrangement.Overlapped
        };

        View view3 = new ()
        {
            Id = "view3",
            X = 4,
            Y = 4,
            Width = 8,
            Height = 8,
            Arrangement = ViewArrangement.Overlapped
        };

        top.Add (view1);
        top.Add (view2);
        top.Add (view3);

        // Act - click at point (5,5) which is inside all views
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (5, 5), false);

        // Assert
        // In a proper implementation, we should get all views in Z-order
        // with view3 on top, then view2, then view1
        Assert.Equal (4, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("view1", result [1]?.Id);
        Assert.Equal ("view2", result [2]?.Id);
        Assert.Equal ("view3", result [3]?.Id);
    }

    [Fact (Skip = "Overlapped not yet supported.")]
    public void Overlapped_With_TransparentMouse_Should_Show_Views_Underneath ()
    {
        // Arrange
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 20, 20),
            Arrangement = ViewArrangement.Overlapped
        };

        // Create overlapping views with the top one being transparent to mouse
        View view1 = new ()
        {
            Id = "view1",
            X = 2,
            Y = 2,
            Width = 8,
            Height = 8,
            Arrangement = ViewArrangement.Overlapped
        };

        View view2 = new ()
        {
            Id = "view2",
            X = 3,
            Y = 3,
            Width = 6,
            Height = 6,
            Arrangement = ViewArrangement.Overlapped,
            ViewportSettings = ViewportSettings.TransparentMouse
        };

        View view3 = new ()
        {
            Id = "view3",
            X = 4,
            Y = 4,
            Width = 4,
            Height = 4,
            Arrangement = ViewArrangement.Overlapped
        };

        top.Add (view1);
        top.Add (view2); // This one is transparent to mouse
        top.Add (view3);

        // Act - click at point (5,5) which is inside all views
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (5, 5), false);

        // Assert
        // We should get top, view1, and view3, but not view2 since it's transparent to mouse
        Assert.Equal (3, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("view1", result [1]?.Id);
        Assert.Equal ("view3", result [2]?.Id);
        Assert.DoesNotContain (result, v => v?.Id == "view2");
    }

    [Fact (Skip = "Overlapped not yet supported.")]
    public void Complex_Overlapped_Hierarchy_With_Partial_Overlaps ()
    {
        // Arrange
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 30, 30),
            Arrangement = ViewArrangement.Overlapped
        };

        // Create a more complex setup with partial overlaps
        View container1 = new ()
        {
            Id = "container1",
            X = 1,
            Y = 1,
            Width = 15,
            Height = 15,
            Arrangement = ViewArrangement.Overlapped
        };

        View container2 = new ()
        {
            Id = "container2",
            X = 10, // Note: This starts at x=10, so it partially overlaps container1
            Y = 1,
            Width = 15,
            Height = 15,
            Arrangement = ViewArrangement.Overlapped
        };

        View viewA = new ()
        {
            Id = "viewA",
            X = 2,
            Y = 2,
            Width = 6,
            Height = 6,
            Arrangement = ViewArrangement.Overlapped
        };

        View viewB = new ()
        {
            Id = "viewB",
            X = 5,
            Y = 5,
            Width = 6,
            Height = 6,
            Arrangement = ViewArrangement.Overlapped
        };

        View viewC = new ()
        {
            Id = "viewC",
            X = 2,
            Y = 2,
            Width = 6,
            Height = 6,
            Arrangement = ViewArrangement.Overlapped
        };

        View viewD = new ()
        {
            Id = "viewD",
            X = 5,
            Y = 5,
            Width = 6,
            Height = 6,
            Arrangement = ViewArrangement.Overlapped
        };

        // Build the hierarchy:
        // top -> container1 -> viewA, viewB
        // top -> container2 -> viewC, viewD
        container1.Add (viewA);
        container1.Add (viewB);
        container2.Add (viewC);
        container2.Add (viewD);
        top.Add (container1);
        top.Add (container2);

        // Act - click at point (11, 6) which should be inside container2 and viewC
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (11, 6), false);

        // Assert
        // Should get: top, container2, viewC
        Assert.Equal (3, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("container2", result [1]?.Id);
        Assert.Equal ("viewC", result [2]?.Id);

        // Now test another point (12, 8) which should be inside container2 and viewD
        result = View.GetViewsUnderLocationForRoot (top, new (12, 8), false);

        Assert.Equal (3, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("container2", result [1]?.Id);
        Assert.Equal ("viewD", result [2]?.Id);
    }

    [Fact]
    public void Overlapped_Views_With_Clipping ()
    {
        // Arrange
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 20, 20),
            Arrangement = ViewArrangement.Overlapped
        };

        // Create container with clipped children
        View container = new ()
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Overlapped
        };

        // This view extends beyond the container bounds
        View view1 = new ()
        {
            Id = "view1",
            X = 5,
            Y = 5,
            Width = 10, // Extends beyond container width
            Height = 10, // Extends beyond container height
            Arrangement = ViewArrangement.Overlapped
        };

        container.Add (view1);
        top.Add (container);

        // Act - click at point (12, 12) which would be inside view1 if not clipped
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (12, 12), false);

        // Assert - should only find top, not the container or view1 since they're clipped
        Assert.Single (result);
        Assert.Equal ("top", result [0]?.Id);

        // Test point inside container but at view1 location
        result = View.GetViewsUnderLocationForRoot (top, new (8, 8), false);

        // Assert - should find all three views
        Assert.Equal (3, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("container", result [1]?.Id);
        Assert.Equal ("view1", result [2]?.Id);
    }

    [Fact]
    public void Only_One_Overlapped_View_Should_Be_Returned_Without_Overlapped_Flag ()
    {
        // Arrange
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 20, 20)

            // Note: Not using ViewArrangement.Overlapped here
        };

        // Create two views with the same location but one on top of the other
        View view1 = new ()
        {
            Id = "view1",
            X = 2,
            Y = 2,
            Width = 6,
            Height = 6
        };

        View view2 = new ()
        {
            Id = "view2",
            X = 2,
            Y = 2,
            Width = 6,
            Height = 6
        };

        top.Add (view1);
        top.Add (view2);

        // Act - click at point (3, 3) which is inside both views
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (3, 3), false);

        // Assert - only top and the topmost view (view2) should be returned
        Assert.Equal (2, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("view2", result [1]?.Id);
        Assert.DoesNotContain (result, v => v?.Id == "view1");
    }

    [Fact]
    public void Tiled_SubViews_Respect_ZOrder_For_Hit_Testing ()
    {
        // Arrange
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 20, 20)

            // Using default arrangement (tiled)
        };

        // Three views at different z-order positions
        View view1 = new ()
        {
            Id = "view1",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };

        View view2 = new ()
        {
            Id = "view2",
            X = 3, // Overlaps view1
            Y = 3, // Overlaps view1
            Width = 5,
            Height = 5
        };

        View view3 = new ()
        {
            Id = "view3",
            X = 5, // Overlaps view2
            Y = 5, // Overlaps view2
            Width = 5,
            Height = 5
        };

        // Add in order (view3 should be topmost)
        top.Add (view1);
        top.Add (view2);
        top.Add (view3);

        // Act - test point (4, 4) which is inside view1 and view2
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (4, 4), false);

        // Assert - Since we're using tiled mode without explicitly setting ViewArrangement.Overlapped,
        // we should only get the topmost view at this position (view2)
        Assert.Equal (2, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("view2", result [1]?.Id);
        Assert.DoesNotContain (result, v => v?.Id == "view1");

        // Act - test point (6, 6) which is inside view2 and view3
        result = View.GetViewsUnderLocationForRoot (top, new (6, 6), false);

        // Assert - Should get view3 (topmost)
        Assert.Equal (2, result.Count);
        Assert.Equal ("top", result [0]?.Id);
        Assert.Equal ("view3", result [1]?.Id);
        Assert.DoesNotContain (result, v => v?.Id == "view2");
    }
}
