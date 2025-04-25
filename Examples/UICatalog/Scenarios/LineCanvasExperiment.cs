using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("LineCanvas Experiments", "Experiments with LineCanvas")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Adornments")]
[ScenarioCategory ("Proof of Concept")]
public class LineCanvasExperiment : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        var frame1 = new FrameView
        {
            Title = "_SuperView",
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Base"]
           ,
            SuperViewRendersLineCanvas = true
        };
        frame1.BorderStyle = LineStyle.None;

        //View.Diagnostics ^= DiagnosticFlags.FrameRuler;

        app.Add (frame1);

        var win1 = new Window
        {
            Title = "win1",
            Text = "Win1 - 0,0",
            X = 0,
            Y = 0,
            Width = 30,
            Height = 10,
            //BorderStyle = LineStyle.Heavy,
            SuperViewRendersLineCanvas = true
        };

        frame1.Add (win1);

        var win2 = new Window
        {
            Title = "win2",
            Text = "Win2 right of win1",
            X = Pos.Right (win1) - 1,
            Y = 0,
            Width = Dim.Percent (30),
            Height = Dim.Percent (70),

            //ColorScheme = Colors.ColorSchemes ["Error"],
            SuperViewRendersLineCanvas = true
        };


        var subViewOfWin2 = new FrameView
        {
            Title = "subViewOfWin2",
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SuperViewRendersLineCanvas = true

            //ColorScheme = Colors.ColorSchemes ["Menu"],
        };

        win2.Add (subViewOfWin2);

        frame1.Add (win2);


        var view4 = new FrameView
        {
            Title = "View 4",
            Text = "View4 below win2 win2.Width/5 Single",
            X = Pos.Right (win1) - 1,
            Y = Pos.Bottom (win2) - 1,
            Width = win2.Width,
            Height = 5,
            SuperViewRendersLineCanvas = true
        };

        frame1.Add (view4);

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

        //frame1.Add (win5);

        //var line = new Line
        //{
        //    X = 1,
        //    Y = 1,
        //    Width = 10,
        //    Height = 1,
        //    Orientation = Orientation.Horizontal,
        //    SuperViewRendersLineCanvas = true
        //};
        //frame1.Add (line);

        //var marginWindow = new Window
        //{
        //    Title = "Positive Margin",
        //    X = 0,
        //    Y = 8,
        //    Width = 25,
        //    Height = 10,

        //    //ColorScheme = Colors.ColorSchemes ["Error"],
        //    SuperViewRendersLineCanvas = true
        //};
        //marginWindow.Margin.ColorScheme = Colors.ColorSchemes ["Error"];
        //marginWindow.Margin.Thickness = new (1);
        //marginWindow.Border.Thickness = new (1, 2, 1, 1);

        //frame1.Add (marginWindow);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
