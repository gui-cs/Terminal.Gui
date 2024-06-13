using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
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
            Height = Dim.Fill (4),
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Source = new ListWrapper<string> (eventSource)
        };
        Application.Top.Add (eventLog);

        var vShortcut1 = new Shortcut
        {
            Orientation = Orientation.Vertical,
            X = 0,
            Width = 35,
            Title = "A_pp Shortcut",
            Key = Key.F1,
            Text = "Width is 30",
            KeyBindingScope = KeyBindingScope.Application,
            BorderStyle = LineStyle.Dotted
        };
        vShortcut1.Border.Thickness = new (1, 1, 1, 1);
        Application.Top.Add (vShortcut1);

        var vShortcut2 = new Shortcut
        {
            Orientation = Orientation.Vertical,
            X = 0,
            Y = Pos.Bottom (vShortcut1) - 1,
            Width = Dim.Width (vShortcut1),
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

        ((RadioGroup)vShortcut2.CommandView).SelectedItemChanged += (o, args) =>
                                                                   {
                                                                       eventSource.Add ($"SelectedItemChanged: {o.GetType ().Name} - {args.SelectedItem}");
                                                                       eventLog.MoveDown ();
                                                                   };

        vShortcut2.Accept += (o, args) =>
                            {
                                // Cycle to next item. If at end, set 0
                                if (((RadioGroup)vShortcut2.CommandView).SelectedItem < ((RadioGroup)vShortcut2.CommandView).RadioLabels.Length - 1)
                                {
                                    ((RadioGroup)vShortcut2.CommandView).SelectedItem++;
                                }
                                else
                                {
                                    ((RadioGroup)vShortcut2.CommandView).SelectedItem = 0;
                                }
                            };
        vShortcut2.Border.Thickness = new (1, 1, 1, 1);
        Application.Top.Add (vShortcut2);

        var vShortcut3 = new Shortcut
        {
            Orientation = Orientation.Vertical,
            X = 0,
            Y = Pos.Bottom (vShortcut2),
            CommandView = new CheckBox { Text = "_Align" },
            Key = Key.F3,
            HelpText = "Width is Fill",
            Width = Dim.Fill () - Dim.Width (eventLog),
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Rounded
        };
        vShortcut3.Border.Thickness = new (1, 1, 1, 0);

        ((CheckBox)vShortcut3.CommandView).Toggled += (s, e) =>
                                                     {
                                                         if (vShortcut3.CommandView is CheckBox cb)
                                                         {
                                                             eventSource.Add ($"Toggled: {cb.Text}");
                                                             eventLog.MoveDown ();

                                                             var max = 0;
                                                             var toAlign = Application.Top.Subviews.Where (v => v is Shortcut s && s.Orientation == Orientation.Vertical && s.BorderStyle == LineStyle.Rounded);

                                                             if (e.NewValue == true)
                                                             {
                                                                 foreach (Shortcut peer in toAlign)
                                                                 {
                                                                     max = Math.Max (max, peer.KeyView.Text.GetColumns ());
                                                                 }
                                                             }

                                                             foreach (Shortcut peer in toAlign)
                                                             {
                                                                 peer.MinimumKeyViewSize = max;
                                                             }
                                                         }
                                                     };
        Application.Top.Add (vShortcut3);

        var vShortcut4 = new Shortcut
        {
            Orientation = Orientation.Vertical,
            X = 0,
            Y = Pos.Bottom (vShortcut3),
            Width = Dim.Width (vShortcut3),
            CommandView = new Button
            {
                Title = "B_utton",
            },
            HelpText = "Width is Fill",
            Key = Key.K,
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Rounded
        };
        Button button = (Button)vShortcut4.CommandView;
        vShortcut4.CommandView.Accept += Button_Clicked;
        vShortcut4.Border.Thickness = new (1, 0, 1, 0);

        Application.Top.Add (vShortcut4);

        var vShortcut5 = new Shortcut
        {
            Orientation = Orientation.Vertical,
            X = 0,
            Y = Pos.Bottom (vShortcut4),
            Width = Dim.Width (vShortcut4),

            Key = Key.F5.WithCtrl.WithAlt.WithShift,
            HelpText = "CommandView.CanFocus",
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Rounded,
            CommandView = new CheckBox { Text = "_CanFocus" },
        };
        vShortcut5.Border.Thickness = new (1, 0, 1, 1);

        ((CheckBox)vShortcut5.CommandView).Toggled += (s, e) =>
                                                     {
                                                         if (vShortcut5.CommandView is CheckBox cb)
                                                         {
                                                             eventSource.Add ($"Toggled: {cb.Text}");
                                                             eventLog.MoveDown ();

                                                             foreach (Shortcut peer in Application.Top.Subviews.Where (v => v is Shortcut)!)
                                                             {
                                                                 peer.CanFocus = e.NewValue == true;
                                                                 peer.CommandView.CanFocus = e.NewValue == true;
                                                             }
                                                         }
                                                     };
        Application.Top.Add (vShortcut5);

        var vShortcutSlider = new Shortcut
        {
            Orientation = Orientation.Vertical,
            X = 0,
            Y = Pos.Bottom (vShortcut5) - 1,
            Key = Key.F5,
            HelpText = "Width is Fill",
            Width = Dim.Width (vShortcut5),

            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Rounded,
            CommandView = new Slider<string>
            {
                Orientation = Orientation.Vertical,
                AllowEmpty = false
            }
        };

        ((Slider<string>)vShortcutSlider.CommandView).Options = new () { new () { Legend = "A" }, new () { Legend = "B" }, new () { Legend = "C" } };
        ((Slider<string>)vShortcutSlider.CommandView).SetOption (0);
        vShortcutSlider.Border.Thickness = new (1, 1, 1, 1);

        ((Slider<string>)vShortcutSlider.CommandView).OptionsChanged += (o, args) =>
                                                                       {
                                                                           eventSource.Add ($"OptionsChanged: {o.GetType ().Name} - {args.Options}");
                                                                           eventLog.MoveDown ();
                                                                       };

        Application.Top.Add (vShortcutSlider);

        var vShortcut6 = new Shortcut
        {
            Orientation = Orientation.Vertical,
            X = 0,
            Y = Pos.Bottom (vShortcutSlider) - 1,
            Width = Dim.Width (vShortcutSlider),

            Title = "_No Key",
            HelpText = "Keyless",
            BorderStyle = LineStyle.Rounded
        };
        vShortcut6.Border.Thickness = new (1, 1, 1, 1);

        Application.Top.Add (vShortcut6);

        ((CheckBox)vShortcut3.CommandView).OnToggled();

        // Horizontal
        var hShortcut1 = new Shortcut
        {
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.Bottom (eventLog) + 1,
            Key = Key.F7,
            HelpText = "Horizontal",
            BorderStyle = LineStyle.Dashed,
            CanFocus = false
        };
        hShortcut1.Border.Thickness = new (0, 0, 1, 0);

        hShortcut1.CommandView = new ProgressBar
        {
            Text = "Progress",
            Title = "P",
            Fraction = 0.5f,
            Width = 10,
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous
        };
        hShortcut1.CommandView.Width = 10;
        hShortcut1.CommandView.Height = 1;
        hShortcut1.CommandView.CanFocus = false;
        Timer timer = new (10)
        {
            AutoReset = true,
        };
        timer.Elapsed += (o, args) =>
                         {
                             if (hShortcut1.CommandView is ProgressBar pb)
                             {
                                 if (pb.Fraction == 1.0)
                                 {
                                     pb.Fraction = 0;
                                 }
                                 pb.Fraction += 0.01f;

                                 Application.Wakeup ();

                                 pb.SetNeedsDisplay ();
                             }
                         };
        timer.Start ();

        Application.Top.Add (hShortcut1);

        var textField = new TextField ()
        {
            Text = "Edit me",
            Width = 10,
            Height = 1,
            CanFocus = true
        };

        var hShortcut2 = new Shortcut
        {
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.Top (hShortcut1),
            Key = Key.F8,
            HelpText = "Edit",
            CanFocus = true,
            BorderStyle = LineStyle.Dashed,
            CommandView = textField,
        };
        hShortcut2.Border.Thickness = new (0, 0, 1, 0);

        Application.Top.Add (hShortcut2);

        var hShortcutBG = new Shortcut
        {
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1)-1,
            Y = Pos.Top (hShortcut2),
            Key = Key.F9,
            HelpText = "BG Color",
            BorderStyle = LineStyle.Dashed,
            CanFocus = false
        };

        var bgColor = new ColorPicker ()
        {
            CanFocus = false,
            BoxHeight = 1,
            BoxWidth = 1,
        };
        bgColor.ColorChanged += (o, args) =>
                                {
                                    Application.Top.ColorScheme = new ColorScheme (Application.Top.ColorScheme)
                                    {
                                        Normal = new Attribute (Application.Top.ColorScheme.Normal.Foreground, args.Color),
                                    };
                                };
        hShortcutBG.CommandView = bgColor;
        hShortcutBG.Border.Thickness = new (1, 0, 1, 0);

        Application.Top.Add (hShortcutBG);

        var hShortcut3 = new Shortcut
        {
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.Top (hShortcut2),
            Key = Key.Esc,
            KeyBindingScope = KeyBindingScope.Application,
            Title = "Quit",
            HelpText = "App Scope",
            BorderStyle = LineStyle.Dashed,
            CanFocus = false
        };
        hShortcut3.Border.Thickness = new (0);
        hShortcut3.Accept += (o, args) =>
                            {
                                Application.RequestStop ();
                            };

        Application.Top.Add (hShortcut3);


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
