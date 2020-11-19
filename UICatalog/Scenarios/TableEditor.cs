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
			var dt = BuildDemoDataTable(30,1000);

			Win.Title = this.GetName() + "-" + dt.TableName ?? "Untitled";
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			this.tableView = new TableView (dt) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			tableView.CanFocus = true;

			tableView.KeyPress += KeyPressed; 

			Win.Add (tableView);
		}

		private void KeyPressed (View.KeyEventEventArgs obj)
		{
			if(obj.KeyEvent.Key == Key.Enter) {
				EditCurrentCell();
			}
			
		}

		private void EditCurrentCell ()
		{
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
				
				tableView.RefreshViewport();
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
	}
}
