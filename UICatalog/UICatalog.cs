using NStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Terminal.Gui;
using Rune = System.Rune;

/// <summary>
/// UI Catalog is a comprehensive sample library for Terminal.Gui. It provides a simple UI for adding to the catalog of scenarios.
/// </summary>
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
///	See the project README for more details (https://github.com/gui-cs/Terminal.Gui/tree/master/UICatalog/README.md).
/// </para>	
/// </remarks>
namespace UICatalog {
	/// <summary>
	/// UI Catalog is a comprehensive sample app and scenario library for <see cref="Terminal.Gui"/>
	/// </summary>
	public class UICatalogApp {
		private static int _nameColumnWidth;
		private static FrameView _leftPane;
		private static List<string> _categories;
		private static ListView _categoryListView;
		private static FrameView _rightPane;
		private static List<Scenario> _scenarios;
		private static ListView _scenarioListView;
		private static StatusBar _statusBar;
		private static StatusItem _capslock;
		private static StatusItem _numlock;
		private static StatusItem _scrolllock;

		// If set, holds the scenario the user selected
		private static Scenario _selectedScenario = null;
		
		private static bool _useSystemConsole = false;
		private static ConsoleDriver.DiagnosticFlags _diagnosticFlags;
		private static bool _heightAsBuffer = false;
		private static bool _isFirstRunning = true;

		// When a scenario is run, the main app is killed. These items
		// are therefore cached so that when the scenario exits the
		// main app UI can be restored to previous state
		private static int _cachedScenarioIndex = 0;
		private static int _cachedCategoryIndex = 0;

		private static StringBuilder _aboutMessage;

		static void Main (string [] args)
		{
			Console.OutputEncoding = Encoding.Default;

			if (Debugger.IsAttached) {
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
			}

			_scenarios = Scenario.GetScenarios ();

			if (args.Length > 0 && args.Contains ("-usc")) {
				_useSystemConsole = true;
				args = args.Where (val => val != "-usc").ToArray ();
			}
			if (args.Length > 0) {
				var item = _scenarios.FindIndex (s => s.GetName ().Equals (args [0], StringComparison.OrdinalIgnoreCase));
				_selectedScenario = (Scenario)Activator.CreateInstance (_scenarios [item].GetType ());
				Application.UseSystemConsole = _useSystemConsole;
				Application.Init ();
				_selectedScenario.Init (Application.Top, _colorScheme);
				_selectedScenario.Setup ();
				_selectedScenario.Run ();
				_selectedScenario = null;
				Application.Shutdown ();
				return;
			}

			_aboutMessage = new StringBuilder ();
			_aboutMessage.AppendLine (@"A comprehensive sample library for");
			_aboutMessage.AppendLine (@"");
			_aboutMessage.AppendLine (@"  _______                  _             _   _____       _  ");
			_aboutMessage.AppendLine (@" |__   __|                (_)           | | / ____|     (_) ");
			_aboutMessage.AppendLine (@"    | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _  ");
			_aboutMessage.AppendLine (@"    | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | | ");
			_aboutMessage.AppendLine (@"    | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | | ");
			_aboutMessage.AppendLine (@"    |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_| ");
			_aboutMessage.AppendLine (@"");
			_aboutMessage.AppendLine (@"https://github.com/gui-cs/Terminal.Gui");

			Scenario scenario;
			while ((scenario = SelectScenario ()) != null) {
#if DEBUG_IDISPOSABLE
				// Validate there are no outstanding Responder-based instances 
				// after a scenario was selected to run. This proves the main UI Catalog
				// 'app' closed cleanly.
				foreach (var inst in Responder.Instances) {
					Debug.Assert (inst.WasDisposed);
				}
				Responder.Instances.Clear ();
#endif

				scenario.Init (Application.Top, _colorScheme);
				scenario.Setup ();
				scenario.Run ();

				// This call to Application.Shutdown brackets the Application.Init call
				// made by Scenario.Init()
				Application.Shutdown ();

#if DEBUG_IDISPOSABLE
				// After the scenario runs, validate all Responder-based instances
				// were disposed. This proves the scenario 'app' closed cleanly.
				foreach (var inst in Responder.Instances) {
					Debug.Assert (inst.WasDisposed);
				}
				Responder.Instances.Clear ();
#endif
			}

#if DEBUG_IDISPOSABLE
			// This proves that when the user exited the UI Catalog app
			// it cleaned up properly.
			foreach (var inst in Responder.Instances) {
				Debug.Assert (inst.WasDisposed);
			}
			Responder.Instances.Clear ();
#endif
		}

