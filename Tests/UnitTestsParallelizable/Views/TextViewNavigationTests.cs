using UnitTests;

namespace UnitTests_Parallelizable.ViewsTests;

/// <summary>
///     Tests for TextView navigation, tabs, and cursor positioning.
///     These replace the old non-parallelizable tests that had hard-coded viewport dimensions.
/// </summary>
public class TextViewNavigationTests : FakeDriverBase
{
    [Fact]
    public void Tab_And_BackTab_Navigation_Without_Text ()
    {
        var textView = new TextView
        {
            Width = 30,
            Height = 10,
            Text = ""
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Add 100 tabs
        for (var i = 0; i < 100; i++)
        {
            textView.Text += "\t";
        }

        // Move to end
        textView.MoveEnd ();
        Assert.Equal (new Point (100, 0), textView.CursorPosition);

        // Test BackTab (Shift+Tab) navigation backwards
        for (var i = 99; i >= 0; i--)
        {
            Assert.True (textView.NewKeyDownEvent (Key.Tab.WithShift));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }

        // Test Tab navigation forwards
        for (var i = 1; i <= 100; i++)
        {
            Assert.True (textView.NewKeyDownEvent (Key.Tab));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }
    }

    [Fact]
    public void Tab_And_BackTab_Navigation_With_Text ()
    {
        var textView = new TextView
        {
            Width = 30,
            Height = 10,
            Text = "TAB to jump between text fields."
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Point (0, 0), textView.CursorPosition);

        // Navigate forward with Tab
        for (var i = 1; i <= 100; i++)
        {
            Assert.True (textView.NewKeyDownEvent (Key.Tab));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }

        // Navigate backward with BackTab
        for (var i = 99; i >= 0; i--)
        {
            Assert.True (textView.NewKeyDownEvent (Key.Tab.WithShift));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }
    }

    [Fact]
    public void Tab_With_CursorLeft_And_CursorRight_Without_Text ()
    {
        var textView = new TextView
        {
            Width = 30,
            Height = 10,
            Text = ""
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Navigate forward with Tab
        for (var i = 1; i <= 100; i++)
        {
            Assert.True (textView.NewKeyDownEvent (Key.Tab));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }

        // Navigate backward with CursorLeft
        for (var i = 99; i >= 0; i--)
        {
            Assert.True (textView.NewKeyDownEvent (Key.CursorLeft));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }

        // Navigate forward with CursorRight
        for (var i = 1; i <= 100; i++)
        {
            Assert.True (textView.NewKeyDownEvent (Key.CursorRight));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }
    }

    [Fact]
    public void Tab_With_CursorLeft_And_CursorRight_With_Text ()
    {
        var textView = new TextView
        {
            Width = 30,
            Height = 10,
            Text = "TAB to jump between text fields."
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (32, textView.Text.Length);
        Assert.Equal (new Point (0, 0), textView.CursorPosition);

        // Navigate forward with Tab
        for (var i = 1; i <= 100; i++)
        {
            Assert.True (textView.NewKeyDownEvent (Key.Tab));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }

        // Navigate backward with CursorLeft
        for (var i = 99; i >= 0; i--)
        {
            Assert.True (textView.NewKeyDownEvent (Key.CursorLeft));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }

        // Navigate forward with CursorRight
        for (var i = 1; i <= 100; i++)
        {
            Assert.True (textView.NewKeyDownEvent (Key.CursorRight));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }
    }

    [Fact]
    public void Tab_With_Home_End_And_BackTab ()
    {
        var textView = new TextView
        {
            Width = 30,
            Height = 10,
            Text = "TAB to jump between text fields."
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (32, textView.Text.Length);
        Assert.Equal (new Point (0, 0), textView.CursorPosition);

        // Navigate forward with Tab to column 100
        for (var i = 1; i <= 100; i++)
        {
            Assert.True (textView.NewKeyDownEvent (Key.Tab));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }

        // Test Length increased due to tabs
        Assert.Equal (132, textView.Text.Length);

        // Press Home to go to beginning
        Assert.True (textView.NewKeyDownEvent (Key.Home));
        Assert.Equal (new Point (0, 0), textView.CursorPosition);

        // Press End to go to end
        Assert.True (textView.NewKeyDownEvent (Key.End));
        Assert.Equal (132, textView.Text.Length);
        Assert.Equal (new Point (132, 0), textView.CursorPosition);

        // Find the position just before the last tab
        string txt = textView.Text;
        var col = txt.Length;
        
        // Find the last tab position
        while (col > 1 && txt [col - 1] != '\t')
        {
            col--;
            
            // Safety check to prevent infinite loop
            if (col == 0)
            {
                break;
            }
        }
        
        // Set cursor to that position
        textView.CursorPosition = new Point (col, 0);

        // Navigate backward with BackTab (removes tabs, going back to original text)
        while (col > 0)
        {
            col--;
            Assert.True (textView.NewKeyDownEvent (Key.Tab.WithShift));
            Assert.Equal (new Point (col, 0), textView.CursorPosition);
        }
        
        // Should be back at the original text
        Assert.Equal ("TAB to jump between text fields.", textView.Text);
        Assert.Equal (32, textView.Text.Length);
    }

    [Fact]
    public void BackTab_Then_Tab_Navigation ()
    {
        var textView = new TextView
        {
            Width = 30,
            Height = 10,
            Text = ""
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Add 100 tabs at end
        for (var i = 0; i < 100; i++)
        {
            textView.Text += "\t";
        }

        textView.MoveEnd ();
        Assert.Equal (new Point (100, 0), textView.CursorPosition);

        // Navigate backward with BackTab
        for (var i = 99; i >= 0; i--)
        {
            Assert.True (textView.NewKeyDownEvent (Key.Tab.WithShift));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }

        // Navigate forward with Tab
        for (var i = 1; i <= 100; i++)
        {
            Assert.True (textView.NewKeyDownEvent (Key.Tab));
            Assert.Equal (new Point (i, 0), textView.CursorPosition);
        }
    }

    [Fact]
    public void TabWidth_Setting_To_Zero_Keeps_AllowsTab ()
    {
        var textView = new TextView
        {
            Width = 30,
            Height = 10,
            Text = "TAB to jump between text fields."
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Verify initial state
        Assert.Equal (4, textView.TabWidth);
        Assert.True (textView.AllowsTab);
        Assert.True (textView.AllowsReturn);
        Assert.True (textView.Multiline);

        // Set TabWidth to -1 (should clamp to 0)
        textView.TabWidth = -1;
        Assert.Equal (0, textView.TabWidth);
        Assert.True (textView.AllowsTab);
        Assert.True (textView.AllowsReturn);
        Assert.True (textView.Multiline);

        // Insert a tab
        Assert.True (textView.NewKeyDownEvent (Key.Tab));
        Assert.Equal ("\tTAB to jump between text fields.", textView.Text);

        // Change TabWidth back to 4
        textView.TabWidth = 4;
        Assert.Equal (4, textView.TabWidth);

        // Remove the tab with BackTab
        Assert.True (textView.NewKeyDownEvent (Key.Tab.WithShift));
        Assert.Equal ("TAB to jump between text fields.", textView.Text);
    }

    [Fact]
    public void KeyBindings_Command_Navigation ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var textView = new TextView
        {
            Width = 10,
            Height = 2,
            Text = text
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (
            $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
            textView.Text
        );
        Assert.Equal (3, textView.Lines);
        Assert.Equal (Point.Empty, textView.CursorPosition);
        Assert.False (textView.ReadOnly);
        Assert.True (textView.CanFocus);
        Assert.False (textView.IsSelecting);

        // Test that CursorLeft doesn't move when at beginning
        textView.CanFocus = false;
        Assert.True (textView.NewKeyDownEvent (Key.CursorLeft));
        Assert.False (textView.IsSelecting);
        
        textView.CanFocus = true;
        Assert.False (textView.NewKeyDownEvent (Key.CursorLeft));
        Assert.False (textView.IsSelecting);
        
        // Move right
        Assert.True (textView.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (new Point (1, 0), textView.CursorPosition);
        Assert.False (textView.IsSelecting);
        
        // Move to end of document
        Assert.True (textView.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal (2, textView.CurrentRow);
        Assert.Equal (23, textView.CurrentColumn);
        Assert.Equal (textView.CurrentColumn, textView.GetCurrentLine ().Count);
        Assert.Equal (new Point (23, 2), textView.CursorPosition);
        Assert.False (textView.IsSelecting);
        
        // Try to move right (should fail, at end)
        Assert.False (textView.NewKeyDownEvent (Key.CursorRight));
        Assert.False (textView.IsSelecting);
        
        // Type a character
        Assert.True (textView.NewKeyDownEvent (Key.F.WithShift));
        Assert.Equal (
            $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.F",
            textView.Text
        );
        Assert.Equal (new Point (24, 2), textView.CursorPosition);
        Assert.False (textView.IsSelecting);
        
        // Undo
        Assert.True (textView.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal (
            $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
            textView.Text
        );
        Assert.Equal (new Point (23, 2), textView.CursorPosition);
        Assert.False (textView.IsSelecting);
        
        // Redo
        Assert.True (textView.NewKeyDownEvent (Key.R.WithCtrl));
        Assert.Equal (
            $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.F",
            textView.Text
        );
        Assert.Equal (new Point (24, 2), textView.CursorPosition);
        Assert.False (textView.IsSelecting);
    }

    [Fact]
    public void UnwrappedCursorPosition_Event_Fires_Correctly ()
    {
        Point unwrappedPosition = Point.Empty;
        
        var textView = new TextView
        {
            Width = 25,
            Height = 25,
            Text = "This is the first line.\nThis is the second line.\n"
        };
        
        textView.UnwrappedCursorPosition += (s, e) => { unwrappedPosition = e; };
        
        textView.BeginInit ();
        textView.EndInit ();

        // Initially no word wrap
        Assert.False (textView.WordWrap);
        Assert.Equal (Point.Empty, textView.CursorPosition);
        Assert.Equal (Point.Empty, unwrappedPosition);

        // Enable word wrap and move cursor
        textView.WordWrap = true;
        textView.CursorPosition = new Point (12, 0);
        Assert.Equal (new Point (12, 0), textView.CursorPosition);
        Assert.Equal (new Point (12, 0), unwrappedPosition);

        // Move right and verify unwrapped position updates
        var currentUnwrapped = unwrappedPosition;
        Assert.True (textView.NewKeyDownEvent (Key.CursorRight));
        // The unwrapped position should have updated
        Assert.True (unwrappedPosition.X >= currentUnwrapped.X);
        
        // Move several more times to verify tracking continues to work
        for (int i = 0; i < 5; i++)
        {
            Assert.True (textView.NewKeyDownEvent (Key.CursorRight));
        }
        
        // Unwrapped position should track the actual position in the text
        Assert.True (unwrappedPosition.X > 12);
    }

    [Fact]
    public void Horizontal_Scrolling_Adjusts_LeftColumn ()
    {
        var textView = new TextView
        {
            Width = 20,
            Height = 5,
            Text = "This is a very long line that will require horizontal scrolling to see all of it",
            WordWrap = false
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Initially at the start
        Assert.Equal (0, textView.LeftColumn);
        Assert.Equal (new Point (0, 0), textView.CursorPosition);

        // Move to the end of the line
        textView.MoveEnd ();
        
        // LeftColumn should have adjusted to show the cursor
        Assert.True (textView.LeftColumn > 0);
        Assert.Equal (textView.Text.Length, textView.CurrentColumn);

        // Move back to the start
        textView.MoveHome ();
        
        // LeftColumn should be back to 0
        Assert.Equal (0, textView.LeftColumn);
        Assert.Equal (0, textView.CurrentColumn);
    }

    [Fact]
    public void Vertical_Scrolling_Adjusts_TopRow ()
    {
        var lines = string.Join ("\n", Enumerable.Range (1, 100).Select (i => $"Line {i}"));
        var textView = new TextView
        {
            Width = 20,
            Height = 5,
            Text = lines
        };
        textView.BeginInit ();
        textView.EndInit ();

        // Initially at the top
        Assert.Equal (0, textView.TopRow);
        Assert.Equal (new Point (0, 0), textView.CursorPosition);

        // Move down many lines
        for (var i = 0; i < 50; i++)
        {
            textView.NewKeyDownEvent (Key.CursorDown);
        }

        // TopRow should have adjusted to show the cursor
        Assert.True (textView.TopRow > 0);
        Assert.Equal (50, textView.CurrentRow);

        // Move back to the top
        textView.NewKeyDownEvent (Key.Home.WithCtrl);
        
        // TopRow should be back to 0
        Assert.Equal (0, textView.TopRow);
        Assert.Equal (0, textView.CurrentRow);
    }
}
