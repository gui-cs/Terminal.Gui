using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ChineseUI", "Chinese UI")]
[ScenarioCategory ("Unicode")]
public class ChineseUI : Scenario {
    public override void Init () {
        Application.Init ();
        Toplevel top = Application.Top;

        var win = new Window {
                                 Title = "Test",
                                 X = 0,
                                 Y = 0,
                                 Width = Dim.Fill (),
                                 Height = Dim.Fill ()
                             };
        top.Add (win);

        var buttonPanel = new FrameView {
                                            Title = "Command",
                                            X = 0,
                                            Y = 1,
                                            Width = Dim.Fill (),
                                            Height = 5
                                        };
        win.Add (buttonPanel);

        var btn = new Button (1, 1, "你", true); // v1: A
        btn.Clicked += (s, e) => {
            int result = MessageBox.Query (
                                           "Confirm",
                                           "Are you sure you want to quit ui?",
                                           0,
                                           "Yes",
                                           "No");
            if (result == 0) {
                RequestStop ();
            }
        };

        buttonPanel.Add (
                         btn,
                         new Button (12, 1, "好"), // v1: B
                         new Button (22, 1, "呀") // v1: C
                        );

        Application.Run ();
    }

    public override void Run () { }
}
