#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Links", "Demonstrates how Links work.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Links : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        Label textLabel = new ()
        {
            Text = "_Text:",
            X = 1,
            Y = 1
        };
        mainWindow.Add (textLabel);

        TextField textField = new ()
        {
            X = Pos.Right (textLabel) + 2,
            Y = 1,
            Width = 20
        };
        mainWindow.Add (textField);

        Label urlLabel = new ()
        {
            Text = "_Url:",
            X = 1,
            Y = Pos.Bottom (textField) + 1
        };
        mainWindow.Add (urlLabel);

        TextField urlField = new ()
        {
            X = Pos.Right (urlLabel) + 2,
            Y = Pos.Bottom (textField) + 1,
            Width = 64
        };
        mainWindow.Add (urlField);

        Label simpleUrlLabel = new ()
        {
            X = 1,
            Y = Pos.Bottom (urlField) + 2
        };
        mainWindow.Add (simpleUrlLabel);

        FrameView linkFrame = new ()
        {
            Title = "_Link rendering",
            X = 0,
            Y = Pos.Bottom (simpleUrlLabel) + 2,
            Width = 64,
            Height = 8,
            AssignHotKeys = true
        };

        Link link = new ()
        {
            X = 1,
            Y = 1,
            Height = 1,
            Width = 64
        };

        link.UrlChanged += (s, e) => simpleUrlLabel.Text = link.Url;
        textField.ValueChanged += (s, e) => link.Text = e.NewValue ?? link.Url;
        urlField.ValueChanged += (s, e) => link.Url = e.NewValue ?? Link.DEFAULT_URL;
        linkFrame.Add (link);

        textField.Text = "GitHub repo";
        urlField.Text = "https://github.com/gui-cs/Terminal.Gui";

        Button copyButton = new ()
        {
            Title = "_Copy",
            X = Pos.Center (),
            Y = Pos.Bottom (link) + 2,
            
        };
        copyButton.Accepting += (s, e) => link.Copy ();

        linkFrame.Add (copyButton);

        mainWindow.Add (linkFrame);

        app.Run (mainWindow);
    }

}
