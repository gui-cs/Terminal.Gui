using System;
using System.Linq;
using System.Threading;
using Terminal.Gui;
using static UICatalog.Scenarios.Frames;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ProgressBar Styles", Description: "Shows the ProgressBar Styles.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Progress")]
	[ScenarioCategory ("Threading")]

	// TODO: Add enable/disable to show that that is working
	// TODO: Clean up how FramesEditor works 
	// TODO: Better align rpPBFormat

	public class ProgressBarStyles : Scenario {
		private Timer _fractionTimer;
		private Timer _pulseTimer;
		private const uint _timerTick = 20;

		public override void Init ()
		{
			Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();

			var editor = new FramesEditor () {
				Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
				BorderStyle = LineStyle.Single
			};
			editor.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

			const float fractionStep = 0.01F;
			const int pbWidth = 25;

			var pbFormatEnum = Enum.GetValues (typeof (ProgressBarFormat)).Cast<ProgressBarFormat> ().ToList ();

			var rbPBFormat = new RadioGroup (pbFormatEnum.Select (e => e.ToString ()).ToArray ()) {
				X = Pos.Center (),
				Y = 10
			};
			editor.Add (rbPBFormat);

			var button = new Button ("Start timer") {
				X = Pos.Center (),
				Y = Pos.Bottom (rbPBFormat) + 1
			};

			editor.Add (button);
			var blocksPB = new ProgressBar () {
				Title = "Blocks",
				X = Pos.Center (),
				Y = Pos.Bottom (button) + 1,
				Width = pbWidth,
				BorderStyle = LineStyle.Single
			};
			editor.Add (blocksPB);

			var continuousPB = new ProgressBar () {
				Title = "Continuous",
				X = Pos.Center (),
				Y = Pos.Bottom (blocksPB) + 1,
				Width = pbWidth,
				ProgressBarStyle = ProgressBarStyle.Continuous,
				BorderStyle = LineStyle.Single
			};
			editor.Add (continuousPB);

			button.Clicked += (s, e) => {
				if (_fractionTimer == null) {
					//blocksPB.Enabled = false;
					blocksPB.Fraction = 0;
					continuousPB.Fraction = 0;
					float fractionSum = 0;
					_fractionTimer = new Timer ((_) => {
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

			var marqueesBlocksPB = new ProgressBar () {
				Title = "Marquee Blocks",
				X = Pos.Center (),
				Y = Pos.Bottom (ckbBidirectional) + 1,
				Width = pbWidth,
				ProgressBarStyle = ProgressBarStyle.MarqueeBlocks,
				BorderStyle = LineStyle.Single
			};
			editor.Add (marqueesBlocksPB);

			var marqueesContinuousPB = new ProgressBar () {
				Title = "Marquee Continuous",
				X = Pos.Center (),
				Y = Pos.Bottom (marqueesBlocksPB) + 1,
				Width = pbWidth,
				ProgressBarStyle = ProgressBarStyle.MarqueeContinuous,
				BorderStyle = LineStyle.Single
			};
			editor.Add (marqueesContinuousPB);

			rbPBFormat.SelectedItemChanged += (s, e) => {
				blocksPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
				continuousPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
				marqueesBlocksPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
				marqueesContinuousPB.ProgressBarFormat = (ProgressBarFormat)e.SelectedItem;
			};

			ckbBidirectional.Toggled += (s, e) => {
				ckbBidirectional.Checked = marqueesBlocksPB.BidirectionalMarquee = marqueesContinuousPB.BidirectionalMarquee = (bool)!e.OldValue;
			};

			_pulseTimer = new Timer ((_) => {
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

			var pbs = editor.Subviews.Where (v => v.GetType () == typeof (ProgressBar)).ToList ();
			var pbList = new ListView (pbs) {
				Title = "Focused ProgressBar",
				Y = 0,
				X = Pos.Center(),
				Width = 30,
				Height = 7,
				BorderStyle = LineStyle.Single
			};
			pbList.SelectedItemChanged += (sender, e) => {
				editor.ViewToEdit = (View)e.Value;
			};
			editor.Add (pbList);
			pbList.SelectedItem = 0;

			Application.Run (editor);
			Application.Shutdown ();
		}

		public override void Run ()
		{
		}
	}
}