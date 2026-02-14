#nullable enable

using Timer = System.Timers.Timer;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Shortcuts", "Illustrates Shortcut class.")]
[ScenarioCategory ("Controls")]
public class Shortcuts : Scenario
{
    private IApplication? _app;
    private Window? _window;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        _app = Application.Create ();
        _app.Init ();
        _window = new Window ();

        _window.IsRunningChanged += HandleOnIsRunningChanged;

        _app.Run (_window);
        _window.Dispose ();
        _app.Dispose ();
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

        _window!.Title = GetQuitKeyAndName ();

        EventLog eventLog = new ()
        {
            Id = "eventLog",
            X = Pos.AnchorEnd (),
            Y = 1,
            Height = Dim.Fill (4),
            SchemeName = "Runnable",
            BorderStyle = LineStyle.Double,
            Title = "E_vents"
        };

        eventLog.Width = Dim.Auto (minimumContentDim: 40, maximumContentDim: Dim.Percent (50));
        _window.Add (eventLog);

        CheckBox canFocusCb = new ()
        {
            X = Pos.Left (eventLog),
            Y = 0,
            Id = "canFocusCB",
            Text = $"*._CommandView.CanFocus",
            CanFocus = false,
        };
        _window.Add (canFocusCb);

        canFocusCb.ValueChanged += (_, args) =>
                                {
                                    SetCommandViewsCanFocus (args.NewValue == CheckState.Checked);
                                };

        Shortcut alignKeysShortcut = new ()
        {
            Id = "alignKeys",
            Width = Dim.Fill () - Dim.Width (eventLog),
            HelpText = "Fill to log",
            CommandView = new CheckBox { Text = "_Align Keys", CanFocus = false, MouseHighlightStates = MouseState.None, Value = CheckState.Checked },
            Key = Key.F5.WithCtrl.WithAlt.WithShift
        };

        ((CheckBox)alignKeysShortcut.CommandView).ValueChanging += (_, a) =>
                                                                   {
                                                                       if (alignKeysShortcut.CommandView is not CheckBox)
                                                                       {
                                                                           return;
                                                                       }
                                                                       bool align = a.NewValue == CheckState.Checked;
                                                                       AlignKeys (align);
                                                                   };

        _window.Add (alignKeysShortcut);

        Shortcut commandFirstShortcut = new ()
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
                                                                         if (commandFirstShortcut.CommandView is not CheckBox)
                                                                         {
                                                                             return;
                                                                         }

                                                                         foreach (Shortcut peer in _window.SubViews.OfType<Shortcut> ())
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

        _window.Add (commandFirstShortcut);

        Shortcut appShortcut = new ()
        {
            Id = "appShortcut",
            X = 0,
            Y = Pos.Bottom (commandFirstShortcut),
            Width = 50,
            Title = "A_pp Shortcut",
            Key = Key.F1,
            Text = "Width is 50",
            BindKeyToApplication = true
        };

        appShortcut.Activated += (_, _) => { MessageBox.Query (_app!, "App Shortcut", "You activated the App scoped shortcut!", Strings.btnOk); };

        _window.Add (appShortcut);

        Shortcut buttonShortcut = new ()
        {
            Id = "button",
            X = 0,
            Y = Pos.Bottom (appShortcut),
            Width = Dim.Fill (eventLog),
            HelpText = "Accepting pops MB",
            CommandView = new Button { Id = "buttonBtn", Title = "_Button", ShadowStyle = ShadowStyle.None },
            Key = Key.K
        };
        buttonShortcut.Activated += ButtonShortcutOnActivated;

        _window.Add (buttonShortcut);

        Shortcut optionSelectorShortcut = new ()
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

        _window.Add (optionSelectorShortcut);

        Shortcut sliderShortcut = new ()
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

        _window.Add (sliderShortcut);

        ListView listView = new ()
        {
            Id = "listViewLV",
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = "ListView",
            BorderStyle = LineStyle.Single
        };
        listView.EnableForDesign ();

        Shortcut listViewShortcut = new ()
        {
            Id = "listView",
            X = 0,
            Y = Pos.Bottom (sliderShortcut),
            Width = Dim.Fill (eventLog),
            HelpText = "A ListView with Border",
            CommandView = listView,
            Key = Key.F5.WithCtrl
        };

        _window.Add (listViewShortcut);

        Shortcut noCommandShortcut = new ()
        {
            Id = "noCommand",
            X = 0,
            Y = Pos.Bottom (listViewShortcut),
            Width = Dim.Width (listViewShortcut),
            HelpText = "No Command",
            Key = Key.D0
        };

        _window.Add (noCommandShortcut);

        Shortcut noKeyShortcut = new ()
        {
            Id = "noKey",
            X = 0,
            Y = Pos.Bottom (noCommandShortcut),
            Width = Dim.Width (noCommandShortcut),
            Title = "No Ke_y",
            HelpText = "Keyless"
        };

        _window.Add (noKeyShortcut);

        Shortcut noHelpShortcut = new ()
        {
            Id = "noHelp",
            X = 0,
            Y = Pos.Bottom (noKeyShortcut),
            Width = Dim.Width (noKeyShortcut),
            Key = Key.F6,
            Title = "Not _very much help",
            HelpText = ""
        };

        _window.Add (noHelpShortcut);
        noHelpShortcut.SetFocus ();

