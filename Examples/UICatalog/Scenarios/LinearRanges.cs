using System.Collections.ObjectModel;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("LinearRanges", "Demonstrates the LinearRange view.")]
[ScenarioCategory ("Controls")]
public class LinearRanges : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ();
        mainWindow.Title = GetQuitKeyAndName ();

        MakeSliders (
                     mainWindow,
                     [
                         500,
                         1000,
                         1500,
                         2000,
                         2500,
                         3000,
                         3500,
                         4000,
                         4500,
                         5000
                     ]
                    );

        FrameView configView = new ()
        {
            Title = "Confi_guration",
            X = Pos.Percent (50),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SchemeName = "Dialog"
        };

        mainWindow.Add (configView);

        #region Config LinearRange

        LinearRange<string> optionsSlider = new ()
        {
            Title = "Options",
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Type = LinearRangeType.Multiple,
            AllowEmpty = true,
            BorderStyle = LineStyle.Single
        };

        optionsSlider.Style.SetChar = optionsSlider.Style.SetChar with { Attribute = new Attribute (Color.BrightGreen, Color.Black) };
        optionsSlider.Style.LegendAttributes.SetAttribute = new Attribute (Color.Green, Color.Black);

        optionsSlider.Options =
        [
            new () { Legend = "Legends" },
            new () { Legend = "RangeAllowSingle" },
            new () { Legend = "EndSpacing" },
            new () { Legend = "DimAuto" }
        ];

        configView.Add (optionsSlider);

        optionsSlider.OptionsChanged += OnOptionsSliderOnOptionsChanged;
        optionsSlider.SetOption (0); // Legends
        optionsSlider.SetOption (1); // RangeAllowSingle
        optionsSlider.SetOption (3); // DimAuto

        CheckBox dimAutoUsesMin = new ()
        {
            Text = "Use minimum size (vs. ideal)",
            X = 0,
            Y = Pos.Bottom (optionsSlider)
        };

        dimAutoUsesMin.CheckedStateChanging += (_, _) =>
                                               {
                                                   foreach (LinearRange s in mainWindow.SubViews.OfType<LinearRange> ())
                                                   {
                                                       s.UseMinimumSize = !s.UseMinimumSize;
                                                   }
                                               };
        configView.Add (dimAutoUsesMin);

        #region LinearRange Orientation LinearRange

        LinearRange<string> orientationSlider = new (new () { "Horizontal", "Vertical" })
        {
            Title = "LinearRange Orientation",
            X = 0,
            Y = Pos.Bottom (dimAutoUsesMin) + 1,
            BorderStyle = LineStyle.Single
        };

        orientationSlider.SetOption (0);

        configView.Add (orientationSlider);

        orientationSlider.OptionsChanged += OnOrientationSliderOnOptionsChanged;

        #endregion LinearRange Orientation LinearRange

        #region Legends Orientation LinearRange

        LinearRange<string> legendsOrientationSlider = new (["Horizontal", "Vertical"])
        {
            Title = "Legends Orientation",
            X = 0,
            Y = Pos.Bottom (orientationSlider) + 1,
            BorderStyle = LineStyle.Single
        };

        legendsOrientationSlider.SetOption (0);

        configView.Add (legendsOrientationSlider);

        legendsOrientationSlider.OptionsChanged += OnLegendsOrientationSliderOnOptionsChanged;

        #endregion Legends Orientation LinearRange

        #region Spacing Options

        FrameView spacingOptions = new ()
        {
            Title = "Spacing Options",
            X = Pos.Right (orientationSlider),
            Y = Pos.Top (orientationSlider),
            Width = Dim.Fill (),
            Height = Dim.Auto (),
            BorderStyle = LineStyle.Single
        };

        Label label = new ()
        {
            Text = "Min _Inner Spacing:"
        };

        NumericUpDown<int> innerSpacingUpDown = new ()
        {
            X = Pos.Right (label) + 1
        };

        innerSpacingUpDown.Value = mainWindow.SubViews.OfType<LinearRange> ().First ().MinimumInnerSpacing;

        innerSpacingUpDown.ValueChanging += (_, e) =>
                                            {
                                                if (e.NewValue < 0)
                                                {
                                                    e.Cancel = true;

                                                    return;
                                                }

                                                foreach (LinearRange s in mainWindow.SubViews.OfType<LinearRange> ())
                                                {
                                                    s.MinimumInnerSpacing = e.NewValue;
                                                }
                                            };

        spacingOptions.Add (label, innerSpacingUpDown);
        configView.Add (spacingOptions);

        #endregion

        #region Color LinearRange

        foreach (LinearRange s in mainWindow.SubViews.OfType<LinearRange> ())
        {
            s.Style.OptionChar = s.Style.OptionChar with { Attribute = mainWindow.GetAttributeForRole (VisualRole.Normal) };
            s.Style.SetChar = s.Style.SetChar with { Attribute = mainWindow.GetAttributeForRole (VisualRole.Normal) };
            s.Style.LegendAttributes.SetAttribute = mainWindow.GetAttributeForRole (VisualRole.Normal);
            s.Style.RangeChar = s.Style.RangeChar with { Attribute = mainWindow.GetAttributeForRole (VisualRole.Normal) };
        }

        LinearRange<(Color, Color)> sliderFgColor = new ()
        {
            Title = "FG Color",
            X = 0,
            Y = Pos.Bottom (legendsOrientationSlider)
                + 1,
            Type = LinearRangeType.Single,
            BorderStyle = LineStyle.Single,
            AllowEmpty = false,
            Orientation = Orientation.Vertical,
            LegendsOrientation = Orientation.Horizontal,
            MinimumInnerSpacing = 0,
            UseMinimumSize = true
        };

        sliderFgColor.Style.SetChar = sliderFgColor.Style.SetChar with { Attribute = new Attribute (Color.BrightGreen, Color.Black) };
        sliderFgColor.Style.LegendAttributes.SetAttribute = new Attribute (Color.Green, Color.Blue);

        List<LinearRangeOption<(Color, Color)>> colorOptions = [];

        colorOptions.AddRange (
                               from colorIndex in Enum.GetValues<ColorName16> ()
                               let colorName = colorIndex.ToString ()
                               select new LinearRangeOption<(Color, Color)>
                                   { Data = (new (colorIndex), new (colorIndex)), Legend = colorName, LegendAbbr = (Rune)colorName [0] });

        sliderFgColor.Options = colorOptions;

        configView.Add (sliderFgColor);

        sliderFgColor.OptionsChanged += OnSliderFgColorOnOptionsChanged;

        LinearRange<(Color, Color)> sliderBgColor = new ()
        {
            Title = "BG Color",
            X = Pos.Right (sliderFgColor),
            Y = Pos.Top (sliderFgColor),
            Type = LinearRangeType.Single,
            BorderStyle = LineStyle.Single,
            AllowEmpty = false,
            Orientation = Orientation.Vertical,
            LegendsOrientation = Orientation.Horizontal,
            MinimumInnerSpacing = 0,
            UseMinimumSize = true
        };

        sliderBgColor.Style.SetChar = sliderBgColor.Style.SetChar with { Attribute = new Attribute (Color.BrightGreen, Color.Black) };
        sliderBgColor.Style.LegendAttributes.SetAttribute = new Attribute (Color.Green, Color.Blue);

        sliderBgColor.Options = colorOptions;

        configView.Add (sliderBgColor);

        sliderBgColor.OptionsChanged += (_, e) =>
                                        {
                                            if (e.Options.Count == 0)
                                            {
                                                return;
                                            }

                                            (Color, Color) data = e.Options.First ().Value.Data;

                                            foreach (LinearRange s in mainWindow.SubViews.OfType<LinearRange> ())
                                            {
                                                s.SetScheme (
                                                             new (s.GetScheme ())
                                                             {
                                                                 Normal = new (
                                                                               s.GetAttributeForRole (VisualRole.Normal).Foreground,
                                                                               data.Item2
                                                                              )
                                                             });
                                            }
                                        };

        #endregion Color LinearRange

        #endregion Config LinearRange

        ObservableCollection<string> eventSource = [];

        ListView eventLog = new ()
        {
            X = Pos.Right (sliderBgColor),
            Y = Pos.Bottom (spacingOptions),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SchemeName = "Runnable",
            Source = new ListWrapper<string> (eventSource)
        };
        configView.Add (eventLog);

        foreach (View view in mainWindow.SubViews.Where (v => v is LinearRange)!)
        {
            var slider = (LinearRange)view;

            slider.Accepting += (_, args) =>
                                {
                                    eventSource.Add ($"Accept: {string.Join (",", slider.GetSetOptions ())}");
                                    eventLog.MoveDown ();
                                    args.Handled = true;
                                };

            slider.OptionsChanged += (_, args) =>
                                     {
                                         eventSource.Add ($"OptionsChanged: {string.Join (",", slider.GetSetOptions ())}");
                                         eventLog.MoveDown ();
                                         args.Cancel = true;
                                     };
        }

        mainWindow.FocusDeepest (NavigationDirection.Forward, null);

        app.Run (mainWindow);

        return;

        void OnSliderFgColorOnOptionsChanged (object _, LinearRangeEventArgs<(Color, Color)> e)
        {
            if (e.Options.Count == 0)
            {
                return;
            }

            (Color, Color) data = e.Options.First ().Value.Data;

            foreach (LinearRange s in mainWindow.SubViews.OfType<LinearRange> ())
            {
                s.SetScheme (
                             new (s.GetScheme ())
                             {
                                 Normal = new (
                                               data.Item2,
                                               s.GetAttributeForRole (VisualRole.Normal).Background,
                                               s.GetAttributeForRole (VisualRole.Normal).Style)
                             });

                s.Style.OptionChar = s.Style.OptionChar with
                {
                    Attribute = new Attribute (
                                               data.Item1,
                                               s.GetAttributeForRole (VisualRole.Normal).Background,
                                               s.GetAttributeForRole (VisualRole.Normal).Style)
                };

                s.Style.SetChar = s.Style.SetChar with
                {
                    Attribute = new Attribute (
                                               data.Item1,
                                               s.Style.SetChar.Attribute?.Background
                                               ?? s.GetAttributeForRole (VisualRole.Normal).Background,
                                               s.Style.SetChar.Attribute?.Style
                                               ?? s.GetAttributeForRole (VisualRole.Normal).Style)
                };

                s.Style.LegendAttributes.SetAttribute = new Attribute (
                                                                       data.Item1,
                                                                       s.GetAttributeForRole (VisualRole.Normal).Background,
                                                                       s.GetAttributeForRole (VisualRole.Normal).Style);

                s.Style.RangeChar = s.Style.RangeChar with
                {
                    Attribute = new Attribute (
                                               data.Item1,
                                               s.GetAttributeForRole (VisualRole.Normal).Background,
                                               s.GetAttributeForRole (VisualRole.Normal).Style)
                };

                s.Style.SpaceChar = s.Style.SpaceChar with
                {
                    Attribute = new Attribute (
                                               data.Item1,
                                               s.GetAttributeForRole (VisualRole.Normal).Background,
                                               s.GetAttributeForRole (VisualRole.Normal).Style)
                };

                s.Style.LegendAttributes.NormalAttribute = new Attribute (
                                                                          data.Item1,
                                                                          s.GetAttributeForRole (VisualRole.Normal).Background,
                                                                          s.GetAttributeForRole (VisualRole.Normal).Style);
            }
        }

        void OnLegendsOrientationSliderOnOptionsChanged (object _, LinearRangeEventArgs<string> e)
        {
            foreach (LinearRange s in mainWindow.SubViews.OfType<LinearRange> ())
            {
                if (e.Options.ContainsKey (0))
                {
                    s.LegendsOrientation = Orientation.Horizontal;
                }
                else if (e.Options.ContainsKey (1))
                {
                    s.LegendsOrientation = Orientation.Vertical;
                }

                if (optionsSlider.GetSetOptions ().Contains (3))
                {
                    s.Width = Dim.Auto (DimAutoStyle.Content);
                    s.Height = Dim.Auto (DimAutoStyle.Content);
                }
                else
                {
                    if (s.Orientation == Orientation.Horizontal)
                    {
                        s.Width = Dim.Percent (50);

                        int h = s.ShowLegends && s.LegendsOrientation == Orientation.Vertical
                                    ? s.Options.Max (o => o.Legend!.Length) + 3
                                    : 4;
                        s.Height = h;
                    }
                    else
                    {
                        int w = s.ShowLegends ? s.Options.Max (o => o.Legend!.Length) + 3 : 3;
                        s.Width = w;
                        s.Height = Dim.Fill ();
                    }
                }
            }
        }

        void OnOrientationSliderOnOptionsChanged (object _, LinearRangeEventArgs<string> e)
        {
            View prev = null;

            foreach (LinearRange s in mainWindow.SubViews.OfType<LinearRange> ())
            {
                if (e.Options.ContainsKey (0))
                {
                    s.Orientation = Orientation.Horizontal;

                    s.Style.SpaceChar = new () { Grapheme = Glyphs.HLine.ToString () };

                    if (prev == null)
                    {
                        s.Y = 0;
                    }
                    else
                    {
                        s.Y = Pos.Bottom (prev) + 1;
                    }

                    s.X = 0;
                    prev = s;
                }
                else if (e.Options.ContainsKey (1))
                {
                    s.Orientation = Orientation.Vertical;

                    s.Style.SpaceChar = new () { Grapheme = Glyphs.VLine.ToString () };

                    if (prev == null)
                    {
                        s.X = 0;
                    }
                    else
                    {
                        s.X = Pos.Right (prev) + 2;
                    }

                    s.Y = 0;
                    prev = s;
                }

                if (optionsSlider.GetSetOptions ().Contains (3))
                {
                    s.Width = Dim.Auto (DimAutoStyle.Content);
                    s.Height = Dim.Auto (DimAutoStyle.Content);
                }
                else
                {
                    if (s.Orientation == Orientation.Horizontal)
                    {
                        s.Width = Dim.Percent (50);

                        int h = s.ShowLegends && s.LegendsOrientation == Orientation.Vertical
                                    ? s.Options.Max (o => o.Legend!.Length) + 3
                                    : 4;
                        s.Height = h;
                    }
                    else
                    {
                        int w = s.ShowLegends ? s.Options.Max (o => o.Legend!.Length) + 3 : 3;
                        s.Width = w;
                        s.Height = Dim.Fill ();
                    }
                }
            }
        }

        void OnOptionsSliderOnOptionsChanged (object _, LinearRangeEventArgs<string> e)
        {
            foreach (LinearRange s in mainWindow.SubViews.OfType<LinearRange> ())
            {
                s.ShowLegends = e.Options.ContainsKey (0);
                s.RangeAllowSingle = e.Options.ContainsKey (1);
                s.ShowEndSpacing = e.Options.ContainsKey (2);

                if (e.Options.ContainsKey (3))
                {
                    s.Width = Dim.Auto (DimAutoStyle.Content);
                    s.Height = Dim.Auto (DimAutoStyle.Content);
                }
                else
                {
                    if (s.Orientation == Orientation.Horizontal)
                    {
                        s.Width = Dim.Percent (50);

                        int h = s.ShowLegends && s.LegendsOrientation == Orientation.Vertical
                                    ? s.Options.Max (o => o.Legend!.Length) + 3
                                    : 4;
                        s.Height = h;
                    }
                    else
                    {
                        int w = s.ShowLegends ? s.Options.Max (o => o.Legend!.Length) + 3 : 3;
                        s.Width = w;
                        s.Height = Dim.Fill ();
                    }
                }
            }
        }
    }

    private void MakeSliders (Window window, List<object> options)
    {
        List<LinearRangeType> types = Enum.GetValues (typeof (LinearRangeType)).Cast<LinearRangeType> ().ToList ();
        LinearRange prev = null;

        foreach (LinearRange view in types.Select (type => new LinearRange (options)
                 {
                     Title = type.ToString (),
                     X = 0,
                     Y = prev == null ? 0 : Pos.Bottom (prev),
                     BorderStyle = LineStyle.Single,
                     Type = type,
                     AllowEmpty = true
                 }))
        {
            //view.Padding.Thickness = new (0,1,0,0);
            window.Add (view);
            prev = view;
        }

        List<object> singleOptions =
        [
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            10,
            11,
            12,
            13,
            14,
            15,
            16,
            17,
            18,
            19,
            20,
            21,
            22,
            23,
            24,
            25,
            26,
            27,
            28,
            29,
            30,
            31,
            32,
            33,
            34,
            35,
            36,
            37,
            38,
            39
        ];

        LinearRange single = new (singleOptions)
        {
            Title = "_Continuous",
            X = 0,
            Y = prev == null ? 0 : Pos.Bottom (prev),
            Type = LinearRangeType.Single,
            BorderStyle = LineStyle.Single,
            AllowEmpty = false
        };

        single.SubViewLayout += (_, _) =>
                                {
                                    if (single.Orientation == Orientation.Horizontal)
                                    {
                                        single.Style.SpaceChar = new () { Grapheme = Glyphs.HLine.ToString () };
                                        single.Style.OptionChar = new () { Grapheme = Glyphs.HLine.ToString () };
                                    }
                                    else
                                    {
                                        single.Style.SpaceChar = new () { Grapheme = Glyphs.VLine.ToString () };
                                        single.Style.OptionChar = new () { Grapheme = Glyphs.VLine.ToString () };
                                    }
                                };
        single.Style.SetChar = new () { Grapheme = Glyphs.ContinuousMeterSegment.ToString () };
        single.Style.DragChar = new () { Grapheme = Glyphs.ContinuousMeterSegment.ToString () };

        window.Add (single);

        single.OptionsChanged += (_, e) => { single.Title = $"_Continuous {e.Options.FirstOrDefault ().Key}"; };

        List<object> oneOption = new () { "The Only Option" };

        LinearRange one = new (oneOption)
        {
            Title = "_One Option",
            X = 0,
            Y = prev == null ? 0 : Pos.Bottom (single),
            Type = LinearRangeType.Single,
            BorderStyle = LineStyle.Single,
            AllowEmpty = false
        };
        window.Add (one);
    }
}
