namespace UICatalog.Scenarios;

[ScenarioMetadata ("Mandelbrot", "Displays a sixel-rendered Mandelbrot set with live settings and an overlay dialog.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
public class Mandelbrot : Scenario
{
    private const int ImageColumns = 30;
    private const int ImageRows = 20;

    private IApplication _app = null!;
    private NumericUpDown<double> _centerX = null!;
    private NumericUpDown<double> _centerY = null!;
    private NumericUpDown<int> _iterations = null!;
    private MandelbrotImageView _mandelbrotView = null!;
    private NumericUpDown<double> _span = null!;
    private Label _status = null!;
    private Window _window = null!;

    public override void Main ()
    {
        using IApplication app = Application.Create ().Init ();
        _app = app;

        _window = new () { Title = $"{Application.GetDefaultKey (Command.Quit)} to Quit - Scenario: {GetName ()}" };

        FrameView settings = new ()
        {
            Title = "Settings",
            Width = 34,
            Height = Dim.Fill ()
        };

        View display = new ()
        {
            X = Pos.Right (settings),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        _status = new ()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill (1),
            Height = 1
        };

        _mandelbrotView = new ()
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = ImageColumns,
            Height = ImageRows,
            BorderStyle = LineStyle.Double,
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Resizable,
            UseSixel = true
        };

        BuildSettings (settings);
        display.Add (_status, _mandelbrotView);
        _window.Add (settings, display);

        _window.Initialized += (_, _) => RenderMandelbrot ();

        app.Run (_window);
        _window.Dispose ();
    }

    private void BuildSettings (View settings)
    {
        settings.Add (
                      new Label { X = 1, Y = 1, Text = "Center X:" },
                      new Label { X = 1, Y = 3, Text = "Center Y:" },
                      new Label { X = 1, Y = 5, Text = "Span:" },
                      new Label { X = 1, Y = 7, Text = "Iterations:" });

        _centerX = new ()
        {
            X = 14,
            Y = 1,
            Value = -0.5,
            Increment = 0.05,
            Format = "{0:0.000}"
        };

        _centerY = new ()
        {
            X = 14,
            Y = 3,
            Value = 0,
            Increment = 0.05,
            Format = "{0:0.000}"
        };

        _span = new ()
        {
            X = 14,
            Y = 5,
            Value = 3,
            Increment = 0.1,
            Format = "{0:0.000}"
        };

        _iterations = new ()
        {
            X = 14,
            Y = 7,
            Value = 80,
            Increment = 10
        };

        _span.ValueChanging += (_, args) =>
                               {
                                   if (args.NewValue <= 0.05)
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

        Button reset = new ()
        {
            X = 1,
            Y = 10,
            Text = "_Reset"
        };

        reset.Accepted += (_, _) => ResetSettings ();

        Button overlay = new ()
        {
            X = Pos.Right (reset) + 2,
            Y = 10,
            Text = "_Overlay"
        };

        overlay.Accepted += (_, _) => ShowOverlay ();

        Label note = new ()
        {
            X = 1,
            Y = 13,
            Width = Dim.Fill (1),
            Height = 4,
            Text = "The image is a 30 x 20\nImageView SubView drawn\nthrough sixel raster\ncommands."
        };

        settings.Add (_centerX, _centerY, _span, _iterations, reset, overlay, note);
    }

    private void ResetSettings ()
    {
        _centerX.Value = -0.5;
        _centerY.Value = 0;
        _span.Value = 3;
        _iterations.Value = 80;
        RenderMandelbrot ();
    }

    private void RenderMandelbrot ()
    {
        if (_mandelbrotView is null)
        {
            return;
        }

        SixelSupportResult support = EnsureSixelSupportForDemo ();
        int pixelWidth = ImageColumns * Math.Max (1, support.Resolution.Width);
        int pixelHeight = ImageRows * Math.Max (1, support.Resolution.Height);
        int iterations = _iterations.Value;
        double centerX = _centerX.Value;
        double centerY = _centerY.Value;
        double span = _span.Value;

        _mandelbrotView.Render (pixelWidth, pixelHeight, centerX, centerY, span, iterations, support);
        _status.Text =
            $"Sixel raster: {pixelWidth} x {pixelHeight}px, {support.Resolution.Width} x {support.Resolution.Height}px/cell";
        _status.SetNeedsDraw ();
    }

    private SixelSupportResult EnsureSixelSupportForDemo ()
    {
        SixelSupportResult source = _app.Driver?.SixelSupport ?? new ();

        if (source.IsSupported)
        {
            return source;
        }

        SixelSupportResult forced = new ()
        {
            IsSupported = true,
            Resolution = source.Resolution,
            MaxPaletteColors = source.MaxPaletteColors,
            SupportsTransparency = source.SupportsTransparency
        };

        if (_app.Driver is DriverImpl driver)
        {
            driver.SetSixelSupport (forced);
        }

        return forced;
    }

    private void ShowOverlay ()
    {
        _mandelbrotView.SetNeedsDraw ();

        Dialog dialog = new ()
        {
            Title = "Overlay Runnable",
            Width = 38,
            Height = 9
        };

        dialog.Add (
                    new Label
                    {
                        X = 1,
                        Y = 1,
                        Width = Dim.Fill (2),
                        Height = 3,
                        Text = "This dialog is a runnable\nshown over the sixel image\nto exercise clipping."
                    });

        dialog.AddButton (new () { Text = "_OK", IsDefault = true });
        _app.Run (dialog);
        dialog.Dispose ();

        _mandelbrotView.SetNeedsDraw ();
    }

    private sealed class MandelbrotImageView : ImageView
    {
        public void Render (int pixelWidth,
                            int pixelHeight,
                            double centerX,
                            double centerY,
                            double span,
                            int maxIterations,
                            SixelSupportResult support)
        {
            SixelEncoder = new ();
            SixelEncoder.Quantizer.MaxColors = Math.Min (support.MaxPaletteColors, 64);
            Image = CreateMandelbrotPixels (pixelWidth, pixelHeight, centerX, centerY, span, maxIterations);
        }

        private static Color [,] CreateMandelbrotPixels (int width,
                                                        int height,
                                                        double centerX,
                                                        double centerY,
                                                        double span,
                                                        int maxIterations)
        {
            Color [,] pixels = new Color [width, height];
            double spanY = span * height / width;
            double xMin = centerX - span / 2;
            double yMin = centerY - spanY / 2;

            for (int y = 0; y < height; y++)
            {
                double cy = yMin + spanY * y / Math.Max (1, height - 1);

                for (int x = 0; x < width; x++)
                {
                    double cx = xMin + span * x / Math.Max (1, width - 1);
                    int iterations = CountIterations (cx, cy, maxIterations);
                    pixels [x, y] = GetMandelbrotColor (iterations, maxIterations);
                }
            }

            return pixels;
        }

        private static int CountIterations (double cx, double cy, int maxIterations)
        {
            double zx = 0;
            double zy = 0;
            int iterations = 0;

            while (zx * zx + zy * zy <= 4 && iterations < maxIterations)
            {
                double nextX = zx * zx - zy * zy + cx;
                zy = 2 * zx * zy + cy;
                zx = nextX;
                iterations++;
            }

            return iterations;
        }

        private static Color GetMandelbrotColor (int iterations, int maxIterations)
        {
            if (iterations >= maxIterations)
            {
                return Color.Black;
            }

            double t = (double)iterations / maxIterations;
            byte red = (byte)(9 * (1 - t) * t * t * t * 255);
            byte green = (byte)(15 * (1 - t) * (1 - t) * t * t * 255);
            byte blue = (byte)(8.5 * (1 - t) * (1 - t) * (1 - t) * t * 255);

            return new Color (red, green, blue);
        }
    }
}
