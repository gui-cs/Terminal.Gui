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
        int fixedVisualColumn = 1 + (4 - 1 % 4) + 1; // a(1) + tab_to_4(3) + 1(1) = 5

        Assert.Equal (expectedVisualColumn, fixedVisualColumn);
    }

    [Fact]
    public void Typing_Text_With_Tab_And_Wide_Characters_Positions_Cursor_Correctly ()
    {
        // This test verifies tab cursor positioning with wide characters (emoji, CJK, etc.)
        // Wide characters take 2 visual columns each

        TextView tv = new ()
        {
            TabKeyAddsTab = true,
            TabWidth = 4,
            Width = 20,
            Height = 3,
            Text = "😀\t1"  // Emoji (2 cols) + tab + "1" (1 col)
        };

        tv.InsertionPoint = new (3, 0); // Position cursor after the "1"

        // Expected visual layout with TabWidth=4:
        // "😀" (emoji) is at visual column 0-1 (takes 2 columns)
        // Tab starts at visual column 2, expands to next tab stop at column 4 (adds 2 columns)
        // "1" is at visual column 4
        // Cursor after "1" should be at visual column 5

        // Calculate expected visual position
        // Character '😀' at index 0: visual column 0, emoji takes 2 columns -> advance to column 2
        // Tab at index 1: at visual column 2, tab to next stop at 4 -> advance by 2 to column 4
        // Character '1' at index 2: at visual column 4, '1' takes 1 column -> advance to column 5
        // Cursor at index 3: should be at visual column 5
        int expectedVisualColumn = 5;

        // The calculation: 2 (for emoji) + 2 (tab from col 2 to 4) + 1 (for '1') = 5
        int fixedVisualColumn = 2 + (4 - 2 % 4) + 1; // emoji(2) + tab_to_4(2) + 1(1) = 5

        Assert.Equal (expectedVisualColumn, fixedVisualColumn);
    }

    [Fact]
    public void Typing_Text_With_Tab_And_Multiple_Wide_Characters_Positions_Cursor_Correctly ()
    {
        // This test verifies tab cursor positioning with multiple wide characters
        // demonstrating tab stops across different starting positions

        TextView tv = new ()
        {
            TabKeyAddsTab = true,
            TabWidth = 8,  // Larger tab width to show multiple scenarios
            Width = 40,
            Height = 3,
            Text = "a😀\t中\tX"  // 'a'(1) + emoji(2) + tab + CJK(2) + tab + 'X'(1)
        };

        tv.InsertionPoint = new (6, 0); // Position cursor after the "X"

        // Expected visual layout with TabWidth=8:
        // "a" is at visual column 0 (1 col) -> cursor at 1
        // "😀" (emoji) at visual column 1-2 (2 cols) -> cursor at 3
        // Tab at visual column 3, next tab stop is at 8 -> adds 5 cols -> cursor at 8
        // "中" (CJK) at visual column 8-9 (2 cols) -> cursor at 10
        // Tab at visual column 10, next tab stop is at 16 -> adds 6 cols -> cursor at 16
        // "X" at visual column 16 (1 col) -> cursor at 17

        // Calculate expected visual position
        int expectedVisualColumn = 17;

        // The calculation:
        // 'a' = 1 col -> at column 1
        // emoji = 2 cols -> at column 3
        // tab from 3 to 8 = 5 cols -> at column 8
        // CJK = 2 cols -> at column 10
        // tab from 10 to 16 = 6 cols -> at column 16
        // 'X' = 1 col -> at column 17
        int fixedVisualColumn = 1 + 2 + (8 - 3 % 8) + 2 + (8 - 10 % 8) + 1;

        Assert.Equal (expectedVisualColumn, fixedVisualColumn);
    }

    [Theory]
    [InlineData ("abc\t1", 4, 5)]   // "abc" (3 cols) + tab to col 4 (1 col) + "1" = col 5
    [InlineData ("abcd\t1", 4, 9)]  // "abcd" (4 cols) + tab to col 8 (4 cols) + "1" = col 9
    [InlineData ("a\t1", 4, 5)]     // "a" (1 col) + tab to col 4 (3 cols) + "1" = col 5
    [InlineData ("ab\t1", 4, 5)]    // "ab" (2 cols) + tab to col 4 (2 cols) + "1" = col 5
    public void Tab_Advances_Correctly_To_Next_Tab_Stop (string text, int tabWidth, int expectedColumn)
    {
        // Test that tabs advance correctly based on current column position
        // Tabs always advance to the next tab stop (multiple of TabWidth)

        TextView tv = new ()
        {
            TabKeyAddsTab = true,
            TabWidth = tabWidth,
            Width = 20,
            Height = 3,
            Text = text
        };

        tv.InsertionPoint = new (text.Length, 0); // Position cursor at end

        // Calculate the visual column position
        int visualColumn = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text [i] == '\t')
            {
                // Standard tab behavior: advance to next tab stop
                visualColumn += tabWidth - visualColumn % tabWidth;
            }
            else
            {
                visualColumn += text [i].ToString ().GetColumns ();
            }
        }

        Assert.Equal (expectedColumn, visualColumn);
    }

    [Fact]
    public void EnterKeyAddsLine_Setter_Should_Not_Scroll_View ()
    {
        // Regression test for https://github.com/gui-cs/Terminal.Gui/issues/3988
        // Setting EnterKeyAddsLine (formerly AllowsReturn) should not scroll the text out of view

        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            X = 1,
            Height = Dim.Fill (2),
            Width = Dim.Fill (2),
            Text = "Heya",
            TabKeyAddsTab = false
        };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.SelectAll ();
        int leftColumnBefore = tv.LeftColumn;

        // Setting EnterKeyAddsLine should not change LeftColumn
        tv.EnterKeyAddsLine = false;

        Assert.Equal (leftColumnBefore, tv.LeftColumn);
    }

    [Fact]
    public void EnterKeyAddsLine_Toggling_Does_Not_Reset_Cursor_Position ()
    {
        // Regression test for https://github.com/gui-cs/Terminal.Gui/issues/3988
        // Setting EnterKeyAddsLine should not reset the cursor position (InsertionPoint)

        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 30,
            Height = 5,
            Text = $"This is the second line.{Environment.NewLine}This is the third "
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Move cursor to a specific position using Ctrl+Backspace twice
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithCtrl);
        Assert.Equal (new (18, 1), tv.InsertionPoint);
        Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third ", tv.Text);

        // Setting EnterKeyAddsLine to false should NOT reset cursor position
        tv.EnterKeyAddsLine = false;
        Assert.Equal (new (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.False (tv.EnterKeyAddsLine);

        // Pressing Enter should trigger Accepted event (not add newline)
        Assert.False (app.Keyboard.RaiseKeyDownEvent (Key.Enter)); // Accepted event not handled
        Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third ", tv.Text);
        Assert.Equal (new (18, 1), tv.InsertionPoint); // Cursor should remain at (18, 1)
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.False (tv.IsSelecting);

        // Setting EnterKeyAddsLine to true should NOT reset cursor position
        tv.EnterKeyAddsLine = true;
        Assert.Equal (new (18, 1), tv.InsertionPoint); // Cursor should still be at (18, 1)
        Assert.True (tv.EnterKeyAddsLine);
        Assert.True (tv.Multiline);

        // Pressing Enter should now add a newline
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Enter));
        Assert.Equal (
                      $"This is the second line.{Environment.NewLine}This is the third {Environment.NewLine}",
                      tv.Text
                     );
        Assert.Equal (new (0, 2), tv.InsertionPoint); // Cursor moved after inserting newline
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.False (tv.IsSelecting);
        Assert.True (tv.EnterKeyAddsLine);
    }
}
