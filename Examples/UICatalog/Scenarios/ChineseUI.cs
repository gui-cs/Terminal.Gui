
namespace UICatalog.Scenarios;

[ScenarioMetadata ("ChineseUI", "Chinese UI")]
[ScenarioCategory ("Text and Formatting")]
public class ChineseUI : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        var win = new Window
        {
            Title = GetQuitKeyAndName (),
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

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

        btn.Accepting += (s, e) =>
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
                              Application.RequestStop ();
                          }
                      };

        buttonPanel.Add (
                         btn,
                         new Button { X = 12, Y = 1, Text = "好" }, // v1: B
                         new Button { X = 22, Y = 1, Text = "呀" } // v1: C
                        );

        Application.Run (win);

        win.Dispose ();

        Application.Shutdown ();
    }
}
