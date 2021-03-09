// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls 
// by phillip.piper@gmail.com).  Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NStack;

namespace Terminal.Gui {

	/// <summary>
	/// Interface for all non generic members of <see cref="TreeView{T}"/>
	/// 
	/// <a href="https://migueldeicaza.github.io/gui.cs/articles/treeview.html">See TreeView Deep Dive for more information</a>.
	/// </summary>
	public interface ITreeView {
		/// <summary>
		/// Contains options for changing how the tree is rendered
		/// </summary>
		TreeStyle Style { get; set; }

		/// <summary>
		/// Removes all objects from the tree and clears selection
		/// </summary>
		void ClearObjects ();

		/// <summary>
		/// Sets a flag indicating this view needs to be redisplayed because its state has changed.
		/// </summary>
		void SetNeedsDisplay ();
	}

	/// <summary>
	/// Convenience implementation of generic <see cref="TreeView{T}"/> for any tree were all nodes
	/// implement <see cref="ITreeNode"/>.
	/// 
	/// <a href="https://migueldeicaza.github.io/gui.cs/articles/treeview.html">See TreeView Deep Dive for more information</a>.
	/// </summary>
	public class TreeView : TreeView<ITreeNode> {

		/// <summary>
		/// Creates a new instance of the tree control with absolute positioning and initialises
		/// <see cref="TreeBuilder{T}"/> with default <see cref="ITreeNode"/> based builder
		/// </summary>
		public TreeView ()
		{
			TreeBuilder = new TreeNodeBuilder ();
			AspectGetter = o => o == null ? "Null" : (o.Text ?? o?.ToString () ?? "Unamed Node");
		}
	}

	/// <summary>
	/// Hierarchical tree view with expandable branches.  Branch objects are dynamically determined
	/// when expanded using a user defined <see cref="ITreeBuilder{T}"/>
	/// 
	/// <a href="https://migueldeicaza.github.io/gui.cs/articles/treeview.html">See TreeView Deep Dive for more information</a>.
	/// </summary>
	public class TreeView<T> : View, ITreeView where T : class {
		private int scrollOffsetVertical;
		private int scrollOffsetHorizontal;

		/// <summary>
		/// Determines how sub branches of the tree are dynamically built at runtime as the user
		/// expands root nodes
		/// </summary>
		/// <value></value>
		public ITreeBuilder<T> TreeBuilder { get; set; }

		/// <summary>
		/// private variable for <see cref="SelectedObject"/>
		/// </summary>
		T selectedObject;


		/// <summary>
		/// Contains options for changing how the tree is rendered
		/// </summary>
		public TreeStyle Style { get; set; } = new TreeStyle ();


		/// <summary>
		/// True to allow multiple objects to be selected at once
		/// </summary>
		/// <value></value>
		public bool MultiSelect { get; set; } = true;


		/// <summary>
		/// True makes a letter key press navigate to the next visible branch that begins with
		/// that letter/digit
		/// </summary>
		/// <value></value>
		public bool AllowLetterBasedNavigation { get; set; } = true;

		/// <summary>
		/// The currently selected object in the tree.  When <see cref="MultiSelect"/> is true this
		/// is the object at which the cursor is at
		/// </summary>
		public T SelectedObject {
			get => selectedObject;
			set {
				var oldValue = selectedObject;
				selectedObject = value;

				if (!ReferenceEquals (oldValue, value)) {
					OnSelectionChanged (new SelectionChangedEventArgs<T> (this, oldValue, value));
				}
			}
		}


		/// <summary>
		/// This event is raised when an object is activated e.g. by double clicking or 
		/// pressing <see cref="ObjectActivationKey"/>
		/// </summary>
		public event Action<ObjectActivatedEventArgs<T>> ObjectActivated;

		/// <summary>
		/// Key which when pressed triggers <see cref="TreeView{T}.ObjectActivated"/>.
		/// Defaults to Enter
		/// </summary>
		public Key ObjectActivationKey { get; set; } = Key.Enter;

		/// <summary>
		/// Mouse event to trigger <see cref="TreeView{T}.ObjectActivated"/>.
		/// Defaults to double click (<see cref="MouseFlags.Button1DoubleClicked"/>).
		/// Set to null to disable this feature.
		/// </summary>
		/// <value></value>
		public MouseFlags? ObjectActivationButton { get; set; } = MouseFlags.Button1DoubleClicked;

		/// <summary>
		/// Secondary selected regions of tree when <see cref="MultiSelect"/> is true
		/// </summary>
		private Stack<TreeSelection<T>> multiSelectedRegions = new Stack<TreeSelection<T>> ();

		/// <summary>
		/// Cached result of <see cref="BuildLineMap"/>
		/// </summary>
		private IReadOnlyCollection<Branch<T>> cachedLineMap;


		/// <summary>
		/// Error message to display when the control is not properly initialized at draw time 
		/// (nodes added but no tree builder set)
		/// </summary>
		public static ustring NoBuilderError = "ERROR: TreeBuilder Not Set";

		/// <summary>
		/// Called when the <see cref="SelectedObject"/> changes
		/// </summary>
		public event EventHandler<SelectionChangedEventArgs<T>> SelectionChanged;

		/// <summary>
		/// The root objects in the tree, note that this collection is of root objects only
		/// </summary>
		public IEnumerable<T> Objects { get => roots.Keys; }

		/// <summary>
		/// Map of root objects to the branches under them.  All objects have 
		/// a <see cref="Branch{T}"/> even if that branch has no children
		/// </summary>
		internal Dictionary<T, Branch<T>> roots { get; set; } = new Dictionary<T, Branch<T>> ();

		/// <summary>
		/// The amount of tree view that has been scrolled off the top of the screen (by the user 
		/// scrolling down)
		/// </summary>
		/// <remarks>Setting a value of less than 0 will result in a offset of 0.  To see changes 
		/// in the UI call <see cref="View.SetNeedsDisplay()"/></remarks>
		public int ScrollOffsetVertical {
			get => scrollOffsetVertical;
			set {
				scrollOffsetVertical = Math.Max (0, value);
			}
		}


		/// <summary>
		/// The amount of tree view that has been scrolled to the right (horizontally)
		/// </summary>
		/// <remarks>Setting a value of less than 0 will result in a offset of 0.  To see changes 
		/// in the UI call <see cref="View.SetNeedsDisplay()"/></remarks>
		public int ScrollOffsetHorizontal {
			get => scrollOffsetHorizontal;
			set {
				scrollOffsetHorizontal = Math.Max (0, value);
			}
		}

