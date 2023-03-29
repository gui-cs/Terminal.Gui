using NStack;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Terminal.Gui.Graphs;
using Xunit;
using Xunit.Abstractions;
//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.CoreTests {
	public class AbsoluteLayoutTests {
		readonly ITestOutputHelper output;

		public AbsoluteLayoutTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void AbsoluteLayout_Constructor ()
		{
			var frame = new Rect (1, 2, 3, 4);
			var v = new View (frame);
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			Assert.Equal (frame, v.Frame);
			Assert.Equal (new Rect(0, 0, frame.Width, frame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

			v = new View (frame, "v");
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			Assert.Equal (frame, v.Frame);
			Assert.Equal (new Rect (0, 0, frame.Width, frame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

			v = new View (frame.X, frame.Y, "v");
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			// BUGBUG: v2 - I think the default size should be 0,0
			Assert.Equal (new Rect(frame.X, frame.Y, 1, 1), v.Frame);
			Assert.Equal (new Rect (0, 0, 1, 1), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);
		}


		[Fact]
		public void AbsoluteLayout_Set_Frame ()
		{
			var frame = new Rect (1, 2, 3, 4);
			var newFrame = new Rect (1, 2, 30, 40);

			var v = new View (frame);
			v.Frame = newFrame;
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

			v = new View (frame.X, frame.Y, "v");
			v.Frame = newFrame;
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

		}


		[Fact]
		public void AbsoluteLayout_Set_Size ()
		{
			var frame = new Rect (1, 2, 3, 4);
			var newFrame = new Rect (1, 2, 30, 40);

			var v = new View (frame);
			v.Height = newFrame.Height;
			v.Width = newFrame.Width;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

			v = new View (frame.X, frame.Y, "v");
			v.Height = newFrame.Height;
			v.Width = newFrame.Width;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

		}

		[Fact]
		public void AbsoluteLayout_Layout ()
		{
			var superRect = new Rect (0, 0, 100, 100);
			var super = new View (superRect, "super");


			var v1 = new View () {
				X = 0,
				Y = 0,
				Width = 10,
				Height = 10
			};

			var v2 = new View () {
				X = 10,
				Y = 10,
				Width = 10,
				Height = 10
			};

			super.Add (v1, v2);

			super.LayoutSubviews ();

			Assert.Equal (new Rect (0, 0, 10, 10), v1.Frame);
			Assert.Equal (new Rect (10, 10, 10, 10), v2.Frame);
		}
	}
}
