﻿using NStack;
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
	public class LayoutTests {
		readonly ITestOutputHelper output;

		public LayoutTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void TopologicalSort_Missing_Add ()
		{
			var root = new View ();
			var sub1 = new View ();
			root.Add (sub1);
			var sub2 = new View ();
			sub1.Width = Dim.Width (sub2);

			Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());

			sub2.Width = Dim.Width (sub1);

			Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());
		}

		[Fact]
		public void TopologicalSort_Recursive_Ref ()
		{
			var root = new View ();
			var sub1 = new View ();
			root.Add (sub1);
			var sub2 = new View ();
			root.Add (sub2);
			sub2.Width = Dim.Width (sub2);
			Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());
		}

		[Fact, AutoInitShutdown]
		public void TrySetWidth_ForceValidatePosDim ()
		{
			var top = new View () {
				X = 0,
				Y = 0,
				Width = 80,
			};

			var v = new View () {
				Width = Dim.Fill (),
				ForceValidatePosDim = true
			};
			top.Add (v);

			Assert.False (v.TrySetWidth (70, out int rWidth));
			Assert.Equal (70, rWidth);

			v.Width = Dim.Fill (1);
			Assert.False (v.TrySetWidth (70, out rWidth));
			Assert.Equal (69, rWidth);

			v.Width = null;
			Assert.True (v.TrySetWidth (70, out rWidth));
			Assert.Equal (70, rWidth);
			Assert.False (v.IsInitialized);

			Application.Top.Add (top);
			Application.Begin (Application.Top);

			Assert.True (v.IsInitialized);
			v.Width = Dim.Fill (1);
			Assert.Throws<ArgumentException> (() => v.Width = 75);
			v.LayoutStyle = LayoutStyle.Absolute;
			v.Width = 75;
			Assert.True (v.TrySetWidth (60, out rWidth));
			Assert.Equal (60, rWidth);
		}

		[Fact, AutoInitShutdown]
		public void TrySetHeight_ForceValidatePosDim ()
		{
			var top = new View () {
				X = 0,
				Y = 0,
				Height = 20
			};

			var v = new View () {
				Height = Dim.Fill (),
				ForceValidatePosDim = true
			};
			top.Add (v);

			Assert.False (v.TrySetHeight (10, out int rHeight));
			Assert.Equal (10, rHeight);

			v.Height = Dim.Fill (1);
			Assert.False (v.TrySetHeight (10, out rHeight));
			Assert.Equal (9, rHeight);

			v.Height = null;
			Assert.True (v.TrySetHeight (10, out rHeight));
			Assert.Equal (10, rHeight);
			Assert.False (v.IsInitialized);

			Application.Top.Add (top);
			Application.Begin (Application.Top);

			Assert.True (v.IsInitialized);

			v.Height = Dim.Fill (1);
			Assert.Throws<ArgumentException> (() => v.Height = 15);
			v.LayoutStyle = LayoutStyle.Absolute;
			v.Height = 15;
			Assert.True (v.TrySetHeight (5, out rHeight));
			Assert.Equal (5, rHeight);
		}

		[Fact]
		public void GetCurrentWidth_TrySetWidth ()
		{
			var top = new View () {
				X = 0,
				Y = 0,
				Width = 80,
			};

			var v = new View () {
				Width = Dim.Fill ()
			};
			top.Add (v);
			top.LayoutSubviews ();

			Assert.False (v.AutoSize);
			Assert.True (v.TrySetWidth (0, out _));
			Assert.Equal (80, v.Frame.Width);

			v.Width = Dim.Fill (1);
			top.LayoutSubviews ();

			Assert.True (v.TrySetWidth (0, out _));
			Assert.Equal (79, v.Frame.Width);

			v.AutoSize = true;
			top.LayoutSubviews ();

			Assert.True (v.TrySetWidth (0, out _));
			Assert.Equal (79, v.Frame.Width);
		}

		[Fact]
		public void GetCurrentHeight_TrySetHeight ()
		{
			var top = new View () {
				X = 0,
				Y = 0,
				Height = 20
			};

			var v = new View () {
				Height = Dim.Fill ()
			};
			top.Add (v);
			top.LayoutSubviews ();

			Assert.False (v.AutoSize);
			Assert.True (v.TrySetHeight (0, out _));
			Assert.Equal (20, v.Frame.Height);

			v.Height = Dim.Fill (1);
			top.LayoutSubviews ();

			Assert.True (v.TrySetHeight (0, out _));
			Assert.Equal (19, v.Frame.Height);

			v.AutoSize = true;
			top.LayoutSubviews ();

			Assert.True (v.TrySetHeight (0, out _));
			Assert.Equal (19, v.Frame.Height);
		}

		[Fact]
		public void AutoSize_False_If_Text_Emmpty ()
		{
			var view1 = new View ();
			var view2 = new View ("");
			var view3 = new View () { Text = "" };

			Assert.False (view1.AutoSize);
			Assert.False (view2.AutoSize);
			Assert.False (view3.AutoSize);
		}

		[Fact]
		public void AutoSize_False_If_Text_Is_Not_Emmpty ()
		{
			var view1 = new View ();
			view1.Text = "Hello World";
			var view2 = new View ("Hello World");
			var view3 = new View () { Text = "Hello World" };

			Assert.False (view1.AutoSize);
			Assert.False (view2.AutoSize);
			Assert.False (view3.AutoSize);
		}

		[Fact]
		public void AutoSize_True_Label_If_Text_Emmpty ()
		{
			var label1 = new Label ();
			var label2 = new Label ("");
			var label3 = new Label () { Text = "" };

			Assert.True (label1.AutoSize);
			Assert.True (label2.AutoSize);
			Assert.True (label3.AutoSize);
		}

		[Fact]
		public void AutoSize_True_Label_If_Text_Is_Not_Emmpty ()
		{
			var label1 = new Label ();
			label1.Text = "Hello World";
			var label2 = new Label ("Hello World");
			var label3 = new Label () { Text = "Hello World" };

			Assert.True (label1.AutoSize);
			Assert.True (label2.AutoSize);
			Assert.True (label3.AutoSize);
		}

		[Fact]
		public void AutoSize_False_ResizeView_Is_Always_False ()
		{
			var label = new Label () { AutoSize = false };

			label.Text = "New text";

			Assert.False (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=0,Height=1}", label.Bounds.ToString ());
		}

		[Fact]
		public void AutoSize_True_ResizeView_With_Dim_Absolute ()
		{
			var label = new Label ();

			label.Text = "New text";

			Assert.True (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=8,Height=1}", label.Bounds.ToString ());
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_ResizeView_With_Dim_Fill_After_IsInitialized ()
		{
			var win = new Window (new Rect (0, 0, 30, 80), "");
			var label = new Label () { AutoSize = false, Width = Dim.Fill (), Height = Dim.Fill () };
			win.Add (label);
			Application.Top.Add (win);

			// Text is empty so height=0
			Assert.False (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=0,Height=0}", label.Bounds.ToString ());

			label.Text = "New text\nNew line";
			Application.Top.LayoutSubviews ();

			Assert.False (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=28,Height=78}", label.Bounds.ToString ());
			Assert.False (label.IsInitialized);

			Application.Begin (Application.Top);
			Assert.True (label.IsInitialized);
			Assert.False (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=28,Height=78}", label.Bounds.ToString ());
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_SetWidthHeight_With_Dim_Fill_And_Dim_Absolute_After_IsAdded_And_IsInitialized ()
		{
			var win = new Window (new Rect (0, 0, 30, 80), "");
			var label = new Label () { Width = Dim.Fill () };
			win.Add (label);
			Application.Top.Add (win);

			Assert.True (label.IsAdded);

			// Text is empty so height=0
			Assert.True (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=0,Height=0}", label.Bounds.ToString ());

			label.Text = "First line\nSecond line";
			Application.Top.LayoutSubviews ();

			Assert.True (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=28,Height=2}", label.Bounds.ToString ());
			Assert.False (label.IsInitialized);

			Application.Begin (Application.Top);

			Assert.True (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=28,Height=2}", label.Bounds.ToString ());
			Assert.True (label.IsInitialized);

			label.AutoSize = false;
			Application.Refresh ();

			Assert.False (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=28,Height=1}", label.Bounds.ToString ());
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_SetWidthHeight_With_Dim_Fill_And_Dim_Absolute_With_Initialization ()
		{
			var win = new Window (new Rect (0, 0, 30, 80), "");
			var label = new Label () { Width = Dim.Fill () };
			win.Add (label);
			Application.Top.Add (win);

			// Text is empty so height=0
			Assert.True (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=0,Height=0}", label.Bounds.ToString ());

			Application.Begin (Application.Top);

			Assert.True (label.AutoSize);
			// Here the AutoSize ensuring the right size with width 28 (Dim.Fill)
			// and height 0 because wasn't set and the text is empty
			Assert.Equal ("{X=0,Y=0,Width=28,Height=0}", label.Bounds.ToString ());

			label.Text = "First line\nSecond line";
			Application.Refresh ();

			// Here the AutoSize ensuring the right size with width 28 (Dim.Fill)
			// and height 2 because wasn't set and the text has 2 lines
			Assert.True (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=28,Height=2}", label.Bounds.ToString ());

			label.AutoSize = false;
			Application.Refresh ();

			// Here the SetMinWidthHeight ensuring the minimum height
			Assert.False (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=28,Height=1}", label.Bounds.ToString ());

			label.Text = "First changed line\nSecond changed line\nNew line";
			Application.Refresh ();

			// Here the AutoSize is false and the width 28 (Dim.Fill) and
			// height 1 because wasn't set and SetMinWidthHeight ensuring the minimum height
			Assert.False (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=28,Height=1}", label.Bounds.ToString ());

			label.AutoSize = true;
			Application.Refresh ();

			// Here the AutoSize ensuring the right size with width 28 (Dim.Fill)
			// and height 3 because wasn't set and the text has 3 lines
			Assert.True (label.AutoSize);
			Assert.Equal ("{X=0,Y=0,Width=28,Height=3}", label.Bounds.ToString ());
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_True_Setting_With_Height_Horizontal ()
		{
			var label = new Label ("Hello") { Width = 10, Height = 2 };
			var viewX = new View ("X") { X = Pos.Right (label) };
			var viewY = new View ("Y") { Y = Pos.Bottom (label) };

			Application.Top.Add (label, viewX, viewY);
			Application.Begin (Application.Top);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 2), label.Frame);

			var expected = @"
Hello     X
           
Y          
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 11, 3), pos);

			label.AutoSize = false;
			Application.Refresh ();

			Assert.False (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 2), label.Frame);

			expected = @"
