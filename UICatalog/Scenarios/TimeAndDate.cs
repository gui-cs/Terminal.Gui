using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata (Name: "Time And Date", Description: "Illustrates TimeField and time & date handling")]
[ScenarioCategory ("Controls"), ScenarioCategory ("DateTime")]
public class TimeAndDate : Scenario {
	Label lblOldTime;
	Label lblNewTime;
	Label lblTimeFmt;
	Label lblOldDate;
	Label lblNewDate;
	Label lblDateFmt;

	public override void Setup ()
	{
		var longTime = new TimeField (DateTime.Now.TimeOfDay) {
			X = Pos.Center (),
			Y = 2,
			IsShortFormat = false,
			ReadOnly = false,
		};
		longTime.TimeChanged += TimeChanged;
		Win.Add (longTime);

		var shortTime = new TimeField (DateTime.Now.TimeOfDay) {
			X = Pos.Center (),
			Y = Pos.Bottom (longTime) + 1,
			IsShortFormat = true,
			ReadOnly = false,
		};
		shortTime.TimeChanged += TimeChanged;
		Win.Add (shortTime);

		var shortDate = new DateField (DateTime.Now) {
			X = Pos.Center (),
			Y = Pos.Bottom (shortTime) + 1,
			ReadOnly = true,
		};
		shortDate.DateChanged += DateChanged;
		Win.Add (shortDate);

		var longDate = new DateField (DateTime.Now) {
			X = Pos.Center (),
			Y = Pos.Bottom (shortDate) + 1,
			ReadOnly = false,
		};
		longDate.DateChanged += DateChanged;
		Win.Add (longDate);

		lblOldTime = new Label {
			X = Pos.Center (),
			Y = Pos.Bottom (longDate) + 1,
			TextAlignment = TextAlignment.Centered,
			Width = Dim.Fill (),
			Text = "Old Time: "
		};
		Win.Add (lblOldTime);

		lblNewTime = new Label {
			X = Pos.Center (),
			Y = Pos.Bottom (lblOldTime) + 1,
			TextAlignment = TextAlignment.Centered,
			Width = Dim.Fill (),
			Text = "New Time: "
		};
		Win.Add (lblNewTime);

		lblTimeFmt = new Label {
			X = Pos.Center (),
			Y = Pos.Bottom (lblNewTime) + 1,
			TextAlignment = TextAlignment.Centered,
			Width = Dim.Fill (),
			Text = "Time Format: "
		};
		Win.Add (lblTimeFmt);

		lblOldDate = new Label {
			X = Pos.Center (),
			Y = Pos.Bottom (lblTimeFmt) + 2,
			TextAlignment = TextAlignment.Centered,
			Width = Dim.Fill (),
			Text = "Old Date: "
		};
		Win.Add (lblOldDate);

		lblNewDate = new Label {
			X = Pos.Center (),
			Y = Pos.Bottom (lblOldDate) + 1,
			TextAlignment = TextAlignment.Centered,
			Width = Dim.Fill (),
			Text = "New Date: "
		};
		Win.Add (lblNewDate);

		lblDateFmt = new Label {
			X = Pos.Center (),
			Y = Pos.Bottom (lblNewDate) + 1,
			TextAlignment = TextAlignment.Centered,
			Width = Dim.Fill (),
			Text = "Date Format: "
		};
		Win.Add (lblDateFmt);

		var swapButton = new Button {
			X = Pos.Center (),
			Y = Pos.Bottom (Win) - 5,
			Text = "Swap Long/Short & Read/Read Only"
		};
		swapButton.Clicked += (s, e) => {
			longTime.ReadOnly = !longTime.ReadOnly;
			shortTime.ReadOnly = !shortTime.ReadOnly;

			longTime.IsShortFormat = !longTime.IsShortFormat;
			shortTime.IsShortFormat = !shortTime.IsShortFormat;

			longDate.ReadOnly = !longDate.ReadOnly;
			shortDate.ReadOnly = !shortDate.ReadOnly;
		};
		Win.Add (swapButton);
	}

	private void TimeChanged (object sender, DateTimeEventArgs<TimeSpan> e)
	{
		lblOldTime.Text = $"Old Time: {e.OldValue}";
		lblNewTime.Text = $"New Time: {e.NewValue}";
		lblTimeFmt.Text = $"Time Format: {e.Format}";
	}

	private void DateChanged (object sender, DateTimeEventArgs<DateTime> e)
	{
		lblOldDate.Text = $"Old Date: {e.OldValue}";
		lblNewDate.Text = $"New Date: {e.NewValue}";
		lblDateFmt.Text = $"Date Format: {e.Format}";
	}
}