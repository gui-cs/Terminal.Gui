namespace Terminal.Gui.Views;

public partial class TextView
{
    internal void ApplyCellsAttribute (Attribute attribute)
    {
        if (!ReadOnly && SelectedLength > 0)
        {
            int startRow = Math.Min (SelectionStartRow, CurrentRow);
            int endRow = Math.Max (CurrentRow, SelectionStartRow);
            int startCol = SelectionStartRow <= CurrentRow ? SelectionStartColumn : CurrentColumn;
            int endCol = CurrentRow >= SelectionStartRow ? CurrentColumn : SelectionStartColumn;
            List<List<Cell>> selectedCellsOriginal = [];
            List<List<Cell>> selectedCellsChanged = [];

            for (int r = startRow; r <= endRow; r++)
            {
                List<Cell> line = GetLine (r);

                selectedCellsOriginal.Add ([.. line]);

                for (int c = r == startRow ? startCol : 0;
                     c < (r == endRow ? endCol : line.Count);
                     c++)
                {
                    Cell cell = line [c]; // Copy value to a new variable
                    cell.Attribute = attribute; // Modify the copy
                    line [c] = cell; // Assign the modified copy back
                }

                selectedCellsChanged.Add ([.. GetLine (r)]);
            }

            GetSelectedRegion ();
            IsSelecting = false;

            _historyText.Add (
                              [.. selectedCellsOriginal],
                              new Point (startCol, startRow)
                             );

            _historyText.Add (
                              [.. selectedCellsChanged],
                              new Point (startCol, startRow),
                              TextEditingLineStatus.Attribute
                             );
        }
    }

    private Attribute? GetSelectedCellAttribute ()
    {
        List<Cell> line;

        if (SelectedLength > 0)
        {
            line = GetLine (SelectionStartRow);

            if (line [Math.Min (SelectionStartColumn, line.Count - 1)].Attribute is { } attributeSel)
            {
                return new (attributeSel);
            }

            return GetAttributeForRole (VisualRole.Active);
        }

        line = GetCurrentLine ();

        if (line [Math.Min (CurrentColumn, line.Count - 1)].Attribute is { } attribute)
        {
            return new (attribute);
        }

        return GetAttributeForRole (VisualRole.Active);
    }

    /// <summary>Invoked when the normal color is drawn.</summary>
    public event EventHandler<CellEventArgs>? DrawNormalColor;

    /// <summary>Invoked when the ready only color is drawn.</summary>
    public event EventHandler<CellEventArgs>? DrawReadOnlyColor;

    /// <summary>Invoked when the selection color is drawn.</summary>
    public event EventHandler<CellEventArgs>? DrawSelectionColor;

    /// <summary>
    ///     Invoked when the used color is drawn. The Used Color is used to indicate if the <see cref="Key.InsertChar"/>
    ///     was pressed and enabled.
    /// </summary>
    public event EventHandler<CellEventArgs>? DrawUsedColor;

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        _isDrawing = true;

        SetAttributeForRole (Enabled ? VisualRole.Editable : VisualRole.Disabled);

        (int width, int height) offB = OffSetBackground ();
        int right = Viewport.Width + offB.width;
        int bottom = Viewport.Height + offB.height;
        var row = 0;

        for (int idxRow = _topRow; idxRow < _model.Count; idxRow++)
        {
            List<Cell> line = _model.GetLine (idxRow);
            int lineRuneCount = line.Count;
            var col = 0;

            Move (0, row);

            for (int idxCol = _leftColumn; idxCol < lineRuneCount; idxCol++)
            {
                string text = idxCol >= lineRuneCount ? " " : line [idxCol].Grapheme;
                int cols = text.GetColumns (false);

                if (idxCol < line.Count && IsSelecting && PointInSelection (idxCol, idxRow))
                {
                    OnDrawSelectionColor (line, idxCol, idxRow);
                }
                else if (idxCol == CurrentColumn && idxRow == CurrentRow && !IsSelecting && !Used && HasFocus && idxCol < lineRuneCount)
                {
                    OnDrawUsedColor (line, idxCol, idxRow);
                }
                else if (ReadOnly)
                {
                    OnDrawReadOnlyColor (line, idxCol, idxRow);
                }
                else
                {
                    OnDrawNormalColor (line, idxCol, idxRow);
                }

                if (text == "\t")
                {
                    cols += TabWidth + 1;

                    if (col + cols > right)
                    {
                        cols = right - col;
                    }

                    for (var i = 0; i < cols; i++)
                    {
                        if (col + i < right)
                        {
                            AddRune (col + i, row, (Rune)' ');
                        }
                    }
                }
                else
                {
                    AddStr (col, row, text);

                    // Ensures that cols less than 0 to be 1 because it will be converted to a printable rune
                    cols = Math.Max (cols, 1);
                }

                if (!TextModel.SetCol (ref col, Viewport.Right, cols))
                {
                    break;
                }

                if (idxCol + 1 < lineRuneCount && col + line [idxCol + 1].Grapheme.GetColumns () > right)
                {
                    break;
                }
            }

            if (col < right)
            {
                SetAttributeForRole (ReadOnly ? VisualRole.ReadOnly : VisualRole.Editable);
                ClearRegion (col, row, right, row + 1);
            }

            row++;
        }

