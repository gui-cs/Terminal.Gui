#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Mandelbrot", "Displays a sixel-rendered Mandelbrot set with live settings and an overlay dialog.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
public class Mandelbrot : Scenario
{
    private const int IMAGE_COLUMNS = 30;
    private const int IMAGE_ROWS = 20;
    private const double MINIMUM_SPAN = 0.05;
    private const int SETTING_LABEL_WIDTH = 11;
    private const int CENTER_X_LABEL_GROUP_ID = 1;
    private const int CENTER_Y_LABEL_GROUP_ID = 2;
    private const int SPAN_LABEL_GROUP_ID = 3;
    private const int ITERATIONS_LABEL_GROUP_ID = 4;
    private const int RESET_GROUP_ID = 5;
    private const int NOTE_GROUP_ID = 6;
    private const int FIRE_LABEL_GROUP_ID = 7;
    private const int FIRE_PROGRESS_GROUP_ID = 8;
    private const double ZOOM_IN_FACTOR = 0.8;
    private const double ZOOM_OUT_FACTOR = 1.25;

    private IApplication _app = null!;
    private NumericUpDown<double> _centerX = null!;
    private NumericUpDown<double> _centerY = null!;
    private ProgressBar _fireProgress = null!;
    private object? _fireProgressTimeout;
    private NumericUpDown<int> _iterations = null!;
    private MandelbrotImageView _mandelbrotView = null!;
    private NumericUpDown<double> _span = null!;
    private Label _status = null!;
    private Window _window = null!;
    private SixelSupportResult _sixelSupportResult = new ();

    public override void Main ()
    {
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        _app = app;
        _app.Driver!.SixelSupportChanged += OnSixelSupportChanged;

        _window = new Window { Title = $"{Application.GetDefaultKey (Command.Quit)} to Quit - Scenario: {GetName ()}" };

        FrameView settings = new () { Title = "Settings", Width = 34, Height = Dim.Fill () };

        View display = new () { X = Pos.Right (settings), Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };

        _status = new Label { X = Pos.Align (Alignment.Start), Y = Pos.Align (Alignment.Start), Width = Dim.Fill (), Height = 1 };

        _mandelbrotView = new MandelbrotImageView
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = IMAGE_COLUMNS,
            Height = IMAGE_ROWS,
            BorderStyle = LineStyle.Double,
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            Arrangement = ViewArrangement.Resizable,
            UseSixel = true
        };
        _mandelbrotView.CenterRequested += CenterMandelbrot;

        BuildSettings (settings);
        BuildZoomButtons ();
        _mandelbrotView.ViewportChanged += (_, _) => RenderMandelbrot ();

        display.Add (_status, _mandelbrotView);
        _window.Add (settings, display);

        _window.Initialized += (_, _) =>
                               {
                                   UpdateSixelSupport (_app.Driver?.SixelSupport);
                                   RenderMandelbrot ();
                                   StartFireProgress ();

                                   _app.AddTimeout (TimeSpan.FromMilliseconds (100),
                                                    () =>
                                                    {
                                                        _mandelbrotView.SetFocus ();

                                                        return false;
                                                    });
                               };

