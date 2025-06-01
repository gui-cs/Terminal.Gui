#nullable enable

using System.Collections.ObjectModel;
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
            SchemeName = "TopLevel",
            Source = new ListWrapper<string> (eventSource)
        };
        eventLog.Border!.Thickness = new (0, 1, 0, 0);

        MenuHost menuHostView = new ()
        {
            Id = "menuHostView",
            Title = $"Menu Host - Use {PopoverMenu.DefaultKey} for Popover Menu",

            X = 0,
            Y = 0,
            Width = Dim.Fill ()! - Dim.Width (eventLog),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        app.Add (menuHostView);

        menuHostView.CommandNotBound += (o, args) =>
                                        {
                                            if (o is not View sender || args.Handled)
                                            {
                                                return;
                                            }

                                            Logging.Debug ($"{sender.Id} CommandNotBound: {args?.Context?.Command}");
                                            eventSource.Add ($"{sender.Id} CommandNotBound: {args?.Context?.Command}");
                                            eventLog.MoveDown ();
                                        };

        menuHostView.Accepting += (o, args) =>
                                  {
                                      if (o is not View sender || args.Handled)
                                      {
                                          return;
                                      }

                                      Logging.Debug ($"{sender.Id} Accepting: {args?.Context?.Source?.Title}");
                                      eventSource.Add ($"{sender.Id} Accepting: {args?.Context?.Source?.Title}: ");
                                      eventLog.MoveDown ();
                                  };

        menuHostView.ContextMenu!.Accepted += (o, args) =>
                                              {
                                                  if (o is not View sender || args.Handled)
                                                  {
                                                      return;
                                                  }

                                                  Logging.Debug ($"{sender.Id} Accepted: {args?.Context?.Source?.Text}");
                                                  eventSource.Add ($"{sender.Id} Accepted: {args?.Context?.Source?.Text}: ");
                                                  eventLog.MoveDown ();
                                              };

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
        internal PopoverMenu? ContextMenu { get; private set; }

        public MenuHost ()
        {
            CanFocus = true;
            BorderStyle = LineStyle.Dashed;

            AddCommand (
                        Command.Context,
                        ctx =>
                        {
                            ContextMenu?.MakeVisible ();

                            return true;
                        });

            MouseBindings.ReplaceCommands (MouseFlags.Button3Clicked, Command.Context);
            KeyBindings.Add (PopoverMenu.DefaultKey, Command.Context);

            AddCommand (
                        Command.Cancel,
                        ctx =>
                        {
                            if (Application.Popover?.GetActivePopover () as PopoverMenu is { Visible: true } visiblePopover)
                            {
                                visiblePopover.Visible = false;
                            }

                            return true;
                        });

            MouseBindings.ReplaceCommands (MouseFlags.Button1Clicked, Command.Cancel);

            Label lastCommandLabel = new ()
            {
                Title = "_Last Command:",
                X = 15,
                Y = 10
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
            HotKeyBindings.Add (Application.QuitKey, Command.Quit);

            AddCommand (Command.Cut, HandleCommand);
            HotKeyBindings.Add (Key.X.WithCtrl, Command.Cut);

            AddCommand (Command.Copy, HandleCommand);
            HotKeyBindings.Add (Key.C.WithCtrl, Command.Copy);

            AddCommand (Command.Paste, HandleCommand);
            HotKeyBindings.Add (Key.V.WithCtrl, Command.Paste);

            AddCommand (Command.SelectAll, HandleCommand);
            HotKeyBindings.Add (Key.T.WithCtrl, Command.SelectAll);

            // BUGBUG: This must come before we create the MenuBar or it will not work.
            // BUGBUG: This is due to TODO's in PopoverMenu where key bindings are not
            // BUGBUG: updated after the MenuBar is created.
            Application.KeyBindings.Remove (Key.F5);
            Application.KeyBindings.Add (Key.F5, this, Command.Edit);

            var menuBar = new MenuBarv2
            {
                Title = "MenuHost MenuBar"
            };
            MenuHost host = this;
            menuBar.EnableForDesign (ref host);

            base.Add (menuBar);

            Label lastAcceptedLabel = new ()
            {
                Title = "Last Accepted:",
                X = Pos.Left (lastCommandLabel),
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

            // MenuItem: AutoSave - Demos simple CommandView state tracking
            // In MenuBar.EnableForDesign, the auto save MenuItem does not specify a Command. But does
            // set a Key (F10). MenuBar adds this key as a hotkey and thus if it's pressed, it toggles the MenuItem
            // CB.
            // So that is needed is to mirror the two check boxes.
            var autoSaveMenuItemCb = menuBar.GetMenuItemsWithTitle ("_Auto Save").FirstOrDefault ()?.CommandView as CheckBox;
            Debug.Assert (autoSaveMenuItemCb is { });

            CheckBox autoSaveStatusCb = new ()
            {
                Title = "AutoSave Status (MenuItem Binding to F10)",
                X = Pos.Left (lastAcceptedLabel),
                Y = Pos.Bottom (lastAcceptedLabel)
            };

            autoSaveStatusCb.CheckedStateChanged += (_, _) => { autoSaveMenuItemCb!.CheckedState = autoSaveStatusCb.CheckedState; };

            if (autoSaveMenuItemCb is { })
            {
                autoSaveMenuItemCb.CheckedStateChanged += (_, _) => { autoSaveStatusCb!.CheckedState = autoSaveMenuItemCb.CheckedState; };
            }

            base.Add (autoSaveStatusCb);

            // MenuItem: Enable Overwrite - Demos View Key Binding
            // In MenuBar.EnableForDesign, the overwrite MenuItem specifies a Command (Command.EnableOverwrite).
            // Ctrl+W is bound to Command.EnableOverwrite by this View.
            // Thus when Ctrl+W is pressed the MenuBar never sees it, but the command is invoked on this.
            // If the user clicks on the MenuItem, Accept will be raised.
            CheckBox enableOverwriteStatusCb = new ()
            {
                Title = "Enable Overwrite (View Binding to Ctrl+W)",
                X = Pos.Left (autoSaveStatusCb),
                Y = Pos.Bottom (autoSaveStatusCb)
            };

            // The source of truth is our status CB; any time it changes, update the menu item
            var enableOverwriteMenuItemCb = menuBar.GetMenuItemsWithTitle ("Overwrite").FirstOrDefault ()?.CommandView as CheckBox;
            enableOverwriteStatusCb.CheckedStateChanged += (_, _) => enableOverwriteMenuItemCb!.CheckedState = enableOverwriteStatusCb.CheckedState;

            menuBar.Accepted += (o, args) =>
                                {
                                    if (args.Context?.Source is MenuItemv2 mi && mi.CommandView == enableOverwriteMenuItemCb)
                                    {
                                        Logging.Debug ($"menuBar.Accepted: {args.Context.Source?.Title}");

                                        // Set Cancel to true to stop propagation of Accepting to superview
                                        args.Handled = true;

                                        // Since overwrite uses a MenuItem.Command the menu item CB is the source of truth
                                        enableOverwriteStatusCb.CheckedState = ((CheckBox)mi.CommandView).CheckedState;
                                        lastAcceptedText.Text = args?.Context?.Source?.Title!;
                                    }
                                };

            HotKeyBindings.Add (Key.W.WithCtrl, Command.EnableOverwrite);

            AddCommand (
                        Command.EnableOverwrite,
                        ctx =>
                        {
                            // The command was invoked. Toggle the status Cb.
                            enableOverwriteStatusCb.AdvanceCheckState ();

                            return HandleCommand (ctx);
                        });
            base.Add (enableOverwriteStatusCb);

            // MenuItem: EditMode - Demos App Level Key Bindings
            // In MenuBar.EnableForDesign, the edit mode MenuItem specifies a Command (Command.Edit).
            // F5 is bound to Command.EnableOverwrite as an Applicatio-Level Key Binding
            // Thus when F5 is pressed the MenuBar never sees it, but the command is invoked on this, via
            // a Application.KeyBinding.
            // If the user clicks on the MenuItem, Accept will be raised.
            CheckBox editModeStatusCb = new ()
            {
                Title = "EditMode (App Binding to F5)",
                X = Pos.Left (enableOverwriteStatusCb),
                Y = Pos.Bottom (enableOverwriteStatusCb)
            };

            // The source of truth is our status CB; any time it changes, update the menu item
            var editModeMenuItemCb = menuBar.GetMenuItemsWithTitle ("EditMode").FirstOrDefault ()?.CommandView as CheckBox;
            editModeStatusCb.CheckedStateChanged += (_, _) => editModeMenuItemCb!.CheckedState = editModeStatusCb.CheckedState;

            menuBar.Accepted += (o, args) =>
                                {
                                    if (args.Context?.Source is MenuItemv2 mi && mi.CommandView == editModeMenuItemCb)
                                    {
                                        Logging.Debug ($"menuBar.Accepted: {args.Context.Source?.Title}");

                                        // Set Cancel to true to stop propagation of Accepting to superview
                                        args.Handled = true;

                                        // Since overwrite uses a MenuItem.Command the menu item CB is the source of truth
                                        editModeMenuItemCb.CheckedState = ((CheckBox)mi.CommandView).CheckedState;
                                        lastAcceptedText.Text = args?.Context?.Source?.Title!;
                                    }
                                };

            AddCommand (
                        Command.Edit,
                        ctx =>
                        {
                            // The command was invoked. Toggle the status Cb.
                            editModeStatusCb.AdvanceCheckState ();

                            return HandleCommand (ctx);
                        });

            base.Add (editModeStatusCb);

            // Set up the Context Menu
            ContextMenu = new ()
            {
                Title = "ContextMenu",
                Id = "ContextMenu"
            };

            ContextMenu.EnableForDesign (ref host);
            ContextMenu.Visible = false;

            // Demo of PopoverMenu as a context menu
            // If we want Commands from the ContextMenu to be handled by the MenuHost
            // we need to subscribe to the ContextMenu's Accepted event.
            ContextMenu!.Accepted += (o, args) =>
                                     {
                                         Logging.Debug ($"ContextMenu.Accepted: {args.Context?.Source?.Title}");

                                         // Forward the event to the MenuHost
                                         if (args.Context is { })
                                         {
                                             //InvokeCommand (args.Context.Command);
                                         }
                                     };

            ContextMenu!.VisibleChanged += (sender, args) =>
                                           {
                                               if (ContextMenu!.Visible)
                                               { }
                                           };

            // Add a button to open the contextmenu
            var openBtn = new Button { X = Pos.Center (), Y = 4, Text = "_Open Menu", IsDefault = true };

            openBtn.Accepting += (s, e) =>
                                 {
                                     e.Handled = true;
                                     Logging.Trace ($"openBtn.Accepting - Sending F9. {e.Context?.Source?.Title}");
                                     NewKeyDownEvent (menuBar.Key);
                                 };

            Add (openBtn);

            //var hideBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (openBtn), Text = "Toggle Menu._Visible" };
            //hideBtn.Accepting += (s, e) => { menuBar.Visible = !menuBar.Visible; };
            //appWindow.Add (hideBtn);

            //var enableBtn = new Button { X = Pos.Center (), Y = Pos.Bottom (hideBtn), Text = "_Toggle Menu.Enable" };
            //enableBtn.Accepting += (s, e) => { menuBar.Enabled = !menuBar.Enabled; };
            //appWindow.Add (enableBtn);

            autoSaveStatusCb.SetFocus ();

            return;

            // Add the commands supported by this View
            bool? HandleCommand (ICommandContext? ctx)
            {
                lastCommandText.Text = ctx?.Command!.ToString ()!;

                Logging.Debug ($"lastCommand: {lastCommandText.Text}");

                return true;
            }
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
