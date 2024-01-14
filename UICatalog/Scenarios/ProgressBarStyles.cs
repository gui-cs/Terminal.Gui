﻿using System;
using System.Linq;
using System.Threading;
using Terminal.Gui;
using static UICatalog.Scenarios.Frames;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ProgressBar Styles", "Shows the ProgressBar Styles.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Progress")]
[ScenarioCategory ("Threading")]

// TODO: Add enable/disable to show that that is working
// TODO: Clean up how FramesEditor works 
// TODO: Better align rpPBFormat
public class ProgressBarStyles : Scenario {
	const uint _timerTick = 20;
	Timer _fractionTimer;
	Timer _pulseTimer;

	public override void Init ()
	{
		Application.Init ();
		ConfigurationManager.Themes.Theme = Theme;
		ConfigurationManager.Apply ();

		var editor = new FramesEditor {
			Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
			BorderStyle = LineStyle.Single
		};
		editor.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

		const float fractionStep = 0.01F;

		var pbList = new ListView () {
			Title = "Focused ProgressBar",
			Y = 0,
			X = Pos.Center (),
			Width = 30,
			Height = 7,
			BorderStyle = LineStyle.Single
		};
		pbList.SelectedItemChanged += (sender, e) => {
			editor.ViewToEdit = editor.Subviews.First (v => v.GetType () == typeof (ProgressBar) && v.Title == (string)e.Value);
		};
		editor.Add (pbList);
		pbList.SelectedItem = 0;

		#region ColorPicker
		ColorName ChooseColor (string text, ColorName colorName)
		{

			var colorPicker = new ColorPicker {
				Title = text,
				SelectedColor = colorName
			};

			var dialog = new Dialog {
				Title = text
			};

			dialog.LayoutComplete += (sender, args) => {
				// TODO: Replace with Dim.Auto
				dialog.X = pbList.Frame.X;
				dialog.Y = pbList.Frame.Height;

				dialog.Bounds = new Rect (0, 0, colorPicker.Frame.Width, colorPicker.Frame.Height);

				Application.Top.LayoutSubviews ();
			};

			dialog.Add (colorPicker);
			colorPicker.ColorChanged += (s, e) => {
				dialog.RequestStop ();
			};
			Application.Run (dialog);

			var retColor = colorPicker.SelectedColor;
			colorPicker.Dispose ();

			return retColor;
		}

		var fgColorPickerBtn = new Button {
			Text = "Foreground HotNormal Color",
			X = Pos.Center (),
			Y = Pos.Bottom (pbList),
		};
		editor.Add (fgColorPickerBtn);
		fgColorPickerBtn.Clicked += (s, e) => {
			var newColor = ChooseColor (fgColorPickerBtn.Text, editor.ViewToEdit.ColorScheme.HotNormal.Foreground.ColorName);
			var cs = new ColorScheme (editor.ViewToEdit.ColorScheme) {
				HotNormal = new Attribute (newColor, editor.ViewToEdit.ColorScheme.HotNormal.Background)
			};
			editor.ViewToEdit.ColorScheme = cs;
		};

		var bgColorPickerBtn = new Button {
			X = Pos.Center (),
			Y = Pos.Bottom (fgColorPickerBtn),
			Text = "Background HotNormal Color"
		};
		editor.Add (bgColorPickerBtn);
		bgColorPickerBtn.Clicked += (s, e) => {
			var newColor = ChooseColor (fgColorPickerBtn.Text, editor.ViewToEdit.ColorScheme.HotNormal.Background.ColorName);
			var cs = new ColorScheme (editor.ViewToEdit.ColorScheme) {
				HotNormal = new Attribute (editor.ViewToEdit.ColorScheme.HotNormal.Foreground, newColor)
			};
			editor.ViewToEdit.ColorScheme = cs;
		};
		#endregion

		var pbFormatEnum = Enum.GetValues (typeof (ProgressBarFormat)).Cast<ProgressBarFormat> ().ToList ();
		var rbPBFormat = new RadioGroup (pbFormatEnum.Select (e => e.ToString ()).ToArray ()) {
			BorderStyle = LineStyle.Single,
			Title = "ProgressBarFormat",
			X = Pos.Left (pbList),
			Y = Pos.Bottom (bgColorPickerBtn) + 1,
		};
		editor.Add (rbPBFormat);

		var button = new Button ("Start timer") {
			X = Pos.Center (),
			Y = Pos.Bottom (rbPBFormat) + 1
		};

		editor.Add (button);
		var blocksPB = new ProgressBar {
			Title = "Blocks",
			X = Pos.Center (),
			Y = Pos.Bottom (button) + 1,
			Width = Dim.Width (pbList),
			BorderStyle = LineStyle.Single,
			CanFocus = true
		};
		editor.Add (blocksPB);

		var continuousPB = new ProgressBar {
			Title = "Continuous",
			X = Pos.Center (),
			Y = Pos.Bottom (blocksPB) + 1,
			Width = Dim.Width (pbList),
			ProgressBarStyle = ProgressBarStyle.Continuous,
			BorderStyle = LineStyle.Single,
			CanFocus = true
		};
		editor.Add (continuousPB);

		button.Clicked += (s, e) => {
			if (_fractionTimer == null) {
				//blocksPB.Enabled = false;
				blocksPB.Fraction = 0;
				continuousPB.Fraction = 0;
				float fractionSum = 0;
				_fractionTimer = new Timer (_ => {
					fractionSum += fractionStep;
					blocksPB.Fraction = fractionSum;
					continuousPB.Fraction = fractionSum;
					if (fractionSum > 1) {
						_fractionTimer.Dispose ();
						_fractionTimer = null;
						button.Enabled = true;
					}
					Application.Wakeup ();
				}, null, 0, _timerTick);
			}
		};

		var ckbBidirectional = new CheckBox ("BidirectionalMarquee", true) {
			X = Pos.Center (),
			Y = Pos.Bottom (continuousPB) + 1
		};
		editor.Add (ckbBidirectional);

		var marqueesBlocksPB = new ProgressBar {
			Title = "Marquee Blocks",
			X = Pos.Center (),
			Y = Pos.Bottom (ckbBidirectional) + 1,
			Width = Dim.Width (pbList),
			ProgressBarStyle = ProgressBarStyle.MarqueeBlocks,
			BorderStyle = LineStyle.Single,
			CanFocus = true
		};
		editor.Add (marqueesBlocksPB);

		var marqueesContinuousPB = new ProgressBar {
			Title = "Marquee Continuous",
			X = Pos.Center (),
			Y = Pos.Bottom (marqueesBlocksPB) + 1,
			Width = Dim.Width (pbList),
			ProgressBarStyle = ProgressBarStyle.MarqueeContinuous,
			BorderStyle = LineStyle.Single,
			CanFocus = true
		};
		editor.Add (marqueesContinuousPB);

		pbList.SetSource (editor.Subviews.Where (v => v.GetType () == typeof (ProgressBar)).Select (v => v.Title).ToList ());
		pbList.SelectedItem = 0;

		rbPBFormat.SelectedItemChanged += (s, e) => {
			blocksPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
			continuousPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
			marqueesBlocksPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
			marqueesContinuousPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
		};

		ckbBidirectional.Toggled += (s, e) => {
			ckbBidirectional.Checked = marqueesBlocksPB.BidirectionalMarquee = marqueesContinuousPB.BidirectionalMarquee = (bool)!e.OldValue;
		};

		_pulseTimer = new Timer (_ => {
			marqueesBlocksPB.Text = marqueesContinuousPB.Text = DateTime.Now.TimeOfDay.ToString ();
			marqueesBlocksPB.Pulse ();
			marqueesContinuousPB.Pulse ();
			Application.Wakeup ();
		}, null, 0, 300);


		Application.Top.Unloaded += Top_Unloaded;

		void Top_Unloaded (object sender, EventArgs args)
		{
			if (_fractionTimer != null) {
				_fractionTimer.Dispose ();
				_fractionTimer = null;
			}
			if (_pulseTimer != null) {
				_pulseTimer.Dispose ();
				_pulseTimer = null;
			}
			Application.Top.Unloaded -= Top_Unloaded;
		}

		Application.Run (editor);
		Application.Shutdown ();
	}

	public override void Run ()
	{
	}
}