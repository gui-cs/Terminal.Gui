
namespace UICatalog.Scenarios;

[ScenarioMetadata ("HotKeys", "Demonstrates how HotKeys work.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class HotKeys : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        var textViewLabel = new Label { Text = "_TextView:", X = 0, Y = 0 };
        app.Add (textViewLabel);

        var textField = new TextField { X = Pos.Right (textViewLabel) + 1, Y = 0, Width = 10 };
        app.Add (textField);

        var viewLabel = new Label { Text = "_View:", X = 0, Y = Pos.Bottom (textField) + 1 };
        app.Add (viewLabel);

        var view = new View
        {
            Title = "View (_focusable)",
            Text = "Text renders _Underscore",
            CanFocus = true,
            X = Pos.Right (viewLabel) + 1, Y = Pos.Top (viewLabel), Width = 30, Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        app.Add (view);

        viewLabel = new () { Text = "Vi_ew:", X = 0, Y = Pos.Bottom (view) + 1 };
        app.Add (viewLabel);

        view = new ()
        {
            Title = "View (n_ot focusable)",
            Text = "Text renders _Underscore",
            X = Pos.Right (viewLabel) + 1, Y = Pos.Top (viewLabel), Width = 30, Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        app.Add (view);

        var labelWithFrameLabel = new Label { Text = "_Label with Frame:", X = 0, Y = Pos.Bottom (view) + 1 };
        app.Add (labelWithFrameLabel);

        var labelWithFrameFocusable = new Label
        {
            Title = "Label _with Frame (focusable)",
            CanFocus = true,
            X = Pos.Right (labelWithFrameLabel) + 1, Y = Pos.Top (labelWithFrameLabel), Width = 40, Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        app.Add (labelWithFrameFocusable);

        labelWithFrameLabel = new () { Text = "L_abel with Frame:", X = 0, Y = Pos.Bottom (labelWithFrameFocusable) + 1 };
        app.Add (labelWithFrameLabel);

        var labelWithFrame = new Label
        {
            Title = "Label with Frame (_not focusable)",
            X = Pos.Right (labelWithFrameLabel) + 1, Y = Pos.Top (labelWithFrameLabel), Width = 40, Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        app.Add (labelWithFrame);

        var buttonWithFrameLabel = new Label { Text = "_Button with Frame:", X = 0, Y = Pos.Bottom (labelWithFrame) + 1 };
        app.Add (buttonWithFrameLabel);

        var buttonWithFrameFocusable = new Button
        {
            Title = "B_utton with Frame (focusable)",
            CanFocus = true,
            X = Pos.Right (buttonWithFrameLabel) + 1, Y = Pos.Top (buttonWithFrameLabel), Width = 40,
            BorderStyle = LineStyle.Dashed
        };
        app.Add (buttonWithFrameFocusable);

        buttonWithFrameLabel = new () { Text = "Butt_on with Frame:", X = 0, Y = Pos.Bottom (buttonWithFrameFocusable) + 1 };
        app.Add (buttonWithFrameLabel);

        var buttonWithFrame = new Button
        {
            Title = "Button with Frame (not focusab_le)",
            X = Pos.Right (buttonWithFrameLabel) + 1, Y = Pos.Top (buttonWithFrameLabel), Width = 40,
            CanFocus = false,
            BorderStyle = LineStyle.Dashed
        };
        app.Add (buttonWithFrame);

        var checkboxWithFrameLabel = new Label { Text = "_Checkbox with Frame:", X = 0, Y = Pos.Bottom (buttonWithFrame) + 1 };
        app.Add (checkboxWithFrameLabel);

        var checkboxWithFrameFocusable = new CheckBox
        {
            Title = "C_heckbox with Frame (focusable)",
            CanFocus = true,
            X = Pos.Right (checkboxWithFrameLabel) + 1, Y = Pos.Top (checkboxWithFrameLabel), Width = 40, Height = 3,
            BorderStyle = LineStyle.Dashed
        };
        app.Add (checkboxWithFrameFocusable);

        checkboxWithFrameLabel = new () { Text = "Checkb_ox with Frame:", X = 0, Y = Pos.Bottom (checkboxWithFrameFocusable) + 1 };
        app.Add (checkboxWithFrameLabel);

        var checkboxWithFrame = new CheckBox
        {
            Title = "Checkbox with Frame (not focusable)",
            X = Pos.Right (checkboxWithFrameLabel) + 1, Y = Pos.Top (checkboxWithFrameLabel), Width = 40, Height = 3,
            CanFocus = false,
            BorderStyle = LineStyle.Dashed
        };
        app.Add (checkboxWithFrame);

        var button = new Button { X = Pos.Center (), Y = Pos.AnchorEnd (), Text = "_Press me!" };
        app.Add (button);

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
