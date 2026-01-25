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
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new () { Title = GetQuitKeyAndName (), TabStop = TabBehavior.TabGroup };

        AdornmentsEditor editor = new ()
        {
            X = 0,
            Y = 0,
            TabStop = TabBehavior.NoStop,
            AutoSelectViewToEdit = true,
            ShowViewIdentifier = true
        };
        window.Add (editor);

        FrameView testFrame = new () { Title = "_1 Test Frame", X = Pos.Right (editor), Width = Dim.Fill (), Height = Dim.Fill () };

        window.Add (testFrame);

        Button button = new () { X = 0, Y = 0, Title = $"TopButton _{GetNextHotKey ()}" };

        testFrame.Add (button);

        button = new Button { X = Pos.AnchorEnd (), Y = Pos.AnchorEnd (), Title = $"TopButton _{GetNextHotKey ()}" };

        View popoverView = new ()
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

        Button popoverButton = new () { X = Pos.Center (), Y = Pos.Center (), Title = Strings.cmdClose };

        //popoverButton.Accepting += (sender, e) => App?.Popover!.Visible = false;
        popoverView.Add (popoverButton);

        button.Accepting += ButtonAccepting;

        void ButtonAccepting (object sender, CommandEventArgs e)
        {
            //App?.Popover = popoverView;
            //App?.Popover!.Visible = true;
        }

        testFrame.Activating += (_, e) =>
                                {
                                    if (e.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouseArgs })
                                    {
                                        if (mouseArgs.Flags == MouseFlags.RightButtonClicked)
                                        {
                                            popoverView.X = mouseArgs.ScreenPosition.X;
                                            popoverView.Y = mouseArgs.ScreenPosition.Y;

                                            //App?.Popover = popoverView;
                                            //App?.Popover!.Visible = true;
                                        }
                                    }
                                };

        testFrame.Add (button);

        editor.AutoSelectViewToEdit = true;
        editor.AutoSelectSuperView = testFrame;
        editor.AutoSelectAdornments = true;

        app.Run (window);
        popoverView.Dispose ();
    }

    private int _hotkeyCount;

    private char GetNextHotKey () => (char)('A' + _hotkeyCount++);
}
