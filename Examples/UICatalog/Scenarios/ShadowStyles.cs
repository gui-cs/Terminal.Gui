namespace UICatalog.Scenarios;

[ScenarioMetadata ("ShadowStyles Demo", "Demonstrates ShadowStyles Effects.")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Adornments")]
public class ShadowStyles : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ()
        {
            Id = "app",
            Title = GetQuitKeyAndName ()
        };

        AdornmentsEditor editor = new ()
        {
            Id = "editor",
            AutoSelectViewToEdit = true,
            ShowViewIdentifier = true
        };
        editor.Initialized += (_, _) => editor.MarginEditor!.ExpanderButton!.Collapsed = false;

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
            ShadowStyle = ShadowStyle.Transparent
        };

        window.DrawingContent += (_, e) =>
                                 {
                                     window!.FillRect (window!.Viewport, Glyphs.Dot);
                                     e.Cancel = true;
                                 };

        Button buttonInWin = new ()
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
            ShadowStyle = ShadowStyle.Transparent
        };
        window.Add (shadowWindow2);

        Button button = new ()
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
            Width = Dim.Percent (80)
        };

        colorPicker.ColorChanged += (_, args) =>
                                    {
                                        Attribute normal = window.GetScheme ().Normal;
                                        window.SetScheme (window.GetScheme () with { Normal = new (normal.Foreground, args.Result) });
                                    };
        window.Add (button, colorPicker);

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = window;
        editor.AutoSelectAdornments = false;

        app.Run (window);
    }

    private void ButtonOnAccepting (object sender, CommandEventArgs e)
    {
        MessageBox.Query ((sender as View)?.App!, "Hello", "You pushed the button!");
        e.Handled = true;
    }
}
