using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Terminal.Gui;
using Rectangle = Terminal.Gui.Rectangle;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Animation", "Demonstration of how to render animated images with threading.")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Drawing")]
public class Animation : Scenario
{
    private bool _isDisposed;

    public override void Setup ()
    {
        base.Setup ();

        var imageView = new ImageView { Width = Dim.Fill (), Height = Dim.Fill () - 2 };

        Win.Add (imageView);

        var lbl = new Label { Y = Pos.AnchorEnd (2), Text = "Image by Wikiscient" };
        Win.Add (lbl);

        var lbl2 = new Label
        {
            Y = Pos.AnchorEnd (1), Text = "https://commons.wikimedia.org/wiki/File:Spinning_globe.gif"
        };
        Win.Add (lbl2);

        DirectoryInfo dir;

        string assemblyLocation = Assembly.GetExecutingAssembly ().Location;

        if (!string.IsNullOrEmpty (assemblyLocation))
        {
            dir = new DirectoryInfo (Path.GetDirectoryName (assemblyLocation));
        }
        else
        {
            dir = new DirectoryInfo (AppContext.BaseDirectory);
        }

        var f = new FileInfo (
                              Path.Combine (dir.FullName, "Scenarios", "Spinning_globe_dark_small.gif")
                             );

        if (!f.Exists)
        {
            MessageBox.ErrorQuery ("Could not find gif", "Could not find " + f.FullName, "Ok");

            return;
        }

        imageView.SetImage (Image.Load<Rgba32> (File.ReadAllBytes (f.FullName)));

        Task.Run (
                  () =>
                  {
                      while (!_isDisposed)
                      {
                          // When updating from a Thread/Task always use Invoke
                          Application.Invoke (
                                              () =>
                                              {
                                                  imageView.NextFrame ();
                                                  imageView.SetNeedsDisplay ();
                                              }
                                             );

                          Task.Delay (100).Wait ();
                      }
                  }
                 );
    }

    protected override void Dispose (bool disposing)
    {
        _isDisposed = true;
        base.Dispose (disposing);
    }

    // This is a C# port of https://github.com/andraaspar/bitmap-to-braille by Andraaspar

    /// <summary>Renders an image as unicode Braille.</summary>
    public class BitmapToBraille
    {
        public const int CHAR_HEIGHT = 4;
        public const int CHAR_WIDTH = 2;

        private const string CHARS =
            " ⠁⠂⠃⠄⠅⠆⠇⡀⡁⡂⡃⡄⡅⡆⡇⠈⠉⠊⠋⠌⠍⠎⠏⡈⡉⡊⡋⡌⡍⡎⡏⠐⠑⠒⠓⠔⠕⠖⠗⡐⡑⡒⡓⡔⡕⡖⡗⠘⠙⠚⠛⠜⠝⠞⠟⡘⡙⡚⡛⡜⡝⡞⡟⠠⠡⠢⠣⠤⠥⠦⠧⡠⡡⡢⡣⡤⡥⡦⡧⠨⠩⠪⠫⠬⠭⠮⠯⡨⡩⡪⡫⡬⡭⡮⡯⠰⠱⠲⠳⠴⠵⠶⠷⡰⡱⡲⡳⡴⡵⡶⡷⠸⠹⠺⠻⠼⠽⠾⠿⡸⡹⡺⡻⡼⡽⡾⡿⢀⢁⢂⢃⢄⢅⢆⢇⣀⣁⣂⣃⣄⣅⣆⣇⢈⢉⢊⢋⢌⢍⢎⢏⣈⣉⣊⣋⣌⣍⣎⣏⢐⢑⢒⢓⢔⢕⢖⢗⣐⣑⣒⣓⣔⣕⣖⣗⢘⢙⢚⢛⢜⢝⢞⢟⣘⣙⣚⣛⣜⣝⣞⣟⢠⢡⢢⢣⢤⢥⢦⢧⣠⣡⣢⣣⣤⣥⣦⣧⢨⢩⢪⢫⢬⢭⢮⢯⣨⣩⣪⣫⣬⣭⣮⣯⢰⢱⢲⢳⢴⢵⢶⢷⣰⣱⣲⣳⣴⣵⣶⣷⢸⢹⢺⢻⢼⢽⢾⢿⣸⣹⣺⣻⣼⣽⣾⣿";

