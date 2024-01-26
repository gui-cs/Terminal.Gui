#nullable enable

// TextView.cs: multi-line text editing
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Terminal.Gui.Resources;

namespace Terminal.Gui;

/// <summary>
///         Represents a single row/column within the <see cref="TextView" />. Includes the glyph and the
///         foreground/background
///         colors.
/// </summary>
[DebuggerDisplay ("{ColorSchemeDebuggerDisplay}")]
public class RuneCell : IEquatable<RuneCell> {
	/// <summary>
	///         The glyph to draw.
	/// </summary>
	[JsonConverter (typeof (RuneJsonConverter))]
	public Rune Rune { get; set; }

	/// <summary>
	///         The <see cref="Terminal.Gui.ColorScheme" /> color sets to draw the glyph with.
	/// </summary>
	[JsonConverter (typeof (ColorSchemeJsonConverter))]
	public ColorScheme? ColorScheme { get; set; }

	string ColorSchemeDebuggerDisplay => ToString ();

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	///         <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter;
	///         otherwise, <see langword="false" />.
	/// </returns>
	public bool Equals (RuneCell? other) => other is not null &&
						Rune.Equals (other.Rune) &&
						ColorScheme == other.ColorScheme;

	/// <summary>Returns a string that represents the current object.</summary>
	/// <returns>A string that represents the current object.</returns>
	public override string ToString ()
	{
		var colorSchemeStr = ColorScheme?.ToString () ?? "null";
		return $"U+{Rune.Value:X4} '{Rune.ToString ()}'; {colorSchemeStr}";
	}
}

class TextModel {
	List<List<RuneCell>> _lines = new ();

	(Point startPointToFind, Point currentPointToFind, bool found) _toFind;

	public string? FilePath { get; set; }

	/// <summary>
	///         The number of text lines in the model
	/// </summary>
	public int Count => _lines.Count;

	public event EventHandler? LinesLoaded;

	public bool LoadFile (string file)
	{
		FilePath = file ?? throw new ArgumentNullException (nameof (file));

		using (var stream = File.OpenRead (file)) {
			LoadStream (stream);
			return true;
		}
	}

	public bool CloseFile ()
	{
		if (FilePath == null) {
			throw new ArgumentNullException (nameof (FilePath));
		}

		FilePath = null;
		_lines = new List<List<RuneCell>> ();
		return true;
	}

	// Turns the string into cells, this does not split the 
	// contents on a newline if it is present.
	internal static List<RuneCell> StringToRuneCells (string str, ColorScheme? colorScheme = null)
	{
		var cells = new List<RuneCell> ();
		foreach (var rune in str.ToRunes ()) {
			cells.Add (new RuneCell { Rune = rune, ColorScheme = colorScheme });
		}

		return cells;
	}

	internal static List<RuneCell> ToRuneCells (IEnumerable<Rune> runes, ColorScheme? colorScheme = null)
	{
		var cells = new List<RuneCell> ();
		foreach (var rune in runes) {
			cells.Add (new RuneCell { Rune = rune, ColorScheme = colorScheme });
		}

		return cells;
	}

	static List<List<RuneCell>> ToRuneCells (List<RuneCell> cells) => SplitNewLines (cells);

	// Splits a string into a List that contains a List<RuneCell> for each line
	public static List<List<RuneCell>> StringToLinesOfRuneCells (string content, ColorScheme? colorScheme = null)
	{
		var cells = content.EnumerateRunes ().Select (x => new RuneCell { Rune = x, ColorScheme = colorScheme })
			.ToList ();

		return SplitNewLines (cells);
	}

	static List<List<RuneCell>> SplitNewLines (List<RuneCell> cells)
	{
		var lines = new List<List<RuneCell>> ();
		int start = 0, i = 0;
		var hasCR = false;
		// ASCII code 13 = Carriage Return.
		// ASCII code 10 = Line Feed.
		for (; i < cells.Count; i++) {
			if (cells [i].Rune.Value == 13) {
				hasCR = true;
				continue;
			}

			if (cells [i].Rune.Value == 10) {
				if (i - start > 0) {
					lines.Add (cells.GetRange (start, hasCR ? i - 1 - start : i - start));
				} else {
					lines.Add (StringToRuneCells (string.Empty));
				}

				start = i + 1;
				hasCR = false;
			}
		}

		if (i - start >= 0) {
			lines.Add (cells.GetRange (start, i - start));
		}

		return lines;
	}

	void Append (List<byte> line)
	{
		var str = StringExtensions.ToString (line.ToArray ());
		_lines.Add (StringToRuneCells (str));
	}

	public void LoadStream (Stream input)
	{
		if (input == null) {
			throw new ArgumentNullException (nameof (input));
		}

		_lines = new List<List<RuneCell>> ();
		var buff = new BufferedStream (input);
		int v;
		var line = new List<byte> ();
		var wasNewLine = false;
		while ((v = buff.ReadByte ()) != -1) {
			if (v == 13) {
				continue;
			}

			if (v == 10) {
				Append (line);
				line.Clear ();
				wasNewLine = true;
				continue;
			}

			line.Add ((byte)v);
			wasNewLine = false;
		}

		if (line.Count > 0 || wasNewLine) {
			Append (line);
		}

		buff.Dispose ();

		OnLinesLoaded ();
	}

	public void LoadString (string content)
	{
		_lines = StringToLinesOfRuneCells (content);

		OnLinesLoaded ();
	}

	public void LoadRuneCells (List<RuneCell> cells, ColorScheme? colorScheme)
	{
		_lines = ToRuneCells (cells);
		SetColorSchemes (colorScheme);
		OnLinesLoaded ();
	}

	public void LoadListRuneCells (List<List<RuneCell>> cellsList, ColorScheme? colorScheme)
	{
		_lines = cellsList;
		SetColorSchemes (colorScheme);
		OnLinesLoaded ();
	}

	void SetColorSchemes (ColorScheme? colorScheme)
	{
		foreach (var line in _lines) {
			foreach (var cell in line) {
				cell.ColorScheme ??= colorScheme;
			}
		}
	}

	void OnLinesLoaded () => LinesLoaded?.Invoke (this, EventArgs.Empty);

	public override string ToString ()
	{
		var sb = new StringBuilder ();
		for (var i = 0; i < _lines.Count; i++) {
			sb.Append (ToString (_lines [i]));
			if (i + 1 < _lines.Count) {
				sb.AppendLine ();
			}
		}

		return sb.ToString ();
	}

	/// <summary>
	///         Returns the specified line as a List of Rune
	/// </summary>
	/// <returns>The line.</returns>
	/// <param name="line">Line number to retrieve.</param>
	public List<RuneCell> GetLine (int line)
	{
		if (_lines.Count > 0) {
			if (line < Count) {
				return _lines [line];
			}

			return _lines [Count - 1];
		}

		_lines.Add (new List<RuneCell> ());
		return _lines [0];
	}

	public List<List<RuneCell>> GetAllLines () => _lines;

	/// <summary>
	///         Adds a line to the model at the specified position.
	/// </summary>
	/// <param name="pos">Line number where the line will be inserted.</param>
	/// <param name="cells">The line of text and color, as a List of RuneCell.</param>
	public void AddLine (int pos, List<RuneCell> cells) => _lines.Insert (pos, cells);

	/// <summary>
	///         Removes the line at the specified position
	/// </summary>
	/// <param name="pos">Position.</param>
	public void RemoveLine (int pos)
	{
		if (_lines.Count > 0) {
			if (_lines.Count == 1 && _lines [0].Count == 0) {
				return;
			}

			_lines.RemoveAt (pos);
		}
	}

	public void ReplaceLine (int pos, List<RuneCell> runes)
	{
		if (_lines.Count > 0 && pos < _lines.Count) {
			_lines [pos] = new List<RuneCell> (runes);
		} else if (_lines.Count == 0 || (_lines.Count > 0 && pos >= _lines.Count)) {
			_lines.Add (runes);
		}
	}

	/// <summary>
	///         Returns the maximum line length of the visible lines.
	/// </summary>
	/// <param name="first">The first line.</param>
	/// <param name="last">The last line.</param>
	/// <param name="tabWidth">The tab width.</param>
	public int GetMaxVisibleLine (int first, int last, int tabWidth)
	{
		var maxLength = 0;
		last = last < _lines.Count ? last : _lines.Count;
		for (var i = first; i < last; i++) {
			var line = GetLine (i);
			var tabSum = line.Sum (c => c.Rune.Value == '\t' ? Math.Max (tabWidth - 1, 0) : 0);
			var l = line.Count + tabSum;
			if (l > maxLength) {
				maxLength = l;
			}
		}

		return maxLength;
	}

	internal static bool SetCol (ref int col, int width, int cols)
	{
		if (col + cols <= width) {
			col += cols;
			return true;
		}

		return false;
	}

	internal static int GetColFromX (List<RuneCell> t, int start, int x, int tabWidth = 0)
	{
		var runes = new List<Rune> ();
		foreach (var cell in t) {
			runes.Add (cell.Rune);
		}

		return GetColFromX (runes, start, x, tabWidth);
	}

	internal static int GetColFromX (List<Rune> t, int start, int x, int tabWidth = 0)
	{
		if (x < 0) {
			return x;
		}

		var size = start;
		var pX = x + start;
		for (var i = start; i < t.Count; i++) {
			var r = t [i];
			size += r.GetColumns ();
			if (r.Value == '\t') {
				size += tabWidth + 1;
			}

			if (i == pX || size > pX) {
				return i - start;
			}
		}

		return t.Count - start;
	}

	internal static (int size, int length) DisplaySize (List<RuneCell> t,
		int start = -1,
		int end = -1,
		bool checkNextRune = true,
		int tabWidth = 0)
	{
		var runes = new List<Rune> ();
		foreach (var cell in t) {
			runes.Add (cell.Rune);
		}

		return DisplaySize (runes, start, end, checkNextRune, tabWidth);
	}

	// Returns the size and length in a range of the string.
	internal static (int size, int length) DisplaySize (List<Rune> t,
		int start = -1,
		int end = -1,
		bool checkNextRune = true,
		int tabWidth = 0)
	{
		if (t == null || t.Count == 0) {
			return (0, 0);
		}

		var size = 0;
		var len = 0;
		var tcount = end == -1 ? t.Count : end > t.Count ? t.Count : end;
		var i = start == -1 ? 0 : start;
		for (; i < tcount; i++) {
			var rune = t [i];
			size += rune.GetColumns ();
			len += rune.GetEncodingLength (Encoding.Unicode);
			if (rune.Value == '\t') {
				size += tabWidth + 1;
				len += tabWidth - 1;
			}

			if (checkNextRune && i == tcount - 1 && t.Count > tcount &&
			    IsWideRune (t [i + 1], tabWidth, out var s, out var l)) {
				size += s;
				len += l;
			}
		}

		bool IsWideRune (Rune r, int tWidth, out int s, out int l)
		{
			s = r.GetColumns ();
			l = r.GetEncodingLength ();
			if (r.Value == '\t') {
				s += tWidth + 1;
				l += tWidth - 1;
			}

			return s > 1;
		}

		return (size, len);
	}

	internal static int CalculateLeftColumn (List<RuneCell> t, int start, int end, int width, int tabWidth = 0)
	{
		var runes = new List<Rune> ();
		foreach (var cell in t) {
			runes.Add (cell.Rune);
		}

		return CalculateLeftColumn (runes, start, end, width, tabWidth);
	}

	// Returns the left column in a range of the string.
	internal static int CalculateLeftColumn (List<Rune> t, int start, int end, int width, int tabWidth = 0)
	{
		if (t == null || t.Count == 0) {
			return 0;
		}

		var size = 0;
		var tcount = end > t.Count - 1 ? t.Count - 1 : end;
		var col = 0;

		for (var i = tcount; i >= 0; i--) {
			var rune = t [i];
			size += rune.GetColumns ();
			if (rune.Value == '\t') {
				size += tabWidth + 1;
			}

			if (size > width) {
				if (col + width == end) {
					col++;
				}

				break;
			}

			if ((end < t.Count && col > 0 && start < end && col == start) || end - col == width - 1) {
				break;
			}

			col = i;
		}

		return col;
	}

	internal (Point current, bool found) FindNextText (string text, out bool gaveFullTurn, bool matchCase = false,
		bool matchWholeWord = false)
	{
		if (text == null || _lines.Count == 0) {
			gaveFullTurn = false;
			return (Point.Empty, false);
		}

		if (_toFind.found) {
			_toFind.currentPointToFind.X++;
		}

		var foundPos = GetFoundNextTextPoint (text, _lines.Count, matchCase, matchWholeWord,
			_toFind.currentPointToFind);
		if (!foundPos.found && _toFind.currentPointToFind != _toFind.startPointToFind) {
			foundPos = GetFoundNextTextPoint (text, _toFind.startPointToFind.Y + 1, matchCase,
				matchWholeWord, Point.Empty);
		}

		gaveFullTurn = ApplyToFind (foundPos);

		return foundPos;
	}

	internal (Point current, bool found) FindPreviousText (string text, out bool gaveFullTurn,
		bool matchCase = false, bool matchWholeWord = false)
	{
		if (text == null || _lines.Count == 0) {
			gaveFullTurn = false;
			return (Point.Empty, false);
		}

		if (_toFind.found) {
			_toFind.currentPointToFind.X++;
		}

		var linesCount = _toFind.currentPointToFind.IsEmpty ? _lines.Count - 1 : _toFind.currentPointToFind.Y;
		var foundPos = GetFoundPreviousTextPoint (text, linesCount, matchCase, matchWholeWord,
			_toFind.currentPointToFind);
		if (!foundPos.found && _toFind.currentPointToFind != _toFind.startPointToFind) {
			foundPos = GetFoundPreviousTextPoint (text, _lines.Count - 1, matchCase, matchWholeWord,
				new Point (_lines [_lines.Count - 1].Count, _lines.Count));
		}

		gaveFullTurn = ApplyToFind (foundPos);

		return foundPos;
	}

	internal (Point current, bool found) ReplaceAllText (string text, bool matchCase = false,
		bool matchWholeWord = false, string? textToReplace = null)
	{
		var found = false;
		var pos = Point.Empty;

		for (var i = 0; i < _lines.Count; i++) {
			var x = _lines [i];
			var txt = GetText (x);
			var matchText = !matchCase ? text.ToUpper () : text;
			var col = txt.IndexOf (matchText);
			while (col > -1) {
				if (matchWholeWord && !MatchWholeWord (txt, matchText, col)) {
					if (col + 1 > txt.Length) {
						break;
					}

					col = txt.IndexOf (matchText, col + 1);
					continue;
				}

				if (col > -1) {
					if (!found) {
						found = true;
					}

					_lines [i] = ToRuneCellList (ReplaceText (x, textToReplace!, matchText, col));
					x = _lines [i];
					txt = GetText (x);
					pos = new Point (col, i);
					col += textToReplace!.Length - matchText.Length;
				}

				if (col < 0 || col + 1 > txt.Length) {
					break;
				}

				col = txt.IndexOf (matchText, col + 1);
			}
		}

		string GetText (List<RuneCell> x)
		{
			var txt = ToString (x);
			if (!matchCase) {
				txt = txt.ToUpper ();
			}

			return txt;
		}

		return (pos, found);
	}

	string ReplaceText (List<RuneCell> source, string textToReplace, string matchText, int col)
	{
		var origTxt = ToString (source);
		var (_, len) = DisplaySize (source, 0, col, false);
		var (_, len2) = DisplaySize (source, col, col + matchText.Length, false);
		var (_, len3) = DisplaySize (source, col + matchText.Length, origTxt.GetRuneCount (), false);

		return origTxt [..len] +
		       textToReplace +
		       origTxt.Substring (len + len2, len3);
	}

	bool ApplyToFind ((Point current, bool found) foundPos)
	{
		var gaveFullTurn = false;
		if (foundPos.found) {
			_toFind.currentPointToFind = foundPos.current;
			if (_toFind.found && _toFind.currentPointToFind == _toFind.startPointToFind) {
				gaveFullTurn = true;
			}

			if (!_toFind.found) {
				_toFind.startPointToFind = _toFind.currentPointToFind = foundPos.current;
				_toFind.found = foundPos.found;
			}
		}

		return gaveFullTurn;
	}

	(Point current, bool found) GetFoundNextTextPoint (string text, int linesCount, bool matchCase,
		bool matchWholeWord, Point start)
	{
		for (var i = start.Y; i < linesCount; i++) {
			var x = _lines [i];
			var txt = ToString (x);
			if (!matchCase) {
				txt = txt.ToUpper ();
			}

			var matchText = !matchCase ? text.ToUpper () : text;
			var col = txt.IndexOf (matchText, Math.Min (start.X, txt.Length));
			if (col > -1 && matchWholeWord && !MatchWholeWord (txt, matchText, col)) {
				continue;
			}

			if (col > -1 && ((i == start.Y && col >= start.X) || i > start.Y) && txt.Contains (matchText)) {
				return (new Point (col, i), true);
			}

			if (col == -1 && start.X > 0) {
				start.X = 0;
			}
		}

		return (Point.Empty, false);
	}

	(Point current, bool found) GetFoundPreviousTextPoint (string text, int linesCount, bool matchCase,
		bool matchWholeWord, Point start)
	{
		for (var i = linesCount; i >= 0; i--) {
			var x = _lines [i];
			var txt = ToString (x);
			if (!matchCase) {
				txt = txt.ToUpper ();
			}

			if (start.Y != i) {
				start.X = Math.Max (x.Count - 1, 0);
			}

			var matchText = !matchCase ? text.ToUpper () : text;
			var col = txt.LastIndexOf (matchText, _toFind.found ? start.X - 1 : start.X);
			if (col > -1 && matchWholeWord && !MatchWholeWord (txt, matchText, col)) {
				continue;
			}

			if (col > -1 && ((i <= linesCount && col <= start.X) || i < start.Y) &&
			    txt.Contains (matchText)) {
				return (new Point (col, i), true);
			}
		}

		return (Point.Empty, false);
	}

	bool MatchWholeWord (string source, string matchText, int index = 0)
	{
		if (string.IsNullOrEmpty (source) || string.IsNullOrEmpty (matchText)) {
			return false;
		}

		var txt = matchText.Trim ();
		var start = index > 0 ? index - 1 : 0;
		var end = index + txt.Length;

		if ((start == 0 || Rune.IsWhiteSpace ((Rune)source [start])) &&
		    (end == source.Length || Rune.IsWhiteSpace ((Rune)source [end]))) {
			return true;
		}

		return false;
	}

	/// <summary>
	///         Redefine column and line tracking.
	/// </summary>
	/// <param name="point">Contains the column and line.</param>
	internal void ResetContinuousFind (Point point)
	{
		_toFind.startPointToFind = _toFind.currentPointToFind = point;
		_toFind.found = false;
	}

	RuneCell RuneAt (int col, int row)
	{
		var line = GetLine (row);
		if (line.Count > 0) {
			return line [col > line.Count - 1 ? line.Count - 1 : col];
		}

		return default!;
	}

	bool MoveNext (ref int col, ref int row, out Rune rune)
	{
		var line = GetLine (row);
		if (col + 1 < line.Count) {
			col++;
			rune = line [col].Rune;
			if (col + 1 == line.Count && !Rune.IsLetterOrDigit (rune) &&
			    !Rune.IsWhiteSpace (line [col - 1].Rune)) {
				col++;
			}

			return true;
		}

		if (col + 1 == line.Count) {
			col++;
		}

		while (row + 1 < Count) {
			col = 0;
			row++;
			line = GetLine (row);
			if (line.Count > 0) {
				rune = line [0].Rune;
				return true;
			}
		}

		rune = default;
		return false;
	}

