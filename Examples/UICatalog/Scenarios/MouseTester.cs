using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Mouse Tester", "Illustrates Mouse event flow and handling")]
[ScenarioCategory ("Mouse and Keyboard")]
public class MouseTester : Scenario
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
            if (filterSlider.Options [i].Data != MouseFlags.PositionReport)
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

        View lastDriverEvent = new ()
        {
            Height = 1,
            Width = Dim.Auto (),
            X = Pos.Right (filterSlider),
            Y = 0,
            Text = "Last Driver Event: "
        };

        win.Add (lastDriverEvent);

        View lastAppEvent = new ()
        {
            Height = 1,
            Width = Dim.Auto (),
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (lastDriverEvent),
            Text = "Last App Event: "
        };

        win.Add (lastAppEvent);

        View lastViewEvent = new ()
        {
            Height = 1,
            Width = Dim.Auto (),
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (lastAppEvent),
            Text = "Last View Event: "
        };

        win.Add (lastViewEvent);

        CheckBox cbRepeatOnHold = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (lastViewEvent),
            Title = "_Repeat On Hold"
        };

        win.Add (cbRepeatOnHold);

        CheckBox cbHighlightOnPressed = new ()
        {
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (cbRepeatOnHold),
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

        cbHighlightOnPressed.CheckedState = demo.MouseHighlightStates.HasFlag (MouseState.Pressed) ? CheckState.Checked : CheckState.UnChecked;

        cbHighlightOnPressed.CheckedStateChanging += (_, e) =>
                                                     {
                                                         if (e.Result == CheckState.Checked)
                                                         {
                                                             demo.MouseHighlightStates |= MouseState.Pressed;
                                                         }
                                                         else
                                                         {
                                                             demo.MouseHighlightStates &= ~MouseState.Pressed;
                                                         }

                                                         foreach (View subview in demo.SubViews)
                                                         {
                                                             if (e.Result == CheckState.Checked)
                                                             {
                                                                 subview.MouseHighlightStates |= MouseState.Pressed;
                                                             }
                                                             else
                                                             {
                                                                 subview.MouseHighlightStates &= ~MouseState.Pressed;
                                                             }
                                                         }

                                                         foreach (View subview in demo.Padding.SubViews)
                                                         {
                                                             if (e.Result == CheckState.Checked)
                                                             {
                                                                 subview.MouseHighlightStates |= MouseState.Pressed;
                                                             }
                                                             else
                                                             {
                                                                 subview.MouseHighlightStates &= ~MouseState.Pressed;
                                                             }
                                                         }
                                                     };

        cbHighlightOnPressedOutside.CheckedState = demo.MouseHighlightStates.HasFlag (MouseState.PressedOutside) ? CheckState.Checked : CheckState.UnChecked;

        cbHighlightOnPressedOutside.CheckedStateChanging += (_, e) =>
                                                            {
                                                                if (e.Result == CheckState.Checked)
                                                                {
                                                                    demo.MouseHighlightStates |= MouseState.PressedOutside;
                                                                }
                                                                else
                                                                {
                                                                    demo.MouseHighlightStates &= ~MouseState.PressedOutside;
                                                                }

                                                                foreach (View subview in demo.SubViews)
                                                                {
                                                                    if (e.Result == CheckState.Checked)
                                                                    {
                                                                        subview.MouseHighlightStates |= MouseState.PressedOutside;
                                                                    }
                                                                    else
                                                                    {
                                                                        subview.MouseHighlightStates &= ~MouseState.PressedOutside;
                                                                    }
                                                                }

                                                                foreach (View subview in demo.Padding.SubViews)
                                                                {
                                                                    if (e.Result == CheckState.Checked)
                                                                    {
                                                                        subview.MouseHighlightStates |= MouseState.PressedOutside;
                                                                    }
                                                                    else
                                                                    {
                                                                        subview.MouseHighlightStates &= ~MouseState.PressedOutside;
                                                                    }
                                                                }
                                                            };

        cbRepeatOnHold.CheckedStateChanging += (_, _) =>
                                                        {
                                                            demo.MouseHoldRepeat = !demo.MouseHoldRepeat;

                                                            foreach (View subview in demo.SubViews)
                                                            {
                                                                subview.MouseHoldRepeat = demo.MouseHoldRepeat;
                                                            }

                                                            foreach (View subview in demo.Padding.SubViews)
                                                            {
                                                                subview.MouseHoldRepeat = demo.MouseHoldRepeat;
                                                            }
                                                        };

        var label = new Label
        {
            Text = "Dri_ver Events:",
            X = Pos.Right (filterSlider),
            Y = Pos.Bottom (demo)
        };

        ObservableCollection<string> driverLogList = new ();

        var driverLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Auto (minimumContentDim: Dim.Percent (20)),
            Height = Dim.Fill (),
            SchemeName = "Runnable",
            Source = new ListWrapper<string> (driverLogList)
        };
        win.Add (label, driverLog);

        Application.Driver.GetInputProcessor ().MouseEventParsed += (_, mouse) =>
                                  {
                                      int i = filterSlider.Options.FindIndex (o => mouse.Flags.HasFlag (o.Data));

                                      if (filterSlider.GetSetOptions ().Contains (i))
                                      {
                                          lastDriverEvent.Text = $"Last Driver Event: {mouse}";
                                          Logging.Trace (lastDriverEvent.Text);
                                          driverLogList.Add ($"{mouse.Position}:{mouse.Flags}");
                                          driverLog.MoveEnd ();
                                      }
                                  };
        label = new Label
        {
            Text = "_App Events:",
            X = Pos.Right (driverLog) + 1,
            Y = Pos.Bottom (demo)
        };

        ObservableCollection<string> appLogList = new ();

        var appLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Auto (minimumContentDim: Dim.Percent (20)),
            Height = Dim.Fill (),
            SchemeName = "Runnable",
            Source = new ListWrapper<string> (appLogList)
        };
        win.Add (label, appLog);

        Application.MouseEvent += (_, mouse) =>
                                  {
                                      int i = filterSlider.Options.FindIndex (o => mouse.Flags.HasFlag (o.Data));

                                      if (filterSlider.GetSetOptions ().Contains (i))
                                      {
                                          lastAppEvent.Text = $"   Last App Event: {mouse}";
                                          appLogList.Add ($"{mouse.Position}:{mouse.Flags}");
                                          appLog.MoveEnd ();
                                      }
                                  };

        label = new ()
        {
            Text = "_View Events:",
            X = Pos.Right (appLog) + 1,
            Y = Pos.Top (label)
        };
        ObservableCollection<string> viewLogList = [];

        ListView viewLog = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Auto (minimumContentDim: Dim.Percent (20)),
            Height = Dim.Fill (),
            SchemeName = "Runnable",
            Source = new ListWrapper<string> (viewLogList)
        };
        win.Add (label, viewLog);


        demo.MouseEvent += (_, mouse) =>
                          {
                              int i = filterSlider.Options.FindIndex (o => mouse.Flags.HasFlag (o.Data));

                              if (filterSlider.GetSetOptions ().Contains (i))
                              {
                                  lastViewEvent.Text = $"  Last View Event: {mouse}";
                                  viewLogList.Add ($"{mouse.Position}:{mouse.View!.Id}:{mouse.Flags}");
                                  viewLog.MoveEnd ();
                              }
                          };

        sub1.MouseEvent += (_, mouse) =>
                           {
                               int i = filterSlider.Options.FindIndex (o => mouse.Flags.HasFlag (o.Data));

                               if (filterSlider.GetSetOptions ().Contains (i))
                               {
                                   lastViewEvent.Text = $"  Last View Event: {mouse}";
                                   viewLogList.Add ($"{mouse.Position}:{mouse.View!.Id}:{mouse.Flags}");
                                   viewLog.MoveEnd ();
                               }
                           };

        sub2.MouseEvent += (_, mouse) =>
                           {
                               int i = filterSlider.Options.FindIndex (o => mouse.Flags.HasFlag (o.Data));

                               if (filterSlider.GetSetOptions ().Contains (i))
                               {
                                   lastViewEvent.Text = $"  Last View Event: {mouse}";
                                   viewLogList.Add ($"{mouse.Position}:{mouse.View!.Id}:{mouse.Flags}");
                                   viewLog.MoveEnd ();
                               }
                           };
        label = new ()
        {
            Text = "_Commands:",
            X = Pos.Right (viewLog) + 1,
            Y = Pos.Top (label)
        };
        ObservableCollection<string> commandLogList = [];

        var commandLog = new ListView
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = Dim.Auto (minimumContentDim: Dim.Percent (15)),
            Height = Dim.Fill (),
            SchemeName = "Runnable",
            Source = new ListWrapper<string> (commandLogList)
        };
        win.Add (label, commandLog);

        demo.Activating += (_, args) =>
                         {
                             commandLogList.Add ($"{args.Context!.Source!.Id}:{args.Context!.Command}");
                             commandLog.MoveEnd ();
                             args.Handled = true;
                         };

        demo.Accepting += (_, args) =>
                          {
                              commandLogList.Add ($"{args.Context!.Source!.Id}:{args.Context!.Command}");
                              commandLog.MoveEnd ();
                              args.Handled = true;
                          };

        sub1.Activating += (_, args) =>
                           {
                               commandLogList.Add ($"{args.Context!.Source!.Id}:{args.Context!.Command}");
                               commandLog.MoveEnd ();
                               args.Handled = true;
                           };

        sub1.Accepting += (_, args) =>
                          {
                              commandLogList.Add ($"{args.Context!.Source!.Id}:{args.Context!.Command}");
                              commandLog.MoveEnd ();
                              args.Handled = true;
                          };

        sub2.Activating += (_, args) =>
                           {
                               commandLogList.Add ($"{args.Context!.Source!.Id}:{args.Context!.Command}");
                               commandLog.MoveEnd ();
                               args.Handled = true;
                           };

        sub2.Accepting += (_, args) =>
                          {
                              commandLogList.Add ($"{args.Context!.Source!.Id}:{args.Context!.Command}");
                              commandLog.MoveEnd ();
                              args.Handled = true;
                          };


        clearButton.Accepting += (_, _) =>
                                 {
                                     driverLogList.Clear ();
                                     driverLog.SetSource (driverLogList);
                                     appLogList.Clear ();
                                     appLog.SetSource (appLogList);
                                     viewLogList.Clear ();
                                     viewLog.SetSource (viewLogList);
                                     commandLogList.Clear ();
                                     commandLog.SetSource (commandLogList);
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

            MouseLeave += (_, _) => { Text = "Leave"; };
            MouseEnter += (_, _) => { Text = "Enter"; };

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
                if (MouseState.HasFlag (MouseState.Pressed) && MouseHighlightStates.HasFlag (MouseState.Pressed))
                {
                    currentAttribute = currentAttribute with { Background = currentAttribute.Foreground.GetBrighterColor () };

                    return true;
                }
            }

            return base.OnGettingAttributeForRole (in role, ref currentAttribute);
        }
    }
}
