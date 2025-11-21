using UnitTests;

namespace UnitTests_Parallelizable.ViewsTests;

/// <summary>
///     Tests for TextView's modern View-based scrolling infrastructure integration.
/// </summary>
public class TextViewScrollingTests : FakeDriverBase
{
    [Fact]
    public void TextView_Uses_SetContentSize_For_Scrolling ()
    {
        IDriver driver = CreateFakeDriver ();
        
        var textView = new TextView
        {
            Width = 10,
            Height = 5,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7",
            Driver = driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Content size should reflect the number of lines
        Size contentSize = textView.GetContentSize ();
        Assert.Equal (7, contentSize.Height); // 7 lines of text
        Assert.True (contentSize.Width >= 6); // At least as wide as "Line 1"
    }

    [Fact]
    public void VerticalScrollBar_AutoShow_Enabled_By_Default ()
    {
        IDriver driver = CreateFakeDriver ();
        
        var textView = new TextView
        {
            Width = 10,
            Height = 3,
            Driver = driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        // VerticalScrollBar should have AutoShow enabled
        Assert.True (textView.VerticalScrollBar.AutoShow);
    }

    [Fact]
    public void HorizontalScrollBar_AutoShow_Tracks_WordWrap ()
    {
        IDriver driver = CreateFakeDriver ();
        
        var textView = new TextView
        {
            Width = 10,
            Height = 3,
            WordWrap = false,
            Driver = driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        // When WordWrap is false, HorizontalScrollBar AutoShow should be true
        Assert.True (textView.HorizontalScrollBar.AutoShow);

        // When WordWrap is true, HorizontalScrollBar AutoShow should be false
        textView.WordWrap = true;
        Assert.False (textView.HorizontalScrollBar.AutoShow);
    }

    [Fact]
    public void TextView_Viewport_Syncs_With_Scrolling ()
    {
        IDriver driver = CreateFakeDriver ();
        
        var textView = new TextView
        {
            Width = 20,
            Height = 5,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7",
            Driver = driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Initially, Viewport.Y should be 0
        Assert.Equal (0, textView.Viewport.Y);
        Assert.Equal (0, textView.TopRow);

        // Scroll down
        textView.TopRow = 2;
        
        // Viewport.Y should update to match
        Assert.Equal (2, textView.Viewport.Y);
        Assert.Equal (2, textView.TopRow);
    }

    [Fact]
    public void TextView_ContentSize_Updates_When_Text_Changes ()
    {
        IDriver driver = CreateFakeDriver ();
        
        var textView = new TextView
        {
            Width = 20,
            Height = 5,
            Text = "Short",
            Driver = driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Size initialContentSize = textView.GetContentSize ();
        Assert.Equal (1, initialContentSize.Height); // 1 line

        // Add more lines
        textView.Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";

        Size newContentSize = textView.GetContentSize ();
        Assert.Equal (5, newContentSize.Height); // 5 lines
    }

    [Fact]
    public void TextView_LeftColumn_Syncs_With_Viewport_X ()
    {
        IDriver driver = CreateFakeDriver ();
        
        var textView = new TextView
        {
            Width = 10,
            Height = 3,
            Text = "This is a very long line that should require horizontal scrolling",
            WordWrap = false,
            Driver = driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Initially at column 0
        Assert.Equal (0, textView.Viewport.X);
        Assert.Equal (0, textView.LeftColumn);

        // Scroll horizontally
        textView.LeftColumn = 5;
        
        // Viewport.X should update
        Assert.Equal (5, textView.Viewport.X);
        Assert.Equal (5, textView.LeftColumn);
    }

    [Fact]
    public void TextView_ScrollTo_Updates_Viewport ()
    {
        IDriver driver = CreateFakeDriver ();
        
        var textView = new TextView
        {
            Width = 20,
            Height = 5,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10",
            Driver = driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Scroll to row 3
        textView.ScrollTo (3, isRow: true);
        
        Assert.Equal (3, textView.TopRow);
        Assert.Equal (3, textView.Viewport.Y);
    }
}