	bool MovePrev (ref int col, ref int row, out Rune rune)
	{
		var line = GetLine (row);

		if (col > 0) {
			col--;
			rune = line [col].Rune;
			return true;
		}

		if (row == 0) {
			rune = default;
			return false;
		}

		while (row > 0) {
			row--;
			line = GetLine (row);
			col = line.Count - 1;
			if (col >= 0) {
				rune = line [col].Rune;
				return true;
			}
		}

		rune = default;
		return false;
	}

	RuneType GetRuneType (Rune rune)
	{
		if (Rune.IsSymbol (rune)) {
			return RuneType.IsSymbol;
		}

		if (Rune.IsWhiteSpace (rune)) {
			return RuneType.IsWhiteSpace;
		}

		if (Rune.IsLetterOrDigit (rune)) {
			return RuneType.IsLetterOrDigit;
		}

		if (Rune.IsPunctuation (rune)) {
			return RuneType.IsPunctuation;
		}

		return RuneType.IsUnknow;
	}

	bool IsSameRuneType (Rune newRune, RuneType runeType)
	{
		var rt = GetRuneType (newRune);
		return rt == runeType;
	}

	public (int col, int row)? WordForward (int fromCol, int fromRow)
	{
		if (fromRow == _lines.Count - 1 && fromCol == GetLine (_lines.Count - 1).Count) {
			return null;
		}

		var col = fromCol;
		var row = fromRow;
		try {
			var rune = RuneAt (col, row).Rune;
			var runeType = GetRuneType (rune);
			var lastValidCol = IsSameRuneType (rune, runeType) && (Rune.IsLetterOrDigit (rune) ||
									       Rune.IsPunctuation (rune) ||
									       Rune.IsSymbol (rune))
				? col
				: -1;

			void ProcMoveNext (ref int nCol, ref int nRow, Rune nRune)
			{
				if (Rune.IsWhiteSpace (nRune)) {
					while (MoveNext (ref nCol, ref nRow, out nRune)) {
						if (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) ||
						    Rune.IsSymbol (nRune)) {
							lastValidCol = nCol;
							return;
						}
					}

					if (nRow != fromRow && (Rune.IsLetterOrDigit (nRune) ||
								Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune))) {
						if (lastValidCol > -1) {
							nCol = lastValidCol;
						}

						return;
					}

					while (MoveNext (ref nCol, ref nRow, out nRune)) {
						if (!Rune.IsLetterOrDigit (nRune) && !Rune.IsPunctuation (nRune) &&
						    !Rune.IsSymbol (nRune)) {
							break;
						}

						if (nRow != fromRow) {
							break;
						}

						lastValidCol =
							(IsSameRuneType (nRune, runeType) &&
							 Rune.IsLetterOrDigit (nRune)) || Rune.IsPunctuation (nRune) ||
							Rune.IsSymbol (nRune)
								? nCol
								: lastValidCol;
					}

					if (lastValidCol > -1) {
						nCol = lastValidCol;
						nRow = fromRow;
					}
				} else {
					if (!MoveNext (ref nCol, ref nRow, out nRune)) {
						return;
					}

					if (!IsSameRuneType (nRune, runeType) && !Rune.IsWhiteSpace (nRune)) {
						return;
					}

					var line = GetLine (nRow);
					if (nCol == line.Count && nRow == fromRow &&
					    (Rune.IsLetterOrDigit (line [0].Rune) ||
					     Rune.IsPunctuation (line [0].Rune) || Rune.IsSymbol (line [0].Rune))) {
						return;
					}

					lastValidCol =
						(IsSameRuneType (nRune, runeType) && Rune.IsLetterOrDigit (nRune)) ||
						Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune)
							? nCol
							: lastValidCol;
					if (fromRow != nRow) {
						nCol = 0;
						return;
					}

					ProcMoveNext (ref nCol, ref nRow, nRune);
				}
			}

			ProcMoveNext (ref col, ref row, rune);

			if (fromCol != col || fromRow != row) {
				return (col, row);
			}

			return null;
		} catch (Exception) {
			return null;
		}
	}

	public (int col, int row)? WordBackward (int fromCol, int fromRow)
	{
		if (fromRow == 0 && fromCol == 0) {
			return null;
		}

		var col = Math.Max (fromCol - 1, 0);
		var row = fromRow;
		try {
			var cell = RuneAt (col, row);
			Rune rune;
			if (cell != null) {
				rune = cell.Rune;
			} else {
				if (col > 0) {
					return (col, row);
				}

				if (col == 0 && row > 0) {
					row--;
					var line = GetLine (row);
					return (line.Count, row);
				}

				return null;
			}

			var runeType = GetRuneType (rune);
			var lastValidCol = IsSameRuneType (rune, runeType) && (Rune.IsLetterOrDigit (rune) ||
									       Rune.IsPunctuation (rune) ||
									       Rune.IsSymbol (rune))
				? col
				: -1;

			void ProcMovePrev (ref int nCol, ref int nRow, Rune nRune)
			{
				if (Rune.IsWhiteSpace (nRune)) {
					while (MovePrev (ref nCol, ref nRow, out nRune)) {
						if (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) ||
						    Rune.IsSymbol (nRune)) {
							lastValidCol = nCol;
							if (runeType == RuneType.IsWhiteSpace ||
							    runeType == RuneType.IsUnknow) {
								runeType = GetRuneType (nRune);
							}

							break;
						}
					}

					if (nRow != fromRow && (Rune.IsLetterOrDigit (nRune) ||
								Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune))) {
						if (lastValidCol > -1) {
							nCol = lastValidCol;
						}

						return;
					}

					while (MovePrev (ref nCol, ref nRow, out nRune)) {
						if (!Rune.IsLetterOrDigit (nRune) && !Rune.IsPunctuation (nRune) &&
						    !Rune.IsSymbol (nRune)) {
							break;
						}

						if (nRow != fromRow) {
							break;
						}

						lastValidCol =
							(IsSameRuneType (nRune, runeType) &&
							 Rune.IsLetterOrDigit (nRune)) || Rune.IsPunctuation (nRune) ||
							Rune.IsSymbol (nRune)
								? nCol
								: lastValidCol;
					}

					if (lastValidCol > -1) {
						nCol = lastValidCol;
						nRow = fromRow;
					}
				} else {
					if (!MovePrev (ref nCol, ref nRow, out nRune)) {
						return;
					}

					var line = GetLine (nRow);
					if (nCol == 0 && nRow == fromRow && (Rune.IsLetterOrDigit (line [0].Rune) ||
									     Rune.IsPunctuation (line [0].Rune) ||
									     Rune.IsSymbol (line [0].Rune))) {
						return;
					}

					lastValidCol =
						(IsSameRuneType (nRune, runeType) && Rune.IsLetterOrDigit (nRune)) ||
						Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune)
							? nCol
							: lastValidCol;
					if (lastValidCol > -1 && Rune.IsWhiteSpace (nRune)) {
						nCol = lastValidCol;
						return;
					}

					if (fromRow != nRow) {
						nCol = line.Count;
						return;
					}

					ProcMovePrev (ref nCol, ref nRow, nRune);
				}
			}

			ProcMovePrev (ref col, ref row, rune);

			if (fromCol != col || fromRow != row) {
				return (col, row);
			}

			return null;
		} catch (Exception) {
			return null;
		}
	}

	/// <summary>
	///         Converts the string into a <see cref="List{RuneCell}" />.
	/// </summary>
	/// <param name="str">The string to convert.</param>
	/// <param name="colorScheme">The <see cref="ColorScheme" /> to use.</param>
	/// <returns></returns>
	public static List<RuneCell> ToRuneCellList (string str, ColorScheme? colorScheme = null)
	{
		var cells = new List<RuneCell> ();
		foreach (var rune in str.EnumerateRunes ()) {
			cells.Add (new RuneCell { Rune = rune, ColorScheme = colorScheme });
		}

		return cells;
	}

	/// <summary>
	///         Converts a <see cref="RuneCell" /> generic collection into a string.
	/// </summary>
	/// <param name="cells">The enumerable cell to convert.</param>
	/// <returns></returns>
	public static string ToString (IEnumerable<RuneCell> cells)
	{
		var str = string.Empty;

		foreach (var cell in cells) {
			str += cell.Rune.ToString ();
		}

		return str;
	}

	enum RuneType {
		IsSymbol,
		IsWhiteSpace,
		IsLetterOrDigit,
		IsPunctuation,
		IsUnknow
	}
}

partial class HistoryText {
	public enum LineStatus {
		Original,
		Replaced,
		Removed,
		Added
	}

	readonly List<HistoryTextItem> _historyTextItems = new ();
	int _idxHistoryText = -1;
	string? _originalText;

	public bool IsFromHistory { get; private set; }

	public bool HasHistoryChanges => _idxHistoryText > -1;

	public event EventHandler<HistoryTextItem>? ChangeText;

	public void Add (List<List<RuneCell>> lines, Point curPos, LineStatus lineStatus = LineStatus.Original)
	{
		if (lineStatus == LineStatus.Original && _historyTextItems.Count > 0 &&
		    _historyTextItems.Last ().LineStatus == LineStatus.Original) {
			return;
		}

		if (lineStatus == LineStatus.Replaced && _historyTextItems.Count > 0 &&
		    _historyTextItems.Last ().LineStatus == LineStatus.Replaced) {
			return;
		}

		if (_historyTextItems.Count == 0 && lineStatus != LineStatus.Original) {
			throw new ArgumentException ("The first item must be the original.");
		}

		if (_idxHistoryText >= 0 && _idxHistoryText + 1 < _historyTextItems.Count) {
			_historyTextItems.RemoveRange (_idxHistoryText + 1,
				_historyTextItems.Count - _idxHistoryText - 1);
		}

		_historyTextItems.Add (new HistoryTextItem (lines, curPos, lineStatus));
		_idxHistoryText++;
	}

	public void ReplaceLast (List<List<RuneCell>> lines, Point curPos, LineStatus lineStatus)
	{
		var found = _historyTextItems.FindLast (x => x.LineStatus == lineStatus);
		if (found != null) {
			found.Lines = lines;
			found.CursorPosition = curPos;
		}
	}

	public void Undo ()
	{
		if (_historyTextItems?.Count > 0 && _idxHistoryText > 0) {
			IsFromHistory = true;

			_idxHistoryText--;

			var historyTextItem = new HistoryTextItem (_historyTextItems [_idxHistoryText]) {
				IsUndoing = true
			};

			ProcessChanges (ref historyTextItem);

			IsFromHistory = false;
		}
	}

	public void Redo ()
	{
		if (_historyTextItems?.Count > 0 && _idxHistoryText < _historyTextItems.Count - 1) {
			IsFromHistory = true;

			_idxHistoryText++;

			var historyTextItem = new HistoryTextItem (_historyTextItems [_idxHistoryText]) {
				IsUndoing = false
			};

			ProcessChanges (ref historyTextItem);

			IsFromHistory = false;
		}
	}

	void ProcessChanges (ref HistoryTextItem historyTextItem)
	{
		if (historyTextItem.IsUndoing) {
			if (_idxHistoryText - 1 > -1 &&
			    (_historyTextItems [_idxHistoryText - 1].LineStatus == LineStatus.Added ||
			     _historyTextItems [_idxHistoryText - 1].LineStatus == LineStatus.Removed ||
			     (historyTextItem.LineStatus == LineStatus.Replaced &&
			      _historyTextItems [_idxHistoryText - 1].LineStatus == LineStatus.Original))) {
				_idxHistoryText--;

				while (_historyTextItems [_idxHistoryText].LineStatus == LineStatus.Added &&
				       _historyTextItems [_idxHistoryText - 1].LineStatus == LineStatus.Removed) {
					_idxHistoryText--;
				}

				historyTextItem = new HistoryTextItem (_historyTextItems [_idxHistoryText]);
				historyTextItem.IsUndoing = true;
				historyTextItem.FinalCursorPosition = historyTextItem.CursorPosition;
			}

			if (historyTextItem.LineStatus == LineStatus.Removed &&
			    _historyTextItems [_idxHistoryText + 1].LineStatus == LineStatus.Added) {
				historyTextItem.RemovedOnAdded =
					new HistoryTextItem (_historyTextItems [_idxHistoryText + 1]);
			}

			if ((historyTextItem.LineStatus == LineStatus.Added &&
			     _historyTextItems [_idxHistoryText - 1].LineStatus == LineStatus.Original) ||
			    (historyTextItem.LineStatus == LineStatus.Removed &&
			     _historyTextItems [_idxHistoryText - 1].LineStatus == LineStatus.Original) ||
			    (historyTextItem.LineStatus == LineStatus.Added &&
			     _historyTextItems [_idxHistoryText - 1].LineStatus == LineStatus.Removed)) {
				if (!historyTextItem.Lines [0]
					    .SequenceEqual (_historyTextItems [_idxHistoryText - 1].Lines [0]) &&
				    historyTextItem.CursorPosition ==
				    _historyTextItems [_idxHistoryText - 1].CursorPosition) {
					historyTextItem.Lines [0] =
						new List<RuneCell> (_historyTextItems [_idxHistoryText - 1].Lines [0]);
				}

				if (historyTextItem.LineStatus == LineStatus.Added &&
				    _historyTextItems [_idxHistoryText - 1].LineStatus == LineStatus.Removed) {
					historyTextItem.FinalCursorPosition =
						_historyTextItems [_idxHistoryText - 2].CursorPosition;
				} else {
					historyTextItem.FinalCursorPosition =
						_historyTextItems [_idxHistoryText - 1].CursorPosition;
				}
			} else {
				historyTextItem.FinalCursorPosition = historyTextItem.CursorPosition;
			}

			OnChangeText (historyTextItem);
			while (_historyTextItems [_idxHistoryText].LineStatus == LineStatus.Removed ||
			       _historyTextItems [_idxHistoryText].LineStatus == LineStatus.Added) {
				_idxHistoryText--;
			}
		} else if (!historyTextItem.IsUndoing) {
			if (_idxHistoryText + 1 < _historyTextItems.Count &&
			    (historyTextItem.LineStatus == LineStatus.Original ||
			     _historyTextItems [_idxHistoryText + 1].LineStatus == LineStatus.Added ||
			     _historyTextItems [_idxHistoryText + 1].LineStatus == LineStatus.Removed)) {
				_idxHistoryText++;
				historyTextItem = new HistoryTextItem (_historyTextItems [_idxHistoryText]);
				historyTextItem.IsUndoing = false;
				historyTextItem.FinalCursorPosition = historyTextItem.CursorPosition;
			}

			if (historyTextItem.LineStatus == LineStatus.Added &&
			    _historyTextItems [_idxHistoryText - 1].LineStatus == LineStatus.Removed) {
				historyTextItem.RemovedOnAdded =
					new HistoryTextItem (_historyTextItems [_idxHistoryText - 1]);
			}

			if ((historyTextItem.LineStatus == LineStatus.Removed &&
			     _historyTextItems [_idxHistoryText + 1].LineStatus == LineStatus.Replaced) ||
			    (historyTextItem.LineStatus == LineStatus.Removed &&
			     _historyTextItems [_idxHistoryText + 1].LineStatus == LineStatus.Original) ||
			    (historyTextItem.LineStatus == LineStatus.Added &&
			     _historyTextItems [_idxHistoryText + 1].LineStatus == LineStatus.Replaced)) {
				if (historyTextItem.LineStatus == LineStatus.Removed && !historyTextItem.Lines [0]
					    .SequenceEqual (_historyTextItems [_idxHistoryText + 1].Lines [0])) {
					historyTextItem.Lines [0] =
						new List<RuneCell> (_historyTextItems [_idxHistoryText + 1].Lines [0]);
				}

				historyTextItem.FinalCursorPosition =
					_historyTextItems [_idxHistoryText + 1].CursorPosition;
			} else {
				historyTextItem.FinalCursorPosition = historyTextItem.CursorPosition;
			}

			OnChangeText (historyTextItem);
			while (_historyTextItems [_idxHistoryText].LineStatus == LineStatus.Removed ||
			       _historyTextItems [_idxHistoryText].LineStatus == LineStatus.Added) {
				_idxHistoryText++;
			}
		}
	}

	void OnChangeText (HistoryTextItem? lines) => ChangeText?.Invoke (this, lines!);

	public void Clear (string text)
	{
		_historyTextItems.Clear ();
		_idxHistoryText = -1;
		_originalText = text;
		OnChangeText (null);
	}

	public bool IsDirty (string text) => _originalText != text;
}

class WordWrapManager {
	int _frameWidth;
	bool _isWrapModelRefreshing;

	List<WrappedLine> _wrappedModelLines = new ();

	public WordWrapManager (TextModel model) => Model = model;

	public TextModel Model { get; private set; }

	public TextModel WrapModel (int width,
		out int nRow,
		out int nCol,
		out int nStartRow,
		out int nStartCol,
		int row = 0,
		int col = 0,
		int startRow = 0,
		int startCol = 0,
		int tabWidth = 0,
		bool preserveTrailingSpaces = true)
	{
		_frameWidth = width;

		var modelRow = _isWrapModelRefreshing ? row : GetModelLineFromWrappedLines (row);
		var modelCol = _isWrapModelRefreshing ? col : GetModelColFromWrappedLines (row, col);
		var modelStartRow = _isWrapModelRefreshing ? startRow : GetModelLineFromWrappedLines (startRow);
		var modelStartCol =
			_isWrapModelRefreshing ? startCol : GetModelColFromWrappedLines (startRow, startCol);
		var wrappedModel = new TextModel ();
		var lines = 0;
		nRow = 0;
		nCol = 0;
		nStartRow = 0;
		nStartCol = 0;
		var isRowAndColSetted = row == 0 && col == 0;
		var isStartRowAndColSetted = startRow == 0 && startCol == 0;
		var wModelLines = new List<WrappedLine> ();

		for (var i = 0; i < Model.Count; i++) {
			var line = Model.GetLine (i);
			var wrappedLines = ToListRune (
				TextFormatter.Format (TextModel.ToString (line), width, TextAlignment.Left, true,
					preserveTrailingSpaces, tabWidth));
			var sumColWidth = 0;
			for (var j = 0; j < wrappedLines.Count; j++) {
				var wrapLine = wrappedLines [j];
				if (!isRowAndColSetted && modelRow == i) {
					if (nCol + wrapLine.Count <= modelCol) {
						nCol += wrapLine.Count;
						nRow = lines;
						if (nCol == modelCol) {
							nCol = wrapLine.Count;
							isRowAndColSetted = true;
						} else if (j == wrappedLines.Count - 1) {
							nCol = wrapLine.Count - j + modelCol - nCol;
							isRowAndColSetted = true;
						}
					} else {
						var offset = nCol + wrapLine.Count - modelCol;
						nCol = wrapLine.Count - offset;
						nRow = lines;
						isRowAndColSetted = true;
					}
				}

				if (!isStartRowAndColSetted && modelStartRow == i) {
					if (nStartCol + wrapLine.Count <= modelStartCol) {
						nStartCol += wrapLine.Count;
						nStartRow = lines;
						if (nStartCol == modelStartCol) {
							nStartCol = wrapLine.Count;
							isStartRowAndColSetted = true;
						} else if (j == wrappedLines.Count - 1) {
							nStartCol = wrapLine.Count - j + modelStartCol - nStartCol;
							isStartRowAndColSetted = true;
						}
					} else {
						var offset = nStartCol + wrapLine.Count - modelStartCol;
						nStartCol = wrapLine.Count - offset;
						nStartRow = lines;
						isStartRowAndColSetted = true;
					}
				}

				for (var k = j; k < wrapLine.Count; k++) {
					wrapLine [k].ColorScheme = line [k].ColorScheme;
				}

				wrappedModel.AddLine (lines, wrapLine);
				sumColWidth += wrapLine.Count;
				var wrappedLine = new WrappedLine {
					ModelLine = i,
					Row = lines,
					RowIndex = j,
					ColWidth = wrapLine.Count
				};
				wModelLines.Add (wrappedLine);
				lines++;
			}
		}

		_wrappedModelLines = wModelLines;

		return wrappedModel;
	}

