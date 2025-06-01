using Timer = System.Timers.Timer;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Clipping", "Demonstrates non-rectangular clip region support.")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Arrangement")]
[ScenarioCategory ("Tests")]
[ScenarioCategory ("Drawing")]
public class Clipping : Scenario
{
    private int _hotkeyCount;

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()

            //BorderStyle = LineStyle.None
        };

        app.DrawingContent += (s, e) =>
                              {
                                  app!.FillRect (app!.Viewport, Glyphs.Dot);
                                  e.Cancel = true;
                              };

        var arrangementEditor = new ArrangementEditor
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            AutoSelectViewToEdit = true
        };
        app.Add (arrangementEditor);

        View tiledView1 = CreateTiledView (1, 0, 0);

        tiledView1.Width = 30;

        ProgressBar tiledProgressBar1 = new ()
        {
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Id = "tiledProgressBar",
            BidirectionalMarquee = true
        };
        tiledView1.Add (tiledProgressBar1);

        View tiledView2 = CreateTiledView (2, 4, 2);

        ProgressBar tiledProgressBar2 = new ()
        {
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Id = "tiledProgressBar",
            BidirectionalMarquee = true,
            ProgressBarStyle = ProgressBarStyle.MarqueeBlocks

            // BorderStyle = LineStyle.Rounded
        };
        tiledView2.Add (tiledProgressBar2);

        app.Add (tiledView1);
        app.Add (tiledView2);

        View tiledView3 = CreateTiledView (3, 8, 4);
        app.Add (tiledView3);

        // View overlappedView1 = CreateOverlappedView (1, 30, 2);

        //ProgressBar progressBar = new ()
        //{
        //    X = Pos.AnchorEnd (),
        //    Y = Pos.AnchorEnd (),
        //    Width = Dim.Fill (),
        //    Id = "progressBar",
        //    BorderStyle = LineStyle.Rounded
        //};
        //overlappedView1.Add (progressBar);

        //View overlappedView2 = CreateOverlappedView (2, 32, 4);
        //View overlappedView3 = CreateOverlappedView (3, 34, 6);

        //app.Add (overlappedView1);
        //app.Add (overlappedView2);
        //app.Add (overlappedView3);

        var progressTimer = new Timer (150)
        {
            AutoReset = true
        };

        progressTimer.Elapsed += (s, e) =>
                                 {
                                     tiledProgressBar1.Pulse ();
                                     tiledProgressBar2.Pulse ();
                                     Application.Wakeup ();
                                 };

        progressTimer.Start ();
        Application.Run (app);
        progressTimer.Stop ();
        app.Dispose ();
        Application.Shutdown ();
    }

    private View CreateOverlappedView (int id, Pos x, Pos y)
    {
        var overlapped = new View
        {
            X = x,
            Y = y,
            Height = Dim.Auto (minimumContentDim: 4),
            Width = Dim.Auto (minimumContentDim: 14),
            Title = $"Overlapped{id} _{GetNextHotKey ()}",
            SchemeName = SchemeManager.SchemesToSchemeName(Schemes.Toplevel),
            Id = $"Overlapped{id}",
            ShadowStyle = ShadowStyle.Transparent,
            BorderStyle = LineStyle.Double,
            CanFocus = true, // Can't drag without this? BUGBUG
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped | ViewArrangement.Resizable
        };

        return overlapped;
    }

    private View CreateTiledView (int id, Pos x, Pos y)
    {
        var tiled = new View
        {
            X = x,
            Y = y,
            Height = Dim.Auto (minimumContentDim: 8),
            Width = Dim.Auto (minimumContentDim: 15),
            Title = $"Tiled{id} _{GetNextHotKey ()}",
            Id = $"Tiled{id}",
            Text = $"Tiled{id}",
            BorderStyle = LineStyle.Single,
            CanFocus = true, // Can't drag without this? BUGBUG
            TabStop = TabBehavior.TabStop,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            ShadowStyle = ShadowStyle.Transparent
        };

        //tiled.Padding.Thickness = new (1);
        //tiled.Padding.Diagnostics =  ViewDiagnosticFlags.Thickness;

        //tiled.Margin.Thickness = new (1);

        FrameView fv = new ()
        {
            Title = "FrameView",
            Width = 15,
            Height = 3
        };
        tiled.Add (fv);

        return tiled;
    }

    private char GetNextHotKey () { return (char)('A' + _hotkeyCount++); }
}
