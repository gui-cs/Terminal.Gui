//
// ListView.cs: ListView control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	public interface IListDataSource {
		int Count { get; }

	}

	/// <summary>
	/// ListView widget renders a list of data.
	/// </summary>
	public class ListView : View {
		public ListView (Rect rect, IListDataSource source, (ustring title, int width) [] headers = null) : base (rect)
		{
		}
	}
}
