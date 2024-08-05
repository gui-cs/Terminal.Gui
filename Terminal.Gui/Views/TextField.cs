using System.Data;
using System.Globalization;
using Terminal.Gui.Resources;

namespace Terminal.Gui;

/// <summary>Single-line text entry <see cref="View"/></summary>
/// <remarks>The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.</remarks>
public class TextField : View
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
        _historyText = new HistoryText ();
        _isButtonReleased = true;
        _selectedStart = -1;
        _text = new List<Rune> ();
        CaptionColor = new Color (Color.DarkGray);
        ReadOnly = false;
        Autocomplete = new TextFieldAutocomplete ();
        Height = Dim.Auto (DimAutoStyle.Text, minimumContentDim: 1);

        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;
        Used = true;
        WantMousePositionReports = true;

        _historyText.ChangeText += HistoryText_ChangeText;

        Initialized += TextField_Initialized;

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
                    Command.LeftHomeExtend,
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
                    Command.LeftHome,
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

        AddCommand (Command.Left,  () => MoveLeft ());

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
                    Command.ShowContextMenu,
                    () =>
                    {
                        ShowContextMenu ();

                        return true;
                    }
                   );

        // By Default pressing ENTER should be ignored (OnAccept will return false or null). Only cancel if the
        // event was fired and set Cancel = true.
        AddCommand (Command.Accept, () => OnAccept () == false);

        // Default keybindings for this view
        // We follow this as closely as possible: https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts
        KeyBindings.Add (Key.Delete, Command.DeleteCharRight);
        KeyBindings.Add (Key.D.WithCtrl, Command.DeleteCharRight);

        KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);

        KeyBindings.Add (Key.Home.WithShift, Command.LeftHomeExtend);
        KeyBindings.Add (Key.Home.WithShift.WithCtrl, Command.LeftHomeExtend);
        KeyBindings.Add (Key.A.WithShift.WithCtrl, Command.LeftHomeExtend);

        KeyBindings.Add (Key.End.WithShift, Command.RightEndExtend);
        KeyBindings.Add (Key.End.WithShift.WithCtrl, Command.RightEndExtend);
        KeyBindings.Add (Key.E.WithShift.WithCtrl, Command.RightEndExtend);

        KeyBindings.Add (Key.Home, Command.LeftHome);
        KeyBindings.Add (Key.Home.WithCtrl, Command.LeftHome);
        KeyBindings.Add (Key.A.WithCtrl, Command.LeftHome);

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
        KeyBindings.Add (Key.T.WithCtrl, Command.SelectAll);

        KeyBindings.Add (Key.R.WithCtrl, Command.DeleteAll);
        KeyBindings.Add (Key.D.WithCtrl.WithShift, Command.DeleteAll);

        _currentCulture = Thread.CurrentThread.CurrentUICulture;

        ContextMenu = new ContextMenu { Host = this, MenuItems = BuildContextMenuBarItem () };
        ContextMenu.KeyChanged += ContextMenu_KeyChanged;

        KeyBindings.Add (ContextMenu.Key, KeyBindingScope.HotKey, Command.ShowContextMenu);
        KeyBindings.Add (Key.Enter, Command.Accept);
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

    /// <summary>Get the <see cref="ContextMenu"/> for this view.</summary>
    public ContextMenu ContextMenu { get; }

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
    public bool IsDirty => _historyText.IsDirty (Text);

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
            CancelEventArgs<string> args = new (ref oldText, ref newText);
            OnTextChanging (args);

            if (args.Cancel)
            {
                if (_cursorPosition > _text.Count)
                {
                    _cursorPosition = _text.Count;
                }

                return;
            }

            ClearAllSelection ();

            // Note we use NewValue here; TextChanging subscribers may have changed it
            _text = args.NewValue.EnumerateRunes ().ToList ();

            if (!Secret && !_historyText.IsFromHistory)
            {
                _historyText.Add (
                                  new List<List<RuneCell>> { TextModel.ToRuneCellList (oldText) },
                                  new Point (_cursorPosition, 0)
                                 );

                _historyText.Add (
                                  new List<List<RuneCell>> { TextModel.ToRuneCells (_text) },
                                  new Point (_cursorPosition, 0),
                                  HistoryText.LineStatus.Replaced
                                 );
            }

            OnTextChanged ();

            ProcessAutocomplete ();

            if (_cursorPosition > _text.Count)
            {
                _cursorPosition = Math.Max (TextModel.DisplaySize (_text, 0).size - 1, 0);
            }

            Adjust ();
            SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Tracks whether the text field should be considered "used", that is, that the user has moved in the entry, so
    ///     new input should be appended at the cursor position, rather than clearing the entry
    /// </summary>
    public bool Used { get; set; }

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
        SetNeedsDisplay ();
    }

    /// <summary>Allows clearing the <see cref="HistoryText.HistoryTextItem"/> items updating the original text.</summary>
    public void ClearHistoryChanges () { _historyText.Clear (Text); }

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
        SetNeedsDisplay ();
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
                          new List<List<RuneCell>> { TextModel.ToRuneCells (_text) },
                          new Point (_cursorPosition, 0)
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
                          new List<List<RuneCell>> { TextModel.ToRuneCells (_text) },
                          new Point (_cursorPosition, 0)
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
    public override Attribute GetNormalColor ()
    {
        ColorScheme cs = ColorScheme;

        if (ColorScheme is null)
        {
            cs = new ColorScheme ();
        }

        return Enabled ? cs.Focus : cs.Disabled;
    }

    /// <summary>
    ///     Inserts the given <paramref name="toAdd"/> text at the current cursor position exactly as if the user had just
    ///     typed it
    /// </summary>
    /// <param name="toAdd">Text to add</param>
    /// <param name="useOldCursorPos">Use the previous cursor position.</param>
    public void InsertText (string toAdd, bool useOldCursorPos = true)
    {
        foreach (char ch in toAdd)
        {
            Key key;

            try
            {
                key = ch;
            }
            catch (Exception)
            {
                throw new ArgumentException (
                                             $"Cannot insert character '{ch}' because it does not map to a Key"
                                            );
            }

            InsertText (key, useOldCursorPos);
        }
    }

    /// <summary>Deletes word backwards.</summary>
    public virtual void KillWordBackwards ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordBackward (_cursorPosition, 0);

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
        (int col, int row)? newPos = GetModel ().WordForward (_cursorPosition, 0);

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
    protected internal override bool OnMouseEvent (MouseEvent ev)
    {
        if (!ev.Flags.HasFlag (MouseFlags.Button1Pressed)
            && !ev.Flags.HasFlag (MouseFlags.ReportMousePosition)
            && !ev.Flags.HasFlag (MouseFlags.Button1Released)
            && !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
            && !ev.Flags.HasFlag (MouseFlags.Button1TripleClicked)
            && !ev.Flags.HasFlag (ContextMenu.MouseFlags))
        {
            return base.OnMouseEvent (ev);
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

            if (Application.MouseGrabView is null)
            {
                Application.GrabMouse (this);
            }
        }
        else if (ev.Flags == MouseFlags.Button1Released)
        {
            _isButtonReleased = true;
            _isButtonPressed = false;
            Application.UngrabMouse ();
        }
        else if (ev.Flags == MouseFlags.Button1DoubleClicked)
        {
            EnsureHasFocus ();
            int x = PositionCursor (ev);
            int sbw = x;

            if (x == _text.Count
                || (x > 0 && (char)_text [x - 1].Value != ' ')
                || (x > 0 && (char)_text [x].Value == ' '))
            {
                (int col, int row)? newPosBw = GetModel ().WordBackward (x, 0);

                if (newPosBw is null)
                {
                    return true;
                }

                sbw = newPosBw.Value.col;
            }

            if (sbw != -1)
            {
                x = sbw;
                PositionCursor (x);
            }

            (int col, int row)? newPosFw = GetModel ().WordForward (x, 0);

            if (newPosFw is null)
            {
                return true;
            }

            ClearAllSelection ();

            if (newPosFw.Value.col != -1 && sbw != -1)
            {
                _cursorPosition = newPosFw.Value.col;
            }

            PrepareSelection (sbw, newPosFw.Value.col - sbw);
        }
        else if (ev.Flags == MouseFlags.Button1TripleClicked)
        {
            EnsureHasFocus ();
            PositionCursor (0);
            ClearAllSelection ();
            PrepareSelection (0, _text.Count);
        }
        else if (ev.Flags == ContextMenu.MouseFlags)
        {
            ShowContextMenu ();
        }

        //SetNeedsDisplay ();

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
    public override void OnDrawContent (Rectangle viewport)
    {
        _isDrawing = true;

        var selColor = new Attribute (GetFocusColor ().Background, GetFocusColor ().Foreground);
        SetSelectedStartSelectedLength ();

        Driver?.SetAttribute (GetNormalColor ());
        Move (0, 0);

        int p = ScrollOffset;
        var col = 0;
        int width = Frame.Width + OffSetBackground ();
        int tcount = _text.Count;
        Attribute roc = GetReadOnlyColor ();

        for (int idx = p; idx < tcount; idx++)
        {
            Rune rune = _text [idx];
            int cols = rune.GetColumns ();

            if (idx == _cursorPosition && HasFocus && !Used && SelectedLength == 0 && !ReadOnly)
            {
                Driver?.SetAttribute (selColor);
            }
            else if (ReadOnly)
            {
                Driver?.SetAttribute (
                                      idx >= _start && SelectedLength > 0 && idx < _start + SelectedLength
                                          ? selColor
                                          : roc
                                     );
            }
            else if (!HasFocus && Enabled)
            {
                Driver?.SetAttribute (GetFocusColor ());
            }
            else if (!Enabled)
            {
                Driver?.SetAttribute (roc);
            }
            else
            {
                Driver?.SetAttribute (
                                      idx >= _start && SelectedLength > 0 && idx < _start + SelectedLength
                                          ? selColor
                                          : ColorScheme.Focus
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

        Driver.SetAttribute (GetFocusColor ());

        for (int i = col; i < width; i++)
        {
            Driver.AddRune ((Rune)' ');
        }

        PositionCursor ();

        RenderCaption ();

        ProcessAutocomplete ();

        _isDrawing = false;
    }

    /// <inheritdoc/>
    public override bool? OnInvokingKeyBindings (Key a, KeyBindingScope scope)
    {
        // Give autocomplete first opportunity to respond to key presses
        if (SelectedLength == 0 && Autocomplete.Suggestions.Count > 0 && Autocomplete.ProcessKey (a))
        {
            return true;
        }

        return base.OnInvokingKeyBindings (a, scope);
    }

    /// <inheritdoc/>
    public override bool OnLeave (View view)
    {
        if (Application.MouseGrabView is { } && Application.MouseGrabView == this)
        {
            Application.UngrabMouse ();
        }

        //if (SelectedLength != 0 && !(Application.MouseGrabView is MenuBar))
        //	ClearAllSelection ();

        return base.OnLeave (view);
    }

    /// TODO: Flush out these docs
    /// <summary>
    ///     Processes key presses for the <see cref="TextField"/>.
    ///     <remarks>
    ///         The <see cref="TextField"/> control responds to the following keys:
    ///         <list type="table">
    ///             <listheader>
    ///                 <term>Keys</term> <description>Function</description>
    ///             </listheader>
    ///             <item>
    ///                 <term><see cref="Key.Delete"/>, <see cref="Key.Backspace"/></term>
    ///                 <description>Deletes the character before cursor.</description>
    ///             </item>
    ///         </list>
    ///     </remarks>
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public override bool OnProcessKeyDown (Key a)
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

    /// <summary>Virtual method that invoke the <see cref="TextChanging"/> event if it's defined.</summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> if the event was cancelled.</returns>
    public bool OnTextChanging (CancelEventArgs<string> args)
    {
        TextChanging?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>Paste the selected text from the clipboard.</summary>
    public virtual void Paste ()
    {
        if (ReadOnly || string.IsNullOrEmpty (Clipboard.Contents))
        {
            return;
        }

        SetSelectedStartSelectedLength ();
        int selStart = _start == -1 ? CursorPosition : _start;
        string cbTxt = Clipboard.Contents.Split ("\n") [0] ?? "";

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
        SetNeedsDisplay ();
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
            TextModel.SetCol (ref col, Frame.Width - 1, cols);
        }

        int pos = _cursorPosition - ScrollOffset + Math.Min (Frame.X, 0);
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
        SetNeedsDisplay ();
    }

    ///// <summary>
    /////     Changed event, raised when the text has changed.
    /////     <remarks>
    /////         This event is raised when the <see cref="Text"/> changes. The passed <see cref="EventArgs"/> is a
    /////         <see cref="string"/> containing the old value.
    /////     </remarks>
    ///// </summary>
    //public event EventHandler<StateEventArgs<string>> TextChanged;

    /// <summary>Changing event, raised before the <see cref="Text"/> changes and can be canceled or changing the new text.</summary>
    public event EventHandler<CancelEventArgs<string>> TextChanging;

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
        if (!IsAdded)
        {
            return;
        }

        // TODO: This is a lame prototype proving it should be easy for TextField to 
        // TODO: support Width = Dim.Auto (DimAutoStyle: Content).
        //SetContentSize(new (TextModel.DisplaySize (_text).size, 1));

        int offB = OffSetBackground ();
        bool need = NeedsDisplay || !Used;

        if (_cursorPosition < ScrollOffset)
        {
            ScrollOffset = _cursorPosition;
            need = true;
        }
        else if (Frame.Width > 0
                 && (ScrollOffset + _cursorPosition - (Frame.Width + offB) == 0
                     || TextModel.DisplaySize (_text, ScrollOffset, _cursorPosition).size >= Frame.Width + offB))
        {
            ScrollOffset = Math.Max (
                                     TextModel.CalculateLeftColumn (
                                                                    _text,
                                                                    ScrollOffset,
                                                                    _cursorPosition,
                                                                    Frame.Width + offB
                                                                   ),
                                     0
                                    );
            need = true;
        }

        if (need)
        {
            SetNeedsDisplay ();
        }
        else
        {
            PositionCursor ();
        }
    }

    private MenuBarItem BuildContextMenuBarItem ()
    {
        return new MenuBarItem (
                                new MenuItem []
                                {
                                    new (
                                         Strings.ctxSelectAll,
                                         "",
                                         () => SelectAll (),
                                         null,
                                         null,
                                         (KeyCode)KeyBindings.GetKeyFromCommands (Command.SelectAll)
                                        ),
                                    new (
                                         Strings.ctxDeleteAll,
                                         "",
                                         () => DeleteAll (),
                                         null,
                                         null,
                                         (KeyCode)KeyBindings.GetKeyFromCommands (Command.DeleteAll)
                                        ),
                                    new (
                                         Strings.ctxCopy,
                                         "",
                                         () => Copy (),
                                         null,
                                         null,
                                         (KeyCode)KeyBindings.GetKeyFromCommands (Command.Copy)
                                        ),
                                    new (
                                         Strings.ctxCut,
                                         "",
                                         () => Cut (),
                                         null,
                                         null,
                                         (KeyCode)KeyBindings.GetKeyFromCommands (Command.Cut)
                                        ),
                                    new (
                                         Strings.ctxPaste,
                                         "",
                                         () => Paste (),
                                         null,
                                         null,
                                         (KeyCode)KeyBindings.GetKeyFromCommands (Command.Paste)
                                        ),
                                    new (
                                         Strings.ctxUndo,
                                         "",
                                         () => Undo (),
                                         null,
                                         null,
                                         (KeyCode)KeyBindings.GetKeyFromCommands (Command.Undo)
                                        ),
                                    new (
                                         Strings.ctxRedo,
                                         "",
                                         () => Redo (),
                                         null,
                                         null,
                                         (KeyCode)KeyBindings.GetKeyFromCommands (Command.Redo)
                                        )
                                }
                               );
    }

    private void ContextMenu_KeyChanged (object sender, KeyChangedEventArgs e)
    {
        KeyBindings.ReplaceKey (e.OldKey.KeyCode, e.NewKey.KeyCode);
    }

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
        List<RuneCell> currentLine = TextModel.ToRuneCellList (Text);
        int cursorPosition = Math.Min (CursorPosition, currentLine.Count);

        Autocomplete.Context = new AutocompleteContext (
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

    private Attribute GetReadOnlyColor ()
    {
        ColorScheme cs = ColorScheme;

        if (ColorScheme is null)
        {
            cs = new ColorScheme ();
        }

        if (cs.Disabled.Foreground == cs.Focus.Background)
        {
            return new Attribute (cs.Focus.Foreground, cs.Focus.Background);
        }

        return new Attribute (cs.Disabled.Foreground, cs.Focus.Background);
    }

    private void HistoryText_ChangeText (object sender, HistoryText.HistoryTextItem obj)
    {
        if (obj is null)
        {
            return;
        }

        Text = TextModel.ToString (obj?.Lines [obj.CursorPosition.Y]);
        CursorPosition = obj.CursorPosition.X;
        Adjust ();
    }

    private void InsertText (Key a, bool usePreTextChangedCursorPos)
    {
        _historyText.Add (
                          new List<List<RuneCell>> { TextModel.ToRuneCells (_text) },
                          new Point (_cursorPosition, 0)
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

    private bool MoveLeft ()
    {

        if (_cursorPosition > 0)
        {
            ClearAllSelection ();
            _cursorPosition--;
            Adjust ();

            return true;
        }

        return false;
    }

    private void MoveLeftExtend ()
    {
        if (_cursorPosition > 0)
        {
            PrepareSelection (_cursorPosition--, -1);
        }
    }

    private bool MoveRight ()
    {
        if (_cursorPosition == _text.Count)
        {
            return false;
        }

        ClearAllSelection ();

        _cursorPosition++;
        Adjust ();

        return true;
    }

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
        (int col, int row)? newPos = GetModel ().WordBackward (_cursorPosition, 0);

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
                (int col, int row)? newPos = GetModel ().WordBackward (x, 0);

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
        (int col, int row)? newPos = GetModel ().WordForward (_cursorPosition, 0);

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
            (int col, int row)? newPos = GetModel ().WordForward (x, 0);

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

        return 0;//offB;
    }

    private int PositionCursor (MouseEvent ev)
    {
        return PositionCursor (TextModel.GetColFromX (_text, ScrollOffset, ev.Position.X), false);
    }

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

            SetNeedsDisplay ();
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

        // draw autocomplete
        GenerateSuggestions ();

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

        var color = new Attribute (CaptionColor, GetNormalColor ().Background);
        Driver.SetAttribute (color);

        Move (0, 0);
        string render = Caption;

        if (render.GetColumns () > Viewport.Width)
        {
            render = render [..Viewport.Width];
        }

        Driver.AddStr (render);
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
        SetNeedsDisplay ();
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

    private void ShowContextMenu ()
    {
        if (_currentCulture != Thread.CurrentThread.CurrentUICulture)
        {
            _currentCulture = Thread.CurrentThread.CurrentUICulture;

            ContextMenu.MenuItems = BuildContextMenuBarItem ();
        }

        ContextMenu.Show ();
    }

    private void TextField_Initialized (object sender, EventArgs e)
    {
        _cursorPosition = Text.GetRuneCount ();

        if (Viewport.Width > 0)
        {
            ScrollOffset = _cursorPosition > Viewport.Width + 1 ? _cursorPosition - Viewport.Width + 1 : 0;
        }

        Autocomplete.HostControl = this;
        Autocomplete.PopupInsideContainer = false;
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
