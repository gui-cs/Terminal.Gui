using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ColorHelper;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Terminal.Gui;
using Color = Terminal.Gui.Color;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Images", "Demonstration of how to render an image with/without true color support.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
public class Images : Scenario
{
    private ImageView _imageView;
    private Point _screenLocationForSixel;
    private string _encodedSixelData;
    private Window _win;

    /// <summary>
    ///     Number of sixel pixels per row of characters in the console.
    /// </summary>
    private NumericUpDown _pxY;

    /// <summary>
    ///     Number of sixel pixels per column of characters in the console
    /// </summary>
    private NumericUpDown _pxX;

    /// <summary>
    ///     View shown in sixel tab if sixel is supported
    /// </summary>
    private View _sixelSupported;

    /// <summary>
    ///     View shown in sixel tab if sixel is not supported
    /// </summary>
    private View _sixelNotSupported;

    private Tab _tabSixel;
    private TabView _tabView;

    /// <summary>
    ///     The view into which the currently opened sixel image is bounded
    /// </summary>
    private View _sixelView;

    private DoomFire _fire;
    private SixelEncoder _fireEncoder;
    private SixelToRender _fireSixel;
    private int _fireFrameCounter;
    private bool _isDisposed;
    private RadioGroup _rgPaletteBuilder;
    private RadioGroup _rgDistanceAlgorithm;
    private NumericUpDown _popularityThreshold;
    private SixelToRender _sixelImage;

    // Start by assuming no support
    private SixelSupportResult _sixelSupportResult = new ();
    private CheckBox _cbSupportsSixel;

    public override void Main ()
    {
        Application.Init ();

        _win = new () { Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}" };

        bool canTrueColor = Application.Driver?.SupportsTrueColor ?? false;

        var tabBasic = new Tab
        {
            DisplayText = "Basic"
        };

        _tabSixel = new ()
        {
            DisplayText = "Sixel"
        };

        var lblDriverName = new Label { X = 0, Y = 0, Text = $"Driver is {Application.Driver?.GetType ().Name}" };
        _win.Add (lblDriverName);

        var cbSupportsTrueColor = new CheckBox
        {
            X = Pos.Right (lblDriverName) + 2,
            Y = 0,
            CheckedState = canTrueColor ? CheckState.Checked : CheckState.UnChecked,
            CanFocus = false,
            Text = "supports true color "
        };
        _win.Add (cbSupportsTrueColor);

        _cbSupportsSixel = new()
        {
            X = Pos.Right (lblDriverName) + 2,
            Y = 1,
            CheckedState = CheckState.UnChecked,
            Text = "Supports Sixel"
        };

        var lblSupportsSixel = new Label
        {
            X = Pos.Right (lblDriverName) + 2,
            Y = Pos.Bottom (_cbSupportsSixel),
            Text = "(Check if your terminal supports Sixel)"
        };

        /*        CheckedState = _sixelSupportResult.IsSupported
                                   ? CheckState.Checked
                                   : CheckState.UnChecked;*/

        _cbSupportsSixel.CheckedStateChanging += (s, e) =>
                                                 {
                                                     _sixelSupportResult.IsSupported = e.NewValue == CheckState.Checked;
                                                     SetupSixelSupported (e.NewValue == CheckState.Checked);
                                                     ApplyShowTabViewHack ();
                                                 };

        _win.Add (_cbSupportsSixel);

        var cbUseTrueColor = new CheckBox
        {
            X = Pos.Right (cbSupportsTrueColor) + 2,
            Y = 0,
            CheckedState = !Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
            Enabled = canTrueColor,
            Text = "Use true color"
        };
        cbUseTrueColor.CheckedStateChanging += (_, evt) => Application.Force16Colors = evt.NewValue == CheckState.UnChecked;
        _win.Add (cbUseTrueColor);

        var btnOpenImage = new Button { X = Pos.Right (cbUseTrueColor) + 2, Y = 0, Text = "Open Image" };
        _win.Add (btnOpenImage);

        _tabView = new ()
        {
            Y = Pos.Bottom (lblSupportsSixel), Width = Dim.Fill (), Height = Dim.Fill ()
        };

        _tabView.AddTab (tabBasic, true);
        _tabView.AddTab (_tabSixel, false);

        BuildBasicTab (tabBasic);
        BuildSixelTab ();

        SetupSixelSupported (_cbSupportsSixel.CheckedState == CheckState.Checked);

        btnOpenImage.Accepting += OpenImage;

        _win.Add (lblSupportsSixel);
        _win.Add (_tabView);

        // Start trying to detect sixel support
        var sixelSupportDetector = new SixelSupportDetector ();
        sixelSupportDetector.Detect (UpdateSixelSupportState);

        Application.Run (_win);
        _win.Dispose ();
        Application.Shutdown ();
    }

