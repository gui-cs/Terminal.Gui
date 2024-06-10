using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            Title = "Zi_gzag",
            Key = Key.F1,
            Text = "Gonna zig zag",
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.Accept,
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
            Width = Dim.Width(shortcut1),
            Title = "_Two",
            Key = Key.F2.WithAlt,
            Text = "Number two",
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.Accept,
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
            Title = "T_hree",
            Key = Key.F3,
            Text = "Number 3",
            KeyBindingScope = KeyBindingScope.HotKey,
            Command = Command.Accept,
            BorderStyle = LineStyle.Dotted
        };
        shortcut3.Border.Thickness = new Thickness (1, 0, 1, 0);

        shortcut3.Accept += (s, e) =>
                            {
                                eventSource.Add ($"Accept: {s}");
                                eventLog.MoveDown ();
                            };
        Application.Top.Add (shortcut3);

        shortcut1.SetFocus ();

    }

    private void Button_Clicked (object sender, EventArgs e) { MessageBox.Query ("Hi", $"You clicked {sender}"); }

}
