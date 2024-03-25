using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("All Views Tester", "Provides a test UI for all classes derived from View.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Tests")]
[ScenarioCategory ("Top Level Windows")]
public class AllViewsTester : Scenario
{
    private readonly List<string> _dimNames = new () { "Factor", "Fill", "Absolute" };

    // TODO: This is missing some
    private readonly List<string> _posNames = new () { "Factor", "AnchorEnd", "Center", "Absolute" };
    private ListView _classListView;
    private CheckBox _computedCheckBox;
    private View _curView;
    private FrameView _hostPane;
    private RadioGroup _hRadioGroup;
    private TextField _hText;
    private int _hVal;
    private FrameView _leftPane;
    private FrameView _locationFrame;

    // Settings
    private FrameView _settingsPane;
    private FrameView _sizeFrame;
    private Dictionary<string, Type> _viewClasses;
    private RadioGroup _wRadioGroup;
    private TextField _wText;
    private int _wVal;
    private RadioGroup _xRadioGroup;
    private TextField _xText;
    private int _xVal;
    private RadioGroup _yRadioGroup;
    private TextField _yText;
    private int _yVal;

    public override void Init ()
    {
        // Don't create a sub-win (Scenario.Win); just use Application.Top
        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
    }

    public override void Setup ()
    {
        var statusBar = new StatusBar (
                                       new StatusItem []
                                       {
                                           new (
                                                Application.QuitKey,
                                                $"{Application.QuitKey} to Quit",
                                                () => Quit ()
                                               ),
                                           new (
                                                KeyCode.F2,
                                                "~F2~ Toggle Frame Ruler",
                                                () =>
                                                {
                                                    View.Diagnostics ^=
                                                        ViewDiagnosticFlags.Ruler;
                                                    Top.SetNeedsDisplay ();
                                                }
                                               ),
                                           new (
                                                KeyCode.F3,
                                                "~F3~ Toggle Frame Padding",
                                                () =>
                                                {
                                                    View.Diagnostics ^=
                                                        ViewDiagnosticFlags.Padding;
                                                    Top.SetNeedsDisplay ();
                                                }
                                               )
                                       }
                                      );
        Top.Add (statusBar);

        _viewClasses = GetAllViewClassesCollection ()
                       .OrderBy (t => t.Name)
                       .Select (t => new KeyValuePair<string, Type> (t.Name, t))
                       .ToDictionary (t => t.Key, t => t.Value);

        _leftPane = new FrameView
        {
            X = 0,
            Y = 0,
            Width = 15,
            Height = Dim.Fill (1), // for status bar
            CanFocus = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Title = "Classes"
        };

        _classListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            AllowsMarking = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            SelectedItem = 0,
            Source = new ListWrapper (_viewClasses.Keys.ToList ())
        };
        _classListView.OpenSelectedItem += (s, a) => { _settingsPane.SetFocus (); };

        _classListView.SelectedItemChanged += (s, args) =>
                                              {
                                                  // Remove existing class, if any
                                                  if (_curView != null)
                                                  {
                                                      _curView.LayoutComplete -= LayoutCompleteHandler;
                                                      _hostPane.Remove (_curView);
                                                      _curView.Dispose ();
                                                      _curView = null;
                                                      _hostPane.Clear ();
                                                  }

                                                  _curView = CreateClass (_viewClasses.Values.ToArray () [_classListView.SelectedItem]);
                                              };
        _leftPane.Add (_classListView);

        _settingsPane = new FrameView
        {
            X = Pos.Right (_leftPane),
            Y = 0, // for menu
            Width = Dim.Fill (),
            Height = 10,
            CanFocus = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Title = "Settings"
        };
        _computedCheckBox = new CheckBox { X = 0, Y = 0, Text = "_Computed Layout", Checked = true };

        _computedCheckBox.Toggled += (s, e) =>
                                     {
                                         if (_curView != null)
                                         {
                                             _hostPane.LayoutSubviews ();
                                         }
                                     };
        _settingsPane.Add (_computedCheckBox);

        string [] radioItems = { "_Percent(x)", "_AnchorEnd(x)", "_Center", "A_t(x)" };

        _locationFrame = new FrameView
        {
            X = Pos.Left (_computedCheckBox),
            Y = Pos.Bottom (_computedCheckBox),
            Height = 3 + radioItems.Length,
            Width = 36,
            Title = "Location (Pos)"
        };
        _settingsPane.Add (_locationFrame);

        var label = new Label { X = 0, Y = 0, Text = "X:" };
        _locationFrame.Add (label);
        _xRadioGroup = new RadioGroup { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        _xRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);
        _xText = new TextField { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_xVal}" };

        _xText.TextChanged += (s, args) =>
                              {
                                  try
                                  {
                                      _xVal = int.Parse (_xText.Text);
                                      DimPosChanged (_curView);
                                  }
                                  catch
                                  { }
                              };
        _locationFrame.Add (_xText);

