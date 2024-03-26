using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tree View", "Simple tree view examples.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class TreeUseCases : Scenario
{
    private View _currentTree;

    public override void Setup ()
    {
        Win.Title = GetName ();
        Win.Y = 1; // menu
        Win.Height = Dim.Fill (1); // status bar

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem ("_File", new MenuItem [] { new ("_Quit", "", () => Quit ()) }),
                new MenuBarItem (
                                 "_Scenarios",
                                 new MenuItem []
                                 {
                                     new (
                                          "_Simple Nodes",
                                          "",
                                          () => LoadSimpleNodes ()
                                         ),
                                     new ("_Rooms", "", () => LoadRooms ()),
                                     new (
                                          "_Armies With Builder",
                                          "",
                                          () => LoadArmies (false)
                                         ),
                                     new (
                                          "_Armies With Delegate",
                                          "",
                                          () => LoadArmies (true)
                                         )
                                 }
                                )
            ]
        };

        Top.Add (menu);

        var statusBar = new StatusBar (
                                       new StatusItem []
                                       {
                                           new (
                                                Application.QuitKey,
                                                $"{Application.QuitKey} to Quit",
                                                () => Quit ()
                                               )
                                       }
                                      );

        Top.Add (statusBar);

        // Start with the most basic use case
        LoadSimpleNodes ();
    }

    private void LoadArmies (bool useDelegate)
    {
        var army1 = new Army
        {
            Designation = "3rd Infantry",
            Units = new List<Unit> { new () { Name = "Orc" }, new () { Name = "Troll" }, new () { Name = "Goblin" } }
        };

        if (_currentTree != null)
        {
            Win.Remove (_currentTree);
            _currentTree.Dispose ();
        }

        TreeView<GameObject> tree = new () { X = 0, Y = 0, Width = 40, Height = 20 };

        if (useDelegate)
        {
            tree.TreeBuilder = new DelegateTreeBuilder<GameObject> (
                                                                    o =>
                                                                        o is Army a
                                                                            ? a.Units
                                                                            : Enumerable.Empty<GameObject> ()
                                                                   );
        }
        else
        {
            tree.TreeBuilder = new GameObjectTreeBuilder ();
        }

        Win.Add (tree);

        tree.AddObject (army1);

        _currentTree = tree;
    }

    private void LoadRooms ()
    {
        var myHouse = new House
        {
            Address = "23 Nowhere Street",
            Rooms = new List<Room>
            {
                new () { Name = "Ballroom" }, new () { Name = "Bedroom 1" }, new () { Name = "Bedroom 2" }
            }
        };

        if (_currentTree != null)
        {
            Win.Remove (_currentTree);
            _currentTree.Dispose ();
        }

        var tree = new TreeView { X = 0, Y = 0, Width = 40, Height = 20 };

        Win.Add (tree);

        tree.AddObject (myHouse);

        _currentTree = tree;
    }

    private void LoadSimpleNodes ()
    {
        if (_currentTree != null)
        {
            Win.Remove (_currentTree);
            _currentTree.Dispose ();
        }

        var tree = new TreeView { X = 0, Y = 0, Width = 40, Height = 20 };

        Win.Add (tree);

        var root1 = new TreeNode ("Root1");
        root1.Children.Add (new TreeNode ("Child1.1"));
        root1.Children.Add (new TreeNode ("Child1.2"));

        var root2 = new TreeNode ("Root2");
        root2.Children.Add (new TreeNode ("Child2.1"));
        root2.Children.Add (new TreeNode ("Child2.2"));

        tree.AddObject (root1);
        tree.AddObject (root2);

        _currentTree = tree;
    }

    private void Quit () { Application.RequestStop (); }

    private class Army : GameObject
    {
        public string Designation { get; set; }
        public List<Unit> Units { get; set; }
        public override string ToString () { return Designation; }
    }

    private abstract class GameObject
    { }

    private class GameObjectTreeBuilder : ITreeBuilder<GameObject>
    {
        public bool SupportsCanExpand => true;
        public bool CanExpand (GameObject model) { return model is Army; }

        public IEnumerable<GameObject> GetChildren (GameObject model)
        {
            if (model is Army a)
            {
                return a.Units;
            }

            return Enumerable.Empty<GameObject> ();
        }
    }

    // Your data class
    private class House : TreeNode
    {
        // Your properties
        public string Address { get; set; }

        // ITreeNode member:
        public override IList<ITreeNode> Children => Rooms.Cast<ITreeNode> ().ToList ();
        public List<Room> Rooms { get; set; }

        public override string Text
        {
            get => Address;
            set => Address = value;
        }
    }

    private class Room : TreeNode
    {
        public string Name { get; set; }

        public override string Text
        {
            get => Name;
            set => Name = value;
        }
    }

    private class Unit : GameObject
    {
        public string Name { get; set; }
        public override string ToString () { return Name; }
    }
}
