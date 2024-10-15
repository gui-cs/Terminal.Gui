#nullable enable

//
// HexView.cs: A hexadecimal viewer
//
// TODO: Support searching and highlighting of the search result
// TODO: Support shrinking the stream (e.g. del/backspace should work).
// 

using System.Buffers;

namespace Terminal.Gui;

/// <summary>Hex viewer and editor <see cref="View"/> over a <see cref="Stream"/></summary>
/// <remarks>
///     <para>
///         <see cref="HexView"/> provides a hex editor on top of a seekable <see cref="Stream"/> with the left side
///         showing the hex values of the bytes in the <see cref="Stream"/> and the right side showing the contents
///         (filtered
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
///     <para>
///         Control the byte at the caret for editing by setting the <see cref="Address"/> property to an offset in the
///         stream.
///     </para>
///     <para>Control the first byte shown by setting the <see cref="DisplayStart"/> property to an offset in the stream.</para>
/// </remarks>
public class HexView : View, IDesignable
{
    private const int DEFAULT_ADDRESS_WIDTH = 8; // The default value for AddressWidth
    private const int NUM_BYTES_PER_HEX_COLUMN = 4;
    private const int HEX_COLUMN_WIDTH = NUM_BYTES_PER_HEX_COLUMN * 3 + 2; // 3 cols per byte + 1 for vert separator + right space

    private bool _firstNibble;
    private bool _leftSideHasFocus;
    private static readonly Rune _spaceCharRune = new (' ');
    private static readonly Rune _periodCharRune = Glyphs.DottedSquare;
    private static readonly Rune _columnSeparatorRune = Glyphs.VLineDa4;

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
        _leftSideHasFocus = true;
        _firstNibble = true;

