using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

/// <summary>
///     Integration tests for GuiTestContext keyboard event handling (InjectKeyEvent).
/// </summary>
public class GuiTestContextKeyEventTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_ViaApplication_Stops (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d)
                                           .Then ((app) =>
                                                  {
                                                      app?.Keyboard.RaiseKeyDownEvent (Application.QuitKey);
                                                      Assert.False (app!.TopRunnable!.IsRunning);
                                                  });
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_ViaInjectKey_Stops (TestDriver d)
    {
        using GuiTestContext context = With.A<Window> (40, 10, d, _out);
        Assert.True (context.App?.TopRunnable!.IsRunning);

        IRunnable? top = context.App?.TopRunnable;
        context.InjectKeyEvent (Application.QuitKey);
        context.App?.Dispose ();

        Assert.False (top!.IsRunning);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_AfterResizeConsole_StillWorks (TestDriver d)
    {
        var keyReceived = false;
        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keyReceived = true;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then ((_) => view.SetFocus ())
                                           .ResizeConsole (50, 20)
                                           .InjectKeyEvent (Key.A);

        Assert.True (keyReceived);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_Backspace_DeletesCharacter (TestDriver d)
    {
        var textField = new TextField { Text = "TEST", Width = 20 };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (textField)
                                           .Focus (textField)
                                           .Then ((_) => textField.CursorPosition = textField.Text.Length)
                                           .InjectKeyEvent (Key.Backspace)
                                           .InjectKeyEvent (Key.Backspace);

        Assert.Equal ("TE", textField.Text);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_ChainedWithOtherOperations_WorksCorrectly (TestDriver d)
    {
        var textField = new TextField { Width = 20 };
        var clickedCount = 0;
        var button = new Button { Text = "Click Me" };
        button.Accepting += (s, e) => clickedCount++;

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (textField)
                                           .Add (button)
                                           .Then ((_) => textField.SetFocus ())
                                           .InjectKeyEvent (Key.T.WithShift)
                                           .InjectKeyEvent (Key.E)
                                           .InjectKeyEvent (Key.S)
                                           .InjectKeyEvent (Key.T)
                                           .AssertEqual ("Test", textField.Text)
                                           .InjectKeyEvent (Key.Tab)
                                           .Then ((_) => Assert.True (button.HasFocus))
                                           .InjectKeyEvent (Key.Enter)
                                           .AssertEqual (1, clickedCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_EnqueuesKeyAndProcessesIt (TestDriver d)
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
                                           .Then ((_) => view.SetFocus ())
                                           .InjectKeyEvent (Key.A);

        Assert.True (keyReceived, "Key was not received by the view");
        Assert.Equal (Key.A, receivedKey);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_FunctionKeys_ProcessesCorrectly (TestDriver d)
    {
        List<Key> keysReceived = [];

        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then ((_) => view.SetFocus ())
                                           .InjectKeyEvent (Key.F1)
                                           .InjectKeyEvent (Key.F5)
                                           .InjectKeyEvent (Key.F12);

        Assert.Equal (3, keysReceived.Count);
        Assert.Equal (Key.F1, keysReceived [0]);
        Assert.Equal (Key.F5, keysReceived [1]);
        Assert.Equal (Key.F12, keysReceived [2]);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_MultipleKeys_ProcessesInOrder (TestDriver d)
    {
        List<Key> keysReceived = [];

        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then ((_) => view.SetFocus ())
                                           .InjectKeyEvent (Key.A)
                                           .InjectKeyEvent (Key.B)
                                           .InjectKeyEvent (Key.C);

        Assert.Equal (3, keysReceived.Count);
        Assert.Equal (Key.A, keysReceived [0]);
        Assert.Equal (Key.B, keysReceived [1]);
        Assert.Equal (Key.C, keysReceived [2]);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_NavigationKeys_ChangeFocus (TestDriver d)
    {
        var view1 = new View { Id = "view1", CanFocus = true };
        var view2 = new View { Id = "view2", CanFocus = true };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view1)
                                           .Add (view2)
                                           .Then ((_) => view1.SetFocus ())
                                           .AssertTrue (view1.HasFocus)
                                           .AssertFalse (view2.HasFocus)
                                           .InjectKeyEvent (Key.Tab)
                                           .AssertFalse (view1.HasFocus)
                                           .AssertTrue (view2.HasFocus)
                                           .InjectKeyEvent (Key.Tab.WithShift)
                                           .AssertTrue (view1.HasFocus)
                                           .AssertFalse (view2.HasFocus);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_NumericKeys_ProcessesCorrectly (TestDriver d)
    {
        var textField = new TextField { Width = 20 };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (textField)
                                           .Then ((_) => textField.SetFocus ())
                                           .InjectKeyEvent (Key.D1)
                                           .InjectKeyEvent (Key.D2)
                                           .InjectKeyEvent (Key.D3)
                                           .InjectKeyEvent (Key.D4)
                                           .InjectKeyEvent (Key.D5);

        Assert.Equal ("12345", textField.Text);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_RapidSequence_ProcessesAllKeys (TestDriver d)
    {
        List<Key> keysReceived = [];
        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then ((_) => view.SetFocus ());

        // Send 10 keys rapidly
        for (var i = 0; i < 10; i++)
        {
            context.InjectKeyEvent ((Key)(Key.A.KeyCode + (uint)i));
        }

        Assert.Equal (10, keysReceived.Count);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal ((Key)(Key.A.KeyCode + (uint)i), keysReceived [i]);
        }
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_SpecialKeys_ProcessesCorrectly (TestDriver d)
    {
        List<Key> keysReceived = [];

        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (view)
                                           .Then ((_) => view.SetFocus ())
                                           .InjectKeyEvent (Key.Enter)
                                           .InjectKeyEvent (Key.Tab)
                                           .InjectKeyEvent (Key.CursorUp)
                                           .InjectKeyEvent (Key.CursorDown)
                                           .InjectKeyEvent (Key.Esc);

        Assert.Equal (5, keysReceived.Count);
        Assert.Equal (Key.Enter, keysReceived [0]);
        Assert.Equal (Key.Tab, keysReceived [1]);
        Assert.Equal (Key.CursorUp, keysReceived [2]);
        Assert.Equal (Key.CursorDown, keysReceived [3]);
        Assert.Equal (Key.Esc, keysReceived [4]);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_WithListView_NavigatesItems (TestDriver d)
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
                                           .Then ((_) => listView.SetFocus ())
                                           .AssertEqual (0, listView.SelectedItem)
                                           .InjectKeyEvent (Key.CursorDown)
                                           .AssertEqual (1, listView.SelectedItem)
                                           .InjectKeyEvent (Key.CursorDown)
                                           .AssertEqual (2, listView.SelectedItem)
                                           .InjectKeyEvent (Key.CursorUp)
                                           .AssertEqual (1, listView.SelectedItem);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_WithModifiers_ProcessesCorrectly (TestDriver d)
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
                                           .Then ((_) => view.SetFocus ())
                                           .InjectKeyEvent (Key.A.WithCtrl);

        Assert.True (keyReceived);
        Assert.Equal (Key.A.WithCtrl, receivedKey);
        Assert.True (receivedKey.IsCtrl);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void InjectKey_WithTextField_UpdatesText (TestDriver d)
    {
        var textField = new TextField { Width = 20 };

        using GuiTestContext context = With.A<Window> (40, 10, d, _out)
                                           .Add (textField)
                                           .InjectKeyEvent (Key.H.WithShift)
                                           .InjectKeyEvent (Key.E)
                                           .InjectKeyEvent (Key.L)
                                           .InjectKeyEvent (Key.L)
                                           .InjectKeyEvent (Key.O);

        //Assert.Equal ("Hello", textField.Text);
    }
}