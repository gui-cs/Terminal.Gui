using System;
using static Terminal.Gui.Application;

namespace Terminal.Gui {
	/// <summary>
	/// Event arguments for events about <see cref="ApplicationRunState"/>
	/// </summary>
	public class RunStateEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="RunStateEventArgs"/> class
		/// </summary>
		/// <param name="state"></param>
		public RunStateEventArgs (ApplicationRunState state)
		{
			State = state;
		}

		/// <summary>
		/// The state being reported on by the event
		/// </summary>
		public ApplicationRunState State { get; }
	}
}
