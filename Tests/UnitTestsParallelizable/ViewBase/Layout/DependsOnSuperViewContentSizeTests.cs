#nullable disable

// Claude - Opus 4.5

namespace ViewBaseTests.Layout;

/// <summary>
/// Tests for the DependsOnSuperViewContentSize property on Dim and Pos types.
/// This property is used by DimAuto to categorize subviews without type checking.
/// </summary>
public class DependsOnSuperViewContentSizeTests
{
    #region Dim Tests

    [Fact]
    public void DimAbsolute_DoesNotDependOnSuperViewContentSize ()
    {
        Dim dim = Dim.Absolute (10);
        Assert.False (dim.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimAuto_DoesNotDependOnSuperViewContentSize ()
    {
        Dim dim = Dim.Auto ();
        Assert.False (dim.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimFunc_DoesNotDependOnSuperViewContentSize ()
    {
        Dim dim = Dim.Func (_ => 10);
        Assert.False (dim.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimView_DoesNotDependOnSuperViewContentSize ()
    {
        View view = new ();
        Dim dim = Dim.Width (view);
        Assert.False (dim.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimPercent_DependsOnSuperViewContentSize ()
    {
        Dim dim = Dim.Percent (50);
        Assert.True (dim.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimPercent_AllModes_DependOnSuperViewContentSize ()
    {
        Dim dimContentSize = Dim.Percent (50, DimPercentMode.ContentSize);
        Assert.True (dimContentSize.DependsOnSuperViewContentSize);

        Dim dimPosition = Dim.Percent (50, DimPercentMode.Position);
        Assert.True (dimPosition.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimFill_DependsOnSuperViewContentSize ()
    {
        Dim dim = Dim.Fill ();
        Assert.True (dim.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimFill_WithMargin_DependsOnSuperViewContentSize ()
    {
        Dim dim = Dim.Fill (2);
        Assert.True (dim.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimFill_WithMinimumContentDim_DependsOnSuperViewContentSize ()
    {
        Dim dim = Dim.Fill (0, 10);
        Assert.True (dim.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimFill_WithTo_DependsOnSuperViewContentSize ()
    {
        View view = new ();
        Dim dim = Dim.Fill (view);
        Assert.True (dim.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimCombine_DependsIfEitherChildDepends ()
    {
        // Both depend
        Dim dim1 = Dim.Percent (50) + Dim.Fill ();
        Assert.True (dim1.DependsOnSuperViewContentSize);

        // Left depends
        Dim dim2 = Dim.Percent (50) + Dim.Absolute (10);
        Assert.True (dim2.DependsOnSuperViewContentSize);

        // Right depends
        Dim dim3 = Dim.Absolute (10) + Dim.Fill ();
        Assert.True (dim3.DependsOnSuperViewContentSize);

        // Neither depends
        Dim dim4 = Dim.Absolute (10) + Dim.Absolute (5);
        Assert.False (dim4.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void DimCombine_Subtraction_DependsIfEitherChildDepends ()
    {
        // Both depend
        Dim dim1 = Dim.Fill () - Dim.Percent (10);
        Assert.True (dim1.DependsOnSuperViewContentSize);

        // Left depends
        Dim dim2 = Dim.Percent (50) - Dim.Absolute (10);
        Assert.True (dim2.DependsOnSuperViewContentSize);

        // Right depends
        Dim dim3 = Dim.Absolute (100) - Dim.Fill ();
        Assert.True (dim3.DependsOnSuperViewContentSize);

        // Neither depends
        Dim dim4 = Dim.Absolute (100) - Dim.Absolute (10);
        Assert.False (dim4.DependsOnSuperViewContentSize);
    }

    #endregion

    #region Pos Tests

    [Fact]
    public void PosAbsolute_DoesNotDependOnSuperViewContentSize ()
    {
        Pos pos = Pos.Absolute (10);
        Assert.False (pos.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void PosFunc_DoesNotDependOnSuperViewContentSize ()
    {
        Pos pos = Pos.Func (_ => 10);
        Assert.False (pos.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void PosView_DoesNotDependOnSuperViewContentSize ()
    {
        View view = new ();
        Pos pos = Pos.Left (view);
        Assert.False (pos.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void PosCenter_DependsOnSuperViewContentSize ()
    {
        Pos pos = Pos.Center ();
        Assert.True (pos.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void PosPercent_DependsOnSuperViewContentSize ()
    {
        Pos pos = Pos.Percent (50);
        Assert.True (pos.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void PosAnchorEnd_DependsOnSuperViewContentSize ()
    {
        Pos pos = Pos.AnchorEnd ();
        Assert.True (pos.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void PosAnchorEnd_WithOffset_DependsOnSuperViewContentSize ()
    {
        Pos pos = Pos.AnchorEnd (10);
        Assert.True (pos.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void PosAlign_DependsOnSuperViewContentSize ()
    {
        Pos pos = Pos.Align (Alignment.Center);
        Assert.True (pos.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void PosCombine_DependsIfEitherChildDepends ()
    {
        // Both depend
        Pos pos1 = Pos.Center () + Pos.Percent (10);
        Assert.True (pos1.DependsOnSuperViewContentSize);

        // Left depends
        Pos pos2 = Pos.Center () + Pos.Absolute (10);
        Assert.True (pos2.DependsOnSuperViewContentSize);

        // Right depends
        Pos pos3 = Pos.Absolute (10) + Pos.Percent (50);
        Assert.True (pos3.DependsOnSuperViewContentSize);

        // Neither depends
        Pos pos4 = Pos.Absolute (10) + Pos.Absolute (5);
        Assert.False (pos4.DependsOnSuperViewContentSize);
    }

    [Fact]
    public void PosCombine_Subtraction_DependsIfEitherChildDepends ()
    {
        // Both depend
        Pos pos1 = Pos.AnchorEnd () - Pos.Percent (10);
        Assert.True (pos1.DependsOnSuperViewContentSize);

        // Left depends
        Pos pos2 = Pos.Center () - Pos.Absolute (10);
        Assert.True (pos2.DependsOnSuperViewContentSize);

        // Right depends (unusual but possible)
        Pos pos3 = Pos.Absolute (100) - Pos.Percent (10);
        Assert.True (pos3.DependsOnSuperViewContentSize);

        // Neither depends
        Pos pos4 = Pos.Absolute (100) - Pos.Absolute (10);
        Assert.False (pos4.DependsOnSuperViewContentSize);
    }

    #endregion
}