        // PERF: Closure capture of 'this' creates a lot of overhead.
        // BUG: Closure capture of 'this' may have unexpected results depending on how this is called.
        // The above two comments apply to all the lambdas passed to all calls to AddCommand below.
        AddCommand (Command.Left, () => MoveLeft ());
        AddCommand (Command.Right, () => MoveRight ());
        AddCommand (Command.Down, () => MoveDown (BytesPerLine));
        AddCommand (Command.Up, () => MoveUp (BytesPerLine));
        AddCommand (Command.PageUp, () => MoveUp (BytesPerLine * Viewport.Height));
        AddCommand (Command.PageDown, () => MoveDown (BytesPerLine * Viewport.Height));
        AddCommand (Command.Start, () => MoveHome ());
        AddCommand (Command.End, () => MoveEnd ());
        AddCommand (Command.LeftStart, () => MoveLeftStart ());
        AddCommand (Command.RightEnd, () => MoveEndOfLine ());
        AddCommand (Command.StartOfPage, () => MoveUp (BytesPerLine * ((int)(Address - _displayStart) / BytesPerLine)));
        AddCommand (
                    Command.EndOfPage,
                    () => MoveDown (BytesPerLine * (Viewport.Height - 1 - (int)(Address - _displayStart) / BytesPerLine))
                   );
        AddCommand (Command.DeleteCharLeft, () => true);
        AddCommand (Command.DeleteCharRight, () => true);
        AddCommand (Command.Insert, () => true);

        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorUp, Command.Up);

        KeyBindings.Add (Key.PageUp, Command.PageUp);

        KeyBindings.Add (Key.PageDown, Command.PageDown);

        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.LeftStart);
        KeyBindings.Add (Key.CursorRight.WithCtrl, Command.RightEnd);
        KeyBindings.Add (Key.CursorUp.WithCtrl, Command.StartOfPage);
        KeyBindings.Add (Key.CursorDown.WithCtrl, Command.EndOfPage);

        KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);
        KeyBindings.Add (Key.Delete, Command.DeleteCharRight);
        KeyBindings.Add (Key.InsertChar, Command.Insert);

        KeyBindings.Remove (Key.Space);
        KeyBindings.Remove (Key.Enter);

        LayoutComplete += HexView_LayoutComplete;
    }

    /// <summary>Initializes a <see cref="HexView"/> class.</summary>
    public HexView () : this (new MemoryStream ()) { }

    /// <summary>
    ///     Gets or sets whether this <see cref="HexView"/> allows editing of the <see cref="Stream"/> of the underlying
    ///     <see cref="Stream"/>.
    /// </summary>
    /// <value><c>true</c> to allow edits; otherwise, <c>false</c>.</value>
    public bool AllowEdits { get; set; } = true;

    /// <summary>Gets the current edit position.</summary>
    public Point Position
    {
        get
        {
            if (_source is null || BytesPerLine == 0)
            {
                return Point.Empty;
            }

            var delta = (int)Address;

            int line = delta / BytesPerLine;
            int item = delta % BytesPerLine;

            return new (item, line);
        }
    }

    ///<inheritdoc/>
    public override Point? PositionCursor ()
    {
        var delta = (int)(Address - _displayStart);
        int line = delta / BytesPerLine;
        int item = delta % BytesPerLine;
        int block = item / NUM_BYTES_PER_HEX_COLUMN;
        int column = item % NUM_BYTES_PER_HEX_COLUMN * 3;

        int x = GetLeftSideStartColumn () + block * HEX_COLUMN_WIDTH + column + (_firstNibble ? 0 : 1);
        int y = line;

        if (!_leftSideHasFocus)
        {
            x = GetLeftSideStartColumn () + BytesPerLine / NUM_BYTES_PER_HEX_COLUMN * HEX_COLUMN_WIDTH + item - 1;
        }

        Move (x, y);

        return new (x, y);
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

            if (Address > _source.Length)
            {
                Address = 0;
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

    private long _address;

    /// <summary>Gets or sets the current byte position in the <see cref="Stream"/>.</summary>
    public long Address
    {
        get => _address;
        set
        {
            if (_address == value)
            {
                return;
            }

            //ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual (value, Source!.Length, $"Position");

            _address = value;
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
            Address = value;

            SetDisplayStart (value);
        }
    }

    private int _addressWidth = DEFAULT_ADDRESS_WIDTH;

    /// <summary>
    ///     Gets or sets the width of the Address column on the left. Set to 0 to hide. The default is 8.
    /// </summary>
    public int AddressWidth
    {
        get => _addressWidth;
        set
        {
            if (_addressWidth == value)
            {
                return;
            }

            _addressWidth = value;
            SetNeedsDisplay ();
        }
    }

    private int GetLeftSideStartColumn () { return AddressWidth == 0 ? 0 : AddressWidth + 1; }

    internal void SetDisplayStart (long value)
    {
        if (value > 0 && value >= _source?.Length)
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
    protected override bool OnMouseEvent (MouseEventArgs me)
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
            DisplayStart = Math.Min (DisplayStart + BytesPerLine, GetEditedSize ());

            return true;
        }

        if (me.Flags == MouseFlags.WheeledUp)
        {
            DisplayStart = Math.Max (DisplayStart - BytesPerLine, 0);

            return true;
        }

        if (me.Position.X < GetLeftSideStartColumn ())
        {
            return true;
        }

        int nblocks = BytesPerLine / NUM_BYTES_PER_HEX_COLUMN;
        int blocksSize = nblocks * HEX_COLUMN_WIDTH;
        int blocksRightOffset = GetLeftSideStartColumn () + blocksSize - 1;

        if (me.Position.X > blocksRightOffset + BytesPerLine - 1)
        {
            return true;
        }

        bool clickIsOnLeftSide = me.Position.X >= blocksRightOffset;
        long lineStart = me.Position.Y * BytesPerLine + _displayStart;
        int x = me.Position.X - GetLeftSideStartColumn () + 1;
        int block = x / HEX_COLUMN_WIDTH;
        x -= block * 2;
        int empty = x % 3;
        int item = x / 3;

        if (!clickIsOnLeftSide && item > 0 && (empty == 0 || x == block * HEX_COLUMN_WIDTH + HEX_COLUMN_WIDTH - 1 - block * 2))
        {
            return true;
        }

        _firstNibble = true;

        if (clickIsOnLeftSide)
        {
            Address = Math.Min (lineStart + me.Position.X - blocksRightOffset, GetEditedSize ());
        }
        else
        {
            Address = Math.Min (lineStart + item, GetEditedSize ());
        }

        if (me.Flags == MouseFlags.Button1DoubleClicked)
        {
            _leftSideHasFocus = !clickIsOnLeftSide;

            if (_leftSideHasFocus)
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

        int nBlocks = BytesPerLine / NUM_BYTES_PER_HEX_COLUMN;
        var data = new byte [nBlocks * NUM_BYTES_PER_HEX_COLUMN * viewport.Height];
        Source.Position = _displayStart;
        int n = _source!.Read (data, 0, data.Length);

        Attribute selectedAttribute = GetHotNormalColor ();
        Attribute editedAttribute = new Attribute (GetNormalColor ().Foreground.GetHighlightColor (), GetNormalColor ().Background);
        Attribute editingAttribute = new Attribute (GetFocusColor ().Background, GetFocusColor ().Foreground);
        for (var line = 0; line < viewport.Height; line++)
        {
            Rectangle lineRect = new (0, line, viewport.Width, 1);

            if (!Viewport.Contains (lineRect))
            {
                continue;
            }

            Move (0, line);
            currentAttribute = new Attribute (GetNormalColor ().Foreground.GetHighlightColor (), GetNormalColor ().Background);
            Driver.SetAttribute (currentAttribute);
            var address = $"{_displayStart + line * nBlocks * NUM_BYTES_PER_HEX_COLUMN:x8}";
            Driver.AddStr ($"{address.Substring (8 - AddressWidth)}");

            if (AddressWidth > 0)
            {
                Driver.AddStr (" ");
            }

            SetAttribute (GetNormalColor ());

            for (var block = 0; block < nBlocks; block++)
            {
                for (var b = 0; b < NUM_BYTES_PER_HEX_COLUMN; b++)
                {
                    int offset = line * nBlocks * NUM_BYTES_PER_HEX_COLUMN + block * NUM_BYTES_PER_HEX_COLUMN + b;
                    byte value = GetData (data, offset, out bool edited);

                    if (offset + _displayStart == Address)
                    {
                        // Selected
                        SetAttribute (_leftSideHasFocus ? editingAttribute : (edited ? editedAttribute : selectedAttribute));
                    }
                    else
                    {
                        SetAttribute (edited ? editedAttribute : GetNormalColor ());
                    }

                    Driver.AddStr (offset >= n && !edited ? "  " : $"{value:x2}");
                    SetAttribute (GetNormalColor ());
                    Driver.AddRune (_spaceCharRune);
                }

                Driver.AddStr (block + 1 == nBlocks ? " " : $"{_columnSeparatorRune} ");
            }

            for (var byteIndex = 0; byteIndex < nBlocks * NUM_BYTES_PER_HEX_COLUMN; byteIndex++)
            {
                int offset = line * nBlocks * NUM_BYTES_PER_HEX_COLUMN + byteIndex;
                byte b = GetData (data, offset, out bool edited);
                Rune c;

                var utf8BytesConsumed = 0;

                if (offset >= n && !edited)
                {
                    c = _spaceCharRune;
                }
                else
                {
                    switch (b)
                    {
                        //case < 32:
                        //    c = _periodCharRune;

                        //    break;
                        case > 127:
                            {
                                var utf8 = GetData (data, offset, 4, out bool _);

                                OperationStatus status = Rune.DecodeFromUtf8 (utf8, out c, out utf8BytesConsumed);

                                while (status == OperationStatus.NeedMoreData)
                                {
                                    status = Rune.DecodeFromUtf8 (utf8, out c, out utf8BytesConsumed);
                                }

                                break;
                            }
                        default:
                            Rune.DecodeFromUtf8 (new (ref b), out c, out _);

                            break;
                    }
                }

                if (offset + _displayStart == Address)
                {
                    // Selected
                    SetAttribute (_leftSideHasFocus ? editingAttribute : (edited ? editedAttribute : selectedAttribute));
                }
                else
                {
                    SetAttribute (edited ? editedAttribute : GetNormalColor ());
                }

                Driver.AddRune (c);

                for (var i = 1; i < utf8BytesConsumed; i++)
                {
                    byteIndex++;
                    Driver.AddRune (_periodCharRune);
                }
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
        OnEdited (e);
        Edited?.Invoke (this, e);
    }

    /// <summary>Event to be invoked when an edit is made on the <see cref="Stream"/>.</summary>
    public event EventHandler<HexViewEditEventArgs>? Edited;

    /// <summary>
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnEdited (HexViewEditEventArgs e) { }

    /// <summary>
    ///     Call this when <see cref="Position"/> (and <see cref="Address"/>) has changed. Raises the
    ///     <see cref="PositionChanged"/> event.
    /// </summary>
    protected void RaisePositionChanged ()
    {
        HexViewEventArgs args = new (Address, Position, BytesPerLine);
        OnPositionChanged (args);
        PositionChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Called when <see cref="Position"/> (and <see cref="Address"/>) has changed.
    /// </summary>
    protected virtual void OnPositionChanged (HexViewEventArgs e) { }

    /// <summary>Raised when <see cref="Position"/> (and <see cref="Address"/>) has changed.</summary>
    public event EventHandler<HexViewEventArgs>? PositionChanged;

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key keyEvent)
    {
        if (!AllowEdits || _source is null)
        {
            return false;
        }

        if (_leftSideHasFocus)
        {
            int value;
            var k = (char)keyEvent.KeyCode;

            if (!char.IsAsciiHexDigit ((char)keyEvent.KeyCode))
            {
                return false;
            }

            if (k is >= 'A' and <= 'F')
            {
                value = k - 'A' + 10;
            }
            else if (k is >= 'a' and <= 'f')
            {
                value = k - 'a' + 10;
            }
            else if (k is >= '0' and <= '9')
            {
                value = k - '0';
            }
            else
            {
                return false;
            }

            if (!_edits.TryGetValue (Address, out byte b))
            {
                _source.Position = Address;
                b = (byte)_source.ReadByte ();
            }

            // BUGBUG: This makes no sense here.
            RedisplayLine (Address);

            if (_firstNibble)
            {
                _firstNibble = false;
                b = (byte)((b & 0xf) | (value << NUM_BYTES_PER_HEX_COLUMN));
                _edits [Address] = b;
                RaiseEdited (new (Address, _edits [Address]));
            }
            else
            {
                b = (byte)((b & 0xf0) | value);
                _edits [Address] = b;
                RaiseEdited (new (Address, _edits [Address]));
                MoveRight ();
            }

            return true;
        }

        keyEvent = keyEvent.NoAlt.NoCtrl;
        Rune r = keyEvent.AsRune;

        if (Rune.IsControl (r))
        {
            return false;
        }

        var utf8 = new byte [4];

        // If the rune is a wide char, encode as utf8
        if (r.TryEncodeToUtf8 (utf8, out int bytesWritten))
        {
            if (bytesWritten > 1)
            {
                bytesWritten = 4;
            }

            for (var utfIndex = 0; utfIndex < bytesWritten; utfIndex++)
            {
                _edits [Address] = utf8 [utfIndex];
                RaiseEdited (new (Address, _edits [Address]));
                MoveRight ();
            }
        }
        else
        {
            _edits [Address] = (byte)r.Value;
            RaiseEdited (new (Address, _edits [Address]));
            MoveRight ();
        }

        return true;
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

    private byte [] GetData (byte [] buffer, int offset, int count, out bool edited)
    {
        var returnBytes = new byte [count];
        edited = false;

        long pos = DisplayStart + offset;
        for (long i = pos; i < pos + count; i++)
        {
            if (_edits.TryGetValue (i, out byte v))
            {
                edited = true;
                returnBytes [i - pos] = v;
            }
            else
            {
                if (pos < buffer.Length - 1)
                {
                    returnBytes [i - pos] = buffer [pos];
                }
            }
        }

        return returnBytes;
    }

    private void HexView_LayoutComplete (object? sender, LayoutEventArgs e)
    {
        // Small buffers will just show the position, with the bsize field value (4 bytes)
        BytesPerLine = NUM_BYTES_PER_HEX_COLUMN;

        if (Viewport.Width - GetLeftSideStartColumn () >= HEX_COLUMN_WIDTH)
        {
            BytesPerLine = Math.Max (
                                     NUM_BYTES_PER_HEX_COLUMN,
                                     NUM_BYTES_PER_HEX_COLUMN * ((Viewport.Width - GetLeftSideStartColumn ()) / (HEX_COLUMN_WIDTH + NUM_BYTES_PER_HEX_COLUMN)));
        }
    }

    private bool MoveDown (int bytes)
    {
        RedisplayLine (Address);

        if (Address + bytes < GetEditedSize ())
        {
            // We can move down lines cleanly (without extending stream)
            Address += bytes;
        }
        else if ((bytes == BytesPerLine * Viewport.Height && _source!.Length >= DisplayStart + BytesPerLine * Viewport.Height)
                 || (bytes <= BytesPerLine * Viewport.Height - BytesPerLine
                     && _source!.Length <= DisplayStart + BytesPerLine * Viewport.Height))
        {
            long p = Address;

            // This lets address go past the end of the stream one, enabling adding to the stream.
            while (p + BytesPerLine <= GetEditedSize ())
            {
                p += BytesPerLine;
            }

            Address = p;
        }

        if (Address >= DisplayStart + BytesPerLine * Viewport.Height)
        {
            SetDisplayStart (DisplayStart + bytes);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (Address);
        }

        return true;
    }

    private bool MoveEnd ()
    {
        // This lets address go past the end of the stream one, enabling adding to the stream.
        Address = GetEditedSize ();

        if (Address >= DisplayStart + BytesPerLine * Viewport.Height)
        {
            SetDisplayStart (Address);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (Address);
        }

        return true;
    }

    private bool MoveEndOfLine ()
    {
        // This lets address go past the end of the stream one, enabling adding to the stream.
        Address = Math.Min (Address / BytesPerLine * BytesPerLine + BytesPerLine - 1, GetEditedSize ());
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
        RedisplayLine (Address);

        if (_leftSideHasFocus)
        {
            if (!_firstNibble)
            {
                _firstNibble = true;

                return true;
            }

            _firstNibble = false;
        }

        if (Address == 0)
        {
            return true;
        }

        if (Address - 1 < DisplayStart)
        {
            SetDisplayStart (_displayStart - BytesPerLine);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (Address);
        }

        Address--;

        return true;
    }

    private bool MoveRight ()
    {
        RedisplayLine (Address);

        if (_leftSideHasFocus)
        {
            if (_firstNibble)
            {
                _firstNibble = false;

                return true;
            }

            _firstNibble = true;
        }

        // This lets address go past the end of the stream one, enabling adding to the stream.
        if (Address < GetEditedSize ())
        {
            Address++;
        }

        if (Address >= DisplayStart + BytesPerLine * Viewport.Height)
        {
            SetDisplayStart (DisplayStart + BytesPerLine);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (Address);
        }

        return true;
    }

    private long GetEditedSize ()
    {
        if (_edits.Count == 0)
        {
            return _source!.Length;
        }

        long maxEditAddress = _edits.Keys.Max ();

        return Math.Max (_source!.Length, maxEditAddress + 1);
    }

    private bool MoveLeftStart ()
    {
        Address = Address / BytesPerLine * BytesPerLine;
        SetNeedsDisplay ();

        return true;
    }

    private bool MoveUp (int bytes)
    {
        RedisplayLine (Address);

        if (Address - bytes > -1)
        {
            Address -= bytes;
        }

        if (Address < DisplayStart)
        {
            SetDisplayStart (DisplayStart - bytes);
            SetNeedsDisplay ();
        }
        else
        {
            RedisplayLine (Address);
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

    /// <inheritdoc />
    protected override bool OnAdvancingFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        if (behavior is { } && behavior != TabStop)
        {
            return false;
        }

        if ((direction == NavigationDirection.Forward && _leftSideHasFocus)
            || (direction == NavigationDirection.Backward && !_leftSideHasFocus))
        {
            _leftSideHasFocus = !_leftSideHasFocus;
            RedisplayLine (Address);
            _firstNibble = true;

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Source = new MemoryStream (Encoding.UTF8.GetBytes ("HexView data with wide codepoints: 𝔹Aℝ𝔽!"));

        return true;
    }
}
