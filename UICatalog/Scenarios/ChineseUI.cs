using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ChineseUI", "Chinese UI")]
[ScenarioCategory ("Unicode")]
public class ChineseUI : Scenario
{
    public override void Init ()
    {
        Application.Init ();

        Toplevel top = new ();

        var win = new Window
        {
            Title = "Test",
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        top.Add (win);

        var buttonPanel = new FrameView
        {
            Title = "Command",
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = 5
        };
        win.Add (buttonPanel);

        var btn = new Button { X = 1, Y = 1, Text = "你" }; // v1: A

        btn.Accept += (s, e) =>
                       {
                           int result = MessageBox.Query (
                                                          "Confirm",
                                                          "Are you sure you want to quit ui?",
                                                          0,
                                                          "Yes",
                                                          "No"
                                                         );

                           if (result == 0)
                           {
                               RequestStop ();
                           }
                       };

        buttonPanel.Add (
                         btn,
                         new Button { X = 12, Y = 1, Text = "好" }, // v1: B
                         new Button { X = 22, Y = 1, Text = "呀" } // v1: C
                        );

        Application.Run (top);

        top.Dispose ();
    }

    public override void Run () { }
}
