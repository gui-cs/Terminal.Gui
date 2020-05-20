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
using System.Threading;
using System.Threading.Tasks;
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
		/// <param name="container">The list view to render.</param>
		/// <param name="driver">The console driver to render.</param>
		/// <param name="selected">Describes whether the item being rendered is currently selected by the user.</param>
		/// <param name="item">The index of the item to render, zero for the first item and so on.</param>
		/// <param name="col">The column where the rendering will start</param>
		/// <param name="line">The line where the rendering will be done.</param>
		/// <param name="width">The width that must be filled out.</param>
		/// <remarks>
		///   The default color will be set before this method is invoked, and will be based on whether the item is selected or not.
		/// </remarks>
		void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width);

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

		/// <summary>
		/// Return the source as IList.
		/// </summary>
		/// <returns></returns>
		IList ToList ();
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
	/// <para>
	///   When AllowsMark is set to true, then the rendering will prefix the list rendering with
	///   [x] or [ ] and bind the space character to toggle the selection.  If you desire a different
	///   marking style do not set the property and provide your own custom rendering.
	/// </para>
	/// </remarks>
	public class ListView : View {
		int top;
		int selected;

		IListDataSource source;
		/// <summary>
		/// Gets or sets the IListDataSource backing this view, use SetSource() if you want to set a new IList source.
		/// </summary>
		/// <value>The source.</value>
		public IListDataSource Source {
			get => source;
			set {
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
				Source = null;
			else {
				Source = MakeWrapper (source);
			}
		}

		/// <summary>
		/// Sets the source to an IList value asynchronously, if you want to set a full IListDataSource, use the Source property.
		/// </summary>
		/// <value>An item implementing the IList interface.</value>
		public Task SetSourceAsync (IList source)
		{
			return Task.Factory.StartNew (() => {
				if (source == null)
					Source = null;
				else
					Source = MakeWrapper (source);
				return source;
			}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		bool allowsMarking;
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.ListView"/> allows items to be marked.
		/// </summary>
		/// <value><c>true</c> if allows marking elements of the list; otherwise, <c>false</c>.
		/// </value>
		/// <remarks>
		/// If set to true, this will default to rendering the marked with "[x]", and unmarked valued with "[ ]"
		/// spaces.   If you desire a different rendering, you need to implement your own renderer.   This will
		/// also by default process the space character as a toggle for the selection.
		/// </remarks>
		public bool AllowsMarking {
			get => allowsMarking;
			set {
				allowsMarking = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// If set to true allows more than one item to be selected. If false only allow one item selected.
		/// </summary>
		public bool AllowsMultipleSelection { get; set; } = true;

		/// <summary>
		/// Gets or sets the item that is displayed at the top of the listview
		/// </summary>
		/// <value>The top item.</value>
		public int TopItem {
			get => top;
			set {
				if (source == null)
					return;

				if (top < 0 || top >= source.Count)
					throw new ArgumentException ("value");
				top = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the currently selected item.
		/// </summary>
		/// <value>The selected item.</value>
		public int SelectedItem {
			get => selected;
			set {
				if (source == null)
					return;
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
		}

		/// <summary>
		/// Initializes a new ListView that will display the provided data source, uses relative positioning.
		/// </summary>
		/// <param name="source">IListDataSource object that provides a mechanism to render the data. The number of elements on the collection should not change, if you must change, set the "Source" property to reset the internal settings of the ListView.</param>
		public ListView (IListDataSource source) : base ()
		{
			Source = source;
			CanFocus = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.ListView"/> class.   You must set the Source property for this to show something.
		/// </summary>
		public ListView () : base ()
		{
		}

		/// <summary>
		/// Initializes a new ListView that will display the contents of the object implementing the IList interface with an absolute position.
		/// </summary>
		/// <param name="rect">Frame for the listview.</param>
		/// <param name="source">An IList data source, if the elements of the IList are strings or ustrings, the string is rendered, otherwise the ToString() method is invoked on the result.</param>
		public ListView (Rect rect, IList source) : this (rect, MakeWrapper (source))
		{
		}

		/// <summary>
		/// Initializes a new ListView that will display the provided data source  with an absolute position
		/// </summary>
		/// <param name="rect">Frame for the listview.</param>
		/// <param name="source">IListDataSource object that provides a mechanism to render the data. The number of elements on the collection should not change, if you must change, set the "Source" property to reset the internal settings of the ListView.</param>
		public ListView (Rect rect, IListDataSource source) : base (rect)
		{
			Source = source;
			CanFocus = true;
		}

		/// <summary>
		/// Redraws the ListView
		/// </summary>
		/// <param name="region">Region.</param>
		public override void Redraw (Rect region)
		{
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Frame;
			var item = top;
			bool focused = HasFocus;
			int col = allowsMarking ? 4 : 0;

			for (int row = 0; row < f.Height; row++, item++) {
				bool isSelected = item == selected;

				var newcolor = focused ? (isSelected ? ColorScheme.Focus : ColorScheme.Normal) : ColorScheme.Normal;
				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}

				Move (0, row);
				if (source == null || item >= source.Count) {
					for (int c = 0; c < f.Width; c++)
						Driver.AddRune (' ');
				} else {
					if (allowsMarking) {
						Driver.AddStr (source.IsMarked (item) ? (AllowsMultipleSelection ? "[x] " : "(o)") : (AllowsMultipleSelection ? "[ ] " : "( )"));
					}
					Source.Render (this, Driver, isSelected, item, col, row, f.Width - col);
				}
			}
		}

		/// <summary>
		/// This event is raised when the cursor selection has changed.
		/// </summary>
		public event EventHandler<ListViewItemEventArgs> SelectedChanged;

		/// <summary>
		/// This event is raised on Enter key or Double Click to open the selected item.
		/// </summary>
		public event EventHandler<ListViewItemEventArgs> OpenSelectedItem;

		/// <summary>
		/// This event is raised on Enter key or Double Click to open the selected item.
		/// </summary>
		public event EventHandler OpenSelectedItem;

		/// <summary>
		/// Handles cursor movement for this view, passes all other events.
		/// </summary>
		/// <returns><c>true</c>, if key was processed, <c>false</c> otherwise.</returns>
		/// <param name="kb">Keyboard event.</param>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (source == null)
				return base.ProcessKey (kb);

			switch (kb.Key) {
			case Key.CursorUp:
			case Key.ControlP:
				return MoveUp ();

			case Key.CursorDown:
			case Key.ControlN:
				return MoveDown ();

			case Key.ControlV:
			case Key.PageDown:
				return MovePageDown ();

			case Key.PageUp:
				return MovePageUp ();

			case Key.Space:
				if (MarkUnmarkRow ())
					return true;
				else
					break;

			case Key.Enter:        
				OnOpenSelectedItem ();
				break;

			}
			return base.ProcessKey (kb);
		}

		/// <summary>
		/// Prevents marking if it's not allowed mark and if it's not allows multiple selection.
		/// </summary>
		/// <returns></returns>
		public virtual bool AllowsAll ()
		{
			if (!allowsMarking)
				return false;
			if (!AllowsMultipleSelection) {
				for (int i = 0; i < Source.Count; i++) {
					if (Source.IsMarked (i) && i != selected) {
						Source.SetMark (i, false);
						return true;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Marks an unmarked row.
		/// </summary>
		/// <returns></returns>
		public virtual bool MarkUnmarkRow ()
		{
			if (AllowsAll ()) {
				Source.SetMark (SelectedItem, !Source.IsMarked (SelectedItem));
				SetNeedsDisplay ();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Moves to the next page.
		/// </summary>
		/// <returns></returns>
		public virtual bool MovePageUp ()
		{
			int n = (selected - Frame.Height);
			if (n < 0)
				n = 0;
			if (n != selected) {
				selected = n;
				top = selected;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Moves to the previous page.
		/// </summary>
		/// <returns></returns>
		public virtual bool MovePageDown ()
		{ 
			var n = (selected + Frame.Height);
			if (n > source.Count)
				n = source.Count - 1;
			if (n != selected) {
				selected = n;
				if (source.Count >= Frame.Height)
					top = selected;
				else
					top = 0;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Moves to the next row.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveDown ()
		{
			if (selected + 1 < source.Count) {
				selected++;
				if (selected >= top + Frame.Height)
					top++;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Moves to the previous row.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveUp ()
		{
			if (selected > 0) {
				selected--;
				if (selected < top)
					top = selected;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		int lastSelectedItem = -1;

		/// <summary>
		/// Invokes the SelectedChanged event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnSelectedChanged ()
		{
			if (selected != lastSelectedItem) {
				var value = source.ToList () [selected];
				SelectedChanged?.Invoke (this, new ListViewItemEventArgs (selected, value));
				lastSelectedItem = selected;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Invokes the OnOpenSelectedItem event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnOpenSelectedItem ()
		{
			var value = source.ToList () [selected];
			OpenSelectedItem?.Invoke (this, new ListViewItemEventArgs (selected, value));

			return true;
		}

		/// <summary>
		/// Positions the cursor in this view
		/// </summary>
		public override void PositionCursor ()
		{
			if (allowsMarking)
				Move (1, selected - top);
			else
				Move (0, selected - top);
		}

		///<inheritdoc cref="MouseEvent(Gui.MouseEvent)"/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp)
				return false;

			if (!HasFocus)
				SuperView.SetFocus (this);

			if (source == null)
				return false;

			if (me.Flags == MouseFlags.WheeledDown) {
				MoveDown ();
				return true;
			} else if (me.Flags == MouseFlags.WheeledUp) {
				MoveUp ();
				return true;
			}

			if (me.Y + top >= source.Count)
				return true;

			selected = top + me.Y;
			if (AllowsAll ()) {
				Source.SetMark (SelectedItem, !Source.IsMarked (SelectedItem));
				SetNeedsDisplay ();
				return true;
			}
			OnSelectedChanged ();
			SetNeedsDisplay ();
			if (me.Flags == MouseFlags.Button1DoubleClicked)
				OnOpenSelectedItem ();
			return true;
		}
	}

	/// <summary>
	/// This class is the built-in IListDataSource that renders arbitrary
	/// IList instances
	/// </summary>
	public class ListWrapper : IListDataSource {
		IList src;
		BitArray marks;
		int count;

		/// <summary>
		/// Constructor based on a source.
		/// </summary>
		/// <param name="source"></param>
		public ListWrapper (IList source)
		{
			count = source.Count;
			marks = new BitArray (count);
			this.src = source;
		}

		/// <summary>
		/// Returns the amount of items in the source.
		/// </summary>
		public int Count => src.Count;

		void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width)
		{
			int byteLen = ustr.Length;
			int used = 0;
			for (int i = 0; i < byteLen;) {
				(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
				var count = Rune.ColumnWidth (rune);
				if (used + count >= width)
					break;
				driver.AddRune (rune);
				used += count;
				i += size;
			}
			for (; used < width; used++) {
				driver.AddRune (' ');
			}
		}

		/// <summary>
		/// Method that render to the appropriate type based on the type of the item passed to it.
		/// </summary>
		/// <param name="container">The ListView.</param>
		/// <param name="driver">The driver used by the caller.</param>
		/// <param name="marked">Informs if it's marked or not.</param>
		/// <param name="item">The item.</param>
		/// <param name="col">The col where to move.</param>
		/// <param name="line">The line where to move.</param>
		/// <param name="width">The item width.</param>
		public void Render (ListView container, ConsoleDriver driver, bool marked, int item, int col, int line, int width)
		{
			container.Move (col, line);
			var t = src [item];
			if (t is ustring) {
				RenderUstr (driver, (ustring)t, col, line, width);
			} else if (t is string) {
				RenderUstr (driver, (string)t, col, line, width);
			} else
				RenderUstr (driver, t.ToString (), col, line, width);
		}

		/// <summary>
		/// Returns true if the item is marked, false otherwise.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns><c>true</c>If is marked.<c>false</c>otherwise.</returns>
		public bool IsMarked (int item)
		{
			if (item >= 0 && item < count)
				return marks [item];
			return false;
		}

		/// <summary>
		/// Sets the item as marked or unmarked based on the value is true or false, respectively.
		/// </summary>
		/// <param name="item">The item</param>
		/// <param name="value"><true>Marks the item.</true><false>Unmarked the item.</false>The value.</param>
		public void SetMark (int item, bool value)
		{
			if (item >= 0 && item < count)
				marks [item] = value;
		}

		/// <summary>
		/// Returns the source as IList.
		/// </summary>
		/// <returns></returns>
		public IList ToList ()
		{
			return src;
		}
	}

	/// <summary>
	/// Gets the item and value to use in an event handler.
	/// </summary>
	public class ListViewItemEventArgs : EventArgs {
		/// <summary>
		/// The item.
		/// </summary>
		public int Item { get; }
		/// <summary>
		/// The item value.
		/// </summary>
		public object Value { get; }

		/// <summary>
		/// Constructor to sets the item and value passed from.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="value">The item value</param>
		public ListViewItemEventArgs (int item, object value)
		{
			Item = item;
			Value = value;
		}
	}
}