Hello     X
           
Y          
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 11, 3), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_True_Setting_With_Height_Vertical ()
		{
			var label = new Label ("Hello") { Width = 2, Height = 10, TextDirection = TextDirection.TopBottom_LeftRight };
			var viewX = new View ("X") { X = Pos.Right (label) };
			var viewY = new View ("Y") { Y = Pos.Bottom (label) };

			Application.Top.Add (label, viewX, viewY);
			Application.Begin (Application.Top);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 2, 10), label.Frame);

			var expected = @"
H X
e  
l  
l  
o  
   
   
   
   
   
Y  
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 3, 11), pos);

			label.AutoSize = false;
			Application.Refresh ();

			Assert.False (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 2, 10), label.Frame);

			expected = @"
H X
e  
l  
l  
o  
   
   
   
   
   
Y  
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 3, 11), pos);
		}

		[Fact]
		[AutoInitShutdown]
		public void Excess_Text_Is_Erased_When_The_Width_Is_Reduced ()
		{
			var lbl = new Label ("123");
			Application.Top.Add (lbl);
			Application.Begin (Application.Top);

			Assert.True (lbl.AutoSize);
			Assert.Equal ("123 ", GetContents ());

			lbl.Text = "12";
			// Here the AutoSize ensuring the right size with width 3 (Dim.Absolute)
			// that was set on the OnAdded method with the text length of 3
			// and height 1 because wasn't set and the text has 1 line
			Assert.Equal (new Rect (0, 0, 3, 1), lbl.Frame);
			Assert.Equal (new Rect (0, 0, 3, 1), lbl.NeedDisplay);
			Assert.Equal (new Rect (0, 0, 0, 0), lbl.SuperView.NeedDisplay);
			Assert.True (lbl.SuperView.LayoutNeeded);
			lbl.SuperView.Redraw (lbl.SuperView.Bounds);
			Assert.Equal ("12  ", GetContents ());

			string GetContents ()
			{
				var text = "";
				for (int i = 0; i < 4; i++) {
					text += (char)Application.Driver.Contents [0, i, 0];
				}
				return text;
			}
		}

		[Fact, AutoInitShutdown]
		public void Width_Height_SetMinWidthHeight_Narrow_Wide_Runes ()
		{
			var text = $"First line{Environment.NewLine}Second line";
			var horizontalView = new View () {
				Width = 20,
				Text = text
			};
			var verticalView = new View () {
				Y = 3,
				Height = 20,
				Text = text,
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = "Window"
			};
			win.Add (horizontalView, verticalView);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (32, 32);

			Assert.False (horizontalView.AutoSize);
			Assert.False (verticalView.AutoSize);
			Assert.Equal (new Rect (0, 0, 20, 1), horizontalView.Frame);
			Assert.Equal (new Rect (0, 3, 1, 20), verticalView.Frame);
			var expected = @"
┌──────────────────────────────┐
│First line Second li          │
│                              │
│                              │
│F                             │
│i                             │
│r                             │
│s                             │
│t                             │
│                              │
│l                             │
│i                             │
│n                             │
│e                             │
│                              │
│S                             │
│e                             │
│c                             │
│o                             │
│n                             │
│d                             │
│                              │
│l                             │
│i                             │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
└──────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 32, 32), pos);

			verticalView.Text = $"最初の行{Environment.NewLine}二行目";
			Application.Top.Redraw (Application.Top.Bounds);
			Assert.Equal (new Rect (0, 3, 2, 20), verticalView.Frame);
			expected = @"
