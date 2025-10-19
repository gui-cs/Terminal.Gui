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
            BorderStyle = LineStyle.None,
            //InvertFocusAttribute = true
        };



        var button = new CheckBox ()
        {
            X = Pos.Center (),
            Y = 1,
            Title = "_Button",

            // Comment this out to see how Issue #4170 is about IsDefaultAcceptView not working with Accepting event.
            // IsDefaultAcceptView = true,
            //SchemeName = "Error"
        };

        button.Accepting += (s, e) =>
                            {
                                // When Accepting is handled, set e.Handled to true to prevent further processing.
                                //e.Handled = true;
                                Logging.Debug ($"button.Acccepting");
                                MessageBox.ErrorQuery ("Error", "You pressed the button!", "_Ok");
                            };

        appWindow.Add (button);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
