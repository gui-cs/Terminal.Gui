using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Combining Marks", "Illustrates how Unicode Combining Marks work (or don't).")]
[ScenarioCategory ("Text and Formatting")]
public class CombiningMarks : Scenario
{
    public override void Init ()
    {
        Application.Init ();
        ConfigurationManager.Themes.Theme = Theme;
        ConfigurationManager.Apply ();
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
    }

    public override void Setup ()
    {
        Top.DrawContentComplete += (s, e) =>
                                               {
                                                   Application.Driver.Move (0, 0);
                                                   Application.Driver.AddStr ("Terminal.Gui only supports combining marks that normalize. See Issue #2616.");
                                                   Application.Driver.Move (0, 2);
                                                   Application.Driver.AddStr ("\u0301\u0301\u0328<- \"\\u301\\u301\\u328]\" using AddStr.");
                                                   Application.Driver.Move (0, 3);
                                                   Application.Driver.AddStr ("[a\u0301\u0301\u0328]<- \"[a\\u301\\u301\\u328]\" using AddStr.");
                                                   Application.Driver.Move (0, 4);
                                                   Application.Driver.AddRune ('[');
                                                   Application.Driver.AddRune ('a');
                                                   Application.Driver.AddRune ('\u0301');
                                                   Application.Driver.AddRune ('\u0301');
                                                   Application.Driver.AddRune ('\u0328');
                                                   Application.Driver.AddRune (']');
                                                   Application.Driver.AddStr ("<- \"[a\\u301\\u301\\u328]\" using AddRune for each.");
                                               };
    }
}
