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
        FrameView? flagSelectorsFrame = null;

        OptionSelector orientationSelector = new ()
        {
            Orientation = Orientation.Horizontal,
            Labels = new List<string> { "_Vertical", "_Horizontal" },
            BorderStyle = LineStyle.Dotted,
            Title = "Selector Or_ientation"
        };
        orientationSelector.ValueChanged += OrientationSelectorOnSelectedItemChanged;

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
            Title = "O_ptionSelectors",
            TabStop = TabBehavior.TabStop
        };

        Label label = new ()
        {
            Title = "Fo_ur Options:"
        };

        OptionSelector optionSelector = new ()
        {
            X = Pos.Right (label) + 1,
            Title = "Fou_r Options",
            BorderStyle = LineStyle.Dotted,
            UsedHotKeys = { label.HotKey },
            AssignHotKeys = true,
            Labels = ["Option _1 (0)", "Option _2 (1)", "Option _3 (5)", "Option _Quattro (4)"],
            Values = [0, 1, 5, 4],
            Styles = SelectorStyles.All
        };
        optionSelectorsFrame.Add (label, optionSelector);

        label = new ()
        {
            Y = Pos.Bottom (optionSelector),
            Title = "<VisualRole>:"
        };

        OptionSelector<VisualRole> optionSelectorT = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Bottom (optionSelector),
            Title = "<VisualRole>",
            BorderStyle = LineStyle.Dotted,
            //UsedHotKeys = optionSelector.UsedHotKeys,
            AssignHotKeys = true,
            Styles = SelectorStyles.All
        };

        optionSelectorsFrame.Add (label, optionSelectorT);

        flagSelectorsFrame = new ()
        {
            Y = Pos.Top (optionSelectorsFrame),
            X = Pos.Right (optionSelectorsFrame),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Title = "_FlagSelectors",
            TabStop = TabBehavior.TabStop
        };

        label = new ()
        {
            Title = "FlagSelector _(uint):"
        };

        FlagSelector flagSelector = new ()
        {
            X = Pos.Right (label) + 1,
            UsedHotKeys = optionSelectorT.UsedHotKeys,
            BorderStyle = LineStyle.Dotted,
            Title = "FlagSe_lector (uint)",
            Styles = SelectorStyles.All,
            AssignHotKeys = true,
            Values =
            [
                0b_0001,
                0b_0010,
                0b_0100,
                0b_1000,
                0b_1111
            ],
            Labels =
            [
                "0x0001 One",
                "0x0010 Two",
                "0x0100 Quattro",
                "0x1000 8",
                "0x1111 Fifteen"
            ]
        };
        flagSelectorsFrame.Add (label, flagSelector);

        label = new ()
        {
            Y = Pos.Bottom (flagSelector),
            Title = "_<ViewDiagnosticFlags>:"
        };

        FlagSelector<ViewDiagnosticFlags> flagSelectorT = new ()
        {
            X = Pos.Right (label) + 1,
            BorderStyle = LineStyle.Dotted,
            Title = "<ViewD_iagnosticFlags>",
            Y = Pos.Bottom (flagSelector),
            Styles = SelectorStyles.All,
            UsedHotKeys = flagSelector.UsedHotKeys,
            AssignHotKeys = true
        };
        flagSelectorsFrame.Add (label, flagSelectorT);

        appWindow.Add (orientationSelector, showBorderAndTitle, optionSelectorsFrame, flagSelectorsFrame);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();

        return;

        void OrientationSelectorOnSelectedItemChanged (object? sender, EventArgs<int?> e)
        {
            List<OptionSelector> optionSelectors = optionSelectorsFrame.SubViews.OfType<OptionSelector> ().ToList ();

            foreach (OptionSelector selector in optionSelectors)
            {
                selector.Orientation = orientationSelector.Value == 0 ? Orientation.Vertical : Orientation.Horizontal;
            }

            List<FlagSelector> flagsSelectors = flagSelectorsFrame.SubViews.OfType<FlagSelector> ().ToList ();

            foreach (FlagSelector selector in flagsSelectors)
            {
                selector.Orientation = orientationSelector.Value == 0 ? Orientation.Vertical : Orientation.Horizontal;
            }
        }

        void ShowBorderAndTitleOnCheckedStateChanged (object? sender, EventArgs<CheckState> e)
        {
            List<OptionSelector> optionSelectors = optionSelectorsFrame.SubViews.OfType<OptionSelector> ().ToList ();

            foreach (OptionSelector selector in optionSelectors)
            {
                selector.Border.Thickness = e.Value == CheckState.Checked ? new (1) : new Thickness (0);
            }

            List<FlagSelector> flagsSelectors = flagSelectorsFrame.SubViews.OfType<FlagSelector> ().ToList ();

            foreach (FlagSelector selector in flagsSelectors)
            {
                selector.Border.Thickness = e.Value == CheckState.Checked ? new (1) : new Thickness (0);
            }
        }
    }
}
