using System.Text.RegularExpressions;
using Markdig;

namespace Terminal.Gui.Views;

public partial class MarkdownView
{
    private static readonly MarkdownPipeline _defaultPipeline = new MarkdownPipelineBuilder ().UseAdvancedExtensions ().Build ();

    private static readonly Regex _headingPattern = new ("^(#{1,6})\\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex _unorderedListPattern = new ("^\\s*[-*+]\\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex _orderedListPattern = new ("^\\s*\\d+\\.\\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex _taskPattern = new ("^\\[(?<state>[ xX])\\]\\s+(?<text>.*)$", RegexOptions.Compiled);

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
        List<string> codeLines = [];
        Dictionary<string, int> slugCounts = new (StringComparer.OrdinalIgnoreCase);

        foreach (string line in lines)
        {
            if (IsFenceDelimiter (line))
            {
                if (!inCodeFence)
                {
                    inCodeFence = true;
                    codeLines.Clear ();

                    continue;
                }

                AddCodeBlockLines (codeLines);
                inCodeFence = false;

                continue;
            }

            if (inCodeFence)
            {
                codeLines.Add (line);

                continue;
            }

            if (string.IsNullOrWhiteSpace (line))
            {
                _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.Normal)], true));

                continue;
            }

            Match headingMatch = _headingPattern.Match (line);

            if (headingMatch.Success)
            {
                string headingText = headingMatch.Groups [2].Value;
                List<InlineRun> headingRuns = ParseInlines (headingText, MarkdownStyleRole.Heading);

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
                List<InlineRun> quoteRuns = ParseInlines (quoteText, MarkdownStyleRole.Quote);
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

            if (LooksLikeTableRow (line))
            {
                _blocks.Add (new IntermediateBlock ([new InlineRun (NormalizeTableRow (line), MarkdownStyleRole.Table)], false));

                continue;
            }

            List<InlineRun> paragraphRuns = ParseInlines (line, MarkdownStyleRole.Normal);
            _blocks.Add (new IntermediateBlock (paragraphRuns, true));
        }

        if (inCodeFence)
        {
            AddCodeBlockLines (codeLines);
        }
    }

    private void AddListLine (string listText, string marker)
    {
        Match task = _taskPattern.Match (listText);

        if (!task.Success)
        {
            List<InlineRun> runs = ParseInlines (listText, MarkdownStyleRole.Normal);
            _blocks.Add (new IntermediateBlock (runs, true, marker, new string (' ', marker.Length)));

            return;
        }

        bool done = task.Groups ["state"].Value.Equals ("x", StringComparison.OrdinalIgnoreCase);
        MarkdownStyleRole role = done ? MarkdownStyleRole.TaskDone : MarkdownStyleRole.TaskTodo;
        string text = task.Groups ["text"].Value;

        List<InlineRun> taskRuns = ParseInlines (text, role);
        _blocks.Add (new IntermediateBlock (taskRuns, true, $"{marker}[{(done ? "x" : " ")}] ", new string (' ', marker.Length + 4)));
    }

    private static bool IsFenceDelimiter (string line)
    {
        string trimmed = line.Trim ();

        return trimmed.StartsWith ("```", StringComparison.Ordinal) || trimmed.StartsWith ("~~~", StringComparison.Ordinal);
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

    private static string NormalizeTableRow (string line)
    {
        string trimmed = line.Trim ();
        string [] cells = trimmed.Trim ('|').Split ('|', StringSplitOptions.TrimEntries);

        return $"| {string.Join (" | ", cells)} |";
    }

    private void AddCodeBlockLines (IReadOnlyList<string> codeLines)
    {
        if (codeLines.Count == 0)
        {
            _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.CodeBlock)], false, isCodeBlock: true));

            return;
        }

        foreach (string line in codeLines)
        {
            IReadOnlyList<InlineRun> runs;

            if (SyntaxHighlighter is null)
            {
                runs = [new InlineRun (line, MarkdownStyleRole.CodeBlock)];
            }
            else
            {
                IReadOnlyList<StyledSegment> highlighted = SyntaxHighlighter.Highlight (line, null);
                List<InlineRun> converted = [];

                foreach (StyledSegment segment in highlighted)
                {
                    converted.Add (new InlineRun (segment.Text, segment.StyleRole, segment.Url, segment.ImageSource));
                }

                runs = converted;
            }

            _blocks.Add (new IntermediateBlock (runs, false, isCodeBlock: true));
        }
    }

    private List<InlineRun> ParseInlines (string text, MarkdownStyleRole defaultRole)
    {
        List<InlineRun> runs = [];
        var idx = 0;

        while (idx < text.Length)
        {
            if (TryParseImage (text, idx, out InlineRun? imageRun, out int imageLen))
            {
                runs.Add (imageRun!);
                idx += imageLen;

                continue;
            }

            if (TryParseLink (text, idx, out InlineRun? linkRun, out int linkLen))
            {
                runs.Add (linkRun!);
                idx += linkLen;

                continue;
            }

            if (TryParseDelimited (text, idx, "`", MarkdownStyleRole.InlineCode, out InlineRun? codeRun, out int codeLen))
            {
                runs.Add (codeRun!);
                idx += codeLen;

                continue;
            }

            if (TryParseDelimited (text, idx, "**", MarkdownStyleRole.Strong, out InlineRun? strongRun, out int strongLen))
            {
                runs.Add (strongRun!);
                idx += strongLen;

                continue;
            }

            if (TryParseDelimited (text, idx, "*", MarkdownStyleRole.Emphasis, out InlineRun? emRun, out int emLen))
            {
                runs.Add (emRun!);
                idx += emLen;

                continue;
            }

            int nextSpecial = FindNextSpecialToken (text, idx);

            // If the next special token is at the current position, no TryParse could consume it.
            // Emit the character as plain text and advance past it to avoid an infinite loop.
            if (nextSpecial == idx)
            {
                runs.Add (new InlineRun (text [idx].ToString (), defaultRole));
                idx++;

                continue;
            }

            string plainText = nextSpecial == -1 ? text [idx..] : text.Substring (idx, nextSpecial - idx);

            runs.Add (new InlineRun (plainText, defaultRole));

            if (nextSpecial == -1)
            {
                break;
            }

            idx = nextSpecial;
        }

        return runs;
    }

    private static bool TryParseDelimited (string text, int start, string delimiter, MarkdownStyleRole role, out InlineRun? run, out int tokenLength)
    {
        run = null;
        tokenLength = 0;

        if (!text.AsSpan (start).StartsWith (delimiter.AsSpan (), StringComparison.Ordinal))
        {
            return false;
        }

        int end = text.IndexOf (delimiter, start + delimiter.Length, StringComparison.Ordinal);

        if (end <= start + delimiter.Length)
        {
            return false;
        }

        string content = text.Substring (start + delimiter.Length, end - start - delimiter.Length);
        run = new InlineRun (content, role);
        tokenLength = end - start + delimiter.Length;

        return true;
    }

    private static bool TryParseLink (string text, int start, out InlineRun? run, out int tokenLength)
    {
        run = null;
        tokenLength = 0;

        if (start >= text.Length || text [start] != '[')
        {
            return false;
        }

        int closeText = text.IndexOf (']', start + 1);

        if (closeText < 0 || closeText + 1 >= text.Length || text [closeText + 1] != '(')
        {
            return false;
        }

        int closeUrl = text.IndexOf (')', closeText + 2);

        if (closeUrl < 0)
        {
            return false;
        }

        string linkText = text.Substring (start + 1, closeText - start - 1);
        string linkUrl = text.Substring (closeText + 2, closeUrl - closeText - 2);

        run = new InlineRun (linkText, MarkdownStyleRole.Link, linkUrl);
        tokenLength = closeUrl - start + 1;

        return true;
    }

    private static bool TryParseImage (string text, int start, out InlineRun? run, out int tokenLength)
    {
        run = null;
        tokenLength = 0;

        if (start + 1 >= text.Length || text [start] != '!' || text [start + 1] != '[')
        {
            return false;
        }

        int closeAlt = text.IndexOf (']', start + 2);

        if (closeAlt < 0 || closeAlt + 1 >= text.Length || text [closeAlt + 1] != '(')
        {
            return false;
        }

        int closeSrc = text.IndexOf (')', closeAlt + 2);

        if (closeSrc < 0)
        {
            return false;
        }

        string alt = text.Substring (start + 2, closeAlt - start - 2);
        string source = text.Substring (closeAlt + 2, closeSrc - closeAlt - 2);

        run = new InlineRun (MarkdownImageResolver.GetFallbackText (alt), MarkdownStyleRole.ImageAlt, imageSource: source);
        tokenLength = closeSrc - start + 1;

        return true;
    }

    private static int FindNextSpecialToken (string text, int start)
    {
        int [] indexes = [text.IndexOf ('!', start), text.IndexOf ('[', start), text.IndexOf ('`', start), text.IndexOf ('*', start)];

        int next = -1;

        foreach (int idx in indexes)
        {
            if (idx < 0)
            {
                continue;
            }

            if (next == -1 || idx < next)
            {
                next = idx;
            }
        }

        return next;
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
