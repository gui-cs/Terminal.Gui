//
// HexView.cs: A hexadecimal viewer
//
// TODO:
// - Support searching and highlighting of the search result
// - Bug showing the last line
// 

namespace Terminal.Gui;

/// <summary>An hex viewer and editor <see cref="View"/> over a <see cref="System.IO.Stream"/></summary>
/// <remarks>
///     <para>
///         <see cref="HexView"/> provides a hex editor on top of a seekable <see cref="Stream"/> with the left side
///         showing an hex dump of the values in the <see cref="Stream"/> and the right side showing the contents (filtered
///         to non-control sequence ASCII characters).
///     </para>
///     <para>Users can switch from one side to the other by using the tab key.</para>
///     <para>
///         To enable editing, set <see cref="AllowEdits"/> to true. When <see cref="AllowEdits"/> is true the user can
///         make changes to the hexadecimal values of the <see cref="Stream"/>. Any changes are tracked in the
///         <see cref="Edits"/> property (a <see cref="SortedDictionary{TKey, TValue}"/>) indicating the position where the
///         changes were made and the new values. A convenience method, <see cref="ApplyEdits"/> will apply the edits to
///         the <see cref="Stream"/>.
///     </para>
///     <para>Control the first byte shown by setting the <see cref="DisplayStart"/> property to an offset in the stream.</para>
/// </remarks>
public class HexView : View
{
    private const int bsize = 4;
    private const int displayWidth = 9;

    private int bpl;
    private CursorVisibility desiredCursorVisibility = CursorVisibility.Default;
    private long displayStart, pos;
    private SortedDictionary<long, byte> edits = [];
    private bool firstNibble, leftSide;
    private Stream source;
    private static readonly Rune SpaceCharRune = new (' ');
    private static readonly Rune PeriodCharRune = new ('.');

    /// <summary>Initializes a <see cref="HexView"/> class using <see cref="LayoutStyle.Computed"/> layout.</summary>
    /// <param name="source">
    ///     The <see cref="Stream"/> to view and edit as hex, this <see cref="Stream"/> must support seeking,
    ///     or an exception will be thrown.
    /// </param>
    public HexView (Stream source)
    {
        Source = source;
        // BUG: This will always call the most-derived definition of CanFocus.
        // Either seal it or don't set it here.
        CanFocus = true;
        leftSide = true;
        firstNibble = true;

        // PERF: Closure capture of 'this' creates a lot of overhead.
        // BUG: Closure capture of 'this' may have unexpected results depending on how this is called.
        // The above two comments apply to all of the lambdas passed to all calls to AddCommand below.
        // Things this view knows how to do
        AddCommand (Command.Left, () => MoveLeft ());
        AddCommand (Command.Right, () => MoveRight ());
        AddCommand (Command.LineDown, () => MoveDown (bytesPerLine));
        AddCommand (Command.LineUp, () => MoveUp (bytesPerLine));
        AddCommand (Command.Accept, () => ToggleSide ());
        AddCommand (Command.PageUp, () => MoveUp (bytesPerLine * Frame.Height));
        AddCommand (Command.PageDown, () => MoveDown (bytesPerLine * Frame.Height));
        AddCommand (Command.TopHome, () => MoveHome ());
        AddCommand (Command.BottomEnd, () => MoveEnd ());
        AddCommand (Command.StartOfLine, () => MoveStartOfLine ());
        AddCommand (Command.EndOfLine, () => MoveEndOfLine ());
        AddCommand (Command.StartOfPage, () => MoveUp (bytesPerLine * ((int)(position - displayStart) / bytesPerLine)));

        AddCommand (
                    Command.EndOfPage,
                    () => MoveDown (bytesPerLine * (Frame.Height - 1 - (int)(position - displayStart) / bytesPerLine))
                   );

        // Default keybindings for this view
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorDown, Command.LineDown);
        KeyBindings.Add (Key.CursorUp, Command.LineUp);
        KeyBindings.Add (Key.Enter, Command.Accept);

        KeyBindings.Add (Key.V.WithAlt, Command.PageUp);
        KeyBindings.Add (Key.PageUp, Command.PageUp);

        KeyBindings.Add (Key.V.WithCtrl, Command.PageDown);
        KeyBindings.Add (Key.PageDown, Command.PageDown);

