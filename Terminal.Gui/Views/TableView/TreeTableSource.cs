using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Terminal.Gui;

public class TreeTableSource<T> : ITableSource where T : class {
	List<TreeView<T>> trees = new List<TreeView<T>> ();

	private string[] cols;
	private Dictionary<string, Func<T, object>> lamdas;
	private TableView tableView;

	public TreeTableSource (TableView table, string firstColumnName, Dictionary<string, Func<T, object>> subsequentColumns)
	{
		this.tableView = table;
		this.tableView.KeyPress += Table_KeyPress;

		var colList = subsequentColumns.Keys.ToList ();
		colList.Insert (0, firstColumnName);

		this.cols = colList.ToArray ();


		this.lamdas = subsequentColumns;
	}

	private string GetColumnZeroRepresentationFromTree(int row)
	{
		var tree = RowToTree (row, out int lineInTree);

		if(tree == null) {
			return string.Empty;
		}

		var branch = tree.BuildLineMap ().ElementAt (lineInTree);

		// Everything on line before the expansion run and branch text
		Rune [] prefix = branch.GetLinePrefix (Application.Driver).ToArray ();
		Rune expansion = branch.GetExpandableSymbol (Application.Driver);
		string lineBody = tree.AspectGetter (branch.Model) ?? "";

		var sb = new StringBuilder ();

		foreach(var p in prefix) {
			sb.Append (p);
		}
		
		sb.Append (expansion);
		sb.Append (lineBody);

		return sb.ToString ();
	}

	private void Table_KeyPress (object sender, KeyEventEventArgs e)
	{
		if (tableView.SelectedColumn != 0) {
			return;
		}

		var tree = RowToTree (tableView.SelectedRow, out var lineInTree);

		if (tree == null) {
			return;
		}

		var obj = tree.GetObjectOnRow (lineInTree);

		if (obj == null) {
			return;
		}

		if (e.KeyEvent.Key == Key.CursorLeft) {
			if (tree.IsExpanded (obj)) {
				tree.Collapse (obj);
				e.Handled = true;
			}
		}
		if (e.KeyEvent.Key == Key.CursorRight) {
			if (tree.CanExpand (obj) && !tree.IsExpanded (obj)) {
				tree.Expand (obj);
				e.Handled = true;
			}
		}

		if (e.Handled) {
			tree.InvalidateLineMap ();
			tableView.SetNeedsDisplay ();
		}
	}

	public object this [int row, int col] => 
		col == 0 ? GetColumnZeroRepresentationFromTree(row):
		this.lamdas [ColumnNames [col]] (RowToObject (row));

	private T RowToObject (int row)
	{
		// Find which tree is rendering into this row
		var tree = RowToTree (row, out var lineInTree);

		return tree?.BuildLineMap ().ElementAt (lineInTree).Model;
	}

	private TreeView<T> RowToTree (int row, out int lineInTree)
	{
		lineInTree = row;
		foreach (var tree in trees) {
			var map = tree.BuildLineMap ();
			if (map.Count > lineInTree)
				return tree;
			else
				lineInTree -= map.Count;
		}
		return null;
	}

	public int Rows => trees.Sum (t => t.BuildLineMap ().Count);

	public int Columns => this.lamdas.Count + 1;

	public string [] ColumnNames => cols;

	public void AddRow (TreeView<T> tree)
	{
		trees.Add (tree);
	}

}