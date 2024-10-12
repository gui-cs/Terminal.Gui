using Terminal.Gui;

namespace UICatalog.Scenarios;



[ScenarioMetadata ("Ansi Requests", "Demonstration of how to send ansi requests.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
public class AnsiRequestsScenario : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var win = new Window { Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}" };

        var btn = new Button ()
        {
            Text = "Send DAR",
            Width = Dim.Auto ()
        };



        var tv = new TextView ()
        {
            Y = Pos.Bottom (btn),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        btn.Accepting += (s, e) =>
                         {
                             // Ask for device attributes (DAR)
                             var p = Application.Driver.GetParser ();
                             p.ExpectResponse ("c", (r) => tv.Text += r + '\n');
                             Application.Driver.RawWrite (EscSeqUtils.CSI_SendDeviceAttributes);

                         };

        win.Add (tv);
        win.Add (btn);

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }
}