        KeyBindings.Add (Key.Home, Command.TopHome);
        KeyBindings.Add (Key.End, Command.BottomEnd);
        KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.StartOfLine);
        KeyBindings.Add (Key.CursorRight.WithCtrl, Command.EndOfLine);
        KeyBindings.Add (Key.CursorUp.WithCtrl, Command.StartOfPage);
        KeyBindings.Add (Key.CursorDown.WithCtrl, Command.EndOfPage);

        LayoutComplete += HexView_LayoutComplete;
    }

    /// <summary>Initializes a <see cref="HexView"/> class using <see cref="LayoutStyle.Computed"/> layout.</summary>
    public HexView () : this (new MemoryStream ()) { }

    /// <summary>
    ///     Gets or sets whether this <see cref="HexView"/> allow editing of the <see cref="Stream"/> of the underlying
    ///     <see cref="Stream"/>.
    /// </summary>
    /// <value><c>true</c> if allow edits; otherwise, <c>false</c>.</value>
    public bool AllowEdits { get; set; } = true;

    /// <summary>The bytes length per line.</summary>
    public int BytesPerLine => bytesPerLine;

    /// <summary>Gets the current cursor position starting at one for both, line and column.</summary>
    public Point CursorPosition
    {
        get
        {
            if (!IsInitialized)
            {
                return Point.Empty;
            }

            var delta = (int)position;
            int line = delta / bytesPerLine + 1;
            int item = delta % bytesPerLine + 1;

            return new Point (item, line);
        }
    }

    /// <summary>Get / Set the wished cursor when the field is focused</summary>
    public CursorVisibility DesiredCursorVisibility
    {
        get => desiredCursorVisibility;
        set
        {
            if (desiredCursorVisibility != value && HasFocus)
            {
                Application.Driver.SetCursorVisibility (value);
            }

            desiredCursorVisibility = value;
        }
    }

    /// <summary>
    ///     Sets or gets the offset into the <see cref="Stream"/> that will displayed at the top of the
    ///     <see cref="HexView"/>
    /// </summary>
    /// <value>The display start.</value>
    public long DisplayStart
    {
        get => displayStart;
        set
        {
            position = value;

            SetDisplayStart (value);
        }
    }

    /// <summary>
    ///     Gets a <see cref="SortedDictionary{TKey, TValue}"/> describing the edits done to the <see cref="HexView"/>.
    ///     Each Key indicates an offset where an edit was made and the Value is the changed byte.
    /// </summary>
    /// <value>The edits.</value>
    public IReadOnlyDictionary<long, byte> Edits => edits;

    /// <summary>Gets the current character position starting at one, related to the <see cref="Stream"/>.</summary>
    public long Position => position + 1;

    /// <summary>
    ///     Sets or gets the <see cref="Stream"/> the <see cref="HexView"/> is operating on; the stream must support
    ///     seeking ( <see cref="Stream.CanSeek"/> == true).
    /// </summary>
    /// <value>The source.</value>
    public Stream Source
    {
        get => source;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException ("source");
            }

            if (!value.CanSeek)
            {
                throw new ArgumentException ("The source stream must be seekable (CanSeek property)", "source");
            }

            source = value;

            if (displayStart > source.Length)
            {
                DisplayStart = 0;
            }

            if (position > source.Length)
            {
                position = 0;
            }

            SetNeedsDisplay ();
        }
    }

    private int bytesPerLine
    {
        get => bpl;
        set
        {
            bpl = value;
            OnPositionChanged ();
        }
    }

    private long position
    {
        get => pos;
        set
        {
            pos = value;
            OnPositionChanged ();
        }
    }

    /// <summary>
    ///     This method applies and edits made to the <see cref="Stream"/> and resets the contents of the
    ///     <see cref="Edits"/> property.
    /// </summary>
    /// <param name="stream">If provided also applies the changes to the passed <see cref="Stream"/></param>
    /// .
    public void ApplyEdits (Stream stream = null)
    {
        foreach (KeyValuePair<long, byte> kv in edits)
        {
            source.Position = kv.Key;
            source.WriteByte (kv.Value);
            source.Flush ();

            if (stream is { })
            {
                stream.Position = kv.Key;
                stream.WriteByte (kv.Value);
                stream.Flush ();
            }
        }

        edits = new SortedDictionary<long, byte> ();
        SetNeedsDisplay ();
    }

    /// <summary>
    ///     This method discards the edits made to the <see cref="Stream"/> by resetting the contents of the
    ///     <see cref="Edits"/> property.
    /// </summary>
    public void DiscardEdits () { edits = new SortedDictionary<long, byte> (); }

    /// <summary>Event to be invoked when an edit is made on the <see cref="Stream"/>.</summary>
    public event EventHandler<HexViewEditEventArgs> Edited;

    /// <inheritdoc/>
    protected internal override bool OnMouseEvent  (MouseEvent me)
    {
        // BUGBUG: Test this with a border! Assumes Frame == Viewport!

        if (!me.Flags.HasFlag (MouseFlags.Button1Clicked)
            && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
            && !me.Flags.HasFlag (MouseFlags.WheeledDown)
            && !me.Flags.HasFlag (MouseFlags.WheeledUp))
        {
            return false;
        }

        if (!HasFocus)
        {
            SetFocus ();
        }

        if (me.Flags == MouseFlags.WheeledDown)
        {
            DisplayStart = Math.Min (DisplayStart + bytesPerLine, source.Length);

            return true;
        }

        if (me.Flags == MouseFlags.WheeledUp)
        {
            DisplayStart = Math.Max (DisplayStart - bytesPerLine, 0);

            return true;
        }

        if (me.X < displayWidth)
        {
            return true;
        }

        int nblocks = bytesPerLine / bsize;
        int blocksSize = nblocks * 14;
        int blocksRightOffset = displayWidth + blocksSize - 1;

        if (me.X > blocksRightOffset + bytesPerLine - 1)
        {
            return true;
        }

        leftSide = me.X >= blocksRightOffset;
        long lineStart = me.Y * bytesPerLine + displayStart;
        int x = me.X - displayWidth + 1;
        int block = x / 14;
        x -= block * 2;
        int empty = x % 3;
        int item = x / 3;

        if (!leftSide && item > 0 && (empty == 0 || x == block * 14 + 14 - 1 - block * 2))
        {
            return true;
        }

        firstNibble = true;

        if (leftSide)
        {
            position = Math.Min (lineStart + me.X - blocksRightOffset, source.Length);
        }
        else
        {
            position = Math.Min (lineStart + item, source.Length);
        }

        if (me.Flags == MouseFlags.Button1DoubleClicked)
        {
            leftSide = !leftSide;

            if (leftSide)
            {
                firstNibble = empty == 1;
            }
            else
            {
                firstNibble = true;
            }
        }

        SetNeedsDisplay ();

        return true;
    }

    ///<inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        Attribute currentAttribute;
        Attribute current = ColorScheme.Focus;
        Driver.SetAttribute (current);
        Move (0, 0);

        // BUGBUG: Viewport!!!!
        Rectangle frame = Frame;

        int nblocks = bytesPerLine / bsize;
        var data = new byte [nblocks * bsize * frame.Height];
        Source.Position = displayStart;
        int n = source.Read (data, 0, data.Length);

        Attribute activeColor = ColorScheme.HotNormal;
        Attribute trackingColor = ColorScheme.HotFocus;

        for (var line = 0; line < frame.Height; line++)
        {
            Rectangle lineRect = new (0, line, frame.Width, 1);

            if (!Viewport.Contains (lineRect))
            {
                continue;
            }

            Move (0, line);
            Driver.SetAttribute (ColorScheme.HotNormal);
            Driver.AddStr ($"{displayStart + line * nblocks * bsize:x8} ");

            currentAttribute = ColorScheme.HotNormal;
            SetAttribute (GetNormalColor ());

            for (var block = 0; block < nblocks; block++)
            {
                for (var b = 0; b < bsize; b++)
                {
                    int offset = line * nblocks * bsize + block * bsize + b;
                    byte value = GetData (data, offset, out bool edited);

                    if (offset + displayStart == position || edited)
                    {
                        SetAttribute (leftSide ? activeColor : trackingColor);
                    }
                    else
                    {
                        SetAttribute (GetNormalColor ());
                    }

                    Driver.AddStr (offset >= n && !edited ? "  " : $"{value:x2}");
                    SetAttribute (GetNormalColor ());
                    Driver.AddRune (SpaceCharRune);
                }

                Driver.AddStr (block + 1 == nblocks ? " " : "| ");
            }

            for (var bitem = 0; bitem < nblocks * bsize; bitem++)
            {
                int offset = line * nblocks * bsize + bitem;
                byte b = GetData (data, offset, out bool edited);
                Rune c;

                if (offset >= n && !edited)
                {
                    c = SpaceCharRune;
                }
                else
                {
                    if (b < 32)
                    {
                        c = PeriodCharRune;
                    }
                    else if (b > 127)
                    {
                        c = PeriodCharRune;
                    }
                    else
                    {
                        Rune.DecodeFromUtf8 (new ReadOnlySpan<byte> (ref b), out c, out _);
                    }
                }

                if (offset + displayStart == position || edited)
                {
                    SetAttribute (leftSide ? trackingColor : activeColor);
                }
                else
                {
                    SetAttribute (GetNormalColor ());
                }

                Driver.AddRune (c);
            }
        }

        void SetAttribute (Attribute attribute)
        {
            if (currentAttribute != attribute)
            {
                currentAttribute = attribute;
                Driver.SetAttribute (attribute);
            }
        }
    }

    /// <summary>Method used to invoke the <see cref="Edited"/> event passing the <see cref="KeyValuePair{TKey, TValue}"/>.</summary>
    /// <param name="e">The key value pair.</param>
    public virtual void OnEdited (HexViewEditEventArgs e) { Edited?.Invoke (this, e); }

    ///<inheritdoc/>
    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (DesiredCursorVisibility);

        return base.OnEnter (view);
    }

    /// <summary>
    ///     Method used to invoke the <see cref="PositionChanged"/> event passing the <see cref="HexViewEventArgs"/>
    ///     arguments.
    /// </summary>
    public virtual void OnPositionChanged () { PositionChanged?.Invoke (this, new HexViewEventArgs (Position, CursorPosition, BytesPerLine)); }

    /// <inheritdoc/>
    public override bool OnProcessKeyDown (Key keyEvent)
    {
        if (!AllowEdits)
        {
            return false;
        }

        // Ignore control characters and other special keys
        if (keyEvent < Key.Space || keyEvent.KeyCode > KeyCode.CharMask)
        {
            return false;
        }

        if (leftSide)
        {
            int value;
            var k = (char)keyEvent.KeyCode;

            if (k >= 'A' && k <= 'F')
            {
                value = k - 'A' + 10;
            }
            else if (k >= 'a' && k <= 'f')
            {
                value = k - 'a' + 10;
            }
            else if (k >= '0' && k <= '9')
            {
                value = k - '0';
            }
            else
            {
                return false;
            }

            byte b;

            if (!edits.TryGetValue (position, out b))
            {
                source.Position = position;
                b = (byte)source.ReadByte ();
            }

            RedisplayLine (position);

            if (firstNibble)
            {
                firstNibble = false;
                b = (byte)((b & 0xf) | (value << bsize));
                edits [position] = b;
                OnEdited (new HexViewEditEventArgs (position, edits [position]));
            }
            else
            {
                b = (byte)((b & 0xf0) | value);
                edits [position] = b;
                OnEdited (new HexViewEditEventArgs (position, edits [position]));
                MoveRight ();
            }

            return true;
        }

        return false;
    }

    /// <summary>Event to be invoked when the position and cursor position changes.</summary>
    public event EventHandler<HexViewEventArgs> PositionChanged;

    ///<inheritdoc/>
    public override Point? PositionCursor ()
    {
        var delta = (int)(position - displayStart);
        int line = delta / bytesPerLine;
        int item = delta % bytesPerLine;
        int block = item / bsize;
        int column = item % bsize * 3;

        int x = displayWidth + block * 14 + column + (firstNibble ? 0 : 1);
        int y = line;
        if (!leftSide)
        {
            x = displayWidth + bytesPerLine / bsize * 14 + item - 1;
        }

        Move (x, y);
        return new (x, y);
    }

    internal void SetDisplayStart (long value)
    {
        if (value > 0 && value >= source.Length)
        {
            displayStart = source.Length - 1;
        }
        else if (value < 0)
        {
            displayStart = 0;
        }
        else
        {
            displayStart = value;
        }

        SetNeedsDisplay ();
    }

    //
    // This is used to support editing of the buffer on a peer List<>, 
    // the offset corresponds to an offset relative to DisplayStart, and
    // the buffer contains the contents of a screenful of data, so the 
    // offset is relative to the buffer.
    //
    // 
    private byte GetData (byte [] buffer, int offset, out bool edited)
    {
        long pos = DisplayStart + offset;

        if (edits.TryGetValue (pos, out byte v))
        {
            edited = true;

            return v;
        }

        edited = false;

        return buffer [offset];
    }

    private void HexView_LayoutComplete (object sender, LayoutEventArgs e)
    {
        // Small buffers will just show the position, with the bsize field value (4 bytes)
        bytesPerLine = bsize;

        if (Viewport.Width - displayWidth > 17)
        {
            bytesPerLine = bsize * ((Viewport.Width - displayWidth) / 18);
        }
    }

    private bool MoveDown (int bytes)
    {
        // BUGBUG: Viewport!
        RedisplayLine (position);

        if (position + bytes < source.Length)
        {
            position += bytes;
        }
        else if ((bytes == bytesPerLine * Frame.Height && source.Length >= DisplayStart + bytesPerLine * Frame.Height)
                 || (bytes <= bytesPerLine * Frame.Height - bytesPerLine
                     && source.Length <= DisplayStart + bytesPerLine * Frame.Height))
        {
            long p = position;

            while (p + bytesPerLine < source.Length)
            {
                p += bytesPerLine;
            }

            position = p;
        }

        if (position >= DisplayStart + bytesPerLine * Frame.Height)
        {
            SetDisplayStart (DisplayStart + bytes);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (position);
        }

        return true;
    }

    private bool MoveEnd ()
    {
        position = source.Length;

        // BUGBUG: Viewport!
        if (position >= DisplayStart + bytesPerLine * Frame.Height)
        {
            SetDisplayStart (position);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (position);
        }

        return true;
    }

    private bool MoveEndOfLine ()
    {
        position = Math.Min (position / bytesPerLine * bytesPerLine + bytesPerLine - 1, source.Length);
        SetNeedsDisplay ();

        return true;
    }

    private bool MoveHome ()
    {
        DisplayStart = 0;
        SetNeedsDisplay ();

        return true;
    }

    private bool MoveLeft ()
    {
        RedisplayLine (position);

        if (leftSide)
        {
            if (!firstNibble)
            {
                firstNibble = true;

                return true;
            }

            firstNibble = false;
        }

        if (position == 0)
        {
            return true;
        }

        if (position - 1 < DisplayStart)
        {
            SetDisplayStart (displayStart - bytesPerLine);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (position);
        }

        position--;

        return true;
    }

    private bool MoveRight ()
    {
        RedisplayLine (position);

        if (leftSide)
        {
            if (firstNibble)
            {
                firstNibble = false;

                return true;
            }

            firstNibble = true;
        }

        if (position < source.Length)
        {
            position++;
        }

        // BUGBUG: Viewport!
        if (position >= DisplayStart + bytesPerLine * Frame.Height)
        {
            SetDisplayStart (DisplayStart + bytesPerLine);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (position);
        }

        return true;
    }

    private bool MoveStartOfLine ()
    {
        position = position / bytesPerLine * bytesPerLine;
        SetNeedsDisplay ();

        return true;
    }

    private bool MoveUp (int bytes)
    {
        RedisplayLine (position);

        if (position - bytes > -1)
        {
            position -= bytes;
        }

        if (position < DisplayStart)
        {
            SetDisplayStart (DisplayStart - bytes);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (position);
        }

        return true;
    }

    private void RedisplayLine (long pos)
    {
        var delta = (int)(pos - DisplayStart);
        int line = delta / bytesPerLine;

        // BUGBUG: Viewport!
        SetNeedsDisplay (new (0, line, Frame.Width, 1));
    }

    private bool ToggleSide ()
    {
        leftSide = !leftSide;
        RedisplayLine (position);
        firstNibble = true;

        return true;
    }
}
