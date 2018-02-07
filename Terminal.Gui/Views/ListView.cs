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
		void Render (int item, int col, int line, int width);
	}

	/// <summary>
	/// ListView widget renders a list of data.
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class ListView : View {
		int top;
		int selected;

		class ListWrapper : IListDataSource {
			IList src;
			public ListView Container;
			public ConsoleDriver Driver;

			public ListWrapper (IList source)
			{
				this.src = source;
			}

			public int Count => src.Count;

			void RenderUstr (ustring ustr, int col, int line, int width)
			{
				int byteLen = ustr.Length;
				int used = 0;
				for (int i = 0; i < byteLen;) {
					(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
					var count = Rune.ColumnWidth (rune);
					if (used+count >= width)
						break;
					Driver.AddRune (rune);
					used += count;
					i += size;
				}
				for (; used < width; used++) {
					Driver.AddRune (' ');
				}
			}

			public void Render (int item, int col, int line, int width)
			{
				Container.Move (col, line);
				var t = src [item];
				if (t is ustring) {
					RenderUstr (t as ustring, col, line, width);
				} else if (t is string) {
					RenderUstr (t as string, col, line, width);
				} else
					RenderUstr (((string)t).ToString (), col, line, width);
			}
		}

		IListDataSource source;
		/// <summary>
		/// Gets or sets the IListDataSource backing this view.
		/// </summary>
		/// <value>The source.</value>
		public IListDataSource Source {
			get => source;
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				source = value;
				top = 0;
				selected = 0;
				SetNeedsDisplay ();
			}
		}

		bool allowsMarking;
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.ListView"/> allows items to be marked.
		/// </summary>
		/// <value><c>true</c> if allows marking elements of the list; otherwise, <c>false</c>.</value>
		public bool AllowsMarking {
			get => allowsMarking;
			set {
				allowsMarking = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the item that is displayed at the top of the listview
		/// </summary>
		/// <value>The top item.</value>
		public int TopItem {
			get => top;
			set {
				if (top < 0 || top >= source.Count)
					throw new ArgumentException ("value");
				top = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the currently selecteded item.
		/// </summary>
		/// <value>The selected item.</value>
		public int SelectedItem {
			get => selected;
			set {
				if (selected < 0 || selected >= source.Count)
					throw new ArgumentException ("value");
				selected = value;
				if (selected < top)
					top = selected;
				else if (selected >= top + Frame.Height)
					top = selected;
			}
		}


		static IListDataSource MakeWrapper (IList source)
		{
			return new ListWrapper (source);
		}

		public ListView (Rect rect, IList source, (ustring title, int width) [] headers = null) : this (rect, MakeWrapper (source), headers)
		{
			((ListWrapper)(Source)).Container = this;
			((ListWrapper)(Source)).Driver = Driver;
		}

		public ListView (Rect rect, IListDataSource source, (ustring title, int width) [] headers = null) : base (rect)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));
			Source = source;
		}

	}
}
