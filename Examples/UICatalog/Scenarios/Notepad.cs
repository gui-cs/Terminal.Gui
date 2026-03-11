#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Notepad", "Multi-tab text editor using the TabView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TextView")]
public class Notepad : Scenario
{
    // TODO: Restore full Notepad implementation after TabView rewrite (#4183)

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ()
        {
            Title = "Notepad (TabView rewrite pending)"
        };

        win.Add (new Label
        {
            Text = "This scenario requires the TabView rewrite (#4183).",
            X = Pos.Center (),
            Y = Pos.Center ()
        });

        app.Run (win);
    }
}
