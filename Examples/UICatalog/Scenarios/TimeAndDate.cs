#nullable enable
using System;
using System.Globalization;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Time And Date", "Illustrates TimeField and time & date handling")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class TimeAndDate : Scenario
{
    private Label? _lblDateFmt;
    private Label? _lblNewDate;
    private Label? _lblNewTime;
    private Label? _lblOldDate;
    private Label? _lblOldTime;
    private Label? _lblTimeFmt;
    private Label? _lblTimeEditorValue;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new () { Title = GetQuitKeyAndName () };

        // TimeField examples (existing)
        Label tfLabel = new ()
        {
            X = Pos.Center (),
            Y = 1,
            Text = "TimeField (Legacy):"
        };
        win.Add (tfLabel);

        TimeField longTime = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (tfLabel),
            IsShortFormat = false,
            ReadOnly = false,
            Value = DateTime.Now.TimeOfDay
        };
        longTime.ValueChanged += TimeChanged;
        win.Add (longTime);

        TimeField shortTime = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (longTime) + 1,
            IsShortFormat = true,
            ReadOnly = false,
            Value = DateTime.Now.TimeOfDay
        };
        shortTime.ValueChanged += TimeChanged;
        win.Add (shortTime);

        // TimeEditor examples (new)
        Label teLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (shortTime) + 1,
            Text = "TimeEditor (New - based on TextValidateField):"
        };
        win.Add (teLabel);

        // Default culture time editor
        TimeEditor defaultTimeEditor = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (teLabel),
            Value = DateTime.Now.TimeOfDay
        };
        defaultTimeEditor.ValueChanged += TimeEditorChanged;
        win.Add (defaultTimeEditor);

        Label defaultPatternLabel = new ()
        {
            X = Pos.Right (defaultTimeEditor) + 1,
            Y = Pos.Top (defaultTimeEditor),
            Text = $"Pattern: {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}"
        };
        win.Add (defaultPatternLabel);

        // 24-hour format time editor
        DateTimeFormatInfo format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();

        TimeEditor time24Editor = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (defaultTimeEditor) + 1,
            Value = DateTime.Now.TimeOfDay,
            Format = format24h
        };
        time24Editor.ValueChanged += TimeEditorChanged;
        win.Add (time24Editor);

        Label time24PatternLabel = new ()
        {
            X = Pos.Right (time24Editor) + 1,
            Y = Pos.Top (time24Editor),
            Text = $"Pattern: {format24h.LongTimePattern}"
        };
        win.Add (time24PatternLabel);

        // Short time format time editor
        DateTimeFormatInfo shortFormat = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
        shortFormat.LongTimePattern = shortFormat.ShortTimePattern;

        TimeEditor shortTimeEditor = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (time24Editor) + 1,
            Value = DateTime.Now.TimeOfDay,
            Format = shortFormat
        };
        shortTimeEditor.ValueChanged += TimeEditorChanged;
        win.Add (shortTimeEditor);

        Label shortPatternLabel = new ()
        {
            X = Pos.Right (shortTimeEditor) + 1,
            Y = Pos.Top (shortTimeEditor),
            Text = $"Pattern: {shortFormat.LongTimePattern}"
        };
        win.Add (shortPatternLabel);

        DateField shortDate = new (DateTime.Now)
        {
            X = Pos.Center (), Y = Pos.Bottom (shortTimeEditor) + 1, ReadOnly = true
        };
        shortDate.ValueChanged += DateChanged;
        win.Add (shortDate);

        DateField longDate = new (DateTime.Now)
        {
            X = Pos.Center (), Y = Pos.Bottom (shortDate) + 1, ReadOnly = false
        };
        longDate.ValueChanged += DateChanged;
        win.Add (longDate);

        _lblOldTime = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (longDate) + 1,
            TextAlignment = Alignment.Center,

            Width = Dim.Fill (),
            Text = "Old Time: "
        };
        win.Add (_lblOldTime);

        _lblNewTime = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (_lblOldTime) + 1,
            TextAlignment = Alignment.Center,

            Width = Dim.Fill (),
            Text = "New Time: "
        };
        win.Add (_lblNewTime);

        _lblTimeFmt = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (_lblNewTime) + 1,
            TextAlignment = Alignment.Center,

            Width = Dim.Fill (),
            Text = "Time Format: "
        };
        win.Add (_lblTimeFmt);

        _lblTimeEditorValue = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (_lblTimeFmt) + 1,
            TextAlignment = Alignment.Center,

            Width = Dim.Fill (),
            Text = "TimeEditor Value: "
        };
        win.Add (_lblTimeEditorValue);

        _lblOldDate = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (_lblTimeEditorValue) + 1,
            TextAlignment = Alignment.Center,

            Width = Dim.Fill (),
            Text = "Old Date: "
        };
        win.Add (_lblOldDate);

        _lblNewDate = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (_lblOldDate) + 1,
            TextAlignment = Alignment.Center,

            Width = Dim.Fill (),
            Text = "New Date: "
        };
        win.Add (_lblNewDate);

        _lblDateFmt = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (_lblNewDate) + 1,
            TextAlignment = Alignment.Center,

            Width = Dim.Fill (),
            Text = "Date Format: "
        };
        win.Add (_lblDateFmt);

        Button swapButton = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (win) - 5, Text = "Swap Long/Short & Read/Read Only"
        };

        swapButton.Accepting += (_, _) =>
                             {
                                 longTime.ReadOnly = !longTime.ReadOnly;
                                 shortTime.ReadOnly = !shortTime.ReadOnly;

                                 longTime.IsShortFormat = !longTime.IsShortFormat;
                                 shortTime.IsShortFormat = !shortTime.IsShortFormat;

                                 longDate.ReadOnly = !longDate.ReadOnly;
                                 shortDate.ReadOnly = !shortDate.ReadOnly;
                             };
        win.Add (swapButton);

        app.Run (win);
    }

    private void DateChanged (object? sender, ValueChangedEventArgs<DateTime?> e)
    {
        _lblNewDate!.Text = $"New Date: {e.NewValue}";
    }

    private void TimeChanged (object? sender, ValueChangedEventArgs<TimeSpan> e)
    {
        _lblNewTime!.Text = $"New Time: {e.NewValue}";
    }

    private void TimeEditorChanged (object? sender, ValueChangedEventArgs<TimeSpan> e)
    {
        _lblTimeEditorValue!.Text = $"TimeEditor Value: {e.NewValue}";
    }
}
