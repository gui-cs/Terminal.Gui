using System.Drawing;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class GuiTestContextTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Constructor_Sets_Application_Screen (TestDriver d)
    {
        using var context = new GuiTestContext (d, _out, TimeSpan.FromSeconds (10));

        Assert.NotEqual (Rectangle.Empty, Application.Screen);

        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_ViaApplication_Stops (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d);
        Assert.True (Application.Top!.Running);

        Toplevel top = Application.Top;
        Application.RaiseKeyDownEvent (Application.QuitKey);
        Assert.False (top!.Running);

        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_ViaEnqueueKey_Stops (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);
        Assert.True (Application.Top!.Running);

        Toplevel top = Application.Top;
        context.EnqueueKey (Application.QuitKey);

        //Thread.Sleep (1000);
        Assert.False (top!.Running);

        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void ResizeConsole_Resizes (TestDriver d)
    {
        var lbl = new Label
        {
            Width = Dim.Fill ()
        };

        using GuiTestContext c = With.A<Window> (40, 10, d)
                                     .Add (lbl)
                                     .AssertEqual (38, lbl.Frame.Width) // Window has 2 border
                                     .ResizeConsole (20, 20)
                                     .WaitIteration ()
                                     .AssertEqual (18, lbl.Frame.Width)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_AfterResizeConsole_StillWorks (TestDriver d)
    {
        var keyReceived = false;
        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keyReceived = true;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then (() => view.SetFocus ())
                                           .ResizeConsole (50, 20)
                                           .EnqueueKey (Key.A)
                                           .WriteOutLogs (_out);

        Assert.True (keyReceived);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_Backspace_DeletesCharacter (TestDriver d)
    {
        var textField = new TextField { Text = "TEST", Width = 20 };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (textField)
                                           .Focus (textField)
                                           .Then (() =>
                                                  {
                                                      textField.CursorPosition = textField.Text.Length;
                                                  })
                                           .EnqueueKey (Key.Backspace)
                                           .EnqueueKey (Key.Backspace)
                                           .WriteOutLogs (_out);

        Assert.Equal ("TE", textField.Text);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_ChainedWithOtherOperations_WorksCorrectly (TestDriver d)
    {
        var textField = new TextField { Width = 20 };
        var clickedCount = 0;
        var button = new Button { Text = "Click Me" };
        button.Accepting += (s, e) => clickedCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (textField)
                                           .Add (button)
                                           .Then (() => textField.SetFocus ())
                                           .EnqueueKey (Key.T.WithShift)
                                           .EnqueueKey (Key.E)
                                           .EnqueueKey (Key.S)
                                           .EnqueueKey (Key.T)
                                           .AssertEqual ("Test", textField.Text)
                                           .EnqueueKey (Key.Tab)
                                           .Then (() => Assert.True (button.HasFocus))
                                           .EnqueueKey (Key.Enter)
                                           .AssertEqual (1, clickedCount)
                                           .WriteOutLogs (_out);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_EnqueuesKeyAndProcessesIt (TestDriver d)
    {
        var keyReceived = false;
        var receivedKey = Key.Empty;

        var view = new View { CanFocus = true };

        view.KeyDown += (s, e) =>
                        {
                            keyReceived = true;
                            receivedKey = e;
                        };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then (() => view.SetFocus ())
                                           .EnqueueKey (Key.A);

        Assert.True (keyReceived, "Key was not received by the view");
        Assert.Equal (Key.A, receivedKey);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_FunctionKeys_ProcessesCorrectly (TestDriver d)
    {
        List<Key> keysReceived = new ();

        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then (() => view.SetFocus ())
                                           .EnqueueKey (Key.F1)
                                           .EnqueueKey (Key.F5)
                                           .EnqueueKey (Key.F12)
                                           .WriteOutLogs (_out);

        Assert.Equal (3, keysReceived.Count);
        Assert.Equal (Key.F1, keysReceived [0]);
        Assert.Equal (Key.F5, keysReceived [1]);
        Assert.Equal (Key.F12, keysReceived [2]);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_MultipleKeys_ProcessesInOrder (TestDriver d)
    {
        List<Key> keysReceived = new ();

        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then (() => view.SetFocus ())
                                           .EnqueueKey (Key.A)
                                           .EnqueueKey (Key.B)
                                           .EnqueueKey (Key.C)
                                           .WriteOutLogs (_out);

        Assert.Equal (3, keysReceived.Count);
        Assert.Equal (Key.A, keysReceived [0]);
        Assert.Equal (Key.B, keysReceived [1]);
        Assert.Equal (Key.C, keysReceived [2]);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_NavigationKeys_ChangeFocus (TestDriver d)
    {
        var view1 = new View { Id = "view1", CanFocus = true };
        var view2 = new View { Id = "view2", CanFocus = true };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view1)
                                           .Add (view2)
                                           .Then (() => view1.SetFocus ())
                                           .AssertTrue (view1.HasFocus)
                                           .AssertFalse (view2.HasFocus)
                                           .EnqueueKey (Key.Tab)
                                           .AssertFalse (view1.HasFocus)
                                           .AssertTrue (view2.HasFocus)
                                           .EnqueueKey (Key.Tab.WithShift)
                                           .AssertTrue (view1.HasFocus)
                                           .AssertFalse (view2.HasFocus)
                                           .WriteOutLogs (_out);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_NumericKeys_ProcessesCorrectly (TestDriver d)
    {
        var textField = new TextField { Width = 20 };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (textField)
                                           .Then (() => textField.SetFocus ())
                                           .EnqueueKey (Key.D1)
                                           .EnqueueKey (Key.D2)
                                           .EnqueueKey (Key.D3)
                                           .EnqueueKey (Key.D4)
                                           .EnqueueKey (Key.D5)
                                           .WriteOutLogs (_out);

        Assert.Equal ("12345", textField.Text);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_RapidSequence_ProcessesAllKeys (TestDriver d)
    {
        List<Key> keysReceived = new ();
        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then (() => view.SetFocus ());

        // Send 10 keys rapidly
        for (var i = 0; i < 10; i++)
        {
            context.EnqueueKey ((Key)(Key.A.KeyCode + (uint)i));
        }

        context.WriteOutLogs (_out);

        Assert.Equal (10, keysReceived.Count);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal ((Key)(Key.A.KeyCode + (uint)i), keysReceived [i]);
        }
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_SpecialKeys_ProcessesCorrectly (TestDriver d)
    {
        List<Key> keysReceived = new ();

        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then (() => view.SetFocus ())
                                           .EnqueueKey (Key.Enter)
                                           .EnqueueKey (Key.Tab)
                                           .EnqueueKey (Key.CursorUp)
                                           .EnqueueKey (Key.CursorDown)
                                           .EnqueueKey (Key.Esc)
                                           .WriteOutLogs (_out);

        Assert.Equal (5, keysReceived.Count);
        Assert.Equal (Key.Enter, keysReceived [0]);
        Assert.Equal (Key.Tab, keysReceived [1]);
        Assert.Equal (Key.CursorUp, keysReceived [2]);
        Assert.Equal (Key.CursorDown, keysReceived [3]);
        Assert.Equal (Key.Esc, keysReceived [4]);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_WithListView_NavigatesItems (TestDriver d)
    {
        var listView = new ListView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        listView.SetSource (["Item1", "Item2", "Item3", "Item4", "Item5"]);
        listView.SelectedItem = 0;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (listView)
                                           .Then (() => listView.SetFocus ())
                                           .AssertEqual (0, listView.SelectedItem)
                                           .EnqueueKey (Key.CursorDown)
                                           .AssertEqual (1, listView.SelectedItem)
                                           .EnqueueKey (Key.CursorDown)
                                           .AssertEqual (2, listView.SelectedItem)
                                           .EnqueueKey (Key.CursorUp)
                                           .AssertEqual (1, listView.SelectedItem)
                                           .WriteOutLogs (_out);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_WithModifiers_ProcessesCorrectly (TestDriver d)
    {
        var keyReceived = false;
        var receivedKey = Key.Empty;

        var view = new View { CanFocus = true };

        view.KeyDown += (s, e) =>
                        {
                            keyReceived = true;
                            receivedKey = e;
                        };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then (() => view.SetFocus ())
                                           .EnqueueKey (Key.A.WithCtrl)
                                           .WriteOutLogs (_out);

        Assert.True (keyReceived);
        Assert.Equal (Key.A.WithCtrl, receivedKey);
        Assert.True (receivedKey.IsCtrl);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnqueueKey_WithTextField_UpdatesText (TestDriver d)
    {
        var textField = new TextField { Width = 20 };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (textField)
                                           .Focus(textField)
                                           .EnqueueKey (Key.H.WithShift)
                                           .EnqueueKey (Key.E)
                                           .EnqueueKey (Key.L)
                                           .EnqueueKey (Key.L)
                                           .EnqueueKey (Key.O)
                                           .WriteOutLogs (_out);

        Assert.Equal ("Hello", textField.Text);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void With_New_A_Runs (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);
        Assert.True (Application.Top!.Running);
        Assert.NotEqual (Rectangle.Empty, Application.Screen);
        context.WriteOutLogs (_out);
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void With_Starts_Stops_Without_Error (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);

        // No actual assertions are needed — if no exceptions are thrown, it's working
        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void With_Without_Stop_Still_Cleans_Up (TestDriver d)
    {
        GuiTestContext? context;

        using (context = With.A<Window> (40, 10, d, _out))
        {
            Assert.False (context.Finished);
        }

        Assert.True (context.Finished);
    }
}
