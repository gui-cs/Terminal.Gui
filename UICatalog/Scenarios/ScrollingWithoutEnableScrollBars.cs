using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scrolling Without ScrollBars", "Demonstrates the scrolling without EnableScrollBars.")]
[ScenarioCategory ("Controls")]
public class ScrollingWithoutEnableScrollBars : Scenario
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

        var viewWithoutAdornments = new View
        {
            X = 0, Y = Pos.Center (), Width = 12, Height = 6,
            Text = text,
            UseContentOffset = true
        };
        SetViewProperties (viewWithoutAdornments);
        win.Add (viewWithoutAdornments);

        win.Add (new Label { X = 0, Y = Pos.Top (viewWithoutAdornments) - 2, Text = "No Adornments:" });

        var viewWithMarginBorderPadding = new View
        {
            X = Pos.AnchorEnd () - 15, Y = Pos.Center (), Width = 15, Height = 8,
            Text = text,
            UseContentOffset = true
        };
        viewWithMarginBorderPadding.Margin.Thickness = new (1);
        viewWithMarginBorderPadding.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        viewWithMarginBorderPadding.BorderStyle = LineStyle.Single;
        viewWithMarginBorderPadding.Padding.Thickness = new (1);
        viewWithMarginBorderPadding.Padding.ColorScheme = Colors.ColorSchemes ["Menu"];
        SetViewProperties (viewWithMarginBorderPadding);
        win.Add (viewWithMarginBorderPadding);

        win.Add (new Label { X = Pos.Left (viewWithMarginBorderPadding), Y = Pos.Top (viewWithMarginBorderPadding) - 2, Text = "All Adornments:" });

        var viewWithMarginBorder = new View
        {
            X = Pos.Left (viewWithMarginBorderPadding) - 30, Y = Pos.Center (), Width = 15, Height = 8,
            Text = text,
            UseContentOffset = true
        };
        viewWithMarginBorder.Margin.Thickness = new (1);
        viewWithMarginBorder.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];
        viewWithMarginBorder.BorderStyle = LineStyle.Single;
        SetViewProperties (viewWithMarginBorder);
        win.Add (viewWithMarginBorder);

        win.Add (new Label { X = Pos.Left (viewWithMarginBorder), Y = Pos.Top (viewWithMarginBorder) - 2, Text = "With Margin/Border:" });

        var viewWithMargin = new View
        {
            X = Pos.Left (viewWithMarginBorder) - 30, Y = Pos.Center (), Width = 13, Height = 8,
            Text = text,
            UseContentOffset = true,
            BorderStyle = LineStyle.None
        };
        viewWithMargin.Margin.Thickness = new (1);
        viewWithMargin.Margin.ColorScheme = Colors.ColorSchemes ["Menu"];
        SetViewProperties (viewWithMargin);
        win.Add (viewWithMargin);

        win.Add (new Label { X = Pos.Left (viewWithMargin), Y = Pos.Top (viewWithMargin) - 2, Text = "With Margin:" });

        var btn = new Button { X = Pos.Center (), Y = Pos.Bottom (viewWithMarginBorderPadding) + 1, Text = "Tab or click to select the views" };
        win.Add (btn);

        viewWithMargin.TabIndex = 1;
        viewWithMarginBorder.TabIndex = 2;
        viewWithMarginBorderPadding.TabIndex = 3;

        Application.Top.Add (win);
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
