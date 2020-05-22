using Mono.Terminal;
using System;
using System.Threading;
using Terminal.Gui;

namespace UICatalog {
	// 
	// This would be a great scenario to show of threading (Issue #471)
	//
	[ScenarioMetadata (Name: "Progress", Description: "Shows off ProgressBar.")]
	[ScenarioCategory ("Controls")]
	class Progress : Scenario {

		private ProgressBar _activityProgressBar;
		private ProgressBar _pulseProgressBar;
		//private Timer _timer;
		private object _timeoutToken = null;
		bool _pause;
		Button pauseButton;

		public override void Setup ()
		{
			pauseButton = new Button ("Pause") {
				X = Pos.Center (),
				Y = Pos.Center () - 5,
				Clicked = () => Pause()
			};

			Win.Add (new Button ("Start Timer") {
				X = Pos.Left(pauseButton) - 20,
				Y = Pos.Y(pauseButton),
				Clicked = () => Start ()
			});

			Win.Add (new Button ("Stop Timer") {
				X = Pos.Right (pauseButton) + 20, // BUGBUG: Right is somehow adding additional width
				Y = Pos.Y (pauseButton),
				Clicked = () => Stop()
			});

			Win.Add (pauseButton);

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
			//_timer?.Dispose ();
			//_timer = null;
			if (_timeoutToken != null) {
				Application.MainLoop.RemoveTimeout (_timeoutToken);
			}
			base.Dispose (disposing);
		}

		private bool Pulse (MainLoop _)
		{
			if (_activityProgressBar.Fraction + 0.01F >= 1) {
				_activityProgressBar.Fraction = 0F;
			} else {
				_activityProgressBar.Fraction += 0.01F;
			}
			_pulseProgressBar.Pulse ();
			if (_pause)
				return false;
			return true;
		}

		private void Pause ()
		{
			_pause = !_pause;
			if (_pause) {
				pauseButton.Text = "Resume";
			} else {
				pauseButton.Text = "Pause";
				Start ();
			}
		}

		private void Start ()
		{
			//_timer?.Dispose ();
			//_timer = null;

			if (!_pause) {
				_activityProgressBar.Fraction = 0F;
				_pulseProgressBar.Fraction = 0F;
			}

			//_timer = new Timer ((o) => Application.MainLoop.Invoke (() => Pulse ()), null, 0, 250);

			// BUGBUG: This timeout does nothing but return true, however it trigger the Application.MainLoop
			// to run the Action. Without this timeout, the display updates are random, 
			// or triggered by user interaction with the UI. See #155
			_timeoutToken = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (10), Pulse);
		}

		private void Stop ()
		{
			//_timer?.Dispose ();
			//_timer = null;
			if (_timeoutToken != null) {
				Application.MainLoop.RemoveTimeout (_timeoutToken);
			}

			_activityProgressBar.Fraction = 1F;
			_pulseProgressBar.Fraction = 1F;
			if (_pause) {
				_pause = false;
				pauseButton.Text = "Pause";
			}
		}
	}
}