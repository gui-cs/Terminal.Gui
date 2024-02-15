using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("HotKeys", "Demonstrates how HotKeys work.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory("Mouse and Keyboard")]
public class HotKeys : Scenario
{
    public override void Init ()
    {
        // The base `Scenario.Init` implementation:
        //  - Calls `Application.Init ()`
        //  - Adds a full-screen Window to Application.Top with a title
        //    that reads "Press <hotkey> to Quit". Access this Window with `this.Win`.
        //  - Sets the Theme & the ColorScheme property of `this.Win` to `colorScheme`.
        // To override this, implement an override of `Init`.

        //base.Init ();

        // A common, alternate, implementation where `this.Win` is not used is below. This code
        // leverages ConfigurationManager to borrow the color scheme settings from UICatalog:

        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
        Application.Top.BorderStyle = LineStyle.RoundedDotted;
        Application.Top.Title = $"{Application.QuitKey} to _Quit - Scenario: {GetName ()}";
    }

    public override void Setup ()
    {
        // Put scenario code here (in a real app, this would be the code
        // that would setup the app before `Application.Run` is called`).
        // With a Scenario, after UI Catalog calls `Scenario.Setup` it calls
        // `Scenario.Run` which calls `Application.Run`. Example:

        var button = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "_Press me!" };
        Application.Top.Add (button);
    }
}
