using System.Drawing;
using Terminal.Gui.Document;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="View"/> that wraps the <c>Terminal.Gui.Editor</c> package's
///     <see cref="Terminal.Gui.Editor.Editor"/> view with an API that is compatible with
///     <see cref="TextView"/>, easing migration from <see cref="TextView"/> to the
///     <c>gui-cs/Editor</c> package.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="TextViewEditor"/> delegates all text editing to the underlying
///         <see cref="Terminal.Gui.Editor.Editor"/> while exposing properties and methods that match
///         the <see cref="TextView"/> API. This enables existing code that uses <see cref="TextView"/>
///         to migrate incrementally by swapping the type to <see cref="TextViewEditor"/> with
///         minimal source changes.
///     </para>
///     <para>
///         Not all <see cref="TextView"/> features are supported. Unsupported features will
///         throw <see cref="NotSupportedException"/> or no-op as documented on each member.
///     </para>
/// </remarks>
public class TextViewEditor : View
{
    // Initialized to null! because the base View constructor calls the virtual Text setter
    // (via SetupText) before this constructor body runs. The null guard in Text protects against this.
    private readonly Terminal.Gui.Editor.Editor _editor = null!;

    /// <summary>
    ///     Initializes a new instance of <see cref="TextViewEditor"/>.
    /// </summary>
    public TextViewEditor ()
    {
        CanFocus = true;

        _editor = new Terminal.Gui.Editor.Editor
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        _editor.CaretChanged += (_, _) => CaretChanged?.Invoke (this, EventArgs.Empty);
        _editor.SelectionChanged += (_, _) => SelectionChanged?.Invoke (this, EventArgs.Empty);

        // Subscribe to document text changes
        _editor.Document.Changed += (_, _) => OnContentsChanged ();

        Add (_editor);
    }

    /// <summary>
    ///     Gets or sets the text content. This is the primary <see cref="TextView"/>-compatible
    ///     text property.
    /// </summary>
    /// <remarks>
    ///     Setting this property replaces the entire document content and resets the cursor
    ///     position to the beginning.
    /// </remarks>
    public override string Text
    {
        get => _editor?.Document.Text ?? string.Empty;
        set
        {
            if (_editor is null)
            {
                return;
            }

            _editor.Document.Text = value ?? string.Empty;
            _editor.CaretOffset = 0;
            OnTextChanged ();
        }
    }

    /// <summary>
    ///     Gets the cursor column (zero-based offset within the current line).
    /// </summary>
    public int CurrentColumn
    {
        get
        {
            DocumentLine line = _editor.Document.GetLineByOffset (_editor.CaretOffset);

            return _editor.CaretOffset - line.Offset;
        }
    }

    /// <summary>
    ///     Gets the current cursor row (zero-based line number).
    /// </summary>
    public int CurrentRow
    {
        get
        {
            DocumentLine line = _editor.Document.GetLineByOffset (_editor.CaretOffset);

            return line.LineNumber - 1; // DocumentLine uses 1-based line numbers
        }
    }

    /// <summary>
    ///     Gets or sets the cursor position as a <see cref="Point"/> where
    ///     <c>X</c> is the column and <c>Y</c> is the row.
    /// </summary>
    public Point InsertionPoint
    {
        get => new (CurrentColumn, CurrentRow);
        set
        {
            int row = Math.Max (0, Math.Min (value.Y, _editor.Document.LineCount - 1));
            DocumentLine line = _editor.Document.GetLineByNumber (row + 1);
            int col = Math.Max (0, Math.Min (value.X, line.Length));
            _editor.CaretOffset = line.Offset + col;
        }
    }

    /// <summary>
    ///     Gets the number of lines in the document.
    /// </summary>
    public int Lines => _editor.Document.LineCount;

    /// <summary>
    ///     Gets or sets whether the editor is in read-only mode.
    /// </summary>
    public bool ReadOnly
    {
        get => _editor.ReadOnly;
        set => _editor.ReadOnly = value;
    }

    /// <summary>
    ///     Gets or sets the tab width (indentation size). Equivalent to <see cref="TextView.TabWidth"/>.
    /// </summary>
    public int TabWidth
    {
        get => _editor.IndentationSize;
        set => _editor.IndentationSize = Math.Max (value, 1);
    }

    /// <summary>
    ///     Gets the selected text, or <see cref="string.Empty"/> if no selection exists.
    /// </summary>
    public string SelectedText
    {
        get
        {
            if (!_editor.HasSelection)
            {
                return string.Empty;
            }

            return _editor.SelectedText ?? string.Empty;
        }
    }

    /// <summary>
    ///     Gets whether the user currently has text selected.
    /// </summary>
    public bool IsSelecting => _editor.HasSelection;

    /// <summary>
    ///     Gets the length of the current selection.
    /// </summary>
    public int SelectedLength => _editor.SelectionLength;

    /// <summary>
    ///     Gets whether the text has been modified since last load or clear.
    /// </summary>
    public bool IsDirty => _editor.Document.UndoStack.CanUndo;

    /// <summary>
    ///     Raised when the caret (cursor) position changes.
    /// </summary>
    public event EventHandler? CaretChanged;

    /// <summary>
    ///     Raised when the selection changes.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    ///     Raised when the contents of the editor are changed.
    /// </summary>
    public event EventHandler? ContentsChanged;

    /// <summary>
    ///     Loads text from a file path.
    /// </summary>
    /// <param name="path">The file path to load.</param>
    /// <returns><see langword="true"/> if the file was loaded successfully.</returns>
    public bool Load (string path)
    {
        if (!File.Exists (path))
        {
            return false;
        }

        string content = File.ReadAllText (path);
        _editor.Document.Text = content;
        _editor.CaretOffset = 0;
        _editor.Document.UndoStack.MarkAsOriginalFile ();
        OnTextChanged ();

        return true;
    }

    /// <summary>
    ///     Loads text from a stream.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    public void Load (Stream stream)
    {
        using StreamReader reader = new (stream);
        string content = reader.ReadToEnd ();
        _editor.Document.Text = content;
        _editor.CaretOffset = 0;
        _editor.Document.UndoStack.MarkAsOriginalFile ();
        OnTextChanged ();
    }

    /// <summary>
    ///     Selects all text in the editor.
    /// </summary>
    public void SelectAll ()
    {
        _editor.SelectAll ();
    }

    /// <summary>
    ///     Clears the current selection.
    /// </summary>
    public void ClearSelection ()
    {
        _editor.ClearSelection ();
    }

    /// <summary>
    ///     Gets the underlying <see cref="Terminal.Gui.Editor.Editor"/> instance for advanced usage.
    /// </summary>
    /// <remarks>
    ///     Use this property to access Editor-specific features not exposed by the
    ///     <see cref="TextView"/>-compatible API, such as folding, syntax highlighting,
    ///     multi-caret editing, and the rendering pipeline.
    /// </remarks>
    public Terminal.Gui.Editor.Editor UnderlyingEditor => _editor;

    /// <summary>
    ///     Gets the underlying <see cref="TextDocument"/> for direct document manipulation.
    /// </summary>
    public TextDocument Document => _editor.Document;

    /// <summary>
    ///     Raises the <see cref="ContentsChanged"/> event.
    /// </summary>
    protected virtual void OnContentsChanged ()
    {
        ContentsChanged?.Invoke (this, EventArgs.Empty);
    }
}
