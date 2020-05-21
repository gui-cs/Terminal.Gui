using System;
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
		public override void Setup ()
		{
			Win.Add (new Button ("Start") {
				X = Pos.Center () - 20,
				Y = Pos.Center () - 5,
				Clicked = () => Start ()
			});

			Win.Add (new Button ("Pulse") {
				X = Pos.Center () - 5,
				Y = Pos.Center () - 5,
				Clicked = () => Pulse ()
			}); 


			Win.Add (new Button ("Stop") {
				X = Pos.Center () + 10,
				Y = Pos.Center () - 5,
				Clicked = () => Stop()
			});

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

		private void Pulse ()
		{
			if (_activityProgressBar.Fraction + 0.1F >= 1) {
				_activityProgressBar.Fraction = 0F;
			} else {
				_activityProgressBar.Fraction += 0.1F;
			}
			_pulseProgressBar.Pulse ();
		}

		private void Start ()
		{
			_activityProgressBar.Fraction = 0F;
			_pulseProgressBar.Fraction = 0F;
		}

		private void Stop ()
		{
			_activityProgressBar.Fraction = 1F;
			_pulseProgressBar.Fraction = 1F;
		}
	}
}