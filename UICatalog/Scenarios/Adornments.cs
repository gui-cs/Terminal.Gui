using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Adornments Demo", "Demonstrates Margin, Border, and Padding on Views.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Adornments")]
public class Adornments : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        var editor = new AdornmentsEditor
        {
            AutoSelectViewToEdit = true,

            // This is for giggles, to show that the editor can be moved around.
            Arrangement = ViewArrangement.Movable,
            X = Pos.AnchorEnd ()
        };

        editor.Border.Thickness = new (1, 2, 1, 1);

        app.Add (editor);

        var window = new Window
        {
            Title = "The _Window",
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable,

            Width = Dim.Fill (Dim.Func (() => editor.Frame.Width )),
            Height = Dim.Fill ()
        };
        app.Add (window);

        var tf1 = new TextField { Width = 10, Text = "TextField" };
        var color = new ColorPicker16 { Title = "BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd () };
        color.BorderStyle = LineStyle.RoundedDotted;

        color.ColorChanged += (s, e) =>
                              {
                                  color.SuperView.ColorScheme = new (color.SuperView.ColorScheme)
                                  {
                                      Normal = new (
                                                    color.SuperView.ColorScheme.Normal.Foreground,
                                                    e.CurrentValue
                                                   )
                                  };
                              };

        var button = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Press me!" };

        button.Accepting += (s, e) =>
                             MessageBox.Query (20, 7, "Hi", $"Am I a {window.GetType ().Name}?", "Yes", "No");

        var label = new TextView
        {
            X = Pos.Center (),
            Y = Pos.Bottom (button),
            Title = "Title",
            Text = "I have a 3 row top border.\nMy border inherits from the SuperView.",
            Width = 40,
            Height = 6 // TODO: Use Dim.Auto
        };
        label.Border.Thickness = new (1, 3, 1, 1);

        var btnButtonInWindow = new Button { X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Text = "Button" };

        var labelAnchorEnd = new Label
        {
            Y = Pos.AnchorEnd (),
            Width = 40,
            Height = Dim.Percent (20),
            Text = "Label\nY=AnchorEnd(),Height=Dim.Percent(10)",
            ColorScheme = Colors.ColorSchemes ["Dialog"]
        };

        window.Margin.Data = "Margin";
        window.Margin.Text = "Margin Text";
        window.Margin.Thickness = new (0);

        window.Border.Data = "Border";
        window.Border.Text = "Border Text";
        window.Border.Thickness = new (0);

        window.Padding.Data = "Padding";
        window.Padding.Text = "Padding Text line 1\nPadding Text line 3\nPadding Text line 3\nPadding Text line 4\nPadding Text line 5";
        window.Padding.Thickness = new (3);
        window.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
        window.Padding.CanFocus = true;

        var longLabel = new Label
        {
            X = 40, Y = 5, Title = "This is long text (in a label) that should clip."
        };
        longLabel.TextFormatter.WordWrap = true;
        window.Add (tf1, color, button, label, btnButtonInWindow, labelAnchorEnd, longLabel);

        window.Initialized += (s, e) =>
                              {
                                  editor.ViewToEdit = window;

                                  editor.ShowViewIdentifier = true;

                                  var labelInPadding = new Label { X = 0, Y = 1, Title = "_Text:" };
                                  window.Padding.Add (labelInPadding);

                                  var textFieldInPadding = new TextField
                                  {
                                      X = Pos.Right (labelInPadding) + 1,
                                      Y = Pos.Top (labelInPadding), Width = 10,
                                      Text = "text (Y = 1)",
                                      CanFocus = true
                                  };
                                  textFieldInPadding.Accepting += (s, e) => MessageBox.Query (20, 7, "TextField", textFieldInPadding.Text, "Ok");
                                  window.Padding.Add (textFieldInPadding);

                                  var btnButtonInPadding = new Button
                                  {
                                      X = Pos.Center (),
                                      Y = 1,
                                      Text = "_Button in Padding Y = 1",
                                      CanFocus = true,
                                      HighlightStyle = HighlightStyle.None,
                                  };
                                  btnButtonInPadding.Accepting += (s, e) => MessageBox.Query (20, 7, "Hi", "Button in Padding Pressed!", "Ok");
                                  btnButtonInPadding.BorderStyle = LineStyle.Dashed;
                                  btnButtonInPadding.Border.Thickness = new (1, 1, 1, 1);
                                  window.Padding.Add (btnButtonInPadding);

#if SUBVIEW_BASED_BORDER
                                btnButtonInPadding.Border.CloseButton.Visible = true;

                                view.Border.CloseButton.Visible = true;
                                view.Border.CloseButton.Accept += (s, e) =>
                                                                  {
                                                                      MessageBox.Query (20, 7, "Hi", "Window Close Button Pressed!", "Ok");
                                                                      e.Cancel = true;
                                                                  };

                                view.Accept += (s, e) => MessageBox.Query (20, 7, "Hi", "Window Close Button Pressed!", "Ok");
#endif
                              };

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = window;
        editor.AutoSelectAdornments = true;

        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();
    }
}
