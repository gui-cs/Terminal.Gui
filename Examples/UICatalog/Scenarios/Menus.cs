#nullable enable

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Menus", "Illustrates MenuBar, Menu, and MenuItem")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
[ScenarioCategory ("Shortcuts")]
public class Menus : Scenario
{
    private EventLog? _eventLog;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        Logging.Logger = CreateLogger ();

        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable runnable = new ();
        runnable.Title = GetQuitKeyAndName ();

        _eventLog = new EventLog
        {
            Id = "eventLog",
            X = Pos.AnchorEnd (),
            Height = Dim.Fill (),
            SchemeName = "Runnable",
            BorderStyle = LineStyle.Double,
            Title = "E_vents",
            Arrangement = ViewArrangement.LeftResizable
        };

        MenuHost menuHostView = new ()
        {
            Id = "menuHostView",
            Title = $"Menu Host - Use {PopoverMenu.DefaultKey} for Popover Menu",
            X = 0,
            Y = 0,
            Width = Dim.Fill ()! - Dim.Width (_eventLog),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        runnable.Add (menuHostView);

        _eventLog.SetViewToLog (runnable);
        _eventLog.SetViewToLog (menuHostView);

        runnable.Initialized += (_, _) =>
                                {
                                    _eventLog.SetViewToLog (menuHostView.MenuBar);

                                    foreach (MenuItem menuItem in menuHostView?.MenuBar?.GetMenuItemsWith (v => true) ?? [])
                                    {
                                        _eventLog.SetViewToLog (menuItem);
                                        _eventLog.SetViewToLog (menuItem.CommandView);
                                        menuItem.Action += () => _eventLog.Log ($"{menuItem.ToIdentifyingString ()} Action!");
                                    }

                                    foreach (Menu menu in menuHostView?.SubViews.OfType<Menu> ().Where (m => m.Id == "TestMenu")!)
                                    {
                                        _eventLog.SetViewToLog (menu);

                                        foreach (MenuItem mi in menu.SubViews.OfType<MenuItem> ())
                                        {
                                            _eventLog.SetViewToLog (mi);
                                            _eventLog.SetViewToLog (mi.CommandView);
                                        }
                                    }

                                    if (menuHostView?.ContextMenu is { })
                                    {
                                        foreach (MenuItem menuItem in menuHostView?.ContextMenu?.GetMenuItemsOfAllSubMenus (v => true) ?? [])
                                        {
                                            _eventLog.SetViewToLog (menuItem);
                                            _eventLog.SetViewToLog (menuItem.CommandView);
                                            menuItem.Action += () => _eventLog.Log ($"{menuItem.ToIdentifyingString ()} Action!");
                                        }
                                        _eventLog.SetViewToLog (menuHostView?.ContextMenu);
                                    }
                                };

        runnable.Add (_eventLog);

        app.Run (runnable);
    }

    /// <summary>
    ///     A demo view class that contains a menu bar and a popover menu.
    /// </summary>
    public class MenuHost : View
    {
        internal PopoverMenu? ContextMenu { get; private set; }

        internal MenuBar? MenuBar { get; private set; }

        public MenuHost ()
        {
            CanFocus = true;
            BorderStyle = LineStyle.Dashed;
        }

