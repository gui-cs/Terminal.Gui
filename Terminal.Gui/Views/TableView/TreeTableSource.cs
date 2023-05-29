using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Terminal.Gui;

public class TreeTableSource<T> : ITableSource where T : class
{
    List<TreeView<T>> trees = new List<TreeView<T>>();

    private string [] cols;
	private Dictionary<string, Func<T, object>> lamdas;
    private TableView tableView;

    public TreeTableSource(TableView table, Dictionary<string, Func<T, object>> columnDefinitions)
    {
        this.tableView = table;
        this.tableView .KeyPress += Table_KeyPress;
        this.cols = columnDefinitions.Keys.ToArray ();
        this.lamdas = columnDefinitions;
    }

	private void Table_KeyPress (object sender, KeyEventEventArgs e)
	{
        if(tableView.SelectedColumn != 0)
        {
            return;
        }

        var tree = RowToTree(tableView.SelectedRow, out var lineInTree);
        
        if(tree == null)
        {
            return;
        }
    
        var obj = tree.GetObjectOnRow(lineInTree);

        if(obj == null)
        {
            return;
        }

        if(e.KeyEvent.Key == Key.CursorLeft)
        {
            if(tree.IsExpanded(obj))
            {
                tree.Collapse(obj);
                e.Handled = true;
            }
        }
        if(e.KeyEvent.Key == Key.CursorRight)
        {
            if(tree.CanExpand(obj) && !tree.IsExpanded(obj))
            {
                tree.Expand(obj);
                e.Handled = true;
            }
        }
        
        if(e.Handled)
        {
            tree.InvalidateLineMap();
            tableView.SetNeedsDisplay();
        }
	}

	public object this [int row, int col] => this.lamdas [ColumnNames [col]] (RowToObject(row));

	private T RowToObject (int row)
	{
        // Find which tree is rendering into this row
        var tree = RowToTree(row, out var lineInTree);

        return tree?.BuildLineMap().ElementAt(lineInTree).Model;
	}

    private TreeView<T> RowToTree (int row, out int lineInTree)
	{
        lineInTree = row;
		foreach(var tree in trees)
        {
            var map = tree.BuildLineMap();
            if(map.Count > lineInTree)
                return tree;
            else
            	lineInTree  -= map.Count;
        }
        return null;
	}

	public int Rows => trees.Sum(t=>t.BuildLineMap().Count);

	public int Columns => this.lamdas.Count;

	public string [] ColumnNames => cols;

	public void AddRow(TreeView<T> tree)
    {
        trees.Add(tree);
    }

}