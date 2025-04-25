using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using static Terminal.Gui.ConfigurationManager;
using Command = Terminal.Gui.Command;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

#nullable enable

namespace UICatalog;

/// <summary>
///     This is the main UI Catalog app view. It is run fresh when the app loads (if a Scenario has not been passed on
///     the command line) and each time a Scenario ends.
/// </summary>
public class UICatalogTop : Toplevel
{
    // When a scenario is run, the main app is killed. The static
    // members are cached so that when the scenario exits the
    // main app UI can be restored to previous state

    // Theme Management
    public static string? CachedTheme { get; set; }

    // Note, we used to pass this to scenarios that run, but it just added complexity
    // So that was removed. But we still have this here to demonstrate how changing
    // the scheme works.
    public static string? CachedTopLevelColorScheme { get; set; }

    // Diagnostics
    private static ViewDiagnosticFlags _diagnosticFlags;

    public UICatalogTop ()
    {
        _diagnosticFlags = Diagnostics;

        _menuBar = CreateMenuBar ();
        _statusBar = CreateStatusBar ();
        _categoryList = CreateCategoryList ();
        _scenarioList = CreateScenarioList ();

        Add (_menuBar, _categoryList, _scenarioList, _statusBar);

        Loaded += LoadedHandler;
        Unloaded += UnloadedHandler;

        // Restore previous selections
        _categoryList.SelectedItem = _cachedCategoryIndex;
        _scenarioList.SelectedRow = _cachedScenarioIndex;

        Applied += ConfigAppliedHandler;
    }


    private static bool _isFirstRunning = true;

    private void LoadedHandler (object? sender, EventArgs? args)
    {
        if (_disableMouseCb is { })
        {
            _disableMouseCb.CheckedState = Application.IsMouseDisabled ? CheckState.Checked : CheckState.UnChecked;
        }

        if (_shVersion is { })
        {
            _shVersion.Title = $"{RuntimeEnvironment.OperatingSystem} {RuntimeEnvironment.OperatingSystemVersion}, {Driver!.GetVersionInfo ()}";
        }

        if (CachedSelectedScenario != null)
        {
            CachedSelectedScenario = null;
            _isFirstRunning = false;
        }

        if (!_isFirstRunning)
        {
            _scenarioList.SetFocus ();
        }

        if (_statusBar is { })
        {
            _statusBar.VisibleChanged += (s, e) => { ShowStatusBar = _statusBar.Visible; };
        }

        Loaded -= LoadedHandler;
        _categoryList!.EnsureSelectedItemVisible ();
        _scenarioList.EnsureSelectedCellIsVisible ();

        Apply ();

    }

    private void UnloadedHandler (object? sender, EventArgs? args)
    {
        Applied -= ConfigAppliedHandler;
        Unloaded -= UnloadedHandler;
    }

    #region MenuBar

    private readonly MenuBarv2? _menuBar;
    private CheckBox? _force16ColorsMenuItemCb;
    private OptionSelector? _themesRg;
    private OptionSelector? _topSchemeRg;
    private OptionSelector? _logLevelRg;
    private FlagSelector<ViewDiagnosticFlags>? _diagnosticFlagsSelector;
    private CheckBox? _disableMouseCb;

