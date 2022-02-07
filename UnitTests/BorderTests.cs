﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Rune = System.Rune;

namespace Terminal.Gui.Core {
	public class BorderTests {
		[Fact]
		[AutoInitShutdown]
		public void Constructor_Defaults ()
		{
			var b = new Border ();
			Assert.Equal (BorderStyle.None, b.BorderStyle);
			Assert.False (b.DrawMarginFrame);
			Assert.Equal (default, b.BorderThickness);
			Assert.Equal (default, b.BorderBrush);
			Assert.Equal (default, b.Background);
			Assert.Equal (default, b.Padding);
			Assert.Equal (0, b.ActualWidth);
			Assert.Equal (0, b.ActualHeight);
			Assert.Null (b.Child);
			Assert.Null (b.ChildContainer);
			Assert.False (b.Effect3D);
			Assert.Equal (new Point (1, 1), b.Effect3DOffset);
			Assert.Null (b.Effect3DBrush);
		}

		[Fact]
		public void BorderStyle_Different_None_Ensures_DrawMarginFrame_To_True ()
		{
			var b = new Border () {
				BorderStyle = BorderStyle.Single,
				DrawMarginFrame = false
			};

			Assert.True (b.DrawMarginFrame);

			b.BorderStyle = BorderStyle.None;
			Assert.True (b.DrawMarginFrame);
			b.DrawMarginFrame = false;
			Assert.False (b.DrawMarginFrame);
		}

		[Fact]
		[AutoInitShutdown]
		public void ActualWidth_ActualHeight ()
		{
			var v = new View (new Rect (5, 10, 60, 20), "", new Border ());

			Assert.Equal (60, v.Border.ActualWidth);
			Assert.Equal (20, v.Border.ActualHeight);
		}

		[Fact]
		public void ToplevelContainer_LayoutStyle_Computed_Constuctor_ ()
		{
			var tc = new Border.ToplevelContainer (new Border ());

			Assert.Equal (LayoutStyle.Computed, tc.LayoutStyle);
		}

		[Fact]
		public void ToplevelContainer_LayoutStyle_Absolute_Constuctor_ ()
		{
			var tc = new Border.ToplevelContainer (new Rect (1, 2, 3, 4), new Border ());

			Assert.Equal (LayoutStyle.Absolute, tc.LayoutStyle);
		}

		[Fact]
		public void GetSumThickness_Test ()
		{
			var b = new Border () {
				BorderThickness = new Thickness (1, 2, 3, 4),
				Padding = new Thickness (4, 3, 2, 1)
			};
			Assert.Equal (new Thickness (5, 5, 5, 5), b.GetSumThickness ());
		}

		[Fact]
		[AutoInitShutdown]
		public void DrawContent_With_Child_Border ()
		{
			var top = Application.Top;
			var driver = (FakeDriver)Application.Driver;

			var label = new Label () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Border = new Border () {
					BorderStyle = BorderStyle.Single,
					Padding = new Thickness (2),
					BorderThickness = new Thickness (2),
					BorderBrush = Color.Red,
					Background = Color.BrightGreen,
					Effect3D = true,
					Effect3DOffset = new Point (2, -3)
				},
				ColorScheme = Colors.TopLevel,
				Text = "This is a test"
			};
			label.Border.Child = label;
			top.Add (label);

			top.LayoutSubviews ();
			label.Redraw (label.Bounds);

			var frame = label.Frame;
			var drawMarginFrame = label.Border.DrawMarginFrame ? 1 : 0;
			var sumThickness = label.Border.GetSumThickness ();
			var padding = label.Border.Padding;
			var effect3DOffset = label.Border.Effect3DOffset;
			var borderStyle = label.Border.BorderStyle;

			// Check the upper BorderThickness
			for (int r = frame.Y - drawMarginFrame - sumThickness.Top;
				r < frame.Y - drawMarginFrame - padding.Top; r++) {
				for (int c = frame.X - drawMarginFrame - sumThickness.Left;
					c < frame.Right + drawMarginFrame + sumThickness.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.Red, color.Background);
				}
			}