┌──────────────────────────────┐
│First line Second li          │
│                              │
│                              │
│最                            │
│初                            │
│の                            │
│行                            │
│                              │
│二                            │
│行                            │
│目                            │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
└──────────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 32, 32), pos);
		}

		[Fact, AutoInitShutdown]
		public void TextDirection_Toggle ()
		{
			var view = new View ();
			var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
			win.Add (view);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (22, 22);

			Assert.False (view.AutoSize);
			Assert.Equal (TextDirection.LeftRight_TopBottom, view.TextDirection);
			Assert.Equal (Rect.Empty, view.Frame);
			Assert.Equal ("Absolute(0)", view.X.ToString ());
			Assert.Equal ("Absolute(0)", view.Y.ToString ());
			Assert.Equal ("Absolute(0)", view.Width.ToString ());
			Assert.Equal ("Absolute(0)", view.Height.ToString ());
			var expected = @"
┌────────────────────┐
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);

			view.Text = "Hello World";
			view.Width = 11;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 11, 1), view.Frame);
			Assert.Equal ("Absolute(0)", view.X.ToString ());
			Assert.Equal ("Absolute(0)", view.Y.ToString ());
			Assert.Equal ("Absolute(11)", view.Width.ToString ());
			Assert.Equal ("Absolute(0)", view.Height.ToString ());
			expected = @"
