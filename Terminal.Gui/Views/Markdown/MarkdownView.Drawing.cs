namespace Terminal.Gui.Views;

public partial class Markdown
{
    /// <inheritdoc/>
    /// <remarks>
    ///     Draws all markdown content (backgrounds, text, styles) before SubViews are drawn.
    ///     This ensures copy <see cref="Button"/> SubViews render on top of code block backgrounds.
    /// </remarks>
    protected override bool OnDrawingSubViews (DrawContext? context)
    {
        Attribute fillAttr = GetAttributeForRole (VisualRole.Normal);

        if (UseThemeBackground && SyntaxHighlighter?.DefaultBackground is { } themeBg)
        {
            fillAttr = fillAttr with { Background = themeBg };
        }

        SetAttribute (fillAttr);
        FillRect (Viewport with { X = 0, Y = 0 }, (Rune)' ');

        int startRow = Viewport.Y;
        int endRow = Math.Min (Viewport.Y + Viewport.Height, _renderedLines.Count);

        for (int contentRow = startRow; contentRow < endRow; contentRow++)
        {
            int drawRow = contentRow - Viewport.Y;
            DrawRenderedLine (_renderedLines [contentRow], contentRow, drawRow);
        }

        // Return false so SubViews (copy buttons) still draw on top
        return false;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        // All visible content was drawn in OnDrawingSubViews; just register the drawn region.
        context?.AddDrawnRegion (new Region (new Rectangle (ContentToScreen (Point.Empty), Viewport.Size)));

        // OnDrawingContent is called AFTER SubViews have drawn.  Plain rendered lines are
        // highlighted during DrawRenderedLine (in OnDrawingSubViews), but table and code-block
        // rows are owned by their SubViews and don't receive that pass.  Draw the selection
        // overlay for those rows here, on top of what the SubViews rendered.
        if (_isSelecting)
        {
            DrawSelectionOverlayOnSubViewRows ();
        }

        return true;
    }

    /// <summary>
    ///     Draws the selection highlight over table and fenced-code-block rows.
    ///     Those rows are owned by SubViews (<see cref="MarkdownTable"/> /
    ///     <see cref="MarkdownCodeBlock"/>) that draw after
    ///     <see cref="OnDrawingSubViews"/> returns.  This pass reads the graphemes
    ///     that the SubViews already placed in the screen buffer and re-draws them
    ///     with the selection attribute, preserving the rendered characters while
    ///     applying the selection background.
    /// </summary>
    private void DrawSelectionOverlayOnSubViewRows ()
    {
        Cell [,]? contents = ScreenContents;

        if (contents is null)
        {
            return;
        }

        Attribute selAttr = GetAttributeForRole (VisualRole.Focus);
        (Point start, Point end) = GetNormalizedSelection ();

        int startRow = Math.Max (start.Y, Viewport.Y);
        int endRow = Math.Min (end.Y, Viewport.Y + Viewport.Height - 1);

        bool anySubViewRows = false;

        for (int lineIdx = startRow; lineIdx <= Math.Min (endRow, _renderedLines.Count - 1); lineIdx++)
        {
            if (_renderedLines [lineIdx].IsTable || _renderedLines [lineIdx].IsCodeBlock)
            {
                anySubViewRows = true;

                break;
            }
        }

        if (!anySubViewRows)
        {
            return;
        }

        // After DoDrawSubViews each SubView calls DoDrawComplete which excludes its screen
        // area from Driver.Clip.  DoDrawContent (OnDrawingContent) runs with those exclusions
        // still active, so drawing would silently no-op on SubView areas.
        // Reset the clip to the raw viewport rectangle to allow the overlay to appear.
        Region? savedClip = GetClip ();
        Rectangle viewportScreen = ViewportToScreen (new Rectangle (Point.Empty, Viewport.Size));
        SetClip (new Region (viewportScreen));

        SetAttribute (selAttr);

        for (int lineIdx = startRow; lineIdx <= Math.Min (endRow, _renderedLines.Count - 1); lineIdx++)
        {
            RenderedLine line = _renderedLines [lineIdx];

            if (!line.IsTable && !line.IsCodeBlock)
            {
                continue;
            }

            int drawRow = lineIdx - Viewport.Y;
            Point screenOrigin = ContentToScreen (new Point (0, drawRow));
            int screenRow = screenOrigin.Y;
            int screenStartCol = screenOrigin.X;
            int cols = Viewport.Width;

            for (int col = 0; col < cols; col++)
            {
                int sc = screenStartCol + col;

                if (screenRow < 0 || screenRow >= contents.GetLength (0) || sc < 0 || sc >= contents.GetLength (1))
                {
                    continue;
                }

                string grapheme = contents [screenRow, sc].Grapheme;

                if (string.IsNullOrEmpty (grapheme))
                {
                    grapheme = " ";
                }

                AddStr (col, drawRow, grapheme);
            }
        }

        SetClip (savedClip);
    }

