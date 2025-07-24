namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scrolling", "Content scrolling, IScrollBars, etc...")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Tests")]
public class Scrolling : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        var app = new Window
        {
            Title = GetQuitKeyAndName ()
        };

        var label = new Label { X = 0, Y = 0 };
        app.Add (label);

        var demoView = new AllViewsView
        {
            Id = "demoView",
            X = 2,
            Y = Pos.Bottom (label) + 1,
            Width = Dim.Fill (4),
            Height = Dim.Fill (4)
        };

        label.Text =
            $"{demoView}\nContentSize: {demoView.GetContentSize ()}\nViewport.Location: {demoView.Viewport.Location}";

        demoView.ViewportChanged += (_, _) =>
                                    {
                                        label.Text =
                                            $"{demoView}\nContentSize: {demoView.GetContentSize ()}\nViewport.Location: {demoView.Viewport.Location}";
                                    };

        app.Add (demoView);

        //// NOTE: This call to EnableScrollBar is technically not needed because the reference
        //// NOTE: to demoView.HorizontalScrollBar below will cause it to be lazy created.
        //// NOTE: The call included in this sample to for illustration purposes.
        //demoView.EnableScrollBar (Orientation.Horizontal);
        var hCheckBox = new CheckBox
        {
            X = Pos.X (demoView),
            Y = Pos.Bottom (demoView),
            Text = "_HorizontalScrollBar.Visible",
            CheckedState = demoView.HorizontalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked
        };
        app.Add (hCheckBox);
        hCheckBox.CheckedStateChanged += (sender, args) => { demoView.HorizontalScrollBar.Visible = args.Value == CheckState.Checked; };

        //// NOTE: This call to EnableScrollBar is technically not needed because the reference
        //// NOTE: to demoView.HorizontalScrollBar below will cause it to be lazy created.
        //// NOTE: The call included in this sample to for illustration purposes.
        //demoView.EnableScrollBar (Orientation.Vertical);
        var vCheckBox = new CheckBox
        {
            X = Pos.Right (hCheckBox) + 3,
            Y = Pos.Bottom (demoView),
            Text = "_VerticalScrollBar.Visible",
            CheckedState = demoView.VerticalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked
        };
        app.Add (vCheckBox);
        vCheckBox.CheckedStateChanged += (sender, args) => { demoView.VerticalScrollBar.Visible = args.Value == CheckState.Checked; };

        var ahCheckBox = new CheckBox
        {
            X = Pos.Left (demoView),
            Y = Pos.Bottom (hCheckBox),
            Text = "_AutoShow (both)",
            CheckedState = demoView.HorizontalScrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked
        };

        ahCheckBox.CheckedStateChanging += (s, e) =>
                                           {
                                               demoView.HorizontalScrollBar.AutoShow = e.Result == CheckState.Checked;
                                               demoView.VerticalScrollBar.AutoShow = e.Result == CheckState.Checked;
                                           };
        app.Add (ahCheckBox);

        demoView.VerticalScrollBar.VisibleChanging += (sender, args) => { vCheckBox.CheckedState = args.NewValue ? CheckState.Checked : CheckState.UnChecked; };

        demoView.HorizontalScrollBar.VisibleChanging += (sender, args) =>
                                                        {
                                                            hCheckBox.CheckedState = args.NewValue ? CheckState.Checked : CheckState.UnChecked;
                                                        };

        // Add a progress bar to cause constant redraws
        var progress = new ProgressBar
        {
            X = Pos.Center (), Y = Pos.AnchorEnd (), Width = Dim.Fill ()
        };

        app.Add (progress);

        var pulsing = true;

        app.Initialized += AppOnInitialized;
        app.Unloaded += AppUnloaded;

        Application.Run (app);
        app.Unloaded -= AppUnloaded;
        app.Dispose ();
        Application.Shutdown ();

        return;

        void AppOnInitialized (object sender, EventArgs e)
        {
            bool TimerFn ()
            {
                progress.Pulse ();

                return pulsing;
            }

            Application.AddTimeout (TimeSpan.FromMilliseconds (200), TimerFn);
        }
        void AppUnloaded (object sender, EventArgs args) { pulsing = false; }
    }
}