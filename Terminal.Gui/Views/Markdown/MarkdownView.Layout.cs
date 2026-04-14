namespace Terminal.Gui.Views;

public partial class MarkdownView
{
    private void BuildRenderedLines ()
    {
        _renderedLines.Clear ();
        _maxLineWidth = 0;

        int viewportWidth = Math.Max (Viewport.Width, MIN_WRAP_WIDTH);

        foreach (IntermediateBlock block in _blocks)
        {
            if (!block.Wrap)
            {
                RenderedLine unwrapped = CreateUnwrappedLine (block);
                _renderedLines.Add (unwrapped);
                _maxLineWidth = Math.Max (_maxLineWidth, unwrapped.Width);
                continue;
            }

            List<RenderedLine> wrapped = WrapBlock (block, viewportWidth);

            foreach (RenderedLine line in wrapped)
            {
                _renderedLines.Add (line);
                _maxLineWidth = Math.Max (_maxLineWidth, line.Width);
            }
        }

        if (_renderedLines.Count == 0)
        {
            _renderedLines.Add (new RenderedLine ([new StyledSegment ("", MarkdownStyleRole.Normal)], true, 0));
        }
    }

    private static RenderedLine CreateUnwrappedLine (IntermediateBlock block)
    {
        List<StyledSegment> segments = [];

        if (!string.IsNullOrEmpty (block.Prefix))
        {
            segments.Add (new StyledSegment (block.Prefix, MarkdownStyleRole.ListMarker));
        }

        foreach (InlineRun run in block.Runs)
        {
            segments.Add (new StyledSegment (run.Text, run.StyleRole, run.Url, run.ImageSource));
        }

        int width = CalculateWidth (segments);

        return new RenderedLine (segments, false, width);
    }

    private static List<RenderedLine> WrapBlock (IntermediateBlock block, int viewportWidth)
    {
        List<RenderedLine> lines = [];

        string firstPrefix = block.Prefix;
        string continuationPrefix = string.IsNullOrEmpty (block.ContinuationPrefix) ? firstPrefix : block.ContinuationPrefix;

        List<StyledSegment> currentSegments = [];
        int currentWidth = 0;
        bool firstLine = true;

        if (!string.IsNullOrEmpty (firstPrefix))
        {
            currentSegments.Add (new StyledSegment (firstPrefix, MarkdownStyleRole.ListMarker));
            currentWidth = firstPrefix.GetColumns ();
        }

        foreach (InlineRun run in block.Runs)
        {
            foreach (string grapheme in GraphemeHelper.GetGraphemes (run.Text))
            {
                int graphemeWidth = Math.Max (grapheme.GetColumns (), 1);

                if (currentWidth + graphemeWidth > viewportWidth && currentSegments.Count > 0)
                {
                    // Find last whitespace for word-boundary wrap (skip prefix segments)
                    int breakIdx = -1;

                    for (int s = currentSegments.Count - 1; s >= 0; s--)
                    {
                        if (currentSegments [s].StyleRole == MarkdownStyleRole.ListMarker)
                        {
                            break;
                        }

                        if (string.IsNullOrWhiteSpace (currentSegments [s].Text))
                        {
                            breakIdx = s;

                            break;
                        }
                    }

                    if (breakIdx >= 0)
                    {
                        // Word wrap: emit up to and including the space
                        List<StyledSegment> lineSegments = currentSegments.GetRange (0, breakIdx + 1);
                        List<StyledSegment> overflow = currentSegments.GetRange (breakIdx + 1, currentSegments.Count - breakIdx - 1);

                        lines.Add (new RenderedLine ([.. lineSegments], true, CalculateWidth (lineSegments)));

                        currentSegments = [.. overflow];
                        currentWidth = CalculateWidth (currentSegments);
                    }
                    else
                    {
                        // No word boundary found — hard break at current position
                        lines.Add (new RenderedLine ([.. currentSegments], true, currentWidth));
                        currentSegments.Clear ();
                        currentWidth = 0;
                    }

                    firstLine = false;

                    if (!string.IsNullOrEmpty (continuationPrefix))
                    {
                        currentSegments.Insert (0, new StyledSegment (continuationPrefix, MarkdownStyleRole.ListMarker));
                        currentWidth += continuationPrefix.GetColumns ();
                    }
                }

                currentSegments.Add (new StyledSegment (grapheme, run.StyleRole, run.Url, run.ImageSource));
                currentWidth += graphemeWidth;

            }
        }

        if (currentSegments.Count == 0)
        {
            string prefix = firstLine ? firstPrefix : continuationPrefix;
            List<StyledSegment> emptySegments = string.IsNullOrEmpty (prefix)
                ? [new StyledSegment ("", MarkdownStyleRole.Normal)]
                : [new StyledSegment (prefix, MarkdownStyleRole.ListMarker)];
            int width = string.IsNullOrEmpty (prefix) ? 0 : prefix.GetColumns ();
            lines.Add (new RenderedLine (emptySegments, true, width));

            return lines;
        }

        lines.Add (new RenderedLine ([.. currentSegments], true, currentWidth));

        return lines;
    }

    private static int CalculateWidth (IReadOnlyList<StyledSegment> segments)
    {
        int width = 0;

        foreach (StyledSegment segment in segments)
        {
            foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
            {
                width += Math.Max (grapheme.GetColumns (), 1);
            }
        }

        return width;
    }
}