		/// <summary>
		/// The current number of rows in the tree (ignoring the controls bounds)
		/// </summary>
		public int ContentHeight => BuildLineMap ().Count ();

		/// <summary>
		/// Returns the string representation of model objects hosted in the tree.  Default 
		/// implementation is to call <see cref="object.ToString"/>
		/// </summary>
		/// <value></value>
		public AspectGetterDelegate<T> AspectGetter { get; set; } = (o) => o.ToString () ?? "";

		/// <summary>
		/// Creates a new tree view with absolute positioning.  
		/// Use <see cref="AddObjects(IEnumerable{T})"/> to set set root objects for the tree.
		/// Children will not be rendered until you set <see cref="TreeBuilder"/>
		/// </summary>
		public TreeView () : base ()
		{
			CanFocus = true;
		}

		/// <summary>
		/// Initialises <see cref="TreeBuilder"/>.Creates a new tree view with absolute 
		/// positioning.  Use <see cref="AddObjects(IEnumerable{T})"/> to set set root 
		/// objects for the tree.
		/// </summary>
		public TreeView (ITreeBuilder<T> builder) : this ()
		{
			TreeBuilder = builder;
		}

		/// <summary>
		/// Adds a new root level object unless it is already a root of the tree
		/// </summary>
		/// <param name="o"></param>
		public void AddObject (T o)
		{
			if (!roots.ContainsKey (o)) {
				roots.Add (o, new Branch<T> (this, null, o));
				InvalidateLineMap ();
				SetNeedsDisplay ();
			}
		}


