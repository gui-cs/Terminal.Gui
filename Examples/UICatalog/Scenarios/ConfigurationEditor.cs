#nullable enable
using System.Reflection;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Configuration Editor", "Edits of Terminal.Gui Config Files")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Files and IO")]
[ScenarioCategory ("TextView")]
[ScenarioCategory ("Configuration")]
public class ConfigurationEditor : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        // TODO: Restore full Configuration Editor after TabView rewrite (#4183)
        using Window win = new ()
        {
            Title = "Configuration Editor (TabView rewrite pending)"
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
