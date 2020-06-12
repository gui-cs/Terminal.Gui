using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Terminal.Gui;
using Rune = System.Rune;

/// <remarks>
/// <para>
///	UI Catalog attempts to satisfy the following goals:
/// </para>
/// <para>
/// <list type="number">
///	<item>
///		<description>
///		Be an easy to use showcase for Terminal.Gui concepts and features.
///		</description>
///	</item>
///	<item>
///		<description>
///		Provide sample code that illustrates how to properly implement said concepts & features.
///		</description>
///	</item>
///	<item>
///		<description>
///		Make it easy for contributors to add additional samples in a structured way.
///		</description>
///	</item>
/// </list>
/// </para>	
/// <para>
///	See the project README for more details (https://github.com/migueldeicaza/gui.cs/tree/master/UICatalog/README.md).
/// </para>	
/// </remarks>

namespace UICatalog {
	/// <summary>
	/// UI Catalog is a comprehensive sample app and scenario library for <see cref="Terminal.Gui"/>
	/// </summary>
	public class UICatalogApp {
		private static Toplevel _top;
		private static MenuBar _menu;
		private static int _nameColumnWidth;
		private static FrameView _leftPane;
		private static List<string> _categories;
		private static ListView _categoryListView;
		private static FrameView _rightPane;
		private static List<Type> _scenarios;
		private static ListView _scenarioListView;
		private static StatusBar _statusBar;
		private static StatusItem _capslock;
		private static StatusItem _numlock;
		private static StatusItem _scrolllock;

		private static Scenario _runningScenario = null;
		private static bool _useSystemConsole = false;

		static void Main (string [] args)
		{
			if (Debugger.IsAttached)
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");

			_scenarios = Scenario.GetDerivedClasses<Scenario> ().OrderBy (t => Scenario.ScenarioMetadata.GetName (t)).ToList ();

			if (args.Length > 0) {
				var item = _scenarios.FindIndex (t => Scenario.ScenarioMetadata.GetName (t).Equals (args [0], StringComparison.OrdinalIgnoreCase));
				_runningScenario = (Scenario)Activator.CreateInstance (_scenarios [item]);
				Application.Init ();
				_runningScenario.Init (Application.Top, _baseColorScheme);
				_runningScenario.Setup ();
				_runningScenario.Run ();
				_runningScenario = null;
				return;
			}

			Scenario scenario = GetScenarioToRun ();
			while (scenario != null) {
				Application.UseSystemConsole = _useSystemConsole;
				Application.Init ();
				scenario.Init (Application.Top, _baseColorScheme);
				scenario.Setup ();
				scenario.Run ();
				scenario = GetScenarioToRun ();
			}
		}

		/// <summary>
		/// This shows the selection UI. Each time it is run, it calls Application.Init to reset everything.
		/// </summary>
		/// <returns></returns>
		private static Scenario GetScenarioToRun ()
		{
			Application.UseSystemConsole = false;
			Application.Init ();

			if (_menu == null) {
				Setup ();
			}

			_top = Application.Top;

			_top.KeyDown += KeyDownHandler;

			_top.Add (_menu);
			_top.Add (_leftPane);
			_top.Add (_rightPane);
			_top.Add (_statusBar);

			_top.Ready += () => {
				if (_runningScenario != null) {
					_top.SetFocus (_rightPane);
					_runningScenario = null;
				}
			};

			Application.Run (_top, true);
			Application.Shutdown ();
			return _runningScenario;
		}

