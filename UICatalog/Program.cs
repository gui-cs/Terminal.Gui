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

		public class Scenario {
			public ustring Name { get; set; }
			public ustring Description { get; set; }
			public override string ToString () => $"{Name, -30}{Description}";
		}

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
				int widestName = Scenarios.OrderByDescending (i => i.Name.Length).FirstOrDefault ().Name.Length;
				// Equivalent to an interpolated string like $"{Scenarios[item].Name, -widtestname}"; if such a thing were possible
				var s = String.Format (String.Format ("{{0,{0}}}", -widestName), Scenarios [item].Name);
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

		static void Main (string [] args)
		{
			if (Debugger.IsAttached)
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");

			Application.Init ();

			var top = Application.Top;

			var win = new Window ("UI Catalog") {
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

			List<Scenario> scenarios = new List<Scenario> () {
				new Scenario() { Name = "Scenario 1", Description = "This is the decription for Scenario 1."},
				new Scenario() { Name = "Scenario 2", Description = "Scenario 2 shows off x, y, and z." },
				new Scenario() { Name = "Scenario Number 3", Description = "#3 is all about fun!" }
			};


			var itemSource = new ScenarioListDataSource (scenarios);

			var listView = new ListView (itemSource) {
				X = Pos.Left(win) + 1,
				Y = Pos.Top(win) + 1,
				Width = Dim.Fill (2),
				Height = Dim.Fill (2),
				//AllowsMarking = _applicationData.OutputMode != OutputModeOption.None,
			};
			top.Add (listView);


			var statusBar = new StatusBar (new StatusItem [] {
				//new StatusItem(Key.F1, "~F1~ Help", () => Help()),
				new StatusItem(Key.Esc, "~ESC~ Quit", () => Application.RequestStop() ),
			});
			top.Add (statusBar);

			Application.Run ();
		}
	}
}