┌────────────────────┐
│Hello World         │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);

			view.AutoSize = true;
			view.Text = "Hello Worlds";
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 12, 1), view.Frame);
			Assert.Equal ("Absolute(0)", view.X.ToString ());
			Assert.Equal ("Absolute(0)", view.Y.ToString ());
			Assert.Equal ("Absolute(11)", view.Width.ToString ());
			Assert.Equal ("Absolute(0)", view.Height.ToString ());
			expected = @"
┌────────────────────┐
│Hello Worlds        │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);

			view.TextDirection = TextDirection.TopBottom_LeftRight;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 11, 12), view.Frame);
			Assert.Equal ("Absolute(0)", view.X.ToString ());
			Assert.Equal ("Absolute(0)", view.Y.ToString ());
			Assert.Equal ("Absolute(11)", view.Width.ToString ());
			Assert.Equal ("Absolute(0)", view.Height.ToString ());
			expected = @"
┌────────────────────┐
│H                   │
│e                   │
│l                   │
│l                   │
│o                   │
│                    │
│W                   │
│o                   │
│r                   │
│l                   │
│d                   │
│s                   │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);

			view.AutoSize = false;
			view.Height = 1;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 11, 1), view.Frame);
			Assert.Equal ("Absolute(0)", view.X.ToString ());
			Assert.Equal ("Absolute(0)", view.Y.ToString ());
			Assert.Equal ("Absolute(11)", view.Width.ToString ());
			Assert.Equal ("Absolute(1)", view.Height.ToString ());
			expected = @"
┌────────────────────┐
│HelloWorlds         │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);

			view.PreserveTrailingSpaces = true;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 11, 1), view.Frame);
			Assert.Equal ("Absolute(0)", view.X.ToString ());
			Assert.Equal ("Absolute(0)", view.Y.ToString ());
			Assert.Equal ("Absolute(11)", view.Width.ToString ());
			Assert.Equal ("Absolute(1)", view.Height.ToString ());
			expected = @"
┌────────────────────┐
│Hello World         │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);

			view.PreserveTrailingSpaces = false;
			var f = view.Frame;
			view.Width = f.Height;
			view.Height = f.Width;
			view.TextDirection = TextDirection.TopBottom_LeftRight;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 1, 11), view.Frame);
			Assert.Equal ("Absolute(0)", view.X.ToString ());
			Assert.Equal ("Absolute(0)", view.Y.ToString ());
			Assert.Equal ("Absolute(1)", view.Width.ToString ());
			Assert.Equal ("Absolute(11)", view.Height.ToString ());
			expected = @"
┌────────────────────┐
│H                   │
│e                   │
│l                   │
│l                   │
│o                   │
│                    │
│W                   │
│o                   │
│r                   │
│l                   │
│d                   │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);

			view.AutoSize = true;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 1, 12), view.Frame);
			Assert.Equal ("Absolute(0)", view.X.ToString ());
			Assert.Equal ("Absolute(0)", view.Y.ToString ());
			Assert.Equal ("Absolute(1)", view.Width.ToString ());
			Assert.Equal ("Absolute(11)", view.Height.ToString ());
			expected = @"
