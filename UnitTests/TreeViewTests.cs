using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;

namespace UnitTests {
	public class TreeViewTests 
	{
		#region Test Setup Methods
		class Factory
		{
			public Car[] Cars {get;set;}
		};
		class Car {

		};
		
		private TreeView CreateTree()
		{
			return CreateTree(out _, out _, out _);
		}

		private TreeView CreateTree(out Factory factory1, out Car car1, out Car car2)
		{
			car1 = new Car();
			car2 = new Car();

			factory1 = new Factory()
			{
				Cars = new []{car1 ,car2}
			};
			
			var tree = new TreeView();
			tree.ChildrenGetter = (s)=> s is Factory f ? f.Cars: null;
			tree.AddObject(factory1);

			return tree;
		}
		#endregion
		
		/// <summary>
		/// Tests that <see cref="TreeView.Expand(object)"/> and <see cref="TreeView.IsExpanded(object)"/> are consistent
		/// </summary>
		[Fact]
		public void IsExpanded_TrueAfterExpand()
		{
			var tree = CreateTree(out Factory f, out _, out _);
			Assert.False(tree.IsExpanded(f));

			tree.Expand(f);
			Assert.True(tree.IsExpanded(f));

			tree.Collapse(f);
			Assert.False(tree.IsExpanded(f));
		}

		/// <summary>
		/// Tests that <see cref="TreeView.IsExpanded(object)"/> and <see cref="TreeView.Expand(object)"/> behaves correctly when an object cannot be expanded (because it has no children)
		/// </summary>
		[Fact]
		public void IsExpanded_FalseIfCannotExpand()
		{
			var tree = CreateTree(out Factory f, out Car c, out _);
			
			// expose the car by expanding the factory
			tree.Expand(f);

			// car is not expanded
			Assert.False(tree.IsExpanded(c));

			//try to expand the car (should have no effect because cars have no children)
			tree.Expand(c);
			
			Assert.False(tree.IsExpanded(c));

			// should also be ignored
			tree.Collapse(c);

			Assert.False(tree.IsExpanded(c));
		}

		/// <summary>
		/// Tests illegal ranges for <see cref="TreeView.ScrollOffset"/>
		/// </summary>
		[Fact]
		public void ScrollOffset_CannotBeNegative()
		{
			var tree = CreateTree();

			Assert.Equal(0,tree.ScrollOffset);

			tree.ScrollOffset = -100;
			Assert.Equal(0,tree.ScrollOffset);
			
			tree.ScrollOffset = 10;
			Assert.Equal(10,tree.ScrollOffset);
		}


		/// <summary>
		/// Tests <see cref="TreeView.GetScrollOffsetOf(object)"/> for objects that are as yet undiscovered by the tree
		/// </summary>
		[Fact]
		public void GetScrollOffsetOf_MinusOneForUnRevealed()
		{
			var tree = CreateTree(out Factory f, out Car c1, out Car c2);
			
			// to start with the tree is collapsed and only knows about the root object
			Assert.Equal(0,tree.GetScrollOffsetOf(f));
			Assert.Equal(-1,tree.GetScrollOffsetOf(c1));
			Assert.Equal(-1,tree.GetScrollOffsetOf(c2));

			// reveal it by expanding the root object
			tree.Expand(f);
			
			// tree now knows about children
			Assert.Equal(0,tree.GetScrollOffsetOf(f));
			Assert.Equal(1,tree.GetScrollOffsetOf(c1));
			Assert.Equal(2,tree.GetScrollOffsetOf(c2));

			// after collapsing the root node again
			tree.Collapse(f);
			
			// tree no longer knows about the locations of these objects
			Assert.Equal(0,tree.GetScrollOffsetOf(f));
			Assert.Equal(-1,tree.GetScrollOffsetOf(c1));
			Assert.Equal(-1,tree.GetScrollOffsetOf(c2));
		}

