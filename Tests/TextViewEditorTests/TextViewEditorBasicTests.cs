// Copilot

using Terminal.Gui.Views;

namespace TextViewEditorTests;

/// <summary>
///     Unit tests for <see cref="TextViewEditor"/>.
/// </summary>
public class TextViewEditorBasicTests
{
    [Fact]
    public void Constructor_Creates_Instance ()
    {
        TextViewEditor editor = new ();

        Assert.NotNull (editor);
        Assert.NotNull (editor.UnderlyingEditor);
        Assert.NotNull (editor.Document);
    }

    [Fact]
    public void Text_SetAndGet_RoundTrips ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Hello, World!";

        Assert.Equal ("Hello, World!", editor.Text);
    }

    [Fact]
    public void Text_SetNull_BecomesEmpty ()
    {
        TextViewEditor editor = new ();

        editor.Text = null!;

        Assert.Equal (string.Empty, editor.Text);
    }

    [Fact]
    public void Lines_ReturnsCorrectCount ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Line1\nLine2\nLine3";

        Assert.Equal (3, editor.Lines);
    }

    [Fact]
    public void CurrentRow_AfterSetText_IsZero ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Line1\nLine2";

        Assert.Equal (0, editor.CurrentRow);
    }

    [Fact]
    public void CurrentColumn_AfterSetText_IsZero ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Hello";

        Assert.Equal (0, editor.CurrentColumn);
    }

    [Fact]
    public void InsertionPoint_SetAndGet ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Line1\nLine2\nLine3";
        editor.InsertionPoint = new Point (3, 1);

        Assert.Equal (1, editor.CurrentRow);
        Assert.Equal (3, editor.CurrentColumn);
        Assert.Equal (new Point (3, 1), editor.InsertionPoint);
    }

    [Fact]
    public void InsertionPoint_ClampsToBounds ()
    {
        TextViewEditor editor = new ();

        editor.Text = "AB\nCD";

        // Set beyond line length
        editor.InsertionPoint = new Point (100, 0);
        Assert.Equal (2, editor.CurrentColumn);

        // Set beyond line count
        editor.InsertionPoint = new Point (0, 100);
        Assert.Equal (1, editor.CurrentRow);
    }

    [Fact]
    public void ReadOnly_DefaultFalse ()
    {
        TextViewEditor editor = new ();

        Assert.False (editor.ReadOnly);
    }

    [Fact]
    public void ReadOnly_SetTrue ()
    {
        TextViewEditor editor = new ();

        editor.ReadOnly = true;

        Assert.True (editor.ReadOnly);
    }

    [Fact]
    public void TabWidth_DefaultIs4 ()
    {
        TextViewEditor editor = new ();

        Assert.Equal (4, editor.TabWidth);
    }

    [Fact]
    public void TabWidth_SetAndGet ()
    {
        TextViewEditor editor = new ();

        editor.TabWidth = 8;

        Assert.Equal (8, editor.TabWidth);
    }

    [Fact]
    public void TabWidth_MinimumIsOne ()
    {
        TextViewEditor editor = new ();

        editor.TabWidth = 0;

        Assert.Equal (1, editor.TabWidth);
    }

    [Fact]
    public void IsSelecting_DefaultFalse ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Hello";

        Assert.False (editor.IsSelecting);
    }

    [Fact]
    public void SelectedText_WhenNoSelection_ReturnsEmpty ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Hello";

        Assert.Equal (string.Empty, editor.SelectedText);
    }

    [Fact]
    public void SelectedLength_WhenNoSelection_ReturnsZero ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Hello";

        Assert.Equal (0, editor.SelectedLength);
    }

    [Fact]
    public void Load_Stream_LoadsContent ()
    {
        TextViewEditor editor = new ();

        using MemoryStream stream = new (System.Text.Encoding.UTF8.GetBytes ("Stream content"));
        editor.Load (stream);

        Assert.Equal ("Stream content", editor.Text);
    }

    [Fact]
    public void Load_NonExistentFile_ReturnsFalse ()
    {
        TextViewEditor editor = new ();

        bool result = editor.Load ("/nonexistent/path/file.txt");

        Assert.False (result);
    }

    [Fact]
    public void ContentsChanged_RaisedOnTextChange ()
    {
        TextViewEditor editor = new ();
        bool raised = false;
        editor.ContentsChanged += (_, _) => raised = true;

        editor.Text = "new content";

        // ContentsChanged is raised via the document's Changed event
        // When Text is set, it replaces the document content which fires Changed
        Assert.True (raised);
    }

    [Fact]
    public void SelectAll_SelectsEntireDocument ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Hello";
        editor.SelectAll ();

        Assert.True (editor.IsSelecting);
        Assert.Equal ("Hello", editor.SelectedText);
        Assert.Equal (5, editor.SelectedLength);
    }

    [Fact]
    public void ClearSelection_RemovesSelection ()
    {
        TextViewEditor editor = new ();

        editor.Text = "Hello";
        editor.SelectAll ();
        editor.ClearSelection ();

        Assert.False (editor.IsSelecting);
        Assert.Equal (string.Empty, editor.SelectedText);
    }
}
