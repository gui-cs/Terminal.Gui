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
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
        };

        var button = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Press me!" };
        button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed the button!", "Ok");
        appWindow.Add (button);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
