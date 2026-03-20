namespace ViewBaseTests.Viewport;

public class ViewScrollbarTests
{
    [Fact]
    public void Horizonal_Constructor_Defaults ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);
        Assert.Null (view.Padding.View);
        Assert.Equal (Thickness.Empty, view.Padding.Thickness);
        Assert.False (view.HorizontalScrollBar.Visible);
    }

    [Fact]
    public void Horizontal_ScrollBarVisibilityMode_Manual_Creates_Invisible ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.HorizontalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Manual;
        Assert.False (view.HorizontalScrollBar.Visible);
        Assert.Equal (Thickness.Empty, view.Padding.Thickness);
        Assert.Equal (new Rectangle (0, 10, 10, 1), view.HorizontalScrollBar.Frame);
    }

    [Fact]
    public void Horizontal_HasHorizontalScrollbar_Sets_VisibilityMode_Auto ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar;
        Assert.Equal (ScrollBarVisibilityMode.Auto, view.HorizontalScrollBar.VisibilityMode);
        Assert.False (view.HorizontalScrollBar.Visible);
        Assert.Equal (new Thickness (0, 0, 0, 0), view.Padding.Thickness);
    }

    [Fact]
    public void Horizontal_HasHorizontalScrollBar_SetContentSize_MakesVisible ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar;
        view.SetContentSize (new Size (20, 10));
        Assert.True (view.HorizontalScrollBar.Visible);
        Assert.Equal (new Thickness (0, 0, 0, 1), view.Padding.Thickness);
    }

    [Fact] // Copilot
    public void Horizontal_ScrollBar_Initialized_When_View_Initialized ()
    {
        View view = new ();

        view.ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar;

        Assert.False (view.IsInitialized);
        Assert.False (view.HorizontalScrollBar.IsInitialized);
        Assert.False (view.Padding.View?.IsInitialized);

        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.IsInitialized);
        Assert.True (view.Padding.View?.IsInitialized);
        Assert.True (view.HorizontalScrollBar.IsInitialized);
        Assert.False (view.HorizontalScrollBar.Visible);

        Assert.Equal (0, view.HorizontalScrollBar.ScrollableContentSize);
    }

    [Fact] // Copilot
    public void Horizontal_ScrollBar_View_Set_Frame_Updates_ScrollableContentSize ()
    {
        View view = new ();

        view.ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar;

        Assert.Equal (0, view.HorizontalScrollBar.ScrollableContentSize);
        Assert.Equal (0, view.HorizontalScrollBar.VisibleContentSize);
        view.Frame = new Rectangle (0, 0, 10, 10);
        Assert.Equal (new Size (10, 10), view.GetContentSize ());
        Assert.Equal (new Size (10, 10), view.Viewport.Size);
        Assert.Equal (10, view.HorizontalScrollBar.VisibleContentSize);
        Assert.Equal (10, view.HorizontalScrollBar.ScrollableContentSize);
        Assert.False (view.HorizontalScrollBar.Visible);
    }

    [Fact]
    public void Vertical_Constructor_Defaults ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);
        Assert.Null (view.Padding.View);
        Assert.Equal (Thickness.Empty, view.Padding.Thickness);
        Assert.False (view.VerticalScrollBar.Visible);
    }

    [Fact]
    public void Vertical_ScrollBarVisibilityMode_Manual_Creates_Invisible ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.VerticalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Manual;
        Assert.False (view.VerticalScrollBar.Visible);
        Assert.Equal (Thickness.Empty, view.Padding.Thickness);
        Assert.Equal (new Rectangle (10, 0, 1, 10), view.VerticalScrollBar.Frame);
    }

    [Fact]
    public void Vertical_HasVerticalScrollbar_Sets_VisibilityMode_Auto ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar;
        Assert.Equal (ScrollBarVisibilityMode.Auto, view.VerticalScrollBar.VisibilityMode);
        Assert.False (view.VerticalScrollBar.Visible);
        Assert.Equal (new Thickness (0, 0, 0, 0), view.Padding.Thickness);
    }

    [Fact]
    public void Vertical_HasVerticalScrollBar_SetContentSize_MakesVisible ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar;
        view.SetContentSize (new Size (10, 20));
        Assert.True (view.VerticalScrollBar.Visible);
        Assert.Equal (new Thickness (0, 0, 1, 0), view.Padding.Thickness);
    }

    [Fact] // Copilot
    public void Vertical_ScrollBar_Initialized_When_View_Initialized ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar;
        view.SetContentSize (new Size (10, 20));

        Assert.False (view.IsInitialized);
        Assert.False (view.VerticalScrollBar.IsInitialized);

        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.IsInitialized);
        Assert.True (view.VerticalScrollBar.IsInitialized);
        Assert.True (view.VerticalScrollBar.Visible);
    }
}
