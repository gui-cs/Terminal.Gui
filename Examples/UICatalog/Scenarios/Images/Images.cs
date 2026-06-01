using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Images", "Demonstration of how to render an image with/without true color support.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
public class Images : Scenario
{
    private ImageView _basicImageView;
    private Image<Rgba32> _fullResImage;
    private Window _win;

    /// <summary>
    ///     Number of sixel pixels per row of characters in the console.
    /// </summary>
    private NumericUpDown _pxY;

    /// <summary>
    ///     Number of sixel pixels per column of characters in the console
    /// </summary>
    private NumericUpDown _pxX;

    private View _sixelSettings;
    private View _tabSixel;

    /// <summary>
    ///     The view into which the currently opened sixel image is bounded
    /// </summary>
    private ImageView _sixelImageView;

    private DoomFire _fire;
    private SixelEncoder _fireEncoder;
    private SixelToRender _fireSixel;
    private int _fireFrameCounter;
    private bool _isDisposed;
    private OptionSelector _osPaletteBuilder;
    private OptionSelector _osDistanceAlgorithm;
    private NumericUpDown _popularityThreshold;

    // Start by assuming no support — updated from driver-level detection.
    private SixelSupportResult _sixelSupportResult = new ();
    private CheckBox _cbSupportsSixel;
    private IApplication _app;
    private Size _winSize;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        _win = new Window { Title = $"{Application.GetDefaultKey (Command.Quit)} to Quit - Scenario: {GetName ()}" };

        bool canTrueColor = app.Driver?.SupportsTrueColor ?? false;

        View tabBasic = new () { Title = "_Cell-based", Width = Dim.Fill (), Height = Dim.Fill () };
        _tabSixel = new View { Title = "_Sixel-based", Width = Dim.Fill (), Height = Dim.Fill () };

        CheckBox cbSupportsTrueColor = new ()
        {
            Y = 0, Value = canTrueColor ? CheckState.Checked : CheckState.UnChecked, CanFocus = false, Text = "Driver supports true color"
        };
        _win.Add (cbSupportsTrueColor);

        _cbSupportsSixel = new CheckBox { X = Pos.Right (cbSupportsTrueColor) + 2, Y = 1, Value = CheckState.UnChecked, Text = "Supports Sixel" };

        _cbSupportsSixel.ValueChanging += (_, e) =>
                                          {
                                              _sixelSupportResult.IsSupported = e.NewValue == CheckState.Checked;
                                              _tabSixel.Enabled = _sixelSupportResult.IsSupported;
                                          };

        _win.Add (_cbSupportsSixel);

        CheckBox cbUseBackgroundRendering = new ()
        {
            X = Pos.Right (_cbSupportsSixel) + 2,
            Y = Pos.Top (_cbSupportsSixel),
            Value = CheckState.Checked,
            Text = "Async rendering"
        };
        cbUseBackgroundRendering.ValueChanging += (_, evt) => SetBackgroundRendering (evt.NewValue == CheckState.Checked);
        _win.Add (cbUseBackgroundRendering);

        CheckBox cbUseTrueColor = new ()
        {
            X = Pos.Right (cbSupportsTrueColor) + 2,
            Y = 0,
            Value = !Driver.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
            Enabled = canTrueColor,
            Text = "Use true color"
        };
        cbUseTrueColor.ValueChanging += (_, evt) => Driver.Force16Colors = evt.NewValue == CheckState.UnChecked;
        _win.Add (cbUseTrueColor);

        Button btnOpenImage = new () { Y = Pos.Bottom (cbUseTrueColor), Text = "_Open Image" };
        _win.Add (btnOpenImage);

        Button btnStartFire = new () { X = Pos.Right (btnOpenImage), Y = Pos.Bottom (cbUseTrueColor), Text = "Start _Fire" };
        btnStartFire.Accepting += BtnStartFireOnAccept;
        _win.Add (btnStartFire);

        Tabs tabs = new () { Y = Pos.Bottom (btnOpenImage), Width = Dim.Fill (), Height = Dim.Fill () };
        BuildBasicTab (tabBasic);
        BuildSixelTab (_tabSixel);
        tabs.Add (tabBasic, _tabSixel);

