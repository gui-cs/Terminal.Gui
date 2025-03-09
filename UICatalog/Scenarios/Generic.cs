#nullable enable
using Terminal.Gui;

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
        };

        var button = new Button { Id = "button", X = Pos.Center (), Y = 1, Text = "_Press me!" };

        button.Accepting += (s, e) =>
                            {
                                // Anytime Accepting is handled, make sure to set e.Cancel to false.
                                e.Cancel = true;
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
