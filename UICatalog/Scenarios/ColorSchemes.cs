using System.Data;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Color Schemes", Description: "Shows All Color Schemes.")]
	[ScenarioCategory ("Colors")]
	class ColorSchemes : Scenario {
		public override void Setup ()
		{
			var tableView = new TableView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};
			var tableStyle = new TableView.TableStyle ();
			tableStyle.AlwaysShowHeaders = true;
			tableStyle.ShowHorizontalHeaderUnderline = true;
			tableView.Style = tableStyle;

			Win.Add (tableView);

			tableView.Table = new DataTable ();

			tableView.Table.Columns.Add (new DataColumn ("Scheme", typeof (string)));
			tableView.Style.ColumnStyles.Add (tableView.Table.Columns ["Scheme"],
				new TableView.ColumnStyle () { MinWidth = 10, MaxWidth = 10, Alignment = TextAlignment.Left });

			var attrs = typeof(ColorScheme).GetProperties ();
			foreach (var prop in attrs) {
				tableView.Table.Columns.Add (new DataColumn (prop.Name, typeof (string)));
				tableView.Style.ColumnStyles.Add (tableView.Table.Columns [prop.Name], 
					new TableView.ColumnStyle () { MinWidth = 15, MaxWidth = 15, Alignment = TextAlignment.Left });
			}

			var row = tableView.Table.NewRow ();
			row ["Scheme"] = "TopLevel";
			tableView.Table.Rows.Add (row);
			row = tableView.Table.NewRow ();
			row ["Scheme"] = "Base";
			tableView.Table.Rows.Add (row);
			row = tableView.Table.NewRow ();
			row ["Scheme"] = "Menu";
			tableView.Table.Rows.Add (row);
			row = tableView.Table.NewRow ();
			row ["Scheme"] = "Dialog";
			tableView.Table.Rows.Add (row);
			row = tableView.Table.NewRow ();
			row ["Scheme"] = "Error";
			tableView.Table.Rows.Add (row);


			var vx = 15;
			var x = 15;
			var y = 15;
			var colors = System.Enum.GetValues (typeof (Color));

		
			foreach (Color bg in colors) {
				var vl = new Label (bg.ToString (), TextDirection.TopBottom_LeftRight) {
					X = vx,
					Y = 20,
					Width = 1,
					Height = 13,
					VerticalTextAlignment = VerticalTextAlignment.Bottom,
					ColorScheme = new ColorScheme () { Normal = new Attribute (bg, colors.Length - 1 - bg) }
				};
				Win.Add (vl);
				var hl = new Label (bg.ToString ()) {
					X = 1,
					Y = y,
					Width = 13,
					Height = 1,
					TextAlignment = TextAlignment.Right,
					ColorScheme = new ColorScheme () { Normal = new Attribute (bg, colors.Length - 1 - bg) }
				};
				Win.Add (hl);
				vx++;
				foreach (Color fg in colors) {
					var c = new Attribute (fg, bg);
					var t = x.ToString ();
					var l = new Label (x, y, t [t.Length - 1].ToString ()) {
						ColorScheme = new ColorScheme () { Normal = c }
					};
					Win.Add (l);
					x++;
				}
				x = 15;
				y++;
			}
		}
	}
}