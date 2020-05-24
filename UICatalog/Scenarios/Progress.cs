using System;
using System.Threading;
using Terminal.Gui;

namespace UICatalog {
	// 
	// This would be a great scenario to show of threading (Issue #471)
	//
	[ScenarioMetadata (Name: "Progress", Description: "Shows off ProgressBar and Threading")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Threading")]
	class Progress : Scenario {

		private ProgressBar _activityProgressBar;
		private ProgressBar _pulseProgressBar;
		private Timer _timer;
		private object _timeoutToken = null;

		public override void Setup ()
		{
			var pulseButton = new Button ("Pulse") {
				X = Pos.Center (),
				Y = Pos.Center () - 3,
				Clicked = () => Pulse ()
			};

			var startButton = new Button ("Start Timer") {
				Y = Pos.Y(pulseButton),
				Clicked = () => Start ()
			};

			var stopbutton = new Button ("Stop Timer") {
				Y = Pos.Y (pulseButton),
				Clicked = () => Stop()
			};

			// Center three buttons with 5 spaces between them
			// TODO: Use Pos.Width instead of (Right-Left) when implemented (#502)
			startButton.X = Pos.Left (pulseButton) - (Pos.Right (startButton) - Pos.Left (startButton)) - 5;
			stopbutton.X = Pos.Right (pulseButton) + 5;

			Win.Add (startButton);
			Win.Add (pulseButton);
			Win.Add (stopbutton);

			_activityProgressBar = new ProgressBar () {
				X = Pos.Center (),
				// BUGBUG: If you remove the +1 below the control is drawn at top?!?!
				Y = Pos.Center ()+1,
				Width = 30,
				Fraction = 0.25F,
			};
			Win.Add (_activityProgressBar);

			_pulseProgressBar = new ProgressBar () {
				X = Pos.Center (),
				// BUGBUG: If you remove the +1 below the control is drawn at top?!?!
				Y = Pos.Center () + 3,
				Width = 30,
			};
			Win.Add (_pulseProgressBar);
		}

		protected override void Dispose (bool disposing)
		{
			_timer?.Dispose ();
			_timer = null;
			if (_timeoutToken != null) {
				Application.MainLoop.RemoveTimeout (_timeoutToken);
			}
			base.Dispose (disposing);
		}

		private void Pulse ()
		{
			if (_activityProgressBar.Fraction + 0.01F >= 1) {
				_activityProgressBar.Fraction = 0F;
			} else {
				_activityProgressBar.Fraction += 0.01F;
			}
			_pulseProgressBar.Pulse ();
		}

		private void Start ()
		{
			_timer?.Dispose ();
			_timer = null;

			_activityProgressBar.Fraction = 0F;
			_pulseProgressBar.Fraction = 0F;

			_timer = new Timer ((o) => {
				Application.MainLoop.Invoke (() => Pulse ());
			}, null, 0, 20);
		}

		private void Stop ()
		{
			_timer?.Dispose ();
			_timer = null;
			if (_timeoutToken != null) {
				Application.MainLoop.RemoveTimeout (_timeoutToken);
			}

			_activityProgressBar.Fraction = 1F;
			_pulseProgressBar.Fraction = 1F;
		}
	}
}