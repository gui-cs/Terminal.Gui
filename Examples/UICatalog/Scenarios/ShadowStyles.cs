using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;

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
            Id = "app",
            Title = GetQuitKeyAndName ()
        };


        var editor = new AdornmentsEditor ()
        {
            Id = "editor",
            AutoSelectViewToEdit = true,
            ShowViewIdentifier = true,
        };
        editor.Initialized += (sender, args) => editor.MarginEditor.ExpanderButton.Collapsed = false;

        app.Add (editor);

        Window shadowWindow = new ()
        {

            Id = "shadowWindow",
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
            Id = "buttonInWin",
            X = Pos.Center (),
            Y = Pos.Center (), Text = "Button in Window",
            ShadowStyle = ShadowStyle.Opaque
        };
        shadowWindow.Add (buttonInWin);
        app.Add (shadowWindow);

        var button = new Button
        {
            Id = "button",
            X = Pos.Right (editor) + 10,
            Y = Pos.Center (), Text = "Button",
            ShadowStyle = ShadowStyle.Opaque
        };

        ColorPicker colorPicker = new ()
        {
            Title = "ColorPicker to illustrate highlight (currently broken)",
            BorderStyle = LineStyle.Dotted,
            Id = "colorPicker16",
            X = Pos.Center (),
            Y = Pos.AnchorEnd (),
            Width = Dim.Percent(80),
        };
        colorPicker.ColorChanged += (sender, args) =>
                                    {
                                        var normal = app.GetScheme ().Normal;
                                        app.SetScheme (app.GetScheme() with {Normal = new Attribute(normal.Foreground, args.Result)});
                                    };
        app.Add (button, colorPicker);

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = app;
        editor.AutoSelectAdornments = false;

        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();

    }
}
