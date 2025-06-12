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
            BorderStyle = LineStyle.None,
        };

        FrameView? optionSelectorsFrame = null;
        FrameView? flagSelectorsFrame = null;

        OptionSelector orientationSelector = new ()
        {
            Orientation = Orientation.Horizontal,
            Options = new List<string> () { "_Vertical", "_Horizontal" },
            BorderStyle = LineStyle.Dotted,
            Title = "Selector Or_ientation",
            SelectedItem = 0
        };
        orientationSelector.SelectedItemChanged += OrientationSelectorOnSelectedItemChanged;

        CheckBox showBorderAndTitle = new ()
        {
            X = Pos.Right (orientationSelector) + 1,
            Title = "Show Border _& Title",
            CheckedState = CheckState.Checked
        };
        showBorderAndTitle.CheckedStateChanged += ShowBorderAndTitleOnCheckedStateChanged;

        optionSelectorsFrame = new ()
        {
            Y = Pos.Bottom (orientationSelector),
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            Title = $"O_ptionSelectors",
            TabStop = TabBehavior.TabStop
        };

        Label label = new ()
        {
            Title = "Fo_ur Options:",
        };

        OptionSelector optionSelector = new ()
        {
            //X = Pos.Right(label) + 1,
            Title = "Fou_r Options",
            BorderStyle = LineStyle.Dotted,
            Options = new List<string> () { "Option _1", "Option _2", "Option _3", "Option _Quattro" },
            SelectedItem = 0,
        };
        optionSelectorsFrame.Add (label, optionSelector);

        flagSelectorsFrame = new ()
        {
            Y = Pos.Top (optionSelectorsFrame),
            X = Pos.Right (optionSelectorsFrame),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Title = $"_FlagSelectors",
            TabStop = TabBehavior.TabStop
        };

        label = new ()
        {
            Title = "FlagSelector _(uint):",
        };

        FlagSelector flagSelector = new ()
        {
            X = Pos.Right (label) + 1,
            BorderStyle = LineStyle.Dotted,
            Title = "FlagSe_lector (uint)",
            Styles = FlagSelectorStyles.All,
        };
        flagSelector.SetFlags (new Dictionary<uint, string>
            {
                { 0b_0001, "_0x0001 One" },
                { 0b_0010, "0x0010 T_wo" },
                { 0b_0100, "0_x0100 Quattro" },
                { 0b_1000, "0x1000 _Eight" },
                { 0b_1111, "0x1111 Fifteen" },
            });
        flagSelectorsFrame.Add (label, flagSelector);

        label = new ()
        {
            Y = Pos.Bottom(flagSelector),
            Title = "_<ViewDiagnosticFlags>:",
        };
        FlagSelector<ViewDiagnosticFlags> flagSelectorT = new ()
        {
            X = Pos.Right (label) + 1,
            BorderStyle = LineStyle.Dotted,
            Title = "<ViewD_iagnosticFlags>",
            Y = Pos.Bottom(flagSelector),
            Styles = FlagSelectorStyles.All,
            AssignHotKeysToCheckBoxes = true
        };
        flagSelectorsFrame.Add (label, flagSelectorT);

        appWindow.Add (orientationSelector, showBorderAndTitle, optionSelectorsFrame, flagSelectorsFrame);


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
            List<FlagSelector> flagsSelectors = flagSelectorsFrame.SubViews.OfType<FlagSelector> ().ToList ();

            foreach (FlagSelector selector in flagsSelectors)
            {
                selector.Orientation = orientationSelector.SelectedItem == 0 ? Orientation.Vertical : Orientation.Horizontal;
            }

        }

        void ShowBorderAndTitleOnCheckedStateChanged (object? sender, EventArgs<CheckState> e)
        {
            List<OptionSelector> optionSelectors = optionSelectorsFrame.SubViews.OfType<OptionSelector> ().ToList ();

            foreach (OptionSelector selector in optionSelectors)
            {
                selector.Border.Thickness = e.Value == CheckState.Checked ? new Thickness (1) : new Thickness (0);
            }
            List<FlagSelector> flagsSelectors = flagSelectorsFrame.SubViews.OfType<FlagSelector> ().ToList ();

            foreach (FlagSelector selector in flagsSelectors)
            {
                selector.Border.Thickness = e.Value == CheckState.Checked ? new Thickness (1) : new Thickness (0);
            }
        }
    }

}
