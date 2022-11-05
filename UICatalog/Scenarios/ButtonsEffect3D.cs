using System;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "ButtonsEffect3D", Description: "Buttons with Effect3D Animation")]
	[ScenarioCategory ("Controls")]
	public class ButtonsEffect3D : Scenario {
		public override void Setup ()
		{
			var buttonRightBottomEffect3D = new FrameView ("Button with right bottom Effect3D") {
				X = 1,
				Y = 1,
				Width = 37,
				Height = 5,
				ColorScheme = Colors.Dialog
			};
			buttonRightBottomEffect3D.Border.Effect3D = true;
			buttonRightBottomEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			buttonRightBottomEffect3D.Border.UseHalfEffect3D = true;

			buttonRightBottomEffect3D.Add (new Button ("Button 1") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			buttonRightBottomEffect3D.Add (new Button ("Button 2") {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			Win.Add (buttonRightBottomEffect3D);

			var buttonRightTopEffect3D = new FrameView ("Button with right top Effect3D") {
				X = 1,
				Y = Pos.Bottom (buttonRightBottomEffect3D) + 2,
				Width = 37,
				Height = 5,
				ColorScheme = Colors.Dialog
			};
			buttonRightTopEffect3D.Border.Effect3D = true;
			buttonRightTopEffect3D.Border.Effect3DOffset = new Point (2, -2);
			buttonRightTopEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			buttonRightTopEffect3D.Border.UseHalfEffect3D = true;

			buttonRightTopEffect3D.Add (new Button ("Button 3") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			buttonRightTopEffect3D.Add (new Button ("Button 4") {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			Win.Add (buttonRightTopEffect3D);

			var buttonLeftTopEffect3D = new FrameView ("Button with left top Effect3D") {
				X = 1,
				Y = Pos.Bottom (buttonRightTopEffect3D) + 2,
				Width = 37,
				Height = 5,
				ColorScheme = Colors.Dialog
			};
			buttonLeftTopEffect3D.Border.Effect3D = true;
			buttonLeftTopEffect3D.Border.Effect3DOffset = new Point (-2, -2);
			buttonLeftTopEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			buttonLeftTopEffect3D.Border.UseHalfEffect3D = true;

			buttonLeftTopEffect3D.Add (new Button ("Button 5") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			buttonLeftTopEffect3D.Add (new Button ("Button 6") {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			Win.Add (buttonLeftTopEffect3D);

			var buttonLeftBottomEffect3D = new FrameView ("Button with left bottom Effect3D") {
				X = 1,
				Y = Pos.Bottom (buttonLeftTopEffect3D) + 2,
				Width = 37,
				Height = 5,
				ColorScheme = Colors.Dialog
			};
			buttonLeftBottomEffect3D.Border.Effect3D = true;
			buttonLeftBottomEffect3D.Border.Effect3DOffset = new Point (-2, 2);
			buttonLeftBottomEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			buttonLeftBottomEffect3D.Border.UseHalfEffect3D = true;

			buttonLeftBottomEffect3D.Add (new Button ("Button 7") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, 2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			buttonLeftBottomEffect3D.Add (new Button ("Button 8") {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, 2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			Win.Add (buttonLeftBottomEffect3D);

			var panelRightBottomEffect3D = new FrameView ("Panel with right bottom Effect3D") {
				X = Pos.Right (buttonRightBottomEffect3D) + 3,
				Y = 1,
				Width = 37,
				Height = 5,
				ColorScheme = Colors.Dialog
			};
			panelRightBottomEffect3D.Border.Effect3D = true;
			panelRightBottomEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			panelRightBottomEffect3D.Border.UseHalfEffect3D = true;

			panelRightBottomEffect3D.Add (new PanelView (new View ("Panel 1") { CanFocus = true }) {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			panelRightBottomEffect3D.Add (new PanelView (new View ("Panel 2") { CanFocus = true }) {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			Win.Add (panelRightBottomEffect3D);

			var panelRightTopEffect3D = new FrameView ("Panel with right top Effect3D") {
				X = Pos.Right (buttonRightBottomEffect3D) + 3,
				Y = Pos.Bottom (panelRightBottomEffect3D) + 2,
				Width = 37,
				Height = 5,
				ColorScheme = Colors.Dialog
			};
			panelRightTopEffect3D.Border.Effect3D = true;
			panelRightTopEffect3D.Border.Effect3DOffset = new Point (2, -2);
			panelRightTopEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			panelRightTopEffect3D.Border.UseHalfEffect3D = true;

			panelRightTopEffect3D.Add (new PanelView (new View ("Panel 3") { CanFocus = true }) {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			panelRightTopEffect3D.Add (new PanelView (new View ("Panel 4") { CanFocus = true }) {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			Win.Add (panelRightTopEffect3D);

			var panelLeftTopEffect3D = new FrameView ("Panel with left top Effect3D") {
				X = Pos.Right (buttonRightBottomEffect3D) + 3,
				Y = Pos.Bottom (panelRightTopEffect3D) + 2,
				Width = 37,
				Height = 5,
				ColorScheme = Colors.Dialog
			};
			panelLeftTopEffect3D.Border.Effect3D = true;
			panelLeftTopEffect3D.Border.Effect3DOffset = new Point (-2, -2);
			panelLeftTopEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			panelLeftTopEffect3D.Border.UseHalfEffect3D = true;

			panelLeftTopEffect3D.Add (new PanelView (new View ("Panel 5") { CanFocus = true }) {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			panelLeftTopEffect3D.Add (new PanelView (new View ("Panel 6") { CanFocus = true }) {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			}); ;

			Win.Add (panelLeftTopEffect3D);

			var panelLeftBottomEffect3D = new FrameView ("Panel with left bottom Effect3D") {
				X = Pos.Right (buttonRightBottomEffect3D) + 3,
				Y = Pos.Bottom (panelLeftTopEffect3D) + 2,
				Width = 37,
				Height = 5,
				ColorScheme = Colors.Dialog
			};
			panelLeftBottomEffect3D.Border.Effect3D = true;
			panelLeftBottomEffect3D.Border.Effect3DOffset = new Point (-2, 2);
			panelLeftBottomEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			panelLeftBottomEffect3D.Border.UseHalfEffect3D = true;

			panelLeftBottomEffect3D.Add (new PanelView (new View ("Panel 7") { CanFocus = true }) {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, 2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			panelLeftBottomEffect3D.Add (new PanelView (new View ("Panel 8") { CanFocus = true }) {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, 2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true
			});

			Win.Add (panelLeftBottomEffect3D);

			var labelRightBottomEffect3D = new FrameView ("Label with right bottom Effect3D") {
				X = Pos.Right (panelRightBottomEffect3D) + 3,
				Y = 1,
				Width = 37,
				Height = 6,
				ColorScheme = Colors.Dialog
			};
			labelRightBottomEffect3D.Border.Effect3D = true;
			labelRightBottomEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			labelRightBottomEffect3D.Border.UseHalfEffect3D = true;

			labelRightBottomEffect3D.Add (new Label ("Label\n1") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true,
				CanFocus = true,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			});

			labelRightBottomEffect3D.Add (new Label ("Label\n2") {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true,
				CanFocus = true,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			});

			Win.Add (labelRightBottomEffect3D);

			var labelRightTopEffect3D = new FrameView ("Label with right top Effect3D") {
				X = Pos.Right (panelRightBottomEffect3D) + 3,
				Y = Pos.Bottom (labelRightBottomEffect3D) + 2,
				Width = 37,
				Height = 6,
				ColorScheme = Colors.Dialog
			};
			labelRightTopEffect3D.Border.Effect3D = true;
			labelRightTopEffect3D.Border.Effect3DOffset = new Point (2, -2);
			labelRightTopEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			labelRightTopEffect3D.Border.UseHalfEffect3D = true;

			labelRightTopEffect3D.Add (new Label ("Label\n3") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true,
				CanFocus = true,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			});

			labelRightTopEffect3D.Add (new Label ("Label\n4") {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true,
				CanFocus = true,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			});

			Win.Add (labelRightTopEffect3D);

			var labelLeftTopEffect3D = new FrameView ("Label with left top Effect3D") {
				X = Pos.Right (panelRightBottomEffect3D) + 3,
				Y = Pos.Bottom (labelRightTopEffect3D) + 2,
				Width = 37,
				Height = 6,
				ColorScheme = Colors.Dialog
			};
			labelLeftTopEffect3D.Border.Effect3D = true;
			labelLeftTopEffect3D.Border.Effect3DOffset = new Point (-2, -2);
			labelLeftTopEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			labelLeftTopEffect3D.Border.UseHalfEffect3D = true;

			labelLeftTopEffect3D.Add (new Label ("Label\n5") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true,
				CanFocus = true,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			});

			labelLeftTopEffect3D.Add (new Label ("Label\n6") {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true,
				CanFocus = true,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			});

			Win.Add (labelLeftTopEffect3D);

			var labelLeftBottomEffect3D = new FrameView ("Label with left bottom Effect3D") {
				X = Pos.Right (panelRightBottomEffect3D) + 3,
				Y = Pos.Bottom (labelLeftTopEffect3D) + 2,
				Width = 37,
				Height = 6,
				ColorScheme = Colors.Dialog
			};
			labelLeftBottomEffect3D.Border.Effect3D = true;
			labelLeftBottomEffect3D.Border.Effect3DOffset = new Point (-2, 2);
			labelLeftBottomEffect3D.Border.Effect3DBrush = new Attribute (Color.Black, Color.Black);
			labelLeftBottomEffect3D.Border.UseHalfEffect3D = true;

			labelLeftBottomEffect3D.Add (new Label ("Label\n7") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, 2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true,
				CanFocus = true,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			});

			labelLeftBottomEffect3D.Add (new Label ("Label\n8") {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, 2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
					UseHalfEffect3D = true
				},
				UseEffect3DAnimation = true,
				CanFocus = true,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			});

			Win.Add (labelLeftBottomEffect3D);
		}
	}
}