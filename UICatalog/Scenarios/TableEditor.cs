using System;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;
using Terminal.Gui.Views;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "TableEditor", Description: "A Terminal.Gui DataTable editor via TableView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("TopLevel")]
	public class TableEditor : Scenario 
	{
		TableView tableView;

		public override void Setup ()
		{
			Win.Title = this.GetName();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_OpenBigExample", "", () => OpenExample(true)),
					new MenuItem ("_OpenSmallExample", "", () => OpenExample(false)),
					new MenuItem ("_CloseExample", "", () => CloseExample()),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					new MenuItem ("_AlwaysShowHeaders", "", () => ToggleAlwaysShowHeader()),
					new MenuItem ("_HeaderOverLine", "", () => ToggleOverline()),
					new MenuItem ("_HeaderMidLine", "", () => ToggleHeaderMidline()),
					new MenuItem ("_HeaderUnderLine", "", () => ToggleUnderline()),
					new MenuItem ("_CellLines", "", () => ToggleCellLines()),
					new MenuItem ("_AllLines", "", () => ToggleAllCellLines()),
					new MenuItem ("_NoLines", "", () => ToggleNoCellLines()),
				}),
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				//new StatusItem(Key.Enter, "~ENTER~ ApplyEdits", () => { _hexView.ApplyEdits(); }),
				new StatusItem(Key.F2, "~F2~ OpenExample", () => OpenExample(true)),
				new StatusItem(Key.F3, "~F3~ EditCell", () => EditCurrentCell()),
				new StatusItem(Key.F4, "~F4~ CloseExample", () => CloseExample()),
				new StatusItem(Key.F5, "~F5~ OpenSimple", () => OpenSimple(true)),
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);

			this.tableView = new TableView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			Win.Add (tableView);
		}



		private void ToggleAlwaysShowHeader ()
		{
			tableView.Style.AlwaysShowHeaders = !tableView.Style.AlwaysShowHeaders;
			tableView.Update();
		}

		private void ToggleOverline ()
		{
			tableView.Style.ShowHorizontalHeaderOverline = !tableView.Style.ShowHorizontalHeaderOverline;
			tableView.Update();
		}
		private void ToggleHeaderMidline ()
		{
			tableView.Style.ShowVerticalHeaderLines = !tableView.Style.ShowVerticalHeaderLines;
			tableView.Update();
		}
		private void ToggleUnderline ()
		{
			tableView.Style.ShowHorizontalHeaderUnderline = !tableView.Style.ShowHorizontalHeaderUnderline;
			tableView.Update();
		}
		private void ToggleCellLines()
		{
			tableView.Style.ShowVerticalCellLines = !tableView.Style.ShowVerticalCellLines;
			tableView.Update();
		}
		private void ToggleAllCellLines()
		{
			tableView.Style.ShowHorizontalHeaderOverline = true;
			tableView.Style.ShowVerticalHeaderLines = true;
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowVerticalCellLines = true;
			tableView.Update();
		}
		private void ToggleNoCellLines()
		{
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.ShowVerticalHeaderLines = false;
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowVerticalCellLines = false;
			tableView.Update();
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
		}
		private void OpenSimple (bool big)
		{
			
			tableView.Table = BuildSimpleDataTable(big ? 30 : 5, big ? 1000 : 5);
		}

		private void EditCurrentCell ()
		{
			if(tableView.Table == null)
				return;

			var oldValue = tableView.Table.Rows[tableView.SelectedRow][tableView.SelectedColumn].ToString();
			bool okPressed = false;

			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += () => { okPressed = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += () => { Application.RequestStop (); };
			var d = new Dialog ("Enter new value", 60, 20, ok, cancel);

			var lbl = new Label() {
				X = 0,
				Y = 1,
				Text = tableView.Table.Columns[tableView.SelectedColumn].ColumnName
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
					tableView.Table.Rows[tableView.SelectedRow][tableView.SelectedColumn] = string.IsNullOrWhiteSpace(tf.Text.ToString()) ? DBNull.Value : (object)tf.Text;
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

			dt.Columns.Add(new DataColumn("StrCol",typeof(string)));
			dt.Columns.Add(new DataColumn("DateCol",typeof(DateTime)));
			dt.Columns.Add(new DataColumn("IntCol",typeof(int)));
			dt.Columns.Add(new DataColumn("DoubleCol",typeof(double)));
			dt.Columns.Add(new DataColumn("NullsCol",typeof(string)));

			for(int i=0;i< cols -5; i++) {
				dt.Columns.Add("Column" + (i+4));
			}
			
			var r = new Random(100);

			for(int i=0;i< rows;i++) {
				
				List<object> row = new List<object>(){ 
					"Some long text that is super cool",
					new DateTime(2000+i,12,25),
					r.Next(i),
					r.NextDouble()*i,
					DBNull.Value
				};
				
				for(int j=0;j< cols -5; j++) {
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
