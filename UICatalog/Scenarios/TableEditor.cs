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
			Win.Add (tableView);
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
					"Some long text with unicode '😀'",
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
