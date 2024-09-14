using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Terminal.Gui;
using Terminal.Gui.Drawing.Quant;
using Color = Terminal.Gui.Color;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Images", "Demonstration of how to render an image with/without true color support.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
public class Images : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var win = new Window { Title = $"{Application.QuitKey} to Quit - Scenario: {GetName()}" };

        bool canTrueColor = Application.Driver?.SupportsTrueColor ?? false;

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
        
        var imageView = new ImageView
        {
            X = 0, Y = Pos.Bottom (lblDriverName), Width = Dim.Fill (), Height = Dim.Fill ()
        };
        win.Add (imageView);

        btnOpenImage.Accept += (_, _) =>
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

                                   imageView.SetImage (img);
                                   Application.Refresh ();
                               };



        var btnSixel = new Button () { X = Pos.Right (btnOpenImage) + 2, Y = 0, Text = "Output Sixel" };
        btnSixel.Accept += (s, e) => { imageView.OutputSixel ();};
        win.Add (btnSixel);

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();
    }


    private class ImageView : View
    {
        private readonly ConcurrentDictionary<Rgba32, Attribute> _cache = new ();
        private Image<Rgba32> _fullResImage;
        private Image<Rgba32> _matchSize;

        public override void OnDrawContent (Rectangle bounds)
        {
            base.OnDrawContent (bounds);

            if (_fullResImage == null)
            {
                return;
            }

            // if we have not got a cached resized image of this size
            if (_matchSize == null || bounds.Width != _matchSize.Width || bounds.Height != _matchSize.Height)
            {
                // generate one
                _matchSize = _fullResImage.Clone (x => x.Resize (bounds.Width, bounds.Height));
            }

            for (var y = 0; y < bounds.Height; y++)
            {
                for (var x = 0; x < bounds.Width; x++)
                {
                    Rgba32 rgb = _matchSize [x, y];

                    Attribute attr = _cache.GetOrAdd (
                                                      rgb,
                                                      rgb => new Attribute (
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
            _fullResImage = image;
            SetNeedsDisplay ();
        }

        public void OutputSixel ()
        {
            if (_fullResImage == null)
            {
                return;
            }

            var encoder = new SixelEncoder ();

            var encoded = encoder.EncodeSixel (ConvertToColorArray (_fullResImage));

            var pv = new PaletteView (encoder.Quantizer.Palette.ToList ());

            var dlg = new Dialog ()
            {
                Title = "Palette (Esc to close)",
                Width = Dim.Fill (2),
                Height = Dim.Fill (1),
            };

            var btn = new Button ()
            {
                Text = "Ok"
            };

            btn.Accept += (s, e) => Application.RequestStop ();
            dlg.Add (pv);
            dlg.AddButton (btn);
            Application.Run (dlg);

            Application.Sixel = encoded;
        }
        public static Color [,] ConvertToColorArray (Image<Rgba32> image)
        {
            int width = image.Width;
            int height = image.Height;
            Color [,] colors = new Color [width, height];

            // Loop through each pixel and convert Rgba32 to System.Drawing.Color
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pixel = image [x, y];
                    colors [x, y] = new Color (pixel.A, pixel.R, pixel.G, pixel.B); // Convert Rgba32 to System.Drawing.Color
                }
            }

            return colors;
        }
    }
    public class PaletteView : View
    {
        private List<Color> _palette;

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
            int availableWidth = bounds.Width / 2;  // Each color block is 2 character wide
            int availableHeight = bounds.Height;

            int numColors = _palette.Count;

            // Calculate the number of columns and rows we can fit within the bounds
            int columns = Math.Min (availableWidth, numColors);
            int rows = (numColors + columns - 1) / columns;  // Ceiling division for rows

            // Ensure we do not exceed the available height
            if (rows > availableHeight)
            {
                rows = availableHeight;
                columns = (numColors + rows - 1) / rows;  // Recalculate columns if needed
            }

            return (columns, rows);
        }

        public override void OnDrawContent (Rectangle bounds)
        {
            base.OnDrawContent (bounds);

            if (_palette == null || _palette.Count == 0)
                return;

            // Calculate the grid size based on the bounds
            var (columns, rows) = CalculateGridSize (bounds);

            // Draw the colors in the palette
            for (int i = 0; i < _palette.Count && i < columns * rows; i++)
            {
                int row = i / columns;
                int col = i % columns;

                // Calculate position in the grid
                int x = col * 2; // Each color block takes up 2 horizontal spaces
                int y = row;

                // Set the color attribute for the block
                Driver.SetAttribute (new Terminal.Gui.Attribute (_palette [i], _palette [i]));

                // Draw the block (2 characters wide per block)
                for (int dx = 0; dx < 2; dx++) // Fill the width of the block
                {
                    AddRune (x + dx, y, (Rune)' ');
                }
            }
        }

        // Allows dynamically changing the palette
        public void SetPalette (List<Color> palette)
        {
            _palette = palette ?? new List<Color> ();
            SetNeedsDisplay ();
        }
    }
}
