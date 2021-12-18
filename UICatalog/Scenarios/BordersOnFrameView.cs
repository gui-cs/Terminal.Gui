using System;
using System.Globalization;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Borders on FrameView", Description: "Demonstrate FrameView borders manipulation.")]
	[ScenarioCategory ("Border")]
	public class BordersOnFrameView : Scenario {
		public override void Setup ()
		{
			var borderStyle = BorderStyle.Double;
			var drawMarginFrame = false;
			var borderThickness = new Thickness (1, 2, 3, 4);
			var borderBrush = Colors.Base.HotFocus.Foreground;
			var padding = new Thickness (1, 2, 3, 4);
			var background = Colors.Base.HotNormal.Foreground;
			var effect3D = true;

			var smartView = new FrameView () {
				X = Pos.Center (),
				Y = Pos.Center () - 7,
				Width = 40,
				Height = 20,
				Border = new Border () {
					BorderStyle = borderStyle,
					DrawMarginFrame = drawMarginFrame,
					BorderThickness = borderThickness,
					BorderBrush = borderBrush,
					Padding = padding,
					Background = background,
					Effect3D = effect3D
				},
				ColorScheme = Colors.TopLevel
			};

			var tf1 = new TextField ("1234567890") { Width = 10 };

			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += () => MessageBox.Query (20, 7, "Hi", "I'm a FrameView?", "Yes", "No");
			var label = new Label ("I'm a FrameView") {
				X = Pos.Center (),
				Y = Pos.Center () - 3,
			};
			var tf2 = new TextField ("1234567890") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			var tv = new TextView () {
				Y = Pos.AnchorEnd (2),
				Width = 10,
				Height = Dim.Fill (),
				ColorScheme = Colors.Dialog,
				Text = "1234567890"
			};
			smartView.Add (tf1, button, label, tf2, tv);

			Win.Add (new Label ("Padding:") {
				X = Pos.Center () - 23,
			});

			var paddingTopEdit = new TextField ("") {
				X = Pos.Center () - 22,
				Y = 1,
				Width = 5
			};
			paddingTopEdit.TextChanging += (e) => {
				try {
					smartView.Border.Padding = new Thickness (smartView.Border.Padding.Left,
						int.Parse (e.NewText.ToString ()), smartView.Border.Padding.Right,
						smartView.Border.Padding.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingTopEdit.Text = $"{smartView.Border.Padding.Top}";

			Win.Add (paddingTopEdit);

			var paddingLeftEdit = new TextField ("") {
				X = Pos.Center () - 30,
				Y = 2,
				Width = 5
			};
			paddingLeftEdit.TextChanging += (e) => {
				try {
					smartView.Border.Padding = new Thickness (int.Parse (e.NewText.ToString ()),
						smartView.Border.Padding.Top, smartView.Border.Padding.Right,
						smartView.Border.Padding.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingLeftEdit.Text = $"{smartView.Border.Padding.Left}";
			Win.Add (paddingLeftEdit);

			var paddingRightEdit = new TextField ("") {
				X = Pos.Center () - 15,
				Y = 2,
				Width = 5
			};
			paddingRightEdit.TextChanging += (e) => {
				try {
					smartView.Border.Padding = new Thickness (smartView.Border.Padding.Left,
						smartView.Border.Padding.Top, int.Parse (e.NewText.ToString ()),
						smartView.Border.Padding.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingRightEdit.Text = $"{smartView.Border.Padding.Right}";
			Win.Add (paddingRightEdit);

			var paddingBottomEdit = new TextField ("") {
				X = Pos.Center () - 22,
				Y = 3,
				Width = 5
			};
			paddingBottomEdit.TextChanging += (e) => {
				try {
					smartView.Border.Padding = new Thickness (smartView.Border.Padding.Left,
						smartView.Border.Padding.Top, smartView.Border.Padding.Right,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingBottomEdit.Text = $"{smartView.Border.Padding.Bottom}";
			Win.Add (paddingBottomEdit);

			var replacePadding = new Button ("Replace all based on top") {
				X = Pos.Center () - 35,
				Y = 5
			};
			replacePadding.Clicked += () => {
				smartView.Border.Padding = new Thickness (smartView.Border.Padding.Top);
				if (paddingTopEdit.Text.IsEmpty) {
					paddingTopEdit.Text = "0";
				}
				paddingBottomEdit.Text = paddingLeftEdit.Text = paddingRightEdit.Text = paddingTopEdit.Text;
			};
			Win.Add (replacePadding);

			Win.Add (new Label ("Border:") {
				X = Pos.Center () + 11,
			});

			var borderTopEdit = new TextField ("") {
				X = Pos.Center () + 12,
				Y = 1,
				Width = 5
			};
			borderTopEdit.TextChanging += (e) => {
				try {
					smartView.Border.BorderThickness = new Thickness (smartView.Border.BorderThickness.Left,
						int.Parse (e.NewText.ToString ()), smartView.Border.BorderThickness.Right,
						smartView.Border.BorderThickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderTopEdit.Text = $"{smartView.Border.BorderThickness.Top}";

			Win.Add (borderTopEdit);

			var borderLeftEdit = new TextField ("") {
				X = Pos.Center () + 5,
				Y = 2,
				Width = 5
			};
			borderLeftEdit.TextChanging += (e) => {
				try {
					smartView.Border.BorderThickness = new Thickness (int.Parse (e.NewText.ToString ()),
						smartView.Border.BorderThickness.Top, smartView.Border.BorderThickness.Right,
						smartView.Border.BorderThickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderLeftEdit.Text = $"{smartView.Border.BorderThickness.Left}";
			Win.Add (borderLeftEdit);

			var borderRightEdit = new TextField ("") {
				X = Pos.Center () + 19,
				Y = 2,
				Width = 5
			};
			borderRightEdit.TextChanging += (e) => {
				try {
					smartView.Border.BorderThickness = new Thickness (smartView.Border.BorderThickness.Left,
						smartView.Border.BorderThickness.Top, int.Parse (e.NewText.ToString ()),
						smartView.Border.BorderThickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderRightEdit.Text = $"{smartView.Border.BorderThickness.Right}";
			Win.Add (borderRightEdit);

			var borderBottomEdit = new TextField ("") {
				X = Pos.Center () + 12,
				Y = 3,
				Width = 5
			};
			borderBottomEdit.TextChanging += (e) => {
				try {
					smartView.Border.BorderThickness = new Thickness (smartView.Border.BorderThickness.Left,
						smartView.Border.BorderThickness.Top, smartView.Border.BorderThickness.Right,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderBottomEdit.Text = $"{smartView.Border.BorderThickness.Bottom}";
			Win.Add (borderBottomEdit);

			var replaceBorder = new Button ("Replace all based on top") {
				X = Pos.Center () + 1,
				Y = 5
			};
			replaceBorder.Clicked += () => {
				smartView.Border.BorderThickness = new Thickness (smartView.Border.BorderThickness.Top);
				if (borderTopEdit.Text.IsEmpty) {
					borderTopEdit.Text = "0";
				}
				borderBottomEdit.Text = borderLeftEdit.Text = borderRightEdit.Text = borderTopEdit.Text;
			};
			Win.Add (replaceBorder);

			Win.Add (new Label ("BorderStyle:"));

			var borderStyleEnum = Enum.GetValues (typeof (BorderStyle)).Cast<BorderStyle> ().ToList ();
			var rbBorderStyle = new RadioGroup (borderStyleEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = 2,
				Y = 1,
				SelectedItem = (int)smartView.Border.BorderStyle
			};
			Win.Add (rbBorderStyle);

			var cbDrawMarginFrame = new CheckBox ("Draw Margin Frame", smartView.Border.DrawMarginFrame) {
				X = Pos.AnchorEnd (20),
				Y = 0,
				Width = 5
			};
			cbDrawMarginFrame.Toggled += (e) => {
				try {
					smartView.Border.DrawMarginFrame = cbDrawMarginFrame.Checked;
					if (cbDrawMarginFrame.Checked != smartView.Border.DrawMarginFrame) {
						cbDrawMarginFrame.Checked = smartView.Border.DrawMarginFrame;
					}
				} catch { }
			};
			Win.Add (cbDrawMarginFrame);

			rbBorderStyle.SelectedItemChanged += (e) => {
				smartView.Border.BorderStyle = (BorderStyle)e.SelectedItem;
				smartView.SetNeedsDisplay ();
				if (cbDrawMarginFrame.Checked != smartView.Border.DrawMarginFrame) {
					cbDrawMarginFrame.Checked = smartView.Border.DrawMarginFrame;
				}
			};

			var cbEffect3D = new CheckBox ("Draw 3D effects", smartView.Border.Effect3D) {
				X = Pos.AnchorEnd (20),
				Y = 1,
				Width = 5
			};
			Win.Add (cbEffect3D);

			Win.Add (new Label ("Effect3D Offset:") {
				X = Pos.AnchorEnd (20),
				Y = 2
			});
			Win.Add (new Label ("X:") {
				X = Pos.AnchorEnd (19),
				Y = 3
			});

			var effect3DOffsetX = new TextField ("") {
				X = Pos.AnchorEnd (16),
				Y = 3,
				Width = 5
			};
			effect3DOffsetX.TextChanging += (e) => {
				try {
					smartView.Border.Effect3DOffset = new Point (int.Parse (e.NewText.ToString ()),
						smartView.Border.Effect3DOffset.Y);
				} catch {
					if (!e.NewText.IsEmpty && e.NewText != CultureInfo.CurrentCulture.NumberFormat.NegativeSign) {
						e.Cancel = true;
					}
				}
			};
			effect3DOffsetX.Text = $"{smartView.Border.Effect3DOffset.X}";
			Win.Add (effect3DOffsetX);

			Win.Add (new Label ("Y:") {
				X = Pos.AnchorEnd (10),
				Y = 3
			});

			var effect3DOffsetY = new TextField ("") {
				X = Pos.AnchorEnd (7),
				Y = 3,
				Width = 5
			};
			effect3DOffsetY.TextChanging += (e) => {
				try {
					smartView.Border.Effect3DOffset = new Point (smartView.Border.Effect3DOffset.X,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty && e.NewText != CultureInfo.CurrentCulture.NumberFormat.NegativeSign) {
						e.Cancel = true;
					}
				}
			};
			effect3DOffsetY.Text = $"{smartView.Border.Effect3DOffset.Y}";
			Win.Add (effect3DOffsetY);

			cbEffect3D.Toggled += (e) => {
				try {
					smartView.Border.Effect3D = effect3DOffsetX.Enabled =
						effect3DOffsetY.Enabled = cbEffect3D.Checked;
				} catch { }
			};

			Win.Add (new Label ("Background:") {
				Y = 5
			});

			var colorEnum = Enum.GetValues (typeof (Color)).Cast<Color> ().ToList ();
			var rbBackground = new RadioGroup (colorEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = 2,
				Y = 6,
				SelectedItem = (int)smartView.Border.Background
			};
			rbBackground.SelectedItemChanged += (e) => {
				smartView.Border.Background = (Color)e.SelectedItem;
			};
			Win.Add (rbBackground);

			Win.Add (new Label ("BorderBrush:") {
				X = Pos.AnchorEnd (20),
				Y = 5
			});

			var rbBorderBrush = new RadioGroup (colorEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = Pos.AnchorEnd (18),
				Y = 6,
				SelectedItem = (int)smartView.Border.BorderBrush
			};
			rbBorderBrush.SelectedItemChanged += (e) => {
				smartView.Border.BorderBrush = (Color)e.SelectedItem;
			};
			Win.Add (rbBorderBrush);

			Win.Add (smartView);
		}
	}
}