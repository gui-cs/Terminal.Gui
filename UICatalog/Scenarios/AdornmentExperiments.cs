using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Adornment Experiments", "Playground for Adornment experiments")]
[ScenarioCategory ("Controls")]
public class AdornmentExperiments : Scenario
{
    private ViewDiagnosticFlags _diagnosticFlags;

    public override void Init ()
    {
        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

        _diagnosticFlags = View.Diagnostics;
        //View.Diagnostics = ViewDiagnosticFlags.MouseEnter;
    }

    private View _frameView;
    public override void Setup ()
    {
        _frameView = new View ()
        {
            Title = "Frame View",
            X = 0,
            Y = 0,
            Width = Dim.Percent(90),
            Height = Dim.Percent (90),
            CanFocus = true,
        };
        Top.Add (_frameView);
        _frameView.Initialized += FrameView_Initialized;

        Top.Closed += (s, e) => View.Diagnostics = _diagnosticFlags;
    }

    private void FrameView_Initialized (object sender, System.EventArgs e)
    {
        _frameView.Border.Thickness = new (1, 1, 1, 1);
        _frameView.Padding.Thickness = new (0, 10, 0, 0);
        _frameView.Padding.ColorScheme = Colors.ColorSchemes ["Error"];

        var label = new Label ()
        {
            Text = "In Padding",
            X = Pos.Center (),
            Y = 0,
            BorderStyle = LineStyle.Dashed
        };
        _frameView.Padding.Add (label);
    }

}
