namespace Terminal.Gui.LayoutTests;

public partial class DimAutoTests
{
    // Tests all the Dim Types in SubView scenarios to ensure DimAutoStyle.Content is calculated correctly

    #region DimAbsolute

    [Theory]
    [InlineData (0, 15, 15)]
    [InlineData (1, 15, 16)]
    [InlineData (-1, 15, 14)]
    public void With_SubView_Using_DimAbsolute (int subViewOffset, int dimAbsoluteSize, int expectedSize)
    {
        var view = new View ();

        var subview = new View
        {
            X = subViewOffset,
            Y = subViewOffset,
            Width = Dim.Absolute (dimAbsoluteSize),
            Height = Dim.Absolute (dimAbsoluteSize)
        };
        view.Add (subview);

        Dim dim = Dim.Auto (DimAutoStyle.Content);

        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = dim.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedSize, calculatedWidth);
        Assert.Equal (expectedSize, calculatedHeight);
    }

    #endregion DimAbsolute

    #region DimPercent

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData (0, 50, 0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 100, 100, 100, 100, 100, 100)]
    [InlineData (0, 50, 100, 100, 100, 100, 100, 100)]
    public void With_SubView_Using_DimPercent (
        int subViewOffset,
        int percent,
        int minWidth,
        int maxWidth,
        int minHeight,
        int maxHeight,
        int expectedWidth,
        int expectedHeight
    )
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var subview = new View
        {
            X = subViewOffset,
            Y = subViewOffset,
            Width = Dim.Percent (percent),
            Height = Dim.Percent (percent)
        };
        view.Add (subview);

        // Assuming the calculation is done after layout
        int calculatedX = view.X.Calculate (100, view.Width, view, Dimension.Width);
        int calculatedY = view.Y.Calculate (100, view.Height, view, Dimension.Height);
        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedWidth, calculatedWidth); // subview's width
        Assert.Equal (expectedHeight, calculatedHeight); // subview's height
        Assert.Equal (subViewOffset, calculatedX);
        Assert.Equal (subViewOffset, calculatedY);

        view.SetRelativeLayout (new (100, 100));
        view.LayoutSubViews ();

        Assert.Equal (expectedWidth * (percent / 100f), subview.Viewport.Width);
        Assert.Equal (expectedHeight * (percent / 100f), subview.Viewport.Height);
    }
    #endregion DimPercent

    #region DimFill

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 0, 0)]
    [InlineData (0, 20, 0, 10, 0, 0)]
    [InlineData (0, 21, 0, 11, 0, 0)]
    [InlineData (1, 21, 1, 11, 1, 1)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_DimFill (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var subview = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        view.Add (subview);

        // Assuming the calculation is done after layout
        int calculatedX = view.X.Calculate (100, view.Width, view, Dimension.Width);
        int calculatedY = view.Y.Calculate (100, view.Height, view, Dimension.Height);
        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedWidth, calculatedWidth);
        Assert.Equal (expectedHeight, calculatedHeight);

        Assert.Equal (0, calculatedX);
        Assert.Equal (0, calculatedY);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 19, 0, 9, 2, 4)]
    [InlineData (0, 20, 0, 10, 2, 4)]
    [InlineData (0, 21, 0, 11, 2, 4)]
    [InlineData (1, 21, 1, 11, 2, 4)]
    [InlineData (21, 21, 11, 11, 21, 11)]
    public void With_SubView_Using_DimFill_And_Another_SubView (int minWidth, int maxWidth, int minHeight, int maxHeight, int expectedWidth, int expectedHeight)
    {
        var view = new View
        {
            Width = Dim.Auto (minimumContentDim: minWidth, maximumContentDim: maxWidth),
            Height = Dim.Auto (minimumContentDim: minHeight, maximumContentDim: maxHeight)
        };

        var absView = new View
        {
            X = 1,
            Y = 2,
            Width = 1,
            Height = 2
        };
        view.Add (absView);

        var subview = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        view.Add (subview);

        // Assuming the calculation is done after layout
        int calculatedX = view.X.Calculate (100, view.Width, view, Dimension.Width);
        int calculatedY = view.Y.Calculate (100, view.Height, view, Dimension.Height);
        int calculatedWidth = view.Width.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = view.Height.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (expectedWidth, calculatedWidth);
        Assert.Equal (expectedHeight, calculatedHeight);

        Assert.Equal (0, calculatedX);
        Assert.Equal (0, calculatedY);
    }

    #endregion

    #region DimFunc

    [Fact]
    public void With_SubView_Using_DimFunc ()
    {
        var view = new View ();
        var subview = new View { Width = Dim.Func (_ => 20), Height = Dim.Func (_ => 25) };
        view.Add (subview);

        subview.SetRelativeLayout (new (100, 100));

        Dim dim = Dim.Auto (DimAutoStyle.Content);

        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = dim.Calculate (0, 100, view, Dimension.Height);

        Assert.Equal (20, calculatedWidth);
        Assert.Equal (25, calculatedHeight);
    }

    #endregion DimFunc

    #region DimView

    [Fact]
    public void With_SubView_Using_DimView ()
    {
        var view = new View ();
        var subview = new View { Width = 30, Height = 40 };
        var subSubView = new View { Width = Dim.Width (subview), Height = Dim.Height (subview) };
        view.Add (subview);
        view.Add (subSubView);

        subview.SetRelativeLayout (new (100, 100));

        Dim dim = Dim.Auto (DimAutoStyle.Content);

        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);
        int calculatedHeight = dim.Calculate (0, 100, view, Dimension.Height);

        // Expecting the size to match the subview, which is the largest
        Assert.Equal (30, calculatedWidth);
        Assert.Equal (40, calculatedHeight);
    }

    #endregion DimView

    #region DimCombine
    // TODO: Need DimCombine tests
    #endregion DimCombine
}
