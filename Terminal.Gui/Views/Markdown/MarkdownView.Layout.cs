namespace Terminal.Gui.Views;

public partial class MarkdownView
{
    private void BuildRenderedLines ()
    {
        _renderedLines.Clear ();
        _headingAnchors.Clear ();
        _codeBlockRegions.Clear ();
        _maxLineWidth = 0;

        int viewportWidth = Math.Max (Viewport.Width, MIN_WRAP_WIDTH);

        foreach (IntermediateBlock block in _blocks)
        {
            // Record heading anchor → rendered-line index before adding lines
            if (!string.IsNullOrEmpty (block.Anchor))
            {
                _headingAnchors [block.Anchor!] = _renderedLines.Count;
            }

            // Thematic breaks get a Line SubView
            if (block.IsThematicBreak)
            {
                int lineY = _renderedLines.Count;

                Line lineView = new ()
                {
                    X = 0,
                    Y = lineY,
                    Width = Dim.Fill (),
                    Height = 1,
                    CanFocus = false,
                };

                _thematicBreakViews.Add (lineView);
                Add (lineView);

                // Reserve a placeholder line
                _renderedLines.Add (new RenderedLine ([new StyledSegment ("", MarkdownStyleRole.ThematicBreak)], false, 0, isThematicBreak: true));

                continue;
            }

            // Table blocks get a MarkdownTable SubView and placeholder lines
            if (block is { IsTable: true, TableData: { } tableData })
            {
                int startLine = _renderedLines.Count;

                MarkdownTable tableView = new (tableData, viewportWidth)
                {
                    X = 0,
                    Y = startLine,
                    Width = Dim.Fill (),
                };

                _tableViews.Add (tableView);
                Add (tableView);

                // Use actual table height (accounts for word-wrapped rows)
                int tableHeight = tableView.Frame.Height;

                // Reserve placeholder lines so content height is correct
                for (var i = 0; i < tableHeight; i++)
                {
                    _renderedLines.Add (new RenderedLine ([new StyledSegment ("", MarkdownStyleRole.Table)], false, 0, isTable: true));
                }

                continue;
            }

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

        BuildCodeBlockRegions ();
        SyncCopyButtons ();
    }

    private void BuildCodeBlockRegions ()
    {
        var i = 0;

        while (i < _renderedLines.Count)
        {
            if (!_renderedLines [i].IsCodeBlock)
            {
                i++;

                continue;
            }

            int start = i;

            while (i < _renderedLines.Count && _renderedLines [i].IsCodeBlock)
            {
                i++;
            }

            _codeBlockRegions.Add (new CodeBlockRegion (start, i));
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

        return new RenderedLine (segments, false, width, block.IsCodeBlock, block.IsThematicBreak);
    }

    private static List<RenderedLine> WrapBlock (IntermediateBlock block, int viewportWidth)
    {
        List<RenderedLine> lines = [];

        string firstPrefix = block.Prefix;
        string continuationPrefix = string.IsNullOrEmpty (block.ContinuationPrefix) ? firstPrefix : block.ContinuationPrefix;

        List<StyledSegment> currentSegments = [];
        var currentWidth = 0;
        var firstLine = true;

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
                    // Find last whitespace for word-boundary wrap (skip prefix segments).
                    // Avoid breaking inside parentheses — for each candidate space, verify
                    // it is not inside unclosed parens by forward-scanning from the start.
                    int breakIdx = -1;

                    for (int s = currentSegments.Count - 1; s >= 0; s--)
                    {
                        if (currentSegments [s].StyleRole == MarkdownStyleRole.ListMarker)
                        {
                            break;
                        }

                        if (!string.IsNullOrWhiteSpace (currentSegments [s].Text))
                        {
                            continue;
                        }

                        // Check paren depth at this position via forward scan
                        var depth = 0;

                        for (var j = 0; j <= s; j++)
                        {
                            if (currentSegments [j].Text == "(")
                            {
                                depth++;
                            }
                            else if (currentSegments [j].Text == ")")
                            {
                                depth--;
                            }
                        }

                        if (depth > 0)
                        {
                            continue;
                        }

                        breakIdx = s;

                        break;
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
        var width = 0;

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
