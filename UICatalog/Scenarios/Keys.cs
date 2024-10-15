using System.Collections.ObjectModel;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Keys", "Shows keyboard input handling.")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Keys : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        ObservableCollection<string> keyDownList = [];
        ObservableCollection<string> keyDownNotHandledList = new ();

        var win = new Window { Title = GetQuitKeyAndName () };

        var label = new Label
        {
            X = 0,
            Y = 0,
            Text = "_Type text here:"
        };
        win.Add (label);

        var edit = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (2),
            Height = 1,
        };
        win.Add (edit);

        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label),
            Text = "Last _Application.KeyDown:"
        };
        win.Add (label);
        var labelAppKeypress = new Label
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label)
        };
        win.Add (labelAppKeypress);

        Application.KeyDown += (s, e) => labelAppKeypress.Text = e.ToString ();

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Text = "_Last TextField.KeyDown:"
        };
        win.Add (label);

        var lastTextFieldKeyDownLabel = new Label
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Height = 1,
        };
        win.Add (lastTextFieldKeyDownLabel);

        edit.KeyDown += (s, e) => lastTextFieldKeyDownLabel.Text = e.ToString ();

        // Application key event log:
        label = new Label
        {
            X = 0,
            Y = Pos.Bottom (label) + 1,
            Text = "Application Key Events:"
        };
        win.Add (label);
        int maxKeyString = Key.CursorRight.WithAlt.WithCtrl.WithShift.ToString ().Length;

        ObservableCollection<string> keyList = new ();

        var appKeyListView = new ListView
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = "KeyDown:".Length + maxKeyString,
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (keyList)
        };
        appKeyListView.ColorScheme = Colors.ColorSchemes ["TopLevel"];
        win.Add (appKeyListView);

        // View key events...
        edit.KeyDown += (s, a) => { keyDownList.Add (a.ToString ()); };

        edit.KeyDownNotHandled += (s, a) =>
                                  {
                                      keyDownNotHandledList.Add ($"{a}");
                                  };

        // KeyDown
        label = new Label
        {
            X = Pos.Right (appKeyListView) + 1,
            Y = Pos.Top (label),
            Text = "TextView Key Down:"
        };
        win.Add (label);

        var onKeyDownListView = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = maxKeyString,
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (keyDownList)
        };
        onKeyDownListView.ColorScheme = Colors.ColorSchemes ["TopLevel"];
        win.Add (onKeyDownListView);

        // KeyDownNotHandled
        label = new Label
        {
            X = Pos.Right (onKeyDownListView) + 1,
            Y = Pos.Top (label),
            Text = "TextView KeyDownNotHandled:"
        };
        win.Add (label);

        var onKeyDownNotHandledListView = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = maxKeyString,
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (keyDownNotHandledList)
        };
        onKeyDownNotHandledListView.ColorScheme = Colors.ColorSchemes ["TopLevel"];
        win.Add (onKeyDownNotHandledListView);

        Application.KeyDown += (s, a) => KeyDownPressUp (a, "Down");
        Application.KeyUp += (s, a) => KeyDownPressUp (a, "Up");

        void KeyDownPressUp (Key args, string updown)
        {
            var msg = $"Key{updown,-7}: {args}";
            keyList.Add (msg);
            appKeyListView.MoveDown ();
            onKeyDownNotHandledListView.MoveDown ();
        }

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }
}
