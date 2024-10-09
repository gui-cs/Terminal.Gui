#nullable enable

//
// HexView.cs: A hexadecimal viewer
//
// TODO:
// - Support searching and highlighting of the search result
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
public class HexView : View, IDesignable
{
    private const int BSIZE = 4;
    private const int DISPLAY_WIDTH = 9;

    private bool _firstNibble;
    private bool _leftSide;
    private static readonly Rune _spaceCharRune = new (' ');
    private static readonly Rune _periodCharRune = new ('.');

    /// <summary>Initializes a <see cref="HexView"/> class.</summary>
    /// <param name="source">
    ///     The <see cref="Stream"/> to view and edit as hex, this <see cref="Stream"/> must support seeking,
    ///     or an exception will be thrown.
    /// </param>
    public HexView (Stream? source)
    {
        Source = source;

        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;
        _leftSide = true;
        _firstNibble = true;

        // PERF: Closure capture of 'this' creates a lot of overhead.
        // BUG: Closure capture of 'this' may have unexpected results depending on how this is called.
        // The above two comments apply to all of the lambdas passed to all calls to AddCommand below.
        // Things this view knows how to do
        AddCommand (Command.Left, () => MoveLeft ());
        AddCommand (Command.Right, () => MoveRight ());
        AddCommand (Command.Down, () => MoveDown (BytesPerLine));
        AddCommand (Command.Up, () => MoveUp (BytesPerLine));
        AddCommand (Command.Tab, () => Navigate (NavigationDirection.Forward));
        AddCommand (Command.BackTab, () => Navigate (NavigationDirection.Backward));
        AddCommand (Command.PageUp, () => MoveUp (BytesPerLine * Frame.Height));
        AddCommand (Command.PageDown, () => MoveDown (BytesPerLine * Frame.Height));
        AddCommand (Command.Start, () => MoveHome ());
        AddCommand (Command.End, () => MoveEnd ());
        AddCommand (Command.LeftStart, () => MoveLeftStart ());
        AddCommand (Command.RightEnd, () => MoveEndOfLine ());
        AddCommand (Command.StartOfPage, () => MoveUp (BytesPerLine * ((int)(position - _displayStart) / BytesPerLine)));

        AddCommand (
                    Command.EndOfPage,
                    () => MoveDown (BytesPerLine * (Frame.Height - 1 - (int)(position - _displayStart) / BytesPerLine))
                   );

        // Default keybindings for this view
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorUp, Command.Up);

        KeyBindings.Add (Key.V.WithAlt, Command.PageUp);
        KeyBindings.Add (Key.PageUp, Command.PageUp);

