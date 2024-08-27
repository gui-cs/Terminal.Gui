using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("View Experiments", "v2 View Experiments")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Borders")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Proof of Concept")]
public class ViewExperiments : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            TabStop = TabBehavior.TabGroup
        };

        var editor = new AdornmentsEditor
        {
            X = 0,
            Y = 0,
            TabStop = TabBehavior.NoStop
        };
        app.Add (editor);

        FrameView testFrame = new ()
        {
            Title = "_1 Test Frame",
            X = Pos.Right (editor),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
        };

        app.Add (testFrame);

        Button button = new ()
        {
            X = 0,
            Y = 0,
            Title = $"TopButton _{GetNextHotKey ()}",
        };

        testFrame.Add (button);

        button = new ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Title = $"TopButton _{GetNextHotKey ()}",
        };

        testFrame.Add (button);

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = testFrame;
        editor.AutoSelectAdornments = true;

        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();

        return;
    }

    private int _hotkeyCount;

    private char GetNextHotKey ()
    {
        return (char)((int)'A' + _hotkeyCount++);
    }
}
