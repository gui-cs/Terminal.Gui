namespace UICatalog.Scenarios;

[ScenarioMetadata ("BigText", "Demonstrates the BigText view for large text rendering.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Drawing")]
public class BigTextExample : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new () { Title = GetQuitKeyAndName () };

        Label inputLabel = new () { Text = "Enter text to render:" };
        app.Add (inputLabel);

        TextField textField = new ()
        {
            X = Pos.Right (inputLabel) + 1,
            Y = Pos.Top (inputLabel),
            Width = 40,
            Text = "Hello World!"
        };
        app.Add (textField);

        Button renderButton = new ()
        {
            X = Pos.Right (textField) + 1,
            Y = Pos.Top (inputLabel),
            Text = "Render"
        };
        app.Add (renderButton);

        BigText dynamicText = new ()
        {
            X = 0,
            Y = Pos.Bottom (textField) + 1,
            Text = textField.Text,
            GlyphHeight = 8,
            Style = LineStyle.Single
        };
        app.Add (dynamicText);

        renderButton.Accepting += (s, e) =>
                                  {
                                      dynamicText.Text = textField.Text;
                                      e.Handled = true;
                                  };

        Label helpLabel = new ()
        {
            X = Pos.Center (),
            Y = Pos.AnchorEnd (),
            Text = "BigText uses TTF fonts rendered as LineCanvas box-drawing characters."
        };
        app.Add (helpLabel);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
