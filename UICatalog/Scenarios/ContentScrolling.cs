using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Content Scrolling", "Demonstrates using View.Viewport and View.GetContentSize () to scroll content.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Scrolling")]
public class ContentScrolling : Scenario
{
    private ViewDiagnosticFlags _diagnosticFlags;

    public class ScrollingDemoView : FrameView
    {
        public ScrollingDemoView ()
        {
            Width = Dim.Fill ();
            Height = Dim.Fill ();
            ColorScheme = Colors.ColorSchemes ["Base"];

            Text =
                "Text (ScrollingDemoView.Text). This is long text.\nThe second line.\n3\n4\n5th line\nLine 6. This is a longer line that should wrap automatically.";
            CanFocus = true;
            BorderStyle = LineStyle.Rounded;
            Arrangement = ViewArrangement.Fixed;

            SetContentSize (new (60, 40));
            ViewportSettings |= ViewportSettings.ClearContentOnly;
            ViewportSettings |= ViewportSettings.ClipContentOnly;

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
            Border.Add (new Label { X = 20 });
            LayoutComplete += VirtualDemoView_LayoutComplete;

            MouseEvent += VirtualDemoView_MouseEvent;
        }

        private void VirtualDemoView_MouseEvent (object sender, MouseEventEventArgs e)
        {
            if (e.MouseEvent.Flags == MouseFlags.WheeledDown)
            {
                ScrollVertical (1);

                return;
            }

            if (e.MouseEvent.Flags == MouseFlags.WheeledUp)
            {
                ScrollVertical (-1);

                return;
            }

            if (e.MouseEvent.Flags == MouseFlags.WheeledRight)
            {
                ScrollHorizontal (1);

                return;
            }

            if (e.MouseEvent.Flags == MouseFlags.WheeledLeft)
            {
                ScrollHorizontal (-1);
            }
        }

        private void VirtualDemoView_LayoutComplete (object sender, LayoutEventArgs e)
        {
            Label status = Border.Subviews.OfType<Label> ().FirstOrDefault ();

            if (status is { })
            {
                status.Title = $"Frame: {Frame}\n\nViewport: {Viewport}, ContentSize = {GetContentSize ()}";
                status.Width = Border.Frame.Width - status.Frame.X - Border.Thickness.Right;
                status.Height = Border.Thickness.Top;
            }

            SetNeedsDisplay ();
        }
    }

    public override void Main ()
    {
        Application.Init ();

        _diagnosticFlags = View.Diagnostics;

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",

            // Use a different colorscheme so ViewSettings.ClearContentOnly is obvious
            ColorScheme = Colors.ColorSchemes ["Toplevel"]
        };

        var editor = new AdornmentsEditor
        {
            AutoSelectViewToEdit = true
        };
        app.Add (editor);