    private void UpdateSixelSupportState (SixelSupportResult newResult)
    {
        _sixelSupportResult = newResult;

        _cbSupportsSixel.CheckedState = newResult.IsSupported ? CheckState.Checked : CheckState.UnChecked;
        _pxX.Value = _sixelSupportResult.Resolution.Width;
        _pxY.Value = _sixelSupportResult.Resolution.Height;
    }

    private void SetupSixelSupported (bool isSupported)
    {
        _tabSixel.View = isSupported ? _sixelSupported : _sixelNotSupported;
        _tabView.SetNeedsDraw ();
    }

    private void BtnStartFireOnAccept (object sender, CommandEventArgs e)
    {
        if (_fire != null)
        {
            return;
        }

        if (!_sixelSupportResult.SupportsTransparency)
        {
            if (MessageBox.Query (
                                  "Transparency Not Supported",
                                  "It looks like your terminal does not support transparent sixel backgrounds. Do you want to try anyway?",
                                  "Yes",
                                  "No")
                != 0)
            {
                return;
            }
        }

        _fire = new (_win.Frame.Width * _pxX.Value, _win.Frame.Height * _pxY.Value);
        _fireEncoder = new ();
        _fireEncoder.Quantizer.MaxColors = Math.Min (_fireEncoder.Quantizer.MaxColors, _sixelSupportResult.MaxPaletteColors);
        _fireEncoder.Quantizer.PaletteBuildingAlgorithm = new ConstPalette (_fire.Palette);

        _fireFrameCounter = 0;

        Application.AddTimeout (TimeSpan.FromMilliseconds (30), AdvanceFireTimerCallback);
    }

    private bool AdvanceFireTimerCallback ()
    {
        _fire.AdvanceFrame ();
        _fireFrameCounter++;

        // Control frame rate by adjusting this
        // Lower number means more FPS
        if (_fireFrameCounter % 2 != 0 || _isDisposed)
        {
            return !_isDisposed;
        }

        Color [,] bmp = _fire.GetFirePixels ();

        // TODO: Static way of doing this, suboptimal
        if (_fireSixel != null)
        {
            Application.Sixel.Remove (_fireSixel);
        }

        _fireSixel = new ()
        {
            SixelData = _fireEncoder.EncodeSixel (bmp),
            ScreenPosition = new (0, 0)
        };

        Application.Sixel.Add (_fireSixel);

        _win.SetNeedsDraw ();

        return !_isDisposed;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);
        _imageView.Dispose ();
        _sixelNotSupported.Dispose ();
        _sixelSupported.Dispose ();
        _isDisposed = true;

