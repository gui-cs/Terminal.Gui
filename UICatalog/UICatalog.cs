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
	class UICatalogApp {
		static void Main (string [] args)
		{
			Console.OutputEncoding = Encoding.Default;

			if (Debugger.IsAttached) {
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
			}

			_scenarios = Scenario.GetScenarios ();
			_categories = Scenario.GetAllCategories ();
			_nameColumnWidth = _scenarios.OrderByDescending (s => s.GetName ().Length).FirstOrDefault ().GetName ().Length;

			if (args.Length > 0 && args.Contains ("-usc")) {
				_useSystemConsole = true;
				args = args.Where (val => val != "-usc").ToArray ();
			}

			// If a Scenario name has been provided on the commandline
			// run it and exit when done.
			if (args.Length > 0) {
				var item = _scenarios.FindIndex (s => s.GetName ().Equals (args [0], StringComparison.OrdinalIgnoreCase));
				_selectedScenario = (Scenario)Activator.CreateInstance (_scenarios [item].GetType ());
				Application.UseSystemConsole = _useSystemConsole;
				Application.Init ();
				_selectedScenario.Init (_colorScheme);
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
			while ((scenario = RunUICatalogTopLevel ()) != null) {
				VerifyObjectsWereDisposed ();
				scenario.Init (_colorScheme);
				scenario.Setup ();
				scenario.Run ();

				// This call to Application.Shutdown brackets the Application.Init call
				// made by Scenario.Init() above
				Application.Shutdown ();

				VerifyObjectsWereDisposed ();
			}
			VerifyObjectsWereDisposed ();
		}

		/// <summary>
		/// Shows the UI Catalog selection UI. When the user selects a Scenario to run, the
		/// UI Catalog main app UI is killed and the Scenario is run as though it were Application.Top. 
		/// When the Scenario exits, this function exits.
		/// </summary>
		/// <returns></returns>
		static Scenario RunUICatalogTopLevel ()
		{
			Application.UseSystemConsole = _useSystemConsole;

			// Run UI Catalog UI. When it exits, if _selectedScenario is != null then
			// a Scenario was selected. Otherwise, the user wants to exit UI Catalog.
			Application.Init ();
			Application.Run<UICatalogTopLevel> ();
			Application.Shutdown ();

			return _selectedScenario;
		}

		static List<Scenario> _scenarios;
		static List<string> _categories;
		static int _nameColumnWidth;
		// When a scenario is run, the main app is killed. These items
		// are therefore cached so that when the scenario exits the
		// main app UI can be restored to previous state
		static int _cachedScenarioIndex = 0;
		static int _cachedCategoryIndex = 0;
		static StringBuilder _aboutMessage;

		// If set, holds the scenario the user selected
		static Scenario _selectedScenario = null;

		static bool _useSystemConsole = false;
		static ConsoleDriver.DiagnosticFlags _diagnosticFlags;
		static bool _heightAsBuffer = false;
		static bool _isFirstRunning = true;
		static ColorScheme _colorScheme;

		/// <summary>
		/// This is the main UI Catalog app view. It is run fresh when the app loads (if a Scenario has not been passed on 
		/// the command line) and each time a Scenario ends.
		/// </summary>
		class UICatalogTopLevel : Toplevel {
			public MenuItem miIsMouseDisabled;
			public MenuItem miHeightAsBuffer;

			public FrameView LeftPane;
			public ListView CategoryListView;
			public FrameView RightPane;
			public ListView ScenarioListView;

			public StatusItem Capslock;
			public StatusItem Numlock;
			public StatusItem Scrolllock;
			public StatusItem DriverName;

			public UICatalogTopLevel ()
			{
				ColorScheme = _colorScheme;
				MenuBar = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_File", new MenuItem [] {
						new MenuItem ("_Quit", "Quit UI Catalog", () => RequestStop(), null, null, Key.Q | Key.CtrlMask)
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

				Capslock = new StatusItem (Key.CharMask, "Caps", null);
				Numlock = new StatusItem (Key.CharMask, "Num", null);
				Scrolllock = new StatusItem (Key.CharMask, "Scroll", null);
				DriverName = new StatusItem (Key.CharMask, "Driver:", null);

				StatusBar = new StatusBar () {
					Visible = true,
				};
				StatusBar.Items = new StatusItem [] {
					Capslock,
					Numlock,
					Scrolllock,
					new StatusItem(Key.Q | Key.CtrlMask, "~CTRL-Q~ Quit", () => {
						if (_selectedScenario is null){
							// This causes GetScenarioToRun to return null
							_selectedScenario = null;
							RequestStop();
						} else {
							_selectedScenario.RequestStop();
						}
					}),
					new StatusItem(Key.F10, "~F10~ Hide/Show Status Bar", () => {
						StatusBar.Visible = !StatusBar.Visible;
						LeftPane.Height = Dim.Fill(StatusBar.Visible ? 1 : 0);
						RightPane.Height = Dim.Fill(StatusBar.Visible ? 1 : 0);
						LayoutSubviews();
						SetChildNeedsDisplay();
					}),
					DriverName,
				};

				LeftPane = new FrameView ("Categories") {
					X = 0,
					Y = 1, // for menu
					Width = 25,
					Height = Dim.Fill (1),
					CanFocus = true,
					Shortcut = Key.CtrlMask | Key.C
				};
				LeftPane.Title = $"{LeftPane.Title} ({LeftPane.ShortcutTag})";
				LeftPane.ShortcutAction = () => LeftPane.SetFocus ();

				CategoryListView = new ListView (_categories) {
					X = 0,
					Y = 0,
					Width = Dim.Fill (0),
					Height = Dim.Fill (0),
					AllowsMarking = false,
					CanFocus = true,
				};
				CategoryListView.OpenSelectedItem += (a) => {
					RightPane.SetFocus ();
				};
				CategoryListView.SelectedItemChanged += CategoryListView_SelectedChanged;
				LeftPane.Add (CategoryListView);

				RightPane = new FrameView ("Scenarios") {
					X = 25,
					Y = 1, // for menu
					Width = Dim.Fill (),
					Height = Dim.Fill (1),
					CanFocus = true,
					Shortcut = Key.CtrlMask | Key.S
				};
				RightPane.Title = $"{RightPane.Title} ({RightPane.ShortcutTag})";
				RightPane.ShortcutAction = () => RightPane.SetFocus ();

				ScenarioListView = new ListView () {
					X = 0,
					Y = 0,
					Width = Dim.Fill (0),
					Height = Dim.Fill (0),
					AllowsMarking = false,
					CanFocus = true,
				};

				ScenarioListView.OpenSelectedItem += ScenarioListView_OpenSelectedItem;
				RightPane.Add (ScenarioListView);

				KeyDown += KeyDownHandler;
				Add (MenuBar);
				Add (LeftPane);
				Add (RightPane);
				Add (StatusBar);

				Loaded += LoadedHandler;

				// Restore previous selections
				CategoryListView.SelectedItem = _cachedCategoryIndex;
				ScenarioListView.SelectedItem = _cachedScenarioIndex;
			}

			void LoadedHandler ()
			{
				Application.HeightAsBuffer = _heightAsBuffer;

				if (_colorScheme == null) {
					ColorScheme = _colorScheme = Colors.Base;
				}

				miIsMouseDisabled.Checked = Application.IsMouseDisabled;
				miHeightAsBuffer.Checked = Application.HeightAsBuffer;
				DriverName.Title = $"Driver: {Driver.GetType ().Name}";

				if (_selectedScenario != null) {
					_selectedScenario = null;
					_isFirstRunning = false;
				}
				if (!_isFirstRunning) {
					RightPane.SetFocus ();
				}
				Loaded -= LoadedHandler;
			}

			/// <summary>
			/// Launches the selected scenario, setting the global _selectedScenario
			/// </summary>
			/// <param name="e"></param>
			void ScenarioListView_OpenSelectedItem (EventArgs e)
			{
				if (_selectedScenario is null) {
					// Save selected item state
					_cachedCategoryIndex = CategoryListView.SelectedItem;
					_cachedScenarioIndex = ScenarioListView.SelectedItem;
					// Create new instance of scenario (even though Scenarios contains instances)
					_selectedScenario = (Scenario)Activator.CreateInstance (ScenarioListView.Source.ToList () [ScenarioListView.SelectedItem].GetType ());

					// Tell the main app to stop
					Application.RequestStop ();
				}
			}

			List<MenuItem []> CreateDiagnosticMenuItems ()
			{
				List<MenuItem []> menuItems = new List<MenuItem []> ();
				menuItems.Add (CreateDiagnosticFlagsMenuItems ());
				menuItems.Add (new MenuItem [] { null });
				menuItems.Add (CreateHeightAsBufferMenuItems ());
				menuItems.Add (CreateDisabledEnabledMouseItems ());
				menuItems.Add (CreateKeybindingsMenuItems ());
				return menuItems;
			}

			MenuItem [] CreateDisabledEnabledMouseItems ()
			{
				List<MenuItem> menuItems = new List<MenuItem> ();
				miIsMouseDisabled = new MenuItem ();
				miIsMouseDisabled.Title = "_Disable Mouse";
				miIsMouseDisabled.Shortcut = Key.CtrlMask | Key.AltMask | (Key)miIsMouseDisabled.Title.ToString ().Substring (1, 1) [0];
				miIsMouseDisabled.CheckType |= MenuItemCheckStyle.Checked;
				miIsMouseDisabled.Action += () => {
					miIsMouseDisabled.Checked = Application.IsMouseDisabled = !miIsMouseDisabled.Checked;
				};
				menuItems.Add (miIsMouseDisabled);

				return menuItems.ToArray ();
			}

			MenuItem [] CreateKeybindingsMenuItems ()
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

			MenuItem [] CreateHeightAsBufferMenuItems ()
			{
				List<MenuItem> menuItems = new List<MenuItem> ();
				miHeightAsBuffer = new MenuItem ();
				miHeightAsBuffer.Title = "_Height As Buffer";
				miHeightAsBuffer.Shortcut = Key.CtrlMask | Key.AltMask | (Key)miHeightAsBuffer.Title.ToString ().Substring (1, 1) [0];
				miHeightAsBuffer.CheckType |= MenuItemCheckStyle.Checked;
				miHeightAsBuffer.Action += () => {
					miHeightAsBuffer.Checked = !miHeightAsBuffer.Checked;
					Application.HeightAsBuffer = miHeightAsBuffer.Checked;
				};
				menuItems.Add (miHeightAsBuffer);

				return menuItems.ToArray ();
			}

			MenuItem [] CreateDiagnosticFlagsMenuItems ()
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

			MenuItem [] CreateColorSchemeMenuItems ()
			{
				List<MenuItem> menuItems = new List<MenuItem> ();
				foreach (var sc in Colors.ColorSchemes) {
					var item = new MenuItem ();
					item.Title = $"_{sc.Key}";
					item.Shortcut = Key.AltMask | (Key)sc.Key.Substring (0, 1) [0];
					item.CheckType |= MenuItemCheckStyle.Radio;
					item.Checked = sc.Value == _colorScheme;
					item.Action += () => {
						ColorScheme = _colorScheme = sc.Value;
						SetNeedsDisplay ();
						foreach (var menuItem in menuItems) {
							menuItem.Checked = menuItem.Title.Equals ($"_{sc.Key}") && sc.Value == _colorScheme;
						}
					};
					menuItems.Add (item);
				}
				return menuItems.ToArray ();
			}

			void KeyDownHandler (View.KeyEventEventArgs a)
			{
				if (a.KeyEvent.IsCapslock) {
					Capslock.Title = "Caps: On";
					StatusBar.SetNeedsDisplay ();
				} else {
					Capslock.Title = "Caps: Off";
					StatusBar.SetNeedsDisplay ();
				}

				if (a.KeyEvent.IsNumlock) {
					Numlock.Title = "Num: On";
					StatusBar.SetNeedsDisplay ();
				} else {
					Numlock.Title = "Num: Off";
					StatusBar.SetNeedsDisplay ();
				}

				if (a.KeyEvent.IsScrolllock) {
					Scrolllock.Title = "Scroll: On";
					StatusBar.SetNeedsDisplay ();
				} else {
					Scrolllock.Title = "Scroll: Off";
					StatusBar.SetNeedsDisplay ();
				}
			}

			void CategoryListView_SelectedChanged (ListViewItemEventArgs e)
			{
				var item = _categories [e.Item];
				List<Scenario> newlist;
				if (e.Item == 0) {
					// First category is "All"
					newlist = _scenarios;

				} else {
					newlist = _scenarios.Where (s => s.GetCategories ().Contains (item)).ToList ();
				}
				ScenarioListView.SetSource (newlist.ToList ());
			}
		}

		static void VerifyObjectsWereDisposed ()
		{
#if DEBUG_IDISPOSABLE
			// Validate there are no outstanding Responder-based instances 
			// after a scenario was selected to run. This proves the main UI Catalog
			// 'app' closed cleanly.
			foreach (var inst in Responder.Instances) {
				Debug.Assert (inst.WasDisposed);
			}
			Responder.Instances.Clear ();

			// Validate there are no outstanding Application.RunState-based instances 
			// after a scenario was selected to run. This proves the main UI Catalog
			// 'app' closed cleanly.
			foreach (var inst in Application.RunState.Instances) {
				Debug.Assert (inst.WasDisposed);
			}
			Application.RunState.Instances.Clear ();
#endif
		}

		static void OpenUrl (string url)
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
