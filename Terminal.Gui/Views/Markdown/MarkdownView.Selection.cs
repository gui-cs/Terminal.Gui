namespace Terminal.Gui.Views;

public partial class Markdown
{
    private bool _isSelecting;
    private Point _selectionAnchor;
    private Point _selectionCurrent;
    private bool _isDragging;

    /// <summary>Gets the context menu for this view.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    /// <summary>Selects all rendered content.</summary>
    /// <returns><see langword="true"/> if the operation succeeded.</returns>
    public bool SelectAll ()
    {
        if (_renderedLines.Count == 0)
        {
            return true;
        }

        _isSelecting = true;
        _selectionAnchor = new Point (0, 0);
        int lastLine = _renderedLines.Count - 1;
        _selectionCurrent = new Point (GetLineDisplayWidth (lastLine), lastLine);
        SetNeedsDraw ();

        return true;
    }

    /// <summary>
    ///     Gets the text that corresponds to the current selection, rendered as plain text from
    ///     the displayed content.  Returns <see langword="null"/> when no selection is active.
    /// </summary>
    /// <remarks>
    ///     The returned string reflects the on-screen representation (display text) of the selected
    ///     region — not the original markdown source.  Markdown structure such as bullet-list
    ///     markers (<c>- </c>), fenced code-block delimiters (<c>```</c>), and heading hashes
    ///     (<c>#</c>) may differ from the source document.
    /// </remarks>
    public string? SelectedText => _isSelecting ? GetSelectedText () : null;

    /// <summary>
    ///     Copies the current selection, or the entire markdown document if nothing is selected, to the clipboard.
    /// </summary>
    /// <returns><see langword="true"/> if the copy was performed.</returns>
    public bool Copy ()
    {
        string text = _isSelecting ? GetSelectedText () : _markdown;
        App?.Clipboard?.TrySetClipboardData (text);

        return true;
    }

