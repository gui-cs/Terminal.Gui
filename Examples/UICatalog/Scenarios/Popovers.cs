#nullable enable
using System.Collections.ObjectModel;
using System.Reflection;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Popovers", "Demonstrates the Popover<TView, TResult> infrastructure.")]
[ScenarioCategory ("Popups")]
[ScenarioCategory ("Controls")]
public class Popovers : Scenario
{
    private IApplication? _app;
    private Dictionary<string, Type>? _viewClasses;
    private ListView? _viewListView;
    private ListView? _popoverListView;
    private EventLog? _eventLog;
    private readonly ObservableCollection<string> _registeredPopovers = [];
    private readonly Dictionary<string, IPopover> _popoverInstances = [];

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        _app = Application.Create ();
        _app.Init ();

        using Window window = new ();
        window.Title = GetQuitKeyAndName ();
        window.Border!.LineStyle = LineStyle.Heavy;
        window.Border!.Thickness = new Thickness (0);

        // Get all View classes
        _viewClasses = GetAllViewClasses ()
                       .OrderBy (t => t.Name)
                       .Select (t => new KeyValuePair<string, Type> (GetFormattedTypeName (t), t))
                       .ToDictionary (t => t.Key, t => t.Value);

        // Left: View list
        _viewListView = new ListView
        {
            Title = "_Views",
            X = 0,
            Y = 0,
            Width = Dim.Percent (33),
            Height = Dim.Fill (),
            ShowMarks = false,
            SelectedItem = 0,
            BorderStyle = LineStyle.Single,
            Source = new ListWrapper<string> (new ObservableCollection<string> (_viewClasses.Keys.ToList ())),
        };
        _viewListView.Accepting += ViewListView_Accepting;

        // Middle: Registered popovers list
        _popoverListView = new ListView
        {
            Title = "_Popovers (click to show)",
            X = Pos.Right (_viewListView),
            Y = 0,
            Width = Dim.Percent (33),
            Height = Dim.Fill (),
            ShowMarks = false,
            BorderStyle = LineStyle.Dotted,
            Source = new ListWrapper<string> (_registeredPopovers),
        };
        _popoverListView.Accepting += PopoverListView_Accepting;

        // Right: Event log
        _eventLog = new EventLog { X = Pos.Right (_popoverListView), Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        _eventLog.SetViewToLog (window);
        window.Add (_viewListView, _popoverListView, _eventLog);

        _app.Run (window);

        _app.Dispose ();
        _app = null;
    }

    private void ViewListView_Accepting (object? sender, CommandEventArgs args)
    {
        if (_viewListView?.SelectedItem is not { } selectedIndex)
        {
            return;
        }

        Type viewType = _viewClasses!.Values.ToArray () [selectedIndex];
        args.Handled = true;

        try
        {
            // Create the view instance
            View? contentView = CreateViewInstance (viewType);

            if (contentView is null)
            {
                _eventLog?.Log ($"Failed to create instance of {viewType.Name}");

                return;
            }

            // Register the popover
            RegisterPopover (contentView);
        }
        catch (Exception ex)
        {
            _eventLog?.Log ($"Error creating popover for {viewType.Name}: {ex.Message}");
        }
    }

    private void PopoverListView_Accepting (object? sender, CommandEventArgs args)
    {
        if (_popoverListView?.SelectedItem is not { } selectedIndex || selectedIndex >= _registeredPopovers.Count)
        {
            return;
        }

        string popoverKey = _registeredPopovers [selectedIndex];

        if (!_popoverInstances.TryGetValue (popoverKey, out IPopover? popover))
        {
            return;
        }

        args.Handled = true;

        try
        {
                

        }
        catch (Exception ex)
        {
            _eventLog?.Log ($"Error showing popover {popoverKey}: {ex.Message}");
        }
    }

    private void RegisterPopover (View contentView)
    {
        string viewTypeName = GetFormattedTypeName (contentView.GetType ());

        try
        {
            // Determine TResult type from IValue<T> or use string
            Type? resultType = GetIValueResultType (contentView) ?? typeof (string);

            // Create Popover<TView, TResult> using reflection
            Type popoverType = typeof (Popover<,>).MakeGenericType (contentView.GetType (), resultType);
            object? popoverObj = Activator.CreateInstance (popoverType, contentView);

            if (popoverObj is not IPopover popover)
            {
                _eventLog?.Log ($"Failed to create popover for {viewTypeName}");

                return;
            }

            // Set up result extraction if not using IValue
            if (resultType == typeof (string))
            {

            }

            // Register with ApplicationPopover
            _app?.Popovers?.Register (popover);

            // Track the popover
            var popoverKey = $"{viewTypeName} → {resultType.Name}";
            _popoverInstances [popoverKey] = popover;
            _registeredPopovers.Add (popoverKey);

            _eventLog?.Log($"Registered: {popoverKey}");

            // Subscribe to IsOpenChanged if it's a Popover<,>
            //SubscribeToPopoverEvents (popoverObj, popoverKey);
        }
        catch (Exception ex)
        {
            _eventLog?.Log($"Error registering popover for {viewTypeName}: {ex.Message}");
        }
    }

    private void OnPopoverIsOpenChanged (object? sender, ValueChangedEventArgs<bool> e)
    {
       // _eventLog?.Log($"{typeName}: IsOpen changed from {e.OldValue} to {e.NewValue}");
    }

    private Type? GetIValueResultType (View view)
    {
        // Check if the view implements IValue<T>
        Type? iValueInterface = view.GetType ().GetInterfaces ().FirstOrDefault (i => i.IsGenericType && i.GetGenericTypeDefinition () == typeof (IValue<>));

        return iValueInterface?.GetGenericArguments () [0];
    }

    private View? CreateViewInstance (Type viewType)
    {
        // Try parameterless constructor
        ConstructorInfo? ctor = viewType.GetConstructor (Type.EmptyTypes);

        if (ctor is { })
        {
            var view = (View?)Activator.CreateInstance (viewType);

            // Set some basic properties
            if (view is { })
            {
                view.Width = Dim.Auto (DimAutoStyle.Content);
                view.Height = Dim.Auto (DimAutoStyle.Content);
            }

            return view;
        }

        // Some views might need constraints satisfied
        if (viewType.IsGenericTypeDefinition)
        {
            Type [] typeParams = viewType.GetGenericArguments ();
            Type [] constraintTypes = new Type [typeParams.Length];

            for (var i = 0; i < typeParams.Length; i++)
            {
                Type [] constraints = typeParams [i].GetGenericParameterConstraints ();
                constraintTypes [i] = constraints.Length > 0 ? constraints [0] : typeof (object);
            }

            Type constructedType = viewType.MakeGenericType (constraintTypes);

            return (View?)Activator.CreateInstance (constructedType);
        }

        return null;
    }

    private string GetFormattedTypeName (Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string baseName = type.Name.Substring (0, type.Name.IndexOf ('`'));
        string args = string.Join (", ", type.GetGenericArguments ().Select (t => t.Name));

        return $"{baseName}<{args}>";
    }

    private IEnumerable<Type> GetAllViewClasses ()
    {
        IEnumerable<Type>? types = typeof (View).Assembly.GetTypes ()
                                                .Where (t => t.IsPublic
                                                             && !t.IsAbstract
                                                             && t.IsSubclassOf (typeof (View))
                                                             && !t.Name.Contains ("Adornment")
                                                             && t != typeof (PopoverMenu)
                                                             && !t.IsGenericType);

        return types;
    }
}
