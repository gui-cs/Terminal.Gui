namespace Terminal.Gui.Views;

public partial class TextField
{
    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="TextField"/>. These are layered on top of
    ///     <see cref="View.DefaultKeyBindings"/> when the view is created.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only single-command bindings are included here. The context menu binding is added directly
    ///         in the constructor.
    ///     </para>
    /// </remarks>
    public new static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
    {
        // Emacs navigation (non-Windows only)
        ["Left"] = Bind.NonWindows ("Ctrl+B"),
        ["Right"] = Bind.NonWindows ("Ctrl+F"),

        // Additional LeftStart key (Ctrl+Home; base already has Home)
        ["LeftStart"] = Bind.All ("Ctrl+Home"),

        // Additional RightEnd keys (Ctrl+End, Ctrl+E; base already has End)
        ["RightEnd"] = Bind.All ("Ctrl+End", "Ctrl+E"),

        // Additional extend keys (CursorUp/CursorDown with Shift; base has CursorLeft/CursorRight with Shift)
        ["LeftExtend"] = Bind.All ("Shift+CursorUp"),
        ["RightExtend"] = Bind.All ("Shift+CursorDown"),

        // Additional LeftStartExtend keys (base already has Shift+Home)
        ["LeftStartExtend"] = Bind.All ("Ctrl+Shift+Home", "Ctrl+Shift+A"),

        // Additional RightEndExtend keys (base already has Shift+End)
        ["RightEndExtend"] = Bind.All ("Ctrl+Shift+End", "Ctrl+Shift+E"),

        // Word navigation
        ["WordLeft"] = Bind.All ("Ctrl+CursorLeft", "Ctrl+CursorUp"),
        ["WordRight"] = Bind.All ("Ctrl+CursorRight", "Ctrl+CursorDown"),
        ["WordLeftExtend"] = Bind.All ("Ctrl+Shift+CursorLeft", "Ctrl+Shift+CursorUp"),
        ["WordRightExtend"] = Bind.All ("Ctrl+Shift+CursorRight", "Ctrl+Shift+CursorDown"),

        // Kill commands
        ["CutToEndOfLine"] = Bind.All ("Ctrl+K"),
        ["CutToStartOfLine"] = Bind.All ("Ctrl+Shift+K"),
        ["KillWordRight"] = Bind.All ("Ctrl+Delete"),
        ["KillWordLeft"] = Bind.All ("Ctrl+Backspace"),

        // Overwrite mode
        ["ToggleOverwrite"] = Bind.All ("Insert"),

        // Delete all text
        ["DeleteAll"] = Bind.All ("Ctrl+Shift+Delete")
    };

    private void CreateCommandsAndBindings ()
    {
        // Things this view knows how to do
        AddCommand (Command.DeleteCharRight, () => DeleteCharRight ());
        AddCommand (Command.DeleteCharLeft, () => DeleteCharLeft (false));
        AddCommand (Command.LeftStartExtend, () => MoveHomeExtend ());
        AddCommand (Command.RightEndExtend, () => MoveEndExtend ());
        AddCommand (Command.LeftStart, () => MoveHome ());
        AddCommand (Command.LeftExtend, () => MoveLeftExtend ());
        AddCommand (Command.RightExtend, () => MoveRightExtend ());
        AddCommand (Command.WordLeftExtend, () => MoveWordLeftExtend ());
        AddCommand (Command.WordRightExtend, () => MoveWordRightExtend ());
        AddCommand (Command.Left, () => MoveLeft ());
        AddCommand (Command.RightEnd, () => MoveEnd ());
        AddCommand (Command.Right, () => MoveRight ());
        AddCommand (Command.CutToEndOfLine, () => KillToEnd ());
        AddCommand (Command.CutToStartOfLine, () => KillToStart ());
        AddCommand (Command.Undo, () => Undo ());
        AddCommand (Command.Redo, () => Redo ());
        AddCommand (Command.WordLeft, () => MoveWordLeft ());
        AddCommand (Command.WordRight, () => MoveWordRight ());
        AddCommand (Command.KillWordRight, () => KillWordForwards ());
        AddCommand (Command.KillWordLeft, () => KillWordBackwards ());
        AddCommand (Command.ToggleOverwrite, () => SetOverwrite (!Used));
        AddCommand (Command.EnableOverwrite, () => SetOverwrite (true));
        AddCommand (Command.DisableOverwrite, () => SetOverwrite (false));
        AddCommand (Command.Copy, () => Copy ());
        AddCommand (Command.Cut, () => Cut ());
        AddCommand (Command.Paste, () => Paste ());
        AddCommand (Command.SelectAll, () => SelectAll ());
        AddCommand (Command.DeleteAll, () => DeleteAll ());
        AddCommand (Command.Context, () => ShowContextMenu (true));

        // Apply configurable key bindings (base layer + TextField-specific layer)
        ApplyKeyBindings (View.DefaultKeyBindings, DefaultKeyBindings);

        // Remove Space binding inherited from View base (not applicable in a text field)
        KeyBindings.Remove (Key.Space);
    }

    /// <summary>Deletes word backwards.</summary>
    public bool KillWordBackwards ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordBackward (_insertionPoint, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return true;
        }