┌────────────────────┐
│H                   │
│e                   │
│l                   │
│l                   │
│o                   │
│                    │
│W                   │
│o                   │
│r                   │
│l                   │
│d                   │
│s                   │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);
		}

		[Fact, AutoInitShutdown]
		public void Width_Height_AutoSize_True_Stay_True_If_TextFormatter_Size_Fit ()
		{
			var text = $"Fi_nish 終";
			var horizontalView = new View () {
				AutoSize = true,
				HotKeySpecifier = '_',
				Text = text
			};
			var verticalView = new View () {
				Y = 3,
				AutoSize = true,
				HotKeySpecifier = '_',
				Text = text,
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = "Window"
			};
			win.Add (horizontalView, verticalView);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (22, 22);

			Assert.True (horizontalView.AutoSize);
			Assert.True (verticalView.AutoSize);
			Assert.Equal (new Size (10, 1), horizontalView.TextFormatter.Size);
			Assert.Equal (new Size (2, 9), verticalView.TextFormatter.Size);
			Assert.Equal (new Rect (0, 0, 9, 1), horizontalView.Frame);
			Assert.Equal ("Absolute(0)", horizontalView.X.ToString ());
			Assert.Equal ("Absolute(0)", horizontalView.Y.ToString ());
			Assert.Equal ("Absolute(9)", horizontalView.Width.ToString ());
			Assert.Equal ("Absolute(1)", horizontalView.Height.ToString ());
			Assert.Equal (new Rect (0, 3, 2, 8), verticalView.Frame);
			Assert.Equal ("Absolute(0)", verticalView.X.ToString ());
			Assert.Equal ("Absolute(3)", verticalView.Y.ToString ());
			Assert.Equal ("Absolute(2)", verticalView.Width.ToString ());
			Assert.Equal ("Absolute(8)", verticalView.Height.ToString ());
			var expected = @"
┌────────────────────┐
│Finish 終           │
│                    │
│                    │
│F                   │
│i                   │
│n                   │
│i                   │
│s                   │
│h                   │
│                    │
│終                  │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);

			verticalView.Text = $"最初_の行二行目";
			Application.Top.Redraw (Application.Top.Bounds);
			Assert.True (horizontalView.AutoSize);
			Assert.True (verticalView.AutoSize);
			// height was initialized with 8 and is kept as minimum
			Assert.Equal (new Rect (0, 3, 2, 8), verticalView.Frame);
			Assert.Equal ("Absolute(0)", verticalView.X.ToString ());
			Assert.Equal ("Absolute(3)", verticalView.Y.ToString ());
			Assert.Equal ("Absolute(2)", verticalView.Width.ToString ());
			Assert.Equal ("Absolute(8)", verticalView.Height.ToString ());
			expected = @"
