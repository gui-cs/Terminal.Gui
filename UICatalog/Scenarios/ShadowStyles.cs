using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ShadowStyles Demo", "Demonstrates ShadowStyles Effects.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Adornments")]
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
            ShowViewIdentifier = true,
        };
        editor.Initialized += (sender, args) => editor.MarginEditor.ExpanderButton.Collapsed = false;

        app.Add (editor);

        Window win = new ()
        {
            X = Pos.Right (editor),
            Y = 0,
            Width = Dim.Percent (30),
            Height = Dim.Percent (30),
            Title = "Shadow Window",
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped,
            BorderStyle = LineStyle.Double,
            ShadowStyle = ShadowStyle.Transparent,
        };

        app.DrawingContent += (s, e) =>
                           {
                               app!.FillRect (app!.Viewport, Glyphs.Dot);
                               e.Cancel = true;
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

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = app;
        editor.AutoSelectAdornments = false;

        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();

    }
}
