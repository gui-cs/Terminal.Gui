using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.Views {

	public interface ITreeViewItem {

		ITreeViewItem Parent { get; set; }

		int Level { get; }

		int Count { get; }

		void Render (TreeView container, ConsoleDriver driver, bool selected, int level, int col, int line, int width);

		bool IsExpanded { get; }

		bool IsMarked { get; }

		void SetMark (bool value);

		IList ToList ();

		IList<ITreeViewItem> Children { get; }
	}

	public class TreeView : View {
		int top;
		int selected;
		ITreeViewItem root;

		public ITreeViewItem Root {
			get => root; 
			set {
				root = value;
				top = 0;
				selected = 0;
				lastSelectedItem = -1;
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

		public int TopItem {
			get => top;
			set {
				if (root == null)
					return;

				if (top < 0 || top >= root.Count)
					throw new ArgumentException ("value");
				top = value;
				SetNeedsDisplay ();
			}
		}

		public int SelectedItem {
			get => selected;
			set {
				if (root == null || root.Count == 0) {
					return;
				}
				if (value < 0 || value >= root.Count) {
					throw new ArgumentException ("value");
				}
				selected = value;
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
			if (selected < top) {
				top = selected;
			} else if (selected >= top + f.Height) {
				top = selected;
			}
			var item = top;
			bool focused = HasFocus;
			int col = allowsMarking ? 4 : 0;

			var items = Root.ToList ().Cast<ITreeViewItem>();

			for (int row = 0; row < f.Height; row++, item++) {
				var treeViewItem = items.ElementAt (item);
				var itemLevel = treeViewItem.Level;
				bool isSelected = item == selected;

				var newcolor = focused ? (isSelected ? ColorScheme.Focus : ColorScheme.Normal) : (isSelected ? ColorScheme.HotNormal : ColorScheme.Normal);
				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}

				Move (0, row);
				if (root == null || item >= root.Count) {
					for (int c = 0; c < f.Width; c++)
						Driver.AddRune (' ');
				} else {
					if (allowsMarking) {
						Driver.AddStr (treeViewItem.IsMarked ? (AllowsMultipleSelection ? "[x] " : "(o)") : (AllowsMultipleSelection ? "[ ] " : "( )"));
					}
					treeViewItem.Render (this, Driver, isSelected, itemLevel, col, row, f.Width - col);
				}
			}
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

			case Key.ControlD:
				return OnExpandSelectedItem ();

			case Key.ControlA:
				return OnCollapseSelectedItem ();

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
			var items = Root.ToList ().Cast<ITreeViewItem>();
			if (!AllowsMultipleSelection) {
				for (int i = 0; i < Root.Count; i++) {
					var treeViewItem = items.ElementAt (i);
					if (treeViewItem.IsMarked && i != selected) {
						treeViewItem.SetMark (false);
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
				var items = Root.ToList ().Cast<ITreeViewItem> ();
				var treeViewItem = items.ElementAt (SelectedItem);
				treeViewItem.SetMark (!treeViewItem.IsMarked);
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
		/// Moves the selected item index to the previous page.
		/// </summary>
		/// <returns></returns>
		public virtual bool MovePageDown ()
		{
			var n = (selected + Frame.Height);
			if (n > root.Count)
				n = root.Count - 1;
			if (n != selected) {
				selected = n;
				if (root.Count >= Frame.Height)
					top = selected;
				else
					top = 0;
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
			if (root.Count == 0) {
				// Do we set lastSelectedItem to zero here?
				return false; //Nothing for us to move to
			}
			if (selected >= root.Count) {
				// If for some reason we are currently outside of the
				// valid values range, we should select the bottommost valid value.
				// This can occur if the backing data source changes.
				selected = root.Count - 1;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected + 1 < root.Count) { //can move by down by one.
				selected++;

				if (selected >= top + Frame.Height)
					top++;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected == 0) {
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Moves the selected item index to the previous row.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveUp ()
		{
			if (root.Count == 0) {
				// Do we set lastSelectedItem to zero here?
				return false; //Nothing for us to move to
			}
			if (selected >= root.Count) {
				// If for some reason we are currently outside of the
				// valid values range, we should select the bottommost valid value.
				// This can occur if the backing data source changes.
				selected = root.Count - 1;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected > 0) {
				selected--;
				if (selected > root.Count) {
					selected = root.Count - 1;
				}
				if (selected < top)
					top = selected;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}
			return true;
		}

		/// <summary>
		/// Moves the selected item index to the last row.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveEnd ()
		{
			if (selected != root.Count - 1) {
				selected = root.Count - 1;
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
			if (selected != 0) {
				selected = 0;
				top = selected;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		int lastSelectedItem;

		public virtual bool OnSelectedChanged ()
		{
			if (selected != lastSelectedItem) {
				var value = root?.Count > 0 ? root.ToList () [selected] : null;
				SelectedItemChanged?.Invoke (new TreeViewItemEventArgs (selected, value));
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
			if (root.Count <= selected || selected < 0) return false;
			var value = root.ToList () [selected];
			OpenSelectedItem?.Invoke (new TreeViewItemEventArgs (selected, value));

			return true;
		}

		public virtual bool OnExpandSelectedItem ()
		{
			if (root.Count <= selected || selected < 0) return false;
			var value = root.ToList () [selected];
			ExpandSelectedItem?.Invoke (new TreeViewItemEventArgs (selected, value));

			return true;
		}

		public virtual bool OnCollapseSelectedItem()
		{
			if (root.Count <= selected || selected < 0) return false;
			var value = root.ToList () [selected];
			CollapseSelectedItem?.Invoke (new TreeViewItemEventArgs (selected, value));

			return true;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			if (lastSelectedItem == -1) {
				OnSelectedChanged ();
				return true;
			}

			return base.OnEnter (view);
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			if (lastSelectedItem > -1) {
				lastSelectedItem = -1;
				return true;
			}

			return false;
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			if (allowsMarking)
				Move (1, selected - top);
			else
				Move (0, selected - top);
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

			if (me.Y + top >= root.Count) {
				return true;
			}

			selected = top + me.Y;
			if (AllowsAll ()) {
				var items = root.ToList().Cast<ITreeViewItem>();
				var treeViewItem = items.ElementAt (SelectedItem);
				treeViewItem.SetMark (!treeViewItem.IsMarked);
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
		}

		public TreeViewItem(string nodeName)
		{
			this.nodeName = nodeName;
		}

		public int Count {
			get {
				int count = 1; // 1, because the root item counts towards Count.
				foreach (ITreeViewItem item in Children) {
					count++;
					count += item.Count;
				}
				return count;
			}
		}

		public bool IsExpanded => isExpanded;

		public bool IsMarked => isMarked;

		public IList<ITreeViewItem> Children => children;

		public ITreeViewItem Parent { 
			get => parent; 
			set => parent = value; 
		}

		public int Level {
			get {
				int level = 0;
				ITreeViewItem tmpParent = parent;
				while (tmpParent != null) {
					level++;
					tmpParent = tmpParent.Parent;
				}
				return level;
			}
		}


		public void Render (TreeView container, ConsoleDriver driver, bool selected, int level, int col, int line, int width)
		{
			container.Move (col, line);
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < level - 1; i++) {
				sb.Append ("  ");
			}
			sb.Append ('|');
			if (Children.Count > 0)
				if (IsExpanded)
					sb.Append ("^");
				else
					sb.Append ('v');
			else
				sb.Append ('-');

			sb.Append (nodeName);

			RenderUstr (driver, sb.ToString(), col, line, width);
		}

		void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width)
		{
			int byteLen = ustr.Length;
			int used = 0;
			for (int i = 0; i < byteLen;) {
				(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
				var count = Rune.ColumnWidth (rune);
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

		public void SetMark (bool value)
		{
			isMarked = value;
		}

		public IList ToList ()
		{
			var list = new List<ITreeViewItem> ();
			foreach (ITreeViewItem item in children) {
				list.Add (item);
				foreach (ITreeViewItem childItem in item.ToList()) {
					list.Add (childItem);
				}
			}
			return list;
		}
	}

	public class TreeViewItemEventArgs : EventArgs {
		public int Item { get; }
		public object Value { get; }

		public TreeViewItemEventArgs (int item, object value)
		{
			Item = item;
			Value = value;
		}
	}
}
