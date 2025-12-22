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
        using IApplication app = Application.Instance;

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        Button button = new ()
        {
            //CanFocus = true,
            X = Pos.Center (),
            Y = 1,
            Text = "_Button",
        };

        button.Accepting += (s, e) =>
                            {
                                // When Accepting is handled, set e.Handled to true to prevent further processing.
                                e.Handled = true;
                                MessageBox.Query ((s as View)!.App!, "Nice Job", "You pressed the button!", "_Ok");
                            };

        appWindow.Add (button);

        // Run - Start the application.
        app.Run (appWindow);
    }
}
