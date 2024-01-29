using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Animation", "Demonstration of how to render animated images with threading.")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Drawing")]
public class Animation : Scenario {
	bool _isDisposed;

	public override void Setup ()
	{
		base.Setup ();

		var imageView = new ImageView {
			Width = Dim.Fill (),
			Height = Dim.Fill () - 2
		};

		Win.Add (imageView);

		var lbl = new Label { Y = Pos.AnchorEnd (2), Text = "Image by Wikiscient" };
		Win.Add (lbl);

		var lbl2 = new Label {
			Y = Pos.AnchorEnd (1),
			Text = "https://commons.wikimedia.org/wiki/File:Spinning_globe.gif"
		};
		Win.Add (lbl2);

		DirectoryInfo dir;

		var assemblyLocation = Assembly.GetExecutingAssembly ().Location;

		if (!string.IsNullOrEmpty (assemblyLocation)) {
			dir = new DirectoryInfo (Path.GetDirectoryName (assemblyLocation));
		} else {
			dir = new DirectoryInfo (AppContext.BaseDirectory);
		}

		var f = new FileInfo (
			Path.Combine (dir.FullName, "Scenarios", "Spinning_globe_dark_small.gif"));
		if (!f.Exists) {
			MessageBox.ErrorQuery ("Could not find gif", "Could not find " + f.FullName, "Ok");
			return;
		}

		imageView.SetImage (Image.Load<Rgba32> (File.ReadAllBytes (f.FullName)));

		Task.Run (() => {
			while (!_isDisposed) {
				// When updating from a Thread/Task always use Invoke
				Application.Invoke (() => {
					imageView.NextFrame ();
					imageView.SetNeedsDisplay ();
				});

				Task.Delay (100).Wait ();
			}
		});
	}

	protected override void Dispose (bool disposing)
	{
		_isDisposed = true;
		base.Dispose (disposing);
	}

	// This is a C# port of https://github.com/andraaspar/bitmap-to-braille by Andraaspar

	/// <summary>
	///         Renders an image as unicode Braille.
	/// </summary>
	public class BitmapToBraille {
		public const int CHAR_WIDTH = 2;
		public const int CHAR_HEIGHT = 4;

		const string CHARS =
			" ⠁⠂⠃⠄⠅⠆⠇⡀⡁⡂⡃⡄⡅⡆⡇⠈⠉⠊⠋⠌⠍⠎⠏⡈⡉⡊⡋⡌⡍⡎⡏⠐⠑⠒⠓⠔⠕⠖⠗⡐⡑⡒⡓⡔⡕⡖⡗⠘⠙⠚⠛⠜⠝⠞⠟⡘⡙⡚⡛⡜⡝⡞⡟⠠⠡⠢⠣⠤⠥⠦⠧⡠⡡⡢⡣⡤⡥⡦⡧⠨⠩⠪⠫⠬⠭⠮⠯⡨⡩⡪⡫⡬⡭⡮⡯⠰⠱⠲⠳⠴⠵⠶⠷⡰⡱⡲⡳⡴⡵⡶⡷⠸⠹⠺⠻⠼⠽⠾⠿⡸⡹⡺⡻⡼⡽⡾⡿⢀⢁⢂⢃⢄⢅⢆⢇⣀⣁⣂⣃⣄⣅⣆⣇⢈⢉⢊⢋⢌⢍⢎⢏⣈⣉⣊⣋⣌⣍⣎⣏⢐⢑⢒⢓⢔⢕⢖⢗⣐⣑⣒⣓⣔⣕⣖⣗⢘⢙⢚⢛⢜⢝⢞⢟⣘⣙⣚⣛⣜⣝⣞⣟⢠⢡⢢⢣⢤⢥⢦⢧⣠⣡⣢⣣⣤⣥⣦⣧⢨⢩⢪⢫⢬⢭⢮⢯⣨⣩⣪⣫⣬⣭⣮⣯⢰⢱⢲⢳⢴⢵⢶⢷⣰⣱⣲⣳⣴⣵⣶⣷⢸⢹⢺⢻⢼⢽⢾⢿⣸⣹⣺⣻⣼⣽⣾⣿";

