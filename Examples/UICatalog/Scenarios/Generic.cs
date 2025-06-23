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

        var textField = new TextField ()
        {
            X = Pos.Center (),
            Y = 1,
            Width = Dim.Auto (DimAutoStyle.Auto, 80),
            Caption = "You can type here and press Enter key to accept trigger the default button.",
        };

        var button = new Button ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (textField) + 1,
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
            CanFocus = false,
            Key = Key.F2
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
                                      // Don't invoke the action here, otherwise will be triggered twice
                                      // If you want to cancel it just comment out the bellow code
                                      //e.Handled = true;
                                  };
        statusBar.Add (scWithAction);

        appWindow.Add (textField, button, statusBar);

        // Set focus to button
        button.SetFocus ();

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
