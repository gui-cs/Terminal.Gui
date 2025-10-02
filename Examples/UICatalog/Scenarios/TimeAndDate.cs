using System;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Time And Date", "Illustrates TimeField and time & date handling")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class TimeAndDate : Scenario
{
    private Label _lblDateFmt;
    private Label _lblNewDate;
    private Label _lblNewTime;
    private Label _lblOldDate;
    private Label _lblOldTime;
    private Label _lblTimeFmt;

    public override void Main ()
    {
        Application.Init ();
        var win = new Window { Title = GetQuitKeyAndName () };
        var longTime = new TimeField
        {
            X = Pos.Center (),
            Y = 2,
            IsShortFormat = false,
            ReadOnly = false,
            Time = DateTime.Now.TimeOfDay
        };
        longTime.TimeChanged += TimeChanged;
        win.Add (longTime);

        var shortTime = new TimeField
        {
            X = Pos.Center (),
            Y = Pos.Bottom (longTime) + 1,
            IsShortFormat = true,
            ReadOnly = false,
            Time = DateTime.Now.TimeOfDay
        };
        shortTime.TimeChanged += TimeChanged;
        win.Add (shortTime);

        var shortDate = new DateField (DateTime.Now)
        {
            X = Pos.Center (), Y = Pos.Bottom (shortTime) + 1, ReadOnly = true
        };
        shortDate.DateChanged += DateChanged;
        win.Add (shortDate);

        var longDate = new DateField (DateTime.Now)
        {
            X = Pos.Center (), Y = Pos.Bottom (shortDate) + 1, ReadOnly = false
        };
        longDate.DateChanged += DateChanged;
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

        _lblOldDate = new()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (_lblTimeFmt) + 2,
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

        var swapButton = new Button
        {
            X = Pos.Center (), Y = Pos.Bottom (win) - 5, Text = "Swap Long/Short & Read/Read Only"
        };

        swapButton.Accepting += (s, e) =>
                             {
                                 longTime.ReadOnly = !longTime.ReadOnly;
                                 shortTime.ReadOnly = !shortTime.ReadOnly;

                                 longTime.IsShortFormat = !longTime.IsShortFormat;
                                 shortTime.IsShortFormat = !shortTime.IsShortFormat;

                                 longDate.ReadOnly = !longDate.ReadOnly;
                                 shortDate.ReadOnly = !shortDate.ReadOnly;
                             };
        win.Add (swapButton);

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }

    private void DateChanged (object sender, DateTimeEventArgs<DateTime> e)
    {
        _lblOldDate.Text = $"Old Date: {e.OldValue}";
        _lblNewDate.Text = $"New Date: {e.NewValue}";
        _lblDateFmt.Text = $"Date Format: {e.Format}";
    }

    private void TimeChanged (object sender, DateTimeEventArgs<TimeSpan> e)
    {
        _lblOldTime.Text = $"Old Time: {e.OldValue}";
        _lblNewTime.Text = $"New Time: {e.NewValue}";
        _lblTimeFmt.Text = $"Time Format: {e.Format}";
    }
}