        public BitmapToBraille (int widthPixels, int heightPixels, Func<int, int, bool> pixelIsLit)
        {
            WidthPixels = widthPixels;
            HeightPixels = heightPixels;
            PixelIsLit = pixelIsLit;
        }

        public int HeightPixels { get; }
        public Func<int, int, bool> PixelIsLit { get; }
        public int WidthPixels { get; }

        public string GenerateImage ()
        {
            var imageHeightChars = (int)Math.Ceiling ((double)HeightPixels / CHAR_HEIGHT);
            var imageWidthChars = (int)Math.Ceiling ((double)WidthPixels / CHAR_WIDTH);

            var result = new StringBuilder ();

            for (var y = 0; y < imageHeightChars; y++)
            {
                for (var x = 0; x < imageWidthChars; x++)
                {
                    int baseX = x * CHAR_WIDTH;
                    int baseY = y * CHAR_HEIGHT;

                    var charIndex = 0;
                    var value = 1;

                    for (var charX = 0; charX < CHAR_WIDTH; charX++)
                    {
                        for (var charY = 0; charY < CHAR_HEIGHT; charY++)
                        {
                            int bitmapX = baseX + charX;
                            int bitmapY = baseY + charY;

                            bool pixelExists = bitmapX < WidthPixels && bitmapY < HeightPixels;

                            if (pixelExists && PixelIsLit (bitmapX, bitmapY))
                            {
                                charIndex += value;
                            }

                            value *= 2;
                        }
                    }

                    result.Append (CHARS [charIndex]);
                }

                result.Append ('\n');
            }

            return result.ToString ().TrimEnd ();
        }
    }

    private class ImageView : View
    {
        private string [] brailleCache;
        private int currentFrame;
        private int frameCount;
        private Image<Rgba32> [] fullResImages;
        private Image<Rgba32> [] matchSizes;
        private Rectangle oldSize = Rectangle.Empty;
        public void NextFrame () { currentFrame = (currentFrame + 1) % frameCount; }

        public override void OnDrawContent (Rectangle contentArea)
        {
            base.OnDrawContent (contentArea);

            if (oldSize != Bounds)
            {
                // Invalidate cached images now size has changed
                matchSizes = new Image<Rgba32> [frameCount];
                brailleCache = new string [frameCount];
                oldSize = Bounds;
            }

            Image<Rgba32> imgScaled = matchSizes [currentFrame];
            string braille = brailleCache [currentFrame];

            if (imgScaled == null)
            {
                Image<Rgba32> imgFull = fullResImages [currentFrame];

                // keep aspect ratio
                int newSize = Math.Min (Bounds.Width, Bounds.Height);

                // generate one
                matchSizes [currentFrame] = imgScaled = imgFull.Clone (
                                                                       x => x.Resize (
                                                                                      newSize * BitmapToBraille.CHAR_HEIGHT,
                                                                                      newSize * BitmapToBraille.CHAR_HEIGHT
                                                                                     )
                                                                      );
            }

            if (braille == null)
            {
                brailleCache [currentFrame] = braille = GetBraille (matchSizes [currentFrame]);
            }

            string [] lines = braille.Split ('\n');

            for (var y = 0; y < lines.Length; y++)
            {
                string line = lines [y];

                for (var x = 0; x < line.Length; x++)
                {
                    AddRune (x, y, (Rune)line [x]);
                }
            }
        }

        internal void SetImage (Image<Rgba32> image)
        {
            frameCount = image.Frames.Count;

            fullResImages = new Image<Rgba32> [frameCount];
            matchSizes = new Image<Rgba32> [frameCount];
            brailleCache = new string [frameCount];

            for (var i = 0; i < frameCount - 1; i++)
            {
                fullResImages [i] = image.Frames.ExportFrame (0);
            }

            fullResImages [frameCount - 1] = image;

            SetNeedsDisplay ();
        }

        private string GetBraille (Image<Rgba32> img)
        {
            var braille = new BitmapToBraille (
                                               img.Width,
                                               img.Height,
                                               (x, y) => IsLit (img, x, y)
                                              );

            return braille.GenerateImage ();
        }

        private bool IsLit (Image<Rgba32> img, int x, int y)
        {
            Rgba32 rgb = img [x, y];

            return rgb.R + rgb.G + rgb.B > 50;
        }
    }
}
