using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Keys", "Shows keyboard input handling.")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Keys : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        ObservableCollection<string> keyDownList = [];
        ObservableCollection<string> keyDownNotHandledList = [];
        ObservableCollection<string> swallowedList = [];

        using Window win = new () { Title = GetQuitKeyAndName () };

        Label label = new ()
        {
            X = 0,
            Y = 0,
            Text = "_Type text here:"
        };
        win.Add (label);

        TextField edit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (2),
            Height = 1,
        };
        win.Add (edit);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Text = "Last _app.Keyboard.KeyDown:"
        };
        win.Add (label);
        Label labelAppKeypress = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label)
        };
        win.Add (labelAppKeypress);

        app.Keyboard.KeyDown += (_, e) => labelAppKeypress.Text = e.ToString ();

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Text = "_Last TextField.KeyDown:"
        };
        win.Add (label);

        Label lastTextFieldKeyDownLabel = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Height = 1,
        };
        win.Add (lastTextFieldKeyDownLabel);

        edit.KeyDown += (_, e) => lastTextFieldKeyDownLabel.Text = e.ToString ();

        // Application key event log:
        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label) + 1,
            Text = "Application Key Events:"
        };
        win.Add (label);
        int maxKeyString = Key.CursorRight.WithAlt.WithCtrl.WithShift.ToString ().Length;

        ObservableCollection<string> keyList = [];

        ListView appKeyListView = new ()
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = "KeyDown:".Length + maxKeyString,
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (keyList)
        };
        appKeyListView.SchemeName = "Runnable";
        win.Add (appKeyListView);

        // View key events...
        edit.KeyDown += (_, a) => { keyDownList.Add (a.ToString ()); };

        edit.KeyDownNotHandled += (_, a) =>
                                  {
                                      keyDownNotHandledList.Add ($"{a}");
                                  };

        // KeyDown
        label = new ()
        {
            X = Pos.Right (appKeyListView) + 1,
            Y = Pos.Top (label),
            Text = "TextView Key Down:"
        };
        win.Add (label);

        ListView onKeyDownListView = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = maxKeyString,
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (keyDownList)
        };
        appKeyListView.SchemeName = "Runnable";
        win.Add (onKeyDownListView);

        // KeyDownNotHandled
        label = new ()
        {
            X = Pos.Right (onKeyDownListView) + 1,
            Y = Pos.Top (label),
            Text = "TextView KeyDownNotHandled:"
        };
        win.Add (label);

        ListView onKeyDownNotHandledListView = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = maxKeyString,
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (keyDownNotHandledList)
        };
        appKeyListView.SchemeName = "Runnable";
        win.Add (onKeyDownNotHandledListView);


        // Swallowed
        label = new ()
        {
            X = Pos.Right (onKeyDownNotHandledListView) + 1,
            Y = Pos.Top (label),
            Text = "Swallowed:"
        };
        win.Add (label);

        ListView onSwallowedListView = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = maxKeyString,
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (swallowedList)
        };
        appKeyListView.SchemeName = "Runnable";
        win.Add (onSwallowedListView);

        app.Driver!.GetInputProcessor ().AnsiSequenceSwallowed += (_, e) => { swallowedList.Add (e.Replace ("\x1b", "Esc")); };

        app.Keyboard.KeyDown += (_, a) => KeyDownPressUp (a, "Down");

        void KeyDownPressUp (Key args, string upDown)
        {
            string msg = $"Key{upDown,-7}: {args}";
            keyList.Add (msg);
            appKeyListView.MoveDown ();
            onKeyDownNotHandledListView.MoveDown ();
        }

        app.Run (win);
    }
}
