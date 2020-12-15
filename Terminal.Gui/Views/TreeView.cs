// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls by phillip.piper@gmail.com).  Phillip has explicitly granted permission for his design and code to be used in this library under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Interface to implement when you want <see cref="TreeView"/> to automatically determine children for your class
	/// </summary>
	public interface ITreeNode
	{
		/// <summary>
		/// The children of your class which should be rendered underneath it when expanded
		/// </summary>
		/// <value></value>
		IList<ITreeNode> Children {get;}

		/// <summary>
		/// The textual representation to be rendered when your class is visible in the tree
		/// </summary>
		/// <value></value>
		string Text {get;}
	}

	/// <summary>
	/// Simple class for representing nodes of a <see cref="TreeView"/>
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
			return Text;
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
	/// Interface for supplying data to a <see cref="TreeView"/> on demand as root level nodes are expanded by the user
	/// </summary>
	public interface ITreeBuilder
	{
		/// <summary>
		/// Returns true if <see cref="CanExpand"/> is implemented by this class
		/// </summary>
		/// <value></value>
		bool SupportsCanExpand {get;}

		/// <summary>
		/// Returns true/false for whether a model has children.  This method should be implemented when <see cref="GetChildren"/> is an expensive operation otherwise <see cref="SupportsCanExpand"/> should return false (in which case this method will not be called)
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		bool CanExpand(object model);

		/// <summary>
		/// Returns all children of a given <paramref name="model"/> which should be added to the tree as new branches underneath it
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		IEnumerable<object> GetChildren(object model);
	}

	/// <summary>
	/// Abstract implementation of <see cref="ITreeBuilder"/>
	/// </summary>
	public abstract class TreeBuilder : ITreeBuilder {

		/// <inheritdoc/>
		public bool SupportsCanExpand { get; protected set;} = false;

		/// <summary>
		/// Override this method to return a rapid answer as to whether <see cref="GetChildren(object)"/> returns results.
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public virtual bool CanExpand (object model){
			
			return GetChildren(model).Any();
		}

		/// <inheritdoc/>
		public abstract IEnumerable<object> GetChildren (object model);

		/// <summary>
		/// Constructs base and initializes <see cref="SupportsCanExpand"/>
		/// </summary>
		/// <param name="supportsCanExpand">Pass true if you intend to implement <see cref="CanExpand(object)"/> otherwise false</param>
		public TreeBuilder(bool supportsCanExpand)
		{
			SupportsCanExpand = supportsCanExpand;
		}
	}

	/// <summary>
	/// <see cref="ITreeBuilder"/> implementation for <see cref="TreeNode"/> objects
	/// </summary>
	public class TreeNodeBuilder : TreeBuilder {
		
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
		public override IEnumerable<object> GetChildren (object model)
		{
			return model is ITreeNode n ? n.Children : Enumerable.Empty<object>();
		}
	}

	/// <summary>
	/// Implementation of <see cref="ITreeBuilder"/> that uses user defined functions
	/// </summary>
	public class DelegateTreeBuilder : TreeBuilder
	{
		private Func<object,IEnumerable<object>> childGetter;
		private Func<object,bool> canExpand;

		/// <summary>
		/// Constructs an implementation of <see cref="ITreeBuilder"/> that calls the user defined method <paramref name="childGetter"/> to determine children
		/// </summary>
		/// <param name="childGetter"></param>
		/// <returns></returns>
   		public DelegateTreeBuilder(Func<object,IEnumerable<object>> childGetter) : base(false)
		{
			this.childGetter = childGetter;
		}

		/// <summary>
		/// Constructs an implementation of <see cref="ITreeBuilder"/> that calls the user defined method <paramref name="childGetter"/> to determine children and <paramref name="canExpand"/> to determine expandability
		/// </summary>
		/// <param name="childGetter"></param>
		/// <param name="canExpand"></param>
		/// <returns></returns>
		public DelegateTreeBuilder(Func<object,IEnumerable<object>> childGetter, Func<object,bool> canExpand) : base(true)
		{
			this.childGetter = childGetter;
			this.canExpand = canExpand;
		}

		/// <summary>
		/// Returns whether a node can be expanded based on the delegate passed during construction
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public override bool CanExpand (object model)
		{
			return canExpand?.Invoke(model) ?? base.CanExpand (model);
		}

		/// <summary>
		/// Returns children using the delegate method passed during construction
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public override IEnumerable<object> GetChildren (object model)
		{
			return childGetter.Invoke(model);
		}
	}


	/// <summary>
	/// Hierarchical tree view with expandable branches.  Branch objects are dynamically determined when expanded using a user defined <see cref="ITreeBuilder"/>
	/// </summary>
	public class TreeView : View
	{   
		private int scrollOffset;

		/// <summary>
		/// True to render vertical lines under expanded nodes to show which node belongs to which parent.  False to use only whitespace
		/// </summary>
		/// <value></value>
		public bool ShowBranchLines {get;set;} = true;

		/// <summary>
		/// Determines how sub branches of the tree are dynamically built at runtime as the user expands root nodes
		/// </summary>
		/// <value></value>
		public ITreeBuilder TreeBuilder { get;set;}

		/// <summary>
		/// private variable for <see cref="SelectedObject"/>
		/// </summary>
		object selectedObject;

		/// <summary>
		/// The currently selected object in the tree
		/// </summary>
		public object SelectedObject { 
			get => selectedObject; 
			set {                
				var oldValue = selectedObject;
				selectedObject = value; 

				if(!ReferenceEquals(oldValue,value))
					SelectionChanged?.Invoke(this,new SelectionChangedEventArgs(this,oldValue,value));
			}
		}
		
		/// <summary>
		/// Called when the <see cref="SelectedObject"/> changes
		/// </summary>
		public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

		/// <summary>
		/// Refreshes the state of the object <paramref name="o"/> in the tree.  This will recompute children, string representation etc
		/// </summary>
		/// <remarks>This has no effect if the object is not exposed in the tree.</remarks>
		/// <param name="o"></param>
		/// <param name="startAtTop">True to also refresh all ancestors of the objects branch (starting with the root).  False to refresh only the passed node</param>
		public void RefreshObject (object o, bool startAtTop = false)
		{
			var branch = ObjectToBranch(o);
			if(branch != null) {
				branch.Refresh(startAtTop);
				SetNeedsDisplay();
			}

		}
		
		/// <summary>
		/// Rebuilds the tree structure for all exposed objects starting with the root objects.  Call this method when you know there are changes to the tree but don't know which objects have changed (otherwise use <see cref="RefreshObject(object, bool)"/>)
		/// </summary>
		public void RebuildTree()
		{
			foreach(var branch in roots.Values)
				branch.Rebuild();
			
			SetNeedsDisplay();
		}

		/// <summary>
		/// The root objects in the tree, note that this collection is of root objects only
		/// </summary>
		public IEnumerable<object> Objects {get=>roots.Keys;}

		/// <summary>
		/// Map of root objects to the branches under them.  All objects have a <see cref="Branch"/> even if that branch has no children
		/// </summary>
		internal Dictionary<object,Branch> roots {get; set;} = new Dictionary<object, Branch>();

		/// <summary>
		/// The amount of tree view that has been scrolled off the top of the screen (by the user scrolling down)
		/// </summary>
		/// <remarks>Setting a value of less than 0 will result in a ScrollOffset of 0.  To see changes in the UI call <see cref="View.SetNeedsDisplay()"/></remarks>
		public int ScrollOffset { 
			get => scrollOffset;
			set {
				scrollOffset = Math.Max(0,value); 
			}
		}


		/// <summary>
		/// Creates a new tree view with absolute positioning.  Use <see cref="AddObjects(IEnumerable{object})"/> to set set root objects for the tree.  
		/// </summary>
		public TreeView():base()
		{
			CanFocus = true;
			TreeBuilder = new TreeNodeBuilder();
		}

		/// <summary>
		/// Initialises <see cref="TreeBuilder"/>.Creates a new tree view with absolute positioning.  Use <see cref="AddObjects(IEnumerable{object})"/> to set set root objects for the tree.
		/// </summary>
		public TreeView(ITreeBuilder builder) : this()
		{
			TreeBuilder = builder;
		}

		/// <summary>
		/// Adds a new root level object unless it is already a root of the tree
		/// </summary>
		/// <param name="o"></param>
		public void AddObject(object o)
		{
			if(!roots.ContainsKey(o)) {
				roots.Add(o,new Branch(this,null,o));
				SetNeedsDisplay();
			}
		}

		/// <summary>
		/// Removes all objects from the tree and clears <see cref="SelectedObject"/>
		/// </summary>
		public void ClearObjects()
		{
			SelectedObject = null;
			roots = new Dictionary<object, Branch>();
			SetNeedsDisplay();
		}

		/// <summary>
		/// Removes the given root object from the tree
		/// </summary>
		/// <remarks>If <paramref name="o"/> is the currently <see cref="SelectedObject"/> then the selection is cleared</remarks>
		/// <param name="o"></param>
		public void Remove(object o)
		{
			if(roots.ContainsKey(o)) {
				roots.Remove(o);
				SetNeedsDisplay();

				if(Equals(SelectedObject,o))
					SelectedObject = null;
			}
		}
		
		/// <summary>
		/// Adds many new root level objects.  Objects that are already root objects are ignored
		/// </summary>
		/// <param name="collection">Objects to add as new root level objects</param>
		public void AddObjects(IEnumerable<object> collection)
		{
			bool objectsAdded = false;

			foreach(var o in collection) {
				if (!roots.ContainsKey (o)) {
					roots.Add(o,new Branch(this,null,o));
					objectsAdded = true;
				}	
			}
				
			if(objectsAdded)
				SetNeedsDisplay();
		}

		/// <summary>
		/// Returns the currently expanded children of the passed object.  Returns an empty collection if the branch is not exposed or not expanded
		/// </summary>
		/// <param name="o">An object in the tree</param>
		/// <returns></returns>
		public IEnumerable<object> GetChildren (object o)
		{
			var branch = ObjectToBranch(o);

			if(branch == null || !branch.IsExpanded)
				return new object[0];

			return branch.ChildBranches?.Values?.Select(b=>b.Model)?.ToArray() ?? new object[0];
		}
		/// <summary>
		/// Returns the parent object of <paramref name="o"/> in the tree.  Returns null if the object is not exposed in the tree
		/// </summary>
		/// <param name="o">An object in the tree</param>
		/// <returns></returns>
		public object GetParent (object o)
		{
			return ObjectToBranch(o)?.Parent?.Model;
		}

		/// <summary>
		/// Returns the string representation of model objects hosted in the tree.  Default implementation is to call <see cref="object.ToString"/>
		/// </summary>
		/// <value></value>
		public AspectGetterDelegate AspectGetter {get;set;} = (o)=>o.ToString();

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if(roots == null)
				return;

			var map = BuildLineMap();

			for(int line = 0 ; line < bounds.Height; line++){

				var idxToRender = ScrollOffset + line;

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
		/// Returns the index of the object <paramref name="o"/> if it is currently exposed (it's parent(s) have been expanded).  This can be used with <see cref="ScrollOffset"/> and <see cref="View.SetNeedsDisplay()"/> to scroll to a specific object
		/// </summary>
		/// <remarks>Uses the Equals method and returns the first index at which the object is found or -1 if it is not found</remarks>
		/// <param name="o">An object that appears in your tree and is currently exposed</param>
		/// <returns>The index the object was found at or -1 if it is not currently revealed or not in the tree at all</returns>
		public int GetScrollOffsetOf(object o)
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
		/// Calculates all currently visible/expanded branches (including leafs) and outputs them by index from the top of the screen
		/// </summary>
		/// <remarks>Index 0 of the returned array is the first item that should be visible in the top of the control, index 1 is the next etc.</remarks>
		/// <returns></returns>
		private Branch[] BuildLineMap()
		{
			List<Branch> toReturn = new List<Branch>();

			foreach(var root in roots.Values) {
				toReturn.AddRange(AddToLineMap(root));
			}

			return toReturn.ToArray();
		}

		private IEnumerable<Branch> AddToLineMap (Branch currentBranch)
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

		/// <summary>
		/// Symbol to use for expanded branch nodes to indicate to the user that they can be collapsed.  Defaults to '-'
		/// </summary>
		public Rune ExpandedSymbol {get;set;} = '-';

		/// <summary>
		/// Symbol to use for branch nodes that can be expanded to indicate this to the user.  Defaults to '+'
		/// </summary>
		public Rune ExpandableSymbol {get;set;} = '+';

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
					SetNeedsDisplay();
				}
			}
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to the first root object and resets the <see cref="ScrollOffset"/> to 0
		/// </summary>
		public void GoToFirst()
		{
			ScrollOffset = 0;
			SelectedObject = roots.Keys.FirstOrDefault();

			SetNeedsDisplay();
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to the last object in the tree and scrolls so that it is visible
		/// </summary>
		public void GoToEnd ()
		{
			var map = BuildLineMap();
			ScrollOffset = Math.Max(0,map.Length - Bounds.Height +1);
			SelectedObject = map.Last().Model;
						
			SetNeedsDisplay();
		}

		/// <summary>
		/// Changes the selected object by a number of screen lines
		/// </summary>
		/// <remarks>If nothing is currently selected the first root is selected.  If the selected object is no longer in the tree the first object is selected</remarks>
		/// <param name="offset"></param>
		private void AdjustSelection (int offset)
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

					
					if(newIdx < ScrollOffset) {
						//if user has scrolled up too far to see their selection
						ScrollOffset = newIdx;
					}
					else if(newIdx >= ScrollOffset + Bounds.Height){
						
						//if user has scrolled off bottom of visible tree
						ScrollOffset = Math.Max(0,(newIdx+1) - Bounds.Height);

					}
				}

			}
						
			SetNeedsDisplay();
		}

		/// <summary>
		/// Expands the supplied object if it is contained in the tree (either as a root object or as an exposed branch object)
		/// </summary>
		/// <param name="toExpand">The object to expand</param>
		public void Expand(object toExpand)
		{
			if(toExpand == null)
				return;
			
			ObjectToBranch(toExpand)?.Expand();
			SetNeedsDisplay();
		}

		/// <summary>
		/// Returns true if the given object <paramref name="o"/> is exposed in the tree and expanded otherwise false
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public bool IsExpanded(object o)
		{
			return ObjectToBranch(o)?.IsExpanded ?? false;
		}

		/// <summary>
		/// Collapses the supplied object if it is currently expanded 
		/// </summary>
		/// <param name="toCollapse">The object to collapse</param>
		public void Collapse(object toCollapse)
		{
			if(toCollapse == null)
				return;

			ObjectToBranch(toCollapse)?.Collapse();
			SetNeedsDisplay();
		}

		/// <summary>
		/// Returns the corresponding <see cref="Branch"/> in the tree for <paramref name="toFind"/>.  This will not work for objects hidden by their parent being collapsed
		/// </summary>
		/// <param name="toFind"></param>
		/// <returns>The branch for <paramref name="toFind"/> or null if it is not currently exposed in the tree</returns>
		private Branch ObjectToBranch(object toFind)
		{
			return BuildLineMap().FirstOrDefault(o=>o.Model.Equals(toFind));
		}
	}

	class Branch
	{
		/// <summary>
		/// True if the branch is expanded to reveal child branches
		/// </summary>
		public bool IsExpanded {get;set;}

		/// <summary>
		/// The users object that is being displayed by this branch of the tree
		/// </summary>
		public object Model {get;private set;}
		
		/// <summary>
		/// The depth of the current branch.  Depth of 0 indicates root level branches
		/// </summary>
		public int Depth {get;private set;} = 0;

		/// <summary>
		/// The children of the current branch.  This is null until the first call to <see cref="FetchChildren"/> to avoid enumerating the entire underlying hierarchy
		/// </summary>
		public Dictionary<object,Branch> ChildBranches {get;set;}

		/// <summary>
		/// The parent <see cref="Branch"/> or null if it is a root.
		/// </summary>
		public Branch Parent {get; private set;}

		private TreeView tree;
		
		/// <summary>
		/// Declares a new branch of <paramref name="tree"/> in which the users object <paramref name="model"/> is presented
		/// </summary>
		/// <param name="tree">The UI control in which the branch resides</param>
		/// <param name="parentBranchIfAny">Pass null for root level branches, otherwise pass the parent</param>
		/// <param name="model">The user's object that should be displayed</param>
		public Branch(TreeView tree,Branch parentBranchIfAny,object model)
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

			var children = tree.TreeBuilder.GetChildren(this.Model) ?? Enumerable.Empty<object>();

			this.ChildBranches = children.ToDictionary(k=>k,val=>new Branch(tree,this,val));
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
			// Everything on line before the expansion run and branch text
			Rune[] prefix = GetLinePrefix(driver).ToArray();
			Rune expansion = GetExpandableIcon(driver);
			string lineBody = tree.AspectGetter(Model);

			var remainingWidth = availableWidth - (prefix.Length + 1 + lineBody.Length);
			            
			tree.Move(0,y);

			driver.SetAttribute(colorScheme.Normal);

			foreach(Rune r in prefix)
				driver.AddRune(r);

			driver.AddRune(expansion);

			driver.SetAttribute(tree.SelectedObject == Model ?
				colorScheme.HotFocus :
				colorScheme.Normal);

			driver.AddStr(lineBody);

			driver.SetAttribute(colorScheme.Normal);

			if(remainingWidth > 0)
				driver.AddStr(new string(' ',remainingWidth));
		}

		/// <summary>
		/// Gets all characters to render prior to the current branches line.  This includes indentation whitespace and any tree branches (if enabled)
		/// </summary>
		/// <param name="driver"></param>
		/// <returns></returns>
		private IEnumerable<Rune> GetLinePrefix (ConsoleDriver driver)
		{
			// If not showing line branches or this is a root object
			if (!tree.ShowBranchLines) {
				for(int i = 0; i < Depth; i++) {
					yield return new Rune(' ');
				}
			}

			// yield indentations with runes appropriate to the state of the parents
			foreach(var cur in GetParentBranches().Reverse())
			{
				if(cur.IsLast())
					yield return new Rune(' ');
				else
					yield return driver.VLine;
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
		private IEnumerable<Branch> GetParentBranches()
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
		public Rune GetExpandableIcon(ConsoleDriver driver)
		{
			if(IsExpanded)
				return tree.ExpandedSymbol;

			var leafSymbol = tree.ShowBranchLines ? driver.HLine : ' ';

			if(ChildBranches == null) {
			
				//if there is a rapid method for determining whether there are children
				if(tree.TreeBuilder.SupportsCanExpand) {
					return tree.TreeBuilder.CanExpand(Model) ? tree.ExpandableSymbol : leafSymbol;
				}
				
				//there is no way of knowing whether we can expand without fetching the children
				FetchChildren();
			}

			//we fetched or already know the children, so return whether we are a leaf or a expandable branch
			return ChildBranches.Any() ? tree.ExpandableSymbol : leafSymbol;
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
				var newChildren = tree.TreeBuilder?.GetChildren(this.Model) ?? Enumerable.Empty<object>();

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
						ChildBranches.Add(newChild,new Branch(tree,this,newChild));
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
	}

	/// <summary>
	/// Delegates of this type are used to fetch string representations of user's model objects
	/// </summary>
	/// <param name="model"></param>
	/// <returns></returns>
	public delegate string AspectGetterDelegate(object model);
	
	/// <summary>
	/// Event arguments describing a change in selected object in a tree view
	/// </summary>
	public class SelectionChangedEventArgs : EventArgs
	{
		/// <summary>
		/// The view in which the change occurred
		/// </summary>
		public TreeView Tree { get; }

		/// <summary>
		/// The previously selected value (can be null)
		/// </summary>
		public object OldValue { get; }

		/// <summary>
		/// The newly selected value in the <see cref="Tree"/> (can be null)
		/// </summary>
		public object NewValue { get; }

		/// <summary>
		/// Creates a new instance of event args describing a change of selection in <paramref name="tree"/>
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		public SelectionChangedEventArgs(TreeView tree, object oldValue, object newValue)
		{
			Tree = tree;
			OldValue = oldValue;
			NewValue = newValue;
		}
	}
}