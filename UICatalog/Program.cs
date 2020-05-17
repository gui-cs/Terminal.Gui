using NStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {
	/// <summary>
	/// Main program for the Terminal.gui UI Catalog app. This app provides a chooser that allows
	/// for a calalog of UI demos, examples, and tests.
	/// </summary>
	class Program {
		internal class ScenarioListDataSource : IListDataSource {
			public List<Scenario> Scenarios { get; set; }

			public bool IsMarked (int item) => false;//  Scenarios [item].IsMarked;

			public int Count => Scenarios.Count;

			public ScenarioListDataSource (List<Scenario> itemList)
			{
				Scenarios = itemList;
			}

			public void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width)
			{
				container.Move (col, line);
				// Equivalent to an interpolated string like $"{Scenarios[item].Name, -widtestname}"; if such a thing were possible
				var s = String.Format (String.Format ("{{0,{0}}}", -_nameColumnWidth), Scenarios [item].GetName ());
				RenderUstr (driver, $"{s}  {Scenarios [item].GetDescription ()}", col, line, width);
			}

			public void SetMark (int item, bool value)
			{
				//Scenarios [item].IsMarked = value;
			}

			// A slightly adapted method from gui.cs: https://github.com/migueldeicaza/gui.cs/blob/fc1faba7452ccbdf49028ac49f0c9f0f42bbae91/Terminal.Gui/Views/ListView.cs#L433-L461
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
		}

		private static int _nameColumnWidth;
		private static List<string> _categories;
		private static ListView _categoryListView;
		private static List<Scenario> _scenarios;
		private static ListView _scenarioListView;
		private static string _startScenario;

		static void Main (string [] args)
		{
			if (Debugger.IsAttached)
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");

			Application.Init ();

			var top = Application.Top;
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Application.RequestStop() )
				}),
				new MenuBarItem ("_About...", "About this app", () =>  MessageBox.Query (0, 6, "About UI Catalog", "UI Catalog is a comprehensive sample library for gui.cs", "Ok")),
			});
			top.Add (menu);

			var leftPane = new Window ("Categories") {
				X = 0,
				Y = 1, // for menu
				Width = 25,
				Height = Dim.Fill (),
				CanFocus = false,
			};
			top.Add (leftPane);

			_categories = Scenario.GetAllCategories ();
			_categoryListView = new ListView (_categories) {
				X = 1,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (2),
				AllowsMarking = false,
				CanFocus = true,
			};
			_categoryListView.SelectedChanged += CategoryListView_SelectedChanged;
			leftPane.Add (_categoryListView);

			var rightPane = new Window ("Scenarios") {
				X = 25,
				Y = 1, // for menu
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				CanFocus = false,

			};
			top.Add (rightPane);

			_scenarios = Scenario.GetDerivedClassesCollection ().ToList ();

			_nameColumnWidth = _scenarios.OrderByDescending (i => i.GetName ().Length).FirstOrDefault ().GetName ().Length;
			//// Equivalent to an interpolated string like $"{str, -widtestname}"; if such a thing were possible
			//var header = new Label ($"{String.Format (String.Format ("{{0,{0}}}", -_nameColumnWidth), "Scenario")}  Description") {
			//	X = Pos.Right(categoryListView) + 1,
			//	Y = 0,
			//	Width = Dim.Fill (0),
			//	Height = 1,
			//	ColorScheme = Colors.Dialog,
			//	TextAlignment = Terminal.Gui.TextAlignment.Left,
			//};
			//win.Add (header);

			//var headerSeparator = new Label ($"{new string('-', _nameColumnWidth)}  {new string ('-', "Description".Length)}") {
			//	X = Pos.Right (categoryListView) + 1,
			//	Y = 1,
			//	Width = Dim.Fill (0),
			//	Height = 1,
			//	ColorScheme = Colors.Dialog
			//};
			//win.Add (headerSeparator);

			//var itemSource = new ScenarioListDataSource (scenarios);
			_scenarioListView = new ListView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (0),
				AllowsMarking = false,
				CanFocus = true,
			};

			rightPane.Add (_scenarioListView);

			_categoryListView.SelectedItem = 0;
			CategoryListView_SelectedChanged ();

			var statusBar = new StatusBar (new StatusItem [] {
				//new StatusItem(Key.F1, "~F1~ Help", () => Help()),
				new StatusItem(Key.Esc, "~ESC~ Quit", () => Application.RequestStop() ),
				new StatusItem(Key.Enter, "~ENTER~ Run Selected Scenario", () => {
					var source = _scenarioListView.Source as ScenarioListDataSource;
					source.Scenarios[_scenarioListView.SelectedItem].Run();
				}),
				new StatusItem(Key.Tab, "~TAB~ Tab", () => {
					top.FocusNext();				
				}),

			});
			top.Add (statusBar);

			if (args.Length > 0) {
				_startScenario = args [0];
				Application.Iteration += StartCommandLineScenario;
			}

			Application.Run (top);
		}

		/// <summary>
		/// This ensures the main app is up and running before the scenario on the command line runs.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void StartCommandLineScenario (object sender, EventArgs e)
		{
			Application.Iteration -= StartCommandLineScenario;
			var source = _scenarioListView.Source as ScenarioListDataSource;
			var item = source.Scenarios.FindIndex (s => s.GetName ().Equals (_startScenario, StringComparison.OrdinalIgnoreCase));
			source.Scenarios [item].Run ();
		}

		private static void CategoryListView_SelectedChanged ()
		{
			var item = _categories [_categoryListView.SelectedItem];
			List<Scenario> newlist;
			if (item.Equals ("All")) {
				newlist = _scenarios;

			} else {
				newlist = _scenarios.Where (s => s.GetCategories ().Contains (item)).ToList ();
			}
			_scenarioListView.Source = new ScenarioListDataSource (newlist);
			_scenarioListView.SelectedItem = 0;
		}
	}
}
