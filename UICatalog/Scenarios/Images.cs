using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
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
            State = canTrueColor ? CheckState.Checked : CheckState.UnChecked,
            CanFocus = false,
            Text = "supports true color "
        };
        win.Add (cbSupportsTrueColor);

        var cbUseTrueColor = new CheckBox
        {
            X = Pos.Right (cbSupportsTrueColor) + 2,
            Y = 0,
            State = !Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
            Enabled = canTrueColor,
            Text = "Use true color"
        };
        cbUseTrueColor.Toggle += (_, evt) => Application.Force16Colors = evt.NewValue == CheckState.UnChecked;
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
    }
}
