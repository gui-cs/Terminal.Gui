using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.IO;
using Terminal.Gui;
using Color = Terminal.Gui.Color;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Images", Description: "Demonstration of how to render an image with/without true color support.")]
	[ScenarioCategory ("Colors")]
	[ScenarioCategory ("Drawing")]
	public class Images : Scenario {
		public override void Setup ()
		{
			base.Setup ();

			var canTrueColor = Application.Driver.SupportsTrueColor;

			var lblDriverName = new Label ($"Driver is {Application.Driver.GetType ().Name}") {
				X = 0,
				Y = 0
			};
			Win.Add (lblDriverName);

			var cbSupportsTrueColor = new CheckBox () {
				Text = "supports true color ",
				X = Pos.Right (lblDriverName) + 2,
				Y = 0,
				Checked = canTrueColor,
				CanFocus = false
			};
			Win.Add (cbSupportsTrueColor);

			var cbUseTrueColor = new CheckBox () {
				Text = "Use true color",
				X = Pos.Right (cbSupportsTrueColor) + 2,
				Y = 0,
				Checked = !Application.Force16Colors,
				Enabled = canTrueColor,
			};
			cbUseTrueColor.Toggled += (_, evt) => Application.Force16Colors = !evt.NewValue ?? false;
			Win.Add (cbUseTrueColor);

			var btnOpenImage = new Button () {
				Text = "Open Image",
				X = Pos.Right (cbUseTrueColor) + 2,
				Y = 0
			};
			Win.Add (btnOpenImage);

			var imageView = new ImageView () {
				X = 0,
				Y = Pos.Bottom (lblDriverName),
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			Win.Add (imageView);


			btnOpenImage.Clicked += (_, _) => {
				var ofd = new OpenDialog () {
					Text = "Open Image",
					AllowsMultipleSelection = false,
				};
				Application.Run (ofd);

				if (ofd.Path is not null) {
					Directory.SetCurrentDirectory (Path.GetFullPath (Path.GetDirectoryName (ofd.Path)!));
				}

				if (ofd.Canceled) {
					return;
				}

				var path = ofd.FilePaths [0];

				if (string.IsNullOrWhiteSpace (path)) {
					return;
				}

				if (!File.Exists (path)) {
					return;
				}

				Image<Rgba32> img;

				try {
					img = Image.Load<Rgba32> (File.ReadAllBytes (path));
				} catch (Exception ex) {

					MessageBox.ErrorQuery ("Could not open file", ex.Message, "Ok");
					return;
				}

				imageView.SetImage (img);
				Application.Refresh ();
			};
		}

		class ImageView : View {

			private Image<Rgba32> fullResImage;
			private Image<Rgba32> matchSize;

			ConcurrentDictionary<Rgba32, Attribute> cache = new ConcurrentDictionary<Rgba32, Attribute> ();

			internal void SetImage (Image<Rgba32> image)
			{
				fullResImage = image;
				this.SetNeedsDisplay ();
			}

			public override void OnDrawContent (Rect bounds)
			{
				base.OnDrawContent (bounds);

				if (fullResImage == null) {
					return;
				}

				// if we have not got a cached resized image of this size
				if (matchSize == null || bounds.Width != matchSize.Width || bounds.Height != matchSize.Height) {

					// generate one
					matchSize = fullResImage.Clone (x => x.Resize (bounds.Width, bounds.Height));
				}

				for (int y = 0; y < bounds.Height; y++) {
					for (int x = 0; x < bounds.Width; x++) {
						var rgb = matchSize [x, y];

						var attr = cache.GetOrAdd (rgb, (rgb) => new Attribute (new Color (), new Color (rgb.R, rgb.G, rgb.B)));

						Driver.SetAttribute (attr);
						AddRune (x, y, (System.Text.Rune)' ');
					}
				}
			}
		}
	}
}
