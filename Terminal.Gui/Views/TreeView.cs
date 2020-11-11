using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Terminal.Gui.Views {

	public interface ITreeViewItem {
		object Data { get; }

		ITreeViewItem Parent { get; set; }

		int Count { get; }

		void Render (TreeView container, ConsoleDriver driver, bool selected, int level, int col, int line, int width);

		bool IsExpanded { get; set; }

		bool IsMarked { get; set; }

		IList<ITreeViewItem> ToList ();

		IList<ITreeViewItem> Children { get; }
	}

	public class TreeView : View {
		ITreeViewItem top;
		ITreeViewItem selected;
		ITreeViewItem root;

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

		public TreeView (ITreeViewItem root) : base ()
		{
			this.root = root;
			Initialize ();
		}

		public TreeView () : base ()
		{
			Initialize ();
		}

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
		/// <param name="includeRoot"></param>
		/// <returns></returns>
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
		/// This event is raised when the selected item in the <see cref="ListView"/> has changed.
		/// </summary>
		public event Action<TreeViewItemEventArgs> SelectedItemChanged;

		/// <summary>
		/// This event is raised when the user Double Clicks on an item or presses ENTER to open the selected item.
		/// </summary>
		public event Action<TreeViewItemEventArgs> OpenSelectedItem;

		public event Action<TreeViewItemEventArgs> ExpandSelectedItem;

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
		/// Prevents marking if it's not allowed mark and if it's not allows multiple selection.
		/// </summary>
		/// <returns></returns>
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
		/// Moves the selected item index to the next page.
		/// </summary>
		/// <returns></returns>
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
		/// Moves the selected item index to the previous page.
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
		/// Moves the selected item index to the next row.
		/// </summary>
		/// <returns></returns>
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
		/// Moves the selected item index to the previous row.
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
		/// Moves the selected item index to the last row.
		/// </summary>
		/// <returns></returns>
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
		/// Moves the selected item index to the first row.
		/// </summary>
		/// <returns></returns>
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
		/// <returns></returns>
		public virtual bool OnOpenSelectedItem ()
		{
			if (selected == null) return false;
			var value = selected;
			OpenSelectedItem?.Invoke (new TreeViewItemEventArgs (value));

			return true;
		}

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

	public class TreeViewItem : ITreeViewItem {

		ITreeViewItem parent;
		string nodeName;
		IList<ITreeViewItem> children;
		bool isExpanded;
		bool isMarked;

		public TreeViewItem (string nodeName, IList<ITreeViewItem> children) : this (nodeName, null, children)
		{
		}

		public TreeViewItem(string nodeName, ITreeViewItem parent) : this(nodeName, parent, null)
		{
		}

		public TreeViewItem(string nodeName, ITreeViewItem parent, IList<ITreeViewItem> children) : this(nodeName)
		{
			this.parent = parent;
			this.children = children;

			SetChildrensParent ();
		}

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

		public int Count => children == null ? 1 : children.Count + 1; // root itself counts as a 1

		public bool IsExpanded { 
			get => isExpanded;
			set => isExpanded = value;
		}

		public bool IsMarked { 
			get => isMarked;
			set => isMarked = value;
		}

		public IList<ITreeViewItem> Children => children;

		public ITreeViewItem Parent { 
			get => parent; 
			set => parent = value; 
		}

		public object Data { get; set; }

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

	public class TreeViewItemEventArgs : EventArgs {
		public ITreeViewItem Value { get; }

		public TreeViewItemEventArgs (ITreeViewItem value)
		{
			Value = value;
		}
	}
}
