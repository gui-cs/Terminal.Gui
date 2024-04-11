using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Adornment Experiments", "Playground for Adornment experiments")]
[ScenarioCategory ("Controls")]
public class AdornmentExperiments : Scenario
{
    private ViewDiagnosticFlags _diagnosticFlags;

    private View _frameView;

    public override void Init ()
    {
        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

        _diagnosticFlags = View.Diagnostics;
        //View.Diagnostics = ViewDiagnosticFlags.MouseEnter;

        _frameView = new View ()
        {
            Title = "Frame View",
            X = 0,
            Y = 0,
            Width = Dim.Percent (90),
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

        var viewInPadding = new View ()
        {
            Title = "View in Padding",
            Text = "Text In View",
            X = Pos.Center (),
            Y = 0,
            Width = 30,
            Height = 10,
            BorderStyle = LineStyle.Dashed
        };
        viewInPadding.Border.Thickness = new (3, 3, 3, 3);
        viewInPadding.Initialized += ViewInPadding_Initialized;
        
        // add a subview to the subview of padding
        var subviewInSubview = new View ()
        {
            X = 0,
            Y = 1,
            Width = 10,
            Height = 1,
            Text = "Subview in Subview of Padding",
        };

        viewInPadding.Add (subviewInSubview);
        _frameView.Padding.Add (viewInPadding);


        void ViewInPadding_Initialized (object sender, System.EventArgs e)
        {

        }
    }

}
