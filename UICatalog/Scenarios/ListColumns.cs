using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ListColumns", "Implements a columned list via a data table.")]
[ScenarioCategory ("TableView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Top Level Windows")]
public class ListColumns : Scenario {
	MenuItem _miAlternatingColors;
	MenuItem _miAlwaysUseNormalColorForVerticalCellLines;
	MenuItem _miBottomline;
	MenuItem _miCellLines;
	MenuItem _miCursor;
	MenuItem _miExpandLastColumn;
	MenuItem _miOrientVertical;
	MenuItem _miScrollParallel;
	MenuItem _miSmoothScrolling;
	MenuItem _miTopline;

	ColorScheme alternatingColorScheme;
	DataTable currentTable;
	TableView listColView;

	public override void Setup ()
	{
		Win.Title = GetName ();
		Win.Y = 1;                 // menu
		Win.Height = Dim.Fill (1); // status bar

		listColView = new TableView {
			X = 0,
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill (1),
			Style = new TableStyle {
				ShowHeaders = false,
				ShowHorizontalHeaderOverline = false,
				ShowHorizontalHeaderUnderline = false,
				ShowHorizontalBottomline = false,
				ExpandLastColumn = false
			}
		};
		var listColStyle = new ListColumnStyle ();

		var menu = new MenuBar (new MenuBarItem [] {
			new ("_File", new MenuItem [] {
				new ("Open_BigListExample", "", () => OpenSimpleList (true)),
				new ("Open_SmListExample", "", () => OpenSimpleList (false)),
				new ("_CloseExample", "", () => CloseExample ()),
				new ("_Quit", "", () => Quit ())
			}),
			new ("_View", new [] {
				_miTopline = new MenuItem ("_TopLine", "", () => ToggleTopline ()) {
					Checked = listColView.Style.ShowHorizontalHeaderOverline,
					CheckType = MenuItemCheckStyle.Checked
				},
				_miBottomline = new MenuItem ("_BottomLine", "", () => ToggleBottomline ()) {
					Checked = listColView.Style.ShowHorizontalBottomline,
					CheckType = MenuItemCheckStyle.Checked
				},
				_miCellLines = new MenuItem ("_CellLines", "", () => ToggleCellLines ()) {
					Checked = listColView.Style.ShowVerticalCellLines,
					CheckType = MenuItemCheckStyle.Checked
				},
				_miExpandLastColumn = new MenuItem ("_ExpandLastColumn", "", () => ToggleExpandLastColumn ()) {
					Checked = listColView.Style.ExpandLastColumn,
					CheckType = MenuItemCheckStyle.Checked
				},
				_miAlwaysUseNormalColorForVerticalCellLines =
					new MenuItem ("_AlwaysUseNormalColorForVerticalCellLines", "",
						() => ToggleAlwaysUseNormalColorForVerticalCellLines ()) {
						Checked = listColView.Style.AlwaysUseNormalColorForVerticalCellLines,
						CheckType = MenuItemCheckStyle.Checked
					},
				_miSmoothScrolling = new MenuItem ("_SmoothHorizontalScrolling", "", () => ToggleSmoothScrolling ()) {
					Checked = listColView.Style.SmoothHorizontalScrolling,
					CheckType = MenuItemCheckStyle.Checked
				},
				_miAlternatingColors = new MenuItem ("Alternating Colors", "", () => ToggleAlternatingColors ())
					{ CheckType = MenuItemCheckStyle.Checked },
				_miCursor = new MenuItem ("Invert Selected Cell First Character", "",
					() => ToggleInvertSelectedCellFirstCharacter ()) {
					Checked = listColView.Style.InvertSelectedCellFirstCharacter,
					CheckType = MenuItemCheckStyle.Checked
				}
			}),
			new ("_List", new [] {
				//new MenuItem ("_Hide Headers", "", HideHeaders),
				_miOrientVertical = new MenuItem ("_OrientVertical", "", () => ToggleVerticalOrientation ()) {
					Checked = listColStyle.Orientation == Orientation.Vertical,
					CheckType = MenuItemCheckStyle.Checked
				},
				_miScrollParallel = new MenuItem ("_ScrollParallel", "", () => ToggleScrollParallel ())
					{ Checked = listColStyle.ScrollParallel, CheckType = MenuItemCheckStyle.Checked },
				new ("Set _Max Cell Width", "", SetListMaxWidth),
				new ("Set Mi_n Cell Width", "", SetListMinWidth)
			})
		});

		Application.Top.Add (menu);

		var statusBar = new StatusBar (new StatusItem [] {
			new (KeyCode.F2, "~F2~ OpenBigListEx", () => OpenSimpleList (true)),
			new (KeyCode.F3, "~F3~ CloseExample", () => CloseExample ()),
			new (KeyCode.F4, "~F4~ OpenSmListEx", () => OpenSimpleList (false)),
			new (Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit ())
		});
		Application.Top.Add (statusBar);

		Win.Add (listColView);

		var selectedCellLabel = new Label {
			X = 0,
			Y = Pos.Bottom (listColView),
			Text = "0,0",
			Width = Dim.Fill (),
			TextAlignment = TextAlignment.Right

		};

		Win.Add (selectedCellLabel);

		listColView.SelectedCellChanged += (s, e) => { selectedCellLabel.Text = $"{listColView.SelectedRow},{listColView.SelectedColumn}"; };
		listColView.KeyDown += TableViewKeyPress;

		SetupScrollBar ();

		alternatingColorScheme = new ColorScheme {

			Disabled = Win.ColorScheme.Disabled,
			HotFocus = Win.ColorScheme.HotFocus,
			Focus = Win.ColorScheme.Focus,
			Normal = new Attribute (Color.White, Color.BrightBlue)
		};

		// if user clicks the mouse in TableView
		listColView.MouseClick += (s, e) => {

			listColView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out var clickedCol);
		};

