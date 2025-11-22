using System.Globalization;
using System.Runtime.CompilerServices;

namespace Terminal.Gui.Views;

/// <summary>Fully featured multi-line text editor</summary>
/// <remarks>
///     <list type="table">
///         <listheader>
///             <term>Shortcut</term> <description>Action performed</description>
///         </listheader>
///         <item>
///             <term>Left cursor, Control-b</term> <description>Moves the editing point left.</description>
///         </item>
///         <item>
///             <term>Right cursor, Control-f</term> <description>Moves the editing point right.</description>
///         </item>
///         <item>
///             <term>Alt-b</term> <description>Moves one word back.</description>
///         </item>
///         <item>
///             <term>Alt-f</term> <description>Moves one word forward.</description>
///         </item>
///         <item>
///             <term>Up cursor, Control-p</term> <description>Moves the editing point one line up.</description>
///         </item>
///         <item>
///             <term>Down cursor, Control-n</term> <description>Moves the editing point one line down</description>
///         </item>
///         <item>
///             <term>Home key, Control-a</term> <description>Moves the cursor to the beginning of the line.</description>
///         </item>
///         <item>
///             <term>End key, Control-e</term> <description>Moves the cursor to the end of the line.</description>
///         </item>
///         <item>
///             <term>Control-Home</term> <description>Scrolls to the first line and moves the cursor there.</description>
///         </item>
///         <item>
///             <term>Control-End</term> <description>Scrolls to the last line and moves the cursor there.</description>
///         </item>
///         <item>
///             <term>Delete, Control-d</term> <description>Deletes the character in front of the cursor.</description>
///         </item>
///         <item>
///             <term>Backspace</term> <description>Deletes the character behind the cursor.</description>
///         </item>
///         <item>
///             <term>Control-k</term>
///             <description>
///                 Deletes the text until the end of the line and replaces the kill buffer with the deleted text.
///                 You can paste this text in a different place by using Control-y.
///             </description>
///         </item>
///         <item>
///             <term>Control-y</term>
///             <description>Pastes the content of the kill ring into the current position.</description>
///         </item>
///         <item>
///             <term>Alt-d</term>
///             <description>
///                 Deletes the word above the cursor and adds it to the kill ring. You can paste the contents of
///                 the kill ring with Control-y.
///             </description>
///         </item>
///         <item>
///             <term>Control-q</term>
///             <description>
///                 Quotes the next input character, to prevent the normal processing of key handling to take
///                 place.
///             </description>
///         </item>
///     </list>
/// </remarks>
public partial class TextView : View, IDesignable
{
    // BUGBUG: AllowsReturn is mis-named. It should be EnterKeyAccepts.
    /// <summary>
    ///     Gets or sets whether pressing ENTER in a <see cref="TextView"/> creates a new line of text
    ///     in the view or invokes the <see cref="View.Accepting"/> event.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property alters <see cref="Multiline"/>.
    ///         If <see cref="AllowsReturn"/> is set to <see langword="true"/>, then <see cref="Multiline"/> is also set to
    ///         `true` and
    ///         vice-versa.
    ///     </para>
    ///     <para>
    ///         If <see cref="AllowsReturn"/> is set to <see langword="false"/>, then <see cref="AllowsTab"/> gets set to
    ///         <see langword="false"/>.
    ///     </para>
    /// </remarks>
    public bool AllowsReturn
    {
        get => _allowsReturn;
        set
        {
            _allowsReturn = value;

            if (_allowsReturn && !_multiline)
            {
                // BUGBUG: Setting properties should not have side-effects like this. Multiline and AllowsReturn should be independent.
                Multiline = true;
            }

            if (!_allowsReturn && _multiline)
            {
                Multiline = false;

                // BUGBUG: Setting properties should not have side-effects like this. Multiline and AllowsTab should be independent.
                AllowsTab = false;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets or sets whether the <see cref="TextView"/> inserts a tab character into the text or ignores tab input. If
    ///     set to `false` and the user presses the tab key (or shift-tab) the focus will move to the next view (or previous
    ///     with shift-tab). The default is `true`; if the user presses the tab key, a tab character will be inserted into the
    ///     text.
    /// </summary>
    public bool AllowsTab
    {
        get => _allowsTab;
        set
        {
            _allowsTab = value;

            if (_allowsTab && _tabWidth == 0)
            {
                _tabWidth = 4;
            }

            if (_allowsTab && !_multiline)
            {
                Multiline = true;
            }

            if (!_allowsTab && _tabWidth > 0)
            {
                _tabWidth = 0;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Provides autocomplete context menu based on suggestions at the current cursor position. Configure
    ///     <see cref="IAutocomplete.SuggestionGenerator"/> to enable this feature
    /// </summary>
    public IAutocomplete Autocomplete { get; protected set; } = new TextViewAutocomplete ();

    /// <summary>Get the Context Menu.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    /// <summary>Gets the cursor column.</summary>
    /// <value>The cursor column.</value>
    public int CurrentColumn { get; private set; }

    /// <summary>Gets the current cursor row.</summary>
    public int CurrentRow { get; private set; }

    /// <summary>Sets or gets the current cursor position.</summary>
    public Point CursorPosition
    {
        get => new (CurrentColumn, CurrentRow);
        set
        {
            List<Cell> line = _model.GetLine (Math.Max (Math.Min (value.Y, _model.Count - 1), 0));

            CurrentColumn = value.X < 0 ? 0 :
                            value.X > line.Count ? line.Count : value.X;

            CurrentRow = value.Y < 0 ? 0 :
                         value.Y > _model.Count - 1 ? Math.Max (_model.Count - 1, 0) : value.Y;
            SetNeedsDraw ();
            AdjustScrollPosition ();
        }
    }

    /// <summary>
    ///     Indicates whatever the text has history changes or not. <see langword="true"/> if the text has history changes
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool HasHistoryChanges => _historyText.HasHistoryChanges;

    /// <summary>
    ///     If <see langword="true"/> and the current <see cref="Cell.Attribute"/> is null will inherit from the
    ///     previous, otherwise if <see langword="false"/> (default) do nothing. If the text is load with
    ///     <see cref="Load(List{Cell})"/> this property is automatically sets to <see langword="true"/>.
    /// </summary>
    public bool InheritsPreviousAttribute { get; set; }

    /// <summary>
    ///     Indicates whatever the text was changed or not. <see langword="true"/> if the text was changed
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool IsDirty
    {
        get => _historyText.IsDirty (_model.GetAllLines ());
        set => _historyText.Clear (_model.GetAllLines ());
    }

    /// <summary>Gets or sets the left column.</summary>
    public int LeftColumn
    {
        get => _leftColumn;
        set
        {
            if (value > 0 && _wordWrap)
            {
                return;
            }

            int clampedValue = Math.Max (Math.Min (value, Maxlength - 1), 0);
            _leftColumn = clampedValue;

            if (IsInitialized && Viewport.X != _leftColumn)
            {
                Viewport = Viewport with { X = _leftColumn };
            }
        }
    }

    /// <summary>Gets the number of lines.</summary>
    public int Lines => _model.Count;

    /// <summary>Gets the maximum visible length line.</summary>
    public int Maxlength => _model.GetMaxVisibleLine (_topRow, _topRow + Viewport.Height, TabWidth);

    /// <summary>Gets or sets a value indicating whether this <see cref="TextView"/> is a multiline text view.</summary>
    public bool Multiline
    {
        get => _multiline;
        set
        {
            _multiline = value;

            if (_multiline && !_allowsTab)
            {
                AllowsTab = true;
            }

            if (_multiline && !_allowsReturn)
            {
                AllowsReturn = true;
            }

            if (!_multiline)
            {
                AllowsReturn = false;
                AllowsTab = false;
                WordWrap = false;
                CurrentColumn = 0;
                CurrentRow = 0;
                _savedHeight = Height;

                Height = Dim.Auto (DimAutoStyle.Text, 1);

                if (!IsInitialized)
                {
                    _model.LoadString (Text);
                }

                SetNeedsDraw ();
            }
            else if (_multiline && _savedHeight is { })
            {
                Height = _savedHeight;
                SetNeedsDraw ();
            }

            KeyBindings.Remove (Key.Enter);
            KeyBindings.Add (Key.Enter, Multiline ? Command.NewLine : Command.Accept);
        }
    }

    /// <summary>Gets or sets whether the <see cref="TextView"/> is in read-only mode or not</summary>
    /// <value>Boolean value(Default false)</value>
    public bool ReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (value != _isReadOnly)
            {
                _isReadOnly = value;

                SetNeedsDraw ();
                WrapTextModel ();
                AdjustScrollPosition ();
            }
        }
    }

    /// <summary>Gets or sets a value indicating the number of whitespace when pressing the TAB key.</summary>
    public int TabWidth
    {
        get => _tabWidth;
        set
        {
            _tabWidth = Math.Max (value, 0);

            if (_tabWidth > 0 && !AllowsTab)
            {
                AllowsTab = true;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>Sets or gets the text in the <see cref="TextView"/>.</summary>
    /// <remarks>
    ///     The <see cref="View.TextChanged"/> event is fired whenever this property is set. Note, however, that Text is not
    ///     set by <see cref="TextView"/> as the user types.
    /// </remarks>
    public override string Text
    {
        get
        {
            if (_wordWrap)
            {
                return _wrapManager!.Model.ToString ();
            }

            return _model.ToString ();
        }
        set
        {
            ResetPosition ();
            _model.LoadString (value);

            if (_wordWrap)
            {
                _wrapManager = new (_model);
                _model = _wrapManager.WrapModel (Viewport.Width, out _, out _, out _, out _);
            }

            OnTextChanged ();
            SetNeedsDraw ();

            _historyText.Clear (_model.GetAllLines ());
        }
    }

    /// <summary>Gets or sets the top row.</summary>
    public int TopRow
    {
        get => _topRow;
        set
        {
            int clampedValue = Math.Max (Math.Min (value, Lines - 1), 0);
            _topRow = clampedValue;

            if (IsInitialized && Viewport.Y != _topRow)
            {
                Viewport = Viewport with { Y = _topRow };
            }
        }
    }

    /// <summary>
    ///     Tracks whether the text view should be considered "used", that is, that the user has moved in the entry, so
    ///     new input should be appended at the cursor position, rather than clearing the entry
    /// </summary>
    public bool Used { get; set; }

    /// <summary>Allows word wrap the to fit the available container width.</summary>
    public bool WordWrap
    {
        get => _wordWrap;
        set
        {
            if (value == _wordWrap)
            {
                return;
            }

            if (value && !_multiline)
            {
                return;
            }

            _wordWrap = value;
            ResetPosition ();

            if (_wordWrap)
            {
                _wrapManager = new (_model);
                WrapTextModel ();
            }
            else if (!_wordWrap && _wrapManager is { })
            {
                _model = _wrapManager.Model;
            }

            // Update horizontal scrollbar AutoShow based on WordWrap
            if (IsInitialized)
            {
                HorizontalScrollBar.AutoShow = !_wordWrap;
                UpdateContentSize ();
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets or sets whether the word forward and word backward navigation should use the same or equivalent rune type.
    ///     Default is <c>false</c> meaning using equivalent rune type.
    /// </summary>
    public bool UseSameRuneTypeForWords { get; set; }



    /// <summary>Allows clearing the <see cref="HistoryTextItemEventArgs"/> items updating the original text.</summary>
    public void ClearHistoryChanges () { _historyText?.Clear (_model.GetAllLines ()); }

    /// <summary>Closes the contents of the stream into the <see cref="TextView"/>.</summary>
    /// <returns><c>true</c>, if stream was closed, <c>false</c> otherwise.</returns>
    public bool CloseFile ()
    {
        SetWrapModel ();
        bool res = _model.CloseFile ();
        ResetPosition ();
        SetNeedsDraw ();
        UpdateWrapModel ();

        return res;
    }

    /// <summary>Raised when the contents of the <see cref="TextView"/> are changed.</summary>
    /// <remarks>
    ///     Unlike the <see cref="View.TextChanged"/> event, this event is raised whenever the user types or otherwise changes
    ///     the contents of the <see cref="TextView"/>.
    /// </remarks>
    public event EventHandler<ContentsChangedEventArgs>? ContentsChanged;

    /// <summary>
    ///     Open a dialog to set the foreground and background colors.
    /// </summary>
    public void PromptForColors ()
    {
        if (!ColorPicker.Prompt (
                                 "Colors",
                                 GetSelectedCellAttribute (),
                                 out Attribute newAttribute
                                ))
        {
            return;
        }

        var attribute = new Attribute (
                                       newAttribute.Foreground,
                                       newAttribute.Background,
                                       newAttribute.Style
                                      );

        ApplyCellsAttribute (attribute);
    }

    /// <summary>Gets all lines of characters.</summary>
    /// <returns></returns>
    public List<List<Cell>> GetAllLines () { return _model.GetAllLines (); }

    /// <summary>
    ///     Returns the characters on the current line (where the cursor is positioned). Use <see cref="CurrentColumn"/>
    ///     to determine the position of the cursor within that line
    /// </summary>
    /// <returns></returns>
    public List<Cell> GetCurrentLine () { return _model.GetLine (CurrentRow); }

    /// <summary>Returns the characters on the <paramref name="line"/>.</summary>
    /// <param name="line">The intended line.</param>
    /// <returns></returns>
    public List<Cell> GetLine (int line) { return _model.GetLine (line); }

    /// <summary>Loads the contents of the file into the <see cref="TextView"/>.</summary>
    /// <returns><c>true</c>, if file was loaded, <c>false</c> otherwise.</returns>
    /// <param name="path">Path to the file to load.</param>
    public bool Load (string path)
    {
        SetWrapModel ();
        bool res;

        try
        {
            SetWrapModel ();
            res = _model.LoadFile (path);
            _historyText.Clear (_model.GetAllLines ());
            ResetPosition ();
        }
        finally
        {
            UpdateWrapModel ();
            SetNeedsDraw ();
            AdjustScrollPosition ();
        }

        UpdateWrapModel ();

        return res;
    }

    /// <summary>Loads the contents of the stream into the <see cref="TextView"/>.</summary>
    /// <returns><c>true</c>, if stream was loaded, <c>false</c> otherwise.</returns>
    /// <param name="stream">Stream to load the contents from.</param>
    public void Load (Stream stream)
    {
        SetWrapModel ();
        _model.LoadStream (stream);
        _historyText.Clear (_model.GetAllLines ());
        ResetPosition ();
        SetNeedsDraw ();
        UpdateWrapModel ();
    }

    /// <summary>Loads the contents of the <see cref="Cell"/> list into the <see cref="TextView"/>.</summary>
    /// <param name="cells">Text cells list to load the contents from.</param>
    public void Load (List<Cell> cells)
    {
        SetWrapModel ();
        _model.LoadCells (cells, GetAttributeForRole (VisualRole.Focus));
        _historyText.Clear (_model.GetAllLines ());
        ResetPosition ();
        SetNeedsDraw ();
        UpdateWrapModel ();
        InheritsPreviousAttribute = true;
    }

    /// <summary>Loads the contents of the list of <see cref="Cell"/> list into the <see cref="TextView"/>.</summary>
    /// <param name="cellsList">List of rune cells list to load the contents from.</param>
    public void Load (List<List<Cell>> cellsList)
    {
        SetWrapModel ();
        InheritsPreviousAttribute = true;
        _model.LoadListCells (cellsList, GetAttributeForRole (VisualRole.Focus));
        _historyText.Clear (_model.GetAllLines ());
        ResetPosition ();
        SetNeedsDraw ();
        UpdateWrapModel ();
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs ev)
    {
        if (ev is { IsSingleDoubleOrTripleClicked: false, IsPressed: false, IsReleased: false, IsWheel: false }
            && !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)
            && !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift)
            && !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked | MouseFlags.ButtonShift)
            && !ev.Flags.HasFlag (ContextMenu!.MouseFlags))
        {
            return false;
        }

        if (!CanFocus)
        {
            return true;
        }

        if (!HasFocus)
        {
            SetFocus ();
        }

        _continuousFind = false;

        // Give autocomplete first opportunity to respond to mouse clicks
        if (SelectedLength == 0 && Autocomplete.OnMouseEvent (ev, true))
        {
            return true;
        }

        if (ev.Flags == MouseFlags.Button1Clicked)
        {
            if (_isButtonReleased)
            {
                _isButtonReleased = false;

                if (SelectedLength == 0)
                {
                    StopSelecting ();
                }

                return true;
            }

            if (_shiftSelecting && !_isButtonShift)
            {
                StopSelecting ();
            }

            ProcessMouseClick (ev, out _);

            if (Used)
            {
                PositionCursor ();
            }
            else
            {
                SetNeedsDraw ();
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (ev.Flags == MouseFlags.WheeledDown)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_topRow + 1);
        }
        else if (ev.Flags == MouseFlags.WheeledUp)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_topRow - 1);
        }
        else if (ev.Flags == MouseFlags.WheeledRight)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_leftColumn + 1, false);
        }
        else if (ev.Flags == MouseFlags.WheeledLeft)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_leftColumn - 1, false);
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            ProcessMouseClick (ev, out List<Cell> line);
            PositionCursor ();

            if (_model.Count > 0 && _shiftSelecting && IsSelecting)
            {
                if (CurrentRow - _topRow >= Viewport.Height - 1 && _model.Count > _topRow + CurrentRow)
                {
                    ScrollTo (_topRow + Viewport.Height);
                }
                else if (_topRow > 0 && CurrentRow <= _topRow)
                {
                    ScrollTo (_topRow - Viewport.Height);
                }
                else if (ev.Position.Y >= Viewport.Height)
                {
                    ScrollTo (_model.Count);
                }
                else if (ev.Position.Y < 0 && _topRow > 0)
                {
                    ScrollTo (0);
                }

                if (CurrentColumn - _leftColumn >= Viewport.Width - 1 && line.Count > _leftColumn + CurrentColumn)
                {
                    ScrollTo (_leftColumn + Viewport.Width, false);
                }
                else if (_leftColumn > 0 && CurrentColumn <= _leftColumn)
                {
                    ScrollTo (_leftColumn - Viewport.Width, false);
                }
                else if (ev.Position.X >= Viewport.Width)
                {
                    ScrollTo (line.Count, false);
                }
                else if (ev.Position.X < 0 && _leftColumn > 0)
                {
                    ScrollTo (0, false);
                }
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift))
        {
            if (!_shiftSelecting)
            {
                _isButtonShift = true;
                StartSelecting ();
            }

            ProcessMouseClick (ev, out _);
            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            if (_shiftSelecting)
            {
                _clickWithSelecting = true;
                StopSelecting ();
            }

            ProcessMouseClick (ev, out _);
            PositionCursor ();

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;

            if (App?.Mouse.MouseGrabView is null)
            {
                App?.Mouse.GrabMouse (this);
            }
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1Released))
        {
            _isButtonReleased = true;
            App?.Mouse.UngrabMouse ();
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked))
        {
            if (ev.Flags.HasFlag (MouseFlags.ButtonShift))
            {
                if (!IsSelecting)
                {
                    StartSelecting ();
                }
            }
            else if (IsSelecting)
            {
                StopSelecting ();
            }

            ProcessMouseClick (ev, out List<Cell> line);

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            (int startCol, int col, int row)? newPos = _model.ProcessDoubleClickSelection (SelectionStartColumn, CurrentColumn, CurrentRow, UseSameRuneTypeForWords, SelectWordOnlyOnDoubleClick);

            if (newPos.HasValue)
            {
                SelectionStartColumn = newPos.Value.startCol;
                CurrentColumn = newPos.Value.col;
                CurrentRow = newPos.Value.row;
            }

            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            SetNeedsDraw ();
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1TripleClicked))
        {
            if (IsSelecting)
            {
                StopSelecting ();
            }

            ProcessMouseClick (ev, out List<Cell> line);
            CurrentColumn = 0;

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            CurrentColumn = line.Count;
            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            SetNeedsDraw ();
        }
        else if (ev.Flags == ContextMenu!.MouseFlags)
        {
            ShowContextMenu (ev.ScreenPosition);
        }

        OnUnwrappedCursorPosition ();

        return true;
    }

    /// <summary>
    ///     Called when the contents of the TextView change. E.g. when the user types text or deletes text. Raises the
    ///     <see cref="ContentsChanged"/> event.
    /// </summary>
    public virtual void OnContentsChanged ()
    {
        ContentsChanged?.Invoke (this, new (CurrentRow, CurrentColumn));

        ProcessInheritsPreviousScheme (CurrentRow, CurrentColumn);
        ProcessAutocomplete ();

        // Update content size when content changes
        if (IsInitialized)
        {
            UpdateContentSize ();
        }
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (App?.Mouse.MouseGrabView is { } && App?.Mouse.MouseGrabView == this)
        {
            App?.Mouse.UngrabMouse ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        if (!key.IsValid)
        {
            return false;
        }

        // Give autocomplete first opportunity to respond to key presses
        if (SelectedLength == 0 && Autocomplete.Suggestions.Count > 0 && Autocomplete.ProcessKey (key))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key a)
    {
        if (!CanFocus)
        {
            return true;
        }

        ResetColumnTrack ();

        // Ignore control characters and other special keys
        if (!a.IsKeyCodeAtoZ && (a.KeyCode < KeyCode.Space || a.KeyCode > KeyCode.CharMask))
        {
            return false;
        }

        InsertText (a);
        DoNeededAction ();

        return true;
    }

    /// <inheritdoc/>
    public override bool OnKeyUp (Key key)
    {
        if (key == Key.Space.WithCtrl)
        {
            return true;
        }

        return false;
    }

    /// <summary>Positions the cursor on the current row and column</summary>
    public override Point? PositionCursor ()
    {
        ProcessAutocomplete ();

        if (!CanFocus || !Enabled || Driver is null)
        {
            return null;
        }

        if (App?.Mouse.MouseGrabView == this && IsSelecting)
        {
            // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
            //var minRow = Math.Min (Math.Max (Math.Min (selectionStartRow, currentRow) - topRow, 0), Viewport.Height);
            //var maxRow = Math.Min (Math.Max (Math.Max (selectionStartRow, currentRow) - topRow, 0), Viewport.Height);
            //SetNeedsDraw (new (0, minRow, Viewport.Width, maxRow));
            SetNeedsDraw ();
        }

        List<Cell> line = _model.GetLine (CurrentRow);
        var col = 0;

        if (line.Count > 0)
        {
            for (int idx = _leftColumn; idx < line.Count; idx++)
            {
                if (idx >= CurrentColumn)
                {
                    break;
                }

                int cols = line [idx].Grapheme.GetColumns ();

                if (line [idx].Grapheme == "\t")
                {
                    cols += TabWidth + 1;
                }
                else
                {
                    // Ensures that cols less than 0 to be 1 because it will be converted to a printable rune
                    cols = Math.Max (cols, 1);
                }

                if (!TextModel.SetCol (ref col, Viewport.Width, cols))
                {
                    col = CurrentColumn;

                    break;
                }
            }
        }

        int posX = CurrentColumn - _leftColumn;
        int posY = CurrentRow - _topRow;

        if (posX > -1 && col >= posX && posX < Viewport.Width && _topRow <= CurrentRow && posY < Viewport.Height)
        {
            Move (col, CurrentRow - _topRow);

            return new (col, CurrentRow - _topRow);
        }

        return null; // Hide cursor
    }

    /// <summary>Redoes the latest changes.</summary>
    public void Redo ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Redo ();
    }

    ///// <summary>Raised when the <see cref="Text"/> property of the <see cref="TextView"/> changes.</summary>
    ///// <remarks>
    /////     The <see cref="Text"/> property of <see cref="TextView"/> only changes when it is explicitly set, not as the
    /////     user types. To be notified as the user changes the contents of the TextView see <see cref="IsDirty"/>.
    ///// </remarks>
    //public event EventHandler? TextChanged;

    /// <summary>Undoes the latest changes.</summary>
    public void Undo ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Undo ();
    }

    private void ClearRegion (int left, int top, int right, int bottom)
    {
        for (int row = top; row < bottom; row++)
        {
            Move (left, row);

            for (int col = left; col < right; col++)
            {
                AddRune (col, row, (Rune)' ');
            }
        }
    }

    private void GenerateSuggestions ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        int cursorPosition = Math.Min (CurrentColumn, currentLine.Count);

        Autocomplete.Context = new (
                                    currentLine,
                                    cursorPosition,
                                    Autocomplete.Context != null
                                        ? Autocomplete.Context.Canceled
                                        : false
                                   );

        Autocomplete.GenerateSuggestions (
                                          Autocomplete.Context
                                         );
    }

    private void ProcessAutocomplete ()
    {
        if (_isDrawing)
        {
            return;
        }

        if (_clickWithSelecting)
        {
            _clickWithSelecting = false;

            return;
        }

        if (SelectedLength > 0)
        {
            return;
        }

        // draw autocomplete
        GenerateSuggestions ();

        var renderAt = new Point (
                                  Autocomplete.Context.CursorPosition,
                                  Autocomplete.PopupInsideContainer
                                      ? CursorPosition.Y + 1 - TopRow
                                      : 0
                                 );

        Autocomplete.RenderOverlay (renderAt);
    }

    private bool ProcessBackTab ()
    {
        ResetColumnTrack ();

        if (!AllowsTab || _isReadOnly)
        {
            return false;
        }

        if (CurrentColumn > 0)
        {
            SetWrapModel ();

            List<Cell> currentLine = GetCurrentLine ();

            if (currentLine.Count > 0 && currentLine [CurrentColumn - 1].Grapheme == "\t")
            {
                _historyText.Add (new () { new (currentLine) }, CursorPosition);

                currentLine.RemoveAt (CurrentColumn - 1);
                CurrentColumn--;

                _historyText.Add (
                                  new () { new (GetCurrentLine ()) },
                                  CursorPosition,
                                  TextEditingLineStatus.Replaced
                                 );
            }

            SetNeedsDraw ();

            UpdateWrapModel ();
        }

        DoNeededAction ();

        return true;
    }

    // If InheritsPreviousScheme is enabled this method will check if the rune cell on
    // the row and col location and around has a not null scheme. If it's null will set it with
    // the very most previous valid scheme.
    private void ProcessInheritsPreviousScheme (int row, int col)
    {
        if (!InheritsPreviousAttribute || (Lines == 1 && GetLine (Lines).Count == 0))
        {
            return;
        }

        List<Cell> line = GetLine (row);
        List<Cell> lineToSet = line;

        while (line.Count == 0)
        {
            if (row == 0 && line.Count == 0)
            {
                return;
            }

            row--;
            line = GetLine (row);
            lineToSet = line;
        }

        int colWithColor = Math.Max (Math.Min (col - 2, line.Count - 1), 0);
        Cell cell = line [colWithColor];
        int colWithoutColor = Math.Max (col - 1, 0);

        Cell lineTo = lineToSet [colWithoutColor];

        if (cell.Attribute is { } && colWithColor == 0 && lineTo.Attribute is { })
        {
            for (int r = row - 1; r > -1; r--)
            {
                List<Cell> l = GetLine (r);

                for (int c = l.Count - 1; c > -1; c--)
                {
                    Cell cell1 = l [c];

                    if (cell1.Attribute is null)
                    {
                        cell1.Attribute = cell.Attribute;
                        l [c] = cell1;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            return;
        }

        if (cell.Attribute is null)
        {
            for (int r = row; r > -1; r--)
            {
                List<Cell> l = GetLine (r);

                colWithColor = l.FindLastIndex (
                                                colWithColor > -1 ? colWithColor : l.Count - 1,
                                                c => c.Attribute != null
                                               );

                if (colWithColor > -1 && l [colWithColor].Attribute is { })
                {
                    cell = l [colWithColor];

                    break;
                }
            }
        }
        else
        {
            int cRow = row;

            while (cell.Attribute is null)
            {
                if ((colWithColor == 0 || cell.Attribute is null) && cRow > 0)
                {
                    line = GetLine (--cRow);
                    colWithColor = line.Count - 1;
                    cell = line [colWithColor];
                }
                else if (cRow == 0 && colWithColor < line.Count)
                {
                    cell = line [colWithColor + 1];
                }
            }
        }

        if (cell.Attribute is { } && colWithColor > -1 && colWithoutColor < lineToSet.Count && lineTo.Attribute is null)
        {
            while (lineTo.Attribute is null)
            {
                lineTo.Attribute = cell.Attribute;
                lineToSet [colWithoutColor] = lineTo;
                colWithoutColor--;

                if (colWithoutColor == -1 && row > 0)
                {
                    lineToSet = GetLine (--row);
                    colWithoutColor = lineToSet.Count - 1;
                }
            }
        }
    }

    private void ProcessMouseClick (MouseEventArgs ev, out List<Cell> line)
    {
        List<Cell>? r = null;

        if (_model.Count > 0)
        {
            int maxCursorPositionableLine = Math.Max (_model.Count - 1 - _topRow, 0);

            if (Math.Max (ev.Position.Y, 0) > maxCursorPositionableLine)
            {
                CurrentRow = maxCursorPositionableLine + _topRow;
            }
            else
            {
                CurrentRow = Math.Max (ev.Position.Y + _topRow, 0);
            }

            r = GetCurrentLine ();
            int idx = TextModel.GetColFromX (r, _leftColumn, Math.Max (ev.Position.X, 0), TabWidth);

            if (idx - _leftColumn >= r.Count)
            {
                CurrentColumn = Math.Max (r.Count - _leftColumn - (ReadOnly ? 1 : 0), 0);
            }
            else
            {
                CurrentColumn = idx + _leftColumn;
            }
        }

        line = r!;
    }



    private bool ProcessEnterKey (ICommandContext? commandContext)
    {
        ResetColumnTrack ();

        if (_isReadOnly)
        {
            return false;
        }

        if (!AllowsReturn)
        {
            // By Default pressing ENTER should be ignored (OnAccept will return false or null). Only cancel if the
            // event was fired and set Cancel = true.
            return RaiseAccepting (commandContext) is null or false;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add (new () { new (currentLine) }, CursorPosition);

        if (IsSelecting)
        {
            ClearSelectedRegion ();
            currentLine = GetCurrentLine ();
        }

        int restCount = currentLine.Count - CurrentColumn;
        List<Cell> rest = currentLine.GetRange (CurrentColumn, restCount);
        currentLine.RemoveRange (CurrentColumn, restCount);

        List<List<Cell>> addedLines = new () { new (currentLine) };

        _model.AddLine (CurrentRow + 1, rest);

        addedLines.Add (new (_model.GetLine (CurrentRow + 1)));

        _historyText.Add (addedLines, CursorPosition, TextEditingLineStatus.Added);

        CurrentRow++;

        var fullNeedsDraw = false;

        if (CurrentRow >= _topRow + Viewport.Height)
        {
            _topRow++;
            fullNeedsDraw = true;
        }

        CurrentColumn = 0;

        _historyText.Add (
                          new () { new (GetCurrentLine ()) },
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        if (!_wordWrap && CurrentColumn < _leftColumn)
        {
            fullNeedsDraw = true;
            _leftColumn = 0;
        }

        if (fullNeedsDraw)
        {
            SetNeedsDraw ();
        }
        else
        {
            // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
            //SetNeedsDraw (new (0, currentRow - topRow, 2, Viewport.Height));
            SetNeedsDraw ();
        }

        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();

        return true;
    }

    private void ProcessSetOverwrite ()
    {
        ResetColumnTrack ();
        SetOverwrite (!Used);
    }

    private bool ProcessTab ()
    {
        ResetColumnTrack ();

        if (!AllowsTab || _isReadOnly)
        {
            return false;
        }

        InsertText (new Key ((KeyCode)'\t'));
        DoNeededAction ();

        return true;
    }



    private void SetOverwrite (bool overwrite)
    {
        Used = overwrite;
        SetNeedsDraw ();
        DoNeededAction ();
    }

    private void SetValidUsedColor (Attribute? attribute)
    {
        // BUGBUG: (v2 truecolor) This code depends on 8-bit color names; disabling for now
        //if ((scheme!.HotNormal.Foreground & scheme.Focus.Background) == scheme.Focus.Foreground) {
        SetAttribute (new (attribute!.Value.Background, attribute!.Value.Foreground, attribute!.Value.Style));
    }

}