        KeyBindings.Add (Key.V.WithCtrl, Command.PageDown);
        KeyBindings.Add (Key.PageDown, Command.PageDown);

        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.LeftStart);
        KeyBindings.Add (Key.CursorRight.WithCtrl, Command.RightEnd);
        KeyBindings.Add (Key.CursorUp.WithCtrl, Command.StartOfPage);
        KeyBindings.Add (Key.CursorDown.WithCtrl, Command.EndOfPage);

        KeyBindings.Add (Key.Tab, Command.Tab);
        KeyBindings.Add (Key.Tab.WithShift, Command.BackTab);

        LayoutComplete += HexView_LayoutComplete;
    }

    /// <summary>Initializes a <see cref="HexView"/> class.</summary>
    public HexView () : this (new MemoryStream ()) { }

    /// <summary>
    ///     Gets or sets whether this <see cref="HexView"/> allow editing of the <see cref="Stream"/> of the underlying
    ///     <see cref="Stream"/>.
    /// </summary>
    /// <value><c>true</c> if allow edits; otherwise, <c>false</c>.</value>
    public bool AllowEdits { get; set; } = true;

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
            int line = delta / BytesPerLine + 1;
            int item = delta % BytesPerLine + 1;

            return new (item, line);
        }
    }

    private SortedDictionary<long, byte> _edits = [];

    /// <summary>
    ///     Gets a <see cref="SortedDictionary{TKey, TValue}"/> describing the edits done to the <see cref="HexView"/>.
    ///     Each Key indicates an offset where an edit was made and the Value is the changed byte.
    /// </summary>
    /// <value>The edits.</value>
    public IReadOnlyDictionary<long, byte> Edits => _edits;


    private Stream? _source;

    /// <summary>
    ///     Sets or gets the <see cref="Stream"/> the <see cref="HexView"/> is operating on; the stream must support
    ///     seeking ( <see cref="Stream.CanSeek"/> == true).
    /// </summary>
    /// <value>The source.</value>
    public Stream? Source
    {
        get => _source;
        set
        {
            ArgumentNullException.ThrowIfNull (value);

            if (!value!.CanSeek)
            {
                throw new ArgumentException (@"The source stream must be seekable (CanSeek property)");
            }

            _source = value;

            if (_displayStart > _source.Length)
            {
                DisplayStart = 0;
            }

            if (position > _source.Length)
            {
                position = 0;
            }

            SetNeedsDisplay ();
        }
    }

    private int _bpl;

    /// <summary>The bytes length per line.</summary>
    public int BytesPerLine
    {
        get => _bpl;
        set
        {
            _bpl = value;
            RaisePositionChanged ();
        }
    }


    private long _pos;

    // TODO: Why is this "starting at one"? How does that make sense?
    /// <summary>Gets the current character position starting at one, related to the <see cref="Stream"/>.</summary>
    public long Position => position + 1;

    private long position
    {
        get => _pos;
        set
        {
            _pos = value;
            RaisePositionChanged ();
        }
    }

    private long _displayStart;

    // TODO: Use Viewport content scrolling instead
    /// <summary>
    ///     Sets or gets the offset into the <see cref="Stream"/> that will be displayed at the top of the
    ///     <see cref="HexView"/>.
    /// </summary>
    /// <value>The display start.</value>
    public long DisplayStart
    {
        get => _displayStart;
        set
        {
            position = value;

            SetDisplayStart (value);
        }
    }

    internal void SetDisplayStart (long value)
    {
        if (value > 0 && value >= _source.Length)
        {
            _displayStart = _source.Length - 1;
        }
        else if (value < 0)
        {
            _displayStart = 0;
        }
        else
        {
            _displayStart = value;
        }

        SetNeedsDisplay ();
    }


    /// <summary>
    ///     Applies and edits made to the <see cref="Stream"/> and resets the contents of the
    ///     <see cref="Edits"/> property.
    /// </summary>
    /// <param name="stream">If provided also applies the changes to the passed <see cref="Stream"/>.</param>
    /// .
    public void ApplyEdits (Stream? stream = null)
    {
        foreach (KeyValuePair<long, byte> kv in _edits)
        {
            _source!.Position = kv.Key;
            _source.WriteByte (kv.Value);
            _source.Flush ();

            if (stream is { })
            {
                stream.Position = kv.Key;
                stream.WriteByte (kv.Value);
                stream.Flush ();
            }
        }

        _edits = new ();
        SetNeedsDisplay ();
    }

    /// <summary>
    ///     Discards the edits made to the <see cref="Stream"/> by resetting the contents of the
    ///     <see cref="Edits"/> property.
    /// </summary>
    public void DiscardEdits () { _edits = new (); }

    /// <inheritdoc/>
    protected internal override bool OnMouseEvent (MouseEvent me)
    {
        if (_source is null)
        {
            return false;
        }

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
            DisplayStart = Math.Min (DisplayStart + BytesPerLine, _source.Length);

            return true;
        }

        if (me.Flags == MouseFlags.WheeledUp)
        {
            DisplayStart = Math.Max (DisplayStart - BytesPerLine, 0);

            return true;
        }

        if (me.Position.X < DISPLAY_WIDTH)
        {
            return true;
        }

        int nblocks = BytesPerLine / BSIZE;
        int blocksSize = nblocks * 14;
        int blocksRightOffset = DISPLAY_WIDTH + blocksSize - 1;

        if (me.Position.X > blocksRightOffset + BytesPerLine - 1)
        {
            return true;
        }

        _leftSide = me.Position.X >= blocksRightOffset;
        long lineStart = me.Position.Y * BytesPerLine + _displayStart;
        int x = me.Position.X - DISPLAY_WIDTH + 1;
        int block = x / 14;
        x -= block * 2;
        int empty = x % 3;
        int item = x / 3;

        if (!_leftSide && item > 0 && (empty == 0 || x == block * 14 + 14 - 1 - block * 2))
        {
            return true;
        }

        _firstNibble = true;

        if (_leftSide)
        {
            position = Math.Min (lineStart + me.Position.X - blocksRightOffset, _source.Length);
        }
        else
        {
            position = Math.Min (lineStart + item, _source.Length);
        }

        if (me.Flags == MouseFlags.Button1DoubleClicked)
        {
            _leftSide = !_leftSide;

            if (_leftSide)
            {
                _firstNibble = empty == 1;
            }
            else
            {
                _firstNibble = true;
            }
        }

        SetNeedsDisplay ();

        return true;
    }

    ///<inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        if (Source is null)
        {
            return;
        }

        Attribute currentAttribute;
        Attribute current = GetFocusColor ();
        Driver.SetAttribute (current);
        Move (0, 0);

        int nblocks = BytesPerLine / BSIZE;
        var data = new byte [nblocks * BSIZE * viewport.Height];
        Source.Position = _displayStart;
        int n = _source.Read (data, 0, data.Length);

        Attribute activeColor = GetHotNormalColor ();
        Attribute trackingColor = GetHotFocusColor ();

        for (var line = 0; line < viewport.Height; line++)
        {
            Rectangle lineRect = new (0, line, viewport.Width, 1);

            if (!Viewport.Contains (lineRect))
            {
                continue;
            }

            Move (0, line);
            currentAttribute = GetHotNormalColor ();
            Driver.SetAttribute (currentAttribute);
            Driver.AddStr ($"{_displayStart + line * nblocks * BSIZE:x8} ");

            SetAttribute (GetNormalColor ());

            for (var block = 0; block < nblocks; block++)
            {
                for (var b = 0; b < BSIZE; b++)
                {
                    int offset = line * nblocks * BSIZE + block * BSIZE + b;
                    byte value = GetData (data, offset, out bool edited);

                    if (offset + _displayStart == position || edited)
                    {
                        SetAttribute (_leftSide ? activeColor : trackingColor);
                    }
                    else
                    {
                        SetAttribute (GetNormalColor ());
                    }

                    Driver.AddStr (offset >= n && !edited ? "  " : $"{value:x2}");
                    SetAttribute (GetNormalColor ());
                    Driver.AddRune (_spaceCharRune);
                }

                Driver.AddStr (block + 1 == nblocks ? " " : "| ");
            }

            for (var bitem = 0; bitem < nblocks * BSIZE; bitem++)
            {
                int offset = line * nblocks * BSIZE + bitem;
                byte b = GetData (data, offset, out bool edited);
                Rune c;

                if (offset >= n && !edited)
                {
                    c = _spaceCharRune;
                }
                else
                {
                    if (b < 32)
                    {
                        c = _periodCharRune;
                    }
                    else if (b > 127)
                    {
                        c = _periodCharRune;
                    }
                    else
                    {
                        Rune.DecodeFromUtf8 (new (ref b), out c, out _);
                    }
                }

                if (offset + _displayStart == position || edited)
                {
                    SetAttribute (_leftSide ? trackingColor : activeColor);
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

    /// <summary>Raises the <see cref="Edited"/> event.</summary>
    protected void RaiseEdited (HexViewEditEventArgs e)
    {
        OnEditied (e);
        Edited?.Invoke (this, e);
    }

    /// <summary>Event to be invoked when an edit is made on the <see cref="Stream"/>.</summary>
    public event EventHandler<HexViewEditEventArgs>? Edited;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnEditied (HexViewEditEventArgs e) { }

    /// <summary>Raises the <see cref="PositionChanged"/> event.</summary>
    protected void RaisePositionChanged ()
    {
        HexViewEventArgs args = new (Position, CursorPosition, BytesPerLine);
        OnPositionChanged (args);
        PositionChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Called when <see cref="Position"/> has changed.
    /// </summary>
    protected virtual void OnPositionChanged (HexViewEventArgs e) { }

    /// <inheritdoc/>
    public override bool OnProcessKeyDown (Key keyEvent)
    {
        if (!AllowEdits || _source is null)
        {
            return false;
        }

        // Ignore control characters and other special keys
        if (keyEvent < Key.Space || keyEvent.KeyCode > KeyCode.CharMask)
        {
            return false;
        }

        if (_leftSide)
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

            if (!_edits.TryGetValue (position, out b))
            {
                _source.Position = position;
                b = (byte)_source.ReadByte ();
            }

            RedisplayLine (position);

            if (_firstNibble)
            {
                _firstNibble = false;
                b = (byte)((b & 0xf) | (value << BSIZE));
                _edits [position] = b;
                RaiseEdited (new (position, _edits [position]));
            }
            else
            {
                b = (byte)((b & 0xf0) | value);
                _edits [position] = b;
                RaiseEdited (new (position, _edits [position]));
                MoveRight ();
            }

            return true;
        }

        return false;
    }

    /// <summary>Event to be invoked when the position and cursor position changes.</summary>
    public event EventHandler<HexViewEventArgs>? PositionChanged;

    ///<inheritdoc/>
    public override Point? PositionCursor ()
    {
        var delta = (int)(position - _displayStart);
        int line = delta / BytesPerLine;
        int item = delta % BytesPerLine;
        int block = item / BSIZE;
        int column = item % BSIZE * 3;

        int x = DISPLAY_WIDTH + block * 14 + column + (_firstNibble ? 0 : 1);
        int y = line;

        if (!_leftSide)
        {
            x = DISPLAY_WIDTH + BytesPerLine / BSIZE * 14 + item - 1;
        }

        Move (x, y);

        return new (x, y);
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

        if (_edits.TryGetValue (pos, out byte v))
        {
            edited = true;

            return v;
        }

        edited = false;

        return buffer [offset];
    }

    private void HexView_LayoutComplete (object? sender, LayoutEventArgs e)
    {
        // Small buffers will just show the position, with the bsize field value (4 bytes)
        BytesPerLine = BSIZE;

        if (Viewport.Width - DISPLAY_WIDTH > 17)
        {
            BytesPerLine = BSIZE * ((Viewport.Width - DISPLAY_WIDTH) / 18);
        }
    }

    private bool MoveDown (int bytes)
    {
        RedisplayLine (position);

        if (position + bytes < _source.Length)
        {
            position += bytes;
        }
        else if ((bytes == BytesPerLine * Viewport.Height && _source.Length >= DisplayStart + BytesPerLine * Viewport.Height)
                 || (bytes <= BytesPerLine * Viewport.Height - BytesPerLine
                     && _source.Length <= DisplayStart + BytesPerLine * Viewport.Height))
        {
            long p = position;

            while (p + BytesPerLine < _source.Length)
            {
                p += BytesPerLine;
            }

            position = p;
        }

        if (position >= DisplayStart + BytesPerLine * Viewport.Height)
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
        position = _source.Length;

        if (position >= DisplayStart + BytesPerLine * Viewport.Height)
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
        position = Math.Min (position / BytesPerLine * BytesPerLine + BytesPerLine - 1, _source.Length);
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

        if (_leftSide)
        {
            if (!_firstNibble)
            {
                _firstNibble = true;

                return true;
            }

            _firstNibble = false;
        }

        if (position == 0)
        {
            return true;
        }

        if (position - 1 < DisplayStart)
        {
            SetDisplayStart (_displayStart - BytesPerLine);
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

        if (_leftSide)
        {
            if (_firstNibble)
            {
                _firstNibble = false;

                return true;
            }

            _firstNibble = true;
        }

        if (position < _source.Length)
        {
            position++;
        }

        if (position >= DisplayStart + BytesPerLine * Viewport.Height)
        {
            SetDisplayStart (DisplayStart + BytesPerLine);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (position);
        }

        return true;
    }

    private bool MoveLeftStart ()
    {
        position = position / BytesPerLine * BytesPerLine;
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
        if (BytesPerLine == 0)
        {
            return;
        }

        var delta = (int)(pos - DisplayStart);
        int line = delta / BytesPerLine;

        SetNeedsDisplay (new (0, line, Viewport.Width, 1));
    }

    private bool Navigate (NavigationDirection direction)
    {
        switch (direction)
        {
            case NavigationDirection.Forward:
                if (_leftSide)
                {
                    _leftSide = false;
                    RedisplayLine (position);
                    _firstNibble = true;

                    return true;
                }

                break;

            case NavigationDirection.Backward:
                if (!_leftSide)
                {
                    _leftSide = true;
                    RedisplayLine (position);
                    _firstNibble = true;

                    return true;
                }

                break;
        }

        return false;
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Source = new MemoryStream (Encoding.UTF8.GetBytes ("HexEditor Unicode that shouldn't 𝔹Aℝ𝔽!"));

        return true;
    }
}
