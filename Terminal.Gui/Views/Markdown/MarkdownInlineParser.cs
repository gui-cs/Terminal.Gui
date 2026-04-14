namespace Terminal.Gui.Views;

/// <summary>
///     Stateless parser for inline Markdown formatting (bold, italic, code, links, images).
///     Used by both <see cref="MarkdownView"/> and <see cref="MarkdownTable"/> to parse
///     inline content from raw Markdown text.
/// </summary>
internal static class MarkdownInlineParser
{
    /// <summary>
    ///     Parses inline Markdown tokens from <paramref name="text"/> and returns a list of
    ///     <see cref="InlineRun"/> segments with their associated <see cref="MarkdownStyleRole"/>.
    /// </summary>
    /// <param name="text">The raw Markdown text to parse.</param>
    /// <param name="defaultRole">
    ///     The <see cref="MarkdownStyleRole"/> to assign to plain text segments that are not
    ///     wrapped in any Markdown formatting.
    /// </param>
    /// <returns>An ordered list of inline runs covering the entire input text.</returns>
    public static List<InlineRun> ParseInlines (string text, MarkdownStyleRole defaultRole)
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
}
