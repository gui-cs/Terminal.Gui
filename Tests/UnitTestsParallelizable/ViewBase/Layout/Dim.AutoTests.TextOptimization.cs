using static Terminal.Gui.ViewBase.Dim;

namespace ViewBaseTests.Layout;

// Claude - Opus 4.8
// Diagnostics for issue #5499: changing Text on a view sized solely by its Text (both Width and Height are exactly
// DimAutoStyle.Text) must skip SetNeedsLayout - and the ancestor propagation it triggers - when the new Text produces
// the same Frame size. The content still changes, so a reformat and redraw must still happen.
public partial class DimAutoTests
{
    [Fact]
    public void Text_SameSize_DoesNotSetNeedsLayout_ButStillRedraws ()
    {
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        view.Layout ();
        Assert.Equal (new Rectangle (0, 0, 8, 1), view.Frame);

        view.NeedsLayout = false;
        view.ClearNeedsDraw ();

        // Same width and height (8x1), different content
        view.Text = "12:00:01";

        Assert.False (view.NeedsLayout);
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void Text_SameSize_SetsTextFormatterNeedsFormat ()
    {
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        view.Layout ();

        view.NeedsLayout = false;
        view.TextFormatter.NeedsFormat = false;

        view.Text = "12:00:01";

        Assert.True (view.TextFormatter.NeedsFormat);
    }

    [Fact]
    public void Text_SameSize_DoesNotPropagateNeedsLayoutToSuperView ()
    {
        // This is the key diagnostic the issue asked for: an identical-size text update must not mark the ancestor
        // chain as needing layout.
        View superView = new () { Width = 50, Height = 10 };
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        superView.Add (view);
        superView.Layout (new Size (50, 10));
        Assert.Equal (new Rectangle (0, 0, 8, 1), view.Frame);

        superView.NeedsLayout = false;
        view.NeedsLayout = false;

        view.Text = "12:00:01";

        Assert.False (view.NeedsLayout);
        Assert.False (superView.NeedsLayout);
    }

    [Fact]
    public void Text_DifferentSize_SetsNeedsLayout_AndPropagatesToSuperView ()
    {
        View superView = new () { Width = 50, Height = 10 };
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        superView.Add (view);
        superView.Layout (new Size (50, 10));

        superView.NeedsLayout = false;
        view.NeedsLayout = false;

        // Wider text changes the Frame size
        view.Text = "12:00:00 in the morning";

        Assert.True (view.NeedsLayout);
        Assert.True (superView.NeedsLayout);
    }

    [Fact]
    public void Text_SameSize_PreservesPendingLayoutRequest ()
    {
        // The optimization only avoids *setting* NeedsLayout; it never clears it. A layout request that was already
        // pending must survive an identical-size text change.
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        view.Layout ();

        view.SetNeedsLayout ();
        Assert.True (view.NeedsLayout);

        view.Text = "12:00:01";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_AutoStyle_BypassesOptimization ()
    {
        // DimAutoStyle.Auto (Content | Text) also depends on content/subviews, so the optimization must not apply
        // even when the text size is unchanged.
        View view = new () { Width = Auto (), Height = Auto (), Text = "12:00:00" };
        view.Layout ();

        view.NeedsLayout = false;

        view.Text = "12:00:01";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_FixedDimensions_BypassOptimization ()
    {
        // The optimization is conservatively scoped to views sized solely by Text. A fixed-size view still routes
        // through SetNeedsLayout on a text change.
        View view = new () { Width = 20, Height = 3, Text = "12:00:00" };
        view.Layout ();

        view.NeedsLayout = false;

        view.Text = "12:00:01";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_NotYetLaidOut_SetsNeedsLayout ()
    {
        // _frame is null until the first layout pass; the optimization must bypass so the first layout can establish
        // the Frame.
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text) };

        view.Text = "12:00:00";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_SameSize_WithMinimum_DoesNotSetNeedsLayout ()
    {
        // Both texts are narrower than the minimum, so the minimum anchor clamps the width to the same value. The
        // predictor reuses DimAuto.Calculate, so the min anchor is honored.
        View view = new ()
        {
            Width = Auto (DimAutoStyle.Text, minimumContentDim: 20),
            Height = Auto (DimAutoStyle.Text),
            Text = "ab"
        };
        view.Layout ();
        Assert.Equal (20, view.Frame.Width);

        view.NeedsLayout = false;

        view.Text = "cd";

        Assert.Equal (20, view.Frame.Width);
        Assert.False (view.NeedsLayout);
    }

    [Fact]
    public void Text_ExceedsMinimum_SetsNeedsLayout ()
    {
        // Growing past the minimum changes the resulting width, so layout must run.
        View view = new ()
        {
            Width = Auto (DimAutoStyle.Text, minimumContentDim: 20),
            Height = Auto (DimAutoStyle.Text),
            Text = "ab"
        };
        view.Layout ();
        Assert.Equal (20, view.Frame.Width);

        view.NeedsLayout = false;

        view.Text = new string ('x', 30);

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_SameSize_WithBorderAdornment_DoesNotSetNeedsLayout ()
    {
        // The adornment thickness is part of the Frame size. The predictor must account for it (it does, by reusing
        // DimAuto.Calculate which adds adornment thickness).
        View view = new ()
        {
            Width = Auto (DimAutoStyle.Text),
            Height = Auto (DimAutoStyle.Text),
            BorderStyle = LineStyle.Single, // 1-thick adornment on each side
            Text = "12:00:00"
        };
        view.Layout ();
        Assert.Equal (new Rectangle (0, 0, 10, 3), view.Frame); // 8 + 2 wide, 1 + 2 tall

        view.NeedsLayout = false;

        view.Text = "12:00:01";

        Assert.Equal (new Rectangle (0, 0, 10, 3), view.Frame);
        Assert.False (view.NeedsLayout);
    }
}
