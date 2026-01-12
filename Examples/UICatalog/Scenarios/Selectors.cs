#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Selectors", "Demonstrates OptionSelector and FlagSelector.")]
[ScenarioCategory ("Controls")]
public sealed class Selectors : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        // Init
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        FrameView? optionSelectorsFrame = null;
        FrameView? flagSelectorsFrame = null;

        OptionSelector<Orientation> orientationSelector = new ()
        {
            Orientation = Orientation.Horizontal,
            BorderStyle = LineStyle.Dotted,
            Title = "Selector Or_ientation",
            Value = Orientation.Vertical
        };
        orientationSelector.ValueChanged += OrientationSelectorOnSelectedItemChanged;

        FlagSelector<SelectorStyles> stylesSelector = new ()
        {
            X = Pos.Right (orientationSelector) + 1,
            Orientation = Orientation.Horizontal,
            BorderStyle = LineStyle.Dotted,
            Title = "Selector St_yles"
        };
        stylesSelector.ValueChanged += StylesSelectorOnValueChanged;

        NumericUpDown<int> horizontalSpace = new ()
        {
            X = 0,
            Y = Pos.Bottom (orientationSelector),
            Width = 11,
            Title = "H_. Space",
            Value = stylesSelector.HorizontalSpace,
            BorderStyle = LineStyle.Dotted
        };
        horizontalSpace.ValueChanging += HorizontalSpaceOnValueChanging;

        CheckBox showBorderAndTitle = new ()
        {
            X = Pos.Right (horizontalSpace) + 1,
            Y = Pos.Top (horizontalSpace),
            Title = "Border _& Title",
            CheckedState = CheckState.Checked,
            BorderStyle = LineStyle.Dotted
        };
        showBorderAndTitle.CheckedStateChanged += ShowBorderAndTitleOnCheckedStateChanged;

        CheckBox canFocus = new ()
        {
            X = Pos.Right (showBorderAndTitle) + 1,
            Y = Pos.Top (horizontalSpace),
            Title = "_CanFocus",
            CheckedState = CheckState.Checked,
            BorderStyle = LineStyle.Dotted
        };
        canFocus.CheckedStateChanged += CanFocusOnCheckedStateChanged;

        optionSelectorsFrame = new ()
        {
            Y = Pos.Bottom (canFocus),
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
            Labels = ["Option _1 (0)", "Option _2 (1)", "Option _3 (5) 你", "Option _Quattro (4) 你"],
            Values = [0, 1, 5, 4],
            Arrangement = ViewArrangement.Resizable
        };
        optionSelectorsFrame.Add (label, optionSelector);

        label = new ()
        {
            Y = Pos.Bottom (optionSelector),
            Title = "<VisualRole_>:"
        };

        OptionSelector<VisualRole> optionSelectorT = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Bottom (optionSelector),
            Title = "<Vi_sualRole>",
            BorderStyle = LineStyle.Dotted,
            UsedHotKeys = optionSelector.UsedHotKeys,
            AssignHotKeys = true
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
            BorderStyle = LineStyle.Dotted,
            Title = "FlagSe_lector (uint)",
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
            UsedHotKeys = flagSelector.UsedHotKeys,
            AssignHotKeys = true,
            Value = View.Diagnostics
        };
        flagSelectorsFrame.Add (label, flagSelectorT);
        flagSelectorT.ValueChanged += (_, a) => { View.Diagnostics = (ViewDiagnosticFlags)a.Value!; };

        appWindow.Add (orientationSelector, stylesSelector, horizontalSpace, showBorderAndTitle, canFocus, optionSelectorsFrame, flagSelectorsFrame);

        // Run - Start the application.
        app.Run (appWindow);

        return;

        void OrientationSelectorOnSelectedItemChanged (object? sender, EventArgs<Orientation?> e)
        {
            if (sender is not OptionSelector<Orientation> s)
            {
                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.Orientation = s.Value!.Value;
            }
        }

        void StylesSelectorOnValueChanged (object? sender, EventArgs<SelectorStyles?> e)
        {
            if (sender is not FlagSelector<SelectorStyles> s)
            {
                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.Styles = s.Value!.Value;
            }
        }

        void HorizontalSpaceOnValueChanging (object? sender, CancelEventArgs<int> e)
        {
            if (sender is not NumericUpDown<int> || e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.HorizontalSpace = e.NewValue;
            }
        }

        void ShowBorderAndTitleOnCheckedStateChanged (object? sender, EventArgs<CheckState> e)
        {
            if (sender is not CheckBox cb)
            {
                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.Border!.Thickness = cb.CheckedState == CheckState.Checked ? new (1) : new Thickness (0);
            }
        }

        void CanFocusOnCheckedStateChanged (object? sender, EventArgs<CheckState> e)
        {
            if (sender is not CheckBox cb)
            {
                return;
            }

            List<SelectorBase> selectors = GetAllSelectors ();

            foreach (SelectorBase selector in selectors)
            {
                selector.CanFocus = cb.CheckedState == CheckState.Checked;
            }
        }

        List<SelectorBase> GetAllSelectors ()
        {
            List<SelectorBase> optionSelectors = [];

            // ReSharper disable once AccessToModifiedClosure
            optionSelectors.AddRange (optionSelectorsFrame!.SubViews.OfType<SelectorBase> ());

            // ReSharper disable once AccessToModifiedClosure
            optionSelectors.AddRange (flagSelectorsFrame!.SubViews.OfType<FlagSelector> ());

            return optionSelectors;
        }
    }
}
