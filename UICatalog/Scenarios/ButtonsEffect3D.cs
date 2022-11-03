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
				Y = 1,
				Width = 37,
				Height = 5,
			};

			buttonRightBottomEffect3D.Add (new Button ("Button 1") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
				},
				UseEffect3DAnimation = true
			});

			buttonRightBottomEffect3D.Add (new Button ("Button 2") {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
				},
				UseEffect3DAnimation = true
			});

			Win.Add (buttonRightBottomEffect3D);

			var buttonRightTopEffect3D = new FrameView ("Button with right top Effect3D") {
				Y = Pos.Bottom (buttonRightBottomEffect3D),
				Width = 37,
				Height = 5,
			};

			buttonRightTopEffect3D.Add (new Button ("Button 3") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
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
				},
				UseEffect3DAnimation = true
			});

			Win.Add (buttonRightTopEffect3D);

			var buttonLeftTopEffect3D = new FrameView ("Button with left top Effect3D") {
				Y = Pos.Bottom (buttonRightTopEffect3D),
				Width = 37,
				Height = 5,
			};

			buttonLeftTopEffect3D.Add (new Button ("Button 5") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
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
				},
				UseEffect3DAnimation = true
			});

			Win.Add (buttonLeftTopEffect3D);

			var buttonLeftBottomEffect3D = new FrameView ("Button with left bottom Effect3D") {
				Y = Pos.Bottom (buttonLeftTopEffect3D),
				Width = 37,
				Height = 5,
			};

			buttonLeftBottomEffect3D.Add (new Button ("Button 7") {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, 2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
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
				},
				UseEffect3DAnimation = true
			});

			Win.Add (buttonLeftBottomEffect3D);

			var panelRightBottomEffect3D = new FrameView ("Panel with right bottom Effect3D") {
				X = Pos.Right (buttonRightBottomEffect3D) + 1,
				Y = 1,
				Width = 37,
				Height = 5,
			};

			panelRightBottomEffect3D.Add (new PanelView (new View ("Panel 1") { CanFocus = true }) {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
				},
				UseEffect3DAnimation = true
			});

			panelRightBottomEffect3D.Add (new PanelView (new View ("Panel 2") { CanFocus = true }) {
				X = 20,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
				},
				UseEffect3DAnimation = true
			});

			Win.Add (panelRightBottomEffect3D);

			var panelRightTopEffect3D = new FrameView ("Panel with right top Effect3D") {
				X = Pos.Right (buttonRightBottomEffect3D) + 1,
				Y = Pos.Bottom (panelRightBottomEffect3D),
				Width = 37,
				Height = 5,
			};

			panelRightTopEffect3D.Add (new PanelView (new View ("Panel 3") { CanFocus = true }) {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
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
				},
				UseEffect3DAnimation = true
			});

			Win.Add (panelRightTopEffect3D);

			var panelLeftTopEffect3D = new FrameView ("Panel with left top Effect3D") {
				X = Pos.Right (buttonRightBottomEffect3D) + 1,
				Y = Pos.Bottom (panelRightTopEffect3D),
				Width = 37,
				Height = 5,
			};

			panelLeftTopEffect3D.Add (new PanelView (new View ("Panel 5") { CanFocus = true }) {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, -2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
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
				},
				UseEffect3DAnimation = true
			}); ;

			Win.Add (panelLeftTopEffect3D);

			var panelLeftBottomEffect3D = new FrameView ("Panel with left bottom Effect3D") {
				X = Pos.Right (buttonRightBottomEffect3D) + 1,
				Y = Pos.Bottom (panelLeftTopEffect3D),
				Width = 37,
				Height = 5,
			};

			panelLeftBottomEffect3D.Add (new PanelView (new View ("Panel 7") { CanFocus = true }) {
				X = 2,
				Y = 1,
				Border = new Border {
					Effect3D = true,
					Effect3DOffset = new Point (-2, 2),
					Effect3DBrush = new Attribute (Color.Black, Color.Black),
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
				},
				UseEffect3DAnimation = true
			});

			Win.Add (panelLeftBottomEffect3D);
		}
	}
}