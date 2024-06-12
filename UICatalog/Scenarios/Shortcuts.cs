using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        ListView eventLog = new ListView ()
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
            Width = 30,
            Title = "Zi_gzag",
            Key = Key.F1,
            Text = "Width is 30",
            KeyBindingScope = KeyBindingScope.Application,
            BorderStyle = LineStyle.Dotted
        };
        shortcut1.Border.Thickness = new Thickness (1, 0, 1, 0);
        shortcut1.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();
                            };
        Application.Top.Add (shortcut1);

        var shortcut2 = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut1),
            Width = Dim.Width (shortcut1),
            Key = Key.F2,
            Text = "Width is ^",
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted,
            CommandView = new RadioGroup ()
            {
                Orientation = Orientation.Vertical,
                RadioLabels = ["One", "Two", "Three", "Four"],
            },
        };

        shortcut2.Border.Thickness = new Thickness (1, 0, 1, 0);
        shortcut2.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();
                            };
        Application.Top.Add (shortcut2);

        var shortcut3 = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut2),
            CommandView = new CheckBox () { Text = "_Align" },
            Key = Key.F3,
            HelpText = "Width is Fill",
            Width = Dim.Fill () - Dim.Width (eventLog),
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted
        };
        shortcut3.Border.Thickness = new Thickness (1, 0, 1, 0);

        ((CheckBox)shortcut3.CommandView).Toggled += (s, e) =>
                            {
                                eventSource.Add ($"Toggled: {s}");
                                eventLog.MoveDown ();

                                if (shortcut3.CommandView is CheckBox cb)
                                {
                                    int max = 0;
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

                                //Application.Top.SetNeedsDisplay ();
                                //Application.Top.LayoutSubviews ();
                                //Application.Top.SetNeedsDisplay ();
                                //Application.Top.Draw ();
                            };
        shortcut3.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();

                            };
        Application.Top.Add (shortcut3);

        var shortcut4 = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut3),
            Width = Dim.Width (shortcut3),
            Title = "C",
            HelpText = "Width is Fill",
            Key = Key.K,
            KeyBindingScope = KeyBindingScope.HotKey,
            //           Command = Command.Accept,
            BorderStyle = LineStyle.Dotted
        };
        shortcut4.Border.Thickness = new Thickness (1, 0, 1, 0);

        shortcut4.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();
                                MessageBox.Query ("Hi", $"You clicked {s}");
                            };
        Application.Top.Add (shortcut4);

        var shortcut5 = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut4),
            Width = Dim.Width (shortcut4),

            Title = "Fi_ve",
            Key = Key.F5.WithCtrl.WithAlt.WithShift,
            HelpText = "Width is Fill",
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted
        };
        shortcut5.Border.Thickness = new Thickness (1, 0, 1, 0);

        shortcut5.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();
                            };
        Application.Top.Add (shortcut5);


        var shortcutSlider = new Shortcut
        {
            X = 20,
            Y = Pos.Bottom (shortcut5),
            Key = Key.F5,
            HelpText = "Width is Fill",
            Width = Dim.Width (shortcut5),

            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted,
            CommandView = new Slider<string> ()
            {
                Orientation = Orientation.Vertical,
                AllowEmpty = false,
            }
        };


        ((Slider<string>)shortcutSlider.CommandView).Options = new List<SliderOption<string>> ()
            { new () { Legend = "A" }, new () { Legend = "B" }, new () { Legend = "C" } };
        ((Slider<string>)shortcutSlider.CommandView).SetOption (0);
        shortcutSlider.Border.Thickness = new Thickness (1, 0, 1, 0);

        shortcutSlider.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();
                            };
        Application.Top.Add (shortcutSlider);
        ;
        ((CheckBox)shortcut3.CommandView).OnToggled ();

        //shortcut1.SetFocus ();
        //View.Diagnostics = ViewDiagnosticFlags.Ruler;

    }

    private void Button_Clicked (object sender, EventArgs e) { MessageBox.Query ("Hi", $"You clicked {sender}"); }

}
