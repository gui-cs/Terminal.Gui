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
            BorderStyle = LineStyle.None
        };

        var button = new Shortcut()
        {
            CanFocus = true,
            Id = "button",
            X = Pos.Center (),
            Y = 1,
            ShadowStyle = ShadowStyle.None,
            Text = "HelpText",
            Title = "Command",
            Key = Key.F10,
            HighlightStyle = HighlightStyle.None
        };
        button.ColorScheme = Colors.ColorSchemes ["Error"];

        button.Padding!.Thickness = new (1);
        button.Padding.ColorScheme = Colors.ColorSchemes ["Toplevel"];
        button.Margin!.Thickness = new (1);

        button.Accepting += (s, e) =>
                            {
                                // When Accepting is handled, set e.Handled to true to prevent further processing.
                                e.Handled = true;
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
