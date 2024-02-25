using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ScrollBars", "Demonstrates the scroll bar built-in the Padding Adornment.")]
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
        var text = "First Line\nSecond Line\nThird Line\nFourth Line\nFifth Line\nSixth Line\nSeventh Line";

        var win = new Window ();

        var viewOnMargin = new View
        {
            X = 0, Y = Pos.Center (), Width = 12, Height = 6,
            Text = text,
            UseContentOffset = true
        };
        viewOnMargin.Margin.ScrollBarType = ScrollBarType.Both;
        SetViewProperties (viewOnMargin);
        win.Add (viewOnMargin);

        win.Add (new Label { X = 0, Y = Pos.Top (viewOnMargin) - 2, Text = "On Margin:" });

        var viewOnContentArea = new View
        {
            X = Pos.AnchorEnd () - 15, Y = Pos.Center (), Width = 15, Height = 8,
            Text = text,
            UseContentOffset = true,
            ScrollBarType = ScrollBarType.Both
        };
        viewOnContentArea.Margin.Thickness = new Thickness (1);
        viewOnContentArea.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        viewOnContentArea.BorderStyle = LineStyle.Single;
        SetViewProperties (viewOnContentArea);
        win.Add (viewOnContentArea);

        win.Add (new Label { X = Pos.Left (viewOnContentArea), Y = Pos.Top (viewOnContentArea) - 2, Text = "On ContentArea:" });

        var viewOnPadding = new View
        {
            X = Pos.Left (viewOnContentArea) - 30, Y = Pos.Center (), Width = 15, Height = 8,
            Text = text,
            UseContentOffset = true
        };
        viewOnPadding.Padding.ScrollBarType = ScrollBarType.Both;
        viewOnPadding.Margin.Thickness = new Thickness (1);
        viewOnPadding.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        viewOnPadding.BorderStyle = LineStyle.Single;
        viewOnPadding.Padding.ColorScheme = Colors.ColorSchemes ["Menu"];
        SetViewProperties (viewOnPadding);
        win.Add (viewOnPadding);

        win.Add (new Label { X = Pos.Left (viewOnPadding), Y = Pos.Top (viewOnPadding) - 2, Text = "On Padding:" });

        var viewOnBorder = new View
        {
            X = Pos.Left (viewOnPadding) - 30, Y = Pos.Center (), Width = 13, Height = 8,
            Text = text,
            UseContentOffset = true,
            BorderStyle = LineStyle.None
        };
        viewOnBorder.Border.ScrollBarType = ScrollBarType.Both;
        viewOnBorder.Margin.Thickness = new Thickness (1);
        viewOnBorder.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        SetViewProperties (viewOnBorder);
        win.Add (viewOnBorder);

        win.Add (new Label { X = Pos.Left (viewOnBorder), Y = Pos.Top (viewOnBorder) - 2, Text = "On Border:" });

        var btn = new Button { X = Pos.Center (), Y = Pos.Bottom (viewOnContentArea) + 1, Text = "Test" };
        win.Add (btn);

        viewOnBorder.TabIndex = 1;
        viewOnPadding.TabIndex = 2;
        viewOnContentArea.TabIndex = 3;

        Application.Top.Add (win);
    }

    private void SetViewProperties (View view)
    {
        view.TextFormatter.WordWrap = false;
        view.TextFormatter.MultiLine = true;
        view.TextFormatter.FillRemaining = true;
        view.CanFocus = true;
        string [] strings = view.Text.Split ("\n").ToArray ();
        view.ContentSize = new Size (strings.OrderByDescending (s => s.Length).First ().GetColumns (), strings.Length);
    }
}
