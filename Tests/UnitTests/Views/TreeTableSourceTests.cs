using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.ViewsTests;

public class TreeTableSourceTests : IDisposable
{
    private readonly Rune _origChecked;
    private readonly Rune _origUnchecked;
    private readonly ITestOutputHelper _output;

    public TreeTableSourceTests (ITestOutputHelper output)
    {
        _output = output;

        _origChecked = Glyphs.CheckStateChecked;
        _origUnchecked = Glyphs.CheckStateUnChecked;
        Glyphs.CheckStateChecked = new Rune ('☑');
        Glyphs.CheckStateUnChecked = new Rune ('☐');
    }

    public void Dispose ()
    {
        Glyphs.CheckStateChecked = _origChecked;
        Glyphs.CheckStateUnChecked = _origUnchecked;
    }

    [Fact]
    [SetupFakeDriver]
    public void TestTreeTableSource_BasicExpanding_WithKeyboard ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (100, 100);
        TableView tv = GetTreeTable (out _);

        tv.Style.GetOrCreateColumnStyle (1).MinAcceptableWidth = 1;

        tv.Draw ();

        var expected =
            @"
│Name          │Description            │
├──────────────┼───────────────────────┤
│├+Lost Highway│Exciting night road    │
│└+Route 66    │Great race course      │";

        DriverAssert.AssertDriverContentsAre (expected, _output);

        Assert.Equal (2, tv.Table.Rows);

        // top left is selected cell
        Assert.Equal (0, tv.SelectedRow);
        Assert.Equal (0, tv.SelectedColumn);

        // when pressing right we should expand the top route
        tv.NewKeyDownEvent (Key.CursorRight);

        View.SetClipToScreen ();
        tv.Draw ();

        expected =
            @"
│Name             │Description         │
├─────────────────┼────────────────────┤
│├-Lost Highway   │Exciting night road │
││ ├─Ford Trans-Am│Talking thunderbird │
││ └─DeLorean     │Time travelling car │
│└+Route 66       │Great race course   │
";

        DriverAssert.AssertDriverContentsAre (expected, _output);

        // when pressing left we should collapse the top route again
        tv.NewKeyDownEvent (Key.CursorLeft);

        View.SetClipToScreen ();
        tv.Draw ();

        expected =
            @"
│Name          │Description            │
├──────────────┼───────────────────────┤
│├+Lost Highway│Exciting night road    │
│└+Route 66    │Great race course      │
";

