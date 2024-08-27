using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ShadowStyles Demo", "Demonstrates ShadowStyles Effects.")]
[ScenarioCategory ("Layout")]
public class ShadowStyles : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()
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
            ShadowStyle = ShadowStyle.Transparent
        };

        var buttonInWin = new Button
        {
            X = Pos.Center (),
            Y = Pos.Center (), Text = "Button in Window",
            ShadowStyle = ShadowStyle.Opaque
        };
        win.Add (buttonInWin);
        app.Add (win);

        var button = new Button
        {
            X = Pos.Right (editor) + 10,
            Y = Pos.Center (), Text = "Button",
            ShadowStyle = ShadowStyle.Opaque
        };
        app.Add (button);

        Application.MouseEvent += ApplicationOnMouseEvent;

        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();

        return;

        void ApplicationOnMouseEvent (object sender, MouseEvent e)
        {
            if (!editor.AutoSelectViewToEdit || editor.FrameToScreen ().Contains (e.Position))
            {
                return;
            }

            // TODO: Add a setting (property) so only subviews of a specified view are considered.
            View view = e.View;

            if (view is { } && e.Flags == MouseFlags.Button1Clicked)
            {
                if (view is Adornment adornment)
                {
                    editor.ViewToEdit = adornment.Parent;
                }
                else
                {
                    editor.ViewToEdit = view;
                }
            }
        }
    }
}
