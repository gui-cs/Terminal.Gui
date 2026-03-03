#nullable enable
using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("EditDropDown", "Demonstrates the EditDropDown control.")]
[ScenarioCategory ("Controls")]
public class EditDropDownExample : Scenario
{
    private IApplication? _app;
    private EventLog? _eventLog;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        _app = Application.Create ();
        _app.Init ();

        using Window window = new ();
        window.Title = GetQuitKeyAndName ();
        window.BorderStyle = LineStyle.None;

        // Event log on the right
        _eventLog = new EventLog
        {
            Title = "_Event Log",
            X = Pos.AnchorEnd (),
            Width = Dim.Auto (maximumContentDim: 50),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Double,
            Arrangement = ViewArrangement.LeftResizable
        };

        // Main content area
        FrameView contentFrame = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (_eventLog!),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Single,
            Title = "EditDropDown Examples"
        };

        // Sample data
        ObservableCollection<string> countries = [
            "Argentina",
            "Brazil",
            "Canada",
            "Denmark",
            "Egypt",
            "France",
            "Germany",
            "Hungary",
            "India",
            "Japan"
        ];

        ObservableCollection<string> colors = [
            "Red",
            "Green",
            "Blue",
            "Yellow",
            "Orange",
            "Purple",
            "Pink",
            "Brown",
            "Black",
            "White"
        ];

        // ReadOnly Mode Example
        Label readOnlyLabel = new ()
        {
            X = 1,
            Y = 1,
            Text = "ReadOnly Mode (default):"
        };

        EditDropDown readOnlyDropDown = new ()
        {
            X = 1,
            Y = Pos.Bottom (readOnlyLabel),
            Width = 30,
            Title = "Select Country",
            Source = new ListWrapper<string> (countries),
            ReadOnly = true,
            Text = "Canada"
        };

        readOnlyDropDown.ValueChanged += (s, e) =>
        {
            _eventLog?.Log ($"ReadOnly: ValueChanged - OldValue: '{e.OldValue}', NewValue: '{e.NewValue}'");
        };

        readOnlyDropDown.ValueChanging += (s, e) =>
        {
            _eventLog?.Log ($"ReadOnly: ValueChanging - CurrentValue: '{e.CurrentValue}', NewValue: '{e.NewValue}'");
        };

        Label readOnlyHelpLabel = new ()
        {
            X = 1,
            Y = Pos.Bottom (readOnlyDropDown) + 1,
            Text = "Click anywhere or press F4 or Alt+Down to open"
        };

        // Editable Mode Example
        Label editableLabel = new ()
        {
            X = 1,
            Y = Pos.Bottom (readOnlyHelpLabel) + 1,
            Text = "Editable Mode:"
        };

        EditDropDown editableDropDown = new ()
        {
            X = 1,
            Y = Pos.Bottom (editableLabel),
            Width = 30,
            Title = "Select or Type Color",
            Source = new ListWrapper<string> (colors),
            ReadOnly = false,
            Text = "Blue"
        };

        editableDropDown.ValueChanged += (s, e) =>
        {
            _eventLog?.Log ($"Editable: ValueChanged - OldValue: '{e.OldValue}', NewValue: '{e.NewValue}'");
        };

        editableDropDown.ValueChanging += (s, e) =>
        {
            _eventLog?.Log ($"Editable: ValueChanging - CurrentValue: '{e.CurrentValue}', NewValue: '{e.NewValue}'");
        };

        Label editableHelpLabel = new ()
        {
            X = 1,
            Y = Pos.Bottom (editableDropDown) + 1,
            Text = "Type text or press F4 to open dropdown"
        };

        // Different positions example
        Label positionLabel = new ()
        {
            X = 1,
            Y = Pos.Bottom (editableHelpLabel) + 1,
            Text = "Different Positions:"
        };

        EditDropDown topLeftDropDown = new ()
        {
            X = 1,
            Y = Pos.Bottom (positionLabel),
            Width = 25,
            Title = "Top-Left",
            Source = new ListWrapper<string> (countries),
            ReadOnly = true,
            Text = "Argentina"
        };

        topLeftDropDown.ValueChanged += (s, e) =>
        {
            _eventLog?.Log ($"TopLeft: Selected '{e.NewValue}'");
        };

        EditDropDown topRightDropDown = new ()
        {
            X = Pos.Right (topLeftDropDown) + 2,
            Y = Pos.Top (topLeftDropDown),
            Width = 25,
            Title = "Top-Right",
            Source = new ListWrapper<string> (colors),
            ReadOnly = true,
            Text = "Red"
        };

        topRightDropDown.ValueChanged += (s, e) =>
        {
            _eventLog?.Log ($"TopRight: Selected '{e.NewValue}'");
        };

        EditDropDown bottomLeftDropDown = new ()
        {
            X = 1,
            Y = Pos.AnchorEnd (2),
            Width = 25,
            Title = "Bottom-Left",
            Source = new ListWrapper<string> (countries),
            ReadOnly = true,
            Text = "Japan"
        };

        bottomLeftDropDown.ValueChanged += (s, e) =>
        {
            _eventLog?.Log ($"BottomLeft: Selected '{e.NewValue}'");
        };

        EditDropDown bottomRightDropDown = new ()
        {
            X = Pos.Right (bottomLeftDropDown) + 2,
            Y = Pos.Top (bottomLeftDropDown),
            Width = 25,
            Title = "Bottom-Right",
            Source = new ListWrapper<string> (colors),
            ReadOnly = true,
            Text = "White"
        };

        bottomRightDropDown.ValueChanged += (s, e) =>
        {
            _eventLog?.Log ($"BottomRight: Selected '{e.NewValue}'");
        };

        contentFrame.Add (
                          readOnlyLabel,
                          readOnlyDropDown,
                          readOnlyHelpLabel,
                          editableLabel,
                          editableDropDown,
                          editableHelpLabel,
                          positionLabel,
                          topLeftDropDown,
                          topRightDropDown,
                          bottomLeftDropDown,
                          bottomRightDropDown
                         );

        _eventLog.SetViewToLog (window);
        window.Add (contentFrame, _eventLog);

        _app.Run (window);

        _app?.Dispose ();
        _app = null;
    }
}
