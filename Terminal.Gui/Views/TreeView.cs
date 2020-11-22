// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls by phillip.piper@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {

	public class TreeView : View
	{   
		/// <summary>
		/// Default implementation of a <see cref="ChildrenGetterDelegate"/>, returns an empty collection (i.e. no children)
		/// </summary>
		static ChildrenGetterDelegate DefaultChildrenGetter = (s)=>{return new object[0];};

		/// <summary>
		/// This is the delegate that will be used to fetch the children of a model object
		/// </summary>
		public ChildrenGetterDelegate ChildrenGetter {
			get { return childrenGetter ?? DefaultChildrenGetter; }
			set { childrenGetter = value; }
		}
	
		private ChildrenGetterDelegate childrenGetter;

		/// <summary>
		/// The currently selected object in the tree
		/// </summary>
		public object SelectedObject {get;set;}

		/// <summary>
		/// The root objects in the tree, note that this collection is of root objects only
		/// </summary>
		public IEnumerable<object> Objects {get=>roots.Keys;}

		/// <summary>
		/// Map of root objects to the branches under them.  All objects have a <see cref="Branch"/> even if that branch has no children
		/// </summary>
		Dictionary<object,Branch> roots {get; set;} = new Dictionary<object, Branch>();

		/// <summary>
		/// The amount of tree view that has been scrolled off the top of the screen (by the user scrolling down)
		/// </summary>
		public int ScrollOffset {get; private set;}

		public TreeView ()
		{
			CanFocus = true;
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
		/// Adds many new root level objects.  Objects that are already root objects are ignored
		/// </summary>
		/// <param name="o"></param>
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
		/// Returns the string representation of model objects hosted in the tree.  Default implementation is to call <see cref="object.ToString"/>
		/// </summary>
		/// <value></value>
		public Func<object,string> AspectGetter {get;set;} = (o)=>o.ToString();

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

		public char ExpandedSymbol {get;set;} = '-';
		public char ExpandableSymbol {get;set;} = '+';
		public char LeafSymbol {get;set;} = ' ';

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			switch (keyEvent.Key) {
				case Key.CursorRight:
					Expand(SelectedObject);
				break;
				case Key.CursorLeft:
					Collapse(SelectedObject);
				break;
			
				case Key.CursorUp:
					AdjustSelection(-1);
				break;
				case Key.CursorDown:
					AdjustSelection(1);
				break;
			}

			PositionCursor ();
			return true;
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
						ScrollOffset = Math.Max(0,newIdx - Bounds.Height);

					}
				}

			}
						
			SetNeedsDisplay();
		}

		private void Expand(object selectedObject)
		{
			if(selectedObject == null)
			return;

			ObjectToBranch(selectedObject).IsExpanded = true;
			SetNeedsDisplay();
		}

		private void Collapse(object selectedObject)
		{
			if(selectedObject == null)
			return;

			ObjectToBranch(selectedObject).IsExpanded = false;
			SetNeedsDisplay();
		}

		/// <summary>
		/// Returns the corresponding <see cref="Branch"/> in the tree for <paramref name="toFind"/>.  This will not work for objects hidden by their parent being collapsed
		/// </summary>
		/// <param name="toFind"></param>
		/// <returns></returns>
		private Branch ObjectToBranch(object toFind)
		{
			return BuildLineMap().FirstOrDefault(o=>o.Model.Equals(toFind));
		}
	}

	class Branch
	{
		public bool IsExpanded {get;set;}
		public Object Model{get;set;}
		
		public int Depth {get;set;} = 0;
		public Dictionary<object,Branch> ChildBranches {get;set;}

		private TreeView tree;

		public Branch(TreeView tree,Branch parentBranchIfAny,Object model)
		{
			this.tree  = tree;
			this.Model = model;
			
			if(parentBranchIfAny != null) {
				Depth = parentBranchIfAny.Depth +1;
			}
		}


		/// <summary>
		/// Fetch the children of this branch. This method populates <see cref="ChildBranches"/>
		/// </summary>
		public virtual void FetchChildren()
		{
			if (tree.ChildrenGetter == null)
			return;

			this.ChildBranches = tree.ChildrenGetter(this.Model).ToDictionary(k=>k,val=>new Branch(tree,this,val));
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
			string representation = new string(' ',Depth) + GetExpandableIcon() + tree.AspectGetter(Model);
            
			tree.Move(0,y);

			driver.SetAttribute(tree.SelectedObject == Model ?
				colorScheme.HotFocus :
				colorScheme.Normal);

			driver.AddStr(representation.PadRight(availableWidth));
		}

		char GetExpandableIcon()
		{
			if(IsExpanded)
				return tree.ExpandedSymbol;

			if(ChildBranches == null)
				FetchChildren();

			return ChildBranches.Any() ? tree.ExpandableSymbol : tree.LeafSymbol;
		}
	}
   
	/// <summary>
	/// Delegates of this type are used to fetch the children of the given model object
	/// </summary>
	/// <param name="model">The parent whose children should be fetched</param>
	/// <returns>An enumerable over the children</returns>
	public delegate IEnumerable<object> ChildrenGetterDelegate(object model);
}