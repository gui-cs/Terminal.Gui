using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ScrollBar Demo", "Demonstrates using ScrollBar view.")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Scrolling")]
public class ScrollBarDemo : Scenario
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

        var scrollBar = new ScrollBar
        {
            X = Pos.AnchorEnd (),
            Height = Dim.Fill (),
        };
        view.Add (scrollBar);

        var lblWidthHeight = new Label
        {
            Text = "Width/Height:"
        };
        view.Add (lblWidthHeight);

        NumericUpDown<int> scrollWidthHeight = new ()
        {
            Value = scrollBar.Frame.Width,
            X = Pos.Right (lblWidthHeight) + 1,
            Y = Pos.Top (lblWidthHeight)
        };
        view.Add (scrollWidthHeight);

        scrollWidthHeight.ValueChanging += (s, e) =>
                                           {
                                               if (e.NewValue < 1
                                                   || (e.NewValue
                                                       > (scrollBar.Orientation == Orientation.Vertical
                                                              ? scrollBar.SuperView?.GetContentSize ().Width
                                                              : scrollBar.SuperView?.GetContentSize ().Height)))
                                               {
                                                   // TODO: This must be handled in the ScrollSlider if Width and Height being virtual
                                                   e.Cancel = true;

                                                   return;
                                               }

                                               if (scrollBar.Orientation == Orientation.Vertical)
                                               {
                                                   scrollBar.Width = e.NewValue;
                                               }
                                               else
                                               {
                                                   scrollBar.Height = e.NewValue;
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
                                                     scrollBar.Orientation = Orientation.Vertical;
                                                     scrollBar.X = Pos.AnchorEnd ();
                                                     scrollBar.Y = 0;
                                                     scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scrollBar.SuperView.GetContentSize ().Width);
                                                     scrollBar.Width = scrollWidthHeight.Value;
                                                     scrollBar.Height = Dim.Fill ();
                                                     scrollBar.Size /= 3;
                                                 }
                                                 else
                                                 {
                                                     scrollBar.Orientation = Orientation.Horizontal;
                                                     scrollBar.X = 0;
                                                     scrollBar.Y = Pos.AnchorEnd ();
                                                     scrollBar.Width = Dim.Fill ();

                                                     scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scrollBar.SuperView.GetContentSize ().Height);
                                                     scrollBar.Height = scrollWidthHeight.Value;
                                                     scrollBar.Size *= 3;
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
            Value = scrollBar.Size,
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

                                        if (scrollBar.Size != e.NewValue)
                                        {
                                            scrollBar.Size = e.NewValue;
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
            Value = scrollBar.Position,
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

                                            if (scrollBar.Position != e.NewValue)
                                            {
                                                scrollBar.Position = e.NewValue;
                                            }

                                            if (scrollBar.Position != e.NewValue)
                                            {
                                                e.Cancel = true;
                                            }
                                        };

        var ckbAutoHide = new CheckBox
            { Y = Pos.Bottom (scrollPosition), Text = "AutoHideScrollBar", CheckedState = scrollBar.AutoHide ? CheckState.Checked : CheckState.UnChecked };
        ckbAutoHide.CheckedStateChanging += (s, e) => scrollBar.AutoHide = e.NewValue == CheckState.Checked;
        view.Add (ckbAutoHide);

        var ckbShowScrollIndicator = new CheckBox
        {
            X = Pos.Right (ckbAutoHide) + 1, Y = Pos.Bottom (scrollPosition), Text = "ShowScrollIndicator",
            CheckedState = scrollBar.ShowScrollIndicator ? CheckState.Checked : CheckState.UnChecked
        };
        ckbShowScrollIndicator.CheckedStateChanging += (s, e) => scrollBar.ShowScrollIndicator = e.NewValue == CheckState.Checked;
        view.Add (ckbShowScrollIndicator);

        var ckbKeepContentInAllViewport = new CheckBox
        {
            X = Pos.Right (ckbShowScrollIndicator) + 1, Y = Pos.Bottom (scrollPosition), Text = "KeepContentInAllViewport",
            CheckedState = scrollBar.KeepContentInAllViewport ? CheckState.Checked : CheckState.UnChecked
        };
        ckbKeepContentInAllViewport.CheckedStateChanging += (s, e) => scrollBar.KeepContentInAllViewport = e.NewValue == CheckState.Checked;
        view.Add (ckbKeepContentInAllViewport);

        var lblSizeChanged = new Label
        {
            Y = Pos.Bottom (ckbShowScrollIndicator) + 1
        };
        view.Add (lblSizeChanged);

        scrollBar.SizeChanged += (s, e) =>
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

        scrollBar.PositionChanging += (s, e) => { lblPosChanging.Text = $"PositionChanging event - CurrentValue: {e.CurrentValue}; NewValue: {e.NewValue}"; };

        var lblPositionChanged = new Label
        {
            Y = Pos.Bottom (lblPosChanging)
        };
        view.Add (lblPositionChanged);

        scrollBar.PositionChanged += (s, e) =>
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


        scrollBar.LayoutComplete += (s, e) =>
                                 {
                                     lblScrollFrame.Text = $"ScrollBar Frame: {scrollBar.Frame.ToString ()}";
                                     lblScrollViewport.Text = $"ScrollBar Viewport: {scrollBar.Viewport.ToString ()}";
                                     lblScrollContentSize.Text = $"ScrollBar ContentSize: {scrollBar.GetContentSize ().ToString ()}";
                                 };

        editor.Initialized += (s, e) =>
                              {
                                  scrollBar.Size = int.Max (app.GetContentSize ().Height * 2, app.GetContentSize ().Width * 2);
                                  editor.ViewToEdit = scrollBar;
                              };

        app.Closed += (s, e) => View.Diagnostics = _diagnosticFlags;

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