		/// <summary>
		/// Shows the UI Catalog selection UI. When the user selects a Scenario to run, the
		/// UI Catalog main app UI is killed and the Scenario is run as though it were Application.Top. 
		/// When the Scenario exits, this function exits.
		/// </summary>
		/// <returns></returns>
		private static Scenario SelectScenario ()
		{
			Application.UseSystemConsole = _useSystemConsole;
			Application.Init ();
			if (_colorScheme == null) {
				// `Colors` is not initilized until the ConsoleDriver is loaded by 
				// Application.Init. Set it only the first time though so it is
				// preserved between running multiple Scenarios
				_colorScheme = Colors.Base;
			}
			Application.HeightAsBuffer = _heightAsBuffer;

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "Quit UI Catalog", () => Application.RequestStop(), null, null, Key.Q | Key.CtrlMask)
				}),
				new MenuBarItem ("_Color Scheme", CreateColorSchemeMenuItems()),
				new MenuBarItem ("Diag_nostics", CreateDiagnosticMenuItems()),
				new MenuBarItem ("_Help", new MenuItem [] {
					new MenuItem ("_gui.cs API Overview", "", () => OpenUrl ("https://gui-cs.github.io/Terminal.Gui/articles/overview.html"), null, null, Key.F1),
					new MenuItem ("gui.cs _README", "", () => OpenUrl ("https://github.com/gui-cs/Terminal.Gui"), null, null, Key.F2),
					new MenuItem ("_About...",
						"About UI Catalog", () =>  MessageBox.Query ("About UI Catalog", _aboutMessage.ToString(), "_Ok"), null, null, Key.CtrlMask | Key.A),
				}),
			});

			_leftPane = new FrameView ("Categories") {
				X = 0,
				Y = 1, // for menu
				Width = 25,
				Height = Dim.Fill (1),
				CanFocus = true,
				Shortcut = Key.CtrlMask | Key.C
			};
			_leftPane.Title = $"{_leftPane.Title} ({_leftPane.ShortcutTag})";
			_leftPane.ShortcutAction = () => _leftPane.SetFocus ();

			_categories = Scenario.GetAllCategories ();
			_categoryListView = new ListView (_categories) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (0),
				AllowsMarking = false,
				CanFocus = true,
			};
			_categoryListView.OpenSelectedItem += (a) => {
				_rightPane.SetFocus ();
			};
			_categoryListView.SelectedItemChanged += CategoryListView_SelectedChanged;
			_leftPane.Add (_categoryListView);

			_rightPane = new FrameView ("Scenarios") {
				X = 25,
				Y = 1, // for menu
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
				CanFocus = true,
				Shortcut = Key.CtrlMask | Key.S
			};
			_rightPane.Title = $"{_rightPane.Title} ({_rightPane.ShortcutTag})";
			_rightPane.ShortcutAction = () => _rightPane.SetFocus ();

			_nameColumnWidth = _scenarios.OrderByDescending (s => s.GetName ().Length).FirstOrDefault ().GetName ().Length;

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

			_capslock = new StatusItem (Key.CharMask, "Caps", null);
			_numlock = new StatusItem (Key.CharMask, "Num", null);
			_scrolllock = new StatusItem (Key.CharMask, "Scroll", null);

			_statusBar = new StatusBar () {
				Visible = true,
			};
			_statusBar.Items = new StatusItem [] {
				_capslock,
				_numlock,
				_scrolllock,
				new StatusItem(Key.Q | Key.CtrlMask, "~CTRL-Q~ Quit", () => {
					if (_selectedScenario is null){
						// This causes GetScenarioToRun to return null
						_selectedScenario = null;
						Application.RequestStop();
					} else {
						_selectedScenario.RequestStop();
					}
				}),
				new StatusItem(Key.F10, "~F10~ Hide/Show Status Bar", () => {
					_statusBar.Visible = !_statusBar.Visible;
					_leftPane.Height = Dim.Fill(_statusBar.Visible ? 1 : 0);
					_rightPane.Height = Dim.Fill(_statusBar.Visible ? 1 : 0);
					Application.Top.LayoutSubviews();
					Application.Top.SetChildNeedsDisplay();
				}),
				new StatusItem (Key.CharMask, Application.Driver.GetType ().Name, null),
			};

			Application.Top.ColorScheme = _colorScheme;
			Application.Top.KeyDown += KeyDownHandler;
			Application.Top.Add (menu);
			Application.Top.Add (_leftPane);
			Application.Top.Add (_rightPane);
			Application.Top.Add (_statusBar);

			void TopHandler ()
			{
				if (_selectedScenario != null) {
					_selectedScenario = null;
					_isFirstRunning = false;
				}
				if (!_isFirstRunning) {
					_rightPane.SetFocus ();
				}
				Application.Top.Loaded -= TopHandler;
			}
			Application.Top.Loaded += TopHandler;

			// Restore previous selections
			_categoryListView.SelectedItem = _cachedCategoryIndex;
			_scenarioListView.SelectedItem = _cachedScenarioIndex;

			// Run UI Catalog UI. When it exits, if _selectedScenario is != null then
			// a Scenario was selected. Otherwise, the user wants to exit UI Catalog.
			Application.Run (Application.Top);
			Application.Shutdown ();

			return _selectedScenario;
		}


		/// <summary>
		/// Launches the selected scenario, setting the global _selectedScenario
		/// </summary>
		/// <param name="e"></param>
		private static void _scenarioListView_OpenSelectedItem (EventArgs e)
		{
			if (_selectedScenario is null) {
				// Save selected item state
				_cachedCategoryIndex = _categoryListView.SelectedItem;
				_cachedScenarioIndex = _scenarioListView.SelectedItem;
				// Create new instance of scenario (even though Scenarios contains instances)
				_selectedScenario = (Scenario)Activator.CreateInstance (_scenarioListView.Source.ToList () [_scenarioListView.SelectedItem].GetType ());

				// Tell the main app to stop
				Application.RequestStop ();
			}
		}

		static List<MenuItem []> CreateDiagnosticMenuItems ()
		{
			List<MenuItem []> menuItems = new List<MenuItem []> ();
			menuItems.Add (CreateDiagnosticFlagsMenuItems ());
			menuItems.Add (new MenuItem [] { null });
			menuItems.Add (CreateSizeStyle ());
			menuItems.Add (CreateDisabledEnabledMouse ());
			menuItems.Add (CreateKeybindings ());
			return menuItems;
		}

		private static MenuItem [] CreateDisabledEnabledMouse ()
		{
			List<MenuItem> menuItems = new List<MenuItem> ();
			var item = new MenuItem ();
			item.Title = "_Disable Mouse";
			item.Shortcut = Key.CtrlMask | Key.AltMask | (Key)item.Title.ToString ().Substring (1, 1) [0];
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = Application.IsMouseDisabled;
			item.Action += () => {
				item.Checked = Application.IsMouseDisabled = !item.Checked;
			};
			menuItems.Add (item);

			return menuItems.ToArray ();
		}
		private static MenuItem [] CreateKeybindings ()
		{

			List<MenuItem> menuItems = new List<MenuItem> ();
			var item = new MenuItem ();
			item.Title = "_Key Bindings";
			item.Help = "Change which keys do what";
			item.Action += () => {
				var dlg = new KeyBindingsDialog ();
				Application.Run (dlg);
			};

			menuItems.Add (null);
			menuItems.Add (item);

			return menuItems.ToArray ();
		}

		static MenuItem [] CreateSizeStyle ()
		{
			List<MenuItem> menuItems = new List<MenuItem> ();
			var item = new MenuItem ();
			item.Title = "_Height As Buffer";
			item.Shortcut = Key.CtrlMask | Key.AltMask | (Key)item.Title.ToString ().Substring (1, 1) [0];
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = Application.HeightAsBuffer;
			item.Action += () => {
				item.Checked = !item.Checked;
				_heightAsBuffer = item.Checked;
				Application.HeightAsBuffer = _heightAsBuffer;
			};
			menuItems.Add (item);

			return menuItems.ToArray ();
		}

		static MenuItem [] CreateDiagnosticFlagsMenuItems ()
		{
			const string OFF = "Diagnostics: _Off";
			const string FRAME_RULER = "Diagnostics: Frame _Ruler";
			const string FRAME_PADDING = "Diagnostics: _Frame Padding";
			var index = 0;

			List<MenuItem> menuItems = new List<MenuItem> ();
			foreach (Enum diag in Enum.GetValues (_diagnosticFlags.GetType ())) {
				var item = new MenuItem ();
				item.Title = GetDiagnosticsTitle (diag);
				item.Shortcut = Key.AltMask + index.ToString () [0];
				index++;
				item.CheckType |= MenuItemCheckStyle.Checked;
				if (GetDiagnosticsTitle (ConsoleDriver.DiagnosticFlags.Off) == item.Title) {
					item.Checked = (_diagnosticFlags & (ConsoleDriver.DiagnosticFlags.FramePadding
					| ConsoleDriver.DiagnosticFlags.FrameRuler)) == 0;
				} else {
					item.Checked = _diagnosticFlags.HasFlag (diag);
				}
				item.Action += () => {
					var t = GetDiagnosticsTitle (ConsoleDriver.DiagnosticFlags.Off);
					if (item.Title == t && !item.Checked) {
						_diagnosticFlags &= ~(ConsoleDriver.DiagnosticFlags.FramePadding | ConsoleDriver.DiagnosticFlags.FrameRuler);
						item.Checked = true;
					} else if (item.Title == t && item.Checked) {
						_diagnosticFlags |= (ConsoleDriver.DiagnosticFlags.FramePadding | ConsoleDriver.DiagnosticFlags.FrameRuler);
						item.Checked = false;
					} else {
						var f = GetDiagnosticsEnumValue (item.Title);
						if (_diagnosticFlags.HasFlag (f)) {
							SetDiagnosticsFlag (f, false);
						} else {
							SetDiagnosticsFlag (f, true);
						}
					}
					foreach (var menuItem in menuItems) {
						if (menuItem.Title == t) {
							menuItem.Checked = !_diagnosticFlags.HasFlag (ConsoleDriver.DiagnosticFlags.FrameRuler)
								&& !_diagnosticFlags.HasFlag (ConsoleDriver.DiagnosticFlags.FramePadding);
						} else if (menuItem.Title != t) {
							menuItem.Checked = _diagnosticFlags.HasFlag (GetDiagnosticsEnumValue (menuItem.Title));
						}
					}
					ConsoleDriver.Diagnostics = _diagnosticFlags;
					Application.Top.SetNeedsDisplay ();
				};
				menuItems.Add (item);
			}
			return menuItems.ToArray ();

			string GetDiagnosticsTitle (Enum diag)
			{
				switch (Enum.GetName (_diagnosticFlags.GetType (), diag)) {
				case "Off":
					return OFF;
				case "FrameRuler":
					return FRAME_RULER;
				case "FramePadding":
					return FRAME_PADDING;
				}
				return "";
			}

			Enum GetDiagnosticsEnumValue (ustring title)
			{
				switch (title.ToString ()) {
				case FRAME_RULER:
					return ConsoleDriver.DiagnosticFlags.FrameRuler;
				case FRAME_PADDING:
					return ConsoleDriver.DiagnosticFlags.FramePadding;
				}
				return null;
			}

			void SetDiagnosticsFlag (Enum diag, bool add)
			{
				switch (diag) {
				case ConsoleDriver.DiagnosticFlags.FrameRuler:
					if (add) {
						_diagnosticFlags |= ConsoleDriver.DiagnosticFlags.FrameRuler;
					} else {
						_diagnosticFlags &= ~ConsoleDriver.DiagnosticFlags.FrameRuler;
					}
					break;
				case ConsoleDriver.DiagnosticFlags.FramePadding:
					if (add) {
						_diagnosticFlags |= ConsoleDriver.DiagnosticFlags.FramePadding;
					} else {
						_diagnosticFlags &= ~ConsoleDriver.DiagnosticFlags.FramePadding;
					}
					break;
				default:
					_diagnosticFlags = default;
					break;
				}
			}
		}

		static ColorScheme _colorScheme;
		static MenuItem [] CreateColorSchemeMenuItems ()
		{
			List<MenuItem> menuItems = new List<MenuItem> ();
			foreach (var sc in Colors.ColorSchemes) {
				var item = new MenuItem ();
				item.Title = $"_{sc.Key}";
				item.Shortcut = Key.AltMask | (Key)sc.Key.Substring (0, 1) [0];
				item.CheckType |= MenuItemCheckStyle.Radio;
				item.Checked = sc.Value == _colorScheme;
				item.Action += () => {
					Application.Top.ColorScheme = _colorScheme = sc.Value;
					Application.Top?.SetNeedsDisplay ();
					foreach (var menuItem in menuItems) {
						menuItem.Checked = menuItem.Title.Equals ($"_{sc.Key}") && sc.Value == _colorScheme;
					}
				};
				menuItems.Add (item);
			}
			return menuItems.ToArray ();
		}

		private static void KeyDownHandler (View.KeyEventEventArgs a)
		{
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
			var item = _categories [e.Item];
			List<Scenario> newlist;
			if (e.Item == 0) {
				// First category is "All"
				newlist = _scenarios;

			} else {
				newlist = _scenarios.Where (s => s.GetCategories ().Contains (item)).ToList ();
			}
			_scenarioListView.SetSource (newlist.ToList ());
		}

		private static void OpenUrl (string url)
		{
			try {
				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					url = url.Replace ("&", "^&");
					Process.Start (new ProcessStartInfo ("cmd", $"/c start {url}") { CreateNoWindow = true });
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					using (var process = new Process {
						StartInfo = new ProcessStartInfo {
							FileName = "xdg-open",
							Arguments = url,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
							UseShellExecute = false
						}
					}) {
						process.Start ();
					}
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					Process.Start ("open", url);
				}
			} catch {
				throw;
			}
		}
	}
}