        /// <inheritdoc/>
        public override void EndInit ()
        {
            base.EndInit ();

            AddCommand (Command.Context,
                        _ =>
                        {
                            ContextMenu?.MakeVisible ();

                            return true;
                        });

            MouseBindings.ReplaceCommands (MouseFlags.RightButtonClicked, Command.Context);
            KeyBindings.Add (PopoverMenu.DefaultKey, Command.Context);

            Label lastCommandLabel = new () { Title = "_Last Command:", X = 15, Y = 10 };

            View lastCommandText = new () { X = Pos.Right (lastCommandLabel) + 1, Y = Pos.Top (lastCommandLabel), Height = Dim.Auto (), Width = Dim.Auto () };

            Add (lastCommandLabel, lastCommandText);

            AddCommand (Command.Quit,
                        _ =>
                        {
                            // Logging.Debug ("MenuHost Command.Quit - RequestStop");
                            App?.RequestStop ();

                            return true;
                        });
            HotKeyBindings.Add (Application.QuitKey, Command.Quit);

            // BUGBUG: This must come before we create the MenuBar or it will not work.
            // BUGBUG: This is due to TODO's in PopoverMenu where key bindings are not
            // BUGBUG: updated after the MenuBar is created.
            App?.Keyboard.KeyBindings.Remove (Key.F5);
            App?.Keyboard.KeyBindings.AddApp (Key.F5, this, Command.Edit);

            MenuBar = new MenuBar { Title = "MenuHost MenuBar" };
            MenuBar.CommandsToBubbleUp = [Command.Accept, Command.Activate, Command.HotKey];
            MenuHost host = this;
            MenuBar?.EnableForDesign (ref host);
            Add (MenuBar);

            Label lastAcceptedLabel = new () { Title = "Last Accepted:", X = Pos.Left (lastCommandLabel), Y = Pos.Bottom (lastCommandLabel) };

            View lastAcceptedText = new ()
            {
                X = Pos.Right (lastAcceptedLabel) + 1, Y = Pos.Top (lastAcceptedLabel), Height = Dim.Auto (), Width = Dim.Auto ()
            };

            Add (lastAcceptedLabel, lastAcceptedText);

            // MenuItem: AutoSave - Demos simple CommandView state tracking
            // In MenuBar.EnableForDesign, the auto save MenuItem does not specify a Command. But does
            // set a Key (F10). MenuBar adds this key as a hotkey and thus if it's pressed, it toggles the MenuItem
            // CB.
            // So that is needed is to mirror the two check boxes.
            var autoSaveMenuItemCb = MenuBar?.GetMenuItemsWith (mi => mi.Id == "AutoSave").FirstOrDefault ()?.CommandView as CheckBox;
            Debug.Assert (autoSaveMenuItemCb is { });

            CheckBox autoSaveStatusCb = new ()
            {
                Title = "AutoSave Status (MenuItem Binding to F10)", X = Pos.Left (lastAcceptedLabel), Y = Pos.Bottom (lastAcceptedLabel)
            };

            autoSaveStatusCb.ValueChanged += (_, _) => { autoSaveMenuItemCb.Value = autoSaveStatusCb.Value; };
            autoSaveMenuItemCb.ValueChanged += (_, _) => { autoSaveStatusCb.Value = autoSaveMenuItemCb.Value; };

            Add (autoSaveStatusCb);

            // MenuItem: Enable Overwrite - Demos View Key Binding
            // In MenuBar.EnableForDesign, to overwrite MenuItem specifies a Command (Command.EnableOverwrite).
            // Ctrl+W is bound to Command.EnableOverwrite by this View.
            // Thus, when Ctrl+W is pressed the MenuBar never sees it, but the command is invoked on this.
            // If the user clicks on the MenuItem, Accept will be raised.
            CheckBox enableOverwriteStatusCb = new ()
            {
                Title = "Enable Overwrite (View Binding to Ctrl+W)", X = Pos.Left (autoSaveStatusCb), Y = Pos.Bottom (autoSaveStatusCb)
            };

            // The source of truth is our status CB; any time it changes, update the menu item
            var enableOverwriteMenuItemCb = MenuBar?.GetMenuItemsWith (mi => mi.Id == "Overwrite").FirstOrDefault ()?.CommandView as CheckBox;

            enableOverwriteStatusCb.ValueChanged += (_, _) => { enableOverwriteMenuItemCb?.Value = enableOverwriteStatusCb.Value; };

            MenuBar?.Accepted += (_, args) =>
                                {
                                    if (args.Context?.Source?.TryGetTarget (out View? sourceView) != true || sourceView is not MenuItem mi)
                                    {
                                        lastCommandText.Text = args.Context?.Command!.ToString ()!;
                                    }
                                };

            HotKeyBindings.Add (Key.W.WithCtrl, Command.EnableOverwrite);

            AddCommand (Command.EnableOverwrite,
                        ctx =>
                        {
                            // The command was invoked. Toggle the status Cb.
                            enableOverwriteStatusCb.AdvanceCheckState ();

                            return true;
                        });
            Add (enableOverwriteStatusCb);

            // MenuItem: EditMode - Demos App Level Key Bindings
            // In MenuBar.EnableForDesign, the edit mode MenuItem specifies a Command (Command.Edit).
            // F5 is bound to Command.EnableOverwrite as an Application-Level Key Binding
            // Thus when F5 is pressed the MenuBar never sees it, but the command is invoked on this, via
            // Application.KeyBinding.
            // If the user clicks on the MenuItem, Accept will be raised.
            CheckBox editModeStatusCb = new ()
            {
                Title = "EditMode (App Binding to F5)", X = Pos.Left (enableOverwriteStatusCb), Y = Pos.Bottom (enableOverwriteStatusCb)
            };

            // The source of truth is our status CB; any time it changes, update the menu item
            var editModeMenuItemCb = MenuBar?.GetMenuItemsWith (mi => mi.Id == "EditMode").FirstOrDefault ()?.CommandView as CheckBox;

            editModeStatusCb.ValueChanged += (_, _) => { editModeMenuItemCb?.Value = editModeStatusCb.Value; };

            MenuBar.Accepted += (_, args) =>
                                {
                                    if (args.Context?.Source?.TryGetTarget (out View? sourceView) != true || sourceView is not MenuItem mi)
                                    {
                                        return;
                                    }

                                    lastAcceptedText.Text = sourceView.Title!;
                                };

            AddCommand (Command.Edit,
                        ctx =>
                        {
                            // The command was invoked. Toggle the status Cb.
                            editModeStatusCb.AdvanceCheckState ();

                            return true;
                        });

            Add (editModeStatusCb);

            OptionSelector<Schemes>? schemeOptionSelector =
                MenuBar.GetMenuItemsWith (mi => mi.Id == "mutuallyExclusiveOptions").FirstOrDefault ()?.CommandView as OptionSelector<Schemes>;

            schemeOptionSelector!.ValueChanged += (_, args) =>
                                                  {
                                                      if (args.Value is { } scheme)
                                                      {
                                                          MenuBar.SchemeName = scheme.ToString ();
                                                      }
                                                  };

            // Set up the Context Menu
            ContextMenu = new PopoverMenu { Title = "ContextMenu", Id = "ContextMenu" };
            Menu testContextMenu = new () { Id = "TestContextMenu" };
            ContextMenu.Root = testContextMenu;
            ConfigureTestMenu (testContextMenu);
            ContextMenu?.Visible = false;

            // Demo of PopoverMenu as a context menu
            // If we want Commands from the ContextMenu to be handled by the MenuHost
            // we need to subscribe to the ContextMenu's Accepted event.
            ContextMenu!.Accepted += (_, args) =>
                                     {
                                         string sourceTitle = args.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Title : "null";

                                         // Logging.Debug ($"ContextMenu.Accepted: {sourceTitle}");

                                         // Forward the event to the MenuHost
                                         if (args.Context is { })
                                         {
                                             //InvokeCommand (args.Context.Command);
                                         }
                                     };

            // Add a button to open the contextmenu
            var openBtn = new Button { X = Pos.Center (), Y = 4, Text = "_Open Menu", IsDefault = true };

            openBtn.Accepting += (_, e) =>
                                 {
                                     e.Handled = true;
                                     string sourceTitle = e.Context?.Source?.TryGetTarget (out View? sourceView) == true ? sourceView.Title : "null";
                                     Logging.Trace ($"openBtn.Accepting - Sending F9. {sourceTitle}");
                                     NewKeyDownEvent (MenuBar.Key);
                                 };

            Add (openBtn);

            //var hideBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (openBtn), Text = "Toggle Menu._Visible" };
            //hideBtn.Accepting += (s, e) => { menuBar.Visible = !menuBar.Visible; };
            //appWindow.Add (hideBtn);

            //var enableBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (hideBtn), Text = "_Toggle Menu.Enable" };
            //enableBtn.Accepting += (s, e) => { menuBar.Enabled = !menuBar.Enabled; };
            //appWindow.Add (enableBtn);

            autoSaveStatusCb.SetFocus ();
            App?.Popovers?.Register (ContextMenu);

            Menu testMenu = new () { Y = Pos.Bottom (editModeStatusCb) + 1, Id = "TestMenu" };
            ConfigureTestMenu (testMenu);
            Add (testMenu);
        }

