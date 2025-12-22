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

        View button = new ()
        {
            //CanFocus = true,
            X = Pos.Center (),
            Y = 1,
            Height = Dim.Auto(),
            Width = Dim.Auto (),
            Text = "_Button",
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        button.Accepting += (s, e) =>
                            {
                                // When Accepting is handled, set e.Handled to true to prevent further processing.
                                e.Handled = true;
                                MessageBox.ErrorQuery ((s as View)!.App!, "Error", "You pressed the button!", "_Ok");
                            };

        appWindow.Add (button);

        View second = new ()
        {
            //CanFocus = true,
            X = Pos.Center (),
            Y = Pos.Bottom (button) + 1,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Text = "_Second",
            MouseHighlightStates = MouseState.In
        };
        appWindow.Add (second);

        // Run - Start the application.
        app.Run (appWindow);
    }
}