    /// <summary>Clears the current selection.</summary>
    public void ClearSelection ()
    {
        if (!_isSelecting)
        {
            return;
        }

        _isSelecting = false;
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the cell at (<paramref name="lineIdx"/>, <paramref name="x"/>)
    ///     falls within the current selection.
    /// </summary>
    /// <param name="lineIdx">The rendered-line index (content Y coordinate).</param>
    /// <param name="x">The display column (content X coordinate).</param>
    internal bool IsInSelection (int lineIdx, int x)
    {
        if (!_isSelecting)
        {
            return false;
        }

        (Point start, Point end) = GetNormalizedSelection ();

        if (lineIdx < start.Y || lineIdx > end.Y)
        {
            return false;
        }

        if (start.Y == end.Y)
        {
            return x >= start.X && x < end.X;
        }

        if (lineIdx == start.Y)
        {
            return x >= start.X;
        }

        if (lineIdx == end.Y)
        {
            return x < end.X;
        }

        return true;
    }

    private (Point Start, Point End) GetNormalizedSelection ()
    {
        if (_selectionAnchor.Y < _selectionCurrent.Y || (_selectionAnchor.Y == _selectionCurrent.Y && _selectionAnchor.X <= _selectionCurrent.X))
        {
            return (_selectionAnchor, _selectionCurrent);
        }

        return (_selectionCurrent, _selectionAnchor);
    }

    private string GetSelectedText ()
    {
        if (_renderedLines.Count == 0)
        {
            return string.Empty;
        }

        // When the entire document is selected return the original source markdown.
        // _renderedLines is a display buffer: it cannot carry inline formatting markers,
        // heading hashes, table syntax, or thematic-break syntax.  Returning _markdown
        // preserves everything and is always correct for a full-document selection.
        if (IsFullDocumentSelected ())
        {
            return _markdown;
        }

        (Point start, Point end) = GetNormalizedSelection ();
        List<string> outputLines = [];
        bool inCodeBlock = false;

        string? currentCodeLanguage = null;

        // Track the last table instance that was output.  All placeholder rows for the
        // same table share the same TableData reference, so we use ReferenceEquals to
        // emit the reconstructed table markdown exactly once even when the selection
        // covers multiple placeholder rows belonging to the same table.
        TableData? lastOutputtedTable = null;

        for (int lineIdx = start.Y; lineIdx <= Math.Min (end.Y, _renderedLines.Count - 1); lineIdx++)
        {
            RenderedLine line = _renderedLines [lineIdx];

            if (line.IsCodeBlock)
            {
                string? nextCodeLanguage = line.CodeLanguage;

                if (!inCodeBlock)
                {
                    // Entering a code block: inject the opening fence with optional language tag
                    outputLines.Add ($"```{nextCodeLanguage ?? string.Empty}");
                    inCodeBlock = true;
                    currentCodeLanguage = nextCodeLanguage;
                }
                else if (!string.Equals (currentCodeLanguage, nextCodeLanguage, StringComparison.Ordinal))
                {
                    // Transitioning directly between two code blocks: close the current fence
                    // and open the next one so adjacent fenced blocks are preserved.
                    outputLines.Add ("```");
                    outputLines.Add ($"```{nextCodeLanguage ?? string.Empty}");
                    currentCodeLanguage = nextCodeLanguage;
                }
            }
            else if (inCodeBlock)
            {
                // Leaving a code block: inject the closing fence
                outputLines.Add ("```");
                inCodeBlock = false;
                currentCodeLanguage = null;
            }

            if (line.IsTable && line.TableData is { } tableData)
            {
                // Each table occupies several zero-width placeholder rows that all share the
                // same TableData instance.  Only reconstruct the table markdown the first time
                // we encounter each distinct instance; skip subsequent placeholder rows.
                if (!ReferenceEquals (tableData, lastOutputtedTable))
                {
                    foreach (string tableLine in RenderTableAsMarkdown (tableData))
                    {
                        outputLines.Add (tableLine);
                    }

                    lastOutputtedTable = tableData;
                }

                continue;
            }

            int lineStartX = lineIdx == start.Y ? start.X : 0;
            int lineEndX = lineIdx == end.Y ? end.X : int.MaxValue;
            StringBuilder lineSb = new ();
            AppendLineText (lineSb, line, lineStartX, lineEndX);
            outputLines.Add (lineSb.ToString ());
        }

        if (inCodeBlock)
        {
            outputLines.Add ("```");
        }

        return string.Join ("\n", outputLines);
    }

    /// <summary>
    ///     Returns <see langword="true"/> when the selection spans the entire rendered document
    ///     from the first character to the last, so that <see cref="GetSelectedText"/> can
    ///     return the original markdown source instead of the lossy display representation.
    /// </summary>
    private bool IsFullDocumentSelected ()
    {
        (Point start, Point end) = GetNormalizedSelection ();

        if (start.X != 0 || start.Y != 0)
        {
            return false;
        }

        int lastLine = _renderedLines.Count - 1;

        // For a document ending with a zero-width placeholder (table rows, thematic breaks),
        // GetLineDisplayWidth(lastLine) returns 0, so end.X >= 0 is always satisfied.
        // This is intentional: any position on a zero-width row is equivalent to the end of
        // that row (there is no content there), so reaching the last row from (0,0) means the
        // entire document is selected.
        return end.Y >= lastLine && end.X >= GetLineDisplayWidth (lastLine);
    }

    /// <summary>Reconstructs GFM pipe-table markdown lines from a <see cref="TableData"/> instance.</summary>
    private static IEnumerable<string> RenderTableAsMarkdown (TableData tableData)
    {
        // Header row
        yield return "| " + string.Join (" | ", tableData.Headers) + " |";

        // Separator row — encode column alignment
        IEnumerable<string> separators = tableData.ColumnAlignments.Select (
            alignment => alignment switch
            {
                Alignment.Center => ":---:",
                Alignment.End    => "---:",
                _                => "---"
            });
        yield return "| " + string.Join (" | ", separators) + " |";

        // Body rows
        foreach (string [] row in tableData.Rows)
        {
            yield return "| " + string.Join (" | ", row) + " |";
        }
    }

    private static void AppendLineText (StringBuilder sb, RenderedLine line, int startX, int endX)
    {
        var contentX = 0;

        foreach (StyledSegment segment in line.Segments)
        {
            string text = segment.StyleRole == MarkdownStyleRole.ListMarker
                              ? TranslateListMarkerText (segment.Text)
                              : segment.Text;

            foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
            {
                int gw = Math.Max (grapheme.GetColumns (), 1);

                if (contentX + gw <= startX)
                {
                    contentX += gw;

                    continue;
                }

                if (contentX >= endX)
                {
                    return;
                }

                sb.Append (grapheme);
                contentX += gw;
            }
        }
    }

    /// <summary>Converts rendered list marker text back to Markdown source marker text for selection and clipboard operations.</summary>
    /// <param name="text">The rendered list marker text.</param>
    /// <returns>The Markdown source marker text, or <paramref name="text"/> when it is not a rendered list marker.</returns>
    private static string TranslateListMarkerText (string text)
    {
        const string BulletPrefix = "• ";

        if (!text.StartsWith (BulletPrefix, StringComparison.Ordinal))
        {
            return text;
        }

        string remainder = text [BulletPrefix.Length..];

        string checkedGlyph = $"{Glyphs.CheckStateChecked} ";
        string uncheckedGlyph = $"{Glyphs.CheckStateUnChecked} ";

        if (remainder.StartsWith (checkedGlyph, StringComparison.Ordinal))
        {
            return "- [x] " + remainder [checkedGlyph.Length..];
        }

        if (remainder.StartsWith (uncheckedGlyph, StringComparison.Ordinal))
        {
            return "- [ ] " + remainder [uncheckedGlyph.Length..];
        }

        return "- " + remainder;
    }

    private int GetLineDisplayWidth (int lineIdx)
    {
        if (lineIdx < 0 || lineIdx >= _renderedLines.Count)
        {
            return 0;
        }

        return _renderedLines [lineIdx].Width;
    }

    private void CreateContextMenu ()
    {
        DisposeContextMenu ();

        PopoverMenu menu = new ([new MenuItem (this, Command.SelectAll), new MenuItem (this, Command.Copy)])
        {
#if DEBUG
            Id = "markdownContextMenu"
#endif
        };

        HotKeyBindings.Remove (menu.Key);
        HotKeyBindings.Add (menu.Key, Command.Context);
        menu.KeyChanged += ContextMenuOnKeyChanged;

        ContextMenu = menu;
        App?.Popovers?.Register (ContextMenu);
    }

    private void DisposeContextMenu ()
    {
        if (ContextMenu is null)
        {
            return;
        }

        ContextMenu.Visible = false;
        App?.Popovers?.DeRegister (ContextMenu);
        ContextMenu.KeyChanged -= ContextMenuOnKeyChanged;
        ContextMenu.Dispose ();
        ContextMenu = null;
    }

    private void ContextMenuOnKeyChanged (object? sender, KeyChangedEventArgs e) => KeyBindings.Replace (e.OldKey.KeyCode, e.NewKey.KeyCode);

    private Point GetContextMenuScreenPosition ()
    {
        Point viewportPosition = _isSelecting ? _selectionCurrent : new Point (0, 0);

        return ViewportToScreen (viewportPosition);
    }

    private bool ShowContextMenu (Point? screenPosition = null)
    {
        Point menuPosition = screenPosition ?? GetContextMenuScreenPosition ();
        ContextMenu?.MakeVisible (menuPosition);

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            DisposeContextMenu ();
        }

        base.Dispose (disposing);
    }
}
