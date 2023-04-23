using System;
using System.Globalization;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	public class BordersOnContainers : Window {
		public BordersOnContainers (NStack.ustring title, string typeName, View smartView)
		{
			var borderStyle = LineStyle.Double;
			var borderThickness = new Thickness (1, 2, 3, 4);
			var borderBrush = Colors.Base.HotFocus.Foreground;
			var padding = new Thickness (1, 2, 3, 4);
			var background = Colors.Base.HotNormal.Foreground;

			smartView.X = Pos.Center ();
			smartView.Y = 0;
			smartView.Width = 40;
			smartView.Height = 20;
			smartView.BorderStyle = borderStyle;

			smartView.ColorScheme = Colors.TopLevel;

			var tf1 = new TextField ("1234567890") { Width = 10 };

			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += (s, e) => MessageBox.Query (20, 7, "Hi", $"I'm a {typeName}?", "Yes", "No");
			var label = new Label ($"I'm a {typeName}") {
				X = Pos.Center (),
				Y = Pos.Center () - 1,
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
				Text = "1234567890"
			};
			smartView.Add (tf1, button, label, tf2, tv);

			var distance = 20;
			var offsetLeftSide = 7;
			var offsetButtons = 5;

			Add (new Label ("Border:") {
				X = Pos.Center (),
			});

			var borderTopEdit = new TextField ("") {
				X = Pos.Center (),
				Y = 1,
				Width = 5
			};
			borderTopEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.Thickness = new Thickness (smartView.Border.Thickness.Left,
						int.Parse (e.NewText.ToString ()), smartView.Border.Thickness.Right,
						smartView.Border.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderTopEdit.Text = $"{smartView.Border.Thickness.Top}";

			Add (borderTopEdit);

			var borderLeftEdit = new TextField ("") {
				X = Pos.Left (borderTopEdit) - 7,
				Y = 2,
				Width = 5
			};
			borderLeftEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.Thickness = new Thickness (int.Parse (e.NewText.ToString ()),
						smartView.Border.Thickness.Top, smartView.Border.Thickness.Right,
						smartView.Border.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderLeftEdit.Text = $"{smartView.Border.Thickness.Left}";
			Add (borderLeftEdit);

			var borderRightEdit = new TextField ("") {
				X = Pos.Right (borderTopEdit) + 2,
				Y = 2,
				Width = 5
			};
			borderRightEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.Thickness = new Thickness (smartView.Border.Thickness.Left,
						smartView.Border.Thickness.Top, int.Parse (e.NewText.ToString ()),
						smartView.Border.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderRightEdit.Text = $"{smartView.Border.Thickness.Right}";
			Add (borderRightEdit);

			var borderBottomEdit = new TextField ("") {
				X = Pos.Left (borderTopEdit),
				Y = 3,
				Width = 5
			};
			borderBottomEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.Thickness = new Thickness (smartView.Border.Thickness.Left,
						smartView.Border.Thickness.Top, smartView.Border.Thickness.Right,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderBottomEdit.Text = $"{smartView.Border.Thickness.Bottom}";
			Add (borderBottomEdit);

			var replaceBorder = new Button ("Copy  Top") {
				X = Pos.Center (),
				Y = 5
			};
			replaceBorder.Clicked += (s, e) => {
				smartView.Border.Thickness = new Thickness (smartView.Border.Thickness.Top);
				if (borderTopEdit.Text.IsEmpty) {
					borderTopEdit.Text = "0";
				}
				borderBottomEdit.Text = borderLeftEdit.Text = borderRightEdit.Text = borderTopEdit.Text;
			};
			Add (replaceBorder);

			Add (new Label ("Margin:") {
				X = Pos.Left (borderLeftEdit) - distance,
			});

			var marginTopEdit = new TextField ("") {
				X = Pos.Left (borderTopEdit) - distance - 6,
				Y = 1,
				Width = 5
			};
			marginTopEdit.TextChanging += (s, e) => {
				try {
					smartView.Margin.Thickness = new Thickness (smartView.Margin.Thickness.Left,
						int.Parse (e.NewText.ToString ()), smartView.Margin.Thickness.Right,
						smartView.Margin.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			marginTopEdit.Text = $"{smartView.Margin.Thickness.Top}";

			Add (marginTopEdit);

			var marginLeftEdit = new TextField ("") {
				X = Pos.Left (marginTopEdit) - offsetLeftSide,
				Y = 2,
				Width = 5
			};
			marginLeftEdit.TextChanging += (s, e) => {
				try {
					smartView.Margin.Thickness = new Thickness (int.Parse (e.NewText.ToString ()),
						smartView.Margin.Thickness.Top, smartView.Margin.Thickness.Right,
						smartView.Margin.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			marginLeftEdit.Text = $"{smartView.Margin.Thickness.Left}";
			Add (marginLeftEdit);

			var marginRightEdit = new TextField ("") {
				X = Pos.Right (marginTopEdit) + 2,
				Y = 2,
				Width = 5
			};
			marginRightEdit.TextChanging += (s, e) => {
				try {
					smartView.Margin.Thickness = new Thickness (smartView.Margin.Thickness.Left,
						smartView.Margin.Thickness.Top, int.Parse (e.NewText.ToString ()),
						smartView.Margin.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			marginRightEdit.Text = $"{smartView.Margin.Thickness.Right}";
			Add (marginRightEdit);

			var marginBottomEdit = new TextField ("") {
				X = Pos.Left (marginTopEdit),
				Y = 3,
				Width = 5
			};
			marginBottomEdit.TextChanging += (s, e) => {
				try {
					smartView.Margin.Thickness = new Thickness (smartView.Margin.Thickness.Left,
						smartView.Margin.Thickness.Top, smartView.Margin.Thickness.Right,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			marginBottomEdit.Text = $"{smartView.Margin.Thickness.Bottom}";
			Add (marginBottomEdit);

			var replaceMargin = new Button ("Copy  Top") {
				X = Pos.Left (replaceBorder) - distance - offsetButtons,
				Y = 5
			};
			replaceMargin.Clicked += (s, e) => {
				smartView.Margin.Thickness = new Thickness (smartView.Margin.Thickness.Top);
				if (marginTopEdit.Text.IsEmpty) {
					marginTopEdit.Text = "0";
				}
				marginBottomEdit.Text = marginLeftEdit.Text = marginRightEdit.Text = marginTopEdit.Text;
			};
			Add (replaceMargin);

			Add (new Label ("Padding:") {
				X = Pos.Right (borderTopEdit) + distance,
			});

			var paddingTopEdit = new TextField ("") {
				X = Pos.Right (borderTopEdit) + distance + 1,
				Y = 1,
				Width = 5
			};
			paddingTopEdit.TextChanging += (s, e) => {
				try {
					smartView.Padding.Thickness = new Thickness (smartView.Padding.Thickness.Left,
						int.Parse (e.NewText.ToString ()), smartView.Padding.Thickness.Right,
						smartView.Padding.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingTopEdit.Text = $"{smartView.Padding.Thickness.Top}";

			Add (paddingTopEdit);

			var paddingLeftEdit = new TextField ("") {
				X = Pos.Left (paddingTopEdit) - offsetLeftSide,
				Y = 2,
				Width = 5
			};
			paddingLeftEdit.TextChanging += (s, e) => {
				try {
					smartView.Padding.Thickness = new Thickness (int.Parse (e.NewText.ToString ()),
						smartView.Padding.Thickness.Top, smartView.Padding.Thickness.Right,
						smartView.Padding.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingLeftEdit.Text = $"{smartView.Padding.Thickness.Left}";
			Add (paddingLeftEdit);

			var paddingRightEdit = new TextField ("") {
				X = Pos.Right (paddingTopEdit) + 2,
				Y = 2,
				Width = 5
			};
			paddingRightEdit.TextChanging += (s, e) => {
				try {
					smartView.Padding.Thickness = new Thickness (smartView.Padding.Thickness.Left,
						smartView.Padding.Thickness.Top, int.Parse (e.NewText.ToString ()),
						smartView.Padding.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingRightEdit.Text = $"{smartView.Padding.Thickness.Right}";
			Add (paddingRightEdit);

			var paddingBottomEdit = new TextField ("") {
				X = Pos.Left (paddingTopEdit),
				Y = 3,
				Width = 5
			};
			paddingBottomEdit.TextChanging += (s, e) => {
				try {
					smartView.Padding.Thickness = new Thickness (smartView.Padding.Thickness.Left,
						smartView.Padding.Thickness.Top, smartView.Padding.Thickness.Right,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingBottomEdit.Text = $"{smartView.Padding.Thickness.Bottom}";
			Add (paddingBottomEdit);

			var replacePadding = new Button ("Copy  Top") {
				X = Pos.Right (replaceBorder) + distance / 2 + offsetButtons / 2,
				Y = 5
			};
			replacePadding.Clicked += (s, e) => {
				smartView.Padding.Thickness = new Thickness (smartView.Padding.Thickness.Top);
				if (paddingTopEdit.Text.IsEmpty) {
					paddingTopEdit.Text = "0";
				}
				paddingBottomEdit.Text = paddingLeftEdit.Text = paddingRightEdit.Text = paddingTopEdit.Text;
			};
			Add (replacePadding);


			smartView.Y = Pos.Center () + 4;

			Add (new Label ("BorderStyle:"));

			var borderStyleEnum = Enum.GetValues (typeof (LineStyle)).Cast<LineStyle> ().ToList ();
			var rbBorderStyle = new RadioGroup (borderStyleEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = 2,
				Y = 1,
				SelectedItem = (int)smartView.BorderStyle
			};
			Add (rbBorderStyle);

			rbBorderStyle.SelectedItemChanged += (s, e) => {
				var prevBorderStyle = smartView.BorderStyle;
				smartView.Border.BorderStyle = (LineStyle)e.SelectedItem;
				if (smartView.Border.BorderStyle == LineStyle.None) {
					smartView.Border.Thickness = new Thickness (0);
				} else if (prevBorderStyle == LineStyle.None && smartView.Border.BorderStyle != LineStyle.None) {
					smartView.Border.Thickness = new Thickness (1);
				}
				borderLeftEdit.Text = smartView.Border.Thickness.Left.ToString ();
				borderTopEdit.Text = smartView.Border.Thickness.Top.ToString ();
				borderRightEdit.Text = smartView.Border.Thickness.Right.ToString ();
				borderBottomEdit.Text = smartView.Border.Thickness.Bottom.ToString ();
				smartView.SetNeedsDisplay ();
			};

			Add (new Label ("BorderBrush:") {
				X = Pos.Right (rbBorderStyle),
				Y = Pos.Bottom (replaceBorder)
			});

			var colorEnum = Enum.GetValues (typeof (Color)).Cast<Color> ().ToList ();

			var rbBorderBrush = new RadioGroup (colorEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = Pos.Right (rbBorderStyle),
				Y = Pos.Bottom (replaceBorder) + 1,
				SelectedItem = (int)smartView.Border.ColorScheme.Normal.Background
			};
			rbBorderBrush.SelectedItemChanged += (s, e) => {
				smartView.Border.ColorScheme.Normal = new Terminal.Gui.Attribute (smartView.Padding.ColorScheme.Normal.Background, (Color)e.SelectedItem);
			};
			Add (rbBorderBrush);

			Add (new Label ("PaddingBrush:") {
				X = Pos.AnchorEnd (20),
				Y = Pos.Bottom (replaceBorder)
			});

			var rbPaddingBrush = new RadioGroup (colorEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = Pos.AnchorEnd (18),
				Y = Pos.Bottom (replaceBorder) + 1,
				SelectedItem = (int)smartView.Padding.ColorScheme.Normal.Background
			};
			rbPaddingBrush.SelectedItemChanged += (s, e) => {
				smartView.Padding.ColorScheme.Normal = new Terminal.Gui.Attribute (smartView.Padding.ColorScheme.Normal.Foreground, (Color)e.SelectedItem);
				smartView.Border.ColorScheme.Normal = new Terminal.Gui.Attribute ((Color)e.SelectedItem, smartView.Border.ColorScheme.Normal.Background);
			};
			Add (rbPaddingBrush);

			Add (smartView);

			Title = title;
		}
	}
}