		listColView.KeyBindings.Add (KeyCode.Space, Command.ToggleChecked);
	}

	void SetupScrollBar ()
	{
		var scrollBar = new ScrollBarView (listColView, true); // (listColView, true, true);

		scrollBar.ChangedPosition += (s, e) => {
			listColView.RowOffset = scrollBar.Position;
			if (listColView.RowOffset != scrollBar.Position) {
				scrollBar.Position = listColView.RowOffset;
			}
			listColView.SetNeedsDisplay ();
		};
		/*
		scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
			listColView.ColumnOffset = scrollBar.OtherScrollBarView.Position;
			if (listColView.ColumnOffset != scrollBar.OtherScrollBarView.Position) {
				scrollBar.OtherScrollBarView.Position = listColView.ColumnOffset;
			}
			listColView.SetNeedsDisplay ();
		};
		*/

		listColView.DrawContent += (s, e) => {
			scrollBar.Size = listColView.Table?.Rows ?? 0;
			scrollBar.Position = listColView.RowOffset;
			//scrollBar.OtherScrollBarView.Size = listColView.Table?.Columns - 1 ?? 0;
			//scrollBar.OtherScrollBarView.Position = listColView.ColumnOffset;
			scrollBar.Refresh ();
		};

	}

	void TableViewKeyPress (object sender, Key e)
	{
		if (e.KeyCode == KeyCode.Delete) {

			// set all selected cells to null
			foreach (var pt in listColView.GetAllSelectedCells ()) {
				currentTable.Rows [pt.Y] [pt.X] = DBNull.Value;
			}

			listColView.Update ();
			e.Handled = true;
		}

	}

	void ToggleTopline ()
	{
		_miTopline.Checked = !_miTopline.Checked;
		listColView.Style.ShowHorizontalHeaderOverline = (bool)_miTopline.Checked;
		listColView.Update ();
	}

	void ToggleBottomline ()
	{
		_miBottomline.Checked = !_miBottomline.Checked;
		listColView.Style.ShowHorizontalBottomline = (bool)_miBottomline.Checked;
		listColView.Update ();
	}

	void ToggleExpandLastColumn ()
	{
		_miExpandLastColumn.Checked = !_miExpandLastColumn.Checked;
		listColView.Style.ExpandLastColumn = (bool)_miExpandLastColumn.Checked;

		listColView.Update ();

	}

	void ToggleAlwaysUseNormalColorForVerticalCellLines ()
	{
		_miAlwaysUseNormalColorForVerticalCellLines.Checked = !_miAlwaysUseNormalColorForVerticalCellLines.Checked;
		listColView.Style.AlwaysUseNormalColorForVerticalCellLines = (bool)_miAlwaysUseNormalColorForVerticalCellLines.Checked;

		listColView.Update ();
	}

	void ToggleSmoothScrolling ()
	{
		_miSmoothScrolling.Checked = !_miSmoothScrolling.Checked;
		listColView.Style.SmoothHorizontalScrolling = (bool)_miSmoothScrolling.Checked;

		listColView.Update ();

	}

	void ToggleCellLines ()
	{
		_miCellLines.Checked = !_miCellLines.Checked;
		listColView.Style.ShowVerticalCellLines = (bool)_miCellLines.Checked;
		listColView.Update ();
	}

	void ToggleAlternatingColors ()
	{
		//toggle menu item
		_miAlternatingColors.Checked = !_miAlternatingColors.Checked;

		if (_miAlternatingColors.Checked == true) {
			listColView.Style.RowColorGetter = a => { return a.RowIndex % 2 == 0 ? alternatingColorScheme : null; };
		} else {
			listColView.Style.RowColorGetter = null;
		}
		listColView.SetNeedsDisplay ();
	}

	void ToggleInvertSelectedCellFirstCharacter ()
	{
		//toggle menu item
		_miCursor.Checked = !_miCursor.Checked;
		listColView.Style.InvertSelectedCellFirstCharacter = (bool)_miCursor.Checked;
		listColView.SetNeedsDisplay ();
	}

	void ToggleVerticalOrientation ()
	{
		_miOrientVertical.Checked = !_miOrientVertical.Checked;
		if ((ListTableSource)listColView.Table != null) {
			((ListTableSource)listColView.Table).Style.Orientation = (bool)_miOrientVertical.Checked ? Orientation.Vertical : Orientation.Horizontal;
			listColView.SetNeedsDisplay ();
		}
	}

	void ToggleScrollParallel ()
	{
		_miScrollParallel.Checked = !_miScrollParallel.Checked;
		if ((ListTableSource)listColView.Table != null) {
			((ListTableSource)listColView.Table).Style.ScrollParallel = (bool)_miScrollParallel.Checked;
			listColView.SetNeedsDisplay ();
		}
	}

	void SetListMinWidth ()
	{
		RunListWidthDialog ("MinCellWidth", (s, v) => s.MinCellWidth = v, s => s.MinCellWidth);
		listColView.SetNeedsDisplay ();
	}

	void SetListMaxWidth ()
	{
		RunListWidthDialog ("MaxCellWidth", (s, v) => s.MaxCellWidth = v, s => s.MaxCellWidth);
		listColView.SetNeedsDisplay ();
	}

	void RunListWidthDialog (string prompt, Action<TableView, int> setter, Func<TableView, int> getter)
	{
		var accepted = false;
		var ok = new Button ("Ok", true);
		ok.Clicked += (s, e) => {
			accepted = true;
			Application.RequestStop ();
		};
		var cancel = new Button ("Cancel");
		cancel.Clicked += (s, e) => { Application.RequestStop (); };
		var d = new Dialog (ok, cancel) { Title = prompt };

		var tf = new TextField {
			Text = getter (listColView).ToString (),
			X = 0,
			Y = 1,
			Width = Dim.Fill ()
		};

		d.Add (tf);
		tf.SetFocus ();

		Application.Run (d);

		if (accepted) {

			try {
				setter (listColView, int.Parse (tf.Text));
			} catch (Exception ex) {
				MessageBox.ErrorQuery (60, 20, "Failed to set", ex.Message, "Ok");
			}
		}
	}

	void CloseExample () => listColView.Table = null;

	void Quit () => Application.RequestStop ();

	void OpenSimpleList (bool big) => SetTable (BuildSimpleList (big ? 1023 : 31));

	void SetTable (IList list)
	{
		listColView.Table = new ListTableSource (list, listColView);
		if ((ListTableSource)listColView.Table != null) {
			currentTable = ((ListTableSource)listColView.Table).DataTable;
		}
	}

	/// <summary>
	/// Builds a simple list in which values are the index.  This helps testing that scrolling etc is working correctly and not
	/// skipping out values when paging
	/// </summary>
	/// <param name="items"></param>
	/// <returns></returns>
	public static IList BuildSimpleList (int items)
	{
		var list = new List<object> ();

		for (var i = 0; i < items; i++) {
			list.Add ("Item " + i);
		}

		return list;
	}
}