        Application.Sixel.Clear ();
    }

    private void OpenImage (object sender, CommandEventArgs e)
    {
        var ofd = new OpenDialog { Title = "Open Image", AllowsMultipleSelection = false };
        Application.Run (ofd);

        if (ofd.Path is { })
        {
            Directory.SetCurrentDirectory (Path.GetFullPath (Path.GetDirectoryName (ofd.Path)!));
        }

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

        Image<Rgba32> img;

        try
        {
            img = Image.Load<Rgba32> (File.ReadAllBytes (path));
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery ("Could not open file", ex.Message, "Ok");

            return;
        }

        _imageView.SetImage (img);
        ApplyShowTabViewHack ();
        Application.LayoutAndDraw ();
    }

    private void ApplyShowTabViewHack ()
    {
        // TODO HACK: This hack seems to be required to make tabview actually refresh itself
        _tabView.SetNeedsDraw ();
        Tab orig = _tabView.SelectedTab;
        _tabView.SelectedTab = _tabView.Tabs.Except (new [] { orig }).ElementAt (0);
        _tabView.SelectedTab = orig;
    }

    private void BuildBasicTab (Tab tabBasic)
    {
        _imageView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true
        };

        tabBasic.View = _imageView;
    }

    private void BuildSixelTab ()
    {
        _sixelSupported = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true
        };

        _sixelNotSupported = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true
        };

        _sixelNotSupported.Add (
                                new Label
                                {
                                    Width = Dim.Fill (),
                                    Height = Dim.Fill (),
                                    TextAlignment = Alignment.Center,
                                    Text = "Your driver does not support Sixel image format",
                                    VerticalTextAlignment = Alignment.Center
                                });

        _sixelView = new ()
        {
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };

        _sixelSupported.Add (_sixelView);

        var btnSixel = new Button
        {
            X = Pos.Right (_sixelView),
            Y = 0,
            Text = "Output Sixel", Width = Dim.Auto ()
        };
        btnSixel.Accepting += OutputSixelButtonClick;
        _sixelSupported.Add (btnSixel);

        var btnStartFire = new Button
        {
            X = Pos.Right (_sixelView),
            Y = Pos.Bottom (btnSixel),
            Text = "Start Fire"
        };
        btnStartFire.Accepting += BtnStartFireOnAccept;
        _sixelSupported.Add (btnStartFire);

        var lblPxX = new Label
        {
            X = Pos.Right (_sixelView),
            Y = Pos.Bottom (btnStartFire) + 1,
            Text = "Pixels per Col:"
        };

        _pxX = new ()
        {
            X = Pos.Right (lblPxX),
            Y = Pos.Bottom (btnStartFire) + 1,
            Value = _sixelSupportResult.Resolution.Width
        };

        var lblPxY = new Label
        {
            X = lblPxX.X,
            Y = Pos.Bottom (_pxX),
            Text = "Pixels per Row:"
        };

        _pxY = new ()
        {
            X = Pos.Right (lblPxY),
            Y = Pos.Bottom (_pxX),
            Value = _sixelSupportResult.Resolution.Height
        };

        var l1 = new Label
        {
            Text = "Palette Building Algorithm",
            Width = Dim.Auto (),
            X = Pos.Right (_sixelView),
            Y = Pos.Bottom (_pxY) + 1
        };

        _rgPaletteBuilder = new ()
        {
            RadioLabels = new []
            {
                "Popularity",
                "Median Cut"
            },
            X = Pos.Right (_sixelView) + 2,
            Y = Pos.Bottom (l1),
            SelectedItem = 1
        };

        _popularityThreshold = new ()
        {
            X = Pos.Right (_rgPaletteBuilder) + 1,
            Y = Pos.Top (_rgPaletteBuilder),
            Value = 8
        };

        var lblPopThreshold = new Label
        {
            Text = "(threshold)",
            X = Pos.Right (_popularityThreshold),
            Y = Pos.Top (_popularityThreshold)
        };

        var l2 = new Label
        {
            Text = "Color Distance Algorithm",
            Width = Dim.Auto (),
            X = Pos.Right (_sixelView),
            Y = Pos.Bottom (_rgPaletteBuilder) + 1
        };

        _rgDistanceAlgorithm = new ()
        {
            RadioLabels = new []
            {
                "Euclidian",
                "CIE76"
            },
            X = Pos.Right (_sixelView) + 2,
            Y = Pos.Bottom (l2)
        };

        _sixelSupported.Add (lblPxX);
        _sixelSupported.Add (_pxX);
        _sixelSupported.Add (lblPxY);
        _sixelSupported.Add (_pxY);
        _sixelSupported.Add (l1);
        _sixelSupported.Add (_rgPaletteBuilder);

        _sixelSupported.Add (l2);
        _sixelSupported.Add (_rgDistanceAlgorithm);
        _sixelSupported.Add (_popularityThreshold);
        _sixelSupported.Add (lblPopThreshold);

        _sixelView.DrawingContent += SixelViewOnDrawingContent;
    }

    private IPaletteBuilder GetPaletteBuilder ()
    {
        switch (_rgPaletteBuilder.SelectedItem)
        {
            case 0: return new PopularityPaletteWithThreshold (GetDistanceAlgorithm (), _popularityThreshold.Value);
            case 1: return new MedianCutPaletteBuilder (GetDistanceAlgorithm ());
            default: throw new ArgumentOutOfRangeException ();
        }
    }

    private IColorDistance GetDistanceAlgorithm ()
    {
        switch (_rgDistanceAlgorithm.SelectedItem)
        {
            case 0: return new EuclideanColorDistance ();
            case 1: return new CIE76ColorDistance ();
            default: throw new ArgumentOutOfRangeException ();
        }
    }

    private void OutputSixelButtonClick (object sender, CommandEventArgs e)
    {
        if (_imageView.FullResImage == null)
        {
            MessageBox.Query ("No Image Loaded", "You must first open an image.  Use the 'Open Image' button above.", "Ok");

            return;
        }

        _screenLocationForSixel = _sixelView.FrameToScreen ().Location;

        _encodedSixelData = GenerateSixelData (
                                               _imageView.FullResImage,
                                               _sixelView.Frame.Size,
                                               _pxX.Value,
                                               _pxY.Value);

        if (_sixelImage == null)
        {
            _sixelImage = new ()
            {
                SixelData = _encodedSixelData,
                ScreenPosition = _screenLocationForSixel
            };

            Application.Sixel.Add (_sixelImage);
        }
        else
        {
            _sixelImage.ScreenPosition = _screenLocationForSixel;
            _sixelImage.SixelData = _encodedSixelData;
        }

        _sixelView.SetNeedsDraw ();
    }

    private void SixelViewOnDrawingContent (object sender, DrawEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace (_encodedSixelData))
        {
            // Does not work (see https://github.com/gui-cs/Terminal.Gui/issues/3763)
            // Application.Driver?.Move (_screenLocationForSixel.X, _screenLocationForSixel.Y);
            // Application.Driver?.AddStr (_encodedSixelData);

            // Works in NetDriver but results in screen flicker when moving mouse but vanish instantly
            // Console.SetCursorPosition (_screenLocationForSixel.X, _screenLocationForSixel.Y);
            // Console.Write (_encodedSixelData);
        }
    }

    public string GenerateSixelData (
        Image<Rgba32> fullResImage,
        Size maxSize,
        int pixelsPerCellX,
        int pixelsPerCellY
    )
    {
        var encoder = new SixelEncoder ();
        encoder.Quantizer.MaxColors = Math.Min (encoder.Quantizer.MaxColors, _sixelSupportResult.MaxPaletteColors);
        encoder.Quantizer.PaletteBuildingAlgorithm = GetPaletteBuilder ();
        encoder.Quantizer.DistanceAlgorithm = GetDistanceAlgorithm ();

        // Calculate the target size in pixels based on console units
        int targetWidthInPixels = maxSize.Width * pixelsPerCellX;
        int targetHeightInPixels = maxSize.Height * pixelsPerCellY;

        // Get the original image dimensions
        int originalWidth = fullResImage.Width;
        int originalHeight = fullResImage.Height;

        // Use the helper function to get the resized dimensions while maintaining the aspect ratio
        Size newSize = CalculateAspectRatioFit (originalWidth, originalHeight, targetWidthInPixels, targetHeightInPixels);

        // Resize the image to match the console size
        Image<Rgba32> resizedImage = fullResImage.Clone (x => x.Resize (newSize.Width, newSize.Height));

        string encoded = encoder.EncodeSixel (ConvertToColorArray (resizedImage));

        var pv = new PaletteView (encoder.Quantizer.Palette.ToList ());

        var dlg = new Dialog
        {
            Title = "Palette (Esc to close)",
            Width = Dim.Fill (2),
            Height = Dim.Fill (1)
        };

        var btn = new Button
        {
            Text = "Ok"
        };

        btn.Accepting += (s, e) => Application.RequestStop ();
        dlg.Add (pv);
        dlg.AddButton (btn);
        Application.Run (dlg);
        dlg.Dispose ();

        return encoded;
    }

    private Size CalculateAspectRatioFit (int originalWidth, int originalHeight, int targetWidth, int targetHeight)
    {
        // Calculate the scaling factor for width and height
        double widthScale = (double)targetWidth / originalWidth;
        double heightScale = (double)targetHeight / originalHeight;

        // Use the smaller scaling factor to maintain the aspect ratio
        double scale = Math.Min (widthScale, heightScale);

        // Calculate the new width and height while keeping the aspect ratio
        var newWidth = (int)(originalWidth * scale);
        var newHeight = (int)(originalHeight * scale);

        // Return the new size as a Size object
        return new (newWidth, newHeight);
    }

    public static Color [,] ConvertToColorArray (Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;
        Color [,] colors = new Color [width, height];

        // Loop through each pixel and convert Rgba32 to Terminal.Gui color
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                Rgba32 pixel = image [x, y];
                colors [x, y] = new (pixel.R, pixel.G, pixel.B); // Convert Rgba32 to Terminal.Gui color
            }
        }

        return colors;
    }

    private class ImageView : View
    {
        private readonly ConcurrentDictionary<Rgba32, Attribute> _cache = new ();
        public Image<Rgba32> FullResImage;
        private Image<Rgba32> _matchSize;

        protected override bool OnDrawingContent ()
        {
            if (FullResImage == null)
            {
                return true;
            }

            // if we have not got a cached resized image of this size
            if (_matchSize == null || Viewport.Width != _matchSize.Width || Viewport.Height != _matchSize.Height)
            {
                // generate one
                _matchSize = FullResImage.Clone (x => x.Resize (Viewport.Width, Viewport.Height));
            }

            for (var y = 0; y < Viewport.Height; y++)
            {
                for (var x = 0; x < Viewport.Width; x++)
                {
                    Rgba32 rgb = _matchSize [x, y];

                    Attribute attr = _cache.GetOrAdd (
                                                      rgb,
                                                      rgb => new (
                                                                  new Color (),
                                                                  new Color (rgb.R, rgb.G, rgb.B)
                                                                 )
                                                     );

                    SetAttribute (attr);
                    AddRune (x, y, (Rune)' ');
                }
            }

            return true;
        }

        internal void SetImage (Image<Rgba32> image)
        {
            FullResImage = image;
            SetNeedsDraw ();
        }
    }

    public class PaletteView : View
    {
        private readonly List<Color> _palette;

        public PaletteView (List<Color> palette)
        {
            _palette = palette ?? new List<Color> ();
            Width = Dim.Fill ();
            Height = Dim.Fill ();
        }

        // Automatically calculates rows and columns based on the available bounds
        private (int columns, int rows) CalculateGridSize (Rectangle bounds)
        {
            // Characters are twice as wide as they are tall, so use 2:1 width-to-height ratio
            int availableWidth = Viewport.Width / 2; // Each color block is 2 character wide
            int availableHeight = Viewport.Height;

            int numColors = _palette.Count;

            // Calculate the number of columns and rows we can fit within the bounds
            int columns = Math.Min (availableWidth, numColors);
            int rows = (numColors + columns - 1) / columns; // Ceiling division for rows

            // Ensure we do not exceed the available height
            if (rows > availableHeight)
            {
                rows = availableHeight;
                columns = (numColors + rows - 1) / rows; // Recalculate columns if needed
            }

            return (columns, rows);
        }

        protected override bool OnDrawingContent ()
        {
            if (_palette == null || _palette.Count == 0)
            {
                return false;
            }

            // Calculate the grid size based on the bounds
            (int columns, int rows) = CalculateGridSize (Viewport);

            // Draw the colors in the palette
            for (var i = 0; i < _palette.Count && i < columns * rows; i++)
            {
                int row = i / columns;
                int col = i % columns;

                // Calculate position in the grid
                int x = col * 2; // Each color block takes up 2 horizontal spaces
                int y = row;

                // Set the color attribute for the block
                SetAttribute (new (_palette [i], _palette [i]));

                // Draw the block (2 characters wide per block)
                for (var dx = 0; dx < 2; dx++) // Fill the width of the block
                {
                    AddRune (x + dx, y, (Rune)' ');
                }
            }

            return true;
        }
    }
}

