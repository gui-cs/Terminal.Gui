using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     Provides text formatting. Supports <see cref="View.HotKey"/>s, horizontal alignment, vertical alignment,
///     multiple lines, and word-based line wrap.
/// </summary>
public class TextFormatter
{
    private bool _autoSize;
    private Key _hotKey = new ();
    private int _hotKeyPos = -1;
    private List<string> _lines = new ();
    private bool _multiLine;
    private bool _preserveTrailingSpaces;
    private Size _size;
    private int _tabWidth = 4;
    private string _text;
    private Alignment _textAlignment = Alignment.Start;
    private TextDirection _textDirection;
    private Alignment _textVerticalAlignment = Alignment.Start;
    private bool _wordWrap = true;

    /// <summary>Get or sets the horizontal text alignment.</summary>
    /// <value>The text alignment.</value>
    public Alignment Alignment
    {
        get => _textAlignment;
        set => _textAlignment = EnableNeedsFormat (value);
    }

    /// <summary>Gets or sets whether the <see cref="Size"/> should be automatically changed to fit the <see cref="Text"/>.</summary>
    /// <remarks>
    ///     <para>Used when <see cref="View"/> is using <see cref="Dim.Auto"/> to resize the view's <see cref="View.Viewport"/> to fit <see cref="Size"/>.</para>
    ///     <para>
    ///         AutoSize is ignored if <see cref="Gui.Alignment.Fill"/> is used.
    ///     </para>
    /// </remarks>
    public bool AutoSize
    {
        get => _autoSize;
        set
        {
            _autoSize = EnableNeedsFormat (value);

            if (_autoSize)
            {
                Size = GetAutoSize ();
            }
        }
    }

    internal Size GetAutoSize ()
    {
        Size size = CalcRect (0, 0, Text, Direction, TabWidth).Size;
        return size with
        {
            Width = size.Width - GetHotKeySpecifierLength (),
            Height = size.Height - GetHotKeySpecifierLength (false)
        };

    }
    /// <summary>
    ///     Gets the width or height of the <see cref="TextFormatter.HotKeySpecifier"/> characters
    ///     in the <see cref="Text"/> property.
    /// </summary>
    /// <remarks>
    ///     Only the first HotKey specifier found in <see cref="Text"/> is supported.
    /// </remarks>
    /// <param name="isWidth">
    ///     If <see langword="true"/> (the default) the width required for the HotKey specifier is returned. Otherwise, the
    ///     height is returned.
    /// </param>
    /// <returns>
    ///     The number of characters required for the <see cref="TextFormatter.HotKeySpecifier"/>. If the text
    ///     direction specified
    ///     by <see cref="TextDirection"/> does not match the <paramref name="isWidth"/> parameter, <c>0</c> is returned.
    /// </returns>
    public int GetHotKeySpecifierLength (bool isWidth = true)
    {
        if (isWidth)
        {
            return TextFormatter.IsHorizontalDirection (Direction) && Text?.Contains ((char)HotKeySpecifier.Value) == true
                       ? Math.Max (HotKeySpecifier.GetColumns (), 0)
                       : 0;
        }

        return TextFormatter.IsVerticalDirection (Direction) && Text?.Contains ((char)HotKeySpecifier.Value) == true
                   ? Math.Max (HotKeySpecifier.GetColumns (), 0)
                   : 0;
    }

    /// <summary>
    ///     Gets the cursor position of the <see cref="HotKey"/>. If the <see cref="HotKey"/> is defined, the cursor will
    ///     be positioned over it.
    /// </summary>
    public int CursorPosition { get; internal set; }

    /// <summary>Gets or sets the text-direction.</summary>
    /// <value>The text direction.</value>
    public TextDirection Direction
    {
        get => _textDirection;
        set
        {
            _textDirection = EnableNeedsFormat (value);

            if (AutoSize)
            {
                Size = GetAutoSize ();
            }
        }
    }
    
    /// <summary>
    ///     Determines if the viewport width will be used or only the text width will be used,
    ///     If <see langword="true"/> all the viewport area will be filled with whitespaces and the same background color
    ///     showing a perfect rectangle.
    /// </summary>
    public bool FillRemaining { get; set; }

    /// <summary>Gets or sets the hot key. Fires the <see cref="HotKeyChanged"/> event.</summary>
    public Key HotKey
    {
        get => _hotKey;
        internal set
        {
            if (_hotKey != value)
            {
                Key oldKey = _hotKey;
                _hotKey = value;
                HotKeyChanged?.Invoke (this, new KeyChangedEventArgs (oldKey, value));
            }
        }
    }

    /// <summary>The position in the text of the hot key. The hot key will be rendered using the hot color.</summary>
    public int HotKeyPos
    {
        get => _hotKeyPos;
        internal set => _hotKeyPos = value;
    }

    /// <summary>
    ///     The specifier character for the hot key (e.g. '_'). Set to '\xffff' to disable hot key support for this View
    ///     instance. The default is '\xffff'.
    /// </summary>
    public Rune HotKeySpecifier { get; set; } = (Rune)0xFFFF;

    /// <summary>Gets or sets a value indicating whether multi line is allowed.</summary>
    /// <remarks>Multi line is ignored if <see cref="WordWrap"/> is <see langword="true"/>.</remarks>
    public bool MultiLine
    {
        get => _multiLine;
        set => _multiLine = EnableNeedsFormat (value);
    }

    /// <summary>Gets or sets whether the <see cref="TextFormatter"/> needs to format the text.</summary>
    /// <remarks>
    ///     <para>If <see langword="false"/> when Draw is called, the Draw call will be faster.</para>
    ///     <para>Used by <see cref="Draw"/></para>
    ///     <para>Set to <see langword="true"/> when any of the properties of <see cref="TextFormatter"/> are set.</para>
    ///     <para>Set to <see langword="false"/> when the text is formatted (if <see cref="GetLines"/> is accessed).</para>
    /// </remarks>
    public bool NeedsFormat { get; set; }

    /// <summary>
    ///     Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved or not when
    ///     <see cref="TextFormatter.WordWrap"/> is enabled. If <see langword="true"/> trailing spaces at the end of wrapped
    ///     lines will be removed when <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
    /// </summary>
    public bool PreserveTrailingSpaces
    {
        get => _preserveTrailingSpaces;
        set => _preserveTrailingSpaces = EnableNeedsFormat (value);
    }

    /// <summary>Gets or sets the size <see cref="Text"/> will be constrained to when formatted.</summary>
    /// <remarks>
    ///     <para>
    ///         Does not return the size of the formatted text but the size that will be used to constrain the text when
    ///         formatted.
    ///     </para>
    ///     <para>When set, <see cref="NeedsFormat"/> is set to <see langword="true"/>.</para>
    /// </remarks>
    public Size Size
    {
        get => _size;
        set
        {
            if (AutoSize)
            {
                _size = EnableNeedsFormat (GetAutoSize());
            }
            else
            {
                _size = EnableNeedsFormat (value);
            }
        }
    }

    /// <summary>Gets or sets the number of columns used for a tab.</summary>
    public int TabWidth
    {
        get => _tabWidth;
        set => _tabWidth = EnableNeedsFormat (value);
    }

    /// <summary>The text to be formatted. This string is never modified.</summary>
    public virtual string Text
    {
        get => _text;
        set
        {
            _text = EnableNeedsFormat (value);

            if (AutoSize)
            {
                Size = GetAutoSize (); ;
            }
        }
    }

