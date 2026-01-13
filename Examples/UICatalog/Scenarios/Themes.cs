#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Themes", "Shows off Themes, Schemes, and VisualRoles.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Configuration")]
public sealed class Themes : Scenario
{
    private IApplication? _app;
    private View? _view;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        // Init
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        string [] options = ThemeManager.GetThemeNames ().Select (option => "_" + option).ToArray ();
        OptionSelector themeOptionSelector = new ()
        {
            Title = "_Themes",
            BorderStyle = LineStyle.Rounded,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Labels= options,
            Value = ThemeManager.GetThemeNames ().IndexOf (ThemeManager.Theme)
        };
        themeOptionSelector.Border!.Thickness = new (0, 1, 0, 0);
        themeOptionSelector.Margin!.Thickness = new (0, 0, 1, 0);

        themeOptionSelector.ValueChanged += (sender, args) =>
                                             {
                                                 if (sender is not OptionSelector optionSelector)
                                                 {
                                                     return;
                                                 }
                                                 string? newTheme = optionSelector.Labels! [(int)args.Value!] as string;
                                                 // strip off the leading underscore
                                                 ThemeManager.Theme = newTheme!.Substring (1);
                                                 ConfigurationManager.Apply ();
                                             };

        ThemeViewer themeViewer = new ()
        {
            X = Pos.Right (themeOptionSelector)
        };

        Dictionary<string, Type> viewClasses = GetAllViewClassesCollection ()
                                               .OrderBy (t => t.Name)
                                               .Select (t => new KeyValuePair<string, Type> (t.Name, t))
                                               .ToDictionary (t => t.Key, t => t.Value);

        CheckBox? allViewsCheckBox = new ()
        {
            Title = "_All Views",
            X = Pos.Right (themeViewer),
        };

        ListView viewListView = new ()
        {
            X = Pos.Right (themeViewer),
            Y = Pos.Bottom(allViewsCheckBox),
            Title = "_Views",
            BorderStyle = LineStyle.Rounded,
            Width = Dim.Auto (),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (new (viewClasses.Keys))
        };
        viewListView.Border!.Thickness = new (0, 1, 0, 0);
        viewListView.Margin!.Thickness = new (0, 0, 1, 0);

        viewListView.VerticalScrollBar.AutoShow = true;


        ViewPropertiesEditor viewPropertiesEditor = new ()
        {
            X = Pos.Right (viewListView),
            Width = Dim.Fill (),
            Height = Dim.Auto (),
        };

        FrameView viewFrame = new ()
        {
            X = Pos.Right (viewListView),
            Y = Pos.Bottom(viewPropertiesEditor),
            Title = "The View",
            BorderStyle = LineStyle.Rounded,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            TabStop = TabBehavior.TabStop
        };
        viewFrame.Border!.Thickness = new (0, 1, 0, 0);

        viewListView.SelectedItemChanged += (_, args) =>
                                            {
                                                if (_view is not null)
                                                {
                                                    viewPropertiesEditor.ViewToEdit = null;
                                                    viewFrame.Remove (_view);
                                                    _view.Dispose ();
                                                    _view = null;
                                                }

                                                _view = CreateView (viewClasses [(args.Value as string)!]);

                                                if (_view is not null)
                                                {
                                                    viewFrame.Add (_view);
                                                    viewPropertiesEditor.ViewToEdit = _view;
                                                }
                                            };


        appWindow.Add (themeOptionSelector, themeViewer, allViewsCheckBox, viewListView, viewPropertiesEditor, viewFrame);

        viewListView.SelectedItem = 0;

        themeViewer.SchemeNameChanging += (_, args) =>
                                          {
                                              if (_view is not null)
                                              {
                                                  _app!.TopRunnableView!.SchemeName = args.NewValue;

                                                  if (_view.HasScheme)
                                                  {
                                                      _view.SetScheme (null);
                                                  }

                                                  _view.SchemeName = args.NewValue;
                                              }
                                          };

        AllViewsView? allViewsView = null;

        allViewsCheckBox.CheckedStateChanged += (_, args) =>
                                                {
                                                    if (args.Value == CheckState.Checked)
                                                    {
                                                        viewListView.Visible = false;
                                                        appWindow.Remove (viewFrame);

                                                        allViewsView = new AllViewsView ()
                                                        {
                                                            X = Pos.Right (themeViewer),
                                                            Y = Pos.Bottom (viewPropertiesEditor),
                                                            Title = "All Views - Focused: {None}",
                                                            BorderStyle = LineStyle.Rounded,
                                                            Width = Dim.Fill (),
                                                            Height = Dim.Fill (),
                                                            TabStop = TabBehavior.TabStop
                                                        };

                                                        allViewsView.FocusedChanged += (_, a) =>
                                                                                       {
                                                                                           allViewsView.Title =
                                                                                               $"All Views - Focused: {a.NewFocused?.Title}";
                                                                                           viewPropertiesEditor.ViewToEdit = a.NewFocused?.SubViews.ElementAt(0);

                                                                                       };
                                                        appWindow.Add (allViewsView);
                                                    }
                                                    else
                                                    {
                                                        appWindow.Remove (allViewsView);
                                                        allViewsView!.Dispose ();
                                                        allViewsView = null;

                                                        appWindow.Add (viewFrame);
                                                        viewListView.Visible = true;
                                                    }
                                                };

        // Run - Start the application.
        app.Run (appWindow);
        viewFrame.Dispose ();
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

    private View? CreateView (Type type)
    {
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
            return null;
        }

        // Instantiate view
        View view = (View)Activator.CreateInstance (type)!;
        string demoText = "This, that, and the other thing.";

        if (view is IDesignable designable)
        {
            designable.EnableForDesign (ref demoText);
        }
        else
        {
            view.Text = demoText;
            view.Title = "_Test Title";
        }

        view.Initialized += OnViewInitialized;

        return view;
    }

    private void OnViewInitialized (object? sender, EventArgs e)
    {
        if (sender is not View view)
        {
            return;
        }

        if (view.Width == Dim.Absolute (0))
        {
            view.Width = Dim.Fill ();
        }

        if (view.Height == Dim.Absolute (0))
        {
            view.Height = Dim.Fill ();
        }
    }
}