internal class ConstPalette : IPaletteBuilder
{
    private readonly List<Color> _palette;

    public ConstPalette (Color [] palette) { _palette = palette.ToList (); }

    /// <inheritdoc/>
    public List<Color> BuildPalette (List<Color> colors, int maxColors) { return _palette; }
}

public abstract class LabColorDistance : IColorDistance
{
    // Reference white point for D65 illuminant (can be moved to constants)
    private const double RefX = 95.047;
    private const double RefY = 100.000;
    private const double RefZ = 108.883;

    // Conversion from RGB to Lab
    protected LabColor RgbToLab (Color c)
    {
        XYZ xyz = ColorConverter.RgbToXyz (new (c.R, c.G, c.B));

        // Normalize XYZ values by reference white point
        double x = xyz.X / RefX;
        double y = xyz.Y / RefY;
        double z = xyz.Z / RefZ;

        // Apply the nonlinear transformation for Lab
        x = x > 0.008856 ? Math.Pow (x, 1.0 / 3.0) : 7.787 * x + 16.0 / 116.0;
        y = y > 0.008856 ? Math.Pow (y, 1.0 / 3.0) : 7.787 * y + 16.0 / 116.0;
        z = z > 0.008856 ? Math.Pow (z, 1.0 / 3.0) : 7.787 * z + 16.0 / 116.0;

        // Calculate Lab values
        double l = 116.0 * y - 16.0;
        double a = 500.0 * (x - y);
        double b = 200.0 * (y - z);

        return new (l, a, b);
    }

