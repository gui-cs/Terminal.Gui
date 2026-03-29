#nullable enable
using System.Collections.ObjectModel;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Generic ListView<T>", "Demonstrates ListView<T> with typed Value, SelectedItem, and Index")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
public class GenericListView : Scenario
{
    private ListView<Country>? _listView;
    private ObservableCollection<string> _eventList = [];
    private ListView? _eventListView;
    private Label? _nameLabel;
    private Label? _capitalLabel;
    private Label? _populationLabel;
    private Label? _indexLabel;
    private CheckBox? _cancelNextCb;
    private bool _cancelNext;

    /// <inheritdoc/>
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new () { Title = GetQuitKeyAndName () };

        ObservableCollection<Country> countries =
        [
            new ("Australia", "Canberra", 26_000_000),
            new ("Brazil", "Brasília", 215_000_000),
            new ("Canada", "Ottawa", 38_000_000),
            new ("Denmark", "Copenhagen", 5_900_000),
            new ("Egypt", "Cairo", 104_000_000),
            new ("France", "Paris", 68_000_000),
            new ("Germany", "Berlin", 84_000_000),
            new ("Hungary", "Budapest", 9_700_000),
            new ("India", "New Delhi", 1_428_000_000),
            new ("Japan", "Tokyo", 124_000_000)
        ];

        // -- Cancel checkbox --------------------------------------------------
        _cancelNextCb = new CheckBox
        {
            X = 0,
            Y = 0,
            Text = "C_ancel next selection change"
        };
        _cancelNextCb.ValueChanging += (_, args) => _cancelNext = args.NewValue == CheckState.Checked;
        appWindow.Add (_cancelNextCb);

        // -- ListView<Country> ------------------------------------------------
        _listView = new ListView<Country>
        {
            Title = "_Countries",
            X = 0,
            Y = Pos.Bottom (_cancelNextCb) + 1,
            Width = 22,
            Height = Dim.Fill (4),
            BorderStyle = LineStyle.Single,
            AspectGetter = c => c.Name
        };
        _listView.SetSource (countries);
        appWindow.Add (_listView);

        // -- Detail panel -----------------------------------------------------
        FrameView detailPanel = new ()
        {
            Title = "_Selected",
            X = Pos.Right (_listView) + 1,
            Y = Pos.Top (_listView),
            Width = Dim.Fill (),
            Height = _listView.Height
        };
        appWindow.Add (detailPanel);

        Label nameTitleLbl = new () { X = 1, Y = 1, Text = "Name:      " };
        detailPanel.Add (nameTitleLbl);
        _nameLabel = new () { X = Pos.Right (nameTitleLbl), Y = 1, Width = Dim.Fill (1), Text = "(none)" };
        detailPanel.Add (_nameLabel);

        Label capitalTitleLbl = new () { X = 1, Y = 2, Text = "Capital:   " };
        detailPanel.Add (capitalTitleLbl);
        _capitalLabel = new () { X = Pos.Right (capitalTitleLbl), Y = 2, Width = Dim.Fill (1), Text = "" };
        detailPanel.Add (_capitalLabel);

        Label populationTitleLbl = new () { X = 1, Y = 3, Text = "Population:" };
        detailPanel.Add (populationTitleLbl);
        _populationLabel = new () { X = Pos.Right (populationTitleLbl), Y = 3, Width = Dim.Fill (1), Text = "" };
        detailPanel.Add (_populationLabel);

        Label indexTitleLbl = new () { X = 1, Y = 5, Text = "Index:     " };
        detailPanel.Add (indexTitleLbl);
        _indexLabel = new () { X = Pos.Right (indexTitleLbl), Y = 5, Width = Dim.Fill (1), Text = "" };
        detailPanel.Add (_indexLabel);

        // -- Event log --------------------------------------------------------
        _eventList = [];
        _eventListView = new ListView
        {
            Title = "_Events",
            X = 0,
            Y = Pos.Bottom (_listView) + 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (_eventList),
            BorderStyle = LineStyle.Single
        };
        appWindow.Add (_eventListView);

        // -- Wire events ------------------------------------------------------
        _listView.ValueChanging += OnValueChanging;
        _listView.ValueChanged += OnValueChanged;

        app.Run (appWindow);
    }

    private void OnValueChanging (object? sender, ValueChangingEventArgs<Country?> args)
    {
        if (_cancelNext)
        {
            args.Handled = true;
            _cancelNext = false;

            if (_cancelNextCb is not null)
            {
                _cancelNextCb.Value = CheckState.UnChecked;
            }

            LogEvent ($"ValueChanging CANCELLED: {FormatCountry (args.CurrentValue)} -> {FormatCountry (args.NewValue)}");

            return;
        }

        LogEvent ($"ValueChanging: {FormatCountry (args.CurrentValue)} -> {FormatCountry (args.NewValue)}");
    }

    private void OnValueChanged (object? sender, ValueChangedEventArgs<Country?> args)
    {
        UpdateDetail (args.NewValue);
        LogEvent ($"ValueChanged:  {FormatCountry (args.OldValue)} -> {FormatCountry (args.NewValue)}");
    }

    private void UpdateDetail (Country? country)
    {
        if (_nameLabel is null)
        {
            return;
        }

        if (country is null)
        {
            _nameLabel.Text = "(none)";
            _capitalLabel!.Text = "";
            _populationLabel!.Text = "";
            _indexLabel!.Text = "";

            return;
        }

        _nameLabel.Text = country.Name;
        _capitalLabel!.Text = country.Capital;
        _populationLabel!.Text = $"{country.Population:N0}";
        _indexLabel!.Text = _listView?.Index?.ToString () ?? "";
    }

    private void LogEvent (string message)
    {
        _eventList.Add (message);

        if (_eventListView is not null)
        {
            _eventListView.MoveEnd ();
        }
    }

    private static string FormatCountry (Country? c) => c is null ? "null" : c.Name;
}

/// <summary>A simple record used to demonstrate <see cref="ListView{T}"/>.</summary>
internal record Country (string Name, string Capital, int Population);
