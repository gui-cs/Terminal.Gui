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


		[Fact]
		public void GetScrollOffsetOf_MinusOneForUnRevealed()
		{
			var tree = CreateTree(out Factory f, out Car c1, out Car c2);
			
			Assert.Equal(0,tree.GetScrollOffsetOf(f));
			Assert.Equal(-1,tree.GetScrollOffsetOf(c1));
			Assert.Equal(-1,tree.GetScrollOffsetOf(c2));

			//reveal it by expanding the root object
			tree.Expand(f);
			
			Assert.Equal(0,tree.GetScrollOffsetOf(f));
			Assert.Equal(1,tree.GetScrollOffsetOf(c1));
			Assert.Equal(2,tree.GetScrollOffsetOf(c2));

			tree.Collapse(f);
			
			Assert.Equal(0,tree.GetScrollOffsetOf(f));
			Assert.Equal(-1,tree.GetScrollOffsetOf(c1));
			Assert.Equal(-1,tree.GetScrollOffsetOf(c2));
		}
	}
}
