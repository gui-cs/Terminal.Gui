#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tree View", "Simple tree view examples.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class TreeUseCases : Scenario
{
    private IApplication? _app;
    private View? _currentTree;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window appWindow = new ();

        // MenuBar
        MenuBar menu = new ();

        menu.Add (new MenuBarItem (Strings.menuFile, [new MenuItem { Title = Strings.cmdQuit, Action = Quit }]));

        menu.Add (new MenuBarItem ("_Scenarios",
                                   [
                                       new MenuItem { Title = "_Simple Nodes", Action = LoadSimpleNodes },
                                       new MenuItem { Title = "_Rooms", Action = LoadRooms },
                                       new MenuItem { Title = "_Armies With Builder", Action = () => LoadArmies (false) },
                                       new MenuItem { Title = "_Armies With Delegate", Action = () => LoadArmies (true) }
                                   ]));

        // StatusBar
        StatusBar statusBar = new ([new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", Quit)]);

        appWindow.Add (menu, statusBar);

        appWindow.IsModalChanged += (_, args) =>
                                    {
                                        if (args.Value)
                                        {
                                            // Start with the most basic use case
                                            LoadSimpleNodes ();
                                        }
                                    };

        app.Run (appWindow);
    }

    private void LoadArmies (bool useDelegate)
    {
        Army army1 = new () { Designation = "3rd Infantry", Units = [new Unit { Name = "Orc" }, new Unit { Name = "Troll" }, new Unit { Name = "Goblin" }] };

        if (_currentTree is { })
        {
            _app?.TopRunnableView?.Remove (_currentTree);

            _currentTree.Dispose ();
        }

        TreeView<GameObject> tree = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        if (useDelegate)
        {
            tree.TreeBuilder = new DelegateTreeBuilder<GameObject> (o => o is Army a ? a.Units : Enumerable.Empty<GameObject> (), o => true);
        }
        else
        {
            tree.TreeBuilder = new GameObjectTreeBuilder ();
        }

        _app?.TopRunnableView?.Add (tree);

        tree.AddObject (army1);

        _currentTree = tree;
    }

    private void LoadRooms ()
    {
        House myHouse = new ()
        {
            Address = "23 Nowhere Street", Rooms = [new Room { Name = "Ballroom" }, new Room { Name = "Bedroom 1" }, new Room { Name = "Bedroom 2" }]
        };

        if (_currentTree is { })
        {
            _app?.TopRunnableView?.Remove (_currentTree);

            _currentTree.Dispose ();
        }

        TreeView tree = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        _app?.TopRunnableView?.Add (tree);

        tree.AddObject (myHouse);

        _currentTree = tree;
    }

    private void LoadSimpleNodes ()
    {
        if (_currentTree is { })
        {
            _app?.TopRunnableView?.Remove (_currentTree);

            _currentTree.Dispose ();
        }

        TreeView tree = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        _app?.TopRunnableView?.Add (tree);

        TreeNode root1 = new () { Text = "Root1" };
        root1.Children.Add (new TreeNode { Text = "Child1.1" });
        root1.Children.Add (new TreeNode { Text = "Child1.2" });

        TreeNode root2 = new () { Text = "Root2" };
        root2.Children.Add (new TreeNode { Text = "Child2.1" });
        root2.Children.Add (new TreeNode { Text = "Child2.2" });

        tree.AddObject (root1);
        tree.AddObject (root2);

        _currentTree = tree;
    }

    private void Quit () => (_app?.TopRunnableView as Runnable)?.RequestStop ();

    private class Army : GameObject
    {
        public string Designation { get; init; } = string.Empty;
        public List<Unit> Units { get; init; } = [];
        public override string ToString () => Designation;
    }

    private abstract class GameObject;

    private class GameObjectTreeBuilder : ITreeBuilder<GameObject>
    {
        public bool SupportsCanExpand => true;
        public bool CanExpand (GameObject model) => model is Army;

        public IEnumerable<GameObject> GetChildren (GameObject model)
        {
            if (model is Army a)
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
        public List<Room> Rooms { get; init; } = [];

        public override string Text { get => Address; set => Address = value; }
    }

    private class Room : TreeNode
    {
        public string Name { get; set; } = string.Empty;

        public override string Text { get => Name; set => Name = value; }
    }

    private class Unit : GameObject
    {
        public string Name { get; init; } = string.Empty;
        public override string ToString () => Name;
    }
}
