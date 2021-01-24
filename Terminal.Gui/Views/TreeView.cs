// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls by phillip.piper@gmail.com).  Phillip has explicitly granted permission for his design and code to be used in this library under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using NStack;

namespace Terminal.Gui {

	/// <summary>
	/// Interface to implement when you want the regular (non generic) <see cref="TreeView"/> to automatically determine children for your class (without having to specify a <see cref="ITreeBuilder{T}"/>)
	/// </summary>
	public interface ITreeNode
	{
		/// <summary>
		/// The children of your class which should be rendered underneath it when expanded
		/// </summary>
		/// <value></value>
		IList<ITreeNode> Children {get;}
	}

	/// <summary>
	/// Simple class for representing nodes, use with regular (non generic) <see cref="TreeView"/>.
	/// </summary>
	public class TreeNode : ITreeNode
	{
		/// <summary>
		/// Children of the current node
		/// </summary>
		/// <returns></returns>
		public IList<ITreeNode> Children {get;set;} = new List<ITreeNode>();
		
		/// <summary>
		/// Text to display in tree node for current entry
		/// </summary>
		/// <value></value>
		public string Text {get;set;}

		/// <summary>
		/// returns <see cref="Text"/>
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Text ?? "Unamed Node";
		}

		/// <summary>
		/// Initialises a new instance with no <see cref="Text"/>
		/// </summary>
		public TreeNode()
		{
			
		}
		/// <summary>
		/// Initialises a new instance and sets starting <see cref="Text"/>
		/// </summary>
		public TreeNode(string text)
		{
			Text = text;
		}
	}

	/// <summary>
	/// Interface for supplying data to a <see cref="TreeView{T}"/> on demand as root level nodes are expanded by the user
	/// </summary>
	public interface ITreeBuilder<T>
	{
		/// <summary>
		/// Returns true if <see cref="CanExpand"/> is implemented by this class
		/// </summary>
		/// <value></value>
		bool SupportsCanExpand {get;}

		/// <summary>
		/// Returns true/false for whether a model has children.  This method should be implemented when <see cref="GetChildren"/> is an expensive operation otherwise <see cref="SupportsCanExpand"/> should return false (in which case this method will not be called)
		/// </summary>
		/// <remarks>Only implement this method if you have a very fast way of determining whether an object can have children e.g. checking a Type (directories can always be expanded)</remarks>
		/// <param name="model"></param>
		/// <returns></returns>
		bool CanExpand(T model);

		/// <summary>
		/// Returns all children of a given <paramref name="model"/> which should be added to the tree as new branches underneath it
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		IEnumerable<T> GetChildren(T model);
	}

	/// <summary>
	/// Abstract implementation of <see cref="ITreeBuilder{T}"/>.
	/// </summary>
	public abstract class TreeBuilder<T> : ITreeBuilder<T> {

		/// <inheritdoc/>
		public bool SupportsCanExpand { get; protected set;} = false;

		/// <summary>
		/// Override this method to return a rapid answer as to whether <see cref="GetChildren(T)"/> returns results.  If you are implementing this method ensure you passed true in base constructor or set <see cref="SupportsCanExpand"/>
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public virtual bool CanExpand (T model){
			
			return GetChildren(model).Any();
		}

		/// <inheritdoc/>
		public abstract IEnumerable<T> GetChildren (T model);

		/// <summary>
		/// Constructs base and initializes <see cref="SupportsCanExpand"/>
		/// </summary>
		/// <param name="supportsCanExpand">Pass true if you intend to implement <see cref="CanExpand(T)"/> otherwise false</param>
		public TreeBuilder(bool supportsCanExpand)
		{
			SupportsCanExpand = supportsCanExpand;
		}
	}

	/// <summary>
	/// <see cref="ITreeBuilder{T}"/> implementation for <see cref="ITreeNode"/> objects
	/// </summary>
	public class TreeNodeBuilder : TreeBuilder<ITreeNode>
	{
		
		/// <summary>
		/// Initialises a new instance of builder for any model objects of Type <see cref="ITreeNode"/>
		/// </summary>
		public TreeNodeBuilder():base(false)
		{
			
		}

		/// <summary>
		/// Returns <see cref="ITreeNode.Children"/> from <paramref name="model"/>
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public override IEnumerable<ITreeNode> GetChildren (ITreeNode model)
		{
			return model.Children;
		}
	}

	/// <summary>
	/// Implementation of <see cref="ITreeBuilder{T}"/> that uses user defined functions
	/// </summary>
	public class DelegateTreeBuilder<T> : TreeBuilder<T>
	{
		private Func<T,IEnumerable<T>> childGetter;
		private Func<T,bool> canExpand;

		/// <summary>
		/// Constructs an implementation of <see cref="ITreeBuilder{T}"/> that calls the user defined method <paramref name="childGetter"/> to determine children
		/// </summary>
		/// <param name="childGetter"></param>
		/// <returns></returns>
		public DelegateTreeBuilder(Func<T,IEnumerable<T>> childGetter) : base(false)
		{
			this.childGetter = childGetter;
		}

		/// <summary>
		/// Constructs an implementation of <see cref="ITreeBuilder{T}"/> that calls the user defined method <paramref name="childGetter"/> to determine children and <paramref name="canExpand"/> to determine expandability
		/// </summary>
		/// <param name="childGetter"></param>
		/// <param name="canExpand"></param>
		/// <returns></returns>
		public DelegateTreeBuilder(Func<T,IEnumerable<T>> childGetter, Func<T,bool> canExpand) : base(true)
		{
			this.childGetter = childGetter;
			this.canExpand = canExpand;
		}

		/// <summary>
		/// Returns whether a node can be expanded based on the delegate passed during construction
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public override bool CanExpand (T model)
		{
			return canExpand?.Invoke(model) ?? base.CanExpand (model);
		}

		/// <summary>
		/// Returns children using the delegate method passed during construction
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public override IEnumerable<T> GetChildren (T model)
		{
			return childGetter.Invoke(model);
		}
	}

	/// <summary>
	/// Interface for all non generic members of <see cref="TreeView{T}"/>
	/// </summary>
	public interface ITreeView {
		
		
		/// <summary>
		/// Contains options for changing how the tree is rendered
		/// </summary>
		TreeStyle Style{get;set;}

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
	/// Convenience implementation of generic <see cref="TreeView{T}"/> for any tree were all nodes implement <see cref="ITreeNode"/>
	/// </summary>
	public class TreeView : TreeView<ITreeNode> {

		/// <summary>
		/// Creates a new instance of the tree control with absolute positioning and initialises <see cref="TreeBuilder{T}"/> with default <see cref="ITreeNode"/> based builder
		/// </summary>
		public TreeView ()
		{
			TreeBuilder = new TreeNodeBuilder();
		}
	}
	
	/// <summary>
	/// Defines rendering options that affect how the tree is displayed
	/// </summary>
	public class TreeStyle {

		/// <summary>
		/// True to render vertical lines under expanded nodes to show which node belongs to which parent.  False to use only whitespace
		/// </summary>
		/// <value></value>
		public bool ShowBranchLines {get;set;} = true;
				
		/// <summary>
		/// Symbol to use for branch nodes that can be expanded to indicate this to the user.  Defaults to '+'. Set to null to hide
		/// </summary>
		public Rune? ExpandableSymbol {get;set;} = '+';
				
		/// <summary>
		/// Symbol to use for branch nodes that can be collapsed (are currently expanded).  Defaults to '-'.  Set to null to hide
		/// </summary>
		public Rune? CollapseableSymbol {get;set;} = '-';

		/// <summary>
		/// Set to true to highlight expand/collapse symbols in hot key color
		/// </summary>
		public bool ColorExpandSymbol {get;set;}

		/// <summary>
		/// Invert console colours used to render the expand symbol
		/// </summary>
		public bool InvertExpandSymbolColors {get;set;}

	}

	/// <summary>
	/// Hierarchical tree view with expandable branches.  Branch objects are dynamically determined when expanded using a user defined <see cref="ITreeBuilder{T}"/>
	/// </summary>
	public class TreeView<T> : View, ITreeView where T:class
	{   
		private int scrollOffsetVertical;
		private int scrollOffsetHorizontal;

		/// <summary>
		/// Determines how sub branches of the tree are dynamically built at runtime as the user expands root nodes
		/// </summary>
		/// <value></value>
		public ITreeBuilder<T> TreeBuilder { get;set;}

		/// <summary>
		/// private variable for <see cref="SelectedObject"/>
		/// </summary>
		T selectedObject;

		
		/// <summary>
		/// Contains options for changing how the tree is rendered
		/// </summary>
		public TreeStyle Style {get;set;} = new TreeStyle();

		/// <summary>
		/// The currently selected object in the tree
		/// </summary>
		public T SelectedObject { 
			get => selectedObject; 
			set {                
				var oldValue = selectedObject;
				selectedObject = value; 

				if(!ReferenceEquals(oldValue,value))
					SelectionChanged?.Invoke(this,new SelectionChangedEventArgs<T>(this,oldValue,value));
			}
		}
		
		/// <summary>
		/// Called when the <see cref="SelectedObject"/> changes
		/// </summary>
		public event EventHandler<SelectionChangedEventArgs<T>> SelectionChanged;

		/// <summary>
		/// The root objects in the tree, note that this collection is of root objects only
		/// </summary>
		public IEnumerable<T> Objects {get=>roots.Keys;}

		/// <summary>
		/// Map of root objects to the branches under them.  All objects have a <see cref="Branch{T}"/> even if that branch has no children
		/// </summary>
		internal Dictionary<T,Branch<T>> roots {get; set;} = new Dictionary<T, Branch<T>>();

		/// <summary>
		/// The amount of tree view that has been scrolled off the top of the screen (by the user scrolling down)
		/// </summary>
		/// <remarks>Setting a value of less than 0 will result in a offset of 0.  To see changes in the UI call <see cref="View.SetNeedsDisplay()"/></remarks>
		public int ScrollOffsetVertical { 
			get => scrollOffsetVertical;
			set {
				scrollOffsetVertical = Math.Max(0,value); 
			}
		}


		/// <summary>
		/// The amount of tree view that has been scrolled to the right (horizontally)
		/// </summary>
		/// <remarks>Setting a value of less than 0 will result in a offset of 0.  To see changes in the UI call <see cref="View.SetNeedsDisplay()"/></remarks>
		public int ScrollOffsetHorizontal { 
			get => scrollOffsetHorizontal;
			set {
				scrollOffsetHorizontal = Math.Max(0,value); 
			}
		}

		/// <summary>
		/// The current number of rows in the tree (ignoring the controls bounds)
		/// </summary>
		public int ContentHeight => BuildLineMap().Count();

		/// <summary>
		/// Returns the string representation of model objects hosted in the tree.  Default implementation is to call <see cref="object.ToString"/>
		/// </summary>
		/// <value></value>
		public AspectGetterDelegate<T> AspectGetter {get;set;} = (o)=>o.ToString() ?? "";

		/// <summary>
		/// Creates a new tree view with absolute positioning.  Use <see cref="AddObjects(IEnumerable{T})"/> to set set root objects for the tree.  Children will not be rendered until you set <see cref="TreeBuilder"/>
		/// </summary>
		public TreeView():base()
		{
			CanFocus = true;
		}

		/// <summary>
		/// Initialises <see cref="TreeBuilder"/>.Creates a new tree view with absolute positioning.  Use <see cref="AddObjects(IEnumerable{T})"/> to set set root objects for the tree.
		/// </summary>
		public TreeView(ITreeBuilder<T> builder) : this()
		{
			TreeBuilder = builder;
		}

		/// <summary>
		/// Adds a new root level object unless it is already a root of the tree
		/// </summary>
		/// <param name="o"></param>
		public void AddObject(T o)
		{
			if(!roots.ContainsKey(o)) {
				roots.Add(o,new Branch<T>(this,null,o));
				SetNeedsDisplay();
			}
		}

		/// <summary>
		/// Removes all objects from the tree and clears <see cref="SelectedObject"/>
		/// </summary>
		public void ClearObjects()
		{
			SelectedObject = default(T);
			roots = new Dictionary<T, Branch<T>>();
			SetNeedsDisplay();
		}

		/// <summary>
		/// Removes the given root object from the tree
		/// </summary>
		/// <remarks>If <paramref name="o"/> is the currently <see cref="SelectedObject"/> then the selection is cleared</remarks>
		/// <param name="o"></param>
		public void Remove(T o)
		{
			if(roots.ContainsKey(o)) {
				roots.Remove(o);
				SetNeedsDisplay();

				if(Equals(SelectedObject,o))
					SelectedObject = default(T);
			}
		}
		
		/// <summary>
		/// Adds many new root level objects.  Objects that are already root objects are ignored
		/// </summary>
		/// <param name="collection">Objects to add as new root level objects</param>
		public void AddObjects(IEnumerable<T> collection)
		{
			bool objectsAdded = false;

			foreach(var o in collection) {
				if (!roots.ContainsKey (o)) {
					roots.Add(o,new Branch<T>(this,null,o));
					objectsAdded = true;
				}	
			}
				
			if(objectsAdded)
				SetNeedsDisplay();
		}

		/// <summary>
		/// Refreshes the state of the object <paramref name="o"/> in the tree.  This will recompute children, string representation etc
		/// </summary>
		/// <remarks>This has no effect if the object is not exposed in the tree.</remarks>
		/// <param name="o"></param>
		/// <param name="startAtTop">True to also refresh all ancestors of the objects branch (starting with the root).  False to refresh only the passed node</param>
		public void RefreshObject (T o, bool startAtTop = false)
		{
			var branch = ObjectToBranch(o);
			if(branch != null) {
				branch.Refresh(startAtTop);
				SetNeedsDisplay();
			}

		}
		
		/// <summary>
		/// Rebuilds the tree structure for all exposed objects starting with the root objects.  Call this method when you know there are changes to the tree but don't know which objects have changed (otherwise use <see cref="RefreshObject(T, bool)"/>)
		/// </summary>
		public void RebuildTree()
		{
			foreach(var branch in roots.Values)
				branch.Rebuild();
			
			SetNeedsDisplay();
		}

		/// <summary>
		/// Returns the currently expanded children of the passed object.  Returns an empty collection if the branch is not exposed or not expanded
		/// </summary>
		/// <param name="o">An object in the tree</param>
		/// <returns></returns>
		public IEnumerable<T> GetChildren (T o)
		{
			var branch = ObjectToBranch(o);

			if(branch == null || !branch.IsExpanded)
				return new T[0];

			return branch.ChildBranches?.Values?.Select(b=>b.Model)?.ToArray() ?? new T[0];
		}
		/// <summary>
		/// Returns the parent object of <paramref name="o"/> in the tree.  Returns null if the object is not exposed in the tree
		/// </summary>
		/// <param name="o">An object in the tree</param>
		/// <returns></returns>
		public T GetParent (T o)
		{
			return ObjectToBranch(o)?.Parent?.Model;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if(roots == null)
				return;

			var map = BuildLineMap();

			for(int line = 0 ; line < bounds.Height; line++){

				var idxToRender = ScrollOffsetVertical + line;

				// Is there part of the tree view to render?
				if(idxToRender < map.Length) {
					// Render the line
					map[idxToRender].Draw(Driver,ColorScheme,line,bounds.Width);
				} else {

					// Else clear the line to prevent stale symbols due to scrolling etc
					Move(0,line);
					Driver.SetAttribute(ColorScheme.Normal);
					Driver.AddStr(new string(' ',bounds.Width));
				}
					
			}
		}
		
		/// <summary>
		/// Returns the index of the object <paramref name="o"/> if it is currently exposed (it's parent(s) have been expanded).  This can be used with <see cref="ScrollOffsetVertical"/> and <see cref="View.SetNeedsDisplay()"/> to scroll to a specific object
		/// </summary>
		/// <remarks>Uses the Equals method and returns the first index at which the object is found or -1 if it is not found</remarks>
		/// <param name="o">An object that appears in your tree and is currently exposed</param>
		/// <returns>The index the object was found at or -1 if it is not currently revealed or not in the tree at all</returns>
		public int GetScrollOffsetOf(T o)
		{
			var map = BuildLineMap();
			for (int i = 0; i < map.Length; i++)
			{
				if (map[i].Model.Equals(o))
					return i;
			}

			//object not found
			return -1;
		}

		/// <summary>
		/// Returns the maximum width line in the tree including prefix and expansion symbols
		/// </summary>
		/// <param name="visible">True to consider only rows currently visible (based on window bounds and <see cref="ScrollOffsetVertical"/>.  False to calculate the width of every exposed branch in the tree</param>
		/// <returns></returns>
		public int GetContentWidth(bool visible){
			
			var map = BuildLineMap();

			if(map.Length == 0)
				return 0;

			if(visible){

				//Somehow we managed to scroll off the end of the control
				if(ScrollOffsetVertical >= map.Length)
					return 0;

				// If control has no height to it then there is no visible area for content
				if(Bounds.Height == 0)
					return 0;

				return map.Skip(ScrollOffsetVertical).Take(Bounds.Height).Max(b=>b.GetWidth(Driver));
			}
			else{

				return map.Max(b=>b.GetWidth(Driver));
			}
		}

		/// <summary>
		/// Calculates all currently visible/expanded branches (including leafs) and outputs them by index from the top of the screen
		/// </summary>
		/// <remarks>Index 0 of the returned array is the first item that should be visible in the top of the control, index 1 is the next etc.</remarks>
		/// <returns></returns>
		private Branch<T>[] BuildLineMap()
		{
			List<Branch<T>> toReturn = new List<Branch<T>>();

			foreach(var root in roots.Values) {
				toReturn.AddRange(AddToLineMap(root));
			}

			return toReturn.ToArray();
		}

		private IEnumerable<Branch<T>> AddToLineMap (Branch<T> currentBranch)
		{
			yield return currentBranch;

			if(currentBranch.IsExpanded){

				foreach(var subBranch in currentBranch.ChildBranches.Values){
					foreach(var sub in AddToLineMap(subBranch)) {
						yield return sub;
					}
				}
			}
		}

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			switch (keyEvent.Key) {
				case Key.CursorRight:
					Expand(SelectedObject);
				break;
				case Key.CursorLeft:
					CursorLeft();
				break;
			
				case Key.CursorUp:
					AdjustSelection(-1);
				break;
				case Key.CursorDown:
					AdjustSelection(1);
				break;
				case Key.PageUp:
					AdjustSelection(-Bounds.Height);
				break;
				
				case Key.PageDown:
					AdjustSelection(Bounds.Height);
				break;
				case Key.Home:
					GoToFirst();
				break;
				case Key.End:
					GoToEnd();
				break;

				default:
					// we don't care about this keystroke
					return false;
			}

			PositionCursor ();
			return true;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp && me.Flags != MouseFlags.WheeledRight&& me.Flags != MouseFlags.WheeledLeft)
				return false;

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}


			if (me.Flags == MouseFlags.WheeledDown) {

				ScrollOffsetVertical++;
				SetNeedsDisplay();

				return true;
			} else if (me.Flags == MouseFlags.WheeledUp) {
				ScrollOffsetVertical--;
				SetNeedsDisplay();

				return true;
			}

			if (me.Flags == MouseFlags.WheeledRight) {

				ScrollOffsetHorizontal++;
				SetNeedsDisplay();

				return true;
			} else if (me.Flags == MouseFlags.WheeledLeft) {
				ScrollOffsetHorizontal--;
				SetNeedsDisplay();

				return true;
			}

			if(me.Flags == MouseFlags.Button1Clicked) {

				var map = BuildLineMap();
				
				var idx = me.Y + ScrollOffsetVertical;

				// click is outside any visible nodes
				if(idx < 0 || idx >= map.Length) {
					return false;
				}
				
				// The line they clicked on
				var clickedBranch = map[idx];

				bool isExpandToggleAttempt = clickedBranch.IsHitOnExpandableSymbol(Driver,me.X);
				
				// If we are already selected (double click)
				if(Equals(SelectedObject,clickedBranch.Model)) 
					isExpandToggleAttempt = true;

				// if they clicked on the +/- expansion symbol
				if( isExpandToggleAttempt) {

					if (clickedBranch.IsExpanded) {
						clickedBranch.Collapse();
					}
					else
					if(clickedBranch.CanExpand())
						clickedBranch.Expand();
					else {
						SelectedObject = clickedBranch.Model; // It is a leaf node
					}
				}
				else {
					// It is a first click somewhere in the current line that doesn't look like an expansion/collapse attempt
					SelectedObject = clickedBranch.Model;
				}

				SetNeedsDisplay();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Positions the cursor at the start of the selected objects line (if visible)
		/// </summary>
		public override void PositionCursor()
		{
			if (CanFocus && HasFocus && Visible && SelectedObject != null) 
			{
				var map = BuildLineMap();
				var idx = Array.FindIndex(map,b=>b.Model.Equals(SelectedObject));

				// if currently selected line is visible
				if(idx - ScrollOffsetVertical >= 0 && idx - ScrollOffsetVertical  < Bounds.Height)
					Move(0,idx - ScrollOffsetVertical);
				else
					base.PositionCursor();

			} else {
				base.PositionCursor();
			}			
		}


		/// <summary>
		/// Determines systems behaviour when the left arrow key is pressed.  Default behaviour is to collapse the current tree node if possible otherwise changes selection to current branches parent
		/// </summary>
		protected virtual void CursorLeft()
		{
			if(IsExpanded(SelectedObject))
				Collapse(SelectedObject);
			else
			{
				var parent = GetParent(SelectedObject);
				if(parent != null){
					SelectedObject = parent;
					AdjustSelection(0);
					SetNeedsDisplay();
				}
			}
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to the first root object and resets the <see cref="ScrollOffsetVertical"/> to 0
		/// </summary>
		public void GoToFirst()
		{
			ScrollOffsetVertical = 0;
			SelectedObject = roots.Keys.FirstOrDefault();

			SetNeedsDisplay();
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to the last object in the tree and scrolls so that it is visible
		/// </summary>
		public void GoToEnd ()
		{
			var map = BuildLineMap();
			ScrollOffsetVertical = Math.Max(0,map.Length - Bounds.Height +1);
			SelectedObject = map.Last().Model;
						
			SetNeedsDisplay();
		}

		/// <summary>
		/// Changes the selected object by a number of screen lines
		/// </summary>
		/// <remarks>If nothing is currently selected the first root is selected.  If the selected object is no longer in the tree the first object is selected</remarks>
		/// <param name="offset"></param>
		public void AdjustSelection (int offset)
		{
			if(SelectedObject == null){
				SelectedObject = roots.Keys.FirstOrDefault();
			}
			else {
				var map = BuildLineMap();

				var idx = Array.FindIndex(map,b=>b.Model.Equals(SelectedObject));

				if(idx == -1) {

					// The current selection has disapeared!
					SelectedObject = roots.Keys.FirstOrDefault();
				}
				else {
					var newIdx = Math.Min(Math.Max(0,idx+offset),map.Length-1);
					SelectedObject = map[newIdx].Model;

					
					if(newIdx < ScrollOffsetVertical) {
						//if user has scrolled up too far to see their selection
						ScrollOffsetVertical = newIdx;
					}
					else if(newIdx >= ScrollOffsetVertical + Bounds.Height){
						
						//if user has scrolled off bottom of visible tree
						ScrollOffsetVertical = Math.Max(0,(newIdx+1) - Bounds.Height);

					}
				}

			}
						
			SetNeedsDisplay();
		}

		/// <summary>
		/// Expands the supplied object if it is contained in the tree (either as a root object or as an exposed branch object)
		/// </summary>
		/// <param name="toExpand">The object to expand</param>
		public void Expand(T toExpand)
		{
			if(toExpand == null)
				return;
			
			ObjectToBranch(toExpand)?.Expand();
			SetNeedsDisplay();
		}
		
		/// <summary>
		/// Returns true if the given object <paramref name="o"/> is exposed in the tree and can be expanded otherwise false
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public bool CanExpand(T o)
		{
			return ObjectToBranch(o)?.CanExpand() ?? false;
		}

		/// <summary>
		/// Returns true if the given object <paramref name="o"/> is exposed in the tree and expanded otherwise false
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public bool IsExpanded(T o)
		{
			return ObjectToBranch(o)?.IsExpanded ?? false;
		}

		/// <summary>
		/// Collapses the supplied object if it is currently expanded 
		/// </summary>
		/// <param name="toCollapse">The object to collapse</param>
		public void Collapse(T toCollapse)
		{
			if(toCollapse == null)
				return;

			ObjectToBranch(toCollapse)?.Collapse();
			SetNeedsDisplay();
		}

		/// <summary>
		/// Returns the corresponding <see cref="Branch{T}"/> in the tree for <paramref name="toFind"/>.  This will not work for objects hidden by their parent being collapsed
		/// </summary>
		/// <param name="toFind"></param>
		/// <returns>The branch for <paramref name="toFind"/> or null if it is not currently exposed in the tree</returns>
		private Branch<T> ObjectToBranch(T toFind)
		{
			return BuildLineMap().FirstOrDefault(o=>o.Model.Equals(toFind));
		}
	}

	class Branch<T> where T:class
	{
		/// <summary>
		/// True if the branch is expanded to reveal child branches
		/// </summary>
		public bool IsExpanded {get;set;}

		/// <summary>
		/// The users object that is being displayed by this branch of the tree
		/// </summary>
		public T Model {get;private set;}
		
		/// <summary>
		/// The depth of the current branch.  Depth of 0 indicates root level branches
		/// </summary>
		public int Depth {get;private set;} = 0;

		/// <summary>
		/// The children of the current branch.  This is null until the first call to <see cref="FetchChildren"/> to avoid enumerating the entire underlying hierarchy
		/// </summary>
		public Dictionary<T,Branch<T>> ChildBranches {get;set;}

		/// <summary>
		/// The parent <see cref="Branch{T}"/> or null if it is a root.
		/// </summary>
		public Branch<T> Parent {get; private set;}

		private TreeView<T> tree;
		
		/// <summary>
		/// Declares a new branch of <paramref name="tree"/> in which the users object <paramref name="model"/> is presented
		/// </summary>
		/// <param name="tree">The UI control in which the branch resides</param>
		/// <param name="parentBranchIfAny">Pass null for root level branches, otherwise pass the parent</param>
		/// <param name="model">The user's object that should be displayed</param>
		public Branch(TreeView<T> tree,Branch<T> parentBranchIfAny,T model)
		{
			this.tree  = tree;
			this.Model = model;
			
			if(parentBranchIfAny != null) {
				Depth = parentBranchIfAny.Depth +1;
				Parent = parentBranchIfAny;
			}
		}


		/// <summary>
		/// Fetch the children of this branch. This method populates <see cref="ChildBranches"/>
		/// </summary>
		public virtual void FetchChildren()
		{
			if (tree.TreeBuilder == null)
				return;

			var children = tree.TreeBuilder.GetChildren(this.Model) ?? Enumerable.Empty<T>();

			this.ChildBranches = children.ToDictionary(k=>k,val=>new Branch<T>(tree,this,val));
		}

		/// <summary>
		/// Returns the width of the line including prefix and the results of <see cref="TreeView{T}.AspectGetter"/> (the line body).
		/// </summary>
		/// <returns></returns>
		public virtual int GetWidth (ConsoleDriver driver)
		{
			return 
				GetLinePrefix(driver).Sum(Rune.ColumnWidth) + 
				Rune.ColumnWidth(GetExpandableSymbol(driver)) + 
				(tree.AspectGetter(Model) ?? "").Length;
		}

		/// <summary>
		/// Renders the current <see cref="Model"/> on the specified line <paramref name="y"/>
		/// </summary>
		/// <param name="driver"></param>
		/// <param name="colorScheme"></param>
		/// <param name="y"></param>
		/// <param name="availableWidth"></param>
		public virtual void Draw(ConsoleDriver driver,ColorScheme colorScheme, int y, int availableWidth)
		{
			// true if the current line of the tree is the selected one and control has focus
			bool isSelected = tree.SelectedObject == Model && tree.HasFocus;
			Attribute lineColor = isSelected? colorScheme.Focus : colorScheme.Normal;

			driver.SetAttribute(lineColor);

			// Everything on line before the expansion run and branch text
			Rune[] prefix = GetLinePrefix(driver).ToArray();
			Rune expansion = GetExpandableSymbol(driver);
			string lineBody = tree.AspectGetter(Model) ?? "";

			tree.Move(0,y);

			// if we have scrolled to the right then bits of the prefix will have dispeared off the screen
			int toSkip = tree.ScrollOffsetHorizontal;

			// Draw the line prefix (all paralell lanes or whitespace and an expand/collapse/leaf symbol)
			foreach(Rune r in prefix){

				if(toSkip > 0){
					toSkip--;
				}
				else{
					driver.AddRune(r);
					availableWidth -= Rune.ColumnWidth(r);
				}
			}

			// pick color for expanded symbol
			if(tree.Style.ColorExpandSymbol || tree.Style.InvertExpandSymbolColors)
			{
				Attribute color;

				if(tree.Style.ColorExpandSymbol)
					color = isSelected ? tree.ColorScheme.HotFocus : tree.ColorScheme.HotNormal;
				else
					color = lineColor;

				if(tree.Style.InvertExpandSymbolColors)
					color = new Attribute(color.Background,color.Foreground);

				driver.SetAttribute(color);
			}

			if(toSkip > 0){
				toSkip--;
			}
			else{
				driver.AddRune(expansion);
				availableWidth -= Rune.ColumnWidth(expansion);
			}

			// horizontal scrolling has already skipped the prefix but now must also skip some of the line body
			if(toSkip > 0)
			{
				if(toSkip > lineBody.Length){
					lineBody = "";
				}
				else{
					lineBody = lineBody.Substring(toSkip);
				}
			}
			
			// If body of line is too long
			if(lineBody.Sum(l=>Rune.ColumnWidth(l)) > availableWidth)
			{
				// remaining space is zero and truncate the line
				lineBody = new string(lineBody.TakeWhile(c=>(availableWidth -= Rune.ColumnWidth(c)) > 0).ToArray());
				availableWidth = 0;
			}
			else{

				// line is short so remaining width will be whatever comes after the line body
				availableWidth -= lineBody.Length;
			}

			//reset the line color if it was changed for rendering expansion symbol
			driver.SetAttribute(lineColor);
			driver.AddStr(lineBody);

			if(availableWidth > 0)
				driver.AddStr(new string(' ',availableWidth));

			driver.SetAttribute(colorScheme.Normal);
		}

		/// <summary>
		/// Gets all characters to render prior to the current branches line.  This includes indentation whitespace and any tree branches (if enabled)
		/// </summary>
		/// <param name="driver"></param>
		/// <returns></returns>
		private IEnumerable<Rune> GetLinePrefix (ConsoleDriver driver)
		{
			// If not showing line branches or this is a root object
			if (!tree.Style.ShowBranchLines) {
				for(int i = 0; i < Depth; i++) {
					yield return new Rune(' ');
				}

				yield break;
			}

			// yield indentations with runes appropriate to the state of the parents
			foreach(var cur in GetParentBranches().Reverse())
			{
				if(cur.IsLast())
					yield return new Rune(' ');
				else
					yield return driver.VLine;

				yield return new Rune(' ');
			}

			if(IsLast())
				yield return driver.LLCorner;
			else
				yield return driver.LeftTee;
		}

		/// <summary>
		/// Returns all parents starting with the immediate parent and ending at the root
		/// </summary>
		/// <returns></returns>
		private IEnumerable<Branch<T>> GetParentBranches()
		{
			var cur = Parent;

			while(cur != null)
			{
				yield return cur;
				cur = cur.Parent;
			}
		}

		/// <summary>
		/// Returns an appropriate symbol for displaying next to the string representation of the <see cref="Model"/> object to indicate whether it <see cref="IsExpanded"/> or not (or it is a leaf)
		/// </summary>
		/// <param name="driver"></param>
		/// <returns></returns>
		public Rune GetExpandableSymbol(ConsoleDriver driver)
		{
			var leafSymbol = tree.Style.ShowBranchLines ? driver.HLine : ' ';

			if(IsExpanded)
				return tree.Style.CollapseableSymbol ?? leafSymbol;

			if(CanExpand())
				return tree.Style.ExpandableSymbol ?? leafSymbol;

			return leafSymbol;
		}

		/// <summary>
		/// Returns true if the current branch can be expanded according to the <see cref="TreeBuilder{T}"/> or cached children already fetched
		/// </summary>
		/// <returns></returns>
		public bool CanExpand ()
		{
			// if we do not know the children yet
			if(ChildBranches == null) {
			
				//if there is a rapid method for determining whether there are children
				if(tree.TreeBuilder.SupportsCanExpand) {
					return tree.TreeBuilder.CanExpand(Model);
				}
				
				//there is no way of knowing whether we can expand without fetching the children
				FetchChildren();
			}

			//we fetched or already know the children, so return whether we have any
			return ChildBranches.Any();
		}

		/// <summary>
		/// Expands the current branch if possible
		/// </summary>
		public void Expand()
		{
			if(ChildBranches == null) {
				FetchChildren();
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
		/// <param name="startAtTop">True to also refresh all <see cref="Parent"/> branches (starting with the root)</param>
		public void Refresh (bool startAtTop)
		{
			// if we must go up and refresh from the top down
			if(startAtTop)
				Parent?.Refresh(true);

			// we don't want to loose the state of our children so lets be selective about how we refresh
			//if we don't know about any children yet just use the normal method
			if(ChildBranches == null)
				FetchChildren();
			else {
				// we already knew about some children so preserve the state of the old children

				// first gather the new Children
				var newChildren = tree.TreeBuilder?.GetChildren(this.Model) ?? Enumerable.Empty<T>();

				// Children who no longer appear need to go
				foreach(var toRemove in ChildBranches.Keys.Except(newChildren).ToArray())
				{
					ChildBranches.Remove(toRemove);
					
					//also if the user has this node selected (its disapearing) so lets change selection to us (the parent object) to be helpful
					if(Equals(tree.SelectedObject ,toRemove))
						tree.SelectedObject = Model;
				}
				
				// New children need to be added
				foreach(var newChild in newChildren)
				{
					// If we don't know about the child yet we need a new branch
					if (!ChildBranches.ContainsKey (newChild)) {
						ChildBranches.Add(newChild,new Branch<T>(tree,this,newChild));
					}
					else{
						//we already have this object but update the reference anyway incase Equality match but the references are new
						ChildBranches[newChild].Model = newChild;
					}					
				}
			}
			
		}

		/// <summary>
		/// Calls <see cref="Refresh(bool)"/> on the current branch and all expanded children
		/// </summary>
		internal void Rebuild()
		{
			Refresh(false);

			// if we know about our children
			if(ChildBranches != null) {
				if(IsExpanded) {
					//if we are expanded we need to updatethe visible children
					foreach(var child in ChildBranches) {
						child.Value.Refresh(false);
					}
					
				}
				else {
					// we are not expanded so should forget about children because they may not exist anymore
					ChildBranches = null;
				}
			}
				
		}

		/// <summary>
		/// Returns true if this branch has parents and it is the last node of it's parents branches (or last root of the tree)
		/// </summary>
		/// <returns></returns>
		private bool IsLast()
		{
			if(Parent == null)
				return this == tree.roots.Values.LastOrDefault();

			return Parent.ChildBranches.Values.LastOrDefault() == this;
		}

		/// <summary>
		/// Returns true if the given x offset on the branch line is the +/- symbol.  Returns false if not showing expansion symbols or leaf node etc
		/// </summary>
		/// <param name="driver"></param>
		/// <param name="x"></param>
		/// <returns></returns>
		internal bool IsHitOnExpandableSymbol (ConsoleDriver driver, int x)
		{
			// if leaf node then we cannot expand
			if(!CanExpand())
				return false;


			// if we could theoretically expand
			if(!IsExpanded && tree.Style.ExpandableSymbol != null) {
				return x == GetLinePrefix(driver).Count();
			}

			// if we could theoretically collapse
			if(IsExpanded && tree.Style.CollapseableSymbol != null) {
				return x == GetLinePrefix(driver).Count();
			}

			return false;
		}

	}

	/// <summary>
	/// Delegates of this type are used to fetch string representations of user's model objects
	/// </summary>
	/// <param name="model"></param>
	/// <returns></returns>
	public delegate string AspectGetterDelegate<T>(T model) where T:class;
	
	/// <summary>
	/// Event arguments describing a change in selected object in a tree view
	/// </summary>
	public class SelectionChangedEventArgs<T> : EventArgs where T:class
	{
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
		/// Creates a new instance of event args describing a change of selection in <paramref name="tree"/>
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		public SelectionChangedEventArgs(TreeView<T> tree, T oldValue, T newValue)
		{
			Tree = tree;
			OldValue = oldValue;
			NewValue = newValue;
		}
	}
}