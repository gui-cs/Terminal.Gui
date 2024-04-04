using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scrolling Without ScrollBars", "Demonstrates the scrolling without EnableScrollBars.")]
[ScenarioCategory ("Controls")]
public class ScrollingWithoutEnableScrollBars : Scenario
{
    public override void Setup ()
    {
        var text = "First Line\nSecond Line\nThird Line\nFourth Line\nFifth Line\nSixth Line\nSeventh Line";

        var viewWithoutAdornments = new View
        {
            X = 0, Y = Pos.Center (), Width = 12, Height = 6,
            Text = text,
            UseContentOffset = true
        };
        SetViewProperties (viewWithoutAdornments);
        Win.Add (viewWithoutAdornments);

        Win.Add (new Label { X = 0, Y = Pos.Top (viewWithoutAdornments) - 2, Text = "No Adornments:" });

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
        Win.Add (viewWithMarginBorderPadding);

        Win.Add (new Label { X = Pos.Left (viewWithMarginBorderPadding), Y = Pos.Top (viewWithMarginBorderPadding) - 2, Text = "All Adornments:" });

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
        Win.Add (viewWithMarginBorder);

        Win.Add (new Label { X = Pos.Left (viewWithMarginBorder), Y = Pos.Top (viewWithMarginBorder) - 2, Text = "With Margin/Border:" });

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
        Win.Add (viewWithMargin);

        Win.Add (new Label { X = Pos.Left (viewWithMargin), Y = Pos.Top (viewWithMargin) - 2, Text = "With Margin:" });

        var btn = new Button { X = Pos.Center (), Y = Pos.Bottom (viewWithMarginBorderPadding) + 1, Text = "Tab or click to select the views" };
        Win.Add (btn);

        var keepCheckBox = new CheckBox
        {
            X = 0,
            Y = 0,
            Text = "Keep Content Always In Viewport",
            Checked = false
        };
        keepCheckBox.Toggled += (s, e) =>
                                {
                                    viewWithMargin.KeepContentAlwaysInContentArea = !(bool)keepCheckBox.Checked;
                                    viewWithMarginBorder.KeepContentAlwaysInContentArea = !(bool)keepCheckBox.Checked;
                                    viewWithMarginBorderPadding.KeepContentAlwaysInContentArea = !(bool)keepCheckBox.Checked;
                                    viewWithoutAdornments.KeepContentAlwaysInContentArea = !(bool)keepCheckBox.Checked;
                                };
        Win.Add (keepCheckBox);

        viewWithMargin.TabIndex = 1;
        viewWithMarginBorder.TabIndex = 2;
        viewWithMarginBorderPadding.TabIndex = 3;
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
