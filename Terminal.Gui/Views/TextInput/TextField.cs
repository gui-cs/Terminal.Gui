using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>Single-line text editor.</summary>
/// <remarks>The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.</remarks>
public class TextField : View, IDesignable
{
    private readonly HistoryText _historyText;
    private CultureInfo _currentCulture;
    private int _cursorPosition;
    private bool _isButtonPressed;
    private bool _isButtonReleased;
    private bool _isDrawing;
    private int _preTextChangedCursorPos;
    private int _selectedStart; // -1 represents there is no text selection.
    private string _selectedText;
    private int _start;
    private List<Rune> _text;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TextField"/> class.
    /// </summary>
    public TextField ()
    {
        _historyText = new ();
        _isButtonReleased = true;
        _selectedStart = -1;
        _text = new ();

        // TODO: Determine if this is a good choice. Previously this was hard coded to 
        // TODO: DarkGray which was NOT a good choice.
        CaptionColor = GetAttributeForRole (VisualRole.Normal).Foreground.GetBrighterColor();
        ReadOnly = false;
        Autocomplete = new TextFieldAutocomplete ();
        Height = Dim.Auto (DimAutoStyle.Text, 1);

        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;
        Used = true;
        WantMousePositionReports = true;

        // By default, disable hotkeys (in case someome sets Title)
        HotKeySpecifier = new ('\xffff');

        _historyText.ChangeText += HistoryText_ChangeText;

        Initialized += TextField_Initialized;

        SuperViewChanged += TextField_SuperViewChanged;

        // Things this view knows how to do
        AddCommand (
                    Command.DeleteCharRight,
                    () =>
                    {
                        DeleteCharRight ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteCharLeft,
                    () =>
                    {
                        DeleteCharLeft (false);

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftStartExtend,
                    () =>
                    {
                        MoveHomeExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEndExtend,
                    () =>
                    {
                        MoveEndExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftStart,
                    () =>
                    {
                        MoveHome ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftExtend,
                    () =>
                    {
                        MoveLeftExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightExtend,
                    () =>
                    {
                        MoveRightExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordLeftExtend,
                    () =>
                    {
                        MoveWordLeftExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordRightExtend,
                    () =>
                    {
                        MoveWordRightExtend ();

                        return true;
                    }
                   );

        AddCommand (Command.Left, () => MoveLeft ());

        AddCommand (
                    Command.RightEnd,
                    () =>
                    {
                        MoveEnd ();

                        return true;
                    }
                   );

        AddCommand (Command.Right, () => MoveRight ());

        AddCommand (
                    Command.CutToEndLine,
                    () =>
                    {
                        KillToEnd ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.CutToStartLine,
                    () =>
                    {
                        KillToStart ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Undo,
                    () =>
                    {
                        Undo ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Redo,
                    () =>
                    {
                        Redo ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordLeft,
                    () =>
                    {
                        MoveWordLeft ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordRight,
                    () =>
                    {
                        MoveWordRight ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.KillWordForwards,
                    () =>
                    {
                        KillWordForwards ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.KillWordBackwards,
                    () =>
                    {
                        KillWordBackwards ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.ToggleOverwrite,
                    () =>
                    {
                        SetOverwrite (!Used);

                        return true;
                    }
                   );

        AddCommand (
                    Command.EnableOverwrite,
                    () =>
                    {
                        SetOverwrite (true);

                        return true;
                    }
                   );

        AddCommand (
                    Command.DisableOverwrite,
                    () =>
                    {
                        SetOverwrite (false);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Copy,
                    () =>
                    {
                        Copy ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Cut,
                    () =>
                    {
                        Cut ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Paste,
                    () =>
                    {
                        Paste ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.SelectAll,
                    () =>
                    {
                        SelectAll ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteAll,
                    () =>
                    {
                        DeleteAll ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Context,
                    () =>
                    {
                        ShowContextMenu (true);

                        return true;
                    }
                   );

        // Default keybindings for this view
        // We follow this as closely as possible: https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts
        KeyBindings.Add (Key.Delete, Command.DeleteCharRight);
        KeyBindings.Add (Key.D.WithCtrl, Command.DeleteCharRight);

        KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);

        KeyBindings.Add (Key.Home.WithShift, Command.LeftStartExtend);
        KeyBindings.Add (Key.Home.WithShift.WithCtrl, Command.LeftStartExtend);
        KeyBindings.Add (Key.A.WithShift.WithCtrl, Command.LeftStartExtend);

        KeyBindings.Add (Key.End.WithShift, Command.RightEndExtend);
        KeyBindings.Add (Key.End.WithShift.WithCtrl, Command.RightEndExtend);
        KeyBindings.Add (Key.E.WithShift.WithCtrl, Command.RightEndExtend);

        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Add (Key.Home.WithCtrl, Command.LeftStart);

        KeyBindings.Add (Key.CursorLeft.WithShift, Command.LeftExtend);
        KeyBindings.Add (Key.CursorUp.WithShift, Command.LeftExtend);

        KeyBindings.Add (Key.CursorRight.WithShift, Command.RightExtend);
        KeyBindings.Add (Key.CursorDown.WithShift, Command.RightExtend);

        KeyBindings.Add (Key.CursorLeft.WithShift.WithCtrl, Command.WordLeftExtend);
        KeyBindings.Add (Key.CursorUp.WithShift.WithCtrl, Command.WordLeftExtend);

        KeyBindings.Add (Key.CursorRight.WithShift.WithCtrl, Command.WordRightExtend);
        KeyBindings.Add (Key.CursorDown.WithShift.WithCtrl, Command.WordRightExtend);

        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.B.WithCtrl, Command.Left);

        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Add (Key.End.WithCtrl, Command.RightEnd);
        KeyBindings.Add (Key.E.WithCtrl, Command.RightEnd);

        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.F.WithCtrl, Command.Right);

        KeyBindings.Add (Key.K.WithCtrl, Command.CutToEndLine);
        KeyBindings.Add (Key.K.WithCtrl.WithShift, Command.CutToStartLine);

        KeyBindings.Add (Key.Z.WithCtrl, Command.Undo);

        KeyBindings.Add (Key.Y.WithCtrl, Command.Redo);

        KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.WordLeft);
        KeyBindings.Add (Key.CursorUp.WithCtrl, Command.WordLeft);

        KeyBindings.Add (Key.CursorRight.WithCtrl, Command.WordRight);
        KeyBindings.Add (Key.CursorDown.WithCtrl, Command.WordRight);

#if UNIX_KEY_BINDINGS
        KeyBindings.Add (Key.F.WithShift.WithAlt, Command.WordRightExtend);
        KeyBindings.Add (Key.K.WithAlt, Command.CutToStartLine);
        KeyBindings.Add (Key.B.WithShift.WithAlt, Command.WordLeftExtend);
        KeyBindings.Add (Key.B.WithAlt, Command.WordLeft);
        KeyBindings.Add (Key.F.WithAlt, Command.WordRight);
        KeyBindings.Add (Key.Backspace.WithAlt, Command.Undo);
#endif

        KeyBindings.Add (Key.Delete.WithCtrl, Command.KillWordForwards);
        KeyBindings.Add (Key.Backspace.WithCtrl, Command.KillWordBackwards);
        KeyBindings.Add (Key.InsertChar, Command.ToggleOverwrite);
        KeyBindings.Add (Key.C.WithCtrl, Command.Copy);
        KeyBindings.Add (Key.X.WithCtrl, Command.Cut);
        KeyBindings.Add (Key.V.WithCtrl, Command.Paste);
        KeyBindings.Add (Key.A.WithCtrl, Command.SelectAll);

        KeyBindings.Add (Key.R.WithCtrl, Command.DeleteAll);
        KeyBindings.Add (Key.D.WithCtrl.WithShift, Command.DeleteAll);

        KeyBindings.Remove (Key.Space);

        _currentCulture = Thread.CurrentThread.CurrentUICulture;

        CreateContextMenu ();
        KeyBindings.Add (ContextMenu.Key, Command.Context);
    }

    /// <summary>
    ///     Provides autocomplete context menu based on suggestions at the current cursor position. Configure
    ///     <see cref="ISuggestionGenerator"/> to enable this feature.
    /// </summary>
    public IAutocomplete Autocomplete { get; set; }

    /// <summary>
    ///     Gets or sets the text to render in control when no value has been entered yet and the <see cref="View"/> does
    ///     not yet have input focus.
    /// </summary>
    public string Caption { get; set; }

    /// <summary>Gets or sets the foreground <see cref="Color"/> to use when rendering <see cref="Caption"/>.</summary>
    public Color CaptionColor { get; set; }

    /// <summary>Get the Context Menu for this view.</summary>
    [CanBeNull]
    public PopoverMenu ContextMenu { get; private set; }

    /// <summary>Sets or gets the current cursor position.</summary>
    public virtual int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            if (value < 0)
            {
                _cursorPosition = 0;
            }
            else if (value > _text.Count)
            {
                _cursorPosition = _text.Count;
            }
            else
            {
                _cursorPosition = value;
            }

            PrepareSelection (_selectedStart, _cursorPosition - _selectedStart);
        }
    }

    /// <summary>
    ///     Indicates whatever the text has history changes or not. <see langword="true"/> if the text has history changes
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool HasHistoryChanges => _historyText.HasHistoryChanges;

    /// <summary>
    ///     Indicates whatever the text was changed or not. <see langword="true"/> if the text was changed
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool IsDirty => _historyText.IsDirty ([Cell.StringToCells (Text)]);

    /// <summary>If set to true its not allow any changes in the text.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>Gets the left offset position.</summary>
    public int ScrollOffset { get; private set; }

    /// <summary>
    ///     Sets the secret property.
    ///     <remarks>This makes the text entry suitable for entering passwords.</remarks>
    /// </summary>
    public bool Secret { get; set; }

    /// <summary>Length of the selected text.</summary>
    public int SelectedLength { get; private set; }

    /// <summary>Start position of the selected text.</summary>
    public int SelectedStart
    {
        get => _selectedStart;
        set
        {
            if (value < -1)
            {
                _selectedStart = -1;
            }
            else if (value > _text.Count)
            {
                _selectedStart = _text.Count;
            }
            else
            {
                _selectedStart = value;
            }

            PrepareSelection (_selectedStart, _cursorPosition - _selectedStart);
        }
    }

    /// <summary>The selected text.</summary>
    public string SelectedText
    {
        get => Secret ? null : _selectedText;
        private set => _selectedText = value;
    }

    /// <summary>Sets or gets the text held by the view.</summary>
    public new string Text
    {
        get => StringExtensions.ToString (_text);
        set
        {
            var oldText = StringExtensions.ToString (_text);

            if (oldText == value)
            {
                return;
            }

            string newText = value.Replace ("\t", "").Split ("\n") [0];
            ResultEventArgs<string> args = new (newText);
            RaiseTextChanging (args);

            if (args.Handled)
            {
                if (_cursorPosition > _text.Count)
                {
                    _cursorPosition = _text.Count;
                }

                return;
            }

            ClearAllSelection ();

            // Note we use NewValue here; TextChanging subscribers may have changed it
            _text = args.Result.EnumerateRunes ().ToList ();

            if (!Secret && !_historyText.IsFromHistory)
            {
                _historyText.Add (
                                  [Cell.ToCellList (oldText)],
                                  new (_cursorPosition, 0)
                                 );

                _historyText.Add (
                                  [Cell.ToCells (_text)],
                                  new (_cursorPosition, 0),
                                  TextEditingLineStatus.Replaced
                                 );
            }

            OnTextChanged ();

            ProcessAutocomplete ();

            if (_cursorPosition > _text.Count)
            {
                _cursorPosition = Math.Max (TextModel.DisplaySize (_text, 0).size - 1, 0);
            }

            Adjust ();
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Tracks whether the text field should be considered "used", that is, that the user has moved in the entry, so
    ///     new input should be appended at the cursor position, rather than clearing the entry
    /// </summary>
    public bool Used { get; set; }

    /// <summary>
    ///     Gets or sets whether the word forward and word backward navigation should use the same or equivalent rune type.
    ///     Default is <c>false</c> meaning using equivalent rune type.
    /// </summary>
    public bool UseSameRuneTypeForWords { get; set; }

    /// <summary>
    ///     Gets or sets whether the word navigation should select only the word itself without spaces around it or with the
    ///     spaces at right.
    ///     Default is <c>false</c> meaning that the spaces at right are included in the selection.
    /// </summary>
    public bool SelectWordOnlyOnDoubleClick { get; set; }

    /// <summary>Clear the selected text.</summary>
    public void ClearAllSelection ()
    {
        if (_selectedStart == -1 && SelectedLength == 0 && string.IsNullOrEmpty (_selectedText))
        {
            return;
        }

        _selectedStart = -1;
        SelectedLength = 0;
        _selectedText = null;
        _start = 0;
        SelectedLength = 0;
        SetNeedsDraw ();
    }

    /// <summary>Clears the history.</summary>
    public void ClearHistoryChanges () { _historyText.Clear ([Cell.StringToCells (Text)]); }

    /// <summary>Copy the selected text to the clipboard.</summary>
    public virtual void Copy ()
    {
        if (Secret || SelectedLength == 0)
        {
            return;
        }

        Clipboard.Contents = SelectedText;
    }

    /// <summary>Cut the selected text to the clipboard.</summary>
    public virtual void Cut ()
    {
        if (ReadOnly || Secret || SelectedLength == 0)
        {
            return;
        }

        Clipboard.Contents = SelectedText;
        List<Rune> newText = DeleteSelectedText ();
        Text = StringExtensions.ToString (newText);
        Adjust ();
    }

    /// <summary>Deletes all text.</summary>
    public void DeleteAll ()
    {
        if (_text.Count == 0)
        {
            return;
        }

        _selectedStart = 0;
        MoveEndExtend ();
        DeleteCharLeft (false);
        SetNeedsDraw ();
    }

    /// <summary>Deletes the character to the left.</summary>
    /// <param name="usePreTextChangedCursorPos">
    ///     If set to <see langword="true">true</see> use the cursor position cached ;
    ///     otherwise use <see cref="CursorPosition"/>. use .
    /// </param>
    public virtual void DeleteCharLeft (bool usePreTextChangedCursorPos)
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Add (
                          new () { Cell.ToCells (_text) },
                          new (_cursorPosition, 0)
                         );

        if (SelectedLength == 0)
        {
            if (_cursorPosition == 0)
            {
                return;
            }

            if (!usePreTextChangedCursorPos)
            {
                _preTextChangedCursorPos = _cursorPosition;
            }

            _cursorPosition--;

            if (_preTextChangedCursorPos < _text.Count)
            {
                SetText (
                         _text.GetRange (0, _preTextChangedCursorPos - 1)
                              .Concat (
                                       _text.GetRange (
                                                       _preTextChangedCursorPos,
                                                       _text.Count - _preTextChangedCursorPos
                                                      )
                                      )
                        );
            }
            else
            {
                SetText (_text.GetRange (0, _preTextChangedCursorPos - 1));
            }

            Adjust ();
        }
        else
        {
            List<Rune> newText = DeleteSelectedText ();
            Text = StringExtensions.ToString (newText);
            Adjust ();
        }
    }

    /// <summary>Deletes the character to the right.</summary>
    public virtual void DeleteCharRight ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Add (
                          new () { Cell.ToCells (_text) },
                          new (_cursorPosition, 0)
                         );

        if (SelectedLength == 0)
        {
            if (_text.Count == 0 || _text.Count == _cursorPosition)
            {
                return;
            }

            SetText (
                     _text.GetRange (0, _cursorPosition)
                          .Concat (_text.GetRange (_cursorPosition + 1, _text.Count - (_cursorPosition + 1)))
                    );
            Adjust ();
        }
        else
        {
            List<Rune> newText = DeleteSelectedText ();
            Text = StringExtensions.ToString (newText);
            Adjust ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role == VisualRole.Normal)
        {
            currentAttribute = GetAttributeForRole (VisualRole.Focus);

            return true;
        }

        return base.OnGettingAttributeForRole (role, ref currentAttribute);
    }

    /// <summary>
    ///     Inserts the given <paramref name="toAdd"/> text at the current cursor position exactly as if the user had just
    ///     typed it
    /// </summary>
    /// <param name="toAdd">Text to add</param>
    /// <param name="useOldCursorPos">Use the previous cursor position.</param>
    public void InsertText (string toAdd, bool useOldCursorPos = true)
    {
        foreach (Rune rune in toAdd.EnumerateRunes ())
        {
            // All rune can be mapped to a Key and no exception will throw here because
            // EnumerateRunes will replace a surrogate char with the Rune.ReplacementChar
            Key key = rune.Value;
            InsertText (key, useOldCursorPos);
        }
    }

    /// <summary>Deletes word backwards.</summary>
    public virtual void KillWordBackwards ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordBackward (_cursorPosition, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return;
        }

        if (newPos.Value.col != -1)
        {
            SetText (
                     _text.GetRange (0, newPos.Value.col)
                          .Concat (_text.GetRange (_cursorPosition, _text.Count - _cursorPosition))
                    );
            _cursorPosition = newPos.Value.col;
        }

        Adjust ();
    }

    /// <summary>Deletes word forwards.</summary>
    public virtual void KillWordForwards ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordForward (_cursorPosition, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return;
        }

        if (newPos.Value.col != -1)
        {
            SetText (
                     _text.GetRange (0, _cursorPosition)
                          .Concat (_text.GetRange (newPos.Value.col, _text.Count - newPos.Value.col))
                    );
        }

        Adjust ();
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs ev)
    {
        if (ev is { IsPressed: false, IsReleased: false }
            && !ev.Flags.HasFlag (MouseFlags.ReportMousePosition)
            && !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
            && !ev.Flags.HasFlag (MouseFlags.Button1TripleClicked)
            && !ev.Flags.HasFlag (ContextMenu!.MouseFlags))
        {
            return false;
        }

        if (!CanFocus)
        {
            return true;
        }

        if (!HasFocus && ev.Flags != MouseFlags.ReportMousePosition)
        {
            SetFocus ();
        }

        // Give autocomplete first opportunity to respond to mouse clicks
        if (SelectedLength == 0 && Autocomplete.OnMouseEvent (ev, true))
        {
            return true;
        }

        if (ev.Flags == MouseFlags.Button1Pressed)
        {
            EnsureHasFocus ();
            PositionCursor (ev);

            if (_isButtonReleased)
            {
                ClearAllSelection ();
            }

            _isButtonReleased = true;
            _isButtonPressed = true;
        }
        else if (ev.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && _isButtonPressed)
        {
            int x = PositionCursor (ev);
            _isButtonReleased = false;
            PrepareSelection (x);

            if (Application.MouseGrabHandler.MouseGrabView is null)
            {
                Application.MouseGrabHandler.GrabMouse (this);
            }
        }
        else if (ev.Flags == MouseFlags.Button1Released)
        {
            _isButtonReleased = true;
            _isButtonPressed = false;
            Application.MouseGrabHandler.UngrabMouse ();
        }
        else if (ev.Flags == MouseFlags.Button1DoubleClicked)
        {
            EnsureHasFocus ();
            int x = PositionCursor (ev);
            (int startCol, int col, int row)? newPos = GetModel ().ProcessDoubleClickSelection (x, x, 0, UseSameRuneTypeForWords, SelectWordOnlyOnDoubleClick);

            if (newPos is null)
            {
                return true;
            }

            SelectedStart = newPos.Value.startCol;
            CursorPosition = newPos.Value.col;
        }
        else if (ev.Flags == MouseFlags.Button1TripleClicked)
        {
            EnsureHasFocus ();
            PositionCursor (0);
            ClearAllSelection ();
            PrepareSelection (0, _text.Count);
        }
        else if (ev.Flags == ContextMenu!.MouseFlags)
        {
            PositionCursor (ev);
            ShowContextMenu (false);
        }

        //SetNeedsDraw ();

        return true;

        void EnsureHasFocus ()
        {
            if (!HasFocus)
            {
                SetFocus ();
            }
        }
    }

    /// <summary>Moves cursor to the end of the typed text.</summary>
    public void MoveEnd ()
    {
        ClearAllSelection ();
        _cursorPosition = _text.Count;
        Adjust ();
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        _isDrawing = true;

        // Cache attributes as GetAttributeForRole might raise events
        Attribute selectedAttribute = new Attribute (GetAttributeForRole (VisualRole.Active));
        Attribute readonlyAttribute = GetAttributeForRole (VisualRole.ReadOnly);
        Attribute normalAttribute = GetAttributeForRole (VisualRole.Editable);

        SetSelectedStartSelectedLength ();

        SetAttribute (GetAttributeForRole (VisualRole.Normal));
        Move (0, 0);

        int p = ScrollOffset;
        var col = 0;
        int width = Viewport.Width + OffSetBackground ();
        int tcount = _text.Count;

        for (int idx = p; idx < tcount; idx++)
        {
            Rune rune = _text [idx];
            int cols = rune.GetColumns ();

            if (!Enabled)
            {
                // Disabled
                SetAttributeForRole (VisualRole.Disabled);
            } 
            else if (idx == _cursorPosition && HasFocus && !Used && SelectedLength == 0 && !ReadOnly)
            {
                // Selected text
                SetAttribute (selectedAttribute);
            }
            else if (ReadOnly)
            {
                SetAttribute (
                              idx >= _start && SelectedLength > 0 && idx < _start + SelectedLength
                                  ? selectedAttribute
                                  : readonlyAttribute
                             );
            }
            else if (!HasFocus && Enabled)
            {
                // Normal text
                SetAttribute (normalAttribute);
            }
            else
            {
                SetAttribute (
                              idx >= _start && SelectedLength > 0 && idx < _start + SelectedLength
                                  ? selectedAttribute
                                  : normalAttribute
                             );
            }

            if (col + cols <= width)
            {
                Driver?.AddRune (Secret ? Glyphs.Dot : rune);
            }

            if (!TextModel.SetCol (ref col, width, cols))
            {
                break;
            }

            if (idx + 1 < tcount && col + _text [idx + 1].GetColumns () > width)
            {
                break;
            }
        }

        SetAttribute (normalAttribute);

        // Fill rest of line with spaces
        for (int i = col; i < width; i++)
        {
            Driver?.AddRune ((Rune)' ');
        }

        PositionCursor ();

        RenderCaption ();

        _isDrawing = false;

        return true;
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View previousFocusedView, View view)
    {
        if (Application.MouseGrabHandler.MouseGrabView is { } && Application.MouseGrabHandler.MouseGrabView == this)
        {
            Application.MouseGrabHandler.UngrabMouse ();
        }

        //if (SelectedLength != 0 && !(Application.MouseGrabHandler.MouseGrabView is MenuBar))
        //	ClearAllSelection ();
    }

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
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
        // Remember the cursor position because the new calculated cursor position is needed
        // to be set BEFORE the TextChanged event is triggered.
        // Needed for the Elmish Wrapper issue https://github.com/DieselMeister/Terminal.Gui.Elmish/issues/2
        _preTextChangedCursorPos = _cursorPosition;

        // Ignore other control characters.
        if (!a.IsKeyCodeAtoZ && (a.KeyCode < KeyCode.Space || a.KeyCode > KeyCode.CharMask))
        {
            return false;
        }

        if (ReadOnly)
        {
            return true;
        }

        InsertText (a, true);

        return true;
    }

    /// <summary>Raises the <see cref="TextChanging"/> event, enabling canceling the change or adjusting the text.</summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> if the event was cancelled or the text was adjusted by the event.</returns>
    public bool RaiseTextChanging (ResultEventArgs<string> args)
    {
        // TODO: CWP: Add an OnTextChanging protected virtual method that can be overridden to handle text changing events.

        TextChanging?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>Raised before <see cref="Text"/> changes. The change can be canceled the text adjusted.</summary>
    public event EventHandler<ResultEventArgs<string>> TextChanging;

    /// <summary>Paste the selected text from the clipboard.</summary>
    public virtual void Paste ()
    {
        if (ReadOnly)
        {
            return;
        }

        string cbTxt = Clipboard.Contents.Split ("\n") [0] ?? "";

        if (string.IsNullOrEmpty (cbTxt))
        {
            return;
        }

        SetSelectedStartSelectedLength ();
        int selStart = _start == -1 ? CursorPosition : _start;

        Text = StringExtensions.ToString (_text.GetRange (0, selStart))
               + cbTxt
               + StringExtensions.ToString (
                                            _text.GetRange (
                                                            selStart + SelectedLength,
                                                            _text.Count - (selStart + SelectedLength)
                                                           )
                                           );

        _cursorPosition = Math.Min (selStart + cbTxt.GetRuneCount (), _text.Count);
        ClearAllSelection ();
        SetNeedsDraw ();
        Adjust ();
    }

    /// <summary>Sets the cursor position.</summary>
    public override Point? PositionCursor ()
    {
        ProcessAutocomplete ();

        var col = 0;

        for (int idx = ScrollOffset < 0 ? 0 : ScrollOffset; idx < _text.Count; idx++)
        {
            if (idx == _cursorPosition)
            {
                break;
            }

            int cols = _text [idx].GetColumns ();
            TextModel.SetCol (ref col, Viewport.Width - 1, cols);
        }

        int pos = col + Math.Min (Viewport.X, 0);
        Move (pos, 0);

        return new Point (pos, 0);
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

    /// <summary>Selects all text.</summary>
    public void SelectAll ()
    {
        if (_text.Count == 0)
        {
            return;
        }

        _selectedStart = 0;
        MoveEndExtend ();
        SetNeedsDraw ();
    }

    ///// <summary>
    /////     Changed event, raised when the text has changed.
    /////     <remarks>
    /////         This event is raised when the <see cref="Text"/> changes. The passed <see cref="EventArgs"/> is a
    /////         <see cref="string"/> containing the old value.
    /////     </remarks>
    ///// </summary>
    //public event EventHandler<StateEventArgs<string>> TextChanged;


    /// <summary>Undoes the latest changes.</summary>
    public void Undo ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Undo ();
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the current cursor position is at the end of the <see cref="Text"/>. This
    ///     includes when it is empty.
    /// </summary>
    /// <returns></returns>
    internal bool CursorIsAtEnd () { return CursorPosition == Text.Length; }

    /// <summary>Returns <see langword="true"/> if the current cursor position is at the start of the <see cref="TextField"/>.</summary>
    /// <returns></returns>
    internal bool CursorIsAtStart () { return CursorPosition <= 0; }

    private void Adjust ()
    {
        if (SuperView is null)
        {
            return;
        }

        // TODO: This is a lame prototype proving it should be easy for TextField to 
        // TODO: support Width = Dim.Auto (DimAutoStyle: Content).
        //SetContentSize(new (TextModel.DisplaySize (_text).size, 1));

        int offB = OffSetBackground ();
        bool need = NeedsDraw || !Used;

        if (_cursorPosition < ScrollOffset)
        {
            ScrollOffset = _cursorPosition;
            need = true;
        }
        else if (Viewport.Width > 0
                 && (ScrollOffset + _cursorPosition - (Viewport.Width + offB) == 0
                     || TextModel.DisplaySize (_text, ScrollOffset, _cursorPosition).size >= Viewport.Width + offB))
        {
            ScrollOffset = Math.Max (
                                     TextModel.CalculateLeftColumn (
                                                                    _text,
                                                                    ScrollOffset,
                                                                    _cursorPosition,
                                                                    Viewport.Width + offB
                                                                   ),
                                     0
                                    );
            need = true;
        }

        if (need)
        {
            SetNeedsDraw ();
        }
        else
        {
            PositionCursor ();
        }
    }

    private void CreateContextMenu ()
    {
        DisposeContextMenu ();

        PopoverMenu menu = new (
                                new List<MenuItemv2>
                                {
                                    new (this, Command.SelectAll, Strings.ctxSelectAll),
                                    new (this, Command.DeleteAll, Strings.ctxDeleteAll),
                                    new (this, Command.Copy, Strings.ctxCopy),
                                    new (this, Command.Cut, Strings.ctxCut),
                                    new (this, Command.Paste, Strings.ctxPaste),
                                    new (this, Command.Undo, Strings.ctxUndo),
                                    new (this, Command.Redo, Strings.ctxRedo)
                                });

        HotKeyBindings.Remove (menu.Key);
        HotKeyBindings.Add (menu.Key, Command.Context);
        menu.KeyChanged += ContextMenu_KeyChanged;

        ContextMenu = menu;
    }

    private void ContextMenu_KeyChanged (object sender, KeyChangedEventArgs e) { KeyBindings.Replace (e.OldKey.KeyCode, e.NewKey.KeyCode); }

    private List<Rune> DeleteSelectedText ()
    {
        SetSelectedStartSelectedLength ();
        int selStart = SelectedStart > -1 ? _start : _cursorPosition;

        string newText = StringExtensions.ToString (_text.GetRange (0, selStart))
                         + StringExtensions.ToString (
                                                      _text.GetRange (
                                                                      selStart + SelectedLength,
                                                                      _text.Count - (selStart + SelectedLength)
                                                                     )
                                                     );

        ClearAllSelection ();
        _cursorPosition = selStart >= newText.GetRuneCount () ? newText.GetRuneCount () : selStart;

        return newText.ToRuneList ();
    }

    private void GenerateSuggestions ()
    {
        List<Cell> currentLine = Cell.ToCellList (Text);
        int cursorPosition = Math.Min (CursorPosition, currentLine.Count);

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

    private TextModel GetModel ()
    {
        var model = new TextModel ();
        model.LoadString (Text);

        return model;
    }

    private void HistoryText_ChangeText (object sender, HistoryTextItemEventArgs obj)
    {
        if (obj is null)
        {
            return;
        }

        Text = Cell.ToString (obj?.Lines [obj.CursorPosition.Y]);
        CursorPosition = obj.CursorPosition.X;
        Adjust ();
    }

    private void InsertText (Key a, bool usePreTextChangedCursorPos)
    {
        _historyText.Add (
                          new () { Cell.ToCells (_text) },
                          new (_cursorPosition, 0)
                         );

        List<Rune> newText = _text;

        if (SelectedLength > 0)
        {
            newText = DeleteSelectedText ();
            _preTextChangedCursorPos = _cursorPosition;
        }

        if (!usePreTextChangedCursorPos)
        {
            _preTextChangedCursorPos = _cursorPosition;
        }

        StringRuneEnumerator kbstr = a.AsRune.ToString ().EnumerateRunes ();

        if (Used)
        {
            _cursorPosition++;

            if (_cursorPosition == newText.Count + 1)
            {
                SetText (newText.Concat (kbstr).ToList ());
            }
            else
            {
                if (_preTextChangedCursorPos > newText.Count)
                {
                    _preTextChangedCursorPos = newText.Count;
                }

                SetText (
                         newText.GetRange (0, _preTextChangedCursorPos)
                                .Concat (kbstr)
                                .Concat (
                                         newText.GetRange (
                                                           _preTextChangedCursorPos,
                                                           Math.Min (
                                                                     newText.Count - _preTextChangedCursorPos,
                                                                     newText.Count
                                                                    )
                                                          )
                                        )
                        );
            }
        }
        else
        {
            SetText (
                     newText.GetRange (0, _preTextChangedCursorPos)
                            .Concat (kbstr)
                            .Concat (
                                     newText.GetRange (
                                                       Math.Min (_preTextChangedCursorPos + 1, newText.Count),
                                                       Math.Max (newText.Count - _preTextChangedCursorPos - 1, 0)
                                                      )
                                    )
                    );
            _cursorPosition++;
        }

        Adjust ();
    }

    private void KillToEnd ()
    {
        if (ReadOnly)
        {
            return;
        }

        ClearAllSelection ();

        if (_cursorPosition >= _text.Count)
        {
            return;
        }

        SetClipboard (_text.GetRange (_cursorPosition, _text.Count - _cursorPosition));
        SetText (_text.GetRange (0, _cursorPosition));
        Adjust ();
    }

    private void KillToStart ()
    {
        if (ReadOnly)
        {
            return;
        }

        ClearAllSelection ();

        if (_cursorPosition == 0)
        {
            return;
        }

        SetClipboard (_text.GetRange (0, _cursorPosition));
        SetText (_text.GetRange (_cursorPosition, _text.Count - _cursorPosition));
        _cursorPosition = 0;
        Adjust ();
    }

    private void MoveEndExtend ()
    {
        if (_cursorPosition <= _text.Count)
        {
            int x = _cursorPosition;
            _cursorPosition = _text.Count;
            PrepareSelection (x, _cursorPosition - x);
        }
    }

    private void MoveHome ()
    {
        ClearAllSelection ();
        _cursorPosition = 0;
        Adjust ();
    }

    private void MoveHomeExtend ()
    {
        if (_cursorPosition > 0)
        {
            int x = _cursorPosition;
            _cursorPosition = 0;
            PrepareSelection (x, _cursorPosition - x);
        }
    }

    /// <summary>
    ///     Moves the cursor +/- the given <paramref name="distance"/>, clearing
    ///     any selection and returning true if any meaningful changes were made.
    /// </summary>
    /// <param name="distance">
    ///     Distance to move the cursor, will be clamped to
    ///     text length. Positive for right, Negative for left.
    /// </param>
    /// <returns></returns>
    private bool Move (int distance)
    {
        int oldCursorPosition = _cursorPosition;
        bool hadSelection = _selectedText != null && _selectedText.Length > 0;

        _cursorPosition = Math.Min (_text.Count, Math.Max (0, _cursorPosition + distance));
        ClearAllSelection ();
        Adjust ();

        return _cursorPosition != oldCursorPosition || hadSelection;
    }

    private bool MoveLeft () { return Move (-1); }

    private void MoveLeftExtend ()
    {
        if (_cursorPosition > 0)
        {
            PrepareSelection (_cursorPosition--, -1);
        }
    }

    private bool MoveRight () { return Move (1); }

    private void MoveRightExtend ()
    {
        if (_cursorPosition < _text.Count)
        {
            PrepareSelection (_cursorPosition++, 1);
        }
    }

    private void MoveWordLeft ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordBackward (_cursorPosition, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return;
        }

        if (newPos.Value.col != -1)
        {
            _cursorPosition = newPos.Value.col;
        }

        Adjust ();
    }

    private void MoveWordLeftExtend ()
    {
        if (_cursorPosition > 0)
        {
            int x = Math.Min (
                              _start > -1 && _start > _cursorPosition ? _start : _cursorPosition,
                              _text.Count
                             );

            if (x > 0)
            {
                (int col, int row)? newPos = GetModel ().WordBackward (x, 0, UseSameRuneTypeForWords);

                if (newPos is null)
                {
                    return;
                }

                if (newPos.Value.col != -1)
                {
                    _cursorPosition = newPos.Value.col;
                }

                PrepareSelection (x, newPos.Value.col - x);
            }
        }
    }

    private void MoveWordRight ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordForward (_cursorPosition, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return;
        }

        if (newPos.Value.col != -1)
        {
            _cursorPosition = newPos.Value.col;
        }

        Adjust ();
    }

    private void MoveWordRightExtend ()
    {
        if (_cursorPosition < _text.Count)
        {
            int x = _start > -1 && _start > _cursorPosition ? _start : _cursorPosition;
            (int col, int row)? newPos = GetModel ().WordForward (x, 0, UseSameRuneTypeForWords);

            if (newPos is null)
            {
                return;
            }

            if (newPos.Value.col != -1)
            {
                _cursorPosition = newPos.Value.col;
            }

            PrepareSelection (x, newPos.Value.col - x);
        }
    }

    // BUGBUG: This assumes Frame == Viewport. It's also not clear what the intention is. For now, changed to always return 0.
    private int OffSetBackground ()
    {
        var offB = 0;

        if (SuperView?.Frame.Right - Frame.Right < 0)
        {
            offB = SuperView.Frame.Right - Frame.Right - 1;
        }

        return 0; //offB;
    }

    private int PositionCursor (MouseEventArgs ev) { return PositionCursor (TextModel.GetColFromX (_text, ScrollOffset, ev.Position.X), false); }

    private int PositionCursor (int x, bool getX = true)
    {
        int pX = x;

        if (getX)
        {
            pX = TextModel.GetColFromX (_text, ScrollOffset, x);
        }

        if (ScrollOffset + pX > _text.Count)
        {
            _cursorPosition = _text.Count;
        }
        else if (ScrollOffset + pX < ScrollOffset)
        {
            _cursorPosition = 0;
        }
        else
        {
            _cursorPosition = ScrollOffset + pX;
        }

        return _cursorPosition;
    }

    private void PrepareSelection (int x, int direction = 0)
    {
        x = x + ScrollOffset < -1 ? 0 : x;

        _selectedStart = _selectedStart == -1 && _text.Count > 0 && x >= 0 && x <= _text.Count
                             ? x
                             : _selectedStart;

        if (_selectedStart > -1)
        {
            SelectedLength = Math.Abs (
                                       x + direction <= _text.Count
                                           ? x + direction - _selectedStart
                                           : _text.Count - _selectedStart
                                      );
            SetSelectedStartSelectedLength ();

            if (_start > -1 && SelectedLength > 0)
            {
                _selectedText = SelectedLength > 0
                                    ? StringExtensions.ToString (
                                                                 _text.GetRange (
                                                                                 _start < 0 ? 0 : _start,
                                                                                 SelectedLength > _text.Count
                                                                                     ? _text.Count
                                                                                     : SelectedLength
                                                                                )
                                                                )
                                    : "";

                if (ScrollOffset > _start)
                {
                    ScrollOffset = _start;
                }
            }
            else if (_start > -1 && SelectedLength == 0)
            {
                _selectedText = null;
            }

            SetNeedsDraw ();
        }
        else if (SelectedLength > 0 || _selectedText is { })
        {
            ClearAllSelection ();
        }

        Adjust ();
    }

    private void ProcessAutocomplete ()
    {
        if (_isDrawing)
        {
            return;
        }

        if (SelectedLength > 0)
        {
            return;
        }

        GenerateSuggestions ();

        DrawAutocomplete ();
    }

    private void DrawAutocomplete ()
    {
        if (SelectedLength > 0)
        {
            return;
        }

        if (Autocomplete?.Context == null)
        {
            return;
        }

        var renderAt = new Point (
                                  Autocomplete.Context.CursorPosition,
                                  0
                                 );

        Autocomplete.RenderOverlay (renderAt);
    }

    private void RenderCaption ()
    {
        if (HasFocus
            || Caption == null
            || Caption.Length == 0
            || Text?.Length > 0)
        {
            return;
        }

        var color = new Attribute (CaptionColor, GetAttributeForRole (VisualRole.Editable).Background, GetAttributeForRole (VisualRole.Editable).Style);
        SetAttribute (color);

        Move (0, 0);
        string render = Caption;

        if (render.GetColumns () > Viewport.Width)
        {
            render = render [..Viewport.Width];
        }

        Driver?.AddStr (render);
    }

    private void SetClipboard (IEnumerable<Rune> text)
    {
        if (!Secret)
        {
            Clipboard.Contents = StringExtensions.ToString (text.ToList ());
        }
    }

    private void SetOverwrite (bool overwrite)
    {
        Used = overwrite;
        SetNeedsDraw ();
    }

    private void SetSelectedStartSelectedLength ()
    {
        if (SelectedStart > -1 && _cursorPosition < SelectedStart)
        {
            _start = _cursorPosition;
        }
        else
        {
            _start = SelectedStart;
        }
    }

    private void SetText (List<Rune> newText) { Text = StringExtensions.ToString (newText); }
    private void SetText (IEnumerable<Rune> newText) { SetText (newText.ToList ()); }

    private void ShowContextMenu (bool keyboard)
    {
        if (!Equals (_currentCulture, Thread.CurrentThread.CurrentUICulture))
        {
            _currentCulture = Thread.CurrentThread.CurrentUICulture;

            if (ContextMenu is { })
            {
                CreateContextMenu ();
            }
        }

        if (keyboard)
        {
            ContextMenu?.MakeVisible (ViewportToScreen (new Point (_cursorPosition - ScrollOffset, 1)));
        }
        else
        {
            ContextMenu?.MakeVisible ();
        }
    }

    private void TextField_SuperViewChanged (object sender, SuperViewChangedEventArgs e)
    {
        if (e.SuperView is { })
        {
            if (Autocomplete.HostControl is null)
            {
                Autocomplete.HostControl = this;
                Autocomplete.PopupInsideContainer = false;
            }
        }
        else
        {
            Autocomplete.HostControl = null;
        }
    }

    private void TextField_Initialized (object sender, EventArgs e)
    {
        _cursorPosition = Text.GetRuneCount ();

        if (Viewport.Width > 0)
        {
            ScrollOffset = _cursorPosition > Viewport.Width + 1 ? _cursorPosition - Viewport.Width + 1 : 0;
        }

        if (Autocomplete.HostControl is null)
        {
            Autocomplete.HostControl = this;
            Autocomplete.PopupInsideContainer = false;
        }
    }

    private void DisposeContextMenu ()
    {
        if (ContextMenu is { })
        {
            ContextMenu.Visible = false;
            ContextMenu.KeyChanged -= ContextMenu_KeyChanged;
            ContextMenu.Dispose ();
            ContextMenu = null;
        }
    }

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        Text = "This is a test.";
        Caption = "Caption";

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            DisposeContextMenu ();
        }

        base.Dispose (disposing);
    }
}

/// <summary>
///     Renders an overlay on another view at a given point that allows selecting from a range of 'autocomplete'
///     options. An implementation on a TextField.
/// </summary>
public class TextFieldAutocomplete : PopupAutocomplete
{
    /// <inheritdoc/>
    protected override void DeleteTextBackwards () { ((TextField)HostControl).DeleteCharLeft (false); }

    /// <inheritdoc/>
    protected override void InsertText (string accepted) { ((TextField)HostControl).InsertText (accepted, false); }

    /// <inheritdoc/>
    protected override void SetCursorPosition (int column) { ((TextField)HostControl).CursorPosition = column; }
}
