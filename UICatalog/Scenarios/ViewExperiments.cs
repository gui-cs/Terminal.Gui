﻿using Terminal.Gui;

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

        var containerLabel = new Label
        {
            X = 0,
            Y = 0,

            Width = Dim.Fill (),
            Height = 3
        };
        app.Add (containerLabel);

        var view = new View
        {
            X = 2,
            Y = Pos.Bottom (containerLabel),
            Height = Dim.Fill (2),
            Width = Dim.Fill (2),
            Title = "View with 2xMargin, 2xBorder, & 2xPadding",
            ColorScheme = Colors.ColorSchemes ["Base"],
            Id = "DaView"
        };

        //app.Add (view);

        view.Margin.Thickness = new (2, 2, 2, 2);
        view.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
        view.Margin.Data = "Margin";
        view.Border.Thickness = new (3);
        view.Border.LineStyle = LineStyle.Single;
        view.Border.ColorScheme = view.ColorScheme;
        view.Border.Data = "Border";
        view.Padding.Thickness = new (2);
        view.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
        view.Padding.Data = "Padding";

        var window1 = new Window
        {
            X = 2,
            Y = 3,
            Height = 7,
            Width = 17,
            Title = "Window 1",
            Text = "Window #2",
            TextAlignment = Alignment.Center
        };

        window1.Margin.Thickness = new (0);
        window1.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
        window1.Margin.Data = "Margin";
        window1.Border.Thickness = new (1);
        window1.Border.LineStyle = LineStyle.Single;
        window1.Border.ColorScheme = view.ColorScheme;
        window1.Border.Data = "Border";
        window1.Padding.Thickness = new (0);
        window1.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
        window1.Padding.Data = "Padding";

        view.Add (window1);

        var window2 = new Window
        {
            X = Pos.Right (window1) + 1,
            Y = 3,
            Height = 5,
            Width = 37,
            Title = "Window2",
            Text = "Window #2 (Right(window1)+1",
            TextAlignment = Alignment.Center
        };

        //view3.InitializeFrames ();
        window2.Margin.Thickness = new (1, 1, 0, 0);
        window2.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
        window2.Margin.Data = "Margin";
        window2.Border.Thickness = new (1, 1, 1, 1);
        window2.Border.LineStyle = LineStyle.Single;
        window2.Border.ColorScheme = view.ColorScheme;
        window2.Border.Data = "Border";
        window2.Padding.Thickness = new (1, 1, 0, 0);
        window2.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
        window2.Padding.Data = "Padding";

        view.Add (window2);

        var view4 = new View
        {
            X = Pos.Right (window2) + 1,
            Y = 3,
            Height = 5,
            Width = 37,
            Title = "View4",
            Text = "View #4 (Right(window2)+1",
            TextAlignment = Alignment.Center
        };

        //view4.InitializeFrames ();
        view4.Margin.Thickness = new (0, 0, 1, 1);
        view4.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
        view4.Margin.Data = "Margin";
        view4.Border.Thickness = new (1, 1, 1, 1);
        view4.Border.LineStyle = LineStyle.Single;
        view4.Border.ColorScheme = view.ColorScheme;
        view4.Border.Data = "Border";
        view4.Padding.Thickness = new (0, 0, 1, 1);
        view4.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
        view4.Padding.Data = "Padding";

        view.Add (view4);

        var view5 = new View
        {
            X = Pos.Right (view4) + 1,
            Y = 3,
            Height = Dim.Fill (2),
            Width = Dim.Fill (),
            Title = "View5",
            Text = "View #5 (Right(view4)+1 Fill",
            TextAlignment = Alignment.Center
        };

        //view5.InitializeFrames ();
        view5.Margin.Thickness = new (0, 0, 0, 0);
        view5.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
        view5.Margin.Data = "Margin";
        view5.Border.Thickness = new (1, 1, 1, 1);
        view5.Border.LineStyle = LineStyle.Single;
        view5.Border.ColorScheme = view.ColorScheme;
        view5.Border.Data = "Border";
        view5.Padding.Thickness = new (0, 0, 0, 0);
        view5.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
        view5.Padding.Data = "Padding";

        view.Add (view5);

        var label = new Label { Text = "AutoSize true; 1;1:", X = 1, Y = 1 };
        view.Add (label);

        var edit = new TextField
        {
            Text = "Right (label)",
            X = Pos.Right (label),
            Y = 1,
            Width = 15,
            Height = 1
        };
        view.Add (edit);

        edit = new()
        {
            Text = "Right (edit) + 1",
            X = Pos.Right (edit) + 1,
            Y = 1,
            Width = 20,
            Height = 1
        };
        view.Add (edit);

        var label50 = new View
        {
            Title = "Border Inherit Demo",
            Text = "Center();50%",
            X = Pos.Center (),
            Y = Pos.Percent (50),
            Width = 30,
            TextAlignment = Alignment.Center
        };
        label50.Border.Thickness = new (1, 3, 1, 1);
        label50.Height = 5;
        view.Add (label50);

        edit = new()
        {
            Text = "0 + Percent(50);70%",
            X = 0 + Pos.Percent (50),
            Y = Pos.Percent (70),
            Width = 30,
            Height = 1
        };
        view.Add (edit);

        edit = new() { Text = "AnchorEnd ();AnchorEnd ()", X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Width = 30, Height = 1 };
        view.Add (edit);

        edit = new()
        {
            Text = "Left;AnchorEnd (2)",
            X = 0,
            Y = Pos.AnchorEnd (2),
            Width = 30,
            Height = 1
        };
        view.Add (edit);

        view.LayoutComplete += (s, e) =>
                               {
                                   containerLabel.Text =
                                       $"Container.Frame: {
                                           app.Frame
                                       } .Bounds: {
                                           app.Viewport
                                       }\nView.Frame: {
                                           view.Frame
                                       } .Viewport: {
                                           view.Viewport
                                       } .viewportOffset: {
                                           view.GetViewportOffsetFromFrame ()
                                       }\n .Padding.Frame: {
                                           view.Padding.Frame
                                       } .Padding.Viewport: {
                                           view.Padding.Viewport
                                       }";
                               };

        view.X = Pos.Center ();

        var editor = new AdornmentsEditor
        {
            X = 0,
            Y = Pos.Bottom (containerLabel),
            AutoSelectViewToEdit = true
        };

        app.Add (editor);
        view.X = 36;
        view.Y = 4;
        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();
        app.Add (view);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
