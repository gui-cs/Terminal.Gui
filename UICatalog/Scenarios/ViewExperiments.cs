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
            Title = GetQuitKeyAndName ()
        };

        var view = new View
        {
            X = 2,
            Y = 2,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = "View1",
            ColorScheme = Colors.ColorSchemes ["Base"],
            Id = "View1",
            ShadowStyle = ShadowStyle.Transparent,
            BorderStyle = LineStyle.Double,
            CanFocus = true, // Can't drag without this? BUGBUG
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
        };

        Button button = new ()
        {
            Title = "Button_1",
        };
        view.Add (button);

        button = new ()
        {
            Y = Pos.Bottom (button),
            Title = "Button_2",
        };
        view.Add (button);

        //app.Add (view);

        view.BorderStyle = LineStyle.Double;

        var view2 = new View
        {
            X = Pos.Right (view),
            Y = Pos.Bottom (view),
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = "View2",
            ColorScheme = Colors.ColorSchemes ["Base"],
            Id = "View2",
            ShadowStyle = ShadowStyle.Transparent,
            BorderStyle = LineStyle.Double,
            CanFocus = true, // Can't drag without this? BUGBUG
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
        };


        button = new ()
        {
            Title = "Button_3",
        };
        view2.Add (button);

        button = new ()
        {
            Y = Pos.Bottom (button),
            Title = "Button_4",
        };
        view2.Add (button);

        view2.Add (button);

        button = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Title = "Button_5",
        };

        var editor = new AdornmentsEditor
        {
            X = 0,
            Y = 0,
            AutoSelectViewToEdit = true
        };

        app.Add (editor);
        view.X = 34;
        view.Y = 4;
        app.Add (view);
        app.Add (view2);
        app.Add (button);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
