using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Text Alignment and Direction", "Demos horizontal and vertical text alignment and direction.")]
[ScenarioCategory ("Text and Formatting")]
public class TextAlignmentAndDirection : Scenario
{

    internal class AlignmentAndDirectionView : View
    {
        public AlignmentAndDirectionView()
        {
            ViewportSettings = Terminal.Gui.ViewportSettings.Transparent;
            BorderStyle = LineStyle.Dotted;
        }
    }

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        var txt = $"Hello World{Environment.NewLine}HELLO WORLD{Environment.NewLine}世界 您好";

        var color1 = new ColorScheme { Normal = new (Color.Black, Color.Gray) };
        var color2 = new ColorScheme { Normal = new (Color.Black, Color.DarkGray) };

        List<View> singleLineLabels = new (); // single line
        List<View> multiLineLabels = new (); // multi line

        // Horizontal Single-Line 

        var labelHL = new Label
        {
            X = 0,
            Y = 0,
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Text = "Start",
        };

        var labelHC = new Label
        {
            X = 0,
            Y = 1,
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Text = "Center"
        };

        var labelHR = new Label
        {
            X = 0,
            Y = 2,
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Text = "End"
        };

        var labelHJ = new Label
        {
            X = 0,
            Y = 3,
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Text = "Fill"
        };

        var txtLabelHL = new View
        {
            X = Pos.Right (labelHL) + 1,
            Y = Pos.Y (labelHL),
            Width = Dim.Fill (9),
            Height = 1,
            ColorScheme = color1,
            TextAlignment = Alignment.Start,
            Text = txt,
            ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
        };

        var txtLabelHC = new View
        {
            X = Pos.Right (labelHC) + 1,
            Y = Pos.Y (labelHC),
            Width = Dim.Fill (9),
            Height = 1,
            ColorScheme = color2,
            TextAlignment = Alignment.Center,
            Text = txt,
            ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
        };

        var txtLabelHR = new View
        {
            X = Pos.Right (labelHR) + 1,
            Y = Pos.Y (labelHR),
            Width = Dim.Fill (9),
            Height = 1,
            ColorScheme = color1,
            TextAlignment = Alignment.End,
            Text = txt,
            ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
        };

        var txtLabelHJ = new View
        {
            X = Pos.Right (labelHJ) + 1,
            Y = Pos.Y (labelHJ),
            Width = Dim.Fill (9),
            Height = 1,
            ColorScheme = color2,
            TextAlignment = Alignment.Fill,
            Text = txt,
            ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
        };

        singleLineLabels.Add (txtLabelHL);
        singleLineLabels.Add (txtLabelHC);
        singleLineLabels.Add (txtLabelHR);
        singleLineLabels.Add (txtLabelHJ);

        app.Add (labelHL);
        app.Add (txtLabelHL);
        app.Add (labelHC);
        app.Add (txtLabelHC);
        app.Add (labelHR);
        app.Add (txtLabelHR);
        app.Add (labelHJ);
        app.Add (txtLabelHJ);

        // Vertical Single-Line

        var labelVT = new Label
        {
            X = Pos.AnchorEnd () - 6,
            Y = 0,
            Width = 2,
            Height = 6,
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = "Start"
        };
        labelVT.TextFormatter.WordWrap = false;

        var labelVM = new Label
        {
            X = Pos.AnchorEnd () - 4,
            Y = 0,
            Width = 2,
            Height = 6,
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = "Center"
        };
        labelVM.TextFormatter.WordWrap = false;

        var labelVB = new Label
        {
            X = Pos.AnchorEnd () - 2,
            Y = 0,
            Width = 2,
            Height = 6,
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = "End"
        };
        labelVB.TextFormatter.WordWrap = false;

