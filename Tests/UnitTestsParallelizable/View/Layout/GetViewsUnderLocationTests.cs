#nullable enable

namespace Terminal.Gui.ViewMouseTests;

[Trait ("Category", "Input")]
public class GetViewsUnderLocationTests
{
    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void Returns_Null_If_No_SubViews_Coords_Outside (int testX, int testY)
    {
        // Arrange
        var view = new View
        {
            Frame = new (0, 0, 10, 10)
        };

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = View.GetViewsUnderLocation (location, ViewportSettingsFlags.None);

        // Assert
        Assert.Empty (viewsUnderMouse);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (2, 1)]
    [InlineData (20, 20)]
    public void Returns_Null_If_Start_Not_Visible (int testX, int testY)
    {
        // Arrange
        var view = new View
        {
            Frame = new (0, 0, 10, 10),
            Visible = false
        };

        var location = new Point (testX, testY);

        // Act
        List<View?> viewsUnderMouse = View.GetViewsUnderLocation (location, ViewportSettingsFlags.None);

        // Assert
        Assert.Empty (viewsUnderMouse);
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
        view.Margin!.Thickness = new (marginThickness);
        view.Border!.Thickness = new (borderThickness);
        view.Padding!.Thickness = new (paddingThickness);

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
}