        if (newPos.Value.col != -1)
        {
            SetText (_text.GetRange (0, newPos.Value.col).Concat (_text.GetRange (_insertionPoint, _text.Count - _insertionPoint)));
            _insertionPoint = newPos.Value.col;
        }

        Adjust ();

        return true;
    }

    /// <summary>Deletes word forwards.</summary>
    public bool KillWordForwards ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordForward (_insertionPoint, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return true;
        }

        if (newPos.Value.col != -1)
        {
            SetText (_text.GetRange (0, _insertionPoint).Concat (_text.GetRange (newPos.Value.col, _text.Count - newPos.Value.col)));
        }

        Adjust ();

        return true;
    }

    /// <summary>Moves cursor to the end of the typed text.</summary>
    public bool MoveEnd ()
    {
        ClearAllSelection ();
        _insertionPoint = _text.Count;
        Adjust ();

        return true;
    }

    /// <summary>Paste the selected text from the clipboard.</summary>
    public bool Paste ()
    {
        if (ReadOnly)
        {
            return true;
        }

        string? cbTxt = App?.Clipboard?.GetClipboardData ().Split ("\n") [0];

        if (string.IsNullOrEmpty (cbTxt))
        {
            return false;
        }

        SetSelectedStartSelectedLength ();
        int selStart = _selectionStart == -1 ? InsertionPoint : _selectionStart;

        Text = StringExtensions.ToString (_text.GetRange (0, selStart))
               + cbTxt
               + StringExtensions.ToString (_text.GetRange (selStart + SelectedLength, _text.Count - (selStart + SelectedLength)));

        _insertionPoint = Math.Min (selStart + cbTxt.GetRuneCount (), _text.Count);
        ClearAllSelection ();
        SetNeedsDraw ();
        Adjust ();

        return true;
    }

    /// <summary>Redoes the latest changes.</summary>
    public bool Redo ()
    {
        if (ReadOnly)
        {
            return true;
        }

        _historyText.Redo ();

        return true;
    }

    /// <summary>Selects all text.</summary>
    public bool SelectAll ()
    {
        if (_text.Count == 0)
        {
            return false;
        }

        _selectionAnchor = 0;
        MoveEndExtend ();
        SetNeedsDraw ();

        return true;
    }

    private bool SetOverwrite (bool overwrite)
    {
        Used = overwrite;
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Undoes the latest changes.</summary>
    public bool Undo ()
    {
        if (ReadOnly)
        {
            return true;
        }

        _historyText.Undo ();

        return true;
    }

    private bool ShowContextMenu (bool keyboard)
    {
        if (!Equals (_currentCulture, Thread.CurrentThread.CurrentUICulture))
        {
            _currentCulture = Thread.CurrentThread.CurrentUICulture;
        }

        ContextMenu?.MakeVisible (ViewportToScreen (new Point (_insertionPoint - ScrollOffset, 1)));

        return true;
    }

    /// <summary>Deletes all text.</summary>
    public bool DeleteAll ()
    {
        if (_text.Count == 0)
        {
            return false;
        }

        _selectionAnchor = 0;
        MoveEndExtend ();
        DeleteCharLeft (false);
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Deletes the character to the left.</summary>
    /// <param name="usePreTextChangedCursorPos">
    ///     If set to <see langword="true">true</see> use the cursor position cached ;
    ///     otherwise use <see cref="InsertionPoint"/>. use .
    /// </param>
    public virtual bool DeleteCharLeft (bool usePreTextChangedCursorPos)
    {
        if (ReadOnly)
        {
            return true;
        }

        _historyText.Add ([Cell.ToCells (_text)], new Point (_insertionPoint, 0));

        if (SelectedLength == 0)
        {
            if (_insertionPoint == 0)
            {
                return true;
            }

            if (!usePreTextChangedCursorPos)
            {
                _preChangeInsertionPoint = _insertionPoint;
            }

            _insertionPoint--;

            if (_preChangeInsertionPoint < _text.Count)
            {
                SetText (_text.GetRange (0, _preChangeInsertionPoint - 1)
                              .Concat (_text.GetRange (_preChangeInsertionPoint, _text.Count - _preChangeInsertionPoint)));
            }
            else
            {
                SetText (_text.GetRange (0, _preChangeInsertionPoint - 1));
            }
        }
        else
        {
            List<string> newText = DeleteSelectedText ();
            Text = StringExtensions.ToString (newText);
        }

        Adjust ();

        return true;
    }

    /// <summary>Deletes the character to the right.</summary>
    public virtual bool DeleteCharRight ()
    {
        if (ReadOnly)
        {
            return true;
        }

        _historyText.Add ([Cell.ToCells (_text)], new Point (_insertionPoint, 0));

        if (SelectedLength == 0)
        {
            if (_text.Count == 0 || _text.Count == _insertionPoint)
            {
                return true;
            }

            SetText (_text.GetRange (0, _insertionPoint).Concat (_text.GetRange (_insertionPoint + 1, _text.Count - (_insertionPoint + 1))));
        }
        else
        {
            List<string> newText = DeleteSelectedText ();
            Text = StringExtensions.ToString (newText);
        }

        Adjust ();

        return true;
    }

    private bool MoveEndExtend ()
    {
        if (_insertionPoint > _text.Count)
        {
            return true;
        }
        int x = _insertionPoint;
        _insertionPoint = _text.Count;
        PrepareSelection (x, _insertionPoint - x);

        return true;
    }

    private bool MoveHome ()
    {
        ClearAllSelection ();
        _insertionPoint = 0;
        Adjust ();

        return true;
    }

    private bool MoveHomeExtend ()
    {
        if (_insertionPoint <= 0)
        {
            return true;
        }
        int x = _insertionPoint;
        _insertionPoint = 0;
        PrepareSelection (x, _insertionPoint - x);

        return true;
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
        int oldCursorPosition = _insertionPoint;
        bool hadSelection = _selectedText is { Length: > 0 };

        _insertionPoint = Math.Min (_text.Count, Math.Max (0, _insertionPoint + distance));
        ClearAllSelection ();
        Adjust ();

        return _insertionPoint != oldCursorPosition || hadSelection;
    }

    private bool MoveLeft () => Move (-1);

    private bool MoveLeftExtend ()
    {
        if (_insertionPoint > 0)
        {
            PrepareSelection (_insertionPoint--, -1);
        }

        return true;
    }

    private bool MoveRight () => Move (1);

    private bool MoveRightExtend ()
    {
        if (_insertionPoint < _text.Count)
        {
            PrepareSelection (_insertionPoint++, 1);
        }

        return true;
    }

    private bool MoveWordLeft ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordBackward (_insertionPoint, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return false;
        }

        if (newPos.Value.col != -1)
        {
            _insertionPoint = newPos.Value.col;
        }

        Adjust ();

        return true;
    }

    private bool MoveWordLeftExtend ()
    {
        if (_insertionPoint <= 0)
        {
            return false;
        }

        int x = Math.Min (_selectionStart > -1 && _selectionStart > _insertionPoint ? _selectionStart : _insertionPoint, _text.Count);

        if (x <= 0)
        {
            return false;
        }

        (int col, int row)? newPos = GetModel ().WordBackward (x, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return false;
        }

        if (newPos.Value.col != -1)
        {
            _insertionPoint = newPos.Value.col;
        }

        PrepareSelection (x, newPos.Value.col - x);

        return true;
    }

    private bool MoveWordRight ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordForward (_insertionPoint, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return false;
        }

        if (newPos.Value.col != -1)
        {
            _insertionPoint = newPos.Value.col;
        }

        Adjust ();

        return true;
    }

    private bool MoveWordRightExtend ()
    {
        if (_insertionPoint >= _text.Count)
        {
            return false;
        }

        int x = _selectionStart > -1 && _selectionStart > _insertionPoint ? _selectionStart : _insertionPoint;
        (int col, int row)? newPos = GetModel ().WordForward (x, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return false;
        }

        if (newPos.Value.col != -1)
        {
            _insertionPoint = newPos.Value.col;
        }

        PrepareSelection (x, newPos.Value.col - x);

        return true;
    }

    /// <summary>Copy the selected text to the clipboard.</summary>
    public bool Copy ()
    {
        if (Secret || SelectedLength == 0 || SelectedText is null)
        {
            return false;
        }

        App?.Clipboard?.SetClipboardData (SelectedText);

        return true;
    }

    /// <summary>Cut the selected text to the clipboard.</summary>
    public bool Cut ()
    {
        if (ReadOnly || Secret || SelectedLength == 0 || SelectedText is null)
        {
            return true;
        }

        App?.Clipboard?.SetClipboardData (SelectedText);
        List<string> newText = DeleteSelectedText ();
        Text = StringExtensions.ToString (newText);
        Adjust ();

        return true;
    }

    private bool KillToEnd ()
    {
        if (ReadOnly)
        {
            return true;
        }

        ClearAllSelection ();

        if (_insertionPoint >= _text.Count)
        {
            return true;
        }

        SetClipboard (_text.GetRange (_insertionPoint, _text.Count - _insertionPoint));
        SetText (_text.GetRange (0, _insertionPoint));
        Adjust ();

        return true;
    }

    private bool KillToStart ()
    {
        if (ReadOnly)
        {
            return true;
        }

        ClearAllSelection ();

        if (_insertionPoint == 0)
        {
            return true;
        }

        SetClipboard (_text.GetRange (0, _insertionPoint));
        SetText (_text.GetRange (_insertionPoint, _text.Count - _insertionPoint));
        _insertionPoint = 0;
        Adjust ();

        return true;
    }

    private void SetClipboard (IEnumerable<string> text)
    {
        if (!Secret && App?.Clipboard is { })
        {
            App.Clipboard.SetClipboardData (StringExtensions.ToString (text.ToList ()));
        }
    }

    /// <summary>
    ///     Gets or sets whether the word forward and word backward navigation should use the same or equivalent rune type.
    ///     Default is <c>false</c> meaning using equivalent rune type.
    /// </summary>
    public bool UseSameRuneTypeForWords { get; set; }
}