        var labelVJ = new Label
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            Width = 2,
            Height = 6,
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = "Fill"
        };
        labelVJ.TextFormatter.WordWrap = false;

        var txtLabelVT = new View
        {
            X = Pos.X (labelVT),
            Y = Pos.Bottom (labelVT) + 1,
            Width = 2,
            Height = Dim.Fill (),
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Start,
            Text = txt,
            ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
        };
        txtLabelVT.TextFormatter.WordWrap = false;

        var txtLabelVM = new View
        {
            X = Pos.X (labelVM),
            Y = Pos.Bottom (labelVM) + 1,
            Width = 2,
            Height = Dim.Fill (),
            ColorScheme = color2,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Center,
            Text = txt,
            ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
        };
        txtLabelVM.TextFormatter.WordWrap = false;

        var txtLabelVB = new View
        {
            X = Pos.X (labelVB),
            Y = Pos.Bottom (labelVB) + 1,
            Width = 2,
            Height = Dim.Fill (),
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End,
            Text = txt,
            ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
        };
        txtLabelVB.TextFormatter.WordWrap = false;

        var txtLabelVJ = new View
        {
            X = Pos.X (labelVJ),
            Y = Pos.Bottom (labelVJ) + 1,
            Width = 2,
            Height = Dim.Fill (),
            ColorScheme = color2,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Fill,
            Text = txt,
            ViewportSettings = Terminal.Gui.ViewportSettings.Transparent
        };
        txtLabelVJ.TextFormatter.WordWrap = false;

        singleLineLabels.Add (txtLabelVT);
        singleLineLabels.Add (txtLabelVM);
        singleLineLabels.Add (txtLabelVB);
        singleLineLabels.Add (txtLabelVJ);

        app.Add (labelVT);
        app.Add (txtLabelVT);
        app.Add (labelVM);
        app.Add (txtLabelVM);
        app.Add (labelVB);
        app.Add (txtLabelVB);
        app.Add (labelVJ);
        app.Add (txtLabelVJ);

        // Multi-Line

        var container = new View
        {
            X = 0,
            Y = Pos.Bottom (txtLabelHJ),
            Width = Dim.Fill (31),
            Height = Dim.Fill (4)

            //ColorScheme = color2
        };

        var txtLabelTL = new AlignmentAndDirectionView
        {
            X = 0,
            Y = 1,
            Width = Dim.Percent (100 / 3),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.Start,
            VerticalTextAlignment = Alignment.Start,
            ColorScheme = color1,
            Text = txt,
        };
        txtLabelTL.TextFormatter.MultiLine = true;

        var txtLabelTC = new AlignmentAndDirectionView
        {
            X = Pos.Right (txtLabelTL),
            Y = 1,
            Width = Dim.Percent (100 / 3),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.Start,
            ColorScheme = color1,
            Text = txt,
        };
        txtLabelTC.TextFormatter.MultiLine = true;

        var txtLabelTR = new AlignmentAndDirectionView
        {
            X = Pos.Right (txtLabelTC),
            Y = 1,
            Width = Dim.Percent (100, DimPercentMode.Position),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.End,
            VerticalTextAlignment = Alignment.Start,
            ColorScheme = color1,
            Text = txt,
        };
        txtLabelTR.TextFormatter.MultiLine = true;

        var txtLabelML = new AlignmentAndDirectionView
        {
            X = Pos.X (txtLabelTL),
            Y = Pos.Bottom (txtLabelTL),
            Width = Dim.Width (txtLabelTL),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.Start,
            VerticalTextAlignment = Alignment.Center,
            ColorScheme = color1,
            Text = txt,
        };
        txtLabelML.TextFormatter.MultiLine = true;

        var txtLabelMC = new AlignmentAndDirectionView
        {
            X = Pos.X (txtLabelTC),
            Y = Pos.Bottom (txtLabelTC),
            Width = Dim.Width (txtLabelTC),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.Center,
            ColorScheme = color1,
            Text = txt,
        };
        txtLabelMC.TextFormatter.MultiLine = true;

        var txtLabelMR = new AlignmentAndDirectionView
        {
            X = Pos.X (txtLabelTR),
            Y = Pos.Bottom (txtLabelTR),
            Width = Dim.Percent (100, DimPercentMode.Position),
            Height = Dim.Percent (100 / 3),
            TextAlignment = Alignment.End,
            VerticalTextAlignment = Alignment.Center,
            ColorScheme = color1,
            Text = txt,
        };
        txtLabelMR.TextFormatter.MultiLine = true;

        var txtLabelBL = new AlignmentAndDirectionView
        {
            X = Pos.X (txtLabelML),
            Y = Pos.Bottom (txtLabelML),
            Width = Dim.Width (txtLabelML),
            Height = Dim.Percent (100, DimPercentMode.Position),
            TextAlignment = Alignment.Start,
            VerticalTextAlignment = Alignment.End,
            ColorScheme = color1,
            Text = txt,
        };
        txtLabelBL.TextFormatter.MultiLine = true;

        var txtLabelBC = new AlignmentAndDirectionView
        {
            X = Pos.X (txtLabelMC),
            Y = Pos.Bottom (txtLabelMC),
            Width = Dim.Width (txtLabelMC),
            Height = Dim.Percent (100, DimPercentMode.Position),
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.End,
            ColorScheme = color1,
            Text = txt,
        };
        txtLabelBC.TextFormatter.MultiLine = true;

        var txtLabelBR = new AlignmentAndDirectionView
        {
            X = Pos.X (txtLabelMR),
            Y = Pos.Bottom(txtLabelMR),
            Width = Dim.Percent (100, DimPercentMode.Position),
            Height = Dim.Percent (100, DimPercentMode.Position),
            TextAlignment = Alignment.End,
            VerticalTextAlignment = Alignment.End,
            ColorScheme = color1,
            Text = txt,
        };
        txtLabelBR.TextFormatter.MultiLine = true;

        multiLineLabels.Add (txtLabelTL);
        multiLineLabels.Add (txtLabelTC);
        multiLineLabels.Add (txtLabelTR);
        multiLineLabels.Add (txtLabelML);
        multiLineLabels.Add (txtLabelMC);
        multiLineLabels.Add (txtLabelMR);
        multiLineLabels.Add (txtLabelBL);
        multiLineLabels.Add (txtLabelBC);
        multiLineLabels.Add (txtLabelBR);

        // Save Alignment in Data
        foreach (View t in multiLineLabels)
        {
            t.Data = new { h = t.TextAlignment, v = t.VerticalTextAlignment };
        }

        container.Add (txtLabelTL);
        container.Add (txtLabelTC);
        container.Add (txtLabelTR);

        container.Add (txtLabelML);
        container.Add (txtLabelMC);
        container.Add (txtLabelMR);

        container.Add (txtLabelBL);
        container.Add (txtLabelBC);
        container.Add (txtLabelBR);

        app.Add (container);

        // Edit Text

        var label = new Label
        {
            X = 1,
            Y = Pos.Bottom (container) + 1,
            Width = 10,
            Height = 1,
            Text = "Edit Text:"
        };

        var editText = new TextView
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (31),
            Height = 3,
            Text = txt
        };

        editText.MouseClick += (s, m) =>
                               {
                                   foreach (View v in singleLineLabels)
                                   {
                                       v.Text = editText.Text;
                                   }

                                   foreach (View v in multiLineLabels)
                                   {
                                       v.Text = editText.Text;
                                   }
                               };

        app.KeyUp += (s, m) =>
                     {
                         foreach (View v in singleLineLabels)
                         {
                             v.Text = editText.Text;
                         }

                         foreach (View v in multiLineLabels)
                         {
                             v.Text = editText.Text;
                         }
                     };

        editText.SetFocus ();

        app.Add (label, editText);

        // JUSTIFY CHECKBOX

        var justifyCheckbox = new CheckBox
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Y (container) + 1,
            Width = Dim.Fill (10),
            Height = 1,
            Text = "Fill"
        };

        app.Add (justifyCheckbox);

        // JUSTIFY OPTIONS

        var justifyOptions = new RadioGroup
        {
            X = Pos.Left (justifyCheckbox) + 1,
            Y = Pos.Y (justifyCheckbox) + 1,
            Width = Dim.Fill (9),
            RadioLabels = ["Current direction", "Opposite direction", "FIll Both"],
            Enabled = false
        };

        justifyCheckbox.CheckedStateChanging += (s, e) => ToggleJustify (e.NewValue != CheckState.Checked);

        justifyOptions.SelectedItemChanged += (s, e) => { ToggleJustify (false, true); };

        app.Add (justifyOptions);

        // WRAP CHECKBOX

        var wrapCheckbox = new CheckBox
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Bottom (justifyOptions),
            Width = Dim.Fill (10),
            Height = 1,
            Text = "Word Wrap"
        };
        wrapCheckbox.CheckedState = wrapCheckbox.TextFormatter.WordWrap ? CheckState.Checked : CheckState.UnChecked;

        wrapCheckbox.CheckedStateChanging += (s, e) =>
                                {
                                    if (e.CurrentValue == CheckState.Checked)
                                    {
                                        foreach (View t in multiLineLabels)
                                        {
                                            t.TextFormatter.WordWrap = false;
                                        }
                                    }
                                    else
                                    {
                                        foreach (View t in multiLineLabels)
                                        {
                                            t.TextFormatter.WordWrap = true;
                                        }
                                    }
                                };

        app.Add (wrapCheckbox);

        List<TextDirection> directionsEnum = Enum.GetValues (typeof (TextDirection)).Cast<TextDirection> ().ToList ();

        var directionOptions = new RadioGroup
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Bottom (wrapCheckbox) + 1,
            Width = Dim.Fill (10),
            Height = Dim.Fill (1),
            HotKeySpecifier = (Rune)'\xffff',
            RadioLabels = directionsEnum.Select (e => e.ToString ()).ToArray ()
        };

        directionOptions.SelectedItemChanged += (s, ev) =>
                                                {
                                                    bool justChecked = justifyCheckbox.CheckedState == CheckState.Checked;

                                                    if (justChecked)
                                                    {
                                                        ToggleJustify (true);
                                                    }

                                                    foreach (View v in multiLineLabels)
                                                    {
                                                        v.TextDirection = (TextDirection)ev.SelectedItem;
                                                    }

                                                    if (justChecked)
                                                    {
                                                        ToggleJustify (false);
                                                    }
                                                };

        app.Add (directionOptions);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();

        void ToggleJustify (bool oldValue, bool wasJustOptions = false)
        {
            if (oldValue)
            {
                if (!wasJustOptions)
                {
                    justifyOptions.Enabled = false;
                }

                foreach (View t in multiLineLabels)
                {
                    t.TextAlignment = (Alignment)((dynamic)t.Data).h;
                    t.VerticalTextAlignment = (Alignment)((dynamic)t.Data).v;
                }
            }
            else
            {
                foreach (View t in multiLineLabels)
                {
                    if (!wasJustOptions)
                    {
                        justifyOptions.Enabled = true;
                    }

                    if (TextFormatter.IsVerticalDirection (t.TextDirection))
                    {
                        switch (justifyOptions.SelectedItem)
                        {
                            case 0:
                                t.VerticalTextAlignment = Alignment.Fill;
                                t.TextAlignment = ((dynamic)t.Data).h;

                                break;
                            case 1:
                                t.VerticalTextAlignment = (Alignment)((dynamic)t.Data).v;
                                t.TextAlignment = Alignment.Fill;

                                break;
                            case 2:
                                t.VerticalTextAlignment = Alignment.Fill;
                                t.TextAlignment = Alignment.Fill;

                                break;
                        }
                    }
                    else
                    {
                        switch (justifyOptions.SelectedItem)
                        {
                            case 0:
                                t.TextAlignment = Alignment.Fill;
                                t.VerticalTextAlignment = ((dynamic)t.Data).v;

                                break;
                            case 1:
                                t.TextAlignment = (Alignment)((dynamic)t.Data).h;
                                t.VerticalTextAlignment = Alignment.Fill;

                                break;
                            case 2:
                                t.TextAlignment = Alignment.Fill;
                                t.VerticalTextAlignment = Alignment.Fill;

                                break;
                        }
                    }
                }
            }
        }
    }
}
