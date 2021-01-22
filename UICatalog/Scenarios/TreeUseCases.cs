using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Tree View", Description: "Simple tree view examples")]
	[ScenarioCategory ("Controls")]
	class TreeUseCases : Scenario {

        View currentTree;

	    public override void Setup ()
		{
			Win.Title = this.GetName();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();
			
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_Scenarios", new MenuItem [] {
					new MenuItem ("_Simple Nodes", "", () => LoadSimpleNodes()),
					new MenuItem ("_Rooms", "", () => LoadRooms()),
				}),
			});
			
            Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});

			Top.Add (statusBar);

            // Start with the most basic use case
            LoadSimpleNodes();
        }

        // Your data class
		private class House : ITreeNode {


            // Your properties
            public string Address {get;set;}
            public List<Room> Rooms {get;set;}

            // ITreeNode member:

			public IList<ITreeNode> Children => Rooms.Cast<ITreeNode>().ToList();
			
            public override string ToString ()
			{
				return Address;
			}
		}
		private class Room : ITreeNode{
            
            public string Name {get;set;}


            // Rooms have no sub objects
			public IList<ITreeNode> Children => new List<ITreeNode>();

			public override string ToString ()
			{
				return Name;
			}
        }

		private void LoadRooms()
		{
            var myHouse = new House()
            {
                Address = "23 Nowhere Street",
                Rooms = new List<Room>{
                    new Room(){Name = "Ballroom"},
                    new Room(){Name = "Bedroom 1"},
                    new Room(){Name = "Bedroom 2"}
                }
            };

            if(currentTree != null)
                Win.Remove(currentTree);

			var tree = new TreeView()
			{
                X = 0,
                Y = 0,
				Width = 40,
				Height = 20
			};

            Win.Add(tree);

            tree.AddObject(myHouse);

            currentTree = tree;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private void LoadSimpleNodes()
		{
            if(currentTree != null)
                Win.Remove(currentTree);

			var tree = new TreeView()
			{
                X = 0,
                Y = 0,
				Width = 40,
				Height = 20
			};

            Win.Add(tree);

			var root1 = new TreeNode("Root1");
			root1.Children.Add(new TreeNode("Child1.1"));
			root1.Children.Add(new TreeNode("Child1.2"));

			var root2 = new TreeNode("Root2");
			root2.Children.Add(new TreeNode("Child2.1"));
			root2.Children.Add(new TreeNode("Child2.2"));
		
			tree.AddObject(root1);
			tree.AddObject(root2);

            currentTree = tree;

		}
	}
}
