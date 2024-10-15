global using Attribute = Terminal.Gui.Attribute;
global using CM = Terminal.Gui.ConfigurationManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Terminal.Gui;
using UICatalog.Scenarios;
using static Terminal.Gui.ConfigurationManager;
using Command = Terminal.Gui.Command;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

#nullable enable

namespace UICatalog;

/// <summary>
///     UI Catalog is a comprehensive sample library for Terminal.Gui. It provides a simple UI for adding to the
///     catalog of scenarios.
/// </summary>
/// <remarks>
///     <para>UI Catalog attempts to satisfy the following goals:</para>
///     <para>
///         <list type="number">
///             <item>
///                 <description>Be an easy-to-use showcase for Terminal.Gui concepts and features.</description>
///             </item>
///             <item>
///                 <description>Provide sample code that illustrates how to properly implement said concepts & features.</description>
///             </item>
///             <item>
///                 <description>Make it easy for contributors to add additional samples in a structured way.</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public class UICatalogApp
{
    private static int _cachedCategoryIndex;

    // When a scenario is run, the main app is killed. These items
    // are therefore cached so that when the scenario exits the
    // main app UI can be restored to previous state
    private static int _cachedScenarioIndex;
    private static string? _cachedTheme = string.Empty;
    private static ObservableCollection<string>? _categories;
    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly FileSystemWatcher _currentDirWatcher = new ();
    private static ViewDiagnosticFlags _diagnosticFlags;
    private static string _forceDriver = string.Empty;
    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly FileSystemWatcher _homeDirWatcher = new ();
    private static bool _isFirstRunning = true;
    private static Options _options;
    private static ObservableCollection<Scenario>? _scenarios;

    // If set, holds the scenario the user selected
    private static Scenario? _selectedScenario;
    private static MenuBarItem? _themeMenuBarItem;
    private static MenuItem []? _themeMenuItems;
    private static string _topLevelColorScheme = string.Empty;

    [SerializableConfigurationProperty (Scope = typeof (AppScope), OmitClassName = true)]
    [JsonPropertyName ("UICatalog.StatusBar")]
    public static bool ShowStatusBar { get; set; } = true;

    /// <summary>
    /// Gets the message displayed in the About Box. `public` so it can be used from Unit tests.
    /// </summary>
    /// <returns></returns>
    public static string GetAboutBoxMessage ()
    {
        // NOTE: Do not use multiline verbatim strings here.
        // WSL gets all confused.
        StringBuilder msg = new ();
        msg.AppendLine ("UI Catalog: A comprehensive sample library for");
        msg.AppendLine ();

        msg.AppendLine ("""
                         _______                  _             _   _____       _ 
                        |__   __|                (_)           | | / ____|     (_)
                           | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _ 
                           | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | |
                           | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | |
                           |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_|
                        """);
        msg.AppendLine ();
        msg.AppendLine ("v2 - Pre-Alpha");
        msg.AppendLine ();
        msg.AppendLine ("https://github.com/gui-cs/Terminal.Gui");

        return msg.ToString ();
    }

    private static void ConfigFileChanged (object sender, FileSystemEventArgs e)
    {
        if (Application.Top == null)
        {
            return;
        }

        // TODO: This is a hack. Figure out how to ensure that the file is fully written before reading it.
        //Thread.Sleep (500);
        Load ();
        Apply ();
    }

    private static int Main (string [] args)
    {
        Console.OutputEncoding = Encoding.Default;

        if (Debugger.IsAttached)
        {
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
        }

        _scenarios = Scenario.GetScenarios ();
        _categories = Scenario.GetAllCategories ();

        // Process command line args
        // "UICatalog [-driver <driver>] [scenario name]"
        // If no driver is provided, the default driver is used.
        Option<string> driverOption = new Option<string> ("--driver", "The ConsoleDriver to use.").FromAmong (
             Application.GetDriverTypes ()
                        .Select (d => d!.Name)
                        .ToArray ()
            );

        driverOption.AddAlias ("-d");
        driverOption.AddAlias ("--d");

        Argument<string> scenarioArgument = new Argument<string> (
                                                                  "scenario",
                                                                  description: "The name of the scenario to run.",
                                                                  getDefaultValue: () => "none"
                                                                 ).FromAmong (
                                                                              _scenarios.Select (s => s.GetName ())
                                                                                        .Append ("none")
                                                                                        .ToArray ()
                                                                             );

        var rootCommand =
            new RootCommand ("A comprehensive sample library for Terminal.Gui") { scenarioArgument, driverOption };

        rootCommand.SetHandler (
                                context =>
                                {
                                    var options = new Options
                                    {
                                        Driver = context.ParseResult.GetValueForOption (driverOption) ?? string.Empty,
                                        Scenario = context.ParseResult.GetValueForArgument (scenarioArgument)
                                        /* etc. */
                                    };

                                    // See https://github.com/dotnet/command-line-api/issues/796 for the rationale behind this hackery
                                    _options = options;
                                }
                               );

        rootCommand.Invoke (args);

        UICatalogMain (_options);

        return 0;
    }

    private static void OpenUrl (string url)
    {
        if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
        {
            url = url.Replace ("&", "^&");
            Process.Start (new ProcessStartInfo ("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
        {
            using var process = new Process
            {
                StartInfo = new ()
                {
                    FileName = "xdg-open",
                    Arguments = url,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            process.Start ();
        }
        else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            Process.Start ("open", url);
        }
    }

    /// <summary>
    ///     Shows the UI Catalog selection UI. When the user selects a Scenario to run, the UI Catalog main app UI is
    ///     killed and the Scenario is run as though it were Application.Top. When the Scenario exits, this function exits.
    /// </summary>
    /// <returns></returns>
    private static Scenario RunUICatalogTopLevel ()
    {
        // Run UI Catalog UI. When it exits, if _selectedScenario is != null then
        // a Scenario was selected. Otherwise, the user wants to quit UI Catalog.

        // If the user specified a driver on the command line then use it,
        // ignoring Config files.

        Application.Init (driverName: _forceDriver);

        if (_cachedTheme is null)
        {
            _cachedTheme = Themes?.Theme;
        }
        else
        {
            Themes!.Theme = _cachedTheme;
            Apply ();
        }

        Application.Run<UICatalogTopLevel> ().Dispose ();
        Application.Shutdown ();

        return _selectedScenario!;
    }

    private static void StartConfigFileWatcher ()
    {
        // Setup a file system watcher for `./.tui/`
        _currentDirWatcher.NotifyFilter = NotifyFilters.LastWrite;

        string assemblyLocation = Assembly.GetExecutingAssembly ().Location;
        string tuiDir;

        if (!string.IsNullOrEmpty (assemblyLocation))
        {
            var assemblyFile = new FileInfo (assemblyLocation);
            tuiDir = Path.Combine (assemblyFile.Directory!.FullName, ".tui");
        }
        else
        {
            tuiDir = Path.Combine (AppContext.BaseDirectory, ".tui");
        }

        if (!Directory.Exists (tuiDir))
        {
            Directory.CreateDirectory (tuiDir);
        }

        _currentDirWatcher.Path = tuiDir;
        _currentDirWatcher.Filter = "*config.json";

        // Setup a file system watcher for `~/.tui/`
        _homeDirWatcher.NotifyFilter = NotifyFilters.LastWrite;
        var f = new FileInfo (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));
        tuiDir = Path.Combine (f.FullName, ".tui");

        if (!Directory.Exists (tuiDir))
        {
            Directory.CreateDirectory (tuiDir);
        }

        _homeDirWatcher.Path = tuiDir;
        _homeDirWatcher.Filter = "*config.json";

        _currentDirWatcher.Changed += ConfigFileChanged;

        //_currentDirWatcher.Created += ConfigFileChanged;
        _currentDirWatcher.EnableRaisingEvents = true;

        _homeDirWatcher.Changed += ConfigFileChanged;

        //_homeDirWatcher.Created += ConfigFileChanged;
        _homeDirWatcher.EnableRaisingEvents = true;
    }

    private static void StopConfigFileWatcher ()
    {
        _currentDirWatcher.EnableRaisingEvents = false;
        _currentDirWatcher.Changed -= ConfigFileChanged;
        _currentDirWatcher.Created -= ConfigFileChanged;

        _homeDirWatcher.EnableRaisingEvents = false;
        _homeDirWatcher.Changed -= ConfigFileChanged;
        _homeDirWatcher.Created -= ConfigFileChanged;
    }

    private static void UICatalogMain (Options options)
    {
        StartConfigFileWatcher ();

        // By setting _forceDriver we ensure that if the user has specified a driver on the command line, it will be used
        // regardless of what's in a config file.
        Application.ForceDriver = _forceDriver = options.Driver;

        // If a Scenario name has been provided on the commandline
        // run it and exit when done.
        if (options.Scenario != "none")
        {
            _topLevelColorScheme = "Base";

            int item = _scenarios!.IndexOf (
                                            _scenarios!.FirstOrDefault (
                                                                        s =>
                                                                            s.GetName ()
                                                                             .Equals (options.Scenario, StringComparison.OrdinalIgnoreCase)
                                                                       )!);
            _selectedScenario = (Scenario)Activator.CreateInstance (_scenarios [item].GetType ())!;

            Application.Init (driverName: _forceDriver);
            _selectedScenario.TopLevelColorScheme = _topLevelColorScheme;
            _selectedScenario.Main ();
            _selectedScenario.Dispose ();
            _selectedScenario = null;

            // TODO: Throw if shutdown was not called already
            Application.Shutdown ();
            VerifyObjectsWereDisposed ();

            return;
        }

        while (RunUICatalogTopLevel () is { } scenario)
        {
            VerifyObjectsWereDisposed ();
            Themes!.Theme = _cachedTheme!;
            Apply ();
            scenario.TopLevelColorScheme = _topLevelColorScheme;

#if DEBUG_IDISPOSABLE
            // Measure how long it takes for the app to shut down
            var sw = new Stopwatch ();
            string scenarioName = scenario.GetName ();
            Application.InitializedChanged += ApplicationOnInitializedChanged;
#endif

            scenario.Main ();
            scenario.Dispose ();

            // This call to Application.Shutdown brackets the Application.Init call
            // made by Scenario.Init() above
            // TODO: Throw if shutdown was not called already
            Application.Shutdown ();
            VerifyObjectsWereDisposed ();

#if DEBUG_IDISPOSABLE
            Application.InitializedChanged -= ApplicationOnInitializedChanged;

            void ApplicationOnInitializedChanged (object? sender, EventArgs<bool> e)
            {
                if (e.CurrentValue)
                {
                    sw.Start ();
                }
                else
                {
                    sw.Stop ();
                    Debug.WriteLine ($"Shutdown of {scenarioName} Scenario took {sw.ElapsedMilliseconds}ms");
                }
            }
#endif
        }

        StopConfigFileWatcher ();
        VerifyObjectsWereDisposed ();

        return;


    }

    private static void VerifyObjectsWereDisposed ()
    {
#if DEBUG_IDISPOSABLE

        // Validate there are no outstanding Responder-based instances 
        // after a scenario was selected to run. This proves the main UI Catalog
        // 'app' closed cleanly.
        foreach (Responder? inst in Responder.Instances)
        {
            
            Debug.Assert (inst.WasDisposed);
        }

        Responder.Instances.Clear ();

        // Validate there are no outstanding Application.RunState-based instances 
        // after a scenario was selected to run. This proves the main UI Catalog
        // 'app' closed cleanly.
        foreach (RunState? inst in RunState.Instances)
        {
            Debug.Assert (inst.WasDisposed);
        }

        RunState.Instances.Clear ();
#endif
    }

    /// <summary>
    ///     This is the main UI Catalog app view. It is run fresh when the app loads (if a Scenario has not been passed on
    ///     the command line) and each time a Scenario ends.
    /// </summary>
    public class UICatalogTopLevel : Toplevel
    {
        public ListView? CategoryList;
        public MenuItem? MiForce16Colors;
        public MenuItem? MiIsMenuBorderDisabled;
        public MenuItem? MiIsMouseDisabled;
        public MenuItem? MiUseSubMenusSingleFrame;

        public Shortcut? ShForce16Colors;

        //public Shortcut? ShDiagnostics;
        public Shortcut? ShVersion;

        // UI Catalog uses TableView for the scenario list instead of a ListView to demonstate how
        // TableView works. There's no real reason not to use ListView. Because we use TableView, and TableView
        // doesn't (currently) have CollectionNavigator support built in, we implement it here, within the app.
        public TableView ScenarioList;

        private readonly StatusBar? _statusBar;

        private readonly CollectionNavigator _scenarioCollectionNav = new ();

        public UICatalogTopLevel ()
        {
            _diagnosticFlags = Diagnostics;

            _themeMenuItems = CreateThemeMenuItems ();
            _themeMenuBarItem = new ("_Themes", _themeMenuItems!);

            MenuBar menuBar = new ()
            {
                Menus =
                [
                    new (
                         "_File",
                         new MenuItem []
                         {
                             new (
                                  "_Quit",
                                  "Quit UI Catalog",
                                  RequestStop
                                 )
                         }
                        ),
                    _themeMenuBarItem,
                    new ("Diag_nostics", CreateDiagnosticMenuItems ()),
                    new (
                         "_Help",
                         new MenuItem []
                         {
                             new (
                                  "_Documentation",
                                  "",
                                  () => OpenUrl ("https://gui-cs.github.io/Terminal.GuiV2Docs"),
                                  null,
                                  null,
                                  (KeyCode)Key.F1
                                 ),
                             new (
                                  "_README",
                                  "",
                                  () => OpenUrl ("https://github.com/gui-cs/Terminal.Gui"),
                                  null,
                                  null,
                                  (KeyCode)Key.F2
                                 ),
                             new (
                                  "_About...",
                                  "About UI Catalog",
                                  () => MessageBox.Query (
                                                          title: "",
                                                          message: GetAboutBoxMessage (),
                                                          wrapMessage: false,
                                                          buttons: "_Ok"
                                                         ),
                                  null,
                                  null,
                                  (KeyCode)Key.A.WithCtrl
                                 )
                         }
                        )
                ]
            };
            Add (menuBar);

            _statusBar = new ()
            {
                Visible = ShowStatusBar,
                AlignmentModes = AlignmentModes.IgnoreFirstOrLast,
                CanFocus = false
            };
            _statusBar.Height = _statusBar.Visible ? Dim.Auto () : 0;

            Add (_statusBar);

            ShVersion = new ()
            {
                Title = "Version Info",
                CanFocus = false,
            };

            var statusBarShortcut = new Shortcut
            {
                Key = Key.F10,
                Title = "Show/Hide Status Bar",
                CanFocus = false,
            };
            statusBarShortcut.Accepting += (sender, args) =>
                                        {
                                            _statusBar.Visible = !_statusBar.Visible;
                                            args.Cancel = true;
                                        };

            ShForce16Colors = new ()
            {
                CanFocus = false,
                CommandView = new CheckBox
                {
                    Title = "16 color mode",
                    CheckedState = Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
                    CanFocus = false
                },
                HelpText = "",
                KeyBindingScope = KeyBindingScope.Application,
                Key = Key.F7
            };

            ((CheckBox)ShForce16Colors.CommandView).CheckedStateChanging += (sender, args) =>
                                                              {
                                                                  Application.Force16Colors = args.NewValue == CheckState.Checked;
                                                                  MiForce16Colors!.Checked = Application.Force16Colors;
                                                                  Application.Refresh ();
                                                              };

            _statusBar.Add (
                           new Shortcut
                           {
                               CanFocus = false,
                               Title = "Quit",
                               Key = Application.QuitKey
                           },
                           statusBarShortcut,
                           ShForce16Colors,

                           //ShDiagnostics,
                           ShVersion
                          );

            // Create the Category list view. This list never changes.
            CategoryList = new ()
            {
                X = 0,
                Y = Pos.Bottom (MenuBar),
                Width = Dim.Auto (),
                Height = Dim.Fill (Dim.Func (() => _statusBar.Frame.Height)),
                AllowsMarking = false,
                CanFocus = true,
                Title = "_Categories",
                BorderStyle = LineStyle.Rounded,
                SuperViewRendersLineCanvas = true,
                Source = new ListWrapper<string> (_categories)
            };
            CategoryList.OpenSelectedItem += (s, a) => { ScenarioList!.SetFocus (); };
            CategoryList.SelectedItemChanged += CategoryView_SelectedChanged;

            // Create the scenario list. The contents of the scenario list changes whenever the
            // Category list selection changes (to show just the scenarios that belong to the selected
            // category).
            ScenarioList = new ()
            {
                X = Pos.Right (CategoryList) - 1,
                Y = Pos.Bottom (MenuBar),
                Width = Dim.Fill (),
                Height = Dim.Fill (Dim.Func (() => _statusBar.Frame.Height)),
                //AllowsMarking = false,
                CanFocus = true,
                Title = "_Scenarios",
                BorderStyle = CategoryList.BorderStyle,
                SuperViewRendersLineCanvas = true
            };

            // TableView provides many options for table headers. For simplicity we turn all 
            // of these off. By enabling FullRowSelect and turning off headers, TableView looks just
            // like a ListView
            ScenarioList.FullRowSelect = true;
            ScenarioList.Style.ShowHeaders = false;
            ScenarioList.Style.ShowHorizontalHeaderOverline = false;
            ScenarioList.Style.ShowHorizontalHeaderUnderline = false;
            ScenarioList.Style.ShowHorizontalBottomline = false;
            ScenarioList.Style.ShowVerticalCellLines = false;
            ScenarioList.Style.ShowVerticalHeaderLines = false;

            /* By default TableView lays out columns at render time and only
             * measures y rows of data at a time.  Where y is the height of the
             * console. This is for the following reasons:
             *
             * - Performance, when tables have a large amount of data
             * - Defensive, prevents a single wide cell value pushing other
             *   columns off screen (requiring horizontal scrolling
             *
             * In the case of UICatalog here, such an approach is overkill so
             * we just measure all the data ourselves and set the appropriate
             * max widths as ColumnStyles
             */
            int longestName = _scenarios!.Max (s => s.GetName ().Length);

            ScenarioList.Style.ColumnStyles.Add (
                                                 0,
                                                 new () { MaxWidth = longestName, MinWidth = longestName, MinAcceptableWidth = longestName }
                                                );
            ScenarioList.Style.ColumnStyles.Add (1, new () { MaxWidth = 1 });

            //// Enable user to find & select a scenario by typing text
            //// TableView does not (currently) have built-in CollectionNavigator support (the ability for the 
            //// user to type and the items that match get selected). We implement it in the app instead. 
            //ScenarioList.KeyDown += (s, a) =>
            //                        {
            //                            if (CollectionNavigatorBase.IsCompatibleKey (a))
            //                            {
            //                                int? newItem =
            //                                    _scenarioCollectionNav?.GetNextMatchingItem (
            //                                                                                 ScenarioList.SelectedRow,
            //                                                                                 (char)a
            //                                                                                );

            //                                if (newItem is int v && newItem != -1)
            //                                {
            //                                    ScenarioList.SelectedRow = v;
            //                                    ScenarioList.EnsureSelectedCellIsVisible ();
            //                                    ScenarioList.SetNeedsDisplay ();
            //                                    a.Handled = true;
            //                                }
            //                            }
            //                        };
            ScenarioList.CellActivated += ScenarioView_OpenSelectedItem;

            // TableView typically is a grid where nav keys are biased for moving left/right.
            ScenarioList.KeyBindings.Remove (Key.Home);
            ScenarioList.KeyBindings.Add (Key.Home, Command.Start);
            ScenarioList.KeyBindings.Remove (Key.End);
            ScenarioList.KeyBindings.Add (Key.End, Command.End);

            // Ideally, TableView.MultiSelect = false would turn off any keybindings for
            // multi-select options. But it currently does not. UI Catalog uses Ctrl-A for
            // a shortcut to About.
            ScenarioList.MultiSelect = false;
            ScenarioList.KeyBindings.Remove (Key.A.WithCtrl);

            Add (CategoryList);
            Add (ScenarioList);

            Loaded += LoadedHandler;
            Unloaded += UnloadedHandler;

            // Restore previous selections
            CategoryList.SelectedItem = _cachedCategoryIndex;
            ScenarioList.SelectedRow = _cachedScenarioIndex;

            Applied += ConfigAppliedHandler;
        }

        public void ConfigChanged ()
        {
            if (_topLevelColorScheme == null || !Colors.ColorSchemes.ContainsKey (_topLevelColorScheme))
            {
                _topLevelColorScheme = "Base";
            }

            _cachedTheme = Themes?.Theme;

            _themeMenuItems = CreateThemeMenuItems ();
            _themeMenuBarItem!.Children = _themeMenuItems;

            foreach (MenuItem mi in _themeMenuItems!)
            {
                if (mi is { Parent: null })
                {
                    mi.Parent = _themeMenuBarItem;
                }
            }

            ColorScheme = Colors.ColorSchemes [_topLevelColorScheme];

            MenuBar!.Menus [0].Children! [0]!.ShortcutKey = Application.QuitKey;

            ((Shortcut)_statusBar!.Subviews [0]).Key = Application.QuitKey;
            _statusBar.Visible = ShowStatusBar;

            MiIsMouseDisabled!.Checked = Application.IsMouseDisabled;

            ((CheckBox)ShForce16Colors!.CommandView!).CheckedState = Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked;

            Application.Top!.SetNeedsDisplay ();
        }

        public MenuItem []? CreateThemeMenuItems ()
        {
            List<MenuItem> menuItems = CreateForce16ColorItems ().ToList ();
            menuItems.Add (null!);

            var schemeCount = 0;

            foreach (KeyValuePair<string, ThemeScope> theme in Themes!)
            {
                var item = new MenuItem
                {
                    Title = theme.Key == "Dark" ? $"{theme.Key.Substring (0, 3)}_{theme.Key.Substring (3, 1)}" : $"_{theme.Key}",
                    ShortcutKey = new Key ((KeyCode)((uint)KeyCode.D1 + schemeCount++))
                        .WithCtrl
                };
                item.CheckType |= MenuItemCheckStyle.Checked;
                item.Checked = theme.Key == _cachedTheme; // CM.Themes.Theme;

                item.Action += () =>
                               {
                                   Themes.Theme = _cachedTheme = theme.Key;
                                   Apply ();
                               };
                menuItems.Add (item);
            }

            List<MenuItem> schemeMenuItems = new ();

            foreach (KeyValuePair<string, ColorScheme?> sc in Colors.ColorSchemes)
            {
                var item = new MenuItem { Title = $"_{sc.Key}", Data = sc.Key };
                item.CheckType |= MenuItemCheckStyle.Radio;
                item.Checked = sc.Key == _topLevelColorScheme;

                item.Action += () =>
                               {
                                   _topLevelColorScheme = (string)item.Data;

                                   foreach (MenuItem schemeMenuItem in schemeMenuItems)
                                   {
                                       schemeMenuItem.Checked = (string)schemeMenuItem.Data == _topLevelColorScheme;
                                   }

                                   ColorScheme = Colors.ColorSchemes [_topLevelColorScheme];
                                   Application.Top!.SetNeedsDisplay ();
                               };
                item.ShortcutKey = ((Key)sc.Key [0].ToString ().ToLower ()).WithCtrl;
                schemeMenuItems.Add (item);
            }

            menuItems.Add (null!);
            var mbi = new MenuBarItem ("_Color Scheme for Application.Top", schemeMenuItems.ToArray ());
            menuItems.Add (mbi);

            return menuItems.ToArray ();
        }

        private void CategoryView_SelectedChanged (object? sender, ListViewItemEventArgs? e)
        {
            string item = _categories! [e!.Item];
            ObservableCollection<Scenario> newlist;

            if (e.Item == 0)
            {
                // First category is "All"
                newlist = _scenarios!;
            }
            else
            {
                newlist = new (_scenarios!.Where (s => s.GetCategories ().Contains (item)).ToList ());
            }

            ScenarioList.Table = new EnumerableTableSource<Scenario> (
                                                                      newlist,
                                                                      new ()
                                                                      {
                                                                          { "Name", s => s.GetName () }, { "Description", s => s.GetDescription () }
                                                                      }
                                                                     );

            // Create a collection of just the scenario names (the 1st column in our TableView)
            // for CollectionNavigator. 
            List<object> firstColumnList = new ();

            for (var i = 0; i < ScenarioList.Table.Rows; i++)
            {
                firstColumnList.Add (ScenarioList.Table [i, 0]);
            }

            _scenarioCollectionNav.Collection = firstColumnList;
        }

        private void ConfigAppliedHandler (object? sender, ConfigurationManagerEventArgs? a) { ConfigChanged (); }

        [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        private MenuItem [] CreateDiagnosticFlagsMenuItems ()
        {
            const string OFF = "View Diagnostics: _Off";
            const string RULER = "View Diagnostics: _Ruler";
            const string PADDING = "View Diagnostics: _Padding";
            const string Hover = "View Diagnostics: _Hover";
            var index = 0;

            List<MenuItem> menuItems = new ();

            foreach (Enum diag in Enum.GetValues (_diagnosticFlags.GetType ()))
            {
                var item = new MenuItem
                {
                    Title = GetDiagnosticsTitle (diag), ShortcutKey = new Key (index.ToString () [0]).WithAlt
                };
                index++;
                item.CheckType |= MenuItemCheckStyle.Checked;

                if (GetDiagnosticsTitle (ViewDiagnosticFlags.Off) == item.Title)
                {
                    item.Checked = !_diagnosticFlags.HasFlag (ViewDiagnosticFlags.Padding)
                                   && !_diagnosticFlags.HasFlag (ViewDiagnosticFlags.Ruler)
                                   && !_diagnosticFlags.HasFlag (ViewDiagnosticFlags.Hover);
                }
                else
                {
                    item.Checked = _diagnosticFlags.HasFlag (diag);
                }

                item.Action += () =>
                               {
                                   string t = GetDiagnosticsTitle (ViewDiagnosticFlags.Off);

                                   if (item.Title == t && item.Checked == false)
                                   {
                                       _diagnosticFlags &= ~(ViewDiagnosticFlags.Padding | ViewDiagnosticFlags.Ruler | ViewDiagnosticFlags.Hover);
                                       item.Checked = true;
                                   }
                                   else if (item.Title == t && item.Checked == true)
                                   {
                                       _diagnosticFlags |= ViewDiagnosticFlags.Padding | ViewDiagnosticFlags.Ruler | ViewDiagnosticFlags.Hover;
                                       item.Checked = false;
                                   }
                                   else
                                   {
                                       Enum f = GetDiagnosticsEnumValue (item.Title);

                                       if (_diagnosticFlags.HasFlag (f))
                                       {
                                           SetDiagnosticsFlag (f, false);
                                       }
                                       else
                                       {
                                           SetDiagnosticsFlag (f, true);
                                       }
                                   }

                                   foreach (MenuItem menuItem in menuItems)
                                   {
                                       if (menuItem.Title == t)
                                       {
                                           menuItem.Checked = !_diagnosticFlags.HasFlag (ViewDiagnosticFlags.Ruler)
                                                              && !_diagnosticFlags.HasFlag (ViewDiagnosticFlags.Padding)
                                                              && !_diagnosticFlags.HasFlag (ViewDiagnosticFlags.Hover);
                                       }
                                       else if (menuItem.Title != t)
                                       {
                                           menuItem.Checked = _diagnosticFlags.HasFlag (GetDiagnosticsEnumValue (menuItem.Title));
                                       }
                                   }

                                   Diagnostics = _diagnosticFlags;
                                   Application.Top!.SetNeedsDisplay ();
                               };
                menuItems.Add (item);
            }

            return menuItems.ToArray ();

            string GetDiagnosticsTitle (Enum diag)
            {
                return Enum.GetName (_diagnosticFlags.GetType (), diag) switch
                {
                    "Off" => OFF,
                    "Ruler" => RULER,
                    "Padding" => PADDING,
                    "Hover" => Hover,
                    _ => ""
                };
            }

            Enum GetDiagnosticsEnumValue (string? title)
            {
                return title switch
                {
                    RULER => ViewDiagnosticFlags.Ruler,
                    PADDING => ViewDiagnosticFlags.Padding,
                    Hover => ViewDiagnosticFlags.Hover,
                    _ => null!
                };
            }

            void SetDiagnosticsFlag (Enum diag, bool add)
            {
                switch (diag)
                {
                    case ViewDiagnosticFlags.Ruler:
                        if (add)
                        {
                            _diagnosticFlags |= ViewDiagnosticFlags.Ruler;
                        }
                        else
                        {
                            _diagnosticFlags &= ~ViewDiagnosticFlags.Ruler;
                        }

                        break;
                    case ViewDiagnosticFlags.Padding:
                        if (add)
                        {
                            _diagnosticFlags |= ViewDiagnosticFlags.Padding;
                        }
                        else
                        {
                            _diagnosticFlags &= ~ViewDiagnosticFlags.Padding;
                        }

                        break;
                    case ViewDiagnosticFlags.Hover:
                        if (add)
                        {
                            _diagnosticFlags |= ViewDiagnosticFlags.Hover;
                        }
                        else
                        {
                            _diagnosticFlags &= ~ViewDiagnosticFlags.Hover;
                        }

                        break;
                    default:
                        _diagnosticFlags = default (ViewDiagnosticFlags);

                        break;
                }
            }
        }

        private List<MenuItem []> CreateDiagnosticMenuItems ()
        {
            List<MenuItem []> menuItems = new ()
            {
                CreateDiagnosticFlagsMenuItems (),
                new MenuItem [] { null! },
                CreateDisabledEnabledMouseItems (),
                CreateDisabledEnabledMenuBorder (),
                CreateDisabledEnableUseSubMenusSingleFrame (),
                CreateKeyBindingsMenuItems ()
            };

            return menuItems;
        }

        // TODO: This should be an ConfigurationManager setting
        private MenuItem [] CreateDisabledEnabledMenuBorder ()
        {
            List<MenuItem> menuItems = new ();
            MiIsMenuBorderDisabled = new () { Title = "Disable Menu _Border" };

            MiIsMenuBorderDisabled.ShortcutKey =
                new Key (MiIsMenuBorderDisabled!.Title!.Substring (14, 1) [0]).WithAlt.WithCtrl.NoShift;
            MiIsMenuBorderDisabled.CheckType |= MenuItemCheckStyle.Checked;

            MiIsMenuBorderDisabled.Action += () =>
                                             {
                                                 MiIsMenuBorderDisabled.Checked = (bool)!MiIsMenuBorderDisabled.Checked!;

                                                 MenuBar!.MenusBorderStyle = !(bool)MiIsMenuBorderDisabled.Checked
                                                                                ? LineStyle.Single
                                                                                : LineStyle.None;
                                             };
            menuItems.Add (MiIsMenuBorderDisabled);

            return menuItems.ToArray ();
        }

        private MenuItem [] CreateDisabledEnabledMouseItems ()
        {
            List<MenuItem> menuItems = new ();
            MiIsMouseDisabled = new () { Title = "_Disable Mouse" };

            MiIsMouseDisabled.ShortcutKey =
                new Key (MiIsMouseDisabled!.Title!.Substring (1, 1) [0]).WithAlt.WithCtrl.NoShift;
            MiIsMouseDisabled.CheckType |= MenuItemCheckStyle.Checked;

            MiIsMouseDisabled.Action += () =>
                                        {
                                            MiIsMouseDisabled.Checked =
                                                Application.IsMouseDisabled = (bool)!MiIsMouseDisabled.Checked!;
                                        };
            menuItems.Add (MiIsMouseDisabled);

            return menuItems.ToArray ();
        }

        // TODO: This should be an ConfigurationManager setting
        private MenuItem [] CreateDisabledEnableUseSubMenusSingleFrame ()
        {
            List<MenuItem> menuItems = new ();
            MiUseSubMenusSingleFrame = new () { Title = "Enable _Sub-Menus Single Frame" };

            MiUseSubMenusSingleFrame.ShortcutKey = KeyCode.CtrlMask
                                                | KeyCode.AltMask
                                                | (KeyCode)MiUseSubMenusSingleFrame!.Title!.Substring (8, 1) [
                                                 0];
            MiUseSubMenusSingleFrame.CheckType |= MenuItemCheckStyle.Checked;

            MiUseSubMenusSingleFrame.Action += () =>
                                               {
                                                   MiUseSubMenusSingleFrame.Checked = (bool)!MiUseSubMenusSingleFrame.Checked!;
                                                   MenuBar!.UseSubMenusSingleFrame = (bool)MiUseSubMenusSingleFrame.Checked;
                                               };
            menuItems.Add (MiUseSubMenusSingleFrame);

            return menuItems.ToArray ();
        }

        private MenuItem [] CreateForce16ColorItems ()
        {
            List<MenuItem> menuItems = new ();

            MiForce16Colors = new ()
            {
                Title = "Force _16 Colors",
                ShortcutKey = Key.F6,
                Checked = Application.Force16Colors,
                CanExecute = () => Application.Driver?.SupportsTrueColor ?? false
            };
            MiForce16Colors.CheckType |= MenuItemCheckStyle.Checked;

            MiForce16Colors.Action += () =>
                                      {
                                          MiForce16Colors.Checked = Application.Force16Colors = (bool)!MiForce16Colors.Checked!;

                                          ((CheckBox)ShForce16Colors!.CommandView!).CheckedState =
                                              Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked;
                                          Application.Refresh ();
                                      };
            menuItems.Add (MiForce16Colors);

            return menuItems.ToArray ();
        }

        private MenuItem [] CreateKeyBindingsMenuItems ()
        {
            List<MenuItem> menuItems = new ();
            var item = new MenuItem { Title = "_Key Bindings", Help = "Change which keys do what" };

            item.Action += () =>
                           {
                               var dlg = new KeyBindingsDialog ();
                               Application.Run (dlg);
                               dlg.Dispose ();
                           };

            menuItems.Add (null!);
            menuItems.Add (item);

            return menuItems.ToArray ();
        }

        private void LoadedHandler (object? sender, EventArgs? args)
        {
            ConfigChanged ();

            MiIsMouseDisabled!.Checked = Application.IsMouseDisabled;

            if (ShVersion is { })
            {
                ShVersion.Title = $"{RuntimeEnvironment.OperatingSystem} {RuntimeEnvironment.OperatingSystemVersion}, {Driver.GetVersionInfo ()}";
            }

            if (_selectedScenario != null)
            {
                _selectedScenario = null;
                _isFirstRunning = false;
            }

            if (!_isFirstRunning)
            {
                ScenarioList.SetFocus ();
            }

            if (_statusBar is { })
            {
                _statusBar.VisibleChanged += (s, e) =>
                                            {
                                                ShowStatusBar = _statusBar.Visible;
                                                _statusBar.Height = _statusBar.Visible ? Dim.Auto () : 0;
                                            };
            }

            Loaded -= LoadedHandler;
            CategoryList!.EnsureSelectedItemVisible ();
            ScenarioList.EnsureSelectedCellIsVisible ();
        }

        /// <summary>Launches the selected scenario, setting the global _selectedScenario</summary>
        /// <param name="e"></param>
        private void ScenarioView_OpenSelectedItem (object? sender, EventArgs? e)
        {
            if (_selectedScenario is null)
            {
                // Save selected item state
                _cachedCategoryIndex = CategoryList!.SelectedItem;
                _cachedScenarioIndex = ScenarioList.SelectedRow;

                // Create new instance of scenario (even though Scenarios contains instances)
                var selectedScenarioName = (string)ScenarioList.Table [ScenarioList.SelectedRow, 0];

                _selectedScenario = (Scenario)Activator.CreateInstance (
                                                                        _scenarios!.FirstOrDefault (
                                                                                                    s => s.GetName ()
                                                                                                         == selectedScenarioName
                                                                                                   )!
                                                                                   .GetType ()
                                                                       )!;

                // Tell the main app to stop
                Application.RequestStop ();
            }
        }

        private void UnloadedHandler (object? sender, EventArgs? args)
        {
            Applied -= ConfigAppliedHandler;
            Unloaded -= UnloadedHandler;
            Dispose ();
        }
    }

    private struct Options
    {
        public string Driver;

        public string Scenario;
        /* etc. */
    }
}
