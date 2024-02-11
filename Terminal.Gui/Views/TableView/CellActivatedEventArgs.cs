﻿namespace Terminal.Gui;

/// <summary>Defines the event arguments for <see cref="TableView.CellActivated"/> event</summary>
public class CellActivatedEventArgs : EventArgs
{
    /// <summary>Creates a new instance of arguments describing a cell being activated in <see cref="TableView"/></summary>
    /// <param name="t"></param>
    /// <param name="col"></param>
    /// <param name="row"></param>
    public CellActivatedEventArgs (ITableSource t, int col, int row)
    {
        Table = t;
        Col = col;
        Row = row;
    }

    /// <summary>The column index of the <see cref="Table"/> cell that is being activated</summary>
    /// <value></value>
    public int Col { get; }

    /// <summary>The row index of the <see cref="Table"/> cell that is being activated</summary>
    /// <value></value>
    public int Row { get; }

    /// <summary>
    ///     The current table to which the new indexes refer.  May be null e.g. if selection change is the result of clearing the table from the view
    /// </summary>
    /// <value></value>
    public ITableSource Table { get; }
}