    private MenuBarv2 CreateMenuBar ()
    {
        MenuBarv2 menuBar = new (
                                 [
                                     new (
                                          "_File",
                                          [
                                              new MenuItemv2 ()
                                              {
                                                  Title ="_Quit",
                                                  HelpText = "Quit UI Catalog",
                                                  Key = Application.QuitKey,
                                                  // By not specifying TargetView the Key Binding will be Application-level
                                                  Command = Command.Quit
                                              }
                                          ]),
                                     new ("_Themes", CreateThemeMenuItems ()),
                                     new ("Diag_nostics", CreateDiagnosticMenuItems ()),
                                     new ("_Logging", CreateLoggingMenuItems ()),
                                     new (
                                          "_Help",
                                          [
                                              new MenuItemv2 (
                                                              "_Documentation",
                                                              "",
                                                              () => OpenUrl ("https://gui-cs.github.io/Terminal.GuiV2Docs"),
                                                              Key.F1
                                                             ),
                                              new MenuItemv2 (
                                                              "_README",
                                                              "",
                                                              () => OpenUrl ("https://github.com/gui-cs/Terminal.Gui"),
                                                              Key.F2
                                                             ),
                                              new MenuItemv2 (
                                                              "_About...",
                                                              "About UI Catalog",
                                                              () => MessageBox.Query (
                                                                                      "",
                                                                                      GetAboutBoxMessage (),
                                                                                      wrapMessage: false,
                                                                                      buttons: "_Ok"
                                                                                     ),
                                                              Key.A.WithCtrl
                                                             )
                                          ])
                                 ])
        {
            Title = "menuBar",
            Id = "menuBar"
        };

        return menuBar;

        View [] CreateThemeMenuItems ()
        {
            List<View> menuItems = [];

            _force16ColorsMenuItemCb = new ()
            {
                Title = "Force _16 Colors",
                CheckedState = Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked
            };

            _force16ColorsMenuItemCb.CheckedStateChanged += (sender, args) =>
            {
                Application.Force16Colors = args.CurrentValue == CheckState.Checked;

                _force16ColorsShortcutCb!.CheckedState = args.CurrentValue;
                Application.LayoutAndDraw ();
            };

            menuItems.Add (
                           new MenuItemv2
                           {
                               CommandView = _force16ColorsMenuItemCb
                           });

            menuItems.Add (new Line ());

            _themesRg = new ()
            {
                HighlightStyle = HighlightStyle.None,
                SelectedItem = Themes.Keys.ToList ().IndexOf (CachedTheme!.Replace ("_", string.Empty))
            };

            _themesRg.SelectedItemChanged += (_, args) =>
            {
                Themes!.Theme = Themes!.Keys.ToArray () [args.SelectedItem!.Value];
                CachedTheme = Themes!.Keys.ToArray () [args.SelectedItem!.Value];
                Apply ();
                SetNeedsDraw ();
            };

            var menuItem = new MenuItemv2
            {
                CommandView = _themesRg,
                HelpText = "Cycle Through Themes",
                Key = Key.T.WithCtrl
            };
            menuItems.Add (menuItem);

            menuItems.Add (new Line ());

            _topSchemeRg = new ()
            {
                HighlightStyle = HighlightStyle.None,
                SelectedItem = Colors.ColorSchemes.Keys.ToList().IndexOf(CachedTopLevelColorScheme!)
            };

            _topSchemeRg.SelectedItemChanged += (_, args) =>
            {
                CachedTopLevelColorScheme = Colors.ColorSchemes.Keys.ToArray () [args.SelectedItem!.Value];
                ColorScheme = Colors.ColorSchemes [CachedTopLevelColorScheme];
                SetNeedsDraw ();
            };

            menuItem = new ()
            {
                Title = "Color Scheme for Application._Top",
                SubMenu = new (
                               [
                                   new ()
                               {
                                   CommandView = _topSchemeRg,
                                   HelpText = "Cycle Through Color Schemes",
                                   Key = Key.S.WithCtrl
                               }
                               ])
            };
            menuItems.Add (menuItem);

            UpdateThemesMenu ();

            return menuItems.ToArray ();
        }

        View [] CreateDiagnosticMenuItems ()
        {
            List<View> menuItems = [];

            _diagnosticFlagsSelector = new ()
            {
                CanFocus = true,
                Styles = FlagSelectorStyles.ShowNone,
                HighlightStyle = HighlightStyle.None,
            };
            _diagnosticFlagsSelector.UsedHotKeys.Add (Key.D);
            _diagnosticFlagsSelector.AssignHotKeysToCheckBoxes = true;
            _diagnosticFlagsSelector.Value = Diagnostics;
            _diagnosticFlagsSelector.ValueChanged += (sender, args) =>
                                                     {
                                                         _diagnosticFlags = (ViewDiagnosticFlags)_diagnosticFlagsSelector.Value;
                                                         Diagnostics = _diagnosticFlags;
                                                     };

            menuItems.Add (
                           new MenuItemv2
                           {
                               CommandView = _diagnosticFlagsSelector,
                               HelpText = "View Diagnostics"
                           });

            menuItems.Add (new Line ());

            _disableMouseCb = new ()
            {
                Title = "_Disable Mouse",
                CheckedState = Application.IsMouseDisabled ? CheckState.Checked : CheckState.UnChecked
            };

            _disableMouseCb.CheckedStateChanged += (_, args) => { Application.IsMouseDisabled = args.CurrentValue == CheckState.Checked; };

            menuItems.Add (
                           new MenuItemv2
                           {
                               CommandView = _disableMouseCb,
                               HelpText = "Disable Mouse"
                           });

            return menuItems.ToArray ();
        }

        View [] CreateLoggingMenuItems ()
        {
            List<View?> menuItems = [];

            LogLevel [] logLevels = Enum.GetValues<LogLevel> ();

            _logLevelRg = new ()
            {
                AssignHotKeysToCheckBoxes = true,
                Options = Enum.GetNames<LogLevel> (),
                SelectedItem = logLevels.ToList ().IndexOf (Enum.Parse<LogLevel> (UICatalog.Options.DebugLogLevel)),
                HighlightStyle = HighlightStyle.Hover
            };

            _logLevelRg.SelectedItemChanged += (_, args) =>
            {
                UICatalog.Options = UICatalog.Options with { DebugLogLevel = Enum.GetName (logLevels [args.SelectedItem!.Value])! };

                UICatalog.LogLevelSwitch.MinimumLevel =
                    UICatalog.LogLevelToLogEventLevel (Enum.Parse<LogLevel> (UICatalog.Options.DebugLogLevel));
            };

            menuItems.Add (
                           new MenuItemv2
                           {
                               CommandView = _logLevelRg,
                               HelpText = "Cycle Through Log Levels",
                               Key = Key.L.WithCtrl
                           });

            // add a separator
            menuItems.Add (new Line ());

            menuItems.Add (
                           new MenuItemv2 (
                                           "_Open Log Folder",
                                           string.Empty,
                                           () => OpenUrl (UICatalog.LOGFILE_LOCATION)
                                          ));

            return menuItems.ToArray ()!;
        }

    }

