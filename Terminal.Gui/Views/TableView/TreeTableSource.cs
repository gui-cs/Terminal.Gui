using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Terminal.Gui;

/// <summary>
/// An <see cref="ITableSource"/> with expandable rows.
/// </summary>
/// <typeparam name="T"></typeparam>
public class TreeTableSource<T> : IEnumerableTableSource<T>, IDisposable where T : class {
	private TreeView<T> tree;

	private string [] cols;
	private Dictionary<string, Func<T, object>> lamdas;
	private TableView tableView;

	/// <summary>
	/// Creates a new instance of <see cref="TreeTableSource{T}"/> presenting the given
	/// <paramref name="tree"/>. This source should only be used with <paramref name="table"/>.
	/// </summary>
	/// <param name="table">The table this source will provide data for.</param>
	/// <param name="firstColumnName">Column name to use for the first column of the table (where
	/// the tree branches/leaves will be rendered.</param>
	/// <param name="tree">The tree data to render. This should be a new view and not used
	/// elsewhere (e.g. via <see cref="View.Add(View)"/>).</param>
	/// <param name="subsequentColumns">
	/// Getter methods for each additional property you want to present in the table. For example:
	/// <code>
	/// new () {
	///    { "Colname1", (t)=>t.SomeField},
	///    { "Colname2", (t)=>t.SomeOtherField}
	///}
	/// </code></param>
	public TreeTableSource (TableView table, string firstColumnName, TreeView<T> tree, Dictionary<string, Func<T, object>> subsequentColumns)
	{
		this.tableView = table;
		this.tree = tree;
		this.tableView.KeyPress += Table_KeyPress;
		this.tableView.MouseClick += Table_MouseClick;

		var colList = subsequentColumns.Keys.ToList ();
		colList.Insert (0, firstColumnName);

		this.cols = colList.ToArray ();


		this.lamdas = subsequentColumns;
	}


	/// <inheritdoc/>
	public object this [int row, int col] =>
		col == 0 ? GetColumnZeroRepresentationFromTree (row) :
		this.lamdas [ColumnNames [col]] (RowToObject (row));

	/// <inheritdoc/>
	public int Rows => tree.BuildLineMap ().Count;

	/// <inheritdoc/>
	public int Columns => this.lamdas.Count + 1;

	/// <inheritdoc/>
	public string [] ColumnNames => cols;

	/// <inheritdoc/>
	public void Dispose ()
	{
		tree.Dispose ();
	}

	/// <summary>
	/// Returns the tree model object rendering on the given <paramref name="row"/>
	/// of the table.
	/// </summary>
	/// <param name="row">Row in table.</param>
	/// <returns></returns>
	public T RowToObject (int row)
	{
		return tree.BuildLineMap ().ElementAt (row).Model;
	}


	private string GetColumnZeroRepresentationFromTree (int row)
	{
		var branch = RowToBranch (row);

		// Everything on line before the expansion run and branch text
		Rune [] prefix = branch.GetLinePrefix (Application.Driver).ToArray ();
		Rune expansion = branch.GetExpandableSymbol (Application.Driver);
		string lineBody = tree.AspectGetter (branch.Model) ?? "";

		var sb = new StringBuilder ();

		foreach (var p in prefix) {
			sb.Append (p);
		}

		sb.Append (expansion);
		sb.Append (lineBody);

		return sb.ToString ();
	}

	private void Table_KeyPress (object sender, KeyEventEventArgs e)
	{
		if (!IsInTreeColumn (tableView.SelectedColumn, true)) {
			return;
		}

		var obj = tree.GetObjectOnRow (tableView.SelectedRow);

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

	private void Table_MouseClick (object sender, MouseEventEventArgs e)
	{
		var hit = tableView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out var headerIfAny, out var offsetX);

		if (hit == null || headerIfAny != null || !IsInTreeColumn (hit.Value.X, false) || offsetX == null) {
			return;
		}

		var branch = RowToBranch (hit.Value.Y);

		if (branch.IsHitOnExpandableSymbol (Application.Driver, offsetX.Value)) {

			var m = branch.Model;

			if (tree.CanExpand (m) && !tree.IsExpanded (m)) {
				tree.Expand (m);

				e.Handled = true;
			} else if (tree.IsExpanded (m)) {
				tree.Collapse (m);
				e.Handled = true;
			}
		}

		if (e.Handled) {
			tree.InvalidateLineMap ();
			tableView.SetNeedsDisplay ();
		}
	}

	private Branch<T> RowToBranch (int row)
	{
		return tree.BuildLineMap ().ElementAt (row);
	}

	private bool IsInTreeColumn (int column, bool isKeyboard)
	{
		var colNames = tableView.Table.ColumnNames;

		if (column < 0 || column >= colNames.Length) {
			return false;
		}

		// if full row is selected then it is hard to tell which sub cell in the tree
		// has focus so we should typically just always respond with expand/collapse
		if (tableView.FullRowSelect && isKeyboard) {
			return true;
		}

		// we cannot just check that SelectedColumn is 0 because source may
		// be wrapped e.g. with a CheckBoxTableSourceWrapperBase
		return colNames [column] == cols [0];
	}

	/// <inheritdoc/>
	public T GetObjectOnRow (int row)
	{
		return RowToObject (row);
	}

	/// <inheritdoc/>
	public IEnumerable<T> GetAllObjects ()
	{
		return tree.BuildLineMap ().Select (b => b.Model);
	}
}