			// Check the left BorderThickness
			for (int r = frame.Y - drawMarginFrame - padding.Top;
				r < frame.Bottom + drawMarginFrame + padding.Bottom; r++) {
				for (int c = frame.X - drawMarginFrame - sumThickness.Left;
					c < frame.X - drawMarginFrame - padding.Left; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.Red, color.Background);
				}
			}

			// Check the right BorderThickness
			for (int r = frame.Y - drawMarginFrame - padding.Top;
				r < frame.Bottom + drawMarginFrame + padding.Bottom; r++) {
				for (int c = frame.Right + drawMarginFrame + padding.Right;
					c < frame.Right + drawMarginFrame - sumThickness.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.Red, color.Background);
				}
			}

			// Check the lower BorderThickness
			for (int r = frame.Bottom + drawMarginFrame + padding.Bottom;
				r < frame.Bottom + drawMarginFrame + sumThickness.Bottom; r++) {
				for (int c = frame.X - drawMarginFrame - sumThickness.Left;
					c < frame.Right + drawMarginFrame + sumThickness.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.Red, color.Background);
				}
			}

			// Check the upper Padding
			for (int r = frame.Y - drawMarginFrame - padding.Top;
				r < frame.Y - drawMarginFrame; r++) {
				for (int c = frame.X - drawMarginFrame - padding.Left;
					c < frame.Right + drawMarginFrame + padding.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Background);
				}
			}

			// Check the left Padding
			for (int r = frame.Y - drawMarginFrame;
				r < frame.Bottom + drawMarginFrame; r++) {
				for (int c = frame.X - drawMarginFrame - padding.Left;
					c < frame.X - drawMarginFrame; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Background);
				}
			}

			// Check the right Padding
			for (int r = frame.Y - drawMarginFrame;
				r < frame.Bottom + drawMarginFrame; r++) {
				for (int c = frame.Right + drawMarginFrame;
					c < frame.Right + drawMarginFrame - padding.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Background);
				}
			}

			// Check the lower Padding
			for (int r = frame.Bottom + drawMarginFrame;
				r < frame.Bottom + drawMarginFrame + padding.Bottom; r++) {
				for (int c = frame.X - drawMarginFrame - padding.Left;
					c < frame.Right + drawMarginFrame + padding.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Background);
				}
			}

			Rune hLine = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.HLine : (borderStyle == BorderStyle.Double ? driver.HDLine : ' ')) : ' ';
			Rune vLine = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.VLine : (borderStyle == BorderStyle.Double ? driver.VDLine : ' ')) : ' ';
			Rune uRCorner = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.URCorner : (borderStyle == BorderStyle.Double ? driver.URDCorner : ' ')) : ' ';
			Rune uLCorner = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.ULCorner : (borderStyle == BorderStyle.Double ? driver.ULDCorner : ' ')) : ' ';
			Rune lLCorner = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.LLCorner : (borderStyle == BorderStyle.Double ? driver.LLDCorner : ' ')) : ' ';
			Rune lRCorner = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.LRCorner : (borderStyle == BorderStyle.Double ? driver.LRDCorner : ' ')) : ' ';

			var text = "";
			// Check the MarginFrame
			for (int r = frame.Y - drawMarginFrame;
				r < frame.Bottom + drawMarginFrame; r++) {
				for (int c = frame.X - drawMarginFrame;
					c <= frame.Right + drawMarginFrame - 1; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					var rune = (Rune)driver.Contents [r, c, 0];
					Assert.Equal (Color.Black, color.Background);
					if (c == frame.X - drawMarginFrame && r == frame.Y - drawMarginFrame) {
						Assert.Equal (uLCorner, rune);
					} else if (c == frame.Right && r == frame.Y - drawMarginFrame) {
						Assert.Equal (uRCorner, rune);
					} else if (c == frame.X - drawMarginFrame && r == frame.Bottom) {
						Assert.Equal (lLCorner, rune);
					} else if (c == frame.Right && r == frame.Bottom) {
						Assert.Equal (lRCorner, rune);
					} else if (c >= frame.X && (r == frame.Y - drawMarginFrame
						|| r == frame.Bottom)) {
						Assert.Equal (hLine, rune);
					} else if ((c == frame.X - drawMarginFrame || c == frame.Right)
						&& r >= frame.Y && r <= frame.Bottom - drawMarginFrame) {
						Assert.Equal (vLine, rune);
					} else {
						text += rune.ToString ();
					}
				}
			}
			Assert.Equal ("This is a test", text.Trim ());

			// Check the upper Effect3D
			for (int r = frame.Y - drawMarginFrame - sumThickness.Top + effect3DOffset.Y;
				r < frame.Y - drawMarginFrame - sumThickness.Top; r++) {
				for (int c = frame.X - drawMarginFrame - sumThickness.Left + effect3DOffset.X;
					c < frame.Right + drawMarginFrame + sumThickness.Right + effect3DOffset.X; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.DarkGray, color.Background);
				}
			}

			// Check the left Effect3D
			for (int r = frame.Y - drawMarginFrame - sumThickness.Top + effect3DOffset.Y;
				r < frame.Bottom + drawMarginFrame + sumThickness.Bottom + effect3DOffset.Y; r++) {
				for (int c = frame.X - drawMarginFrame - sumThickness.Left + effect3DOffset.X;
					c < frame.X - drawMarginFrame - sumThickness.Left; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.DarkGray, color.Background);
				}
			}

			// Check the right Effect3D
			for (int r = frame.Y - drawMarginFrame - sumThickness.Top + effect3DOffset.Y;
				r < frame.Bottom + drawMarginFrame + sumThickness.Bottom + effect3DOffset.Y; r++) {
				for (int c = frame.Right + drawMarginFrame + sumThickness.Right;
					c < frame.Right + drawMarginFrame + sumThickness.Right + effect3DOffset.X; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.DarkGray, color.Background);
				}
			}

			// Check the lower Effect3D
			for (int r = frame.Bottom + drawMarginFrame + sumThickness.Bottom;
				r < frame.Bottom + drawMarginFrame + sumThickness.Bottom + effect3DOffset.Y; r++) {
				for (int c = frame.X - drawMarginFrame - sumThickness.Left + effect3DOffset.X;
					c < frame.Right + drawMarginFrame + sumThickness.Right + effect3DOffset.X; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.DarkGray, color.Background);
				}
			}

			// Check the Child frame
			for (int r = frame.Y; r < frame.Y + frame.Height; r++) {
				for (int c = frame.X; c < frame.X + frame.Width; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Foreground);
					Assert.Equal (Color.Black, color.Background);
				}
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void DrawContent_With_Parent_Border ()
		{
			var top = Application.Top;
			var driver = (FakeDriver)Application.Driver;

			var frameView = new FrameView () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = 24,
				Height = 13,
				Border = new Border () {
					BorderStyle = BorderStyle.Single,
					Padding = new Thickness (2),
					BorderThickness = new Thickness (2),
					BorderBrush = Color.Red,
					Background = Color.BrightGreen,
					Effect3D = true,
					Effect3DOffset = new Point (2, -3)
				}
			};
			frameView.Add (new Label () {
				ColorScheme = Colors.TopLevel,
				Text = "This is a test"
			});
			//frameView.Border.Child = frameView;
			top.Add (frameView);

			top.LayoutSubviews ();
			frameView.Redraw (frameView.Bounds);

			var frame = frameView.Frame;
			var drawMarginFrame = frameView.Border.DrawMarginFrame ? 1 : 0;
			var sumThickness = frameView.Border.GetSumThickness ();
			var borderThickness = frameView.Border.BorderThickness;
			var padding = frameView.Border.Padding;

			var effect3DOffset = frameView.Border.Effect3DOffset;
			var borderStyle = frameView.Border.BorderStyle;

			// Check the upper BorderThickness
			for (int r = frame.Y;
				r < Math.Min (frame.Y + borderThickness.Top, frame.Bottom); r++) {
				for (int c = frame.X;
					c < frame.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.Red, color.Background);
				}
			}

			// Check the left BorderThickness
			for (int r = Math.Min (frame.Y + borderThickness.Top, frame.Bottom);
				r < frame.Bottom - borderThickness.Bottom; r++) {
				for (int c = frame.X;
					c < Math.Min (frame.X + borderThickness.Left, frame.Right); c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.Red, color.Background);
				}
			}

			// Check the right BorderThickness
			for (int r = Math.Min (frame.Y + borderThickness.Top, frame.Bottom);
				r < frame.Bottom - borderThickness.Bottom; r++) {
				for (int c = Math.Max (frame.Right - borderThickness.Right, frame.X);
					c < frame.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.Red, color.Background);
				}
			}

			// Check the lower BorderThickness
			for (int r = Math.Max (frame.Bottom - borderThickness.Bottom, frame.Y);
				r < frame.Bottom; r++) {
				for (int c = frame.X;
					c < frame.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.Red, color.Background);
				}
			}

			// Check the upper Padding
			for (int r = frame.Y + borderThickness.Top;
				r < Math.Min (frame.Y + sumThickness.Top, frame.Bottom - borderThickness.Bottom); r++) {
				for (int c = frame.X + borderThickness.Left;
					c < frame.Right - borderThickness.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Background);
				}
			}

			// Check the left Padding
			for (int r = frame.Y + sumThickness.Top;
							r < frame.Bottom - sumThickness.Bottom; r++) {
				for (int c = frame.X + borderThickness.Left;
					c < Math.Min (frame.X + sumThickness.Left, frame.Right - borderThickness.Right); c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Background);
				}
			}

			// Check the right Padding
			// Draw the right Padding
			for (int r = frame.Y + sumThickness.Top;
				r < frame.Bottom - sumThickness.Bottom; r++) {
				for (int c = Math.Max (frame.Right - sumThickness.Right, frame.X + sumThickness.Left);
					c < Math.Max (frame.Right - borderThickness.Right, frame.X + sumThickness.Left); c++) {


					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Background);
				}
			}

			// Check the lower Padding
			for (int r = Math.Max (frame.Bottom - sumThickness.Bottom, frame.Y + borderThickness.Top);
				r < frame.Bottom - borderThickness.Bottom; r++) {
				for (int c = frame.X + borderThickness.Left;
					c < frame.Right - borderThickness.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Background);
				}
			}

			Rune hLine = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.HLine : (borderStyle == BorderStyle.Double ? driver.HDLine : ' ')) : ' ';
			Rune vLine = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.VLine : (borderStyle == BorderStyle.Double ? driver.VDLine : ' ')) : ' ';
			Rune uRCorner = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.URCorner : (borderStyle == BorderStyle.Double ? driver.URDCorner : ' ')) : ' ';
			Rune uLCorner = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.ULCorner : (borderStyle == BorderStyle.Double ? driver.ULDCorner : ' ')) : ' ';
			Rune lLCorner = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.LLCorner : (borderStyle == BorderStyle.Double ? driver.LLDCorner : ' ')) : ' ';
			Rune lRCorner = drawMarginFrame > 0 ? (borderStyle == BorderStyle.Single
				? driver.LRCorner : (borderStyle == BorderStyle.Double ? driver.LRDCorner : ' ')) : ' ';

			var text = "";
			// Check the MarginFrame
			for (int r = frame.Y + sumThickness.Top;
				r < frame.Bottom - sumThickness.Bottom; r++) {
				for (int c = frame.X + sumThickness.Left;
					c <= frame.Right - sumThickness.Right - 1; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					var rune = (Rune)driver.Contents [r, c, 0];
					Assert.Equal (Color.Black, color.Background);
					if (c == frame.X + sumThickness.Left && r == frame.Y + sumThickness.Top) {
						Assert.Equal (uLCorner, rune);
					} else if (c == frame.Right - drawMarginFrame - sumThickness.Right
						&& r == frame.Y + sumThickness.Top) {
						Assert.Equal (uRCorner, rune);
					} else if (c == frame.X + sumThickness.Left
						&& r == frame.Bottom - drawMarginFrame - sumThickness.Bottom) {
						Assert.Equal (lLCorner, rune);
					} else if (c == frame.Right - drawMarginFrame - sumThickness.Right
						&& r == frame.Bottom - drawMarginFrame - sumThickness.Bottom) {
						Assert.Equal (lRCorner, rune);
					} else if (c > frame.X + sumThickness.Left
						&& (r == frame.Y + sumThickness.Top
						|| r == frame.Bottom - drawMarginFrame - sumThickness.Bottom)) {
						Assert.Equal (hLine, rune);
					} else if ((c == frame.X + sumThickness.Left
						|| c == frame.Right - drawMarginFrame - sumThickness.Right)
						&& r >= frame.Y + drawMarginFrame + sumThickness.Top) {
						Assert.Equal (vLine, rune);
					} else {
						text += rune.ToString ();
					}
				}
			}
			Assert.Equal ("This is a test", text.Trim ());

			// Check the upper Effect3D
			for (int r = frame.Y + effect3DOffset.Y;
				r < frame.Y; r++) {
				for (int c = frame.X + effect3DOffset.X;
					c < frame.Right + effect3DOffset.X; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.DarkGray, color.Background);
				}
			}

			// Check the left Effect3D
			for (int r = frame.Y + effect3DOffset.Y;
				r < frame.Bottom + effect3DOffset.Y; r++) {
				for (int c = frame.X + effect3DOffset.X;
					c < frame.X; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.DarkGray, color.Background);
				}
			}

			// Check the right Effect3D
			for (int r = frame.Y + effect3DOffset.Y;
				r < frame.Bottom + effect3DOffset.Y; r++) {
				for (int c = frame.Right;
					c < frame.Right + effect3DOffset.X; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.DarkGray, color.Background);
				}
			}

			// Check the lower Effect3D
			for (int r = frame.Bottom;
				r < frame.Bottom + effect3DOffset.Y; r++) {
				for (int c = frame.X + effect3DOffset.X;
					c < frame.Right + effect3DOffset.X; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.DarkGray, color.Background);
				}
			}

			// Check the Child frame
			for (int r = frame.Y + drawMarginFrame + sumThickness.Top;
				r < frame.Bottom - drawMarginFrame - sumThickness.Bottom; r++) {
				for (int c = frame.X + drawMarginFrame + sumThickness.Left;
					c < frame.Right - drawMarginFrame - sumThickness.Right; c++) {

					var color = (Attribute)driver.Contents [r, c, 1];
					Assert.Equal (Color.BrightGreen, color.Foreground);
					Assert.Equal (Color.Black, color.Background);
				}
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void BorderOnControlWithNoChildren ()
		{
			var label = new TextField ("Loading...") {
				Border = new Border () {
					BorderStyle = BorderStyle.Single,
					DrawMarginFrame = true,
					Padding = new Thickness (1),
					BorderBrush = Color.White
				}
			};

			Application.Top.Add (label);

			Assert.Null (Record.Exception (() => label.Redraw (label.Bounds)));
		}
	}
}