		static MenuItem [] CreateDiagnosticMenuItems ()
		{
			MenuItem CheckedMenuMenuItem (ustring menuItem, Action action, Func<bool> checkFunction)
			{
				var mi = new MenuItem ();
				mi.Title = menuItem;
				mi.CheckType |= MenuItemCheckStyle.Checked;
				mi.Checked = checkFunction ();
				mi.Action = () => {
					action?.Invoke ();
					mi.Title = menuItem;
					mi.Checked = checkFunction ();
				};
				return mi;
			}

			return new MenuItem [] {
				CheckedMenuMenuItem ("Use _System Console",
					() => {
						_useSystemConsole = !_useSystemConsole;
					},
					() => _useSystemConsole),
				CheckedMenuMenuItem ("Diagnostics: _Frame Padding",
					() => {
						ConsoleDriver.Diagnostics ^= ConsoleDriver.DiagnosticFlags.FramePadding;
						_top.SetNeedsDisplay ();
					},
					() => (ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FramePadding) == ConsoleDriver.DiagnosticFlags.FramePadding),
				CheckedMenuMenuItem ("Diagnostics: Frame _Ruler",
					() => {
						ConsoleDriver.Diagnostics ^= ConsoleDriver.DiagnosticFlags.FrameRuler;
						_top.SetNeedsDisplay ();
					},
					() => (ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FrameRuler) == ConsoleDriver.DiagnosticFlags.FrameRuler),
			};
		}

		static void SetColorScheme ()
		{
			_leftPane.ColorScheme = _baseColorScheme;
			_rightPane.ColorScheme = _baseColorScheme;
			_top?.SetNeedsDisplay ();
		}

		static ColorScheme _baseColorScheme;
		static MenuItem [] CreateColorSchemeMenuItems ()
		{
			List<MenuItem> menuItems = new List<MenuItem> ();
			foreach (var sc in Colors.ColorSchemes) {
				var item = new MenuItem ();
				item.Title = sc.Key;
				item.CheckType |= MenuItemCheckStyle.Radio;
				item.Checked = sc.Value == _baseColorScheme;
				item.Action += () => {
					_baseColorScheme = sc.Value;
					SetColorScheme ();
					foreach (var menuItem in menuItems) {
						menuItem.Checked = menuItem.Title.Equals (sc.Key) && sc.Value == _baseColorScheme;
					}
				};
				menuItems.Add (item);
			}
			return menuItems.ToArray ();
		}