	public List<List<RuneCell>> ToListRune (List<string> textList)
	{
		var runesList = new List<List<RuneCell>> ();

		foreach (var text in textList) {
			runesList.Add (TextModel.ToRuneCellList (text));
		}

		return runesList;
	}

	public int GetModelLineFromWrappedLines (int line) => _wrappedModelLines.Count > 0
		? _wrappedModelLines [Math.Min (line, _wrappedModelLines.Count - 1)].ModelLine
		: 0;

	public int GetModelColFromWrappedLines (int line, int col)
	{
		if (_wrappedModelLines?.Count == 0) {
			return 0;
		}

		var modelLine = GetModelLineFromWrappedLines (line);
		var firstLine = _wrappedModelLines.IndexOf (r => r.ModelLine == modelLine);
		var modelCol = 0;

		for (var i = firstLine; i <= Math.Min (line, _wrappedModelLines!.Count - 1); i++) {
			var wLine = _wrappedModelLines [i];

			if (i < line) {
				modelCol += wLine.ColWidth;
			} else {
				modelCol += col;
			}
		}

		return modelCol;
	}

	List<RuneCell> GetCurrentLine (int row) => Model.GetLine (row);

	public void AddLine (int row, int col)
	{
		var modelRow = GetModelLineFromWrappedLines (row);
		var modelCol = GetModelColFromWrappedLines (row, col);
		var line = GetCurrentLine (modelRow);
		var restCount = line.Count - modelCol;
		var rest = line.GetRange (modelCol, restCount);
		line.RemoveRange (modelCol, restCount);
		Model.AddLine (modelRow + 1, rest);
		_isWrapModelRefreshing = true;
		WrapModel (_frameWidth, out _, out _, out _, out _, modelRow + 1);
		_isWrapModelRefreshing = false;
	}

	public bool Insert (int row, int col, RuneCell cell)
	{
		var line = GetCurrentLine (GetModelLineFromWrappedLines (row));
		line.Insert (GetModelColFromWrappedLines (row, col), cell);
		if (line.Count > _frameWidth) {
			return true;
		}

		return false;
	}

	public bool RemoveAt (int row, int col)
	{
		var modelRow = GetModelLineFromWrappedLines (row);
		var line = GetCurrentLine (modelRow);
		var modelCol = GetModelColFromWrappedLines (row, col);

		if (modelCol > line.Count) {
			Model.RemoveLine (modelRow);
			RemoveAt (row, 0);
			return false;
		}

		if (modelCol < line.Count) {
			line.RemoveAt (modelCol);
		}

		if (line.Count > _frameWidth || (row + 1 < _wrappedModelLines.Count &&
						 _wrappedModelLines [row + 1].ModelLine == modelRow)) {
			return true;
		}

		return false;
	}

	public bool RemoveLine (int row, int col, out bool lineRemoved, bool forward = true)
	{
		lineRemoved = false;
		var modelRow = GetModelLineFromWrappedLines (row);
		var line = GetCurrentLine (modelRow);
		var modelCol = GetModelColFromWrappedLines (row, col);

		if (modelCol == 0 && line.Count == 0) {
			Model.RemoveLine (modelRow);
			return false;
		}

		if (modelCol < line.Count) {
			if (forward) {
				line.RemoveAt (modelCol);
				return true;
			}

			if (modelCol - 1 > -1) {
				line.RemoveAt (modelCol - 1);
				return true;
			}
		}

		lineRemoved = true;
		if (forward) {
			if (modelRow + 1 == Model.Count) {
				return false;
			}

			var nextLine = Model.GetLine (modelRow + 1);
			line.AddRange (nextLine);
			Model.RemoveLine (modelRow + 1);
			if (line.Count > _frameWidth) {
				return true;
			}
		} else {
			if (modelRow == 0) {
				return false;
			}

			var prevLine = Model.GetLine (modelRow - 1);
			prevLine.AddRange (line);
			Model.RemoveLine (modelRow);
			if (prevLine.Count > _frameWidth) {
				return true;
			}
		}

		return false;
	}

	public bool RemoveRange (int row, int index, int count)
	{
		var modelRow = GetModelLineFromWrappedLines (row);
		var line = GetCurrentLine (modelRow);
		var modelCol = GetModelColFromWrappedLines (row, index);

		try {
			line.RemoveRange (modelCol, count);
		} catch (Exception) {
			return false;
		}

		return true;
	}

	public void UpdateModel (TextModel model,
		out int nRow,
		out int nCol,
		out int nStartRow,
		out int nStartCol,
		int row,
		int col,
		int startRow,
		int startCol,
		bool preserveTrailingSpaces)
	{
		_isWrapModelRefreshing = true;
		Model = model;
		WrapModel (_frameWidth, out nRow, out nCol, out nStartRow, out nStartCol, row, col, startRow, startCol,
			0, preserveTrailingSpaces);
		_isWrapModelRefreshing = false;
	}

	public int GetWrappedLineColWidth (int line, int col, WordWrapManager wrapManager)
	{
		if (_wrappedModelLines?.Count == 0) {
			return 0;
		}

		var wModelLines = wrapManager._wrappedModelLines;
		var modelLine = GetModelLineFromWrappedLines (line);
		var firstLine = _wrappedModelLines.IndexOf (r => r.ModelLine == modelLine);
		var modelCol = 0;
		var colWidthOffset = 0;
		var i = firstLine;

		while (modelCol < col) {
			var wLine = _wrappedModelLines! [i];
			var wLineToCompare = wModelLines [i];

			if (wLine.ModelLine != modelLine || wLineToCompare.ModelLine != modelLine) {
				break;
			}

			modelCol += Math.Max (wLine.ColWidth, wLineToCompare.ColWidth);
			colWidthOffset += wLine.ColWidth - wLineToCompare.ColWidth;
			if (modelCol > col) {
				modelCol += col - modelCol;
			}

			i++;
		}

		return modelCol - colWidthOffset;
	}

	class WrappedLine {
		public int ColWidth;
		public int ModelLine;
		public int Row;
		public int RowIndex;
	}
}

/// <summary>
///         Multi-line text editing <see cref="View" />.
/// </summary>
/// <remarks>
///         <para>
///                 <see cref="TextView" /> provides a multi-line text editor. Users interact
///                 with it with the standard Windows, Mac, and Linux (Emacs) commands.
///         </para>
///         <list type="table">
///                 <listheader>
///                         <term>Shortcut</term>
///                         <description>Action performed</description>
///                 </listheader>
///                 <item>
///                         <term>Left cursor, Control-b</term>
///                         <description>
///                                 Moves the editing point left.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Right cursor, Control-f</term>
///                         <description>
///                                 Moves the editing point right.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Alt-b</term>
///                         <description>
///                                 Moves one word back.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Alt-f</term>
///                         <description>
///                                 Moves one word forward.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Up cursor, Control-p</term>
///                         <description>
///                                 Moves the editing point one line up.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Down cursor, Control-n</term>
///                         <description>
///                                 Moves the editing point one line down
///                         </description>
///                 </item>
///                 <item>
///                         <term>Home key, Control-a</term>
///                         <description>
///                                 Moves the cursor to the beginning of the line.
///                         </description>
///                 </item>
///                 <item>
///                         <term>End key, Control-e</term>
///                         <description>
///                                 Moves the cursor to the end of the line.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Control-Home</term>
///                         <description>
///                                 Scrolls to the first line and moves the cursor there.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Control-End</term>
///                         <description>
///                                 Scrolls to the last line and moves the cursor there.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Delete, Control-d</term>
///                         <description>
///                                 Deletes the character in front of the cursor.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Backspace</term>
///                         <description>
///                                 Deletes the character behind the cursor.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Control-k</term>
///                         <description>
///                                 Deletes the text until the end of the line and replaces the kill buffer
///                                 with the deleted text. You can paste this text in a different place by
///                                 using Control-y.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Control-y</term>
///                         <description>
///                                 Pastes the content of the kill ring into the current position.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Alt-d</term>
///                         <description>
///                                 Deletes the word above the cursor and adds it to the kill ring. You
///                                 can paste the contents of the kill ring with Control-y.
///                         </description>
///                 </item>
///                 <item>
///                         <term>Control-q</term>
///                         <description>
///                                 Quotes the next input character, to prevent the normal processing of
///                                 key handling to take place.
///                         </description>
///                 </item>
///         </list>
/// </remarks>
public class TextView : View {
	readonly HistoryText _historyText = new ();
	bool _allowsReturn = true;
	bool _allowsTab = true;
	int _bottomOffset, _rightOffset;
	bool _clickWithSelecting;

	// The column we are tracking, or -1 if we are not tracking any column
	int _columnTrack = -1;
	bool _continuousFind;

	bool _copyWithoutSelection;

	string? _currentCaller;
	CultureInfo? _currentCulture;

	CursorVisibility _desiredCursorVisibility = CursorVisibility.Default;

	bool _isButtonShift;
	bool _isDrawing;
	bool _isReadOnly;

	bool _lastWasKill;
	int _leftColumn;
	TextModel _model = new ();
	bool _multiline = true;

	CursorVisibility _savedCursorVisibility;

	Dim? _savedHeight;
	int _selectionStartColumn, _selectionStartRow;
	bool _shiftSelecting;
	int _tabWidth = 4;
	int _topRow;
	bool _wordWrap;
	WordWrapManager? _wrapManager;
	bool _wrapNeeded;

	/// <summary>
	///         Initializes a <see cref="TextView" /> on the specified area,
	///         with dimensions controlled with the X, Y, Width and Height properties.
	/// </summary>
	public TextView ()
	{
		CanFocus = true;
		Used = true;

		_model.LinesLoaded += Model_LinesLoaded!;
		_historyText.ChangeText += HistoryText_ChangeText!;

		Initialized += TextView_Initialized!;

		LayoutComplete += TextView_LayoutComplete;

		// Things this view knows how to do
		AddCommand (Command.PageDown, () => {
			ProcessPageDown ();
			return true;
		});
		AddCommand (Command.PageDownExtend, () => {
			ProcessPageDownExtend ();
			return true;
		});
		AddCommand (Command.PageUp, () => {
			ProcessPageUp ();
			return true;
		});
		AddCommand (Command.PageUpExtend, () => {
			ProcessPageUpExtend ();
			return true;
		});
		AddCommand (Command.LineDown, () => {
			ProcessMoveDown ();
			return true;
		});
		AddCommand (Command.LineDownExtend, () => {
			ProcessMoveDownExtend ();
			return true;
		});
		AddCommand (Command.LineUp, () => {
			ProcessMoveUp ();
			return true;
		});
		AddCommand (Command.LineUpExtend, () => {
			ProcessMoveUpExtend ();
			return true;
		});
		AddCommand (Command.Right, () => ProcessMoveRight ());
		AddCommand (Command.RightExtend, () => {
			ProcessMoveRightExtend ();
			return true;
		});
		AddCommand (Command.Left, () => ProcessMoveLeft ());
		AddCommand (Command.LeftExtend, () => {
			ProcessMoveLeftExtend ();
			return true;
		});
		AddCommand (Command.DeleteCharLeft, () => {
			ProcessDeleteCharLeft ();
			return true;
		});
		AddCommand (Command.StartOfLine, () => {
			ProcessMoveStartOfLine ();
			return true;
		});
		AddCommand (Command.StartOfLineExtend, () => {
			ProcessMoveStartOfLineExtend ();
			return true;
		});
		AddCommand (Command.DeleteCharRight, () => {
			ProcessDeleteCharRight ();
			return true;
		});
		AddCommand (Command.EndOfLine, () => {
			ProcessMoveEndOfLine ();
			return true;
		});
		AddCommand (Command.EndOfLineExtend, () => {
			ProcessMoveEndOfLineExtend ();
			return true;
		});
		AddCommand (Command.CutToEndLine, () => {
			KillToEndOfLine ();
			return true;
		});
		AddCommand (Command.CutToStartLine, () => {
			KillToStartOfLine ();
			return true;
		});
		AddCommand (Command.Paste, () => {
			ProcessPaste ();
			return true;
		});
		AddCommand (Command.ToggleExtend, () => {
			ToggleSelecting ();
			return true;
		});
		AddCommand (Command.Copy, () => {
			ProcessCopy ();
			return true;
		});
		AddCommand (Command.Cut, () => {
			ProcessCut ();
			return true;
		});
		AddCommand (Command.WordLeft, () => {
			ProcessMoveWordBackward ();
			return true;
		});
		AddCommand (Command.WordLeftExtend, () => {
			ProcessMoveWordBackwardExtend ();
			return true;
		});
		AddCommand (Command.WordRight, () => {
			ProcessMoveWordForward ();
			return true;
		});
		AddCommand (Command.WordRightExtend, () => {
			ProcessMoveWordForwardExtend ();
			return true;
		});
		AddCommand (Command.KillWordForwards, () => {
			ProcessKillWordForward ();
			return true;
		});
		AddCommand (Command.KillWordBackwards, () => {
			ProcessKillWordBackward ();
			return true;
		});
		AddCommand (Command.NewLine, () => ProcessReturn ());
		AddCommand (Command.BottomEnd, () => {
			MoveBottomEnd ();
			return true;
		});
		AddCommand (Command.BottomEndExtend, () => {
			MoveBottomEndExtend ();
			return true;
		});
		AddCommand (Command.TopHome, () => {
			MoveTopHome ();
			return true;
		});
		AddCommand (Command.TopHomeExtend, () => {
			MoveTopHomeExtend ();
			return true;
		});
		AddCommand (Command.SelectAll, () => {
			ProcessSelectAll ();
			return true;
		});
		AddCommand (Command.ToggleOverwrite, () => {
			ProcessSetOverwrite ();
			return true;
		});
		AddCommand (Command.EnableOverwrite, () => {
			SetOverwrite (true);
			return true;
		});
		AddCommand (Command.DisableOverwrite, () => {
			SetOverwrite (false);
			return true;
		});
		AddCommand (Command.Tab, () => ProcessTab ());
		AddCommand (Command.BackTab, () => ProcessBackTab ());
		AddCommand (Command.NextView, () => ProcessMoveNextView ());
		AddCommand (Command.PreviousView, () => ProcessMovePreviousView ());
		AddCommand (Command.Undo, () => {
			Undo ();
			return true;
		});
		AddCommand (Command.Redo, () => {
			Redo ();
			return true;
		});
		AddCommand (Command.DeleteAll, () => {
			DeleteAll ();
			return true;
		});
		AddCommand (Command.ShowContextMenu, () => {
			ContextMenu!.Position = new Point (CursorPosition.X - _leftColumn + 2,
				CursorPosition.Y - _topRow + 2);
			ShowContextMenu ();
			return true;
		});

		// Default keybindings for this view
		KeyBindings.Add (KeyCode.PageDown, Command.PageDown);
		KeyBindings.Add (KeyCode.V | KeyCode.CtrlMask, Command.PageDown);

		KeyBindings.Add (KeyCode.PageDown | KeyCode.ShiftMask, Command.PageDownExtend);

		KeyBindings.Add (KeyCode.PageUp, Command.PageUp);
		KeyBindings.Add ('V' + KeyCode.AltMask, Command.PageUp);

		KeyBindings.Add (KeyCode.PageUp | KeyCode.ShiftMask, Command.PageUpExtend);

		KeyBindings.Add (KeyCode.N | KeyCode.CtrlMask, Command.LineDown);
		KeyBindings.Add (KeyCode.CursorDown, Command.LineDown);

		KeyBindings.Add (KeyCode.CursorDown | KeyCode.ShiftMask, Command.LineDownExtend);

		KeyBindings.Add (KeyCode.P | KeyCode.CtrlMask, Command.LineUp);
		KeyBindings.Add (KeyCode.CursorUp, Command.LineUp);

		KeyBindings.Add (KeyCode.CursorUp | KeyCode.ShiftMask, Command.LineUpExtend);

		KeyBindings.Add (KeyCode.F | KeyCode.CtrlMask, Command.Right);
		KeyBindings.Add (KeyCode.CursorRight, Command.Right);

		KeyBindings.Add (KeyCode.CursorRight | KeyCode.ShiftMask, Command.RightExtend);

		KeyBindings.Add (KeyCode.B | KeyCode.CtrlMask, Command.Left);
		KeyBindings.Add (KeyCode.CursorLeft, Command.Left);

		KeyBindings.Add (KeyCode.CursorLeft | KeyCode.ShiftMask, Command.LeftExtend);

		KeyBindings.Add (KeyCode.Backspace, Command.DeleteCharLeft);

		KeyBindings.Add (KeyCode.Home, Command.StartOfLine);
		KeyBindings.Add (KeyCode.A | KeyCode.CtrlMask, Command.StartOfLine);

		KeyBindings.Add (KeyCode.Home | KeyCode.ShiftMask, Command.StartOfLineExtend);

		KeyBindings.Add (KeyCode.Delete, Command.DeleteCharRight);
		KeyBindings.Add (KeyCode.D | KeyCode.CtrlMask, Command.DeleteCharRight);

		KeyBindings.Add (KeyCode.End, Command.EndOfLine);
		KeyBindings.Add (KeyCode.E | KeyCode.CtrlMask, Command.EndOfLine);

		KeyBindings.Add (KeyCode.End | KeyCode.ShiftMask, Command.EndOfLineExtend);

		KeyBindings.Add (KeyCode.K | KeyCode.CtrlMask, Command.CutToEndLine); // kill-to-end
		KeyBindings.Add (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.ShiftMask,
			Command.CutToEndLine); // kill-to-end

		KeyBindings.Add (KeyCode.K | KeyCode.AltMask, Command.CutToStartLine); // kill-to-start
		KeyBindings.Add (KeyCode.Backspace | KeyCode.CtrlMask | KeyCode.ShiftMask,
			Command.CutToStartLine); // kill-to-start

		KeyBindings.Add (KeyCode.Y | KeyCode.CtrlMask, Command.Paste); // Control-y, yank
		KeyBindings.Add (KeyCode.Space | KeyCode.CtrlMask, Command.ToggleExtend);

		KeyBindings.Add ('C' + KeyCode.AltMask, Command.Copy);
		KeyBindings.Add (KeyCode.C | KeyCode.CtrlMask, Command.Copy);

		KeyBindings.Add ('W' + KeyCode.AltMask, Command.Cut);
		KeyBindings.Add (KeyCode.W | KeyCode.CtrlMask, Command.Cut);
		KeyBindings.Add (KeyCode.X | KeyCode.CtrlMask, Command.Cut);

		KeyBindings.Add (KeyCode.CursorLeft | KeyCode.CtrlMask, Command.WordLeft);
		KeyBindings.Add ('B' + KeyCode.AltMask, Command.WordLeft);

		KeyBindings.Add (KeyCode.CursorLeft | KeyCode.CtrlMask | KeyCode.ShiftMask, Command.WordLeftExtend);

		KeyBindings.Add (KeyCode.CursorRight | KeyCode.CtrlMask, Command.WordRight);
		KeyBindings.Add ('F' + KeyCode.AltMask, Command.WordRight);

		KeyBindings.Add (KeyCode.CursorRight | KeyCode.CtrlMask | KeyCode.ShiftMask, Command.WordRightExtend);
		KeyBindings.Add (KeyCode.Delete | KeyCode.CtrlMask, Command.KillWordForwards); // kill-word-forwards
		KeyBindings.Add (KeyCode.Backspace | KeyCode.CtrlMask,
			Command.KillWordBackwards); // kill-word-backwards

		// BUGBUG: If AllowsReturn is false, Key.Enter should not be bound (so that Toplevel can cause Command.Accept).
		KeyBindings.Add (KeyCode.Enter, Command.NewLine);
		KeyBindings.Add (KeyCode.End | KeyCode.CtrlMask, Command.BottomEnd);
		KeyBindings.Add (KeyCode.End | KeyCode.CtrlMask | KeyCode.ShiftMask, Command.BottomEndExtend);
		KeyBindings.Add (KeyCode.Home | KeyCode.CtrlMask, Command.TopHome);
		KeyBindings.Add (KeyCode.Home | KeyCode.CtrlMask | KeyCode.ShiftMask, Command.TopHomeExtend);
		KeyBindings.Add (KeyCode.T | KeyCode.CtrlMask, Command.SelectAll);
		KeyBindings.Add (KeyCode.Insert, Command.ToggleOverwrite);
		KeyBindings.Add (KeyCode.Tab, Command.Tab);
		KeyBindings.Add (KeyCode.Tab | KeyCode.ShiftMask, Command.BackTab);

		KeyBindings.Add (KeyCode.Tab | KeyCode.CtrlMask, Command.NextView);
		KeyBindings.Add ((KeyCode)Application.AlternateForwardKey, Command.NextView);

		KeyBindings.Add (KeyCode.Tab | KeyCode.CtrlMask | KeyCode.ShiftMask, Command.PreviousView);
		KeyBindings.Add ((KeyCode)Application.AlternateBackwardKey, Command.PreviousView);

		KeyBindings.Add (KeyCode.Z | KeyCode.CtrlMask, Command.Undo);
		KeyBindings.Add (KeyCode.R | KeyCode.CtrlMask, Command.Redo);

		KeyBindings.Add (KeyCode.G | KeyCode.CtrlMask, Command.DeleteAll);
		KeyBindings.Add (KeyCode.D | KeyCode.CtrlMask | KeyCode.ShiftMask, Command.DeleteAll);

		_currentCulture = Thread.CurrentThread.CurrentUICulture;

		ContextMenu = new ContextMenu { MenuItems = BuildContextMenuBarItem () };
		ContextMenu.KeyChanged += ContextMenu_KeyChanged!;

		KeyBindings.Add ((KeyCode)ContextMenu.Key, KeyBindingScope.HotKey, Command.ShowContextMenu);
	}

