using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui{
	public class ViewEventArgs :EventArgs{
		public ViewEventArgs (View view)
		{
			View = view;
		}

		public View View { get; }
	}
}