    /// <summary>Gets or sets the vertical text-alignment.</summary>
    /// <value>The text vertical alignment.</value>
    public Alignment VerticalAlignment
    {
        get => _textVerticalAlignment;
        set => _textVerticalAlignment = EnableNeedsFormat (value);
    }

    /// <summary>Gets or sets whether word wrap will be used to fit <see cref="Text"/> to <see cref="Size"/>.</summary>
    public bool WordWrap
    {
        get => _wordWrap;
        set => _wordWrap = EnableNeedsFormat (value);
    }

    /// <summary>Draws the text held by <see cref="TextFormatter"/> to <see cref="ConsoleDriver"/> using the colors specified.</summary>
    /// <remarks>
    ///     Causes the text to be formatted (references <see cref="GetLines"/>). Sets <see cref="NeedsFormat"/> to
    ///     <c>false</c>.
    /// </remarks>
    /// <param name="screen">Specifies the screen-relative location and maximum size for drawing the text.</param>
    /// <param name="normalColor">The color to use for all text except the hotkey</param>
    /// <param name="hotColor">The color to use to draw the hotkey</param>
    /// <param name="maximum">Specifies the screen-relative location and maximum container size.</param>
    /// <param name="driver">The console driver currently used by the application.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Draw (
        Rectangle screen,
        Attribute normalColor,
        Attribute hotColor,
        Rectangle maximum = default,
        ConsoleDriver driver = null
    )
    {
        // With this check, we protect against subclasses with overrides of Text (like Button)
        if (string.IsNullOrEmpty (Text))
        {
            return;
        }

        driver ??= Application.Driver;

        driver?.SetAttribute (normalColor);

        List<string> linesFormatted = GetLines ();

        bool isVertical = IsVerticalDirection (Direction);
        Rectangle maxScreen = screen;

        if (driver is { })
        {
            // INTENT: What, exactly, is the intent of this?
            maxScreen = maximum == default (Rectangle)
                            ? screen
                            : new (
                                   Math.Max (maximum.X, screen.X),
                                   Math.Max (maximum.Y, screen.Y),
                                   Math.Max (
                                             Math.Min (maximum.Width, maximum.Right - screen.Left),
                                             0
                                            ),
                                   Math.Max (
                                             Math.Min (
                                                       maximum.Height,
                                                       maximum.Bottom - screen.Top
                                                      ),
                                             0
                                            )
                                  );
        }

        if (maxScreen.Width == 0 || maxScreen.Height == 0)
        {
            return;
        }

        int lineOffset = !isVertical && screen.Y < 0 ? Math.Abs (screen.Y) : 0;

        for (int line = lineOffset; line < linesFormatted.Count; line++)
        {
            if ((isVertical && line > screen.Width) || (!isVertical && line > screen.Height))
            {
                continue;
            }

            if ((isVertical && line >= maxScreen.Left + maxScreen.Width)
                || (!isVertical && line >= maxScreen.Top + maxScreen.Height + lineOffset))
            {
                break;
            }

            Rune [] runes = linesFormatted [line].ToRunes ();

            // When text is justified, we lost left or right, so we use the direction to align. 

            int x = 0, y = 0;

            // Horizontal Alignment
            if (Alignment is Alignment.End)
            {
                if (isVertical)
                {
                    int runesWidth = GetColumnsRequiredForVerticalText (linesFormatted, line, linesFormatted.Count - line, TabWidth);
                    x = screen.Right - runesWidth;
                    CursorPosition = screen.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
                }
                else
                {
                    int runesWidth = StringExtensions.ToString (runes).GetColumns ();
                    x = screen.Right - runesWidth;
                    CursorPosition = screen.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
                }
            }
            else if (Alignment is Alignment.Start)
            {
                if (isVertical)
                {
                    int runesWidth = line > 0
                                         ? GetColumnsRequiredForVerticalText (linesFormatted, 0, line, TabWidth)
                                         : 0;
                    x = screen.Left + runesWidth;
                }
                else
                {
                    x = screen.Left;
                }

                CursorPosition = _hotKeyPos > -1 ? _hotKeyPos : 0;
            }
            else if (Alignment is Alignment.Fill)
            {
                if (isVertical)
                {
                    int runesWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, linesFormatted.Count, TabWidth);
                    int prevLineWidth = line > 0 ? GetColumnsRequiredForVerticalText (linesFormatted, line - 1, 1, TabWidth) : 0;
                    int firstLineWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, 1, TabWidth);
                    int lastLineWidth = GetColumnsRequiredForVerticalText (linesFormatted, linesFormatted.Count - 1, 1, TabWidth);
                    var interval = (int)Math.Round ((double)(screen.Width + firstLineWidth + lastLineWidth) / linesFormatted.Count);

                    x = line == 0
                            ? screen.Left
                            : line < linesFormatted.Count - 1
                                ? screen.Width - runesWidth <= lastLineWidth ? screen.Left + prevLineWidth : screen.Left + line * interval
                                : screen.Right - lastLineWidth;
                }
                else
                {
                    x = screen.Left;
                }

                CursorPosition = _hotKeyPos > -1 ? _hotKeyPos : 0;
            }
            else if (Alignment is Alignment.Center)
            {
                if (isVertical)
                {
                    int runesWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, linesFormatted.Count, TabWidth);
                    int linesWidth = GetColumnsRequiredForVerticalText (linesFormatted, 0, line, TabWidth);
                    x = screen.Left + linesWidth + (screen.Width - runesWidth) / 2;

                    CursorPosition = (screen.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
                }
                else
                {
                    int runesWidth = StringExtensions.ToString (runes).GetColumns ();
                    x = screen.Left + (screen.Width - runesWidth) / 2;

                    CursorPosition = (screen.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
                }
            }
            else
            {
                Debug.WriteLine ($"Unsupported Alignment: {nameof (VerticalAlignment)}");

                return;
            }

            // Vertical Alignment
            if (VerticalAlignment is Alignment.End)
            {
                if (isVertical)
                {
                    y = screen.Bottom - runes.Length;
                }
                else
                {
                    y = screen.Bottom - linesFormatted.Count + line;
                }
            }
            else if (VerticalAlignment is Alignment.Start)
            {
                if (isVertical)
                {
                    y = screen.Top;
                }
                else
                {
                    y = screen.Top + line;
                }
            }
            else if (VerticalAlignment is Alignment.Fill)
            {
                if (isVertical)
                {
                    y = screen.Top;
                }
                else
                {
                    var interval = (int)Math.Round ((double)(screen.Height + 2) / linesFormatted.Count);

                    y = line == 0 ? screen.Top :
                        line < linesFormatted.Count - 1 ? screen.Height - interval <= 1 ? screen.Top + 1 : screen.Top + line * interval : screen.Bottom - 1;
                }
            }
            else if (VerticalAlignment is Alignment.Center)
            {
                if (isVertical)
                {
                    int s = (screen.Height - runes.Length) / 2;
                    y = screen.Top + s;
                }
                else
                {
                    int s = (screen.Height - linesFormatted.Count) / 2;
                    y = screen.Top + line + s;
                }
            }
            else
            {
               Debug.WriteLine ($"Unsupported Alignment: {nameof (VerticalAlignment)}");

               return;
            }

            int colOffset = screen.X < 0 ? Math.Abs (screen.X) : 0;
            int start = isVertical ? screen.Top : screen.Left;
            int size = isVertical ? screen.Height : screen.Width;
            int current = start + colOffset;
            List<Point?> lastZeroWidthPos = null;
            Rune rune = default;
            int zeroLengthCount = isVertical ? runes.Sum (r => r.GetColumns () == 0 ? 1 : 0) : 0;

            for (int idx = (isVertical ? start - y : start - x) + colOffset;
                 current < start + size + zeroLengthCount;
                 idx++)
            {
                Rune lastRuneUsed = rune;

                if (lastZeroWidthPos is null)
                {
                    if (idx < 0
                        || (isVertical
                                ? VerticalAlignment != Alignment.End && current < 0
                                : Alignment != Alignment.End && x + current + colOffset < 0))
                    {
                        current++;

                        continue;
                    }

                    if (!FillRemaining && idx > runes.Length - 1)
                    {
                        break;
                    }

                    if ((!isVertical && (current - start > maxScreen.Left + maxScreen.Width - screen.X + colOffset || (idx < runes.Length && runes [idx].GetColumns () > screen.Width)))
                        || (isVertical && ((current > start + size + zeroLengthCount && idx > maxScreen.Top + maxScreen.Height - screen.Y) || (idx < runes.Length && runes [idx].GetColumns () > screen.Width))))
                    {
                        break;
                    }
                }

                //if ((!isVertical && idx > maxBounds.Left + maxBounds.Width - viewport.X + colOffset)
                //	|| (isVertical && idx > maxBounds.Top + maxBounds.Height - viewport.Y))

                //	break;

                rune = (Rune)' ';

                if (isVertical)
                {
                    if (idx >= 0 && idx < runes.Length)
                    {
                        rune = runes [idx];
                    }

                    if (lastZeroWidthPos is null)
                    {
                        driver?.Move (x, current);
                    }
                    else
                    {
                        int foundIdx = lastZeroWidthPos.IndexOf (
                                                                 p =>
                                                                     p is { } && p.Value.Y == current
                                                                );

                        if (foundIdx > -1)
                        {
                            if (rune.IsCombiningMark ())
                            {
                                lastZeroWidthPos [foundIdx] =
                                    new Point (
                                               lastZeroWidthPos [foundIdx].Value.X + 1,
                                               current
                                              );

                                driver?.Move (
                                              lastZeroWidthPos [foundIdx].Value.X,
                                              current
                                             );
                            }
                            else if (!rune.IsCombiningMark () && lastRuneUsed.IsCombiningMark ())
                            {
                                current++;
                                driver?.Move (x, current);
                            }
                            else
                            {
                                driver?.Move (x, current);
                            }
                        }
                        else
                        {
                            driver?.Move (x, current);
                        }
                    }
                }
                else
                {
                    driver?.Move (current, y);

                    if (idx >= 0 && idx < runes.Length)
                    {
                        rune = runes [idx];
                    }
                }

                int runeWidth = GetRuneWidth (rune, TabWidth);

                if (HotKeyPos > -1 && idx == HotKeyPos)
                {
                    if ((isVertical && VerticalAlignment == Alignment.Fill) || (!isVertical && Alignment == Alignment.Fill))
                    {
                        CursorPosition = idx - start;
                    }

                    driver?.SetAttribute (hotColor);
                    driver?.AddRune (rune);
                    driver?.SetAttribute (normalColor);
                }
                else
                {
                    if (isVertical)
                    {
                        if (runeWidth == 0)
                        {
                            if (lastZeroWidthPos is null)
                            {
                                lastZeroWidthPos = new List<Point?> ();
                            }

                            int foundIdx = lastZeroWidthPos.IndexOf (
                                                                     p =>
                                                                         p is { } && p.Value.Y == current
                                                                    );

                            if (foundIdx == -1)
                            {
                                current--;
                                lastZeroWidthPos.Add (new Point (x + 1, current));
                            }

                            driver?.Move (x + 1, current);
                        }
                    }

                    driver?.AddRune (rune);
                }

                if (isVertical)
                {
                    if (runeWidth > 0)
                    {
                        current++;
                    }
                }
                else
                {
                    current += runeWidth;
                }

                int nextRuneWidth = idx + 1 > -1 && idx + 1 < runes.Length
                                        ? runes [idx + 1].GetColumns ()
                                        : 0;

                if (!isVertical && idx + 1 < runes.Length && current + nextRuneWidth > start + size)
                {
                    break;
                }
            }
        }
    }

    /// <summary>Returns the formatted text, constrained to <see cref="Size"/>.</summary>
    /// <remarks>
    ///     If <see cref="NeedsFormat"/> is <see langword="true"/>, causes a format, resetting <see cref="NeedsFormat"/>
    ///     to <see langword="false"/>.
    /// </remarks>
    /// <returns>The formatted text.</returns>
    public string Format ()
    {
        var sb = new StringBuilder ();

        // Lines_get causes a Format
        foreach (string line in GetLines ())
        {
            sb.AppendLine (line);
        }

        return sb.ToString ().TrimEnd (Environment.NewLine.ToCharArray ());
    }

    /// <summary>Gets the size required to hold the formatted text, given the constraints placed by <see cref="Size"/>.</summary>
    /// <remarks>Causes a format, resetting <see cref="NeedsFormat"/> to <see langword="false"/>.</remarks>
    /// <returns>The size required to hold the formatted text.</returns>
    public Size FormatAndGetSize ()
    {
        if (string.IsNullOrEmpty (Text) || Size.Height == 0 || Size.Width == 0)
        {
            return Size.Empty;
        }

        int width = GetLines ().Max (static line => line.GetColumns ());
        int height = GetLines ().Count;

        return new (width, height);
    }

    /// <summary>Gets a list of formatted lines, constrained to <see cref="Size"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         If the text needs to be formatted (if <see cref="NeedsFormat"/> is <see langword="true"/>)
    ///         <see cref="Format()"/> will be called and upon return
    ///         <see cref="NeedsFormat"/> will be <see langword="false"/>.
    ///     </para>
    ///     <para>
    ///         If either of the dimensions of <see cref="Size"/> are zero, the text will not be formatted and no lines will
    ///         be returned.
    ///     </para>
    /// </remarks>
    public List<string> GetLines ()
    {
        // With this check, we protect against subclasses with overrides of Text
        if (string.IsNullOrEmpty (Text) || Size.Height == 0 || Size.Width == 0)
        {
            _lines = new List<string> { string.Empty };
            NeedsFormat = false;

            return _lines;
        }

        if (NeedsFormat)
        {
            string text = _text;

            if (FindHotKey (_text, HotKeySpecifier, out _hotKeyPos, out Key newHotKey))
            {
                HotKey = newHotKey;
                text = RemoveHotKeySpecifier (Text, _hotKeyPos, HotKeySpecifier);
                text = ReplaceHotKeyWithTag (text, _hotKeyPos);
            }

            if (IsVerticalDirection (Direction))
            {
                int colsWidth = GetSumMaxCharWidth (text, 0, 1, TabWidth);

                _lines = Format (
                                 text,
                                 Size.Height,
                                 VerticalAlignment == Alignment.Fill,
                                 Size.Width > colsWidth && WordWrap,
                                 PreserveTrailingSpaces,
                                 TabWidth,
                                 Direction,
                                 MultiLine,
                                 this
                                );

                if (!AutoSize)
                {
                    colsWidth = GetMaxColsForWidth (_lines, Size.Width, TabWidth);

                    if (_lines.Count > colsWidth)
                    {
                        _lines.RemoveRange (colsWidth, _lines.Count - colsWidth);
                    }
                }
            }
            else
            {
                _lines = Format (
                                 text,
                                 Size.Width,
                                 Alignment == Alignment.Fill,
                                 Size.Height > 1 && WordWrap,
                                 PreserveTrailingSpaces,
                                 TabWidth,
                                 Direction,
                                 MultiLine,
                                 this
                                );

                if (!AutoSize && _lines.Count > Size.Height)
                {
                    _lines.RemoveRange (Size.Height, _lines.Count - Size.Height);
                }
            }

            NeedsFormat = false;
        }

        return _lines;
    }

    /// <summary>Event invoked when the <see cref="HotKey"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

    /// <summary>Sets <see cref="NeedsFormat"/> to <see langword="true"/> and returns the value.</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    private T EnableNeedsFormat<T> (T value)
    {
        NeedsFormat = true;

        return value;
    }

    #region Static Members

    /// <summary>Check if it is a horizontal direction</summary>
    public static bool IsHorizontalDirection (TextDirection textDirection)
    {
        return textDirection switch
        {
            TextDirection.LeftRight_TopBottom => true,
            TextDirection.LeftRight_BottomTop => true,
            TextDirection.RightLeft_TopBottom => true,
            TextDirection.RightLeft_BottomTop => true,
            _ => false
        };
    }

    /// <summary>Check if it is a vertical direction</summary>
    public static bool IsVerticalDirection (TextDirection textDirection)
    {
        return textDirection switch
        {
            TextDirection.TopBottom_LeftRight => true,
            TextDirection.TopBottom_RightLeft => true,
            TextDirection.BottomTop_LeftRight => true,
            TextDirection.BottomTop_RightLeft => true,
            _ => false
        };
    }

    /// <summary>Check if it is Left to Right direction</summary>
    public static bool IsLeftToRight (TextDirection textDirection)
    {
        return textDirection switch
        {
            TextDirection.LeftRight_TopBottom => true,
            TextDirection.LeftRight_BottomTop => true,
            _ => false
        };
    }

    /// <summary>Check if it is Top to Bottom direction</summary>
    public static bool IsTopToBottom (TextDirection textDirection)
    {
        return textDirection switch
        {
            TextDirection.TopBottom_LeftRight => true,
            TextDirection.TopBottom_RightLeft => true,
            _ => false
        };
    }

    // TODO: Move to StringExtensions?
    private static string StripCRLF (string str, bool keepNewLine = false)
    {
        List<Rune> runes = str.ToRuneList ();

        for (var i = 0; i < runes.Count; i++)
        {
            switch ((char)runes [i].Value)
            {
                case '\n':
                    if (!keepNewLine)
                    {
                        runes.RemoveAt (i);
                    }

                    break;

                case '\r':
                    if (i + 1 < runes.Count && runes [i + 1].Value == '\n')
                    {
                        runes.RemoveAt (i);

                        if (!keepNewLine)
                        {
                            runes.RemoveAt (i);
                        }

                        i++;
                    }
                    else
                    {
                        if (!keepNewLine)
                        {
                            runes.RemoveAt (i);
                        }
                    }

                    break;
            }
        }

        return StringExtensions.ToString (runes);
    }

    // TODO: Move to StringExtensions?
    private static string ReplaceCRLFWithSpace (string str)
    {
        List<Rune> runes = str.ToRuneList ();

        for (var i = 0; i < runes.Count; i++)
        {
            switch (runes [i].Value)
            {
                case '\n':
                    runes [i] = (Rune)' ';

                    break;

                case '\r':
                    if (i + 1 < runes.Count && runes [i + 1].Value == '\n')
                    {
                        runes [i] = (Rune)' ';
                        runes.RemoveAt (i + 1);
                        i++;
                    }
                    else
                    {
                        runes [i] = (Rune)' ';
                    }

                    break;
            }
        }

        return StringExtensions.ToString (runes);
    }

    // TODO: Move to StringExtensions?
    private static string ReplaceTABWithSpaces (string str, int tabWidth)
    {
        if (tabWidth == 0)
        {
            return str.Replace ("\t", "");
        }

        return str.Replace ("\t", new string (' ', tabWidth));
    }

    // TODO: Move to StringExtensions?
    /// <summary>
    ///     Splits all newlines in the <paramref name="text"/> into a list and supports both CRLF and LF, preserving the
    ///     ending newline.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>A list of text without the newline characters.</returns>
    public static List<string> SplitNewLine (string text)
    {
        List<Rune> runes = text.ToRuneList ();
        List<string> lines = new ();
        var start = 0;

        for (var i = 0; i < runes.Count; i++)
        {
            int end = i;

            switch (runes [i].Value)
            {
                case '\n':
                    lines.Add (StringExtensions.ToString (runes.GetRange (start, end - start)));
                    i++;
                    start = i;

                    break;

                case '\r':
                    if (i + 1 < runes.Count && runes [i + 1].Value == '\n')
                    {
                        lines.Add (StringExtensions.ToString (runes.GetRange (start, end - start)));
                        i += 2;
                        start = i;
                    }
                    else
                    {
                        lines.Add (StringExtensions.ToString (runes.GetRange (start, end - start)));
                        i++;
                        start = i;
                    }

                    break;
            }
        }

        switch (runes.Count)
        {
            case > 0 when lines.Count == 0:
                lines.Add (StringExtensions.ToString (runes));

                break;
            case > 0 when start < runes.Count:
                lines.Add (StringExtensions.ToString (runes.GetRange (start, runes.Count - start)));

                break;
            default:
                lines.Add ("");

                break;
        }

        return lines;
    }

    // TODO: Move to StringExtensions?
    /// <summary>
    ///     Adds trailing whitespace or truncates <paramref name="text"/> so that it fits exactly <paramref name="width"/>
    ///     columns. Note that some unicode characters take 2+ columns
    /// </summary>
    /// <param name="text"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    public static string ClipOrPad (string text, int width)
    {
        if (string.IsNullOrEmpty (text))
        {
            return text;
        }

        // if value is not wide enough
        if (text.EnumerateRunes ().Sum (c => c.GetColumns ()) < width)
        {
            // pad it out with spaces to the given Alignment
            int toPad = width - text.EnumerateRunes ().Sum (c => c.GetColumns ());

            return text + new string (' ', toPad);
        }

        // value is too wide
        return new string (text.TakeWhile (c => (width -= ((Rune)c).GetColumns ()) >= 0).ToArray ());
    }

    /// <summary>Formats the provided text to fit within the width provided using word wrapping.</summary>
    /// <param name="text">The text to word wrap</param>
    /// <param name="width">The number of columns to constrain the text to</param>
    /// <param name="preserveTrailingSpaces">
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be
    ///     preserved. If <see langword="false"/> , trailing spaces at the end of wrapped lines will be trimmed.
    /// </param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <returns>A list of word wrapped lines.</returns>
    /// <remarks>
    ///     <para>This method does not do any alignment.</para>
    ///     <para>This method strips Newline ('\n' and '\r\n') sequences before processing.</para>
    ///     <para>
    ///         If <paramref name="preserveTrailingSpaces"/> is <see langword="false"/> at most one space will be preserved
    ///         at the end of the last line.
    ///     </para>
    /// </remarks>
    /// <returns>A list of lines.</returns>
    public static List<string> WordWrapText (
        string text,
        int width,
        bool preserveTrailingSpaces = false,
        int tabWidth = 0,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        TextFormatter textFormatter = null
    )
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException ($"{nameof (width)} cannot be negative.");
        }

        List<string> lines = new ();

        if (string.IsNullOrEmpty (text))
        {
            return lines;
        }

        List<Rune> runes = StripCRLF (text).ToRuneList ();

        int start = Math.Max (
                              !runes.Contains ((Rune)' ') && textFormatter is { VerticalAlignment: Alignment.End } && IsVerticalDirection (textDirection)
                                  ? runes.Count - width
                                  : 0,
                              0);
        int end;

        if (preserveTrailingSpaces)
        {
            while ((end = start) < runes.Count)
            {
                end = GetNextWhiteSpace (start, width, out bool incomplete);

                if (end == 0 && incomplete)
                {
                    start = text.GetRuneCount ();

                    break;
                }

                lines.Add (StringExtensions.ToString (runes.GetRange (start, end - start)));
                start = end;

                if (incomplete)
                {
                    start = text.GetRuneCount ();

                    break;
                }
            }
        }
        else
        {
            if (IsHorizontalDirection (textDirection))
            {
                while ((end = start
                              + GetLengthThatFits (
                                                   runes.GetRange (start, runes.Count - start),
                                                   width,
                                                   tabWidth,
                                                   textDirection
                                                  ))
                       < runes.Count)
                {
                    while (runes [end].Value != ' ' && end > start)
                    {
                        end--;
                    }

                    if (end == start)
                    {
                        end = start
                              + GetLengthThatFits (
                                                   runes.GetRange (end, runes.Count - end),
                                                   width,
                                                   tabWidth,
                                                   textDirection
                                                  );
                    }

                    var str = StringExtensions.ToString (runes.GetRange (start, end - start));
                    int zeroLength = text.EnumerateRunes ().Sum (r => r.GetColumns () == 0 ? 1 : 0);

                    if (end > start && GetRuneWidth (str, tabWidth, textDirection) <= width + zeroLength)
                    {
                        lines.Add (str);
                        start = end;

                        if (runes [end].Value == ' ')
                        {
                            start++;
                        }
                    }
                    else
                    {
                        end++;
                        start = end;
                    }
                }
            }
            else
            {
                while ((end = start + width) < runes.Count)
                {
                    while (runes [end].Value != ' ' && end > start)
                    {
                        end--;
                    }

                    if (end == start)
                    {
                        end = start + width;
                    }

                    var zeroLength = 0;

                    for (int i = end; i < runes.Count - start; i++)
                    {
                        Rune r = runes [i];

                        if (r.GetColumns () == 0)
                        {
                            zeroLength++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    lines.Add (
                               StringExtensions.ToString (
                                                          runes.GetRange (
                                                                          start,
                                                                          end - start + zeroLength
                                                                         )
                                                         )
                              );
                    end += zeroLength;
                    start = end;

                    if (runes [end].Value == ' ')
                    {
                        start++;
                    }
                }
            }
        }

        int GetNextWhiteSpace (int from, int cWidth, out bool incomplete, int cLength = 0)
        {
            int to = from;
            int length = cLength;
            incomplete = false;

            while (length < cWidth && to < runes.Count)
            {
                Rune rune = runes [to];

                if (IsHorizontalDirection (textDirection))
                {
                    length += rune.GetColumns ();
                }
                else
                {
                    length++;
                }

                if (length > cWidth)
                {
                    if (to >= runes.Count || (length > 1 && cWidth <= 1))
                    {
                        incomplete = true;
                    }

                    return to;
                }

                switch (rune.Value)
                {
                    case ' ' when length == cWidth:
                        return to + 1;
                    case ' ' when length > cWidth:
                        return to;
                    case ' ':
                        return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
                    case '\t':
                        {
                            length += tabWidth + 1;

                            if (length == tabWidth && tabWidth > cWidth)
                            {
                                return to + 1;
                            }

                            if (length > cWidth && tabWidth > cWidth)
                            {
                                return to;
                            }

                            return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
                        }
                    default:
                        to++;

                        break;
                }
            }

            return cLength switch
            {
                > 0 when to < runes.Count && runes [to].Value != ' ' && runes [to].Value != '\t' => from,
                > 0 when to < runes.Count && (runes [to].Value == ' ' || runes [to].Value == '\t') => from,
                _ => to
            };
        }

        if (start < text.GetRuneCount ())
        {
            string str = ReplaceTABWithSpaces (
                                               StringExtensions.ToString (runes.GetRange (start, runes.Count - start)),
                                               tabWidth
                                              );

            if (IsVerticalDirection (textDirection) || preserveTrailingSpaces || str.GetColumns () <= width)
            {
                lines.Add (str);
            }
        }

        return lines;
    }

    /// <summary>Justifies text within a specified width.</summary>
    /// <param name="text">The text to justify.</param>
    /// <param name="width">
    ///     The number of columns to clip the text to. Text longer than <paramref name="width"/> will be
    ///     clipped.
    /// </param>
    /// <param name="textAlignment">Alignment.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <returns>Justified and clipped text.</returns>
    public static string ClipAndJustify (
        string text,
        int width,
        Alignment textAlignment,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        int tabWidth = 0,
        TextFormatter textFormatter = null
    )
    {
        return ClipAndJustify (text, width, textAlignment == Alignment.Fill, textDirection, tabWidth, textFormatter);
    }

    /// <summary>Justifies text within a specified width.</summary>
    /// <param name="text">The text to justify.</param>
    /// <param name="width">
    ///     The number of columns to clip the text to. Text longer than <paramref name="width"/> will be
    ///     clipped.
    /// </param>
    /// <param name="justify">Justify.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <returns>Justified and clipped text.</returns>
    public static string ClipAndJustify (
        string text,
        int width,
        bool justify,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        int tabWidth = 0,
        TextFormatter textFormatter = null
    )
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException ($"{nameof (width)} cannot be negative.");
        }

        if (string.IsNullOrEmpty (text))
        {
            return text;
        }

        text = ReplaceTABWithSpaces (text, tabWidth);
        List<Rune> runes = text.ToRuneList ();
        int zeroLength = runes.Sum (r => r.GetColumns () == 0 ? 1 : 0);

        if (runes.Count - zeroLength > width)
        {
            if (IsHorizontalDirection (textDirection))
            {
                if (textFormatter is { Alignment: Alignment.End })
                {
                    return GetRangeThatFits (runes, runes.Count - width, text, width, tabWidth, textDirection);
                }

                if (textFormatter is { Alignment: Alignment.Center })
                {
                    return GetRangeThatFits (runes, Math.Max ((runes.Count - width) / 2, 0), text, width, tabWidth, textDirection);
                }

                return GetRangeThatFits (runes, 0, text, width, tabWidth, textDirection);
            }

            if (IsVerticalDirection (textDirection))
            {
                if (textFormatter is { VerticalAlignment: Alignment.End })
                {
                    return GetRangeThatFits (runes, runes.Count - width, text, width, tabWidth, textDirection);
                }

                if (textFormatter is { VerticalAlignment: Alignment.Center })
                {
                    return GetRangeThatFits (runes, Math.Max ((runes.Count - width) / 2, 0), text, width, tabWidth, textDirection);
                }

                return GetRangeThatFits (runes, 0, text, width, tabWidth, textDirection);
            }

            return StringExtensions.ToString (runes.GetRange (0, width + zeroLength));
        }

        if (justify)
        {
            return Justify (text, width, ' ', textDirection, tabWidth);
        }

        if (IsHorizontalDirection (textDirection))
        {
            if (textFormatter is { Alignment: Alignment.End })
            {
                if (GetRuneWidth (text, tabWidth, textDirection) > width)
                {
                    return GetRangeThatFits (runes, runes.Count - width, text, width, tabWidth, textDirection);
                }
            }
            else if (textFormatter is { Alignment: Alignment.Center })
            {
                return GetRangeThatFits (runes, Math.Max ((runes.Count - width) / 2, 0), text, width, tabWidth, textDirection);
            }
            else if (GetRuneWidth (text, tabWidth, textDirection) > width)
            {
                return GetRangeThatFits (runes, 0, text, width, tabWidth, textDirection);
            }
        }

        if (IsVerticalDirection (textDirection))
        {
            if (textFormatter is { VerticalAlignment: Alignment.End })
            {
                if (runes.Count - zeroLength > width)
                {
                    return GetRangeThatFits (runes, runes.Count - width, text, width, tabWidth, textDirection);
                }
            }
            else if (textFormatter is { VerticalAlignment: Alignment.Center })
            {
                return GetRangeThatFits (runes, Math.Max ((runes.Count - width) / 2, 0), text, width, tabWidth, textDirection);
            }
            else if (runes.Count - zeroLength > width)
            {
                return GetRangeThatFits (runes, 0, text, width, tabWidth, textDirection);
            }
        }

        return text;
    }

    private static string GetRangeThatFits (List<Rune> runes, int index, string text, int width, int tabWidth, TextDirection textDirection)
    {
        return StringExtensions.ToString (
                                          runes.GetRange (
                                                          Math.Max (index, 0),
                                                          GetLengthThatFits (text, width, tabWidth, textDirection)
                                                         )
                                         );
    }

    /// <summary>
    ///     Justifies the text to fill the width provided. Space will be added between words to make the text just fit
    ///     <c>width</c>. Spaces will not be added to the start or end.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="width"></param>
    /// <param name="spaceChar">Character to replace whitespace and pad with. For debugging purposes.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The justified text.</returns>
    public static string Justify (
        string text,
        int width,
        char spaceChar = ' ',
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        int tabWidth = 0
    )
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException ($"{nameof (width)} cannot be negative.");
        }

        if (string.IsNullOrEmpty (text))
        {
            return text;
        }

        text = ReplaceTABWithSpaces (text, tabWidth);
        string [] words = text.Split (' ');
        int textCount;

        if (IsHorizontalDirection (textDirection))
        {
            textCount = words.Sum (arg => GetRuneWidth (arg, tabWidth, textDirection));
        }
        else
        {
            textCount = words.Sum (arg => arg.GetRuneCount ());
        }

        int spaces = words.Length > 1 ? (width - textCount) / (words.Length - 1) : 0;
        int extras = words.Length > 1 ? (width - textCount) % (words.Length - 1) : 0;

        var s = new StringBuilder ();

        for (var w = 0; w < words.Length; w++)
        {
            string x = words [w];
            s.Append (x);

            if (w + 1 < words.Length)
            {
                for (var i = 0; i < spaces; i++)
                {
                    s.Append (spaceChar);
                }
            }

            if (extras > 0)
            {
                for (var i = 0; i < 1; i++)
                {
                    s.Append (spaceChar);
                }

                extras--;
            }

            if (w + 1 == words.Length - 1)
            {
                for (var i = 0; i < extras; i++)
                {
                    s.Append (spaceChar);
                }
            }
        }

        return s.ToString ();
    }

    /// <summary>Formats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.</summary>
    /// <param name="text"></param>
    /// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
    /// <param name="textAlignment">Specifies how the text will be aligned horizontally.</param>
    /// <param name="wordWrap">
    ///     If <see langword="true"/>, the text will be wrapped to new lines no longer than
    ///     <paramref name="width"/>. If <see langword="false"/>, forces text to fit a single line. Line breaks are converted
    ///     to spaces. The text will be clipped to <paramref name="width"/>.
    /// </param>
    /// <param name="preserveTrailingSpaces">
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be
    ///     preserved. If <see langword="false"/> , trailing spaces at the end of wrapped lines will be trimmed.
    /// </param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="multiLine">If <see langword="true"/> new lines are allowed.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <returns>A list of word wrapped lines.</returns>
    /// <remarks>
    ///     <para>An empty <paramref name="text"/> string will result in one empty line.</para>
    ///     <para>If <paramref name="width"/> is 0, a single, empty line will be returned.</para>
    ///     <para>If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible.</para>
    /// </remarks>
    public static List<string> Format (
        string text,
        int width,
        Alignment textAlignment,
        bool wordWrap,
        bool preserveTrailingSpaces = false,
        int tabWidth = 0,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        bool multiLine = false,
        TextFormatter textFormatter = null
    )
    {
        return Format (
                       text,
                       width,
                       textAlignment == Alignment.Fill,
                       wordWrap,
                       preserveTrailingSpaces,
                       tabWidth,
                       textDirection,
                       multiLine,
                       textFormatter
                      );
    }

    /// <summary>Formats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.</summary>
    /// <param name="text"></param>
    /// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
    /// <param name="justify">Specifies whether the text should be justified.</param>
    /// <param name="wordWrap">
    ///     If <see langword="true"/>, the text will be wrapped to new lines no longer than
    ///     <paramref name="width"/>. If <see langword="false"/>, forces text to fit a single line. Line breaks are converted
    ///     to spaces. The text will be clipped to <paramref name="width"/>.
    /// </param>
    /// <param name="preserveTrailingSpaces">
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be
    ///     preserved. If <see langword="false"/> , trailing spaces at the end of wrapped lines will be trimmed.
    /// </param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <param name="multiLine">If <see langword="true"/> new lines are allowed.</param>
    /// <param name="textFormatter"><see cref="TextFormatter"/> instance to access any of his objects.</param>
    /// <returns>A list of word wrapped lines.</returns>
    /// <remarks>
    ///     <para>An empty <paramref name="text"/> string will result in one empty line.</para>
    ///     <para>If <paramref name="width"/> is 0, a single, empty line will be returned.</para>
    ///     <para>If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible.</para>
    /// </remarks>
    public static List<string> Format (
        string text,
        int width,
        bool justify,
        bool wordWrap,
        bool preserveTrailingSpaces = false,
        int tabWidth = 0,
        TextDirection textDirection = TextDirection.LeftRight_TopBottom,
        bool multiLine = false,
        TextFormatter textFormatter = null
    )
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException ($"{nameof (width)} cannot be negative.");
        }

        List<string> lineResult = new ();

        if (string.IsNullOrEmpty (text) || width == 0)
        {
            lineResult.Add (string.Empty);

            return lineResult;
        }

        if (!wordWrap)
        {
            text = ReplaceTABWithSpaces (text, tabWidth);

            if (multiLine)
            {
                // Abhorrent case: Just a new line
                if (text == "\n")
                {
                    lineResult.Add (string.Empty);

                    return lineResult;
                }

                string [] lines = null;

                if (text.Contains ("\r\n"))
                {
                    lines = text.Split ("\r\n");
                }
                else if (text.Contains ('\n'))
                {
                    lines = text.Split ('\n');
                }

                lines ??= new [] { text };

                foreach (string line in lines)
                {

                    lineResult.Add (ClipAndJustify (PerformCorrectFormatDirection (textDirection, line), width, justify, textDirection, tabWidth, textFormatter));
                }

                return PerformCorrectFormatDirection (textDirection, lineResult);
            }

            text = ReplaceCRLFWithSpace (text);
            lineResult.Add (ClipAndJustify (PerformCorrectFormatDirection (textDirection, text), width, justify, textDirection, tabWidth, textFormatter));

            return PerformCorrectFormatDirection (textDirection, lineResult);
        }

        List<Rune> runes = StripCRLF (text, true).ToRuneList ();
        int runeCount = runes.Count;
        var lp = 0;

        for (var i = 0; i < runeCount; i++)
        {
            Rune c = runes [i];

            if (c.Value == '\n')
            {
                List<string> wrappedLines =
                    WordWrapText (
                                  StringExtensions.ToString (PerformCorrectFormatDirection (textDirection, runes.GetRange (lp, i - lp))),
                                  width,
                                  preserveTrailingSpaces,
                                  tabWidth,
                                  textDirection,
                                  textFormatter
                                 );

                foreach (string line in wrappedLines)
                {
                    lineResult.Add (ClipAndJustify (line, width, justify, textDirection, tabWidth));
                }

                if (wrappedLines.Count == 0)
                {
                    lineResult.Add (string.Empty);
                }

                lp = i + 1;
            }
        }

        foreach (string line in WordWrapText (
                                              StringExtensions.ToString (PerformCorrectFormatDirection (textDirection, runes.GetRange (lp, runeCount - lp))),
                                              width,
                                              preserveTrailingSpaces,
                                              tabWidth,
                                              textDirection,
                                              textFormatter
                                             ))
        {
            lineResult.Add (ClipAndJustify (line, width, justify, textDirection, tabWidth));
        }

        return PerformCorrectFormatDirection (textDirection, lineResult);
    }

    private static string PerformCorrectFormatDirection (TextDirection textDirection, string line)
    {
        return textDirection switch
               {
                   TextDirection.RightLeft_BottomTop
                       or TextDirection.RightLeft_TopBottom
                       or TextDirection.BottomTop_LeftRight
                       or TextDirection.BottomTop_RightLeft => StringExtensions.ToString (line.EnumerateRunes ().Reverse ()),
                   _ => line
               };
    }

    private static List<Rune> PerformCorrectFormatDirection (TextDirection textDirection, List<Rune> runes)
    {
        return PerformCorrectFormatDirection (textDirection, StringExtensions.ToString (runes)).ToRuneList ();
    }

    private static List<string> PerformCorrectFormatDirection (TextDirection textDirection, List<string> lines)
    {
        return textDirection switch
               {
                   TextDirection.TopBottom_RightLeft
                       or TextDirection.LeftRight_BottomTop
                       or TextDirection.RightLeft_BottomTop
                       or TextDirection.BottomTop_RightLeft => lines.ToArray ().Reverse ().ToList (),
                   _ => lines
               };
    }

    /// <summary>Returns the number of lines needed to render the specified text given the width.</summary>
    /// <remarks>Calls <see cref="Format()"/>.</remarks>
    /// <returns>Number of lines.</returns>
    /// <param name="text">Text, may contain newlines.</param>
    /// <param name="width">The minimum width for the text.</param>
    public static int GetLineCount (string text, int width)
    {
        List<string> result = Format (text, width, false, true);

        return result.Count;
    }

    /// <summary>
    ///     Returns the number of columns required to render <paramref name="lines"/> oriented vertically.
    /// </summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="lines">The lines.</param>
    /// <param name="startLine">The line in the list to start with (any lines before will be ignored).</param>
    /// <param name="linesCount">The number of lines to process (if less than <c>lines.Count</c>, any lines after will be ignored).</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The width required.</returns>
    public static int GetColumnsRequiredForVerticalText (
        List<string> lines,
        int startLine = -1,
        int linesCount = -1,
        int tabWidth = 0
    )
    {
        var max = 0;

        for (int i = startLine == -1 ? 0 : startLine;
             i < (linesCount == -1 ? lines.Count : startLine + linesCount);
             i++)
        {
            string runes = lines [i];
            if (runes.Length > 0)
            {
                max += runes.EnumerateRunes ().Max (r => GetRuneWidth (r, tabWidth));
            }
        }
        return max;
    }

    /// <summary>
    ///     Returns the number of columns in the widest line in the text, without word wrap, accounting for wide-glyphs
    ///     (uses <see cref="StringExtensions.GetColumns"/>). <paramref name="text"/> if it contains newlines.
    /// </summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="text">Text, may contain newlines.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The length of the longest line.</returns>
    public static int GetWidestLineLength (string text, int tabWidth = 0)
    {
        List<string> result = SplitNewLine (text);

        return result.Max (x => GetRuneWidth (x, tabWidth));
    }

    /// <summary>
    ///     Gets the maximum number of columns from the text based on the <paramref name="startIndex"/> and the
    ///     <paramref name="length"/>.
    /// </summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="text">The text.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="length">The length.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The maximum characters width.</returns>
    public static int GetSumMaxCharWidth (string text, int startIndex = -1, int length = -1, int tabWidth = 0)
    {
        var max = 0;
        Rune [] runes = text.ToRunes ();

        for (int i = startIndex == -1 ? 0 : startIndex;
             i < (length == -1 ? runes.Length : startIndex + length);
             i++)
        {
            max += GetRuneWidth (runes [i], tabWidth);
        }

        return max;
    }

    /// <summary>Gets the number of the Runes in the text that will fit in <paramref name="width"/>.</summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="text">The text.</param>
    /// <param name="width">The width.</param>
    /// <param name="tabWidth">The width used for a tab.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <returns>The index of the text that fit the width.</returns>
    public static int GetLengthThatFits (string text, int width, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
    {
        return GetLengthThatFits (text?.ToRuneList (), width, tabWidth, textDirection);
    }

    /// <summary>Gets the number of the Runes in a list of Runes that will fit in <paramref name="width"/>.</summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="runes">The list of runes.</param>
    /// <param name="width">The width.</param>
    /// <param name="tabWidth">The width used for a tab.</param>
    /// <param name="textDirection">The text direction.</param>
    /// <returns>The index of the last Rune in <paramref name="runes"/> that fit in <paramref name="width"/>.</returns>
    public static int GetLengthThatFits (List<Rune> runes, int width, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
    {
        if (runes is null || runes.Count == 0)
        {
            return 0;
        }

        var runesLength = 0;
        var runeIdx = 0;

        for (; runeIdx < runes.Count; runeIdx++)
        {
            int runeWidth = GetRuneWidth (runes [runeIdx], tabWidth, textDirection);

            if (runesLength + runeWidth > width)
            {
                break;
            }

            runesLength += runeWidth;
        }

        return runeIdx;
    }

    private static int GetRuneWidth (string str, int tabWidth, TextDirection textDirection = TextDirection.LeftRight_TopBottom) { return GetRuneWidth (str.EnumerateRunes ().ToList (), tabWidth, textDirection); }
    private static int GetRuneWidth (List<Rune> runes, int tabWidth, TextDirection textDirection = TextDirection.LeftRight_TopBottom) { return runes.Sum (r => GetRuneWidth (r, tabWidth, textDirection)); }

    private static int GetRuneWidth (Rune rune, int tabWidth, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
    {
        int runeWidth = IsHorizontalDirection (textDirection) ? rune.GetColumns () : 1;

        if (rune.Value == '\t')
        {
            return tabWidth;
        }

        if (runeWidth < 0 || runeWidth > 0)
        {
            return Math.Max (runeWidth, 1);
        }

        return runeWidth;
    }

    /// <summary>Gets the index position from the list based on the <paramref name="width"/>.</summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="lines">The lines.</param>
    /// <param name="width">The width.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns>The index of the list that fit the width.</returns>
    public static int GetMaxColsForWidth (List<string> lines, int width, int tabWidth = 0)
    {
        var runesLength = 0;
        var lineIdx = 0;

        for (; lineIdx < lines.Count; lineIdx++)
        {
            List<Rune> runes = lines [lineIdx].ToRuneList ();

            int maxRruneWidth = runes.Count > 0
                                    ? runes.Max (r => GetRuneWidth (r, tabWidth))
                                    : 1;

            if (runesLength + maxRruneWidth > width)
            {
                break;
            }

            runesLength += maxRruneWidth;
        }

        return lineIdx;
    }

    /// <summary>Calculates the rectangle required to hold text, assuming no word wrapping or alignment.</summary>
    /// <remarks>
    ///     This API will return incorrect results if the text includes glyphs whose width is dependent on surrounding
    ///     glyphs (e.g. Arabic).
    /// </remarks>
    /// <param name="x">The x location of the rectangle</param>
    /// <param name="y">The y location of the rectangle</param>
    /// <param name="text">The text to measure</param>
    /// <param name="direction">The text direction.</param>
    /// <param name="tabWidth">The number of columns used for a tab.</param>
    /// <returns></returns>
    public static Rectangle CalcRect (
        int x,
        int y,
        string text,
        TextDirection direction = TextDirection.LeftRight_TopBottom,
        int tabWidth = 0
    )
    {
        if (string.IsNullOrEmpty (text))
        {
            return new (new (x, y), Size.Empty);
        }

        int w, h;

        if (IsHorizontalDirection (direction))
        {
            var mw = 0;
            var ml = 1;

            var cols = 0;

            foreach (Rune rune in text.EnumerateRunes ())
            {
                if (rune.Value == '\n')
                {
                    ml++;

                    if (cols > mw)
                    {
                        mw = cols;
                    }

                    cols = 0;
                }
                else if (rune.Value != '\r')
                {
                    cols++;
                    var rw = 0;

                    if (rune.Value == '\t')
                    {
                        rw += tabWidth - 1;
                    }
                    else
                    {
                        rw = rune.GetColumns ();

                        if (rw > 0)
                        {
                            rw--;
                        }
                        else if (rw == 0)
                        {
                            cols--;
                        }
                    }

                    cols += rw;
                }
            }

            if (cols > mw)
            {
                mw = cols;
            }

            w = mw;
            h = ml;
        }
        else
        {
            int vw = 1, cw = 1;
            var vh = 0;

            var rows = 0;

            foreach (Rune rune in text.EnumerateRunes ())
            {
                if (rune.Value == '\n')
                {
                    vw++;

                    if (rows > vh)
                    {
                        vh = rows;
                    }

                    rows = 0;
                    cw = 1;
                }
                else if (rune.Value != '\r')
                {
                    rows++;
                    var rw = 0;

                    if (rune.Value == '\t')
                    {
                        rw += tabWidth - 1;
                        rows += rw;
                    }
                    else
                    {
                        rw = rune.GetColumns ();

                        if (rw == 0)
                        {
                            rows--;
                        }
                        else if (cw < rw)
                        {
                            cw = rw;
                            vw++;
                        }
                    }
                }
            }

            if (rows > vh)
            {
                vh = rows;
            }

            w = vw;
            h = vh;
        }

        return new (x, y, w, h);
    }

    /// <summary>Finds the HotKey and its location in text.</summary>
    /// <param name="text">The text to look in.</param>
    /// <param name="hotKeySpecifier">The HotKey specifier (e.g. '_') to look for.</param>
    /// <param name="hotPos">Outputs the Rune index into <c>text</c>.</param>
    /// <param name="hotKey">Outputs the hotKey. <see cref="Key.Empty"/> if not found.</param>
    /// <param name="firstUpperCase">
    ///     If <c>true</c> the legacy behavior of identifying the first upper case character as the
    ///     HotKey will be enabled. Regardless of the value of this parameter, <c>hotKeySpecifier</c> takes precedence.
    ///     Defaults to <see langword="false"/>.
    /// </param>
    /// <returns><c>true</c> if a HotKey was found; <c>false</c> otherwise.</returns>
    public static bool FindHotKey (
        string text,
        Rune hotKeySpecifier,
        out int hotPos,
        out Key hotKey,
        bool firstUpperCase = false
    )
    {
        if (string.IsNullOrEmpty (text) || hotKeySpecifier == (Rune)0xFFFF)
        {
            hotPos = -1;
            hotKey = Key.Empty;

            return false;
        }

        var curHotKey = (Rune)0;
        int curHotPos = -1;

        // Use first hot_key char passed into 'hotKey'.
        // TODO: Ignore hot_key of two are provided
        // TODO: Do not support non-alphanumeric chars that can't be typed
        var i = 0;

        foreach (Rune c in text.EnumerateRunes ())
        {
            if ((char)c.Value != 0xFFFD)
            {
                if (c == hotKeySpecifier)
                {
                    curHotPos = i;
                }
                else if (curHotPos > -1)
                {
                    curHotKey = c;

                    break;
                }
            }

            i++;
        }

        // Legacy support - use first upper case char if the specifier was not found
        if (curHotPos == -1 && firstUpperCase)
        {
            i = 0;

            foreach (Rune c in text.EnumerateRunes ())
            {
                if ((char)c.Value != 0xFFFD)
                {
                    if (Rune.IsUpper (c))
                    {
                        curHotKey = c;
                        curHotPos = i;

                        break;
                    }
                }

                i++;
            }
        }

        if (curHotKey != (Rune)0 && curHotPos != -1)
        {
            hotPos = curHotPos;

            var newHotKey = (KeyCode)curHotKey.Value;

            if (newHotKey != KeyCode.Null && !(newHotKey == KeyCode.Space || Rune.IsControl (curHotKey)))
            {
                if ((newHotKey & ~KeyCode.Space) is >= KeyCode.A and <= KeyCode.Z)
                {
                    newHotKey &= ~KeyCode.Space;
                }

                hotKey = newHotKey;

                //hotKey.Scope = KeyBindingScope.HotKey;

                return true;
            }
        }

        hotPos = -1;
        hotKey = KeyCode.Null;

        return false;
    }

    /// <summary>
    ///     Replaces the Rune at the index specified by the <c>hotPos</c> parameter with a tag identifying it as the
    ///     hotkey.
    /// </summary>
    /// <param name="text">The text to tag the hotkey in.</param>
    /// <param name="hotPos">The Rune index of the hotkey in <c>text</c>.</param>
    /// <returns>The text with the hotkey tagged.</returns>
    /// <remarks>The returned string will not render correctly without first un-doing the tag. To undo the tag, search for</remarks>
    public static string ReplaceHotKeyWithTag (string text, int hotPos)
    {
        // Set the high bit
        List<Rune> runes = text.ToRuneList ();

        if (Rune.IsLetterOrDigit (runes [hotPos]))
        {
            runes [hotPos] = new Rune ((uint)runes [hotPos].Value);
        }

        return StringExtensions.ToString (runes);
    }

    /// <summary>Removes the hotkey specifier from text.</summary>
    /// <param name="text">The text to manipulate.</param>
    /// <param name="hotKeySpecifier">The hot-key specifier (e.g. '_') to look for.</param>
    /// <param name="hotPos">Returns the position of the hot-key in the text. -1 if not found.</param>
    /// <returns>The input text with the hotkey specifier ('_') removed.</returns>
    public static string RemoveHotKeySpecifier (string text, int hotPos, Rune hotKeySpecifier)
    {
        if (string.IsNullOrEmpty (text))
        {
            return text;
        }

        // Scan 
        var start = string.Empty;
        var i = 0;

        foreach (Rune c in text)
        {
            if (c == hotKeySpecifier && i == hotPos)
            {
                i++;

                continue;
            }

            start += c;
            i++;
        }

        return start;
    }

    #endregion // Static Members
}