    private void DrawRenderedLine (RenderedLine line, int contentRow, int drawRow)
    {
        // Thematic breaks are drawn by Line SubViews
        if (line.IsThematicBreak)
        {
            return;
        }

        // Table lines are drawn by MarkdownTable SubViews
        if (line.IsTable)
        {
            return;
        }

        // Code block lines are drawn by MarkdownCodeBlock SubViews
        if (line.IsCodeBlock)
        {
            return;
        }

        var contentX = 0;

        foreach (StyledSegment segment in line.Segments)
        {
            foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
            {
                int graphemeWidth = Math.Max (grapheme.GetColumns (), 1);
                bool visible = contentX + graphemeWidth > Viewport.X && contentX < Viewport.X + Viewport.Width;

                if (!visible)
                {
                    contentX += graphemeWidth;

                    continue;
                }

                int drawCol = contentX - Viewport.X;

                if (drawCol < 0 || drawCol >= Viewport.Width)
                {
                    contentX += graphemeWidth;

                    continue;
                }

                // If this grapheme is in the active link and the view has focus, use reversed highlight
                if (HasFocus && IsActiveLinkAt (contentRow, contentX))
                {
                    Attribute linkAttr = GetAttributeForSegment (segment);
                    Attribute reversed = new (linkAttr.Background, linkAttr.Foreground, linkAttr.Style);

                    if (!string.IsNullOrWhiteSpace (segment.Url) && Uri.IsWellFormedUriString (segment.Url, UriKind.Absolute) && Driver is { })
                    {
                        Driver.CurrentUrl = segment.Url;

                        try
                        {
                            SetAttribute (reversed);
                            AddStr (drawCol, drawRow, grapheme);
                        }
                        finally
                        {
                            Driver.CurrentUrl = null;
                        }
                    }
                    else
                    {
                        SetAttribute (reversed);
                        AddStr (drawCol, drawRow, grapheme);
                    }
                }
                else if (IsInSelection (contentRow, contentX))
                {
                    // Use the scheme's Focus attribute for selection highlight — it provides
                    // reliable contrast regardless of per-segment colours.
                    SetAttribute (GetAttributeForRole (VisualRole.Focus));
                    AddStr (drawCol, drawRow, grapheme);
                }
                else
                {
                    DrawGrapheme (segment, grapheme, drawCol, drawRow);
                }

                if (!string.IsNullOrWhiteSpace (segment.ImageSource))
                {
                    TryQueueSixel (segment.ImageSource!, new Point (drawCol, drawRow));
                }

                contentX += graphemeWidth;
            }
        }
    }

    private void DrawGrapheme (StyledSegment segment, string grapheme, int x, int y)
    {
        Attribute attr = GetAttributeForSegment (segment);

        if (!string.IsNullOrWhiteSpace (segment.Url) && Uri.IsWellFormedUriString (segment.Url, UriKind.Absolute) && Driver is { })
        {
            Driver.CurrentUrl = segment.Url;

            try
            {
                SetAttribute (attr);
                AddStr (x, y, grapheme);
            }
            finally
            {
                Driver.CurrentUrl = null;
            }

            return;
        }

        SetAttribute (attr);
        AddStr (x, y, grapheme);
    }

    private Attribute GetAttributeForSegment (StyledSegment segment) =>
        MarkdownAttributeHelper.GetAttributeForSegment (this, segment, SyntaxHighlighter, UseThemeBackground ? SyntaxHighlighter?.DefaultBackground : null);

    private void TryQueueSixel (string imageSource, Point screenPosition)
    {
        if (!EnableSixelImages || Driver is null)
        {
            return;
        }

        if (!MarkdownImageResolver.TryGetSixelData (ImageLoader, imageSource, out string sixelData))
        {
            return;
        }

        var queueId = $"{imageSource}:{screenPosition.X}:{screenPosition.Y}";

        if (!_queuedSixelIds.Add (queueId))
        {
            return;
        }

        Driver.GetSixels ().Enqueue (new SixelToRender { Id = queueId, ScreenPosition = ContentToScreen (screenPosition), SixelData = sixelData });
    }
}
