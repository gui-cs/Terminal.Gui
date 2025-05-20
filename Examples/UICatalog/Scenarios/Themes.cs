#nullable enable
using System.Diagnostics;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Themes", "Shows off Themes, Schemes, and VisualRoles.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Configuration")]

public sealed class Themes : Scenario
{
    private View? _view = null;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        ListView? themeListView = new ListView ()
        {
            Title = "_Themes",
            BorderStyle = LineStyle.Double,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Source = new ListWrapper<string> (new (ThemeManager.GetThemeNames ())),
            SelectedItem = ThemeManager.GetThemeNames ().IndexOf (ThemeManager.Theme)
        };

        themeListView.SelectedItemChanged += (sender, args) =>
                                             {
                                                 ListView? listView = sender as ListView;
                                                 string? newTheme = listView!.Source.ToList () [args.Item] as string;
                                                 ThemeManager.Theme = newTheme!;
                                                 ConfigurationManager.Apply ();
                                             };

        ThemeViewer? themeViewer = new ThemeViewer ()
        {
            X = Pos.Right (themeListView)
        };



        Dictionary<string, Type> viewClasses = GetAllViewClassesCollection ()
                                               .OrderBy (t => t.Name)
                                               .Select (t => new KeyValuePair<string, Type> (t.Name, t))
                                               .ToDictionary (t => t.Key, t => t.Value);

        ListView? viewListView = new ListView ()
        {
            X = Pos.Right (themeViewer),
            Title = "_Views",
            BorderStyle = LineStyle.Double,
            Width = Dim.Auto (),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (new (viewClasses.Keys)),
        };
        viewListView.VerticalScrollBar.AutoShow = true;

        FrameView? viewFrame = new FrameView ()
        {
            X = Pos.Right (viewListView),
            Title = "The View",
            BorderStyle = LineStyle.Single,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            TabStop = TabBehavior.TabStop
        };
        viewFrame.Border.Thickness = new (0, 1, 0, 0);

        viewListView.SelectedItemChanged += (sender, args) =>
                                            {
                                                ListView? listView = sender as ListView;

                                                if (_view is { })
                                                {
                                                    viewFrame.Remove (_view);
                                                    _view.Dispose ();
                                                    _view = null;
                                                }

                                                _view = CreateView (viewClasses [(args.Value as string)!]);

                                                if (_view is { })
                                                {
                                                    _view.CanFocus = false;
                                                    viewFrame.Add (_view);
                                                }
                                            };

        appWindow.Add (themeListView, themeViewer, viewListView, viewFrame);

        viewListView.SelectedItem = 0;

        themeViewer.SettingSchemeName += (sender, args) =>
                                         {
                                             if (_view is { })
                                             {
                                                 Application.Top.SchemeName = args.NewString;
                                                 //viewListView.SchemeName = args.NewString;
                                                 //viewFrame.SchemeName = args.NewString;

                                                 if (_view.HasScheme)
                                                 {
                                                     _view.SetScheme (null);
                                                 }
                                                 _view.SchemeName = args.NewString;
                                             }
                                         };

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
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
        var view = (View)Activator.CreateInstance (type)!;
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

        if (view.Width == Dim.Absolute (0) || view.Width is null)
        {
            view.Width = Dim.Fill ();
        }

        if (view.Height == Dim.Absolute (0) || view.Height is null)
        {
            view.Height = Dim.Fill ();
        }
    }
}

