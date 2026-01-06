namespace Terminal.Gui.Views;

public partial class TextView
{
    private void CreateCommandsAndBindings ()
    {
        // Things this view knows how to do

        // Note - NewLine is only bound to Enter if Multiline is true
        AddCommand (Command.NewLine, ctx => ProcessEnterKey (ctx));

        AddCommand (
                    Command.PageDown,
                    () =>
                    {
                        ProcessPageDown ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageDownExtend,
                    () =>
                    {
                        ProcessPageDownExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUp,
                    () =>
                    {
                        ProcessPageUp ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUpExtend,
                    () =>
                    {
                        ProcessPageUpExtend ();

                        return true;
                    }
                   );

        AddCommand (Command.Down, () => ProcessMoveDown ());

        AddCommand (
                    Command.DownExtend,
                    () =>
                    {
                        ProcessMoveDownExtend ();

                        return true;
                    }
                   );

        AddCommand (Command.Up, () => ProcessMoveUp ());

        AddCommand (
                    Command.UpExtend,
                    () =>
                    {
                        ProcessMoveUpExtend ();

                        return true;
                    }
                   );
        AddCommand (Command.Right, () => ProcessMoveRight ());

        AddCommand (
                    Command.RightExtend,
                    () =>
                    {
                        ProcessMoveRightExtend ();

                        return true;
                    }
                   );
        AddCommand (Command.Left, () => ProcessMoveLeft ());

        AddCommand (
                    Command.LeftExtend,
                    () =>
                    {
                        ProcessMoveLeftExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteCharLeft,
                    () =>
                    {
                        ProcessDeleteCharLeft ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftStart,
                    () =>
                    {
                        ProcessMoveLeftStart ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftStartExtend,
                    () =>
                    {
                        ProcessMoveLeftStartExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteCharRight,
                    () =>
                    {
                        ProcessDeleteCharRight ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEnd,
                    () =>
                    {
                        ProcessMoveEndOfLine ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEndExtend,
                    () =>
                    {
                        ProcessMoveRightEndExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.CutToEndLine,
                    () =>
                    {
                        KillToEndOfLine ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.CutToStartLine,
                    () =>
                    {
                        KillToLeftStart ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Paste,
                    () =>
                    {
                        ProcessPaste ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.ToggleExtend,
                    () =>
                    {
                        ToggleSelecting ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Copy,
                    () =>
                    {
                        ProcessCopy ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Cut,
                    () =>
                    {
                        ProcessCut ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordLeft,
                    () =>
                    {
                        ProcessMoveWordBackward ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordLeftExtend,
                    () =>
                    {
                        ProcessMoveWordBackwardExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordRight,
                    () =>
                    {
                        ProcessMoveWordForward ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordRightExtend,
                    () =>
                    {
                        ProcessMoveWordForwardExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.KillWordForwards,
                    () =>
                    {
                        ProcessKillWordForward ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.KillWordBackwards,
                    () =>
                    {
                        ProcessKillWordBackward ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.End,
                    () =>
                    {
                        MoveBottomEnd ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.EndExtend,
                    () =>
                    {
                        MoveBottomEndExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Start,
                    () =>
                    {
                        MoveTopHome ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.StartExtend,
                    () =>
                    {
                        MoveTopHomeExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.SelectAll,
                    () =>
                    {
                        ProcessSelectAll ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.ToggleOverwrite,
                    () =>
                    {
                        ProcessSetOverwrite ();

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
        AddCommand (Command.NextTabStop, () => ProcessTab ());
        AddCommand (Command.PreviousTabStop, () => ProcessBackTab ());

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
                        ShowContextMenu (null);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Open,
                    () =>
                    {
                        PromptForColors ();

                        return true;
                    });

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

        KeyBindings.Add (Key.K.WithCtrl, Command.CutToEndLine); // kill-to-end

        KeyBindings.Add (Key.Delete.WithCtrl.WithShift, Command.CutToEndLine); // kill-to-end

        KeyBindings.Add (Key.Backspace.WithCtrl.WithShift, Command.CutToStartLine); // kill-to-start

        KeyBindings.Add (Key.Y.WithCtrl, Command.Paste); // Control-y, yank
        KeyBindings.Add (Key.Space.WithCtrl, Command.ToggleExtend);

        KeyBindings.Add (Key.C.WithCtrl, Command.Copy);

        KeyBindings.Add (Key.W.WithCtrl, Command.Cut); // Move to Unix?
        KeyBindings.Add (Key.X.WithCtrl, Command.Cut);

        KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.WordLeft);

        KeyBindings.Add (Key.CursorLeft.WithCtrl.WithShift, Command.WordLeftExtend);

        KeyBindings.Add (Key.CursorRight.WithCtrl, Command.WordRight);

        KeyBindings.Add (Key.CursorRight.WithCtrl.WithShift, Command.WordRightExtend);
        KeyBindings.Add (Key.Delete.WithCtrl, Command.KillWordForwards); // kill-word-forwards
        KeyBindings.Add (Key.Backspace.WithCtrl, Command.KillWordBackwards); // kill-word-backwards

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

#if UNIX_KEY_BINDINGS
        KeyBindings.Add (Key.C.WithAlt, Command.Copy);
        KeyBindings.Add (Key.B.WithAlt, Command.WordLeft);
        KeyBindings.Add (Key.W.WithAlt, Command.Cut);
        KeyBindings.Add (Key.V.WithAlt, Command.PageUp);
        KeyBindings.Add (Key.F.WithAlt, Command.WordRight);
        KeyBindings.Add (Key.K.WithAlt, Command.CutToStartLine); // kill-to-start
#endif
    }


    private void DoNeededAction ()
    {
        if (!NeedsDraw && (IsSelecting || _wrapNeeded || !Used))
        {
            SetNeedsDraw ();
        }

        if (NeedsDraw)
        {
            Adjust ();
        }
        else
        {
            PositionCursor ();
            OnUnwrappedCursorPosition ();
        }
    }

    /// <summary>
    ///     Open a dialog to set the foreground and background colors.
    /// </summary>
    public void PromptForColors ()
    {
        if (!ColorPicker.Prompt (
                                 App!,
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

    private void SetClipboard (string text)
    {
        if (text is { })
        {
            App?.Clipboard?.SetClipboardData (text);
        }
    }

    /// <summary>Copy the selected text to the clipboard contents.</summary>
    public void Copy ()
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
    }

    /// <summary>Cut the selected text to the clipboard contents.</summary>
    public void Cut ()
    {
        SetWrapModel ();
        _copiedText = GetRegion (out _copiedCellsList);
        SetClipboard (_copiedText);

        if (!_isReadOnly)
        {
            ClearRegion ();

            _historyText.Add (
                              [new (GetCurrentLine ())],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();
        OnContentsChanged ();
    }

    /// <summary>Deletes all text.</summary>
    public void DeleteAll ()
    {
        if (Lines == 0)
        {
            return;
        }

        _selectionStartColumn = 0;
        _selectionStartRow = 0;
        MoveBottomEndExtend ();
        DeleteCharLeft ();
        SetNeedsDraw ();
    }

    /// <summary>Deletes all the selected or a single character at left from the position of the cursor.</summary>
    public void DeleteCharLeft ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        if (IsSelecting)
        {
            _historyText.Add (new () { new (GetCurrentLine ()) }, CursorPosition);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        if (DeleteTextBackwards ())
        {
            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();
    }

    /// <summary>Deletes all the selected or a single character at right from the position of the cursor.</summary>
    public void DeleteCharRight ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        if (IsSelecting)
        {
            _historyText.Add (new () { new (GetCurrentLine ()) }, CursorPosition);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        if (DeleteTextForwards ())
        {
            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();
    }

    /// <summary>Find the next text based on the match case with the option to replace it.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="gaveFullTurn"><c>true</c>If all the text was forward searched.<c>false</c>otherwise.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <param name="replace"><c>true</c>If is replacing.<c>false</c>otherwise.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool FindNextText (
        string textToFind,
        out bool gaveFullTurn,
        bool matchCase = false,
        bool matchWholeWord = false,
        string? textToReplace = null,
        bool replace = false
    )
    {
        if (_model.Count == 0)
        {
            gaveFullTurn = false;

            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos =
            _model.FindNextText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

        return SetFoundText (textToFind, foundPos, textToReplace, replace);
    }

    /// <summary>Find the previous text based on the match case with the option to replace it.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="gaveFullTurn"><c>true</c>If all the text was backward searched.<c>false</c>otherwise.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <param name="replace"><c>true</c>If the text was found.<c>false</c>otherwise.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool FindPreviousText (
        string textToFind,
        out bool gaveFullTurn,
        bool matchCase = false,
        bool matchWholeWord = false,
        string? textToReplace = null,
        bool replace = false
    )
    {
        if (_model.Count == 0)
        {
            gaveFullTurn = false;

            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos =
            _model.FindPreviousText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

        return SetFoundText (textToFind, foundPos, textToReplace, replace);
    }

    /// <summary>Reset the flag to stop continuous find.</summary>
    public void FindTextChanged () { _continuousFind = false; }

    /// <summary>Will scroll the <see cref="TextView"/> to the last line and position the cursor there.</summary>
    public void MoveEnd ()
    {
        CurrentRow = _model.Count - 1;
        List<Cell> line = GetCurrentLine ();
        CurrentColumn = line.Count;
        TrackColumn ();
        DoNeededAction ();
    }

    /// <summary>Will scroll the <see cref="TextView"/> to the first line and position the cursor there.</summary>
    public void MoveHome ()
    {
        CurrentRow = 0;
        _topRow = 0;
        CurrentColumn = 0;
        _leftColumn = 0;
        TrackColumn ();
        DoNeededAction ();
    }

    /// <summary>Paste the clipboard contents into the current selected position.</summary>
    public void Paste ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();
        string? contents = App?.Clipboard?.GetClipboardData ();

        if (_copyWithoutSelection && contents!.FirstOrDefault (x => x is '\n' or '\r') == 0)
        {
            List<Cell> runeList = contents is null ? [] : Cell.ToCellList (contents);
            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add ([new (currentLine)], CursorPosition);

            List<List<Cell>> addedLine = [new (currentLine), runeList];

            _historyText.Add (
                              [.. addedLine],
                              CursorPosition,
                              TextEditingLineStatus.Added
                             );

            _model.AddLine (CurrentRow, runeList);
            CurrentRow++;

            _historyText.Add (
                              [new (GetCurrentLine ())],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            SetNeedsDraw ();
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
                _historyText.ReplaceLast (
                                          [new (GetCurrentLine ())],
                                          CursorPosition,
                                          TextEditingLineStatus.Original
                                         );
            }

            SetNeedsDraw ();
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();
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

    /// <summary>
    ///     Will scroll the <see cref="TextView"/> to display the specified row at the top if <paramref name="isRow"/> is
    ///     true or will scroll the <see cref="TextView"/> to display the specified column at the left if
    ///     <paramref name="isRow"/> is false.
    /// </summary>
    /// <param name="idx">
    ///     Row that should be displayed at the top or Column that should be displayed at the left, if the value
    ///     is negative it will be reset to zero
    /// </param>
    /// <param name="isRow">If true (default) the <paramref name="idx"/> is a row, column otherwise.</param>
    public void ScrollTo (int idx, bool isRow = true)
    {
        if (idx < 0)
        {
            idx = 0;
        }

        if (isRow)
        {
            _topRow = Math.Max (idx > _model.Count - 1 ? _model.Count - 1 : idx, 0);
        }
        else if (!_wordWrap)
        {
            int maxlength =
                _model.GetMaxVisibleLine (_topRow, _topRow + Viewport.Height, TabWidth);
            _leftColumn = Math.Max (!_wordWrap && idx > maxlength - 1 ? maxlength - 1 : idx, 0);
        }

        SetNeedsDraw ();
    }

    /// <summary>Select all text.</summary>
    public void SelectAll ()
    {
        if (_model.Count == 0)
        {
            return;
        }

        StartSelecting ();
        _selectionStartColumn = 0;
        _selectionStartRow = 0;
        CurrentColumn = _model.GetLine (_model.Count - 1).Count;
        CurrentRow = _model.Count - 1;
        SetNeedsDraw ();
    }

    /// <summary>Undoes the latest changes.</summary>
    public void Undo ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Undo ();
    }

    private bool DeleteTextBackwards ()
    {
        SetWrapModel ();

        if (CurrentColumn > 0)
        {
            // Delete backwards 
            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add (new () { new (currentLine) }, CursorPosition);

            currentLine.RemoveAt (CurrentColumn - 1);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn--;

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (CurrentColumn < _leftColumn)
            {
                _leftColumn--;
                SetNeedsDraw ();
            }
            else
            {
                // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
                //SetNeedsDraw (new (0, currentRow - topRow, 1, Viewport.Width));
                SetNeedsDraw ();
            }
        }
        else
        {
            // Merges the current line with the previous one.
            if (CurrentRow == 0)
            {
                return true;
            }

            int prowIdx = CurrentRow - 1;
            List<Cell> prevRow = _model.GetLine (prowIdx);

            _historyText.Add (new () { new (prevRow) }, CursorPosition);

            List<List<Cell>> removedLines = new () { new (prevRow) };

            removedLines.Add (new (GetCurrentLine ()));

            _historyText.Add (
                              removedLines,
                              new (CurrentColumn, prowIdx),
                              TextEditingLineStatus.Removed
                             );

            int prevCount = prevRow.Count;
            _model.GetLine (prowIdx).AddRange (GetCurrentLine ());
            _model.RemoveLine (CurrentRow);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentRow--;

            _historyText.Add (
                              new () { GetCurrentLine () },
                              new (CurrentColumn, prowIdx),
                              TextEditingLineStatus.Replaced
                             );

            CurrentColumn = prevCount;
            SetNeedsDraw ();
        }

        UpdateWrapModel ();

        return false;
    }

    private bool DeleteTextForwards ()
    {
        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        if (CurrentColumn == currentLine.Count)
        {
            if (CurrentRow + 1 == _model.Count)
            {
                UpdateWrapModel ();

                return true;
            }

            _historyText.Add (new () { new (currentLine) }, CursorPosition);

            List<List<Cell>> removedLines = new () { new (currentLine) };

            List<Cell> nextLine = _model.GetLine (CurrentRow + 1);

            removedLines.Add (new (nextLine));

            _historyText.Add (removedLines, CursorPosition, TextEditingLineStatus.Removed);

            currentLine.AddRange (nextLine);
            _model.RemoveLine (CurrentRow + 1);

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, CurrentRow - _topRow + 1));
        }
        else
        {
            _historyText.Add ([[.. currentLine]], CursorPosition);

            currentLine.RemoveAt (CurrentColumn);

            _historyText.Add (
                              [[.. currentLine]],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            DoSetNeedsDraw (
                            new (
                                 CurrentColumn - _leftColumn,
                                 CurrentRow - _topRow,
                                 Viewport.Width,
                                 Math.Max (CurrentRow - _topRow + 1, 0)
                                )
                           );
        }

        UpdateWrapModel ();

        return false;
    }

    private void KillToEndOfLine ()
    {
        if (_isReadOnly)
        {
            return;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == currentLine.Count)
        {
            UpdateWrapModel ();

            DeleteTextForwards ();

            return;
        }

        _historyText.Add (new () { new (currentLine) }, CursorPosition);

        if (currentLine.Count == 0)
        {
            if (CurrentRow < _model.Count - 1)
            {
                List<List<Cell>> removedLines = new () { new (currentLine) };

                _model.RemoveLine (CurrentRow);

                removedLines.Add (new (GetCurrentLine ()));

                _historyText.Add (
                                  new (removedLines),
                                  CursorPosition,
                                  TextEditingLineStatus.Removed
                                 );
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
        }

        _historyText.Add (
                          [[.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));

        _lastWasKill = setLastWasKill;
        DoNeededAction ();
    }

    private void KillToLeftStart ()
    {
        if (_isReadOnly)
        {
            return;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == 0)
        {
            UpdateWrapModel ();

            DeleteTextBackwards ();

            return;
        }

        _historyText.Add ([[.. currentLine]], CursorPosition);

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

                List<List<Cell>> removedLine =
                [
                    [..currentLine],
                    []
                ];

                _historyText.Add (
                                  [.. removedLine],
                                  CursorPosition,
                                  TextEditingLineStatus.Removed
                                 );

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

        _historyText.Add (
                          [[.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));

        _lastWasKill = setLastWasKill;
        DoNeededAction ();
    }

    private void KillWordBackward ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([[.. GetCurrentLine ()]], CursorPosition);

        if (CurrentColumn == 0)
        {
            DeleteTextBackwards ();

            _historyText.ReplaceLast (
                                      [[.. GetCurrentLine ()]],
                                      CursorPosition,
                                      TextEditingLineStatus.Replaced
                                     );

            UpdateWrapModel ();

            return;
        }

        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            int restCount = CurrentColumn - newPos.Value.col;
            currentLine.RemoveRange (newPos.Value.col, restCount);

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
            }
            else
            {
                while (CurrentRow != newPos.Value.row)
                {
                    restCount = currentLine.Count;
                    currentLine.RemoveRange (0, restCount);

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

        _historyText.Add (
                          [[.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));
        DoNeededAction ();
    }

    private void KillWordForward ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([[.. GetCurrentLine ()]], CursorPosition);

        if (currentLine.Count == 0 || CurrentColumn == currentLine.Count)
        {
            DeleteTextForwards ();

            _historyText.ReplaceLast (
                                      [[.. GetCurrentLine ()]],
                                      CursorPosition,
                                      TextEditingLineStatus.Replaced
                                     );

            UpdateWrapModel ();

            return;
        }

        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);
        var restCount = 0;

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            restCount = newPos.Value.col - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
        }
        else if (newPos.HasValue)
        {
            restCount = currentLine.Count - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
        }

        if (_wordWrap)
        {
            _wrapNeeded = true;
        }

        _historyText.Add (
                          [[.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));
        DoNeededAction ();
    }

    private void MoveBottomEnd ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveEnd ();
    }

    private void MoveBottomEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveEnd ();
    }

    private bool MoveDown ()
    {
        if (CurrentRow + 1 < _model.Count)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow++;

            if (CurrentRow >= _topRow + Viewport.Height)
            {
                _topRow++;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }
        else if (CurrentRow > Viewport.Height)
        {
            Adjust ();
        }
        else
        {
            return false;
        }

        DoNeededAction ();

        return true;
    }

    private void MoveEndOfLine ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        CurrentColumn = currentLine.Count;
        DoNeededAction ();
    }

    private bool MoveLeft ()
    {
        if (CurrentColumn > 0)
        {
            CurrentColumn--;
        }
        else
        {
            if (CurrentRow > 0)
            {
                CurrentRow--;

                if (CurrentRow < _topRow)
                {
                    _topRow--;
                    SetNeedsDraw ();
                }

                List<Cell> currentLine = GetCurrentLine ();
                CurrentColumn = Math.Max (currentLine.Count - (ReadOnly ? 1 : 0), 0);
            }
            else
            {
                return false;
            }
        }

        DoNeededAction ();

        return true;
    }

    private void MovePageDown ()
    {
        int nPageDnShift = Viewport.Height - 1;

        if (CurrentRow >= 0 && CurrentRow < _model.Count)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow = CurrentRow + nPageDnShift > _model.Count
                             ? _model.Count > 0 ? _model.Count - 1 : 0
                             : CurrentRow + nPageDnShift;

            if (_topRow < CurrentRow - nPageDnShift)
            {
                _topRow = CurrentRow >= _model.Count
                              ? CurrentRow - nPageDnShift
                              : _topRow + nPageDnShift;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }

        DoNeededAction ();
    }

    private void MovePageUp ()
    {
        int nPageUpShift = Viewport.Height - 1;

        if (CurrentRow > 0)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow = CurrentRow - nPageUpShift < 0 ? 0 : CurrentRow - nPageUpShift;

            if (CurrentRow < _topRow)
            {
                _topRow = _topRow - nPageUpShift < 0 ? 0 : _topRow - nPageUpShift;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }

        DoNeededAction ();
    }

    private bool MoveRight ()
    {
        List<Cell> currentLine = GetCurrentLine ();

        if (CurrentColumn < currentLine.Count)
        {
            CurrentColumn++;
        }
        else
        {
            if (CurrentRow + 1 < _model.Count)
            {
                CurrentRow++;
                CurrentColumn = 0;

                if (CurrentRow >= _topRow + Viewport.Height)
                {
                    _topRow++;
                    SetNeedsDraw ();
                }
            }
            else
            {
                return false;
            }
        }

        DoNeededAction ();

        return true;
    }

    private void MoveLeftStart ()
    {
        if (_leftColumn > 0)
        {
            SetNeedsDraw ();
        }

        CurrentColumn = 0;
        _leftColumn = 0;
        DoNeededAction ();
    }

    private void MoveTopHome ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveHome ();
    }

    private void MoveTopHomeExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveHome ();
    }

    private bool MoveUp ()
    {
        if (CurrentRow > 0)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow--;

            if (CurrentRow < _topRow)
            {
                _topRow--;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }
        else
        {
            return false;
        }

        DoNeededAction ();

        return true;
    }

    private void MoveWordBackward ()
    {
        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        DoNeededAction ();
    }

    private void MoveWordForward ()
    {
        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        DoNeededAction ();
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
    private void AppendClipboard (string text) { App?.Clipboard?.SetClipboardData (App?.Clipboard?.GetClipboardData () + text); }

    private void ProcessCopy ()
    {
        ResetColumnTrack ();
        Copy ();
    }

    private void ProcessCut ()
    {
        ResetColumnTrack ();
        Cut ();
    }

    private void ProcessDeleteCharLeft ()
    {
        ResetColumnTrack ();
        DeleteCharLeft ();
    }

    private void ProcessDeleteCharRight ()
    {
        ResetColumnTrack ();
        DeleteCharRight ();
    }

    private void ProcessKillWordBackward ()
    {
        ResetColumnTrack ();
        KillWordBackward ();
    }

    private void ProcessKillWordForward ()
    {
        ResetColumnTrack ();
        StopSelecting ();
        KillWordForward ();
    }

    private bool ProcessMoveDown ()
    {
        ResetContinuousFindTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveDown ();
    }

    private void ProcessMoveDownExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveDown ();
    }

    private void ProcessMoveEndOfLine ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveEndOfLine ();
    }

    private void ProcessMoveRightEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveEndOfLine ();
    }

    private bool ProcessMoveLeft ()
    {
        // if the user presses Left (without any control keys) and they are at the start of the text
        if (CurrentColumn == 0 && CurrentRow == 0)
        {
            if (IsSelecting)
            {
                StopSelecting ();

                return true;
            }

            // do not respond (this lets the key press fall through to navigation system - which usually changes focus backward)
            return false;
        }

        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveLeft ();

        return true;
    }

    private void ProcessMoveLeftExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveLeft ();
    }

    private bool ProcessMoveRight ()
    {
        // if the user presses Right (without any control keys)
        // determine where the last cursor position in the text is
        int lastRow = _model.Count - 1;
        int lastCol = _model.GetLine (lastRow).Count;

        // if they are at the very end of all the text do not respond (this lets the key press fall through to navigation system - which usually changes focus forward)
        if (CurrentColumn == lastCol && CurrentRow == lastRow)
        {
            // Unless they have text selected
            if (IsSelecting)
            {
                // In which case clear
                StopSelecting ();

                return true;
            }

            return false;
        }

        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveRight ();

        return true;
    }

    private void ProcessMoveRightExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveRight ();
    }

    private void ProcessMoveLeftStart ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveLeftStart ();
    }

    private void ProcessMoveLeftStartExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveLeftStart ();
    }

    private bool ProcessMoveUp ()
    {
        ResetContinuousFindTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveUp ();
    }

    private void ProcessMoveUpExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveUp ();
    }

    private void ProcessMoveWordBackward ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveWordBackward ();
    }

    private void ProcessMoveWordBackwardExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveWordBackward ();
    }

    private void ProcessMoveWordForward ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveWordForward ();
    }

    private void ProcessMoveWordForwardExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveWordForward ();
    }

    private void ProcessPageDown ()
    {
        ResetColumnTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MovePageDown ();
    }

    private void ProcessPageDownExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MovePageDown ();
    }

    private void ProcessPageUp ()
    {
        ResetColumnTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MovePageUp ();
    }

    private void ProcessPageUpExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MovePageUp ();
    }

    private void ProcessPaste ()
    {
        ResetColumnTrack ();

        if (_isReadOnly)
        {
            return;
        }

        Paste ();
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

    private void ProcessSelectAll ()
    {
        ResetColumnTrack ();
        SelectAll ();
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
}
