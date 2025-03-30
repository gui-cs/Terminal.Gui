using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("View Experiments", "v2 View Experiments")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Adornments")]
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
            TabStop = TabBehavior.NoStop,
            AutoSelectViewToEdit = true,
            ShowViewIdentifier = true
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

        var popoverView = new View ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = 30,
            Height = 10,
            Title = "Popover",
            Text = "This is a popover",
            Visible = false,
            CanFocus = true,
            Arrangement = ViewArrangement.Resizable | ViewArrangement.Movable
        };
        popoverView.BorderStyle = LineStyle.RoundedDotted;

        Button popoverButton = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Title = $"_Close",
        };
        //popoverButton.Accepting += (sender, e) => Application.Popover!.Visible = false;
        popoverView.Add (popoverButton);

        button.Accepting += ButtonAccepting;

        void ButtonAccepting (object sender, CommandEventArgs e)
        {
            //Application.Popover = popoverView;
            //Application.Popover!.Visible = true;
        }

        testFrame.MouseClick += TestFrameOnMouseClick;

        void TestFrameOnMouseClick (object sender, MouseEventArgs e)
        {
            if (e.Flags == MouseFlags.Button3Clicked)
            {
                popoverView.X = e.ScreenPosition.X;
                popoverView.Y = e.ScreenPosition.Y;
                //Application.Popover = popoverView;
                //Application.Popover!.Visible = true;
            }
        }

        testFrame.Add (button);

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = testFrame;
        editor.AutoSelectAdornments = true;

        Application.Run (app);
        popoverView.Dispose ();
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
