#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tree View", "Simple tree view examples.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public class TreeUseCases : Scenario
{
    private Runnable? _appWindow;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        _appWindow = new Runnable ();

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

        _appWindow?.Add (menu, statusBar);

        _appWindow?.IsModalChanged += (_, args) =>
                                     {
                                         if (args.Value)
                                         {
                                             // Start with the most basic use case
                                             LoadSimpleNodes ();
                                         }
                                     };

        app.Run (_appWindow!);
        _appWindow?.Dispose ();
    }

    private View? CurrentTree
    {
        set
        {
            if (field == value)
            {
                return;
            }

            if (field is { })
            {
                field?.Dispose ();
                _appWindow?.Remove (field);
            }
            field = value;

            if (field is null)
            {
                return;
            }
            _appWindow?.Add (field);
            _appWindow?.MoveSubViewTowardsStart (field);

            field?.SetFocus ();
        }
    }

    private void LoadArmies (bool useDelegate)
    {
        Army army1 = new () { Designation = "3rd Infantry", Units = [new Unit { Name = "Orc" }, new Unit { Name = "Troll" }, new Unit { Name = "Goblin" }] };

        TreeView<GameObject> tree = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        if (useDelegate)
        {
            tree.TreeBuilder = new DelegateTreeBuilder<GameObject> (o => o is Army a ? a.Units : Enumerable.Empty<GameObject> (), _ => true);
        }
        else
        {
            tree.TreeBuilder = new GameObjectTreeBuilder ();
        }

        tree.AddObject (army1);

        CurrentTree = tree;
    }

    private void LoadRooms ()
    {
        House myHouse = new ()
        {
            Address = "23 Nowhere Street", Rooms = [new Room { Name = "Ballroom" }, new Room { Name = "Bedroom 1" }, new Room { Name = "Bedroom 2" }]
        };

        TreeView tree = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        tree.AddObject (myHouse);

        CurrentTree = tree;
    }

    private void LoadSimpleNodes ()
    {
        TreeView tree = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };
        tree.EnableForDesign ();

        CurrentTree = tree;
    }

    private void Quit () => _appWindow?.RequestStop ();

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
