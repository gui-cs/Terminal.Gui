using NStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Terminal.Gui;

namespace UICatalog {

	/// <summary>
	/// Base class for each demo/scenario. To define a new sceanrio simply 
	/// 1) declare a class derived from Scenario, 
	/// 2) Initializing Name and Description as appropriate,
	/// 3) Implement Run.
	/// The Main program uses reflection to find all sceanarios and adds them to the 
	/// ListView. Press ENTER to run the selected sceanrio. Press ESC to exit it.
	/// </summary>
	public class Scenario {
		public ustring Name { get; set; }
		public ustring Description { get; set; }
		public override string ToString () => $"{Name,-30}{Description}";

		public virtual void Run (Toplevel top) { 
		}

		/// <summary>
		/// https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
		/// </summary>
		public static ICollection<Scenario> GetDerivedClassesCollection ()
		{
			List<Scenario> objects = new List<Scenario> ();
			foreach (Type type in typeof (Scenario).Assembly.GetTypes ()
			    .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf (typeof (Scenario)))) {
				objects.Add ((Scenario)Activator.CreateInstance (type));
			}
			return objects;
		}
	}


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
				var s = String.Format (String.Format ("{{0,{0}}}", -_nameColumnWidth), Scenarios [item].Name);
				RenderUstr (driver, $"{s}  {Scenarios [item].Description}" , col, line, width);
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

		static int _nameColumnWidth;
		static void Main (string [] args)
		{
			if (Debugger.IsAttached)
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");

			Application.Init ();

			var top = Application.Top;

			var win = new Window ("Terminal.Gui UI Catalog") {
				X = 1,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill () - 1
			};
			top.Add (win);

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_File Demos", "", () => { }, () => false),
					null,
					new MenuItem ("_Quit", "", () => Application.RequestStop() )
				}),
			});
			top.Add (menu);

			List<Scenario> scenarios = Scenario.GetDerivedClassesCollection ().ToList();

			_nameColumnWidth = scenarios.OrderByDescending (i => i.Name.Length).FirstOrDefault ().Name.Length;
			// Equivalent to an interpolated string like $"{str, -widtestname}"; if such a thing were possible
			var header = new Label ($"{String.Format (String.Format ("{{0,{0}}}", -_nameColumnWidth), "Scenario")}  Description") {
				X = 1,
				Y = 0,
				Width = Dim.Fill (3),
				Height = 1,
			};
			win.Add (header);

			var headerSeparator = new Label ($"{new string('-', _nameColumnWidth)}  {new string ('-', "Description".Length)}") {
				X = 1,
				Y = 1,
				Width = Dim.Fill (3),
				Height = 1,
			};
			win.Add (headerSeparator);

			var itemSource = new ScenarioListDataSource (scenarios);
			var listView = new ListView () {
				X = 3,
				Y = 4,
				Width = header.Width,
				Height = Dim.Fill (2),
				AllowsMarking = false,
			};
			top.Add (listView);

			listView.CanFocus = true;
			listView.Source = itemSource;

			var statusBar = new StatusBar (new StatusItem [] {
				//new StatusItem(Key.F1, "~F1~ Help", () => Help()),
				new StatusItem(Key.Esc, "~ESC~ Quit", () => Application.RequestStop() ),
				new StatusItem(Key.Enter, "~ENTER~ Run Selected Scenario", () => {
					scenarios[listView.SelectedItem].Run(top);
				}),
			});
			top.Add (statusBar);

			top.SetFocus (listView);

			Application.Run ();
		}
	}
}
