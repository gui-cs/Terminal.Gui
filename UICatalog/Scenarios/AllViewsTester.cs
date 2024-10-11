using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("All Views Tester", "Provides a test UI for all classes derived from View.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Tests")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Adornments")]
public class AllViewsTester : Scenario
{
    private readonly List<string> _dimNames = new () { "Auto", "Percent", "Fill", "Absolute" };

    // TODO: This is missing some
    private readonly List<string> _posNames = new () { "Percent", "AnchorEnd", "Center", "Absolute" };
    private ListView _classListView;
    private View _curView;
    private FrameView _hostPane;
    private AdornmentsEditor _adornmentsEditor;
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
    private RadioGroup _orientation;
    private string _demoText = "This, that, and the other thing.";
    private TextView _demoTextView;

    public override void Main ()
    {
        // Don't create a sub-win (Scenario.Win); just use Application.Top
        Application.Init ();
   //     ConfigurationManager.Apply ();

        var app = new Window
        {
            Title = GetQuitKeyAndName (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"]
        };

        _viewClasses = GetAllViewClassesCollection ()
                       .OrderBy (t => t.Name)
                       .Select (t => new KeyValuePair<string, Type> (t.Name, t))
                       .ToDictionary (t => t.Key, t => t.Value);

        _leftPane = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (DimAutoStyle.Content),
            Height = Dim.Fill (),
            CanFocus = true,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Title = "Classes"
        };

        _classListView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Fill (),
            AllowsMarking = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            SelectedItem = 0,
            Source = new ListWrapper<string> (new (_viewClasses.Keys.ToList ()))
        };

        _classListView.SelectedItemChanged += (s, args) =>
                                              {
                                                  // Dispose existing current View, if any
                                                  DisposeCurrentView ();

                                                  CreateCurrentView (_viewClasses.Values.ToArray () [_classListView.SelectedItem]);

                                                  // Force ViewToEdit to be the view and not a subview
                                                  if (_adornmentsEditor is { })
                                                  {
                                                      _adornmentsEditor.AutoSelectSuperView = _curView;

                                                      _adornmentsEditor.ViewToEdit = _curView;
                                                  }
                                              };
        _leftPane.Add (_classListView);

        _adornmentsEditor = new ()
        {
            X = Pos.Right (_leftPane),
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            BorderStyle = LineStyle.Single,
            AutoSelectViewToEdit = true,
            AutoSelectAdornments = false,
        };

        var expandButton = new ExpanderButton
        {
            CanFocus = false,
            Orientation = Orientation.Horizontal
        };
        _adornmentsEditor.Border.Add (expandButton);

        _settingsPane = new ()
        {
            X = Pos.Right (_adornmentsEditor),
            Y = 0, // for menu
            Width = Dim.Fill (),
            Height = Dim.Auto (),
            CanFocus = true,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Title = "Settings"
        };

        string [] radioItems = { "_Percent(x)", "_AnchorEnd", "_Center", "A_bsolute(x)" };

        _locationFrame = new ()
        {
            X = 0,
            Y = 0,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = "Location (Pos)",
            TabStop = TabBehavior.TabStop,
        };
        _settingsPane.Add (_locationFrame);

        var label = new Label { X = 0, Y = 0, Text = "X:" };
        _locationFrame.Add (label);
        _xRadioGroup = new () { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        _xRadioGroup.SelectedItemChanged += OnRadioGroupOnSelectedItemChanged;
        _xText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_xVal}" };

        _xText.Accepting += (s, args) =>
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

