using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Clipping", "Used to test that things clip correctly")]
[ScenarioCategory ("Tests")]
public class Clipping : Scenario
{
    public override void Init ()
    {
        Application.Init ();
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes ["Base"];
    }

    public override void Setup ()
    {
        //Win.X = 1;
        //Win.Y = 2;
        //Win.Width = Dim.Fill () - 4;
        //Win.Height = Dim.Fill () - 2;
        var label = new Label
        {
            X = 0, Y = 0, Text = "ScrollView (new Rectangle (3, 3, 50, 20)) with a 200, 100 ContentSize..."
        };
        Top.Add (label);

        var scrollView = new ScrollView { X = 3, Y = 3, Width = 50, Height = 20 };
        scrollView.ColorScheme = Colors.ColorSchemes ["Menu"];
        scrollView.ContentSize = new (200, 100);

        //ContentOffset = Point.Empty,
        //scrollView.ShowVerticalScrollIndicator = true;
        //scrollView.ShowHorizontalScrollIndicator = true;

        var embedded1 = new View
        {
            Title = "1",
            X = 3,
            Y = 3,
            Width = Dim.Fill (3),
            Height = Dim.Fill (3),
            ColorScheme = Colors.ColorSchemes ["Dialog"],
            Id = "1",
            BorderStyle = LineStyle.Rounded,
            Arrangement = ViewArrangement.Movable
        };

        var embedded2 = new View
        {
            Title = "2",
            X = 3,
            Y = 3,
            Width = Dim.Fill (3),
            Height = Dim.Fill (3),
            ColorScheme = Colors.ColorSchemes ["Error"],
            Id = "2",
            BorderStyle = LineStyle.Rounded,
            Arrangement = ViewArrangement.Movable
        };
        embedded1.Add (embedded2);

        var embedded3 = new View
        {
            Title = "3",
            X = 3,
            Y = 3,
            Width = Dim.Fill (3),
            Height = Dim.Fill (3),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Id = "3",
            BorderStyle = LineStyle.Rounded,
            Arrangement = ViewArrangement.Movable
        };

        var testButton = new Button { X = 2, Y = 2, Text = "click me" };
        testButton.Accept += (s, e) => { MessageBox.Query (10, 5, "Test", "test message", "Ok"); };
        embedded3.Add (testButton);
        embedded2.Add (embedded3);

        scrollView.Add (embedded1);

        Top.Add (scrollView);
    }
}