    // LabColor class encapsulating L, A, and B values
    protected class LabColor
    {
        public double L { get; }
        public double A { get; }
        public double B { get; }

        public LabColor (double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }
    }

    /// <inheritdoc/>
    public abstract double CalculateDistance (Color c1, Color c2);
}

/// <summary>
///     This is the simplest method to measure color difference in the CIE Lab color space. The Euclidean distance in Lab
///     space is more aligned with human perception than RGB space, as Lab attempts to model how humans perceive color
///     differences.
/// </summary>
public class CIE76ColorDistance : LabColorDistance
{
    public override double CalculateDistance (Color c1, Color c2)
    {
        LabColor lab1 = RgbToLab (c1);
        LabColor lab2 = RgbToLab (c2);

        // Euclidean distance in Lab color space
        return Math.Sqrt (Math.Pow (lab1.L - lab2.L, 2) + Math.Pow (lab1.A - lab2.A, 2) + Math.Pow (lab1.B - lab2.B, 2));
    }
}

public class MedianCutPaletteBuilder : IPaletteBuilder
{
    private readonly IColorDistance _colorDistance;

    public MedianCutPaletteBuilder (IColorDistance colorDistance) { _colorDistance = colorDistance; }

    public List<Color> BuildPalette (List<Color> colors, int maxColors)
    {
        if (colors == null || colors.Count == 0 || maxColors <= 0)
        {
            return new ();
        }

        return MedianCut (colors, maxColors);
    }

