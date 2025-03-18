#nullable enable

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Serilog;
using Serilog.Core;
using Serilog.Events;
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

        FrameView frame = new ()
        {
            Id = "frame",
            Title = "Cascading Menu...",

            X = 4,
            Y = 4,
            Width = Dim.Fill (8)! - Dim.Width (eventLog),
            Height = Dim.Fill (8),
            BorderStyle = LineStyle.Dotted
        };
        app.Add (frame);

        var rootMenu = new Menuv2 ()
        {
            Id = "rootMenu",
        };
        ConfigureRootMenu (frame, rootMenu);

        var optionsSubMenu = new Menuv2
        {
            Id = "optionsSubMenu",
            Visible = false
        };
        ConfigureOptionsSubMenu (frame, optionsSubMenu);

        var optionsSubMenuItem = new MenuItemv2 (frame, Command.Accept, "O_ptions", "File options", optionsSubMenu);
        rootMenu.Add (optionsSubMenuItem);

        var detailsSubMenu = new Menuv2
        {
            Id = "detailsSubMenu",
            Visible = false
        };
        ConfigureDetialsSubMenu (frame, detailsSubMenu);

        var detailsSubMenuItem = new MenuItemv2 (frame, Command.Accept, "_Details", "File details", detailsSubMenu);
        rootMenu.Add (detailsSubMenuItem);

        var moreDetailsSubMenu = new Menuv2
        {
            Id = "moreDetailsSubMenu",
            Visible = false
        };
        ConfigureMoreDetailsSubMenu (frame, moreDetailsSubMenu);

        var moreDetailsSubMenuItem = new MenuItemv2 (frame, Command.Accept, "_More Details", "More details", moreDetailsSubMenu);
        detailsSubMenu.Add (moreDetailsSubMenuItem);

        var popoverMenu = new PopoverMenu (rootMenu)
        {
            Id = "popOverMenu",
            Visible = true,
            X = 1, Y = 1
        };

        ////Application.PopoverHost.Add (popoverMenu);
        //Application.PopoverHost.Visible = true;

        //rootMenu.SubViews.ElementAt (0).SetFocus ();

        frame.Add (popoverMenu);

        frame.CommandNotBound += (o, args) =>
                               {
                                   eventSource.Add ($"{args.Context!.Command}: {frame?.Id}");
                                   eventLog.MoveDown ();
                                   args.Cancel = true;
                               };

        frame.Accepting += (o, args) =>
                               {
                                   eventSource.Add ($"{args.Context!.Command}: {frame?.Id}");
                                   eventLog.MoveDown ();
                                   // args.Cancel = true;
                               };

        popoverMenu.Accepting += (o, args) =>
                                 {
                                     Logging.Trace ($"Accepting: {popoverMenu!.Id} {args.Context.Command}");
                                     //eventSource.Add ($"Accepting: {menu!.Id} {args.Context.Command}");
                                     //eventLog.MoveDown ();
                                     //args.Cancel = true;
                                 };

        popoverMenu.Selecting += (o, args) =>
                                 {
                                     Logging.Trace ($"Selecting: {popoverMenu!.Id} {args.Context.Command}");
                                     //eventSource.Add ($"Selecting: {menu!.Id} {args.Context.Command}");
                                     //eventLog.MoveDown ();
                                     //args.Cancel = false;
                                 };

        ////popoverMenu.Root.MenuItemCommandInvoked += (o, args) =>
        ////                                      {
        ////                                          Logging.Trace ($"MenuItemCommandInvoked");
        ////                                          if (args.Context is CommandContext<KeyBinding> { Binding.Data: MenuItemv2 { } sc })
        ////                                          {
        ////                                              Logging.Trace($"Invoked: {sc.Title} {args.Context.Command}");
        ////                                              eventSource.Add ($"Invoked: {sc.Title} {args.Context.Command}");
        ////                                              //args.Cancel = true;
        ////                                          }

        ////                                          eventLog.MoveDown ();
        ////                                      };


        foreach (View view2 in popoverMenu.Root.SubViews.Where (s => s is MenuItemv2)!)
        {
            var sh = (MenuItemv2)view2;

            sh.Accepting += (o, args) =>
                            {
                                Logging.Trace ($"Accepting: {sh!.SuperView?.Id} {sh!.CommandView.Text}");
                                //eventSource.Add ($"Accepting: {sh!.SuperView?.Id} {sh!.CommandView.Text}");
                                //eventLog.MoveDown ();
                                //args.Cancel = true;
                            };

            sh.Selecting += (o, args) =>
                            {
                                Logging.Trace ($"Selecting: {sh!.SuperView?.Id} {sh!.CommandView.Text}");
                                //eventSource.Add ($"Selecting: {sh!.SuperView?.Id} {sh!.CommandView.Text}");
                                //eventLog.MoveDown ();
                                //args.Cancel = false;
                            };
        }

        app.Add (eventLog);

        Application.Run (app);
        app.Dispose ();
        //popoverMenu.Dispose ();
        Application.Shutdown ();
    }

    private void ConfigureRootMenu (View targetView, Menuv2 menu)
    {
        var shortcut1 = new MenuItemv2
        {
            Title = "_New",
            Key = Key.N.WithCtrl,
            Text = "New File",
            Command = Command.New,
            TargetView = targetView
        };

        var shortcut2 = new MenuItemv2
        {
            Title = "_Open...",
            Text = "Open File",
            Key = Key.O.WithCtrl,
            Command = Command.Open,
            TargetView = targetView
        };

        var shortcut3 = new MenuItemv2
        {
            Title = "_Save",
            Text = "Save file",
            Key = Key.S.WithCtrl,
            Command = Command.Save,
            TargetView = targetView
        };

        var shortcut4 = new MenuItemv2
        {
            Title = "Sa_ve As...",
            Text = "Save file as",
            Key = Key.V.WithCtrl,
            Command = Command.SaveAs,
            TargetView = targetView

        };


        var shortcut5 = new MenuItemv2
        {
            Title = "_Auto Save",
            Text = "Automatically save",
            Key = Key.A.WithCtrl,
            TargetView = targetView
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


        //// This ensures the checkbox state toggles when the hotkey of Title is pressed.
        ////shortcut4.Accepting += (sender, args) => args.Cancel = true;

        menu.Add (shortcut1, shortcut2, shortcut3, shortcut4, line, shortcut5);
    }


    private void ConfigureOptionsSubMenu (View targetView, Menuv2 menu)
    {
        var shortcut2 = new MenuItemv2
        {
            Title = "_Option 1",
            Text = "Some option #1",
            Key = Key.G.WithAlt
        };

        var shortcut3 = new MenuItemv2
        {
            Title = "_Three",
            Text = "The 3rd item",
            Key = Key.T.WithAlt
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
            Key = Key.D7.WithAlt
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

    private void ConfigureDetialsSubMenu (View targetView, Menuv2 menu)
    {
        var shortcut2 = new MenuItemv2
        {
            Title = "_Detail 1",
            Text = "Some detail #1",
            Key = Key.G.WithAlt
        };

        var shortcut3 = new MenuItemv2
        {
            Title = "_Three",
            Text = "The 3rd item",
            Key = Key.D3.WithAlt
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
            Key = Key.D8.WithAlt
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


    private void ConfigureMoreDetailsSubMenu (View targetView, Menuv2 menu)
    {
        var shortcut2 = new MenuItemv2
        {
            Title = "_Deeper Detail",
            Text = "Deeper Detail",
            Key = Key.V.WithAlt
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
            Key = Key.D3.WithAlt
        };

        // This ensures the checkbox state toggles when the hotkey of Title is pressed.
        //shortcut4.Accepting += (sender, args) => args.Cancel = true;

        menu.Add (shortcut2, line, shortcut4);
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
