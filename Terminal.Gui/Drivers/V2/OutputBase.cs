namespace Terminal.Gui.Drivers;

public abstract class OutputBase
{
    private CursorVisibility? _cachedCursorVisibility;

    // Last text style used, for updating style with EscSeqUtils.CSI_AppendTextStyleChange().
    private TextStyle _redrawTextStyle = TextStyle.None;

    /// <inheritdoc/>
    public virtual void Write (IOutputBuffer buffer)
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        if (Console.WindowHeight < 1
            || buffer.Contents.Length != buffer.Rows * buffer.Cols
            || buffer.Rows != Console.WindowHeight)
        {
            //     return;
        }

        var top = 0;
        var left = 0;
        int rows = buffer.Rows;
        int cols = buffer.Cols;
        var output = new StringBuilder ();
        Attribute? redrawAttr = null;
        int lastCol = -1;

        CursorVisibility? savedVisibility = _cachedCursorVisibility;
        SetCursorVisibility (CursorVisibility.Invisible);

        const int maxCharsPerRune = 2;
        Span<char> runeBuffer = stackalloc char [maxCharsPerRune];

        for (int row = top; row < rows; row++)
        {
            if (Console.WindowHeight < 1)
            {
                return;
            }

            if (!SetCursorPositionImpl (0, row))
            {
                return;
            }

            output.Clear ();

            for (int col = left; col < cols; col++)
            {
                lastCol = -1;
                var outputWidth = 0;

                for (; col < cols; col++)
                {
                    if (!buffer.Contents [row, col].IsDirty)
                    {
                        if (output.Length > 0)
                        {
                            WriteToConsole (output, ref lastCol, row, ref outputWidth);
                        }
                        else if (lastCol == -1)
                        {
                            lastCol = col;
                        }

                        if (lastCol + 1 < cols)
                        {
                            lastCol++;
                        }

                        continue;
                    }

                    if (lastCol == -1)
                    {
                        lastCol = col;
                    }

                    Attribute attr = buffer.Contents [row, col].Attribute.Value;

                    // Performance: Only send the escape sequence if the attribute has changed.
                    if (attr != redrawAttr)
                    {
                        redrawAttr = attr;

                        AppendOrWriteAttribute (output, attr, _redrawTextStyle);

                        _redrawTextStyle = attr.Style;
                    }

                    outputWidth++;

                    // Avoid Rune.ToString() by appending the rune chars.
                    Rune rune = buffer.Contents [row, col].Rune;
                    int runeCharsWritten = rune.EncodeToUtf16 (runeBuffer);
                    ReadOnlySpan<char> runeChars = runeBuffer [..runeCharsWritten];
                    output.Append (runeChars);

                    if (buffer.Contents [row, col].CombiningMarks.Count > 0)
                    {
                        // AtlasEngine does not support NON-NORMALIZED combining marks in a way
                        // compatible with the driver architecture. Any CMs (except in the first col)
                        // are correctly combined with the base char, but are ALSO treated as 1 column
                        // width codepoints E.g. `echo "[e`u{0301}`u{0301}]"` will output `[é  ]`.
                        // 
                        // For now, we just ignore the list of CMs.
                        //foreach (var combMark in Contents [row, col].CombiningMarks) {
                        //	output.Append (combMark);
                        //}
                        // WriteToConsole (output, ref lastCol, row, ref outputWidth);
                    }
                    else if (rune.IsSurrogatePair () && rune.GetColumns () < 2)
                    {
                        WriteToConsole (output, ref lastCol, row, ref outputWidth);
                        SetCursorPositionImpl (col - 1, row);
                    }

                    buffer.Contents [row, col].IsDirty = false;
                }
            }

            if (output.Length > 0)
            {
                SetCursorPositionImpl (lastCol, row);
                Write (output);
            }
        }

        foreach (SixelToRender s in Application.Sixel)
        {
            if (!string.IsNullOrWhiteSpace (s.SixelData))
            {
                SetCursorPositionImpl (s.ScreenPosition.X, s.ScreenPosition.Y);
                Console.Out.Write (s.SixelData);
            }
        }

        SetCursorVisibility (savedVisibility ?? CursorVisibility.Default);
        _cachedCursorVisibility = savedVisibility;
    }

    protected abstract void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle);

    private void WriteToConsole (StringBuilder output, ref int lastCol, int row, ref int outputWidth)
    {
        SetCursorPositionImpl (lastCol, row);
        Write (output);
        output.Clear ();
        lastCol += outputWidth;
        outputWidth = 0;
    }

    protected abstract void Write (StringBuilder output);

    protected abstract bool SetCursorPositionImpl (int screenPositionX, int screenPositionY);

    public abstract void SetCursorVisibility (CursorVisibility visibility);
}
