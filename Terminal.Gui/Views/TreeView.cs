using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Terminal.Gui.Views {

	/// <summary>
	/// Implement <see cref="ITreeViewItem"/> to provide rendering for a single item in the <see cref="TreeView"/>
	/// </summary>
	public interface ITreeViewItem {
		/// <summary>
		/// Gets arbitrary data associated with the particular <see cref="ITreeViewItem"/>
		/// </summary>
		/// <remarks>This property is not used internally.</remarks>
		object Data { get; }

		/// <summary>
		/// The parent of this <see cref="ITreeViewItem"/>
		/// </summary>
		/// <remarks>This property is used internally by <see cref="TreeView"/> and can be null. </remarks>
		ITreeViewItem Parent { get; set; }

		/// <summary>
		/// Count of items if the tree were to be flattened into a collection such as a list.
		/// </summary>
		/// <remarks>This item must be counted, as well as all the children items below this item.</remarks>
		int Count { get; }

		/// <summary>
		/// This method is invoked to render a specified item, the method should cover the entire provided width.
		/// </summary>
		/// <returns>The render.</returns>
		/// <param name="container">The parent tree view.</param>
		/// <param name="driver">The console driver to render.</param>
		/// <param name="selected">Describes whether the item being rendered is currently selected by the user.</param>
		/// <param name="level">Describes the indentation level of the item.</param>
		/// <param name="col">The column where the rendering will start</param>
		/// <param name="line">The line where the rendering will be done.</param>
		/// <param name="width">The width that must be filled out.</param>
		/// <remarks>
		///   The default color will be set before this method is invoked, and will be based on whether the item is selected or not.
		/// </remarks>
		void Render (TreeView container, ConsoleDriver driver, bool selected, int level, int col, int line, int width);

		/// <summary>
		/// Gets or sets whether this item is expanded in the <see cref="TreeView"/>.
		/// The children of expanded items must be displayed in the <see cref="TreeView"/>.
		/// </summary>
		bool IsExpanded { get; set; }

		/// <summary>
		/// Gets or sets whether this item is marked by the user.
		/// </summary>
		bool IsMarked { get; set; }

		/// <summary>
		/// Collects all items in this <see cref="ITreeViewItem"/> node into a flat collection.
		/// </summary>
		/// <remarks>This item is included in the collection.</remarks>
		/// <returns>A collection of items, including this item and all children below this item.</returns>
		IList<ITreeViewItem> ToList ();

		/// <summary>
		/// Gets the direct children of this item.
		/// </summary>
		IList<ITreeViewItem> Children { get; }
	}

	/// <summary>
	/// TreeView <see cref="View"/> renders a tree-like scrollable collection of items. Items can be activated
	/// </summary>
	/// /// <remarks>
	/// <para>
	///   The <see cref="TreeView"/> displays data in forms of trees and allows the user to scroll through the data.
	///   Items can aggregate other items in their Children collection and display them.
	///   Items in the can be activated firing an event (with the ENTER key or a mouse double-click). 
	///   If the <see cref="AllowsMarking"/> property is true, elements can be marked by the user.
	/// </para>
	/// <para>
	///   A default implementation if <see cref="ITreeViewItem"/> is provided: <see cref="TreeViewItem"/>.
	///   Alternatively, the way items are rendered can be customised by implementing ITreeViewItem.
	/// </para>
	/// <para>
	///   To change the contents of the <see cref="TreeView"/> set the <see cref="Root"/> property,
	///   or manipulate any of the children items by removing or adding items.
	///   Adding or removing a child item requires a call to the <see cref="View.SetNeedsDisplay()"/>.
	/// </para>
	/// <para>
	///   When <see cref="AllowsMarking"/> is set to true the rendering will prefix the rendered items with
	///   [x] or [ ] and bind the SPACE key to toggle the selection. To implement a different
	///   marking style set <see cref="AllowsMarking"/> to false and implement custom rendering.
	/// </para>
	/// </remarks>
	public class TreeView : View {
		ITreeViewItem top;
		ITreeViewItem selected;
		ITreeViewItem root;

		/// <summary>
		/// Gets or sets the top-level item of the <see cref="TreeView"/>.
		/// </summary>
		/// <value>The root.</value>
		public ITreeViewItem Root {
			get => root; 
			set {
				root = value;
				top = root;
				selected = root;
				lastSelectedItem = null;
				SetNeedsDisplay ();
			}
		}

		bool allowsMarking;
		/// <summary>
		/// Gets or sets whether this <see cref="TreeView"/> allows items to be marked.
		/// </summary>
		/// <value><c>true</c> if allows marking elements of the list; otherwise, <c>false</c>.
		/// </value>
		/// <remarks>
		/// If set to true, <see cref="TreeView"/> will render items marked items with "[x]", and unmarked items with "[ ]".
		/// SPACE key will toggle marking.
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
		/// Gets or sets the item that is displayed at the top of the <see cref="TreeView"/>.
		/// </summary>
		/// <value>The top item.</value>
		public ITreeViewItem TopItem {
			get => top;
			set {
				if (root == null)
					return;

				if (!IsVisible (value)) // An element which is not currently visible can not be the top item
					throw new ArgumentException ("value");
				top = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the currently selected item.
		/// </summary>
		/// <value>The selected item.</value>
		public ITreeViewItem SelectedItem {
			get => selected;
			set {
				if (root == null) {
					return;
				}

				selected = value;
				if (!IsVisible (value)) // An element which is not currently visible can not be selected
					throw new ArgumentException ("value");
				OnSelectedChanged ();
			}
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TreeView"/> with a starting root item 
		/// and relative positioning
		/// </summary>
		/// <param name="root">The root item.</param>
		public TreeView (ITreeViewItem root) : base ()
		{
			this.root = root;
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TreeView"/> with relative positioning. 
		/// A root must be supplied later, in order to display any items.
		/// </summary>
		public TreeView () : base ()
		{
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TreeView"/> with absolute positioning.
		/// </summary>
		/// <param name="rect">Frame for the treeview.</param>
		/// <param name="root">The root item.</param>
		public TreeView (Rect rect, ITreeViewItem root) : base (rect)
		{
			this.root = root;
			Initialize ();
		}

		void Initialize ()
		{
			Root = root;
			CanFocus = true;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Frame;

			var items = GetVisibleItems ();
			var indexOfSelected = items.IndexOf (selected);
			var indexOfTop = items.IndexOf (top); ;
			if (indexOfSelected < indexOfTop) {
				top = selected;
			} else if (indexOfSelected >= indexOfTop + f.Height) {
				var difference = indexOfSelected - (indexOfTop + f.Height);
				var newTop = items [indexOfTop + difference + 1];
				top = newTop;
			}

			var item = top;
			bool focused = HasFocus;
			int col = allowsMarking ? 4 : 0;


			for (int row = 0; row < f.Height; row++, item = GetNext(item)) {
				bool isSelected = items.Contains(item) && item == selected;

				var newcolor = focused ? (isSelected ? ColorScheme.Focus : ColorScheme.Normal) : (isSelected ? ColorScheme.HotNormal : ColorScheme.Normal);
				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}

				Move (0, row);
				if (root == null || item == null) {
					for (int c = 0; c < f.Width; c++)
						Driver.AddRune (' ');
				} else {
					var treeViewItem = item;
					if (allowsMarking) {
						Driver.AddStr (treeViewItem.IsMarked ? (AllowsMultipleSelection ? "[x] " : "(o)") : (AllowsMultipleSelection ? "[ ] " : "( )"));
					}
					treeViewItem.Render (this, Driver, isSelected, GetLevel(treeViewItem), col, row, f.Width - col);
				}
			}
		}

		/// <summary>
		/// In essence, this method returns a flattened collection of tree items, in which items which are hidden
		/// due to their parent not being expanded aren't included.
		/// </summary>
		private IList<ITreeViewItem> GetVisibleItems()
		{
			bool includeRoot = true;
			var list = new List<ITreeViewItem> ();
			if (Root == null)
				return list;
			if (includeRoot)
				list.Add (Root);
			if (Root.Children == null)
				return list;
			foreach (var childItem in Root.Children) {
				list.Add (childItem);
				if (childItem.IsExpanded) {
					list.AddRange (GetVisibleItemsRecursively (childItem));
				}
			}
			return list;
		}

		private IList<ITreeViewItem> GetVisibleItemsRecursively(ITreeViewItem item)
		{
			var list = new List<ITreeViewItem> ();
			if (item.Children == null)
				return list;
			foreach (var childItem in item.Children) {
				list.Add (childItem);
				if (childItem.IsExpanded) {
					list.AddRange (GetVisibleItemsRecursively (childItem));
				}
			}
			return list;
		}

		/// <summary>
		/// This event is raised when the selected item in the <see cref="TreeView"/> has changed.
		/// </summary>
		public event Action<TreeViewItemEventArgs> SelectedItemChanged;

		/// <summary>
		/// This event is raised when the user Double Clicks on an item or presses ENTER to open the selected item.
		/// </summary>
		public event Action<TreeViewItemEventArgs> OpenSelectedItem;

		/// <summary>
		/// This event is raised when the user presses TAB to expand the selected item.
		/// </summary>
		public event Action<TreeViewItemEventArgs> ExpandSelectedItem;

		/// <summary>
		/// This event is raised when the user presses TAB to collapse the selected item.
		/// </summary>
		public event Action<TreeViewItemEventArgs> CollapseSelectedItem;

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (root == null)
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

			case Key.End:
				return MoveEnd ();

			case Key.Home:
				return MoveHome ();

			case Key.Tab:
				return OnExpandOrCollapseSelectedItem ();
			}
			return base.ProcessKey (kb);
		}

		/// <summary>
		/// Prevents marking if it's not allowed and if it is, does not allow multiple selection.
		/// </summary>
		public virtual bool AllowsAll ()
		{
			if (!allowsMarking)
				return false;
			var items = Root.ToList ();
			if (!AllowsMultipleSelection) {
				for (int i = 0; i < items.Count(); i++) {
					var treeViewItem = items.ElementAt (i);
					if (treeViewItem.IsMarked && treeViewItem != selected) {
						treeViewItem.IsMarked = false;
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
				selected.IsMarked = !selected.IsMarked;
				SetNeedsDisplay ();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Moves the selected item to the next page.
		/// </summary>
		public virtual bool MovePageUp ()
		{
			var items = GetVisibleItems ();
			var visibleSelectedItemIndex = items.IndexOf(selected);
			int n = (visibleSelectedItemIndex - Frame.Height);
			if (n < 0)
				n = 0;
			if (n != visibleSelectedItemIndex) {
				selected = items [n];
				top = selected;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Moves the selected item to the previous page.
		/// </summary>
		/// <returns></returns>
		public virtual bool MovePageDown ()
		{
			var items = GetVisibleItems ();
			var visibleSelectedItemIndex = items.IndexOf(selected);
			var n = (visibleSelectedItemIndex + Frame.Height);
			if (n > items.Count)
				n = items.Count - 1;
			if (n != visibleSelectedItemIndex) {
				selected = items [n];
				if (items.Count >= Frame.Height)
					top = selected;
				else
					top = Root;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Moves the selected item to the next visible item.
		/// </summary>
		public virtual bool MoveDown ()
		{
			selected = GetNext (selected);
			if (selected == null)
				selected = GetVisibleItems ().Last ();

			OnSelectedChanged ();
			SetNeedsDisplay ();

			return true;
		}

		/// <summary>
		/// Moves the selected item to the previous visible item.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveUp ()
		{
			selected = GetPrevious (selected);
			if (selected == null)
				selected = Root;

			OnSelectedChanged ();
			SetNeedsDisplay ();

			return true;
		}

		private bool IsVisible (ITreeViewItem value)
		{
			ITreeViewItem parent = value.Parent;
			if (value == Root)
				return true;

			while (parent != Root) {
				if (!parent.IsExpanded)
					return false;
				parent = parent.Parent;
			}

			return true;
		}

		private ITreeViewItem GetNext (ITreeViewItem item)
		{
			if (item == null) return null;
			if (item.IsExpanded && item.Children?.Count > 0)
				return item.Children.First ();

			var itemTemp = item;
			var parentTemp = itemTemp.Parent;

			if (itemTemp == Root) {
				return itemTemp.Children.FirstOrDefault ();
			}

			while (parentTemp != Root) {
				var indexInParent = parentTemp.Children.IndexOf (itemTemp);
				if (indexInParent + 1 < parentTemp.Children.Count)
					return parentTemp.Children [indexInParent + 1];

				itemTemp = parentTemp;
				parentTemp = itemTemp.Parent;
			}

			{
				var indexInParent = parentTemp.Children.IndexOf (itemTemp);
				if (indexInParent + 1 < parentTemp.Children.Count)
					return parentTemp.Children [indexInParent + 1];
			}

			return null;
		}

		private ITreeViewItem GetPrevious(ITreeViewItem item)
		{
			if (item == null || item == Root) return null;
			var indexInParent = item.Parent.Children.IndexOf (item);
			if (indexInParent == 0)
				return item.Parent;

			var previousSibling = item.Parent.Children [indexInParent - 1];
			if (!previousSibling.IsExpanded)
				return previousSibling;

			var lastExpandedChild = previousSibling;
			while (lastExpandedChild.IsExpanded && lastExpandedChild.Children?.Count > 0) {
				lastExpandedChild = lastExpandedChild.Children.Last ();
			}
			return lastExpandedChild;
		}

		private int GetLevel(ITreeViewItem item)
		{
			int level = 0;
			ITreeViewItem tmpParent = item.Parent;
			while (tmpParent != null) {
				level++;
				tmpParent = tmpParent.Parent;
			}
			return level;
		}

		/// <summary>
		/// Moves the selected item to the last visible item.
		/// </summary>
		public virtual bool MoveEnd ()
		{
			var items = GetVisibleItems ();
			var last = items.Last ();
			if (selected != last) {
				selected = last;
				top = selected;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Moves the selected item index to the first visible item.
		/// </summary>
		public virtual bool MoveHome ()
		{
			if (selected != Root) {
				selected = Root;
				top = selected;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		ITreeViewItem lastSelectedItem;

		/// <summary>
		/// Invokes <see cref="SelectedItemChanged"/> event if it is defined.
		/// </summary>
		public virtual bool OnSelectedChanged ()
		{
			if (selected != lastSelectedItem) {
				var value = root?.Count > 0 ? selected : null;
				SelectedItemChanged?.Invoke (new TreeViewItemEventArgs (value));
				lastSelectedItem = selected;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Invokes the OnOpenSelectedItem event if it is defined.
		/// </summary>
		public virtual bool OnOpenSelectedItem ()
		{
			if (selected == null) return false;
			var value = selected;
			OpenSelectedItem?.Invoke (new TreeViewItemEventArgs (value));

			return true;
		}

		/// <summary>
		/// Switches the <see cref="ITreeViewItem.IsMarked"/> property of the <see cref="SelectedItem"/> in the <see cref="TreeView"/>.
		/// Invokes <see cref="CollapseSelectedItem"/> if the item becomes collapsed or the <see cref="ExpandSelectedItem"/>
		/// if it becomes expanded.
		/// </summary>
		public virtual bool OnExpandOrCollapseSelectedItem ()
		{
			if (selected == null) return false;
			if (selected.IsExpanded) {
				selected.IsExpanded = false;
				CollapseSelectedItem?.Invoke (new TreeViewItemEventArgs (selected));
			} else {
				selected.IsExpanded = true;
				ExpandSelectedItem?.Invoke (new TreeViewItemEventArgs (selected));
			}
			SetNeedsDisplay ();

			return true;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			if (lastSelectedItem == null) {
				OnSelectedChanged ();
				return true;
			}

			return base.OnEnter (view);
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			if (lastSelectedItem != null) {
				lastSelectedItem = null;
				return true;
			}

			return false;
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			var items = GetVisibleItems ();
			var index = items.IndexOf (selected);
			var topIndex = items.IndexOf (top);
			if (allowsMarking)
				Move (1, index - topIndex);
			else
				Move (0, index - topIndex);
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp)
				return false;

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}

			if (root == null) {
				return false;
			}

			if (me.Flags == MouseFlags.WheeledDown) {
				MoveDown ();
				return true;
			} else if (me.Flags == MouseFlags.WheeledUp) {
				MoveUp ();
				return true;
			}

			var visibleItems = GetVisibleItems ();
			var indexOfClickedItem = visibleItems.IndexOf (top) + me.Y;

			if (indexOfClickedItem >= visibleItems.Count) {
				return true;
			}

			selected = visibleItems[indexOfClickedItem];
			if (AllowsAll ()) {
				selected.IsMarked = !selected.IsMarked;
				SetNeedsDisplay ();
				return true;
			}
			OnSelectedChanged ();
			SetNeedsDisplay ();
			if (me.Flags == MouseFlags.Button1DoubleClicked) {
				OnOpenSelectedItem ();
			}

			return true;
		}
	}

	/// <summary>
	/// The default implementation of <see cref="ITreeViewItem"/>.
	/// A single level of indentation consists of two characters to accomodate the formats of items.
	/// </summary>
	/// <remarks>
	/// <para>
	/// An item without children will be displayed as follows: <c>|-item</c>
	/// </para>
	/// <para>
	/// An collapsed item with children will be displayed as follows: <c>|vitem</c>
	/// </para>
	/// <para>
	/// An expanded item with children will be displayed as follows: <c>|^item</c>
	/// </para>
	/// <para>
	/// In all cases the vertical bar will be placed under the first character of the parent item.
	/// </para>
	/// </remarks>
	public class TreeViewItem : ITreeViewItem {

		ITreeViewItem parent;
		string nodeName;
		IList<ITreeViewItem> children;
		bool isExpanded;
		bool isMarked;

		/// <summary>
		/// Initializes a new instance of <see cref="TreeViewItem"/> with a name and children
		/// </summary>
		/// <param name="nodeName">The name of this item to display in the TreeView.</param>
		/// <param name="children">The children of this item.</param>
		public TreeViewItem (string nodeName, IList<ITreeViewItem> children) : this (nodeName, null, children)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TreeViewItem"/> with a name and a parent.
		/// </summary>
		/// <param name="nodeName">The name of this item to display in the TreeView.</param>
		/// <param name="parent">The parent of this item.</param>
		public TreeViewItem (string nodeName, ITreeViewItem parent) : this(nodeName, parent, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TreeViewItem"/> with a name, parent and children.
		/// </summary>
		/// <param name="nodeName">The name of this item to display in the TreeView.</param>
		/// <param name="parent">The parent of this item.</param>
		/// <param name="children">The children of this item.</param>
		public TreeViewItem (string nodeName, ITreeViewItem parent, IList<ITreeViewItem> children) : this(nodeName)
		{
			this.parent = parent;
			this.children = children;

			SetChildrensParent ();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TreeViewItem"/> with a name.
		/// </summary>
		/// <param name="nodeName">The name of this item to display in the TreeView.</param>
		public TreeViewItem(string nodeName)
		{
			this.nodeName = nodeName;
		}

		private void SetChildrensParent()
		{
			if (children != null) {
				foreach (var child in children) {
					child.Parent = this;
				}
			}
		}

		/// <inheritdoc/>
		public int Count => children == null ? 1 : children.Count + 1; // root itself counts as a 1

		/// <inheritdoc/>
		public bool IsExpanded {
			get => isExpanded;
			set => isExpanded = value;
		}

		/// <inheritdoc/>
		public bool IsMarked {
			get => isMarked;
			set => isMarked = value;
		}

		/// <inheritdoc/>
		public IList<ITreeViewItem> Children => children;

		/// <inheritdoc/>
		public ITreeViewItem Parent {
			get => parent; 
			set => parent = value; 
		}

		/// <inheritdoc/>
		public object Data { get; set; }

		/// <inheritdoc/>
		public void Render (TreeView container, ConsoleDriver driver, bool selected, int level, int col, int line, int width)
		{
			container.Move (col, line);
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < level - 1; i++) {
				sb.Append ("  ");
			}
			if (level > 0) {
				sb.Append ('|');
				if (Children != null && Children.Count > 0)
					if (IsExpanded)
						sb.Append ("^");
					else
						sb.Append ('v');
				else
					sb.Append ('-');
			}

			sb.Append (nodeName);

			RenderUstr (driver, sb.ToString(), col, line, width);
		}

		void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width)
		{
			int byteLen = ustr.Length;
			int used = 0;
			for (int i = 0; i < byteLen;) {
				(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
				var count = System.Rune.ColumnWidth (rune);
				if (used + count > width)
					break;
				driver.AddRune (rune);
				used += count;
				i += size;
			}
			for (; used < width; used++) {
				driver.AddRune (' ');
			}
		}

		/// <inheritdoc/>
		public IList<ITreeViewItem> ToList ()
		{
			var list = new List<ITreeViewItem> ();
			list.Add (this);
			if (children == null)
				return list;

			foreach (ITreeViewItem item in children) {
				foreach (ITreeViewItem childItem in item.ToList()) {
					list.Add (childItem);
				}
			}
			return list;
		}
	}

	/// <summary>
	/// <see cref="EventArgs"/> for <see cref="TreeView"/> events.
	/// </summary>
	public class TreeViewItemEventArgs : EventArgs {
		/// <summary>
		/// The affected item.
		/// </summary>
		public ITreeViewItem Value { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="TreeViewItemEventArgs"/>
		/// </summary>
		/// <param name="value">The affected <see cref="TreeView"/> item.</param>
		public TreeViewItemEventArgs (ITreeViewItem value)
		{
			Value = value;
		}
	}
}
