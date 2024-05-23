using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scroll Demo", "Demonstrates using Scroll view.")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Scrolling")]
public class ScrollDemo : Scenario
{
    private ViewDiagnosticFlags _diagnosticFlags;

    public override void Main ()
    {
        Application.Init ();

        _diagnosticFlags = View.Diagnostics;

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}"
        };

        var editor = new Adornments.AdornmentsEditor ();
        app.Add (editor);

        var view = new FrameView
        {
            Title = "Demo View",
            X = Pos.Right (editor),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Base"]
        };
        app.Add (view);

        var scroll = new Scroll
        {
            X = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        view.Add (scroll);

        var rgOrientation = new RadioGroup
        {
            RadioLabels = ["Vertical", "Horizontal"],
            Orientation = Orientation.Horizontal
        };
        view.Add (rgOrientation);

        rgOrientation.SelectedItemChanged += (s, e) =>
                                             {
                                                 if (e.SelectedItem == e.PreviousSelectedItem)
                                                 {
                                                     return;
                                                 }

                                                 if (rgOrientation.SelectedItem == 0)
                                                 {
                                                     scroll.Orientation = Orientation.Vertical;
                                                     scroll.X = Pos.AnchorEnd ();
                                                     scroll.Height = Dim.Fill ();
                                                     scroll.Size /= 3;
                                                 }
                                                 else
                                                 {
                                                     scroll.Orientation = Orientation.Horizontal;
                                                     scroll.Y = Pos.AnchorEnd ();
                                                     scroll.Width = Dim.Fill ();
                                                     scroll.Size *= 3;
                                                 }
                                             };

        var lblSize = new Label
        {
            Y = Pos.Bottom (rgOrientation),
            Text = "Size:"
        };
        view.Add (lblSize);

        Buttons.NumericUpDown<int> scrollSize = new Buttons.NumericUpDown<int>
        {
            Value = scroll.Size,
            X = Pos.Right (lblSize) + 1,
            Y = Pos.Top (lblSize)
        };
        view.Add (scrollSize);

        scrollSize.ValueChanging += (s, e) =>
                                    {
                                        if (e.NewValue < 0)
                                        {
                                            e.Cancel = true;

                                            return;
                                        }

                                        if (scroll.Size != e.NewValue)
                                        {
                                            scroll.Size = e.NewValue;
                                        }
                                    };

        var lblPosition = new Label
        {
            Y = Pos.Bottom (lblSize),
            Text = "Position:"
        };
        view.Add (lblPosition);

        Buttons.NumericUpDown<int> scrollPosition = new Buttons.NumericUpDown<int>
        {
            Value = scroll.Position,
            X = Pos.Right (lblPosition) + 1,
            Y = Pos.Top (lblPosition)
        };
        view.Add (scrollPosition);

        scrollPosition.ValueChanging += (s, e) =>
                                        {
                                            if (e.NewValue < 0)
                                            {
                                                e.Cancel = true;

                                                return;
                                            }

                                            if (scroll.Position != e.NewValue)
                                            {
                                                scroll.Position = e.NewValue;
                                            }

                                            if (scroll.Position != e.NewValue)
                                            {
                                                e.Cancel = true;
                                            }
                                        };

        var lblSizeChanged = new Label
        {
            Y = Pos.Bottom (lblPosition) + 1
        };
        view.Add (lblSizeChanged);

        scroll.SizeChanged += (s, e) =>
                              {
                                  lblSizeChanged.Text = $"SizeChanged event - OldValue: {e.OldValue}; NewValue: {e.NewValue}";

                                  if (scrollSize.Value != e.NewValue)
                                  {
                                      scrollSize.Value = e.NewValue;
                                  }
                              };

        var lblPosChanging = new Label
        {
            Y = Pos.Bottom (lblSizeChanged)
        };
        view.Add (lblPosChanging);

        scroll.PositionChanging += (s, e) => { lblPosChanging.Text = $"PositionChanging event - OldValue: {e.OldValue}; NewValue: {e.NewValue}"; };

        var lblPositionChanged = new Label
        {
            Y = Pos.Bottom (lblPosChanging)
        };
        view.Add (lblPositionChanged);

        scroll.PositionChanged += (s, e) =>
                                  {
                                      lblPositionChanged.Text = $"PositionChanged event - OldValue: {e.OldValue}; NewValue: {e.NewValue}";
                                      scrollPosition.Value = e.NewValue;
                                  };

        var lblScrollFrame = new Label
        {
            Y = Pos.Bottom (lblPositionChanged) + 1
        };
        view.Add (lblScrollFrame);

        scroll.LayoutComplete += (s, e) => lblScrollFrame.Text = $"Scroll Frame: {scroll.Frame.ToString ()}";

        editor.Initialized += (s, e) =>
                              {
                                  scroll.Size = 40;
                                  editor.ViewToEdit = view;
                              };

        app.Closed += (s, e) => View.Diagnostics = _diagnosticFlags;

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
