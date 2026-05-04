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

    // --- Copy fidelity tests ---
    // These use content from Markdown.DefaultMarkdownSample where possible, which contains
    // Unicode characters (emoji), task-list markers, and fenced code blocks with language tags.

    // Copilot
    [Fact]
    public void SelectedText_Is_Null_When_No_Selection ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        Assert.Null (mv.SelectedText);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - task-list items from DefaultMarkdownSample (includes emoji ✅ 🔧 🎉)
    [Fact]
    public void SelectAll_TaskList_SelectedText_Preserves_Markdown_List_Markers ()
    {
        // Source task-list lines taken from Markdown.DefaultMarkdownSample § Checklist
        string md = "- [x] Bold & italic ✅\n- [x] Code blocks 🔧\n- [ ] Emojis 🎉";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("- [x] Bold & italic ✅", selected);
        Assert.Contains ("- [x] Code blocks 🔧", selected);
        Assert.Contains ("- [ ] Emojis 🎉", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - plain bullet list (no task markers)
    [Fact]
    public void SelectAll_BulletList_SelectedText_Preserves_Markdown_List_Markers ()
    {
        string md = "- foo\n- bar\n- baz";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 40, height: 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.DoesNotContain ("•", selected);
        Assert.Contains ("- foo", selected);
        Assert.Contains ("- bar", selected);
        Assert.Contains ("- baz", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - fenced code block from DefaultMarkdownSample (csharp, contains 🌍 emoji)
    [Fact]
    public void SelectAll_FencedCodeBlock_With_Language_SelectedText_Preserves_Fences ()
    {
        // Source taken from Markdown.DefaultMarkdownSample § Code Block (csharp)
        string md = "```csharp\nConsole.WriteLine (\"Hello, Terminal.Gui! 🌍\");\nvar x = 42;\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("```csharp", selected);
        Assert.Contains ("Console.WriteLine", selected);
        Assert.Contains ("🌍", selected);
        Assert.Contains ("var x = 42;", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - fenced code block without language specifier
    [Fact]
    public void SelectAll_FencedCodeBlock_Without_Language_SelectedText_Preserves_Fences ()
    {
        string md = "```\nhello world\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 40, height: 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("```", selected);
        Assert.Contains ("hello world", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - Copy() after SelectAll with task list (includes emoji)
    [Fact]
    public void Copy_After_SelectAll_TaskList_Clipboard_Contains_Markdown_Markers ()
    {
        string md = "- [x] Bold & italic ✅\n- [x] Code blocks 🔧\n- [ ] Emojis 🎉";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        mv.SelectAll ();
        mv.Copy ();

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Contains ("- [x] Bold & italic ✅", clipboard);
        Assert.Contains ("- [x] Code blocks 🔧", clipboard);
        Assert.Contains ("- [ ] Emojis 🎉", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - Copy() after SelectAll with csharp code block (includes 🌍)
    [Fact]
    public void Copy_After_SelectAll_FencedCodeBlock_Clipboard_Contains_Fences ()
    {
        string md = "```csharp\nConsole.WriteLine (\"Hello, Terminal.Gui! 🌍\");\nvar x = 42;\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        mv.SelectAll ();
        mv.Copy ();

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Contains ("```csharp", clipboard);
        Assert.Contains ("🌍", clipboard);
        Assert.Contains ("var x = 42;", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - partial drag selection spanning task-list items with emoji
    [Fact]
    public void PartialSelection_TaskList_SelectedText_Preserves_Markdown_Markers ()
    {
        string md = "- [x] Bold & italic ✅\n- [ ] Emojis 🎉";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        // Press at start of line 0, drag to column 10 of line 1
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (10, 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("- [x] Bold & italic ✅", selected);
        Assert.Contains ("- [ ]", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - partial drag selection spanning lines inside a csharp code block (with 🌍)
    [Fact]
    public void PartialSelection_FencedCodeBlock_SelectedText_Preserves_Fence_Context ()
    {
        string md = "```csharp\nConsole.WriteLine (\"Hello, Terminal.Gui! 🌍\");\nvar x = 42;\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        // Select both code lines (rendered as lines 0 and 1 — fence lines are not in _renderedLines)
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (12, 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("```csharp", selected);
        Assert.Contains ("Console.WriteLine", selected);
        Assert.Contains ("🌍", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: inline formatting (**bold**, *italic*, `code`, ~~strike~~), heading
    // markers (#), tables, thematic breaks, and link syntax ([text](url)) are all lost in
    // the display representation — the selected text cannot equal the original markdown source.
    [Fact]
    public void SelectAll_DefaultMarkdownSample_SelectedText_RoundTrips ()
    {
        string md = Terminal.Gui.Views.Markdown.DefaultMarkdownSample;
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 120, height: 60);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Equal (md, selected);

        window.Dispose ();
        app.Dispose ();
    }
}
