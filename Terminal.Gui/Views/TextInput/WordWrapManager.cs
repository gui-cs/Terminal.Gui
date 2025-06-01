#nullable enable

namespace Terminal.Gui.Views;

/// <summary>
///     Manages word wrapping for a <see cref="TextModel"/> in a <see cref="TextView"/>.
/// </summary>
/// <remarks>
///     The <see cref="WordWrapManager"/> class provides functionality to handle word wrapping for multi-line text.
///     It works with a <see cref="TextModel"/> to manage wrapped lines, calculate positions, and update the model
///     when text is added, removed, or modified. This is used internally by text input controls like
///     <see cref="TextView"/>
///     to ensure proper word wrapping behavior.
/// </remarks>
internal class WordWrapManager (TextModel model)
{
    private int _frameWidth;
    private bool _isWrapModelRefreshing;
    private List<WrappedLine> _wrappedModelLines = [];
    public TextModel Model { get; private set; } = model;

    public void AddLine (int row, int col)
    {
        int modelRow = GetModelLineFromWrappedLines (row);
        int modelCol = GetModelColFromWrappedLines (row, col);
        List<Cell> line = GetCurrentLine (modelRow);
        int restCount = line.Count - modelCol;
        List<Cell> rest = line.GetRange (modelCol, restCount);
        line.RemoveRange (modelCol, restCount);
        Model.AddLine (modelRow + 1, rest);
        _isWrapModelRefreshing = true;
        WrapModel (_frameWidth, out _, out _, out _, out _, modelRow + 1);
        _isWrapModelRefreshing = false;
    }

    public int GetModelColFromWrappedLines (int line, int col)
    {
        if (_wrappedModelLines?.Count == 0)
        {
            return 0;
        }

        int modelLine = GetModelLineFromWrappedLines (line);
        int firstLine = _wrappedModelLines.IndexOf (r => r.ModelLine == modelLine);
        var modelCol = 0;

        for (int i = firstLine; i <= Math.Min (line, _wrappedModelLines!.Count - 1); i++)
        {
            WrappedLine wLine = _wrappedModelLines [i];

            if (i < line)
            {
                modelCol += wLine.ColWidth;
            }
            else
            {
                modelCol += col;
            }
        }

        return modelCol;
    }

    public int GetModelLineFromWrappedLines (int line)
    {
        return _wrappedModelLines.Count > 0
                   ? _wrappedModelLines [Math.Min (
                                                   line,
                                                   _wrappedModelLines.Count - 1
                                                  )].ModelLine
                   : 0;
    }

    public int GetWrappedLineColWidth (int line, int col, WordWrapManager wrapManager)
    {
        if (_wrappedModelLines?.Count == 0)
        {
            return 0;
        }

        List<WrappedLine> wModelLines = wrapManager._wrappedModelLines;
        int modelLine = GetModelLineFromWrappedLines (line);
        int firstLine = _wrappedModelLines.IndexOf (r => r.ModelLine == modelLine);
        var modelCol = 0;
        var colWidthOffset = 0;
        int i = firstLine;

        while (modelCol < col)
        {
            WrappedLine wLine = _wrappedModelLines! [i];
            WrappedLine wLineToCompare = wModelLines [i];

            if (wLine.ModelLine != modelLine || wLineToCompare.ModelLine != modelLine)
            {
                break;
            }

            modelCol += Math.Max (wLine.ColWidth, wLineToCompare.ColWidth);
            colWidthOffset += wLine.ColWidth - wLineToCompare.ColWidth;

            if (modelCol > col)
            {
                modelCol += col - modelCol;
            }

            i++;
        }

        return modelCol - colWidthOffset;
    }

    public bool Insert (int row, int col, Cell cell)
    {
        List<Cell> line = GetCurrentLine (GetModelLineFromWrappedLines (row));
        line.Insert (GetModelColFromWrappedLines (row, col), cell);

        if (line.Count > _frameWidth)
        {
            return true;
        }

        return false;
    }

    public bool RemoveAt (int row, int col)
    {
        int modelRow = GetModelLineFromWrappedLines (row);
        List<Cell> line = GetCurrentLine (modelRow);
        int modelCol = GetModelColFromWrappedLines (row, col);

        if (modelCol > line.Count)
        {
            Model.RemoveLine (modelRow);
            RemoveAt (row, 0);

            return false;
        }

        if (modelCol < line.Count)
        {
            line.RemoveAt (modelCol);
        }

        if (line.Count > _frameWidth || (row + 1 < _wrappedModelLines.Count && _wrappedModelLines [row + 1].ModelLine == modelRow))
        {
            return true;
        }

        return false;
    }

    public bool RemoveLine (int row, int col, out bool lineRemoved, bool forward = true)
    {
        lineRemoved = false;
        int modelRow = GetModelLineFromWrappedLines (row);
        List<Cell> line = GetCurrentLine (modelRow);
        int modelCol = GetModelColFromWrappedLines (row, col);

        if (modelCol == 0 && line.Count == 0)
        {
            Model.RemoveLine (modelRow);

            return false;
        }

        if (modelCol < line.Count)
        {
            if (forward)
            {
                line.RemoveAt (modelCol);

                return true;
            }

            if (modelCol - 1 > -1)
            {
                line.RemoveAt (modelCol - 1);

                return true;
            }
        }

        lineRemoved = true;

        if (forward)
        {
            if (modelRow + 1 == Model.Count)
            {
                return false;
            }

            List<Cell> nextLine = Model.GetLine (modelRow + 1);
            line.AddRange (nextLine);
            Model.RemoveLine (modelRow + 1);

            if (line.Count > _frameWidth)
            {
                return true;
            }
        }
        else
        {
            if (modelRow == 0)
            {
                return false;
            }

            List<Cell> prevLine = Model.GetLine (modelRow - 1);
            prevLine.AddRange (line);
            Model.RemoveLine (modelRow);

            if (prevLine.Count > _frameWidth)
            {
                return true;
            }
        }

        return false;
    }

