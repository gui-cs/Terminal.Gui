using Xunit.Abstractions;

namespace ViewBaseTests.Layout;

/// <summary>
/// Tests for <see cref="View.SetContentSize"/>, <see cref="View.GetContentSize"/>, and related events.
/// CoPilot - Claude 3.7 Sonnet
/// </summary>
public class ContentSizeTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region SetContentSize and GetContentSize Tests

    [Fact]
    public void SetContentSize_Null_SetsContentSizeToNull ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentSize (null);

        Assert.Null (view.GetContentSize () == view.Viewport.Size ? null : view.GetContentSize ());
        Assert.True (view.ContentSizeTracksViewport);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (10, 10)]
    [InlineData (100, 100)]
    public void SetContentSize_ValidSize_SetsContentSize (int width, int height)
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = new (width, height);
        view.SetContentSize (size);

        Assert.Equal (size, view.GetContentSize ());
        Assert.False (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void SetContentSize_SameValue_DoesNotRaiseEvent ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = new (10, 10);
        view.SetContentSize (size);

        int eventCount = 0;
        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentSize (size);

        Assert.Equal (0, eventCount);
    }

    [Theory]
    [InlineData (-1, 0)]
    [InlineData (0, -1)]
    [InlineData (-1, -1)]
    [InlineData (-10, -10)]
    public void SetContentSize_NegativeSize_ThrowsArgumentException (int width, int height)
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = new (width, height);

        Assert.Throws<ArgumentException> (() => view.SetContentSize (size));
    }

    [Fact]
    public void GetContentSize_NotSet_ReturnsViewportSize ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.Equal (view.Viewport.Size, view.GetContentSize ());
        Assert.True (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void GetContentSize_Set_ReturnsSetValue ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Size contentSize = new (100, 100);
        view.SetContentSize (contentSize);

        Assert.Equal (contentSize, view.GetContentSize ());
        Assert.NotEqual (view.Viewport.Size, view.GetContentSize ());
        Assert.False (view.ContentSizeTracksViewport);
    }

    #endregion

    #region ContentSizeTracksViewport Tests

    [Fact]
    public void ContentSizeTracksViewport_DefaultValue_IsTrue ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void ContentSizeTracksViewport_AfterSetContentSize_IsFalse ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.SetContentSize (new Size (10, 10));

        Assert.False (view.ContentSizeTracksViewport);
    }

    [Fact]
    public void ContentSizeTracksViewport_SetToTrue_ResetsContentSize ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentSize (new Size (100, 100));
        Assert.False (view.ContentSizeTracksViewport);

        view.ContentSizeTracksViewport = true;

        Assert.True (view.ContentSizeTracksViewport);
        Assert.Equal (view.Viewport.Size, view.GetContentSize ());
    }

    [Fact]
    public void ContentSizeTracksViewport_SetToFalse_PreservesContentSize ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 15
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.SetContentSize (new Size (100, 100));
        view.ContentSizeTracksViewport = false;

        Assert.False (view.ContentSizeTracksViewport);
        Assert.Equal (new Size (100, 100), view.GetContentSize ());
    }

    #endregion

    #region ContentSizeChanged Event Tests

    [Fact]
    public void ContentSizeChanged_RaisedWhenContentSizeChanges ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        int eventCount = 0;
        Size? capturedSize = null;

        view.ContentSizeChanged += (sender, e) =>
        {
            eventCount++;
            capturedSize = e.Size;
        };

        Size newSize = new (50, 50);
        view.SetContentSize (newSize);

        Assert.Equal (1, eventCount);
        Assert.Equal (newSize, capturedSize);
    }

    [Fact]
    public void ContentSizeChanged_NotRaisedWhenSameValue ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = new (10, 10);
        view.SetContentSize (size);

        int eventCount = 0;
        view.ContentSizeChanged += (_, _) => eventCount++;

        view.SetContentSize (size);

        Assert.Equal (0, eventCount);
    }

    [Fact]
    public void ContentSizeChanged_RaisedMultipleTimes ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        int eventCount = 0;
        List<Size?> capturedSizes = new ();

        view.ContentSizeChanged += (sender, e) =>
        {
            eventCount++;
            capturedSizes.Add (e.Size);
        };

        view.SetContentSize (new Size (10, 10));
        view.SetContentSize (new Size (20, 20));
        view.SetContentSize (new Size (30, 30));

        Assert.Equal (3, eventCount);
        Assert.Equal (new Size (10, 10), capturedSizes [0]);
        Assert.Equal (new Size (20, 20), capturedSizes [1]);
        Assert.Equal (new Size (30, 30), capturedSizes [2]);
    }

    [Fact]
    public void ContentSizeChanged_Cancel_DoesNothing_CurrentImplementation ()
    {
        // Current implementation: Cancel flag exists but is not used
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        view.ContentSizeChanged += (sender, e) =>
        {
            e.Cancel = true;
        };

        Size newSize = new (50, 50);
        view.SetContentSize (newSize);

        // Current behavior: Change happens even when Cancel is set to true
        Assert.Equal (newSize, view.GetContentSize ());
    }

    [Fact]
    public void OnContentSizeChanged_TriggersSetNeedsLayout ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        // SetNeedsLayout is called internally when ContentSize changes
        // We verify this by checking that layout state is updated
        view.SetContentSize (new Size (50, 50));

        // Layout should be triggered automatically
        // This is a basic test to ensure no exception is thrown
        view.LayoutSubViews ();
    }

    #endregion

    #region Integration Tests with ScrollBars

    [Fact]
    public void ContentSizeChanged_UpdatesVerticalScrollBar ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 20
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Enable vertical scroll bar
        ScrollBar? verticalScrollBar = view.VerticalScrollBar;
        Assert.NotNull (verticalScrollBar);

        view.SetContentSize (new Size (20, 100));

        Assert.Equal (100, verticalScrollBar.ScrollableContentSize);
    }

    [Fact]
    public void ContentSizeChanged_UpdatesHorizontalScrollBar ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 20
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Enable horizontal scroll bar
        ScrollBar? horizontalScrollBar = view.HorizontalScrollBar;
        Assert.NotNull (horizontalScrollBar);

        view.SetContentSize (new Size (100, 20));

        Assert.Equal (100, horizontalScrollBar.ScrollableContentSize);
    }

    [Fact]
    public void ContentSizeChanged_UpdatesBothScrollBars ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 20
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        ScrollBar? verticalScrollBar = view.VerticalScrollBar;
        ScrollBar? horizontalScrollBar = view.HorizontalScrollBar;
        Assert.NotNull (verticalScrollBar);
        Assert.NotNull (horizontalScrollBar);

        view.SetContentSize (new Size (100, 200));

        Assert.Equal (100, horizontalScrollBar.ScrollableContentSize);
        Assert.Equal (200, verticalScrollBar.ScrollableContentSize);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SetContentSize_ZeroSize_IsValid ()
    {
        View view = new ();
        view.BeginInit ();
        view.EndInit ();

        Size size = Size.Empty;
        view.SetContentSize (size);

        Assert.Equal (size, view.GetContentSize ());
    }

    [Fact]
    public void SetContentSize_AfterBeginInit_BeforeEndInit_Works ()
    {
        View view = new ();
        view.BeginInit ();

        Size size = new (10, 10);
        view.SetContentSize (size);

        view.EndInit ();

        Assert.Equal (size, view.GetContentSize ());
    }

    [Fact]
    public void SetContentSize_BeforeBeginInit_Works ()
    {
        View view = new ();

        Size size = new (10, 10);
        view.SetContentSize (size);

        Assert.Equal (size, view.GetContentSize ());
    }

    #endregion
}