        private void ConfigureTestMenu (Menu menu)
        {
            MenuItem menuItem1 = new () { Title = "Z_igzag", Key = Key.I.WithCtrl, Text = "Gonna zig zag" };

            Line line = new ();

            MenuItem menuItemBorders = new () { Title = "_Borders", Text = "Borders", Key = Key.D4.WithAlt };
            menuItemBorders.CommandView = new CheckBox { Title = menuItemBorders.Title, CanFocus = false };

            menuItemBorders.Action += () =>
                                      {
                                          if (menuItemBorders.CommandView is CheckBox cb)
                                          {
                                              menu.BorderStyle = cb.Value == CheckState.Checked ? LineStyle.Double : LineStyle.None;
                                          }
                                      };

            // This ensures the checkbox state toggles when the hotkey of Title is pressed.
            menuItemBorders.Accepting += (_, args) => args.Handled = true;

            OptionSelector<Schemes>? schemeOptionSelector = new () { Title = "Scheme", CanFocus = false };

            MenuItem menuItemScheme = new ()
            {
                Title = "Scheme",
                Text = "Scheme",
                Key = Key.S.WithCtrl,
                CommandView = schemeOptionSelector
            };

            schemeOptionSelector!.ValueChanged += (_, args) =>
                                                  {
                                                      if (args.Value is { } scheme)
                                                      {
                                                          menu.SchemeName = scheme.ToString ();
                                                      }
                                                  };

            menu.Add (menuItem1, line, menuItemBorders, menuItemScheme);
        }

