namespace Terminal.Gui.Views;

public partial class TextView
{
    private void CreateCommandsAndBindings ()
    {
        // Things this view knows how to do

        // Note - NewLine is only bound to Enter if Multiline is true
        AddCommand (Command.NewLine, ctx => ProcessEnterKey (ctx));

        // Movement
        AddCommand (Command.PageDown, () => ProcessPageDown ());
        AddCommand (Command.PageDownExtend, () => ProcessPageDownExtend ());
        AddCommand (Command.PageUp, () => ProcessPageUp ());
        AddCommand (Command.PageUpExtend, () => ProcessPageUpExtend ());
        AddCommand (Command.Down, () => ProcessMoveDown ());
        AddCommand (Command.DownExtend, () => ProcessMoveDownExtend ());
        AddCommand (Command.Up, () => ProcessMoveUp ());
        AddCommand (Command.UpExtend, () => ProcessMoveUpExtend ());
        AddCommand (Command.Right, () => ProcessMoveRight ());
        AddCommand (Command.RightExtend, () => ProcessMoveRightExtend ());
        AddCommand (Command.Left, () => ProcessMoveLeft ());
        AddCommand (Command.LeftExtend, () => ProcessMoveLeftExtend ());
        AddCommand (Command.LeftStart, () => ProcessMoveLeftStart ());
        AddCommand (Command.LeftStartExtend, () => ProcessMoveLeftStartExtend ());
        AddCommand (Command.RightEnd, () => ProcessMoveEndOfLine ());
        AddCommand (Command.RightEndExtend, () => ProcessMoveRightEndExtend ());
        AddCommand (Command.WordLeft, () => ProcessMoveWordLeft ());
        AddCommand (Command.WordLeftExtend, () => ProcessMoveWordLeftExtend ());
        AddCommand (Command.WordRight, () => ProcessMoveWordRight ());
        AddCommand (Command.WordRightExtend, () => ProcessMoveWordRightExtend ());
        AddCommand (Command.End, () => MoveBottomEnd ());
        AddCommand (Command.EndExtend, () => MoveBottomEndExtend ());
        AddCommand (Command.Start, () => MoveTopHome ());
        AddCommand (Command.StartExtend, () => MoveTopHomeExtend ());
        AddCommand (Command.ToggleExtend, () => ToggleSelecting ());

        // Editing
        AddCommand (Command.SelectAll, () => ProcessSelectAll ());
        AddCommand (Command.CutToEndOfLine, () => CutToEndOfLine ());
        AddCommand (Command.CutToStartOfLine, () => CutToStartOfLine ());
        AddCommand (Command.Paste, () => ProcessPaste ());
        AddCommand (Command.Copy, () => ProcessCopy ());
        AddCommand (Command.Cut, () => ProcessCut ());
        AddCommand (Command.DeleteCharLeft, () => ProcessDeleteCharLeft ());
        AddCommand (Command.DeleteCharRight, () => ProcessDeleteCharRight ());
        AddCommand (Command.DeleteAll, () => DeleteAll ());
        AddCommand (Command.KillWordRight, () => ProcessKillWordRight ());
        AddCommand (Command.KillWordLeft, () => ProcessKillWordLeft ());
        AddCommand (Command.Undo, () => Undo ());
        AddCommand (Command.Redo, () => Redo ());

        AddCommand (Command.NextTabStop, () => ProcessTab ());
        AddCommand (Command.ToggleOverwrite, () => ProcessSetOverwrite ());
        AddCommand (Command.EnableOverwrite, () => SetOverwrite (true));
        AddCommand (Command.DisableOverwrite, () => SetOverwrite (false));
        AddCommand (Command.Context, () => ShowContextMenu (null));
        AddCommand (Command.Open, () => PromptForColors ());

        // Default keybindings for this view
        KeyBindings.Remove (Key.Space);

        KeyBindings.Remove (Key.Enter);
        KeyBindings.Add (Key.Enter, Multiline ? Command.NewLine : Command.Accept);

        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.V.WithCtrl, Command.PageDown);