    private void UpdateThemesMenu ()
    {
        if (_themesRg is null)
        {
            return;
        }

        _themesRg.AssignHotKeysToCheckBoxes = true;
        _themesRg.UsedHotKeys.Clear ();
        _themesRg.Options = Themes!.Keys.ToArray ();
        _themesRg.SelectedItem = Themes.Keys.ToList ().IndexOf (CachedTheme!.Replace ("_", string.Empty));

        if (_topSchemeRg is null)
        {
            return;
        }

        _topSchemeRg.AssignHotKeysToCheckBoxes = true;
        _topSchemeRg.UsedHotKeys.Clear ();
        int? selected = _topSchemeRg.SelectedItem;
        _topSchemeRg.Options = Colors.ColorSchemes.Keys.ToArray ();
        _topSchemeRg.SelectedItem = selected;

        if (CachedTopLevelColorScheme is null || !Colors.ColorSchemes.ContainsKey (CachedTopLevelColorScheme))
        {
            CachedTopLevelColorScheme = "Base";
        }

        _topSchemeRg.SelectedItem = Array.IndexOf (Colors.ColorSchemes.Keys.ToArray (), CachedTopLevelColorScheme);
    }

    #endregion MenuBar

    #region Scenario List

    private readonly TableView _scenarioList;

    private static int _cachedScenarioIndex;

    public static ObservableCollection<Scenario>? CachedScenarios { get; set; }

    // UI Catalog uses TableView for the scenario list instead of a ListView to demonstrate how
    // TableView works. There's no real reason not to use ListView. Because we use TableView, and TableView
    // doesn't (currently) have CollectionNavigator support built in, we implement it here, within the app.
    private readonly CollectionNavigator _scenarioCollectionNav = new ();

    // If set, holds the scenario the user selected to run
    public static Scenario? CachedSelectedScenario { get; set; }