        LoadDefaultImage ();

        btnOpenImage.Accepting += OpenImage;

        _win.Add (_cbSupportsSixel);
        _win.Add (tabs);

        _win.SubViewsLaidOut += Win_SubViewsLaidOut;
        _win.Initialized += (_, _) => UpdateSixelSupportState (app.Driver?.SixelSupport);
        app.Driver!.SixelSupportChanged += Driver_SixelSupportChanged;

        try
        {
            app.Run (_win);
        }
        finally
        {
            app.Driver!.SixelSupportChanged -= Driver_SixelSupportChanged;
            _win.Dispose ();
        }
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);
        _fullResImage?.Dispose ();
        _isDisposed = true;
    }

    private bool AdvanceFireTimerCallback ()
    {
        _fire.AdvanceFrame ();
        _fireFrameCounter++;

        // Control frame rate by adjusting this. Lower number means more FPS.
        if (_fireFrameCounter % 2 != 0 || _isDisposed)
        {
            return !_isDisposed;
        }

        Color [,] bmp = _fire.GetFirePixels ();
        string sixelFireData = _fireEncoder.EncodeSixel (bmp);

        if (_fireSixel == null)
        {
            _fireSixel = new SixelToRender { SixelData = sixelFireData, ScreenPosition = GetFireScreenPosition (), Id = "fireSixel", AlwaysRender = true };

            _app.Driver?.GetOutput ().GetSixels ().Enqueue (_fireSixel);
        }
        else
        {
            _fireSixel.SixelData = sixelFireData;
            _fireSixel.ScreenPosition = GetFireScreenPosition ();
        }

        _win.SetNeedsDraw ();

        return !_isDisposed;
    }

    private void BtnStartFireOnAccept (object sender, CommandEventArgs e)
    {
        if (_fire != null)
        {
            return;
        }

        if (!_sixelSupportResult.SupportsTransparency)
        {
            if (MessageBox.Query (_app!,
                                  "Transparency Not Supported",
                                  "It looks like your terminal does not support transparent sixel backgrounds. Do you want to try anyway?",
                                  "Yes",
                                  "No")
                != 0)
            {
                return;
            }
        }

        _winSize = _win.Viewport.Size;

        GenerateSixelFire (true);
    }

    private void BuildBasicTab (View tabBasic)
    {
        _basicImageView = new ImageView
        {
            BorderStyle = LineStyle.Dotted,
            Width = 30,
            Height = 10,
            CanFocus = true,
            Arrangement = ViewArrangement.Resizable,
            UseSixel = false,
            UseBackgroundRendering = true
        };

        tabBasic.Add (_basicImageView);
    }

    private void BuildSixelTab (View tabSixel)
    {
        _sixelSettings = new View { X = Pos.AnchorEnd (), Width = Dim.Auto (), Height = Dim.Auto (), CanFocus = true };

        _sixelImageView = new ImageView
        {
            CanFocus = true,
            Width = 30,
            Height = 10,
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.Resizable,
            UseSixel = true,
            UseBackgroundRendering = true,
            Title = "ImageView"
        };

        Label lblPxX = new () { Y = 0, Text = "Pixels per Col:" };
        _pxX = new NumericUpDown { X = Pos.Right (lblPxX), Y = 0, Value = GetDefaultFirePixelsPerColumn () };

        Label lblPxY = new () { X = lblPxX.X, Y = Pos.Bottom (_pxX), Text = "Pixels per Row:" };
        _pxY = new NumericUpDown { X = Pos.Right (lblPxY), Y = Pos.Bottom (_pxX), Value = GetDefaultFirePixelsPerRow () };

        Label paletteLabel = new () { Text = "Palette Building Algorithm", Width = Dim.Auto (), Y = Pos.Bottom (_pxY) + 1 };
        _osPaletteBuilder = new OptionSelector { Labels = ["Popularity", "Median Cut"], Y = Pos.Bottom (paletteLabel), Value = 1 };

        _popularityThreshold = new NumericUpDown { X = Pos.Right (_osPaletteBuilder) + 1, Y = Pos.Top (_osPaletteBuilder), Value = 8 };

        Label lblPopThreshold = new () { Text = "(threshold)", X = Pos.Right (_popularityThreshold), Y = Pos.Top (_popularityThreshold) };

        Label distanceLabel = new () { Text = "Color Distance Algorithm", Width = Dim.Auto (), Y = Pos.Bottom (_osPaletteBuilder) + 1 };
        _osDistanceAlgorithm = new OptionSelector { Labels = ["Euclidian", "CIE76"], Y = Pos.Bottom (distanceLabel) };

        _sixelSettings.Add (lblPxX);
        _sixelSettings.Add (_pxX);
        _sixelSettings.Add (lblPxY);
        _sixelSettings.Add (_pxY);
        _sixelSettings.Add (paletteLabel);
        _sixelSettings.Add (_osPaletteBuilder);
        _sixelSettings.Add (distanceLabel);
        _sixelSettings.Add (_osDistanceAlgorithm);
        _sixelSettings.Add (_popularityThreshold);
        _sixelSettings.Add (lblPopThreshold);

        tabSixel.Add (_sixelImageView, _sixelSettings);
    }

    private void SetBackgroundRendering (bool enabled)
    {
        _basicImageView?.UseBackgroundRendering = enabled;

        _sixelImageView?.UseBackgroundRendering = enabled;
    }

    private void Driver_SixelSupportChanged (object sender, ValueChangedEventArgs<SixelSupportResult> e) => UpdateSixelSupportState (e.NewValue);

    private void GenerateSixelFire (bool addTimeout)
    {
        int pixelsPerColumn = Math.Max (1, _pxX.Value);
        int pixelsPerRow = Math.Max (1, _pxY.Value);
        Size fireCellSize = GetFireTargetCellSize ();
        _fire = new DoomFire (fireCellSize.Width * pixelsPerColumn, fireCellSize.Height * pixelsPerRow);
        _fireEncoder = new SixelEncoder { AvoidBottomScroll = true };
        _fireEncoder.Quantizer.MaxColors = Math.Min (_fireEncoder.Quantizer.MaxColors, _sixelSupportResult.MaxPaletteColors);
        _fireEncoder.Quantizer.PaletteBuildingAlgorithm = new ConstPalette (_fire.Palette);

        _fireFrameCounter = 0;

        if (addTimeout)
        {
            _app?.AddTimeout (TimeSpan.FromMilliseconds (30), AdvanceFireTimerCallback);
        }
    }

    private Size GetFireTargetCellSize ()
    {
        Size frameSize = _win.Frame.Size;

        return new Size (Math.Max (1, frameSize.Width), Math.Max (1, frameSize.Height));
    }

    private Size GetFireRenderedCellSize ()
    {
        int pixelsPerColumn = Math.Max (1, _pxX.Value);
        int pixelsPerRow = Math.Max (1, _pxY.Value);
        Size fireCellSize = GetFireTargetCellSize ();
        int resolutionWidth = Math.Max (1, _sixelSupportResult.Resolution.Width);
        int resolutionHeight = Math.Max (1, _sixelSupportResult.Resolution.Height);
        int pixelWidth = fireCellSize.Width * pixelsPerColumn;
        int pixelHeight = Math.Max (1, fireCellSize.Height * pixelsPerRow - 6);

        return new Size (Math.Max (1, (pixelWidth + resolutionWidth - 1) / resolutionWidth),
                         Math.Max (1, (pixelHeight + resolutionHeight - 1) / resolutionHeight));
    }

    private Point GetFireScreenPosition ()
    {
        Rectangle frameScreen = _win.FrameToScreen ();
        Size fireCellSize = GetFireRenderedCellSize ();

        return new Point (frameScreen.X, Math.Max (frameScreen.Y, frameScreen.Bottom - fireCellSize.Height));
    }

    private int GetDefaultFirePixelsPerColumn () => Math.Max (1, _sixelSupportResult.Resolution.Width);

    private int GetDefaultFirePixelsPerRow () => Math.Max (1, _sixelSupportResult.Resolution.Height);

    private IColorDistance GetDistanceAlgorithm ()
    {
        switch (_osDistanceAlgorithm.Value)
        {
            case 0: return new EuclideanColorDistance ();

            case 1: return new CIE76ColorDistance ();

            default: throw new ArgumentOutOfRangeException ();
        }
    }

    private IPaletteBuilder GetPaletteBuilder ()
    {
        switch (_osPaletteBuilder.Value)
        {
            case 0: return new PopularityPaletteWithThreshold (GetDistanceAlgorithm (), _popularityThreshold.Value);

            case 1: return new MedianCutPaletteBuilder (GetDistanceAlgorithm ());

            default: throw new ArgumentOutOfRangeException ();
        }
    }

    private void LoadDefaultImage ()
    {
        Color [,] image = ImagesTestCard.Create (ImagesTestCard.DEFAULT_WIDTH, ImagesTestCard.DEFAULT_HEIGHT);
        _basicImageView.Image = image;
        UpdateSixelImage (image);
    }

    private void LoadImage (string path, bool showError)
    {
        Image<Rgba32> img;

        try
        {
            img = Image.Load<Rgba32> (File.ReadAllBytes (path));
        }
        catch (Exception ex)
        {
            if (showError)
            {
                MessageBox.ErrorQuery (_app!, "Could not open file", ex.Message, "Ok");
            }

            return;
        }

        _fullResImage?.Dispose ();
        _fullResImage = img;
        Color [,] image = ConvertToColorArray (img);
        _basicImageView.Image = image;
        UpdateSixelImage (image);
    }

    public static Color [,] ConvertToColorArray (Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;
        Color [,] colors = new Color [width, height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                Rgba32 pixel = image [x, y];
                colors [x, y] = new Color (pixel.R, pixel.G, pixel.B);
            }
        }

        return colors;
    }

    private void OpenImage (object sender, CommandEventArgs e)
    {
        OpenDialog ofd = new () { Title = "Open Image", AllowsMultipleSelection = false };
        _app?.Run (ofd);

        Directory.SetCurrentDirectory (Path.GetFullPath (Path.GetDirectoryName (ofd.Path)!));

        if (ofd.Canceled)
        {
            ofd.Dispose ();

            return;
        }

        string path = ofd.FilePaths [0];

        ofd.Dispose ();

        if (string.IsNullOrWhiteSpace (path))
        {
            return;
        }

        if (!File.Exists (path))
        {
            return;
        }

        LoadImage (path, true);
        _app?.LayoutAndDraw ();
    }

    private void UpdateSixelImage (Color [,] image)
    {
        SixelEncoder encoder = new ();
        encoder.Quantizer.MaxColors = Math.Min (encoder.Quantizer.MaxColors, _sixelSupportResult.MaxPaletteColors);
        encoder.Quantizer.PaletteBuildingAlgorithm = GetPaletteBuilder ();
        encoder.Quantizer.DistanceAlgorithm = GetDistanceAlgorithm ();
        _sixelImageView.SixelEncoder = encoder;
        _sixelImageView.Image = image;
    }

    private void UpdateSixelSupportState (SixelSupportResult newResult)
    {
        newResult ??= new SixelSupportResult ();
        _sixelSupportResult = newResult;

        _cbSupportsSixel.Value = newResult.IsSupported ? CheckState.Checked : CheckState.UnChecked;
        _pxX.Value = GetDefaultFirePixelsPerColumn ();
        _pxY.Value = GetDefaultFirePixelsPerRow ();

        if (_sixelImageView?.Image is { } image)
        {
            UpdateSixelImage (image);
        }
    }

    private void Win_SubViewsLaidOut (object sender, LayoutEventArgs e)
    {
        Size currentSize = _win.Viewport.Size;

        if (_winSize == currentSize)
        {
            return;
        }

        _winSize = currentSize;

        if (_fireSixel is { })
        {
            GenerateSixelFire (false);
        }
    }
}
