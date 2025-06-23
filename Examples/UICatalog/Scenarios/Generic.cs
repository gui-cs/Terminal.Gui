#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Generic", "Generic sample - A template for creating new Scenarios")]
[ScenarioCategory ("Controls")]
public sealed class Generic : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        var button = new Button ()
        {
            CanFocus = true,
            X = Pos.Center (),
            Y = 1,
            Height = Dim.Auto(),
            Width = Dim.Auto(),
            Title = "_Button",
            //Text = "_Button"
            IsDefault = true
        };
        //button.MouseClick += (s, e) =>
        //                     {
        //                         if (e.Handled)
        //                         {
        //                             return;
        //                         }

        //                         // TODO: With https://github.com/gui-cs/Terminal.Gui/issues/3778 we won't have to pass data:
        //                         e.Handled = button.InvokeCommand<KeyBinding> (Command.Accept, new KeyBinding ([Command.HotKey], button, data: null)) == true;
        //                     };

        button.Accepting += (s, e) =>
                            {
                                // When Accepting is handled, set e.Handled to true to prevent further processing.
                                //e.Handled = true;
                                Logging.Debug($"button.Acccepting");
                                //MessageBox.ErrorQuery ("Error", "You pressed the button!", "_Ok");
                            };

        appWindow.Add (button);

        // Create StatusBar
        StatusBar statusBar = new ()
        {
            Visible = true,
            CanFocus = false
        };

        Shortcut shortcut = new ()
        {
            Title = "_Click here to see bug",
            Key = Key.F2,
            CanFocus = false
        };
        statusBar.Add (shortcut);

        appWindow.Add (statusBar);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