    private TableView CreateScenarioList ()
    {
        // Create the scenario list. The contents of the scenario list changes whenever the
        // Category list selection changes (to show just the scenarios that belong to the selected
        // category).
        TableView scenarioList = new ()
        {
            X = Pos.Right (_categoryList!) - 1,
            Y = Pos.Bottom (_menuBar!),
            Width = Dim.Fill (),
            Height = Dim.Fill (
                               Dim.Func (
                                         () =>
                                         {
                                             if (_statusBar!.NeedsLayout)
                                             {
                                                 throw new LayoutException ("DimFunc.Fn aborted because dependent View needs layout.");

                                                 //_statusBar.Layout ();
                                             }

                                             return _statusBar.Frame.Height;
                                         })),

            //AllowsMarking = false,
            CanFocus = true,
            Title = "_Scenarios",
            BorderStyle = _categoryList!.BorderStyle,
            SuperViewRendersLineCanvas = true
        };

        // TableView provides many options for table headers. For simplicity, we turn all
        // of these off. By enabling FullRowSelect and turning off headers, TableView looks just
        // like a ListView
        scenarioList.FullRowSelect = true;
        scenarioList.Style.ShowHeaders = false;
        scenarioList.Style.ShowHorizontalHeaderOverline = false;
        scenarioList.Style.ShowHorizontalHeaderUnderline = false;
        scenarioList.Style.ShowHorizontalBottomline = false;
        scenarioList.Style.ShowVerticalCellLines = false;
        scenarioList.Style.ShowVerticalHeaderLines = false;

        /* By default, TableView lays out columns at render time and only
         * measures y rows of data at a time.  Where y is the height of the
         * console. This is for the following reasons:
         *
         * - Performance, when tables have a large amount of data
         * - Defensive, prevents a single wide cell value pushing other
         *   columns off-screen (requiring horizontal scrolling
         *
         * In the case of UICatalog here, such an approach is overkill so
         * we just measure all the data ourselves and set the appropriate
         * max widths as ColumnStyles
         */
        int longestName = CachedScenarios!.Max (s => s.GetName ().Length);

        scenarioList.Style.ColumnStyles.Add (
                                             0,
                                             new () { MaxWidth = longestName, MinWidth = longestName, MinAcceptableWidth = longestName }
                                            );
        scenarioList.Style.ColumnStyles.Add (1, new () { MaxWidth = 1 });
        scenarioList.CellActivated += ScenarioView_OpenSelectedItem;

        // TableView typically is a grid where nav keys are biased for moving left/right.
        scenarioList.KeyBindings.Remove (Key.Home);
        scenarioList.KeyBindings.Add (Key.Home, Command.Start);
        scenarioList.KeyBindings.Remove (Key.End);
        scenarioList.KeyBindings.Add (Key.End, Command.End);

        // Ideally, TableView.MultiSelect = false would turn off any keybindings for
        // multi-select options. But it currently does not. UI Catalog uses Ctrl-A for
        // a shortcut to About.
        scenarioList.MultiSelect = false;
        scenarioList.KeyBindings.Remove (Key.A.WithCtrl);

        return scenarioList;
    }


    /// <summary>Launches the selected scenario, setting the global _selectedScenario</summary>
    /// <param name="e"></param>
    private void ScenarioView_OpenSelectedItem (object? sender, EventArgs? e)
    {
        if (CachedSelectedScenario is null)
        {
            // Save selected item state
            _cachedCategoryIndex = _categoryList!.SelectedItem;
            _cachedScenarioIndex = _scenarioList.SelectedRow;

            // Create new instance of scenario (even though Scenarios contains instances)
            var selectedScenarioName = (string)_scenarioList.Table [_scenarioList.SelectedRow, 0];

            CachedSelectedScenario = (Scenario)Activator.CreateInstance (
                                                                         CachedScenarios!.FirstOrDefault (
                                                                                                          s => s.GetName ()
                                                                                                              == selectedScenarioName
                                                                                                         )!
                                                                                         .GetType ()
                                                                        )!;

            // Tell the main app to stop
            Application.RequestStop ();
        }
    }

    #endregion Scenario List

    #region Category List

    private readonly ListView? _categoryList;
    private static int _cachedCategoryIndex;
    public static ObservableCollection<string>? CachedCategories { get; set; }

    private ListView CreateCategoryList ()
    {
        // Create the Category list view. This list never changes.
        ListView categoryList = new ()
        {
            X = 0,
            Y = Pos.Bottom (_menuBar!),
            Width = Dim.Auto (),
            Height = Dim.Fill (
                               Dim.Func (
                                         () =>
                                         {
                                             if (_statusBar!.NeedsLayout)
                                             {
                                                 throw new LayoutException ("DimFunc.Fn aborted because dependent View needs layout.");

                                                 //_statusBar.Layout ();
                                             }

                                             return _statusBar.Frame.Height;
                                         })),
            AllowsMarking = false,
            CanFocus = true,
            Title = "_Categories",
            BorderStyle = LineStyle.Rounded,
            SuperViewRendersLineCanvas = true,
            Source = new ListWrapper<string> (CachedCategories)
        };
        categoryList.OpenSelectedItem += (s, a) => { _scenarioList!.SetFocus (); };
        categoryList.SelectedItemChanged += CategoryView_SelectedChanged;

        // This enables the scrollbar by causing lazy instantiation to happen
        categoryList.VerticalScrollBar.AutoShow = true;

        return categoryList;
    }