		/// <summary>
		/// Simulates behind the scenes changes to an object (which children it has) and how to sync that into the tree using <see cref="TreeView.RefreshObject(object, bool)"/>
		/// </summary>
		[Fact]
		public void RefreshObject_ChildRemoved()
		{
			var tree = CreateTree(out Factory f, out Car c1, out Car c2);
			
			//reveal it by expanding the root object
			tree.Expand(f);
			
			Assert.Equal(0,tree.GetScrollOffsetOf(f));
			Assert.Equal(1,tree.GetScrollOffsetOf(c1));
			Assert.Equal(2,tree.GetScrollOffsetOf(c2));
			
			// Factory now no longer makes Car c1 (only c2)
			f.Cars = new Car[]{c2};

			// Tree does not know this yet
			Assert.Equal(0,tree.GetScrollOffsetOf(f));
			Assert.Equal(1,tree.GetScrollOffsetOf(c1));
			Assert.Equal(2,tree.GetScrollOffsetOf(c2));

			// If the user has selected the node c1
			tree.SelectedObject = c1;

			// When we refresh the tree
			tree.RefreshObject(f);

			// Now tree knows that factory has only one child node c2
			Assert.Equal(0,tree.GetScrollOffsetOf(f));
			Assert.Equal(-1,tree.GetScrollOffsetOf(c1));
			Assert.Equal(1,tree.GetScrollOffsetOf(c2));

			// The old selection was c1 which is now gone so selection should default to the parent of that branch (the factory)
			Assert.Equal(f,tree.SelectedObject);
		}

		/// <summary>
		/// Tests that <see cref="TreeView.GetParent(object)"/> returns the parent object for
		/// Cars (Factories).  Note that the method only works once the parent branch (Factory)
		/// is expanded to expose the child (Car)
		/// </summary>
		[Fact]
		public void GetParent_ReturnsParentOnlyWhenExpanded()
		{
			var tree = CreateTree(out Factory f, out Car c1, out Car c2);
			
			Assert.Null(tree.GetParent(f));
			Assert.Null(tree.GetParent(c1));
			Assert.Null(tree.GetParent(c2));

			// now when we expand the factory we discover the cars
			tree.Expand(f);
			
			Assert.Null(tree.GetParent(f));
			Assert.Equal(f,tree.GetParent(c1));
			Assert.Equal(f,tree.GetParent(c2));

			tree.Collapse(f);

			Assert.Null(tree.GetParent(f));
			Assert.Null(tree.GetParent(c1));
			Assert.Null(tree.GetParent(c2));
		}

		/// <summary>
		/// Tests how the tree adapts to changes in the ChildrenGetter delegate during runtime
		/// when some branches are expanded and the new delegate returns children for a node that
		/// previously didn't have any children
		/// </summary>
		[Fact]
		public void RefreshObject_AfterChangingChildrenGetterDuringRuntime()
		{
			var tree = CreateTree(out Factory f, out Car c1, out Car c2);
			
			string wheel = "Shiny Wheel";

			// Expand the Factory
			tree.Expand(f);
			
			// c1 cannot have children
			Assert.Equal(f,tree.GetParent(c1));

			// expanding it does nothing
			tree.Expand(c1);
			Assert.False(tree.IsExpanded(c1));

			// change the children getter so that now cars can have wheels
			tree.ChildrenGetter = (o)=>
				// factories have cars
				o is Factory ? new object[]{c1,c2} 
				// cars have wheels
				: new object[]{wheel };
			
			// still cannot expand
			tree.Expand(c1);
			Assert.False(tree.IsExpanded(c1));

			tree.RefreshObject(c1);
			tree.Expand(c1);
			Assert.True(tree.IsExpanded(c1));
			Assert.Equal(wheel,tree.GetChildren(c1).FirstOrDefault());
		}
		/// <summary>
		/// Same as <see cref="RefreshObject_AfterChangingChildrenGetterDuringRuntime"/> but
		/// uses <see cref="TreeView.RebuildTree()"/> instead of <see cref="TreeView.RefreshObject(object, bool)"/>
		/// </summary>
		[Fact]
		public void RebuildTree_AfterChangingChildrenGetterDuringRuntime()
		{
			var tree = CreateTree(out Factory f, out Car c1, out Car c2);
			
			string wheel = "Shiny Wheel";

			// Expand the Factory
			tree.Expand(f);
			
			// c1 cannot have children
			Assert.Equal(f,tree.GetParent(c1));

			// expanding it does nothing
			tree.Expand(c1);
			Assert.False(tree.IsExpanded(c1));

			// change the children getter so that now cars can have wheels
			tree.ChildrenGetter = (o)=>
				// factories have cars
				o is Factory ? new object[]{c1,c2} 
				// cars have wheels
				: new object[]{wheel };
			
			// still cannot expand
			tree.Expand(c1);
			Assert.False(tree.IsExpanded(c1));

			// Rebuild the tree
			tree.RebuildTree();
			
			// Rebuild should not have collapsed any branches or done anything wierd
			Assert.True(tree.IsExpanded(f));

			tree.Expand(c1);
			Assert.True(tree.IsExpanded(c1));
			Assert.Equal(wheel,tree.GetChildren(c1).FirstOrDefault());
		}
		/// <summary>
		/// Tests that <see cref="TreeView.GetChildren(object)"/> returns the child objects for
		/// the factory.  Note that the method only works once the parent branch (Factory)
		/// is expanded to expose the child (Car)
		/// </summary>
		[Fact]
		public void GetChildren_ReturnsChildrenOnlyWhenExpanded()
		{
			var tree = CreateTree(out Factory f, out Car c1, out Car c2);
			
			Assert.Empty(tree.GetChildren(f));
			Assert.Empty(tree.GetChildren(c1));
			Assert.Empty(tree.GetChildren(c2));

			// now when we expand the factory we discover the cars
			tree.Expand(f);
			
			Assert.Contains(c1,tree.GetChildren(f));
			Assert.Contains(c2,tree.GetChildren(f));
			Assert.Empty(tree.GetChildren(c1));
			Assert.Empty(tree.GetChildren(c2));

			tree.Collapse(f);

			Assert.Empty(tree.GetChildren(f));
			Assert.Empty(tree.GetChildren(c1));
			Assert.Empty(tree.GetChildren(c2));
		}

