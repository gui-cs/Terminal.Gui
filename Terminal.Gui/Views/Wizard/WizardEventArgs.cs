using System;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="EventArgs"/> for <see cref="Wizard.WizardStep"/> transition events.
	/// </summary>
	public class WizardButtonEventArgs : EventArgs {
		/// <summary>
		/// Set to true to cancel the transition to the next step.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Initializes a new instance of <see cref="WizardButtonEventArgs"/>
		/// </summary>
		public WizardButtonEventArgs ()
		{
			Cancel = false;
		}
	}

	/// <summary>
	/// <see cref="EventArgs"/> for <see cref="Wizard.WizardStep"/> events.
	/// </summary>
	public class StepChangeEventArgs : EventArgs {
		/// <summary>
		/// The current (or previous) <see cref="Wizard.WizardStep"/>.
		/// </summary>
		public Wizard.WizardStep OldStep { get; }

		/// <summary>
		/// The <see cref="Wizard.WizardStep"/> the <see cref="Wizard"/> is changing to or has changed to.
		/// </summary>
		public Wizard.WizardStep NewStep { get; }

		/// <summary>
		/// Event handlers can set to true before returning to cancel the step transition.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Initializes a new instance of <see cref="StepChangeEventArgs"/>
		/// </summary>
		/// <param name="oldStep">The current <see cref="Wizard.WizardStep"/>.</param>
		/// <param name="newStep">The new <see cref="Wizard.WizardStep"/>.</param>
		public StepChangeEventArgs (Wizard.WizardStep oldStep, Wizard.WizardStep newStep)
		{
			OldStep = oldStep;
			NewStep = newStep;
			Cancel = false;
		}
	}
}
