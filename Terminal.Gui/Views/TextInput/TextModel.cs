#nullable enable

namespace Terminal.Gui.Views;

/// <summary>
///     Represents the underlying data model for managing and manipulating multi-line text in a <see cref="TextView"/>.
/// </summary>
/// <remarks>
///     The <see cref="TextModel"/> class provides functionality for storing, retrieving, and modifying lines of text,
///     as well as supporting operations like word navigation, text search, and file loading. It is used internally
///     by text input controls such as <see cref="TextView"/> to manage text content.
/// </remarks>
internal class TextModel
{
    private List<List<Cell>> _lines = new ();
    private (Point startPointToFind, Point currentPointToFind, bool found) _toFind;

    /// <summary>The number of text lines in the model</summary>
    public int Count => _lines.Count;

    public string? FilePath { get; set; }

    /// <summary>Adds a line to the model at the specified position.</summary>
    /// <param name="pos">Line number where the line will be inserted.</param>
    /// <param name="cells">The line of text and color, as a List of Cell.</param>
    public void AddLine (int pos, List<Cell> cells) { _lines.Insert (pos, cells); }

    public bool CloseFile ()
    {
        if (FilePath is null)
        {
            throw new ArgumentNullException (nameof (FilePath));
        }

        FilePath = null;
        _lines = new ();

        return true;
    }

    public List<List<Cell>> GetAllLines () { return _lines; }

    /// <summary>Returns the specified line as a List of Rune</summary>
    /// <returns>The line.</returns>
    /// <param name="line">Line number to retrieve.</param>
    public List<Cell> GetLine (int line)
    {
        if (_lines.Count > 0)
        {
            if (line < Count)
            {
                return _lines [line];
            }

            return _lines [Count - 1];
        }

        _lines.Add (new ());

        return _lines [0];
    }

    /// <summary>Returns the maximum line length of the visible lines.</summary>
    /// <param name="first">The first line.</param>
    /// <param name="last">The last line.</param>
    /// <param name="tabWidth">The tab width.</param>
    public int GetMaxVisibleLine (int first, int last, int tabWidth)
    {
        var maxLength = 0;
        last = last < _lines.Count ? last : _lines.Count;

        for (int i = first; i < last; i++)
        {
            List<Cell> line = GetLine (i);
            int tabSum = line.Sum (c => c.Rune.Value == '\t' ? Math.Max (tabWidth - 1, 0) : 0);
            int l = line.Count + tabSum;

            if (l > maxLength)
            {
                maxLength = l;
            }
        }

        return maxLength;
    }

    public event EventHandler? LinesLoaded;

    public bool LoadFile (string file)
    {
        FilePath = file ?? throw new ArgumentNullException (nameof (file));

        using (FileStream stream = File.OpenRead (file))
        {
            LoadStream (stream);

            return true;
        }
    }

    public void LoadListCells (List<List<Cell>> cellsList, Attribute? attribute)
    {
        _lines = cellsList;
        SetAttributes (attribute);
        OnLinesLoaded ();
    }

    public void LoadCells (List<Cell> cells, Attribute? attribute)
    {
        _lines = Cell.ToCells (cells);
        SetAttributes (attribute);
        OnLinesLoaded ();
    }

    public void LoadStream (Stream input)
    {
        if (input is null)
        {
            throw new ArgumentNullException (nameof (input));
        }

        _lines = new ();
        var buff = new BufferedStream (input);
        int v;
        List<byte> line = new ();
        var wasNewLine = false;

        while ((v = buff.ReadByte ()) != -1)
        {
            if (v == 13)
            {
                continue;
            }

            if (v == 10)
            {
                Append (line);
                line.Clear ();
                wasNewLine = true;

                continue;
            }

            line.Add ((byte)v);
            wasNewLine = false;
        }

        if (line.Count > 0 || wasNewLine)
        {
            Append (line);
        }

        buff.Dispose ();

        OnLinesLoaded ();
    }

    public void LoadString (string content)
    {
        _lines = Cell.StringToLinesOfCells (content);

        OnLinesLoaded ();
    }