    private List<Color> MedianCut (List<Color> colors, int maxColors)
    {
        List<List<Color>> cubes = new () { colors };

        // Recursively split color regions
        while (cubes.Count < maxColors)
        {
            var added = false;
            cubes.Sort ((a, b) => Volume (a).CompareTo (Volume (b)));

            List<Color> largestCube = cubes.Last ();
            cubes.RemoveAt (cubes.Count - 1);

            // Check if the largest cube contains only one unique color
            if (IsSingleColorCube (largestCube))
            {
                // Add back and stop splitting this cube
                cubes.Add (largestCube);

                break;
            }

            (List<Color> cube1, List<Color> cube2) = SplitCube (largestCube);

            if (cube1.Any ())
            {
                cubes.Add (cube1);
                added = true;
            }

            if (cube2.Any ())
            {
                cubes.Add (cube2);
                added = true;
            }

            // Break the loop if no new cubes were added
            if (!added)
            {
                break;
            }
        }

        // Calculate average color for each cube
        return cubes.Select (AverageColor).Distinct ().ToList ();
    }

    // Checks if all colors in the cube are the same
    private bool IsSingleColorCube (List<Color> cube)
    {
        Color firstColor = cube.First ();

        return cube.All (c => c.R == firstColor.R && c.G == firstColor.G && c.B == firstColor.B);
    }

    // Splits the cube based on the largest color component range
    private (List<Color>, List<Color>) SplitCube (List<Color> cube)
    {
        (int component, int range) = FindLargestRange (cube);

        // Sort by the largest color range component (either R, G, or B)
        cube.Sort (
                   (c1, c2) => component switch
                               {
                                   0 => c1.R.CompareTo (c2.R),
                                   1 => c1.G.CompareTo (c2.G),
                                   2 => c1.B.CompareTo (c2.B),
                                   _ => 0
                               });

        int medianIndex = cube.Count / 2;
        List<Color> cube1 = cube.Take (medianIndex).ToList ();
        List<Color> cube2 = cube.Skip (medianIndex).ToList ();

        return (cube1, cube2);
    }

