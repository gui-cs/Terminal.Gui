using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Mouse", "Demonstrates Mouse Events and States")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Mouse : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window win = new ()
        {
            Id = "win",
            Title = GetQuitKeyAndName ()
        };

        Slider<MouseFlags> filterSlider = new ()
        {
            Title = "_Filter",
            X = 0,
            Y = 0,
            BorderStyle = LineStyle.Single,
            Type = SliderType.Multiple,
            Orientation = Orientation.Vertical,
            UseMinimumSize = true,
            MinimumInnerSpacing = 0
        };

        filterSlider.Options = Enum.GetValues (typeof (MouseFlags))
                                   .Cast<MouseFlags> ()
                                   .Where (value => !value.ToString ().Contains ("None") && !value.ToString ().Contains ("All"))
                                   .Select (
                                            value => new SliderOption<MouseFlags>
                                            {
                                                Legend = value.ToString (),
                                                Data = value
                                            })
                                   .ToList ();

        for (var i = 0; i < filterSlider.Options.Count; i++)
        {
            if (filterSlider.Options [i].Data != MouseFlags.ReportMousePosition)
            {
                filterSlider.SetOption (i);
            }
        }

        win.Add (filterSlider);

        var clearButton = new Button
        {
            Title = "_Clear Logs",
            X = 1,
            Y = Pos.Bottom (filterSlider) + 1
        };
        win.Add (clearButton);
        Label ml;
        var count = 0;
        ml = new () { X = Pos.Right (filterSlider), Y = 0, Text = "Mouse: " };

        win.Add (ml);

        CheckBox cbWantContinuousPresses = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (ml),
            Title = "_Want Continuous Button Pressed"
        };

        win.Add (cbWantContinuousPresses);

        CheckBox cbHighlightOnPressed = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbWantContinuousPresses),
            Title = "_Highlight on Pressed"
        };

        win.Add (cbHighlightOnPressed);

        CheckBox cbHighlightOnPressedOutside = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbHighlightOnPressed),
            Title = "_Highlight on PressedOutside"
        };

        win.Add (cbHighlightOnPressedOutside);

        var demo = new MouseEventDemoView
        {
            Id = "demo",
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbHighlightOnPressedOutside),
            Width = Dim.Fill (),
            Height = 15,
            Title = "Enter/Leave Demo"
        };

        demo.Padding!.Initialized += DemoPaddingOnInitialized;

        void DemoPaddingOnInitialized (object o, EventArgs eventArgs)
        {
            demo.Padding!.Add (
                               new MouseEventDemoView
                               {
                                   X = 0,
                                   Y = 0,
                                   Width = Dim.Fill (),
                                   Height = Dim.Func (_ => demo.Padding.Thickness.Top),
                                   Title = "inPadding",
                                   Id = "inPadding"
                               });
            demo.Padding.Thickness = demo.Padding.Thickness with { Top = 5 };
        }

        View sub1 = new MouseEventDemoView
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent (20),
            Height = Dim.Fill (),
            Title = "sub1",
            Id = "sub1"
        };
        demo.Add (sub1);

        View sub2 = new MouseEventDemoView
        {
            X = Pos.Right (sub1) - 4,
            Y = Pos.Top (sub1) + 1,
            Width = Dim.Percent (20),
            Height = Dim.Fill (1),
            Title = "sub2",
            Id = "sub2"
        };

        demo.Add (sub2);

        win.Add (demo);

        cbHighlightOnPressed.CheckedState = demo.HighlightStates.HasFlag (MouseState.Pressed) ? CheckState.Checked : CheckState.UnChecked;

        cbHighlightOnPressed.CheckedStateChanging += (s, e) =>
                                                     {
                                                         if (e.Result == CheckState.Checked)
                                                         {
                                                             demo.HighlightStates |= MouseState.Pressed;
                                                         }
                                                         else
                                                         {
                                                             demo.HighlightStates &= ~MouseState.Pressed;
                                                         }

                                                         foreach (View subview in demo.SubViews)
                                                         {
                                                             if (e.Result == CheckState.Checked)
                                                             {
                                                                 subview.HighlightStates |= MouseState.Pressed;
                                                             }
                                                             else
                                                             {
                                                                 subview.HighlightStates &= ~MouseState.Pressed;
                                                             }
                                                         }

                                                         foreach (View subview in demo.Padding.SubViews)
                                                         {
                                                             if (e.Result == CheckState.Checked)
                                                             {
                                                                 subview.HighlightStates |= MouseState.Pressed;
                                                             }
                                                             else
                                                             {
                                                                 subview.HighlightStates &= ~MouseState.Pressed;
                                                             }
                                                         }
                                                     };

        cbHighlightOnPressedOutside.CheckedState = demo.HighlightStates.HasFlag (MouseState.PressedOutside) ? CheckState.Checked : CheckState.UnChecked;

        cbHighlightOnPressedOutside.CheckedStateChanging += (s, e) =>
                                                            {
                                                                if (e.Result == CheckState.Checked)
                                                                {
                                                                    demo.HighlightStates |= MouseState.PressedOutside;
                                                                }
                                                                else
                                                                {
                                                                    demo.HighlightStates &= ~MouseState.PressedOutside;
                                                                }

                                                                foreach (View subview in demo.SubViews)
                                                                {
                                                                    if (e.Result == CheckState.Checked)
                                                                    {
                                                                        subview.HighlightStates |= MouseState.PressedOutside;
                                                                    }
                                                                    else
                                                                    {
                                                                        subview.HighlightStates &= ~MouseState.PressedOutside;
                                                                    }
                                                                }

                                                                foreach (View subview in demo.Padding.SubViews)
                                                                {
                                                                    if (e.Result == CheckState.Checked)
                                                                    {
                                                                        subview.HighlightStates |= MouseState.PressedOutside;
                                                                    }
                                                                    else
                                                                    {
                                                                        subview.HighlightStates &= ~MouseState.PressedOutside;
                                                                    }
                                                                }
                                                            };

        cbWantContinuousPresses.CheckedStateChanging += (s, e) =>
                                                        {
                                                            demo.WantContinuousButtonPressed = !demo.WantContinuousButtonPressed;

                                                            foreach (View subview in demo.SubViews)
                                                            {
                                                                subview.WantContinuousButtonPressed = demo.WantContinuousButtonPressed;
                                                            }

                                                            foreach (View subview in demo.Padding.SubViews)
                                                            {
                                                                subview.WantContinuousButtonPressed = demo.WantContinuousButtonPressed;
                                                            }
                                                        };

        var label = new Label
        {
            Text = "_App Events:",
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (demo)
        };

        ObservableCollection<string> appLogList = new ();

        var appLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = 50,
            Height = Dim.Fill (),
            SchemeName = "TopLevel",
            Source = new ListWrapper<string> (appLogList)
        };
        win.Add (label, appLog);

        Application.MouseEvent += (sender, a) =>
                                  {
                                      int i = filterSlider.Options.FindIndex (o => o.Data == a.Flags);

                                      if (filterSlider.GetSetOptions ().Contains (i))
                                      {
                                          ml.Text = $"MouseEvent: ({a.Position}) - {a.Flags} {count}";
                                          appLogList.Add ($"({a.Position}) - {a.Flags} {count++}");
                                          appLog.MoveDown ();
                                      }
                                  };

        label = new ()
        {
            Text = "_Window Events:",
            X = Pos.Right (appLog) + 1,
            Y = Pos.Top (label)
        };
        ObservableCollection<string> winLogList = new ();

        var winLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            SchemeName = "TopLevel",
            Source = new ListWrapper<string> (winLogList)
        };
        win.Add (label, winLog);

        clearButton.Accepting += (s, e) =>
                                 {
                                     appLogList.Clear ();
                                     appLog.SetSource (appLogList);
                                     winLogList.Clear ();
                                     winLog.SetSource (winLogList);
                                 };

        win.MouseEvent += (sender, a) =>
                          {
                              int i = filterSlider.Options.FindIndex (o => o.Data == a.Flags);

                              if (filterSlider.GetSetOptions ().Contains (i))
                              {
                                  winLogList.Add ($"MouseEvent: ({a.Position}) - {a.Flags} {count++}");
                                  winLog.MoveDown ();
                              }
                          };

        win.MouseClick += (sender, a) =>
                          {
                              winLogList.Add ($"MouseClick: ({a.Position}) - {a.Flags} {count++}");
                              winLog.MoveDown ();
                          };

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }

    public class MouseEventDemoView : View
    {
        public MouseEventDemoView ()
        {
            CanFocus = true;
            Id = "mouseEventDemoView";

            Initialized += OnInitialized;

            MouseLeave += (s, e) => { Text = "Leave"; };
            MouseEnter += (s, e) => { Text = "Enter"; };

            return;

            void OnInitialized (object sender, EventArgs e)
            {
                TextAlignment = Alignment.Center;
                VerticalTextAlignment = Alignment.Center;

                Padding!.Thickness = new (1, 1, 1, 1);
                Padding!.SetScheme (new (new Attribute (Color.Black)));
                Padding.Id = $"{Id}.Padding";

                Border!.Thickness = new (1);
                Border.LineStyle = LineStyle.Rounded;
                Border.Id = $"{Id}.Border";

                MouseStateChanged += (_, args) =>
                                     {
                                         if (args.Value.HasFlag (MouseState.PressedOutside))
                                         {
                                             Border.LineStyle = LineStyle.Dotted;
                                         }
                                         else
                                         {
                                             Border.LineStyle = LineStyle.Single;
                                         }

                                         SetNeedsDraw ();
                                     };
            }
        }

        /// <inheritdoc/>
        protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
        {
            if (role == VisualRole.Normal)
            {
                if (MouseState.HasFlag (MouseState.Pressed) && HighlightStates.HasFlag (MouseState.Pressed))
                {
                    currentAttribute = currentAttribute with { Background = currentAttribute.Foreground.GetBrighterColor () };

                    return true;
                }
            }

            return base.OnGettingAttributeForRole (in role, ref currentAttribute);
        }
    }
}
