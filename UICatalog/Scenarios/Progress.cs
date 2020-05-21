using System;
using Terminal.Gui;

namespace UICatalog {
	// 
	// This would be a great scenario to show of threading (Issue #471)
	//
	[ScenarioMetadata (Name: "Progress", Description: "Shows off ProgressBar.")]
	[ScenarioCategory ("Controls")]
	class Progress : Scenario {

		private ProgressBar _progressBar;
		public override void Setup ()
		{
			Win.Add (new Button ("Start") {
				X = Pos.Center () - 20,
				Y = Pos.Center () - 5,
				Clicked = () => Start ()
			}); ;

			Win.Add (new Button ("Stop") {
				X = Pos.Center () + 10,
				Y = Pos.Center () - 5,
				Clicked = () => Stop()
			});

			_progressBar = new ProgressBar () {
				X = Pos.Center (),
				// BUGBUG: If you remove the +1 below the control is drawn at top?!?!
				Y = Pos.Center ()+1,
				Width = 30,
				Fraction = 0.25F,
			};
			Win.Add (_progressBar);
		}

		private void Start ()
		{
			_progressBar.Fraction = 0F;
		}

		private void Stop ()
		{
			_progressBar.Fraction = 1F;
		}
	}
}