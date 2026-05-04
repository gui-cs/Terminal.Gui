using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests.Markdown;

// Copilot
[TestSubject (typeof (Terminal.Gui.Views.Markdown))]
public class MarkdownViewSelectionTests
{
    /// <summary>Helper: builds and lays out a Markdown view at the given width/height.</summary>
    private static (IApplication App, Runnable Window, Terminal.Gui.Views.Markdown Mv) CreateMv (string text, int width = 40, int height = 10)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, height);
        app.Clipboard = new FakeClipboard ();

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        Terminal.Gui.Views.Markdown mv = new () { Text = text, Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);
        app.Begin (window);
        app.LayoutAndDraw ();

        return (app, window, mv);
    }

    [Fact]
    public void SelectAll_Sets_IsSelecting_And_Returns_True ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello World", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        bool result = mv.SelectAll ();

        Assert.True (result);
        Assert.True (mv.IsInSelection (0, 0));

        host.Dispose ();
    }

    [Fact]
    public void SelectAll_Empty_Content_Returns_True ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        bool result = mv.SelectAll ();

        Assert.True (result);

        host.Dispose ();
    }

    [Fact]
    public void ClearSelection_Clears_IsSelecting ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        mv.SelectAll ();
        Assert.True (mv.IsInSelection (0, 0));

        mv.ClearSelection ();
        Assert.False (mv.IsInSelection (0, 0));

        host.Dispose ();
    }

    [Fact]
    public void IsInSelection_False_When_Not_Selecting ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // No SelectAll called — should return false
        Assert.False (mv.IsInSelection (0, 0));
        Assert.False (mv.IsInSelection (0, 5));

        host.Dispose ();
    }

    [Fact]
    public void Copy_Without_Selection_Copies_Markdown_Source ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("# Hello\n\nWorld");

        bool result = mv.Copy ();

        Assert.True (result);
        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Equal ("# Hello\n\nWorld", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Copy_With_SelectAll_Copies_Selected_Text ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        mv.SelectAll ();
        bool result = mv.Copy ();

        Assert.True (result);
        app.Clipboard!.TryGetClipboardData (out string clipboard);

        // The rendered text for "Hello" should contain "Hello"
        Assert.Contains ("Hello", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Copy_Command_Copies_Markdown_When_No_Selection ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("# Test");

        // Invoke Command.Copy directly (no selection active)
        mv.InvokeCommand (Command.Copy);

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Equal ("# Test", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SelectAll_Command_Then_Copy_Command_Copies_All_Text ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello World");

        mv.InvokeCommand (Command.SelectAll);
        mv.InvokeCommand (Command.Copy);

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Contains ("Hello World", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Text_Change_Clears_Selection ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        mv.SelectAll ();
        Assert.True (mv.IsInSelection (0, 0));

        // Changing text should clear selection
        mv.Text = "World";

        Assert.False (mv.IsInSelection (0, 0));

        host.Dispose ();
    }

    [Fact]
    public void ContextMenu_Is_Null_Before_Init ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello" };

        Assert.Null (mv.ContextMenu);

        mv.Dispose ();
    }

    [Fact]
    public void ContextMenu_Is_Created_On_Focus ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        mv.SetFocus ();

        Assert.NotNull (mv.ContextMenu);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void ContextMenu_Is_Disposed_On_Losing_Focus ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        // Add another focusable view
        Button btn = new () { Text = "OK", X = 0, Y = 5 };
        window.Add (btn);
        app.LayoutAndDraw ();

        mv.SetFocus ();
        Assert.NotNull (mv.ContextMenu);

        // Move focus away
        btn.SetFocus ();
        Assert.Null (mv.ContextMenu);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - verifies that LeftButtonPressed is bound to Command.Activate
    [Fact]
    public void MouseBindings_LeftButtonPressed_IsBoundTo_Activate ()
    {
        Terminal.Gui.Views.Markdown mv = new ();

        bool found = mv.MouseBindings.TryGet (MouseFlags.LeftButtonPressed, out MouseBinding binding);

        Assert.True (found);
        Assert.Contains (Command.Activate, binding.Commands);

        mv.Dispose ();
    }

    // Copilot - verifies that LeftButtonPressed|PositionReport is bound to Command.Activate
    [Fact]
    public void MouseBindings_LeftButtonPressedPositionReport_IsBoundTo_Activate ()
    {
        Terminal.Gui.Views.Markdown mv = new ();

        bool found = mv.MouseBindings.TryGet (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, out MouseBinding binding);

        Assert.True (found);
        Assert.Contains (Command.Activate, binding.Commands);

        mv.Dispose ();
    }

    // Copilot - verifies that a drag (press + position-report) activates the selection
    [Fact]
    public void Drag_Mouse_Creates_Selection ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello World");

        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        // After a drag the selection should span columns 0-4
        Assert.True (mv.IsInSelection (0, 0));
        Assert.True (mv.IsInSelection (0, 3));
        Assert.False (mv.IsInSelection (0, 6));

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - verifies that mouse release does not clear an active selection
    [Fact]
    public void Selection_Persists_After_LeftButtonReleased ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello World");

        // Simulate a drag: press, drag, release
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });
        mv.NewMouseEvent (new Mouse { Position = new Point (5, 0), Flags = MouseFlags.LeftButtonReleased });

        // Selection should survive the release
        Assert.True (mv.IsInSelection (0, 0));

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - verifies that a plain click (no drag) clears the selection
    [Fact]
    public void Plain_Click_Clears_Selection ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello World");

        mv.SelectAll ();
        Assert.True (mv.IsInSelection (0, 0));

        // Simulate a plain click (no PositionReport drag events)
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased });
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });

        // Selection should be cleared by the click
        Assert.False (mv.IsInSelection (0, 0));

        window.Dispose ();
        app.Dispose ();
    }
}

