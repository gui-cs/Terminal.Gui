using System;

namespace Terminal.Gui {

	public partial class Wizard {
		/// <summary>	
		/// <see cref="EventArgs"/> for <see cref="WizardStep"/> transition events.
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
	}
}