		public BitmapToBraille (int widthPixels, int heightPixels, Func<int, int, bool> pixelIsLit)
		{
			WidthPixels = widthPixels;
			HeightPixels = heightPixels;
			PixelIsLit = pixelIsLit;
		}

		public int WidthPixels { get; }
		public int HeightPixels { get; }

		public Func<int, int, bool> PixelIsLit { get; }

		public string GenerateImage ()
		{
			var imageHeightChars = (int)Math.Ceiling ((double)HeightPixels / CHAR_HEIGHT);
			var imageWidthChars = (int)Math.Ceiling ((double)WidthPixels / CHAR_WIDTH);

			var result = new StringBuilder ();

			for (var y = 0; y < imageHeightChars; y++) {
				for (var x = 0; x < imageWidthChars; x++) {
					var baseX = x * CHAR_WIDTH;
					var baseY = y * CHAR_HEIGHT;

					var charIndex = 0;
					var value = 1;

					for (var charX = 0; charX < CHAR_WIDTH; charX++) {
						for (var charY = 0; charY < CHAR_HEIGHT; charY++) {
							var bitmapX = baseX + charX;
							var bitmapY = baseY + charY;
							var pixelExists = bitmapX < WidthPixels &&
									  bitmapY < HeightPixels;

							if (pixelExists && PixelIsLit (bitmapX, bitmapY)) {
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

	class ImageView : View {
		string [] brailleCache;
		int currentFrame;
		int frameCount;

		Image<Rgba32> [] fullResImages;
		Image<Rgba32> [] matchSizes;

		Rect oldSize = Rect.Empty;

		internal void SetImage (Image<Rgba32> image)
		{
			frameCount = image.Frames.Count;

			fullResImages = new Image<Rgba32> [frameCount];
			matchSizes = new Image<Rgba32> [frameCount];
			brailleCache = new string [frameCount];

			for (var i = 0; i < frameCount - 1; i++) {
				fullResImages [i] = image.Frames.ExportFrame (0);
			}

			fullResImages [frameCount - 1] = image;

			SetNeedsDisplay ();
		}

		public void NextFrame () => currentFrame = (currentFrame + 1) % frameCount;

		public override void OnDrawContent (Rect contentArea)
		{
			base.OnDrawContent (contentArea);

			if (oldSize != Bounds) {
				// Invalidate cached images now size has changed
				matchSizes = new Image<Rgba32> [frameCount];
				brailleCache = new string [frameCount];
				oldSize = Bounds;
			}

			var imgScaled = matchSizes [currentFrame];
			var braille = brailleCache [currentFrame];

			if (imgScaled == null) {
				var imgFull = fullResImages [currentFrame];

				// keep aspect ratio
				var newSize = Math.Min (Bounds.Width, Bounds.Height);

				// generate one
				matchSizes [currentFrame] = imgScaled = imgFull.Clone (
					x => x.Resize (
						newSize * BitmapToBraille.CHAR_HEIGHT,
						newSize * BitmapToBraille.CHAR_HEIGHT));
			}

			if (braille == null) {
				brailleCache [currentFrame] = braille = GetBraille (matchSizes [currentFrame]);
			}

			var lines = braille.Split ('\n');

			for (var y = 0; y < lines.Length; y++) {
				var line = lines [y];
				for (var x = 0; x < line.Length; x++) {
					AddRune (x, y, (Rune)line [x]);
				}
			}
		}

		string GetBraille (Image<Rgba32> img)
		{
			var braille = new BitmapToBraille (
				img.Width,
				img.Height,
				(x, y) => IsLit (img, x, y));

			return braille.GenerateImage ();
		}

		bool IsLit (Image<Rgba32> img, int x, int y)
		{
			var rgb = img [x, y];
			return rgb.R + rgb.G + rgb.B > 50;
		}
	}
}