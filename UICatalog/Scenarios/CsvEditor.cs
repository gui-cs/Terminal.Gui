using System;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Csv Editor", Description: "Open and edit simple CSV files")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("TopLevel")]
	public class CsvEditor : Scenario 
	{
		TableView tableView;
		private string currentFile;

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
					new MenuItem ("_Open CSV", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Rename Column", "", () => RenameColumn()),
				}),
				new MenuBarItem ("_Insert", new MenuItem [] {
					new MenuItem ("_New Column", "", () => AddColumn()),
					new MenuItem ("_New Row", "", () => AddRow()),
				})
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.O, "~^O~ Open", () => Open()),
				new StatusItem(Key.CtrlMask | Key.S, "~^S~ Save", () => Save()),
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

			tableView.SelectedCellChanged += (e)=>{selectedCellLabel.Text = $"{tableView.SelectedRow},{tableView.SelectedColumn}";};
			tableView.CellActivated += EditCurrentCell;
			tableView.KeyPress += TableViewKeyPress;

			SetupScrollBar();
		}

		private void RenameColumn ()
		{
			if(NoTableLoaded()) {
				return;
			}

			var currentCol = tableView.Table.Columns[tableView.SelectedColumn];

			if(GetText("Rename Column","Name:",currentCol.ColumnName,out string newName)) {
				currentCol.ColumnName = newName;
				tableView.Update();
			}


		}

		private bool NoTableLoaded ()
		{
			if(tableView.Table == null) {
				MessageBox.ErrorQuery("No Table Loaded","No table has currently be opened","Ok");
				return true;
			}

			return false;
		}

		private void AddRow ()
		{
			if(NoTableLoaded()) {
				return;
			}

			tableView.Table.Rows.Add();
			tableView.Update();
		}

		private void AddColumn ()
		{
			if(NoTableLoaded()) {
				return;
			}

			if(GetText("Enter column name","Name:","",out string colName)) {
				tableView.Table.Columns.Add(new DataColumn(colName));
				tableView.Update();
			}
				
		}

		private void Save()
		{
			if(tableView.Table == null || string.IsNullOrWhiteSpace(currentFile)) {
				MessageBox.ErrorQuery("No file loaded","No file is currently loaded","Ok");
				return;
			}

			var sb = new StringBuilder();

			sb.AppendLine(string.Join(",",tableView.Table.Columns.Cast<DataColumn>().Select(c=>c.ColumnName)));

			foreach(DataRow row in tableView.Table.Rows) {
				sb.AppendLine(string.Join(",",row.ItemArray));
			}
			
			File.WriteAllText(currentFile,sb.ToString());
		}

		private void Open()
		{
			var ofd = new FileDialog("Select File","Open","File","Select a CSV file to open (does not support newlines, escaping etc)");
			ofd.AllowedFileTypes = new string[]{".csv" };

			Application.Run(ofd);
			
			if(!string.IsNullOrWhiteSpace(ofd.FilePath?.ToString()))
			{
				Open(ofd.FilePath.ToString());
			}
		}
		
		private void Open(string filename)
		{
			
			int lineNumber = 0;
			currentFile = null;

			try {
				var dt = new DataTable();
				var lines = File.ReadAllLines(filename);
			
				foreach(var h in lines[0].Split(',')){
					dt.Columns.Add(h);
				}
				

				foreach(var line in lines.Skip(1)) {
					lineNumber++;
					dt.Rows.Add(line.Split(','));
				}
				
				tableView.Table = dt;
				
				// Only set the current filename if we succesfully loaded the entire file
				currentFile = filename;
			}
			catch(Exception ex) {
				MessageBox.ErrorQuery("Open Failed",$"Error on line {lineNumber}{Environment.NewLine}{ex.Message}","Ok");
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
			var alignMid = new ColumnStyle() {
				Alignment = TextAlignment.Centered
			};
			var alignRight = new ColumnStyle() {
				Alignment = TextAlignment.Right
			};

			var dateFormatStyle = new ColumnStyle() {
				Alignment = TextAlignment.Right,
				RepresentationGetter = (v)=> v is DateTime d ? d.ToString("yyyy-MM-dd"):v.ToString()
			};

			var negativeRight = new ColumnStyle() {
				
				Format = "0.##",
				MinWidth = 10,
				AlignmentGetter = (v)=>v is double d ? 
								// align negative values right
								d < 0 ? TextAlignment.Right : 
								// align positive values left
								TextAlignment.Left:
								// not a double
								TextAlignment.Left
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
		private bool GetText(string title, string label, string initialText, out string enteredText)
		{
			bool okPressed = false;

			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += () => { okPressed = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += () => { Application.RequestStop (); };
			var d = new Dialog (title, 60, 20, ok, cancel);

			var lbl = new Label() {
				X = 0,
				Y = 1,
				Text = label
			};

			var tf = new TextField()
				{
					Text = initialText,
					X = 0,
					Y = 2,
					Width = Dim.Fill()
				};
			
			d.Add (lbl,tf);
			tf.SetFocus();

			Application.Run (d);

			enteredText = okPressed? tf.Text.ToString() : null;
			return okPressed;
		}
		private void EditCurrentCell (CellActivatedEventArgs e)
		{
			if(e.Table == null)
				return;

			var oldValue = e.Table.Rows[e.Row][e.Col].ToString();

			if(GetText("Enter new value",e.Table.Columns[e.Col].ColumnName,oldValue, out string newText)) {
				try {
					e.Table.Rows[e.Row][e.Col] = string.IsNullOrWhiteSpace(newText) ? DBNull.Value : (object)newText;
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
