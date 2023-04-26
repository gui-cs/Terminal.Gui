using System;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;
using System.Linq;
using System.Globalization;
using static Terminal.Gui.ListColumnView;
using static Terminal.Gui.TableView;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "ListColumnEditor", Description: "Implements data table editor using the ListColumnEditor control.")]
	[ScenarioCategory ("TableView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Top Level Windows")]
	public class ListColumnEditor : Scenario {
		ListColumnView listColView;
		private MenuItem miCellLines;
		private MenuItem miExpandLastColumn;
		private MenuItem miAlwaysUseNormalColorForVerticalCellLines;
		private MenuItem miSmoothScrolling;
		private MenuItem miAlternatingColors;
		private MenuItem miCursor;
		private MenuItem miPopulateVertical;
		private MenuItem miScrollParallel;

		ColorScheme alternatingColorScheme;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Application.Top.LayoutSubviews ();

			this.listColView = new ListColumnView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("Open_ListExample", "", () => OpenSimpleList (true)),
					new MenuItem ("_CloseExample", "", () => CloseExample()),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					miCellLines =new MenuItem ("_CellLines", "", () => ToggleCellLines()){Checked = listColView.Style.ShowVerticalCellLines, CheckType = MenuItemCheckStyle.Checked },
					miExpandLastColumn = new MenuItem ("_ExpandLastColumn", "", () => ToggleExpandLastColumn()){Checked = listColView.Style.ExpandLastColumn, CheckType = MenuItemCheckStyle.Checked },
					miAlwaysUseNormalColorForVerticalCellLines = new MenuItem ("_AlwaysUseNormalColorForVerticalCellLines", "", () => ToggleAlwaysUseNormalColorForVerticalCellLines()){Checked = listColView.Style.AlwaysUseNormalColorForVerticalCellLines, CheckType = MenuItemCheckStyle.Checked },
					miSmoothScrolling = new MenuItem ("_SmoothHorizontalScrolling", "", () => ToggleSmoothScrolling()){Checked = listColView.Style.SmoothHorizontalScrolling, CheckType = MenuItemCheckStyle.Checked },
					miAlternatingColors = new MenuItem ("Alternating Colors", "", () => ToggleAlternatingColors()){CheckType = MenuItemCheckStyle.Checked},
					miCursor = new MenuItem ("Invert Selected Cell First Character", "", () => ToggleInvertSelectedCellFirstCharacter()){Checked = listColView.Style.InvertSelectedCellFirstCharacter,CheckType = MenuItemCheckStyle.Checked},
				}),
				new MenuBarItem ("_List", new MenuItem [] {
					miPopulateVertical = new MenuItem ("_PopulateVertical", "", () => TogglePopulateVertical ()) { Checked = listColView.ListStyle.PopulateVertical, CheckType = MenuItemCheckStyle.Checked },
					miScrollParallel = new MenuItem ("_ScrollParallel", "", () => ToggleScrollParallel ()) { Checked = listColView.ListStyle.ScrollParallel, CheckType = MenuItemCheckStyle.Checked },
					new MenuItem ("_Set Max Cell Width", "", SetListMaxWidth),
					new MenuItem ("_Set Min Cell Width", "", SetListMinWidth),
				}),
			});

			Application.Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ OpenListExample", () => OpenSimpleList(true)),
				new StatusItem(Key.F3, "~F3~ CloseExample", () => CloseExample()),
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
			});
			Application.Top.Add (statusBar);

			Win.Add (listColView);

			var selectedCellLabel = new Label () {
				X = 0,
				Y = Pos.Bottom (listColView),
				Text = "0,0",
				Width = Dim.Fill (),
				TextAlignment = TextAlignment.Right

			};

			Win.Add (selectedCellLabel);

			listColView.SelectedCellChanged += (s, e) => { selectedCellLabel.Text = $"{listColView.SelectedRow},{listColView.SelectedColumn}"; };
			listColView.CellActivated += EditCurrentCell;
			listColView.KeyPress += TableViewKeyPress;

			SetupScrollBar ();

			alternatingColorScheme = new ColorScheme () {

				Disabled = Win.ColorScheme.Disabled,
				HotFocus = Win.ColorScheme.HotFocus,
				Focus = Win.ColorScheme.Focus,
				Normal = Application.Driver.MakeAttribute (Color.White, Color.BrightBlue)
			};

			// if user clicks the mouse in TableView
			listColView.MouseClick += (s,e) => {

				listColView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out DataColumn clickedCol);

				if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {
					ShowContextMenu (e);
				}
			};

			listColView.AddKeyBinding (Key.Space, Command.ToggleChecked);
		}

		private void SortList (bool asc)
		{
			Array array = new object [listColView.Count];
			listColView.ListData.CopyTo(array, 0);
			Array.Sort (array);
			if (!asc) {
				Array.Reverse (array);
			}
			listColView.ListData = array;
		}

		private void ShowContextMenu (MouseEventEventArgs e)
		{
			var contextMenu = new ContextMenu (e.MouseEvent.X + 1, e.MouseEvent.Y + 1,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("Sort Ascending","",()=>SortList(true)),
					new MenuItem ("Sort Descending","",()=>SortList(false)),
				})
			);

			contextMenu.Show ();
		}

		private void SetupScrollBar ()
		{
			var _scrollBar = new ScrollBarView (listColView, true);

			_scrollBar.ChangedPosition += (s,e) => {
				listColView.RowOffset = _scrollBar.Position;
				if (listColView.RowOffset != _scrollBar.Position) {
					_scrollBar.Position = listColView.RowOffset;
				}
				listColView.SetNeedsDisplay ();
			};
			/*
			_scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
				_listView.LeftItem = _scrollBar.OtherScrollBarView.Position;
				if (_listView.LeftItem != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
				}
				_listView.SetNeedsDisplay ();
			};*/

			listColView.DrawContent += (s,e) => {
				_scrollBar.Size = listColView.Table?.Rows?.Count ?? 0;
				_scrollBar.Position = listColView.RowOffset;
				//	_scrollBar.OtherScrollBarView.Size = _listView.Maxlength - 1;
				//	_scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
				_scrollBar.Refresh ();
			};

		}

		private void TableViewKeyPress (object sender, KeyEventEventArgs e)
		{
			if (e.KeyEvent.Key == Key.DeleteChar) {

				foreach (var pt in listColView.GetAllSelectedCells ()) {
					listColView.Table.Rows [pt.Y] [pt.X] = DBNull.Value;
				}

				listColView.Update ();
				e.Handled = true;
			}

		}

		private void ToggleExpandLastColumn ()
		{
			miExpandLastColumn.Checked = !miExpandLastColumn.Checked;
			listColView.Style.ExpandLastColumn = (bool)miExpandLastColumn.Checked;

			listColView.Update ();

		}

		private void ToggleAlwaysUseNormalColorForVerticalCellLines()
		{
			miAlwaysUseNormalColorForVerticalCellLines.Checked = !miAlwaysUseNormalColorForVerticalCellLines.Checked;
			listColView.Style.AlwaysUseNormalColorForVerticalCellLines = (bool)miAlwaysUseNormalColorForVerticalCellLines.Checked;

			listColView.Update ();
		}
		private void ToggleSmoothScrolling ()
		{
			miSmoothScrolling.Checked = !miSmoothScrolling.Checked;
			listColView.Style.SmoothHorizontalScrolling = (bool)miSmoothScrolling.Checked;

			listColView.Update ();

		}
		private void ToggleCellLines ()
		{
			miCellLines.Checked = !miCellLines.Checked;
			listColView.Style.ShowVerticalCellLines = (bool)miCellLines.Checked;
			listColView.Update ();
		}
		private void ToggleAlternatingColors ()
		{
			//toggle menu item
			miAlternatingColors.Checked = !miAlternatingColors.Checked;

			if (miAlternatingColors.Checked == true) {
				listColView.Style.RowColorGetter = (a) => { return a.RowIndex % 2 == 0 ? alternatingColorScheme : null; };
			} else {
				listColView.Style.RowColorGetter = null;
			}
			listColView.SetNeedsDisplay ();
		}

		private void ToggleInvertSelectedCellFirstCharacter ()
		{
			//toggle menu item
			miCursor.Checked = !miCursor.Checked;
			listColView.Style.InvertSelectedCellFirstCharacter = (bool)miCursor.Checked;
			listColView.SetNeedsDisplay ();
		}

		private void TogglePopulateVertical ()
		{
			//toggle menu item
			miPopulateVertical.Checked = !miPopulateVertical.Checked;
			listColView.ListStyle.PopulateVertical = (bool)miPopulateVertical.Checked;
			listColView.ListUpdate ();
		}

		private void ToggleScrollParallel ()
		{
			//toggle menu item
			miScrollParallel.Checked = !miScrollParallel.Checked;
			listColView.ListStyle.ScrollParallel = (bool)miScrollParallel.Checked;
			listColView.ListUpdate ();
		}

		private void SetListMinWidth ()
		{
			RunListWidthDialog ("MinCellWidth", (s, v) => s.MinCellWidth = v, (s) => s.MinCellWidth);
		}

		private void SetListMaxWidth ()
		{
			RunListWidthDialog ("MaxCellWidth", (s, v) => s.MaxCellWidth = v, (s) => s.MaxCellWidth);
		}

		private void RunListWidthDialog (string prompt, Action<ListColumnView, int> setter, Func<ListColumnView, int> getter)
		{
			var accepted = false;
			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += (s, e) => { accepted = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += (s, e) => { Application.RequestStop (); };
			var d = new Dialog (ok, cancel) { Title = prompt };

			var tf = new TextField () {
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
					setter (listColView, int.Parse (tf.Text.ToString ()));
				} catch (Exception ex) {
					MessageBox.ErrorQuery (60, 20, "Failed to set", ex.Message, "Ok");
				}

				listColView.ListUpdate ();
			}
		}

		private void CloseExample ()
		{
			listColView.Table = null;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private void OpenSimpleList (bool big) {
			listColView.ListData = BuildSimpleList (big ? 1000 : 20).ToArray ();
			listColView.Style.ShowHeaders = false;
			listColView.Style.ShowHorizontalHeaderOverline = false;
			listColView.Style.ShowHorizontalHeaderUnderline = false;
			listColView.Style.ShowHorizontalBottomline = false;
			listColView.FullRowSelect = false;
		}

		private void EditCurrentCell (object sender, CellActivatedEventArgs e)
		{
			if (e.Table == null)
				return;
			var o = e.Table.Rows [e.Row] [e.Col];

			var title = "Enter new value";

			var oldValue = e.Table.Rows [e.Row] [e.Col].ToString ();
			bool okPressed = false;

			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += (s,e) => { okPressed = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += (s,e) => { Application.RequestStop (); };
			var d = new Dialog (ok, cancel) { Title = title };

			var lbl = new Label () {
				X = 0,
				Y = 1,
				Text = e.Table.Columns [e.Col].ColumnName
			};

			var tf = new TextField () {
				Text = oldValue,
				X = 0,
				Y = 2,
				Width = Dim.Fill ()
			};

			d.Add (lbl, tf);
			tf.SetFocus ();

			Application.Run (d);

			if (okPressed) {

				try {
					e.Table.Rows [e.Row] [e.Col] = string.IsNullOrWhiteSpace (tf.Text.ToString ()) ? DBNull.Value : (object)tf.Text;
				} catch (Exception ex) {
					MessageBox.ErrorQuery (60, 20, "Failed to set text", ex.Message, "Ok");
				}

				listColView.Update ();
			}
		}

		/// <summary>
		/// Builds a simple list in which values are the index.  This helps testing that scrolling etc is working correctly and not skipping out values when paging
		/// </summary>
		/// <param name="cols"></param>
		/// <param name="rows"></param>
		/// <returns></returns>
		public static IList<object> BuildSimpleList (int items) {
			var list = new List<object> ();

			for (int i = 0; i < items; i++) {
				list.Add ("Item " + i);
			}

			return list;
		}
	}
}
