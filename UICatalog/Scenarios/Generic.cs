using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Generic", "Generic sample - A template for creating new Scenarios")]
[ScenarioCategory ("Controls")]
public sealed class MyScenario : Scenario
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

        var button = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "_Press me!" };
        button.Accepting += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed the button!", "_Ok");
        appWindow.Add (button);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
