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
    private EventLog? _eventLog;
    private readonly Dictionary<int, IPopoverView> _popoverInstances = [];

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

        // Left: View list with marks — space toggles mark (register/deregister), enter shows marked popover
        _viewListView = new ListView
        {
            Title = "_Views (space=register, enter=show)",
            X = 0,
            Y = 0,
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            ShowMarks = true,
            MarkMultiple = true,
            SelectedItem = 0,
            BorderStyle = LineStyle.Single,
            Source = new ListWrapper<string> (new ObservableCollection<string> (_viewClasses.Keys)),
        };
        _viewListView.Activated += ViewListView_Activated;
        _viewListView.Accepting += ViewListView_Accepting;

        // Right: Event log
        _eventLog = new EventLog { X = Pos.Right (_viewListView), Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        _eventLog.SetViewToLog (window);
        window.Add (_viewListView, _eventLog);

        _app.Run (window);

        _app.Dispose ();
        _app = null;
    }

    /// <summary>
    ///     Handles space key (Activated) — sync mark state to register/deregister the popover.
    /// </summary>
    private void ViewListView_Activated (object? sender, EventArgs<ICommandContext?> args)
    {
        SyncRegistrations ();
    }

    /// <summary>
    ///     Handles enter key (Accepting) — show the popover for the selected item if it's marked/registered.
    /// </summary>
    private void ViewListView_Accepting (object? sender, CommandEventArgs args)
    {
        if (_viewListView?.SelectedItem is not { } selectedIndex || _viewClasses is null)
        {
            return;
        }

        // Sync registrations first in case marks changed
        SyncRegistrations ();

        if (!_popoverInstances.TryGetValue (selectedIndex, out IPopoverView? popover))
        {
            _eventLog?.Log ($"{_viewClasses.Keys.ElementAt (selectedIndex)} is not registered (mark it with space first)");

            return;
        }

        args.Handled = true;

        try
        {
            Rectangle screen = _app!.Screen;
            Point center = new (screen.Width / 2, screen.Height / 2);
            popover.MakeVisible (center);
            _eventLog?.Log ($"Showed: {_viewClasses.Keys.ElementAt (selectedIndex)}");
        }
        catch (Exception ex)
        {
            _eventLog?.Log ($"Error showing popover: {ex.Message}");
        }
    }

    /// <summary>
    ///     Syncs mark state with popover registrations — marked items get registered, unmarked get deregistered.
    /// </summary>
    private void SyncRegistrations ()
    {
        if (_viewListView?.Source is null || _viewClasses is null)
        {
            return;
        }

        for (var i = 0; i < _viewListView.Source.Count; i++)
        {
            bool isMarked = _viewListView.Source.IsMarked (i);
            bool isRegistered = _popoverInstances.ContainsKey (i);

            if (isMarked && !isRegistered)
            {
                // Register
                Type viewType = _viewClasses.Values.ElementAt (i);

                try
                {
                    View? contentView = CreateViewInstance (viewType);

                    if (contentView is null)
                    {
                        _eventLog?.Log ($"Failed to create instance of {viewType.Name}");
                        _viewListView.Source.SetMark (i, false);

                        continue;
                    }

                    RegisterPopover (i, contentView);
                }
                catch (Exception ex)
                {
                    _eventLog?.Log ($"Error creating popover for {viewType.Name}: {ex.Message}");
                    _viewListView.Source.SetMark (i, false);
                }
            }
            else if (!isMarked && isRegistered)
            {
                // Deregister
                IPopoverView popover = _popoverInstances [i];
                _app?.Popovers?.DeRegister (popover);
                _popoverInstances.Remove (i);
                string key = _viewClasses.Keys.ElementAt (i);
                _eventLog?.Log ($"Deregistered: {key}");
            }
        }
    }

    private void RegisterPopover (int index, View contentView)
    {
        string key = _viewClasses!.Keys.ElementAt (index);

        try
        {
            Type? resultType = GetIValueResultType (contentView) ?? typeof (string);

            Type popoverType = typeof (Popover<,>).MakeGenericType (contentView.GetType (), resultType);
            object? popoverObj = Activator.CreateInstance (popoverType, contentView);

            if (popoverObj is not IPopoverView popover)
            {
                _eventLog?.Log ($"Failed to create popover for {key}");

                return;
            }

            _app?.Popovers?.Register (popover);
            _popoverInstances [index] = popover;
            _eventLog?.Log ($"Registered: {key}");
        }
        catch (Exception ex)
        {
            _eventLog?.Log ($"Error registering popover for {key}: {ex.Message}");
        }
    }

    private Type? GetIValueResultType (View view)
    {
        Type? iValueInterface = view.GetType ().GetInterfaces ().FirstOrDefault (i => i.IsGenericType && i.GetGenericTypeDefinition () == typeof (IValue<>));

        return iValueInterface?.GetGenericArguments () [0];
    }

    private View? CreateViewInstance (Type viewType)
    {
        ConstructorInfo? ctor = viewType.GetConstructor (Type.EmptyTypes);

        if (ctor is { })
        {
            var view = (View?)Activator.CreateInstance (viewType);

            if (view is { })
            {
                view.Width = Dim.Auto (DimAutoStyle.Content);
                view.Height = Dim.Auto (DimAutoStyle.Content);
            }

            return view;
        }

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
