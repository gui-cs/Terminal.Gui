using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("RuneWidthGreaterThanOne", "Test rune width greater than one")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Tests")]
public class RuneWidthGreaterThanOne : Scenario
{
    private Button _button;
    private Label _label;
    private Label _labelR;
    private Label _labelV;
    private string _lastRunesUsed;
    private TextField _text;
    private Window _win;

    public override void Main ()
    {
        Application.Init ();

        Toplevel topLevel = new ();

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem (
                                 "Padding",
                                 new MenuItem []
                                 {
                                     new (
                                          "With Padding",
                                          "",
                                          () => _win.Padding.Thickness =
                                                    new Thickness (1)
                                         ),
                                     new (
                                          "Without Padding",
                                          "",
                                          () => _win.Padding.Thickness =
                                                    new Thickness (0)
                                         )
                                 }
                                ),
                new MenuBarItem (
                                 "BorderStyle",
                                 new MenuItem []
                                 {
                                     new (
                                          "Single",
                                          "",
                                          () => _win.BorderStyle = LineStyle.Single
                                         ),
                                     new (
                                          "None",
                                          "",
                                          () => _win.BorderStyle = LineStyle.None
                                         )
                                 }
                                ),
                new MenuBarItem (
                                 "Runes length",
                                 new MenuItem []
                                 {
                                     new ("Wide", "", WideRunes),
                                     new ("Narrow", "", NarrowRunes),
                                     new ("Mixed", "", MixedRunes)
                                 }
                                )
            ]
        };

        _label = new Label
        {
            X = Pos.Center (), Y = 1, ColorScheme = new ColorScheme { Normal = Colors.ColorSchemes ["Base"].Focus }
        };
        _text = new TextField { X = Pos.Center (), Y = 3, Width = 20 };
        _button = new Button { X = Pos.Center (), Y = 5 };
        _labelR = new Label { X = Pos.AnchorEnd (30), Y = 18 };

        _labelV = new Label
        {
            TextDirection = TextDirection.TopBottom_LeftRight, X = Pos.AnchorEnd (30), Y = Pos.Bottom (_labelR)
        };
        _win = new Window { X = 5, Y = 5, Width = Dim.Fill (22), Height = Dim.Fill (5) };
        _win.Add (_label, _text, _button, _labelR, _labelV);
        topLevel.Add (menu, _win);

        WideRunes ();

        //NarrowRunes ();
        //MixedRunes ();
        Application.Run (topLevel);
        topLevel.Dispose ();
        Application.Shutdown ();
    }

    private void MixedMessage (object sender, EventArgs e) { MessageBox.Query ("Say Hello 你", $"Hello {_text.Text}", "Ok"); }

    private void MixedRunes ()
    {
        UnsetClickedEvent ();
        _label.Text = "Enter your name 你:";
        _text.Text = "gui.cs 你:";
        _button.Text = "Say Hello 你";
        _button.Accepting += MixedMessage;
        _labelR.X = Pos.AnchorEnd (21);
        _labelR.Y = 18;
        _labelR.Text = "This is a test text 你";
        _labelV.X = Pos.AnchorEnd (21);
        _labelV.Y = Pos.Bottom (_labelR);
        _labelV.Text = "This is a test text 你";
        _win.Title = "HACC Demo 你";
        _lastRunesUsed = "Mixed";
        Application.LayoutAndDraw ();
    }

    private void NarrowMessage (object sender, EventArgs e) { MessageBox.Query ("Say Hello", $"Hello {_text.Text}", "Ok"); }

    private void NarrowRunes ()
    {
        UnsetClickedEvent ();
        _label.Text = "Enter your name:";
        _text.Text = "gui.cs";
        _button.Text = "Say Hello";
        _button.Accepting += NarrowMessage;
        _labelR.X = Pos.AnchorEnd (19);
        _labelR.Y = 18;
        _labelR.Text = "This is a test text";
        _labelV.X = Pos.AnchorEnd (19);
        _labelV.Y = Pos.Bottom (_labelR);
        _labelV.Text = "This is a test text";
        _win.Title = "HACC Demo";
        _lastRunesUsed = "Narrow";
        Application.LayoutAndDraw ();
    }

    private void UnsetClickedEvent ()
    {
        switch (_lastRunesUsed)
        {
            case "Narrow":
                _button.Accepting -= NarrowMessage;

                break;
            case "Mixed":
                _button.Accepting -= MixedMessage;

                break;
            case "Wide":
                _button.Accepting -= WideMessage;

                break;
        }
    }

    private void WideMessage (object sender, EventArgs e) { MessageBox.Query ("こんにちはと言う", $"こんにちは {_text.Text}", "Ok"); }

    private void WideRunes ()
    {
        UnsetClickedEvent ();
        _label.Text = "あなたの名前を入力してください：";
        _text.Text = "ティラミス";
        _button.Text = "こんにちはと言う";
        _button.Accepting += WideMessage;
        _labelR.X = Pos.AnchorEnd (29);
        _labelR.Y = 18;
        _labelR.Text = "あなたの名前を入力してください";
        _labelV.X = Pos.AnchorEnd (29);
        _labelV.Y = Pos.Bottom (_labelR);
        _labelV.Text = "あなたの名前を入力してください";
        _win.Title = "デモエムポンズ";
        _lastRunesUsed = "Wide";
        Application.LayoutAndDraw ();
    }
}