		[Fact]
		public void TreeNode_WorksWithoutDelegate()
		{
			var tree = new TreeView();

			var root = new TreeNode("Root");
			root.Children.Add(new TreeNode("Leaf1"));
			root.Children.Add(new TreeNode("Leaf2"));

			tree.AddObject(root);

			tree.Expand(root);
			Assert.Equal(2,tree.GetChildren(root).Count());
		}


		/// <summary>
		/// Simulates behind the scenes changes to an object (which children it has) and how to sync that into the tree using <see cref="TreeView.RefreshObject(object, bool)"/>
		/// </summary>
		[Fact]
		public void RefreshObject_EqualityTest()
		{
			var obj1 = new EqualityTestObject(){Name="Bob",Age=1 };
			var obj2 = new EqualityTestObject(){Name="Bob",Age=2 };;

			string root = "root";
			
			var tree = new TreeView();
			tree.ChildrenGetter = (s)=>  ReferenceEquals(s , root) ? new object[]{obj1 } : null;
			tree.AddObject(root);

			// Tree is not expanded so the root has no children yet
			Assert.Empty(tree.GetChildren(root));


			tree.Expand(root);

			// now that the tree is expanded we should get our child returned
			Assert.Equal(1,tree.GetChildren(root).Count(child=>ReferenceEquals(obj1,child)));

			// change the getter to return an Equal object (but not the same reference - obj2)
			tree.ChildrenGetter = (s)=>  ReferenceEquals(s , root) ? new object[]{obj2 } : null;

			// tree has cached the knowledge of what children the root has so won't know about the change (we still get obj1)
			Assert.Equal(1,tree.GetChildren(root).Count(child=>ReferenceEquals(obj1,child)));

			// now that we refresh the root we should get the new child reference (obj2)
			tree.RefreshObject(root);
			Assert.Equal(1,tree.GetChildren(root).Count(child=>ReferenceEquals(obj2,child)));

		}

		/// <summary>
		/// Test object which considers for equality only <see cref="Name"/>
		/// </summary>
		private class EqualityTestObject
		{
			public string Name { get;set;}
			public int Age { get;set;}

			public override int GetHashCode ()
			{
				return Name?.GetHashCode()??base.GetHashCode ();
			}
			public override bool Equals (object obj)
			{
				return obj is EqualityTestObject eto && Equals(Name, eto.Name);
			}
		}
	}
}
