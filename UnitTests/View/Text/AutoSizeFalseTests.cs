using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>Tests of the  <see cref="View.Text"/> property with AutoSize set to false.</summary>
public class AutoSizeFalseTests (ITestOutputHelper output)
{
    [Fact]
    public void AutoSize_False_ResizeView_With_Dim_Fill_After_IsInitialized ()
    {
        var super = new View { Frame = new (0, 0, 30, 80) };
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill () };
        super.Add (view);

        view.Text = "New text\nNew line";
        super.LayoutSubviews ();
        Rectangle expectedViewBounds = new (0, 0, 30, 80);

        Assert.Equal (expectedViewBounds, view.Viewport);
        Assert.False (view.IsInitialized);

        super.BeginInit ();
        super.EndInit ();

        Assert.True (view.IsInitialized);
        Assert.Equal (expectedViewBounds, view.Viewport);
    }

    [Fact]
    [SetupFakeDriver]
    public void AutoSize_False_View_IsEmpty_False_Return_Null_Lines ()
    {
        var text = "Views";
        var view = new View { Width = Dim.Fill () - text.Length, Height = 1, Text = text };
        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };
        frame.Add (view);

        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);
        frame.BeginInit ();
        frame.EndInit ();
        frame.LayoutSubviews ();

        Assert.Equal (5, text.Length);
        Assert.Equal (new (0, 0, 3, 1), view.Frame);
        Assert.Equal (new (3, 1), view.TextFormatter.Size);
        Assert.Equal (new() { "Vie" }, view.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 10, 4), frame.Frame);

        frame.LayoutSubviews ();
        frame.Clear ();
        frame.Draw ();

        var expected = @"
┌────────┐
│Vie     │
│        │
└────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);
        view.Width = Dim.Fill () - text.Length;

        frame.LayoutSubviews ();
        frame.Clear ();
        frame.Draw ();

        Assert.Equal (new (0, 0, 0, 1), view.Frame);
        Assert.Equal (new (0, 1), view.TextFormatter.Size);
        Assert.Equal (new() { string.Empty }, view.TextFormatter.GetLines ());

        expected = @"
┌────────┐
│        │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    [SetupFakeDriver]
    public void AutoSize_False_Width_Height_SetMinWidthHeight_Narrow_Wide_Runes ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (32, 32);
        var top = new View { Width = 32, Height = 32 };

        var text = $"First line{Environment.NewLine}Second line";
        var horizontalView = new View { Width = 20, Height = 1, Text = text };

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        horizontalView.TextFormatter.Size = new (20, 1);

        var verticalView = new View
        {
            Y = 3,
            Height = 20,
            Width = 1,
            Text = text,
            TextDirection = TextDirection.TopBottom_LeftRight
        };

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        verticalView.TextFormatter.Size = new (1, 20);

        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), Text = "Window" };
        frame.Add (horizontalView, verticalView);
        top.Add (frame);
        top.BeginInit ();
        top.EndInit ();

        Assert.Equal (new (0, 0, 20, 1), horizontalView.Frame);
        Assert.Equal (new (0, 3, 1, 20), verticalView.Frame);

        top.Draw ();

        var expected = @"
┌──────────────────────────────┐
│First line Second li          │
│                              │
│                              │
│F                             │
│i                             │
│r                             │
│s                             │
│t                             │
│                              │
│l                             │
│i                             │
│n                             │
│e                             │
│                              │
│S                             │
│e                             │
│c                             │
│o                             │
│n                             │
│d                             │
│                              │
│l                             │
│i                             │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
└──────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        verticalView.Text = $"最初の行{Environment.NewLine}二行目";
        Assert.True (verticalView.TextFormatter.NeedsFormat);

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        // We know these glpyhs are 2 cols wide, so we need to widen the view
        verticalView.Width = 2;
        verticalView.TextFormatter.Size = new (2, 20);
        Assert.True (verticalView.TextFormatter.NeedsFormat);

        top.Draw ();
        Assert.Equal (new (0, 3, 2, 20), verticalView.Frame);

        expected = @"
┌──────────────────────────────┐
│First line Second li          │
│                              │
│                              │
│最                            │
│初                            │
│の                            │
│行                            │
│                              │
│二                            │
│行                            │
│目                            │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
└──────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }
}