        _locationFrame.Add (_xRadioGroup);

        radioItems = new [] { "P_ercent(y)", "A_nchorEnd(y)", "C_enter", "At(_y)" };
        label = new Label { X = Pos.Right (_xRadioGroup) + 1, Y = 0, Text = "Y:" };
        _locationFrame.Add (label);
        _yText = new TextField { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_yVal}" };

        _yText.TextChanged += (s, args) =>
                              {
                                  try
                                  {
                                      _yVal = int.Parse (_yText.Text);
                                      DimPosChanged (_curView);
                                  }
                                  catch
                                  { }
                              };
        _locationFrame.Add (_yText);
        _yRadioGroup = new RadioGroup { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        _yRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);
        _locationFrame.Add (_yRadioGroup);

        _sizeFrame = new FrameView
        {
            X = Pos.Right (_locationFrame),
            Y = Pos.Y (_locationFrame),
            Height = 3 + radioItems.Length,
            Width = 40,
            Title = "Size (Dim)"
        };

        radioItems = new [] { "_Percent(width)", "_Fill(width)", "_Sized(width)" };
        label = new Label { X = 0, Y = 0, Text = "Width:" };
        _sizeFrame.Add (label);
        _wRadioGroup = new RadioGroup { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        _wRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);
        _wText = new TextField { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_wVal}" };

        _wText.TextChanged += (s, args) =>
                              {
                                  try
                                  {
                                      switch (_wRadioGroup.SelectedItem)
                                      {
                                          case 0:
                                              _wVal = Math.Min (int.Parse (_wText.Text), 100);

                                              break;
                                          case 1:
                                          case 2:
                                              _wVal = int.Parse (_wText.Text);

                                              break;
                                      }

                                      DimPosChanged (_curView);
                                  }
                                  catch
                                  { }
                              };
        _sizeFrame.Add (_wText);
        _sizeFrame.Add (_wRadioGroup);

        radioItems = new [] { "P_ercent(height)", "F_ill(height)", "Si_zed(height)" };
        label = new Label { X = Pos.Right (_wRadioGroup) + 1, Y = 0, Text = "Height:" };
        _sizeFrame.Add (label);
        _hText = new TextField { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_hVal}" };

        _hText.TextChanged += (s, args) =>
                              {
                                  try
                                  {
                                      switch (_hRadioGroup.SelectedItem)
                                      {
                                          case 0:
                                              _hVal = Math.Min (int.Parse (_hText.Text), 100);

                                              break;
                                          case 1:
                                          case 2:
                                              _hVal = int.Parse (_hText.Text);

                                              break;
                                      }

                                      DimPosChanged (_curView);
                                  }
                                  catch
                                  { }
                              };
        _sizeFrame.Add (_hText);

        _hRadioGroup = new RadioGroup { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        _hRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);
        _sizeFrame.Add (_hRadioGroup);

        _settingsPane.Add (_sizeFrame);

        _hostPane = new FrameView
        {
            X = Pos.Right (_leftPane),
            Y = Pos.Bottom (_settingsPane),
            Width = Dim.Fill (),
            Height = Dim.Fill (1), // + 1 for status bar
            ColorScheme = Colors.ColorSchemes ["Dialog"]
        };

        Top.Add (_leftPane, _settingsPane, _hostPane);

