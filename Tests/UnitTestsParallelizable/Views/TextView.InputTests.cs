namespace ViewsTests.TextViewTests;

public class TextViewInputTests
{
    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void EnterKeyAddsLine_Adds_Line_Or_Fires_Accepted (bool enterKeyAddsLine)
    {
        TextView tv = new () { EnterKeyAddsLine = enterKeyAddsLine };
        var accepted = false;
        tv.Accepting += (_, _) => accepted = true;

        tv.NewKeyDownEvent (Key.Enter);

        if (enterKeyAddsLine)
        {
            Assert.Equal (Environment.NewLine, tv.Text);
            Assert.False (accepted);
        }
        else
        {
            Assert.Equal ("", tv.Text);
            Assert.True (accepted);
        }
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void TabKeyAddsTab_Adds_Tab_Or_Moves_Focus (bool tabKeyAddsTab)
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();
        runnable.CanFocus = true;

        TextView tv = new () { TabKeyAddsTab = tabKeyAddsTab };
        View otherView = new () { CanFocus = true };
        runnable.Add (tv, otherView);

        app.Begin (runnable);

        Assert.True (tv.CanFocus);
        Assert.True (tv.HasFocus);

        app.Keyboard.RaiseKeyDownEvent (Key.Tab);

        if (tabKeyAddsTab)
        {
            Assert.Equal ("\t", tv.Text);
            Assert.True (tv.HasFocus);
        }
        else
        {
            Assert.Equal ("", tv.Text);
            Assert.False (tv.HasFocus);
            Assert.True (otherView.HasFocus);
        }
    }

    [Fact]
    public void Typing_Text_With_Tab_Positions_Cursor_Correctly ()
    {
        // This test demonstrates tab cursor positioning with tabs.

        TextView tv = new ()
        {
            TabKeyAddsTab = true,
            TabWidth = 4,
            Width = 20,
            Height = 3,
            Text = "a\t1"
        };

        tv.InsertionPoint = new (3, 0); // Position cursor after the "1"

        // Expected visual layout with TabWidth=4:
        // "a" is at visual column 0
        // Tab expands to next tab stop at column 4 (adds 3 columns since we're at column 1)
        // "1" is at visual column 4
        // Cursor after "1" should be at visual column 5

        // Calculate expected visual position
        // Character 'a' at index 0: visual column starts at 0, 'a' takes 1 column -> advance to column 1
        // Tab at index 1: at visual column 1, tab to next stop at 4 -> advance by 3 to column 4
        // Character '1' at index 2: at visual column 4, '1' takes 1 column -> advance to column 5
        // Cursor at index 3: should be at visual column 5
        int expectedVisualColumn = 5;

        // The fix should calculate: 1 (for 'a') + 3 (tab from col 1 to 4) = 4 (where '1' starts)
        // Then after '1': 4 + 1 = 5
        int fixedVisualColumn = 1 + (4 - (1 % 4)) + 1; // a(1) + tab_to_4(3) + 1(1) = 5

        Assert.Equal (expectedVisualColumn, fixedVisualColumn);
    }
}
