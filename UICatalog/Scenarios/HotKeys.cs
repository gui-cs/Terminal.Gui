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
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
        Top.BorderStyle = LineStyle.RoundedDotted;
        Top.Title = $"{Application.QuitKey} to _Quit - Scenario: {GetName ()}";
    }

    public override void Run ()
    {
        var textViewLabel = new Label { Text = "_TextView:", X = 0, Y = 0 };
        Top.Add (textViewLabel);
        
        var textField = new TextField (){ X = Pos.Right (textViewLabel) + 1, Y = 0, Width = 10 };
        Top.Add (textField);

        var viewLabel = new Label { Text = "_View:", X = 0, Y = Pos.Bottom (textField) + 1 };
        Top.Add (viewLabel);

        var view = new View () { 
            Title = "View (_focusable)", 
            Text = "Text renders _Underscore", 
            CanFocus = true,
            X = Pos.Right (viewLabel) + 1, Y = Pos.Top (viewLabel), Width = 30, Height = 3,
            BorderStyle = LineStyle.Dashed,
        };
        Top.Add (view);

        viewLabel = new Label { Text = "Vi_ew:", X = 0, Y = Pos.Bottom (view) + 1 };
        Top.Add (viewLabel);

        view = new View ()
        {
            Title = "View (n_ot focusable)",
            Text = "Text renders _Underscore",
            X = Pos.Right (viewLabel) + 1, Y = Pos.Top (viewLabel), Width = 30, Height = 3,
            BorderStyle = LineStyle.Dashed,
        };
        Top.Add (view);

        var labelWithFrameLabel = new Label { Text = "_Label with Frame:", X = 0, Y = Pos.Bottom (view) + 1 };
        Top.Add (labelWithFrameLabel);

        var labelWithFrameFocusable = new Label ()
        {
            AutoSize = false,
            Title = "Label _with Frame (focusable)",
            CanFocus = true,
            X = Pos.Right (labelWithFrameLabel) + 1, Y = Pos.Top (labelWithFrameLabel), Width = 40, Height = 3,
            BorderStyle = LineStyle.Dashed,
        };
        Top.Add (labelWithFrameFocusable);

        labelWithFrameLabel = new Label { Text = "L_abel with Frame:", X = 0, Y = Pos.Bottom (labelWithFrameFocusable) + 1 };
        Top.Add (labelWithFrameLabel);

        var labelWithFrame = new Label ()
        {
            AutoSize = false,
            Title = "Label with Frame (_not focusable)",
            X = Pos.Right (labelWithFrameLabel) + 1, Y = Pos.Top (labelWithFrameLabel), Width = 40, Height = 3,
            BorderStyle = LineStyle.Dashed,
        };
        Top.Add (labelWithFrame);

        
        var buttonWithFrameLabel = new Label { Text = "_Button with Frame:", X = 0, Y = Pos.Bottom (labelWithFrame) + 1 };
        Top.Add (buttonWithFrameLabel);

        var buttonWithFrameFocusable = new Button ()
        {
            AutoSize = false,
            Title = "B_utton with Frame (focusable)",
            CanFocus = true,
            X = Pos.Right (buttonWithFrameLabel) + 1, Y = Pos.Top (buttonWithFrameLabel), Width = 40, Height = 3,
            BorderStyle = LineStyle.Dashed,
        };
        Top.Add (buttonWithFrameFocusable);

        buttonWithFrameLabel = new Label { Text = "Butt_on with Frame:", X = 0, Y = Pos.Bottom (buttonWithFrameFocusable) + 1 };
        Top.Add (buttonWithFrameLabel);

        var buttonWithFrame = new Button ()
        {
            AutoSize = false,
            Title = "Button with Frame (not focusab_le)",
            X = Pos.Right (buttonWithFrameLabel) + 1, Y = Pos.Top (buttonWithFrameLabel), Width = 40, Height = 3,
            CanFocus = false,
            BorderStyle = LineStyle.Dashed,
        };
        Top.Add (buttonWithFrame);



        var checkboxWithFrameLabel = new Label { Text = "_Checkbox with Frame:", X = 0, Y = Pos.Bottom (buttonWithFrame) + 1 };
        Top.Add (checkboxWithFrameLabel);

        var checkboxWithFrameFocusable = new CheckBox
        {
            AutoSize = false,
            Title = "C_heckbox with Frame (focusable)",
            CanFocus = true,
            X = Pos.Right (checkboxWithFrameLabel) + 1, Y = Pos.Top (checkboxWithFrameLabel), Width = 40, Height = 3,
            BorderStyle = LineStyle.Dashed,
        };
        Top.Add (checkboxWithFrameFocusable);

        checkboxWithFrameLabel = new Label { Text = "Checkb_ox with Frame:", X = 0, Y = Pos.Bottom (checkboxWithFrameFocusable) + 1 };
        Top.Add (checkboxWithFrameLabel);

        var checkboxWithFrame = new CheckBox
        {
            AutoSize = false,
            Title = "Checkbox with Frame (not focusable)",
            X = Pos.Right (checkboxWithFrameLabel) + 1, Y = Pos.Top (checkboxWithFrameLabel), Width = 40, Height = 3,
            CanFocus = false,
            BorderStyle = LineStyle.Dashed,
        };
        Top.Add (checkboxWithFrame);


        var button = new Button { X = Pos.Center (), Y = Pos.AnchorEnd (1), Text = "_Press me!" };
        Top.Add (button);

        Application.Run (Top);
    }
}
