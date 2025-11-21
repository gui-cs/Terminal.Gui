namespace Terminal.Gui.Views;

public partial class TextView
{
    private void SetClipboard (string text)
    {
        if (text is { })
        {
            Clipboard.Contents = text;
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

    /// <summary>Paste the clipboard contents into the current selected position.</summary>
    public void Paste ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();
        string? contents = Clipboard.Contents;

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

    private void ProcessPaste ()
    {
        ResetColumnTrack ();

        if (_isReadOnly)
        {
            return;
        }

        Paste ();
    }


    private void AppendClipboard (string text) { Clipboard.Contents += text; }
}
