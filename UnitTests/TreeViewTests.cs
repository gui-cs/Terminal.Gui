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
	}
}
