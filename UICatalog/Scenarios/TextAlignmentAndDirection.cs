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
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}"
        };

        var txt = $"Hello World{Environment.NewLine}HELLO WORLD{Environment.NewLine}世界 您好";

        var color1 = new ColorScheme { Normal = new (Color.Black, Color.Gray) };
        var color2 = new ColorScheme { Normal = new (Color.Black, Color.DarkGray) };

        List<Label> txts = new (); // single line
        List<Label> mtxts = new (); // multi line

        // Horizontal Single-Line 

        var labelHL = new Label
        {
            X = 1,
            Y = 1,
            Width = 9,
            Height = 1,
            TextAlignment = Alignment.Right,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Text = "Left"
        };

        var labelHC = new Label
        {
            X = 1,
            Y = 2,
            Width = 9,
            Height = 1,
            TextAlignment = Alignment.Right,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Text = "Centered"
        };

        var labelHR = new Label
        {
            X = 1,
            Y = 3,
            Width = 9,
            Height = 1,
            TextAlignment = Alignment.Right,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Text = "Right"
        };

        var labelHJ = new Label
        {
            X = 1,
            Y = 4,
            Width = 9,
            Height = 1,
            TextAlignment = Alignment.Right,
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Text = "Justified"
        };

        var txtLabelHL = new Label
        {
            X = Pos.Right (labelHL) + 1,
            Y = Pos.Y (labelHL),
            Width = Dim.Fill (1) - 9,
            Height = 1,
            ColorScheme = color1,
            TextAlignment = Alignment.Left,
            Text = txt
        };

        var txtLabelHC = new Label
        {
            X = Pos.Right (labelHC) + 1,
            Y = Pos.Y (labelHC),
            Width = Dim.Fill (1) - 9,
            Height = 1,
            ColorScheme = color2,
            TextAlignment = Alignment.Centered,
            Text = txt
        };

        var txtLabelHR = new Label
        {
            X = Pos.Right (labelHR) + 1,
            Y = Pos.Y (labelHR),
            Width = Dim.Fill (1) - 9,
            Height = 1,
            ColorScheme = color1,
            TextAlignment = Alignment.Right,
            Text = txt
        };

        var txtLabelHJ = new Label
        {
            X = Pos.Right (labelHJ) + 1,
            Y = Pos.Y (labelHJ),
            Width = Dim.Fill (1) - 9,
            Height = 1,
            ColorScheme = color2,
            TextAlignment = Alignment.Justified,
            Text = txt
        };

        txts.Add (txtLabelHL);
        txts.Add (txtLabelHC);
        txts.Add (txtLabelHR);
        txts.Add (txtLabelHJ);

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
            X = Pos.AnchorEnd (8),
            Y = 1,
            Width = 2,
            Height = 9,
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Bottom,
            Text = "Top"
        };
        labelVT.TextFormatter.WordWrap = false;

        var labelVM = new Label
        {
            X = Pos.AnchorEnd (6),
            Y = 1,
            Width = 2,
            Height = 9,
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Bottom,
            Text = "Centered"
        };
        labelVM.TextFormatter.WordWrap = false;

        var labelVB = new Label
        {
            X = Pos.AnchorEnd (4),
            Y = 1,
            Width = 2,
            Height = 9,
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Bottom,
            Text = "Bottom"
        };
        labelVB.TextFormatter.WordWrap = false;

        var labelVJ = new Label
        {
            X = Pos.AnchorEnd (2),
            Y = 1,
            Width = 2,
            Height = 9,
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Bottom,
            Text = "Justified"
        };
        labelVJ.TextFormatter.WordWrap = false;

        var txtLabelVT = new Label
        {
            X = Pos.X (labelVT),
            Y = Pos.Bottom (labelVT) + 1,
            Width = 2,
            Height = Dim.Fill (1),
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Top,
            Text = txt
        };
        txtLabelVT.TextFormatter.WordWrap = false;

        var txtLabelVM = new Label
        {
            X = Pos.X (labelVM),
            Y = Pos.Bottom (labelVM) + 1,
            Width = 2,
            Height = Dim.Fill (1),
            ColorScheme = color2,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Centered,
            Text = txt
        };
        txtLabelVM.TextFormatter.WordWrap = false;

        var txtLabelVB = new Label
        {
            X = Pos.X (labelVB),
            Y = Pos.Bottom (labelVB) + 1,
            Width = 2,
            Height = Dim.Fill (1),
            ColorScheme = color1,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Bottom,
            Text = txt
        };
        txtLabelVB.TextFormatter.WordWrap = false;

        var txtLabelVJ = new Label
        {
            X = Pos.X (labelVJ),
            Y = Pos.Bottom (labelVJ) + 1,
            Width = 2,
            Height = Dim.Fill (1),
            ColorScheme = color2,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Justified,
            Text = txt
        };
        txtLabelVJ.TextFormatter.WordWrap = false;

        txts.Add (txtLabelVT);
        txts.Add (txtLabelVM);
        txts.Add (txtLabelVB);
        txts.Add (txtLabelVJ);

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
            Height = Dim.Fill (4),
            ColorScheme = color2
        };

        var txtLabelTL = new Label
        {
            X = 1 /*                    */,
            Y = 1,
            Width = Dim.Percent (100f / 3f),
            Height = Dim.Percent (100f / 3f),
            TextAlignment = Alignment.Left,
            VerticalTextAlignment = Alignment.Top,
            ColorScheme = color1,
            Text = txt
        };
        txtLabelTL.TextFormatter.MultiLine = true;

        var txtLabelTC = new Label
        {
            X = Pos.Right (txtLabelTL) + 2,
            Y = 1,
            Width = Dim.Percent (100f / 3f),
            Height = Dim.Percent (100f / 3f),
            TextAlignment = Alignment.Centered,
            VerticalTextAlignment = Alignment.Top,
            ColorScheme = color1,
            Text = txt
        };
        txtLabelTC.TextFormatter.MultiLine = true;

        var txtLabelTR = new Label
        {
            X = Pos.Right (txtLabelTC) + 2,
            Y = 1,
            Width = Dim.Percent (100f, true),
            Height = Dim.Percent (100f / 3f),
            TextAlignment = Alignment.Right,
            VerticalTextAlignment = Alignment.Top,
            ColorScheme = color1,
            Text = txt
        };
        txtLabelTR.TextFormatter.MultiLine = true;

        var txtLabelML = new Label
        {
            X = Pos.X (txtLabelTL),
            Y = Pos.Bottom (txtLabelTL) + 1,
            Width = Dim.Width (txtLabelTL),
            Height = Dim.Percent (100f / 3f),
            TextAlignment = Alignment.Left,
            VerticalTextAlignment = Alignment.Centered,
            ColorScheme = color1,
            Text = txt
        };
        txtLabelML.TextFormatter.MultiLine = true;

        var txtLabelMC = new Label
        {
            X = Pos.X (txtLabelTC),
            Y = Pos.Bottom (txtLabelTC) + 1,
            Width = Dim.Width (txtLabelTC),
            Height = Dim.Percent (100f / 3f),
            TextAlignment = Alignment.Centered,
            VerticalTextAlignment = Alignment.Centered,
            ColorScheme = color1,
            Text = txt
        };
        txtLabelMC.TextFormatter.MultiLine = true;

        var txtLabelMR = new Label
        {
            X = Pos.X (txtLabelTR),
            Y = Pos.Bottom (txtLabelTR) + 1,
            Width = Dim.Percent (100f, true),
            Height = Dim.Percent (100f / 3f),
            TextAlignment = Alignment.Right,
            VerticalTextAlignment = Alignment.Centered,
            ColorScheme = color1,
            Text = txt
        };
        txtLabelMR.TextFormatter.MultiLine = true;

        var txtLabelBL = new Label
        {
            X = Pos.X (txtLabelML),
            Y = Pos.Bottom (txtLabelML) + 1,
            Width = Dim.Width (txtLabelML),
            Height = Dim.Percent (100f, true),
            TextAlignment = Alignment.Left,
            VerticalTextAlignment = Alignment.Bottom,
            ColorScheme = color1,
            Text = txt
        };
        txtLabelBL.TextFormatter.MultiLine = true;

        var txtLabelBC = new Label
        {
            X = Pos.X (txtLabelMC),
            Y = Pos.Bottom (txtLabelMC) + 1,
            Width = Dim.Width (txtLabelMC),
            Height = Dim.Percent (100f, true),
            TextAlignment = Alignment.Centered,
            VerticalTextAlignment = Alignment.Bottom,
            ColorScheme = color1,
            Text = txt
        };
        txtLabelBC.TextFormatter.MultiLine = true;

        var txtLabelBR = new Label
        {
            X = Pos.X (txtLabelMR),
            Y = Pos.Bottom (txtLabelMR) + 1,
            Width = Dim.Percent (100f, true),
            Height = Dim.Percent (100f, true),
            TextAlignment = Alignment.Right,
            VerticalTextAlignment = Alignment.Bottom,
            ColorScheme = color1,
            Text = txt
        };
        txtLabelBR.TextFormatter.MultiLine = true;

        mtxts.Add (txtLabelTL);
        mtxts.Add (txtLabelTC);
        mtxts.Add (txtLabelTR);
        mtxts.Add (txtLabelML);
        mtxts.Add (txtLabelMC);
        mtxts.Add (txtLabelMR);
        mtxts.Add (txtLabelBL);
        mtxts.Add (txtLabelBC);
        mtxts.Add (txtLabelBR);

        // Save Alignment in Data
        foreach (Label t in mtxts)
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

        var editText = new TextView
        {
            X = 1,
            Y = Pos.Bottom (container) + 1,
            Width = Dim.Fill (10),
            Height = Dim.Fill (1),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Text = txt
        };

        editText.MouseClick += (s, m) =>
                               {
                                   foreach (Label v in txts)
                                   {
                                       v.Text = editText.Text;
                                   }

                                   foreach (Label v in mtxts)
                                   {
                                       v.Text = editText.Text;
                                   }
                               };

        app.KeyUp += (s, m) =>
                     {
                         foreach (Label v in txts)
                         {
                             v.Text = editText.Text;
                         }

                         foreach (Label v in mtxts)
                         {
                             v.Text = editText.Text;
                         }
                     };

        editText.SetFocus ();

        app.Add (editText);

        // JUSTIFY CHECKBOX

        var justifyCheckbox = new CheckBox
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Y (container) + 1,
            Width = Dim.Fill (10),
            Height = 1,
            Text = "Justify"
        };

        app.Add (justifyCheckbox);

        // JUSTIFY OPTIONS

        var justifyOptions = new RadioGroup
        {
            X = Pos.Left (justifyCheckbox) + 1,
            Y = Pos.Y (justifyCheckbox) + 1,
            Width = Dim.Fill (11),
            RadioLabels = ["Current direction", "Opposite direction", "Justify Both"],
            Enabled = false
        };

        justifyCheckbox.Toggled += (s, e) => ToggleJustify (e.OldValue is { } && (bool)e.OldValue);

        justifyOptions.SelectedItemChanged += (s, e) =>
                                              {
                                                  ToggleJustify (false, true);
                                              };

        app.Add (justifyOptions);

        // WRAP CHECKBOX

        var wrapCheckbox = new CheckBox
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Bottom (justifyOptions),
            Width = Dim.Fill (10),
            Height = 1,
            Text = "Word Wrap",
        };
        wrapCheckbox.Checked = wrapCheckbox.TextFormatter.WordWrap;
        wrapCheckbox.Toggled += (s, e) =>
                                {
                                    if (e.OldValue == true)
                                    {
                                        foreach (Label t in mtxts)
                                        {
                                            t.TextFormatter.WordWrap = false;
                                        }
                                    }
                                    else
                                    {
                                        foreach (Label t in mtxts)
                                        {
                                            t.TextFormatter.WordWrap = true;
                                        }
                                    }
                                };

        app.Add (wrapCheckbox);

        // AUTOSIZE CHECKBOX

        var autoSizeCheckbox = new CheckBox
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Y (wrapCheckbox) + 1,
            Width = Dim.Fill (10),
            Height = 1,
            Text = "AutoSize",
        };
        autoSizeCheckbox.Checked = autoSizeCheckbox.TextFormatter.AutoSize;
        autoSizeCheckbox.Toggled += (s, e) =>
                                    {
                                        if (e.OldValue == true)
                                        {
                                            foreach (Label t in mtxts)
                                            {
                                                t.TextFormatter.AutoSize = false;
                                            }
                                        }
                                        else
                                        {
                                            foreach (Label t in mtxts)
                                            {
                                                t.TextFormatter.AutoSize = true;
                                            }
                                        }
                                    };

        app.Add (autoSizeCheckbox);

        // Direction Options

        List<TextDirection> directionsEnum = Enum.GetValues (typeof (TextDirection)).Cast<TextDirection> ().ToList ();

        var directionOptions = new RadioGroup
        {
            X = Pos.Right (container) + 1,
            Y = Pos.Bottom (autoSizeCheckbox) + 1,
            Width = Dim.Fill (10),
            Height = Dim.Fill (1),
            HotKeySpecifier = (Rune)'\xffff',
            RadioLabels = directionsEnum.Select (e => e.ToString ()).ToArray ()
        };

        directionOptions.SelectedItemChanged += (s, ev) =>
                                                {
                                                    var justChecked = justifyCheckbox.Checked is { } && (bool)justifyCheckbox.Checked;
                                                    if (justChecked)
                                                    {
                                                        ToggleJustify (true);
                                                    }
                                                    foreach (Label v in mtxts)
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

        void ToggleJustify (bool oldValue, bool wasJustOptions = false)
        {
            if (oldValue == true)
            {
                if (!wasJustOptions)
                {
                    justifyOptions.Enabled = false;
                }

                foreach (Label t in mtxts)
                {
                    t.TextAlignment = (Alignment)((dynamic)t.Data).h;
                    t.VerticalTextAlignment = (Alignment)((dynamic)t.Data).v;
                }
            }
            else
            {
                foreach (Label t in mtxts)
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
                                t.VerticalTextAlignment = Alignment.Justified;
                                t.TextAlignment = ((dynamic)t.Data).h;
                                break;
                            case 1:
                                t.VerticalTextAlignment = (Alignment)((dynamic)t.Data).v;
                                t.TextAlignment = Alignment.Justified;
                                break;
                            case 2:
                                t.VerticalTextAlignment = Alignment.Justified;
                                t.TextAlignment = Alignment.Justified;
                                break;
                        }
                    }
                    else
                    {
                        switch (justifyOptions.SelectedItem)
                        {
                            case 0:
                                t.TextAlignment = Alignment.Justified;
                                t.VerticalTextAlignment = ((dynamic)t.Data).v;
                                break;
                            case 1:
                                t.TextAlignment = (Alignment)((dynamic)t.Data).h;
                                t.VerticalTextAlignment = Alignment.Justified;
                                break;
                            case 2:
                                t.TextAlignment = Alignment.Justified;
                                t.VerticalTextAlignment = Alignment.Justified;
                                break;
                        }
                    }
                }
            }
        }
    }
}
