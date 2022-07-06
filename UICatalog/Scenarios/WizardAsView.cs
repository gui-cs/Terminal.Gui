using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "WizardAsView", Description: "Demonstrates how to use the Wizard class in an no-modal way")]
	[ScenarioCategory ("Dialogs")]
	public class WizardAsView : Scenario {

		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			var wizard = new Wizard ($"{GetName ()} - CTRL-Q to Cancel") {
				X = 0,
				Y = 0,
				Width = Application.Driver.Cols,
				Height = Application.Driver.Rows,
			};
			//wizard.Modal = false;
			wizard.Border.Effect3D = false;

			wizard.MovingBack += (args) => {
				//args.Cancel = true;
				//actionLabel.Text = "Moving Back";
			};

			wizard.MovingNext += (args) => {
				//args.Cancel = true;
				//actionLabel.Text = "Moving Next";
			};

			wizard.Finished += (args) => {
				//args.Cancel = true;
				MessageBox.Query ("Step", "Finished", "Ok");
				Application.RequestStop ();
			};

			// Add 1st step
			var firstStep = new Wizard.WizardStep ("End User License Agreement");
			wizard.AddStep (firstStep);
			firstStep.ShowControls = false;
			firstStep.NextButtonText = "Accept!";
			firstStep.HelpText = "This is the End User License Agreement.\n\n\n\n\n\nThis is a test of the emergency broadcast system. This is a test of the emergency broadcast system.\nThis is a test of the emergency broadcast system.\n\n\nThis is a test of the emergency broadcast system.\n\nThis is a test of the emergency broadcast system.\n\n\n\nThe end of the EULA.";

			// Add 2nd step
			var secondStep = new Wizard.WizardStep ("Second Step");
			wizard.AddStep (secondStep);
			secondStep.HelpText = "This is the help text for the Second Step.\n\nPress the button demo changing the Title.\n\nIf First Name is empty the step will prevent moving to the next step.";

			// Add last step
			var lastStep = new Wizard.WizardStep ("The last step");
			wizard.AddStep (lastStep);
			lastStep.HelpText = "The wizard is complete!\n\nPress the Finish button to continue.\n\nPressing ESC will cancel the wizard.";

			// Normally Modal's like Wizard or Dialog can't take focus
			//wizard.CanFocus = true;
			//top.Add (wizard);

			top.ColorScheme = Colors.Error;
			top.Ready += () => {
				// Normally only the view passed to Application.Run gets `OnLoaded` called. 
				//wizard.OnLoaded ();
				Application.Run (wizard);
				Application.RequestStop (top);
			};

			//Application.Top.Loaded += (args)
			//Application.Run<Wizard> (null);

			Application.Run (top);
		   
		}

		// Override Run to NOT call Application.Run since we call it above
		public override void Run ()
		{
		}
	}
}
