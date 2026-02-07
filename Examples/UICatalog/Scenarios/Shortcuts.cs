#nullable enable

using Timer = System.Timers.Timer;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Shortcuts", "Illustrates Shortcut class.")]
[ScenarioCategory ("Controls")]
public class Shortcuts : Scenario
{
    private IApplication? _app;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window window = new ();

        window.IsRunningChanged += HandleOnIsRunningChanged;

        app.Run (window);
    }

    // Setting everything up in Loaded handler because we change the
    // QuitKey and it only sticks if changed after init
    private void HandleOnIsRunningChanged (object? sender, EventArgs<bool> e)
    {
        if (!e.Value)
        {
            // Stopping
            return;
        }

        _app!.TopRunnableView!.Title = GetQuitKeyAndName ();

        EventLog eventLog = new ()
        {
            Id = "eventLog",
            X = Pos.AnchorEnd (),
            Y = 0,
            Height = Dim.Fill (4),
            SchemeName = "Runnable",
            BorderStyle = LineStyle.Double,
            Title = "E_vents"
        };

        eventLog.Width = Dim.Auto (minimumContentDim: 40, maximumContentDim: Dim.Percent (50));
        _app?.TopRunnableView.Add (eventLog);

        var alignKeysShortcut = new Shortcut
        {
            Id = "alignKeys",
            X = 0,
            Y = 0,
            Width = Dim.Fill () - Dim.Width (eventLog),
            HelpText = "Fill to log",
            CommandView = new CheckBox { Text = "_Align Keys", CanFocus = false, MouseHighlightStates = MouseState.None, Value = CheckState.Checked },
            Key = Key.F5.WithCtrl.WithAlt.WithShift
        };

        ((CheckBox)alignKeysShortcut.CommandView).ValueChanging += (_, a) =>
                                                                   {
                                                                       if (alignKeysShortcut.CommandView is CheckBox cb)
                                                                       {
                                                                           bool align = a.NewValue == CheckState.Checked;
                                                                           AlignKeys (align);
                                                                       }
                                                                   };

        _app?.TopRunnableView.Add (alignKeysShortcut);

        var commandFirstShortcut = new Shortcut
        {
            Id = "commandFirst",
            X = 0,
            Y = Pos.Bottom (alignKeysShortcut),
            Width = Dim.Fill () - Dim.Width (eventLog),
            HelpText = "Show Command first",
            CommandView = new CheckBox { Id = "commandFirstCB", Text = "Command _First", CanFocus = false },
            Key = Key.F.WithCtrl
        };

        ((CheckBox)commandFirstShortcut.CommandView).ValueChanged += (_, eventArgs) =>
                                                                      {
                                                                          if (commandFirstShortcut.CommandView is not CheckBox cb)
                                                                          {
                                                                              return;
                                                                          }

                                                                          foreach (Shortcut peer in _app!.TopRunnableView!.SubViews.OfType<Shortcut> ())
                                                                          {
                                                                              if (eventArgs.NewValue == CheckState.Checked)
                                                                              {
                                                                                  peer.AlignmentModes &= ~AlignmentModes.EndToStart;
                                                                              }
                                                                              else
                                                                              {
                                                                                  peer.AlignmentModes |= AlignmentModes.EndToStart;
                                                                              }
                                                                          }
                                                                      };

        ((CheckBox)commandFirstShortcut.CommandView).Value =
            commandFirstShortcut.AlignmentModes.HasFlag (AlignmentModes.EndToStart) ? CheckState.UnChecked : CheckState.Checked;

        _app?.TopRunnableView.Add (commandFirstShortcut);

        Shortcut canFocusShortcut = new ()
        {
            Id = "canFocus",
            X = 0,
            Y = Pos.Bottom (commandFirstShortcut),
            Width = Dim.Fill (eventLog),
            Key = Key.F4,
            HelpText = "Sets all CommandView's .CanFocus",
            CommandView = new CheckBox { Id = "canFocusCB", Text = "_CanFocus", CanFocus = false }
        };

        canFocusShortcut.Activated += (s, args) =>
                                       {
                                           if ((s as Shortcut)?.CommandView is not CheckBox cb)
                                           {
                                               return;
                                           }
                                           SetCommandViewsCanFocus (cb.Value == CheckState.Checked);
                                       };
        _app?.TopRunnableView.Add (canFocusShortcut);

        var appShortcut = new Shortcut
        {
            Id = "appShortcut",
            X = 0,
            Y = Pos.Bottom (canFocusShortcut),
            Width = Dim.Fill (eventLog),
            Title = "A_pp Shortcut",
            Key = Key.F1,
            Text = "Width is DimFill",
            BindKeyToApplication = true
        };

        //_app?.TopRunnableView.CommandsToBubbleUp = [Command.Accept, Command.Activate];

        appShortcut.Accepting += (_, args) =>
                                 {
                                     args.Handled = true;
                                     MessageBox.Query (_app!, "App Shortcut", "You activated the App scoped shortcut!", Strings.btnOk);
                                 };

        _app?.TopRunnableView.Add (appShortcut);

        var buttonShortcut = new Shortcut
        {
            Id = "button",
            X = 0,
            Y = Pos.Bottom (appShortcut),
            Width = Dim.Fill (eventLog),
            HelpText = "Accepting pops MB",
            CommandView = new Button { Id = "buttonBtn", Title = "_Button", ShadowStyle = ShadowStyle.None },
            Key = Key.K
        };
        buttonShortcut.Accepting += Button_Clicked;

        _app?.TopRunnableView.Add (buttonShortcut);

        var optionSelectorShortcut = new Shortcut
        {
            Id = "optionSelector",
            HelpText = "Linear Range Orientation",
            X = 0,
            Y = Pos.Bottom (buttonShortcut),
            Key = Key.F2,
            Width = Dim.Fill (eventLog),
            CommandView = new OptionSelector<Orientation>
            {
                Id = "optionSelectorOS", Orientation = Orientation.Vertical, MouseHighlightStates = MouseState.None
            }
        };

        _app?.TopRunnableView.Add (optionSelectorShortcut);

        var sliderShortcut = new Shortcut
        {
            Id = "sliderShortcut",
            X = 0,
            Y = Pos.Bottom (optionSelectorShortcut),
            Width = Dim.Fill (eventLog),
            HelpText = "LinearRanges work!",
            CommandView = new LinearRange<string> { Id = "sliderLR", Orientation = Orientation.Horizontal, AllowEmpty = true },
            Key = Key.F5
        };

        ((LinearRange<string>)sliderShortcut.CommandView).Options =
        [
            new LinearRangeOption<string> { Legend = "A" }, new LinearRangeOption<string> { Legend = "B" }, new LinearRangeOption<string> { Legend = "C" }
        ];
        ((LinearRange<string>)sliderShortcut.CommandView).SetOption (0);

        ((LinearRange<string>)sliderShortcut.CommandView).OptionsChanged += (send, _) =>
                                                                            {
                                                                                if (send is LinearRange<string> lr)
                                                                                {
                                                                                    eventLog.Log ($"OptionsChanged: {lr.GetType ().Name} - {string.Join (",", lr.GetSetOptions ())}");
                                                                                }
                                                                            };

        optionSelectorShortcut.Action += () =>
                                         {
                                             ((LinearRange<string>)sliderShortcut.CommandView).Orientation =
                                                 ((OptionSelector<Orientation>)optionSelectorShortcut.CommandView).Value!.Value;
                                         };

        _app?.TopRunnableView.Add (sliderShortcut);

        // BUGBUG: Border causes issues with ListView sizing
        var listView = new ListView
        {
            Id = "listViewLV",
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = "ListView",
            BorderStyle = LineStyle.Single
        };
        listView.EnableForDesign ();

        var listViewShortcut = new Shortcut
        {
            Id = "listView",
            X = 0,
            Y = Pos.Bottom (sliderShortcut),
            Width = Dim.Fill (eventLog),
            HelpText = "A ListView with Border",
            CommandView = listView,
            Key = Key.F5.WithCtrl
        };

        _app?.TopRunnableView.Add (listViewShortcut);

        var noCommandShortcut = new Shortcut
        {
            Id = "noCommand",
            X = 0,
            Y = Pos.Bottom (listViewShortcut),
            Width = Dim.Width (listViewShortcut),
            HelpText = "No Command",
            Key = Key.D0
        };

        _app?.TopRunnableView.Add (noCommandShortcut);

        var noKeyShortcut = new Shortcut
        {
            Id = "noKey",
            X = 0,
            Y = Pos.Bottom (noCommandShortcut),
            Width = Dim.Width (noCommandShortcut),
            Title = "No Ke_y",
            HelpText = "Keyless"
        };

        _app?.TopRunnableView.Add (noKeyShortcut);

        var noHelpShortcut = new Shortcut
        {
            Id = "noHelp",
            X = 0,
            Y = Pos.Bottom (noKeyShortcut),
            Width = Dim.Width (noKeyShortcut),
            Key = Key.F6,
            Title = "Not _very much help",
            HelpText = ""
        };

        _app?.TopRunnableView.Add (noHelpShortcut);
        noHelpShortcut.SetFocus ();

        var framedShortcut = new Shortcut
        {
            Id = "framed",
            X = 0,
            Y = Pos.Bottom (noHelpShortcut) + 1,
            Width = Dim.Width (noHelpShortcut),
            Title = "Framed Shortcut",
            Key = Key.K.WithCtrl,
            Text = "Help: You can resize this",
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.RightResizable | ViewArrangement.BottomResizable
        };
        framedShortcut.Border!.Settings = BorderSettings.Title;

        //framedShortcut.Orientation = Orientation.Horizontal;

        if (framedShortcut.Padding is { })
        {
            framedShortcut.Padding.Thickness = new Thickness (0, 1, 0, 0);
            framedShortcut.Padding.Diagnostics = ViewDiagnosticFlags.Ruler;
        }

        if (framedShortcut.CommandView.Margin is { })
        {
            framedShortcut.CommandView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
            framedShortcut.HelpView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Error);
            framedShortcut.KeyView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        }

        framedShortcut.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Runnable);
        _app?.TopRunnableView.Add (framedShortcut);

        // Horizontal
        var progressShortcut = new Shortcut
        {
            Id = "progress",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd () - 1,
            Key = Key.F7,
            HelpText = "Horizontal"
        };

        progressShortcut.CommandView = new ProgressBar
        {
            Id = "progressPB",
            Text = "Progress",
            Title = "P",
            Fraction = 0.5f,
            Width = 10,
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous
        };
        progressShortcut.CommandView.Width = 10;
        progressShortcut.CommandView.Height = 1;
        progressShortcut.CommandView.CanFocus = false;

        Timer timer = new (10) { AutoReset = true };

        timer.Elapsed += (_, _) =>
                         {
                             if (progressShortcut.CommandView is ProgressBar pb)
                             {
                                 if (pb.Fraction >= 1.0f)
                                 {
                                     pb.Fraction = 0;
                                 }

                                 pb.Fraction += 0.01f;

                                 pb.SetNeedsDraw ();
                             }
                         };
        timer.Start ();

        _app?.TopRunnableView.Add (progressShortcut);

        var textField = new TextField { Id = "textFieldTF", Text = "Edit me", Width = 14, Height = 1 };

        var textFieldShortcut = new Shortcut
        {
            Id = "textField",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd () - 1,
            Key = Key.F8,
            HelpText = "TextField",
            CommandView = textField,
            MouseHighlightStates = MouseState.None
        };
        textField.MouseHighlightStates = MouseState.None;
        textField.CanFocus = false;

        _app?.TopRunnableView.Add (textFieldShortcut);

        var bgColorShortcut = new Shortcut
        {
            Id = "bgColor",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd (),
            Key = Key.F9,
            HelpText = "Cycles BG Color"
        };

        var bgColor = new ColorPicker16 { Id = "bgColorCP", BoxHeight = 1, BoxWidth = 1 };

        bgColorShortcut.Action += () =>
                                      {
                                          if (bgColor.SelectedColor == ColorName16.White)
                                          {
                                              bgColor.SelectedColor = ColorName16.Black;

                                              return;
                                          }

                                          bgColor.SelectedColor++;
                                      };

        bgColorShortcut.Activating += (s, args) =>
                                  {
                                      // Cycle colors only if activating didn't come from the commandview
                                      if (args.Context.TryGetSource (out View? ctxSource) == true && ctxSource is ColorPicker16)
                                      {

                                      }
                                  };

        bgColor.ValueChanged += (sendingView, args) =>
                                                            {
                                                                if (sendingView is { })
                                                                {
                                                                    _app!.TopRunnableView!.SetScheme (new Scheme (_app.TopRunnableView.GetScheme ())
                                                                    {
                                                                        Normal =
                                                                            new Attribute (_app.TopRunnableView
                                                                                               .GetAttributeForRole (VisualRole.Normal)
                                                                                               .Foreground,
                                                                                           args.NewValue,
                                                                                           _app.TopRunnableView
                                                                                               .GetAttributeForRole (VisualRole.Normal)
                                                                                               .Style)
                                                                    });
                                                                }
                                                            };
        bgColorShortcut.CommandView = bgColor;

        _app?.TopRunnableView.Add (bgColorShortcut);

        var appQuitShortcut = new Shortcut
        {
            Id = "appQuit",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd () - 1,
            Key = Key.Esc,
            BindKeyToApplication = true,
            Title = "Quit",
            HelpText = "App Scope"
        };

        appQuitShortcut.Activating += (sendingView, args) =>
                                      {
                                          args.Handled = true;
                                          (sendingView as View)?.App?.RequestStop ();
                                      };

        appQuitShortcut.Accepting += (sendingView, args) =>
                                     {
                                         args.Handled = true;
                                         (sendingView as View)?.App?.RequestStop ();
                                     };

        _app!.TopRunnableView!.Add (appQuitShortcut);

        foreach (Shortcut shortcut in _app!.TopRunnableView!.SubViews.OfType<Shortcut> ())
        {
            eventLog.SetViewToLog (shortcut);
            eventLog.SetViewToLog (shortcut.CommandView);
        }

        AlignKeys (true);

        SetCommandViewsCanFocus (false);

        return;

        void SetCommandViewsCanFocus (bool canFocus)
        {
            foreach (Shortcut peer in _app!.TopRunnableView!.SubViews.OfType<Shortcut> ())
            {
                if (peer.CanFocus)
                {
                    //peer.CanFocus = canFocus;
                    peer.CommandView.CanFocus = canFocus;
                    //peer.SetFocus ();
                }
            }
            canFocusShortcut.HelpText = $"Sets all CommandView's .CanFocus ({(canFocus)})";
        }

        void AlignKeys (bool align)
        {
            var max = 0;

            IEnumerable<Shortcut> toAlign = _app!.TopRunnableView!.SubViews.OfType<Shortcut> ().Where (s => !s.Y.Has<PosAnchorEnd> (out _));
            Shortcut [] shortcuts = toAlign as Shortcut [] ?? toAlign.ToArray ();

            if (align)
            {
                max = shortcuts.Select (s => s.Key.ToString ().GetColumns ()).Prepend (max).Max ();
                max = shortcuts.Select (s => s.KeyView.Text.GetColumns ()).Prepend (max).Max ();
            }

            foreach (Shortcut shortcut in shortcuts)
            {
                shortcut.MinimumKeyTextSize = max;
            }
        }
    }

    private void Button_Clicked (object? sender, CommandEventArgs e)
    {
        e.Handled = true;

        if (sender is View view)
        {
            MessageBox.Query (view.App!, "Hi", $"You clicked {view.Text}", Strings.btnOk);
        }
    }
}
