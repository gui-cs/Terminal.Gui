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

        Window window = new ()
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

        window.Add (editor);

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

        window.DrawingContent += (s, e) =>
                           {
                               window!.FillRect (window!.Viewport, Glyphs.Dot);
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
        window.Add (shadowWindow);

        Window shadowWindow2 = new ()
        {

            Id = "shadowWindow2",
            X = Pos.Right (editor) + 10,
            Y = 10,
            Width = Dim.Percent (30),
            Height = Dim.Percent (30),
            Title = "Shadow Window #2",
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped,
            BorderStyle = LineStyle.Double,
            ShadowStyle = ShadowStyle.Transparent,
        };
        window.Add (shadowWindow2);


        var button = new Button
        {
            Id = "button",
            X = Pos.Right (editor) + 10,
            Y = Pos.Center (), Text = "Button",
            ShadowStyle = ShadowStyle.Opaque
        };
        button.Accepting += ButtonOnAccepting;

        ColorPicker colorPicker = new ()
        {
            Title = "ColorPicker to illustrate highlight (currently broken)",
            BorderStyle = LineStyle.Dotted,
            Id = "colorPicker16",
            X = Pos.Center (),
            Y = Pos.AnchorEnd (),
            Width = Dim.Percent (80),
        };
        colorPicker.ColorChanged += (sender, args) =>
                                    {
                                        var normal = window.GetScheme ().Normal;
                                        window.SetScheme (window.GetScheme () with { Normal = new Attribute (normal.Foreground, args.Result) });
                                    };
        window.Add (button, colorPicker);

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = window;
        editor.AutoSelectAdornments = false;

        Application.Run (window);
        window.Dispose ();

        Application.Shutdown ();

    }

    private void ButtonOnAccepting (object sender, CommandEventArgs e)
    {
        MessageBox.Query ((sender as View)?.App, "Hello", "You pushed the button!");
        e.Handled = true;
    }
}
