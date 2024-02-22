using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ScrollBar BuiltIn", "Demonstrates the scroll bar built-in the Padding Adornment.")]
[ScenarioCategory ("Controls")]
public class ScrollBars : Scenario
{
    public override void Init ()
    {
        Application.Init ();
        Application.Top.ColorScheme = Colors.ColorSchemes ["Base"];
    }

    public override void Setup ()
    {
        var view = new View
        {
            X = Pos.Center (), Y = Pos.Center (), Width = 15, Height = 8, ScrollBarType = ScrollBarType.Both,
            Text = "First Line\nSecond Line\nThird Line\nFourth Line\nFifth Line\nSixth Line\nSeventh Line",
            UseNegativeBoundsLocation = true
        };

        //var view = new View
        //{
        //    X = 5, Y = 5, Width = 9, Height = 6, ScrollBarType = ScrollBarType.Both,
        //    Text = "First Line\nSecond Line\nThird Line\nFourth Line\nFifth Line\nSixth Line\nSeventh Line", UseNegativeBoundsLocation = true
        //};
        view.TextFormatter.WordWrap = false;
        view.TextFormatter.MultiLine = true;
        view.TextFormatter.FillRemaining = true;
        view.CanFocus = true;
        view.Padding.ColorScheme = Colors.ColorSchemes ["Menu"];
        string [] strings = view.Text.Split ("\n").ToArray ();
        view.ScrollColsSize = strings.OrderByDescending (s => s.Length).First ().GetColumns ();
        view.ScrollRowsSize = strings.Length;
        view.Margin.Thickness = new Thickness (1);
        view.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        view.BorderStyle = LineStyle.Single;
        var win = new Window ();
        win.Add (view);

        var btn = new Button { X = Pos.Center (), Y = Pos.Bottom (view), Text = "Test" };
        win.Add (btn);
        Application.Top.Add (win);
    }
}
