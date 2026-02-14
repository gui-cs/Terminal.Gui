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

        using Window win = new ();
        win.Title = GetQuitKeyAndName ();

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

        label.Text = $"{demoView}\nContentSize: {demoView.GetContentSize ()}\nViewport.Location: {demoView.Viewport.Location}";

        demoView.ViewportChanged += (_, _) =>
                                    {
                                        label.Text = $"{demoView}\nContentSize: {demoView.GetContentSize ()}\nViewport.Location: {demoView.Viewport.Location}";
                                    };

        win.Add (demoView);

        var hCheckBox = new CheckBox
        {
            X = Pos.X (demoView),
            Y = Pos.Bottom (demoView),
            Text = "ViewportSettings.Has_HorizontalScrollBar",
            Value = demoView.ViewportSettings.HasFlag (ViewportSettingsFlags.HasHorizontalScrollBar) ? CheckState.Checked : CheckState.UnChecked
        };
        win.Add (hCheckBox);

        hCheckBox.ValueChanging += (_, e) =>
                                   {
                                       if (e.NewValue == CheckState.Checked)
                                       {
                                           demoView.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
                                       }
                                       else
                                       {
                                           demoView.ViewportSettings &= ~ViewportSettingsFlags.HasHorizontalScrollBar;
                                       }
                                   };

        var vCheckBox = new CheckBox
        {
            X = Pos.Right (hCheckBox) + 3,
            Y = Pos.Bottom (demoView),
            Text = "ViewportSettings.Has_VerticalScrollBar",
            Value = demoView.ViewportSettings.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar) ? CheckState.Checked : CheckState.UnChecked
        };
        win.Add (vCheckBox);

        vCheckBox.ValueChanging += (_, e) =>
                                   {
                                       if (e.NewValue == CheckState.Checked)
                                       {
                                           demoView.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
                                       }
                                       else
                                       {
                                           demoView.ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
                                       }
                                   };

        // Add a progress bar to cause constant redraws
        var progress = new ProgressBar { X = Pos.Center (), Y = Pos.AnchorEnd (), Width = Dim.Fill () };

        win.Add (progress);

        win.Initialized += WinOnInitialized;
        win.IsRunningChanged += WinIsRunningChanged;

        app.Run (win);
        win.IsRunningChanged -= WinIsRunningChanged;

        return;

        void WinOnInitialized (object? sender, EventArgs e)
        {
            _progressTimer = app.AddTimeout (TimeSpan.FromMilliseconds (200), TimerFn);

            return;

            bool TimerFn ()
            {
                progress.Pulse ();

                return _progressTimer is { };
            }
        }

        void WinIsRunningChanged (object? sender, EventArgs<bool> args)
        {
            if (args.Value || _progressTimer is null)
            {
                return;
            }
            app.RemoveTimeout (_progressTimer);
            _progressTimer = null;
        }
    }
}
