using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ViewportSettings", "Demonstrates manipulating Viewport, ViewportSettings, and ContentSize to scroll content.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Adornments")]
public class ViewportSettings : Scenario
{
    public class ViewportSettingsDemoView : FrameView
    {
        public ViewportSettingsDemoView ()
        {
            Id = "ViewportSettingsDemoView";
            Width = Dim.Fill ();
            Height = Dim.Fill ();
            base.ColorScheme = Colors.ColorSchemes ["Base"];

            base.Text =
                "Text (ViewportSettingsDemoView.Text). This is long text.\nThe second line.\n3\n4\n5th line\nLine 6. This is a longer line that should wrap automatically.";
            CanFocus = true;
            BorderStyle = LineStyle.Rounded;
            Arrangement = ViewArrangement.Resizable;

            SetContentSize (new (60, 40));
            ViewportSettings |= Terminal.Gui.ViewportSettings.ClearContentOnly;
            ViewportSettings |= Terminal.Gui.ViewportSettings.ClipContentOnly;
            VerticalScrollBar.Visible = true;

            // Things this view knows how to do
            AddCommand (Command.ScrollDown, () => ScrollVertical (1));
            AddCommand (Command.ScrollUp, () => ScrollVertical (-1));

            AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
            AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

            // Default keybindings for all ListViews
            KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
            KeyBindings.Add (Key.CursorDown, Command.ScrollDown);
            KeyBindings.Add (Key.CursorLeft, Command.ScrollLeft);
            KeyBindings.Add (Key.CursorRight, Command.ScrollRight);

            // Add a status label to the border that shows Viewport and ContentSize values. Bit of a hack.
            // TODO: Move to Padding with controls
            Border?.Add (new Label { X = 20 });

            ViewportChanged += VirtualDemoView_LayoutComplete;

            MouseEvent += VirtualDemoView_MouseEvent;
        }

        private void VirtualDemoView_MouseEvent (object sender, MouseEventArgs e)
        {
            if (e.Flags == MouseFlags.WheeledDown)
            {
                ScrollVertical (1);

                return;
            }

            if (e.Flags == MouseFlags.WheeledUp)
            {
                ScrollVertical (-1);

                return;
            }

            if (e.Flags == MouseFlags.WheeledRight)
            {
                ScrollHorizontal (1);

                return;
            }

            if (e.Flags == MouseFlags.WheeledLeft)
            {
                ScrollHorizontal (-1);
            }
        }

