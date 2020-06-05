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
			List<string> items = new List<string> ();
			foreach (var dir in new [] { "/etc", @"\windows\System32" }) {
				if (Directory.Exists (dir)) {
					items = Directory.GetFiles (dir).Union(Directory.GetDirectories(dir))
					.Select (Path.GetFileName)
					.Where (x => char.IsLetterOrDigit (x [0]))
					.OrderBy (x => x).ToList ();
				}
			}

			Dim width = 30;

			// ListView
			var lbListView = new Label ("Listview") {
				ColorScheme = Colors.TopLevel,
				X = 0,
				Width = width
			};

			var listview = new ListView (items) {
				X = 0,
				Y = Pos.Bottom (lbListView) + 1,
				Height = Dim.Fill(2),
				Width = width
			};
			listview.OpenSelectedItem += (object sender, ListViewItemEventArgs e) => lbListView.Text = items [listview.SelectedItem];
			Win.Add (lbListView, listview);

			// ComboBox
			var lbComboBox = new Label ("ComboBox") {
				ColorScheme = Colors.TopLevel,
				X = Pos.Right (lbListView) + 1,
				Width = width
			};

			var comboBox = new ComboBox () {
				X = Pos.Right (listview) + 1,
				Y = Pos.Bottom (lbListView) + 1,
				Height = Dim.Fill (2),
				Width = width
			};
			comboBox.SetSource (items);

			comboBox.Changed += (object sender, ustring text) => lbComboBox.Text = text;
			Win.Add (lbComboBox, comboBox);
		}
	}
}
