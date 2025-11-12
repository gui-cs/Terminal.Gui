using System.Drawing;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

/// <summary>
///     Integration tests for GuiTestContext mouse event handling (LeftClick, RightClick).
/// </summary>
public class GuiTestContextMouseEventTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_Click_RaisesAccepting (TestDriver d)
    {
        var clickedCount = 0;
        var button = new Button
        {
            X = 5,
            Y = 5,
            Text = "Click Me"
        };
        button.Accepting += (s, e) => clickedCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (button)
                                           .LeftClick (6, 6) // Click inside button (accounting for Window's border)
                                           .AssertEqual (1, clickedCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_Click_TView_RaisesAccepting (TestDriver d)
    {
        var clickedCount = 0;
        var button = new Button
        {
            X = 5,
            Y = 5,
            Text = "Click Me"
        };
        button.Accepting += (s, e) => clickedCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (button)
                                           .LeftClick<Button> (b => b.Text == "Click Me")
                                           .AssertEqual (1, clickedCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_Click_OnView_RaisesMouseEvent (TestDriver d)
    {
        var mouseReceived = false;
        Point receivedPosition = Point.Empty;

        var view = new View
        {
            X = 10,
            Y = 5,
            Width = 20,
            Height = 5
        };

        view.MouseEvent += (s, e) =>
        {
            mouseReceived = true;
            receivedPosition = e.Position;
        };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .LeftClick (15, 7)
                                           .AssertTrue (mouseReceived);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_MultipleClicks_ProcessesInOrder (TestDriver d)
    {
        var clickCount = 0;
        var button = new Button
        {
            X = 5,
            Y = 5,
            Text = "Click Me"
        };
        button.Accepting += (s, e) => clickCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (button)
                                           .LeftClick (6, 6)
                                           .LeftClick (6, 6)
                                           .LeftClick (6, 6)
                                           .AssertEqual (3, clickCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_RightClick_RaisesCorrectEvent (TestDriver d)
    {
        var rightClickCount = 0;
        var view = new View
        {
            X = 10,
            Y = 5,
            Width = 20,
            Height = 5
        };

        view.MouseEvent += (s, e) =>
        {
            if (e.Flags.HasFlag (MouseFlags.Button3Clicked))
            {
                rightClickCount++;
            }
        };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .RightClick (15, 7)
                                           .AssertEqual (1, rightClickCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_Click_SetsFocusOnView (TestDriver d)
    {
        var view1 = new View
        {
            Id = "view1",
            X = 5,
            Y = 5,
            Width = 10,
            Height = 5,
            CanFocus = true
        };

        var view2 = new View
        {
            Id = "view2",
            X = 20,
            Y = 5,
            Width = 10,
            Height = 5,
            CanFocus = true
        };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view1)
                                           .Add (view2)
                                           .Then (() => view1.SetFocus ())
                                           .AssertTrue (view1.HasFocus)
                                           .LeftClick (25, 7) // Click on view2
                                           .AssertFalse (view1.HasFocus)
                                           .AssertTrue (view2.HasFocus);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_ChainedWithKeyboard_WorksCorrectly (TestDriver d)
    {
        var clickCount = 0;

        var button = new Button
        {
            X = 5,
            Y = 5,
            Text = "Click Me"
        };
        button.Accepting += (s, e) => clickCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (button)
                                           .LeftClick (6, 6) // Click button to focus it
                                           .AssertEqual (1, clickCount)
                                           .AssertTrue (button.HasFocus)
                                           .EnqueueKeyEvent (Key.Enter) // Press Enter
                                           .AssertEqual (2, clickCount); // Should trigger button again
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_Click_OnTextField_SetsCaretPosition (TestDriver d)
    {
        var textField = new TextField
        {
            X = 5,
            Y = 5,
            Width = 20,
            Text = "Hello World"
        };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (textField)
                                           .LeftClick (11, 6) // Click in middle of text (accounting for border)
                                           .AssertTrue (textField.HasFocus);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_RapidClicks_AllProcessed (TestDriver d)
    {
        var clickCount = 0;
        var view = new View
        {
            X = 10,
            Y = 5,
            Width = 20,
            Height = 5
        };

        view.MouseEvent += (s, e) => clickCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view);

        // Rapid fire 10 clicks
        for (var i = 0; i < 10; i++)
        {
            context.LeftClick (15, 7);
        }

        context.AssertEqual (10, clickCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_Click_OutsideView_DoesNotRaiseEvent (TestDriver d)
    {
        var clickCount = 0;
        var view = new View
        {
            X = 10,
            Y = 5,
            Width = 10,
            Height = 5
        };

        view.MouseEvent += (s, e) => clickCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .LeftClick (5, 5) // Click outside view
                                           .AssertEqual (0, clickCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_ClickOnDisabledView_DoesNotTrigger (TestDriver d)
    {
        var clickCount = 0;
        var button = new Button
        {
            X = 5,
            Y = 5,
            Text = "Click Me",
            Enabled = false
        };
        button.Accepting += (s, e) => clickCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (button)
                                           .LeftClick (6, 6)
                                           .AssertEqual (0, clickCount); // Should not increment because button is disabled
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_AfterResize_StillWorks (TestDriver d)
    {
        var clickCount = 0;
        var button = new Button
        {
            X = 5,
            Y = 5,
            Text = "Click Me"
        };
        button.Accepting += (s, e) => clickCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (button)
                                           .ResizeConsole (50, 20)
                                           .LeftClick (6, 6)
                                           .AssertEqual (1, clickCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_WithCheckBox_TogglesState (TestDriver d)
    {
        var checkBox = new CheckBox
        {
            X = 5,
            Y = 5,
            Text = "Check Me"
        };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (checkBox)
                                           .AssertEqual (CheckState.UnChecked, checkBox.CheckedState)
                                           .LeftClick (6, 6) // Click checkbox
                                           .AssertEqual (CheckState.Checked, checkBox.CheckedState)
                                           .LeftClick (6, 6) // Click again
                                           .AssertEqual (CheckState.UnChecked, checkBox.CheckedState);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueMouseEvent_WithListView_SelectsItem (TestDriver d)
    {
        var listView = new ListView
        {
            X = 5,
            Y = 5,
            Width = 20,
            Height = 10
        };
        listView.SetSource (["Item1", "Item2", "Item3", "Item4", "Item5"]);
        listView.SelectedItem = 0;

        using GuiTestContext context = With.A<Window> (40, 20, d, _out)
                                           .Add (listView)
                                           .AssertEqual (0, listView.SelectedItem)
                                           .LeftClick (6, 7) // Click on Item2 (accounting for header/border)
                                           .AssertEqual (1, listView.SelectedItem);
    }
}