#nullable enable
using System;
using System.Globalization;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Time And Date", "Illustrates TimeEditor, DateEditor, and time & date handling")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class TimeAndDate : Scenario
{
    private Label? _lblDateEditorValue;
    private Label? _lblTimeEditorValue;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new () { Title = GetQuitKeyAndName () };

        // ── TimeEditor examples ──────────────────────────────────────
        Label teLabel = new ()
        {
            X = Pos.Center (),
            Y = 1,
            Text = "TimeEditor (based on TextValidateField):"
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

        _lblTimeEditorValue = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (shortTimeEditor) + 1,
            TextAlignment = Alignment.Center,
            Width = Dim.Fill (),
            Text = "TimeEditor Value: "
        };
        win.Add (_lblTimeEditorValue);

        // ── DateEditor examples ──────────────────────────────────────
        Label deLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (_lblTimeEditorValue) + 2,
            Text = "DateEditor (based on TextValidateField):"
        };
        win.Add (deLabel);

        // Default culture date editor
        DateEditor defaultDateEditor = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (deLabel),
            Value = DateTime.Today
        };
        defaultDateEditor.ValueChanged += DateEditorChanged;
        win.Add (defaultDateEditor);

        Label defaultDatePatternLabel = new ()
        {
            X = Pos.Right (defaultDateEditor) + 1,
            Y = Pos.Top (defaultDateEditor),
            Text = $"Pattern: {CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern}"
        };
        win.Add (defaultDatePatternLabel);

        // US format date editor
        DateTimeFormatInfo usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();

        DateEditor usDateEditor = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (defaultDateEditor) + 1,
            Value = DateTime.Today,
            Format = usFormat
        };
        usDateEditor.ValueChanged += DateEditorChanged;
        win.Add (usDateEditor);

        Label usDatePatternLabel = new ()
        {
            X = Pos.Right (usDateEditor) + 1,
            Y = Pos.Top (usDateEditor),
            Text = $"Pattern: {usFormat.ShortDatePattern}"
        };
        win.Add (usDatePatternLabel);

        // German format date editor
        DateTimeFormatInfo deFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("de-DE").DateTimeFormat.Clone ();

        DateEditor germanDateEditor = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (usDateEditor) + 1,
            Value = DateTime.Today,
            Format = deFormat
        };
        germanDateEditor.ValueChanged += DateEditorChanged;
        win.Add (germanDateEditor);

        Label germanDatePatternLabel = new ()
        {
            X = Pos.Right (germanDateEditor) + 1,
            Y = Pos.Top (germanDateEditor),
            Text = $"Pattern: {deFormat.ShortDatePattern}"
        };
        win.Add (germanDatePatternLabel);

        _lblDateEditorValue = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (germanDateEditor) + 1,
            TextAlignment = Alignment.Center,
            Width = Dim.Fill (),
            Text = "DateEditor Value: "
        };
        win.Add (_lblDateEditorValue);

        app.Run (win);
    }

    private void DateEditorChanged (object? sender, ValueChangedEventArgs<DateTime?> e)
    {
        _lblDateEditorValue!.Text = $"DateEditor Value: {e.NewValue:d}";
    }

    private void TimeEditorChanged (object? sender, ValueChangedEventArgs<TimeSpan> e)
    {
        _lblTimeEditorValue!.Text = $"TimeEditor Value: {e.NewValue}";
    }
}