public class ThemeViewer : FrameView
{
    public ThemeViewer ()
    {
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        Height = Dim.Fill ();
        Width = Dim.Auto ();
        Title = $"{ThemeManager.Theme}";

        VerticalScrollBar.AutoShow = true;
        HorizontalScrollBar.AutoShow = true;

        SubViewsLaidOut += (sender, _) =>
                                     {
                                         if (sender is View sendingView)
                                         {
                                             sendingView.SetContentSize (sendingView.GetSizeRequiredForSubViews ());
                                         }
                                     };

        AddCommand (Command.Up, () => ScrollVertical (-1));
        AddCommand (Command.Down, () => ScrollVertical (1));

        AddCommand (Command.PageUp, () => ScrollVertical (-SubViews.OfType<SchemeViewer> ().First ().Frame.Height));
        AddCommand (Command.PageDown, () => ScrollVertical (SubViews.OfType<SchemeViewer> ().First ().Frame.Height));
        AddCommand (Command.Start, () => { Viewport = Viewport with { Y = 0 }; return true; });
        AddCommand (Command.End, () => { Viewport = Viewport with { Y = GetContentSize ().Height }; return true; });

        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.Add (PopoverMenu.DefaultKey, Command.Context);

        MouseBindings.Add (MouseFlags.Button1DoubleClicked, Command.Accept);
        MouseBindings.ReplaceCommands (MouseFlags.Button3Clicked, Command.Context);
        MouseBindings.ReplaceCommands (MouseFlags.Button1Clicked | MouseFlags.ButtonCtrl, Command.Context);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledLeft, Command.ScrollLeft);
        MouseBindings.Add (MouseFlags.WheeledRight, Command.ScrollRight);

        SchemeViewer? prevSchemeViewer = null;
        foreach (KeyValuePair<string, Scheme?> kvp in SchemeManager.GetSchemesForCurrentTheme ())
        {
            SchemeViewer? schemeViewer = new SchemeViewer ()
            {
                Id = $"schemeViewer for {kvp.Key}",
                SchemeName = kvp.Key,
            };
            if (prevSchemeViewer is { })
            {
                schemeViewer.Y = Pos.Bottom (prevSchemeViewer);
            }

            prevSchemeViewer = schemeViewer;
            base.Add (schemeViewer);
        }

        ThemeManager.ThemeChanged += OnThemeManagerOnThemeChanged;
    }

    /// <inheritdoc />
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);

        if (focused is { })
        {
            SchemeName = focused.Title;
        }
    }

    private void OnThemeManagerOnThemeChanged (object? _, StringPropertyEventArgs args)
    {
        Title = args.NewString!;
    }

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnThemeManagerOnThemeChanged;
        }
        base.Dispose (disposing);
    }
}

public class SchemeViewer : FrameView
{
    public SchemeViewer ()
    {
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        Height = Dim.Auto ();
        Width = Dim.Auto ();

        VisualRoleViewer? prevRoleViewer = null;
        foreach (VisualRole role in Enum.GetValues<VisualRole> ())
        {
            VisualRoleViewer? roleViewer = new VisualRoleViewer ()
            {
                Role = role,
            };
            if (prevRoleViewer is { })
            {
                roleViewer.Y = Pos.Bottom (prevRoleViewer);
            }
            base.Add (roleViewer);

            prevRoleViewer = roleViewer;
        }
    }

    /// <inheritdoc />
    protected override bool OnSettingSchemeName (in string? currentName, ref string? newName)
    {
        Title = newName ?? "null";

        foreach (VisualRoleViewer v in SubViews.OfType<VisualRoleViewer> ())
        {
            v.SchemeName = newName;
        }
        return base.OnSettingSchemeName (in currentName, ref newName);
    }
}

public class VisualRoleViewer : View
{
    public VisualRoleViewer ()
    {
        CanFocus = false;
        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);
    }

    private VisualRole? _role;

    public VisualRole? Role
    {
        get => _role;
        set
        {
            _role = value;
            Text = $"{Role?.ToString ()?.PadRight (10)} 0123456789 𝔽𝕆𝕆𝔹𝔸ℝ {SchemeName}";
        }
    }

    /// <inheritdoc />
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role != Role)
        {
            currentAttribute = GetAttributeForRole (Role!.Value);
            return true;
        }

        return base.OnGettingAttributeForRole (in role, ref currentAttribute);
    }
}