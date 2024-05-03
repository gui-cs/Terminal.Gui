using System;
using Terminal.Gui;
using static Terminal.Gui.Dim;

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
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}"
        };

        var view = new FrameView
        {
            Title = "Type to make View grow",
            X = 1,
            Y = 1,
            Width = Auto (DimAutoStyle.Content, 40),
            Height = Auto (DimAutoStyle.Content, 10)
        };
        view.ValidatePosDim = true;

        var textEdit = new TextView
        {
            Text = "",
            X = 1, Y = 0, Width = 20, Height = 4
        };
        view.Add (textEdit);

        var vlabel = new Label
        {
            Text = textEdit.Text,
            X = Pos.Left (textEdit),
            Y = Pos.Bottom (textEdit) + 1,
            Width = Auto (DimAutoStyle.Text, 1),
            Height = Auto (DimAutoStyle.Text, 8),
            ColorScheme = Colors.ColorSchemes ["Error"],
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        vlabel.Id = "vlabel";
        view.Add (vlabel);

        var hlabel = new Label
        {
            Text = textEdit.Text,
            X = Pos.Right (vlabel) + 1,
            Y = Pos.Bottom (textEdit),
            Width = Auto (DimAutoStyle.Text, 20),
            Height = Auto (DimAutoStyle.Text, 1),
            ColorScheme = Colors.ColorSchemes ["Error"]
        };
        hlabel.Id = "hlabel";
        view.Add (hlabel);

        var heightAuto = new View
        {
            X = Pos.Right (vlabel) + 1,
            Y = Pos.Bottom (hlabel) + 1,
            Width = 20,
            Height = Auto (),
            ColorScheme = Colors.ColorSchemes ["Error"],
            Title = "W: 20, H: Auto",
            BorderStyle = LineStyle.Rounded
        };
        heightAuto.Id = "heightAuto";
        view.Add (heightAuto);

        var widthAuto = new View
        {
            X = Pos.Right (heightAuto) + 1,
            Y = Pos.Bottom (hlabel) + 1,
            Width = Auto (),
            Height = 5,
            ColorScheme = Colors.ColorSchemes ["Error"],
            Title = "W: Auto, H: 5",
            BorderStyle = LineStyle.Rounded
        };
        widthAuto.Id = "widthAuto";
        view.Add (widthAuto);

        var bothAuto = new View
        {
            X = Pos.Right (widthAuto) + 1,
            Y = Pos.Bottom (hlabel) + 1,
            Width = Auto (),
            Height = Auto (),
            ColorScheme = Colors.ColorSchemes ["Error"],
            Title = "W: Auto, H: Auto",
            BorderStyle = LineStyle.Rounded
        };
        bothAuto.Id = "bothAuto";
        view.Add (bothAuto);

        textEdit.ContentsChanged += (s, e) =>
                                    {
                                        hlabel.Text = textEdit.Text;
                                        vlabel.Text = textEdit.Text;
                                        heightAuto.Text = textEdit.Text;
                                        widthAuto.Text = textEdit.Text;
                                        bothAuto.Text = textEdit.Text;
                                    };

        var movingButton = new Button
        {
            Text = "_Move down",
            X = Pos.Right (vlabel),
            Y = Pos.Bottom (vlabel)
        };
        movingButton.Accept += (s, e) => { movingButton.Y = movingButton.Frame.Y + 1; };
        view.Add (movingButton);

        var resetButton = new Button
        {
            Text = "_Reset Button (AnchorEnd)",
            X = Pos.AnchorEnd (),
            Y = Pos.Top (movingButton)
        };

        resetButton.Accept += (s, e) => { movingButton.Y = Pos.Bottom (hlabel); };
        view.Add (resetButton);

        var dlgButton = new Button
        {
            Text = "Open Test _Dialog",
            X = Pos.Right (view),
            Y = Pos.Top (view)
        };
        dlgButton.Accept += DlgButton_Clicked;

        appWindow.Add (view, dlgButton);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private void DlgButton_Clicked (object sender, EventArgs e)
    {
        var dlg = new Dialog
        {
            Title = "Test Dialog",
            Width = Auto (min: Percent (10))

            //Height = Dim.Auto (min: Dim.Percent (50))
        };
        var text = new TextField
        {
            ValidatePosDim = true,
            Text = "TextField: X=1; Y=Pos.Bottom (label)+1, Width=Dim.Fill (0); Height=1",
            TextFormatter = new() { WordWrap = true },
            X = 0,
            Y = 0, //Pos.Bottom (label) + 1,
            Width = Fill (10),
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
