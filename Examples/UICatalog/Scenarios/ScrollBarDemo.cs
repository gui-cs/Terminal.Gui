#nullable enable
using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ScrollBar Demo", "Demonstrates ScrollBar.")]
[ScenarioCategory ("Scrolling")]
public class ScrollBarDemo : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ();
        window.Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}";
        window.Arrangement = ViewArrangement.Fixed;

        FrameView demoFrame = new ()
        {
            Title = "Demo View",
            X = 0,
            Width = 75,
            Height = 25 + 4,
            SchemeName = "Base",
            Arrangement = ViewArrangement.Resizable
        };
        demoFrame.Padding!.Thickness = new (1);
        demoFrame.Padding.Diagnostics = ViewDiagnosticFlags.Ruler;
        window.Add (demoFrame);

        ScrollBar scrollBar = new ()
        {
            X = Pos.AnchorEnd () - 5,
            AutoShow = false,
            ScrollableContentSize = 100,
            Height = Dim.Fill ()
        };
        demoFrame.Add (scrollBar);

        ListView controlledList = new ()
        {
            X = Pos.AnchorEnd (),
            Width = 5,
            Height = Dim.Fill (),
            SchemeName = "Error"
        };

        demoFrame.Add (controlledList);

        // populate the list box with Size items of the form "{n:00000}"
        controlledList.SetSource (new ObservableCollection<string> (Enumerable.Range (0, scrollBar.ScrollableContentSize).Select (n => $"{n:00000}")));

        int GetMaxLabelWidth (int groupId)
        {
            return demoFrame.SubViews.Max (v =>
                                           {
                                               if (v.Y.Has (out PosAlign pos) && pos.GroupId == groupId)
                                               {
                                                   return v.Text.GetColumns ();
                                               }

                                               return 0;
                                           });
        }

        Label lblWidthHeight = new ()
        {
            Text = "_Width/Height:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, AlignmentModes.StartToEnd, 1),
            Width = Dim.Func (_ => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblWidthHeight);

        NumericUpDown<int> scrollWidthHeight = new ()
        {
            Value = 1,
            X = Pos.Right (lblWidthHeight) + 1,
            Y = Pos.Top (lblWidthHeight)
        };
        demoFrame.Add (scrollWidthHeight);

        scrollWidthHeight.ValueChanging += (_, e) =>
                                           {
                                               if (e.NewValue < 1
                                                   || e.NewValue
                                                   > (scrollBar.Orientation == Orientation.Vertical
                                                          ? scrollBar.SuperView?.GetContentSize ().Width
                                                          : scrollBar.SuperView?.GetContentSize ().Height))
                                               {
                                                   // TODO: This must be handled in the ScrollSlider if Width and Height being virtual
                                                   e.Cancel = true;

                                                   return;
                                               }

                                               if (scrollBar.Orientation == Orientation.Vertical)
                                               {
                                                   scrollBar.Width = e.NewValue;
                                               }
                                               else
                                               {
                                                   scrollBar.Height = e.NewValue;
                                               }
                                           };

        Label lblOrientationLabel = new ()
        {
            Text = "_Orientation:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (_ => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblOrientationLabel);

        OptionSelector<Orientation> osOrientation = new ()
        {
            X = Pos.Right (lblOrientationLabel) + 1,
            Y = Pos.Top (lblOrientationLabel),
            AssignHotKeys = true,
            Orientation = Orientation.Horizontal
        };
        demoFrame.Add (osOrientation);

        osOrientation.ValueChanged += (_, _) =>
                                      {
                                          if (osOrientation.Value == Orientation.Horizontal)
                                          {
                                              scrollBar.Orientation = Orientation.Vertical;
                                              scrollBar.X = Pos.AnchorEnd () - 5;
                                              scrollBar.Y = 0;
                                              scrollBar.Width = scrollWidthHeight.Value;
                                              scrollBar.Height = Dim.Fill ();
                                              controlledList.Visible = true;
                                          }
                                          else
                                          {
                                              scrollBar.Orientation = Orientation.Horizontal;
                                              scrollBar.X = 0;
                                              scrollBar.Y = Pos.AnchorEnd ();
                                              scrollBar.Height = scrollWidthHeight.Value;
                                              scrollBar.Width = Dim.Fill ();
                                              controlledList.Visible = false;
                                          }
                                      };

        Label lblSize = new ()
        {
            Text = "Scrollable_ContentSize:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (_ => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblSize);

        NumericUpDown<int> scrollContentSize = new ()
        {
            Value = scrollBar.ScrollableContentSize,
            X = Pos.Right (lblSize) + 1,
            Y = Pos.Top (lblSize)
        };
        demoFrame.Add (scrollContentSize);

        scrollContentSize.ValueChanging += (_, e) =>
                                           {
                                               if (e.NewValue < 0)
                                               {
                                                   e.Cancel = true;

                                                   return;
                                               }

                                               if (scrollBar.ScrollableContentSize != e.NewValue)
                                               {
                                                   scrollBar.ScrollableContentSize = e.NewValue;

                                                   controlledList.SetSource (
                                                                             new ObservableCollection<string> (
                                                                              Enumerable.Range (0, scrollBar.ScrollableContentSize)
                                                                                        .Select (n => $"{n:00000}")));
                                               }
                                           };

        Label lblVisibleContentSize = new ()
        {
            Text = "_VisibleContentSize:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (_ => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblVisibleContentSize);

        NumericUpDown<int> visibleContentSize = new ()
        {
            Value = scrollBar.VisibleContentSize,
            X = Pos.Right (lblVisibleContentSize) + 1,
            Y = Pos.Top (lblVisibleContentSize)
        };
        demoFrame.Add (visibleContentSize);

        visibleContentSize.ValueChanging += (_, e) =>
                                            {
                                                if (e.NewValue < 0)
                                                {
                                                    e.Cancel = true;

                                                    return;
                                                }

                                                if (scrollBar.VisibleContentSize != e.NewValue)
                                                {
                                                    scrollBar.VisibleContentSize = e.NewValue;
                                                }
                                            };

        Label lblPosition = new ()
        {
            Text = "_Position:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (_ => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblPosition);

        NumericUpDown<int> scrollPosition = new ()
        {
            Value = scrollBar.GetSliderPosition (),
            X = Pos.Right (lblPosition) + 1,
            Y = Pos.Top (lblPosition)
        };
        demoFrame.Add (scrollPosition);

        scrollPosition.ValueChanging += (_, e) =>
                                        {
                                            if (e.NewValue < 0)
                                            {
                                                e.Cancel = true;

                                                return;
                                            }

                                            if (scrollBar.Position != e.NewValue)
                                            {
                                                scrollBar.Position = e.NewValue;
                                            }

                                            if (scrollBar.Position != e.NewValue)
                                            {
                                                e.Cancel = true;
                                            }
                                        };

        Label lblOptions = new ()
        {
            Text = "Options:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (_ => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblOptions);

        CheckBox autoShow = new ()
        {
            Y = Pos.Top (lblOptions),
            X = Pos.Right (lblOptions) + 1,
            Text = "_AutoShow",
            CheckedState = scrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked
        };
        autoShow.CheckedStateChanging += (_, e) => scrollBar.AutoShow = e.Result == CheckState.Checked;
        demoFrame.Add (autoShow);

        Label lblSliderPosition = new ()
        {
            Text = "SliderPosition:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (_ => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblSliderPosition);

        Label scrollSliderPosition = new ()
        {
            Text = scrollBar.GetSliderPosition ().ToString (),
            X = Pos.Right (lblSliderPosition) + 1,
            Y = Pos.Top (lblSliderPosition)
        };
        demoFrame.Add (scrollSliderPosition);

        Label lblScrolled = new ()
        {
            Text = "Scrolled:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (_ => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblScrolled);

        Label scrolled = new ()
        {
            X = Pos.Right (lblScrolled) + 1,
            Y = Pos.Top (lblScrolled)
        };
        demoFrame.Add (scrolled);

        Label lblScrollFrame = new ()
        {
            Y = Pos.Bottom (lblScrolled) + 1
        };
        demoFrame.Add (lblScrollFrame);

        Label lblScrollViewport = new ()
        {
            Y = Pos.Bottom (lblScrollFrame)
        };
        demoFrame.Add (lblScrollViewport);

        Label lblScrollContentSize = new ()
        {
            Y = Pos.Bottom (lblScrollViewport)
        };
        demoFrame.Add (lblScrollContentSize);

        scrollBar.SubViewsLaidOut += (_, _) =>
                                     {
                                         lblScrollFrame.Text = $"Scroll Frame: {scrollBar.Frame}";
                                         lblScrollViewport.Text = $"Scroll Viewport: {scrollBar.Viewport}";
                                         lblScrollContentSize.Text = $"Scroll ContentSize: {scrollBar.GetContentSize ()}";
                                         visibleContentSize.Value = scrollBar.VisibleContentSize;
                                     };

        EventLog eventLog = new ()
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            Height = Dim.Fill (),
            Width = 30,
            BorderStyle = LineStyle.Single,
            ViewToLog = scrollBar
        };
        window.Add (eventLog);

        window.Initialized += AppOnInitialized;

        void AppOnInitialized (object? sender, EventArgs e)
        {
            scrollBar.ScrollableContentSizeChanged += (_, args) =>
                                                      {
                                                          eventLog.Log ($"SizeChanged: {args.Value}");

                                                          if (scrollContentSize.Value != args.Value)
                                                          {
                                                              scrollContentSize.Value = args.Value;
                                                          }
                                                      };

            scrollBar.SliderPositionChanged += (_, args) =>
                                               {
                                                   eventLog.Log ($"SliderPositionChanged: {args.Value}");
                                                   eventLog.Log ($"  Position: {scrollBar.Position}");
                                                   scrollSliderPosition.Text = args.Value.ToString ();
                                               };

            scrollBar.Scrolled += (_, args) =>
                                  {
                                      eventLog.Log ($"Scrolled: {args.Value}");
                                      eventLog.Log ($"  SliderPosition: {scrollBar.GetSliderPosition ()}");
                                      scrolled.Text = args.Value.ToString ();
                                  };

            scrollBar.PositionChanged += (_, args) =>
                                         {
                                             eventLog.Log ($"PositionChanged: {args.Value}");
                                             scrollPosition.Value = args.Value;
                                             controlledList.Viewport = controlledList.Viewport with { Y = args.Value };
                                         };

            controlledList.ViewportChanged += (_, args) =>
                                              {
                                                  eventLog.Log ($"ViewportChanged: {args.NewViewport}");
                                                  scrollBar.Position = args.NewViewport.Y;
                                              };
        }

        app.Run (window);
    }
}
