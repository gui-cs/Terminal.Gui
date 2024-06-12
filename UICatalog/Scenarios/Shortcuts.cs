using System;
using System.Collections.ObjectModel;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Shortcuts", "Illustrates Shortcut class.")]
[ScenarioCategory ("Controls")]
public class Shortcuts : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        Window app = new ();

        app.Loaded += App_Loaded;

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    // Setting everything up in Loaded handler because we change the
    // QuitKey and it only sticks if changed after init
    private void App_Loaded (object sender, EventArgs e)
    {
        Application.QuitKey = Key.Z.WithCtrl;
        Application.Top.Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}";

        ObservableCollection<string> eventSource = new ();

        var eventLog = new ListView
        {
            X = Pos.AnchorEnd (),
            Width = 40,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Source = new ListWrapper<string> (eventSource)
        };
        Application.Top.Add (eventLog);

        var shortcut1 = new Shortcut
        {
            X = 20,
            Width = 35,
            Title = "A_pp Shortcut",
            Key = Key.F1,
            Text = "Width is 30",
            KeyBindingScope = KeyBindingScope.Application,
            BorderStyle = LineStyle.Dotted
        };
        shortcut1.Border.Thickness = new (1, 1, 1, 1);
        Application.Top.Add (shortcut1);

        var shortcut2 = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut1) - 1,
            Width = Dim.Width (shortcut1),
            Key = Key.F2,
            Text = "Width is ^",
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted,
            CommandView = new RadioGroup
            {
                Orientation = Orientation.Vertical,
                RadioLabels = ["One", "Two", "Three", "Four"]
            }
        };

        ((RadioGroup)shortcut2.CommandView).SelectedItemChanged += (o, args) =>
                                                                   {
                                                                       eventSource.Add ($"SelectedItemChanged: {o.GetType ().Name} - {args.SelectedItem}");
                                                                       eventLog.MoveDown ();
                                                                   };

        shortcut2.Accept += (o, args) =>
                            {
                                // Cycle to next item. If at end, set 0
                                if (((RadioGroup)shortcut2.CommandView).SelectedItem < ((RadioGroup)shortcut2.CommandView).RadioLabels.Length - 1)
                                {
                                    ((RadioGroup)shortcut2.CommandView).SelectedItem++;
                                }
                                else
                                {
                                    ((RadioGroup)shortcut2.CommandView).SelectedItem = 0;
                                }
                            };
        shortcut2.Border.Thickness = new (1, 1, 1, 1);
        Application.Top.Add (shortcut2);

        var shortcut3 = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut2),
            CommandView = new CheckBox { Text = "_Align" },
            Key = Key.F3,
            HelpText = "Width is Fill",
            Width = Dim.Fill () - Dim.Width (eventLog),
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted
        };
        shortcut3.CommandView.CanFocus = true;
        shortcut3.Border.Thickness = new (1, 1, 1, 0);

        ((CheckBox)shortcut3.CommandView).Toggled += (s, e) =>
                                                     {
                                                         if (shortcut3.CommandView is CheckBox cb)
                                                         {
                                                             eventSource.Add ($"Toggled: {cb.Text}");
                                                             eventLog.MoveDown ();

                                                             var max = 0;

                                                             if (e.NewValue == true)
                                                             {
                                                                 foreach (Shortcut peer in Application.Top.Subviews.Where (v => v is Shortcut)!)
                                                                 {
                                                                     max = Math.Max (max, peer.KeyView.Text.GetColumns ());
                                                                 }
                                                             }

                                                             foreach (Shortcut peer in Application.Top.Subviews.Where (v => v is Shortcut)!)
                                                             {
                                                                 peer.MinimumKeyViewSize = max;
                                                             }
                                                         }
                                                     };
        Application.Top.Add (shortcut3);

        var shortcut4 = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut3),
            Width = Dim.Width (shortcut3),
            CommandView = new Button
            {
                Title = "B_utton",
            },
            HelpText = "Width is Fill",
            Key = Key.K,
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted
        };
        Button button = (Button)shortcut4.CommandView;
        shortcut4.CommandView.Accept += Button_Clicked;
        shortcut4.CommandView.CanFocus = true;
        shortcut4.Border.Thickness = new (1, 0, 1,0);

        Application.Top.Add (shortcut4);

        var shortcut5 = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut4) ,
            Width = Dim.Width (shortcut4),

            Title = "Fi_ve",
            Key = Key.F5.WithCtrl.WithAlt.WithShift,
            HelpText = "Width is Fill",
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted
        };
        shortcut5.Border.Thickness = new (1, 0, 1, 1);

        Application.Top.Add (shortcut5);

        var shortcutSlider = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut5) - 1,
            Key = Key.F5,
            HelpText = "Width is Fill",
            Width = Dim.Width (shortcut5),

            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted,
            CommandView = new Slider<string>
            {
                Orientation = Orientation.Vertical,
                AllowEmpty = false
            }
        };

        ((Slider<string>)shortcutSlider.CommandView).Options = new () { new () { Legend = "A" }, new () { Legend = "B" }, new () { Legend = "C" } };
        ((Slider<string>)shortcutSlider.CommandView).SetOption (0);
        shortcutSlider.Border.Thickness = new (1, 1, 1, 1);

        ((Slider<string>)shortcutSlider.CommandView).OptionsChanged += (o, args) =>
                                                                       {
                                                                           eventSource.Add ($"OptionsChanged: {o.GetType ().Name} - {args.Options}");
                                                                           eventLog.MoveDown ();
                                                                       };

        Application.Top.Add (shortcutSlider);

        var shortcut6 = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcutSlider) - 1,
            Width = Dim.Width (shortcutSlider),

            Title = "_No Key",
            HelpText = "Keyless",
            BorderStyle = LineStyle.Dotted
        };
        shortcut6.Border.Thickness = new (1, 1, 1, 1);

        Application.Top.Add (shortcut6);

        foreach (View sh in Application.Top.Subviews.Where (v => v is Shortcut)!)
        {
            if (sh is Shortcut shortcut)
            {
                shortcut.Accept += (o, args) =>
                                   {
                                       var x = button;
                                       eventSource.Add ($"Accept: {shortcut!.CommandView.Text}");
                                       eventLog.MoveDown ();
                                   };
            }
        }
    }

    private void Button_Clicked (object sender, EventArgs e) { MessageBox.Query ("Hi", $"You clicked {sender}"); }
}
