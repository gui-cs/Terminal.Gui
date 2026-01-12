#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scrolling", "Content scrolling, IScrollBars, etc...")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Tests")]
public class Scrolling : Scenario
{
    private object? _progressTimer;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        var label = new Label { X = 0, Y = 0 };
        win.Add (label);

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

        win.Add (demoView);

        var hCheckBox = new CheckBox
        {
            X = Pos.X (demoView),
            Y = Pos.Bottom (demoView),
            Text = "_HorizontalScrollBar.Visible",
            CheckedState = demoView.HorizontalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked
        };
        win.Add (hCheckBox);
        hCheckBox.CheckedStateChanged += (_, args) => { demoView.HorizontalScrollBar.Visible = args.Value == CheckState.Checked; };

        var vCheckBox = new CheckBox
        {
            X = Pos.Right (hCheckBox) + 3,
            Y = Pos.Bottom (demoView),
            Text = "_VerticalScrollBar.Visible",
            CheckedState = demoView.VerticalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked
        };
        win.Add (vCheckBox);
        vCheckBox.CheckedStateChanged += (_, args) => { demoView.VerticalScrollBar.Visible = args.Value == CheckState.Checked; };

        var ahCheckBox = new CheckBox
        {
            X = Pos.Left (demoView),
            Y = Pos.Bottom (hCheckBox),
            Text = "_AutoShow (both)",
            CheckedState = demoView.HorizontalScrollBar.AutoShow ? CheckState.Checked : CheckState.UnChecked
        };

        ahCheckBox.CheckedStateChanging += (_, e) =>
                                           {
                                               demoView.HorizontalScrollBar.AutoShow = e.Result == CheckState.Checked;
                                               demoView.VerticalScrollBar.AutoShow = e.Result == CheckState.Checked;
                                           };
        win.Add (ahCheckBox);

        demoView.VerticalScrollBar.VisibleChanging += (_, args) => { vCheckBox.CheckedState = args.NewValue ? CheckState.Checked : CheckState.UnChecked; };

        demoView.HorizontalScrollBar.VisibleChanging += (_, args) => { hCheckBox.CheckedState = args.NewValue ? CheckState.Checked : CheckState.UnChecked; };

        // Add a progress bar to cause constant redraws
        var progress = new ProgressBar
        {
            X = Pos.Center (), Y = Pos.AnchorEnd (), Width = Dim.Fill ()
        };

        win.Add (progress);

        win.Initialized += WinOnInitialized;
        win.IsRunningChanged += WinIsRunningChanged;

        app.Run (win);
        win.IsRunningChanged -= WinIsRunningChanged;

        return;

        void WinOnInitialized (object? sender, EventArgs e)
        {
            bool TimerFn ()
            {
                progress.Pulse ();

                return _progressTimer is { };
            }

            _progressTimer = app.AddTimeout (TimeSpan.FromMilliseconds (200), TimerFn);
        }

        void WinIsRunningChanged (object? sender, EventArgs<bool> args)
        {
            if (!args.Value && _progressTimer is { })
            {
                app.RemoveTimeout (_progressTimer);
                _progressTimer = null;
            }
        }
    }
}