        try
        {
            app.Run (_window);
        }
        finally
        {
            StopFireProgress ();
            _app.Driver!.SixelSupportChanged -= OnSixelSupportChanged;
            _window.Dispose ();
        }
    }

    private bool AdvanceFireProgress ()
    {
        if (_fireProgressTimeout is null)
        {
            return false;
        }

        float nextFraction = _fireProgress.Fraction + 0.02f;
        _fireProgress.Fraction = nextFraction >= 1 ? 0 : nextFraction;

        return true;
    }

    private void BuildSettings (View settings)
    {
        Label centerXLabel = CreateSettingLabel ("Center X:", CENTER_X_LABEL_GROUP_ID);
        Label centerYLabel = CreateSettingLabel ("Center Y:", CENTER_Y_LABEL_GROUP_ID, centerXLabel);
        Label spanLabel = CreateSettingLabel ("Span:", SPAN_LABEL_GROUP_ID, centerYLabel);
        Label iterationsLabel = CreateSettingLabel ("Iterations:", ITERATIONS_LABEL_GROUP_ID, spanLabel);

        _centerX = new NumericUpDown<double>
        {
            X = Pos.Right (centerXLabel) + 1,
            Y = Pos.Top (centerXLabel),
            Width = Dim.Fill (),
            Value = -0.5,
            Increment = 0.05,
            Format = "{0:0.000}"
        };

        _centerY = new NumericUpDown<double>
        {
            X = Pos.Right (centerYLabel) + 1,
            Y = Pos.Top (centerYLabel),
            Width = Dim.Fill (),
            Value = 0,
            Increment = 0.05,
            Format = "{0:0.000}"
        };

        _span = new NumericUpDown<double>
        {
            X = Pos.Right (spanLabel) + 1,
            Y = Pos.Top (spanLabel),
            Width = Dim.Fill (),
            Value = 3,
            Increment = 0.1,
            Format = "{0:0.000}"
        };

        _iterations = new NumericUpDown<int>
        {
            X = Pos.Right (iterationsLabel) + 1,
            Y = Pos.Top (iterationsLabel),
            Width = Dim.Fill (),
            Value = 80,
            Increment = 10
        };

        _span.ValueChanging += (_, args) =>
                               {
                                   if (args.NewValue < MINIMUM_SPAN)
                                   {
                                       args.Handled = true;
                                   }
                               };

        _iterations.ValueChanging += (_, args) =>
                                     {
                                         if (args.NewValue < 8)
                                         {
                                             args.Handled = true;
                                         }
                                     };

        _centerX.ValueChanged += (_, _) => RenderMandelbrot ();
        _centerY.ValueChanged += (_, _) => RenderMandelbrot ();
        _span.ValueChanged += (_, _) => RenderMandelbrot ();
        _iterations.ValueChanged += (_, _) => RenderMandelbrot ();

        Button reset = new () { X = Pos.Align (Alignment.Start, groupId: RESET_GROUP_ID), Y = Pos.Bottom (iterationsLabel) + 2, Text = "_Reset" };

        reset.Accepted += (_, _) => ResetSettings ();

        Button overlay = new () { X = Pos.Right (reset) + 1, Y = Pos.Top (reset), Text = "_Overlay" };

        overlay.Accepted += (_, _) => ShowOverlay ();

        Label note = new ()
        {
            X = Pos.Align (Alignment.Start, groupId: NOTE_GROUP_ID),
            Y = Pos.Bottom (reset) + 2,
            Width = Dim.Fill (),
            Height = Dim.Auto (),
            Text = "The bordered image starts\nat 30 x 20 cells. Resize\nit or use +/- to rerender\nthrough sixel raster\ncommands."
        };

        Label fireLabel = new () { X = Pos.Align (Alignment.Start, groupId: FIRE_LABEL_GROUP_ID), Y = Pos.AnchorEnd (2), Text = "Fire progress:" };

        _fireProgress = new ProgressBar
        {
            X = Pos.Align (Alignment.Start, groupId: FIRE_PROGRESS_GROUP_ID),
            Y = Pos.Bottom (fireLabel),
            Width = Dim.Fill (),
            ProgressBarStyle = ProgressBarStyle.Fire,
            ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage,
            CanFocus = true
        };

        settings.Add (centerXLabel,
                      centerYLabel,
                      spanLabel,
                      iterationsLabel,
                      _centerX,
                      _centerY,
                      _span,
                      _iterations,
                      reset,
                      overlay,
                      note,
                      fireLabel,
                      _fireProgress);
    }

    private void BuildZoomButtons ()
    {
        Button zoomOut = new ()
        {
            X = Pos.AnchorEnd (2),
            Y = Pos.AnchorEnd (1),
            Width = 1,
            Height = 1,
            Text = "-",
            CanFocus = false,
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = null
        };

        Button zoomIn = new ()
        {
            X = Pos.AnchorEnd (1),
            Y = Pos.AnchorEnd (1),
            Width = 1,
            Height = 1,
            Text = "+",
            CanFocus = false,
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = null
        };

        zoomOut.Accepted += (_, _) =>
                            {
                                Zoom (ZOOM_OUT_FACTOR);
                                _mandelbrotView.SetFocus ();
                            };

        zoomIn.Accepted += (_, _) =>
                           {
                               Zoom (ZOOM_IN_FACTOR);
                               _mandelbrotView.SetFocus ();
                           };

        _mandelbrotView.Add (zoomOut, zoomIn);
    }

    private static Label CreateSettingLabel (string text, int groupId, View? previous = null) =>
        new ()
        {
            X = Pos.Align (Alignment.Start, groupId: groupId),
            Y = previous is null ? Pos.Align (Alignment.Start) : Pos.Bottom (previous) + 1,
            Width = SETTING_LABEL_WIDTH,
            TextAlignment = Alignment.End,
            Text = text
        };

    private void CenterMandelbrot (Point position)
    {
        Rectangle viewport = _mandelbrotView.Viewport;

        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        if (position.X < 0 || position.Y < 0 || position.X >= viewport.Width || position.Y >= viewport.Height)
        {
            return;
        }

        double span = _span.Value;
        double spanY = span * viewport.Height / viewport.Width;
        _centerX.Value += ((position.X + 0.5d) / viewport.Width - 0.5d) * span;
        _centerY.Value += ((position.Y + 0.5d) / viewport.Height - 0.5d) * spanY;
    }

    private void OnSixelSupportChanged (object? sender, ValueChangedEventArgs<SixelSupportResult?> args)
    {
        UpdateSixelSupport (args.NewValue);
        RenderMandelbrot ();
    }

    private void UpdateSixelSupport (SixelSupportResult? support) => _sixelSupportResult = support ?? new SixelSupportResult ();

    private void RenderMandelbrot ()
    {
        SixelSupportResult support = _app.Driver?.SixelSupport ?? _sixelSupportResult;
        Rectangle viewport = _mandelbrotView.Viewport;
        int imageColumns = Math.Max (0, viewport.Width);
        int imageRows = Math.Max (0, viewport.Height);

        if (imageColumns == 0 || imageRows == 0)
        {
            return;
        }

        int pixelWidth = imageColumns * Math.Max (1, support.Resolution.Width);
        int pixelHeight = imageRows * Math.Max (1, support.Resolution.Height);
        int iterations = _iterations.Value;
        double centerX = _centerX.Value;
        double centerY = _centerY.Value;
        double span = _span.Value;

        _mandelbrotView.Render (pixelWidth, pixelHeight, centerX, centerY, span, iterations, support);
        string renderMode = _mandelbrotView.IsUsingSixel ? "Sixel raster" : "Cell fallback";
        _status.Text = $"{renderMode}: {imageColumns} x {imageRows} cells, {pixelWidth} x {pixelHeight}px";
        _status.SetNeedsDraw ();
    }

    private void ResetSettings ()
    {
        _centerX.Value = -0.5;
        _centerY.Value = 0;
        _span.Value = 3;
        _iterations.Value = 80;
        RenderMandelbrot ();
    }

    private void ShowOverlay ()
    {
        _mandelbrotView.SetNeedsDraw ();

        Dialog dialog = new () { Title = "Overlay Runnable", Width = 38, Height = 9 };

        dialog.Add (new Label
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "This dialog is a runnable\nshown over the sixel image\nto exercise clipping."
        });

        dialog.AddButton (new Button { Text = "_OK", IsDefault = true });
        _app.Run (dialog);
        dialog.Dispose ();

        _mandelbrotView.SetNeedsDraw ();
    }

    private void StartFireProgress () => _fireProgressTimeout ??= _app.AddTimeout (TimeSpan.FromMilliseconds (80), AdvanceFireProgress);

    private void StopFireProgress ()
    {
        if (_fireProgressTimeout is null)
        {
            return;
        }

        _app.RemoveTimeout (_fireProgressTimeout);
        _fireProgressTimeout = null;
    }

    private void Zoom (double spanMultiplier)
    {
        double nextSpan = Math.Max (MINIMUM_SPAN, _span.Value * spanMultiplier);

        if (Math.Abs (nextSpan - _span.Value) < double.Epsilon)
        {
            return;
        }

        _span.Value = nextSpan;
    }

    private sealed class MandelbrotImageView : ImageView
    {
        public event Action<Point>? CenterRequested;

        public void Render (int pixelWidth, int pixelHeight, double centerX, double centerY, double span, int maxIterations, SixelSupportResult support)
        {
            SixelEncoder = new SixelEncoder { Quantizer = { MaxColors = Math.Min (support.MaxPaletteColors, 64) } };
            Image = CreateMandelbrotPixels (pixelWidth, pixelHeight, centerX, centerY, span, maxIterations);
        }

        protected override bool CenterOnViewportPoint (Point position)
        {
            CenterRequested?.Invoke (position);

            return true;
        }

        private static int CountIterations (double cx, double cy, int maxIterations)
        {
            double zx = 0;
            double zy = 0;
            var iterations = 0;

            while (zx * zx + zy * zy <= 4 && iterations < maxIterations)
            {
                double nextX = zx * zx - zy * zy + cx;
                zy = 2 * zx * zy + cy;
                zx = nextX;
                iterations++;
            }

            return iterations;
        }

        private static Color [,] CreateMandelbrotPixels (int width, int height, double centerX, double centerY, double span, int maxIterations)
        {
            Color [,] pixels = new Color [width, height];
            double spanY = span * height / width;
            double xMin = centerX - span / 2;
            double yMin = centerY - spanY / 2;

            for (var y = 0; y < height; y++)
            {
                double cy = yMin + spanY * y / Math.Max (1, height - 1);

                for (var x = 0; x < width; x++)
                {
                    double cx = xMin + span * x / Math.Max (1, width - 1);
                    int iterations = CountIterations (cx, cy, maxIterations);
                    pixels [x, y] = GetMandelbrotColor (iterations, maxIterations);
                }
            }

            return pixels;
        }

        private static Color GetMandelbrotColor (int iterations, int maxIterations)
        {
            if (iterations >= maxIterations)
            {
                return Color.Black;
            }

            double t = (double)iterations / maxIterations;
            var red = (byte)(9 * (1 - t) * t * t * t * 255);
            var green = (byte)(15 * (1 - t) * (1 - t) * t * t * 255);
            var blue = (byte)(8.5 * (1 - t) * (1 - t) * (1 - t) * t * 255);

            return new Color (red, green, blue);
        }
    }
}
