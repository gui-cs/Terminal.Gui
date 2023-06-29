using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Images", Description: "Demonstration of how to render an image with/without true color support.")]
	[ScenarioCategory ("Colors")]
	public class Images : Scenario
	{
		public override void Setup ()
		{
			base.Setup ();

			var x = 0;
			var y = 0;

			var canTrueColor = Application.Driver.SupportsTrueColorOutput;

			var lblDriverName = new Label ($"Current driver is {Application.Driver.GetType ().Name}") {
				X = x,
				Y = y++
			};
			Win.Add (lblDriverName);
			y++;

			var cbSupportsTrueColor = new CheckBox ("Driver supports true color ") {
				X = x,
				Y = y++,
				Checked = canTrueColor,
				CanFocus = false
			};
			Win.Add (cbSupportsTrueColor);

			var cbUseTrueColor = new CheckBox ("Use true color") {
				X = x,
				Y = y++,
				Checked = Application.Driver.UseTrueColor,
				Enabled = canTrueColor,
			};
			cbUseTrueColor.Toggled += (b, c) => Application.Driver.UseTrueColor = (bool) cbUseTrueColor.Checked;
			Win.Add (cbUseTrueColor);

			var btnOpenImage = new Button ("Open Image") {
				X = x,
				Y = y++
			};
			Win.Add (btnOpenImage);

			var imageView = new ImageView () {
				X = x,
				Y = y++,
				Width = Dim.Fill(),
				Height = Dim.Fill(),
			};
			Win.Add (imageView);

			btnOpenImage.Clicked += (_, _) => {
				string [] allowedExts = {
					"jpg",
					"png",
					"jpeg"
				};
				List<IAllowedType> allowedTypeList = new ();
				allowedTypeList.Add (new AllowedType ("Image", allowedExts));
				var ofd = new OpenDialog ("Open Image", allowedTypeList);
				Application.Run (ofd);

				var path = ofd.Path.ToString ();
				
				if (string.IsNullOrWhiteSpace (path)) {
					return;
				}

				if(!File.Exists(path)) {
					return;
				}

				Image<Rgba32> img;

				try {
					img = Image.Load<Rgba32> (File.ReadAllBytes (path));
				} catch (Exception ex) {

					MessageBox.ErrorQuery ("Could not open file", ex.Message, "Ok");
					return;
				}
				
				imageView.SetImage(img);
			};
			
		}

		class ImageView : View {

			private Image<Rgba32> fullResImage;
			private Image<Rgba32> matchSize;

			ConcurrentDictionary<Rgba32, Attribute> cache = new ConcurrentDictionary<Rgba32, Attribute>();

			internal void SetImage (Image<Rgba32> image)
			{
				fullResImage = image;
				Draw ();
				//Driver.UpdateScreen ();
				//this.SetNeedsDisplay ();
			}

			public override void OnDrawContent (Rect bounds)
			{
				//base.OnDrawContent (bounds);

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

						var attr = new Attribute (new TrueColor (0, 0, 0), new TrueColor (rgb.R, rgb.G, rgb.B));

						Driver.SetAttribute (attr);
						AddRune (x, y, (System.Text.Rune)' ');
					}
				}
				//SetNeedsDisplay (bounds);
				
			}
			public override void OnDrawContentComplete (Rect contentArea)
			{
				base.OnDrawContentComplete (contentArea);
				System.Diagnostics.Debug.WriteLine (contentArea.ToString ());
			}
		}
	}
}