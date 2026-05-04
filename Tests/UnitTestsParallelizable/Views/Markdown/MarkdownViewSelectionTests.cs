using JetBrains.Annotations;

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

    // --- Copy fidelity tests (these FAIL with the current implementation) ---
    // The current GetSelectedText reads from rendered lines (display text),
    // which loses markdown structure. These tests document the expected behaviour.

    // Copilot
    [Fact]
    public void SelectedText_Is_Null_When_No_Selection ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        Assert.Null (mv.SelectedText);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: GetSelectedText returns "• foo" (Unicode bullet), not "- foo"
    [Fact]
    public void SelectAll_BulletList_SelectedText_Preserves_Markdown_List_Markers ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("- foo\n- bar\n- baz", 40, 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);

        // Expect standard markdown unordered list syntax, not Unicode bullets
        Assert.Contains ("- foo", selected);
        Assert.Contains ("- bar", selected);
        Assert.Contains ("- baz", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: GetSelectedText returns "• foo" (Unicode bullet), not "* foo"
    [Fact]
    public void SelectAll_AsteriskBulletList_SelectedText_Preserves_Markdown_List_Markers ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("* alpha\n* beta\n* gamma", 40, 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);

        // Expect recognisable markdown list syntax (- or *), not Unicode bullets
        Assert.DoesNotContain ("•", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: fenced code block fence lines are never added to _renderedLines
    [Fact]
    public void SelectAll_FencedCodeBlock_With_Language_SelectedText_Preserves_Fences ()
    {
        var md = "```cs\nvar x = 1;\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);

        // Must contain the opening fence with language tag and the code itself
        Assert.Contains ("```cs", selected);
        Assert.Contains ("var x = 1;", selected);

        // Must contain the closing fence
        Assert.Contains ("```", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: fences lost even without a language specifier
    [Fact]
    public void SelectAll_FencedCodeBlock_Without_Language_SelectedText_Preserves_Fences ()
    {
        var md = "```\nhello world\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("```", selected);
        Assert.Contains ("hello world", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: Copy() calls GetSelectedText() even for SelectAll, losing bullet markers
    [Fact]
    public void Copy_After_SelectAll_BulletList_Clipboard_Contains_Markdown_Markers ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("- one\n- two\n- three");

        mv.SelectAll ();
        mv.Copy ();

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Contains ("- one", clipboard);
        Assert.Contains ("- two", clipboard);
        Assert.Contains ("- three", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: Copy() loses code fences when selection is active
    [Fact]
    public void Copy_After_SelectAll_FencedCodeBlock_Clipboard_Contains_Fences ()
    {
        var md = "```python\nprint('hi')\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md);

        mv.SelectAll ();
        mv.Copy ();

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Contains ("```python", clipboard);
        Assert.Contains ("print('hi')", clipboard);
        Assert.Contains ("```", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: a partial selection of bullet items loses the list markers
    [Fact]
    public void PartialSelection_BulletList_SelectedText_Preserves_Markdown_Markers ()
    {
        // Two bullet items. We select both rendered lines (Y=0 and Y=1).
        var md = "- alpha\n- beta";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md);

        // Anchor at start of line 0, extend to end of line 1
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (10, 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("- alpha", selected);
        Assert.Contains ("- beta", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: partial selection inside a fenced code block loses fence context
    [Fact]
    public void PartialSelection_FencedCodeBlock_SelectedText_Preserves_Fence_Context ()
    {
        var md = "```cs\nint a = 1;\nint b = 2;\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md);

        // Select both code lines (Y=0 and Y=1 in rendered output — fence lines don't appear)
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (10, 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);

        // The selection is from inside a code block — the language tag must survive
        Assert.Contains ("```cs", selected);
        Assert.Contains ("int a = 1;", selected);

        window.Dispose ();
        app.Dispose ();
    }
}
