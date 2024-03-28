using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ScrollBars", "Demonstrates the scroll bar built-in on Adornments and view.")]
[ScenarioCategory ("Controls")]
public class ScrollBars : Scenario
{
    public override void Setup ()
    {
        var text = "First Line\nSecond Line\nThird Line\nFourth Line\nFifth Line\nSixth Line\nSeventh Line";

        var viewOnMargin = new View
        {
            X = 0, Y = Pos.Center (), Width = 12, Height = 6,
            Text = text,
            UseContentOffset = true
        };
        viewOnMargin.Margin.EnableScrollBars = true;
        SetViewProperties (viewOnMargin);
        Win.Add (viewOnMargin);

        Win.Add (new Label { X = 0, Y = Pos.Top (viewOnMargin) - 2, Text = "On Margin:" });

        var viewOnContentArea = new View
        {
            X = Pos.AnchorEnd () - 15, Y = Pos.Center (), Width = 15, Height = 8,
            Text = text,
            UseContentOffset = true,
            EnableScrollBars = true
        };
        viewOnContentArea.Margin.Thickness = new (1);
        viewOnContentArea.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        viewOnContentArea.BorderStyle = LineStyle.Single;
        SetViewProperties (viewOnContentArea);
        Win.Add (viewOnContentArea);

        Win.Add (new Label { X = Pos.Left (viewOnContentArea), Y = Pos.Top (viewOnContentArea) - 2, Text = "On ContentArea:" });

        var viewOnPadding = new View
        {
            X = Pos.Left (viewOnContentArea) - 30, Y = Pos.Center (), Width = 15, Height = 8,
            Text = text,
            UseContentOffset = true
        };
        viewOnPadding.Padding.EnableScrollBars = true;
        viewOnPadding.Margin.Thickness = new (1);
        viewOnPadding.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        viewOnPadding.BorderStyle = LineStyle.Single;
        viewOnPadding.Padding.ColorScheme = Colors.ColorSchemes ["Menu"];
        SetViewProperties (viewOnPadding);
        Win.Add (viewOnPadding);

        Win.Add (new Label { X = Pos.Left (viewOnPadding), Y = Pos.Top (viewOnPadding) - 2, Text = "On Padding:" });

        var viewOnBorder = new View
        {
            X = Pos.Left (viewOnPadding) - 30, Y = Pos.Center (), Width = 13, Height = 8,
            Text = text,
            UseContentOffset = true,
            BorderStyle = LineStyle.None
        };
        viewOnBorder.Border.EnableScrollBars = true;
        viewOnBorder.Margin.Thickness = new (1);
        viewOnBorder.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        SetViewProperties (viewOnBorder);
        Win.Add (viewOnBorder);

        Win.Add (new Label { X = Pos.Left (viewOnBorder), Y = Pos.Top (viewOnBorder) - 2, Text = "On Border:" });

        var btn = new Button { X = Pos.Center (), Y = Pos.Bottom (viewOnContentArea) + 1, Text = "Tab or click to select the views" };
        Win.Add (btn);

        viewOnBorder.TabIndex = 1;
        viewOnPadding.TabIndex = 2;
        viewOnContentArea.TabIndex = 3;
    }

    private void SetViewProperties (View view)
    {
        view.TextFormatter.WordWrap = false;
        view.TextFormatter.MultiLine = true;
        view.TextFormatter.FillRemaining = true;
        view.CanFocus = true;
        string [] strings = view.Text.Split ("\n").ToArray ();
        view.ContentSize = new (strings.OrderByDescending (s => s.Length).First ().GetColumns (), strings.Length);
    }
}
