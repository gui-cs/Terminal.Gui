using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Terminal.Gui.Views;

public partial class Markdown
{
    private static readonly MarkdownPipeline _defaultPipeline = new MarkdownPipelineBuilder ().UseAdvancedExtensions ().Build ();

    private Dictionary<string, int> _slugCounts = new (StringComparer.OrdinalIgnoreCase);

    private void EnsureParsed ()
    {
        if (_parsed)
        {
            return;
        }

        _blocks.Clear ();

        MarkdownPipeline pipeline = MarkdownPipeline ?? _defaultPipeline;
        MarkdownDocument doc = Markdig.Markdown.Parse (_markdown, pipeline);

        LowerFromAst (doc);

        _parsed = true;
    }

    private void LowerFromAst (MarkdownDocument doc)
    {
        _slugCounts = new Dictionary<string, int> (StringComparer.OrdinalIgnoreCase);

        Block? prevBlock = null;

        foreach (Block block in doc)
        {
            if (block is LinkReferenceDefinitionGroup)
            {
                prevBlock = block;

                continue;
            }

            if (prevBlock is not null and not LinkReferenceDefinitionGroup)
            {
                int prevEndLine = GetBlockEndLine (prevBlock);
                int thisStartLine = block.Line;

                if (thisStartLine > prevEndLine + 1)
                {
                    _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.Normal)], true));
                }
            }

            WalkBlock (block, string.Empty, string.Empty, MarkdownStyleRole.Normal);
            prevBlock = block;
        }
    }

    private void WalkBlock (Block block, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        switch (block)
        {
            case HeadingBlock heading:
                HandleHeadingBlock (heading, prefix);

                break;

            case ParagraphBlock para:
                HandleParagraphBlock (para, prefix, contPrefix, defaultRole);

                break;

            case FencedCodeBlock:
            case CodeBlock:
                HandleCodeBlockNode ((LeafBlock)block);

                break;

            case ThematicBreakBlock:
                _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.ThematicBreak)], false, isThematicBreak: true));

                break;

            case QuoteBlock quote:
                foreach (Block child in quote)
                {
                    WalkBlock (child, prefix + "> ", contPrefix + "> ", MarkdownStyleRole.Quote);
                }

                break;

            case ListBlock list:
                HandleListBlock (list, prefix, contPrefix, defaultRole);

                break;

            case Table table:
                HandleTableBlock (table);

                break;

            case HtmlBlock html:
                HandleHtmlBlock (html, prefix, contPrefix, defaultRole);

                break;

            case LinkReferenceDefinitionGroup:
                // Consumed by Markdig during inline resolution; nothing to render.
                break;

            default:
                HandleUnknownBlock (block, prefix, contPrefix, defaultRole);

                break;
        }
    }

    private void HandleHeadingBlock (HeadingBlock heading, string prefix)
    {
        List<InlineRun> runs = WalkInlines (heading.Inline?.FirstChild, MarkdownStyleRole.Heading);

        // Compute anchor slug from heading text before inserting the marker
        string headingText = string.Concat (runs.Select (r => r.Text));
        string anchor = DeduplicateSlug (GenerateAnchorSlug (headingText), _slugCounts);

        if (ShowHeadingPrefix)
        {
            string hashes = new string ('#', heading.Level);
            runs.Insert (0, new InlineRun ($"{prefix}{hashes} ", MarkdownStyleRole.HeadingMarker));
        }
        else if (!string.IsNullOrEmpty (prefix))
        {
            runs.Insert (0, new InlineRun (prefix, MarkdownStyleRole.HeadingMarker));
        }

        _blocks.Add (new IntermediateBlock (runs, true, anchor: anchor));
    }

    private void HandleParagraphBlock (ParagraphBlock para, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        List<InlineRun> runs = WalkInlines (para.Inline?.FirstChild, defaultRole);
        _blocks.Add (new IntermediateBlock (runs, true, prefix, contPrefix));
    }

    private void HandleCodeBlockNode (LeafBlock codeBlock)
    {
        string? language = (codeBlock as FencedCodeBlock)?.Info;

        // Markdig returns empty string for fences with no language; normalize to null
        if (string.IsNullOrEmpty (language))
        {
            language = null;
        }

        List<string> lines = [];

        foreach (StringLine line in codeBlock.Lines)
        {
            lines.Add (line.Slice.ToString ());
        }

        AddCodeBlockLines (lines, language);
    }

    private void HandleListBlock (ListBlock list, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        foreach (Block item in list)
        {
            if (item is not ListItemBlock listItem)
            {
                continue;
            }

            string marker = list.IsOrdered ? $"{listItem.Order}. " : "• ";
            string itemPrefix = prefix + marker;
            string itemCont = contPrefix + new string (' ', marker.Length);

            bool isFirst = true;

            foreach (Block child in listItem)
            {
                if (isFirst && child is ParagraphBlock para)
                {
                    Inline? firstInline = para.Inline?.FirstChild;

                    if (firstInline is TaskList tl)
                    {
                        bool done = tl.Checked;
                        MarkdownStyleRole role = done ? MarkdownStyleRole.TaskDone : MarkdownStyleRole.TaskTodo;
                        string checkbox = done ? "[x] " : "[ ] ";
                        string taskPrefix = itemPrefix + checkbox;
                        string taskCont = contPrefix + new string (' ', marker.Length + 4);

                        List<InlineRun> runs = WalkInlines (firstInline.NextSibling, role);
                        TrimLeadingSpace (runs);
                        _blocks.Add (new IntermediateBlock (runs, true, taskPrefix, taskCont));
                    }
                    else
                    {
                        List<InlineRun> runs = WalkInlines (firstInline, defaultRole);
                        _blocks.Add (new IntermediateBlock (runs, true, itemPrefix, itemCont));
                    }

                    isFirst = false;
                }
                else
                {
                    WalkBlock (child, isFirst ? itemPrefix : itemCont, itemCont, defaultRole);
                    isFirst = false;
                }
            }
        }
    }

    private void HandleTableBlock (Table table)
    {
        List<string> headers = [];
        Alignment [] alignments = [];
        List<string []> rows = [];
        bool headerParsed = false;

        foreach (Block tableRow in table)
        {
            if (tableRow is not TableRow row)
            {
                continue;
            }

            string [] cells = row.Select (cell => ExtractCellText ((TableCell)cell)).ToArray ();

            if (!headerParsed)
            {
                headers.AddRange (cells);
                alignments = table.ColumnDefinitions
                                  .Take (headers.Count)
                                  .Select (col => col.Alignment switch
                                                  {
                                                      TableColumnAlign.Center => Alignment.Center,
                                                      TableColumnAlign.Right => Alignment.End,
                                                      _ => Alignment.Start
                                                  })
                                  .ToArray ();
                headerParsed = true;
            }
            else if (!row.IsHeader)
            {
                rows.Add (cells);
            }
        }

        if (headers.Count == 0)
        {
            return;
        }

        // Pad alignments if there are more headers than alignment definitions
        if (alignments.Length < headers.Count)
        {
            Alignment [] padded = new Alignment [headers.Count];
            alignments.CopyTo (padded, 0);
            alignments = padded;
        }

        TableData tableData = new (headers.ToArray (), alignments, rows.ToArray ());
        _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.Table)], false, tableData: tableData));
    }

    private static string ExtractCellText (TableCell cell)
    {
        // Option A: reconstruct raw markdown-like text from the AST so MarkdownTable
        // can re-parse inline formatting with its existing MarkdownInlineParser path.
        System.Text.StringBuilder sb = new ();

        foreach (Block child in cell)
        {
            if (child is ParagraphBlock para)
            {
                AppendInlineText (para.Inline?.FirstChild, sb);
            }
        }

        return sb.ToString ().Trim ();
    }

    private static void AppendInlineText (Inline? inline, System.Text.StringBuilder sb)
    {
        while (inline is not null)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    sb.Append (lit.Content.ToString ());

                    break;

                case EmphasisInline em:
                    string delim = new string (em.DelimiterChar, em.DelimiterCount);
                    sb.Append (delim);
                    AppendInlineText (em.FirstChild, sb);
                    sb.Append (delim);

                    break;

                case CodeInline code:
                    sb.Append ('`');
                    sb.Append (code.Content);
                    sb.Append ('`');

                    break;

                case LinkInline link when link.IsImage:
                    sb.Append ("![");
                    AppendInlineText (link.FirstChild, sb);
                    sb.Append ("](");
                    sb.Append (link.Url);
                    sb.Append (')');

                    break;

                case LinkInline link:
                    sb.Append ('[');
                    AppendInlineText (link.FirstChild, sb);
                    sb.Append ("](");
                    sb.Append (link.Url);
                    sb.Append (')');

                    break;

                case HtmlEntityInline entity:
                    sb.Append (entity.Transcoded.ToString ());

                    break;

                case AutolinkInline auto:
                    sb.Append (auto.Url);

                    break;

                case ContainerInline container:
                    AppendInlineText (container.FirstChild, sb);

                    break;
            }

            inline = inline.NextSibling;
        }
    }

    private void HandleHtmlBlock (HtmlBlock html, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        foreach (StringLine line in html.Lines)
        {
            string text = line.Slice.ToString ().Trim ();

            if (string.IsNullOrEmpty (text))
            {
                continue;
            }

            // Strip HTML tags for plain-text terminal rendering
            string stripped = Regex.Replace (text, "<[^>]+>", string.Empty, RegexOptions.None).Trim ();

            if (string.IsNullOrEmpty (stripped))
            {
                continue;
            }

            _blocks.Add (new IntermediateBlock ([new InlineRun (stripped, defaultRole)], true, prefix, contPrefix));
        }
    }

    private void HandleUnknownBlock (Block block, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        // For unrecognized block types: extract text from leaf blocks or recurse containers.
        if (block is LeafBlock leaf && leaf.Lines.Count > 0)
        {
            System.Text.StringBuilder sb = new ();

            foreach (StringLine line in leaf.Lines)
            {
                if (sb.Length > 0)
                {
                    sb.Append (' ');
                }

                sb.Append (line.Slice.ToString ());
            }

            string text = sb.ToString ().Trim ();

            if (!string.IsNullOrEmpty (text))
            {
                _blocks.Add (new IntermediateBlock ([new InlineRun (text, defaultRole)], true, prefix, contPrefix));
            }
        }
        else if (block is ContainerBlock container)
        {
            foreach (Block child in container)
            {
                WalkBlock (child, prefix, contPrefix, defaultRole);
            }
        }
    }

    private static List<InlineRun> WalkInlines (Inline? inline, MarkdownStyleRole defaultRole)
    {
        List<InlineRun> runs = [];

        while (inline is not null)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    string litText = lit.Content.ToString ();

                    if (!string.IsNullOrEmpty (litText))
                    {
                        runs.Add (new InlineRun (litText, defaultRole));
                    }

                    break;

                case EmphasisInline em:
                    MarkdownStyleRole emRole = em.DelimiterChar == '~' && em.DelimiterCount >= 2
                                                  ? MarkdownStyleRole.Strikethrough
                                                  : em.DelimiterCount >= 2
                                                      ? MarkdownStyleRole.Strong
                                                      : MarkdownStyleRole.Emphasis;
                    runs.AddRange (WalkInlines (em.FirstChild, emRole));

                    break;

                case CodeInline code:
                    runs.Add (new InlineRun (code.Content, MarkdownStyleRole.InlineCode));

                    break;

                case LinkInline link when link.IsImage:
                    List<InlineRun> altRuns = WalkInlines (link.FirstChild, MarkdownStyleRole.ImageAlt);
                    string altText = string.Concat (altRuns.Select (r => r.Text));
                    string fallback = MarkdownImageResolver.GetFallbackText (altText);
                    runs.Add (new InlineRun (fallback, MarkdownStyleRole.ImageAlt, imageSource: link.Url));

                    break;

                case LinkInline link:
                    string? url = link.Url;
                    List<InlineRun> linkRuns = WalkInlines (link.FirstChild, MarkdownStyleRole.Link);
                    runs.AddRange (linkRuns.Select (r => new InlineRun (r.Text, MarkdownStyleRole.Link, url, r.ImageSource, r.Attribute)));

                    break;

                case AutolinkInline auto:
                    runs.Add (new InlineRun (auto.Url, MarkdownStyleRole.Link, auto.Url));

                    break;

                case LineBreakInline:
                    runs.Add (new InlineRun (" ", defaultRole));

                    break;

                case HtmlEntityInline entity:
                    string entityText = entity.Transcoded.ToString ();

                    if (!string.IsNullOrEmpty (entityText))
                    {
                        runs.Add (new InlineRun (entityText, defaultRole));
                    }

                    break;

                case HtmlInline:
                    // Inline HTML — skip in terminal context.
                    break;

                default:
                    // For unrecognized container inline types (e.g. PipeTableDelimiterInline),
                    // recurse into children to extract any text content.
                    if (inline is ContainerInline unknownContainer)
                    {
                        runs.AddRange (WalkInlines (unknownContainer.FirstChild, defaultRole));
                    }

                    break;
            }

            inline = inline.NextSibling;
        }

        return runs;
    }

    private static void TrimLeadingSpace (List<InlineRun> runs)
    {
        if (runs.Count == 0)
        {
            return;
        }

        string trimmed = runs [0].Text.TrimStart ();

        if (trimmed == runs [0].Text)
        {
            return;
        }

        InlineRun first = runs [0];
        runs [0] = new InlineRun (trimmed, first.StyleRole, first.Url, first.ImageSource, first.Attribute);

        if (runs [0].Text.Length == 0)
        {
            runs.RemoveAt (0);
        }
    }

    private static int GetBlockEndLine (Block block)
    {
        return block switch
               {
                   FencedCodeBlock fcb => fcb.Line + fcb.Lines.Count + 1, // opening fence + content + closing fence
                   CodeBlock cb => cb.Line + Math.Max (cb.Lines.Count - 1, 0),
                   QuoteBlock qb => qb.Count > 0 ? GetBlockEndLine (qb [qb.Count - 1]) : qb.Line,
                   ListBlock lb => lb.Count > 0 ? GetBlockEndLine (lb [lb.Count - 1]) : lb.Line,
                   ListItemBlock lib => lib.Count > 0 ? GetBlockEndLine (lib [lib.Count - 1]) : lib.Line,
                   Table t => t.Count > 0 ? GetBlockEndLine (t [t.Count - 1]) : t.Line,
                   _ => block.Line
               };
    }

    private void AddCodeBlockLines (IReadOnlyList<string> codeLines, string? language)
    {
        if (codeLines.Count == 0)
        {
            _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.CodeBlock)], false, isCodeBlock: true));

            return;
        }

        SyntaxHighlighter?.ResetState ();

        foreach (string line in codeLines)
        {
            IReadOnlyList<InlineRun> runs;

            if (SyntaxHighlighter is null)
            {
                runs = [new InlineRun (line, MarkdownStyleRole.CodeBlock)];
            }
            else
            {
                IReadOnlyList<StyledSegment> highlighted = SyntaxHighlighter.Highlight (line, language);
                List<InlineRun> converted = [];
                converted.AddRange (highlighted.Select (segment => new InlineRun (segment.Text, segment.StyleRole, segment.Url, segment.ImageSource, segment.Attribute)));

                runs = converted;
            }

            _blocks.Add (new IntermediateBlock (runs, false, isCodeBlock: true));
        }
    }

    private static string DeduplicateSlug (string baseSlug, Dictionary<string, int> slugCounts)
    {
        if (!slugCounts.TryGetValue (baseSlug, out int count))
        {
            slugCounts [baseSlug] = 1;

            return baseSlug;
        }

        slugCounts [baseSlug] = count + 1;
        var deduped = $"{baseSlug}-{count}";

        // Ensure the deduped slug itself is tracked
        slugCounts [deduped] = 1;

        return deduped;
    }
}
