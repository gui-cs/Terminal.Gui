#nullable enable
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;
using Trace = Terminal.Gui.Tracing.Trace;

namespace UICatalog;

/// <summary>
///     This is the main UI Catalog app view. It is run fresh when the app loads (if a Scenario has not been passed on
///     the command line) and each time a Scenario ends.
/// </summary>
public sealed class UICatalogRunnable : Runnable
{
    // When a scenario is run, the main app is killed. The static
    // members are cached so that when the scenario exits the
    // main app UI can be restored to previous state

    // Note, we used to pass this to scenarios that run, but it just added complexity
    // So that was removed. But we still have this here to demonstrate how changing
    // the scheme works.
    public static string? CachedRunnableScheme { get; set; }

    // Diagnostics
    private static ViewDiagnosticFlags _diagnosticFlags;

    public UICatalogRunnable ()
    {
        _diagnosticFlags = Diagnostics;
        SchemeName = CachedRunnableScheme = SchemeManager.SchemesToSchemeName (Schemes.Base);
        ConfigurationManager.Applied += ConfigAppliedHandler;
    }

    /// <inheritdoc/>
    public override void BeginInit ()
    {
        _menuBar = CreateMenuBar ();
        _statusBar = CreateStatusBar ();
        _categoryList = CreateCategoryList ();
        _scenarioList = CreateScenarioList ();

        Add (_menuBar, _categoryList, _scenarioList, _statusBar);

        // Restore previous selections
        if (_categoryList.Source?.Count > 0)
        {
            _categoryList.SelectedItem = _cachedCategoryIndex ?? 0;
        }
        else
        {
            _categoryList.SelectedItem = null;
        }
        _scenarioList.SelectedRow = _cachedScenarioIndex;

        base.BeginInit ();
    }

    /// <inheritdoc/>
    protected override void OnIsModalChanged (bool newIsModal)
    {
        _disableMouseCb?.Value = App!.Mouse.IsMouseDisabled ? CheckState.Checked : CheckState.UnChecked;

        _shVersion?.Title = $"{RuntimeEnvironment.OperatingSystem} {RuntimeEnvironment.OperatingSystemVersion}, {App!.Driver!.GetVersionInfo ()}";

        if (string.IsNullOrEmpty ((string?)Result))
        {
            _isFirstRunning = false;
        }

        if (!_isFirstRunning)
        {
            _scenarioList?.SetFocus ();
        }

        _categoryList?.EnsureSelectedItemVisible ();
        _scenarioList?.EnsureSelectedCellIsVisible ();

        if (ShowStatusBar)
        {
            _statusBar!.Height = Dim.Auto ();
        }
        else
        {
            _statusBar!.Height = 0;
        }
    }

    /// <inheritdoc/>
    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        if (newIsRunning)
        {
            // Show error dialog if any errors occurred during the scenario
            if (!UICatalog.LogCapture.HasErrors)
            {
                return;
            }

            if (_scenarioList is { })
            {
                ShowScenarioErrorsDialog (App!, (string)_scenarioList.Table! [_scenarioList.SelectedRow, 0], UICatalog.LogCapture.GetScenarioLogs ());
            }

            UICatalog.LogCapture.HasErrors = false;

            return;
        }

