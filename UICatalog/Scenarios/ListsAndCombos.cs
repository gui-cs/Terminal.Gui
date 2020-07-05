using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Terminal.Gui;
using NStack;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ListView & ComboBox", Description: "Demonstrates a ListView populating a ComboBox that acts as a filter.")]
	[ScenarioCategory ("Controls")]
	class ListsAndCombos : Scenario {

		public override void Setup ()
		{
			//TODO: Duplicated code in Demo.cs Consider moving to shared assembly
			var items = new List<ustring> ();
			foreach (var dir in new [] { "/etc", @$"{Environment.GetEnvironmentVariable ("SystemRoot")}\System32" }) {
				if (Directory.Exists (dir)) {
					items = Directory.GetFiles (dir).Union(Directory.GetDirectories(dir))
						.Select (Path.GetFileName)
						.Where (x => char.IsLetterOrDigit (x [0]))
						.OrderBy (x => x).Select(x => ustring.Make(x)).ToList() ;
				}
			}

			// ListView
			var lbListView = new Label ("Listview") {
				ColorScheme = Colors.TopLevel,
				X = 0,
				Width = Dim.Percent (40)
			};

			var listview = new ListView (items) {
				X = 0,
				Y = Pos.Bottom (lbListView) + 1,
				Height = Dim.Fill(2),
				Width = Dim.Percent (40)
			};
			listview.SelectedItemChanged += (ListViewItemEventArgs e) => lbListView.Text = items [listview.SelectedItem];
			Win.Add (lbListView, listview);

			// ComboBox
			var lbComboBox = new Label ("ComboBox") {
				ColorScheme = Colors.TopLevel,
				X = Pos.Right (lbListView) + 1,
				Width = Dim.Percent(40)
			};

			var comboBox = new ComboBox () {
				X = Pos.Right (listview) + 1,
				Y = Pos.Bottom (lbListView) + 1,
				Height = Dim.Fill (2),
				Width = Dim.Percent(40)
			};
			comboBox.SetSource (items);

			comboBox.SelectedItemChanged += (ListViewItemEventArgs text) => lbComboBox.Text = items[comboBox.SelectedItem];
			Win.Add (lbComboBox, comboBox);
		}
	}
}
