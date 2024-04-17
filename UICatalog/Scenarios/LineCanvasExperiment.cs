using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("LineCanvas Experiments", "Experiments with LineCanvas")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Borders")]
[ScenarioCategory ("Proof of Concept")]
public class LineCanvasExperiment : Scenario
{
    /// <summary>Setup the scenario.</summary>
    public override void Main ()
    {
        var app = new Window
        {
            Title = "LineCanvas Experiments",
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Base"],
            BorderStyle = LineStyle.None
        };

        //View.Diagnostics ^= DiagnosticFlags.FrameRuler;

        //var win1 = new Window
        //{
        //    AutoSize = false,
        //    Title = "win1",
        //    Text = "Win1 30%/50% Heavy",
        //    X = 20,
        //    Y = 0,
        //    Width = 30, //Dim.Percent (30) - 5,
        //    Height = 10, //Dim.Percent (50) - 5,
        //    //ColorScheme = Colors.ColorSchemes ["Base"],
        //    BorderStyle = LineStyle.Heavy,
        //    SuperViewRendersLineCanvas = true
        //};
        //win1.Padding.Thickness = new Thickness (1);

        //app.Add (win1);

        //var win2 = new Window
        //{
        //    Title = "win2",
        //    Text = "Win2 right of win1, 30%/70% Single.",
        //    X = Pos.Right (win1) - 1,
        //    Y = 0,
        //    Width = Dim.Percent (30),
        //    Height = Dim.Percent (70),

        //    //ColorScheme = Colors.ColorSchemes ["Error"],
        //    SuperViewRendersLineCanvas = true
        //};

        //app.Add (win2);

        //var view3 = new FrameView
        //{
        //    Title = "View 3",
        //    Text = "View3 right of win2 Fill/Fill Single",
        //    X = Pos.Right (win2) - 1,
        //    Y = 0,
        //    Width = Dim.Fill (-1),
        //    Height = Dim.Fill (-1),
        //    SuperViewRendersLineCanvas = true

        //    //ColorScheme = Colors.ColorSchemes ["Menu"],
        //};

        //app.Add (view3);

        //var view4 = new FrameView
        //{
        //    Title = "View 4",
        //    Text = "View4 below win2 win2.Width/5 Single",
        //    X = Pos.Left (win2),
        //    Y = Pos.Bottom (win2) - 1,
        //    Width = win2.Width,
        //    Height = 5,
        //    SuperViewRendersLineCanvas = true

        //    //ColorScheme = Colors.ColorSchemes ["TopLevel"],
        //};

        //app.Add (view4);

        //var win5 = new Window
        //{
        //    Title = "Win 5",
        //    Text = "win5 below View4 view4.Width/5 Double",
        //    X = Pos.Left (win2),
        //    Y = Pos.Bottom (view4) - 1,
        //    Width = view4.Width,
        //    Height = 5,

        //    //ColorScheme = Colors.ColorSchemes ["TopLevel"],
        //    SuperViewRendersLineCanvas = true,
        //    BorderStyle = LineStyle.Double
        //};

        //app.Add (win5);


        //var marginWindow = new Window
        //{
        //    Title = "Positive Margin",
        //    X = 15,
        //    Y = 10,
        //    Width = 10,
        //    Height = 10,

        //    //ColorScheme = Colors.ColorSchemes ["Error"],
        //    SuperViewRendersLineCanvas = true
        //};
        //marginWindow.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        //marginWindow.Margin.Thickness = new Thickness (1);
        //marginWindow.Border.Thickness = new Thickness (1, 2, 1, 1);

        //app.Add (marginWindow);


        //var line = new Line
        //{
        //    Id = "line1",
        //    X = 1,
        //    Y = 1,
        //    Width = 10,
        //    Height = 1,
        //    Orientation = Orientation.Horizontal,
        //    SuperViewRendersLineCanvas = true
        //};
        //app.Add (line);

        //line = new Line
        //{
        //    Id = "line2",
        //    X = 1,
        //    Y = 1,
        //    Width = 1,
        //    Height = 10,
        //    Orientation = Orientation.Vertical,
        //    SuperViewRendersLineCanvas = true
        //};
        //app.Add (line);

        var label = new View ()
        {
            //Arrangement = ViewArrangement.Movable,
            Id = "label1",
            Text = "Label",
            Title = "label1",
            X = 5,
            Y = 4,
            Width = 15,
            Height = 6,
            //TextAlignment = TextAlignment.Centered,
            //SuperViewRendersLineCanvas = true,
            //BorderStyle = LineStyle.Double
        };
        label.Border.Thickness = new Thickness (1, 3, 1, 1);
        label.Border.LineStyle = LineStyle.Double;
        app.Add (label);

        Application.Run (app);
        app.Dispose ();
    }
}
