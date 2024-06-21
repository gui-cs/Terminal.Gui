using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("3D Effects Demo", "Demonstrates 3D UI Effects.")]
[ScenarioCategory ("Layout")]
public class ThreeD : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}"
        };


        var editor = new AdornmentsEditor ()
        {
            AutoSelectViewToEdit = true,
        };
        app.Add (editor);

        Window win = new ()
        {
            X = Pos.Right (editor),
            Y = 0,
            Width = Dim.Percent (30),
            Height = Dim.Percent (30),
            Title = "Shadow Window",
            Arrangement = ViewArrangement.Movable,
        };

        var buttonInWin = new Button
        {
            X = Pos.Center (),
            Y = Pos.Center (), Text = "Button in Window",
            //Shadow = true
        };
        win.Add (buttonInWin);
        app.Add (win);

        var button = new Button
        {
            X = Pos.Right (editor) + 10,
            Y = Pos.Center (), Text = "Button",
        };
        app.Add (button);


        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();

        return;
    }
}