    /// <summary>Removes the line at the specified position</summary>
    /// <param name="pos">Position.</param>
    public void RemoveLine (int pos)
    {
        if (_lines.Count > 0)
        {
            if (_lines.Count == 1 && _lines [0].Count == 0)
            {
                return;
            }

            _lines.RemoveAt (pos);
        }
    }

    public void ReplaceLine (int pos, List<Cell> runes)
    {
        if (_lines.Count > 0 && pos < _lines.Count)
        {
            _lines [pos] = [.. runes];
        }
        else if (_lines.Count == 0 || (_lines.Count > 0 && pos >= _lines.Count))
        {
            _lines.Add (runes);
        }
    }

    public override string ToString ()
    {
        var sb = new StringBuilder ();

        for (var i = 0; i < _lines.Count; i++)
        {
            sb.Append (Cell.ToString (_lines [i]));

            if (i + 1 < _lines.Count)
            {
                sb.AppendLine ();
            }
        }

        return sb.ToString ();
    }

    public (int col, int row)? WordBackward (int fromCol, int fromRow, bool useSameRuneType)
    {
        if (fromRow == 0 && fromCol == 0)
        {
            return null;
        }

        int col = Math.Max (fromCol - 1, 0);
        int row = fromRow;

        try
        {
            Cell? cell = RuneAt (col, row);
            Rune rune;

            if (cell is { })
            {
                rune = cell.Value.Rune;
            }
            else
            {
                if (col > 0)
                {
                    return (col, row);
                }

                if (col == 0 && row > 0)
                {
                    row--;
                    List<Cell> line = GetLine (row);

                    return (line.Count, row);
                }

                return null;
            }

            RuneType runeType = GetRuneType (rune);

            int lastValidCol = IsSameRuneType (rune, runeType, useSameRuneType) && (Rune.IsLetterOrDigit (rune) || Rune.IsPunctuation (rune) || Rune.IsSymbol (rune))
                                   ? col
                                   : -1;

            void ProcMovePrev (ref int nCol, ref int nRow, Rune nRune)
            {
                if (Rune.IsWhiteSpace (nRune))
                {
                    while (MovePrev (ref nCol, ref nRow, out nRune, useSameRuneType))
                    {
                        lastValidCol = nCol;

                        if (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune))
                        {
                            rune = nRune;
                            runeType = GetRuneType (nRune);
                        }
                    }

                    if (lastValidCol > -1)
                    {
                        nCol = lastValidCol;
                        nRow = fromRow;
                    }

                    if ((!Rune.IsWhiteSpace (nRune) && Rune.IsWhiteSpace (rune))
                        || (Rune.IsWhiteSpace (nRune) && !Rune.IsWhiteSpace (rune)))
                    {
                        return;
                    }

                    if (nRow != fromRow && (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune)))
                    {
                        List<Cell> line = GetLine (nRow);

                        if (lastValidCol > -1)
                        {
                            nCol = lastValidCol + Math.Max (lastValidCol, line.Count);
                        }
                    }
                }
                else
                {
                    if (!MovePrev (ref nCol, ref nRow, out nRune, useSameRuneType))
                    {
                        if (lastValidCol > -1)
                        {
                            nCol = lastValidCol;
                            nRow = fromRow;
                        }

                        return;
                    }

                    List<Cell> line = GetLine (nRow);

                    if (nCol == 0
                        && nRow == fromRow
                        && (Rune.IsLetterOrDigit (line [0].Rune) || Rune.IsPunctuation (line [0].Rune) || Rune.IsSymbol (line [0].Rune)))
                    {
                        return;
                    }

                    lastValidCol =
                        (IsSameRuneType (nRune, runeType, useSameRuneType) && Rune.IsLetterOrDigit (nRune)) || Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune)
                            ? nCol
                            : lastValidCol;

                    if (lastValidCol > -1 && Rune.IsWhiteSpace (nRune))
                    {
                        nCol = lastValidCol;

                        return;
                    }

                    if (fromRow != nRow)
                    {
                        nCol = line.Count;

                        return;
                    }

                    ProcMovePrev (ref nCol, ref nRow, nRune);
                }
            }

            ProcMovePrev (ref col, ref row, rune);

            if (fromCol != col || fromRow != row)
            {
                return (col, row);
            }

            if (fromCol == col && fromRow == row && row > 0)
            {
                row--;
                List<Cell> line = GetLine (row);
                col = line.Count;

                return (col, row);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public (int col, int row)? WordForward (int fromCol, int fromRow, bool useSameRuneType)
    {
        if (fromRow == _lines.Count - 1 && fromCol == GetLine (_lines.Count - 1).Count)
        {
            return null;
        }

        int col = fromCol;
        int row = fromRow;

        try
        {
            Rune rune = _lines [row].Count > 0 ? RuneAt (col, row)!.Value.Rune : default (Rune);
            RuneType runeType = GetRuneType (rune);

            int lastValidCol = IsSameRuneType (rune, runeType, useSameRuneType) && (Rune.IsLetterOrDigit (rune) || Rune.IsPunctuation (rune) || Rune.IsSymbol (rune))
                                   ? col
                                   : -1;

            void ProcMoveNext (ref int nCol, ref int nRow, Rune nRune)
            {
                if (Rune.IsWhiteSpace (nRune))
                {
                    while (MoveNext (ref nCol, ref nRow, out nRune, useSameRuneType))
                    {
                        lastValidCol = nCol;

                        if (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune))
                        {
                            return;
                        }
                    }

                    lastValidCol = nCol;

                    if (!Rune.IsWhiteSpace (nRune) && Rune.IsWhiteSpace (rune))
                    {
                        return;
                    }

                    if (nRow != fromRow && (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune)))
                    {
                        if (lastValidCol > -1)
                        {
                            nCol = lastValidCol;
                        }

                        return;
                    }

                    if (lastValidCol > -1)
                    {
                        nCol = lastValidCol;
                        nRow = fromRow;
                    }
                }
                else
                {
                    if (!MoveNext (ref nCol, ref nRow, out nRune, useSameRuneType))
                    {
                        return;
                    }

                    lastValidCol = nCol;

                    if (!IsSameRuneType (nRune, runeType, useSameRuneType) && !Rune.IsWhiteSpace (nRune))
                    {
                        return;
                    }

                    List<Cell> line = GetLine (nRow);

                    if (nCol == line.Count
                        && nRow == fromRow
                        && (Rune.IsLetterOrDigit (line [0].Rune) || Rune.IsPunctuation (line [0].Rune) || Rune.IsSymbol (line [0].Rune)))
                    {
                        return;
                    }

                    if (fromRow != nRow)
                    {
                        nCol = 0;

                        return;
                    }

                    ProcMoveNext (ref nCol, ref nRow, nRune);
                }
            }

            ProcMoveNext (ref col, ref row, rune);

            if (fromCol != col || fromRow != row)
            {
                return (col, row);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public (int startCol, int col, int row)? ProcessDoubleClickSelection (int fromStartCol, int fromCol, int fromRow, bool useSameRuneType, bool selectWordOnly)
    {
        List<Cell> line = GetLine (fromRow);

        int startCol = fromStartCol;
        int col = fromCol;
        int row = fromRow;

        (int col, int row)? newPos = WordForward (col, row, useSameRuneType);

        if (newPos.HasValue)
        {
            col = row == newPos.Value.row ? newPos.Value.col : 0;
        }

        if (startCol > 0
            && StringExtensions.ToString (line.GetRange (startCol, col - startCol).Select (c => c.Rune).ToList ()).Trim () == ""
            && (col - startCol > 1 || (col - startCol > 0 && line [startCol - 1].Rune == (Rune)' ')))
        {
            while (startCol > 0 && line [startCol - 1].Rune == (Rune)' ')
            {
                startCol--;
            }
        }
        else
        {
            newPos = WordBackward (col, row, useSameRuneType);

            if (newPos is { })
            {
                startCol = row == newPos.Value.row ? newPos.Value.col : line.Count;
            }
        }

        if (selectWordOnly)
        {
            List<Rune> selRunes = line.GetRange (startCol, col - startCol).Select (c => c.Rune).ToList ();

            if (StringExtensions.ToString (selRunes).Trim () != "")
            {
                for (int i = selRunes.Count - 1; i > -1; i--)
                {
                    if (selRunes [i] == (Rune)' ')
                    {
                        col--;
                    }
                }
            }
        }

        if (fromStartCol != startCol || fromCol != col || fromRow != row)
        {
            return (startCol, col, row);
        }

        return null;
    }

    internal static int CalculateLeftColumn (List<Cell> t, int start, int end, int width, int tabWidth = 0)
    {
        List<Rune> runes = new ();

        foreach (Cell cell in t)
        {
            runes.Add (cell.Rune);
        }

        return CalculateLeftColumn (runes, start, end, width, tabWidth);
    }

    // Returns the left column in a range of the string.
    internal static int CalculateLeftColumn (List<Rune> t, int start, int end, int width, int tabWidth = 0)
    {
        if (t is null || t.Count == 0)
        {
            return 0;
        }

        var size = 0;
        int tcount = end > t.Count - 1 ? t.Count - 1 : end;
        var col = 0;

        for (int i = tcount; i >= 0; i--)
        {
            Rune rune = t [i];
            size += rune.GetColumns ();

            if (rune.Value == '\t')
            {
                size += tabWidth + 1;
            }

            if (size > width)
            {
                if (col + width == end)
                {
                    col++;
                }

                break;
            }

            if ((end < t.Count && col > 0 && start < end && col == start) || end - col == width - 1)
            {
                break;
            }

            col = i;
        }

        return col;
    }

    internal static (int size, int length) DisplaySize (
        List<Cell> t,
        int start = -1,
        int end = -1,
        bool checkNextRune = true,
        int tabWidth = 0
    )
    {
        List<Rune> runes = new ();

        foreach (Cell cell in t)
        {
            runes.Add (cell.Rune);
        }

        return DisplaySize (runes, start, end, checkNextRune, tabWidth);
    }

    // Returns the size and length in a range of the string.
    internal static (int size, int length) DisplaySize (
        List<Rune> t,
        int start = -1,
        int end = -1,
        bool checkNextRune = true,
        int tabWidth = 0
    )
    {
        if (t is null || t.Count == 0)
        {
            return (0, 0);
        }

        var size = 0;
        var len = 0;

        int tcount = end == -1 ? t.Count :
                     end > t.Count ? t.Count : end;
        int i = start == -1 ? 0 : start;

        for (; i < tcount; i++)
        {
            Rune rune = t [i];
            size += rune.GetColumns ();
            len += rune.GetEncodingLength (Encoding.Unicode);

            if (rune.Value == '\t')
            {
                size += tabWidth + 1;
                len += tabWidth - 1;
            }

            if (checkNextRune && i == tcount - 1 && t.Count > tcount && IsWideRune (t [i + 1], tabWidth, out int s, out int l))
            {
                size += s;
                len += l;
            }
        }

        bool IsWideRune (Rune r, int tWidth, out int s, out int l)
        {
            s = r.GetColumns ();
            l = r.GetEncodingLength ();

            if (r.Value == '\t')
            {
                s += tWidth + 1;
                l += tWidth - 1;
            }

            return s > 1;
        }

        return (size, len);
    }

    internal Size GetDisplaySize ()
    {
        var size = Size.Empty;

        return size;
    }

    internal (Point current, bool found) FindNextText (
        string text,
        out bool gaveFullTurn,
        bool matchCase = false,
        bool matchWholeWord = false
    )
    {
        if (text is null || _lines.Count == 0)
        {
            gaveFullTurn = false;

            return (Point.Empty, false);
        }

        if (_toFind.found)
        {
            _toFind.currentPointToFind.X++;
        }

        (Point current, bool found) foundPos = GetFoundNextTextPoint (
                                                                      text,
                                                                      _lines.Count,
                                                                      matchCase,
                                                                      matchWholeWord,
                                                                      _toFind.currentPointToFind
                                                                     );

        if (!foundPos.found && _toFind.currentPointToFind != _toFind.startPointToFind)
        {
            foundPos = GetFoundNextTextPoint (
                                              text,
                                              _toFind.startPointToFind.Y + 1,
                                              matchCase,
                                              matchWholeWord,
                                              Point.Empty
                                             );
        }

        gaveFullTurn = ApplyToFind (foundPos);

        return foundPos;
    }

    internal (Point current, bool found) FindPreviousText (
        string text,
        out bool gaveFullTurn,
        bool matchCase = false,
        bool matchWholeWord = false
    )
    {
        if (text is null || _lines.Count == 0)
        {
            gaveFullTurn = false;

            return (Point.Empty, false);
        }

        if (_toFind.found)
        {
            _toFind.currentPointToFind.X++;
        }

        int linesCount = _toFind.currentPointToFind.IsEmpty ? _lines.Count - 1 : _toFind.currentPointToFind.Y;

        (Point current, bool found) foundPos = GetFoundPreviousTextPoint (
                                                                          text,
                                                                          linesCount,
                                                                          matchCase,
                                                                          matchWholeWord,
                                                                          _toFind.currentPointToFind
                                                                         );

        if (!foundPos.found && _toFind.currentPointToFind != _toFind.startPointToFind)
        {
            foundPos = GetFoundPreviousTextPoint (
                                                  text,
                                                  _lines.Count - 1,
                                                  matchCase,
                                                  matchWholeWord,
                                                  new (_lines [_lines.Count - 1].Count, _lines.Count)
                                                 );
        }

        gaveFullTurn = ApplyToFind (foundPos);

        return foundPos;
    }

    internal static int GetColFromX (List<Cell> t, int start, int x, int tabWidth = 0)
    {
        List<Rune> runes = new ();

        foreach (Cell cell in t)
        {
            runes.Add (cell.Rune);
        }

        return GetColFromX (runes, start, x, tabWidth);
    }

    internal static int GetColFromX (List<Rune> t, int start, int x, int tabWidth = 0)
    {
        if (x < 0)
        {
            return x;
        }

        int size = start;
        int pX = x + start;

        for (int i = start; i < t.Count; i++)
        {
            Rune r = t [i];
            size += r.GetColumns ();

            if (r.Value == '\t')
            {
                size += tabWidth + 1;
            }

            if (i == pX || size > pX)
            {
                return i - start;
            }
        }

        return t.Count - start;
    }

    internal (Point current, bool found) ReplaceAllText (
        string text,
        bool matchCase = false,
        bool matchWholeWord = false,
        string? textToReplace = null
    )
    {
        var found = false;
        var pos = Point.Empty;

        for (var i = 0; i < _lines.Count; i++)
        {
            List<Cell> x = _lines [i];
            string txt = GetText (x);
            string matchText = !matchCase ? text.ToUpper () : text;
            int col = txt.IndexOf (matchText);

            while (col > -1)
            {
                if (matchWholeWord && !MatchWholeWord (txt, matchText, col))
                {
                    if (col + 1 > txt.Length)
                    {
                        break;
                    }

                    col = txt.IndexOf (matchText, col + 1);

                    continue;
                }

                if (col > -1)
                {
                    if (!found)
                    {
                        found = true;
                    }

                    _lines [i] = Cell.ToCellList (ReplaceText (x, textToReplace!, matchText, col));
                    x = _lines [i];
                    txt = GetText (x);
                    pos = new (col, i);
                    col += textToReplace!.Length - matchText.Length;
                }

                if (col < 0 || col + 1 > txt.Length)
                {
                    break;
                }

                col = txt.IndexOf (matchText, col + 1);
            }
        }

        string GetText (List<Cell> x)
        {
            var txt = Cell.ToString (x);

            if (!matchCase)
            {
                txt = txt.ToUpper ();
            }

            return txt;
        }

        return (pos, found);
    }

    /// <summary>Redefine column and line tracking.</summary>
    /// <param name="point">Contains the column and line.</param>
    internal void ResetContinuousFind (Point point)
    {
        _toFind.startPointToFind = _toFind.currentPointToFind = point;
        _toFind.found = false;
    }

    internal static bool SetCol (ref int col, int width, int cols)
    {
        if (col + cols <= width)
        {
            col += cols;

            return true;
        }

        return false;
    }

    private void Append (List<byte> line)
    {
        var str = StringExtensions.ToString (line.ToArray ());
        _lines.Add (Cell.StringToCells (str));
    }

    private bool ApplyToFind ((Point current, bool found) foundPos)
    {
        var gaveFullTurn = false;

        if (foundPos.found)
        {
            _toFind.currentPointToFind = foundPos.current;

            if (_toFind.found && _toFind.currentPointToFind == _toFind.startPointToFind)
            {
                gaveFullTurn = true;
            }

            if (!_toFind.found)
            {
                _toFind.startPointToFind = _toFind.currentPointToFind = foundPos.current;
                _toFind.found = foundPos.found;
            }
        }

        return gaveFullTurn;
    }

    private (Point current, bool found) GetFoundNextTextPoint (
        string text,
        int linesCount,
        bool matchCase,
        bool matchWholeWord,
        Point start
    )
    {
        for (int i = start.Y; i < linesCount; i++)
        {
            List<Cell> x = _lines [i];
            var txt = Cell.ToString (x);

            if (!matchCase)
            {
                txt = txt.ToUpper ();
            }

            string matchText = !matchCase ? text.ToUpper () : text;
            int col = txt.IndexOf (matchText, Math.Min (start.X, txt.Length));

            if (col > -1 && matchWholeWord && !MatchWholeWord (txt, matchText, col))
            {
                continue;
            }

            if (col > -1 && ((i == start.Y && col >= start.X) || i > start.Y) && txt.Contains (matchText))
            {
                return (new (col, i), true);
            }

            if (col == -1 && start.X > 0)
            {
                start.X = 0;
            }
        }

        return (Point.Empty, false);
    }

    private (Point current, bool found) GetFoundPreviousTextPoint (
        string text,
        int linesCount,
        bool matchCase,
        bool matchWholeWord,
        Point start
    )
    {
        for (int i = linesCount; i >= 0; i--)
        {
            List<Cell> x = _lines [i];
            var txt = Cell.ToString (x);

            if (!matchCase)
            {
                txt = txt.ToUpper ();
            }

            if (start.Y != i)
            {
                start.X = Math.Max (x.Count - 1, 0);
            }

            string matchText = !matchCase ? text.ToUpper () : text;
            int col = txt.LastIndexOf (matchText, _toFind.found ? start.X - 1 : start.X);

            if (col > -1 && matchWholeWord && !MatchWholeWord (txt, matchText, col))
            {
                continue;
            }

            if (col > -1 && ((i <= linesCount && col <= start.X) || i < start.Y) && txt.Contains (matchText))
            {
                return (new (col, i), true);
            }
        }

        return (Point.Empty, false);
    }

    private RuneType GetRuneType (Rune rune)
    {
        if (Rune.IsSymbol (rune))
        {
            return RuneType.IsSymbol;
        }

        if (Rune.IsWhiteSpace (rune))
        {
            return RuneType.IsWhiteSpace;
        }

        if (Rune.IsLetterOrDigit (rune))
        {
            return RuneType.IsLetterOrDigit;
        }

        if (Rune.IsPunctuation (rune))
        {
            return RuneType.IsPunctuation;
        }

        return RuneType.IsUnknown;
    }

    private bool IsSameRuneType (Rune newRune, RuneType runeType, bool useSameRuneType)
    {
        RuneType rt = GetRuneType (newRune);

        if (useSameRuneType)
        {
            return rt == runeType;
        }

        switch (runeType)
        {
            case RuneType.IsSymbol:
            case RuneType.IsPunctuation:
                return rt is RuneType.IsSymbol or RuneType.IsPunctuation;
            case RuneType.IsWhiteSpace:
            case RuneType.IsLetterOrDigit:
            case RuneType.IsUnknown:
                return rt == runeType;
            default:
                throw new ArgumentOutOfRangeException (nameof (runeType), runeType, null);
        }
    }

    private bool MatchWholeWord (string source, string matchText, int index = 0)
    {
        if (string.IsNullOrEmpty (source) || string.IsNullOrEmpty (matchText))
        {
            return false;
        }

        string txt = matchText.Trim ();
        int start = index > 0 ? index - 1 : 0;
        int end = index + txt.Length;

        if ((start == 0 || Rune.IsWhiteSpace ((Rune)source [start])) && (end == source.Length || Rune.IsWhiteSpace ((Rune)source [end])))
        {
            return true;
        }

        return false;
    }

    private bool MoveNext (ref int col, ref int row, out Rune rune, bool useSameRuneType)
    {
        List<Cell> line = GetLine (row);

        if (col + 1 < line.Count)
        {
            col++;
            rune = line [col].Rune;

            if (col + 1 == line.Count
                && !Rune.IsLetterOrDigit (rune)
                && !Rune.IsWhiteSpace (line [col - 1].Rune)
                && IsSameRuneType (line [col - 1].Rune, GetRuneType (rune), useSameRuneType))
            {
                col++;
            }

            if (!Rune.IsWhiteSpace (rune)
                && (Rune.IsWhiteSpace (line [col - 1].Rune) || !IsSameRuneType (line [col - 1].Rune, GetRuneType (rune), useSameRuneType)))
            {
                return false;
            }

            return true;
        }

        if (col + 1 == line.Count)
        {
            col++;
            rune = default (Rune);

            return false;
        }

        // End of line
        col = 0;
        row++;
        rune = default (Rune);

        return false;
    }

    private bool MovePrev (ref int col, ref int row, out Rune rune, bool useSameRuneType)
    {
        List<Cell> line = GetLine (row);

        if (col > 0)
        {
            col--;
            rune = line [col].Rune;

            if ((!Rune.IsWhiteSpace (rune)
                 && !Rune.IsWhiteSpace (line [col + 1].Rune)
                 && !IsSameRuneType (line [col + 1].Rune, GetRuneType (rune), useSameRuneType))
                || (Rune.IsWhiteSpace (rune) && !Rune.IsWhiteSpace (line [col + 1].Rune)))
            {
                return false;
            }

            return true;
        }

        rune = default (Rune);

        return false;
    }

    private void OnLinesLoaded () { LinesLoaded?.Invoke (this, EventArgs.Empty); }

    private string ReplaceText (List<Cell> source, string textToReplace, string matchText, int col)
    {
        var origTxt = Cell.ToString (source);
        (_, int len) = DisplaySize (source, 0, col, false);
        (_, int len2) = DisplaySize (source, col, col + matchText.Length, false);
        (_, int len3) = DisplaySize (source, col + matchText.Length, origTxt.GetRuneCount (), false);

        return origTxt [..len] + textToReplace + origTxt.Substring (len + len2, len3);
    }

    private Cell? RuneAt (int col, int row)
    {
        List<Cell> line = GetLine (row);

        if (line.Count > 0)
        {
            return line [col > line.Count - 1 ? line.Count - 1 : col];
        }

        return null;
    }

    private void SetAttributes (Attribute? attribute)
    {
        foreach (List<Cell> line in _lines)
        {
            for (var i = 0; i < line.Count; i++)
            {
                Cell cell = line [i];
                cell.Attribute ??= attribute;
                line [i] = cell;
            }
        }
    }

    private enum RuneType
    {
        IsSymbol,
        IsWhiteSpace,
        IsLetterOrDigit,
        IsPunctuation,
        IsUnknown
    }
}
