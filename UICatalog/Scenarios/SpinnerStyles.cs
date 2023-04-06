using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "SpinnerView Styles", Description: "Shows the SpinnerView Styles.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Progress")]

	public class SpinnerViewStyles : Scenario {

		public override void Setup ()
		{
			const int DEFAULT_DELAY = 130;
			const string DEFAULT_CUSTOM = @"-\|/";
			var styleEnum = Enum.GetValues (typeof (SpinnerStyle)).Cast<SpinnerStyle> ().ToList ();

			var preview = new View () {
				X = Pos.Center (),
				Y = 0,
				Width = 22,
				Height = 3,
				//Title = "Preview",
				Border = new Border () {
					BorderStyle = BorderStyle.Single
				}
			};
			Win.Add (preview);

			var spinner = new SpinnerView () {
				X = Pos.Center (),
				Y = 0
			};
			preview.Add (spinner);
			spinner.AutoSpin ();

			var ckbAscii = new CheckBox ("Ascii Only", false) {
				X = Pos.Center () - 7,
				Y = Pos.Bottom (preview),
				Enabled = false,
				Checked = true
			};
			Win.Add (ckbAscii);

			var ckbNoSpecial = new CheckBox ("No Special", false) {
				X = Pos.Center () + 7,
				Y = Pos.Bottom (preview),
				Enabled = false,
				Checked = true
			};
			Win.Add (ckbNoSpecial);

			var ckbReverse = new CheckBox ("Reverse", false) {
				X = Pos.Center () - 22,
				Y = Pos.Bottom (preview) + 1,
				Checked = false
			};
			Win.Add (ckbReverse);

			var ckbBounce = new CheckBox ("Bounce", false) {
				X = Pos.Right (ckbReverse) + 2,
				Y = Pos.Bottom (preview) + 1,
				Checked = false
			};
			Win.Add (ckbBounce);

			var delayLabel = new Label ("Delay:") {
				X = Pos.Right (ckbBounce) + 2,
				Y = Pos.Bottom (preview) + 1
			};
			Win.Add (delayLabel);
			var delayField = new TextField (DEFAULT_DELAY.ToString ()) {
				X = Pos.Right (delayLabel),
				Y = Pos.Bottom (preview) + 1,
				Width = 5
			};
			Win.Add (delayField);
			delayField.TextChanged += (s, e) => {
				if (ushort.TryParse (delayField.Text.ToString (), out var i))
					spinner.SpinDelayInMilliseconds = i;
			};

			var customLabel = new Label ("Custom:") {
				X = Pos.Right (delayField) + 2,
				Y = Pos.Bottom (preview) + 1
			};
			Win.Add (customLabel);
			var customField = new TextField (DEFAULT_CUSTOM) {
				X = Pos.Right (customLabel),
				Y = Pos.Bottom (preview) + 1,
				Width = 12
			};
			Win.Add (customField);

			var styleArray = styleEnum.Select (e => NStack.ustring.Make (e.ToString ())).ToArray ();
			if (styleArray.Length < 2)
				return;

			var styles = new ListView () {
				X = Pos.Center (),
				Y = Pos.Bottom (preview) + 2,
				Height = Dim.Fill (),
				Width = Dim.Fill (1)
			};
			styles.SetSource (styleArray);
			styles.SelectedItem = (int)SpinnerStyle.Custom;
			Win.Add (styles);
			customField.TextChanged += (s, e) => {
				if (customField.Text.Length > 0) {
					if (styles.SelectedItem != (int)SpinnerStyle.Custom)
						styles.SelectedItem = (int)SpinnerStyle.Custom;
					SetCustom ();
				}
			};

			styles.SelectedItemChanged += (s, e) => {
				if ((SpinnerStyle)e.Item == SpinnerStyle.Custom) {
					//customField.Text = DEFAULT_CUSTOM;
					delayField.Text = DEFAULT_DELAY.ToString ();
					SetCustom ();
				} else {
					spinner.Visible = true;
					spinner.SpinnerStyle = (SpinnerStyle)e.Item;
					ckbReverse.Checked = false;
					ckbBounce.Checked = spinner.SpinBounce;
					ckbAscii.Checked = spinner.IsAsciiOnly;
					ckbNoSpecial.Checked = !spinner.HasSpecialCharacters;
					delayField.Text = spinner.SpinDelayInMilliseconds.ToString ();
					customField.Text = "";
				}
			};

			ckbReverse.Toggled += (s, e) => {
				spinner.SpinReverse = (bool)!e.OldValue;
			};

			ckbBounce.Toggled += (s, e) => {
				spinner.SpinBounce = (bool)!e.OldValue;
			};

			Application.Top.Unloaded += Top_Unloaded;

			void SetCustom ()
			{
				if (customField.Text.Length > 0) {
					spinner.Visible = true;
					if (ushort.TryParse (delayField.Text.ToString (), out var d))
						spinner.SpinDelayInMilliseconds = d;
					else {
						spinner.SpinDelayInMilliseconds = DEFAULT_DELAY;
						delayField.Text = DEFAULT_DELAY.ToString ();
					}
					var str = new List<string> ();
					foreach (var c in customField.Text.ToString ().ToCharArray ()) {
						str.Add (c.ToString ());
					}
					spinner.Frames = str.ToArray ();
				} else {
					spinner.Visible = false;
				}
			}

			void Top_Unloaded (object sender, EventArgs args)
			{
				if (spinner != null) {
					spinner.Dispose ();
					spinner = null;
				}
				Application.Top.Unloaded -= Top_Unloaded;
			}
		}
	}
}