        KeyBindings.Add (Key.PageDown.WithShift, Command.PageDownExtend);

        KeyBindings.Add (Key.PageUp, Command.PageUp);

        KeyBindings.Add (Key.PageUp.WithShift, Command.PageUpExtend);

        KeyBindings.Add (Key.N.WithCtrl, Command.Down);
        KeyBindings.Add (Key.CursorDown, Command.Down);

        KeyBindings.Add (Key.CursorDown.WithShift, Command.DownExtend);

        KeyBindings.Add (Key.P.WithCtrl, Command.Up);
        KeyBindings.Add (Key.CursorUp, Command.Up);

        KeyBindings.Add (Key.CursorUp.WithShift, Command.UpExtend);

        KeyBindings.Add (Key.F.WithCtrl, Command.Right);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        KeyBindings.Add (Key.CursorRight.WithShift, Command.RightExtend);

        KeyBindings.Add (Key.B.WithCtrl, Command.Left);
        KeyBindings.Add (Key.CursorLeft, Command.Left);

        KeyBindings.Add (Key.CursorLeft.WithShift, Command.LeftExtend);

        KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);

        KeyBindings.Add (Key.Home, Command.LeftStart);

        KeyBindings.Add (Key.Home.WithShift, Command.LeftStartExtend);

        KeyBindings.Add (Key.Delete, Command.DeleteCharRight);
        KeyBindings.Add (Key.D.WithCtrl, Command.DeleteCharRight);

        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Add (Key.E.WithCtrl, Command.RightEnd);

        KeyBindings.Add (Key.End.WithShift, Command.RightEndExtend);

        KeyBindings.Add (Key.K.WithCtrl, Command.CutToEndOfLine); // kill-to-end

        KeyBindings.Add (Key.Delete.WithCtrl.WithShift, Command.CutToEndOfLine); // kill-to-end

        KeyBindings.Add (Key.Backspace.WithCtrl.WithShift, Command.CutToStartOfLine); // kill-to-start

        KeyBindings.Add (Key.Y.WithCtrl, Command.Paste); // Control-y, yank
        KeyBindings.Add (Key.Space.WithCtrl, Command.ToggleExtend);

        KeyBindings.Add (Key.C.WithCtrl, Command.Copy);

        KeyBindings.Add (Key.W.WithCtrl, Command.Cut); // Move to Unix?
        KeyBindings.Add (Key.X.WithCtrl, Command.Cut);

        KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.WordLeft);

        KeyBindings.Add (Key.CursorLeft.WithCtrl.WithShift, Command.WordLeftExtend);

        KeyBindings.Add (Key.CursorRight.WithCtrl, Command.WordRight);

        KeyBindings.Add (Key.CursorRight.WithCtrl.WithShift, Command.WordRightExtend);
        KeyBindings.Add (Key.Delete.WithCtrl, Command.KillWordRight); // kill-word-forwards
        KeyBindings.Add (Key.Backspace.WithCtrl, Command.KillWordLeft); // kill-word-backwards

        KeyBindings.Add (Key.End.WithCtrl, Command.End);
        KeyBindings.Add (Key.End.WithCtrl.WithShift, Command.EndExtend);
        KeyBindings.Add (Key.Home.WithCtrl, Command.Start);
        KeyBindings.Add (Key.Home.WithCtrl.WithShift, Command.StartExtend);
        KeyBindings.Add (Key.A.WithCtrl, Command.SelectAll);
        KeyBindings.Add (Key.InsertChar, Command.ToggleOverwrite);
        KeyBindings.Add (Key.Tab, Command.NextTabStop);
        KeyBindings.Add (Key.Tab.WithShift, Command.PreviousTabStop);

        KeyBindings.Add (Key.Z.WithCtrl, Command.Undo);
        KeyBindings.Add (Key.R.WithCtrl, Command.Redo);

        KeyBindings.Add (Key.G.WithCtrl, Command.DeleteAll);
        KeyBindings.Add (Key.D.WithCtrl.WithShift, Command.DeleteAll);

        KeyBindings.Add (Key.L.WithCtrl, Command.Open);
    }

    private void DoNeededAction ()
    {
        if (!NeedsDraw && (IsSelecting || _wrapNeeded || !Used))
        {
            SetNeedsDraw ();
        }

        if (NeedsDraw)
        {
            AdjustViewport ();
        }
        else
        {
            PositionCursor ();
            RaiseUnwrappedCursorPositionChanged ();
        }
        ProcessAutocomplete ();
    }

    /// <summary>
    ///     Open a dialog to set the foreground and background colors.
    /// </summary>
    public bool PromptForColors ()
    {
        Attribute? attribute = App?.TopRunnable?.Prompt<AttributePicker, Attribute?> (input: GetSelectedCellAttribute (),
                                                                                      beginInitHandler: prompt =>
                                                                                                        {
                                                                                                            // Customize the Prompt dialog
                                                                                                            prompt.Title = "Pick an Attribute";
                                                                                                        });

        if (attribute is null)
        {
            return true;
        }

        ApplyCellsAttribute (attribute.Value);

        return true;
    }

    private void SetClipboard (string text) => App?.Clipboard?.SetClipboardData (text);

    private string? _copiedText;
    private List<List<Cell>> _copiedCellsList = [];

    /// <summary>Copy the selected text to the clipboard contents.</summary>
    public bool Copy ()
    {
        SetWrapModel ();

        if (IsSelecting)
        {
            _copiedText = GetRegion (out _copiedCellsList);
            SetClipboard (_copiedText);
            _copyWithoutSelection = false;
        }
        else
        {
            List<Cell> currentLine = GetCurrentLine ();
            _copiedCellsList.Add (currentLine);
            _copiedText = Cell.ToString (currentLine);
            SetClipboard (_copiedText);
            _copyWithoutSelection = true;
        }

        UpdateWrapModel ();
        DoNeededAction ();

        return true;
    }

    /// <summary>Cut the selected text to the clipboard contents.</summary>
    public bool Cut ()
    {
        SetWrapModel ();
        _copiedText = GetRegion (out _copiedCellsList);
        SetClipboard (_copiedText);

        if (!_isReadOnly)
        {
            ClearRegion ();

            _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();
        OnContentsChanged ();

        return true;
    }

    /// <summary>Paste the clipboard contents into the current selected position.</summary>
    public bool Paste ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        SetWrapModel ();
        string? contents = App?.Clipboard?.GetClipboardData ();

        if (_copyWithoutSelection && contents!.FirstOrDefault (x => x is '\n' or '\r') == 0)
        {
            List<Cell> runeList = contents is null ? [] : Cell.ToCellList (contents);
            List<Cell> currentLine = GetCurrentLine ();
            _historyText.Add ([[.. currentLine]], InsertionPoint);
            List<List<Cell>> addedLine = [[.. currentLine], runeList];
            _historyText.Add ([.. addedLine], InsertionPoint, TextEditingLineStatus.Added);
            _model.AddLine (CurrentRow, runeList);
            SetNeedsDraw ();
            CurrentRow++;
            _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

            OnContentsChanged ();
        }
        else
        {
            if (IsSelecting)
            {
                ClearRegion ();
            }

            _copyWithoutSelection = false;
            InsertAllText (contents!, true);

            if (IsSelecting)
            {
                _historyText.ReplaceLast ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Original);
            }
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();

        return true;
    }

    private void AppendClipboard (string text) => App?.Clipboard?.SetClipboardData (App?.Clipboard?.GetClipboardData () + text);

    private bool ProcessCopy ()
    {
        ResetColumnTrack ();

        return Copy ();
    }

    private bool ProcessCut ()
    {
        ResetColumnTrack ();

        return Cut ();
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

    /// <summary>Select all text.</summary>
    public bool SelectAll ()
    {
        if (_model.Count == 0)
        {
            return true;
        }

        StartSelecting ();
        _selectionStartColumn = 0;
        _selectionStartRow = 0;
        CurrentColumn = _model.GetLine (_model.Count - 1).Count;
        CurrentRow = _model.Count - 1;

        return true;
    }

    private bool ToggleSelecting ()
    {
        ResetColumnTrack ();
        IsSelecting = !IsSelecting;
        _selectionStartColumn = CurrentColumn;
        _selectionStartRow = CurrentRow;

        return true;
    }

    /// <summary>Deletes all text.</summary>
    public bool DeleteAll ()
    {
        if (Lines == 0)
        {
            return true;
        }

        _selectionStartColumn = 0;
        _selectionStartRow = 0;
        MoveBottomEndExtend ();

        return DeleteCharLeft ();
    }

    /// <summary>Deletes all the selected or a single character at left from the position of the cursor.</summary>
    public bool DeleteCharLeft ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        if (IsSelecting)
        {
            SetWrapModel ();
            _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add ([[.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);

            UpdateWrapModel ();
            OnContentsChanged ();

            return true;
        }

        bool retValue = DeleteTextLeft ();

        DoNeededAction ();
        OnContentsChanged ();

        return retValue;
    }

    /// <summary>Deletes all the selected or a single character at right from the position of the cursor.</summary>
    public bool DeleteCharRight ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        if (IsSelecting)
        {
            SetWrapModel ();
            _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add ([[.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);

            UpdateWrapModel ();
            OnContentsChanged ();

            return true;
        }

        bool retValue = DeleteTextRight ();

        DoNeededAction ();
        OnContentsChanged ();

        return retValue;
    }

    private bool DeleteTextLeft ()
    {
        if (CurrentColumn > 0)
        {
            SetWrapModel ();

            // Delete backwards
            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add ([[.. currentLine]], InsertionPoint);

            currentLine.RemoveAt (CurrentColumn - 1);
            SetNeedsDraw ();

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn--;

            _historyText.Add ([[.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);

            if (CurrentColumn < Viewport.X)
            {
                Viewport = Viewport with { X = Viewport.X - 1 };
            }
            UpdateWrapModel ();
        }
        else
        {
            // Merges the current line with the previous one.
            if (CurrentRow == 0)
            {
                return true;
            }

            SetWrapModel ();
            int prowIdx = CurrentRow - 1;
            List<Cell> prevRow = _model.GetLine (prowIdx);

            _historyText.Add ([[.. prevRow]], InsertionPoint);

            List<List<Cell>> removedLines = [[..prevRow], [..GetCurrentLine ()]];

            _historyText.Add (removedLines, new Point (CurrentColumn, prowIdx), TextEditingLineStatus.Removed);

            int prevCount = prevRow.Count;
            _model.GetLine (prowIdx).AddRange (GetCurrentLine ());
            _model.RemoveLine (CurrentRow);
            SetNeedsDraw ();

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentRow--;

            _historyText.Add ([GetCurrentLine ()], new Point (CurrentColumn, prowIdx), TextEditingLineStatus.Replaced);

            CurrentColumn = prevCount;
            UpdateWrapModel ();
        }

        return true;
    }

    private bool DeleteTextRight ()
    {
        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        if (CurrentColumn == currentLine.Count && CurrentRow + 1 == _model.Count)
        {
            // Nothing to delete
            UpdateWrapModel ();

            return true;
        }

        if (CurrentColumn == currentLine.Count)
        {
            // We're at the end of the line; need to merge with the next line
            _historyText.Add ([[.. currentLine]], InsertionPoint);

            List<List<Cell>> removedLines = [[.. currentLine]];
            List<Cell> nextLine = _model.GetLine (CurrentRow + 1);
            removedLines.Add ([.. nextLine]);
            _historyText.Add (removedLines, InsertionPoint, TextEditingLineStatus.Removed);
            currentLine.AddRange (nextLine);
            _model.RemoveLine (CurrentRow + 1);
            SetNeedsDraw ();

            _historyText.Add ([[.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            UpdateWrapModel ();

            return true;
        }

        // We're not at the end of the line; delete to end of line
        _historyText.Add ([[.. currentLine]], InsertionPoint);

        currentLine.RemoveAt (CurrentColumn);
        SetNeedsDraw ();

        _historyText.Add ([[.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);

        if (_wordWrap)
        {
            _wrapNeeded = true;
        }
        UpdateWrapModel ();

        return true;
    }

    private bool CutToEndOfLine ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return true;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == currentLine.Count)
        {
            UpdateWrapModel ();

            DeleteTextRight ();

            return true;
        }

        _historyText.Add ([[.. currentLine]], InsertionPoint);

        if (currentLine.Count == 0)
        {
            if (CurrentRow < _model.Count - 1)
            {
                List<List<Cell>> removedLines = [[.. currentLine]];

                _model.RemoveLine (CurrentRow);
                SetNeedsDraw ();

                removedLines.Add ([.. GetCurrentLine ()]);

                _historyText.Add ([.. removedLines], InsertionPoint, TextEditingLineStatus.Removed);
            }

            if (_model.Count > 0 || _lastWasKill)
            {
                string val = Environment.NewLine;

                if (_lastWasKill)
                {
                    AppendClipboard (val);
                }
                else
                {
                    SetClipboard (val);
                }
            }

            if (_model.Count == 0)
            {
                // Prevents from adding line feeds if there is no more lines.
                setLastWasKill = false;
            }
        }
        else
        {
            int restCount = currentLine.Count - CurrentColumn;
            List<Cell> rest = currentLine.GetRange (CurrentColumn, restCount);
            var val = string.Empty;
            val += StringFromCells (rest);

            if (_lastWasKill)
            {
                AppendClipboard (val);
            }
            else
            {
                SetClipboard (val);
            }

            currentLine.RemoveRange (CurrentColumn, restCount);
            SetNeedsDraw ();
        }

        _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();

        _lastWasKill = setLastWasKill;
        DoNeededAction ();

        return true;
    }

    private bool CutToStartOfLine ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return true;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == 0)
        {
            UpdateWrapModel ();

            DeleteTextLeft ();

            return true;
        }

        _historyText.Add ([[.. currentLine]], InsertionPoint);

        if (currentLine.Count == 0)
        {
            if (CurrentRow > 0)
            {
                _model.RemoveLine (CurrentRow);

                if (_model.Count > 0 || _lastWasKill)
                {
                    string val = Environment.NewLine;

                    if (_lastWasKill)
                    {
                        AppendClipboard (val);
                    }
                    else
                    {
                        SetClipboard (val);
                    }
                }

                if (_model.Count == 0)
                {
                    // Prevents from adding line feeds if there is no more lines.
                    setLastWasKill = false;
                }

                CurrentRow--;
                currentLine = _model.GetLine (CurrentRow);

                List<List<Cell>> removedLine = [[..currentLine], []];

                _historyText.Add ([.. removedLine], InsertionPoint, TextEditingLineStatus.Removed);

                CurrentColumn = currentLine.Count;
            }
        }
        else
        {
            int restCount = CurrentColumn;
            List<Cell> rest = currentLine.GetRange (0, restCount);
            var val = string.Empty;
            val += StringFromCells (rest);

            if (_lastWasKill)
            {
                AppendClipboard (val);
            }
            else
            {
                SetClipboard (val);
            }

            currentLine.RemoveRange (0, restCount);
            CurrentColumn = 0;
        }

        _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();

        SetNeedsDraw ();

        _lastWasKill = setLastWasKill;
        DoNeededAction ();

        return true;
    }

    private bool KillWordLeft ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint);

        if (CurrentColumn == 0)
        {
            DeleteTextLeft ();

            _historyText.ReplaceLast ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

            UpdateWrapModel ();

            return true;
        }

        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            int restCount = CurrentColumn - newPos.Value.col;
            currentLine.RemoveRange (newPos.Value.col, restCount);
            SetNeedsDraw ();

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn = newPos.Value.col;
        }
        else if (newPos.HasValue)
        {
            int restCount;

            if (newPos.Value.row == CurrentRow)
            {
                restCount = currentLine.Count - CurrentColumn;
                currentLine.RemoveRange (CurrentColumn, restCount);
                SetNeedsDraw ();
            }
            else
            {
                while (CurrentRow != newPos.Value.row)
                {
                    restCount = currentLine.Count;
                    currentLine.RemoveRange (0, restCount);
                    SetNeedsDraw ();

                    CurrentRow--;
                    currentLine = GetCurrentLine ();
                }
            }

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();

        DoNeededAction ();

        return true;
    }

    private bool KillWordRight ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint);

        if (currentLine.Count == 0 || CurrentColumn == currentLine.Count)
        {
            DeleteTextRight ();

            _historyText.ReplaceLast ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

            UpdateWrapModel ();

            return true;
        }

        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);
        int restCount;

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            restCount = newPos.Value.col - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
            SetNeedsDraw ();
        }
        else if (newPos.HasValue)
        {
            restCount = currentLine.Count - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
            SetNeedsDraw ();
        }

        if (_wordWrap)
        {
            _wrapNeeded = true;
        }

        _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();

        DoNeededAction ();

        return true;
    }

    private bool ProcessDeleteCharLeft ()
    {
        ResetColumnTrack ();

        return DeleteCharLeft ();
    }

    private bool ProcessDeleteCharRight ()
    {
        ResetColumnTrack ();

        return DeleteCharRight ();
    }

    private bool ProcessKillWordLeft ()
    {
        ResetColumnTrack ();

        return KillWordLeft ();
    }

    private bool ProcessKillWordRight ()
    {
        ResetColumnTrack ();
        StopSelecting ();

        return KillWordRight ();
    }

    private bool ProcessPaste ()
    {
        ResetColumnTrack ();

        if (_isReadOnly)
        {
            return true;
        }

        return Paste ();
    }

    private bool ProcessEnterKey (ICommandContext? commandContext)
    {
        ResetColumnTrack ();

        if (_isReadOnly)
        {
            return false;
        }

        if (!EnterKeyAddsLine)
        {
            // By Default pressing ENTER should be ignored (OnAccept will return false or null). Only cancel if the
            // event was fired and set Cancel = true.
            return RaiseAccepting (commandContext) is null or false;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        _historyText.Add ([[.. currentLine]], InsertionPoint);

        if (IsSelecting)
        {
            ClearSelectedRegion ();
            currentLine = GetCurrentLine ();
        }
        int restCount = currentLine.Count - CurrentColumn;
        List<Cell> rest = currentLine.GetRange (CurrentColumn, restCount);
        currentLine.RemoveRange (CurrentColumn, restCount);
        List<List<Cell>> addedLines = [[.. currentLine]];
        _model.AddLine (CurrentRow + 1, rest);
        addedLines.Add ([.. _model.GetLine (CurrentRow + 1)]);
        _historyText.Add (addedLines, InsertionPoint, TextEditingLineStatus.Added);
        CurrentRow++;

        if (CurrentRow >= Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = Viewport.Y + 1 };
        }

        CurrentColumn = 0;

        _historyText.Add ([[.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        if (!_wordWrap && CurrentColumn < Viewport.X)
        {
            Viewport = Viewport with { X = 0 };
        }

        SetNeedsDraw ();
        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();

        return true;
    }

    private bool ProcessSelectAll ()
    {
        ResetColumnTrack ();

        return SelectAll ();
    }

    private bool SetOverwrite (bool overwrite)
    {
        Used = overwrite;
        SetNeedsDraw ();
        DoNeededAction ();

        return true;
    }

    private bool ProcessSetOverwrite ()
    {
        ResetColumnTrack ();

        return SetOverwrite (!Used);
    }

    private bool ProcessTab ()
    {
        ResetColumnTrack ();

        if (!TabKeyAddsTab || _isReadOnly)
        {
            return false;
        }

        InsertText (new Key ((KeyCode)'\t'));
        DoNeededAction ();

        return true;
    }

    private bool ShowContextMenu (Point? mousePosition)
    {
        if (!Equals (_currentCulture, Thread.CurrentThread.CurrentUICulture))
        {
            _currentCulture = Thread.CurrentThread.CurrentUICulture;
        }

        mousePosition ??= ViewportToScreen (new Point (InsertionPoint.X, InsertionPoint.Y));

        ContextMenu?.MakeVisible (mousePosition);

        return true;
    }
}