        Shortcut framedShortcut = new ()
        {
            Id = "framed",
            X = 0,
            Y = Pos.Bottom (noHelpShortcut) + 1,
            Width = Dim.Width (noHelpShortcut),
            Title = "Frame_d Shortcut",
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
        _window.Add (framedShortcut);

        // Horizontal
        Shortcut progressShortcut = new ()
        {
            Id = "progress",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd () - 1,
            Key = Key.F7,
            HelpText = "Cycle style"
        };

        ProgressBar progressBar = new ()
        {
            Id = "progressPB",
            Text = "Progress",
            Title = "P",
            Fraction = 0.5f,
            Width = 10,
            Height = 1,
            ProgressBarStyle = ProgressBarStyle.Continuous
        };
        progressShortcut.CommandView = progressBar;

        Timer timer = new (10) { AutoReset = true };

        timer.Elapsed += (_, _) =>
                         {
                             if (progressShortcut.CommandView is not ProgressBar pb)
                             {
                                 return;
                             }

                             if (pb.Fraction >= 1.0f)
                             {
                                 pb.Fraction = 0;
                             }

                             pb.Fraction += 0.01f;

                             pb.SetNeedsDraw ();
                         };
        timer.Start ();

        progressShortcut.Action = () =>
                                  {
                                      progressBar.ProgressBarFormat = progressBar.ProgressBarFormat == ProgressBarFormat.Simple
                                                                          ? ProgressBarFormat.SimplePlusPercentage
                                                                          : ProgressBarFormat.Simple;
                                  };
        _window.Add (progressShortcut);

        TextField textField = new () { Id = "textFieldTF", Text = "Edit me", Width = 14, Height = 1 };

        Shortcut textFieldShortcut = new ()
        {
            Id = "textField",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd () - 1,
            Key = Key.F8,
            HelpText = "TextField",
            CommandView = textField,
            MouseHighlightStates = MouseState.None
        };

        textFieldShortcut.Activated += (_, _) => { MessageBox.Query (_app!, "Hi", $"You entered \"{textField.Text}\"", Strings.btnOk); };

        _window.Add (textFieldShortcut);

        // Set the CommandView to a ColorPicker16. This demonstrates how to support handling direct value changes
        // when the user activates the CommandView and cycling the value if the user activates any other part
        // of the Shortcut. The trick is to mark the Activating event as handled if the source of the command
        // was the CommandView.
        ColorPicker16 bgColor = new () { Id = "bgColorCP", BoxHeight = 1, BoxWidth = 1 };

        Shortcut bgColorShortcut = new ()
        {
            Id = "bgColor",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd (),
            Key = Key.F9,
            HelpText = "Cycles BG Color",
            CommandView = bgColor
        };

        bgColorShortcut.Activating += (_, args) =>
                                      {
                                          args.Handled = true;

                                          // Cycle colors only if activating didn't come from the CommandView
                                          if (args.Context?.Binding is null || args.Context.TryGetSource (out View? ctxSource) && ctxSource == bgColor)
                                          { }
                                          else
                                          {
                                              if (bgColor.SelectedColor == ColorName16.White)
                                              {
                                                  bgColor.SelectedColor = ColorName16.Black;

                                                  return;
                                              }

                                              bgColor.SelectedColor++;
                                          }
                                      };

        bgColor.ValueChanged += (sendingView, args) =>
                                {
                                    if (sendingView is { })
                                    {
                                        _window.SetScheme (new Scheme (_window.GetScheme ())
                                        {
                                            Normal = new Attribute (_window.GetAttributeForRole (VisualRole.Normal).Foreground,
                                                                    args.NewValue,
                                                                    _window.GetAttributeForRole (VisualRole.Normal).Style)
                                        });
                                    }
                                };
        _window.Add (bgColorShortcut);

        Shortcut appQuitShortcut = new ()
        {
            Id = "appQuit",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd () - 1,
            Key = Key.Esc.WithShift,
            BindKeyToApplication = true,
            Title = "_Quit",
            HelpText = "App Scope",
            Action = () => _app?.RequestStop ()
        };

        _window.Add (appQuitShortcut);

        foreach (Shortcut shortcut in _window.SubViews.OfType<Shortcut> ())
        {
            eventLog.SetViewToLog (shortcut);
            eventLog.SetViewToLog (shortcut.CommandView);
        }

        AlignKeys (true);

        SetCommandViewsCanFocus (false);

        alignKeysShortcut.SetFocus ();

        return;

        void ButtonShortcutOnActivated (object? s, EventArgs<ICommandContext?> _)
        {
            if (s is View view)
            {
                MessageBox.Query (view.App!, "Hi", $"You clicked {view.Text}", Strings.btnOk);
            }
        }

        void SetCommandViewsCanFocus (bool canFocus)
        {
            View? focused = _window.MostFocused;

            foreach (Shortcut peer in _window.SubViews.OfType<Shortcut> ())
            {
                if (peer.CanFocus)
                {
                    peer.CommandView.CanFocus = canFocus;
                }
            }
            focused?.SetFocus ();
        }

        void AlignKeys (bool align)
        {
            var max = 0;

            IEnumerable<Shortcut> toAlign = _window.SubViews.OfType<Shortcut> ().Where (s => !s.Y.Has<PosAnchorEnd> (out _));
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
}
