namespace Terminal.Gui.Views;

public partial class MarkdownView
{
    /// <inheritdoc />
    /// <remarks>
    ///     Draws all markdown content (backgrounds, text, styles) before SubViews are drawn.
    ///     This ensures copy <see cref="Button"/> SubViews render on top of code block backgrounds.
    /// </remarks>
    protected override bool OnDrawingSubViews (DrawContext? context)
    {
        EnsureLayout ();

        SetAttributeForRole (VisualRole.Normal);
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

    /// <inheritdoc />
    protected override bool OnDrawingContent (DrawContext? context)
    {
        // All visible content was drawn in OnDrawingSubViews; just register the drawn region.
        context?.AddDrawnRegion (new Region (new Rectangle (ContentToScreen (Point.Empty), Viewport.Size)));

        return true;
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

                // If this grapheme is in the active link, use reversed highlight
                if (IsActiveLinkAt (contentRow, contentX))
                {
                    Attribute linkAttr = GetAttributeForSegment (segment);
                    SetAttribute (new Attribute (linkAttr.Background, linkAttr.Foreground, linkAttr.Style));
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

    private Attribute GetAttributeForSegment (StyledSegment segment) => MarkdownAttributeHelper.GetAttributeForSegment (this, segment);

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
