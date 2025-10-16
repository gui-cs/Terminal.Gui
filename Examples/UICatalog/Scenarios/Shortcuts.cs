#nullable enable

using System.Collections.ObjectModel;
using Timer = System.Timers.Timer;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Shortcuts", "Illustrates Shortcut class.")]
[ScenarioCategory ("Controls")]
public class Shortcuts : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        Window app = new ();

        app.Loaded += App_Loaded;

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    // Setting everything up in Loaded handler because we change the
    // QuitKey and it only sticks if changed after init
    private void App_Loaded (object? sender, EventArgs e)
    {
        Application.QuitKey = Key.F4.WithCtrl;
        Application.Top!.Title = GetQuitKeyAndName ();

        ObservableCollection<string> eventSource = new ();

        var eventLog = new ListView
        {
            Id = "eventLog",
            X = Pos.AnchorEnd (),
            Y = 0,
            Height = Dim.Fill (4),
            SchemeName = "TopLevel",
            Source = new ListWrapper<string> (eventSource),
            BorderStyle = LineStyle.Double,
            Title = "E_vents"
        };

        eventLog.Width = Dim.Func (
                                   _ => Math.Min (
                                                  Application.Top.Viewport.Width / 2,
                                                  eventLog?.MaxLength + eventLog!.GetAdornmentsThickness ().Horizontal ?? 0));

        eventLog.Width = Dim.Func (
                                   _ => Math.Min (
                                                  eventLog.SuperView!.Viewport.Width / 2,
                                                  eventLog?.MaxLength + eventLog!.GetAdornmentsThickness ().Horizontal ?? 0));
        Application.Top.Add (eventLog);

        var alignKeysShortcut = new Shortcut
        {
            Id = "alignKeysShortcut",
            X = 0,
            Y = 0,
            Width = Dim.Fill ()! - Dim.Width (eventLog),
            HelpText = "Fill to log",
            CommandView = new CheckBox
            {
                Text = "_Align Keys",
                CanFocus = false,
                HighlightStates = MouseState.None
            },
            Key = Key.F5.WithCtrl.WithAlt.WithShift
        };

        // ((CheckBox)vShortcut3.CommandView).CheckedStateChanging += (_, args) =>
        ((CheckBox)alignKeysShortcut.CommandView).CheckedStateChanging += (s, e) =>
                                                                          {
                                                                              if (alignKeysShortcut.CommandView is CheckBox cb)
                                                                              {
                                                                                  eventSource.Add (
                                                                                                   $"{alignKeysShortcut.Id}.CommandView.CheckedStateChanging: {cb.Text}");
                                                                                  eventLog.MoveDown ();

                                                                                  var max = 0;

                                                                                  IEnumerable<View> toAlign =
                                                                                      Application.Top.SubViews.Where (
                                                                                       v => v is Shortcut { Width: not DimAbsolute });
                                                                                  IEnumerable<View> enumerable = toAlign as View [] ?? toAlign.ToArray ();

                                                                                  if (e.Result == CheckState.Checked)
                                                                                  {
                                                                                      max = (from Shortcut? peer in enumerable
                                                                                             select peer.Key.ToString ().GetColumns ()).Prepend (max)
                                                                                          .Max ();

                                                                                      foreach (View view in enumerable)
                                                                                      {
                                                                                          var peer = (Shortcut)view;
                                                                                          max = Math.Max (max, peer.KeyView.Text.GetColumns ());
                                                                                      }
                                                                                  }

                                                                                  foreach (View view in enumerable)
                                                                                  {
                                                                                      var peer = (Shortcut)view;
                                                                                      peer.MinimumKeyTextSize = max;
                                                                                  }
                                                                              }
                                                                          };
        Application.Top.Add (alignKeysShortcut);

        var commandFirstShortcut = new Shortcut
        {
            Id = "commandFirstShortcut",
            X = 0,
            Y = Pos.Bottom (alignKeysShortcut),
            Width = Dim.Fill ()! - Dim.Width (eventLog),
            HelpText = "Show _Command first",
            CommandView = new CheckBox
            {
                Text = "Command _First",
                CanFocus = false,
                HighlightStates = MouseState.None
            },
            Key = Key.F.WithCtrl
        };

        ((CheckBox)commandFirstShortcut.CommandView).CheckedState =
            commandFirstShortcut.AlignmentModes.HasFlag (AlignmentModes.EndToStart) ? CheckState.UnChecked : CheckState.Checked;

        ((CheckBox)commandFirstShortcut.CommandView).CheckedStateChanging += (s, e) =>
                                                                             {
                                                                                 if (commandFirstShortcut.CommandView is CheckBox cb)
                                                                                 {
                                                                                     eventSource.Add (
                                                                                                      $"{commandFirstShortcut.Id}.CommandView.CheckedStateChanging: {cb.Text}");
                                                                                     eventLog.MoveDown ();

                                                                                     IEnumerable<View> toAlign =
                                                                                         Application.Top.SubViews.Where (
                                                                                          v => v is Shortcut { Width: not DimAbsolute });
                                                                                     IEnumerable<View> enumerable = toAlign as View [] ?? toAlign.ToArray ();

                                                                                     foreach (View view in enumerable)
                                                                                     {
                                                                                         var peer = (Shortcut)view;

                                                                                         if (e.Result == CheckState.Checked)
                                                                                         {
                                                                                             peer.AlignmentModes &= ~AlignmentModes.EndToStart;
                                                                                         }
                                                                                         else
                                                                                         {
                                                                                             peer.AlignmentModes |= AlignmentModes.EndToStart;
                                                                                         }
                                                                                     }
                                                                                 }
                                                                             };

        Application.Top.Add (commandFirstShortcut);

        var canFocusShortcut = new Shortcut
        {
            Id = "canFocusShortcut",
            X = 0,
            Y = Pos.Bottom (commandFirstShortcut),
            Width = Dim.Fill ()! - Dim.Width (eventLog),
            Key = Key.F4,
            HelpText = "Changes all Command.CanFocus",
            CommandView = new CheckBox { Text = "_CanFocus" }
        };

        ((CheckBox)canFocusShortcut.CommandView).CheckedStateChanging += (s, e) =>
                                                                         {
                                                                             if (canFocusShortcut.CommandView is CheckBox cb)
                                                                             {
                                                                                 eventSource.Add ($"Toggle: {cb.Text}");
                                                                                 eventLog.MoveDown ();

                                                                                 //cb.CanFocus = e.NewValue == CheckState.Checked;

                                                                                 foreach (Shortcut peer in Application.Top.SubViews.Where (v => v is Shortcut)!)
                                                                                 {
                                                                                     if (peer.CanFocus)
                                                                                     {
                                                                                         peer.CommandView.CanFocus = e.Result == CheckState.Checked;
                                                                                     }
                                                                                 }
                                                                             }
                                                                         };
        Application.Top.Add (canFocusShortcut);

        var appShortcut = new Shortcut
        {
            Id = "appShortcut",
            X = 0,
            Y = Pos.Bottom (canFocusShortcut),
            Width = Dim.Fill (Dim.Func (_ => eventLog.Frame.Width)),
            Title = "A_pp Shortcut",
            Key = Key.F1,
            Text = "Width is DimFill",
            BindKeyToApplication = true
        };

        Application.Top.Add (appShortcut);

        var buttonShortcut = new Shortcut
        {
            Id = "buttonShortcut",
            X = 0,
            Y = Pos.Bottom (appShortcut),
            Width = Dim.Fill ()! - Dim.Width (eventLog),
            HelpText = "Accepting pops MB",
            CommandView = new Button
            {
                Title = "_Button",
                ShadowStyle = ShadowStyle.None,
                HighlightStates = MouseState.None
            },
            Key = Key.K
        };
        var button = (Button)buttonShortcut.CommandView;
        buttonShortcut.Accepting += Button_Clicked;

        Application.Top.Add (buttonShortcut);

        var radioGroupShortcut = new Shortcut
        {
            Id = "radioGroupShortcut",
            X = 0,
            Y = Pos.Bottom (buttonShortcut),
            Key = Key.F2,
            Width = Dim.Fill ()! - Dim.Width (eventLog),
            CommandView = new RadioGroup
            {
                Orientation = Orientation.Vertical,
                RadioLabels = ["O_ne", "T_wo", "Th_ree", "Fo_ur"]
            }
        };

        ((RadioGroup)radioGroupShortcut.CommandView).SelectedItemChanged += (o, args) =>
                                                                            {
                                                                                if (o is { })
                                                                                {
                                                                                    eventSource.Add (
                                                                                                     $"SelectedItemChanged: {o.GetType ().Name} - {args.SelectedItem}");
                                                                                    eventLog.MoveDown ();
                                                                                }
                                                                            };

        Application.Top.Add (radioGroupShortcut);

        var sliderShortcut = new Shortcut
        {
            Id = "sliderShortcut",
            X = 0,
            Y = Pos.Bottom (radioGroupShortcut),
            Width = Dim.Fill ()! - Dim.Width (eventLog),
            HelpText = "Sliders work!",
            CommandView = new Slider<string>
            {
                Orientation = Orientation.Horizontal,
                AllowEmpty = true
            },
            Key = Key.F5
        };

        ((Slider<string>)sliderShortcut.CommandView).Options = [new () { Legend = "A" }, new () { Legend = "B" }, new () { Legend = "C" }];
        ((Slider<string>)sliderShortcut.CommandView).SetOption (0);

        ((Slider<string>)sliderShortcut.CommandView).OptionsChanged += (o, args) =>
                                                                       {
                                                                           eventSource.Add (
                                                                                            $"OptionsChanged: {o?.GetType ().Name} - {string.Join (",", ((Slider<string>)o!)!.GetSetOptions ())}");
                                                                           eventLog.MoveDown ();
                                                                       };

        Application.Top.Add (sliderShortcut);

        var noCommandShortcut = new Shortcut
        {
            Id = "noCommandShortcut",
            X = 0,
            Y = Pos.Bottom (sliderShortcut),
            Width = Dim.Width (sliderShortcut),
            HelpText = "No Command",
            Key = Key.D0
        };

        Application.Top.Add (noCommandShortcut);

        var noKeyShortcut = new Shortcut
        {
            Id = "noKeyShortcut",
            X = 0,
            Y = Pos.Bottom (noCommandShortcut),
            Width = Dim.Width (noCommandShortcut),

            Title = "No Ke_y",
            HelpText = "Keyless"
        };

        Application.Top.Add (noKeyShortcut);

        var noHelpShortcut = new Shortcut
        {
            Id = "noHelpShortcut",
            X = 0,
            Y = Pos.Bottom (noKeyShortcut),
            Width = Dim.Width (noKeyShortcut),
            Key = Key.F6,
            Title = "Not _very much help",
            HelpText = ""
        };

        Application.Top.Add (noHelpShortcut);
        noHelpShortcut.SetFocus ();

        var framedShortcut = new Shortcut
        {
            Id = "framedShortcut",
            X = 0,
            Y = Pos.Bottom (noHelpShortcut) + 1,
            Title = "Framed",
            Key = Key.K.WithCtrl,
            Text = "Resize frame",
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.RightResizable | ViewArrangement.BottomResizable
        };
        framedShortcut.Orientation = Orientation.Horizontal;

        if (framedShortcut.Padding is { })
        {
            framedShortcut.Padding.Thickness = new (0, 1, 0, 0);
            framedShortcut.Padding.Diagnostics = ViewDiagnosticFlags.Ruler;
        }

        if (framedShortcut.CommandView.Margin is { })
        {
            framedShortcut.CommandView.Margin.SchemeName = framedShortcut.CommandView.SchemeName = "Error";
            framedShortcut.HelpView.Margin!.SchemeName = framedShortcut.HelpView.SchemeName = "Dialog";
            framedShortcut.KeyView.Margin!.SchemeName = framedShortcut.KeyView.SchemeName = "Menu";
        }

        framedShortcut.SchemeName = "TopLevel";
        Application.Top.Add (framedShortcut);

        // Horizontal
        var progressShortcut = new Shortcut
        {
            Id = "progressShortcut",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd () - 1,
            Key = Key.F7,
            HelpText = "Horizontal"
        };

        progressShortcut.CommandView = new ProgressBar
        {
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

        Timer timer = new (10)
        {
            AutoReset = true
        };

        timer.Elapsed += (o, args) =>
                         {
                             if (progressShortcut.CommandView is ProgressBar pb)
                             {
                                 if (pb.Fraction == 1.0)
                                 {
                                     pb.Fraction = 0;
                                 }

                                 pb.Fraction += 0.01f;

                                 Application.Wakeup ();

                                 pb.SetNeedsDraw ();
                             }
                         };
        timer.Start ();

        Application.Top.Add (progressShortcut);

        var textField = new TextField
        {
            Text = "Edit me",
            Width = 10,
            Height = 1
        };

        var textFieldShortcut = new Shortcut
        {
            Id = "textFieldShortcut",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd () - 1,
            Key = Key.F8,
            HelpText = "TextField",
            CanFocus = true,
            CommandView = textField
        };
        textField.CanFocus = true;

        Application.Top.Add (textFieldShortcut);

        var bgColorShortcut = new Shortcut
        {
            Id = "bgColorShortcut",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd (),
            Key = Key.F9,
            HelpText = "Cycles BG Color"
        };

        var bgColor = new ColorPicker16
        {
            BoxHeight = 1,
            BoxWidth = 1
        };

        bgColorShortcut.Selecting += (o, args) =>
                                     {
                                         //args.Cancel = true;
                                     };

        bgColorShortcut.Accepting += (o, args) =>
                                     {
                                         if (bgColor.SelectedColor == ColorName16.White)
                                         {
                                             bgColor.SelectedColor = ColorName16.Black;

                                             return;
                                         }

                                         bgColor.SelectedColor++;
                                         args.Handled = true;
                                     };

        bgColor.ColorChanged += (o, args) =>
                                {
                                    if (o is { })
                                    {
                                        eventSource.Add ($"ColorChanged: {o.GetType ().Name} - {args.Result}");
                                        eventLog.MoveDown ();

                                        Application.Top.SetScheme (
                                                                   new (Application.Top.GetScheme ())
                                                                   {
                                                                       Normal = new (
                                                                                     Application.Top!.GetAttributeForRole (VisualRole.Normal).Foreground,
                                                                                     args.Result,
                                                                                     Application.Top!.GetAttributeForRole (VisualRole.Normal).Style)
                                                                   });
                                    }
                                };
        bgColorShortcut.CommandView = bgColor;

        Application.Top.Add (bgColorShortcut);

        var appQuitShortcut = new Shortcut
        {
            Id = "appQuitShortcut",
            X = Pos.Align (Alignment.Start, AlignmentModes.IgnoreFirstOrLast, 1),
            Y = Pos.AnchorEnd () - 1,
            Key = Key.Esc,
            BindKeyToApplication = true,
            Title = "Quit",
            HelpText = "App Scope"
        };
        appQuitShortcut.Accepting += (o, args) => { Application.RequestStop (); };

        Application.Top.Add (appQuitShortcut);

        foreach (Shortcut shortcut in Application.Top.SubViews.OfType<Shortcut> ())
        {
            shortcut.Selecting += (o, args) =>
                                  {
                                      if (args.Handled)
                                      {
                                          return;
                                      }

                                      eventSource.Add ($"{shortcut!.Id}.Selecting: {shortcut!.CommandView.Text} {shortcut!.CommandView.GetType ().Name}");
                                      eventLog.MoveDown ();
                                  };

            shortcut.CommandView.Selecting += (o, args) =>
                                              {
                                                  if (args.Handled)
                                                  {
                                                      return;
                                                  }

                                                  eventSource.Add (
                                                                   $"{shortcut!.Id}.CommandView.Selecting: {shortcut!.CommandView.Text} {shortcut!.CommandView.GetType ().Name}");
                                                  eventLog.MoveDown ();
                                                  args.Handled = true;
                                              };

            shortcut.Accepting += (o, args) =>
                                  {
                                      eventSource.Add ($"{shortcut!.Id}.Accepting: {shortcut!.CommandView.Text} {shortcut!.CommandView.GetType ().Name}");
                                      eventLog.MoveDown ();

                                      // We don't want this to exit the Scenario
                                      args.Handled = true;
                                  };

            shortcut.CommandView.Accepting += (o, args) =>
                                              {
                                                  eventSource.Add (
                                                                   $"{shortcut!.Id}.CommandView.Accepting: {shortcut!.CommandView.Text} {shortcut!.CommandView.GetType ().Name}");
                                                  eventLog.MoveDown ();
                                              };
        }
    }

    private void Button_Clicked (object? sender, CommandEventArgs e)
    {
        e.Handled = true;
        var view = sender as View;
        MessageBox.Query ("Hi", $"You clicked {view?.Text}", "_Ok");
    }
}
