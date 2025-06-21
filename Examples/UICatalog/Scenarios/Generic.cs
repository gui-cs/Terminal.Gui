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
            X = Pos.Center (),
            Y = 1,
            Title = "_Button",
            IsDefault = true, // This button will be the default button
        };

        button.Accepting += (s, e) =>
                            {
                                MessageBox.ErrorQuery ("Error", "You pressed the button!", "_Ok");
                            };

        // Create StatusBar
        StatusBar statusBar = new ()
        {
            Visible = true,
            CanFocus = false
        };

        Shortcut scNoAction = new ()
        {
            Title = "No action",
            CanFocus = false
        };
        statusBar.Add (scNoAction);

        Shortcut scWithAction = new ()
        {
            Title = "With action",
            CanFocus = false,
            Action = () => MessageBox.Query ("Shortcut", "With action", "Ok"),
            Key = Key.F5
        };
        scWithAction.Accepting += (s, e) =>
                                  {
                                      // This is just to show that the action can be triggered by Accepting event
                                      scWithAction.Action?.Invoke ();
                                  };
        statusBar.Add (scWithAction);

        appWindow.Add (button, statusBar);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
