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
            Title = "Test Frame",
            X = Pos.Right (editor),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
        };

        app.Add (testFrame);

        Button button = new ()
        {
            X = 0,
            Y = 0,
            Title = "TopButton_1",
        };

        testFrame.Add (button);

        var overlappedView1 = CreateOverlappedView (3, 2, 2);
        var overlappedView2 = CreateOverlappedView (4, 34, 4);


        testFrame.Add (overlappedView1);
        testFrame.Add (overlappedView2);

        button = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Title = "TopButton_2",
        };

        testFrame.Add (button);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    private View CreateOverlappedView (int id, int x, int y)
    {
        View overlapped = new View
        {
            X = x,
            Y = y,
            Height = Dim.Auto (),
            Width = Dim.Auto (),
            Title = $"Overlapped_{id}",
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
            Title = $"Button{id} _{id * 2}"
        };
        overlapped.Add (button);

        button = new ()
        {
            Y = Pos.Bottom (button),
            Title = $"Button{id} _{id * 2 + 1}"
        };
        overlapped.Add (button);

        return overlapped;
    }
}
