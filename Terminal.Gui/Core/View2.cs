using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using NStack;

namespace Terminal.Gui {

	public class View2 : View, ISupportInitializeNotification {
		public Thickness Margin { get; set; }

		void DrawThickness (Thickness thickness)
		{

		}

		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			DrawThickness (Margin);
		}


	}
}
