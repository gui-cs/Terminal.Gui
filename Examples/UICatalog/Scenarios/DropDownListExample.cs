#nullable enable
using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("DropDownList", "Demonstrates the DropDownList control.")]
[ScenarioCategory ("Controls")]
public class DropDownListExample : Scenario
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
            BorderStyle = LineStyle.None,
            Title = "DropDownList Examples - press F4 to open focused dropdown"
        };

        // Sample data
        ObservableCollection<string> countries =
        [
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

        ObservableCollection<string> colors =
        [
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
        Label readOnlyLabel = new () { X = 1, Y = 1, Text = "ReadOnly Mode (default):" };

        DropDownList readOnlyDropDown = new ()
        {
            X = Pos.Right(readOnlyLabel) + 1,
            Y = Pos.Top (readOnlyLabel),
            Title = "Select Country",
            Source = new ListWrapper<string> (countries),
            ReadOnly = true,
            Text = "Canada"
        };

        readOnlyDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"ReadOnly: ValueChanged - OldValue: '{e.OldValue}', NewValue: '{e.NewValue}'"); };

        readOnlyDropDown.ValueChanging += (s, e) =>
                                          {
                                              _eventLog?.Log ($"ReadOnly: ValueChanging - CurrentValue: '{e.CurrentValue}', NewValue: '{e.NewValue}'");
                                          };

        // Editable Mode Example
        Label editableLabel = new () { X = 1, Y = Pos.Bottom (readOnlyDropDown) + 1, Text = "Editable Mode:" };

        DropDownList editableDropDown = new ()
        {
            X = Pos.Right (editableLabel) + 1,
            Y = Pos.Top (editableLabel),
            Title = "Select or Type Color",
            Source = new ListWrapper<string> (colors),
            ReadOnly = false,
            Text = "Blue"
        };

        editableDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"Editable: ValueChanged - OldValue: '{e.OldValue}', NewValue: '{e.NewValue}'"); };

        editableDropDown.ValueChanging += (s, e) =>
                                          {
                                              _eventLog?.Log ($"Editable: ValueChanging - CurrentValue: '{e.CurrentValue}', NewValue: '{e.NewValue}'");
                                          };

        // Different positions example
        Label positionLabel = new () { X = 1, Y = Pos.Bottom (editableDropDown) + 1, Text = "Different Positions (with Borders):" };

        DropDownList topLeftDropDown = new ()
        {
            X = 1,
            Y = Pos.Bottom (positionLabel),
            Width = 25,
            Title = "Top-Left",
            Source = new ListWrapper<string> (countries),
            ReadOnly = true,
            Text = "Argentina",
            BorderStyle = LineStyle.Dotted
        };

        topLeftDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"TopLeft: Selected '{e.NewValue}'"); };

        DropDownList topRightDropDown = new ()
        {
            X = Pos.Right (topLeftDropDown) + 2,
            Y = Pos.Top (topLeftDropDown),
            Width = 25,
            Title = "Top-Right",
            Source = new ListWrapper<string> (colors),
            ReadOnly = true,
            Text = "Red",
            BorderStyle = LineStyle.Dotted
        };

        topRightDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"TopRight: Selected '{e.NewValue}'"); };

        DropDownList bottomLeftDropDown = new ()
        {
            X = 1,
            Y = Pos.AnchorEnd (2),
            Width = 25,
            Title = "Bottom-Left",
            Source = new ListWrapper<string> (countries),
            ReadOnly = true,
            Text = "Japan"
        };

        bottomLeftDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"BottomLeft: Selected '{e.NewValue}'"); };

        DropDownList bottomRightDropDown = new ()
        {
            X = Pos.Right (bottomLeftDropDown) + 2,
            Y = Pos.Top (bottomLeftDropDown),
            Width = 25,
            Title = "Bottom-Right",
            Source = new ListWrapper<string> (colors),
            ReadOnly = true,
            Text = "White"
        };

        bottomRightDropDown.ValueChanged += (s, e) => { _eventLog?.Log ($"BottomRight: Selected '{e.NewValue}'"); };

        contentFrame.AssignHotKeys = true;
        contentFrame.AssignHotKeysToSubViews ();

        contentFrame.Add (readOnlyLabel,
                          readOnlyDropDown,
                          editableLabel,
                          editableDropDown,
                          positionLabel,
                          topLeftDropDown,
                          topRightDropDown,
                          bottomLeftDropDown,
                          bottomRightDropDown);

        _eventLog.SetViewToLog (window);

        window.Add (contentFrame, _eventLog);

        _app.Run (window);

        _app?.Dispose ();
        _app = null;
    }
}
