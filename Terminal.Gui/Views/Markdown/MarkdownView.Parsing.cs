using System.Text.RegularExpressions;
using Markdig;

namespace Terminal.Gui.Views;

public partial class Markdown
{
    private static readonly MarkdownPipeline _defaultPipeline = new MarkdownPipelineBuilder ().UseAdvancedExtensions ().Build ();

    private static readonly Regex _headingPattern = new Regex ("^(#{1,6})\\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex _unorderedListPattern = new Regex ("^\\s*[-*+]\\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex _orderedListPattern = new Regex ("^\\s*\\d+\\.\\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex _taskPattern = new Regex ("^\\[(?<state>[ xX])\\]\\s+(?<text>.*)$", RegexOptions.Compiled);

    private void EnsureParsed ()
    {
        if (_parsed)
        {
            return;
        }

        _blocks.Clear ();

        MarkdownPipeline pipeline = MarkdownPipeline ?? _defaultPipeline;

        // Keep parse stage explicit (parse -> lower -> layout -> draw); parsed AST is intentionally unused in v1 lowering.
        _ = Markdig.Markdown.Parse (_markdown, pipeline);

        LowerFromSourceText ();

        _parsed = true;
    }

    private void LowerFromSourceText ()
    {
        string normalized = _markdown.Replace ("\r\n", "\n");
        string [] lines = normalized.Split ('\n');

        var inCodeFence = false;
        string? fenceLanguage = null;
        List<string> codeLines = [];
        List<string> tableLines = [];
        Dictionary<string, int> slugCounts = new (StringComparer.OrdinalIgnoreCase);

        foreach (string line in lines)
        {
            if (IsFenceDelimiter (line))
            {
                FlushTableLines (tableLines);

                if (!inCodeFence)
                {
                    inCodeFence = true;
                    codeLines.Clear ();
                    fenceLanguage = ExtractFenceLanguage (line);

                    continue;
                }

                AddCodeBlockLines (codeLines, fenceLanguage);
                inCodeFence = false;
                fenceLanguage = null;

                continue;
            }

            if (inCodeFence)
            {
                codeLines.Add (line);

                continue;
            }

            // Accumulate consecutive table rows
            if (LooksLikeTableRow (line))
            {
                tableLines.Add (line);

                continue;
            }

            // Non-table line encountered — flush any accumulated table rows
            FlushTableLines (tableLines);

            if (string.IsNullOrWhiteSpace (line))
            {
                _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.Normal)], true));

                continue;
            }

            Match headingMatch = _headingPattern.Match (line);

            if (headingMatch.Success)
            {
                string hashes = headingMatch.Groups [1].Value;
                string headingText = headingMatch.Groups [2].Value;
                List<InlineRun> headingRuns = MarkdownInlineParser.ParseInlines (headingText, MarkdownStyleRole.Heading);

                if (ShowHeadingPrefix)
                {
                    headingRuns.Insert (0, new InlineRun ($"{hashes} ", MarkdownStyleRole.HeadingMarker));
                }

                string baseSlug = GenerateAnchorSlug (headingText);
                string anchor = DeduplicateSlug (baseSlug, slugCounts);
                _blocks.Add (new IntermediateBlock (headingRuns, true, anchor: anchor));

                continue;
            }

            if (IsThematicBreak (line))
            {
                // Thematic breaks are rendered via LineCanvas during drawing; no text content needed.
                _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.ThematicBreak)], false, isThematicBreak: true));

                continue;
            }

            if (line.TrimStart ().StartsWith ('>'))
            {
                string quoteText = line.TrimStart ().TrimStart ('>').TrimStart ();
                List<InlineRun> quoteRuns = MarkdownInlineParser.ParseInlines (quoteText, MarkdownStyleRole.Quote);
                _blocks.Add (new IntermediateBlock (quoteRuns, true, "> ", "> "));

                continue;
            }

            Match unordered = _unorderedListPattern.Match (line);

            if (unordered.Success)
            {
                AddListLine (unordered.Groups [1].Value, "• ");

                continue;
            }

            Match ordered = _orderedListPattern.Match (line);

            if (ordered.Success)
            {
                AddListLine (ordered.Groups [1].Value, "1. ");

                continue;
            }

            List<InlineRun> paragraphRuns = MarkdownInlineParser.ParseInlines (line, MarkdownStyleRole.Normal);
            _blocks.Add (new IntermediateBlock (paragraphRuns, true));
        }

        // Flush any remaining accumulated lines
        FlushTableLines (tableLines);

        if (inCodeFence)
        {
            AddCodeBlockLines (codeLines, fenceLanguage);
        }
    }

    private void FlushTableLines (List<string> tableLines)
    {
        if (tableLines.Count == 0)
        {
            return;
        }

        TableData? tableData = TableData.TryParse (tableLines);

        if (tableData is { })
        {
            _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.Table)], false, tableData: tableData));
        }
        else
        {
            // Not a valid table — emit as plain text lines
            foreach (string line in tableLines)
            {
                List<InlineRun> runs = MarkdownInlineParser.ParseInlines (line, MarkdownStyleRole.Normal);
                _blocks.Add (new IntermediateBlock (runs, true));
            }
        }

        tableLines.Clear ();
    }

    private void AddListLine (string listText, string marker)
    {
        Match task = _taskPattern.Match (listText);

        if (!task.Success)
        {
            List<InlineRun> runs = MarkdownInlineParser.ParseInlines (listText, MarkdownStyleRole.Normal);
            _blocks.Add (new IntermediateBlock (runs, true, marker, new string (' ', marker.Length)));

            return;
        }

        bool done = task.Groups ["state"].Value.Equals ("x", StringComparison.OrdinalIgnoreCase);
        MarkdownStyleRole role = done ? MarkdownStyleRole.TaskDone : MarkdownStyleRole.TaskTodo;
        string text = task.Groups ["text"].Value;

        List<InlineRun> taskRuns = MarkdownInlineParser.ParseInlines (text, role);
        _blocks.Add (new IntermediateBlock (taskRuns, true, $"{marker}[{(done ? "x" : " ")}] ", new string (' ', marker.Length + 4)));
    }

    private static bool IsFenceDelimiter (string line)
    {
        string trimmed = line.Trim ();

        return trimmed.StartsWith ("```", StringComparison.Ordinal) || trimmed.StartsWith ("~~~", StringComparison.Ordinal);
    }

    private static string? ExtractFenceLanguage (string line)
    {
        string trimmed = line.Trim ();

        // Skip the fence characters (``` or ~~~)
        char fenceChar = trimmed [0];
        var i = 0;

        while (i < trimmed.Length && trimmed [i] == fenceChar)
        {
            i++;
        }

        string language = trimmed [i..].Trim ();

        return string.IsNullOrEmpty (language) ? null : language;
    }

    private static bool IsThematicBreak (string line)
    {
        string trimmed = line.Trim ();

        if (trimmed.Length < 3)
        {
            return false;
        }

        return trimmed.All (c => c is '-' or '*' or '_');
    }

    private static bool LooksLikeTableRow (string line)
    {
        string trimmed = line.Trim ();

        return trimmed.StartsWith ('|') && trimmed.IndexOf ('|', 1) >= 0;
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
