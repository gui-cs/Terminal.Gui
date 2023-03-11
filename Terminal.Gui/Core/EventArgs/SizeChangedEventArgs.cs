using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui{
	public class SizeChangedEventArgs : EventArgs {

		public SizeChangedEventArgs (Size size)
		{
			Size = size;
		}

		public Size Size { get; }
	}
}