        var view = new ScrollingDemoView
        {
            Title = "Demo View",
            X = Pos.Right (editor),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        app.Add (view);

        // Add Scroll Setting UI to Padding
        view.Padding.Thickness = new (0, 3, 0, 0);
        view.Padding.ColorScheme = Colors.ColorSchemes ["Error"];

        var cbAllowNegativeX = new CheckBox
        {
            Title = "Allow _X < 0",
            Y = 0,
            CanFocus = false
        };
        cbAllowNegativeX.Checked = view.ViewportSettings.HasFlag (ViewportSettings.AllowNegativeX);
        cbAllowNegativeX.Toggle += AllowNegativeX_Toggle;

        void AllowNegativeX_Toggle (object sender, CancelEventArgs<bool?> e)
        {
            if (e.NewValue == true)
            {
                view.ViewportSettings |= ViewportSettings.AllowNegativeX;
            }
            else
            {
                view.ViewportSettings &= ~ViewportSettings.AllowNegativeX;
            }
        }

        view.Padding.Add (cbAllowNegativeX);

        var cbAllowNegativeY = new CheckBox
        {
            Title = "Allow _Y < 0",
            X = Pos.Right (cbAllowNegativeX) + 1,
            Y = 0,
            CanFocus = false
        };
        cbAllowNegativeY.Checked = view.ViewportSettings.HasFlag (ViewportSettings.AllowNegativeY);
        cbAllowNegativeY.Toggle += AllowNegativeY_Toggle;

        void AllowNegativeY_Toggle (object sender, CancelEventArgs<bool?> e)
        {
            if (e.NewValue == true)
            {
                view.ViewportSettings |= ViewportSettings.AllowNegativeY;
            }
            else
            {
                view.ViewportSettings &= ~ViewportSettings.AllowNegativeY;
            }
        }

        view.Padding.Add (cbAllowNegativeY);

        var cbAllowXGreaterThanContentWidth = new CheckBox
        {
            Title = "All_ow X > Content",
            Y = Pos.Bottom (cbAllowNegativeX),
            CanFocus = false
        };
        cbAllowXGreaterThanContentWidth.Checked = view.ViewportSettings.HasFlag (ViewportSettings.AllowXGreaterThanContentWidth);
        cbAllowXGreaterThanContentWidth.Toggle += AllowXGreaterThanContentWidth_Toggle;

        void AllowXGreaterThanContentWidth_Toggle (object sender, CancelEventArgs<bool?> e)
        {
            if (e.NewValue == true)
            {
                view.ViewportSettings |= ViewportSettings.AllowXGreaterThanContentWidth;
            }
            else
            {
                view.ViewportSettings &= ~ViewportSettings.AllowXGreaterThanContentWidth;
            }
        }

        view.Padding.Add (cbAllowXGreaterThanContentWidth);

        var cbAllowYGreaterThanContentHeight = new CheckBox
        {
            Title = "Allo_w Y > Content",
            X = Pos.Right (cbAllowXGreaterThanContentWidth) + 1,
            Y = Pos.Bottom (cbAllowNegativeX),
            CanFocus = false
        };
        cbAllowYGreaterThanContentHeight.Checked = view.ViewportSettings.HasFlag (ViewportSettings.AllowYGreaterThanContentHeight);
        cbAllowYGreaterThanContentHeight.Toggle += AllowYGreaterThanContentHeight_Toggle;

        void AllowYGreaterThanContentHeight_Toggle (object sender, CancelEventArgs<bool?> e)
        {
            if (e.NewValue == true)
            {
                view.ViewportSettings |= ViewportSettings.AllowYGreaterThanContentHeight;
            }
            else
            {
                view.ViewportSettings &= ~ViewportSettings.AllowYGreaterThanContentHeight;
            }
        }

        view.Padding.Add (cbAllowYGreaterThanContentHeight);

        var labelContentSize = new Label
        {
            Title = "_ContentSize:",
            Y = Pos.Bottom (cbAllowYGreaterThanContentHeight)
        };

        Buttons.NumericUpDown<int> contentSizeWidth = new Buttons.NumericUpDown<int>
        {
            Value = view.GetContentSize ().Width,
            X = Pos.Right (labelContentSize) + 1,
            Y = Pos.Top (labelContentSize)
        };
        contentSizeWidth.ValueChanging += ContentSizeWidth_ValueChanged;

        void ContentSizeWidth_ValueChanged (object sender, CancelEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }
            // BUGBUG: set_ContentSize is supposed to be `protected`. 
            view.SetContentSize (view.GetContentSize () with { Width = e.NewValue });
        }

        var labelComma = new Label
        {
            Title = ",",
            X = Pos.Right (contentSizeWidth),
            Y = Pos.Top (labelContentSize)
        };

        Buttons.NumericUpDown<int> contentSizeHeight = new Buttons.NumericUpDown<int>
        {
            Value = view.GetContentSize ().Height,
            X = Pos.Right (labelComma) + 1,
            Y = Pos.Top (labelContentSize),
            CanFocus = false
        };
        contentSizeHeight.ValueChanging += ContentSizeHeight_ValueChanged;

