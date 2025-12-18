namespace ViewBaseTests.AdornmentTests;

/// <summary>
///     Tests for keyboard-based border arrangement mode (CTRL-F5).
///     These tests verify that arrangement buttons are properly shown for different ViewArrangement configurations.
/// </summary>
[Trait ("Category", "Adornment")]
[Trait ("Category", "Border")]
public class BorderArrangementKeyboardTests
{
    /// <summary>
    ///     Tests that keyboard arrangement mode (CTRL-F5) properly shows arrangement buttons
    ///     for ViewArrangement.LeftResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_LeftResizable_ShowsLeftSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act - Enter keyboard arrange mode (ViewArrangement.Fixed triggers keyboard mode)
        bool? result = view.Border!.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, view.Border.Arranging);

        // Check that the left size button was created and is visible
        View? leftButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "leftSizeButton");
        Assert.NotNull (leftButton);
        Assert.True (leftButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.RightResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_RightResizable_ShowsRightSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = view.Border!.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, view.Border.Arranging);

        View? rightButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "rightSizeButton");
        Assert.NotNull (rightButton);
        Assert.True (rightButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.TopResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_TopResizable_ShowsTopSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = view.Border!.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, view.Border.Arranging);

        View? topButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "topSizeButton");
        Assert.NotNull (topButton);
        Assert.True (topButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.BottomResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_BottomResizable_ShowsBottomSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.BottomResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = view.Border!.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, view.Border.Arranging);

        View? bottomButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "bottomSizeButton");
        Assert.NotNull (bottomButton);
        Assert.True (bottomButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.Movable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_Movable_ShowsMoveButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = view.Border!.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.Equal (ViewArrangement.Movable, view.Border.Arranging);

        View? moveButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "moveButton");
        Assert.NotNull (moveButton);
        Assert.True (moveButton.Visible);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for combined arrangements like LeftResizable | BottomResizable
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_LeftAndBottomResizable_ShowsCorrectButtons ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.LeftResizable | ViewArrangement.BottomResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = view.Border!.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, view.Border.Arranging);

        View? leftButton = view.Border.SubViews.FirstOrDefault (v => v is { Id: "leftSizeButton", Visible: true });
        Assert.NotNull (leftButton);

        View? bottomButton = view.Border.SubViews.FirstOrDefault (v => v is { Id: "bottomSizeButton", Visible: true });
        Assert.NotNull (bottomButton);

        View? moveButton = view.Border.SubViews.FirstOrDefault (v => v is { Id: "moveButton", Visible: true });
        Assert.Null (moveButton);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for ViewArrangement.Resizable (all directions).
    ///     For fully Resizable views, only move and all-size buttons should be visible in keyboard mode.
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_Resizable_ShowsAllSizeButton ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.Resizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = view.Border!.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, view.Border.Arranging);

        // For fully Resizable, only the all-size button should be visible (not individual direction buttons)
        View? allSizeButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "allSizeButton");
        Assert.NotNull (allSizeButton);
        Assert.True (allSizeButton.Visible);

        // Individual direction buttons should be visible for fully Resizable
        View? leftButton = view.Border.SubViews.FirstOrDefault (v => v is { Id: "leftSizeButton", Visible: true });
        Assert.NotNull (leftButton);

        View? rightButton = view.Border.SubViews.FirstOrDefault (v => v is { Id: "rightSizeButton", Visible: true });
        Assert.NotNull (rightButton);

        View? topButton = view.Border.SubViews.FirstOrDefault (v => v is { Id: "topSizeButton", Visible: true });

        View? bottomButton = view.Border.SubViews.FirstOrDefault (v => v is { Id: "bottomSizeButton", Visible: true });
        Assert.NotNull (bottomButton);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode properly shows arrangement buttons
    ///     for TopResizable | RightResizable combination
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_TopAndRightResizable_ShowsCorrectButtons ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.TopResizable | ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = view.Border!.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);
        Assert.NotEqual (ViewArrangement.Fixed, view.Border.Arranging);

        View? topButton = view.Border.SubViews.FirstOrDefault (v => v is { Id: "topSizeButton", Visible: true });
        Assert.NotNull (topButton);

        View? rightButton = view.Border.SubViews.FirstOrDefault (v => v is { Id: "rightSizeButton", Visible: true });
        Assert.NotNull (rightButton);

        // Cleanup
        superView.Dispose ();
    }

    /// <summary>
    ///     Tests that keyboard arrangement mode only shows buttons for enabled arrangements
    /// </summary>
    [Fact]
    public void EnterArrangeMode_Keyboard_OnlyShowsButtonsForEnabledArrangements ()
    {
        // Arrange
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single,
            X = 20,
            Y = 10,
            Width = 40,
            Height = 10
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        // Act
        bool? result = view.Border!.EnterArrangeMode (ViewArrangement.Fixed);

        // Assert
        Assert.True (result);

        // Only left button should be visible
        View? leftButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "leftSizeButton");
        Assert.NotNull (leftButton);
        Assert.True (leftButton.Visible);

        // Other buttons should not exist or be invisible
        View? rightButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "rightSizeButton");
        Assert.True (rightButton == null || !rightButton.Visible);

        View? topButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "topSizeButton");
        Assert.True (topButton == null || !topButton.Visible);

        View? bottomButton = view.Border.SubViews.FirstOrDefault (v => v.Id == "bottomSizeButton");
        Assert.True (bottomButton == null || !bottomButton.Visible);

        // Cleanup
        superView.Dispose ();
    }
}
