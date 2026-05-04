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

        (Point start, Point end) = GetNormalizedSelection ();
        StringBuilder sb = new ();

        for (int lineIdx = start.Y; lineIdx <= Math.Min (end.Y, _renderedLines.Count - 1); lineIdx++)
        {
            if (lineIdx > start.Y)
            {
                sb.Append ('\n');
            }

            int lineStartX = lineIdx == start.Y ? start.X : 0;
            int lineEndX = lineIdx == end.Y ? end.X : int.MaxValue;
            AppendLineText (sb, _renderedLines [lineIdx], lineStartX, lineEndX);
        }

        return sb.ToString ();
    }

    private static void AppendLineText (StringBuilder sb, RenderedLine line, int startX, int endX)
    {
        var contentX = 0;

        foreach (StyledSegment segment in line.Segments)
        {
            foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
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

    private bool ShowContextMenu (Point? screenPosition = null)
    {
        ContextMenu?.MakeVisible (screenPosition);

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
