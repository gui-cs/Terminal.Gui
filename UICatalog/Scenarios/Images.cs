using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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

    public override void Main ()
    {
        Application.Init ();
        var win = new Window { Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}" };

        bool canTrueColor = Application.Driver?.SupportsTrueColor ?? false;

        var tabBasic = new Tab
        {
            DisplayText = "Basic"
        };

        var tabSixel = new Tab
        {
            DisplayText = "Sixel"
        };

        var lblDriverName = new Label { X = 0, Y = 0, Text = $"Driver is {Application.Driver?.GetType ().Name}" };
        win.Add (lblDriverName);

        var cbSupportsTrueColor = new CheckBox
        {
            X = Pos.Right (lblDriverName) + 2,
            Y = 0,
            CheckedState = canTrueColor ? CheckState.Checked : CheckState.UnChecked,
            CanFocus = false,
            Text = "supports true color "
        };
        win.Add (cbSupportsTrueColor);

        var cbUseTrueColor = new CheckBox
        {
            X = Pos.Right (cbSupportsTrueColor) + 2,
            Y = 0,
            CheckedState = !Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
            Enabled = canTrueColor,
            Text = "Use true color"
        };
        cbUseTrueColor.CheckedStateChanging += (_, evt) => Application.Force16Colors = evt.NewValue == CheckState.UnChecked;
        win.Add (cbUseTrueColor);

        var btnOpenImage = new Button { X = Pos.Right (cbUseTrueColor) + 2, Y = 0, Text = "Open Image" };
        win.Add (btnOpenImage);

        var tv = new TabView
        {
            Y = Pos.Bottom (lblDriverName), Width = Dim.Fill (), Height = Dim.Fill ()
        };

        tv.AddTab (tabBasic, true);
        tv.AddTab (tabSixel, false);

        BuildBasicTab (tabBasic);
        BuildSixelTab (tabSixel);

        btnOpenImage.Accept += OpenImage;

        win.Add (tv);
        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();

    }

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);
        _imageView.Dispose ();
    }

    private void OpenImage (object sender, HandledEventArgs e)
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
        Application.Refresh ();
    }

    private void BuildBasicTab (Tab tabBasic)
    {
        _imageView = new()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true

        };

        tabBasic.View = _imageView;
    }

    private void BuildSixelTab (Tab tabSixel)
    {
        tabSixel.View = new()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true
        };

        var btnSixel = new Button { X = 0, Y = 0, Text = "Output Sixel", Width = Dim.Auto () };
        tabSixel.View.Add (btnSixel);

        var sixelView = new View
        {
            Y = Pos.Bottom (btnSixel),
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };

        tabSixel.View.Add (sixelView);

        var lblPxX = new Label
        {
            X = Pos.Right (sixelView),
            Text = "Pixels per Col:"
        };

        var pxX = new NumericUpDown
        {
            X = Pos.Right (lblPxX),
            Value = 12
        };

        var lblPxY = new Label
        {
            X = lblPxX.X,
            Y = 1,
            Text = "Pixels per Row:"
        };

        var pxY = new NumericUpDown
        {
            X = Pos.Right (lblPxY),
            Y = 1,
            Value = 6
        };

        tabSixel.View.Add (lblPxX);
        tabSixel.View.Add (pxX);
        tabSixel.View.Add (lblPxY);
        tabSixel.View.Add (pxY);

        sixelView.DrawContent += SixelViewOnDrawContent;


        btnSixel.Accept += (s, e) =>
                           {

                               if (_imageView.FullResImage == null)
                               {
                                   return;
                               }


                               _screenLocationForSixel = sixelView.FrameToScreen ().Location;
                               _encodedSixelData = GenerateSixelData(
                                                               _imageView.FullResImage,
                                                               sixelView.Frame.Size,
                                                               pxX.Value,
                                                               pxY.Value);

                               // TODO: Static way of doing this, suboptimal
                               Application.Sixel.Add (new SixelToRender
                               {
                                   SixelData = _encodedSixelData,
                                   ScreenPosition = _screenLocationForSixel
                               });
                           };
    }
    void SixelViewOnDrawContent (object sender, DrawEventArgs e)
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

    public string GenerateSixelData(
            Image<Rgba32> fullResImage,
            Size maxSize,
            int pixelsPerCellX,
            int pixelsPerCellY
        )
    {
        var encoder = new SixelEncoder ();

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

        btn.Accept += (s, e) => Application.RequestStop ();
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

        public override void OnDrawContent (Rectangle bounds)
        {
            base.OnDrawContent (bounds);

            if (FullResImage == null)
            {
                return;
            }

            // if we have not got a cached resized image of this size
            if (_matchSize == null || bounds.Width != _matchSize.Width || bounds.Height != _matchSize.Height)
            {
                // generate one
                _matchSize = FullResImage.Clone (x => x.Resize (bounds.Width, bounds.Height));
            }

            for (var y = 0; y < bounds.Height; y++)
            {
                for (var x = 0; x < bounds.Width; x++)
                {
                    Rgba32 rgb = _matchSize [x, y];

                    Attribute attr = _cache.GetOrAdd (
                                                      rgb,
                                                      rgb => new (
                                                                  new Color (),
                                                                  new Color (rgb.R, rgb.G, rgb.B)
                                                                 )
                                                     );

                    Driver.SetAttribute (attr);
                    AddRune (x, y, (Rune)' ');
                }
            }
        }

        internal void SetImage (Image<Rgba32> image)
        {
            FullResImage = image;
            SetNeedsDisplay ();
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
            int availableWidth = bounds.Width / 2; // Each color block is 2 character wide
            int availableHeight = bounds.Height;

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

        public override void OnDrawContent (Rectangle bounds)
        {
            base.OnDrawContent (bounds);

            if (_palette == null || _palette.Count == 0)
            {
                return;
            }

            // Calculate the grid size based on the bounds
            (int columns, int rows) = CalculateGridSize (bounds);

            // Draw the colors in the palette
            for (var i = 0; i < _palette.Count && i < columns * rows; i++)
            {
                int row = i / columns;
                int col = i % columns;

                // Calculate position in the grid
                int x = col * 2; // Each color block takes up 2 horizontal spaces
                int y = row;

                // Set the color attribute for the block
                Driver.SetAttribute (new (_palette [i], _palette [i]));

                // Draw the block (2 characters wide per block)
                for (var dx = 0; dx < 2; dx++) // Fill the width of the block
                {
                    AddRune (x + dx, y, (Rune)' ');
                }
            }
        }
    }
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
