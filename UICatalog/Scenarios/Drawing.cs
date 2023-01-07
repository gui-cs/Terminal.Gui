using System;
using Terminal.Gui;
using Terminal.Gui.Graphs;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Drawing", Description: "Demonstrates StraightLineCanvas.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Layout")]
	public class Drawing : Scenario {

		public override void Setup ()
		{
			var canvas = new DrawingPanel {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			Win.Add(canvas);
		}

		class DrawingPanel : View
		{
			StraightLineCanvas canvas;
			Point? currentLineStart = null;

			public DrawingPanel ()
			{
				canvas = new StraightLineCanvas (Application.Driver);
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				var runes = canvas.GenerateImage (bounds);
				
				for(int y=0;y<bounds.Height;y++) {
					for (int x = 0; x < bounds.Width; x++) {
						var rune = runes [y, x];
						
						if(rune.HasValue) {
							AddRune (x, y, rune.Value);
						}
					}

				}
			}
			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{

				if(mouseEvent.Flags.HasFlag(MouseFlags.Button1Pressed)) {
					if(currentLineStart == null) {
						currentLineStart = new Point(mouseEvent.X,mouseEvent.Y);
					}
				}
				else {
					if (currentLineStart != null) {

						var start = currentLineStart.Value;
						var end = new Point(mouseEvent.X, mouseEvent.Y);
						var orientation = Orientation.Vertical;
						var length = end.Y - start.Y;

						// if line is wider than it is tall switch to horizontal
						if(Math.Abs(start.X - end.X) > Math.Abs(start.Y - end.Y)) 
						{
							orientation = Orientation.Horizontal;
							length = end.X - start.X;
						}


						canvas.AddLine (start, length, orientation, BorderStyle.Single);
						currentLineStart = null;
						SetNeedsDisplay ();
					}
				}

				return base.OnMouseEvent (mouseEvent);
			}
		}
	}
}
