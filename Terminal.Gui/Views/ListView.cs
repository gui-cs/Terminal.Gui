//
// ListView.cs: ListView control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// 
// TODO:
//   - Should we support multiple columns, if so, how should that be done?
//   - Show mark for items that have been marked.
//   - Mouse support
//   - Scrollbars?
//
// Column considerations:
//   - Would need a way to specify widths
//   - Should it automatically extract data out of structs/classes based on public fields/properties?
//   - It seems that this would be useful just for the "simple" API, not the IListDAtaSource, as that one has full support for it.
//   - Should a function be specified that retrieves the individual elements?   
// 
using System;
using System.Collections;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// Implement this interface to provide your own custom rendering for a list.
	/// </summary>
	public interface IListDataSource {
		/// <summary>
		/// Returns the number of elements to display
		/// </summary>
		int Count { get; }

		/// <summary>
		/// This method is invoked to render a specified item, the method should cover the entire provided width.
		/// </summary>
		/// <returns>The render.</returns>
		/// <param name="selected">Describes whether the item being rendered is currently selected by the user.</param>
		/// <param name="item">The index of the item to render, zero for the first item and so on.</param>
		/// <param name="col">The column where the rendering will start</param>
		/// <param name="line">The line where the rendering will be done.</param>
		/// <param name="width">The width that must be filled out.</param>
		/// <remarks>
		///   The default color will be set before this method is invoked, and will be based on whether the item is selected or not.
		/// </remarks>
		void Render (bool selected, int item, int col, int line, int width);

		/// <summary>
		/// Should return whether the specified item is currently marked.
		/// </summary>
		/// <returns><c>true</c>, if marked, <c>false</c> otherwise.</returns>
		/// <param name="item">Item index.</param>
		bool IsMarked (int item);

		/// <summary>
		/// Flags the item as marked.
		/// </summary>
		/// <param name="item">Item index.</param>
		/// <param name="value">If set to <c>true</c> value.</param>
		void SetMark (int item, bool value);
	}

	/// <summary>
	/// ListView widget renders a list of data.
	/// </summary>
	/// <remarks>
	/// <para>
	///   The ListView displays lists of data and allows the user to scroll through the data
	///   and optionally mark elements of the list (controlled by the AllowsMark property).  
	/// </para>
	/// <para>
	///   The ListView can either render an arbitrary IList object (for example, arrays, List&lt;T&gt;
	///   and other collections) which are drawn by drawing the string/ustring contents or the 
	///   result of calling ToString().   Alternatively, you can provide you own IListDataSource
	///   object that gives you full control of what is rendered.
	/// </para>
	/// <para>
	///   The ListView can display any object that implements the System.Collection.IList interface,
	///   string values are converted into ustring values before rendering, and other values are
	///   converted into ustrings by calling ToString() and then converting to ustring.
	/// </para>
	/// <para>
	///   If you must change the contents of the ListView, set the Source property (when you are
	///   providing your own rendering via the IListDataSource implementation) or call SetSource
	///   when you are providing an IList.
	/// </para>
	/// </remarks>
	public class ListView : View {
		int top;
		int selected;

		//
		// This class is the built-in IListDataSource that renders arbitrary
		// IList instances
		//
		class ListWrapper : IListDataSource {
			IList src;
			public ListView Container;
			public ConsoleDriver Driver;
			BitArray marks;
			int count;

			public ListWrapper (IList source)
			{
				count = source.Count;
				marks = new BitArray (count);
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

			public void Render (bool marked, int item, int col, int line, int width)
			{
				Container.Move (col, line);
				var t = src [item];
				if (t is ustring) {
					RenderUstr (t as ustring, col, line, width);
				} else if (t is string) {
					RenderUstr (t as string, col, line, width);
				} else
					RenderUstr (t.ToString (), col, line, width);
			}

			public bool IsMarked (int item)
			{
				if (item >= 0 && item < count)
					return marks [item];
				return false;
			}

			public void SetMark (int item, bool value)
			{
				if (item >= 0 && item < count)
					marks [item] = value;
			}
		}

		IListDataSource source;
		/// <summary>
		/// Gets or sets the IListDataSource backing this view, use SetSource() if you want to set a new IList source.
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

		/// <summary>
		/// Sets the source to an IList value, if you want to set a full IListDataSource, use the Source property.
		/// </summary>
		/// <value>An item implementing the IList interface.</value>
		public void SetSource (IList source)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));
			Source = MakeWrapper (source);
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

		/// <summary>
		/// Initializes a new ListView that will display the contents of the object implementing the IList interface, with relative positioning
		/// </summary>
		/// <param name="source">An IList data source, if the elements of the IList are strings or ustrings, the string is rendered, otherwise the ToString() method is invoked on the result.</param>
		public ListView (IList source) : this (MakeWrapper (source))
		{
			((ListWrapper)(Source)).Container = this;
			((ListWrapper)(Source)).Driver = Driver;
		}

		/// <summary>
		/// Initializes a new ListView that will display the provided data source, uses relative positioning.
		/// </summary>
		/// <param name="source">IListDataSource object that provides a mechanism to render the data. The number of elements on the collection should not change, if you must change, set the "Source" property to reset the internal settings of the ListView.</param>
		public ListView (IListDataSource source) : base ()
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));
			Source = source;
			CanFocus = true;
		}

		/// <summary>
		/// Initializes a new ListView that will display the contents of the object implementing the IList interface with an absolute position.
		/// </summary>
		/// <param name="rect">Frame for the listview.</param>
		/// <param name="source">An IList data source, if the elements of the IList are strings or ustrings, the string is rendered, otherwise the ToString() method is invoked on the result.</param>
		public ListView (Rect rect, IList source) : this (rect, MakeWrapper (source))
		{
			((ListWrapper)(Source)).Container = this;
			((ListWrapper)(Source)).Driver = Driver;
		}

		/// <summary>
		/// Initializes a new ListView that will display the provided data source  with an absolute position
		/// </summary>
		/// <param name="rect">Frame for the listview.</param>
		/// <param name="source">IListDataSource object that provides a mechanism to render the data. The number of elements on the collection should not change, if you must change, set the "Source" property to reset the internal settings of the ListView.</param>
		public ListView (Rect rect, IListDataSource source) : base (rect)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));
			Source = source;
			CanFocus = true;
		}

		/// <summary>
		/// Redraws the ListView
		/// </summary>
		/// <param name="region">Region.</param>
		public override void Redraw(Rect region)
		{
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Frame;
			var item = top;
			bool focused = HasFocus;

			for (int row = 0; row < f.Height; row++, item++) {
				bool isSelected = item == selected;

				var newcolor = focused ? (isSelected ? ColorScheme.Focus : ColorScheme.Normal) : ColorScheme.Normal;
				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}
				if (item >= source.Count)
					for (int c = 0; c < f.Width; c++)
						Driver.AddRune (' ');
				else
					Source.Render (isSelected, item, 0, row, f.Width);
			}
		}

		/// <summary>
		/// This event is raised when the cursor selection has changed.
		/// </summary>
		public event Action SelectedChanged;

		/// <summary>
		/// Handles cursor movement for this view, passes all other events.
		/// </summary>
		/// <returns><c>true</c>, if key was processed, <c>false</c> otherwise.</returns>
		/// <param name="kb">Keyboard event.</param>
		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.CursorUp:
			case Key.ControlP:
				if (selected > 0) {
					selected--;
					if (selected < top)
						top = selected;
					if (SelectedChanged != null)
						SelectedChanged ();
					SetNeedsDisplay ();
				}
				return true;

			case Key.CursorDown:
			case Key.ControlN:
				if (selected + 1 < source.Count) {
					selected++;
					if (selected >= top + Frame.Height)
						top++;
					if (SelectedChanged != null)
						SelectedChanged ();
					SetNeedsDisplay ();
				}
				return true;

			case Key.ControlV:
			case Key.PageDown:
				var n = (selected + Frame.Height);
				if (n > source.Count)
					n = source.Count - 1;
				if (n != selected) {
					selected = n;
					if (source.Count >= Frame.Height)
						top = selected;
					else
						top = 0;
					if (SelectedChanged != null)
						SelectedChanged ();
					SetNeedsDisplay ();
				}
				return true;

			case Key.PageUp:
				n = (selected - Frame.Height);
				if (n < 0)
					n = 0;
				if (n != selected) {
					selected = n;
					top = selected;
					if (SelectedChanged != null)
						SelectedChanged ();
					SetNeedsDisplay ();
				}
				return true;
			}
			return base.ProcessKey (kb);
		}

		/// <summary>
		/// Positions the cursor in this view
		/// </summary>
		public override void PositionCursor()
		{
			Move (0, selected-top);
		}

		public override bool MouseEvent(MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked))
				return false;

			if (!HasFocus) 
				SuperView.SetFocus (this);

			if (me.Y + top >= source.Count)
				return true;

			selected = top + me.Y;
			SetNeedsDisplay ();
			return true;
		}
	}
}
