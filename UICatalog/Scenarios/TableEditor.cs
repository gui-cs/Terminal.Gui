using System;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;
using System.Linq;
using System.Globalization;
using static Terminal.Gui.TableView;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "TableEditor", Description: "Implements data table editor using the TableView control.")]
	[ScenarioCategory ("TableView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Top Level Windows")]
	public class TableEditor : Scenario 
	{
		TableView tableView;
		private MenuItem miAlwaysShowHeaders;
		private MenuItem miHeaderOverline;
		private MenuItem miHeaderMidline;
		private MenuItem miHeaderUnderline;
		private MenuItem miShowHorizontalScrollIndicators;
		private MenuItem miCellLines;
		private MenuItem miFullRowSelect;
		private MenuItem miExpandLastColumn;
		private MenuItem miSmoothScrolling;
		private MenuItem miAlternatingColors;
		private MenuItem miCursor;

		ColorScheme redColorScheme;
		ColorScheme redColorSchemeAlt;
		ColorScheme alternatingColorScheme;

		public override void Setup ()
		{
			Win.Title = this.GetName();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			this.tableView = new TableView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_OpenBigExample", "", () => OpenExample(true)),
					new MenuItem ("_OpenSmallExample", "", () => OpenExample(false)),
					new MenuItem ("_CloseExample", "", () => CloseExample()),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					miAlwaysShowHeaders = new MenuItem ("_AlwaysShowHeaders", "", () => ToggleAlwaysShowHeader()){Checked = tableView.Style.AlwaysShowHeaders, CheckType = MenuItemCheckStyle.Checked },
					miHeaderOverline = new MenuItem ("_HeaderOverLine", "", () => ToggleOverline()){Checked = tableView.Style.ShowHorizontalHeaderOverline, CheckType = MenuItemCheckStyle.Checked },
					miHeaderMidline = new MenuItem ("_HeaderMidLine", "", () => ToggleHeaderMidline()){Checked = tableView.Style.ShowVerticalHeaderLines, CheckType = MenuItemCheckStyle.Checked },
					miHeaderUnderline = new MenuItem ("_HeaderUnderLine", "", () => ToggleUnderline()){Checked = tableView.Style.ShowHorizontalHeaderUnderline, CheckType = MenuItemCheckStyle.Checked },
					miShowHorizontalScrollIndicators = new MenuItem ("_HorizontalScrollIndicators", "", () => ToggleHorizontalScrollIndicators()){Checked = tableView.Style.ShowHorizontalScrollIndicators, CheckType = MenuItemCheckStyle.Checked },
					miFullRowSelect =new MenuItem ("_FullRowSelect", "", () => ToggleFullRowSelect()){Checked = tableView.FullRowSelect, CheckType = MenuItemCheckStyle.Checked },
					miCellLines =new MenuItem ("_CellLines", "", () => ToggleCellLines()){Checked = tableView.Style.ShowVerticalCellLines, CheckType = MenuItemCheckStyle.Checked },
					miExpandLastColumn = new MenuItem ("_ExpandLastColumn", "", () => ToggleExpandLastColumn()){Checked = tableView.Style.ExpandLastColumn, CheckType = MenuItemCheckStyle.Checked },
					miSmoothScrolling = new MenuItem ("_SmoothHorizontalScrolling", "", () => ToggleSmoothScrolling()){Checked = tableView.Style.SmoothHorizontalScrolling, CheckType = MenuItemCheckStyle.Checked },
					new MenuItem ("_AllLines", "", () => ToggleAllCellLines()),
					new MenuItem ("_NoLines", "", () => ToggleNoCellLines()),
					miAlternatingColors = new MenuItem ("Alternating Colors", "", () => ToggleAlternatingColors()){CheckType = MenuItemCheckStyle.Checked},
					miCursor = new MenuItem ("Invert Selected Cell First Character", "", () => ToggleInvertSelectedCellFirstCharacter()){Checked = tableView.Style.InvertSelectedCellFirstCharacter,CheckType = MenuItemCheckStyle.Checked},
					new MenuItem ("_ClearColumnStyles", "", () => ClearColumnStyles()),
				}),
				new MenuBarItem ("_Column", new MenuItem [] {
					new MenuItem ("_Set Max Width", "", SetMaxWidth),
					new MenuItem ("_Set Min Width", "", SetMinWidth),
					new MenuItem ("_Set MinAcceptableWidth", "",SetMinAcceptableWidth),
					new MenuItem ("_Set All MinAcceptableWidth=1", "",SetMinAcceptableWidthToOne),
				}),
			});
		

		Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ OpenExample", () => OpenExample(true)),
				new StatusItem(Key.F3, "~F3~ CloseExample", () => CloseExample()),
				new StatusItem(Key.F4, "~F4~ OpenSimple", () => OpenSimple(true)),
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);

			Win.Add (tableView);

			var selectedCellLabel = new Label(){
				X = 0,
				Y = Pos.Bottom(tableView),
				Text = "0,0",
				Width = Dim.Fill(),
				TextAlignment = TextAlignment.Right
				
			};

			Win.Add(selectedCellLabel);

			tableView.SelectedCellChanged += (e) => { selectedCellLabel.Text = $"{tableView.SelectedRow},{tableView.SelectedColumn}"; };
			tableView.CellActivated += EditCurrentCell;
			tableView.KeyPress += TableViewKeyPress;

			SetupScrollBar();

			redColorScheme = new ColorScheme(){
				Disabled = Win.ColorScheme.Disabled,
				HotFocus = Win.ColorScheme.HotFocus,
				Focus = Win.ColorScheme.Focus,
				Normal = Application.Driver.MakeAttribute(Color.Red,Win.ColorScheme.Normal.Background)
			};

			alternatingColorScheme = new ColorScheme(){

				Disabled = Win.ColorScheme.Disabled,
				HotFocus = Win.ColorScheme.HotFocus,
				Focus = Win.ColorScheme.Focus,
				Normal = Application.Driver.MakeAttribute(Color.White,Color.BrightBlue)
			};
			redColorSchemeAlt = new ColorScheme(){

				Disabled = Win.ColorScheme.Disabled,
				HotFocus = Win.ColorScheme.HotFocus,
				Focus = Win.ColorScheme.Focus,
				Normal = Application.Driver.MakeAttribute(Color.Red,Color.BrightBlue)
			};
		}


		private DataColumn GetColumn ()
		{
			if (tableView.Table == null)
				return null;

			if (tableView.SelectedColumn < 0 || tableView.SelectedColumn > tableView.Table.Columns.Count)
				return null;

			return tableView.Table.Columns [tableView.SelectedColumn];
		}

		private void SetMinAcceptableWidthToOne ()
		{
			foreach (DataColumn c in tableView.Table.Columns) 
			{
				var style = tableView.Style.GetOrCreateColumnStyle (c);
				style.MinAcceptableWidth = 1;
			}
		}
		private void SetMinAcceptableWidth ()
		{
			var col = GetColumn ();
			RunColumnWidthDialog (col, "MinAcceptableWidth", (s,v)=>s.MinAcceptableWidth = v,(s)=>s.MinAcceptableWidth);
		}

		private void SetMinWidth ()
		{
			var col = GetColumn ();
			RunColumnWidthDialog (col, "MinWidth", (s, v) => s.MinWidth = v, (s) => s.MinWidth);
		}

		private void SetMaxWidth ()
		{
			var col = GetColumn ();
			RunColumnWidthDialog (col, "MaxWidth", (s, v) => s.MaxWidth = v, (s) => s.MaxWidth);
		}

		private void RunColumnWidthDialog (DataColumn col, string prompt, Action<ColumnStyle,int> setter,Func<ColumnStyle,int> getter)
		{
			var accepted = false;
			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += () => { accepted = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += () => { Application.RequestStop (); };
			var d = new Dialog (prompt, 60, 20, ok, cancel);

			var style = tableView.Style.GetOrCreateColumnStyle (col);

			var lbl = new Label () {
				X = 0,
				Y = 1,
				Text = col.ColumnName
			};

			var tf = new TextField () {
				Text = getter(style).ToString (),
				X = 0,
				Y = 2,
				Width = Dim.Fill ()
			};

			d.Add (lbl, tf);
			tf.SetFocus ();

			Application.Run (d);

			if (accepted) {

				try {
					setter (style, int.Parse (tf.Text.ToString()));
				} catch (Exception ex) {
					MessageBox.ErrorQuery (60, 20, "Failed to set", ex.Message, "Ok");
				}

				tableView.Update ();
			}
		}

		private void SetupScrollBar ()
		{
			var _scrollBar = new ScrollBarView (tableView, true);

			_scrollBar.ChangedPosition += () => {
				tableView.RowOffset = _scrollBar.Position;
				if (tableView.RowOffset != _scrollBar.Position) {
					_scrollBar.Position = tableView.RowOffset;
				}
				tableView.SetNeedsDisplay ();
			};
			/*
			_scrollBar.OtherScrollBarView.ChangedPosition += () => {
				_listView.LeftItem = _scrollBar.OtherScrollBarView.Position;
				if (_listView.LeftItem != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
				}
				_listView.SetNeedsDisplay ();
			};*/

			tableView.DrawContent += (e) => {
				_scrollBar.Size = tableView.Table?.Rows?.Count ??0;
				_scrollBar.Position = tableView.RowOffset;
			//	_scrollBar.OtherScrollBarView.Size = _listView.Maxlength - 1;
			//	_scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
				_scrollBar.Refresh ();
			};
		
		}

		private void TableViewKeyPress (View.KeyEventEventArgs e)
		{
			if(e.KeyEvent.Key == Key.DeleteChar){

				if(tableView.FullRowSelect)
				{
					// Delete button deletes all rows when in full row mode
					foreach(int toRemove in tableView.GetAllSelectedCells().Select(p=>p.Y).Distinct().OrderByDescending(i=>i))
						tableView.Table.Rows.RemoveAt(toRemove);
				}
				else{

					// otherwise set all selected cells to null
					foreach(var pt in tableView.GetAllSelectedCells())
					{
						tableView.Table.Rows[pt.Y][pt.X] = DBNull.Value;
					}
				}

				tableView.Update();
				e.Handled = true;
			}


		}

		private void ClearColumnStyles ()
		{
			tableView.Style.ColumnStyles.Clear();
			tableView.Update();
		}

		private void ToggleAlwaysShowHeader ()
		{
			miAlwaysShowHeaders.Checked = !miAlwaysShowHeaders.Checked;
			tableView.Style.AlwaysShowHeaders = miAlwaysShowHeaders.Checked;
			tableView.Update();
		}

		private void ToggleOverline ()
		{
			miHeaderOverline.Checked = !miHeaderOverline.Checked;
			tableView.Style.ShowHorizontalHeaderOverline = miHeaderOverline.Checked;
			tableView.Update();
		}
		private void ToggleHeaderMidline ()
		{
			miHeaderMidline.Checked = !miHeaderMidline.Checked;
			tableView.Style.ShowVerticalHeaderLines = miHeaderMidline.Checked;
			tableView.Update();
		}
		private void ToggleUnderline ()
		{
			miHeaderUnderline.Checked = !miHeaderUnderline.Checked;
			tableView.Style.ShowHorizontalHeaderUnderline = miHeaderUnderline.Checked;
			tableView.Update();
		}
		private void ToggleHorizontalScrollIndicators ()
		{
			miShowHorizontalScrollIndicators.Checked = !miShowHorizontalScrollIndicators.Checked;
			tableView.Style.ShowHorizontalScrollIndicators = miShowHorizontalScrollIndicators.Checked;
			tableView.Update();
		}
		private void ToggleFullRowSelect ()
		{
			miFullRowSelect.Checked = !miFullRowSelect.Checked;
			tableView.FullRowSelect= miFullRowSelect.Checked;
			tableView.Update();
		}

		private void ToggleExpandLastColumn()
		{
			miExpandLastColumn.Checked = !miExpandLastColumn.Checked;
			tableView.Style.ExpandLastColumn = miExpandLastColumn.Checked;

			tableView.Update();

		}
		private void ToggleSmoothScrolling()
		{
			miSmoothScrolling.Checked = !miSmoothScrolling.Checked;
			tableView.Style.SmoothHorizontalScrolling = miSmoothScrolling.Checked;

			tableView.Update ();

		}
		private void ToggleCellLines()
		{
			miCellLines.Checked = !miCellLines.Checked;
			tableView.Style.ShowVerticalCellLines = miCellLines.Checked;
			tableView.Update();
		}
		private void ToggleAllCellLines()
		{
			tableView.Style.ShowHorizontalHeaderOverline = true;
			tableView.Style.ShowVerticalHeaderLines = true;
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowVerticalCellLines = true;
						
			miHeaderOverline.Checked = true;
			miHeaderMidline.Checked = true;
			miHeaderUnderline.Checked = true;
			miCellLines.Checked = true;

			tableView.Update();
		}
		private void ToggleNoCellLines()
		{
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.ShowVerticalHeaderLines = false;
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowVerticalCellLines = false;

			miHeaderOverline.Checked = false;
			miHeaderMidline.Checked = false;
			miHeaderUnderline.Checked = false;
			miCellLines.Checked = false;

			tableView.Update();
		}

		private void ToggleAlternatingColors()
		{
			//toggle menu item
			miAlternatingColors.Checked = !miAlternatingColors.Checked;

			if(miAlternatingColors.Checked){
				tableView.Style.RowColorGetter = (a)=> {return a.RowIndex%2==0 ? alternatingColorScheme : null;};
			}
			else
			{
				tableView.Style.RowColorGetter = null;
			}
			tableView.SetNeedsDisplay();
		}

		private void ToggleInvertSelectedCellFirstCharacter ()
		{
			//toggle menu item
			miCursor.Checked = !miCursor.Checked;
			tableView.Style.InvertSelectedCellFirstCharacter = miCursor.Checked;
			tableView.SetNeedsDisplay ();
		}
		private void CloseExample ()
		{
			tableView.Table = null;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private void OpenExample (bool big)
		{
			tableView.Table = BuildDemoDataTable(big ? 30 : 5, big ? 1000 : 5);
			SetDemoTableStyles();
		}

		private void SetDemoTableStyles ()
		{
			var alignMid = new TableView.ColumnStyle () {
				Alignment = TextAlignment.Centered
			};
			var alignRight = new TableView.ColumnStyle () {
				Alignment = TextAlignment.Right
			};

			var dateFormatStyle = new TableView.ColumnStyle () {
				Alignment = TextAlignment.Right,
				RepresentationGetter = (v)=> v is DateTime d ? d.ToString("yyyy-MM-dd"):v.ToString()
			};

			var negativeRight = new TableView.ColumnStyle () {
				
				Format = "0.##",
				MinWidth = 10,
				AlignmentGetter = (v)=>v is double d ? 
								// align negative values right
								d < 0 ? TextAlignment.Right : 
								// align positive values left
								TextAlignment.Left:
								// not a double
								TextAlignment.Left,
				
				ColorGetter = (a)=> a.CellValue is double d ? 
								// color 0 and negative values red
								d <= 0.0000001 ? a.RowIndex%2==0 && miAlternatingColors.Checked ? redColorSchemeAlt: redColorScheme : 
								// use normal scheme for positive values
								null:
								// not a double
								null
			};
			
			tableView.Style.ColumnStyles.Add(tableView.Table.Columns["DateCol"],dateFormatStyle);
			tableView.Style.ColumnStyles.Add(tableView.Table.Columns["DoubleCol"],negativeRight);
			tableView.Style.ColumnStyles.Add(tableView.Table.Columns["NullsCol"],alignMid);
			tableView.Style.ColumnStyles.Add(tableView.Table.Columns["IntCol"],alignRight);
			
			tableView.Update();
		}

		private void OpenSimple (bool big)
		{
			tableView.Table = BuildSimpleDataTable(big ? 30 : 5, big ? 1000 : 5);
		}

		private void EditCurrentCell (TableView.CellActivatedEventArgs e)
		{
			if(e.Table == null)
				return;

			var oldValue = e.Table.Rows[e.Row][e.Col].ToString();
			bool okPressed = false;

			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += () => { okPressed = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += () => { Application.RequestStop (); };
			var d = new Dialog ("Enter new value", 60, 20, ok, cancel);

			var lbl = new Label() {
				X = 0,
				Y = 1,
				Text = e.Table.Columns[e.Col].ColumnName
			};

			var tf = new TextField()
				{
					Text = oldValue,
					X = 0,
					Y = 2,
					Width = Dim.Fill()
				};
			
			d.Add (lbl,tf);
			tf.SetFocus();

			Application.Run (d);

			if(okPressed) {

				try {
					e.Table.Rows[e.Row][e.Col] = string.IsNullOrWhiteSpace(tf.Text.ToString()) ? DBNull.Value : (object)tf.Text;
				}
				catch(Exception ex) {
					MessageBox.ErrorQuery(60,20,"Failed to set text", ex.Message,"Ok");
				}
				
				tableView.Update();
			}
		}

		/// <summary>
		/// Generates a new demo <see cref="DataTable"/> with the given number of <paramref name="cols"/> (min 5) and <paramref name="rows"/>
		/// </summary>
		/// <param name="cols"></param>
		/// <param name="rows"></param>
		/// <returns></returns>
		public static DataTable BuildDemoDataTable(int cols, int rows)
		{
			var dt = new DataTable();

			int explicitCols = 6;
			dt.Columns.Add(new DataColumn("StrCol",typeof(string)));
			dt.Columns.Add(new DataColumn("DateCol",typeof(DateTime)));
			dt.Columns.Add(new DataColumn("IntCol",typeof(int)));
			dt.Columns.Add(new DataColumn("DoubleCol",typeof(double)));
			dt.Columns.Add(new DataColumn("NullsCol",typeof(string)));
			dt.Columns.Add(new DataColumn("Unicode",typeof(string)));

			for(int i=0;i< cols -explicitCols; i++) {
				dt.Columns.Add("Column" + (i+explicitCols));
			}
			
			var r = new Random(100);

			for(int i=0;i< rows;i++) {
				
				List<object> row = new List<object>(){ 
					"Some long text that is super cool",
					new DateTime(2000+i,12,25),
					r.Next(i),
					(r.NextDouble()*i)-0.5 /*add some negatives to demo styles*/,
					DBNull.Value,
					"Les Mise" + Char.ConvertFromUtf32(Int32.Parse("0301", NumberStyles.HexNumber)) + "rables"
				};
				
				for(int j=0;j< cols -explicitCols; j++) {
					row.Add("SomeValue" + r.Next(100));
				}

				dt.Rows.Add(row.ToArray());
			}

			return dt;
		}

		/// <summary>
		/// Builds a simple table in which cell values contents are the index of the cell.  This helps testing that scrolling etc is working correctly and not skipping out any rows/columns when paging
		/// </summary>
		/// <param name="cols"></param>
		/// <param name="rows"></param>
		/// <returns></returns>
		public static DataTable BuildSimpleDataTable(int cols, int rows)
		{
			var dt = new DataTable();

			for(int c = 0; c < cols; c++) {
				dt.Columns.Add("Col"+c);
			}
				
			for(int r = 0; r < rows; r++) {
				var newRow = dt.NewRow();

				for(int c = 0; c < cols; c++) {
					newRow[c] = $"R{r}C{c}";
				}

				dt.Rows.Add(newRow);
			}
			
			return dt;
		}
	}
}