    public bool RemoveRange (int row, int index, int count)
    {
        int modelRow = GetModelLineFromWrappedLines (row);
        List<Cell> line = GetCurrentLine (modelRow);
        int modelCol = GetModelColFromWrappedLines (row, index);

        try
        {
            line.RemoveRange (modelCol, count);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public List<List<Cell>> ToListRune (List<string> textList)
    {
        List<List<Cell>> runesList = new ();

        foreach (string text in textList)
        {
            runesList.Add (Cell.ToCellList (text));
        }

        return runesList;
    }

    public void UpdateModel (
        TextModel model,
        out int nRow,
        out int nCol,
        out int nStartRow,
        out int nStartCol,
        int row,
        int col,
        int startRow,
        int startCol,
        bool preserveTrailingSpaces
    )
    {
        _isWrapModelRefreshing = true;
        Model = model;

        WrapModel (
                   _frameWidth,
                   out nRow,
                   out nCol,
                   out nStartRow,
                   out nStartCol,
                   row,
                   col,
                   startRow,
                   startCol,
                   0,
                   preserveTrailingSpaces
                  );
        _isWrapModelRefreshing = false;
    }

    public TextModel WrapModel (
        int width,
        out int nRow,
        out int nCol,
        out int nStartRow,
        out int nStartCol,
        int row = 0,
        int col = 0,
        int startRow = 0,
        int startCol = 0,
        int tabWidth = 0,
        bool preserveTrailingSpaces = true
    )
    {
        _frameWidth = width;

        int modelRow = _isWrapModelRefreshing ? row : GetModelLineFromWrappedLines (row);
        int modelCol = _isWrapModelRefreshing ? col : GetModelColFromWrappedLines (row, col);
        int modelStartRow = _isWrapModelRefreshing ? startRow : GetModelLineFromWrappedLines (startRow);

        int modelStartCol =
            _isWrapModelRefreshing ? startCol : GetModelColFromWrappedLines (startRow, startCol);
        var wrappedModel = new TextModel ();
        var lines = 0;
        nRow = 0;
        nCol = 0;
        nStartRow = 0;
        nStartCol = 0;
        bool isRowAndColSet = row == 0 && col == 0;
        bool isStartRowAndColSet = startRow == 0 && startCol == 0;
        List<WrappedLine> wModelLines = new ();

        for (var i = 0; i < Model.Count; i++)
        {
            List<Cell> line = Model.GetLine (i);

            List<List<Cell>> wrappedLines = ToListRune (
                                                        TextFormatter.Format (
                                                                              Cell.ToString (line),
                                                                              width,
                                                                              Alignment.Start,
                                                                              true,
                                                                              preserveTrailingSpaces,
                                                                              tabWidth
                                                                             )
                                                       );
            var sumColWidth = 0;

            for (var j = 0; j < wrappedLines.Count; j++)
            {
                List<Cell> wrapLine = wrappedLines [j];

                if (!isRowAndColSet && modelRow == i)
                {
                    if (nCol + wrapLine.Count <= modelCol)
                    {
                        nCol += wrapLine.Count;
                        nRow = lines;

                        if (nCol == modelCol)
                        {
                            nCol = wrapLine.Count;
                            isRowAndColSet = true;
                        }
                        else if (j == wrappedLines.Count - 1)
                        {
                            nCol = wrapLine.Count - j + modelCol - nCol;
                            isRowAndColSet = true;
                        }
                    }
                    else
                    {
                        int offset = nCol + wrapLine.Count - modelCol;
                        nCol = wrapLine.Count - offset;
                        nRow = lines;
                        isRowAndColSet = true;
                    }
                }

                if (!isStartRowAndColSet && modelStartRow == i)
                {
                    if (nStartCol + wrapLine.Count <= modelStartCol)
                    {
                        nStartCol += wrapLine.Count;
                        nStartRow = lines;

                        if (nStartCol == modelStartCol)
                        {
                            nStartCol = wrapLine.Count;
                            isStartRowAndColSet = true;
                        }
                        else if (j == wrappedLines.Count - 1)
                        {
                            nStartCol = wrapLine.Count - j + modelStartCol - nStartCol;
                            isStartRowAndColSet = true;
                        }
                    }
                    else
                    {
                        int offset = nStartCol + wrapLine.Count - modelStartCol;
                        nStartCol = wrapLine.Count - offset;
                        nStartRow = lines;
                        isStartRowAndColSet = true;
                    }
                }

                for (int k = j; k < wrapLine.Count; k++)
                {
                    Cell cell = wrapLine [k];
                    cell.Attribute = line [k].Attribute;
                    wrapLine [k] = cell;
                }

                wrappedModel.AddLine (lines, wrapLine);
                sumColWidth += wrapLine.Count;

                var wrappedLine = new WrappedLine
                {
                    ModelLine = i, Row = lines, RowIndex = j, ColWidth = wrapLine.Count
                };
                wModelLines.Add (wrappedLine);
                lines++;
            }
        }

        _wrappedModelLines = wModelLines;

        return wrappedModel;
    }

    private List<Cell> GetCurrentLine (int row) { return Model.GetLine (row); }

    private class WrappedLine
    {
        public int ColWidth;
        public int ModelLine;
        public int Row;
        public int RowIndex;
    }
}
