using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Navigation", "Navigation Tester")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Layout")]
public class Navigation : Scenario
{
    private int _hotkeyCount;

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            TabStop = TabBehavior.TabGroup
        };

        var editor = new AdornmentsEditor
        {
            X = 0,
            Y = 0,
            AutoSelectViewToEdit = true,
            TabStop = TabBehavior.NoStop
        };
        app.Add (editor);

        FrameView testFrame = new ()
        {
            Title = "_1 Test Frame",
            X = Pos.Right (editor),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        app.Add (testFrame);

        Button button = new ()
        {
            X = 0,
            Y = 0,
            Title = $"TopButton _{GetNextHotKey ()}"
        };

        testFrame.Add (button);

        View tiledView1 = CreateTiledView (0, 2, 2);
        View tiledView2 = CreateTiledView (1, Pos.Right (tiledView1), Pos.Top (tiledView1));

        testFrame.Add (tiledView1);
        testFrame.Add (tiledView2);

        View tiledView3 = CreateTiledView (1, Pos.Right (tiledView2), Pos.Top (tiledView2));
        tiledView3.TabStop = TabBehavior.TabGroup;
        tiledView3.BorderStyle = LineStyle.Double;
        testFrame.Add (tiledView3);

        View overlappedView1 = CreateOverlappedView (2, Pos.Center () - 5, Pos.Center ());
        View tiledSubView = CreateTiledView (4, 0, 2);
        overlappedView1.Add (tiledSubView);

        View overlappedView2 = CreateOverlappedView (3, Pos.Center () + 10, Pos.Center () + 5);

        var overlappedInOverlapped1 = CreateOverlappedView (4, 1, 4);
        overlappedView2.Add (overlappedInOverlapped1);

        var overlappedInOverlapped2 = CreateOverlappedView (5, 10, 7);
        overlappedView2.Add (overlappedInOverlapped2);

        CheckBox cb = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Title = "Checkbo_x"
        };
        overlappedView2.Add (cb);

        testFrame.Add (overlappedView1);
        testFrame.Add (overlappedView2);

        button = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Title = $"TopButton _{GetNextHotKey ()}"
        };

        testFrame.Add (button);

        editor.AutoSelectSuperView = testFrame;
        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    private View CreateOverlappedView (int id, Pos x, Pos y)
    {
        var overlapped = new View
        {
            X = x,
            Y = y,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = $"Overlapped{id} _{GetNextHotKey ()}",
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Id = $"Overlapped{id}",
            ShadowStyle = ShadowStyle.Transparent,
            BorderStyle = LineStyle.Double,
            CanFocus = true, // Can't drag without this? BUGBUG
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
        };

        Button button = new ()
        {
            Title = $"Button{id} _{GetNextHotKey ()}"
        };
        overlapped.Add (button);

        button = new ()
        {
            Y = Pos.Bottom (button),
            Title = $"Button{id} _{GetNextHotKey ()}"
        };
        overlapped.Add (button);

        return overlapped;
    }

    private View CreateTiledView (int id, Pos x, Pos y)
    {
        var overlapped = new View
        {
            X = x,
            Y = y,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = $"Tiled{id} _{GetNextHotKey ()}",
            Id = $"Tiled{id}",
            BorderStyle = LineStyle.Single,
            CanFocus = true, // Can't drag without this? BUGBUG
            TabStop = TabBehavior.TabStop,
            Arrangement = ViewArrangement.Fixed
        };

        Button button = new ()
        {
            Title = $"Tiled Button{id} _{GetNextHotKey ()}"
        };
        overlapped.Add (button);

        button = new ()
        {
            Y = Pos.Bottom (button),
            Title = $"Tiled Button{id} _{GetNextHotKey ()}"
        };
        overlapped.Add (button);

        return overlapped;
    }

    private char GetNextHotKey () { return (char)('A' + _hotkeyCount++); }
}
