// GitHub Copilot - Generated tests for Dim.Has<T> method

namespace ViewBaseTests.Layout;

public class DimHasTests
{
    [Fact]
    public void Has_DirectMatch_ReturnsTrue ()
    {
        Dim dim = Dim.Fill ();
        Assert.True (dim.Has (out DimFill result));
        Assert.Same (dim, result);
    }

    [Fact]
    public void Has_DimAbsolute_DirectMatch_ReturnsTrue ()
    {
        Dim dim = Dim.Absolute (10);
        Assert.True (dim.Has (out DimAbsolute result));
        Assert.Same (dim, result);
    }

    [Fact]
    public void Has_DimAuto_DirectMatch_ReturnsTrue ()
    {
        Dim dim = Dim.Auto ();
        Assert.True (dim.Has (out DimAuto result));
        Assert.Same (dim, result);
    }

    [Fact]
    public void Has_DimPercent_DirectMatch_ReturnsTrue ()
    {
        Dim dim = Dim.Percent (50);
        Assert.True (dim.Has (out DimPercent result));
        Assert.Same (dim, result);
    }

    [Fact]
    public void Has_NoMatch_ReturnsFalse ()
    {
        Dim dim = Dim.Absolute (10);
        Assert.False (dim.Has<DimFill> (out _));
    }

    [Fact]
    public void Has_DimAbsolute_DoesNotMatchDimAuto_ReturnsFalse ()
    {
        Dim dim = Dim.Absolute (10);
        Assert.False (dim.Has<DimAuto> (out _));
    }

    [Fact]
    public void Has_InCombine_Left_ReturnsTrue ()
    {
        Dim combined = Dim.Fill () + 5;
        Assert.True (combined.Has (out DimFill result));
        Assert.IsType<DimFill> (result);
    }

    [Fact]
    public void Has_InCombine_Right_ReturnsTrue ()
    {
        Dim combined = Dim.Absolute (5) + Dim.Percent (50);
        Assert.True (combined.Has (out DimPercent result));
        Assert.IsType<DimPercent> (result);
    }

    [Fact]
    public void Has_InCombine_Subtraction_ReturnsTrue ()
    {
        Dim combined = Dim.Fill () - 5;
        Assert.True (combined.Has (out DimFill result));
        Assert.IsType<DimFill> (result);
    }

    [Fact]
    public void Has_NestedCombine_FindsBothTypes ()
    {
        Dim nested = Dim.Fill () + 5 - Dim.Percent (10);
        Assert.True (nested.Has<DimFill> (out _));
        Assert.True (nested.Has<DimPercent> (out _));
    }

    [Fact]
    public void Has_NestedCombine_FindsDimAbsolute ()
    {
        Dim nested = Dim.Fill () + Dim.Absolute (5);
        Assert.True (nested.Has<DimAbsolute> (out _));
    }

    [Fact]
    public void Has_DimCombine_DirectMatch_ReturnsTrue ()
    {
        Dim combined = Dim.Fill () + 5;
        Assert.True (combined.Has (out DimCombine result));
        Assert.Same (combined, result);
    }

    [Fact]
    public void Has_NestedDimCombine_FindsOuterCombine ()
    {
        // When searching for DimCombine, the outer one is found first (direct match)
        Dim inner = Dim.Fill () + 5;
        Dim outer = inner + Dim.Absolute (10);
        Assert.True (outer.Has (out DimCombine result));
        Assert.Same (outer, result);
    }

    [Fact]
    public void Has_DimView_DirectMatch_ReturnsTrue ()
    {
        View view = new () { Width = 10, Height = 5 };
        Dim dim = Dim.Width (view);
        Assert.True (dim.Has (out DimView result));
        Assert.Same (dim, result);
        view.Dispose ();
    }

    [Fact]
    public void Has_DimFunc_DirectMatch_ReturnsTrue ()
    {
        Dim dim = Dim.Func (_ => 42);
        Assert.True (dim.Has (out DimFunc result));
        Assert.Same (dim, result);
    }

    [Fact]
    public void Has_OutParameter_ReturnsCorrectInstance ()
    {
        DimFill originalFill = new (Dim.Absolute (5));
        Dim combined = originalFill + Dim.Absolute (10);

        Assert.True (combined.Has (out DimFill result));
        Assert.Same (originalFill, result);
    }

    [Fact]
    public void Has_OutParameter_IsNullWhenNotFound ()
    {
        Dim dim = Dim.Absolute (10);

        Assert.False (dim.Has (out DimFill result));
        Assert.Null (result);
    }

    // The following tests document the current behavior where Has<T> does NOT
    // traverse into DimAuto or DimFill's inner Dim properties. This is noted
    // in the QUESTION comment in Dim.Has<T>.

    [Fact]
    public void Has_DimAuto_ContainingMinimum_DoesNotTraverseIntoMinimum ()
    {
        // Current implementation does NOT traverse into DimAuto's MinimumContentDim
        Dim dim = Dim.Auto (minimumContentDim: Dim.Fill ());

        Assert.True (dim.Has<DimAuto> (out _));

        // This returns false because Has<T> does not traverse into DimAuto's properties
        Assert.False (dim.Has<DimFill> (out _));
    }

    [Fact]
    public void Has_DimAuto_ContainingMaximum_DoesNotTraverseIntoMaximum ()
    {
        // Current implementation does NOT traverse into DimAuto's MaximumContentDim
        Dim dim = Dim.Auto (maximumContentDim: Dim.Percent (80));

        Assert.True (dim.Has<DimAuto> (out _));

        // This returns false because Has<T> does not traverse into DimAuto's properties
        Assert.False (dim.Has<DimPercent> (out _));
    }

    [Fact]
    public void Has_DimFill_ContainingMargin_DoesNotTraverseIntoMargin ()
    {
        // Current implementation does NOT traverse into DimFill's Margin
        Dim dim = Dim.Fill (Dim.Percent (10));

        Assert.True (dim.Has<DimFill> (out _));

        // This returns false because Has<T> does not traverse into DimFill's properties
        Assert.False (dim.Has<DimPercent> (out _));
    }

    [Fact]
    public void Has_DimFill_ContainingMinimum_DoesNotTraverseIntoMinimum ()
    {
        // Current implementation does NOT traverse into DimFill's MinimumContentDim
        Dim dim = Dim.Fill (Dim.Absolute (0), Dim.Percent (50));

        Assert.True (dim.Has<DimFill> (out _));

        // This returns false because Has<T> does not traverse into DimFill's properties
        Assert.False (dim.Has<DimPercent> (out _));
    }
}