        private void VirtualDemoView_LayoutComplete (object sender, DrawEventArgs drawEventArgs)
        {
            Label frameLabel = Padding?.Subviews.OfType<Label> ().FirstOrDefault ();

            if (frameLabel is { })
            {
                frameLabel.Text = $"Viewport: {Viewport}\nFrame: {Frame}";
            }
        }
    }

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),

            // Use a different colorscheme so ViewSettings.ClearContentOnly is obvious
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            BorderStyle = LineStyle.None
        };

        var adornmentsEditor = new AdornmentsEditor
        {
            X = Pos.AnchorEnd(),
            AutoSelectViewToEdit = true,
            ShowViewIdentifier = true
        };
        app.Add (adornmentsEditor);

        ViewportSettingsEditor viewportSettingsEditor = new ViewportSettingsEditor ()
        {
            Y = Pos.AnchorEnd(),
            //X = Pos.Right (adornmentsEditor),
        };
        app.Add (viewportSettingsEditor);

        var view = new ViewportSettingsDemoView
        {
            Title = "ViewportSettings Demo View",
            Width = Dim.Fill (Dim.Func (() => app.IsInitialized ? adornmentsEditor.Frame.Width+1: 1)),
            Height = Dim.Fill (Dim.Func (() => app.IsInitialized ? viewportSettingsEditor.Frame.Height : 1))
        };

        app.Add (view);

        // Add demo views to show that things work correctly
        var textField = new TextField { X = 20, Y = 7, Width = 15, Text = "Test Te_xtField" };

        var colorPicker = new ColorPicker16 { Title = "_BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd (), Y = 10 };
        colorPicker.BorderStyle = LineStyle.RoundedDotted;

        colorPicker.ColorChanged += (s, e) =>
                                    {
                                        colorPicker.SuperView.ColorScheme = new (colorPicker.SuperView.ColorScheme)
                                        {
                                            Normal = new (
                                                          colorPicker.SuperView.ColorScheme.Normal.Foreground,
                                                          e.CurrentValue
                                                         )
                                        };
                                    };

        var textView = new TextView
        {
            X = Pos.Center (),
            Y = 10,
            Title = "TextVie_w",
            Text = "I have a 3 row top border.\nMy border inherits from the SuperView.\nI have 3 lines of text with room for 2.",
            AllowsTab = false,
            Width = 30,
            Height = 6 // TODO: Use Dim.Auto
        };
        textView.Border!.Thickness = new (1, 3, 1, 1);

        var charMap = new CharMap
        {
            X = Pos.Center (),
            Y = Pos.Bottom (textView) + 1,
            Width = Dim.Auto (DimAutoStyle.Content, maximumContentDim: Dim.Func (() => view.GetContentSize ().Width)),
            Height = Dim.Auto (DimAutoStyle.Content, maximumContentDim: Dim.Percent (20)),
        };

        charMap.Accepting += (s, e) =>
                              MessageBox.Query (20, 7, "Hi", $"Am I a {view.GetType ().Name}?", "Yes", "No");

        var buttonAnchored = new Button
        {
            X = Pos.AnchorEnd () - 10, Y = Pos.AnchorEnd () - 4, Text = "Bottom Rig_ht"
        };
        buttonAnchored.Accepting += (sender, args) => MessageBox.Query ("Hi", $"You pressed {((Button)sender)?.Text}", "_Ok");

        view.Margin!.Data = "Margin";
        view.Margin.Thickness = new (0);

        view.Border!.Data = "Border";
        view.Border.Thickness = new (3);

        view.Padding.Data = "Padding";

        view.Add (buttonAnchored, textField, colorPicker, charMap, textView);

        var longLabel = new Label
        {
            Id = "label2",
            X = 0,
            Y = 30,
            Text =
                "This label is long. It should clip to the ContentArea if ClipContentOnly is set. This is a virtual scrolling demo. Use the arrow keys and/or mouse wheel to scroll the content."
        };
        longLabel.TextFormatter.WordWrap = true;
        view.Add (longLabel);

        List<object> options = new () { "Option 1", "Option 2", "Option 3" };

        Slider slider = new (options)
        {
            X = 0,
            Y = Pos.Bottom (textField) + 1,
            Orientation = Orientation.Vertical,
            Type = SliderType.Multiple,
            AllowEmpty = false,
            BorderStyle = LineStyle.Double,
            Title = "_Slider"
        };
        view.Add (slider);

        adornmentsEditor.Initialized += (s, e) =>
                              {
                                  adornmentsEditor.ViewToEdit = view;
                              };

        adornmentsEditor.AutoSelectViewToEdit = true;
        adornmentsEditor.AutoSelectSuperView = view;
        adornmentsEditor.AutoSelectAdornments = false;

        view.Initialized += (s, e) =>
                                              {
                                                  viewportSettingsEditor.ViewToEdit = view;
                                              };
        view.SetFocus ();
        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    public override List<Key> GetDemoKeyStrokes ()
    {
        var keys = new List<Key> ();

        for (int i = 0; i < 50; i++)
        {
            keys.Add (Key.CursorRight);
        }

        for (int i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        for (int i = 0; i < 50; i++)
        {
            keys.Add (Key.CursorDown);
        }

        for (int i = 0; i < 25; i++)
        {
            keys.Add (Key.CursorUp);
        }

        return keys;
    }
}
