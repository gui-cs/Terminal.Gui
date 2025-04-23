#nullable enable

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Terminal.Gui;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Menu", "Illustrates non-Popover Menu and MenuItems")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Menus")]
[ScenarioCategory ("Shortcuts")]
public class Menu : Scenario
{
    public override void Main ()
    {
        Logging.Logger = CreateLogger ();

        Application.Init ();
        Toplevel app = new ();
        app.Title = GetQuitKeyAndName ();

        ObservableCollection<string> eventSource = new ();

        var eventLog = new ListView
        {
            Title = "Event Log",
            X = Pos.AnchorEnd (),
            Width = Dim.Auto (),
            Height = Dim.Fill (), // Make room for some wide things
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Source = new ListWrapper<string> (eventSource)
        };
        eventLog.Border!.Thickness = new (0, 1, 0, 0);

        MenuHost menuHostView = new ()
        {
            Id = "menuHostView",
            Title = $"Menu Host",

            X = 0,
            Y = 0,
            Width = Dim.Fill ()! - Dim.Width (eventLog),
            Height = Dim.Fill (),
        };
        app.Add (menuHostView);

        menuHostView.CommandNotBound += (o, args) =>
                                        {
                                            if (o is not View sender || args.Cancel)
                                            {
                                                return;
                                            }

                                            Logging.Debug ($"{sender.Id} CommandNotBound: {args?.Context?.Command}");
                                            eventSource.Add ($"{sender.Id} CommandNotBound: {args?.Context?.Command}");
                                            eventLog.MoveDown ();
                                        };

        menuHostView.Accepting += (o, args) =>
                                  {
                                      if (o is not View sender || args.Cancel)
                                      {
                                          return;
                                      }

                                      Logging.Debug ($"{sender.Id} Accepting: {args?.Context?.Source?.Title}");
                                      eventSource.Add ($"{sender.Id} Accepting: {args?.Context?.Source?.Title}: ");
                                      eventLog.MoveDown ();
                                  };

        if (menuHostView.Menu is { })
        {
            menuHostView.Menu.Accepted += (o, args) =>
                                          {
                                              if (o is not View sender || args.Cancel)
                                              {
                                                  return;
                                              }

                                              Logging.Debug ($"{sender.Id} Accepted: {args?.Context?.Source?.Text}");
                                              eventSource.Add ($"{sender.Id} Accepted: {args?.Context?.Source?.Text}: ");
                                              eventLog.MoveDown ();
                                          };
        }

        app.Add (eventLog);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    /// <summary>
    ///     A demo view class that contains a menu bar and a popover menu.
    /// </summary>
    public class MenuHost : View
    {
        internal Menuv2? Menu { get; private set; }

        public MenuHost ()
        {
            CanFocus = true;
            BorderStyle = LineStyle.Dashed;

            Label lastCommandLabel = new ()
            {
                Title = "_Last Command:",
                X = 0,
                Y = 0
            };

            View lastCommandText = new ()
            {
                X = Pos.Right (lastCommandLabel) + 1,
                Y = Pos.Top (lastCommandLabel),
                Height = Dim.Auto (),
                Width = Dim.Auto ()
            };

            Add (lastCommandLabel, lastCommandText);

            AddCommand (Command.New, HandleCommand);
            HotKeyBindings.Add (Key.F2, Command.New);

            AddCommand (Command.Open, HandleCommand);
            HotKeyBindings.Add (Key.F3, Command.Open);

            AddCommand (Command.Save, HandleCommand);
            HotKeyBindings.Add (Key.F4, Command.Save);

            AddCommand (Command.SaveAs, HandleCommand);
            HotKeyBindings.Add (Key.A.WithCtrl, Command.SaveAs);

            AddCommand (
                        Command.Quit,
                        ctx =>
                        {
                            Logging.Debug ("MenuHost Command.Quit - RequestStop");
                            Application.RequestStop ();

                            return true;
                        });

            AddCommand (Command.Cut, HandleCommand);
            HotKeyBindings.Add (Key.X.WithCtrl, Command.Cut);

            AddCommand (Command.Copy, HandleCommand);
            HotKeyBindings.Add (Key.C.WithCtrl, Command.Copy);

            AddCommand (Command.Paste, HandleCommand);
            HotKeyBindings.Add (Key.V.WithCtrl, Command.Paste);

            AddCommand (Command.SelectAll, HandleCommand);
            HotKeyBindings.Add (Key.T.WithCtrl, Command.SelectAll);

            MenuHost host = this;

            //var menuBar = new MenuBarv2
            //{
            //    Title = "MenuHost MenuBar"
            //};
            //menuBar.EnableForDesign (ref host);

            //base.Add (menuBar);

            Label lastAcceptedLabel = new ()
            {
                Title = "Last Accepted:",
                X = 0,
                Y = Pos.Bottom (lastCommandLabel)
            };

            View lastAcceptedText = new ()
            {
                X = Pos.Right (lastAcceptedLabel) + 1,
                Y = Pos.Top (lastAcceptedLabel),
                Height = Dim.Auto (),
                Width = Dim.Auto ()
            };

            Add (lastAcceptedLabel, lastAcceptedText);

            //// MenuItem: AutoSave - Demos simple CommandView state tracking
            //// In MenuBar.EnableForDesign, the auto save MenuItem does not specify a Command. But does
            //// set a Key (F10). MenuBar adds this key as a hotkey and thus if it's pressed, it toggles the MenuItem
            //// CB.
            //// So that is needed is to mirror the two check boxes.
            //var autoSaveMenuItemCb = menuBar.GetMenuItemsWithTitle ("_Auto Save").FirstOrDefault ()?.CommandView as CheckBox;
            //Debug.Assert (autoSaveMenuItemCb is { });

            //CheckBox autoSaveStatusCb = new ()
            //{
            //    Title = "AutoSave Status (MenuItem Binding to F10)",
            //    X = Pos.Left (lastAcceptedLabel),
            //    Y = Pos.Bottom (lastAcceptedLabel)
            //};

            //autoSaveStatusCb.CheckedStateChanged += (_, _) => { autoSaveMenuItemCb!.CheckedState = autoSaveStatusCb.CheckedState; };

            //if (autoSaveMenuItemCb is { })
            //{
            //    autoSaveMenuItemCb.CheckedStateChanged += (_, _) => { autoSaveStatusCb!.CheckedState = autoSaveMenuItemCb.CheckedState; };
            //}

            //base.Add (autoSaveStatusCb);

            //// MenuItem: Enable Overwrite - Demos View Key Binding
            //// In MenuBar.EnableForDesign, the overwrite MenuItem specifies a Command (Command.EnableOverwrite).
            //// Ctrl+W is bound to Command.EnableOverwrite by this View.
            //// Thus when Ctrl+W is pressed the MenuBar never sees it, but the command is invoked on this.
            //// If the user clicks on the MenuItem, Accept will be raised.
            //CheckBox enableOverwriteStatusCb = new ()
            //{
            //    Title = "Enable Overwrite (View Binding to Ctrl+W)",
            //    X = Pos.Left (autoSaveStatusCb),
            //    Y = Pos.Bottom (autoSaveStatusCb)
            //};

            //// The source of truth is our status CB; any time it changes, update the menu item
            //var enableOverwriteMenuItemCb = menuBar.GetMenuItemsWithTitle ("Overwrite").FirstOrDefault ()?.CommandView as CheckBox;
            //enableOverwriteStatusCb.CheckedStateChanged += (_, _) => enableOverwriteMenuItemCb!.CheckedState = enableOverwriteStatusCb.CheckedState;

            //menuBar.Accepted += (o, args) =>
            //                    {
            //                        if (args.Context?.Source is MenuItemv2 mi && mi.CommandView == enableOverwriteMenuItemCb)
            //                        {
            //                            Logging.Debug ($"menuBar.Accepted: {args.Context.Source?.Title}");

            //                            // Set Cancel to true to stop propagation of Accepting to superview
            //                            args.Cancel = true;

            //                            // Since overwrite uses a MenuItem.Command the menu item CB is the source of truth
            //                            enableOverwriteStatusCb.CheckedState = ((CheckBox)mi.CommandView).CheckedState;
            //                            lastAcceptedText.Text = args?.Context?.Source?.Title!;
            //                        }
            //                    };

            //HotKeyBindings.Add (Key.W.WithCtrl, Command.EnableOverwrite);

            //AddCommand (
            //            Command.EnableOverwrite,
            //            ctx =>
            //            {
            //                // The command was invoked. Toggle the status Cb.
            //                enableOverwriteStatusCb.AdvanceCheckState ();

            //                return HandleCommand (ctx);
            //            });
            //base.Add (enableOverwriteStatusCb);

            //// MenuItem: EditMode - Demos App Level Key Bindings
            //// In MenuBar.EnableForDesign, the edit mode MenuItem specifies a Command (Command.Edit).
            //// F5 is bound to Command.EnableOverwrite as an Applicatio-Level Key Binding
            //// Thus when F5 is pressed the MenuBar never sees it, but the command is invoked on this, via
            //// a Application.KeyBinding.
            //// If the user clicks on the MenuItem, Accept will be raised.
            //CheckBox editModeStatusCb = new ()
            //{
            //    Title = "EditMode (App Binding to F5)",
            //    X = Pos.Left (enableOverwriteStatusCb),
            //    Y = Pos.Bottom (enableOverwriteStatusCb)
            //};

            //// The source of truth is our status CB; any time it changes, update the menu item
            //CheckBox? editModeMenuItemCb = menuBar.GetMenuItemsWithTitle ("EditMode").FirstOrDefault ()?.CommandView as CheckBox;
            //editModeStatusCb.CheckedStateChanged += (_, _) =>
            //                                        {
            //                                            if (editModeMenuItemCb is { })
            //                                            {
            //                                                editModeMenuItemCb.CheckedState = editModeStatusCb.CheckedState;
            //                                            }
            //                                        };

            //menuBar.Accepted += (o, args) =>
            //                    {
            //                        if (args.Context?.Source is MenuItemv2 mi && mi.CommandView == editModeMenuItemCb)
            //                        {
            //                            Logging.Debug ($"menuBar.Accepted: {args.Context.Source?.Title}");

            //                            // Set Cancel to true to stop propagation of Accepting to superview
            //                            args.Cancel = true;

            //                            // Since overwrite uses a MenuItem.Command the menu item CB is the source of truth
            //                            editModeMenuItemCb.CheckedState = ((CheckBox)mi.CommandView).CheckedState;
            //                            lastAcceptedText.Text = args?.Context?.Source?.Title!;
            //                        }
            //                    };

            //AddCommand (
            //            Command.Edit,
            //            ctx =>
            //            {
            //                // The command was invoked. Toggle the status Cb.
            //                editModeStatusCb.AdvanceCheckState ();

            //                return HandleCommand (ctx);
            //            });

            //base.Add (editModeStatusCb);

            Menu = new ()
            {
                Title = "Menu",
                Id = "Menu",
                X = 0,
                Y = Pos.Bottom (lastAcceptedText) + 1
            };

            Menu.EnableForDesign (ref host);

            base.Add (Menu);

            AddCommand (Command.Select, ctx =>
                                        {
                                            if (RaiseSelecting (ctx) is true)
                                            {
                                                return true;
                                            }

                                            //if (CanFocus)
                                            //{
                                            //    SetFocus ();

                                            //    return true;
                                            //}

                                            return false;
                                        });

            Menu!.Selecting += (o, args) =>
                                    {
                                        if (o is not View sender || args.Cancel)
                                        {
                                            return;
                                        }
                                        Logging.Debug ($"Menu.Selecting: Sender: {sender.Title} - Source: {args.Context?.Source?.Title}");

                                        MenuItemv2? source = args.Context?.Source as MenuItemv2;

                                        if (source is { SubMenu: {} menu })
                                        {
                                            if (menu.Visible)
                                            {
                                                if (menu is { Visible: true })
                                                {
                                                    Logging.Debug ($"Menu.Selecting: {menu.Title} - Removing Menu");

                                                    menu.ClearFocus ();
                                                    base.Remove (menu);
                                                }
                                            }
                                            else
                                            {
                                                // Add the submenu and show it
                                                if (menu is { SuperView: null, Visible: false })
                                                {
                                                    Logging.Debug ($"Menu.Selecting: {menu.Title} - Adding Menu");

                                                    menu.ClearFocus ();
                                                    base.Add (menu);
                                                    Debug.Assert (menu.IsInitialized);
                                                }
                                            }
                                        }


                                       
                                    };

            Menu!.ShowingSubMenu += (o, args) =>
                                    {
                                        if (o is not Menuv2 menu || args.Handled)
                                        {
                                            return;
                                        }
                                        Logging.Debug ($"Menu.ActiveChanging: {menu.Title}");

                                        if (menu.Visible)
                                        {
                                            if (menu is { Visible: true })
                                            {
                                                Logging.Debug ($"Menu.ActiveChanging: {menu.Title} - Removing Menu");

                                                menu.ClearFocus ();
                                                base.Remove (menu);
                                            }
                                        }
                                        else
                                        {
                                            // Add the submenu and show it
                                            if (menu is { SuperView: null, Visible: false })
                                            {
                                                Logging.Debug ($"Menu.ActiveChanging: {menu.Title} - Adding Menu");

                                                menu.ClearFocus ();
                                                base.Add (menu);
                                                Debug.Assert (menu.IsInitialized);

                                                menu.Layout ();
                                            }
                                        }

                                    };

            Menu!.Accepted += (o, args) =>
                                     {
                                         Logging.Debug ($"Menu.Accepted: {args.Context?.Source?.Title}");

                                         // Forward the event to the MenuHost
                                         if (args.Context is { })
                                         {
                                             //InvokeCommand (args.Context.Command);
                                         }
                                     };


            ////var hideBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (openBtn), Text = "Toggle Menu._Visible" };
            ////hideBtn.Accepting += (s, e) => { menuBar.Visible = !menuBar.Visible; };
            ////appWindow.Add (hideBtn);

            ////var enableBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (hideBtn), Text = "_Toggle Menu.Enable" };
            ////enableBtn.Accepting += (s, e) => { menuBar.Enabled = !menuBar.Enabled; };
            ////appWindow.Add (enableBtn);

            //autoSaveStatusCb.SetFocus ();

            return;

            // Add the commands supported by this View
            bool? HandleCommand (ICommandContext? ctx)
            {
                lastCommandText.Text = ctx?.Command!.ToString ()!;

                Logging.Debug ($"lastCommand: {lastCommandText.Text}");

                return true;
            }
        }
    }

    private const string LOGFILE_LOCATION = "./logs";
    private static readonly string _logFilePath = string.Empty;
    private static readonly LoggingLevelSwitch _logLevelSwitch = new ();

    private static ILogger CreateLogger ()
    {
        // Configure Serilog to write logs to a file
        _logLevelSwitch.MinimumLevel = LogEventLevel.Verbose;

        Log.Logger = new LoggerConfiguration ()
                     .MinimumLevel.ControlledBy (_logLevelSwitch)
                     .Enrich.FromLogContext () // Enables dynamic enrichment
                     .WriteTo.Debug ()
                     .WriteTo.File (
                                    _logFilePath,
                                    rollingInterval: RollingInterval.Day,
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                     .CreateLogger ();

        // Create a logger factory compatible with Microsoft.Extensions.Logging
        using ILoggerFactory loggerFactory = LoggerFactory.Create (
                                                                   builder =>
                                                                   {
                                                                       builder
                                                                           .AddSerilog (dispose: true) // Integrate Serilog with ILogger
                                                                           .SetMinimumLevel (LogLevel.Trace); // Set minimum log level
                                                                   });

        // Get an ILogger instance
        return loggerFactory.CreateLogger ("Global Logger");
    }
}
