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
            Width = 50,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Source = new ListWrapper<string> (eventSource)
        };
        Application.Top.Add (eventLog);

        var shortcut1 = new Shortcut
        {
            //Width =30,
            Title = "Zi_gzag",
            Key = Key.F1,
            Text = "Gonna zig zag",
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
            Y = Pos.Bottom (shortcut1),
            Width = Dim.Width (shortcut1),
            Title = "_Two",
            Key = Key.F2.WithAlt,
            Text = "Number two",
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted
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
            Y = Pos.Bottom (shortcut2),
            Width = Dim.Width (shortcut1),
            CommandView = new CheckBox () { Text = "_Align" },
            Key = Key.F3,
            HelpText = "Alignment",
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
            Width = Dim.Fill (50),
            Title = "C",
            Text = "H",
            Key = Key.K,
            KeyBindingScope = KeyBindingScope.HotKey,
            //           Command = Command.Accept,
            BorderStyle = LineStyle.Dotted
        };
        shortcut4.Border.Thickness = new Thickness (1, 0, 1, 0);
        shortcut4.Margin.Thickness = new Thickness (0, 1, 0, 0);
        View.Diagnostics = ViewDiagnosticFlags.Ruler;

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
            Width = Dim.Fill (50),
            Title = "Fi_ve",
            Key = Key.F5.WithCtrl.WithAlt.WithShift,
            Text = "Help text",
            KeyBindingScope = KeyBindingScope.HotKey,
            BorderStyle = LineStyle.Dotted
        };
        shortcut5.Border.Thickness = new Thickness (1, 0, 1, 0);
        shortcut5.Margin.Thickness = new Thickness (0, 1, 0, 0);
        View.Diagnostics = ViewDiagnosticFlags.Ruler;

        shortcut5.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();
                            };
        Application.Top.Add (shortcut5);


        ((CheckBox)shortcut3.CommandView).OnToggled ();

        //shortcut1.SetFocus ();

    }

    private void Button_Clicked (object sender, EventArgs e) { MessageBox.Query ("Hi", $"You clicked {sender}"); }

}