		/// <summary>
		/// Removes all objects from the tree and clears <see cref="SelectedObject"/>
		/// </summary>
		public void ClearObjects ()
		{
			SelectedObject = default (T);
			multiSelectedRegions.Clear ();
			roots = new Dictionary<T, Branch<T>> ();
			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Removes the given root object from the tree
		/// </summary>
		/// <remarks>If <paramref name="o"/> is the currently <see cref="SelectedObject"/> then the
		/// selection is cleared</remarks>
		/// <param name="o"></param>
		public void Remove (T o)
		{
			if (roots.ContainsKey (o)) {
				roots.Remove (o);
				InvalidateLineMap ();
				SetNeedsDisplay ();

				if (Equals (SelectedObject, o)) {
					SelectedObject = default (T);
				}
			}
		}

		/// <summary>
		/// Adds many new root level objects.  Objects that are already root objects are ignored
		/// </summary>
		/// <param name="collection">Objects to add as new root level objects</param>
		public void AddObjects (IEnumerable<T> collection)
		{
			bool objectsAdded = false;

			foreach (var o in collection) {
				if (!roots.ContainsKey (o)) {
					roots.Add (o, new Branch<T> (this, null, o));
					objectsAdded = true;
				}
			}

			if (objectsAdded) {
				InvalidateLineMap ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Refreshes the state of the object <paramref name="o"/> in the tree.  This will 
		/// recompute children, string representation etc
		/// </summary>
		/// <remarks>This has no effect if the object is not exposed in the tree.</remarks>
		/// <param name="o"></param>
		/// <param name="startAtTop">True to also refresh all ancestors of the objects branch 
		/// (starting with the root).  False to refresh only the passed node</param>
		public void RefreshObject (T o, bool startAtTop = false)
		{
			var branch = ObjectToBranch (o);
			if (branch != null) {
				branch.Refresh (startAtTop);
				InvalidateLineMap ();
				SetNeedsDisplay ();
			}

		}

		/// <summary>
		/// Rebuilds the tree structure for all exposed objects starting with the root objects.
		/// Call this method when you know there are changes to the tree but don't know which 
		/// objects have changed (otherwise use <see cref="RefreshObject(T, bool)"/>)
		/// </summary>
		public void RebuildTree ()
		{
			foreach (var branch in roots.Values) {
				branch.Rebuild ();
			}

			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Returns the currently expanded children of the passed object.  Returns an empty
		/// collection if the branch is not exposed or not expanded
		/// </summary>
		/// <param name="o">An object in the tree</param>
		/// <returns></returns>
		public IEnumerable<T> GetChildren (T o)
		{
			var branch = ObjectToBranch (o);

			if (branch == null || !branch.IsExpanded) {
				return new T [0];
			}

			return branch.ChildBranches?.Values?.Select (b => b.Model)?.ToArray () ?? new T [0];
		}
		/// <summary>
		/// Returns the parent object of <paramref name="o"/> in the tree.  Returns null if 
		/// the object is not exposed in the tree
		/// </summary>
		/// <param name="o">An object in the tree</param>
		/// <returns></returns>
		public T GetParent (T o)
		{
			return ObjectToBranch (o)?.Parent?.Model;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (roots == null) {
				return;
			}

			if (TreeBuilder == null) {
				Move (0, 0);
				Driver.AddStr (NoBuilderError);
				return;
			}

			var map = BuildLineMap ();

			for (int line = 0; line < bounds.Height; line++) {

				var idxToRender = ScrollOffsetVertical + line;

				// Is there part of the tree view to render?
				if (idxToRender < map.Count) {
					// Render the line
					map.ElementAt (idxToRender).Draw (Driver, ColorScheme, line, bounds.Width);
				} else {

					// Else clear the line to prevent stale symbols due to scrolling etc
					Move (0, line);
					Driver.SetAttribute (ColorScheme.Normal);
					Driver.AddStr (new string (' ', bounds.Width));
				}

			}
		}

		/// <summary>
		/// Returns the index of the object <paramref name="o"/> if it is currently exposed (it's 
		/// parent(s) have been expanded).  This can be used with <see cref="ScrollOffsetVertical"/>
		/// and <see cref="View.SetNeedsDisplay()"/> to scroll to a specific object
		/// </summary>
		/// <remarks>Uses the Equals method and returns the first index at which the object is found
		///  or -1 if it is not found</remarks>
		/// <param name="o">An object that appears in your tree and is currently exposed</param>
		/// <returns>The index the object was found at or -1 if it is not currently revealed or
		/// not in the tree at all</returns>
		public int GetScrollOffsetOf (T o)
		{
			var map = BuildLineMap ();
			for (int i = 0; i < map.Count; i++) {
				if (map.ElementAt (i).Model.Equals (o)) {
					return i;
				}
			}

			//object not found
			return -1;
		}

		/// <summary>
		/// Returns the maximum width line in the tree including prefix and expansion symbols
		/// </summary>
		/// <param name="visible">True to consider only rows currently visible (based on window
		///  bounds and <see cref="ScrollOffsetVertical"/>.  False to calculate the width of 
		/// every exposed branch in the tree</param>
		/// <returns></returns>
		public int GetContentWidth (bool visible)
		{
			var map = BuildLineMap ();

			if (map.Count == 0) {
				return 0;
			}

			if (visible) {

				//Somehow we managed to scroll off the end of the control
				if (ScrollOffsetVertical >= map.Count) {
					return 0;
				}

				// If control has no height to it then there is no visible area for content
				if (Bounds.Height == 0) {
					return 0;
				}

				return map.Skip (ScrollOffsetVertical).Take (Bounds.Height).Max (b => b.GetWidth (Driver));
			} else {

				return map.Max (b => b.GetWidth (Driver));
			}
		}

		/// <summary>
		/// Calculates all currently visible/expanded branches (including leafs) and outputs them 
		/// by index from the top of the screen
		/// </summary>
		/// <remarks>Index 0 of the returned array is the first item that should be visible in the
		/// top of the control, index 1 is the next etc.</remarks>
		/// <returns></returns>
		private IReadOnlyCollection<Branch<T>> BuildLineMap ()
		{
			if (cachedLineMap != null) {
				return cachedLineMap;
			}

			List<Branch<T>> toReturn = new List<Branch<T>> ();

			foreach (var root in roots.Values) {
				toReturn.AddRange (AddToLineMap (root));
			}

			return cachedLineMap = new ReadOnlyCollection<Branch<T>> (toReturn);
		}

		private IEnumerable<Branch<T>> AddToLineMap (Branch<T> currentBranch)
		{
			yield return currentBranch;

			if (currentBranch.IsExpanded) {

				foreach (var subBranch in currentBranch.ChildBranches.Values) {
					foreach (var sub in AddToLineMap (subBranch)) {
						yield return sub;
					}
				}
			}
		}

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (keyEvent.Key == ObjectActivationKey) {
				var o = SelectedObject;

				if (o != null) {
					OnObjectActivated (new ObjectActivatedEventArgs<T> (this, o));

					PositionCursor ();
					return true;
				}
			}

			if (keyEvent.KeyValue > 0 && keyEvent.KeyValue < 0xFFFF) {

				var character = (char)keyEvent.KeyValue;

				// if it is a single character pressed without any control keys
				if (char.IsLetterOrDigit (character) && AllowLetterBasedNavigation && !keyEvent.IsShift && !keyEvent.IsAlt && !keyEvent.IsCtrl) {
					// search for next branch that begins with that letter
					var characterAsStr = character.ToString ();
					AdjustSelectionToNext (b => AspectGetter (b.Model).StartsWith (characterAsStr, StringComparison.CurrentCultureIgnoreCase));

					PositionCursor ();
					return true;
				}

			}


			switch (keyEvent.Key) {

			case Key.CursorRight:
				Expand (SelectedObject);
				break;
			case Key.CursorRight | Key.CtrlMask:
				ExpandAll (SelectedObject);
				break;
			case Key.CursorLeft:
			case Key.CursorLeft | Key.CtrlMask:
				CursorLeft (keyEvent.Key.HasFlag (Key.CtrlMask));
				break;

			case Key.CursorUp:
			case Key.CursorUp | Key.ShiftMask:
				AdjustSelection (-1, keyEvent.Key.HasFlag (Key.ShiftMask));
				break;
			case Key.CursorDown:
			case Key.CursorDown | Key.ShiftMask:
				AdjustSelection (1, keyEvent.Key.HasFlag (Key.ShiftMask));
				break;
			case Key.CursorUp | Key.CtrlMask:
				AdjustSelectionToBranchStart ();
				break;
			case Key.CursorDown | Key.CtrlMask:
				AdjustSelectionToBranchEnd ();
				break;
			case Key.PageUp:
			case Key.PageUp | Key.ShiftMask:
				AdjustSelection (-Bounds.Height, keyEvent.Key.HasFlag (Key.ShiftMask));
				break;

			case Key.PageDown:
			case Key.PageDown | Key.ShiftMask:
				AdjustSelection (Bounds.Height, keyEvent.Key.HasFlag (Key.ShiftMask));
				break;
			case Key.A | Key.CtrlMask:
				SelectAll ();
				break;
			case Key.Home:
				GoToFirst ();
				break;
			case Key.End:
				GoToEnd ();
				break;

			default:
				// we don't care about this keystroke
				return false;
			}

			PositionCursor ();
			return true;
		}

		/// <summary>
		/// Raises the <see cref="ObjectActivated"/> event
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnObjectActivated (ObjectActivatedEventArgs<T> e)
		{
			ObjectActivated?.Invoke (e);
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			// If it is not an event we care about
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) &&
				!me.Flags.HasFlag (ObjectActivationButton ?? MouseFlags.Button1DoubleClicked) &&
				!me.Flags.HasFlag (MouseFlags.WheeledDown) &&
				!me.Flags.HasFlag (MouseFlags.WheeledUp) &&
				!me.Flags.HasFlag (MouseFlags.WheeledRight) &&
				!me.Flags.HasFlag (MouseFlags.WheeledLeft)) {

				// do nothing
				return false;
			}

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}


			if (me.Flags == MouseFlags.WheeledDown) {

				ScrollOffsetVertical++;
				SetNeedsDisplay ();

				return true;
			} else if (me.Flags == MouseFlags.WheeledUp) {
				ScrollOffsetVertical--;
				SetNeedsDisplay ();

				return true;
			}

			if (me.Flags == MouseFlags.WheeledRight) {

				ScrollOffsetHorizontal++;
				SetNeedsDisplay ();

				return true;
			} else if (me.Flags == MouseFlags.WheeledLeft) {
				ScrollOffsetHorizontal--;
				SetNeedsDisplay ();

				return true;
			}

			if (me.Flags.HasFlag (MouseFlags.Button1Clicked)) {

				// The line they clicked on a branch
				var clickedBranch = HitTest (me.Y);

				if (clickedBranch == null) {
					return false;
				}

				bool isExpandToggleAttempt = clickedBranch.IsHitOnExpandableSymbol (Driver, me.X);

				// If we are already selected (double click)
				if (Equals (SelectedObject, clickedBranch.Model)) {
					isExpandToggleAttempt = true;
				}

				// if they clicked on the +/- expansion symbol
				if (isExpandToggleAttempt) {

					if (clickedBranch.IsExpanded) {
						clickedBranch.Collapse ();
						InvalidateLineMap ();
					} else
					if (clickedBranch.CanExpand ()) {
						clickedBranch.Expand ();
						InvalidateLineMap ();
					} else {
						SelectedObject = clickedBranch.Model; // It is a leaf node
						multiSelectedRegions.Clear ();
					}
				} else {

					// It is a first click somewhere in the current line that doesn't look like an expansion/collapse attempt
					SelectedObject = clickedBranch.Model;
					multiSelectedRegions.Clear ();
				}

				SetNeedsDisplay ();
				return true;
			}

			// If it is activation via mouse (e.g. double click)
			if (ObjectActivationButton.HasValue && me.Flags.HasFlag (ObjectActivationButton.Value)) {
				// The line they clicked on a branch
				var clickedBranch = HitTest (me.Y);

				if (clickedBranch == null) {
					return false;
				}

				// Double click changes the selection to the clicked node as well as triggering
				// activation otherwise it feels wierd
				SelectedObject = clickedBranch.Model;
				SetNeedsDisplay ();

				// trigger activation event				
				OnObjectActivated (new ObjectActivatedEventArgs<T> (this, clickedBranch.Model));

				// mouse event is handled.
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns the branch at the given <paramref name="y"/> client
		/// coordinate e.g. following a click event
		/// </summary>
		/// <param name="y">Client Y position in the controls bounds</param>
		/// <returns>The clicked branch or null if outside of tree region</returns>
		private Branch<T> HitTest (int y)
		{
			var map = BuildLineMap ();

			var idx = y + ScrollOffsetVertical;

			// click is outside any visible nodes
			if (idx < 0 || idx >= map.Count) {
				return null;
			}

			// The line they clicked on
			return map.ElementAt (idx);
		}

		/// <summary>
		/// Positions the cursor at the start of the selected objects line (if visible)
		/// </summary>
		public override void PositionCursor ()
		{
			if (CanFocus && HasFocus && Visible && SelectedObject != null) {

				var map = BuildLineMap ();
				var idx = map.IndexOf(b => b.Model.Equals (SelectedObject));

				// if currently selected line is visible
				if (idx - ScrollOffsetVertical >= 0 && idx - ScrollOffsetVertical < Bounds.Height) {
					Move (0, idx - ScrollOffsetVertical);
				} else {
					base.PositionCursor ();
				}

			} else {
				base.PositionCursor ();
			}
		}


		/// <summary>
		/// Determines systems behaviour when the left arrow key is pressed.  Default behaviour is
		/// to collapse the current tree node if possible otherwise changes selection to current 
		/// branches parent
		/// </summary>
		protected virtual void CursorLeft (bool ctrl)
		{
			if (IsExpanded (SelectedObject)) {

				if (ctrl) {
					CollapseAll (SelectedObject);
				} else {
					Collapse (SelectedObject);
				}
			} else {
				var parent = GetParent (SelectedObject);

				if (parent != null) {
					SelectedObject = parent;
					AdjustSelection (0);
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to the first root object and resets 
		/// the <see cref="ScrollOffsetVertical"/> to 0
		/// </summary>
		public void GoToFirst ()
		{
			ScrollOffsetVertical = 0;
			SelectedObject = roots.Keys.FirstOrDefault ();

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to the last object in the tree and scrolls so
		/// that it is visible
		/// </summary>
		public void GoToEnd ()
		{
			var map = BuildLineMap ();
			ScrollOffsetVertical = Math.Max (0, map.Count - Bounds.Height + 1);
			SelectedObject = map.Last ().Model;

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to <paramref name="toSelect"/> and scrolls to ensure
		/// it is visible.  Has no effect if <paramref name="toSelect"/> is not exposed in the tree (e.g. 
		/// its parents are collapsed)
		/// </summary>
		/// <param name="toSelect"></param>
		public void GoTo (T toSelect)
		{
			if (ObjectToBranch (toSelect) == null) {
				return;
			}

			SelectedObject = toSelect;
			EnsureVisible (toSelect);
			SetNeedsDisplay ();
		}

		/// <summary>
		/// The number of screen lines to move the currently selected object by.  Supports negative 
		/// <paramref name="offset"/>.  Each branch occupies 1 line on screen
		/// </summary>
		/// <remarks>If nothing is currently selected or the selected object is no longer in the tree
		/// then the first object in the tree is selected instead</remarks>
		/// <param name="offset">Positive to move the selection down the screen, negative to move it up</param>
		/// <param name="expandSelection">True to expand the selection (assuming 
		/// <see cref="MultiSelect"/> is enabled).  False to replace</param>
		public void AdjustSelection (int offset, bool expandSelection = false)
		{
			// if it is not a shift click or we don't allow multi select
			if (!expandSelection || !MultiSelect) {
				multiSelectedRegions.Clear ();
			}

			if (SelectedObject == null) {
				SelectedObject = roots.Keys.FirstOrDefault ();
			} else {
				var map = BuildLineMap ();

				var idx = map.IndexOf(b => b.Model.Equals (SelectedObject));

				if (idx == -1) {

					// The current selection has disapeared!
					SelectedObject = roots.Keys.FirstOrDefault ();
				} else {
					var newIdx = Math.Min (Math.Max (0, idx + offset), map.Count - 1);

					var newBranch = map.ElementAt(newIdx);

					// If it is a multi selection
					if (expandSelection && MultiSelect) {
						if (multiSelectedRegions.Any ()) {
							// expand the existing head selection
							var head = multiSelectedRegions.Pop ();
							multiSelectedRegions.Push (new TreeSelection<T> (head.Origin, newIdx, map));
						} else {
							// or start a new multi selection region
							multiSelectedRegions.Push (new TreeSelection<T> (map.ElementAt(idx), newIdx, map));
						}
					}

					SelectedObject = newBranch.Model;

					EnsureVisible (SelectedObject);
				}

			}

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Moves the selection to the first child in the currently selected level
		/// </summary>
		public void AdjustSelectionToBranchStart ()
		{
			var o = SelectedObject;
			if (o == null) {
				return;
			}

			var map = BuildLineMap ();

			int currentIdx = map.IndexOf(b => Equals (b.Model, o));

			if (currentIdx == -1) {
				return;
			}

			var currentBranch = map.ElementAt(currentIdx);
			var next = currentBranch;

			for (; currentIdx >= 0; currentIdx--) {
				//if it is the beginning of the current depth of branch
				if (currentBranch.Depth != next.Depth) {

					SelectedObject = currentBranch.Model;
					EnsureVisible (currentBranch.Model);
					SetNeedsDisplay ();
					return;
				}

				// look at next branch up for consideration
				currentBranch = next;
				next = map.ElementAt(currentIdx);
			}

			// We ran all the way to top of tree
			GoToFirst ();
		}

		/// <summary>
		/// Moves the selection to the last child in the currently selected level
		/// </summary>
		public void AdjustSelectionToBranchEnd ()
		{
			var o = SelectedObject;
			if (o == null) {
				return;
			}

			var map = BuildLineMap ();

			int currentIdx = map.IndexOf(b => Equals (b.Model, o));

			if (currentIdx == -1) {
				return;
			}

			var currentBranch = map.ElementAt(currentIdx);
			var next = currentBranch;

			for (; currentIdx < map.Count; currentIdx++) {
				//if it is the end of the current depth of branch
				if (currentBranch.Depth != next.Depth) {

					SelectedObject = currentBranch.Model;
					EnsureVisible (currentBranch.Model);
					SetNeedsDisplay ();
					return;
				}

				// look at next branch for consideration
				currentBranch = next;
				next = map.ElementAt(currentIdx);
			}

			GoToEnd ();
		}


		/// <summary>
		/// Sets the selection to the next branch that matches the <paramref name="predicate"/>
		/// </summary>
		/// <param name="predicate"></param>
		private void AdjustSelectionToNext (Func<Branch<T>, bool> predicate)
		{
			var map = BuildLineMap ();

			// empty map means we can't select anything anyway
			if (map.Count == 0) {
				return;
			}

			// Start searching from the first element in the map
			var idxStart = 0;

			// or the current selected branch
			if (SelectedObject != null) {
				idxStart = map.IndexOf(b => Equals (b.Model, SelectedObject));
			}

			// if currently selected object mysteriously vanished, search from beginning
			if (idxStart == -1) {
				idxStart = 0;
			}

			// loop around all indexes and back to first index
			for (int idxCur = (idxStart + 1) % map.Count; idxCur != idxStart; idxCur = (idxCur + 1) % map.Count) {
				if (predicate (map.ElementAt(idxCur))) {
					SelectedObject = map.ElementAt(idxCur).Model;
					EnsureVisible (map.ElementAt(idxCur).Model);
					SetNeedsDisplay ();
					return;
				}
			}
		}

		/// <summary>
		/// Adjusts the <see cref="ScrollOffsetVertical"/> to ensure the given
		/// <paramref name="model"/> is visible.  Has no effect if already visible
		/// </summary>
		public void EnsureVisible (T model)
		{
			var map = BuildLineMap ();

			var idx = map.IndexOf(b => Equals (b.Model, model));

			if (idx == -1) {
				return;
			}


			/*this -1 allows for possible horizontal scroll bar in the last row of the control*/
			int leaveSpace = Style.LeaveLastRow ? 1 : 0;

			if (idx < ScrollOffsetVertical) {
				//if user has scrolled up too far to see their selection
				ScrollOffsetVertical = idx;
			} else if (idx >= ScrollOffsetVertical + Bounds.Height - leaveSpace) {

				//if user has scrolled off bottom of visible tree
				ScrollOffsetVertical = Math.Max (0, (idx + 1) - (Bounds.Height - leaveSpace));
			}
		}

		/// <summary>
		/// Expands the supplied object if it is contained in the tree (either as a root object or 
		/// as an exposed branch object)
		/// </summary>
		/// <param name="toExpand">The object to expand</param>
		public void Expand (T toExpand)
		{
			if (toExpand == null) {
				return;
			}

			ObjectToBranch (toExpand)?.Expand ();
			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Expands the supplied object and all child objects
		/// </summary>
		/// <param name="toExpand">The object to expand</param>
		public void ExpandAll (T toExpand)
		{
			if (toExpand == null) {
				return;
			}

			ObjectToBranch (toExpand)?.ExpandAll ();
			InvalidateLineMap ();
			SetNeedsDisplay ();
		}
		/// <summary>
		/// Fully expands all nodes in the tree, if the tree is very big and built dynamically this
		/// may take a while (e.g. for file system)
		/// </summary>
		public void ExpandAll ()
		{
			foreach (var item in roots) {
				item.Value.ExpandAll ();
			}

			InvalidateLineMap ();
			SetNeedsDisplay ();
		}
		/// <summary>
		/// Returns true if the given object <paramref name="o"/> is exposed in the tree and can be
		/// expanded otherwise false
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public bool CanExpand (T o)
		{
			return ObjectToBranch (o)?.CanExpand () ?? false;
		}

		/// <summary>
		/// Returns true if the given object <paramref name="o"/> is exposed in the tree and 
		/// expanded otherwise false
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public bool IsExpanded (T o)
		{
			return ObjectToBranch (o)?.IsExpanded ?? false;
		}

		/// <summary>
		/// Collapses the supplied object if it is currently expanded 
		/// </summary>
		/// <param name="toCollapse">The object to collapse</param>
		public void Collapse (T toCollapse)
		{
			CollapseImpl (toCollapse, false);
		}

		/// <summary>
		/// Collapses the supplied object if it is currently expanded.  Also collapses all children
		/// branches (this will only become apparent when/if the user expands it again)
		/// </summary>
		/// <param name="toCollapse">The object to collapse</param>
		public void CollapseAll (T toCollapse)
		{
			CollapseImpl (toCollapse, true);
		}

		/// <summary>
		/// Collapses all root nodes in the tree
		/// </summary>
		public void CollapseAll ()
		{
			foreach (var item in roots) {
				item.Value.Collapse ();
			}

			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Implementation of <see cref="Collapse(T)"/> and <see cref="CollapseAll(T)"/>.  Performs
		/// operation and updates selection if disapeared
		/// </summary>
		/// <param name="toCollapse"></param>
		/// <param name="all"></param>
		protected void CollapseImpl (T toCollapse, bool all)
		{

			if (toCollapse == null) {
				return;
			}


			var branch = ObjectToBranch (toCollapse);

			// Nothing to collapse
			if (branch == null) {
				return;
			}

			if (all) {
				branch.CollapseAll ();
			} else {
				branch.Collapse ();
			}

			if (SelectedObject != null && ObjectToBranch (SelectedObject) == null) {
				// If the old selection suddenly became invalid then clear it
				SelectedObject = null;
			}

			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Clears any cached results of <see cref="BuildLineMap"/>
		/// </summary>
		protected void InvalidateLineMap ()
		{
			cachedLineMap = null;
		}

		/// <summary>
		/// Returns the corresponding <see cref="Branch{T}"/> in the tree for
		/// <paramref name="toFind"/>.  This will not work for objects hidden
		/// by their parent being collapsed
		/// </summary>
		/// <param name="toFind"></param>
		/// <returns>The branch for <paramref name="toFind"/> or null if it is not currently 
		/// exposed in the tree</returns>
		private Branch<T> ObjectToBranch (T toFind)
		{
			return BuildLineMap ().FirstOrDefault (o => o.Model.Equals (toFind));
		}

		/// <summary>
		/// Returns true if the <paramref name="model"/> is either the 
		/// <see cref="SelectedObject"/> or part of a <see cref="MultiSelect"/>
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public bool IsSelected (T model)
		{
			return Equals (SelectedObject, model) ||
				(MultiSelect && multiSelectedRegions.Any (s => s.Contains (model)));
		}

		/// <summary>
		/// Returns <see cref="SelectedObject"/> (if not null) and all multi selected objects if 
		/// <see cref="MultiSelect"/> is true
		/// </summary>
		/// <returns></returns>
		public IEnumerable<T> GetAllSelectedObjects ()
		{
			var map = BuildLineMap ();

			// To determine multi selected objects, start with the line map, that avoids yielding 
			// hidden nodes that were selected then the parent collapsed e.g. programmatically or
			// with mouse click
			if (MultiSelect) {
				foreach (var m in map.Select (b => b.Model).Where (IsSelected)) {
					yield return m;
				}
			} else {
				if (SelectedObject != null) {
					yield return SelectedObject;
				}
			}
		}

		/// <summary>
		/// Selects all objects in the tree when <see cref="MultiSelect"/> is enabled otherwise 
		/// does nothing
		/// </summary>
		public void SelectAll ()
		{
			if (!MultiSelect) {
				return;
			}

			multiSelectedRegions.Clear ();

			var map = BuildLineMap ();

			if (map.Count == 0) {
				return;
			}

			multiSelectedRegions.Push (new TreeSelection<T> (map.ElementAt(0), map.Count, map));
			SetNeedsDisplay ();

			OnSelectionChanged (new SelectionChangedEventArgs<T> (this, SelectedObject, SelectedObject));
		}


		/// <summary>
		/// Raises the SelectionChanged event
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnSelectionChanged (SelectionChangedEventArgs<T> e)
		{
			SelectionChanged?.Invoke (this, e);
		}
	}

	/// <summary>
	/// Event args for the <see cref="TreeView{T}.ObjectActivated"/> event
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ObjectActivatedEventArgs<T> where T : class {

		/// <summary>
		/// The tree in which the activation occurred
		/// </summary>
		/// <value></value>
		public TreeView<T> Tree { get; }

		/// <summary>
		/// The object that was selected at the time of activation
		/// </summary>
		/// <value></value>
		public T ActivatedObject { get; }


		/// <summary>
		/// Creates a new instance documenting activation of the <paramref name="activated"/> object
		/// </summary>
		/// <param name="tree">Tree in which the activation is happening</param>
		/// <param name="activated">What object is being activated</param>
		public ObjectActivatedEventArgs (TreeView<T> tree, T activated)
		{
			Tree = tree;
			ActivatedObject = activated;
		}
	}

	class TreeSelection<T> where T : class {

		public Branch<T> Origin { get; }

		private HashSet<T> included = new HashSet<T> ();

		/// <summary>
		/// Creates a new selection between two branches in the tree
		/// </summary>
		/// <param name="from"></param>
		/// <param name="toIndex"></param>
		/// <param name="map"></param>
		public TreeSelection (Branch<T> from, int toIndex, IReadOnlyCollection<Branch<T>> map)
		{
			Origin = from;
			included.Add (Origin.Model);

			var oldIdx = map.IndexOf(from);

			var lowIndex = Math.Min (oldIdx, toIndex);
			var highIndex = Math.Max (oldIdx, toIndex);

			// Select everything between the old and new indexes
			foreach (var alsoInclude in map.Skip (lowIndex).Take (highIndex - lowIndex)) {
				included.Add (alsoInclude.Model);
			}

		}
		public bool Contains (T model)
		{
			return included.Contains (model);
		}
	}

	class Branch<T> where T : class {
		/// <summary>
		/// True if the branch is expanded to reveal child branches
		/// </summary>
		public bool IsExpanded { get; set; }

		/// <summary>
		/// The users object that is being displayed by this branch of the tree
		/// </summary>
		public T Model { get; private set; }

		/// <summary>
		/// The depth of the current branch.  Depth of 0 indicates root level branches
		/// </summary>
		public int Depth { get; private set; } = 0;

		/// <summary>
		/// The children of the current branch.  This is null until the first call to 
		/// <see cref="FetchChildren"/> to avoid enumerating the entire underlying hierarchy
		/// </summary>
		public Dictionary<T, Branch<T>> ChildBranches { get; set; }

		/// <summary>
		/// The parent <see cref="Branch{T}"/> or null if it is a root.
		/// </summary>
		public Branch<T> Parent { get; private set; }

		private TreeView<T> tree;

		/// <summary>
		/// Declares a new branch of <paramref name="tree"/> in which the users object 
		/// <paramref name="model"/> is presented
		/// </summary>
		/// <param name="tree">The UI control in which the branch resides</param>
		/// <param name="parentBranchIfAny">Pass null for root level branches, otherwise
		/// pass the parent</param>
		/// <param name="model">The user's object that should be displayed</param>
		public Branch (TreeView<T> tree, Branch<T> parentBranchIfAny, T model)
		{
			this.tree = tree;
			this.Model = model;

			if (parentBranchIfAny != null) {
				Depth = parentBranchIfAny.Depth + 1;
				Parent = parentBranchIfAny;
			}
		}


		/// <summary>
		/// Fetch the children of this branch. This method populates <see cref="ChildBranches"/>
		/// </summary>
		public virtual void FetchChildren ()
		{
			if (tree.TreeBuilder == null) {
				return;
			}

			var children = tree.TreeBuilder.GetChildren (this.Model) ?? Enumerable.Empty<T> ();

			this.ChildBranches = children.ToDictionary (k => k, val => new Branch<T> (tree, this, val));
		}

		/// <summary>
		/// Returns the width of the line including prefix and the results 
		/// of <see cref="TreeView{T}.AspectGetter"/> (the line body).
		/// </summary>
		/// <returns></returns>
		public virtual int GetWidth (ConsoleDriver driver)
		{
			return
				GetLinePrefix (driver).Sum (Rune.ColumnWidth) +
				Rune.ColumnWidth (GetExpandableSymbol (driver)) +
				(tree.AspectGetter (Model) ?? "").Length;
		}

		/// <summary>
		/// Renders the current <see cref="Model"/> on the specified line <paramref name="y"/>
		/// </summary>
		/// <param name="driver"></param>
		/// <param name="colorScheme"></param>
		/// <param name="y"></param>
		/// <param name="availableWidth"></param>
		public virtual void Draw (ConsoleDriver driver, ColorScheme colorScheme, int y, int availableWidth)
		{
			// true if the current line of the tree is the selected one and control has focus
			bool isSelected = tree.IsSelected (Model) && tree.HasFocus;
			Attribute lineColor = isSelected ? colorScheme.Focus : colorScheme.Normal;

			driver.SetAttribute (lineColor);

			// Everything on line before the expansion run and branch text
			Rune [] prefix = GetLinePrefix (driver).ToArray ();
			Rune expansion = GetExpandableSymbol (driver);
			string lineBody = tree.AspectGetter (Model) ?? "";

			tree.Move (0, y);

			// if we have scrolled to the right then bits of the prefix will have dispeared off the screen
			int toSkip = tree.ScrollOffsetHorizontal;

			// Draw the line prefix (all paralell lanes or whitespace and an expand/collapse/leaf symbol)
			foreach (Rune r in prefix) {

				if (toSkip > 0) {
					toSkip--;
				} else {
					driver.AddRune (r);
					availableWidth -= Rune.ColumnWidth (r);
				}
			}

			// pick color for expanded symbol
			if (tree.Style.ColorExpandSymbol || tree.Style.InvertExpandSymbolColors) {
				Attribute color;

				if (tree.Style.ColorExpandSymbol) {
					color = isSelected ? tree.ColorScheme.HotFocus : tree.ColorScheme.HotNormal;
				} else {
					color = lineColor;
				}

				if (tree.Style.InvertExpandSymbolColors) {
					color = new Attribute (color.Background, color.Foreground);
				}

				driver.SetAttribute (color);
			}

			if (toSkip > 0) {
				toSkip--;
			} else {
				driver.AddRune (expansion);
				availableWidth -= Rune.ColumnWidth (expansion);
			}

			// horizontal scrolling has already skipped the prefix but now must also skip some of the line body
			if (toSkip > 0) {
				if (toSkip > lineBody.Length) {
					lineBody = "";
				} else {
					lineBody = lineBody.Substring (toSkip);
				}
			}

			// If body of line is too long
			if (lineBody.Sum (l => Rune.ColumnWidth (l)) > availableWidth) {
				// remaining space is zero and truncate the line
				lineBody = new string (lineBody.TakeWhile (c => (availableWidth -= Rune.ColumnWidth (c)) >= 0).ToArray ());
				availableWidth = 0;
			} else {

				// line is short so remaining width will be whatever comes after the line body
				availableWidth -= lineBody.Length;
			}

			//reset the line color if it was changed for rendering expansion symbol
			driver.SetAttribute (lineColor);
			driver.AddStr (lineBody);

			if (availableWidth > 0) {
				driver.AddStr (new string (' ', availableWidth));
			}

			driver.SetAttribute (colorScheme.Normal);
		}

		/// <summary>
		/// Gets all characters to render prior to the current branches line.  This includes indentation
		/// whitespace and any tree branches (if enabled)
		/// </summary>
		/// <param name="driver"></param>
		/// <returns></returns>
		private IEnumerable<Rune> GetLinePrefix (ConsoleDriver driver)
		{
			// If not showing line branches or this is a root object
			if (!tree.Style.ShowBranchLines) {
				for (int i = 0; i < Depth; i++) {
					yield return new Rune (' ');
				}

				yield break;
			}

			// yield indentations with runes appropriate to the state of the parents
			foreach (var cur in GetParentBranches ().Reverse ()) {
				if (cur.IsLast ()) {
					yield return new Rune (' ');
				} else {
					yield return driver.VLine;
				}

				yield return new Rune (' ');
			}

			if (IsLast ()) {
				yield return driver.LLCorner;
			} else {
				yield return driver.LeftTee;
			}
		}

		/// <summary>
		/// Returns all parents starting with the immediate parent and ending at the root
		/// </summary>
		/// <returns></returns>
		private IEnumerable<Branch<T>> GetParentBranches ()
		{
			var cur = Parent;

			while (cur != null) {
				yield return cur;
				cur = cur.Parent;
			}
		}

		/// <summary>
		/// Returns an appropriate symbol for displaying next to the string representation of 
		/// the <see cref="Model"/> object to indicate whether it <see cref="IsExpanded"/> or
		/// not (or it is a leaf)
		/// </summary>
		/// <param name="driver"></param>
		/// <returns></returns>
		public Rune GetExpandableSymbol (ConsoleDriver driver)
		{
			var leafSymbol = tree.Style.ShowBranchLines ? driver.HLine : ' ';

			if (IsExpanded) {
				return tree.Style.CollapseableSymbol ?? leafSymbol;
			}

			if (CanExpand ()) {
				return tree.Style.ExpandableSymbol ?? leafSymbol;
			}

			return leafSymbol;
		}

		/// <summary>
		/// Returns true if the current branch can be expanded according to 
		/// the <see cref="TreeBuilder{T}"/> or cached children already fetched
		/// </summary>
		/// <returns></returns>
		public bool CanExpand ()
		{
			// if we do not know the children yet
			if (ChildBranches == null) {

				//if there is a rapid method for determining whether there are children
				if (tree.TreeBuilder.SupportsCanExpand) {
					return tree.TreeBuilder.CanExpand (Model);
				}

				//there is no way of knowing whether we can expand without fetching the children
				FetchChildren ();
			}

			//we fetched or already know the children, so return whether we have any
			return ChildBranches.Any ();
		}

		/// <summary>
		/// Expands the current branch if possible
		/// </summary>
		public void Expand ()
		{
			if (ChildBranches == null) {
				FetchChildren ();
			}

			if (ChildBranches.Any ()) {
				IsExpanded = true;
			}
		}

		/// <summary>
		/// Marks the branch as collapsed (<see cref="IsExpanded"/> false)
		/// </summary>
		public void Collapse ()
		{
			IsExpanded = false;
		}

		/// <summary>
		/// Refreshes cached knowledge in this branch e.g. what children an object has
		/// </summary>
		/// <param name="startAtTop">True to also refresh all <see cref="Parent"/> 
		/// branches (starting with the root)</param>
		public void Refresh (bool startAtTop)
		{
			// if we must go up and refresh from the top down
			if (startAtTop) {
				Parent?.Refresh (true);
			}

			// we don't want to loose the state of our children so lets be selective about how we refresh
			//if we don't know about any children yet just use the normal method
			if (ChildBranches == null) {
				FetchChildren ();
			} else {
				// we already knew about some children so preserve the state of the old children

				// first gather the new Children
				var newChildren = tree.TreeBuilder?.GetChildren (this.Model) ?? Enumerable.Empty<T> ();

				// Children who no longer appear need to go
				foreach (var toRemove in ChildBranches.Keys.Except (newChildren).ToArray ()) {
					ChildBranches.Remove (toRemove);

					//also if the user has this node selected (its disapearing) so lets change selection to us (the parent object) to be helpful
					if (Equals (tree.SelectedObject, toRemove)) {
						tree.SelectedObject = Model;
					}
				}

				// New children need to be added
				foreach (var newChild in newChildren) {
					// If we don't know about the child yet we need a new branch
					if (!ChildBranches.ContainsKey (newChild)) {
						ChildBranches.Add (newChild, new Branch<T> (tree, this, newChild));
					} else {
						//we already have this object but update the reference anyway incase Equality match but the references are new
						ChildBranches [newChild].Model = newChild;
					}
				}
			}

		}

		/// <summary>
		/// Calls <see cref="Refresh(bool)"/> on the current branch and all expanded children
		/// </summary>
		internal void Rebuild ()
		{
			Refresh (false);

			// if we know about our children
			if (ChildBranches != null) {
				if (IsExpanded) {
					//if we are expanded we need to updatethe visible children
					foreach (var child in ChildBranches) {
						child.Value.Rebuild ();
					}

				} else {
					// we are not expanded so should forget about children because they may not exist anymore
					ChildBranches = null;
				}
			}

		}

		/// <summary>
		/// Returns true if this branch has parents and it is the last node of it's parents 
		/// branches (or last root of the tree)
		/// </summary>
		/// <returns></returns>
		private bool IsLast ()
		{
			if (Parent == null) {
				return this == tree.roots.Values.LastOrDefault ();
			}

			return Parent.ChildBranches.Values.LastOrDefault () == this;
		}

		/// <summary>
		/// Returns true if the given x offset on the branch line is the +/- symbol.  Returns 
		/// false if not showing expansion symbols or leaf node etc
		/// </summary>
		/// <param name="driver"></param>
		/// <param name="x"></param>
		/// <returns></returns>
		internal bool IsHitOnExpandableSymbol (ConsoleDriver driver, int x)
		{
			// if leaf node then we cannot expand
			if (!CanExpand ()) {
				return false;
			}

			// if we could theoretically expand
			if (!IsExpanded && tree.Style.ExpandableSymbol != null) {
				return x == GetLinePrefix (driver).Count ();
			}

			// if we could theoretically collapse
			if (IsExpanded && tree.Style.CollapseableSymbol != null) {
				return x == GetLinePrefix (driver).Count ();
			}

			return false;
		}

		/// <summary>
		/// Expands the current branch and all children branches
		/// </summary>
		internal void ExpandAll ()
		{
			Expand ();

			if (ChildBranches != null) {
				foreach (var child in ChildBranches) {
					child.Value.ExpandAll ();
				}
			}
		}

		/// <summary>
		/// Collapses the current branch and all children branches (even though those branches are 
		/// no longer visible they retain collapse/expansion state)
		/// </summary>
		internal void CollapseAll ()
		{
			Collapse ();

			if (ChildBranches != null) {
				foreach (var child in ChildBranches) {
					child.Value.CollapseAll ();
				}
			}
		}
	}

	/// <summary>
	/// Delegates of this type are used to fetch string representations of user's model objects
	/// </summary>
	/// <param name="toRender">The object that is being rendered</param>
	/// <returns></returns>
	public delegate string AspectGetterDelegate<T> (T toRender) where T : class;

	/// <summary>
	/// Event arguments describing a change in selected object in a tree view
	/// </summary>
	public class SelectionChangedEventArgs<T> : EventArgs where T : class {
		/// <summary>
		/// The view in which the change occurred
		/// </summary>
		public TreeView<T> Tree { get; }

		/// <summary>
		/// The previously selected value (can be null)
		/// </summary>
		public T OldValue { get; }

		/// <summary>
		/// The newly selected value in the <see cref="Tree"/> (can be null)
		/// </summary>
		public T NewValue { get; }

		/// <summary>
		/// Creates a new instance of event args describing a change of selection 
		/// in <paramref name="tree"/>
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		public SelectionChangedEventArgs (TreeView<T> tree, T oldValue, T newValue)
		{
			Tree = tree;
			OldValue = oldValue;
			NewValue = newValue;
		}
	}

	static class ReadOnlyCollectionExtensions {
		
		public static int IndexOf<T> (this IReadOnlyCollection<T> self, Func<T,bool> predicate)
		{
			int i = 0;
			foreach (T element in self) {
				if (predicate(element))
					return i;
				i++;
			}
			return -1;
		}
		public static int IndexOf<T> (this IReadOnlyCollection<T> self, T toFind)
		{
			int i = 0;
			foreach (T element in self) {
				if (Equals(element,toFind))
					return i;
				i++;
			}
			return -1;
		}
	}
}