using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("View Experiments", "v2 View Experiments")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Borders")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Proof of Concept")]
public class ViewExperiments : Scenario
{
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
            Height = Dim.Fill (),
        };

        app.Add (testFrame);

        Button button = new ()
        {
            X = 0,
            Y = 0,
            Title = $"TopButton _{GetNextHotKey()}",
        };

        testFrame.Add (button);

        var tiledView1 = CreateTiledView (0, 2, 2);
        var tiledView2 = CreateTiledView (1, Pos.Right (tiledView1), Pos.Top (tiledView1));

        testFrame.Add (tiledView1);
        testFrame.Add (tiledView2);

        var overlappedView1 = CreateOverlappedView (2, Pos.Center(), Pos.Center());
        var tiledSubView = CreateTiledView (4, 0, 2);
        overlappedView1.Add (tiledSubView);
        
        var overlappedView2 = CreateOverlappedView (3, Pos.Center() + 5, Pos.Center() + 5);
        tiledSubView = CreateTiledView (4, 0, 2);
        overlappedView2.Add (tiledSubView);

        tiledSubView = CreateTiledView (5, 0, Pos.Bottom(tiledSubView));
        overlappedView2.Add (tiledSubView);

        testFrame.Add (overlappedView1);
        testFrame.Add (overlappedView2);

        button = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Title = $"TopButton _{GetNextHotKey ()}",
        };

        testFrame.Add (button);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    private int _hotkeyCount;

    private char GetNextHotKey ()
    {
        return (char)((int)'A' + _hotkeyCount++);
    }

    private View CreateTiledView (int id, Pos x, Pos y)
    {
        View overlapped = new View
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


    private View CreateOverlappedView (int id, Pos x, Pos y)
    {
        View overlapped = new View
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
}
