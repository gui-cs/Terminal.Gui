#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("All Views Tester", "Provides a test UI for all classes derived from View.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Tests")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Adornments")]
[ScenarioCategory ("Arrangement")]
public class AllViewsTester : Scenario
{
    private Dictionary<string, Type>? _viewClasses;
    private ListView? _classListView;
    private AdornmentsEditor? _adornmentsEditor;
    private ArrangementEditor? _arrangementEditor;
    private LayoutEditor? _layoutEditor;
    private ViewportSettingsEditor? _viewportSettingsEditor;
    private ViewPropertiesEditor? _propertiesEditor;

    private FrameView? _hostPane;
    private View? _curView;
    private EventLog? _eventLog;

    public override void Main ()
    {
        // Don't create a sub-win (Scenario.Win); just use Application.Top
        Application.Init ();

        var app = new Window
        {
            Title = GetQuitKeyAndName (),
        };

        // Set the BorderStyle we use for all subviews, but disable the app border thickness
        app.Border!.LineStyle = LineStyle.Heavy;
        app.Border.Thickness = new (0);


        _viewClasses = GetAllViewClassesCollection ()
                       .OrderBy (t => t.Name)
                       .Select (t => new KeyValuePair<string, Type> (t.Name, t))
                       .ToDictionary (t => t.Key, t => t.Value);

        _classListView = new ()
        {
            Title = "Classes [_1]",
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Fill (),
            AllowsMarking = false,
            SelectedItem = 0,
            Source = new ListWrapper<string> (new (_viewClasses.Keys.ToList ())),
            SuperViewRendersLineCanvas = true
        };
        _classListView.Border!.Thickness = new (1);

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

        _classListView.Accepting += (sender, args) =>
                                    {
                                        _curView?.SetFocus ();
                                        args.Handled = true;
                                    };

        _adornmentsEditor = new ()
        {
            Title = "Adornments [_2]",
            X = Pos.Right (_classListView) - 1,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            AutoSelectViewToEdit = false,
            AutoSelectAdornments = false,
            SuperViewRendersLineCanvas = true,
        };
        _adornmentsEditor.Border!.Thickness = new (1);
        _adornmentsEditor.ExpanderButton!.Orientation = Orientation.Horizontal;
        _adornmentsEditor.ExpanderButton.Enabled = false;

        _arrangementEditor = new ()
        {
            Title = "Arrangement [_3]",
            X = Pos.Right (_classListView) - 1,
            Y = Pos.Bottom (_adornmentsEditor) - Pos.Func (_ => _adornmentsEditor.Frame.Height == 1 ? 0 : 1),
            Width = Dim.Width (_adornmentsEditor),
            Height = Dim.Fill (),
            AutoSelectViewToEdit = false,
            AutoSelectAdornments = false,
            SuperViewRendersLineCanvas = true
        };
        _arrangementEditor.ExpanderButton!.Orientation = Orientation.Horizontal;

        _arrangementEditor.ExpanderButton.CollapsedChanging += (sender, args) =>
                                                               {
                                                                   _adornmentsEditor.ExpanderButton.Collapsed = args.NewValue;
                                                               };
        _arrangementEditor.Border!.Thickness = new (1);

        _layoutEditor = new ()
        {
            Title = "Layout [_4]",
            X = Pos.Right (_arrangementEditor) - 1,
            Y = 0,
            //Width = Dim.Fill (), // set below
            Height = Dim.Auto (),
            CanFocus = true,
            AutoSelectViewToEdit = false,
            AutoSelectAdornments = false,
            SuperViewRendersLineCanvas = true
        };
        _layoutEditor.Border!.Thickness = new (1, 1, 1, 0);

        _viewportSettingsEditor = new ()
        {
            Title = "ViewportSettings [_5]",
            X = Pos.Right (_arrangementEditor) - 1,
            Y = Pos.Bottom (_layoutEditor) - Pos.Func (_ => _layoutEditor.Frame.Height == 1 ? 0 : 1),
            Width = Dim.Width (_layoutEditor),
            Height = Dim.Auto (),
            CanFocus = true,
            AutoSelectViewToEdit = false,
            AutoSelectAdornments = false,
            SuperViewRendersLineCanvas = true
        };
        _viewportSettingsEditor.Border!.Thickness = new (1, 1, 1, 1);

        _propertiesEditor = new ()
        {
            Title = "View Properties [_6]",
            X = Pos.Right (_adornmentsEditor) - 1,
            Y = Pos.Bottom (_viewportSettingsEditor) - Pos.Func (_ => _viewportSettingsEditor.Frame.Height == 1 ? 0 : 1),
            Width = Dim.Width (_layoutEditor),
            Height = Dim.Auto (),
            CanFocus = true,
            SuperViewRendersLineCanvas = true
        };
        _propertiesEditor.Border!.Thickness = new (1, 1, 1, 0);

        _eventLog = new ()
        {
            // X = Pos.Right(_layoutEditor),
            SuperViewRendersLineCanvas = true
        };
        _eventLog.Border!.Thickness = new (1);
        _eventLog.X = Pos.AnchorEnd () - 1;
        _eventLog.Y = 0;

        _eventLog.Height = Dim.Height (_classListView);

        //_eventLog.Width = 30;

        _layoutEditor.Width = Dim.Fill (
                                        Dim.Func (
                                                  _ =>
                                                  {
                                                      if (_eventLog.NeedsLayout)
                                                      {
                                                          // We have two choices:
                                                          // 1) Call Layout explicitly
                                                          // 2) Throw LayoutException so Layout tries again
                                                          _eventLog.Layout ();
                                                          //throw new LayoutException ("_eventLog");
                                                      }

                                                      return _eventLog.Frame.Width;
                                                  }));

        _hostPane = new ()
        {
            Id = "_hostPane",
            X = Pos.Right (_adornmentsEditor),
            Y = Pos.Bottom (_propertiesEditor),
            Width = Dim.Width (_layoutEditor) - 2,
            Height = Dim.Fill (),
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            //SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base),
            Arrangement = ViewArrangement.LeftResizable | ViewArrangement.BottomResizable | ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Double,
            SuperViewRendersLineCanvas = true
        };
        _hostPane.Border!.SetScheme (app.GetScheme ());
        _hostPane.Padding!.Thickness = new (1);
        _hostPane.Padding.Diagnostics = ViewDiagnosticFlags.Ruler;
        _hostPane.Padding.SetScheme (app.GetScheme ());

        app.Add (_classListView, _adornmentsEditor, _arrangementEditor, _layoutEditor, _viewportSettingsEditor, _propertiesEditor, _eventLog, _hostPane);

        app.Initialized += App_Initialized;

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    private void App_Initialized (object? sender, EventArgs e)
    {
        _classListView!.SelectedItem = 0;
        _classListView.SetFocus ();
    }

    // TODO: Add Command.HotKey handler (pop a message box?)
    private void CreateCurrentView (Type type)
    {
        Debug.Assert (_curView is null);

        // If we are to create a generic Type
        if (type.IsGenericType)
        {
            // For each of the <T> arguments
            List<Type> typeArguments = new ();

            // use <object> or the original type if applicable
            foreach (Type arg in type.GetGenericArguments ())
            {
                if (arg.IsValueType && Nullable.GetUnderlyingType (arg) == null)
                {
                    typeArguments.Add (arg);
                }
                else
                {
                    typeArguments.Add (typeof (object));
                }
            }

            // And change what type we are instantiating from MyClass<T> to MyClass<object> or MyClass<T>
            type = type.MakeGenericType (typeArguments.ToArray ());
        }

        // Ensure the type does not contain any generic parameters
        if (type.ContainsGenericParameters)
        {
            Logging.Warning ($"Cannot create an instance of {type} because it contains generic parameters.");
            //throw new ArgumentException ($"Cannot create an instance of {type} because it contains generic parameters.");
            return;
        }

        // Instantiate view
        var view = (View)Activator.CreateInstance (type)!;
        _eventLog!.ViewToLog = view;

        if (view is IDesignable designable)
        {
            string settingsEditorDemoText = _propertiesEditor!.DemoText;
            designable.EnableForDesign (ref settingsEditorDemoText);
        }
        else
        {
            view.Text = _propertiesEditor!.DemoText;
            view.Title = "_Test Title";
        }


        view.Initialized += CurrentView_Initialized;
        view.SubViewsLaidOut += CurrentView_LayoutComplete;

        view.Id = "_curView";
        _curView = view;

        _hostPane!.Add (_curView);
        _layoutEditor!.ViewToEdit = _curView;
        _viewportSettingsEditor!.ViewToEdit = _curView;
        _arrangementEditor!.ViewToEdit = _curView;
        _propertiesEditor!.ViewToEdit = _curView;
        _curView.SetNeedsLayout ();
    }


    private void DisposeCurrentView ()
    {
        if (_curView != null)
        {
            _curView.Initialized -= CurrentView_Initialized;
            _curView.SubViewsLaidOut -= CurrentView_LayoutComplete;
            _hostPane!.Remove (_curView);
            _layoutEditor!.ViewToEdit = null;
            _viewportSettingsEditor!.ViewToEdit = null;
            _arrangementEditor!.ViewToEdit = null;
            _propertiesEditor!.ViewToEdit = null;

            _curView.Dispose ();
            _curView = null;
        }
    }

    private static List<Type> GetAllViewClassesCollection ()
    {
        List<Type> types = typeof (View).Assembly.GetTypes ()
                                        .Where (
                                                myType => myType is { IsClass: true, IsAbstract: false, IsPublic: true }
                                                          && myType.IsSubclassOf (typeof (View)))
                                        .ToList ();

        types.Add (typeof (View));

        return types;
    }

    private void CurrentView_LayoutComplete (object? sender, LayoutEventArgs args) { UpdateHostTitle (_curView); }

    private void UpdateHostTitle (View? view) { _hostPane!.Title = $"{view!.GetType ().Name} [_0]"; }

    private void CurrentView_Initialized (object? sender, EventArgs e)
    {
        if (sender is not View view)
        {
            return;
        }

        if (view.Width == Dim.Absolute (0) || view.Width is null)
        {
            view.Width = Dim.Fill ();
        }

        if (view.Height == Dim.Absolute (0) || view.Height is null)
        {
            view.Height = Dim.Fill ();
        }

        UpdateHostTitle (view);
    }

    public override List<Key> GetDemoKeyStrokes ()
    {
        var keys = new List<Key> ();

        for (int i = 0; i < GetAllViewClassesCollection ().Count; i++)
        {
            keys.Add (Key.CursorDown);
        }

        return keys;
    }
}
