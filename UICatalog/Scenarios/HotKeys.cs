using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("HotKeys", "Demonstrates how HotKeys work.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory("Mouse and Keyboard")]
public class HotKeys : Scenario
{
    public override void Init ()
    {
        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
        Application.Top.BorderStyle = LineStyle.RoundedDotted;
        Application.Top.Title = $"{Application.QuitKey} to _Quit - Scenario: {GetName ()}";
    }

    public override void Run ()
    {
        var textViewLabel = new Label { Text = "_Text View:", X = 0, Y = 0 };
        Application.Top.Add (textViewLabel);
        
        var textField = new TextField (){ X = Pos.Right (textViewLabel) + 1, Y = 0, Width = 10 };
        Application.Top.Add (textField);

        var button = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "_Press me!" };
        Application.Top.Add (button);

        Application.Run (Application.Top);
    }
}
