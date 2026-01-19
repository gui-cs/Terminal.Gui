#nullable enable
// ReSharper disable AccessToDisposedClosure

// ReSharper disable AssignNullToNotNullAttribute

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Adornments Demo", "Demonstrates Margin, Border, and Padding on Views.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Adornments")]
public class Adornments : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        var editor = new AdornmentsEditor
        {
            BorderStyle = LineStyle.Single,
            AutoSelectViewToEdit = true,

            // This is for giggles, to show that the editor can be moved around.
            Arrangement = ViewArrangement.Movable,
            X = Pos.AnchorEnd ()
        };

        editor.Border!.Thickness = new (1, 2, 1, 1);

        appWindow.Add (editor);

        Window window = new ()
        {
            Title = "The _Window",
            Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable,

            Width = Dim.Fill (Dim.Func (_ => editor.Frame.Width)),
            Height = Dim.Fill ()
        };
        appWindow.Add (window);

        TextField tf1 = new () { Width = 10, Text = "TextField" };
        ColorPicker16 color = new () { Title = "BG", BoxHeight = 1, BoxWidth = 1, X = Pos.AnchorEnd () };
        color.BorderStyle = LineStyle.RoundedDotted;

        color.ColorChanged += (_, e) =>
                              {
                                  color.SuperView!.SetScheme (
                                                              new (color.SuperView.GetScheme ())
                                                              {
                                                                  Normal = new (
                                                                                color.SuperView.GetAttributeForRole (VisualRole.Normal).Foreground,
                                                                                e.Result,
                                                                                color.SuperView.GetAttributeForRole (VisualRole.Normal).Style
                                                                               )
                                                              });
                              };

        Button button = new () { X = Pos.Center (), Y = Pos.Center (), Text = "Press me!" };

        button.Accepting += (_, _) =>
                                MessageBox.Query (appWindow.App!, "Hi", $"Am I a {window.GetType ().Name}?", Strings.btnNo, Strings.btnYes);

        TextView label = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (button),
            Title = "Title",
            Text = "I have a 3 row top border.\nMy border inherits from the SuperView.",
            Width = 40,
            Height = 6 // TODO: Use Dim.Auto
        };
        label.Border!.Thickness = new (1, 3, 1, 1);

        Button btnButtonInWindow = new () { X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Text = "Button" };

        Label labelAnchorEnd = new ()
        {
            Y = Pos.AnchorEnd (),
            Width = 40,
            Height = Dim.Percent (20),
            Text = "Label\nY=AnchorEnd(),Height=Dim.Percent(10)",
            SchemeName = "Dialog"
        };

        window.Margin!.Data = "Margin";
        window.Margin!.Text = "Margin Text";
        window.Margin!.Thickness = new (3);

        window.Border!.Data = "Border";
        window.Border!.Text = "Border Text";
        window.Border!.Thickness = new (5);
        window.Border!.SetScheme (SchemeManager.GetScheme (Schemes.Dialog));

        window.Padding!.Data = "Padding";
        window.Padding.Text = "Padding Text line 1\nPadding Text line 3\nPadding Text line 3\nPadding Text line 4\nPadding Text line 5";
        window.Padding.Thickness = new (4);
        window.Padding!.SetScheme (SchemeManager.GetScheme (Schemes.Menu));
        window.Padding.CanFocus = true;

        Label longLabel = new ()
        {
            X = 40, Y = 5, Title = "This is long text (in a label) that should clip."
        };
        longLabel.TextFormatter.WordWrap = true;
        window.Add (tf1, color, button, label, btnButtonInWindow, labelAnchorEnd, longLabel);

        window.Initialized += (_, _) =>
                              {
                                  editor.ViewToEdit = window;

                                  editor.ShowViewIdentifier = true;

                                  // NOTE: Adding SubViews to Margin is not supported

                                  Button btnButtonInBorder = new ()
                                  {
                                      X = Pos.Center (),
                                      Y = 1,
                                      Text = "Button in Border _Y = 1"
                                  };
                                  btnButtonInBorder.Accepting += (_, args) =>
                                                                 {
                                                                     MessageBox.Query (appWindow.App!, 20, 7, "Hi", "Button in Border Pressed!", "Ok");
                                                                     args.Handled = true;
                                                                 };
                                  window.Border.Add (btnButtonInBorder);

                                  Label labelInPadding = new () { X = 0, Y = 1, Title = "_Text:" };
                                  window.Padding.Add (labelInPadding);

                                  TextField textFieldInPadding = new ()
                                  {
                                      X = Pos.Right (labelInPadding) + 1,
                                      Y = Pos.Top (labelInPadding), Width = 10,
                                      Text = "text (Y = 1)"
                                  };

                                  textFieldInPadding.Accepting += (_, _) => MessageBox.Query (appWindow.App!, 20, 7, "TextField", textFieldInPadding.Text, "Ok");
                                  window.Padding.Add (textFieldInPadding);

                                  Button btnButtonInPadding = new ()
                                  {
                                      X = Pos.Center (),
                                      Y = 1,
                                      Text = "_Button in Padding Y = 1",
                                      CanFocus = true
                                  };
                                  btnButtonInPadding.Accepting += (_, args) =>
                                                                  {
                                                                      MessageBox.Query (appWindow.App!, 20, 7, "Hi", "Button in Padding Pressed!", "Ok");
                                                                      args.Handled = true;
                                                                  };
                                  window.Padding.Add (btnButtonInPadding);

#if SUBVIEW_BASED_BORDER
                                btnButtonInPadding.Border!.CloseButton.Visible = true;

                                view.Border!.CloseButton.Visible = true;
                                view.Border!.CloseButton.Accept += (_, _) =>
                                                                  {
                                                                      MessageBox.Query (20, 7, "Hi", "Window Close Button Pressed!", "Ok");
                                                                      e.Handled = true;
                                                                  };

                                view.Accept += (_, _) => MessageBox.Query (20, 7, "Hi", "Window Close Button Pressed!", "Ok");
#endif
                              };

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = window;
        editor.AutoSelectAdornments = true;

        app.Run (appWindow);
    }
}
