using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Terminal.Gui;
using Application = Terminal.Gui.Application;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Graph View", "Demos the GraphView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Drawing")]
public class GraphViewExample : Scenario
{
    private readonly Thickness _thickness = new (1, 1, 1, 1);
    private TextView _about;
    private int _currentGraph;
    private Action [] _graphs;
    private GraphView _graphView;
    private MenuItem _miDiags;
    private MenuItem _miShowBorder;
    private ViewDiagnosticFlags _viewDiagnostics;

    public override void Main ()
    {
        Application.Init ();
        Toplevel app = new ();

        _graphs = new []
        {
            () => SetupPeriodicTableScatterPlot (), //0
            () => SetupLifeExpectancyBarGraph (true), //1
            () => SetupLifeExpectancyBarGraph (false), //2
            () => SetupPopulationPyramid (), //3
            () => SetupLineGraph (), //4
            () => SetupSineWave (), //5
            () => SetupDisco (), //6
            () => MultiBarGraph () //7
        };

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new (
                              "Scatter _Plot",
                              "",
                              () => _graphs [_currentGraph =
                                                 0] ()
                             ),
                         new (
                              "_V Bar Graph",
                              "",
                              () => _graphs [_currentGraph =
                                                 1] ()
                             ),
                         new (
                              "_H Bar Graph",
                              "",
                              () => _graphs [_currentGraph =
                                                 2] ()
                             ),
                         new (
                              "P_opulation Pyramid",
                              "",
                              () => _graphs [_currentGraph =
                                                 3] ()
                             ),
                         new (
                              "_Line Graph",
                              "",
                              () => _graphs [_currentGraph =
                                                 4] ()
                             ),
                         new (
                              "Sine _Wave",
                              "",
                              () => _graphs [_currentGraph =
                                                 5] ()
                             ),
                         new (
                              "Silent _Disco",
                              "",
                              () => _graphs [_currentGraph =
                                                 6] ()
                             ),
                         new (
                              "_Multi Bar Graph",
                              "",
                              () => _graphs [_currentGraph =
                                                 7] ()
                             ),
                         new ("_Quit", "", () => Quit ())
                     }
                    ),
                new (
                     "_View",
                     new []
                     {
                         new ("Zoom _In", "", () => Zoom (0.5f)),
                         new ("Zoom _Out", "", () => Zoom (2f)),
                         new ("MarginLeft++", "", () => Margin (true, true)),
                         new ("MarginLeft--", "", () => Margin (true, false)),
                         new ("MarginBottom++", "", () => Margin (false, true)),
                         new ("MarginBottom--", "", () => Margin (false, false)),
                         _miShowBorder = new (
                                              "_Enable Margin, Border, and Padding",
                                              "",
                                              () => ShowBorder ()
                                             )
                         {
                             Checked = true,
                             CheckType = MenuItemCheckStyle
                                 .Checked
                         },
                         _miDiags = new (
                                         "_Diagnostics",
                                         "",
                                         () => ToggleDiagnostics ()
                                        )
                         {
                             Checked = View.Diagnostics
                                       == (ViewDiagnosticFlags
                                               .Thickness
                                           | ViewDiagnosticFlags
                                               .Ruler),
                             CheckType = MenuItemCheckStyle.Checked
                         }
                     }
                    )
            ]
        };
        app.Add (menu);

        _graphView = new()
        {
            X = 0,
            Y = 1,
            Width = Dim.Percent (70),
            Height = Dim.Fill (1),
            BorderStyle = LineStyle.Single
        };
        _graphView.Border.Thickness = _thickness;
        _graphView.Margin.Thickness = _thickness;
        _graphView.Padding.Thickness = _thickness;

        app.Add (_graphView);

        var frameRight = new FrameView
        {
            X = Pos.Right (_graphView),
            Y = Pos.Top (_graphView),
            Width = Dim.Fill (),
            Height = Dim.Height (_graphView),
            Title = "About"
        };

        frameRight.Add (
                        _about = new() { Width = Dim.Fill (), Height = Dim.Fill (), ReadOnly = true }
                       );

        app.Add (frameRight);

        var statusBar = new StatusBar (
                                       new Shortcut []
                                       {
                                           new (Key.G.WithCtrl, "Next Graph", () => _graphs [_currentGraph++ % _graphs.Length] ()),
                                           new (Key.PageUp, "Zoom In", () => Zoom (0.5f)),
                                           new (Key.PageDown, "Zoom Out", () => Zoom (2f))
                                       }
                                      );
        app.Add (statusBar);

        var diagShortcut = new Shortcut
        {
            Key = Key.F10,
            CommandView = new CheckBox
            {
                Title = "Diagnostics",
                CanFocus = false
            }
        };
        statusBar.Add (diagShortcut).Accepting += DiagShortcut_Accept;

        _graphs [_currentGraph++ % _graphs.Length] ();

        _viewDiagnostics = View.Diagnostics;
        Application.Run (app);
        View.Diagnostics = _viewDiagnostics;
        app.Dispose ();
        Application.Shutdown ();
    }

    private void DiagShortcut_Accept (object sender, CommandEventArgs e)
    {
        ToggleDiagnostics ();

        if (sender is Shortcut shortcut && shortcut.CommandView is CheckBox checkBox)
        {
            checkBox.CheckedState = _miDiags.Checked ?? false ? CheckState.Checked : CheckState.UnChecked;
        }
    }

    private void ToggleDiagnostics ()
    {
        _miDiags.Checked = !_miDiags.Checked;

        View.Diagnostics = _miDiags.Checked == true
                               ? ViewDiagnosticFlags.Thickness
                                 | ViewDiagnosticFlags.Ruler
                               : ViewDiagnosticFlags.Off;
        Application.LayoutAndDraw ();
    }

    private void Margin (bool left, bool increase)
    {
        if (left)
        {
            _graphView.MarginLeft = (uint)Math.Max (0, _graphView.MarginLeft + (increase ? 1 : -1));
        }
        else
        {
            _graphView.MarginBottom = (uint)Math.Max (0, _graphView.MarginBottom + (increase ? 1 : -1));
        }

        _graphView.SetNeedsDraw ();
    }

    private void MultiBarGraph ()
    {
        _graphView.Reset ();

        _graphView.Title = "Multi Bar";

        _about.Text = "Housing Expenditures by income thirds 1996-2003";

        Color fore = _graphView.ColorScheme.Normal.Foreground == Color.Black
                         ? Color.White
                         : _graphView.ColorScheme.Normal.Foreground;
        var black = new Attribute (fore, Color.Black);
        var cyan = new Attribute (Color.BrightCyan, Color.Black);
        var magenta = new Attribute (Color.BrightMagenta, Color.Black);
        var red = new Attribute (Color.BrightRed, Color.Black);

        _graphView.GraphColor = black;

        var series = new MultiBarSeries (3, 1, 0.25f, new [] { magenta, cyan, red });

        Rune stiple = Glyphs.Stipple;

        series.AddBars ("'96", stiple, 5900, 9000, 14000);
        series.AddBars ("'97", stiple, 6100, 9200, 14800);
        series.AddBars ("'98", stiple, 6000, 9300, 14600);
        series.AddBars ("'99", stiple, 6100, 9400, 14950);
        series.AddBars ("'00", stiple, 6200, 9500, 15200);
        series.AddBars ("'01", stiple, 6250, 9900, 16000);
        series.AddBars ("'02", stiple, 6600, 11000, 16700);
        series.AddBars ("'03", stiple, 7000, 12000, 17000);

        _graphView.CellSize = new (0.25f, 1000);
        _graphView.Series.Add (series);
        _graphView.SetNeedsDraw ();

        _graphView.MarginLeft = 3;
        _graphView.MarginBottom = 1;

        _graphView.AxisY.LabelGetter = v => '$' + (v.Value / 1000f).ToString ("N0") + 'k';

        // Do not show x axis labels (bars draw their own labels)
        _graphView.AxisX.Increment = 0;
        _graphView.AxisX.ShowLabelsEvery = 0;
        _graphView.AxisX.Minimum = 0;

        _graphView.AxisY.Minimum = 0;

        var legend = new LegendAnnotation (new (_graphView.Viewport.Width - 20, 0, 20, 5));

        legend.AddEntry (
                         new (stiple, series.SubSeries.ElementAt (0).OverrideBarColor),
                         "Lower Third"
                        );

        legend.AddEntry (
                         new (stiple, series.SubSeries.ElementAt (1).OverrideBarColor),
                         "Middle Third"
                        );

        legend.AddEntry (
                         new (stiple, series.SubSeries.ElementAt (2).OverrideBarColor),
                         "Upper Third"
                        );
        _graphView.Annotations.Add (legend);
    }

    private void Quit () { Application.RequestStop (); }

    private void SetupDisco ()
    {
        _graphView.Reset ();

        _graphView.Title = "Graphic Equalizer";

        _about.Text = "This graph shows a graphic equalizer for an imaginary song";

        _graphView.GraphColor = new Attribute (Color.White, Color.Black);

        var stiple = new GraphCellToRender ((Rune)'\u2593');

        var r = new Random ();
        var series = new DiscoBarSeries ();
        List<BarSeriesBar> bars = new ();

        Func<bool> genSample = () =>
                               {
                                   bars.Clear ();

                                   // generate an imaginary sample
                                   for (var i = 0; i < 31; i++)
                                   {
                                       bars.Add (
                                                 new (null, stiple, r.Next (0, 100))
                                                 {
                                                     //ColorGetter = colorDelegate
                                                 }
                                                );
                                   }

                                   _graphView.SetNeedsDraw ();

                                   // while the equaliser is showing
                                   return _graphView.Series.Contains (series);
                               };

        Application.AddTimeout (TimeSpan.FromMilliseconds (250), genSample);

        series.Bars = bars;

        _graphView.Series.Add (series);

        // How much graph space each cell of the console depicts
        _graphView.CellSize = new (1, 10);
        _graphView.AxisX.Increment = 0; // No graph ticks
        _graphView.AxisX.ShowLabelsEvery = 0; // no labels

        _graphView.AxisX.Visible = false;
        _graphView.AxisY.Visible = false;

        _graphView.SetNeedsDraw ();
    }
    /*
    Country,Both,Male,Female

"Switzerland",83.4,81.8,85.1
"South Korea",83.3,80.3,86.1
"Singapore",83.2,81,85.5
"Spain",83.2,80.7,85.7
"Cyprus",83.1,81.1,85.1
"Australia",83,81.3,84.8
"Italy",83,80.9,84.9
"Norway",83,81.2,84.7
"Israel",82.6,80.8,84.4
"France",82.5,79.8,85.1
"Luxembourg",82.4,80.6,84.2
"Sweden",82.4,80.8,84
"Iceland",82.3,80.8,83.9
"Canada",82.2,80.4,84.1
"New Zealand",82,80.4,83.5
"Malta,81.9",79.9,83.8
"Ireland",81.8,80.2,83.5
"Netherlands",81.8,80.4,83.1
"Germany",81.7,78.7,84.8
"Austria",81.6,79.4,83.8
"Finland",81.6,79.2,84
"Portugal",81.6,78.6,84.4
"Belgium",81.4,79.3,83.5
"United Kingdom",81.4,79.8,83
"Denmark",81.3,79.6,83
"Slovenia",81.3,78.6,84.1
"Greece",81.1,78.6,83.6
"Kuwait",81,79.3,83.9
"Costa Rica",80.8,78.3,83.4*/

    private void SetupLifeExpectancyBarGraph (bool verticalBars)
    {
        _graphView.Reset ();

        _graphView.Title = $"Life Expectancy - {(verticalBars ? "Vertical" : "Horizontal")}";

        _about.Text = "This graph shows the life expectancy at birth of a range of countries";

        var softStiple = new GraphCellToRender ((Rune)'\u2591');
        var mediumStiple = new GraphCellToRender ((Rune)'\u2592');

        var barSeries = new BarSeries
        {
            Bars = new()
            {
                new ("Switzerland", softStiple, 83.4f),
                new (
                     "South Korea",
                     !verticalBars
                         ? mediumStiple
                         : softStiple,
                     83.3f
                    ),
                new ("Singapore", softStiple, 83.2f),
                new (
                     "Spain",
                     !verticalBars
                         ? mediumStiple
                         : softStiple,
                     83.2f
                    ),
                new ("Cyprus", softStiple, 83.1f),
                new (
                     "Australia",
                     !verticalBars
                         ? mediumStiple
                         : softStiple,
                     83
                    ),
                new ("Italy", softStiple, 83),
                new (
                     "Norway",
                     !verticalBars
                         ? mediumStiple
                         : softStiple,
                     83
                    ),
                new ("Israel", softStiple, 82.6f),
                new (
                     "France",
                     !verticalBars
                         ? mediumStiple
                         : softStiple,
                     82.5f
                    ),
                new ("Luxembourg", softStiple, 82.4f),
                new (
                     "Sweden",
                     !verticalBars
                         ? mediumStiple
                         : softStiple,
                     82.4f
                    ),
                new ("Iceland", softStiple, 82.3f),
                new (
                     "Canada",
                     !verticalBars
                         ? mediumStiple
                         : softStiple,
                     82.2f
                    ),
                new ("New Zealand", softStiple, 82),
                new (
                     "Malta",
                     !verticalBars
                         ? mediumStiple
                         : softStiple,
                     81.9f
                    ),
                new ("Ireland", softStiple, 81.8f)
            }
        };

        _graphView.Series.Add (barSeries);

        if (verticalBars)
        {
            barSeries.Orientation = Orientation.Vertical;

            // How much graph space each cell of the console depicts
            _graphView.CellSize = new (0.1f, 0.25f);

            // No axis marks since Bar will add it's own categorical marks
            _graphView.AxisX.Increment = 0f;
            _graphView.AxisX.Text = "Country";
            _graphView.AxisX.Minimum = 0;

            _graphView.AxisY.Increment = 1f;
            _graphView.AxisY.ShowLabelsEvery = 1;
            _graphView.AxisY.LabelGetter = v => v.Value.ToString ("N2");
            _graphView.AxisY.Minimum = 0;
            _graphView.AxisY.Text = "Age";

            // leave space for axis labels and title
            _graphView.MarginBottom = 2;
            _graphView.MarginLeft = 6;

            // Start the graph at 80 years because that is where most of our data is
            _graphView.ScrollOffset = new (0, 80);
        }
        else
        {
            barSeries.Orientation = Orientation.Horizontal;

            // How much graph space each cell of the console depicts
            _graphView.CellSize = new (0.1f, 1f);

            // No axis marks since Bar will add it's own categorical marks
            _graphView.AxisY.Increment = 0f;
            _graphView.AxisY.ShowLabelsEvery = 1;
            _graphView.AxisY.Text = "Country";
            _graphView.AxisY.Minimum = 0;

            _graphView.AxisX.Increment = 1f;
            _graphView.AxisX.ShowLabelsEvery = 1;
            _graphView.AxisX.LabelGetter = v => v.Value.ToString ("N2");
            _graphView.AxisX.Text = "Age";
            _graphView.AxisX.Minimum = 0;

            // leave space for axis labels and title
            _graphView.MarginBottom = 2;
            _graphView.MarginLeft = (uint)barSeries.Bars.Max (b => b.Text.Length) + 2;

            // Start the graph at 80 years because that is where most of our data is
            _graphView.ScrollOffset = new (80, 0);
        }

        _graphView.SetNeedsDraw ();
    }

    private void SetupLineGraph ()
    {
        _graphView.Reset ();

        _graphView.Title = "Line";

        _about.Text = "This graph shows random points";

        var black = new Attribute (_graphView.ColorScheme.Normal.Foreground, Color.Black);
        var cyan = new Attribute (Color.BrightCyan, Color.Black);
        var magenta = new Attribute (Color.BrightMagenta, Color.Black);
        var red = new Attribute (Color.BrightRed, Color.Black);

        _graphView.GraphColor = black;

        List<PointF> randomPoints = new ();

        var r = new Random ();

        for (var i = 0; i < 10; i++)
        {
            randomPoints.Add (new (r.Next (100), r.Next (100)));
        }

        var points = new ScatterSeries { Points = randomPoints };

        var line = new PathAnnotation
        {
            LineColor = cyan, Points = randomPoints.OrderBy (p => p.X).ToList (), BeforeSeries = true
        };

        _graphView.Series.Add (points);
        _graphView.Annotations.Add (line);

        randomPoints = new ();

        for (var i = 0; i < 10; i++)
        {
            randomPoints.Add (new (r.Next (100), r.Next (100)));
        }

        var points2 = new ScatterSeries { Points = randomPoints, Fill = new ((Rune)'x', red) };

        var line2 = new PathAnnotation
        {
            LineColor = magenta, Points = randomPoints.OrderBy (p => p.X).ToList (), BeforeSeries = true
        };

        _graphView.Series.Add (points2);
        _graphView.Annotations.Add (line2);

        // How much graph space each cell of the console depicts
        _graphView.CellSize = new (2, 5);

        // leave space for axis labels
        _graphView.MarginBottom = 2;
        _graphView.MarginLeft = 3;

        // One axis tick/label per
        _graphView.AxisX.Increment = 20;
        _graphView.AxisX.ShowLabelsEvery = 1;
        _graphView.AxisX.Text = "X →";

        _graphView.AxisY.Increment = 20;
        _graphView.AxisY.ShowLabelsEvery = 1;
        _graphView.AxisY.Text = "↑Y";

        PointF max = line.Points.Union (line2.Points).OrderByDescending (p => p.Y).First ();

        _graphView.Annotations.Add (
                                    new TextAnnotation
                                    {
                                        Text = "(Max)",
                                        GraphPosition = new (
                                                             max.X + 2 * _graphView.CellSize.X,
                                                             max.Y
                                                            )
                                    }
                                   );

        _graphView.SetNeedsDraw ();
    }

    private void SetupPeriodicTableScatterPlot ()
    {
        _graphView.Reset ();

        _graphView.Title = "Scatter Plot";

        _about.Text =
            "This graph shows the atomic weight of each element in the periodic table.\nStarting with Hydrogen (atomic Number 1 with a weight of 1.007)";

        //AtomicNumber and AtomicMass of all elements in the periodic table
        _graphView.Series.Add (
                               new ScatterSeries
                               {
                                   Points = new()
                                   {
                                       new (1, 1.007f),
                                       new (2, 4.002f),
                                       new (3, 6.941f),
                                       new (4, 9.012f),
                                       new (5, 10.811f),
                                       new (6, 12.011f),
                                       new (7, 14.007f),
                                       new (8, 15.999f),
                                       new (9, 18.998f),
                                       new (10, 20.18f),
                                       new (11, 22.99f),
                                       new (12, 24.305f),
                                       new (13, 26.982f),
                                       new (14, 28.086f),
                                       new (15, 30.974f),
                                       new (16, 32.065f),
                                       new (17, 35.453f),
                                       new (18, 39.948f),
                                       new (19, 39.098f),
                                       new (20, 40.078f),
                                       new (21, 44.956f),
                                       new (22, 47.867f),
                                       new (23, 50.942f),
                                       new (24, 51.996f),
                                       new (25, 54.938f),
                                       new (26, 55.845f),
                                       new (27, 58.933f),
                                       new (28, 58.693f),
                                       new (29, 63.546f),
                                       new (30, 65.38f),
                                       new (31, 69.723f),
                                       new (32, 72.64f),
                                       new (33, 74.922f),
                                       new (34, 78.96f),
                                       new (35, 79.904f),
                                       new (36, 83.798f),
                                       new (37, 85.468f),
                                       new (38, 87.62f),
                                       new (39, 88.906f),
                                       new (40, 91.224f),
                                       new (41, 92.906f),
                                       new (42, 95.96f),
                                       new (43, 98f),
                                       new (44, 101.07f),
                                       new (45, 102.906f),
                                       new (46, 106.42f),
                                       new (47, 107.868f),
                                       new (48, 112.411f),
                                       new (49, 114.818f),
                                       new (50, 118.71f),
                                       new (51, 121.76f),
                                       new (52, 127.6f),
                                       new (53, 126.904f),
                                       new (54, 131.293f),
                                       new (55, 132.905f),
                                       new (56, 137.327f),
                                       new (57, 138.905f),
                                       new (58, 140.116f),
                                       new (59, 140.908f),
                                       new (60, 144.242f),
                                       new (61, 145),
                                       new (62, 150.36f),
                                       new (63, 151.964f),
                                       new (64, 157.25f),
                                       new (65, 158.925f),
                                       new (66, 162.5f),
                                       new (67, 164.93f),
                                       new (68, 167.259f),
                                       new (69, 168.934f),
                                       new (70, 173.054f),
                                       new (71, 174.967f),
                                       new (72, 178.49f),
                                       new (73, 180.948f),
                                       new (74, 183.84f),
                                       new (75, 186.207f),
                                       new (76, 190.23f),
                                       new (77, 192.217f),
                                       new (78, 195.084f),
                                       new (79, 196.967f),
                                       new (80, 200.59f),
                                       new (81, 204.383f),
                                       new (82, 207.2f),
                                       new (83, 208.98f),
                                       new (84, 210),
                                       new (85, 210),
                                       new (86, 222),
                                       new (87, 223),
                                       new (88, 226),
                                       new (89, 227),
                                       new (90, 232.038f),
                                       new (91, 231.036f),
                                       new (92, 238.029f),
                                       new (93, 237),
                                       new (94, 244),
                                       new (95, 243),
                                       new (96, 247),
                                       new (97, 247),
                                       new (98, 251),
                                       new (99, 252),
                                       new (100, 257),
                                       new (101, 258),
                                       new (102, 259),
                                       new (103, 262),
                                       new (104, 261),
                                       new (105, 262),
                                       new (106, 266),
                                       new (107, 264),
                                       new (108, 267),
                                       new (109, 268),
                                       new (113, 284),
                                       new (114, 289),
                                       new (115, 288),
                                       new (116, 292),
                                       new (117, 295),
                                       new (118, 294)
                                   }
                               }
                              );

        // How much graph space each cell of the console depicts
        _graphView.CellSize = new (1, 5);

        // leave space for axis labels
        _graphView.MarginBottom = 2;
        _graphView.MarginLeft = 3;

        // One axis tick/label per 5 atomic numbers
        _graphView.AxisX.Increment = 5;
        _graphView.AxisX.ShowLabelsEvery = 1;
        _graphView.AxisX.Text = "Atomic Number";
        _graphView.AxisX.Minimum = 0;

        // One label every 5 atomic weight
        _graphView.AxisY.Increment = 5;
        _graphView.AxisY.ShowLabelsEvery = 1;
        _graphView.AxisY.Minimum = 0;

        _graphView.SetNeedsDraw ();
    }

    private void SetupPopulationPyramid ()
    {
        /*
        Age,M,F
0-4,2009363,1915127
5-9,2108550,2011016
10-14,2022370,1933970
15-19,1880611,1805522
20-24,2072674,2001966
25-29,2275138,2208929
30-34,2361054,2345774
35-39,2279836,2308360
40-44,2148253,2159877
45-49,2128343,2167778
50-54,2281421,2353119
55-59,2232388,2306537
60-64,1919839,1985177
65-69,1647391,1734370
70-74,1624635,1763853
75-79,1137438,1304709
80-84,766956,969611
85-89,438663,638892
90-94,169952,320625
95-99,34524,95559
100+,3016,12818*/

        _about.Text = "This graph shows population of each age divided by gender";

        _graphView.Title = "Population Pyramid";

        _graphView.Reset ();

        // How much graph space each cell of the console depicts
        _graphView.CellSize = new (100_000, 1);

        //center the x axis in middle of screen to show both sides
        _graphView.ScrollOffset = new (-3_000_000, 0);

        _graphView.AxisX.Text = "Number Of People";
        _graphView.AxisX.Increment = 500_000;
        _graphView.AxisX.ShowLabelsEvery = 2;

        // use Abs to make negative axis labels positive
        _graphView.AxisX.LabelGetter = v => Math.Abs (v.Value / 1_000_000).ToString ("N2") + "M";

        // leave space for axis labels
        _graphView.MarginBottom = 2;
        _graphView.MarginLeft = 1;

        // do not show axis titles (bars have their own categories)
        _graphView.AxisY.Increment = 0;
        _graphView.AxisY.ShowLabelsEvery = 0;
        _graphView.AxisY.Minimum = 0;

        var stiple = new GraphCellToRender (Glyphs.Stipple);

        // Bars in 2 directions

        // Males (negative to make the bars go left)
        var malesSeries = new BarSeries
        {
            Orientation = Orientation.Horizontal,
            Bars = new()
            {
                new ("0-4", stiple, -2009363),
                new ("5-9", stiple, -2108550),
                new ("10-14", stiple, -2022370),
                new ("15-19", stiple, -1880611),
                new ("20-24", stiple, -2072674),
                new ("25-29", stiple, -2275138),
                new ("30-34", stiple, -2361054),
                new ("35-39", stiple, -2279836),
                new ("40-44", stiple, -2148253),
                new ("45-49", stiple, -2128343),
                new ("50-54", stiple, -2281421),
                new ("55-59", stiple, -2232388),
                new ("60-64", stiple, -1919839),
                new ("65-69", stiple, -1647391),
                new ("70-74", stiple, -1624635),
                new ("75-79", stiple, -1137438),
                new ("80-84", stiple, -766956),
                new ("85-89", stiple, -438663),
                new ("90-94", stiple, -169952),
                new ("95-99", stiple, -34524),
                new ("100+", stiple, -3016)
            }
        };
        _graphView.Series.Add (malesSeries);

        // Females
        var femalesSeries = new BarSeries
        {
            Orientation = Orientation.Horizontal,
            Bars = new()
            {
                new ("0-4", stiple, 1915127),
                new ("5-9", stiple, 2011016),
                new ("10-14", stiple, 1933970),
                new ("15-19", stiple, 1805522),
                new ("20-24", stiple, 2001966),
                new ("25-29", stiple, 2208929),
                new ("30-34", stiple, 2345774),
                new ("35-39", stiple, 2308360),
                new ("40-44", stiple, 2159877),
                new ("45-49", stiple, 2167778),
                new ("50-54", stiple, 2353119),
                new ("55-59", stiple, 2306537),
                new ("60-64", stiple, 1985177),
                new ("65-69", stiple, 1734370),
                new ("70-74", stiple, 1763853),
                new ("75-79", stiple, 1304709),
                new ("80-84", stiple, 969611),
                new ("85-89", stiple, 638892),
                new ("90-94", stiple, 320625),
                new ("95-99", stiple, 95559),
                new ("100+", stiple, 12818)
            }
        };

        var softStiple = new GraphCellToRender ((Rune)'\u2591');
        var mediumStiple = new GraphCellToRender ((Rune)'\u2592');

        for (var i = 0; i < malesSeries.Bars.Count; i++)
        {
            malesSeries.Bars [i].Fill = i % 2 == 0 ? softStiple : mediumStiple;
            femalesSeries.Bars [i].Fill = i % 2 == 0 ? softStiple : mediumStiple;
        }

        _graphView.Series.Add (femalesSeries);

        _graphView.Annotations.Add (new TextAnnotation { Text = "M", ScreenPosition = new Point (0, 10) });

        _graphView.Annotations.Add (
                                    new TextAnnotation { Text = "F", ScreenPosition = new Point (_graphView.Viewport.Width - 1, 10) }
                                   );

        _graphView.SetNeedsDraw ();
    }

    private void SetupSineWave ()
    {
        _graphView.Reset ();

        _graphView.Title = "Sine Wave";

        _about.Text = "This graph shows a sine wave";

        var points = new ScatterSeries ();
        var line = new PathAnnotation ();

        // Draw line first so it does not draw over top of points or axis labels
        line.BeforeSeries = true;

        // Generate line graph with 2,000 points
        for (float x = -500; x < 500; x += 0.5f)
        {
            points.Points.Add (new (x, (float)Math.Sin (x)));
            line.Points.Add (new (x, (float)Math.Sin (x)));
        }

        _graphView.Series.Add (points);
        _graphView.Annotations.Add (line);

        // How much graph space each cell of the console depicts
        _graphView.CellSize = new (0.1f, 0.1f);

        // leave space for axis labels
        _graphView.MarginBottom = 2;
        _graphView.MarginLeft = 3;

        // One axis tick/label per
        _graphView.AxisX.Increment = 0.5f;
        _graphView.AxisX.ShowLabelsEvery = 2;
        _graphView.AxisX.Text = "X →";
        _graphView.AxisX.LabelGetter = v => v.Value.ToString ("N2");

        _graphView.AxisY.Increment = 0.2f;
        _graphView.AxisY.ShowLabelsEvery = 2;
        _graphView.AxisY.Text = "↑Y";
        _graphView.AxisY.LabelGetter = v => v.Value.ToString ("N2");

        _graphView.ScrollOffset = new (-2.5f, -1);

        _graphView.SetNeedsDraw ();
    }

    private void ShowBorder ()
    {
        _miShowBorder.Checked = !_miShowBorder.Checked;

        if (_miShowBorder.Checked == true)
        {
            _graphView.BorderStyle = LineStyle.Single;
            _graphView.Border.Thickness = _thickness;
            _graphView.Margin.Thickness = _thickness;
            _graphView.Padding.Thickness = _thickness;
        }
        else
        {
            _graphView.BorderStyle = LineStyle.None;
            _graphView.Margin.Thickness = Thickness.Empty;
            _graphView.Padding.Thickness = Thickness.Empty;
        }
    }

    private void Zoom (float factor)
    {
        _graphView.CellSize = new (
                                   _graphView.CellSize.X * factor,
                                   _graphView.CellSize.Y * factor
                                  );

        _graphView.AxisX.Increment *= factor;
        _graphView.AxisY.Increment *= factor;

        _graphView.SetNeedsDraw ();
    }

    private class DiscoBarSeries : BarSeries
    {
        private readonly Attribute _brightgreen;
        private readonly Attribute _brightred;
        private readonly Attribute _brightyellow;
        private readonly Attribute _green;
        private readonly Attribute _red;

        public DiscoBarSeries ()
        {
            _green = new (Color.BrightGreen, Color.Black);
            _brightgreen = new (Color.Green, Color.Black);
            _brightyellow = new (Color.BrightYellow, Color.Black);
            _red = new (Color.Red, Color.Black);
            _brightred = new (Color.BrightRed, Color.Black);
        }

        protected override void DrawBarLine (GraphView graph, Point start, Point end, BarSeriesBar beingDrawn)
        {
            IConsoleDriver driver = Application.Driver;

            int x = start.X;

            for (int y = end.Y; y <= start.Y; y++)
            {
                float height = graph.ScreenToGraphSpace (x, y).Y;

                if (height >= 85)
                {
                    graph.SetAttribute (_red);
                }
                else if (height >= 66)
                {
                    graph.SetAttribute (_brightred);
                }
                else if (height >= 45)
                {
                    graph.SetAttribute (_brightyellow);
                }
                else if (height >= 25)
                {
                    graph.SetAttribute (_brightgreen);
                }
                else
                {
                    graph.SetAttribute (_green);
                }

                graph.AddRune (x, y, beingDrawn.Fill.Rune);
            }
        }
    }
}
