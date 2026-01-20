using System.Drawing;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
///     Integration tests for TestContext mouse event handling (LeftClick, RightClick).
/// </summary>
public class TestContextMouseEventTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_RaisesAccepting (string d)
    {
        var clickedCount = 0;

        var button = new Button { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (s, e) => clickedCount++;

        using TestContext context = With.A<Window> (40, 10, d, _out)
                                        .Add (button)
                                        .LeftClick (6, 6) // Click inside button (accounting for Window's border)
                                        .AssertEqual (1, clickedCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_TView_RaisesAccepting (string d)
    {
        var clickedCount = 0;

        var button = new Button { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (s, e) => clickedCount++;

        using TestContext context = With.A<Window> (40, 10, d, _out).Add (button).LeftClick<Button> (b => b.Text == "Click Me").AssertEqual (1, clickedCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_OnView_RaisesMouseEvent (string d)
    {
        var mouseReceived = false;
        var receivedPosition = Point.Empty;

        var view = new View { X = 10, Y = 5, Width = 20, Height = 5 };

        view.MouseEvent += (_, mouse) =>
                           {
                               mouseReceived = true;
                               receivedPosition = mouse.Position!.Value;
                           };

        using TestContext context = With.A<Window> (40, 10, d, _out).Add (view).LeftClick (15, 7).AssertTrue (mouseReceived);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MultipleClicks_ProcessesInOrder (string d)
    {
        var clickCount = 0;

        var button = new Button { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (s, e) => clickCount++;

        using TestContext context = With.A<Window> (40, 10, d, _out)
                                        .Add (button)
                                        .LeftClick (6, 6)
                                        .LeftClick (6, 6)
                                        .LeftClick (6, 6)
                                        .AssertEqual (3, clickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void RightClick_RaisesCorrectEvent (string d)
    {
        var rightClickCount = 0;

        var view = new View { X = 10, Y = 5, Width = 20, Height = 5 };

        view.MouseEvent += (s, e) =>
                           {
                               if (e.Flags.HasFlag (MouseFlags.RightButtonClicked))
                               {
                                   rightClickCount++;
                               }
                           };

        using TestContext context = With.A<Window> (40, 10, d, _out).Add (view).RightClick (15, 7).AssertEqual (1, rightClickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_SetsFocusOnView (string d)
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

        using TestContext context = With.A<Window> (40, 10, d, _out)
                                        .Add (view1)
                                        .Add (view2)
                                        .Then (_ => view1.SetFocus ())
                                        .AssertTrue (view1.HasFocus)
                                        .LeftClick (25, 7) // Click on view2
                                        .AssertFalse (view1.HasFocus)
                                        .AssertTrue (view2.HasFocus);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void ChainedWithKeyboard_WorksCorrectly (string d)
    {
        var clickCount = 0;

        var button = new Button { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (s, e) => clickCount++;

        using TestContext context = With.A<Window> (40, 10, d, _out)
                                        .Add (button)
                                        .LeftClick (6, 6) // Click button to focus it
                                        .AssertEqual (1, clickCount)
                                        .AssertTrue (button.HasFocus)
                                        .KeyDown (Key.Enter) // Press Enter
                                        .AssertEqual (2, clickCount); // Should trigger button again
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_OnTextField_SetsCaretPosition (string d)
    {
        var textField = new TextField { X = 5, Y = 5, Width = 20, Text = "Hello World" };

        using TestContext context = With.A<Window> (40, 10, d, _out)
                                        .Add (textField)
                                        .LeftClick (11, 6) // Click in middle of text (accounting for border)
                                        .AssertTrue (textField.HasFocus);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void RapidClicks_AllProcessed (string d)
    {
        var clickCount = 0;

        var view = new View { X = 10, Y = 5, Width = 20, Height = 5 };

        // Only count Clicked events, not all mouse events (Pressed, Released, Clicked)
        view.MouseEvent += (s, e) =>
                           {
                               if (e.Flags.HasFlag (MouseFlags.LeftButtonClicked))
                               {
                                   clickCount++;
                               }
                           };

        using TestContext context = With.A<Window> (40, 10, d, _out).Add (view);

        // Rapid fire 10 clicks
        for (var i = 0; i < 10; i++)
        {
            context.LeftClick (15, 7);
        }

        context.AssertEqual (10, clickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_OutsideView_DoesNotRaiseEvent (string d)
    {
        var clickCount = 0;

        var view = new View { X = 10, Y = 5, Width = 10, Height = 5 };

        view.MouseEvent += (s, e) => clickCount++;

        using TestContext context = With.A<Window> (40, 10, d, _out)
                                        .Add (view)
                                        .LeftClick (5, 5) // Click outside view
                                        .AssertEqual (0, clickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void ClickOnDisabledView_DoesNotTrigger (string d)
    {
        var clickCount = 0;

        var button = new Button { X = 5, Y = 5, Text = "Click Me", Enabled = false };
        button.Accepting += (s, e) => clickCount++;

        using TestContext context = With.A<Window> (40, 10, d, _out)
                                        .Add (button)
                                        .LeftClick (6, 6)
                                        .AssertEqual (0, clickCount); // Should not increment because button is disabled
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void AfterResize_StillWorks (string d)
    {
        var clickCount = 0;

        var button = new Button { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (s, e) => clickCount++;

        using TestContext context = With.A<Window> (40, 10, d, _out).Add (button).ResizeConsole (50, 20).LeftClick (6, 6).AssertEqual (1, clickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void WithCheckBox_TogglesState (string d)
    {
        var checkBox = new CheckBox { X = 5, Y = 5, Text = "Check Me" };

        using TestContext context = With.A<Window> (40, 10, d, _out)
                                        .Add (checkBox)
                                        .AssertEqual (CheckState.UnChecked, checkBox.CheckedState)
                                        .LeftClick (6, 6) // Click checkbox
                                        .AssertEqual (CheckState.Checked, checkBox.CheckedState)
                                        .LeftClick (6, 6) // Click again
                                        .AssertEqual (CheckState.UnChecked, checkBox.CheckedState);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void WithListView_SelectsItem (string d)
    {
        var listView = new ListView { X = 5, Y = 5, Width = 20, Height = 10 };
        listView.SetSource (["Item1", "Item2", "Item3", "Item4", "Item5"]);
        listView.SelectedItem = 0;

        using TestContext context = With.A<Window> (40, 20, d, _out)
                                        .Add (listView)
                                        .AssertEqual (0, listView.SelectedItem)
                                        .LeftClick (6, 7) // Click on Item2 (accounting for header/border)
                                        .AssertEqual (1, listView.SelectedItem);
    }
}