    private (int, int) FindLargestRange (List<Color> cube)
    {
        byte minR = cube.Min (c => c.R);
        byte maxR = cube.Max (c => c.R);
        byte minG = cube.Min (c => c.G);
        byte maxG = cube.Max (c => c.G);
        byte minB = cube.Min (c => c.B);
        byte maxB = cube.Max (c => c.B);

        int rangeR = maxR - minR;
        int rangeG = maxG - minG;
        int rangeB = maxB - minB;

        if (rangeR >= rangeG && rangeR >= rangeB)
        {
            return (0, rangeR);
        }

        if (rangeG >= rangeR && rangeG >= rangeB)
        {
            return (1, rangeG);
        }

        return (2, rangeB);
    }

    private Color AverageColor (List<Color> cube)
    {
        var avgR = (byte)cube.Average (c => c.R);
        var avgG = (byte)cube.Average (c => c.G);
        var avgB = (byte)cube.Average (c => c.B);

        return new (avgR, avgG, avgB);
    }

    private int Volume (List<Color> cube)
    {
        if (cube == null || cube.Count == 0)
        {
            // Return a volume of 0 if the cube is empty or null
            return 0;
        }

        byte minR = cube.Min (c => c.R);
        byte maxR = cube.Max (c => c.R);
        byte minG = cube.Min (c => c.G);
        byte maxG = cube.Max (c => c.G);
        byte minB = cube.Min (c => c.B);
        byte maxB = cube.Max (c => c.B);

        return (maxR - minR) * (maxG - minG) * (maxB - minB);
    }
}

public class DoomFire
{
    private readonly int _width;
    private readonly int _height;
    private readonly Color [,] _firePixels;
    private static Color [] _palette;
    public Color [] Palette => _palette;
    private readonly Random _random = new ();

    public DoomFire (int width, int height)
    {
        _width = width;
        _height = height;
        _firePixels = new Color [width, height];
        InitializePalette ();
        InitializeFire ();
    }

    private void InitializePalette ()
    {
        // Initialize a basic fire palette. You can modify these colors as needed.
        _palette = new Color [37]; // Using 37 colors as per the original Doom fire palette scale.

        // First color is transparent black
        _palette [0] = new (0, 0, 0, 0); // Transparent black (ARGB)

        // The rest of the palette is fire colors
        for (var i = 1; i < 37; i++)
        {
            var r = (byte)Math.Min (255, i * 7);
            var g = (byte)Math.Min (255, i * 5);
            var b = (byte)Math.Min (255, i * 2);
            _palette [i] = new (r, g, b); // Full opacity
        }
    }

    public void InitializeFire ()
    {
        // Set the bottom row to full intensity (simulate the base of the fire).
        for (var x = 0; x < _width; x++)
        {
            _firePixels [x, _height - 1] = _palette [36]; // Max intensity fire.
        }

        // Set the rest of the pixels to black (transparent).
        for (var y = 0; y < _height - 1; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                _firePixels [x, y] = _palette [0]; // Transparent black
            }
        }
    }

    public void AdvanceFrame ()
    {
        // Process every pixel except the bottom row
        for (var x = 0; x < _width; x++)
        {
            for (var y = 1; y < _height; y++) // Skip the last row (which is always max intensity)
            {
                int srcX = x;
                int srcY = y;
                int dstY = y - 1;

                // Spread fire upwards with randomness
                int decay = _random.Next (0, 2);
                int dstX = srcX + _random.Next (-1, 2);

                if (dstX < 0 || dstX >= _width) // Prevent out of bounds
                {
                    dstX = srcX;
                }

                // Get the fire color from below and reduce its intensity
                Color srcColor = _firePixels [srcX, srcY];
                int intensity = Array.IndexOf (_palette, srcColor) - decay;

                if (intensity < 0)
                {
                    intensity = 0;
                }

                _firePixels [dstX, dstY] = _palette [intensity];
            }
        }
    }

    public Color [,] GetFirePixels () { return _firePixels; }
}