	/// <summary>
	///         Provides autocomplete context menu based on suggestions at the current cursor
	///         position. Configure <see cref="IAutocomplete.SuggestionGenerator" /> to enable this feature
	/// </summary>
	public IAutocomplete Autocomplete { get; protected set; } = new TextViewAutocomplete ();

	/// <summary>
	///         Tracks whether the text view should be considered "used", that is, that the user has moved in the entry,
	///         so new input should be appended at the cursor position, rather than clearing the entry
	/// </summary>
	public bool Used { get; set; }

	/// <summary>
	///         Sets or gets the text in the <see cref="TextView" />.
	/// </summary>
	/// <remarks>
	///         The <see cref="TextChanged" /> event is fired whenever this property is set. Note, however,
	///         that Text is not set by <see cref="TextView" /> as the user types.
	/// </remarks>
	public override string Text {
		get {
			if (_wordWrap) {
				return _wrapManager!.Model.ToString ();
			}

			return _model.ToString ();
		}

		set {
			ResetPosition ();
			_model.LoadString (value);
			if (_wordWrap) {
				_wrapManager = new WordWrapManager (_model);
				_model = _wrapManager.WrapModel (_frameWidth, out _, out _, out _, out _);
			}

			TextChanged?.Invoke (this, EventArgs.Empty);
			SetNeedsDisplay ();

			_historyText.Clear (Text);
		}
	}

	int _frameWidth => Math.Max (Frame.Width - (RightOffset != 0 ? 2 : 1), 0);

	/// <summary>
	///         Gets or sets the top row.
	/// </summary>
	public int TopRow { get => _topRow; set => _topRow = Math.Max (Math.Min (value, Lines - 1), 0); }

	/// <summary>
	///         Gets or sets the left column.
	/// </summary>
	public int LeftColumn {
		get => _leftColumn;
		set {
			if (value > 0 && _wordWrap) {
				return;
			}

			_leftColumn = Math.Max (Math.Min (value, Maxlength - 1), 0);
		}
	}

	/// <summary>
	///         Gets the maximum visible length line.
	/// </summary>
	public int Maxlength => _model.GetMaxVisibleLine (_topRow, _topRow + Frame.Height, TabWidth);

	/// <summary>
	///         Gets the number of lines.
	/// </summary>
	public int Lines => _model.Count;

	/// <summary>
	///         Sets or gets the current cursor position.
	/// </summary>
	public Point CursorPosition {
		get => new (CurrentColumn, CurrentRow);
		set {
			var line = _model.GetLine (Math.Max (Math.Min (value.Y, _model.Count - 1), 0));
			CurrentColumn = value.X < 0 ? 0 : value.X > line.Count ? line.Count : value.X;
			CurrentRow = value.Y < 0
				? 0
				: value.Y > _model.Count - 1
					? Math.Max (_model.Count - 1, 0)
					: value.Y;
			SetNeedsDisplay ();
			Adjust ();
		}
	}

	/// <summary>
	///         Start column position of the selected text.
	/// </summary>
	public int SelectionStartColumn {
		get => _selectionStartColumn;
		set {
			var line = _model.GetLine (_selectionStartRow);
			_selectionStartColumn = value < 0 ? 0 : value > line.Count ? line.Count : value;
			Selecting = true;
			SetNeedsDisplay ();
			Adjust ();
		}
	}

	/// <summary>
	///         Start row position of the selected text.
	/// </summary>
	public int SelectionStartRow {
		get => _selectionStartRow;
		set {
			_selectionStartRow = value < 0
				? 0
				: value > _model.Count - 1
					? Math.Max (_model.Count - 1, 0)
					: value;
			Selecting = true;
			SetNeedsDisplay ();
			Adjust ();
		}
	}

	/// <summary>
	///         The selected text.
	/// </summary>
	public string SelectedText {
		get {
			if (!Selecting || (_model.Count == 1 && _model.GetLine (0).Count == 0)) {
				return string.Empty;
			}

			return GetSelectedRegion ();
		}
	}

	/// <summary>
	///         Length of the selected text.
	/// </summary>
	public int SelectedLength => GetSelectedLength ();

	/// <summary>
	///         Get or sets the selecting.
	/// </summary>
	public bool Selecting { get; set; }

	/// <summary>
	///         Allows word wrap the to fit the available container width.
	/// </summary>
	public bool WordWrap {
		get => _wordWrap;
		set {
			if (value == _wordWrap) {
				return;
			}

			if (value && !_multiline) {
				return;
			}

			_wordWrap = value;
			ResetPosition ();
			if (_wordWrap) {
				_wrapManager = new WordWrapManager (_model);
				_model = _wrapManager.WrapModel (_frameWidth, out _, out _, out _, out _);
			} else if (!_wordWrap && _wrapManager != null) {
				_model = _wrapManager.Model;
			}

			SetNeedsDisplay ();
		}
	}

	/// <summary>
	///         The bottom offset needed to use a horizontal scrollbar or for another reason.
	///         This is only needed with the keyboard navigation.
	/// </summary>
	public int BottomOffset {
		get => _bottomOffset;
		set {
			if (CurrentRow == Lines - 1 && _bottomOffset > 0 && value == 0) {
				_topRow = Math.Max (_topRow - _bottomOffset, 0);
			}

			_bottomOffset = value;
			Adjust ();
		}
	}

	/// <summary>
	///         The right offset needed to use a vertical scrollbar or for another reason.
	///         This is only needed with the keyboard navigation.
	/// </summary>
	public int RightOffset {
		get => _rightOffset;
		set {
			if (!_wordWrap && CurrentColumn == GetCurrentLine ().Count && _rightOffset > 0 && value == 0) {
				_leftColumn = Math.Max (_leftColumn - _rightOffset, 0);
			}

			_rightOffset = value;
			Adjust ();
		}
	}

	/// <summary>
	///         Gets or sets a value indicating whether pressing ENTER in a <see cref="TextView" />
	///         creates a new line of text in the view or activates the default button for the Toplevel.
	/// </summary>
	public bool AllowsReturn {
		get => _allowsReturn;
		set {
			_allowsReturn = value;
			if (_allowsReturn && !_multiline) {
				Multiline = true;
			}

			if (!_allowsReturn && _multiline) {
				Multiline = false;
				AllowsTab = false;
			}

			SetNeedsDisplay ();
		}
	}

	/// <summary>
	///         Gets or sets whether the <see cref="TextView" /> inserts a tab character into the text or ignores
	///         tab input. If set to `false` and the user presses the tab key (or shift-tab) the focus will move to the
	///         next view (or previous with shift-tab). The default is `true`; if the user presses the tab key, a tab
	///         character will be inserted into the text.
	/// </summary>
	public bool AllowsTab {
		get => _allowsTab;
		set {
			_allowsTab = value;
			if (_allowsTab && _tabWidth == 0) {
				_tabWidth = 4;
			}

			if (_allowsTab && !_multiline) {
				Multiline = true;
			}

			if (!_allowsTab && _tabWidth > 0) {
				_tabWidth = 0;
			}

			SetNeedsDisplay ();
		}
	}

	/// <summary>
	///         Gets or sets a value indicating the number of whitespace when pressing the TAB key.
	/// </summary>
	public int TabWidth {
		get => _tabWidth;
		set {
			_tabWidth = Math.Max (value, 0);
			if (_tabWidth > 0 && !AllowsTab) {
				AllowsTab = true;
			}

			SetNeedsDisplay ();
		}
	}

	/// <summary>
	///         Gets or sets a value indicating whether this <see cref="TextView" /> is a multiline text view.
	/// </summary>
	public bool Multiline {
		get => _multiline;
		set {
			_multiline = value;
			if (_multiline && !_allowsTab) {
				AllowsTab = true;
			}

			if (_multiline && !_allowsReturn) {
				AllowsReturn = true;
			}

			if (!_multiline) {
				AllowsReturn = false;
				AllowsTab = false;
				WordWrap = false;
				CurrentColumn = 0;
				CurrentRow = 0;
				_savedHeight = Height;
				//var prevLayoutStyle = LayoutStyle;
				//if (LayoutStyle == LayoutStyle.Computed) {
				//	LayoutStyle = LayoutStyle.Absolute;
				//}
				Height = 1;
				//LayoutStyle = prevLayoutStyle;
				if (!IsInitialized) {
					_model.LoadString (Text);
				}

				SetNeedsDisplay ();
			} else if (_multiline && _savedHeight != null) {
				//var lyout = LayoutStyle;
				//if (LayoutStyle == LayoutStyle.Computed) {
				//	LayoutStyle = LayoutStyle.Absolute;
				//}
				Height = _savedHeight;
				//LayoutStyle = lyout;
				SetNeedsDisplay ();
			}
		}
	}

	/// <summary>
	///         Indicates whatever the text was changed or not.
	///         <see langword="true" /> if the text was changed <see langword="false" /> otherwise.
	/// </summary>
	public bool IsDirty {
		get => _historyText.IsDirty (Text);
		set => _historyText.Clear (Text);
	}

	/// <summary>
	///         Indicates whatever the text has history changes or not.
	///         <see langword="true" /> if the text has history changes <see langword="false" /> otherwise.
	/// </summary>
	public bool HasHistoryChanges => _historyText.HasHistoryChanges;

	/// <summary>
	///         Get the <see cref="ContextMenu" /> for this view.
	/// </summary>
	public ContextMenu? ContextMenu { get; }

	/// <summary>
	///         If <see langword="true" /> and the current <see cref="RuneCell.ColorScheme" /> is null
	///         will inherit from the previous, otherwise if <see langword="false" /> (default) do nothing.
	///         If the text is load with <see cref="Load(List{RuneCell})" /> this
	///         property is automatically sets to <see langword="true" />.
	/// </summary>
	public bool InheritsPreviousColorScheme { get; set; }

	/// <summary>
	///         Gets the current cursor row.
	/// </summary>
	public int CurrentRow { get; private set; }

	/// <summary>
	///         Gets the cursor column.
	/// </summary>
	/// <value>The cursor column.</value>
	public int CurrentColumn { get; private set; }

	/// <summary>
	///         Gets or sets whether the <see cref="TextView" /> is in read-only mode or not
	/// </summary>
	/// <value>Boolean value(Default false)</value>
	public bool ReadOnly {
		get => _isReadOnly;
		set {
			if (value != _isReadOnly) {
				_isReadOnly = value;

				SetNeedsDisplay ();
				Adjust ();
			}
		}
	}

	/// <summary>
	///         Get / Set the wished cursor when the field is focused
	/// </summary>
	public CursorVisibility DesiredCursorVisibility {
		get => _desiredCursorVisibility;
		set {
			if (HasFocus) {
				Application.Driver.SetCursorVisibility (value);
			}

			_desiredCursorVisibility = value;
			SetNeedsDisplay ();
		}
	}

	/// <inheritdoc />
	public override bool CanFocus {
		get => base.CanFocus;
		set => base.CanFocus = value;
	}

	/// <summary>
	///         Raised when the <see cref="Text" /> property of the <see cref="TextView" /> changes.
	/// </summary>
	/// <remarks>
	///         The <see cref="Text" /> property of <see cref="TextView" /> only changes when it is explicitly
	///         set, not as the user types. To be notified as the user changes the contents of the TextView
	///         see <see cref="IsDirty" />.
	/// </remarks>
	public event EventHandler? TextChanged;

	/// <summary>
	///         Raised when the contents of the <see cref="TextView" /> are changed.
	/// </summary>
	/// <remarks>
	///         Unlike the <see cref="TextChanged" /> event, this event is raised whenever the user types or
	///         otherwise changes the contents of the <see cref="TextView" />.
	/// </remarks>
	public event EventHandler<ContentsChangedEventArgs>? ContentsChanged;

	/// <summary>
	///         Invoked with the unwrapped <see cref="CursorPosition" />.
	/// </summary>
	public event EventHandler<PointEventArgs>? UnwrappedCursorPosition;

	/// <summary>
	///         Invoked when the normal color is drawn.
	/// </summary>
	public event EventHandler<RuneCellEventArgs>? DrawNormalColor;

	/// <summary>
	///         Invoked when the selection color is drawn.
	/// </summary>
	public event EventHandler<RuneCellEventArgs>? DrawSelectionColor;

	/// <summary>
	///         Invoked when the ready only color is drawn.
	/// </summary>
	public event EventHandler<RuneCellEventArgs>? DrawReadOnlyColor;

	/// <summary>
	///         Invoked when the used color is drawn. The Used Color is used to indicate
	///         if the <see cref="Key.InsertChar" /> was pressed and enabled.
	/// </summary>
	public event EventHandler<RuneCellEventArgs>? DrawUsedColor;

	void TextView_LayoutComplete (object? sender, LayoutEventArgs e)
	{
		WrapTextModel ();
		Adjust ();
	}

