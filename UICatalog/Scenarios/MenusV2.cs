#nullable enable

using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Terminal.Gui;
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
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
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
                                          if (o is not View sender || args.Cancel)
                                          {
                                              return;
                                          }

                                          Logging.Trace ($"{sender.Id} CommandNotBound: {args?.Context?.Command}");
                                          eventSource.Add ($"{sender.Id} CommandNotBound: {args?.Context?.Command}");
                                          eventLog.MoveDown ();
                                      };

        menuHostView.Accepting += (o, args) =>
                                {
                                    if (o is not View sender || args.Cancel)
                                    {
                                        return;
                                    }

                                    Logging.Trace ($"{sender.Id} Accepting: {args?.Context?.Source?.Title}");
                                    eventSource.Add ($"{sender.Id} Accepting: {args?.Context?.Source?.Title}: ");
                                    eventLog.MoveDown ();
                                };

        menuHostView.DemoPopoverMenu!.Accepted += (o, args) =>
                                                {
                                                    if (o is not View sender || args.Cancel)
                                                    {
                                                        return;
                                                    }

                                                    Logging.Trace ($"{sender.Id} Accepted: {args?.Context?.Source?.Text}");
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
        internal PopoverMenu? DemoPopoverMenu { get; }

        private CheckBox? _enableOverwriteCb;
        private CheckBox? _autoSaveCb;
        private CheckBox? _editModeCb;

        private RadioGroup? _mutuallyExclusiveOptionsRg;

        private ColorPicker? _menuBgColorCp;

        public MenuHost ()
        {
            CanFocus = true;
            BorderStyle = LineStyle.Dashed;

            AddCommand (
                        Command.Context,
                        ctx =>
                        {
                            DemoPopoverMenu?.MakeVisible ();

                            return true;
                        });

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
                Y = 10,
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

            HotKeyBindings.Add (Key.W.WithCtrl, Command.EnableOverwrite);

            AddCommand (Command.Cut, HandleCommand);
            HotKeyBindings.Add (Key.X.WithCtrl, Command.Cut);

            AddCommand (Command.Copy, HandleCommand);
            HotKeyBindings.Add (Key.C.WithCtrl, Command.Copy);

            AddCommand (Command.Paste, HandleCommand);
            HotKeyBindings.Add (Key.V.WithCtrl, Command.Paste);

            AddCommand (Command.SelectAll, HandleCommand);
            HotKeyBindings.Add (Key.T.WithCtrl, Command.SelectAll);

            DemoPopoverMenu = new ()
            {
                Id = "FilePopoverMenu"
            };
            DemoPopoverMenu.EnableForDesign ();
            DemoPopoverMenu.Visible = false;

            MenuBarv2 menuBar = new MenuBarv2 ();
            menuBar.EnableForDesign ();

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

            CheckBox autoSaveStatusCb = new ()
            {
                Title = "AutoSave",
                X = Pos.Left (lastAcceptedLabel),
                Y = Pos.Bottom (lastAcceptedLabel)
            };

            autoSaveStatusCb.CheckedStateChanged += (sender, args) =>
                                                    {
                                                        if (menuBar.GetMenuItemsWithId ("AutoSave").FirstOrDefault ()?.CommandView is CheckBox checkBox)
                                                        {
                                                            checkBox.CheckedState = autoSaveStatusCb.CheckedState;
                                                        }
                                                    };

            Add (autoSaveStatusCb);

            Accepting += (o, args) =>
                                         {
                                             lastAcceptedText.Text = args?.Context?.Source?.Title!;

                                             if (args?.Context?.Source is MenuItemv2 mi)
                                             {
                                                 if (mi.CommandView == menuBar.GetMenuItemsWithId ("AutoSave").FirstOrDefault ()?.CommandView)
                                                 {
                                                     autoSaveStatusCb.CheckedState = ((CheckBox)mi.CommandView).CheckedState;
                                                     // Set Cancel to true to stop propagation of Accepting to superview
                                                     // args.Cancel = true;
                                                 }
                                             }
                                         };

            CheckBox enableOverwriteStatusCb = new ()
            {
                Title = "Enable Overwrite",
                X = Pos.Left (autoSaveStatusCb),
                Y = Pos.Bottom (autoSaveStatusCb)
            };
            enableOverwriteStatusCb.CheckedStateChanged += (sender, args) => { _enableOverwriteCb!.CheckedState = enableOverwriteStatusCb.CheckedState; };
            base.Add (enableOverwriteStatusCb);

            AddCommand (
                        Command.EnableOverwrite,
                        ctx =>
                        {
                            enableOverwriteStatusCb.CheckedState =
                                enableOverwriteStatusCb.CheckedState == CheckState.UnChecked ? CheckState.Checked : CheckState.UnChecked;

                            return HandleCommand (ctx);
                        });

            CheckBox editModeStatusCb = new ()
            {
                Title = "EditMode (App binding)",
                X = Pos.Left (enableOverwriteStatusCb),
                Y = Pos.Bottom (enableOverwriteStatusCb)
            };
            editModeStatusCb.CheckedStateChanged += (sender, args) => { _editModeCb!.CheckedState = editModeStatusCb.CheckedState; };
            base.Add (editModeStatusCb);

            AddCommand (Command.Edit, ctx =>
                                      {
                                          editModeStatusCb.CheckedState =
                                              editModeStatusCb.CheckedState == CheckState.UnChecked ? CheckState.Checked : CheckState.UnChecked;

                                          return HandleCommand (ctx);
                                      });

            Application.KeyBindings.Add (Key.F9, this, Command.Edit);


            DemoPopoverMenu!.Accepted += (o, args) =>
                                         {
                                             lastAcceptedText.Text = args?.Context?.Source?.Title!;

                                             if (args?.Context?.Source is MenuItemv2 mi && mi.CommandView == _autoSaveCb)
                                             {
                                                 autoSaveStatusCb.CheckedState = _autoSaveCb.CheckedState;
                                             }
                                         };

            DemoPopoverMenu!.VisibleChanged += (sender, args) =>
                                               {
                                                   if (DemoPopoverMenu!.Visible)
                                                   {
                                                       lastCommandText.Text = string.Empty;
                                                   }
                                               };

            Add (
                 new Button
                 {
                     Title = "_Button",
                     X = Pos.Center (),
                     Y = Pos.Center ()
                 });

            autoSaveStatusCb.SetFocus ();

            return;

            // Add the commands supported by this View
            bool? HandleCommand (ICommandContext? ctx)
            {
                lastCommandText.Text = ctx?.Command!.ToString ()!;

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
