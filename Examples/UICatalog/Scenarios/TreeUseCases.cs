#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tree View", "Simple tree view examples.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class TreeUseCases : Scenario
{
    private View? _currentTree;

    public override void Main ()
    {
        Application.Init ();

        Window appWindow = new ();

        // MenuBar
        MenuBar menu = new ();

        menu.Add (
            new MenuBarItem (
                "_File",
                [
                    new MenuItem
                    {
                        Title = "_Quit",
                        Action = Quit
                    }
                ]
            )
        );

        menu.Add (
            new MenuBarItem (
                "_Scenarios",
                [
                    new MenuItem
                    {
                        Title = "_Simple Nodes",
                        Action = LoadSimpleNodes
                    },
                    new MenuItem
                    {
                        Title = "_Rooms",
                        Action = LoadRooms
                    },
                    new MenuItem
                    {
                        Title = "_Armies With Builder",
                        Action = () => LoadArmies (false)
                    },
                    new MenuItem
                    {
                        Title = "_Armies With Delegate",
                        Action = () => LoadArmies (true)
                    }
                ]
            )
        );

        // StatusBar
        StatusBar statusBar = new (
            [
                new (Application.QuitKey, "Quit", Quit)
            ]
        );

        appWindow.Add (menu, statusBar);

        appWindow.Ready += (sender, args) =>
        {
            // Start with the most basic use case
            LoadSimpleNodes ();
        };

        Application.Run (appWindow);
        appWindow.Dispose ();
        Application.Shutdown ();
    }

    private void LoadArmies (bool useDelegate)
    {
        Army army1 = new ()
        {
            Designation = "3rd Infantry",
            Units = [new () { Name = "Orc" }, new () { Name = "Troll" }, new () { Name = "Goblin" }]
        };

        if (_currentTree is { })
        {
            if (Application.TopRunnable is { })
            {
                Application.TopRunnable.Remove (_currentTree);
            }

            _currentTree.Dispose ();
        }

        TreeView<GameObject> tree = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        if (useDelegate)
        {
            tree.TreeBuilder = new DelegateTreeBuilder<GameObject> (
                o =>
                    o is Army a && a.Units is { }
                        ? a.Units
                        : Enumerable.Empty<GameObject> ()
            );
        }
        else
        {
            tree.TreeBuilder = new GameObjectTreeBuilder ();
        }

        if (Application.TopRunnable is { })
        {
            Application.TopRunnable.Add (tree);
        }

        tree.AddObject (army1);

        _currentTree = tree;
    }

    private void LoadRooms ()
    {
        House myHouse = new ()
        {
            Address = "23 Nowhere Street",
            Rooms =
            [
                new () { Name = "Ballroom" },
                new () { Name = "Bedroom 1" },
                new () { Name = "Bedroom 2" }
            ]
        };

        if (_currentTree is { })
        {
            if (Application.TopRunnable is { })
            {
                Application.TopRunnable.Remove (_currentTree);
            }

            _currentTree.Dispose ();
        }

        TreeView tree = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        if (Application.TopRunnable is { })
        {
            Application.TopRunnable.Add (tree);
        }

        tree.AddObject (myHouse);

        _currentTree = tree;
    }

    private void LoadSimpleNodes ()
    {
        if (_currentTree is { })
        {
            if (Application.TopRunnable is { })
            {
                Application.TopRunnable.Remove (_currentTree);
            }

            _currentTree.Dispose ();
        }

        TreeView tree = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        if (Application.TopRunnable is { })
        {
            Application.TopRunnable.Add (tree);
        }

        TreeNode root1 = new ("Root1");
        root1.Children.Add (new TreeNode ("Child1.1"));
        root1.Children.Add (new TreeNode ("Child1.2"));

        TreeNode root2 = new ("Root2");
        root2.Children.Add (new TreeNode ("Child2.1"));
        root2.Children.Add (new TreeNode ("Child2.2"));

        tree.AddObject (root1);
        tree.AddObject (root2);

        _currentTree = tree;
    }

    private void Quit ()
    {
        Application.RequestStop ();
    }

    private class Army : GameObject
    {
        public string Designation { get; set; } = string.Empty;
        public List<Unit> Units { get; set; } = [];
        public override string ToString () { return Designation; }
    }

    private abstract class GameObject
    {
    }

    private class GameObjectTreeBuilder : ITreeBuilder<GameObject>
    {
        public bool SupportsCanExpand => true;
        public bool CanExpand (GameObject model) { return model is Army; }

        public IEnumerable<GameObject> GetChildren (GameObject model)
        {
            if (model is Army a && a.Units is { })
            {
                return a.Units;
            }

            return Enumerable.Empty<GameObject> ();
        }
    }

    private class House : TreeNode
    {
        public string Address { get; set; } = string.Empty;

        public override IList<ITreeNode> Children => Rooms.Cast<ITreeNode> ().ToList ();
        public List<Room> Rooms { get; set; } = [];

        public override string Text
        {
            get => Address;
            set => Address = value;
        }
    }

    private class Room : TreeNode
    {
        public string Name { get; set; } = string.Empty;

        public override string Text
        {
            get => Name;
            set => Name = value;
        }
    }

    private class Unit : GameObject
    {
        public string Name { get; set; } = string.Empty;
        public override string ToString () { return Name; }
    }
}