	MenuBarItem BuildContextMenuBarItem () => new (new MenuItem [] {
		new(Strings.ctxSelectAll, "", SelectAll, null, null,
			(KeyCode)KeyBindings.GetKeyFromCommands (Command.SelectAll)),
		new(Strings.ctxDeleteAll, "", DeleteAll, null, null,
			(KeyCode)KeyBindings.GetKeyFromCommands (Command.DeleteAll)),
		new(Strings.ctxCopy, "", Copy, null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Copy)),
		new(Strings.ctxCut, "", Cut, null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Cut)),
		new(Strings.ctxPaste, "", Paste, null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Paste)),
		new(Strings.ctxUndo, "", Undo, null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Undo)),
		new(Strings.ctxRedo, "", Redo, null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Redo))
	});

	void ContextMenu_KeyChanged (object sender, KeyChangedEventArgs e) =>
		KeyBindings.Replace ((KeyCode)e.OldKey, (KeyCode)e.NewKey);

	void Model_LinesLoaded (object sender, EventArgs e)
	{
		// This call is not needed. Model_LinesLoaded gets invoked when
		// model.LoadString (value) is called. LoadString is called from one place
		// (Text.set) and historyText.Clear() is called immediately after.
		// If this call happens, HistoryText_ChangeText will get called multiple times
		// when Text is set, which is wrong.
		//historyText.Clear (Text);

		if (!_multiline && !IsInitialized) {
			CurrentColumn = Text.GetRuneCount ();
			_leftColumn = CurrentColumn > Frame.Width + 1 ? CurrentColumn - Frame.Width + 1 : 0;
		}
	}

	void HistoryText_ChangeText (object sender, HistoryText.HistoryTextItem obj)
	{
		SetWrapModel ();

		if (obj != null) {
			var startLine = obj.CursorPosition.Y;

			if (obj.RemovedOnAdded != null) {
				int offset;
				if (obj.IsUndoing) {
					offset = Math.Max (obj.RemovedOnAdded.Lines.Count - obj.Lines.Count, 1);
				} else {
					offset = obj.RemovedOnAdded.Lines.Count - 1;
				}

				for (var i = 0; i < offset; i++) {
					if (Lines > obj.RemovedOnAdded.CursorPosition.Y) {
						_model.RemoveLine (obj.RemovedOnAdded.CursorPosition.Y);
					} else {
						break;
					}
				}
			}

			for (var i = 0; i < obj.Lines.Count; i++) {
				if (i == 0) {
					_model.ReplaceLine (startLine, obj.Lines [i]);
				} else if ((obj.IsUndoing && obj.LineStatus == HistoryText.LineStatus.Removed) ||
					   (!obj.IsUndoing && obj.LineStatus == HistoryText.LineStatus.Added)) {
					_model.AddLine (startLine, obj.Lines [i]);
				} else if (Lines > obj.CursorPosition.Y + 1) {
					_model.RemoveLine (obj.CursorPosition.Y + 1);
				}

				startLine++;
			}

			CursorPosition = obj.FinalCursorPosition;
		}

		UpdateWrapModel ();

		Adjust ();
		OnContentsChanged ();
	}

	void TextView_Initialized (object sender, EventArgs e)
	{
		Autocomplete.HostControl = this;
		if (Application.Top != null) {
			Application.Top.AlternateForwardKeyChanged += Top_AlternateForwardKeyChanged!;
			Application.Top.AlternateBackwardKeyChanged += Top_AlternateBackwardKeyChanged!;
		}

		OnContentsChanged ();
	}

	void Top_AlternateBackwardKeyChanged (object sender, KeyChangedEventArgs e) =>
		KeyBindings.Replace ((KeyCode)e.OldKey, (KeyCode)e.NewKey);

	void Top_AlternateForwardKeyChanged (object sender, KeyChangedEventArgs e) =>
		KeyBindings.Replace ((KeyCode)e.OldKey, (KeyCode)e.NewKey);

	void ResetPosition ()
	{
		_topRow = _leftColumn = CurrentRow = CurrentColumn = 0;
		StopSelecting ();
		ResetCursorVisibility ();
	}

	void WrapTextModel ()
	{
		if (_wordWrap && _wrapManager != null) {
			_model = _wrapManager.WrapModel (_frameWidth,
				out var nRow, out var nCol,
				out var nStartRow, out var nStartCol,
				CurrentRow, CurrentColumn,
				_selectionStartRow, _selectionStartColumn,
				_tabWidth);
			CurrentRow = nRow;
			CurrentColumn = nCol;
			_selectionStartRow = nStartRow;
			_selectionStartColumn = nStartCol;
			SetNeedsDisplay ();
		}
	}

	int GetSelectedLength () => SelectedText.Length;

	void SaveCursorVisibility ()
	{
		if (_desiredCursorVisibility != CursorVisibility.Invisible) {
			if (_savedCursorVisibility == 0) {
				_savedCursorVisibility = _desiredCursorVisibility;
			}

			DesiredCursorVisibility = CursorVisibility.Invisible;
		}
	}

	void ResetCursorVisibility ()
	{
		if (_savedCursorVisibility != 0) {
			DesiredCursorVisibility = _savedCursorVisibility;
			_savedCursorVisibility = 0;
		}
	}

	/// <summary>
	///         Loads the contents of the file into the <see cref="TextView" />.
	/// </summary>
	/// <returns><c>true</c>, if file was loaded, <c>false</c> otherwise.</returns>
	/// <param name="path">Path to the file to load.</param>
	public bool Load (string path)
	{
		SetWrapModel ();
		bool res;
		try {
			SetWrapModel ();
			res = _model.LoadFile (path);
			_historyText.Clear (Text);
			ResetPosition ();
		} finally {
			UpdateWrapModel ();
			SetNeedsDisplay ();
			Adjust ();
		}

		UpdateWrapModel ();
		return res;
	}

	/// <summary>
	///         Loads the contents of the stream into the <see cref="TextView" />.
	/// </summary>
	/// <returns><c>true</c>, if stream was loaded, <c>false</c> otherwise.</returns>
	/// <param name="stream">Stream to load the contents from.</param>
	public void Load (Stream stream)
	{
		SetWrapModel ();
		_model.LoadStream (stream);
		_historyText.Clear (Text);
		ResetPosition ();
		SetNeedsDisplay ();
		UpdateWrapModel ();
	}

	/// <summary>
	///         Loads the contents of the <see cref="RuneCell" /> list into the <see cref="TextView" />.
	/// </summary>
	/// <param name="cells">Rune cells list to load the contents from.</param>
	public void Load (List<RuneCell> cells)
	{
		SetWrapModel ();
		_model.LoadRuneCells (cells, ColorScheme);
		_historyText.Clear (Text);
		ResetPosition ();
		SetNeedsDisplay ();
		UpdateWrapModel ();
		InheritsPreviousColorScheme = true;
	}

	/// <summary>
	///         Loads the contents of the list of <see cref="RuneCell" /> list into the <see cref="TextView" />.
	/// </summary>
	/// <param name="cellsList">List of rune cells list to load the contents from.</param>
	public void Load (List<List<RuneCell>> cellsList)
	{
		SetWrapModel ();
		InheritsPreviousColorScheme = true;
		_model.LoadListRuneCells (cellsList, ColorScheme);
		_historyText.Clear (Text);
		ResetPosition ();
		SetNeedsDisplay ();
		UpdateWrapModel ();
	}

	/// <summary>
	///         Closes the contents of the stream into the <see cref="TextView" />.
	/// </summary>
	/// <returns><c>true</c>, if stream was closed, <c>false</c> otherwise.</returns>
	public bool CloseFile ()
	{
		SetWrapModel ();
		var res = _model.CloseFile ();
		ResetPosition ();
		SetNeedsDisplay ();
		UpdateWrapModel ();
		return res;
	}

	/// <summary>
	///         Positions the cursor on the current row and column
	/// </summary>
	public override void PositionCursor ()
	{
		ProcessAutocomplete ();

		if (!CanFocus || !Enabled || Application.Driver == null) {
			return;
		}

		if (Selecting) {
			// BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
			//var minRow = Math.Min (Math.Max (Math.Min (selectionStartRow, currentRow) - topRow, 0), Frame.Height);
			//var maxRow = Math.Min (Math.Max (Math.Max (selectionStartRow, currentRow) - topRow, 0), Frame.Height);
			//SetNeedsDisplay (new Rect (0, minRow, Frame.Width, maxRow));
			SetNeedsDisplay ();
		}

		var line = _model.GetLine (CurrentRow);
		var col = 0;
		if (line.Count > 0) {
			for (var idx = _leftColumn; idx < line.Count; idx++) {
				if (idx >= CurrentColumn) {
					break;
				}

				var cols = line [idx].Rune.GetColumns ();
				if (line [idx].Rune.Value == '\t') {
					cols += TabWidth + 1;
				}

				if (!TextModel.SetCol (ref col, Frame.Width, cols)) {
					col = CurrentColumn;
					break;
				}
			}
		}

		var posX = CurrentColumn - _leftColumn;
		var posY = CurrentRow - _topRow;
		if (posX > -1 && col >= posX && posX < Frame.Width - RightOffset && _topRow <= CurrentRow &&
		    posY < Frame.Height - BottomOffset) {
			ResetCursorVisibility ();
			Move (col, CurrentRow - _topRow);
		} else {
			SaveCursorVisibility ();
		}
	}

	void ClearRegion (int left, int top, int right, int bottom)
	{
		for (var row = top; row < bottom; row++) {
			Move (left, row);
			for (var col = left; col < right; col++) {
				AddRune (col, row, (Rune)' ');
			}
		}
	}

	/// <inheritdoc />
	public override Attribute GetNormalColor ()
	{
		var cs = ColorScheme;
		if (ColorScheme == null) {
			cs = new ColorScheme ();
		}

		return Enabled ? cs.Focus : cs.Disabled;
	}

	/// <summary>
	///         Sets the driver to the default color for the control where no text is being rendered. Defaults to
	///         <see cref="ColorScheme.Normal" />.
	/// </summary>
	protected virtual void SetNormalColor () => Driver.SetAttribute (GetNormalColor ());

	/// <summary>
	///         Sets the <see cref="View.Driver" /> to an appropriate color for rendering the given <paramref name="idxCol" />
	///         of the
	///         current <paramref name="line" />. Override to provide custom coloring by calling
	///         <see cref="ConsoleDriver.SetAttribute(Attribute)" />
	///         Defaults to <see cref="ColorScheme.Normal" />.
	/// </summary>
	/// <param name="line">The line.</param>
	/// <param name="idxCol">The col index.</param>
	/// <param name="idxRow">The row index.</param>
	protected virtual void OnDrawNormalColor (List<RuneCell> line, int idxCol, int idxRow)
	{
		var unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
		var ev = new RuneCellEventArgs (line, idxCol, unwrappedPos);
		DrawNormalColor?.Invoke (this, ev);

		if (line [idxCol].ColorScheme != null) {
			var colorScheme = line [idxCol].ColorScheme;
			Driver.SetAttribute (Enabled ? colorScheme!.Focus : colorScheme!.Disabled);
		} else {
			Driver.SetAttribute (GetNormalColor ());
		}
	}

	/// <summary>
	///         Sets the <see cref="View.Driver" /> to an appropriate color for rendering the given <paramref name="idxCol" />
	///         of the
	///         current <paramref name="line" />. Override to provide custom coloring by calling
	///         <see cref="ConsoleDriver.SetAttribute(Attribute)" />
	///         Defaults to <see cref="ColorScheme.Focus" />.
	/// </summary>
	/// <param name="line">The line.</param>
	/// <param name="idxCol">The col index.</param>
	/// ///
	/// <param name="idxRow">The row index.</param>
	protected virtual void OnDrawSelectionColor (List<RuneCell> line, int idxCol, int idxRow)
	{
		var unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
		var ev = new RuneCellEventArgs (line, idxCol, unwrappedPos);
		DrawSelectionColor?.Invoke (this, ev);

		if (line [idxCol].ColorScheme != null) {
			var colorScheme = line [idxCol].ColorScheme;
			Driver.SetAttribute (
				new Attribute (colorScheme!.Focus.Background, colorScheme.Focus.Foreground));
		} else {
			Driver.SetAttribute (new Attribute (ColorScheme.Focus.Background,
				ColorScheme.Focus.Foreground));
		}
	}

	/// <summary>
	///         Sets the <see cref="View.Driver" /> to an appropriate color for rendering the given <paramref name="idxCol" />
	///         of the
	///         current <paramref name="line" />. Override to provide custom coloring by calling
	///         <see cref="ConsoleDriver.SetAttribute(Attribute)" />
	///         Defaults to <see cref="ColorScheme.Focus" />.
	/// </summary>
	/// <param name="line">The line.</param>
	/// <param name="idxCol">The col index.</param>
	/// ///
	/// <param name="idxRow">The row index.</param>
	protected virtual void OnDrawReadOnlyColor (List<RuneCell> line, int idxCol, int idxRow)
	{
		var unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
		var ev = new RuneCellEventArgs (line, idxCol, unwrappedPos);
		DrawReadOnlyColor?.Invoke (this, ev);

		var colorScheme = line [idxCol].ColorScheme != null ? line [idxCol].ColorScheme : ColorScheme;
		Attribute attribute;
		if (colorScheme!.Disabled.Foreground == colorScheme.Focus.Background) {
			attribute = new Attribute (colorScheme.Focus.Foreground, colorScheme.Focus.Background);
		} else {
			attribute = new Attribute (colorScheme.Disabled.Foreground, colorScheme.Focus.Background);
		}

		Driver.SetAttribute (attribute);
	}

	/// <summary>
	///         Sets the <see cref="View.Driver" /> to an appropriate color for rendering the given <paramref name="idxCol" />
	///         of the
	///         current <paramref name="line" />. Override to provide custom coloring by calling
	///         <see cref="ConsoleDriver.SetAttribute(Attribute)" />
	///         Defaults to <see cref="ColorScheme.HotFocus" />.
	/// </summary>
	/// <param name="line">The line.</param>
	/// <param name="idxCol">The col index.</param>
	/// ///
	/// <param name="idxRow">The row index.</param>
	protected virtual void OnDrawUsedColor (List<RuneCell> line, int idxCol, int idxRow)
	{
		var unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
		var ev = new RuneCellEventArgs (line, idxCol, unwrappedPos);
		DrawUsedColor?.Invoke (this, ev);

		if (line [idxCol].ColorScheme != null) {
			var colorScheme = line [idxCol].ColorScheme;
			SetValidUsedColor (colorScheme!);
		} else {
			SetValidUsedColor (ColorScheme);
		}
	}

	static void SetValidUsedColor (ColorScheme colorScheme) =>
		// BUGBUG: (v2 truecolor) This code depends on 8-bit color names; disabling for now
		//if ((colorScheme!.HotNormal.Foreground & colorScheme.Focus.Background) == colorScheme.Focus.Foreground) {
		Driver.SetAttribute (new Attribute (colorScheme.Focus.Background, colorScheme.Focus.Foreground));

	/// <inheritdoc />
	public override bool OnEnter (View view)
	{
		//TODO: Improve it by handling read only mode of the text field
		Application.Driver.SetCursorVisibility (DesiredCursorVisibility);

		return base.OnEnter (view);
	}

	/// <inheritdoc />
	public override bool OnLeave (View view)
	{
		if (Application.MouseGrabView != null && Application.MouseGrabView == this) {
			Application.UngrabMouse ();
		}

		return base.OnLeave (view);
	}

	// Returns an encoded region start..end (top 32 bits are the row, low32 the column)
	void GetEncodedRegionBounds (out long start,
		out long end,
		int? startRow = null,
		int? startCol = null,
		int? cRow = null,
		int? cCol = null)
	{
		long selection;
		long point;
		if (startRow == null || startCol == null || cRow == null || cCol == null) {
			selection = ((long)(uint)_selectionStartRow << 32) | (uint)_selectionStartColumn;
			point = ((long)(uint)CurrentRow << 32) | (uint)CurrentColumn;
		} else {
			selection = ((long)(uint)startRow << 32) | (uint)startCol;
			point = ((long)(uint)cRow << 32) | (uint)cCol;
		}

		if (selection > point) {
			start = point;
			end = selection;
		} else {
			start = selection;
			end = point;
		}
	}

	bool PointInSelection (int col, int row)
	{
		long start, end;
		GetEncodedRegionBounds (out start, out end);
		var q = ((long)(uint)row << 32) | (uint)col;
		return q >= start && q <= end - 1;
	}

	//
	// Returns a string with the text in the selected 
	// region.
	//
	string GetRegion (int? sRow = null, int? sCol = null, int? cRow = null, int? cCol = null,
		TextModel? model = null)
	{
		long start, end;
		GetEncodedRegionBounds (out start, out end, sRow, sCol, cRow, cCol);
		if (start == end) {
			return string.Empty;
		}

		var startRow = (int)(start >> 32);
		var maxrow = (int)(end >> 32);
		var startCol = (int)(start & 0xffffffff);
		var endCol = (int)(end & 0xffffffff);
		var line = model == null ? _model.GetLine (startRow) : model.GetLine (startRow);

		if (startRow == maxrow) {
			return StringFromRunes (line.GetRange (startCol, endCol - startCol));
		}

		var res = StringFromRunes (line.GetRange (startCol, line.Count - startCol));

		for (var row = startRow + 1; row < maxrow; row++) {
			res = res +
			      Environment.NewLine +
			      StringFromRunes (model == null
				      ? _model.GetLine (row)
				      : model.GetLine (row));
		}

		line = model == null ? _model.GetLine (maxrow) : model.GetLine (maxrow);
		res = res + Environment.NewLine + StringFromRunes (line.GetRange (0, endCol));
		return res;
	}

	//
	// Clears the contents of the selected region
	//
	void ClearRegion ()
	{
		SetWrapModel ();

		long start, end;
		var currentEncoded = ((long)(uint)CurrentRow << 32) | (uint)CurrentColumn;
		GetEncodedRegionBounds (out start, out end);
		var startRow = (int)(start >> 32);
		var maxrow = (int)(end >> 32);
		var startCol = (int)(start & 0xffffffff);
		var endCol = (int)(end & 0xffffffff);
		var line = _model.GetLine (startRow);

		_historyText.Add (new List<List<RuneCell>> { new (line) }, new Point (startCol, startRow));

		var removedLines = new List<List<RuneCell>> ();

		if (startRow == maxrow) {
			removedLines.Add (new List<RuneCell> (line));

			line.RemoveRange (startCol, endCol - startCol);
			CurrentColumn = startCol;
			if (_wordWrap) {
				SetNeedsDisplay ();
			} else {
				// BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
				//SetNeedsDisplay (new Rect (0, startRow - topRow, Frame.Width, startRow - topRow + 1));
				SetNeedsDisplay ();
			}

			_historyText.Add (new List<List<RuneCell>> (removedLines), CursorPosition,
				HistoryText.LineStatus.Removed);

			UpdateWrapModel ();

			return;
		}

		removedLines.Add (new List<RuneCell> (line));

		line.RemoveRange (startCol, line.Count - startCol);
		var line2 = _model.GetLine (maxrow);
		line.AddRange (line2.Skip (endCol));
		for (var row = startRow + 1; row <= maxrow; row++) {
			removedLines.Add (new List<RuneCell> (_model.GetLine (startRow + 1)));

			_model.RemoveLine (startRow + 1);
		}

		if (currentEncoded == end) {
			CurrentRow -= maxrow - startRow;
		}

		CurrentColumn = startCol;

		_historyText.Add (new List<List<RuneCell>> (removedLines), CursorPosition,
			HistoryText.LineStatus.Removed);

		UpdateWrapModel ();

		SetNeedsDisplay ();
	}

	/// <summary>
	///         Select all text.
	/// </summary>
	public void SelectAll ()
	{
		if (_model.Count == 0) {
			return;
		}

		StartSelecting ();
		_selectionStartColumn = 0;
		_selectionStartRow = 0;
		CurrentColumn = _model.GetLine (_model.Count - 1).Count;
		CurrentRow = _model.Count - 1;
		SetNeedsDisplay ();
	}

	/// <summary>
	///         Find the next text based on the match case with the option to replace it.
	/// </summary>
	/// <param name="textToFind">The text to find.</param>
	/// <param name="gaveFullTurn"><c>true</c>If all the text was forward searched.<c>false</c>otherwise.</param>
	/// <param name="matchCase">The match case setting.</param>
	/// <param name="matchWholeWord">The match whole word setting.</param>
	/// <param name="textToReplace">The text to replace.</param>
	/// <param name="replace"><c>true</c>If is replacing.<c>false</c>otherwise.</param>
	/// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
	public bool FindNextText (string textToFind,
		out bool gaveFullTurn,
		bool matchCase = false,
		bool matchWholeWord = false,
		string? textToReplace = null,
		bool replace = false)
	{
		if (_model.Count == 0) {
			gaveFullTurn = false;
			return false;
		}

		SetWrapModel ();
		ResetContinuousFind ();
		var foundPos = _model.FindNextText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

		return SetFoundText (textToFind, foundPos, textToReplace, replace);
	}

	/// <summary>
	///         Find the previous text based on the match case with the option to replace it.
	/// </summary>
	/// <param name="textToFind">The text to find.</param>
	/// <param name="gaveFullTurn"><c>true</c>If all the text was backward searched.<c>false</c>otherwise.</param>
	/// <param name="matchCase">The match case setting.</param>
	/// <param name="matchWholeWord">The match whole word setting.</param>
	/// <param name="textToReplace">The text to replace.</param>
	/// <param name="replace"><c>true</c>If the text was found.<c>false</c>otherwise.</param>
	/// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
	public bool FindPreviousText (string textToFind,
		out bool gaveFullTurn,
		bool matchCase = false,
		bool matchWholeWord = false,
		string? textToReplace = null,
		bool replace = false)
	{
		if (_model.Count == 0) {
			gaveFullTurn = false;
			return false;
		}

		SetWrapModel ();
		ResetContinuousFind ();
		var foundPos = _model.FindPreviousText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

		return SetFoundText (textToFind, foundPos, textToReplace, replace);
	}

	/// <summary>
	///         Reset the flag to stop continuous find.
	/// </summary>
	public void FindTextChanged () => _continuousFind = false;

	/// <summary>
	///         Replaces all the text based on the match case.
	/// </summary>
	/// <param name="textToFind">The text to find.</param>
	/// <param name="matchCase">The match case setting.</param>
	/// <param name="matchWholeWord">The match whole word setting.</param>
	/// <param name="textToReplace">The text to replace.</param>
	/// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
	public bool ReplaceAllText (string textToFind,
		bool matchCase = false,
		bool matchWholeWord = false,
		string? textToReplace = null)
	{
		if (_isReadOnly || _model.Count == 0) {
			return false;
		}

		SetWrapModel ();
		ResetContinuousFind ();
		var foundPos = _model.ReplaceAllText (textToFind, matchCase, matchWholeWord, textToReplace);

		return SetFoundText (textToFind, foundPos, textToReplace, false, true);
	}

	bool SetFoundText (string text,
		(Point current, bool found) foundPos,
		string? textToReplace = null,
		bool replace = false,
		bool replaceAll = false)
	{
		if (foundPos.found) {
			StartSelecting ();
			_selectionStartColumn = foundPos.current.X;
			_selectionStartRow = foundPos.current.Y;
			if (!replaceAll) {
				CurrentColumn = _selectionStartColumn + text.GetRuneCount ();
			} else {
				CurrentColumn = _selectionStartColumn + textToReplace!.GetRuneCount ();
			}

			CurrentRow = foundPos.current.Y;
			if (!_isReadOnly && replace) {
				Adjust ();
				ClearSelectedRegion ();
				InsertAllText (textToReplace!);
				StartSelecting ();
				_selectionStartColumn = CurrentColumn - textToReplace!.GetRuneCount ();
			} else {
				UpdateWrapModel ();
				SetNeedsDisplay ();
				Adjust ();
			}

			_continuousFind = true;
			return foundPos.found;
		}

		UpdateWrapModel ();
		_continuousFind = false;

		return foundPos.found;
	}

	void ResetContinuousFind ()
	{
		if (!_continuousFind) {
			var col = Selecting ? _selectionStartColumn : CurrentColumn;
			var row = Selecting ? _selectionStartRow : CurrentRow;
			_model.ResetContinuousFind (new Point (col, row));
		}
	}

	/// <summary>
	///         Restore from original model.
	/// </summary>
	void SetWrapModel ([CallerMemberName] string? caller = null)
	{
		if (_currentCaller != null) {
			return;
		}

		if (_wordWrap) {
			_currentCaller = caller;

			CurrentColumn = _wrapManager!.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
			CurrentRow = _wrapManager.GetModelLineFromWrappedLines (CurrentRow);
			_selectionStartColumn =
				_wrapManager.GetModelColFromWrappedLines (_selectionStartRow, _selectionStartColumn);
			_selectionStartRow = _wrapManager.GetModelLineFromWrappedLines (_selectionStartRow);
			_model = _wrapManager.Model;
		}
	}

	/// <summary>
	///         Update the original model.
	/// </summary>
	void UpdateWrapModel ([CallerMemberName] string? caller = null)
	{
		if (_currentCaller != null && _currentCaller != caller) {
			return;
		}

		if (_wordWrap) {
			_currentCaller = null;

			_wrapManager!.UpdateModel (_model, out var nRow, out var nCol,
				out var nStartRow, out var nStartCol,
				CurrentRow, CurrentColumn,
				_selectionStartRow, _selectionStartColumn, true);
			CurrentRow = nRow;
			CurrentColumn = nCol;
			_selectionStartRow = nStartRow;
			_selectionStartColumn = nStartCol;
			_wrapNeeded = true;

			SetNeedsDisplay ();
		}

		if (_currentCaller != null) {
			throw new InvalidOperationException (
				$"WordWrap settings was changed after the {_currentCaller} call.");
		}
	}

	/// <summary>
	///         Invoke the <see cref="UnwrappedCursorPosition" /> event with the unwrapped <see cref="CursorPosition" />.
	/// </summary>
	public virtual void OnUnwrappedCursorPosition (int? cRow = null, int? cCol = null)
	{
		var row = cRow == null ? CurrentRow : cRow;
		var col = cCol == null ? CurrentColumn : cCol;
		if (cRow == null && cCol == null && _wordWrap) {
			row = _wrapManager!.GetModelLineFromWrappedLines (CurrentRow);
			col = _wrapManager.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
		}

		UnwrappedCursorPosition?.Invoke (this, new PointEventArgs (new Point ((int)col, (int)row)));
	}

	string GetSelectedRegion ()
	{
		var cRow = CurrentRow;
		var cCol = CurrentColumn;
		var startRow = _selectionStartRow;
		var startCol = _selectionStartColumn;
		var model = _model;
		if (_wordWrap) {
			cRow = _wrapManager!.GetModelLineFromWrappedLines (CurrentRow);
			cCol = _wrapManager.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
			startRow = _wrapManager.GetModelLineFromWrappedLines (_selectionStartRow);
			startCol = _wrapManager.GetModelColFromWrappedLines (_selectionStartRow, _selectionStartColumn);
			model = _wrapManager.Model;
		}

		OnUnwrappedCursorPosition (cRow, cCol);
		return GetRegion (startRow, startCol, cRow, cCol, model);
	}

	/// <inheritdoc />
	public override void OnDrawContent (Rect contentArea)
	{
		_isDrawing = true;

		SetNormalColor ();

		var offB = OffSetBackground ();
		var right = Frame.Width + offB.width + RightOffset;
		var bottom = Frame.Height + offB.height + BottomOffset;
		var row = 0;
		for (var idxRow = _topRow; idxRow < _model.Count; idxRow++) {
			var line = _model.GetLine (idxRow);
			var lineRuneCount = line.Count;
			var col = 0;

			Move (0, row);
			for (var idxCol = _leftColumn; idxCol < lineRuneCount; idxCol++) {
				var rune = idxCol >= lineRuneCount ? (Rune)' ' : line [idxCol].Rune;
				var cols = rune.GetColumns ();
				if (idxCol < line.Count && Selecting && PointInSelection (idxCol, idxRow)) {
					OnDrawSelectionColor (line, idxCol, idxRow);
				} else if (idxCol == CurrentColumn && idxRow == CurrentRow && !Selecting && !Used &&
					   HasFocus && idxCol < lineRuneCount) {
					OnDrawUsedColor (line, idxCol, idxRow);
				} else if (ReadOnly) {
					OnDrawReadOnlyColor (line, idxCol, idxRow);
				} else {
					OnDrawNormalColor (line, idxCol, idxRow);
				}

				if (rune.Value == '\t') {
					cols += TabWidth + 1;
					if (col + cols > right) {
						cols = right - col;
					}

					for (var i = 0; i < cols; i++) {
						if (col + i < right) {
							AddRune (col + i, row, (Rune)' ');
						}
					}
				} else {
					AddRune (col, row, rune);
				}

				if (!TextModel.SetCol (ref col, contentArea.Right, cols)) {
					break;
				}

				if (idxCol + 1 < lineRuneCount && col + line [idxCol + 1].Rune.GetColumns () > right) {
					break;
				}
			}

			if (col < right) {
				SetNormalColor ();
				ClearRegion (col, row, right, row + 1);
			}

			row++;
		}

		if (row < bottom) {
			SetNormalColor ();
			ClearRegion (contentArea.Left, row, right, bottom);
		}

		PositionCursor ();

		_isDrawing = false;
	}

	(int Row, int Col) GetUnwrappedPosition (int line, int col)
	{
		if (WordWrap) {
			return new ValueTuple<int, int> (_wrapManager!.GetModelLineFromWrappedLines (line),
				_wrapManager.GetModelColFromWrappedLines (line, col));
		}

		return new ValueTuple<int, int> (line, col);
	}

	void ProcessAutocomplete ()
	{
		if (_isDrawing) {
			return;
		}

		if (_clickWithSelecting) {
			_clickWithSelecting = false;
			return;
		}

		if (SelectedLength > 0) {
			return;
		}

		// draw autocomplete
		GenerateSuggestions ();

		var renderAt = new Point (
			Autocomplete.Context.CursorPosition,
			Autocomplete.PopupInsideContainer
				? CursorPosition.Y + 1 - TopRow
				: 0);

		Autocomplete.RenderOverlay (renderAt);
	}

	void GenerateSuggestions ()
	{
		var currentLine = GetCurrentLine ();
		var cursorPosition = Math.Min (CurrentColumn, currentLine.Count);
		Autocomplete.Context = new AutocompleteContext (currentLine, cursorPosition,
			Autocomplete.Context != null ? Autocomplete.Context.Canceled : false);
		Autocomplete.GenerateSuggestions (
			Autocomplete.Context);
	}

	void SetClipboard (string text)
	{
		if (text != null) {
			Clipboard.Contents = text;
		}
	}

	void AppendClipboard (string text) => Clipboard.Contents += text;

	/// <summary>
	///         Inserts the given <paramref name="toAdd" /> text at the current cursor position
	///         exactly as if the user had just typed it
	/// </summary>
	/// <param name="toAdd">Text to add</param>
	public void InsertText (string toAdd)
	{
		foreach (var ch in toAdd) {
			Key key;
			try {
				key = new Key (ch);
			} catch (Exception) {
				throw new ArgumentException (
					$"Cannot insert character '{ch}' because it does not map to a Key");
			}

			InsertText (key);

			if (NeedsDisplay) {
				Adjust ();
			} else {
				PositionCursor ();
			}
		}
	}

	void Insert (RuneCell cell)
	{
		var line = GetCurrentLine ();
		if (Used) {
			line.Insert (Math.Min (CurrentColumn, line.Count), cell);
		} else {
			if (CurrentColumn < line.Count) {
				line.RemoveAt (CurrentColumn);
			}

			line.Insert (Math.Min (CurrentColumn, line.Count), cell);
		}

		var prow = CurrentRow - _topRow;
		if (!_wrapNeeded) {
			// BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
			//SetNeedsDisplay (new Rect (0, prow, Math.Max (Frame.Width, 0), Math.Max (prow + 1, 0)));
			SetNeedsDisplay ();
		}
	}

	string StringFromRunes (List<RuneCell> cells)
	{
		if (cells == null) {
			throw new ArgumentNullException (nameof (cells));
		}

		var size = 0;
		foreach (var cell in cells) {
			size += cell.Rune.GetEncodingLength ();
		}

		var encoded = new byte [size];
		var offset = 0;
		foreach (var cell in cells) {
			offset += cell.Rune.Encode (encoded, offset);
		}

		return StringExtensions.ToString (encoded);
	}

	/// <summary>
	///         Returns the characters on the current line (where the cursor is positioned).
	///         Use <see cref="CurrentColumn" /> to determine the position of the cursor within
	///         that line
	/// </summary>
	/// <returns></returns>
	public List<RuneCell> GetCurrentLine () => _model.GetLine (CurrentRow);

	/// <summary>
	///         Returns the characters on the <paramref name="line" />.
	/// </summary>
	/// <param name="line">The intended line.</param>
	/// <returns></returns>
	public List<RuneCell> GetLine (int line) => _model.GetLine (line);

	/// <summary>
	///         Gets all lines of characters.
	/// </summary>
	/// <returns></returns>
	public List<List<RuneCell>> GetAllLines () => _model.GetAllLines ();

	void InsertAllText (string text)
	{
		if (string.IsNullOrEmpty (text)) {
			return;
		}

		var lines = TextModel.StringToLinesOfRuneCells (text);

		if (lines.Count == 0) {
			return;
		}

		SetWrapModel ();

		var line = GetCurrentLine ();

		_historyText.Add (new List<List<RuneCell>> { new (line) }, CursorPosition);

		// Optimize single line
		if (lines.Count == 1) {
			line.InsertRange (CurrentColumn, lines [0]);
			CurrentColumn += lines [0].Count;

			_historyText.Add (new List<List<RuneCell>> { new (line) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			if (!_wordWrap && CurrentColumn - _leftColumn > Frame.Width) {
				_leftColumn = Math.Max (CurrentColumn - Frame.Width + 1, 0);
			}

			if (_wordWrap) {
				SetNeedsDisplay ();
			} else {
				// BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
				//SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, Math.Max (currentRow - topRow + 1, 0)));
				SetNeedsDisplay ();
			}

			UpdateWrapModel ();

			OnContentsChanged ();

			return;
		}

		List<RuneCell>? rest = null;
		var lastp = 0;

		if (_model.Count > 0 && line.Count > 0 && !_copyWithoutSelection) {
			// Keep a copy of the rest of the line
			var restCount = line.Count - CurrentColumn;
			rest = line.GetRange (CurrentColumn, restCount);
			line.RemoveRange (CurrentColumn, restCount);
		}

		// First line is inserted at the current location, the rest is appended
		line.InsertRange (CurrentColumn, lines [0]);
		//model.AddLine (currentRow, lines [0]);

		var addedLines = new List<List<RuneCell>> { new (line) };

		for (var i = 1; i < lines.Count; i++) {
			_model.AddLine (CurrentRow + i, lines [i]);

			addedLines.Add (new List<RuneCell> (lines [i]));
		}

		if (rest != null) {
			var last = _model.GetLine (CurrentRow + lines.Count - 1);
			lastp = last.Count;
			last.InsertRange (last.Count, rest);

			addedLines.Last ().InsertRange (addedLines.Last ().Count, rest);
		}

		_historyText.Add (addedLines, CursorPosition, HistoryText.LineStatus.Added);

		// Now adjust column and row positions
		CurrentRow += lines.Count - 1;
		CurrentColumn = rest != null ? lastp : lines [lines.Count - 1].Count;
		Adjust ();

		_historyText.Add (new List<List<RuneCell>> { new (line) }, CursorPosition,
			HistoryText.LineStatus.Replaced);

		UpdateWrapModel ();
		OnContentsChanged ();
	}

	// Tries to snap the cursor to the tracking column
	void TrackColumn ()
	{
		// Now track the column
		var line = GetCurrentLine ();
		if (line.Count < _columnTrack) {
			CurrentColumn = line.Count;
		} else if (_columnTrack != -1) {
			CurrentColumn = _columnTrack;
		} else if (CurrentColumn > line.Count) {
			CurrentColumn = line.Count;
		}

		Adjust ();
	}

	void Adjust ()
	{
		var offB = OffSetBackground ();
		var line = GetCurrentLine ();
		var need = NeedsDisplay || _wrapNeeded || !Used;
		var tSize = TextModel.DisplaySize (line, -1, -1, false, TabWidth);
		var dSize = TextModel.DisplaySize (line, _leftColumn, CurrentColumn, true, TabWidth);
		if (!_wordWrap && CurrentColumn < _leftColumn) {
			_leftColumn = CurrentColumn;
			need = true;
		} else if (!_wordWrap && (CurrentColumn - _leftColumn + RightOffset > Frame.Width + offB.width ||
					  dSize.size + RightOffset >= Frame.Width + offB.width)) {
			_leftColumn = TextModel.CalculateLeftColumn (line, _leftColumn, CurrentColumn,
				Frame.Width + offB.width - RightOffset, TabWidth);
			need = true;
		} else if ((_wordWrap && _leftColumn > 0) || (dSize.size + RightOffset < Frame.Width + offB.width &&
							      tSize.size + RightOffset < Frame.Width + offB.width)) {
			if (_leftColumn > 0) {
				_leftColumn = 0;
				need = true;
			}
		}

		if (CurrentRow < _topRow) {
			_topRow = CurrentRow;
			need = true;
		} else if (CurrentRow - _topRow + BottomOffset >= Frame.Height + offB.height) {
			_topRow = Math.Min (Math.Max (CurrentRow - Frame.Height + 1 + BottomOffset, 0), CurrentRow);
			need = true;
		} else if (_topRow > 0 && CurrentRow < _topRow) {
			_topRow = Math.Max (_topRow - 1, 0);
			need = true;
		}

		if (need) {
			if (_wrapNeeded) {
				WrapTextModel ();
				_wrapNeeded = false;
			}

			SetNeedsDisplay ();
		} else {
			PositionCursor ();
		}

		OnUnwrappedCursorPosition ();
	}

	/// <summary>
	///         Called when the contents of the TextView change. E.g. when the user types text or deletes text. Raises
	///         the <see cref="ContentsChanged" /> event.
	/// </summary>
	public virtual void OnContentsChanged ()
	{
		ContentsChanged?.Invoke (this, new ContentsChangedEventArgs (CurrentRow, CurrentColumn));

		ProcessInheritsPreviousColorScheme (CurrentRow, CurrentColumn);
		ProcessAutocomplete ();
	}

	// If InheritsPreviousColorScheme is enabled this method will check if the rune cell on
	// the row and col location and around has a not null color scheme. If it's null will set it with
	// the very most previous valid color scheme.
	void ProcessInheritsPreviousColorScheme (int row, int col)
	{
		if (!InheritsPreviousColorScheme || (Lines == 1 && GetLine (Lines).Count == 0)) {
			return;
		}

		var line = GetLine (row);
		var lineToSet = line;
		while (line.Count == 0) {
			if (row == 0 && line.Count == 0) {
				return;
			}

			row--;
			line = GetLine (row);
			lineToSet = line;
		}

		var colWithColor = Math.Max (Math.Min (col - 2, line.Count - 1), 0);
		var cell = line [colWithColor];
		var colWithoutColor = Math.Max (col - 1, 0);

		if (cell.ColorScheme != null && colWithColor == 0 && lineToSet [colWithoutColor].ColorScheme != null) {
			for (var r = row - 1; r > -1; r--) {
				var l = GetLine (r);
				for (var c = l.Count - 1; c > -1; c--) {
					if (l [c].ColorScheme == null) {
						l [c].ColorScheme = cell.ColorScheme;
					} else {
						return;
					}
				}
			}

			return;
		}

		if (cell.ColorScheme == null) {
			for (var r = row; r > -1; r--) {
				var l = GetLine (r);
				colWithColor = l.FindLastIndex (colWithColor > -1 ? colWithColor : l.Count - 1,
					rc => rc.ColorScheme != null);
				if (colWithColor > -1 && l [colWithColor].ColorScheme != null) {
					cell = l [colWithColor];
					break;
				}
			}
		} else {
			var cRow = row;
			while (cell.ColorScheme == null) {
				if ((colWithColor == 0 || cell.ColorScheme == null) && cRow > 0) {
					line = GetLine (--cRow);
					colWithColor = line.Count - 1;
					cell = line [colWithColor];
				} else if (cRow == 0 && colWithColor < line.Count) {
					cell = line [colWithColor + 1];
				}
			}
		}

		if (cell.ColorScheme != null && colWithColor > -1 && colWithoutColor < lineToSet.Count &&
		    lineToSet [colWithoutColor].ColorScheme == null) {
			while (lineToSet [colWithoutColor].ColorScheme == null) {
				lineToSet [colWithoutColor].ColorScheme = cell.ColorScheme;
				colWithoutColor--;
				if (colWithoutColor == -1 && row > 0) {
					lineToSet = GetLine (--row);
					colWithoutColor = lineToSet.Count - 1;
				}
			}
		}
	}

	(int width, int height) OffSetBackground ()
	{
		var w = 0;
		var h = 0;
		if (SuperView?.Frame.Right - Frame.Right < 0) {
			w = SuperView!.Frame.Right - Frame.Right - 1;
		}

		if (SuperView?.Frame.Bottom - Frame.Bottom < 0) {
			h = SuperView!.Frame.Bottom - Frame.Bottom - 1;
		}

		return (w, h);
	}

	/// <summary>
	///         Will scroll the <see cref="TextView" /> to display the specified row at the top if <paramref name="isRow" /> is
	///         true or
	///         will scroll the <see cref="TextView" /> to display the specified column at the left if
	///         <paramref name="isRow" /> is
	///         false.
	/// </summary>
	/// <param name="idx">
	///         Row that should be displayed at the top or Column that should be displayed at the left,
	///         if the value is negative it will be reset to zero
	/// </param>
	/// <param name="isRow">If true (default) the <paramref name="idx" /> is a row, column otherwise.</param>
	public void ScrollTo (int idx, bool isRow = true)
	{
		if (idx < 0) {
			idx = 0;
		}

		if (isRow) {
			_topRow = Math.Max (idx > _model.Count - 1 ? _model.Count - 1 : idx, 0);
		} else if (!_wordWrap) {
			var maxlength =
				_model.GetMaxVisibleLine (_topRow, _topRow + Frame.Height + RightOffset, TabWidth);
			_leftColumn = Math.Max (!_wordWrap && idx > maxlength - 1 ? maxlength - 1 : idx, 0);
		}

		SetNeedsDisplay ();
	}

	/// <inheritdoc />
	public override bool? OnInvokingKeyBindings (Key a)
	{
		if (!a.IsValid) {
			return false;
		}

		// Give autocomplete first opportunity to respond to key presses
		if (SelectedLength == 0 && Autocomplete.Suggestions.Count > 0 && Autocomplete.ProcessKey (a)) {
			return true;
		}

		return base.OnInvokingKeyBindings (a);
	}

	/// <inheritdoc />
	public override bool OnProcessKeyDown (Key a)
	{
		if (!CanFocus) {
			return true;
		}


		ResetColumnTrack ();

		// Ignore control characters and other special keys
		if (!a.IsKeyCodeAtoZ && (a.KeyCode < KeyCode.Space || a.KeyCode > KeyCode.CharMask)) {
			return false;
		}

		InsertText (a);
		DoNeededAction ();

		return true;
	}

	/// <summary>
	///         Redoes the latest changes.
	/// </summary>
	public void Redo ()
	{
		if (ReadOnly) {
			return;
		}

		_historyText.Redo ();
	}

	/// <summary>
	///         Undoes the latest changes.
	/// </summary>
	public void Undo ()
	{
		if (ReadOnly) {
			return;
		}

		_historyText.Undo ();
	}

	bool ProcessMovePreviousView ()
	{
		ResetColumnTrack ();
		return MovePreviousView ();
	}

	bool ProcessMoveNextView ()
	{
		ResetColumnTrack ();
		return MoveNextView ();
	}

	void ProcessSetOverwrite ()
	{
		ResetColumnTrack ();
		SetOverwrite (!Used);
	}

	void ProcessSelectAll ()
	{
		ResetColumnTrack ();
		SelectAll ();
	}

	void MoveTopHomeExtend ()
	{
		ResetColumnTrack ();
		StartSelecting ();
		MoveHome ();
	}

	void MoveTopHome ()
	{
		ResetAllTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveHome ();
	}

	void MoveBottomEndExtend ()
	{
		ResetAllTrack ();
		StartSelecting ();
		MoveEnd ();
	}

	void MoveBottomEnd ()
	{
		ResetAllTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveEnd ();
	}

	void ProcessKillWordBackward ()
	{
		ResetColumnTrack ();
		KillWordBackward ();
	}

	void ProcessKillWordForward ()
	{
		ResetColumnTrack ();
		KillWordForward ();
	}

	void ProcessMoveWordForwardExtend ()
	{
		ResetAllTrack ();
		StartSelecting ();
		MoveWordForward ();
	}

	void ProcessMoveWordForward ()
	{
		ResetAllTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveWordForward ();
	}

	void ProcessMoveWordBackwardExtend ()
	{
		ResetAllTrack ();
		StartSelecting ();
		MoveWordBackward ();
	}

	void ProcessMoveWordBackward ()
	{
		ResetAllTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveWordBackward ();
	}

	void ProcessCut ()
	{
		ResetColumnTrack ();
		Cut ();
	}

	void ProcessCopy ()
	{
		ResetColumnTrack ();
		Copy ();
	}

	void ToggleSelecting ()
	{
		ResetColumnTrack ();
		Selecting = !Selecting;
		_selectionStartColumn = CurrentColumn;
		_selectionStartRow = CurrentRow;
	}

	void ProcessPaste ()
	{
		ResetColumnTrack ();
		if (_isReadOnly) {
			return;
		}

		Paste ();
	}

	void ProcessMoveEndOfLineExtend ()
	{
		ResetAllTrack ();
		StartSelecting ();
		MoveEndOfLine ();
	}

	void ProcessMoveEndOfLine ()
	{
		ResetAllTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveEndOfLine ();
	}

	void ProcessDeleteCharRight ()
	{
		ResetColumnTrack ();
		DeleteCharRight ();
	}

	void ProcessMoveStartOfLineExtend ()
	{
		ResetAllTrack ();
		StartSelecting ();
		MoveStartOfLine ();
	}

	void ProcessMoveStartOfLine ()
	{
		ResetAllTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveStartOfLine ();
	}

	void ProcessDeleteCharLeft ()
	{
		ResetColumnTrack ();
		DeleteCharLeft ();
	}

	void ProcessMoveLeftExtend ()
	{
		ResetAllTrack ();
		StartSelecting ();
		MoveLeft ();
	}

	bool ProcessMoveLeft ()
	{
		// if the user presses Left (without any control keys) and they are at the start of the text
		if (CurrentColumn == 0 && CurrentRow == 0) {
			// do not respond (this lets the key press fall through to navigation system - which usually changes focus backward)
			return false;
		}

		ResetAllTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveLeft ();
		return true;
	}

	void ProcessMoveRightExtend ()
	{
		ResetAllTrack ();
		StartSelecting ();
		MoveRight ();
	}

	bool ProcessMoveRight ()
	{
		// if the user presses Right (without any control keys)
		// determine where the last cursor position in the text is
		var lastRow = _model.Count - 1;
		var lastCol = _model.GetLine (lastRow).Count;

		// if they are at the very end of all the text do not respond (this lets the key press fall through to navigation system - which usually changes focus forward)
		if (CurrentColumn == lastCol && CurrentRow == lastRow) {
			return false;
		}

		ResetAllTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveRight ();
		return true;
	}

	void ProcessMoveUpExtend ()
	{
		ResetColumnTrack ();
		StartSelecting ();
		MoveUp ();
	}

	void ProcessMoveUp ()
	{
		ResetContinuousFindTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveUp ();
	}

	void ProcessMoveDownExtend ()
	{
		ResetColumnTrack ();
		StartSelecting ();
		MoveDown ();
	}

	void ProcessMoveDown ()
	{
		ResetContinuousFindTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MoveDown ();
	}

	void ProcessPageUpExtend ()
	{
		ResetColumnTrack ();
		StartSelecting ();
		MovePageUp ();
	}

	void ProcessPageUp ()
	{
		ResetColumnTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MovePageUp ();
	}

	void ProcessPageDownExtend ()
	{
		ResetColumnTrack ();
		StartSelecting ();
		MovePageDown ();
	}

	void ProcessPageDown ()
	{
		ResetColumnTrack ();
		if (_shiftSelecting && Selecting) {
			StopSelecting ();
		}

		MovePageDown ();
	}

	bool MovePreviousView ()
	{
		if (Application.OverlappedTop != null) {
			return SuperView?.FocusPrev () == true;
		}

		return false;
	}

	bool MoveNextView ()
	{
		if (Application.OverlappedTop != null) {
			return SuperView?.FocusNext () == true;
		}

		return false;
	}

	bool ProcessBackTab ()
	{
		ResetColumnTrack ();

		if (!AllowsTab || _isReadOnly) {
			return ProcessMovePreviousView ();
		}

		if (CurrentColumn > 0) {
			SetWrapModel ();

			var currentLine = GetCurrentLine ();
			if (currentLine.Count > 0 && currentLine [CurrentColumn - 1].Rune.Value == '\t') {
				_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition);

				currentLine.RemoveAt (CurrentColumn - 1);
				CurrentColumn--;

				_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
					HistoryText.LineStatus.Replaced);
			}

			SetNeedsDisplay ();

			UpdateWrapModel ();
		}

		DoNeededAction ();
		return true;
	}

	bool ProcessTab ()
	{
		ResetColumnTrack ();

		if (!AllowsTab || _isReadOnly) {
			return ProcessMoveNextView ();
		}

		InsertText (new Key ((KeyCode)'\t'));
		DoNeededAction ();
		return true;
	}

	void SetOverwrite (bool overwrite)
	{
		Used = overwrite;
		SetNeedsDisplay ();
		DoNeededAction ();
	}

	bool ProcessReturn ()
	{
		ResetColumnTrack ();

		if (!AllowsReturn || _isReadOnly) {
			return false;
		}

		SetWrapModel ();

		var currentLine = GetCurrentLine ();

		_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition);

		if (Selecting) {
			ClearSelectedRegion ();
			currentLine = GetCurrentLine ();
		}

		var restCount = currentLine.Count - CurrentColumn;
		var rest = currentLine.GetRange (CurrentColumn, restCount);
		currentLine.RemoveRange (CurrentColumn, restCount);

		var addedLines = new List<List<RuneCell>> { new (currentLine) };

		_model.AddLine (CurrentRow + 1, rest);

		addedLines.Add (new List<RuneCell> (_model.GetLine (CurrentRow + 1)));

		_historyText.Add (addedLines, CursorPosition, HistoryText.LineStatus.Added);

		CurrentRow++;

		var fullNeedsDisplay = false;
		if (CurrentRow >= _topRow + Frame.Height) {
			_topRow++;
			fullNeedsDisplay = true;
		}

		CurrentColumn = 0;

		_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
			HistoryText.LineStatus.Replaced);

		if (!_wordWrap && CurrentColumn < _leftColumn) {
			fullNeedsDisplay = true;
			_leftColumn = 0;
		}

		if (fullNeedsDisplay) {
			SetNeedsDisplay ();
		} else {
			// BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
			//SetNeedsDisplay (new Rect (0, currentRow - topRow, 2, Frame.Height));
			SetNeedsDisplay ();
		}

		UpdateWrapModel ();

		DoNeededAction ();
		OnContentsChanged ();
		return true;
	}

	void KillWordBackward ()
	{
		if (_isReadOnly) {
			return;
		}

		SetWrapModel ();

		var currentLine = GetCurrentLine ();

		_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition);

		if (CurrentColumn == 0) {
			DeleteTextBackwards ();

			_historyText.ReplaceLast (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();

			return;
		}

		var newPos = _model.WordBackward (CurrentColumn, CurrentRow);
		if (newPos.HasValue && CurrentRow == newPos.Value.row) {
			var restCount = CurrentColumn - newPos.Value.col;
			currentLine.RemoveRange (newPos.Value.col, restCount);
			if (_wordWrap) {
				_wrapNeeded = true;
			}

			CurrentColumn = newPos.Value.col;
		} else if (newPos.HasValue) {
			var restCount = currentLine.Count - CurrentColumn;
			currentLine.RemoveRange (CurrentColumn, restCount);
			if (_wordWrap) {
				_wrapNeeded = true;
			}

			CurrentColumn = newPos.Value.col;
			CurrentRow = newPos.Value.row;
		}

		_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
			HistoryText.LineStatus.Replaced);

		UpdateWrapModel ();

		DoSetNeedsDisplay (new Rect (0, CurrentRow - _topRow, Frame.Width, Frame.Height));
		DoNeededAction ();
	}

	void KillWordForward ()
	{
		if (_isReadOnly) {
			return;
		}

		SetWrapModel ();

		var currentLine = GetCurrentLine ();

		_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition);

		if (currentLine.Count == 0 || CurrentColumn == currentLine.Count) {
			DeleteTextForwards ();

			_historyText.ReplaceLast (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();

			return;
		}

		var newPos = _model.WordForward (CurrentColumn, CurrentRow);
		var restCount = 0;
		if (newPos.HasValue && CurrentRow == newPos.Value.row) {
			restCount = newPos.Value.col - CurrentColumn;
			currentLine.RemoveRange (CurrentColumn, restCount);
		} else if (newPos.HasValue) {
			restCount = currentLine.Count - CurrentColumn;
			currentLine.RemoveRange (CurrentColumn, restCount);
		}

		if (_wordWrap) {
			_wrapNeeded = true;
		}

		_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
			HistoryText.LineStatus.Replaced);

		UpdateWrapModel ();

		DoSetNeedsDisplay (new Rect (0, CurrentRow - _topRow, Frame.Width, Frame.Height));
		DoNeededAction ();
	}

	void MoveWordForward ()
	{
		var newPos = _model.WordForward (CurrentColumn, CurrentRow);
		if (newPos.HasValue) {
			CurrentColumn = newPos.Value.col;
			CurrentRow = newPos.Value.row;
		}

		Adjust ();
		DoNeededAction ();
	}

	void MoveWordBackward ()
	{
		var newPos = _model.WordBackward (CurrentColumn, CurrentRow);
		if (newPos.HasValue) {
			CurrentColumn = newPos.Value.col;
			CurrentRow = newPos.Value.row;
		}

		Adjust ();
		DoNeededAction ();
	}

	void KillToStartOfLine ()
	{
		if (_isReadOnly) {
			return;
		}

		if (_model.Count == 1 && GetCurrentLine ().Count == 0) {
			// Prevents from adding line feeds if there is no more lines.
			return;
		}

		SetWrapModel ();

		var currentLine = GetCurrentLine ();
		var setLastWasKill = true;
		if (currentLine.Count > 0 && CurrentColumn == 0) {
			UpdateWrapModel ();

			DeleteTextBackwards ();
			return;
		}

		_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition);

		if (currentLine.Count == 0) {
			if (CurrentRow > 0) {
				_model.RemoveLine (CurrentRow);

				if (_model.Count > 0 || _lastWasKill) {
					var val = Environment.NewLine;
					if (_lastWasKill) {
						AppendClipboard (val);
					} else {
						SetClipboard (val);
					}
				}

				if (_model.Count == 0) {
					// Prevents from adding line feeds if there is no more lines.
					setLastWasKill = false;
				}

				CurrentRow--;
				currentLine = _model.GetLine (CurrentRow);

				var removedLine = new List<List<RuneCell>> { new (currentLine) };

				removedLine.Add (new List<RuneCell> ());

				_historyText.Add (new List<List<RuneCell>> (removedLine), CursorPosition,
					HistoryText.LineStatus.Removed);

				CurrentColumn = currentLine.Count;
			}
		} else {
			var restCount = CurrentColumn;
			var rest = currentLine.GetRange (0, restCount);
			var val = string.Empty;
			val += StringFromRunes (rest);
			if (_lastWasKill) {
				AppendClipboard (val);
			} else {
				SetClipboard (val);
			}

			currentLine.RemoveRange (0, restCount);
			CurrentColumn = 0;
		}

		_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
			HistoryText.LineStatus.Replaced);

		UpdateWrapModel ();

		DoSetNeedsDisplay (new Rect (0, CurrentRow - _topRow, Frame.Width, Frame.Height));

		_lastWasKill = setLastWasKill;
		DoNeededAction ();
	}

	void KillToEndOfLine ()
	{
		if (_isReadOnly) {
			return;
		}

		if (_model.Count == 1 && GetCurrentLine ().Count == 0) {
			// Prevents from adding line feeds if there is no more lines.
			return;
		}

		SetWrapModel ();

		var currentLine = GetCurrentLine ();
		var setLastWasKill = true;
		if (currentLine.Count > 0 && CurrentColumn == currentLine.Count) {
			UpdateWrapModel ();

			DeleteTextForwards ();
			return;
		}

		_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition);

		if (currentLine.Count == 0) {
			if (CurrentRow < _model.Count - 1) {
				var removedLines = new List<List<RuneCell>> { new (currentLine) };

				_model.RemoveLine (CurrentRow);

				removedLines.Add (new List<RuneCell> (GetCurrentLine ()));

				_historyText.Add (new List<List<RuneCell>> (removedLines), CursorPosition,
					HistoryText.LineStatus.Removed);
			}

			if (_model.Count > 0 || _lastWasKill) {
				var val = Environment.NewLine;
				if (_lastWasKill) {
					AppendClipboard (val);
				} else {
					SetClipboard (val);
				}
			}

			if (_model.Count == 0) {
				// Prevents from adding line feeds if there is no more lines.
				setLastWasKill = false;
			}
		} else {
			var restCount = currentLine.Count - CurrentColumn;
			var rest = currentLine.GetRange (CurrentColumn, restCount);
			var val = string.Empty;
			val += StringFromRunes (rest);
			if (_lastWasKill) {
				AppendClipboard (val);
			} else {
				SetClipboard (val);
			}

			currentLine.RemoveRange (CurrentColumn, restCount);
		}

		_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
			HistoryText.LineStatus.Replaced);

		UpdateWrapModel ();

		DoSetNeedsDisplay (new Rect (0, CurrentRow - _topRow, Frame.Width, Frame.Height));

		_lastWasKill = setLastWasKill;
		DoNeededAction ();
	}

	void MoveEndOfLine ()
	{
		var currentLine = GetCurrentLine ();
		CurrentColumn = currentLine.Count;
		Adjust ();
		DoNeededAction ();
	}

	void MoveStartOfLine ()
	{
		if (_leftColumn > 0) {
			SetNeedsDisplay ();
		}

		CurrentColumn = 0;
		_leftColumn = 0;
		Adjust ();
		DoNeededAction ();
	}

	/// <summary>
	///         Deletes all the selected or a single character at right from the position of the cursor.
	/// </summary>
	public void DeleteCharRight ()
	{
		if (_isReadOnly) {
			return;
		}

		SetWrapModel ();

		if (Selecting) {
			_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition);

			ClearSelectedRegion ();

			var currentLine = GetCurrentLine ();

			_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();
			OnContentsChanged ();

			return;
		}

		if (DeleteTextForwards ()) {
			UpdateWrapModel ();
			OnContentsChanged ();

			return;
		}

		UpdateWrapModel ();

		DoNeededAction ();
		OnContentsChanged ();
	}

	/// <summary>
	///         Deletes all the selected or a single character at left from the position of the cursor.
	/// </summary>
	public void DeleteCharLeft ()
	{
		if (_isReadOnly) {
			return;
		}

		SetWrapModel ();

		if (Selecting) {
			_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition);

			ClearSelectedRegion ();

			var currentLine = GetCurrentLine ();

			_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();
			OnContentsChanged ();

			return;
		}

		if (DeleteTextBackwards ()) {
			UpdateWrapModel ();
			OnContentsChanged ();

			return;
		}

		UpdateWrapModel ();

		DoNeededAction ();
		OnContentsChanged ();
	}

	void MoveLeft ()
	{
		if (CurrentColumn > 0) {
			CurrentColumn--;
		} else {
			if (CurrentRow > 0) {
				CurrentRow--;
				if (CurrentRow < _topRow) {
					_topRow--;
					SetNeedsDisplay ();
				}

				var currentLine = GetCurrentLine ();
				CurrentColumn = currentLine.Count;
			}
		}

		Adjust ();
		DoNeededAction ();
	}

	void MoveRight ()
	{
		var currentLine = GetCurrentLine ();
		if (CurrentColumn < currentLine.Count) {
			CurrentColumn++;
		} else {
			if (CurrentRow + 1 < _model.Count) {
				CurrentRow++;
				CurrentColumn = 0;
				if (CurrentRow >= _topRow + Frame.Height) {
					_topRow++;
					SetNeedsDisplay ();
				}
			}
		}

		Adjust ();
		DoNeededAction ();
	}

	void MovePageUp ()
	{
		var nPageUpShift = Frame.Height - 1;
		if (CurrentRow > 0) {
			if (_columnTrack == -1) {
				_columnTrack = CurrentColumn;
			}

			CurrentRow = CurrentRow - nPageUpShift < 0 ? 0 : CurrentRow - nPageUpShift;
			if (CurrentRow < _topRow) {
				_topRow = _topRow - nPageUpShift < 0 ? 0 : _topRow - nPageUpShift;
				SetNeedsDisplay ();
			}

			TrackColumn ();
			PositionCursor ();
		}

		DoNeededAction ();
	}

	void MovePageDown ()
	{
		var nPageDnShift = Frame.Height - 1;
		if (CurrentRow >= 0 && CurrentRow < _model.Count) {
			if (_columnTrack == -1) {
				_columnTrack = CurrentColumn;
			}

			CurrentRow = CurrentRow + nPageDnShift > _model.Count
				? _model.Count > 0 ? _model.Count - 1 : 0
				: CurrentRow + nPageDnShift;
			if (_topRow < CurrentRow - nPageDnShift) {
				_topRow = CurrentRow >= _model.Count
					? CurrentRow - nPageDnShift
					: _topRow + nPageDnShift;
				SetNeedsDisplay ();
			}

			TrackColumn ();
			PositionCursor ();
		}

		DoNeededAction ();
	}

	void ResetContinuousFindTrack ()
	{
		// Handle some state here - whether the last command was a kill
		// operation and the column tracking (up/down)
		_lastWasKill = false;
		_continuousFind = false;
	}

	void ResetColumnTrack ()
	{
		// Handle some state here - whether the last command was a kill
		// operation and the column tracking (up/down)
		_lastWasKill = false;
		_columnTrack = -1;
	}

	void ResetAllTrack ()
	{
		// Handle some state here - whether the last command was a kill
		// operation and the column tracking (up/down)
		_lastWasKill = false;
		_columnTrack = -1;
		_continuousFind = false;
	}

	bool InsertText (Key a, ColorScheme? colorScheme = null)
	{
		//So that special keys like tab can be processed
		if (_isReadOnly) {
			return true;
		}

		SetWrapModel ();

		_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition);

		if (Selecting) {
			ClearSelectedRegion ();
		}

		if ((uint)a.KeyCode == '\n') {
			_model.AddLine (CurrentRow + 1, new List<RuneCell> ());
			CurrentRow++;
			CurrentColumn = 0;
		} else if ((uint)a.KeyCode == '\r') {
			CurrentColumn = 0;
		} else {
			if (Used) {
				Insert (new RuneCell { Rune = a.AsRune, ColorScheme = colorScheme });
				CurrentColumn++;
				if (CurrentColumn >= _leftColumn + Frame.Width) {
					_leftColumn++;
					SetNeedsDisplay ();
				}
			} else {
				Insert (new RuneCell { Rune = a.AsRune, ColorScheme = colorScheme });
				CurrentColumn++;
			}
		}

		_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
			HistoryText.LineStatus.Replaced);

		UpdateWrapModel ();
		OnContentsChanged ();

		return true;
	}

	void ShowContextMenu ()
	{
		if (_currentCulture != Thread.CurrentThread.CurrentUICulture) {
			_currentCulture = Thread.CurrentThread.CurrentUICulture;

			ContextMenu!.MenuItems = BuildContextMenuBarItem ();
		}

		ContextMenu!.Show ();
	}

	/// <summary>
	///         Deletes all text.
	/// </summary>
	public void DeleteAll ()
	{
		if (Lines == 0) {
			return;
		}

		_selectionStartColumn = 0;
		_selectionStartRow = 0;
		MoveBottomEndExtend ();
		DeleteCharLeft ();
		SetNeedsDisplay ();
	}

	/// <inheritdoc />
	public override bool OnKeyUp (Key a)
	{
		switch (a.KeyCode) {
		case KeyCode.Space | KeyCode.CtrlMask:
			return true;
		}

		return base.OnKeyUp (a);
	}

	void DoNeededAction ()
	{
		if (NeedsDisplay) {
			Adjust ();
		} else {
			PositionCursor ();
		}
	}

	bool DeleteTextForwards ()
	{
		SetWrapModel ();

		var currentLine = GetCurrentLine ();
		if (CurrentColumn == currentLine.Count) {
			if (CurrentRow + 1 == _model.Count) {
				UpdateWrapModel ();

				return true;
			}

			_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition);

			var removedLines = new List<List<RuneCell>> { new (currentLine) };

			var nextLine = _model.GetLine (CurrentRow + 1);

			removedLines.Add (new List<RuneCell> (nextLine));

			_historyText.Add (removedLines, CursorPosition, HistoryText.LineStatus.Removed);

			currentLine.AddRange (nextLine);
			_model.RemoveLine (CurrentRow + 1);

			_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			if (_wordWrap) {
				_wrapNeeded = true;
			}

			DoSetNeedsDisplay (new Rect (0, CurrentRow - _topRow, Frame.Width, CurrentRow - _topRow + 1));
		} else {
			_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition);

			currentLine.RemoveAt (CurrentColumn);

			_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			if (_wordWrap) {
				_wrapNeeded = true;
			}

			DoSetNeedsDisplay (new Rect (CurrentColumn - _leftColumn, CurrentRow - _topRow, Frame.Width,
				CurrentRow - _topRow + 1));
		}

		UpdateWrapModel ();

		return false;
	}

	void DoSetNeedsDisplay (Rect rect)
	{
		if (_wrapNeeded) {
			SetNeedsDisplay ();
		} else {
			// BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
			//SetNeedsDisplay (rect);
			SetNeedsDisplay ();
		}
	}

	bool DeleteTextBackwards ()
	{
		SetWrapModel ();

		if (CurrentColumn > 0) {
			// Delete backwards 
			var currentLine = GetCurrentLine ();

			_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition);

			currentLine.RemoveAt (CurrentColumn - 1);
			if (_wordWrap) {
				_wrapNeeded = true;
			}

			CurrentColumn--;

			_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			if (CurrentColumn < _leftColumn) {
				_leftColumn--;
				SetNeedsDisplay ();
			} else {
				// BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
				//SetNeedsDisplay (new Rect (0, currentRow - topRow, 1, Frame.Width));
				SetNeedsDisplay ();
			}
		} else {
			// Merges the current line with the previous one.
			if (CurrentRow == 0) {
				return true;
			}

			var prowIdx = CurrentRow - 1;
			var prevRow = _model.GetLine (prowIdx);

			_historyText.Add (new List<List<RuneCell>> { new (prevRow) }, CursorPosition);

			List<List<RuneCell>> removedLines = new () { new List<RuneCell> (prevRow) };

			removedLines.Add (new List<RuneCell> (GetCurrentLine ()));

			_historyText.Add (removedLines, new Point (CurrentColumn, prowIdx),
				HistoryText.LineStatus.Removed);

			var prevCount = prevRow.Count;
			_model.GetLine (prowIdx).AddRange (GetCurrentLine ());
			_model.RemoveLine (CurrentRow);
			if (_wordWrap) {
				_wrapNeeded = true;
			}

			CurrentRow--;

			_historyText.Add (new List<List<RuneCell>> { GetCurrentLine () },
				new Point (CurrentColumn, prowIdx),
				HistoryText.LineStatus.Replaced);

			CurrentColumn = prevCount;
			SetNeedsDisplay ();
		}

		UpdateWrapModel ();

		return false;
	}

	/// <summary>
	///         Copy the selected text to the clipboard contents.
	/// </summary>
	public void Copy ()
	{
		SetWrapModel ();
		if (Selecting) {
			SetClipboard (GetRegion ());
			_copyWithoutSelection = false;
		} else {
			var currentLine = GetCurrentLine ();
			SetClipboard (TextModel.ToString (currentLine));
			_copyWithoutSelection = true;
		}

		UpdateWrapModel ();
		DoNeededAction ();
	}

	/// <summary>
	///         Cut the selected text to the clipboard contents.
	/// </summary>
	public void Cut ()
	{
		SetWrapModel ();
		SetClipboard (GetRegion ());
		if (!_isReadOnly) {
			ClearRegion ();

			_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);
		}

		UpdateWrapModel ();
		Selecting = false;
		DoNeededAction ();
		OnContentsChanged ();
	}

	/// <summary>
	///         Paste the clipboard contents into the current selected position.
	/// </summary>
	public void Paste ()
	{
		if (_isReadOnly) {
			return;
		}

		SetWrapModel ();
		var contents = Clipboard.Contents;
		if (_copyWithoutSelection && contents.FirstOrDefault (x => x == '\n' || x == '\r') == 0) {
			var runeList = contents == null ? new List<RuneCell> () : TextModel.ToRuneCellList (contents);
			var currentLine = GetCurrentLine ();

			_historyText.Add (new List<List<RuneCell>> { new (currentLine) }, CursorPosition);

			var addedLine = new List<List<RuneCell>> {
				new(currentLine),
				runeList
			};

			_historyText.Add (new List<List<RuneCell>> (addedLine), CursorPosition,
				HistoryText.LineStatus.Added);

			_model.AddLine (CurrentRow, runeList);
			CurrentRow++;

			_historyText.Add (new List<List<RuneCell>> { new (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			SetNeedsDisplay ();
			OnContentsChanged ();
		} else {
			if (Selecting) {
				ClearRegion ();
			}

			_copyWithoutSelection = false;
			InsertAllText (contents);

			if (Selecting) {
				_historyText.ReplaceLast (new List<List<RuneCell>> { new (GetCurrentLine ()) },
					CursorPosition,
					HistoryText.LineStatus.Original);
			}

			SetNeedsDisplay ();
		}

		UpdateWrapModel ();
		Selecting = false;
		DoNeededAction ();
	}

	void StartSelecting ()
	{
		if (_shiftSelecting && Selecting) {
			return;
		}

		_shiftSelecting = true;
		Selecting = true;
		_selectionStartColumn = CurrentColumn;
		_selectionStartRow = CurrentRow;
	}

	void StopSelecting ()
	{
		_shiftSelecting = false;
		Selecting = false;
		_isButtonShift = false;
	}

	void ClearSelectedRegion ()
	{
		SetWrapModel ();
		if (!_isReadOnly) {
			ClearRegion ();
		}

		UpdateWrapModel ();
		Selecting = false;
		DoNeededAction ();
	}

	void MoveUp ()
	{
		if (CurrentRow > 0) {
			if (_columnTrack == -1) {
				_columnTrack = CurrentColumn;
			}

			CurrentRow--;
			if (CurrentRow < _topRow) {
				_topRow--;
				SetNeedsDisplay ();
			}

			TrackColumn ();
			PositionCursor ();
		}

		DoNeededAction ();
	}

	void MoveDown ()
	{
		if (CurrentRow + 1 < _model.Count) {
			if (_columnTrack == -1) {
				_columnTrack = CurrentColumn;
			}

			CurrentRow++;
			if (CurrentRow + BottomOffset >= _topRow + Frame.Height) {
				_topRow++;
				SetNeedsDisplay ();
			}

			TrackColumn ();
			PositionCursor ();
		} else if (CurrentRow > Frame.Height) {
			Adjust ();
		}

		DoNeededAction ();
	}

	IEnumerable<(int col, int row, RuneCell rune)> ForwardIterator (int col, int row)
	{
		if (col < 0 || row < 0) {
			yield break;
		}

		if (row >= _model.Count) {
			yield break;
		}

		var line = GetCurrentLine ();
		if (col >= line.Count) {
			yield break;
		}

		while (row < _model.Count) {
			for (var c = col; c < line.Count; c++) {
				yield return (c, row, line [c]);
			}

			col = 0;
			row++;
			line = GetCurrentLine ();
		}
	}

	/// <summary>
	///         Will scroll the <see cref="TextView" /> to the last line and position the cursor there.
	/// </summary>
	public void MoveEnd ()
	{
		CurrentRow = _model.Count - 1;
		var line = GetCurrentLine ();
		CurrentColumn = line.Count;
		TrackColumn ();
		PositionCursor ();
	}

	/// <summary>
	///         Will scroll the <see cref="TextView" /> to the first line and position the cursor there.
	/// </summary>
	public void MoveHome ()
	{
		CurrentRow = 0;
		_topRow = 0;
		CurrentColumn = 0;
		_leftColumn = 0;
		TrackColumn ();
		PositionCursor ();
		SetNeedsDisplay ();
	}

	/// <inheritdoc />
	public override bool MouseEvent (MouseEvent ev)
	{
		if (!ev.Flags.HasFlag (MouseFlags.Button1Clicked) &&
		    !ev.Flags.HasFlag (MouseFlags.Button1Pressed) &&
		    !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) &&
		    !ev.Flags.HasFlag (MouseFlags.Button1Released) &&
		    !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift) &&
		    !ev.Flags.HasFlag (MouseFlags.WheeledDown) &&
		    !ev.Flags.HasFlag (MouseFlags.WheeledUp) &&
		    !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
		    !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked | MouseFlags.ButtonShift) &&
		    !ev.Flags.HasFlag (MouseFlags.Button1TripleClicked) &&
		    !ev.Flags.HasFlag (ContextMenu!.MouseFlags)) {
			return false;
		}

		if (!CanFocus) {
			return true;
		}

		if (!HasFocus) {
			SetFocus ();
		}

		_continuousFind = false;

		// Give autocomplete first opportunity to respond to mouse clicks
		if (SelectedLength == 0 && Autocomplete.MouseEvent (ev, true)) {
			return true;
		}

		if (ev.Flags == MouseFlags.Button1Clicked) {
			if (_shiftSelecting && !_isButtonShift) {
				StopSelecting ();
			}

			ProcessMouseClick (ev, out _);
			if (Used) {
				PositionCursor ();
			} else {
				SetNeedsDisplay ();
			}

			_lastWasKill = false;
			_columnTrack = CurrentColumn;
		} else if (ev.Flags == MouseFlags.WheeledDown) {
			_lastWasKill = false;
			_columnTrack = CurrentColumn;
			ScrollTo (_topRow + 1);
		} else if (ev.Flags == MouseFlags.WheeledUp) {
			_lastWasKill = false;
			_columnTrack = CurrentColumn;
			ScrollTo (_topRow - 1);
		} else if (ev.Flags == MouseFlags.WheeledRight) {
			_lastWasKill = false;
			_columnTrack = CurrentColumn;
			ScrollTo (_leftColumn + 1, false);
		} else if (ev.Flags == MouseFlags.WheeledLeft) {
			_lastWasKill = false;
			_columnTrack = CurrentColumn;
			ScrollTo (_leftColumn - 1, false);
		} else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
			ProcessMouseClick (ev, out var line);
			PositionCursor ();
			if (_model.Count > 0 && _shiftSelecting && Selecting) {
				if (CurrentRow - _topRow + BottomOffset >= Frame.Height - 1 &&
				    _model.Count + BottomOffset > _topRow + CurrentRow) {
					ScrollTo (_topRow + Frame.Height);
				} else if (_topRow > 0 && CurrentRow <= _topRow) {
					ScrollTo (_topRow - Frame.Height);
				} else if (ev.Y >= Frame.Height) {
					ScrollTo (_model.Count + BottomOffset);
				} else if (ev.Y < 0 && _topRow > 0) {
					ScrollTo (0);
				}

				if (CurrentColumn - _leftColumn + RightOffset >= Frame.Width - 1 &&
				    line.Count + RightOffset > _leftColumn + CurrentColumn) {
					ScrollTo (_leftColumn + Frame.Width, false);
				} else if (_leftColumn > 0 && CurrentColumn <= _leftColumn) {
					ScrollTo (_leftColumn - Frame.Width, false);
				} else if (ev.X >= Frame.Width) {
					ScrollTo (line.Count + RightOffset, false);
				} else if (ev.X < 0 && _leftColumn > 0) {
					ScrollTo (0, false);
				}
			}

			_lastWasKill = false;
			_columnTrack = CurrentColumn;
		} else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift)) {
			if (!_shiftSelecting) {
				_isButtonShift = true;
				StartSelecting ();
			}

			ProcessMouseClick (ev, out _);
			PositionCursor ();
			_lastWasKill = false;
			_columnTrack = CurrentColumn;
		} else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed)) {
			if (_shiftSelecting) {
				_clickWithSelecting = true;
				StopSelecting ();
			}

			ProcessMouseClick (ev, out _);
			PositionCursor ();
			if (!Selecting) {
				StartSelecting ();
			}

			_lastWasKill = false;
			_columnTrack = CurrentColumn;
			if (Application.MouseGrabView == null) {
				Application.GrabMouse (this);
			}
		} else if (ev.Flags.HasFlag (MouseFlags.Button1Released)) {
			Application.UngrabMouse ();
		} else if (ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked)) {
			if (ev.Flags.HasFlag (MouseFlags.ButtonShift)) {
				if (!Selecting) {
					StartSelecting ();
				}
			} else if (Selecting) {
				StopSelecting ();
			}

			ProcessMouseClick (ev, out var line);
			(int col, int row)? newPos;
			if (CurrentColumn == line.Count || (CurrentColumn > 0 &&
							    (line [CurrentColumn - 1].Rune.Value != ' ' ||
							     line [CurrentColumn].Rune.Value == ' '))) {
				newPos = _model.WordBackward (CurrentColumn, CurrentRow);
				if (newPos.HasValue) {
					CurrentColumn = CurrentRow == newPos.Value.row ? newPos.Value.col : 0;
				}
			}

			if (!Selecting) {
				StartSelecting ();
			}

			newPos = _model.WordForward (CurrentColumn, CurrentRow);
			if (newPos != null && newPos.HasValue) {
				CurrentColumn = CurrentRow == newPos.Value.row ? newPos.Value.col : line.Count;
			}

			PositionCursor ();
			_lastWasKill = false;
			_columnTrack = CurrentColumn;
		} else if (ev.Flags.HasFlag (MouseFlags.Button1TripleClicked)) {
			if (Selecting) {
				StopSelecting ();
			}

			ProcessMouseClick (ev, out var line);
			CurrentColumn = 0;
			if (!Selecting) {
				StartSelecting ();
			}

			CurrentColumn = line.Count;
			PositionCursor ();
			_lastWasKill = false;
			_columnTrack = CurrentColumn;
		} else if (ev.Flags == ContextMenu!.MouseFlags) {
			ContextMenu.Position = new Point (ev.X + 2, ev.Y + 2);
			ShowContextMenu ();
		}

		return true;
	}

	void ProcessMouseClick (MouseEvent ev, out List<RuneCell> line)
	{
		List<RuneCell>? r = null;
		if (_model.Count > 0) {
			var maxCursorPositionableLine = Math.Max (_model.Count - 1 - _topRow, 0);
			if (Math.Max (ev.Y, 0) > maxCursorPositionableLine) {
				CurrentRow = maxCursorPositionableLine + _topRow;
			} else {
				CurrentRow = Math.Max (ev.Y + _topRow, 0);
			}

			r = GetCurrentLine ();
			var idx = TextModel.GetColFromX (r, _leftColumn, Math.Max (ev.X, 0), TabWidth);
			if (idx - _leftColumn >= r.Count + RightOffset) {
				CurrentColumn = Math.Max (r.Count - _leftColumn + RightOffset, 0);
			} else {
				CurrentColumn = idx + _leftColumn;
			}
		}

		line = r!;
	}

	/// <summary>
	///         Allows clearing the <see cref="HistoryText.HistoryTextItem" /> items updating the original text.
	/// </summary>
	public void ClearHistoryChanges () => _historyText?.Clear (Text);
}

/// <summary>
///         Renders an overlay on another view at a given point that allows selecting
///         from a range of 'autocomplete' options.
///         An implementation on a TextView.
/// </summary>
public class TextViewAutocomplete : PopupAutocomplete {
	/// <inheritdoc />
	protected override void DeleteTextBackwards () => ((TextView)HostControl).DeleteCharLeft ();

	/// <inheritdoc />
	protected override void InsertText (string accepted) => ((TextView)HostControl).InsertText (accepted);

	/// <inheritdoc />
	protected override void SetCursorPosition (int column) => ((TextView)HostControl).CursorPosition =
		new Point (column, ((TextView)HostControl).CurrentRow);
}