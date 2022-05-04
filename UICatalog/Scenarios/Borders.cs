using System;
using System.Globalization;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Borders with/without PanelView", Description: "Demonstrate with/without PanelView borders manipulation.")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Borders")]
	public class Borders : Scenario {
		public override void Setup ()
		{
			var borderStyle = BorderStyle.Single;
			var drawMarginFrame = true;
			var borderThickness = new Thickness (2);
			var borderBrush = Color.Red;
			var padding = new Thickness (2);
			var background = Color.BrightGreen;
			var effect3D = true;

			var smartPanel = new PanelView () {
				X = Pos.Center () - 38,
				Y = Pos.Center () - 3,
				Width = 24,
				Height = 13,
				Border = new Border () {
					BorderStyle = borderStyle,
					DrawMarginFrame = drawMarginFrame,
					BorderThickness = borderThickness,
					BorderBrush = borderBrush,
					Padding = padding,
					Background = background,
					Effect3D = effect3D
				},
			};
			smartPanel.Add (new Label () { // Or smartPanel.Child = 
				X = 0,
				Y = 0,
				//Width = 24, commenting because now setting the size disable auto-size
				//Height = 13,
				ColorScheme = Colors.TopLevel,
				Text = "This is a test\nwith a \nPanelView",
				TextAlignment = TextAlignment.Centered
			});

			// Can be initialized this way too.

			//var smartPanel = new PanelView (new Label () {
			//	X = Pos.Center () - 38,
			//	Y = Pos.Center () - 3,
			//	Width = 24,
			//	Height = 13,
			//	Border = new Border () {
			//		BorderStyle = borderStyle,
			//		DrawMarginFrame = drawMarginFrame,
			//		BorderThickness = borderThickness,
			//		BorderBrush = borderBrush,
			//		Padding = padding,
			//		Background = background,
			//		Effect3D = effect3D
			//	},
			//	ColorScheme = Colors.TopLevel,
			//	Text = "This is a test\nwith a \nPanelView",
			//	TextAlignment = TextAlignment.Centered
			//}) {
			//	X = Pos.Center () - 38,
			//	Y = Pos.Center () - 3,
			//	Width = 24,
			//	Height = 13
			//};

			var smartView = new Label () {
				X = Pos.Center () + 10,
				Y = Pos.Center () + 2,
				Border = new Border () {
					BorderStyle = borderStyle,
					DrawMarginFrame = drawMarginFrame,
					BorderThickness = borderThickness,
					BorderBrush = borderBrush,
					Padding = padding,
					Background = background,
					Effect3D = effect3D
				},
				ColorScheme = Colors.TopLevel,
				Text = "This is a test\nwithout a \nPanelView",
				TextAlignment = TextAlignment.Centered
			};
			smartView.Border.Child = smartView;

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
					smartPanel.Child.Border.Padding = new Thickness (smartPanel.Child.Border.Padding.Left,
						int.Parse (e.NewText.ToString ()), smartPanel.Child.Border.Padding.Right,
						smartPanel.Child.Border.Padding.Bottom);

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
					smartPanel.Child.Border.Padding = new Thickness (int.Parse (e.NewText.ToString ()),
						smartPanel.Child.Border.Padding.Top, smartPanel.Child.Border.Padding.Right,
						smartPanel.Child.Border.Padding.Bottom);

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
					smartPanel.Child.Border.Padding = new Thickness (smartPanel.Child.Border.Padding.Left,
						smartPanel.Child.Border.Padding.Top, int.Parse (e.NewText.ToString ()),
						smartPanel.Child.Border.Padding.Bottom);

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
					smartPanel.Child.Border.Padding = new Thickness (smartPanel.Child.Border.Padding.Left,
						smartPanel.Child.Border.Padding.Top, smartPanel.Child.Border.Padding.Right,
						int.Parse (e.NewText.ToString ()));

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
				smartPanel.Child.Border.Padding = new Thickness (smartPanel.Child.Border.Padding.Top);
				smartView.Border.Padding = new Thickness (smartView.Border.Padding.Top);
				if (paddingTopEdit.Text.IsEmpty) {
					paddingTopEdit.Text = "0";
				}
				paddingBottomEdit.Text = paddingLeftEdit.Text = paddingRightEdit.Text = paddingTopEdit.Text;
			};
			Win.Add (replacePadding);

			var cbUseUsePanelFrame = new CheckBox ("UsePanelFrame") {
				X = Pos.X (replacePadding),
				Y = Pos.Y (replacePadding) + 3,
				Checked = smartPanel.UsePanelFrame
			};
			cbUseUsePanelFrame.Toggled += (e) => smartPanel.UsePanelFrame = !e;
			Win.Add (cbUseUsePanelFrame);

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
					smartPanel.Child.Border.BorderThickness = new Thickness (smartPanel.Child.Border.BorderThickness.Left,
						int.Parse (e.NewText.ToString ()), smartPanel.Child.Border.BorderThickness.Right,
						smartPanel.Child.Border.BorderThickness.Bottom);

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
					smartPanel.Child.Border.BorderThickness = new Thickness (int.Parse (e.NewText.ToString ()),
						smartPanel.Child.Border.BorderThickness.Top, smartPanel.Child.Border.BorderThickness.Right,
						smartPanel.Child.Border.BorderThickness.Bottom);

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
					smartPanel.Child.Border.BorderThickness = new Thickness (smartPanel.Child.Border.BorderThickness.Left,
						smartPanel.Child.Border.BorderThickness.Top, int.Parse (e.NewText.ToString ()),
						smartPanel.Child.Border.BorderThickness.Bottom);

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
					smartPanel.Child.Border.BorderThickness = new Thickness (smartPanel.Child.Border.BorderThickness.Left,
						smartPanel.Child.Border.BorderThickness.Top, smartPanel.Child.Border.BorderThickness.Right,
						int.Parse (e.NewText.ToString ()));

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
				smartPanel.Child.Border.BorderThickness = new Thickness (smartPanel.Child.Border.BorderThickness.Top);
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
					smartPanel.Child.Border.DrawMarginFrame = cbDrawMarginFrame.Checked;
					smartView.Border.DrawMarginFrame = cbDrawMarginFrame.Checked;
					if (cbDrawMarginFrame.Checked != smartView.Border.DrawMarginFrame) {
						cbDrawMarginFrame.Checked = smartView.Border.DrawMarginFrame;
					}
				} catch { }
			};
			Win.Add (cbDrawMarginFrame);

			rbBorderStyle.SelectedItemChanged += (e) => {
				smartPanel.Child.Border.BorderStyle = (BorderStyle)e.SelectedItem;
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
					smartPanel.Child.Border.Effect3DOffset = new Point (int.Parse (e.NewText.ToString ()),
						smartPanel.Child.Border.Effect3DOffset.Y);

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
					smartPanel.Child.Border.Effect3DOffset = new Point (smartPanel.Child.Border.Effect3DOffset.X,
						int.Parse (e.NewText.ToString ()));

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
					smartPanel.Child.Border.Effect3D = smartView.Border.Effect3D = effect3DOffsetX.Enabled =
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
				smartPanel.Child.Border.Background = smartView.Border.Background = (Color)e.SelectedItem;
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
				smartPanel.Child.Border.BorderBrush = smartView.Border.BorderBrush = (Color)e.SelectedItem;
			};
			Win.Add (rbBorderBrush);

			Win.Add (smartPanel);
			Win.Add (smartView);

			Win.BringSubviewToFront (smartPanel);
		}
	}
}