        if (row < bottom)
        {
            SetAttributeForRole (ReadOnly ? VisualRole.ReadOnly : VisualRole.Editable);
            ClearRegion (Viewport.Left, row, right, bottom);
        }

        _isDrawing = false;

        return false;
    }

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute"/> Defaults to <see cref="Scheme.Normal"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawNormalColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawNormalColor?.Invoke (this, ev);

        if (line [idxCol].Attribute is { })
        {
            Attribute? attribute = line [idxCol].Attribute;
            SetAttribute ((Attribute)attribute!);
        }
        else
        {
            SetAttribute (GetAttributeForRole (VisualRole.Normal));
        }
    }

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute(Attribute)"/> Defaults to <see cref="Scheme.Focus"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// ///
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawReadOnlyColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawReadOnlyColor?.Invoke (this, ev);

        Attribute? cellAttribute = line [idxCol].Attribute is { } ? line [idxCol].Attribute : GetAttributeForRole (VisualRole.ReadOnly);

        if (cellAttribute!.Value.Foreground == cellAttribute.Value.Background)
        {
            SetAttribute (new (cellAttribute.Value.Foreground, cellAttribute.Value.Background, cellAttribute.Value.Style));
        }
        else
        {
            SetAttributeForRole (VisualRole.ReadOnly);
        }
    }

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute(Attribute)"/> Defaults to <see cref="Scheme.Focus"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// ///
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawSelectionColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawSelectionColor?.Invoke (this, ev);

        if (line [idxCol].Attribute is { })
        {
            Attribute? attribute = line [idxCol].Attribute;
            Attribute? active = GetAttributeForRole (VisualRole.Active);
            SetAttribute (new (active!.Value.Foreground, active.Value.Background, attribute!.Value.Style));
        }
        else
        {
            SetAttributeForRole (VisualRole.Active);
        }
    }

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute(Attribute)"/> Defaults to <see cref="Scheme.HotFocus"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// ///
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawUsedColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawUsedColor?.Invoke (this, ev);

        if (line [idxCol].Attribute is { })
        {
            Attribute? attribute = line [idxCol].Attribute;
            SetValidUsedColor (attribute!);
        }
        else
        {
            SetValidUsedColor (GetAttributeForRole (VisualRole.Focus));
        }
    }

    private void DoSetNeedsDraw (Rectangle rect)
    {
        if (_wrapNeeded)
        {
            SetNeedsDraw ();
        }
        else
        {
            // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
            //SetNeedsDraw (rect);
            SetNeedsDraw ();
        }
    }

    private Attribute? GetSelectedAttribute (int row, int col)
    {
        if (!InheritsPreviousAttribute || (Lines == 1 && GetLine (Lines).Count == 0))
        {
            return null;
        }

        List<Cell> line = GetLine (row);
        int foundRow = row;

        while (line.Count == 0)
        {
            if (foundRow == 0 && line.Count == 0)
            {
                return null;
            }

            foundRow--;
            line = GetLine (foundRow);
        }

        int foundCol = foundRow < row ? line.Count - 1 : Math.Min (col, line.Count - 1);

        Cell cell = line [foundCol];

        return cell.Attribute;
    }

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role == VisualRole.Normal)
        {
            currentAttribute = GetAttributeForRole (VisualRole.Editable);

            return true;
        }

        return base.OnGettingAttributeForRole (role, ref currentAttribute);
    }
}
