#nullable enable

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Terminal.Gui;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MenusV2", "Illustrates MenuV2")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Shortcuts")]
public class MenusV2 : Scenario
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

        TargetView targetView = new ()
        {
            Id = "targetView",
            Title = "Target View",

            X = 5,
            Y = 5,
            Width = Dim.Fill (2)! - Dim.Width (eventLog),
            Height = Dim.Fill (2),
            BorderStyle = LineStyle.Dotted
        };
        app.Add (targetView);

        targetView.CommandNotBound += (o, args) =>
                                      {
                                          if (args.Cancel)
                                          {
                                              return;
                                          }

                                          Logging.Trace ($"targetView CommandNotBound: {args?.Context?.Command}");
                                          eventSource.Add ($"targetView CommandNotBound: {args?.Context?.Command}");
                                          eventLog.MoveDown ();
                                      };

        targetView.Accepting += (o, args) =>
                                {
                                    if (args.Cancel)
                                    {
                                        return;
                                    }

                                    Logging.Trace ($"targetView Accepting: {args?.Context?.Source?.Title}");
                                    eventSource.Add ($"targetView Accepting: {args?.Context?.Source?.Title}: ");
                                    eventLog.MoveDown ();
                                };

        targetView.FilePopoverMenu!.Accepted += (o, args) =>
                                                {
                                                    if (args.Cancel)
                                                    {
                                                        return;
                                                    }

                                                    Logging.Trace ($"FilePopoverMenu Accepted: {args?.Context?.Source?.Text}");
                                                    eventSource.Add ($"FilePopoverMenu Accepted: {args?.Context?.Source?.Text}: ");
                                                    eventLog.MoveDown ();
                                                };

        app.Add (eventLog);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    public class TargetView : View
    {
        internal PopoverMenu? FilePopoverMenu { get; }

        private CheckBox? _enableOverwriteCb;
        private CheckBox? _autoSaveCb;
        private CheckBox? _editModeCb;

        private RadioGroup? _mutuallyExclusiveOptionsRg;

        private ColorPicker? _menuBgColorCp;

        public TargetView ()
        {
            CanFocus = true;
            Text = "TargetView";
            BorderStyle = LineStyle.Dashed;

            AddCommand (
                        Command.Context,
                        ctx =>
                        {
                            FilePopoverMenu?.MakeVisible ();

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

            var fileMenu = new Menuv2
            {
                Id = "fileMenu"
            };
            ConfigureFileMenu (fileMenu);

            var optionsSubMenu = new Menuv2
            {
                Id = "optionsSubMenu",
                Visible = false
            };
            ConfigureOptionsSubMenu (optionsSubMenu);

            var optionsSubMenuItem = new MenuItemv2 (this, Command.NotBound, "O_ptions", "File options", optionsSubMenu);
            fileMenu.Add (optionsSubMenuItem);

            var detailsSubMenu = new Menuv2
            {
                Id = "detailsSubMenu",
                Visible = false
            };
            ConfigureDetialsSubMenu (detailsSubMenu);

            var detailsSubMenuItem = new MenuItemv2 (this, Command.NotBound, "_Details", "File details", detailsSubMenu);
            fileMenu.Add (detailsSubMenuItem);

            var moreDetailsSubMenu = new Menuv2
            {
                Id = "moreDetailsSubMenu",
                Visible = false
            };
            ConfigureMoreDetailsSubMenu (moreDetailsSubMenu);

            var moreDetailsSubMenuItem = new MenuItemv2 (this, Command.NotBound, "_More Details", "More details", moreDetailsSubMenu);
            detailsSubMenu.Add (moreDetailsSubMenuItem);

            FilePopoverMenu = new (fileMenu)
            {
                Id = "FilePopoverMenu"
            };

            MenuBarItemv2 fileMenuRootItem = new ("_File", FilePopoverMenu);

            AddCommand (Command.Cut, HandleCommand);
            HotKeyBindings.Add (Key.X.WithCtrl, Command.Cut);

            AddCommand (Command.Copy, HandleCommand);
            HotKeyBindings.Add (Key.C.WithCtrl, Command.Copy);

            AddCommand (Command.Paste, HandleCommand);
            HotKeyBindings.Add (Key.V.WithCtrl, Command.Paste);

            AddCommand (Command.SelectAll, HandleCommand);
            HotKeyBindings.Add (Key.T.WithCtrl, Command.SelectAll);

            Add (new MenuBarv2 (
                                [
                                    fileMenuRootItem,
                                    new MenuBarItemv2 (
                                                       "_Edit",
                                                       [
                                                           new MenuItemv2 (this, Command.Cut),
                                                           new MenuItemv2 (this, Command.Copy),
                                                           new MenuItemv2 (this, Command.Paste),
                                                           new Line (),
                                                           new MenuItemv2 (this, Command.SelectAll)
                                                       ]
                                                      ),
                                    new MenuBarItemv2 (this, Command.NotBound, "_Help")
                                    {
                                        Key = Key.F1,
                                        Action = () => { MessageBox.Query ("Help", "This is the help...", "_Ok"); }
                                    }
                                ]
                               )
                 );

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

            autoSaveStatusCb.CheckedStateChanged += (sender, args) => { _autoSaveCb!.CheckedState = autoSaveStatusCb.CheckedState; };

            Add (autoSaveStatusCb);

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


            FilePopoverMenu!.Accepted += (o, args) =>
                                         {
                                             lastAcceptedText.Text = args?.Context?.Source?.Title!;

                                             if (args?.Context?.Source is MenuItemv2 mi && mi.CommandView == _autoSaveCb)
                                             {
                                                 autoSaveStatusCb.CheckedState = _autoSaveCb.CheckedState;
                                             }
                                         };

            FilePopoverMenu!.VisibleChanged += (sender, args) =>
                                               {
                                                   if (FilePopoverMenu!.Visible)
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

        private void ConfigureFileMenu (Menuv2 menu)
        {
            var newFile = new MenuItemv2
            {
                Command = Command.New,
                TargetView = this
            };

            var openFile = new MenuItemv2
            {
                Command = Command.Open,
                TargetView = this
            };

            var saveFile = new MenuItemv2
            {
                Command = Command.Save,
                TargetView = this
            };

            var saveFileAs = new MenuItemv2 (this, Command.SaveAs);

            menu.Add (newFile, openFile, saveFile, saveFileAs, new Line ());
        }

        private void ConfigureOptionsSubMenu (Menuv2 menu)
        {
            // This is an example of a menu item with a checkbox that is NOT
            // bound to a Command. The PopoverMenu will raise Accepted when Alt-U is pressed.
            // The checkbox state will automatically toggle each time Alt-U is pressed beacuse
            // the MenuItem actaully gets the key events.
            var autoSave = new MenuItemv2
            {
                Title = "_Auto Save",
                Text = "(no Command)",
                Key = Key.F10
            };

            autoSave.CommandView = _autoSaveCb = new ()
            {
                Title = autoSave.Title,
                HighlightStyle = HighlightStyle.None,
                CanFocus = false
            };

            // This is an example of a MenuItem with a checkbox that is bound to a command.
            // When the key bound to Command.EntableOverwrite is pressed, InvokeCommand will invoke it 
            // on targetview, and thus the MenuItem will never see the key event. 
            // Because of this, the check box will not automatically track the state.
            var enableOverwrite = new MenuItemv2
            {
                Title = "Enable _Overwrite",
                Text = "Overwrite",
                Command = Command.EnableOverwrite,
                TargetView = this
            };

            enableOverwrite.CommandView = _enableOverwriteCb = new ()
            {
                Title = enableOverwrite.Title,
                HighlightStyle = HighlightStyle.None,
                CanFocus = false
            };

            _enableOverwriteCb.Accepting += (sender, args) => args.Cancel = true;

            var mutuallyExclusiveOptions = new MenuItemv2
            {
                HelpText = "3 Mutually Exclusive Options",
                Key = Key.F7
            };

            mutuallyExclusiveOptions.CommandView = _mutuallyExclusiveOptionsRg = new RadioGroup ()
            {
                RadioLabels = [ "G_ood", "_Bad", "U_gly" ]
            };

            var menuBGColor = new MenuItemv2
            {
                HelpText = "Menu BG Color",
                Key = Key.F8,
            };

            menuBGColor.CommandView = _menuBgColorCp = new ColorPicker() 
            {
                Width = 30
            };

            _menuBgColorCp.ColorChanged += (sender, args) =>
                                           {
                                               menu.ColorScheme = menu.ColorScheme! with
                                               {
                                                   Normal = new (menu.ColorScheme.Normal.Foreground, args.CurrentValue)
                                               };
                                           };

            menu.Add (autoSave, enableOverwrite, new Line (), mutuallyExclusiveOptions, new Line (), menuBGColor);
        }

        private void ConfigureDetialsSubMenu (Menuv2 menu)
        {
            var shortcut2 = new MenuItemv2
            {
                Title = "_Detail 1",
                Text = "Some detail #1"
            };

            var shortcut3 = new MenuItemv2
            {
                Title = "_Three",
                Text = "The 3rd item"
            };

            var editMode = new MenuItemv2
            {
                Title = "E_dit Mode",
                Text = "App binding to Command.Edit",
                Command = Command.Edit,
            };

            editMode.CommandView = _editModeCb = new CheckBox
            {
                Title = editMode.Title,
                HighlightStyle = HighlightStyle.None,
                CanFocus = false
            };

            // This ensures the checkbox state toggles when the hotkey of Title is pressed.
            //shortcut4.Accepting += (sender, args) => args.Cancel = true;

            menu.Add (shortcut2, shortcut3, new Line (), editMode);
        }

        private void ConfigureMoreDetailsSubMenu (Menuv2 menu)
        {
            var deeperDetail = new MenuItemv2
            {
                Title = "_Deeper Detail",
                Text = "Deeper Detail",
                Action = () => { MessageBox.Query ("Deeper Detail", "Lots of details", "_Ok"); }
            };

            var shortcut4 = new MenuItemv2
            {
                Title = "_Third",
                Text = "Below the line"
            };

            // This ensures the checkbox state toggles when the hotkey of Title is pressed.
            //shortcut4.Accepting += (sender, args) => args.Cancel = true;

            menu.Add (deeperDetail, new Line (), shortcut4);
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