        DriverAssert.AssertDriverContentsAre (expected, _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void TestTreeTableSource_BasicExpanding_WithMouse ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (100, 100);

        TableView tv = GetTreeTable (out _);

        tv.Style.GetOrCreateColumnStyle (1).MinAcceptableWidth = 1;

        View.SetClipToScreen ();
        tv.Draw ();

        var expected =
            @"
│Name          │Description            │
├──────────────┼───────────────────────┤
│├+Lost Highway│Exciting night road    │
│└+Route 66    │Great race course      │";

        DriverAssert.AssertDriverContentsAre (expected, _output);

        Assert.Equal (2, tv.Table.Rows);

        // top left is selected cell
        Assert.Equal (0, tv.SelectedRow);
        Assert.Equal (0, tv.SelectedColumn);

        Assert.True (tv.NewMouseEvent (new MouseEventArgs { Position = new (2, 2), Flags = MouseFlags.Button1Clicked }));

        View.SetClipToScreen ();
        tv.Draw ();

        expected =
            @"
│Name             │Description         │
├─────────────────┼────────────────────┤
│├-Lost Highway   │Exciting night road │
││ ├─Ford Trans-Am│Talking thunderbird │
││ └─DeLorean     │Time travelling car │
│└+Route 66       │Great race course   │
";

        DriverAssert.AssertDriverContentsAre (expected, _output);

        // Clicking to the right/left of the expand/collapse does nothing
        tv.NewMouseEvent (new MouseEventArgs { Position = new (3, 2), Flags = MouseFlags.Button1Clicked });
        tv.Draw ();
        DriverAssert.AssertDriverContentsAre (expected, _output);
        tv.NewMouseEvent (new MouseEventArgs { Position = new (1, 2), Flags = MouseFlags.Button1Clicked });
        tv.Draw ();
        DriverAssert.AssertDriverContentsAre (expected, _output);

        // Clicking on the + again should collapse
        tv.NewMouseEvent (new MouseEventArgs { Position = new (2, 2), Flags = MouseFlags.Button1Clicked });
        View.SetClipToScreen ();
        tv.Draw ();

        expected =
            @"
│Name          │Description            │
├──────────────┼───────────────────────┤
│├+Lost Highway│Exciting night road    │
│└+Route 66    │Great race course      │";

        DriverAssert.AssertDriverContentsAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void TestTreeTableSource_CombinedWithCheckboxes ()
    {
        Toplevel top = new ();
        TableView tv = GetTreeTable (out TreeView<IDescribedThing> treeSource);

        CheckBoxTableSourceWrapperByIndex checkSource;
        tv.Table = checkSource = new CheckBoxTableSourceWrapperByIndex (tv, tv.Table);
        tv.Style.GetOrCreateColumnStyle (2).MinAcceptableWidth = 1;
        top.Add (tv);
        Application.Begin (top);

        tv.Draw ();

        var expected =
            @"
    │ │Name          │Description          │
├─┼──────────────┼─────────────────────┤
│☐│├+Lost Highway│Exciting night road  │
│☐│└+Route 66    │Great race course    │
";

        DriverAssert.AssertDriverContentsAre (expected, _output);

        Assert.Equal (2, tv.Table.Rows);

        // top left is selected cell
        Assert.Equal (0, tv.SelectedRow);
        Assert.Equal (0, tv.SelectedColumn);

        // when pressing right we move to tree column
        tv.NewKeyDownEvent (Key.CursorRight);

        // now we are in tree column
        Assert.Equal (0, tv.SelectedRow);
        Assert.Equal (1, tv.SelectedColumn);

        Application.RaiseKeyDownEvent (Key.CursorRight);

        View.SetClipToScreen ();
        tv.Draw ();

        expected =
            @"

│ │Name             │Description       │
├─┼─────────────────┼──────────────────┤
│☐│├-Lost Highway   │Exciting night roa│
│☐││ ├─Ford Trans-Am│Talking thunderbir│
│☐││ └─DeLorean     │Time travelling ca│
│☐│└+Route 66       │Great race course │
";

        DriverAssert.AssertDriverContentsAre (expected, _output);

        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.Space);
        View.SetClipToScreen ();
        tv.Draw ();

        expected =
            @"

│ │Name             │Description       │
├─┼─────────────────┼──────────────────┤
│☐│├-Lost Highway   │Exciting night roa│
│☒││ ├─Ford Trans-Am│Talking thunderbir│
│☐││ └─DeLorean     │Time travelling ca│
│☐│└+Route 66       │Great race course │
";

        DriverAssert.AssertDriverContentsAre (expected, _output);

        IDescribedThing [] selectedObjects = checkSource.CheckedRows.Select (treeSource.GetObjectOnRow).ToArray ();
        IDescribedThing selected = Assert.Single (selectedObjects);

        Assert.Equal ("Ford Trans-Am", selected.Name);
        Assert.Equal ("Talking thunderbird car", selected.Description);
        top.Dispose ();
    }

    private TableView GetTreeTable (out TreeView<IDescribedThing> tree)
    {
        var tableView = new TableView ();
        tableView.SchemeName = "TopLevel";
        tableView.Viewport = new Rectangle (0, 0, 40, 6);

        tableView.Style.ShowHorizontalHeaderUnderline = true;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.SmoothHorizontalScrolling = true;

        tree = new TreeView<IDescribedThing> ();
        tree.AspectGetter = d => d.Name;

        tree.TreeBuilder = new DelegateTreeBuilder<IDescribedThing> (
                                                                     d => d is Road r
                                                                              ? r.Traffic
                                                                              : Enumerable.Empty<IDescribedThing> ()
                                                                    );

        tree.AddObject (
                        new Road
                        {
                            Name = "Lost Highway",
                            Description = "Exciting night road",
                            Traffic = new List<Car>
                            {
                                new () { Name = "Ford Trans-Am", Description = "Talking thunderbird car" },
                                new () { Name = "DeLorean", Description = "Time travelling car" }
                            }
                        }
                       );

        tree.AddObject (
                        new Road
                        {
                            Name = "Route 66",
                            Description = "Great race course",
                            Traffic = new List<Car>
                            {
                                new () { Name = "Pink Compact", Description = "Penelope Pitstop's car" },
                                new () { Name = "Mean Machine", Description = "Dick Dastardly's car" }
                            }
                        }
                       );

        tableView.Table = new TreeTableSource<IDescribedThing> (
                                                                tableView,
                                                                "Name",
                                                                tree,
                                                                new Dictionary<string, Func<IDescribedThing, object>> { { "Description", d => d.Description } }
                                                               );

        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.LayoutSubViews ();

        var top = new Toplevel ();
        top.Add (tableView);
        top.SetFocus ();
        Assert.Equal (tableView, top.MostFocused);

        return tableView;
    }

    private class Car : IDescribedThing
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    private interface IDescribedThing
    {
        string Description { get; }
        string Name { get; }
    }

    private class Road : IDescribedThing
    {
        public List<Car> Traffic { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