┌────────────────────┐
│Finish 終           │
│                    │
│                    │
│最                  │
│初                  │
│の                  │
│行                  │
│二                  │
│行                  │
│目                  │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 22, 22), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_Center_HotKeySpecifier ()
		{
			var btn = new Button () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = "Say He_llo 你"
			};

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.True (btn.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @"
┌ Test Demo 你 ──────────────┐
│                            │
│      [ Say Hello 你 ]      │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (btn.AutoSize);
			btn.Text = "Say He_llo 你 changed";
			Assert.True (btn.AutoSize);
			Application.Refresh ();
			expected = @"
┌ Test Demo 你 ──────────────┐
│                            │
│  [ Say Hello 你 changed ]  │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_Equal_Before_And_After_IsInitialized_With_Differents_Orders ()
		{
			var view1 = new View () { Text = "Say Hello view1 你", AutoSize = false, Width = 10, Height = 5 };
			var view2 = new View () { Text = "Say Hello view2 你", Width = 10, Height = 5, AutoSize = false };
			var view3 = new View () { AutoSize = false, Width = 10, Height = 5, Text = "Say Hello view3 你" };
			var view4 = new View () {
				Text = "Say Hello view4 你",
				AutoSize = false,
				Width = 10,
				Height = 5,
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			var view5 = new View () {
				Text = "Say Hello view5 你",
				Width = 10,
				Height = 5,
				AutoSize = false,
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			var view6 = new View () {
				AutoSize = false,
				Width = 10,
				Height = 5,
				TextDirection = TextDirection.TopBottom_LeftRight,
				Text = "Say Hello view6 你",
			};
			Application.Top.Add (view1, view2, view3, view4, view5, view6);

			Assert.False (view1.IsInitialized);
			Assert.False (view2.IsInitialized);
			Assert.False (view3.IsInitialized);
			Assert.False (view4.IsInitialized);
			Assert.False (view5.IsInitialized);
			Assert.False (view1.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view1.Frame);
			Assert.Equal ("Absolute(10)", view1.Width.ToString ());
			Assert.Equal ("Absolute(5)", view1.Height.ToString ());
			Assert.False (view2.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view2.Frame);
			Assert.Equal ("Absolute(10)", view2.Width.ToString ());
			Assert.Equal ("Absolute(5)", view2.Height.ToString ());
			Assert.False (view3.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view3.Frame);
			Assert.Equal ("Absolute(10)", view3.Width.ToString ());
			Assert.Equal ("Absolute(5)", view3.Height.ToString ());
			Assert.False (view4.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view4.Frame);
			Assert.Equal ("Absolute(10)", view4.Width.ToString ());
			Assert.Equal ("Absolute(5)", view4.Height.ToString ());
			Assert.False (view5.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view5.Frame);
			Assert.Equal ("Absolute(10)", view5.Width.ToString ());
			Assert.Equal ("Absolute(5)", view5.Height.ToString ());
			Assert.False (view6.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view6.Frame);
			Assert.Equal ("Absolute(10)", view6.Width.ToString ());
			Assert.Equal ("Absolute(5)", view6.Height.ToString ());

			Application.Begin (Application.Top);

			Assert.True (view1.IsInitialized);
			Assert.True (view2.IsInitialized);
			Assert.True (view3.IsInitialized);
			Assert.True (view4.IsInitialized);
			Assert.True (view5.IsInitialized);
			Assert.False (view1.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view1.Frame);
			Assert.Equal ("Absolute(10)", view1.Width.ToString ());
			Assert.Equal ("Absolute(5)", view1.Height.ToString ());
			Assert.False (view2.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view2.Frame);
			Assert.Equal ("Absolute(10)", view2.Width.ToString ());
			Assert.Equal ("Absolute(5)", view2.Height.ToString ());
			Assert.False (view3.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view3.Frame);
			Assert.Equal ("Absolute(10)", view3.Width.ToString ());
			Assert.Equal ("Absolute(5)", view3.Height.ToString ());
			Assert.False (view4.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view4.Frame);
			Assert.Equal ("Absolute(10)", view4.Width.ToString ());
			Assert.Equal ("Absolute(5)", view4.Height.ToString ());
			Assert.False (view5.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view5.Frame);
			Assert.Equal ("Absolute(10)", view5.Width.ToString ());
			Assert.Equal ("Absolute(5)", view5.Height.ToString ());
			Assert.False (view6.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 5), view6.Frame);
			Assert.Equal ("Absolute(10)", view6.Width.ToString ());
			Assert.Equal ("Absolute(5)", view6.Height.ToString ());
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_True_Equal_Before_And_After_IsInitialized_With_Differents_Orders ()
		{
			var view1 = new View () { Text = "Say Hello view1 你", AutoSize = true, Width = 10, Height = 5 };
			var view2 = new View () { Text = "Say Hello view2 你", Width = 10, Height = 5, AutoSize = true };
			var view3 = new View () { AutoSize = true, Width = 10, Height = 5, Text = "Say Hello view3 你" };
			var view4 = new View () {
				Text = "Say Hello view4 你",
				AutoSize = true,
				Width = 10,
				Height = 5,
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			var view5 = new View () {
				Text = "Say Hello view5 你",
				Width = 10,
				Height = 5,
				AutoSize = true,
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			var view6 = new View () {
				AutoSize = true,
				Width = 10,
				Height = 5,
				TextDirection = TextDirection.TopBottom_LeftRight,
				Text = "Say Hello view6 你",
			};
			Application.Top.Add (view1, view2, view3, view4, view5, view6);

			Assert.False (view1.IsInitialized);
			Assert.False (view2.IsInitialized);
			Assert.False (view3.IsInitialized);
			Assert.False (view4.IsInitialized);
			Assert.False (view5.IsInitialized);
			Assert.True (view1.AutoSize);
			Assert.Equal (new Rect (0, 0, 18, 5), view1.Frame);
			Assert.Equal ("Absolute(10)", view1.Width.ToString ());
			Assert.Equal ("Absolute(5)", view1.Height.ToString ());
			Assert.True (view2.AutoSize);
			Assert.Equal (new Rect (0, 0, 18, 5), view2.Frame);
			Assert.Equal ("Absolute(10)", view2.Width.ToString ());
			Assert.Equal ("Absolute(5)", view2.Height.ToString ());
			Assert.True (view3.AutoSize);
			Assert.Equal (new Rect (0, 0, 18, 5), view3.Frame);
			Assert.Equal ("Absolute(10)", view3.Width.ToString ());
			Assert.Equal ("Absolute(5)", view3.Height.ToString ());
			Assert.True (view4.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 17), view4.Frame);
			Assert.Equal ("Absolute(10)", view4.Width.ToString ());
			Assert.Equal ("Absolute(5)", view4.Height.ToString ());
			Assert.True (view5.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 17), view5.Frame);
			Assert.Equal ("Absolute(10)", view5.Width.ToString ());
			Assert.Equal ("Absolute(5)", view5.Height.ToString ());
			Assert.True (view6.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 17), view6.Frame);
			Assert.Equal ("Absolute(10)", view6.Width.ToString ());
			Assert.Equal ("Absolute(5)", view6.Height.ToString ());

			Application.Begin (Application.Top);

			Assert.True (view1.IsInitialized);
			Assert.True (view2.IsInitialized);
			Assert.True (view3.IsInitialized);
			Assert.True (view4.IsInitialized);
			Assert.True (view5.IsInitialized);
			Assert.True (view1.AutoSize);
			Assert.Equal (new Rect (0, 0, 18, 5), view1.Frame);
			Assert.Equal ("Absolute(10)", view1.Width.ToString ());
			Assert.Equal ("Absolute(5)", view1.Height.ToString ());
			Assert.True (view2.AutoSize);
			Assert.Equal (new Rect (0, 0, 18, 5), view2.Frame);
			Assert.Equal ("Absolute(10)", view2.Width.ToString ());
			Assert.Equal ("Absolute(5)", view2.Height.ToString ());
			Assert.True (view3.AutoSize);
			Assert.Equal (new Rect (0, 0, 18, 5), view3.Frame);
			Assert.Equal ("Absolute(10)", view3.Width.ToString ());
			Assert.Equal ("Absolute(5)", view3.Height.ToString ());
			Assert.True (view4.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 17), view4.Frame);
			Assert.Equal ("Absolute(10)", view4.Width.ToString ());
			Assert.Equal ("Absolute(5)", view4.Height.ToString ());
			Assert.True (view5.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 17), view5.Frame);
			Assert.Equal ("Absolute(10)", view5.Width.ToString ());
			Assert.Equal ("Absolute(5)", view5.Height.ToString ());
			Assert.True (view6.AutoSize);
			Assert.Equal (new Rect (0, 0, 10, 17), view6.Frame);
			Assert.Equal ("Absolute(10)", view6.Width.ToString ());
			Assert.Equal ("Absolute(5)", view6.Height.ToString ());
		}

		[Fact, AutoInitShutdown]
		public void Setting_Frame_Dont_Respect_AutoSize_True_On_Layout_Absolute ()
		{
			var view1 = new View (new Rect (0, 0, 10, 0)) { Text = "Say Hello view1 你", AutoSize = true };
			var view2 = new View (new Rect (0, 0, 0, 10)) {
				Text = "Say Hello view2 你",
				AutoSize = true,
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			Application.Top.Add (view1, view2);

			var rs = Application.Begin (Application.Top);

			Assert.True (view1.AutoSize);
			Assert.Equal (LayoutStyle.Absolute, view1.LayoutStyle);
			Assert.Equal (new Rect (0, 0, 18, 1), view1.Frame);
			Assert.Equal ("Absolute(0)", view1.X.ToString ());
			Assert.Equal ("Absolute(0)", view1.Y.ToString ());
			Assert.Equal ("Absolute(18)", view1.Width.ToString ());
			Assert.Equal ("Absolute(1)", view1.Height.ToString ());
			Assert.True (view2.AutoSize);
			Assert.Equal (LayoutStyle.Absolute, view2.LayoutStyle);
			Assert.Equal (new Rect (0, 0, 2, 17), view2.Frame);
			Assert.Equal ("Absolute(0)", view2.X.ToString ());
			Assert.Equal ("Absolute(0)", view2.Y.ToString ());
			Assert.Equal ("Absolute(2)", view2.Width.ToString ());
			Assert.Equal ("Absolute(17)", view2.Height.ToString ());

			view1.Frame = new Rect (0, 0, 25, 4);
			bool firstIteration = false;
			Application.RunMainLoopIteration (ref rs, true, ref firstIteration);

			Assert.True (view1.AutoSize);
			Assert.Equal (LayoutStyle.Absolute, view1.LayoutStyle);
			Assert.Equal (new Rect (0, 0, 25, 4), view1.Frame);
			Assert.Equal ("Absolute(0)", view1.X.ToString ());
			Assert.Equal ("Absolute(0)", view1.Y.ToString ());
			Assert.Equal ("Absolute(18)", view1.Width.ToString ());
			Assert.Equal ("Absolute(1)", view1.Height.ToString ());

			view2.Frame = new Rect (0, 0, 1, 25);
			Application.RunMainLoopIteration (ref rs, true, ref firstIteration);

			Assert.True (view2.AutoSize);
			Assert.Equal (LayoutStyle.Absolute, view2.LayoutStyle);
			Assert.Equal (new Rect (0, 0, 1, 25), view2.Frame);
			Assert.Equal ("Absolute(0)", view2.X.ToString ());
			Assert.Equal ("Absolute(0)", view2.Y.ToString ());
			Assert.Equal ("Absolute(2)", view2.Width.ToString ());
			Assert.Equal ("Absolute(17)", view2.Height.ToString ());
		}

		[Fact, AutoInitShutdown]
		public void Pos_Dim_Are_Null_If_Not_Initialized_On_Constructor_IsAdded_False ()
		{
			var top = Application.Top;
			var view1 = new View ();
			Assert.False (view1.IsAdded);
			Assert.Null (view1.X);
			Assert.Null (view1.Y);
			Assert.Null (view1.Width);
			Assert.Null (view1.Height);
			top.Add (view1);
			Assert.True (view1.IsAdded);
			Assert.Equal ("Absolute(0)", view1.X.ToString ());
			Assert.Equal ("Absolute(0)", view1.Y.ToString ());
			Assert.Equal ("Absolute(0)", view1.Width.ToString ());
			Assert.Equal ("Absolute(0)", view1.Height.ToString ());

			var view2 = new View () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			Assert.False (view2.IsAdded);
			Assert.Equal ("Center", view2.X.ToString ());
			Assert.Equal ("Center", view2.Y.ToString ());
			Assert.Equal ("Fill(0)", view2.Width.ToString ());
			Assert.Equal ("Fill(0)", view2.Height.ToString ());
			top.Add (view2);
			Assert.True (view2.IsAdded);
			Assert.Equal ("Center", view2.X.ToString ());
			Assert.Equal ("Center", view2.Y.ToString ());
			Assert.Equal ("Fill(0)", view2.Width.ToString ());
			Assert.Equal ("Fill(0)", view2.Height.ToString ());
		}

		[Fact]
		public void SetRelativeLayout_PosCombine_Center_Plus_Absolute ()
		{
			var superView = new View () {
				AutoSize = false,
				Width = 10,
				Height = 10
			};

			var testView = new View () {
				AutoSize = false,
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = 1,
				Height = 1
			};
			superView.Add (testView);
			testView.SetRelativeLayout (superView.Frame);
			Assert.Equal (4, testView.Frame.X);
			Assert.Equal (4, testView.Frame.Y);

			testView = new View () {
				AutoSize = false,
				X = Pos.Center () + 1,
				Y = Pos.Center () + 1,
				Width = 1,
				Height = 1
			};
			superView.Add (testView);
			testView.SetRelativeLayout (superView.Frame);
			Assert.Equal (5, testView.Frame.X);
			Assert.Equal (5, testView.Frame.Y);

			testView = new View () {
				AutoSize = false,
				X = 1 + Pos.Center (),
				Y = 1 + Pos.Center (),
				Width = 1,
				Height = 1
			};
			superView.Add (testView);
			testView.SetRelativeLayout (superView.Frame);
			Assert.Equal (5, testView.Frame.X);
			Assert.Equal (5, testView.Frame.Y);

			testView = new View () {
				AutoSize = false,
				X = 1 + Pos.Percent (50),
				Y = Pos.Percent (50) + 1,
				Width = 1,
				Height = 1
			};
			superView.Add (testView);
			testView.SetRelativeLayout (superView.Frame);
			Assert.Equal (6, testView.Frame.X);
			Assert.Equal (6, testView.Frame.Y);

			testView = new View () {
				AutoSize = false,
				X = Pos.Percent (10) + Pos.Percent (40),
				Y = Pos.Percent (10) + Pos.Percent (40),
				Width = 1,
				Height = 1
			};
			superView.Add (testView);
			testView.SetRelativeLayout (superView.Frame);
			Assert.Equal (5, testView.Frame.X);
			Assert.Equal (5, testView.Frame.Y);

			testView = new View () {
				AutoSize = false,
				X = 1 + Pos.Percent (10) + Pos.Percent (40) - 1,
				Y = 5 + Pos.Percent (10) + Pos.Percent (40) - 5,
				Width = 1,
				Height = 1
			};
			superView.Add (testView);
			testView.SetRelativeLayout (superView.Frame);
			Assert.Equal (5, testView.Frame.X);
			Assert.Equal (5, testView.Frame.Y);

			testView = new View () {
				AutoSize = false,
				X = Pos.Left(testView),
				Y = Pos.Left (testView),
				Width = 1,
				Height = 1
			};
			superView.Add (testView);
			testView.SetRelativeLayout (superView.Frame);
			Assert.Equal (5, testView.Frame.X);
			Assert.Equal (5, testView.Frame.Y);

			testView = new View () {
				AutoSize = false,
				X = 1 + Pos.Left (testView),
				Y = Pos.Top (testView) + 1,
				Width = 1,
				Height = 1
			};
			superView.Add (testView);
			testView.SetRelativeLayout (superView.Frame);
			Assert.Equal (6, testView.Frame.X);
			Assert.Equal (6, testView.Frame.Y);
		}
	}
}
