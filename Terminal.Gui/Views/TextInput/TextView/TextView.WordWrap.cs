using System.Runtime.CompilerServices;

namespace Terminal.Gui.Views;

public partial class TextView
{
    private bool _wordWrap;
    private WordWrapManager? _wrapManager;
    private bool _wrapNeeded;

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

            SetNeedsDraw ();
        }
    }



    /// <summary>Invoke the <see cref="UnwrappedCursorPosition"/> event with the unwrapped <see cref="InsertionPoint"/>.</summary>
    public virtual void OnUnwrappedCursorPosition (int? cRow = null, int? cCol = null)
    {
        int? row = cRow ?? CurrentRow;
        int? col = cCol ?? CurrentColumn;

        if (cRow is null && cCol is null && _wordWrap)
        {
            row = _wrapManager!.GetModelLineFromWrappedLines (CurrentRow);
            col = _wrapManager.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
        }

        UnwrappedCursorPosition?.Invoke (this, new (col.Value, row.Value));
    }

    /// <summary>Invoked with the unwrapped <see cref="InsertionPoint"/>.</summary>
    public event EventHandler<Point>? UnwrappedCursorPosition;

    private (int Row, int Col) GetUnwrappedPosition (int line, int col)
    {
        if (WordWrap)
        {
            return new ValueTuple<int, int> (
                                             _wrapManager!.GetModelLineFromWrappedLines (line),
                                             _wrapManager.GetModelColFromWrappedLines (line, col)
                                            );
        }

        return new ValueTuple<int, int> (line, col);
    }

    /// <summary>Restore from original model.</summary>
    private void SetWrapModel ([CallerMemberName] string? caller = null)
    {
        if (_currentCaller is { })
        {
            return;
        }

        if (_wordWrap)
        {
            _currentCaller = caller;

            CurrentColumn = _wrapManager!.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
            CurrentRow = _wrapManager.GetModelLineFromWrappedLines (CurrentRow);

            _selectionStartColumn =
                _wrapManager.GetModelColFromWrappedLines (_selectionStartRow, _selectionStartColumn);
            _selectionStartRow = _wrapManager.GetModelLineFromWrappedLines (_selectionStartRow);
            _model = _wrapManager.Model;
        }
    }

    /// <summary>Update the original model.</summary>
    private void UpdateWrapModel ([CallerMemberName] string? caller = null)
    {
        if (_currentCaller is { } && _currentCaller != caller)
        {
            return;
        }

        if (_wordWrap)
        {
            _currentCaller = null;

            _wrapManager!.UpdateModel (
                                       _model,
                                       out int nRow,
                                       out int nCol,
                                       out int nStartRow,
                                       out int nStartCol,
                                       CurrentRow,
                                       CurrentColumn,
                                       _selectionStartRow,
                                       _selectionStartColumn,
                                       true
                                      );
            CurrentRow = nRow;
            CurrentColumn = nCol;
            _selectionStartRow = nStartRow;
            _selectionStartColumn = nStartCol;
            _wrapNeeded = true;

            SetNeedsDraw ();
        }

        if (_currentCaller is { })
        {
            throw new InvalidOperationException (
                                                 $"WordWrap settings was changed after the {_currentCaller} call."
                                                );
        }
    }

    private void WrapTextModel ()
    {
        if (_wordWrap && _wrapManager is { })
        {
            _model = _wrapManager.WrapModel (
                                             Math.Max (Viewport.Width - (ReadOnly ? 0 : 1), 0), // For the cursor on the last column of a line
                                             out int nRow,
                                             out int nCol,
                                             out int nStartRow,
                                             out int nStartCol,
                                             CurrentRow,
                                             CurrentColumn,
                                             _selectionStartRow,
                                             _selectionStartColumn,
                                             _tabWidth
                                            );
            CurrentRow = nRow;
            CurrentColumn = nCol;
            _selectionStartRow = nStartRow;
            _selectionStartColumn = nStartCol;
            SetNeedsDraw ();
        }
    }
}
