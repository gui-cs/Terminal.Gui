#nullable enable
using System.Collections.ObjectModel;
using System.Text;

namespace UICatalog.Scenarios;

/// <summary>
///     An event log that automatically shows the Command-related events that are raised.
/// </summary>
/// <remarks>
/// </remarks>
public class EventLog : ListView
{
    public EventLog ()
    {
        Title = "Event Log";
        CanFocus = true;

        X = Pos.AnchorEnd ();
        Y = 0;

        Width = Dim.Func (_ =>
                          {
                              if (!IsInitialized)
                              {
                                  return 0;
                              }

                              return Math.Min (SuperView!.Viewport.Width / 3, MaxItemLength + GetAdornmentsThickness ().Horizontal);
                          });
        Height = Dim.Fill ();

        ExpandButton = new () { Orientation = Orientation.Horizontal };

        Initialized += EventLog_Initialized;

        HorizontalScrollBar.AutoShow = true;
        VerticalScrollBar.AutoShow = true;

        AddCommand (Command.DeleteAll,
                    () =>
                    {
                        SelectedItem = null;
                        _eventSource.Clear ();

                        return true;
                    });

        KeyBindings.Add (Key.Delete, Command.DeleteAll);
    }

    public ExpanderButton? ExpandButton { get; }

    private readonly ObservableCollection<string> _eventSource = [];

    private View? _viewToLog;

    public View? ViewToLog
    {
        get => _viewToLog;
        set
        {
            if (_viewToLog == value)
            {
                return;
            }

            _viewToLog = value;

            if (_viewToLog is { })
            {
                _viewToLog.Initialized += (s, _) =>
                                          {
                                              var sender = s as View;
                                              Log ($"Initialized: {GetIdentifyingString (sender)}");
                                          };

                _viewToLog.HandlingHotKey += (_, args) => { Log ($"HandlingHotKey: {FormatContext (args.Context)}"); };
                _viewToLog.Activating += (_, args) => { Log ($"Activating: {FormatContext (args.Context)}"); };
                _viewToLog.Accepting += (_, args) => { Log ($"Accepting: {FormatContext (args.Context)}"); };
                _viewToLog.Accepted += (_, args) => { Log ($"Accepted: {FormatContext (args.Context)}"); };
            }
        }
    }

    private string FormatContext (ICommandContext? context)
    {
        if (context is null)
        {
            return "null";
        }

        StringBuilder sb = new ();
        sb.Append ($"{context.Command}");

        if (context.Binding is { } binding)
        {
            sb.Append ($", Binding={binding}");
        }

        if (context.Source is { } && context.Source.TryGetTarget (out View? view))
        {
            sb.Append ($", Source={GetIdentifyingString (view)}");
        }

        return sb.ToString ();
    }

    public void Log (string text)
    {
        _eventSource.Add (text);
        MoveEnd ();
        SelectedItem = null;
    }

    private void EventLog_Initialized (object? _, EventArgs e)
    {
        Border?.Add (ExpandButton!);
        Source = new ListWrapper<string> (_eventSource);
    }

    private string GetIdentifyingString (View? view)
    {
        if (view is null)
        {
            return "null";
        }

        if (!string.IsNullOrEmpty (view.Title))
        {
            return view.Title;
        }

        if (!string.IsNullOrEmpty (view.Text))
        {
            return view.Text;
        }

        return view.GetType ().Name;
    }
}