    private void CategoryView_SelectedChanged (object? sender, ListViewItemEventArgs? e)
    {
        string item = CachedCategories! [e!.Item];
        ObservableCollection<Scenario> newScenarioList;

        if (e.Item == 0)
        {
            // First category is "All"
            newScenarioList = CachedScenarios!;
        }
        else
        {
            newScenarioList = new (CachedScenarios!.Where (s => s.GetCategories ().Contains (item)).ToList ());
        }

        _scenarioList.Table = new EnumerableTableSource<Scenario> (
                                                                   newScenarioList,
                                                                   new ()
                                                                   {
                                                                       { "Name", s => s.GetName () }, { "Description", s => s.GetDescription () }
                                                                   }
                                                                  );

        // Create a collection of just the scenario names (the 1st column in our TableView)
        // for CollectionNavigator. 
        List<object> firstColumnList = [];

        for (var i = 0; i < _scenarioList.Table.Rows; i++)
        {
            firstColumnList.Add (_scenarioList.Table [i, 0]);
        }

        _scenarioCollectionNav.Collection = firstColumnList;
    }

    #endregion Category List

    #region StatusBar

    private readonly StatusBar? _statusBar;

    [SerializableConfigurationProperty (Scope = typeof (AppScope), OmitClassName = true)]
    [JsonPropertyName ("UICatalog.StatusBar")]
    public static bool ShowStatusBar { get; set; } = true;

    private Shortcut? _shQuit;
    private Shortcut? _shVersion;
    private CheckBox? _force16ColorsShortcutCb;

    private StatusBar CreateStatusBar ()
    {
        StatusBar statusBar = new ()
        {
            Visible = ShowStatusBar,
            AlignmentModes = AlignmentModes.IgnoreFirstOrLast,
            CanFocus = false
        };

        // ReSharper disable All
        statusBar.Height = Dim.Auto (
                                     DimAutoStyle.Auto,
                                     minimumContentDim: Dim.Func (() => statusBar.Visible ? 1 : 0),
                                     maximumContentDim: Dim.Func (() => statusBar.Visible ? 1 : 0));
        // ReSharper restore All

        _shQuit = new ()
        {
            CanFocus = false,
            Title = "Quit",
            Key = Application.QuitKey
        };

        _shVersion = new ()
        {
            Title = "Version Info",
            CanFocus = false
        };

        var statusBarShortcut = new Shortcut
        {
            Key = Key.F10,
            Title = "Show/Hide Status Bar",
            CanFocus = false
        };

        statusBarShortcut.Accepting += (sender, args) =>
        {
            statusBar.Visible = !_statusBar!.Visible;
            args.Cancel = true;
        };

        _force16ColorsShortcutCb = new ()
        {
            Title = "16 color mode",
            CheckedState = Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
            CanFocus = false
        };

        _force16ColorsShortcutCb.CheckedStateChanging += (sender, args) =>
        {
            Application.Force16Colors = args.NewValue == CheckState.Checked;
            _force16ColorsMenuItemCb!.CheckedState = args.NewValue;
            Application.LayoutAndDraw ();
        };

        statusBar.Add (
                       _shQuit,
                       statusBarShortcut,
                       new Shortcut
                       {
                           CanFocus = false,
                           CommandView = _force16ColorsShortcutCb,
                           HelpText = "",
                           BindKeyToApplication = true,
                           Key = Key.F7
                       },
                       _shVersion
                      );

        return statusBar;
    }

    #endregion StatusBar

    #region Configuration Manager

    /// <summary>
    ///     Called when CM has applied changes.
    /// </summary>
    private void ConfigApplied ()
    {
        CachedTheme = Themes?.Theme;

        UpdateThemesMenu ();

        ColorScheme = Colors.ColorSchemes [CachedTopLevelColorScheme!];

        if (_shQuit is { })
        {
            _shQuit.Key = Application.QuitKey;
        }

        if (_statusBar is { })
        {
            _statusBar.Visible = ShowStatusBar;
        }

        _disableMouseCb!.CheckedState = Application.IsMouseDisabled ? CheckState.Checked : CheckState.UnChecked;
        _force16ColorsShortcutCb!.CheckedState = Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked;

        Application.Top?.SetNeedsDraw ();
    }

    private void ConfigAppliedHandler (object? sender, ConfigurationManagerEventArgs? a) { ConfigApplied (); }

    #endregion Configuration Manager

    /// <summary>
    ///     Gets the message displayed in the About Box. `public` so it can be used from Unit tests.
    /// </summary>
    /// <returns></returns>
    public static string GetAboutBoxMessage ()
    {
        // NOTE: Do not use multiline verbatim strings here.
        // WSL gets all confused.
        StringBuilder msg = new ();
        msg.AppendLine ("UI Catalog: A comprehensive sample library and test app for");
        msg.AppendLine ();

        msg.AppendLine (
                        """
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

    public static void OpenUrl (string url)
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
}