        void ContentSizeHeight_ValueChanged (object sender, CancelEventArgs<int> e)
        {
            if (e.NewValue < 0)
            {
                e.Cancel = true;

                return;
            }
            // BUGBUG: set_ContentSize is supposed to be `protected`. 
            view.SetContentSize (view.GetContentSize () with { Height = e.NewValue });
        }

        var cbClearOnlyVisible = new CheckBox
        {
            Title = "ClearContentOnly",
            X = Pos.Right (contentSizeHeight) + 1,
            Y = Pos.Top (labelContentSize),
            CanFocus = false
        };
        cbClearOnlyVisible.Checked = view.ViewportSettings.HasFlag (ViewportSettings.ClearContentOnly);
        cbClearOnlyVisible.Toggle += ClearVisibleContentOnly_Toggle;

        void ClearVisibleContentOnly_Toggle (object sender, CancelEventArgs<bool?> e)
        {
            if (e.NewValue == true)
            {
                view.ViewportSettings |= ViewportSettings.ClearContentOnly;
            }
            else
            {
                view.ViewportSettings &= ~ViewportSettings.ClearContentOnly;
            }
        }

        var cbDoNotClipContent = new CheckBox
        {
            Title = "ClipContentOnly",
            X = Pos.Right (cbClearOnlyVisible) + 1,
            Y = Pos.Top (labelContentSize),
            CanFocus = false
        };
        cbDoNotClipContent.Checked = view.ViewportSettings.HasFlag (ViewportSettings.ClipContentOnly);
        cbDoNotClipContent.Toggle += ClipVisibleContentOnly_Toggle;

        void ClipVisibleContentOnly_Toggle (object sender, CancelEventArgs<bool?> e)
        {
            if (e.NewValue == true)
            {
                view.ViewportSettings |= ViewportSettings.ClipContentOnly;
            }
            else
            {
                view.ViewportSettings &= ~ViewportSettings.ClipContentOnly;
            }
        }

        view.Padding.Add (labelContentSize, contentSizeWidth, labelComma, contentSizeHeight, cbClearOnlyVisible, cbDoNotClipContent);

        // Add demo views to show that things work correctly
        var textField = new TextField { X = 20, Y = 7, Width = 15, Text = "Test TextField" };

        var colorPicker = new ColorPicker { Title = "BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd (), Y = 10 };
        colorPicker.BorderStyle = LineStyle.RoundedDotted;

        colorPicker.ColorChanged += (s, e) =>
                                    {
                                        colorPicker.SuperView.ColorScheme = new (colorPicker.SuperView.ColorScheme)
                                        {
                                            Normal = new (
                                                          colorPicker.SuperView.ColorScheme.Normal.Foreground,
                                                          e.Color
                                                         )
                                        };
                                    };

        var textView = new TextView
        {
            X = Pos.Center (),
            Y = 10,
            Title = "TextView",
            Text = "I have a 3 row top border.\nMy border inherits from the SuperView.\nI have 3 lines of text with room for 2.",
            AllowsTab = false,
            Width = 30,
            Height = 6 // TODO: Use Dim.Auto
        };
        textView.Border.Thickness = new (1, 3, 1, 1);

        var charMap = new CharMap
        {
            X = Pos.Center (),
            Y = Pos.Bottom (textView) + 1,
            Width = Dim.Auto (DimAutoStyle.Content, maximumContentDim: Dim.Func (() => view.GetContentSize ().Width)),
            Height = Dim.Auto (DimAutoStyle.Content, maximumContentDim: Dim.Percent (20)),
        };

        charMap.Accept += (s, e) =>
                              MessageBox.Query (20, 7, "Hi", $"Am I a {view.GetType ().Name}?", "Yes", "No");

        var buttonAnchored = new Button
        {
            X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Text = "Bottom Right"
        };

        view.Margin.Data = "Margin";
        view.Margin.Thickness = new (0);

        view.Border.Data = "Border";
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

        editor.Initialized += (s, e) => { editor.ViewToEdit = view; };

        app.Closed += (s, e) => View.Diagnostics = _diagnosticFlags;

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
