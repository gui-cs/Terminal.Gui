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
		private Timer _timer;
		private object _timeoutToken;

		public override void Setup ()
		{
			var pulseButton = new Button ("Pulse") {
				X = Pos.Center (),
				Y = Pos.Center () - 5,
				Clicked = () => Pulse ()
			};

			Win.Add (new Button ("Start Timer") {
				X = Pos.Left(pulseButton) - 20,
				Y = Pos.Y(pulseButton),
				Clicked = () => Start ()
			});

			Win.Add (new Button ("Stop Timer") {
				X = Pos.Right (pulseButton) + 20, // BUGBUG: Right is somehow adding additional width
				Y = Pos.Y (pulseButton),
				Clicked = () => Stop()
			});

			Win.Add (pulseButton);

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
			Application.MainLoop.RemoveTimeout (_timeoutToken);
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

			_timer = new Timer ((o) => Application.MainLoop.Invoke (() => Pulse ()), null, 0, 250);

			// BUGBUG: This timeout does nothing but return true, however it trigger the Application.MainLoop
			// to run the Action. Without this timeout, the display updates are random, 
			// or triggered by user interaction with the UI. See #155
			//_timeoutToken = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (10), loop => true);
		}

		private void Stop ()
		{
			_timer?.Dispose ();
			_timer = null;
			Application.MainLoop.RemoveTimeout (_timeoutToken);

			_activityProgressBar.Fraction = 1F;
			_pulseProgressBar.Fraction = 1F;
		}
	}
}