        _curView = CreateClass (_viewClasses.First ().Value);
    }

    // TODO: Add Command.HotKey handler (pop a message box?)
    private View CreateClass (Type type)
    {
        // If we are to create a generic Type
        if (type.IsGenericType)
        {
            // For each of the <T> arguments
            List<Type> typeArguments = new ();

            // use <object>
            foreach (Type arg in type.GetGenericArguments ())
            {
                typeArguments.Add (typeof (object));
            }

            // And change what type we are instantiating from MyClass<T> to MyClass<object>
            type = type.MakeGenericType (typeArguments.ToArray ());
        }

        // Instantiate view
        var view = (View)Activator.CreateInstance (type);

        // Set the colorscheme to make it stand out if is null by default
        if (view.ColorScheme == null)
        {
            view.ColorScheme = Colors.ColorSchemes ["Base"];
        }

        // If the view supports a Text property, set it so we have something to look at
        if (view.GetType ().GetProperty ("Text") != null)
        {
            try
            {
                view.GetType ()
                    .GetProperty ("Text")
                    ?.GetSetMethod ()
                    ?.Invoke (view, new [] { "Test Text" });
            }
            catch (TargetInvocationException e)
            {
                MessageBox.ErrorQuery ("Exception", e.InnerException.Message, "Ok");
                view = null;
            }
        }

        // If the view supports a Title property, set it so we have something to look at
        if (view != null && view.GetType ().GetProperty ("Title") != null)
        {
            if (view.GetType ().GetProperty ("Title").PropertyType == typeof (string))
            {
                view?.GetType ()
                    .GetProperty ("Title")
                    ?.GetSetMethod ()
                    ?.Invoke (view, new [] { "Test Title" });
            }
            else
            {
                view?.GetType ()
                    .GetProperty ("Title")
                    ?.GetSetMethod ()
                    ?.Invoke (view, new [] { "Test Title" });
            }
        }

        // If the view supports a Source property, set it so we have something to look at
        if (view != null && view.GetType ().GetProperty ("Source") != null && view.GetType ().GetProperty ("Source").PropertyType == typeof (IListDataSource))
        {
            var source = new ListWrapper (new List<string> { "Test Text #1", "Test Text #2", "Test Text #3" });
            view?.GetType ().GetProperty ("Source")?.GetSetMethod ()?.Invoke (view, new [] { source });
        }

        // Set Settings
        _computedCheckBox.Checked = view.LayoutStyle == LayoutStyle.Computed;

        view.Initialized += View_Initialized;

        // Add
        _hostPane.Add (view);
        _hostPane.SetNeedsDisplay ();

        return view;
    }

    private void DimPosChanged (View view)
    {
        if (view == null)
        {
            return;
        }

        LayoutStyle layout = view.LayoutStyle;

        try
        {
            //view.LayoutStyle = LayoutStyle.Absolute;

            view.X = _xRadioGroup.SelectedItem switch
                     {
                         0 => Pos.Percent (_xVal),
                         1 => Pos.AnchorEnd (_xVal),
                         2 => Pos.Center (),
                         3 => Pos.At (_xVal),
                         _ => view.X
                     };

            view.Y = _yRadioGroup.SelectedItem switch
                     {
                         0 => Pos.Percent (_yVal),
                         1 => Pos.AnchorEnd (_yVal),
                         2 => Pos.Center (),
                         3 => Pos.At (_yVal),
                         _ => view.Y
                     };

            view.Width = _wRadioGroup.SelectedItem switch
                         {
                             0 => Dim.Percent (_wVal),
                             1 => Dim.Fill (_wVal),
                             2 => Dim.Sized (_wVal),
                             _ => view.Width
                         };

            view.Height = _hRadioGroup.SelectedItem switch
                          {
                              0 => Dim.Percent (_hVal),
                              1 => Dim.Fill (_hVal),
                              2 => Dim.Sized (_hVal),
                              _ => view.Height
                          };
        }
        catch (Exception e)
        {
            MessageBox.ErrorQuery ("Exception", e.Message, "Ok");
        }

        UpdateTitle (view);
    }

    private List<Type> GetAllViewClassesCollection ()
    {
        List<Type> types = new ();

        foreach (Type type in typeof (View).Assembly.GetTypes ()
                                           .Where (
                                                   myType =>
                                                       myType.IsClass && !myType.IsAbstract && myType.IsPublic && myType.IsSubclassOf (typeof (View))
                                                  ))
        {
            types.Add (type);
        }

        types.Add (typeof (View));

        return types;
    }

    private void LayoutCompleteHandler (object sender, LayoutEventArgs args)
    {
        UpdateSettings (_curView);
        UpdateTitle (_curView);
    }

    private void Quit () { Application.RequestStop (); }

    private void UpdateSettings (View view)
    {
        var x = view.X.ToString ();
        var y = view.Y.ToString ();
        _xRadioGroup.SelectedItem = _posNames.IndexOf (_posNames.Where (s => x.Contains (s)).First ());
        _yRadioGroup.SelectedItem = _posNames.IndexOf (_posNames.Where (s => y.Contains (s)).First ());
        _xText.Text = $"{view.Frame.X}";
        _yText.Text = $"{view.Frame.Y}";

        var w = view.Width.ToString ();
        var h = view.Height.ToString ();
        _wRadioGroup.SelectedItem = _dimNames.IndexOf (_dimNames.Where (s => w.Contains (s)).First ());
        _hRadioGroup.SelectedItem = _dimNames.IndexOf (_dimNames.Where (s => h.Contains (s)).First ());
        _wText.Text = $"{view.Frame.Width}";
        _hText.Text = $"{view.Frame.Height}";
    }

    private void UpdateTitle (View view) { _hostPane.Title = $"{view.GetType ().Name} - {view.X}, {view.Y}, {view.Width}, {view.Height}"; }

    private void View_Initialized (object sender, EventArgs e)
    {
        var view = sender as View;

        //view.X = Pos.Center ();
        //view.Y = Pos.Center ();
        if (view.Width == null || view.Frame.Width == 0)
        {
            view.Width = Dim.Fill ();
        }

        if (view.Height == null || view.Frame.Height == 0)
        {
            view.Height = Dim.Fill ();
        }

        UpdateSettings (view);
        UpdateTitle (view);
    }
}
