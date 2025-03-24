#nullable enable

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using static UICatalog.Scenarios.MenusV2;

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

            X = 1,
            Y = 1,
            Width = Dim.Fill (2)! - Dim.Width (eventLog),
            Height = Dim.Fill (2),
            BorderStyle = LineStyle.Dotted
        };
        app.Add (targetView);

        targetView.CommandNotBound += (o, args) =>
                               {
                                   Logging.Trace ($"targetView CommandNotBound: {args?.Context?.Command}");
                                   eventSource.Add ($"targetView CommandNotBound: {args?.Context?.Command}");
                                   eventLog.MoveDown ();
                                   args.Cancel = true;
                               };

        targetView.Accepting += (o, args) =>
                               {
                                   Logging.Trace ($"targetView Accepting: {args?.Context?.Source?.Title}");
                                   eventSource.Add ($"targetView Accepting: {args?.Context?.Source?.Title}: ");
                                   eventLog.MoveDown ();
                                   args.Cancel = true;
                               };

        targetView.PopoverMenu!.Accepted += (o, args) =>
                                           {
                                               Logging.Trace ($"PopoverMenu Accepted: {args?.Context?.Source?.Title}");
                                               eventSource.Add ($"PopoverMenu Accepted: {args?.Context?.Source?.Title}: ");
                                               eventLog.MoveDown ();
                                           };

        app.Add (eventLog);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    public class TargetView : View
    {
        internal PopoverMenu? PopoverMenu { get; private set; }

        public TargetView ()
        {
            CanFocus = true;
            Text = "TargetView";
            BorderStyle = LineStyle.Dashed;
            AddCommand (Command.Context,
                       ctx =>
                       {
                           if (Application.Popover?.GetPopover () as PopoverMenu is { Visible: false } visiblePopover)
                           {
                               visiblePopover.MakeVisible ();
                           }

                           return true;
                       });

            KeyBindings.Add (PopoverMenu.DefaultKey, Command.Context);

            MouseBindings.ReplaceCommands (PopoverMenu.MouseFlags, Command.Context);

            AddCommand (Command.Cancel,
                        ctx =>
                        {
                            if (Application.Popover?.GetPopover () as PopoverMenu is { Visible: true } visiblePopover)
                            {
                                visiblePopover.Visible = false;
                            }

                            return true;
                        });

            MouseBindings.ReplaceCommands (MouseFlags.Button1Clicked, Command.Cancel);

            Add (
                 new Button ()
                 {
                     Title = "_Button",
                     X = Pos.Center (),
                     Y = Pos.Center ()
                 });


            var rootMenu = new Menuv2 ()
            {
                Id = "rootMenu",
            };
            ConfigureRootMenu (rootMenu);

            var optionsSubMenu = new Menuv2
            {
                Id = "optionsSubMenu",
                Visible = false
            };
            ConfigureOptionsSubMenu (optionsSubMenu);

            var optionsSubMenuItem = new MenuItemv2 (this, Command.NotBound, "O_ptions", "File options", optionsSubMenu);
            rootMenu.Add (optionsSubMenuItem);

            var detailsSubMenu = new Menuv2
            {
                Id = "detailsSubMenu",
                Visible = false
            };
            ConfigureDetialsSubMenu (detailsSubMenu);

            var detailsSubMenuItem = new MenuItemv2 (this, Command.NotBound, "_Details", "File details", detailsSubMenu);
            rootMenu.Add (detailsSubMenuItem);

            var moreDetailsSubMenu = new Menuv2
            {
                Id = "moreDetailsSubMenu",
                Visible = false
            };
            ConfigureMoreDetailsSubMenu (moreDetailsSubMenu);

            var moreDetailsSubMenuItem = new MenuItemv2 (this, Command.NotBound, "_More Details", "More details", moreDetailsSubMenu);
            detailsSubMenu.Add (moreDetailsSubMenuItem);


            PopoverMenu = new PopoverMenu (rootMenu)
            {
                Id = "popoverMenu",
            };

            Initialized += (sender, args) =>
                           {
                               Application.Popover?.ShowPopover(PopoverMenu);
                               PopoverMenu?.BeginInit ();
                               PopoverMenu?.EndInit ();
                               PopoverMenu?.MakeVisible ();
                           };


        }


        private void ConfigureRootMenu (Menuv2 menu)
        {
            var shortcut1 = new MenuItemv2
            {
                Title = "_New",
                Key = Key.N.WithAlt,
                BindKeyToApplication = true,
                Text = "New File",
                Command = Command.New,
                TargetView = this
            };

            var shortcut2 = new MenuItemv2
            {
                Title = "_Open...",
                Text = "Open File",
                Key = Key.O.WithAlt,
                BindKeyToApplication = true,
                Command = Command.Open,
                TargetView = this
            };

            var shortcut3 = new MenuItemv2
            {
                Title = "_Save",
                Text = "Save file",
                Key = Key.S.WithAlt,
                BindKeyToApplication = true,
                Command = Command.Save,
                TargetView = this
            };

            var shortcut4 = new MenuItemv2
            {
                Title = "Sa_ve As...",
                Text = "Save file as",
                Key = Key.V.WithAlt,
                BindKeyToApplication = true,
                Command = Command.SaveAs,
                TargetView = this

            };


            var shortcut5 = new MenuItemv2
            {
                Title = "_Auto Save",
                Text = "Automatically save",
                Key = Key.A.WithAlt,
                BindKeyToApplication = true,

            };

            shortcut5.CommandView = new CheckBox
            {
                Title = shortcut5.Title,
                HighlightStyle = HighlightStyle.None,
                CanFocus = false
            };

            var line = new Line
            {
                X = -1,
                Width = Dim.Fill ()! + 1
            };


            // This ensures the checkbox state toggles when the hotkey of Title is pressed.
            //shortcut4.Accepting += (sender, args) => args.Cancel = true;

            menu.Add (shortcut1, shortcut2, shortcut3, shortcut4, line, shortcut5);
        }


        private void ConfigureOptionsSubMenu (Menuv2 menu)
        {
            var shortcut2 = new MenuItemv2
            {
                Title = "Enable Over_write",
                Text = "Overwrite",
                Key = Key.W.WithAlt,
                BindKeyToApplication = true,
                Command = Command.EnableOverwrite,
                TargetView = this
            };

            var shortcut3 = new MenuItemv2
            {
                Title = "_Three",
                Text = "The 3rd item",
                Key = Key.T.WithAlt,
                BindKeyToApplication = true,
            };

            var line = new Line
            {
                X = -1,
                Width = Dim.Fill ()! + 1
            };

            var shortcut4 = new MenuItemv2
            {
                Title = "_Four",
                Text = "Below the line",
                Key = Key.D7.WithAlt,
                BindKeyToApplication = true,
            };

            shortcut4.CommandView = new CheckBox
            {
                Title = shortcut4.Title,
                HighlightStyle = HighlightStyle.None,
                CanFocus = false
            };

            // This ensures the checkbox state toggles when the hotkey of Title is pressed.
            // shortcut4.Accepting += (sender, args) => args.Cancel = true;

            menu.Add (shortcut2, shortcut3, line, shortcut4);
        }

        private void ConfigureDetialsSubMenu (Menuv2 menu)
        {
            var shortcut2 = new MenuItemv2
            {
                Title = "_Detail 1",
                Text = "Some detail #1",
                Key = Key.G.WithAlt,
                BindKeyToApplication = true,
            };

            var shortcut3 = new MenuItemv2
            {
                Title = "_Three",
                Text = "The 3rd item",
                Key = Key.D9.WithAlt,
                BindKeyToApplication = true,
            };

            var line = new Line
            {
                X = -1,
                Width = Dim.Fill ()! + 1
            };

            var shortcut4 = new MenuItemv2
            {
                Title = "_Four",
                Text = "Below the line",
                Key = Key.D8.WithAlt,
                BindKeyToApplication = true,

            };

            shortcut4.CommandView = new CheckBox
            {
                Title = shortcut4.Title,
                HighlightStyle = HighlightStyle.None,
                CanFocus = false
            };

            // This ensures the checkbox state toggles when the hotkey of Title is pressed.
            //shortcut4.Accepting += (sender, args) => args.Cancel = true;

            menu.Add (shortcut2, shortcut3, line, shortcut4);
        }


        private void ConfigureMoreDetailsSubMenu (Menuv2 menu)
        {
            var shortcut2 = new MenuItemv2
            {
                Title = "_Deeper Detail",
                Text = "Deeper Detail",
                Key = Key.D.WithAlt,
                BindKeyToApplication = true,
            };

            var line = new Line
            {
                X = -1,
                Width = Dim.Fill ()! + 1
            };

            var shortcut4 = new MenuItemv2
            {
                Title = "_Third",
                Text = "Below the line",
                Key = Key.D5.WithAlt,
                BindKeyToApplication = true,
            };

            // This ensures the checkbox state toggles when the hotkey of Title is pressed.
            //shortcut4.Accepting += (sender, args) => args.Cancel = true;

            menu.Add (shortcut2, line, shortcut4);
        }

        /// <inheritdoc />
        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                PopoverMenu?.Dispose ();
                PopoverMenu = null;
            }
        }
    }



    private const string LOGFILE_LOCATION = "./logs";
    private static string _logFilePath = string.Empty;
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
