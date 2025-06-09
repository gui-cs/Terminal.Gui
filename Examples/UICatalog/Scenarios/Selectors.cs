#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Selectors", "Demonstrates OptionSelector and FlagSelector.")]
[ScenarioCategory ("Controls")]
public sealed class Selectors : Scenario
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

        FrameView? optionSelectorsFrame = null;

        OptionSelector orientationSelector = new ()
        {
            Orientation = Orientation.Horizontal,
            Options = new List<string> () { "_Vertical", "_Horizontal" },
            BorderStyle = LineStyle.Dotted,
            Title = "Selector Or_ientation",
            SelectedItem = 0
        };
        orientationSelector.SelectedItemChanged += OrientationSelectorOnSelectedItemChanged;

        optionSelectorsFrame = new ()
        {
            Y = Pos.Bottom (orientationSelector),
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            Title = $"_OptionSelectors",
        };

        Label optionSelectorLabel = new ()
        {
            Title = "Fo_ur Options:",

        };

        OptionSelector optionSelector = new ()
        {
            X = Pos.Right(optionSelectorLabel) + 1,
            Title = "Fou_r Options",
            BorderStyle = LineStyle.Dotted,
            Options = new List<string> () { "Option _1", "Option _2", "Option _3", "Option _Quattro" },
            SelectedItem = 0
        };
        optionSelectorsFrame.Add (optionSelectorLabel, optionSelector);

        FrameView flagSelectorsFrame = new ()
        {
            Y = Pos.Top (optionSelectorsFrame),
            X = Pos.Right (optionSelectorsFrame),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Title = $"_FlagSelectors",
        };

        appWindow.Add (orientationSelector, optionSelectorsFrame, flagSelectorsFrame);


        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();

        return;


        void OrientationSelectorOnSelectedItemChanged (object? sender, SelectedItemChangedArgs e)
        {
            List<OptionSelector> optionSelectors = optionSelectorsFrame.SubViews.OfType<OptionSelector> ().ToList ();

            foreach (OptionSelector selector in optionSelectors)
            {
                selector.Orientation = orientationSelector.SelectedItem == 0 ? Orientation.Vertical : Orientation.Horizontal;
            }
        }
    }

}
