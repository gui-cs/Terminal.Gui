using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "List View With Selection", Description: "ListView with colunns and selection")]
	[ScenarioCategory ("Controls")]
	class ListViewWithSelection : Scenario {

		public CheckBox _customRenderCB;
		public CheckBox _allowMarkingCB;
		public CheckBox _allowMultipleCB;
		public ListView _listView;

		public List<Type> _scenarios = Scenario.GetDerivedClasses<Scenario>().OrderBy (t => Scenario.ScenarioMetadata.GetName (t)).ToList ();

		public override void Setup ()
		{
			_customRenderCB = new CheckBox ("Render with columns") {
				X = 0,
				Y = 0,
				Height = 1,
			};
			Win.Add (_customRenderCB);
			_customRenderCB.Toggled += _customRenderCB_Toggled; ;

			_allowMarkingCB = new CheckBox ("Allow Marking") {
				X = Pos.Right (_customRenderCB) + 1,
				Y = 0,
				Height = 1,
			};
			Win.Add (_allowMarkingCB);
			_allowMarkingCB.Toggled += AllowMarkingCB_Toggled;

			_allowMultipleCB = new CheckBox ("Allow Multi-Select") {
				X = Pos.Right (_allowMarkingCB) + 1,
				Y = 0,
				Height = 1,
				Visible = _allowMarkingCB.Checked
			};
			Win.Add (_allowMultipleCB);
			_allowMultipleCB.Toggled += AllowMultipleCB_Toggled;

			_listView = new ListView () {
				X = 1,
				Y = 2,
				Height = Dim.Fill (),
				Width = Dim.Fill (1),
				//ColorScheme = Colors.TopLevel,
				AllowsMarking = false,
				AllowsMultipleSelection = false
			};
			Win.Add (_listView);

			
			_listView.SetSource (_scenarios);

		}

		private void _customRenderCB_Toggled (bool prev)
		{
			if (prev) {
				_listView.SetSource (_scenarios);
			} else {
				_listView.Source = new ScenarioListDataSource (_scenarios);
			}

			Win.SetNeedsDisplay ();
		}

		private void AllowMarkingCB_Toggled (bool prev)
		{
			_listView.AllowsMarking = !prev;
			_allowMultipleCB.Visible = _listView.AllowsMarking;
			Win.SetNeedsDisplay ();
		}

		private void AllowMultipleCB_Toggled (bool prev)
		{
			_listView.AllowsMultipleSelection = !prev;
			Win.SetNeedsDisplay ();
		}

		// This is basicaly the same implementation used by the UICatalog main window
		internal class ScenarioListDataSource : IListDataSource {
			int _nameColumnWidth = 30;
			private List<Type> scenarios;
			BitArray marks;
			int count;

			public List<Type> Scenarios {
				get => scenarios; 
				set {
					if (value != null) {
						count = value.Count;
						marks = new BitArray (count);
						scenarios = value;
					}
				}
			}
			public bool IsMarked (int item)
			{
				if (item >= 0 && item < count)
					return marks [item];
				return false;
			}

			public int Count => Scenarios != null ? Scenarios.Count : 0;

			public ScenarioListDataSource (List<Type> itemList) => Scenarios = itemList;

			public void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width)
			{
				container.Move (col, line);
				// Equivalent to an interpolated string like $"{Scenarios[item].Name, -widtestname}"; if such a thing were possible
				var s = String.Format (String.Format ("{{0,{0}}}", -_nameColumnWidth), Scenario.ScenarioMetadata.GetName (Scenarios [item]));
				RenderUstr (driver, $"{s}  {Scenario.ScenarioMetadata.GetDescription (Scenarios [item])}", col, line, width);
			}

			public void SetMark (int item, bool value)
			{
				if (item >= 0 && item < count)
					marks [item] = value;
			}

			// A slightly adapted method from: https://github.com/migueldeicaza/gui.cs/blob/fc1faba7452ccbdf49028ac49f0c9f0f42bbae91/Terminal.Gui/Views/ListView.cs#L433-L461
			private void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width)
			{
				int used = 0;
				int index = 0;
				while (index < ustr.Length) {
					(var rune, var size) = Utf8.DecodeRune (ustr, index, index - ustr.Length);
					var count = Rune.ColumnWidth (rune);
					if (used + count >= width) break;
					driver.AddRune (rune);
					used += count;
					index += size;
				}

				while (used < width) {
					driver.AddRune (' ');
					used++;
				}
			}

			public IList ToList ()
			{
				return Scenarios;
			}
		}
	}
}