        ConfigurationManager.Applied -= ConfigAppliedHandler;
    }

    // Track if this is the first time running the main UI Catalog screen
    private static bool _isFirstRunning = true;

    #region MenuBar

    private MenuBar? _menuBar;
    private CheckBox? _force16ColorsMenuItemCb;
    private OptionSelector? _themesSelector;
    private OptionSelector? _topSchemesSelector;
    private OptionSelector? _logLevelSelector;
    private FlagSelector<ViewDiagnosticFlags>? _diagnosticFlagsSelector;
    private CheckBox? _disableMouseCb;

    private MenuBar CreateMenuBar ()
    {
        MenuBar menuBar = new ([
                                   new MenuBarItem (Strings.menuFile,
                                                    [
                                                        new MenuItem
                                                        {
                                                            Title = Strings.cmdQuit,
                                                            HelpText = "Quit UI Catalog",
                                                            Key = Application.GetDefaultKey (Command.Quit),

                                                            // By not specifying TargetView the Key Binding will be Application-level
                                                            Command = Command.Quit
                                                        }
                                                    ]),
                                   new MenuBarItem ("_Themes", CreateThemeMenuItems ()),
                                   new MenuBarItem ("Diag_nostics", CreateDiagnosticMenuItems ()),
                                   new MenuBarItem ("_Logging", CreateLoggingMenuItems ()),
                                   new MenuBarItem (Strings.menuHelp,
                                                    [
                                                        new MenuItem ("_Documentation",
                                                                      "API docs",
                                                                      () => Link.OpenUrl ("https://gui-cs.github.io/Terminal.Gui"),
                                                                      Key.F1),
                                                        new MenuItem ("_README",
                                                                      "Project readme",
                                                                      () => Link.OpenUrl ("https://github.com/gui-cs/Terminal.Gui"),
                                                                      Key.F2),
                                                        new MenuItem ("_About...",
                                                                      "About UI Catalog",
                                                                      () => MessageBox.Query (App!,
                                                                                              "",
                                                                                              GetAboutBoxMessage (),
                                                                                              wrapMessage: false,
                                                                                              buttons: Strings.btnOk),
                                                                      Key.A.WithCtrl)
                                                    ])
                               ]) { Title = "menuBar", Id = "menuBar" };

        return menuBar;

        View [] CreateThemeMenuItems ()
        {
            List<View> menuItems = [];

            _force16ColorsMenuItemCb = new CheckBox { Title = "Force _16 Colors", Value = Driver.Force16Colors ? CheckState.Checked : CheckState.UnChecked };

            menuItems.Add (new MenuItem
            {
                CommandView = _force16ColorsMenuItemCb,
                Action = () =>
                         {
                             Driver.Force16Colors = !Driver.Force16Colors;
                             _force16ColorsShortcutCb!.Value = Driver.Force16Colors ? CheckState.Checked : CheckState.UnChecked;
                             SetNeedsDraw ();
                         }
            });

            menuItems.Add (new Line ());

            if (ConfigurationManager.IsEnabled)
            {
                _themesSelector = new OptionSelector { CanFocus = true };
                _themesSelector.ValueChanged += OnThemesSelectorOnValueChanged;

                MenuItem menuItem = new () { CommandView = _themesSelector, HelpText = "Cycle Through Themes", Key = Key.T.WithCtrl };

                menuItems.Add (menuItem);

                menuItems.Add (new Line ());

                _topSchemesSelector = new OptionSelector ();

                _topSchemesSelector.ValueChanged += OnTopSchemesSelectorOnValueChanged;

                menuItem = new MenuItem
                {
                    Title = "Scheme for Runnable",
                    SubMenu = new Menu ([new MenuItem { CommandView = _topSchemesSelector, HelpText = "Cycle Through Schemes", Key = Key.S.WithCtrl }])
                };
                menuItems.Add (menuItem);

                UpdateThemesMenu ();
            }
            else
            {
                menuItems.Add (new MenuItem { Title = "Configuration Manager is not Enabled", Enabled = false });
            }

            return menuItems.ToArray ();

            void OnTopSchemesSelectorOnValueChanged (object? _, ValueChangedEventArgs<int?> args)
            {
                if (args.NewValue is null)
                {
                    return;
                }
                CachedRunnableScheme = SchemeManager.GetSchemesForCurrentTheme ().Keys.ToArray () [(int)args.NewValue];
                SchemeName = CachedRunnableScheme;
                SetNeedsDraw ();
            }

            void OnThemesSelectorOnValueChanged (object? _, ValueChangedEventArgs<int?> args)
            {
                if (args.NewValue is null)
                {
                    return;
                }
                ThemeManager.Theme = ThemeManager.GetThemeNames () [(int)args.NewValue];
            }
        }

        View [] CreateDiagnosticMenuItems ()
        {
            List<View> menuItems = [];

            _diagnosticFlagsSelector = new FlagSelector<ViewDiagnosticFlags> { Styles = SelectorStyles.ShowNoneFlag };
            _diagnosticFlagsSelector.UsedHotKeys.Add (Key.D);
            _diagnosticFlagsSelector.AssignHotKeys = true;
            _diagnosticFlagsSelector.Value = Diagnostics;

            _diagnosticFlagsSelector.ValueChanged += OnDiagnosticFlagsSelectorOnValueChanged;

            MenuItem diagFlagMenuItem = new () { CommandView = _diagnosticFlagsSelector, HelpText = "View Diagnostics" };

            menuItems.Add (diagFlagMenuItem);

            menuItems.Add (new Line ());

            _disableMouseCb = new CheckBox
            {
                Title = "_Disable MouseEventArgs", Value = App!.Mouse.IsMouseDisabled ? CheckState.Checked : CheckState.UnChecked
            };

            _disableMouseCb.ValueChanged += (_, args) => { App!.Mouse.IsMouseDisabled = args.NewValue == CheckState.Checked; };
            menuItems.Add (new MenuItem { CommandView = _disableMouseCb, HelpText = "Disable MouseEventArgs" });

            return menuItems.ToArray ();

            void OnDiagnosticFlagsSelectorOnValueChanged (object? _, EventArgs<ViewDiagnosticFlags?> args) =>
                Diagnostics = args.Value ?? ViewDiagnosticFlags.Off;
        }

        View [] CreateLoggingMenuItems ()
        {
            List<View?> menuItems = [];

            _logLevelSelector = new OptionSelector
            {
                AssignHotKeys = true,
                Labels = Enum.GetNames<LogLevel> (),
                Value = Enum.GetValues<LogLevel> ().ToList ().IndexOf (Enum.Parse<LogLevel> (UICatalog.Options.DebugLogLevel))
            };

            MenuItem logMenu = new () { CommandView = _logLevelSelector, HelpText = "Cycle Through Log Levels", Key = Key.L.WithCtrl };

            _logLevelSelector.ValueChanged += OnLogLevelSelectorOnValueChanged;

            menuItems.Add (logMenu);

            // add a separator
            menuItems.Add (new Line ());

            // Trace toggles
            CheckBox lifecycleTraceCheckBox = new ()
            {
                Text = "_Lifecycle",
                Value = Trace.EnabledCategories.HasFlag (TraceCategory.Lifecycle) ? CheckState.Checked : CheckState.UnChecked,
                CanFocus = false
            };

            lifecycleTraceCheckBox.ValueChanging += (_, e) =>
                                                    {
                                                        if (e.NewValue == CheckState.Checked)
                                                        {
                                                            Trace.EnabledCategories |= TraceCategory.Lifecycle;
                                                        }
                                                        else
                                                        {
                                                            Trace.EnabledCategories &= ~TraceCategory.Lifecycle;
                                                        }
                                                    };

            menuItems.Add (new MenuItem { CommandView = lifecycleTraceCheckBox, HelpText = "Toggle App & Driver lifecycle tracing", Key = Key.L.WithCtrl });

            // ReSharper disable once StringLiteralTypo
            CheckBox commandTraceCheckBox = new ()
            {
                Text = "C_ommand",
                Value = Trace.EnabledCategories.HasFlag (TraceCategory.Command) ? CheckState.Checked : CheckState.UnChecked,
                CanFocus = false
            };

            commandTraceCheckBox.ValueChanging += (_, e) =>
                                                  {
                                                      if (e.NewValue == CheckState.Checked)
                                                      {
                                                          Trace.EnabledCategories |= TraceCategory.Command;
                                                      }
                                                      else
                                                      {
                                                          Trace.EnabledCategories &= ~TraceCategory.Command;
                                                      }
                                                  };

            menuItems.Add (new MenuItem { CommandView = commandTraceCheckBox, HelpText = "Toggle Command tracing", Key = Key.C.WithCtrl });

            CheckBox mouseTraceCheckBox = new ()
            {
                Text = "_Mouse", Value = Trace.EnabledCategories.HasFlag (TraceCategory.Mouse) ? CheckState.Checked : CheckState.UnChecked, CanFocus = false
            };

            mouseTraceCheckBox.ValueChanging += (_, e) =>
                                                {
                                                    if (e.NewValue == CheckState.Checked)
                                                    {
                                                        Trace.EnabledCategories |= TraceCategory.Mouse;
                                                    }
                                                    else
                                                    {
                                                        Trace.EnabledCategories &= ~TraceCategory.Mouse;
                                                    }
                                                };

            menuItems.Add (new MenuItem { CommandView = mouseTraceCheckBox, HelpText = "Toggle Mouse event tracing", Key = Key.U.WithCtrl });

            CheckBox keyboardTraceCheckBox = new ()
            {
                Text = "_Keyboard",
                Value = Trace.EnabledCategories.HasFlag (TraceCategory.Keyboard) ? CheckState.Checked : CheckState.UnChecked,
                CanFocus = false
            };

            keyboardTraceCheckBox.ValueChanging += (_, e) =>
                                                   {
                                                       if (e.NewValue == CheckState.Checked)
                                                       {
                                                           Trace.EnabledCategories |= TraceCategory.Keyboard;
                                                       }
                                                       else
                                                       {
                                                           Trace.EnabledCategories &= ~TraceCategory.Keyboard;
                                                       }
                                                   };

            menuItems.Add (new MenuItem { CommandView = keyboardTraceCheckBox, HelpText = "Toggle Keyboard event tracing", Key = Key.K.WithCtrl });

            CheckBox navTraceCheckBox = new ()
            {
                Text = "_Navigation",
                Value = Trace.EnabledCategories.HasFlag (TraceCategory.Navigation) ? CheckState.Checked : CheckState.UnChecked,
                CanFocus = false
            };

            navTraceCheckBox.ValueChanging += (_, e) =>
                                              {
                                                  if (e.NewValue == CheckState.Checked)
                                                  {
                                                      Trace.EnabledCategories |= TraceCategory.Navigation;
                                                  }
                                                  else
                                                  {
                                                      Trace.EnabledCategories &= ~TraceCategory.Navigation;
                                                  }
                                              };

            // TODO: Implement Trace.Navigation and enable this
            menuItems.Add (new MenuItem
            {
                Enabled = false, CommandView = navTraceCheckBox, HelpText = "Toggle Focus & TabBehavior tracing", Key = Key.K.WithCtrl
            });

            CheckBox configTraceCheckBox = new ()
            {
                Text = "Con_figuration",
                Value = Trace.EnabledCategories.HasFlag (TraceCategory.Configuration) ? CheckState.Checked : CheckState.UnChecked,
                CanFocus = false
            };

            configTraceCheckBox.ValueChanging += (_, e) =>
                                                 {
                                                     if (e.NewValue == CheckState.Checked)
                                                     {
                                                         Trace.EnabledCategories |= TraceCategory.Configuration;
                                                     }
                                                     else
                                                     {
                                                         Trace.EnabledCategories &= ~TraceCategory.Configuration;
                                                     }
                                                 };

            menuItems.Add (new MenuItem { CommandView = configTraceCheckBox, HelpText = "Toggle Configuration management tracing" });

            CheckBox drawTraceCheckBox = new ()
            {
                Text = "_Draw",
                Value = Trace.EnabledCategories.HasFlag (TraceCategory.Draw) ? CheckState.Checked : CheckState.UnChecked,
                CanFocus = false
            };

            drawTraceCheckBox.ValueChanging += (_, e) =>
                                               {
                                                   if (e.NewValue == CheckState.Checked)
                                                   {
                                                       Trace.EnabledCategories |= TraceCategory.Draw;
                                                   }
                                                   else
                                                   {
                                                       Trace.EnabledCategories &= ~TraceCategory.Draw;
                                                   }
                                               };

            menuItems.Add (new MenuItem { CommandView = drawTraceCheckBox, HelpText = "Toggle Draw operation tracing" });

            // add a separator
            menuItems.Add (new Line ());

            menuItems.Add (new MenuItem ("_Open Log Folder", string.Empty, () => Link.OpenUrl (UICatalog.LOGFILE_LOCATION)));

            return menuItems.ToArray ()!;

            void OnLogLevelSelectorOnValueChanged (object? _, ValueChangedEventArgs<int?> args)
            {
                UICatalog.Options = UICatalog.Options with { DebugLogLevel = Enum.GetName (Enum.GetValues<LogLevel> () [args.NewValue!.Value])! };

                UICatalog.LogLevelSwitch.MinimumLevel = UICatalog.LogLevelToLogEventLevel (Enum.Parse<LogLevel> (UICatalog.Options.DebugLogLevel));
            }
        }
    }

    private void UpdateThemesMenu ()
    {
        if (_themesSelector is null)
        {
            return;
        }

        string [] labels = ThemeManager.GetThemeNames ().ToArray ();

        // Prevent resetting the selector if the themes have not changed
        // BUGBUG: Just comparing the size of the arrays is not really sufficient. We should actually
        // BUGBUG: deep compare the arrays.
        if (_themesSelector.Labels is null || labels.Length != _themesSelector.Labels.Count)
        {
            _themesSelector.Value = null;
            _themesSelector.AssignHotKeys = true;
            _themesSelector.UsedHotKeys.Clear ();
            _themesSelector.Labels = labels;
            _themesSelector.Value = ThemeManager.GetThemeNames ().IndexOf (ThemeManager.GetCurrentThemeName ());
        }

        if (_topSchemesSelector is null)
        {
            return;
        }

        _topSchemesSelector.AssignHotKeys = true;
        _topSchemesSelector.UsedHotKeys.Clear ();
        int? selectedScheme = _topSchemesSelector.Value;
        _topSchemesSelector.Labels = SchemeManager.GetSchemeNames ().ToArray ();
        _topSchemesSelector.Value = selectedScheme;

        if (CachedRunnableScheme is null || !SchemeManager.GetSchemeNames ().Contains (CachedRunnableScheme))
        {
            CachedRunnableScheme = SchemeManager.SchemesToSchemeName (Schemes.Base);
        }

        int newSelectedItem = SchemeManager.GetSchemeNames ().IndexOf (CachedRunnableScheme!);

        // if the item is in bounds then select it
        if (newSelectedItem >= 0 && newSelectedItem < SchemeManager.GetSchemeNames ().Count)
        {
            _topSchemesSelector.Value = newSelectedItem;
        }
    }

    #endregion MenuBar

    #region Scenario List

    private TableView? _scenarioList;
    private static int _cachedScenarioIndex;

    public static ObservableCollection<Scenario>? CachedScenarios { get; set; }

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
            Height = Dim.Height (_categoryList),
            CanFocus = true,
            Title = "_Scenarios",
            BorderStyle = _categoryList!.BorderStyle,
            SuperViewRendersLineCanvas = true
        };

        scenarioList.Border.Settings = BorderSettings.Title | BorderSettings.Tab;
        scenarioList.Border.TabSide = Side.Top;
        scenarioList.Border.Thickness = new Thickness (1, 2, 1, 1);

        // TableView provides many options for table headers. For simplicity, we turn all
        // of these off. By enabling FullRowSelect and turning off headers, TableView looks just
        // like a ListView
        scenarioList.FullRowSelect = true;
        scenarioList.Style.ShowHeaders = false;
        scenarioList.Style.ShowHorizontalHeaderOverline = false;
        scenarioList.Style.ShowHorizontalHeaderUnderline = false;
        scenarioList.Style.ShowHorizontalBottomLine = false;
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

        scenarioList.Style.ColumnStyles.Add (0, new ColumnStyle { MaxWidth = longestName, MinWidth = longestName, MinAcceptableWidth = longestName });
        scenarioList.Style.ColumnStyles.Add (1, new ColumnStyle { MaxWidth = 1 });
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
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ScenarioView_OpenSelectedItem (object? sender, EventArgs? e)
    {
        // Save selected item state
        _cachedCategoryIndex = _categoryList!.SelectedItem;
        _cachedScenarioIndex = _scenarioList!.SelectedRow;

        // Set the Result to the selected scenario name
        Result = (string)_scenarioList.Table! [_scenarioList.SelectedRow, 0];
        Logging.Information ($"Scenario Selected; Stopping {GetType ().Name}: {Result}");
        App?.RequestStop ();
    }

    #endregion Scenario List

    #region Category List

    private ListView? _categoryList;
    private static int? _cachedCategoryIndex;
    public static ObservableCollection<string>? CachedCategories { get; set; }

    private ListView CreateCategoryList ()
    {
        // Create the Category list view. This list never changes.
        ListView categoryList = new ()
        {
            X = 0,
            Y = Pos.Bottom (_menuBar!),
            Width = Dim.Auto (),
            Height = Dim.Fill (_statusBar!),
            ShowMarks = false,
            CanFocus = true,
            Title = "_Categories",
            BorderStyle = LineStyle.Rounded,
            SuperViewRendersLineCanvas = true,
            Source = new ListWrapper<string> (CachedCategories)
        };
        categoryList.Border.Settings = BorderSettings.Title | BorderSettings.Tab;
        categoryList.Border.TabSide = Side.Top;
        categoryList.Border.Thickness = new Thickness (1, 2, 1, 1);

        categoryList.Accepting += (_, e) =>
                                  {
                                      _scenarioList!.SetFocus ();
                                      e.Handled = true;
                                  };
        categoryList.ValueChanged += CategoryView_SelectedChanged;

        // This enables the scrollbar by causing lazy instantiation to happen
        categoryList.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        return categoryList;
    }

    private void CategoryView_SelectedChanged (object? sender, ValueChangedEventArgs<int?> e)
    {
        if (e.NewValue is null)
        {
            return;
        }
        string item = CachedCategories! [e.NewValue.Value];
        ObservableCollection<Scenario> newScenarioList;

        if (e.NewValue == 0)
        {
            // First category is "All"
            newScenarioList = CachedScenarios!;
        }
        else
        {
            newScenarioList = new ObservableCollection<Scenario> (CachedScenarios!.Where (s => s.GetCategories ().Contains (item)).ToList ());
        }

        _scenarioList!.Table = new EnumerableTableSource<Scenario> (newScenarioList,
                                                                    new Dictionary<string, Func<Scenario, object>>
                                                                    {
                                                                        { "Name", s => s.GetName () }, { "Description", s => s.GetDescription () }
                                                                    });
    }

    #endregion Category List

    #region StatusBar

    private StatusBar? _statusBar;

    [ConfigurationProperty (Scope = typeof (AppSettingsScope), OmitClassName = true)]
    [JsonPropertyName ("UICatalog.StatusBar")]
    public static bool ShowStatusBar
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;
            StatusBarChanged?.Invoke (null, new ValueChangedEventArgs<bool> (!field, field));
        }
    } = true;

    /// <summary>Raised when "UICatalog.StatusBar" changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<bool>>? StatusBarChanged;

    private Shortcut? _shQuit;
    private Shortcut? _shVersion;
    private CheckBox? _force16ColorsShortcutCb;

    private StatusBar CreateStatusBar ()
    {
        StatusBar statusBar = new () { AlignmentModes = AlignmentModes.IgnoreFirstOrLast, CanFocus = false };

        // This demonstrates a shortcut that invokes RequestStop to quit the app
        _shQuit = new Shortcut { CanFocus = false, Title = "Quit", Key = Application.GetDefaultKey (Command.Quit), Action = RequestStop };

        _shVersion = new Shortcut { Title = "Version Info", CanFocus = false };

        Shortcut statusBarShortcut = new () { Key = Key.F10, Title = "Show/Hide Status Bar", CanFocus = false, Action = () => ShowStatusBar = !ShowStatusBar };

        _force16ColorsShortcutCb = new CheckBox
        {
            Title = "16 color mode", Value = Driver.Force16Colors ? CheckState.Checked : CheckState.UnChecked, CanFocus = true
        };

        Shortcut force16ColorsShortcut = new ()
        {
            CanFocus = false,
            CommandView = _force16ColorsShortcutCb,
            HelpText = "",
            BindKeyToApplication = true,
            Key = Key.F7,
            Action = () =>
                     {
                         Driver.Force16Colors = !Driver.Force16Colors;
                         _force16ColorsMenuItemCb!.Value = Driver.Force16Colors ? CheckState.Checked : CheckState.UnChecked;
                         SetNeedsDraw ();
                     }
        };
        statusBar.Add (_shQuit, statusBarShortcut, force16ColorsShortcut, _shVersion);

        if (UICatalog.Options.DontEnableConfigurationManagement)
        {
            statusBar.AddShortcutAt (statusBar.SubViews.ToList ().IndexOf (_shVersion), new Shortcut { Title = "CM is Disabled" });
        }

        StatusBarChanged += (_, args) =>
                            {
                                switch (args.NewValue)
                                {
                                    case true:
                                        _statusBar!.Height = Dim.Auto ();

                                        break;

                                    case false:
                                        _statusBar!.Height = 0;

                                        break;
                                }
                            };

        return statusBar;
    }

    #endregion StatusBar

    #region Configuration Manager

    /// <summary>
    ///     Called when CM has applied changes.
    /// </summary>
    private void ConfigApplied ()
    {
        UpdateThemesMenu ();

        SchemeName = CachedRunnableScheme;

        _shQuit?.Key = Application.GetDefaultKey (Command.Quit);

        _disableMouseCb!.Value = App!.Mouse.IsMouseDisabled ? CheckState.Checked : CheckState.UnChecked;
        _force16ColorsShortcutCb!.Value = Driver.Force16Colors ? CheckState.Checked : CheckState.UnChecked;

        App.TopRunnableView?.SetNeedsDraw ();
    }

    private void ConfigAppliedHandler (object? sender, ConfigurationManagerEventArgs? a) => ConfigApplied ();

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

        msg.AppendLine ("""
                         _______                  _             _   _____       _ 
                        |__   __|                (_)           | | / ____|     (_)
                           | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _ 
                           | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | |
                           | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | |
                           |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_|
                        """);
        msg.AppendLine ();
        msg.AppendLine ("v2 - Beta");
        msg.AppendLine ();
        msg.Append ("https://github.com/gui-cs/Terminal.Gui");

        return msg.ToString ();
    }

    /// <summary>
    ///     Shows a dialog displaying error logs from a scenario run.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="scenarioName">The name of the scenario that was run.</param>
    /// <param name="logs">The captured log output.</param>
    private static void ShowScenarioErrorsDialog (IApplication app, string scenarioName, string logs)
    {
        using Dialog dialog = new ();
        dialog.Title = $"Errors in {scenarioName}";

        ListView eventLog = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Source = new ListWrapper<string> (new ObservableCollection<string> (logs.Split ([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries))),
            SelectedItem = 0,
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Error)
        };
        eventLog.ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

        Button okButton = new () { Text = "OK" };

        dialog.Add (eventLog);
        dialog.AddButton (okButton);

        app.Run (dialog);
    }
}