        /// <inheritdoc/>
        protected override void Dispose (bool disposing)
        {
            if (ContextMenu is { })
            {
                ContextMenu.Dispose ();
                ContextMenu = null;
            }

            base.Dispose (disposing);
        }
    }

    private const string LOGFILE_LOCATION = "./logs";
    private static readonly string _logFilePath = string.Empty;
    private static readonly LoggingLevelSwitch _logLevelSwitch = new ();

    private static ILogger CreateLogger ()
    {
        // Configure Serilog to write logs to a file
        _logLevelSwitch.MinimumLevel = LogEventLevel.Verbose;

        Log.Logger = new LoggerConfiguration ().MinimumLevel.ControlledBy (_logLevelSwitch)
                                               .Enrich.FromLogContext () // Enables dynamic enrichment
                                               .WriteTo.Debug ()
                                               .WriteTo.File (_logFilePath,
                                                              rollingInterval: RollingInterval.Day,
                                                              outputTemplate:
                                                              "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                                               .CreateLogger ();

        // Create a logger factory compatible with Microsoft.Extensions.Logging
        using ILoggerFactory loggerFactory = LoggerFactory.Create (builder =>
                                                                   {
                                                                       builder.AddSerilog (dispose: true) // Integrate Serilog with ILogger
                                                                              .SetMinimumLevel (LogLevel.Trace); // Set minimum log level
                                                                   });

        // Get an ILogger instance
        return loggerFactory.CreateLogger ("Global Logger");
    }
}
