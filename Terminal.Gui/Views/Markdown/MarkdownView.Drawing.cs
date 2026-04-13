using Terminal.Gui.Drawing;

namespace Terminal.Gui.Views;

public partial class MarkdownView
{
    protected override bool OnDrawingContent (DrawContext? context)
    {
        EnsureLayout ();

        _linkRanges.Clear ();

        SetAttributeForRole (VisualRole.Normal);
        ClearRegion (0, 0, Viewport.Width, Viewport.Height);

        int startRow = Viewport.Y;
        int endRow = Math.Min (Viewport.Y + Viewport.Height, _renderedLines.Count);

        for (int contentRow = startRow; contentRow < endRow; contentRow++)
        {
            int drawRow = contentRow - Viewport.Y;
            DrawRenderedLine (_renderedLines [contentRow], contentRow, drawRow, context);
        }

        return true;
    }

    private void DrawRenderedLine (RenderedLine line, int contentRow, int drawRow, DrawContext? context)
    {
        int contentX = 0;

        foreach (StyledSegment segment in line.Segments)
        {
            foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
            {
                int graphemeWidth = Math.Max (grapheme.GetColumns (), 1);
                bool visible = contentX + graphemeWidth > Viewport.X && contentX < Viewport.X + Viewport.Width;

                if (visible)
                {
                    int drawCol = contentX - Viewport.X;

                    if (drawCol >= 0 && drawCol < Viewport.Width)
                    {
                        DrawGrapheme (segment, grapheme, drawCol, drawRow);

                        if (!string.IsNullOrWhiteSpace (segment.Url))
                        {
                            _linkRanges.Add (new MarkdownLinkRange
                            {
                                Y = contentRow,
                                StartX = contentX,
                                EndXExclusive = contentX + graphemeWidth,
                                Url = segment.Url!
                            });
                        }

                        if (!string.IsNullOrWhiteSpace (segment.ImageSource))
                        {
                            TryQueueSixel (segment.ImageSource!, new Point (drawCol, drawRow));
                        }
                    }
                }

                contentX += graphemeWidth;
            }
        }

        context?.AddDrawnRegion (new Region (new Rectangle (ContentToScreen (new Point (0, drawRow)), new Size (Viewport.Width, 1))));
    }

    private void DrawGrapheme (StyledSegment segment, string grapheme, int x, int y)
    {
        Attribute attr = GetAttributeForSegment (segment.StyleRole);

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

    private Attribute GetAttributeForSegment (MarkdownStyleRole role)
    {
        Attribute normal = HasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal);

        switch (role)
        {
        case MarkdownStyleRole.Heading:
            return normal with { Style = normal.Style | TextStyle.Bold };
        case MarkdownStyleRole.Emphasis:
            return normal with { Style = normal.Style | TextStyle.Italic };
        case MarkdownStyleRole.Strong:
            return normal with { Style = normal.Style | TextStyle.Bold };
        case MarkdownStyleRole.InlineCode:
        case MarkdownStyleRole.CodeBlock:
            return GetAttributeForRole (VisualRole.HotNormal);
        case MarkdownStyleRole.Link:
            return normal with { Style = normal.Style | TextStyle.Underline };
        case MarkdownStyleRole.Quote:
            return normal with { Style = normal.Style | TextStyle.Dim };
        case MarkdownStyleRole.Table:
            return normal with { Style = normal.Style | TextStyle.Bold };
        case MarkdownStyleRole.ThematicBreak:
            return normal with { Style = normal.Style | TextStyle.Dim };
        case MarkdownStyleRole.ImageAlt:
            return normal with { Style = normal.Style | TextStyle.Italic };
        case MarkdownStyleRole.TaskDone:
            return normal with { Style = normal.Style | TextStyle.Strikethrough };
        case MarkdownStyleRole.TaskTodo:
            return normal with { Style = normal.Style | TextStyle.Bold };
        case MarkdownStyleRole.ListMarker:
            return HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal);
        default:
            return normal;
        }
    }

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

        string queueId = $"{imageSource}:{screenPosition.X}:{screenPosition.Y}";

        if (!_queuedSixelIds.Add (queueId))
        {
            return;
        }

        Driver.GetSixels ().Enqueue (new SixelToRender { Id = queueId, ScreenPosition = ContentToScreen (screenPosition), SixelData = sixelData });
    }
}