		/// <summary>
		/// Create all controls. This gets called once and the controls remain with their state between Sceanrio runs.
		/// </summary>
		private static void Setup ()
		{
			// Set this here because not initilzied until driver is loaded
			_baseColorScheme = Colors.Base;

			StringBuilder aboutMessage = new StringBuilder ();
			aboutMessage.AppendLine ("UI Catalog is a comprehensive sample library for Terminal.Gui");
			aboutMessage.AppendLine ("");
			aboutMessage.AppendLine ($"Version: {typeof (UICatalogApp).Assembly.GetName ().Version}");
			aboutMessage.AppendLine ($"Using Terminal.Gui Version: {typeof (Terminal.Gui.Application).Assembly.GetName ().Version}");
			aboutMessage.AppendLine ("");

			_menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Application.RequestStop() )
				}),
				new MenuBarItem ("_Color Scheme", CreateColorSchemeMenuItems()),
				new MenuBarItem ("_Diagostics", CreateDiagnosticMenuItems()),
				new MenuBarItem ("_About...", "About this app", () =>  MessageBox.Query ("About UI Catalog", aboutMessage.ToString(), "Ok")),
			});

			_leftPane = new FrameView ("Categories") {
				X = 0,
				Y = 1, // for menu
				Width = 25,
				Height = Dim.Fill (1),
				CanFocus = false,
			};


			_categories = Scenario.GetAllCategories ().OrderBy (c => c).ToList ();
			_categoryListView = new ListView (_categories) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (0),
				AllowsMarking = false,
				CanFocus = true,
			};
			_categoryListView.OpenSelectedItem += (a) => {
				_top.SetFocus (_rightPane);
			};
			_categoryListView.SelectedItemChanged += CategoryListView_SelectedChanged;
			_leftPane.Add (_categoryListView);

			_rightPane = new FrameView ("Scenarios") {
				X = 25,
				Y = 1, // for menu
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
				CanFocus = true,

			};

			_nameColumnWidth = Scenario.ScenarioMetadata.GetName (_scenarios.OrderByDescending (t => Scenario.ScenarioMetadata.GetName (t).Length).FirstOrDefault ()).Length;

			_scenarioListView = new ListView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (0),
				AllowsMarking = false,
				CanFocus = true,
			};

			_scenarioListView.OpenSelectedItem += _scenarioListView_OpenSelectedItem;
			_rightPane.Add (_scenarioListView);

			_categoryListView.SelectedItem = 0;
			_categoryListView.OnSelectedChanged ();

			_capslock = new StatusItem (Key.CharMask, "Caps", null);
			_numlock = new StatusItem (Key.CharMask, "Num", null);
			_scrolllock = new StatusItem (Key.CharMask, "Scroll", null);

			_statusBar = new StatusBar (new StatusItem [] {
				_capslock,
				_numlock,
				_scrolllock,
				new StatusItem(Key.ControlQ, "~CTRL-Q~ Quit", () => {
					if (_runningScenario is null){
						// This causes GetScenarioToRun to return null
						_runningScenario = null;
						Application.RequestStop();
					} else {
						_runningScenario.RequestStop();
					}
				}),
			});

			SetColorScheme ();
		}

		private static void _scenarioListView_OpenSelectedItem (EventArgs e)
		{
			if (_runningScenario is null) {
				var source = _scenarioListView.Source as ScenarioListDataSource;
				_runningScenario = (Scenario)Activator.CreateInstance (source.Scenarios [_scenarioListView.SelectedItem]);
				Application.RequestStop ();
			}
		}

		internal class ScenarioListDataSource : IListDataSource {
			public List<Type> Scenarios { get; set; }

			public bool IsMarked (int item) => false;

			public int Count => Scenarios.Count;

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

		/// <summary>
		/// When Scenarios are running we need to override the behavior of the Menu 
		/// and Statusbar to enable Scenarios that use those (or related key input)
		/// to not be impacted. Same as for tabs.
		/// </summary>
		/// <param name="ke"></param>
		private static void KeyDownHandler (View.KeyEventEventArgs a)
		{
			//if (a.KeyEvent.Key == Key.Tab || a.KeyEvent.Key == Key.BackTab) {
			//	// BUGBUG: Work around Issue #434 by implementing our own TAB navigation
			//	if (_top.MostFocused == _categoryListView)
			//		_top.SetFocus (_rightPane);
			//	else
			//		_top.SetFocus (_leftPane);
			//}

			if (a.KeyEvent.IsCapslock) {
				_capslock.Title = "Caps: On";
				_statusBar.SetNeedsDisplay ();
			} else {
				_capslock.Title = "Caps: Off";
				_statusBar.SetNeedsDisplay ();
			}

			if (a.KeyEvent.IsNumlock) {
				_numlock.Title = "Num: On";
				_statusBar.SetNeedsDisplay ();
			} else {
				_numlock.Title = "Num: Off";
				_statusBar.SetNeedsDisplay ();
			}

			if (a.KeyEvent.IsScrolllock) {
				_scrolllock.Title = "Scroll: On";
				_statusBar.SetNeedsDisplay ();
			} else {
				_scrolllock.Title = "Scroll: Off";
				_statusBar.SetNeedsDisplay ();
			}
		}

		private static void CategoryListView_SelectedChanged (ListViewItemEventArgs e)
		{
			var item = _categories [_categoryListView.SelectedItem];
			List<Type> newlist;
			if (item.Equals ("All")) {
				newlist = _scenarios;

			} else {
				newlist = _scenarios.Where (t => Scenario.ScenarioCategory.GetCategories (t).Contains (item)).ToList ();
			}
			_scenarioListView.Source = new ScenarioListDataSource (newlist);
			_scenarioListView.SelectedItem = 0;
		}
	}
}
