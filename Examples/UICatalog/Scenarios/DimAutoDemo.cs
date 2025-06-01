using System;
using System.Collections.Generic;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("DimAuto", "Demonstrates Dim.Auto")]
[ScenarioCategory ("Layout")]
public class DimAutoDemo : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
        };

        // For diagnostics
        appWindow.Padding.Thickness = new Thickness (1);

        FrameView dimAutoFrameView = CreateDimAutoContentFrameView ();

        //FrameView sliderFrameView = CreateSliderFrameView ();
        //sliderFrameView.X = Pos.Right(dimAutoFrameView) + 1;
        //sliderFrameView.Width = Dim.Fill ();
        //sliderFrameView.Height = Dim.Fill ();


        ////var dlgButton = new Button
        ////{
        ////    Text = "Open Test _Dialog",
        ////    X = Pos.Right (dimAutoFrameView),
        ////    Y = Pos.Top (dimAutoFrameView)
        ////};
        ////dlgButton.Accept += DlgButton_Clicked;

        appWindow.Add (dimAutoFrameView/*, sliderFrameView dlgButton*/);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private static FrameView CreateDimAutoContentFrameView ()
    {
        var dimAutoFrameView = new FrameView
        {
            Title = "Type to make View grow",
            X = 0,
            Y = 0,
            Width = Dim.Auto (DimAutoStyle.Content, minimumContentDim: Dim.Percent (25)),
            Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: 10)
        };
        dimAutoFrameView.Margin.Thickness = new Thickness (1);
        dimAutoFrameView.ValidatePosDim = true;

        var textEdit = new TextView
        {
            Text = "",
            X = 0, Y = 0, Width = 20, Height = 4
        };
        dimAutoFrameView.Add (textEdit);

        var vlabel = new Label
        {
            Text = textEdit.Text,
            X = Pos.Left (textEdit),
            Y = Pos.Bottom (textEdit) + 1,
            Width = Dim.Auto (DimAutoStyle.Text, 1),
            Height = Dim.Auto (DimAutoStyle.Text, 8),
            SchemeName = "Error",
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        vlabel.Id = "vlabel";
        dimAutoFrameView.Add (vlabel);

        var hlabel = new Label
        {
            Text = textEdit.Text,
            X = Pos.Right (vlabel) + 1,
            Y = Pos.Bottom (textEdit),
            Width = Dim.Auto (DimAutoStyle.Text, 20),
            Height = Dim.Auto (DimAutoStyle.Text, 1),
            SchemeName = "Error"
        };
        hlabel.Id = "hlabel";
        dimAutoFrameView.Add (hlabel);

        var heightAuto = new View
        {
            X = Pos.Right (vlabel) + 1,
            Y = Pos.Bottom (hlabel) + 1,
            Width = 20,
            Height = Dim.Auto (),
            SchemeName = "Error",
            Title = "W: 20, H: Auto",
            BorderStyle = LineStyle.Rounded
        };
        heightAuto.Id = "heightAuto";
        dimAutoFrameView.Add (heightAuto);

        var widthAuto = new View
        {
            X = Pos.Right (heightAuto) + 1,
            Y = Pos.Bottom (hlabel) + 1,
            Width = Dim.Auto (),
            Height = 5,
            SchemeName = "Error",
            Title = "W: Auto, H: 5",
            BorderStyle = LineStyle.Rounded
        };
        widthAuto.Id = "widthAuto";
        dimAutoFrameView.Add (widthAuto);

        var bothAuto = new View
        {
            X = Pos.Right (widthAuto) + 1,
            Y = Pos.Bottom (hlabel) + 1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            SchemeName = "Error",
            Title = "W: Auto, H: Auto",
            BorderStyle = LineStyle.Rounded
        };
        bothAuto.Id = "bothAuto";
        dimAutoFrameView.Add (bothAuto);

        textEdit.ContentsChanged += (s, e) =>
                                    {
                                        hlabel.Text = textEdit.Text;
                                        vlabel.Text = textEdit.Text;
                                        heightAuto.Text = textEdit.Text;
                                        widthAuto.Text = textEdit.Text;
                                        bothAuto.Text = textEdit.Text;
                                    };

        //var movingButton = new Button
        //{
        //    Text = "_Click\nTo Move\nDown",
        //    X = Pos.Right (vlabel),
        //    Y = Pos.Bottom (vlabel)
        //};
        //movingButton.Accept += (s, e) => { movingButton.Y = movingButton.Frame.Y + 1; };
        //dimAutoFrameView.Add (movingButton);

        var resetButton = new Button
        {
            Text = "_Reset Button (AnchorEnd)",
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd ()
        };

        resetButton.Accepting += (s, e) =>
        {
            //movingButton.Y = Pos.Bottom (hlabel);
            //movingButton.X = 0;
        };
        dimAutoFrameView.Add (resetButton);


        var radioGroup = new RadioGroup ()
        {
            RadioLabels = ["One", "Two", "Three"],
            X = 0,
            Y = Pos.AnchorEnd (),
            Title = "Radios",
            BorderStyle = LineStyle.Dotted
        };
        dimAutoFrameView.Add (radioGroup);
        return dimAutoFrameView;
    }

    private static FrameView CreateSliderFrameView ()
    {
        var sliderFrameView = new FrameView
        {
            Title = "Slider - Example of a DimAuto View",
        };

        List<object> options = new () { "One", "Two", "Three", "Four" };
        Slider slider = new (options)
        {
            X = 0,
            Y = 0,
            Type = SliderType.Multiple,
            AllowEmpty = false,
            BorderStyle = LineStyle.Double,
            Title = "_Slider"
        };
        sliderFrameView.Add (slider);

        return sliderFrameView;
    }

    private void DlgButton_Clicked (object sender, EventArgs e)
    {
        var dlg = new Dialog
        {
            Title = "Test Dialog",
            Width = Dim.Auto (minimumContentDim: Dim.Percent (10))

            //Height = Dim.Auto (min: Dim.Percent (50))
        };
        var text = new TextField
        {
            ValidatePosDim = true,
            Text = "TextField: X=1; Y=Pos.Bottom (label)+1, Width=Dim.Fill (0); Height=1",
            TextFormatter = new () { WordWrap = true },
            X = 0,
            Y = 0, //Pos.Bottom (label) + 1,
            Width = Dim.Fill (10),
            Height = 1
        };

        //var btn = new Button
        //{
        //    Text = "AnchorEnd", Y = Pos.AnchorEnd (1)
        //};

        //// TODO: We should really fix AnchorEnd to do this automatically. 
        //btn.X = Pos.AnchorEnd () - (Pos.Right (btn) - Pos.Left (btn));
        //dlg.Add (label);
        dlg.Add (text);

        //dlg.Add (btn);
        Application.Run (dlg);
        dlg.Dispose ();
    }
}