        radioItems = new [] { "P_ercent(y)", "A_nchorEnd", "C_enter", "Absolute(_y)" };
        label = new () { X = Pos.Right (_xRadioGroup) + 1, Y = 0, Text = "Y:" };
        _locationFrame.Add (label);
        _yText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_yVal}" };

        _yText.Accepting += (s, args) =>
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
        _yRadioGroup = new () { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        _yRadioGroup.SelectedItemChanged += OnRadioGroupOnSelectedItemChanged;
        _locationFrame.Add (_yRadioGroup);

        _sizeFrame = new ()
        {
            X = Pos.Right (_locationFrame),
            Y = Pos.Y (_locationFrame),
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = "Size (Dim)",
            TabStop = TabBehavior.TabStop,
        };

        radioItems = new [] { "Auto", "_Percent(width)", "_Fill(width)", "A_bsolute(width)" };
        label = new () { X = 0, Y = 0, Text = "Width:" };
        _sizeFrame.Add (label);
        _wRadioGroup = new () { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        _wRadioGroup.SelectedItemChanged += OnRadioGroupOnSelectedItemChanged;
        _wText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_wVal}" };

        _wText.Accepting += (s, args) =>
                         {
                             try
                             {
                                 switch (_wRadioGroup.SelectedItem)
                                 {
                                     case 1:
                                         _wVal = Math.Min (int.Parse (_wText.Text), 100);

                                         break;
                                     case 0:
                                     case 2:
                                     case 3:
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

        radioItems = new [] { "_Auto", "P_ercent(height)", "F_ill(height)", "Ab_solute(height)" };
        label = new () { X = Pos.Right (_wRadioGroup) + 1, Y = 0, Text = "Height:" };
        _sizeFrame.Add (label);
        _hText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_hVal}" };

        _hText.Accepting += (s, args) =>
                         {
                             try
                             {
                                 switch (_hRadioGroup.SelectedItem)
                                 {
                                     case 1:
                                         _hVal = Math.Min (int.Parse (_hText.Text), 100);

                                         break;
                                     case 0:
                                     case 2:
                                     case 3:
                                         _hVal = int.Parse (_hText.Text);

                                         break;
                                 }

                                 DimPosChanged (_curView);
                             }
                             catch
                             { }
                         };
        _sizeFrame.Add (_hText);

        _hRadioGroup = new () { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        _hRadioGroup.SelectedItemChanged += OnRadioGroupOnSelectedItemChanged;
        _sizeFrame.Add (_hRadioGroup);

        _settingsPane.Add (_sizeFrame);

        label = new () { X = 0, Y = Pos.Bottom (_sizeFrame), Text = "_Orientation:" };

        _orientation = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            RadioLabels = new [] { "Horizontal", "Vertical" },
            Orientation = Orientation.Horizontal
        };

        _orientation.SelectedItemChanged += (s, selected) =>
                                            {
                                                if (_curView is IOrientation orientatedView)
                                                {
                                                    orientatedView.Orientation = (Orientation)_orientation.SelectedItem;
                                                }
                                            };
        _settingsPane.Add (label, _orientation);

        label = new () { X = 0, Y = Pos.Bottom (_orientation), Text = "_Text:" };

        _demoTextView = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = Dim.Auto (minimumContentDim: 2),
            Text = _demoText
        };

        _demoTextView.ContentsChanged += (s, e) =>
                                         {
                                             _demoText = _demoTextView.Text;

                                             if (_curView is { })
                                             {
                                                 _curView.Text = _demoText;
                                             }
                                         };

        _settingsPane.Add (label, _demoTextView);

        _hostPane = new ()
        {
            X = Pos.Right (_adornmentsEditor),
            Y = Pos.Bottom (_settingsPane),
            Width = Dim.Fill (),
            Height = Dim.Fill (), // + 1 for status bar
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            ColorScheme = Colors.ColorSchemes ["Dialog"]
        };

        _hostPane.LayoutStarted += (sender, args) =>
                                   {

                                   };

        app.Add (_leftPane, _adornmentsEditor, _settingsPane, _hostPane);

        _classListView.SelectedItem = 0;
        _leftPane.SetFocus ();

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    private void OnRadioGroupOnSelectedItemChanged (object s, SelectedItemChangedArgs selected) { DimPosChanged (_curView); }

    // TODO: Add Command.HotKey handler (pop a message box?)
    private void CreateCurrentView (Type type)
    {
        Debug.Assert(_curView is null);

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

        if (view is IDesignable designable)
        {
            designable.EnableForDesign (ref _demoText);
        }
        else
        {
            view.Text = _demoText;
            view.Title = "_Test Title";
        }

        if (view is IOrientation orientatedView)
        {
            _orientation.SelectedItem = (int)orientatedView.Orientation;
            _orientation.Enabled = true;
        }
        else
        {
            _orientation.Enabled = false;
        }

        view.Initialized += CurrentView_Initialized;
        view.LayoutComplete += CurrentView_LayoutComplete;

        _curView = view;
        _hostPane.Add (_curView);
       // Application.Refresh();
    }

    private void DisposeCurrentView ()
    {
        if (_curView != null)
        {
            _curView.Initialized -= CurrentView_Initialized;
            _curView.LayoutComplete -= CurrentView_LayoutComplete;
            _hostPane.Remove (_curView);
            _curView.Dispose ();
            _curView = null;
        }
    }

    private void DimPosChanged (View view)
    {
        if (view == null || _updatingSettings)
        {
            return;
        }

        try
        {
            view.X = _xRadioGroup.SelectedItem switch
            {
                0 => Pos.Percent (_xVal),
                1 => Pos.AnchorEnd (),
                2 => Pos.Center (),
                3 => Pos.Absolute (_xVal),
                _ => view.X
            };

            view.Y = _yRadioGroup.SelectedItem switch
            {
                0 => Pos.Percent (_yVal),
                1 => Pos.AnchorEnd (),
                2 => Pos.Center (),
                3 => Pos.Absolute (_yVal),
                _ => view.Y
            };

            view.Width = _wRadioGroup.SelectedItem switch
            {
                0 => Dim.Auto (),
                1 => Dim.Percent (_wVal),
                2 => Dim.Fill (_wVal),
                3 => Dim.Absolute (_wVal),
                _ => view.Width
            };

            view.Height = _hRadioGroup.SelectedItem switch
            {
                0 => Dim.Auto (),
                1 => Dim.Percent (_hVal),
                2 => Dim.Fill (_hVal),
                3 => Dim.Absolute (_hVal),
                _ => view.Height
            };
        }
        catch (Exception e)
        {
            MessageBox.ErrorQuery ("Exception", e.Message, "Ok");
        }

        if (view.Width is DimAuto)
        {
            _wText.Text = "Auto";
            _wText.Enabled = false;
        }
        else
        {
            _wText.Text = $"{_wVal}";
            _wText.Enabled = true;
        }

        if (view.Height is DimAuto)
        {
            _hText.Text = "Auto";
            _hText.Enabled = false;
        }
        else
        {
            _hText.Text = $"{_hVal}";
            _hText.Enabled = true;
        }

        UpdateHostTitle (view);
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

    private void CurrentView_LayoutComplete (object sender, LayoutEventArgs args)
    {
        UpdateSettings (_curView);
        UpdateHostTitle (_curView);
    }

    private bool _updatingSettings = false;
    private void UpdateSettings (View view)
    {
        _updatingSettings = true;
        var x = view.X.ToString ();
        var y = view.Y.ToString ();

        try
        {
            _xRadioGroup.SelectedItem = _posNames.IndexOf (_posNames.First (s => x.Contains (s)));
            _yRadioGroup.SelectedItem = _posNames.IndexOf (_posNames.First (s => y.Contains (s)));
        }
        catch (InvalidOperationException e)
        {
            // This is a hack to work around the fact that the Pos enum doesn't have an "Align" value yet
            Debug.WriteLine ($"{e}");
        }

        _xText.Text = $"{view.Frame.X}";
        _yText.Text = $"{view.Frame.Y}";

        var w = view.Width.ToString ();
        var h = view.Height.ToString ();
        _wRadioGroup.SelectedItem = _dimNames.IndexOf (_dimNames.First (s => w.Contains (s)));
        _hRadioGroup.SelectedItem = _dimNames.IndexOf (_dimNames.First (s => h.Contains (s)));

        if (view.Width.Has<DimAuto> (out _))
        {
            _wText.Text = "Auto";
            _wText.Enabled = false;
        }
        else
        {
            _wText.Text = $"{view.Frame.Width}";
            _wText.Enabled = true;
        }

        if (view.Height.Has<DimAuto> (out _))
        {
            _hText.Text = "Auto";
            _hText.Enabled = false;
        }
        else
        {
            _hText.Text = $"{view.Frame.Height}";
            _hText.Enabled = true;
        }

        _updatingSettings = false;
    }

    private void UpdateHostTitle (View view) { _hostPane.Title = $"_Demo of {view.GetType ().Name}"; }

    private void CurrentView_Initialized (object sender, EventArgs e)
    {
        if (sender is not View view)
        {
            return;
        }

        if (!view.Width!.Has<DimAuto> (out _) || (view.Width is null || view.Frame.Width == 0))
        {
            view.Width = Dim.Fill ();
        }

        if (!view.Height!.Has<DimAuto> (out _) || (view.Height is null || view.Frame.Height == 0))
        {
            view.Height = Dim.Fill ();
        }

        UpdateSettings (view);

        UpdateHostTitle (view);
    }
}
