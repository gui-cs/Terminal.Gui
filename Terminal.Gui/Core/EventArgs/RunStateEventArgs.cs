using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Terminal.Gui.Application;

namespace Terminal.Gui {
	public class RunStateEventArgs : EventArgs {

		public RunStateEventArgs (RunState state)
		{
			State = state;
		}

		public RunState State { get; }
	}
}
