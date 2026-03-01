#nullable enable
using System.Collections.ObjectModel;
using System.Reflection;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Popovers", "Demonstrates the Popover<TView, TResult> infrastructure.")]
[ScenarioCategory ("Popups")]
[ScenarioCategory ("Controls")]
public class Popovers : Scenario
{
    private Dictionary<string, Type>? _viewClasses;
    private ListView? _viewListView;
    private ListView? _popoverListView;
    private EventLog? _eventLog;
    private readonly ObservableCollection<string> _registeredPopovers = [];
    private readonly Dictionary<string, IPopover> _popoverInstances = [];

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

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
            Title = "_V_iews",
            X = 0,
            Y = 0,
            Width = Dim.Percent (33),
            Height = Dim.Fill (),
            ShowMarks = false,
            SelectedItem = 0,
            Source = new ListWrapper<string> (new ObservableCollection<string> (_viewClasses.Keys.ToList ())),
            SuperViewRendersLineCanvas = true
        };
        _viewListView.Border!.Thickness = new Thickness (1);
        _viewListView.Accepting += ViewListView_Accepting;

        // Middle: Registered popovers list
        _popoverListView = new ListView
        {
            Title = "_P_opovers (click to show)",
            X = Pos.Right (_viewListView),
            Y = 0,
            Width = Dim.Percent (33),
            Height = Dim.Fill (),
            ShowMarks = false,
            Source = new ListWrapper<string> (_registeredPopovers),
            SuperViewRendersLineCanvas = true
        };
        _popoverListView.Border!.Thickness = new Thickness (1);
        _popoverListView.Accepting += PopoverListView_Accepting;

        // Right: Event log
        _eventLog = new EventLog
        {
            X = Pos.Right (_popoverListView),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        _eventLog.Border!.Thickness = new Thickness (1);

        window.Add (_viewListView, _popoverListView, _eventLog);

        app.Run (window);
        app.Dispose ();
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
                LogEvent ($"Failed to create instance of {viewType.Name}");

                return;
            }

            // Register the popover
            RegisterPopover (contentView);
        }
        catch (Exception ex)
        {
            LogEvent ($"Error creating popover for {viewType.Name}: {ex.Message}");
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
            // Show the popover centered using ApplicationPopover
            Rectangle screen = Application.Screen;
            Point center = new (screen.Width / 2, screen.Height / 2);
            
            // Use reflection to call MakeVisible on the concrete type
            MethodInfo? makeVisibleMethod = popover.GetType ().GetMethod ("MakeVisible", [typeof (Point), typeof (Rectangle?)]);

            if (makeVisibleMethod is { })
            {
                makeVisibleMethod.Invoke (popover, [center, null]);
                LogEvent ($"Showed popover: {popoverKey}");
            }
            else
            {
                LogEvent ($"MakeVisible method not found on {popoverKey}");
            }
        }
        catch (Exception ex)
        {
            LogEvent ($"Error showing popover {popoverKey}: {ex.Message}");
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
                LogEvent ($"Failed to create popover for {viewTypeName}");

                return;
            }

            // Set up result extraction if not using IValue
            if (resultType == typeof (string))
            {
                // Use reflection to set ResultExtractor
                PropertyInfo? extractorProp = popoverType.GetProperty ("ResultExtractor");

                if (extractorProp is { })
                {
                    // Create a delegate: (TView view) => view.ToString()
                    Type delegateType = typeof (Func<,>).MakeGenericType (contentView.GetType (), typeof (string));
                    MethodInfo? toStringMethod = contentView.GetType ().GetMethod ("ToString", BindingFlags.Public | BindingFlags.Instance);

                    if (toStringMethod is { })
                    {
                        Delegate? extractor = Delegate.CreateDelegate (delegateType, toStringMethod);
                        extractorProp.SetValue (popoverObj, extractor);
                    }
                }
            }

            // Register with ApplicationPopover
            Application.Popover?.Register (popover);

            // Track the popover
            string popoverKey = $"{viewTypeName} → {resultType.Name}";
            _popoverInstances [popoverKey] = popover;
            _registeredPopovers.Add (popoverKey);

            LogEvent ($"Registered: {popoverKey}");

            // Subscribe to IsOpenChanged if it's a Popover<,>
            SubscribeToPopoverEvents (popoverObj, popoverKey);
        }
        catch (Exception ex)
        {
            LogEvent ($"Error registering popover for {viewTypeName}: {ex.Message}");
        }
    }

    private void SubscribeToPopoverEvents (object popoverObj, string popoverKey)
    {
        // Use reflection to subscribe to IsOpenChanged
        EventInfo? isOpenChangedEvent = popoverObj.GetType ().GetEvent ("IsOpenChanged");

        if (isOpenChangedEvent is { })
        {
            MethodInfo? handler = GetType ().GetMethod (nameof (OnPopoverIsOpenChanged), BindingFlags.NonPublic | BindingFlags.Instance);

            if (handler is { })
            {
                Delegate? del = Delegate.CreateDelegate (isOpenChangedEvent.EventHandlerType!, this, handler);
                isOpenChangedEvent.AddEventHandler (popoverObj, del);
            }
        }

        // Subscribe to ResultChanged
        EventInfo? resultChangedEvent = popoverObj.GetType ().GetEvent ("ResultChanged");

        if (resultChangedEvent is { })
        {
            MethodInfo? handler = GetType ().GetMethod (nameof (OnPopoverResultChanged), BindingFlags.NonPublic | BindingFlags.Instance);

            if (handler is { })
            {
                // Create a handler that captures the popoverKey
                Action<object?, EventArgs> wrappedHandler = (sender, args) => OnPopoverResultChanged (sender, args, popoverKey);
                Delegate? del = Delegate.CreateDelegate (resultChangedEvent.EventHandlerType!, wrappedHandler.Target, wrappedHandler.Method);
                resultChangedEvent.AddEventHandler (popoverObj, del);
            }
        }
    }

    private void OnPopoverIsOpenChanged (object? sender, ValueChangedEventArgs<bool> e)
    {
        string typeName = sender?.GetType ().GetGenericArguments () [0].Name ?? "Unknown";
        LogEvent ($"{typeName}: IsOpen changed from {e.OldValue} to {e.NewValue}");
    }

    private void OnPopoverResultChanged (object? sender, EventArgs e, string popoverKey)
    {
        // Extract result value using reflection
        PropertyInfo? resultProp = sender?.GetType ().GetProperty ("Result");
        object? result = resultProp?.GetValue (sender);
        LogEvent ($"{popoverKey}: Result = {result ?? "null"}");
    }

    private Type? GetIValueResultType (View view)
    {
        // Check if the view implements IValue<T>
        Type? iValueInterface = view.GetType ().GetInterfaces ()
                                    .FirstOrDefault (i => i.IsGenericType && i.GetGenericTypeDefinition () == typeof (IValue<>));

        return iValueInterface?.GetGenericArguments () [0];
    }

    private View? CreateViewInstance (Type viewType)
    {
        // Try parameterless constructor
        ConstructorInfo? ctor = viewType.GetConstructor (Type.EmptyTypes);

        if (ctor is { })
        {
            View? view = (View?)Activator.CreateInstance (viewType);

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
                                                 .Where (
                                                         t => t.IsPublic
                                                              && !t.IsAbstract
                                                              && t.IsSubclassOf (typeof (View))
                                                              && !t.Name.Contains ("Adornment")
                                                              && t != typeof (PopoverMenu)
                                                              && !t.IsGenericType
                                                        );

        return types;
    }

    private void LogEvent (string message)
    {
        if (_eventLog is null)
        {
            return;
        }

        // Access the internal event source
        FieldInfo? sourceField = typeof (EventLog).GetField ("_eventSource", BindingFlags.NonPublic | BindingFlags.Instance);

        if (sourceField?.GetValue (_eventLog) is ObservableCollection<string> eventSource)
        {
            eventSource.Add ($"{DateTime.Now:HH:mm:ss.fff} - {message}");
        }
    }
}
