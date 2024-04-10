using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Coordinates", "Demonstrates Screen, Frame, Content, and Viewport coordinates.")]
[ScenarioCategory ("Layout")]
public sealed class Coordinates : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window app = new ()
        {
            Title = $"Application/Screen",
            BorderStyle = LineStyle.HeavyDotted
        };

        View frame = new ()
        {
            Title = "",
            Text = "Content - Location (0 0), Size: (54, 14)",
            X = 3,
            Y = 3,
            Width = 60,
            Height = 20,
            ColorScheme = new ColorScheme (new Attribute (Color.Black, Color.White))
        };
        frame.Margin.Thickness = new (1);
        frame.Margin.ColorScheme = new ColorScheme (new Attribute (Color.Black, Color.BrightRed));
        frame.Margin.Add (new Label () { Title = "Margin - Frame-Relative Location (0,0)" });
        frame.Border.LineStyle = LineStyle.None;
        frame.Border.Thickness = new (1);
        frame.Border.ColorScheme = new ColorScheme (new Attribute (Color.Black, Color.BrightGreen));
        frame.Border.Add (new Label () { Title = "Border - Frame-Relative Location (1,1)" });
        frame.Padding.Thickness = new (1);
        frame.Padding.ColorScheme = new ColorScheme (new Attribute (Color.Black, Color.BrightYellow));
        frame.Padding.Add (new Label () { Title = "Padding - Frame-Relative Location (2,2)" });

        app.Add (frame);
        // Run - Start the application.
        Application.Run (app);
        app.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
