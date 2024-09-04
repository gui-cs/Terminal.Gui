using System;
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

        var editor = new AdornmentsEditor ();
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
            Height = Dim.Fill (),
        };
        view.Add (scroll);

        var lblWidthHeight = new Label
        {
            Text = "Width/Height:"
        };
        view.Add (lblWidthHeight);

        NumericUpDown<int> scrollWidthHeight = new ()
        {
            Value = scroll.Frame.Width,
            X = Pos.Right (lblWidthHeight) + 1,
            Y = Pos.Top (lblWidthHeight)
        };
        view.Add (scrollWidthHeight);

        scrollWidthHeight.ValueChanging += (s, e) =>
                                           {
                                               if (e.NewValue < 1
                                                   || (e.NewValue
                                                       > (scroll.Orientation == Orientation.Vertical
                                                              ? scroll.SuperView?.GetContentSize ().Width
                                                              : scroll.SuperView?.GetContentSize ().Height)))
                                               {
                                                   // TODO: This must be handled in the ScrollSlider if Width and Height being virtual
                                                   e.Cancel = true;

                                                   return;
                                               }

                                               if (scroll.Orientation == Orientation.Vertical)
                                               {
                                                   scroll.Width = e.NewValue;
                                               }
                                               else
                                               {
                                                   scroll.Height = e.NewValue;
                                               }
                                           };

        var rgOrientation = new RadioGroup
        {
            Y = Pos.Bottom (lblWidthHeight),
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
                                                     scroll.Y = 0;
                                                     scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scroll.SuperView.GetContentSize ().Width);
                                                     scroll.Width = scrollWidthHeight.Value;
                                                     scroll.Height = Dim.Fill ();
                                                     scroll.Size /= 3;
                                                 }
                                                 else
                                                 {
                                                     scroll.Orientation = Orientation.Horizontal;
                                                     scroll.X = 0;
                                                     scroll.Y = Pos.AnchorEnd ();
                                                     scroll.Width = Dim.Fill ();

                                                     scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scroll.SuperView.GetContentSize ().Height);
                                                     scroll.Height = scrollWidthHeight.Value;
                                                     scroll.Size *= 3;
                                                 }
                                             };

        var lblSize = new Label
        {
            Y = Pos.Bottom (rgOrientation),
            Text = "Size:"
        };
        view.Add (lblSize);

        NumericUpDown<int> scrollSize = new ()
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

        NumericUpDown<int> scrollPosition = new ()
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

        var ckbKeepContentInAllViewport = new CheckBox
        {
            Y = Pos.Bottom (scrollPosition), Text = "KeepContentInAllViewport",
            CheckedState = scroll.KeepContentInAllViewport ? CheckState.Checked : CheckState.UnChecked
        };
        ckbKeepContentInAllViewport.CheckedStateChanging += (s, e) => scroll.KeepContentInAllViewport = e.NewValue == CheckState.Checked;
        view.Add (ckbKeepContentInAllViewport);

        var lblSizeChanged = new Label
        {
            Y = Pos.Bottom (ckbKeepContentInAllViewport) + 1
        };
        view.Add (lblSizeChanged);

        scroll.SizeChanged += (s, e) =>
                              {
                                  lblSizeChanged.Text = $"SizeChanged event - CurrentValue: {e.CurrentValue}";

                                  if (scrollSize.Value != e.CurrentValue)
                                  {
                                      scrollSize.Value = e.CurrentValue;
                                  }
                              };

        var lblPosChanging = new Label
        {
            Y = Pos.Bottom (lblSizeChanged)
        };
        view.Add (lblPosChanging);

        scroll.PositionChanging += (s, e) => { lblPosChanging.Text = $"PositionChanging event - CurrentValue: {e.CurrentValue}; NewValue: {e.NewValue}"; };

        var lblPositionChanged = new Label
        {
            Y = Pos.Bottom (lblPosChanging)
        };
        view.Add (lblPositionChanged);

        scroll.PositionChanged += (s, e) =>
                                  {
                                      lblPositionChanged.Text = $"PositionChanged event - CurrentValue: {e.CurrentValue}";
                                      scrollPosition.Value = e.CurrentValue;
                                  };

        var lblScrollFrame = new Label
        {
            Y = Pos.Bottom (lblPositionChanged) + 1
        };
        view.Add (lblScrollFrame);

        var lblScrollViewport = new Label
        {
            Y = Pos.Bottom (lblScrollFrame)
        };
        view.Add (lblScrollViewport);

        var lblScrollContentSize = new Label
        {
            Y = Pos.Bottom (lblScrollViewport)
        };
        view.Add (lblScrollContentSize);


        scroll.LayoutComplete += (s, e) =>
                                 {
                                     lblScrollFrame.Text = $"Scroll Frame: {scroll.Frame.ToString ()}";
                                     lblScrollViewport.Text = $"Scroll Viewport: {scroll.Viewport.ToString ()}";
                                     lblScrollContentSize.Text = $"Scroll ContentSize: {scroll.GetContentSize ().ToString ()}";
                                 };

        editor.Initialized += (s, e) =>
                              {
                                  scroll.Size = int.Max (app.GetContentSize ().Height * 2, app.GetContentSize ().Width * 2);
                                  editor.ViewToEdit = scroll;
                              };

        app.Closed += (s, e) => View.Diagnostics = _diagnosticFlags;

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
