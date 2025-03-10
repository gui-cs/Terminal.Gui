#nullable enable

using System.Collections.ObjectModel;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("MenusV2", "Illustrates MenuV2")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Shortcuts")]
public class MenusV2 : Scenario
{
    public override void Main ()
    {
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
            Title = "Cascading Menu...",
            X = 0,
            Y = 0,
            Width = Dim.Fill ()! - Dim.Width (eventLog),
            Height = Dim.Fill ()
        };
        app.Add (frame);

        var menu = new Menuv2
        {
            Id = "menu",
            X = 10,
            Y = 5
        };

        menu.MenuItemCommandInvoked += (o, args) =>
                                       {
                                           if (args.Context is CommandContext<KeyBinding> { Binding.Data: MenuItemv2 { } sc })
                                           {
                                               eventSource.Add ($"Invoked: {sc.Id} {args.Context.Command}");
                                           }

                                           eventLog.MoveDown ();
                                       };

        frame.Add (menu);
        ConfigureMenu (menu);

        var subMenu = new Menuv2
        {
            Id = "menu",
            X = 0,
            Y = 0,
            Visible = false
        };
        ConfigureMenu (subMenu);
        frame.Add (subMenu);

        var cascadeShortcut = new MenuItemv2 (frame, Command.Context, "_Cascade", "Sub menu...");

        //cascadeShortcut.Accepting += (o, args) =>
        //                             {
        //                                 Point loc = cascadeShortcut.Frame.Location;
        //                                 subMenu.X = loc.X + menu.Frame.Width - 1;
        //                                 subMenu.Y = loc.Y;
        //                                 subMenu.Visible = !subMenu.Visible;
        //                             };

        //cascadeShortcut.Highlight += (o, args) =>
        //                                   {

        //                                       {
        //                                           Point loc = cascadeShortcut.Frame.Location;
        //                                           subMenu.X = loc.X + menu.Frame.Width - 1;
        //                                           subMenu.Y = loc.Y;
        //                                           subMenu.Visible = args.NewValue.HasFlag(HighlightStyle.Hover);
        //                                       }
        //                                   };

        //subMenu.HasFocusChanged += (o, args) =>
        //                           {
        //                               if (!args.NewValue)
        //                               {
        //                                   subMenu.Visible = false;
        //                               }
        //                           };

        menu.Add (cascadeShortcut);

        menu.SubViews.ElementAt (0).SetFocus ();

        FrameView frameView = frame;

        frameView.Accepting += (o, args) =>
                               {
                                   eventSource.Add ($"Accepting: {frameView?.Id}");
                                   eventLog.MoveDown ();
                                   args.Cancel = true;
                               };

        foreach (View view1 in frameView.SubViews.Where (b => b is Bar || b is MenuBarv2 || b is Menuv2)!)
        {
            var barView = (Bar)view1;

            barView.Accepting += (o, args) =>
                                 {
                                     eventSource.Add ($"Accepting: {barView!.Id} {args.Context.Command}");
                                     eventLog.MoveDown ();
                                     args.Cancel = true;
                                 };

            barView.Selecting += (o, args) =>
                                 {
                                     eventSource.Add ($"Selecting: {barView!.Id} {args.Context.Command}");
                                     eventLog.MoveDown ();
                                     args.Cancel = false;
                                 };

            if (barView is Menuv2 menuv2)
            {
                menuv2.MenuItemCommandInvoked += (o, args) =>
                                                 {
                                                     if (args.Context is CommandContext<KeyBinding> { Binding.Data: MenuItemv2 { } sc })
                                                     {
                                                         eventSource.Add ($"Invoked: {sc.Id} {args.Context.Command}");
                                                     }

                                                     eventLog.MoveDown ();
                                                 };
            }

            foreach (View view2 in barView.SubViews.Where (s => s is Shortcut)!)
            {
                var sh = (Shortcut)view2;

                sh.Accepting += (o, args) =>
                                {
                                    eventSource.Add ($"Accepting: {sh!.SuperView?.Id} {sh!.CommandView.Text}");
                                    eventLog.MoveDown ();
                                    args.Cancel = true;
                                };

                sh.Selecting += (o, args) =>
                                {
                                    eventSource.Add ($"Selecting: {sh!.SuperView?.Id} {sh!.CommandView.Text}");
                                    eventLog.MoveDown ();
                                    args.Cancel = false;
                                };
            }
        }

        app.Add (eventLog);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    private void ConfigureMenu (Menuv2 menu)
    {
        var shortcut1 = new MenuItemv2
        {
            Title = "Z_igzag",
            Key = Key.I.WithCtrl,
            Text = "Gonna zig zag"
        };

        var shortcut2 = new MenuItemv2
        {
            Title = "Za_G",
            Text = "Gonna zag",
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
            Key = Key.D3.WithAlt
        };

        shortcut4.CommandView = new CheckBox
        {
            Title = shortcut4.Title,
            HighlightStyle = HighlightStyle.None,
            CanFocus = false
        };

        // This ensures the checkbox state toggles when the hotkey of Title is pressed.
        shortcut4.Accepting += (sender, args) => args.Cancel = true;

        menu.Add (shortcut1, shortcut2, shortcut3, line, shortcut4);
    }
}
