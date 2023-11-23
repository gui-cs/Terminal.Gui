using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Terminal.Gui.SpinnerStyle;


// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ApplicationTests;

public class MouseTests {
	readonly ITestOutputHelper _output;

	public MouseTests (ITestOutputHelper output)
	{
		this._output = output;
#if DEBUG_IDISPOSABLE
		Responder.Instances.Clear ();
		RunState.Instances.Clear ();
#endif
	}

	#region mouse coordinate tests
	// test Application.MouseEvent - ensure coordinates are screen relative
	[Theory]
	// inside tests
	[InlineData (0, 0, 0, 0, true)]
	[InlineData (1, 0, 1, 0, true)]
	[InlineData (0, 1, 0, 1, true)]
	[InlineData (9, 0, 9, 0, true)]
	[InlineData (0, 9, 0, 9, true)]

	// outside tests
	[InlineData (-1, -1, -1, -1, true)]
	[InlineData (0, -1, 0, -1, true)]
	[InlineData (-1, 0, -1, 0, true)]
	public void MouseEventCoordinatesAreScreenRelative (int clickX, int clickY, int expectedX, int expectedY, bool expectedClicked)
	{
		var mouseEvent = new MouseEvent () {
			X = clickX,
			Y = clickY,
			Flags = MouseFlags.Button1Pressed
		};
		var mouseEventArgs = new MouseEventEventArgs (mouseEvent);
		var clicked = false;

		void OnApplicationOnMouseEvent (object s, MouseEventEventArgs e)
		{
			Assert.Equal (expectedX, e.MouseEvent.X);
			Assert.Equal (expectedY, e.MouseEvent.Y);
			clicked = true;
		}
		Application.MouseEvent += OnApplicationOnMouseEvent;

		Application.OnMouseEvent (mouseEventArgs);
		Assert.Equal (expectedClicked, clicked);
		Application.MouseEvent -= OnApplicationOnMouseEvent;
	}

	/// <summary>
	/// Tests that the mouse coordinates passed to the focused view are correct when the mouse is clicked.
	/// No frames; Frame == Bounds
	/// </summary>
	[AutoInitShutdown]
	[Theory]
	// click inside view tests
	[InlineData (0, 0, 0, 0, 0, true)]
	[InlineData (0, 1, 0, 1, 0, true)]
	[InlineData (0, 0, 1, 0, 1, true)]
	[InlineData (0, 9, 0, 9, 0, true)]
	[InlineData (0, 0, 9, 0, 9, true)]

	// view is offset from origin ; click is inside view 
	[InlineData (1, 1, 1, 0, 0, true)]
	[InlineData (1, 2, 1, 1, 0, true)]
	[InlineData (1, 1, 2, 0, 1, true)]
	[InlineData (1, 9, 1, 8, 0, true)]
	[InlineData (1, 1, 9, 0, 8, true)]

	// click outside view tests
	[InlineData (0, -1, -1, 0, 0, false)]
	[InlineData (0, 0, -1, 0, 0, false)]
	[InlineData (0, -1, 0, 0, 0, false)]
	[InlineData (0, 0, 10, 0, 0, false)]
	[InlineData (0, 10, 0, 0, 0, false)]
	[InlineData (0, 10, 10, 0, 0, false)]

	// view is offset from origin ; click is outside view 
	[InlineData (1, 0, 0, 0, 0, false)]
	[InlineData (1, 1, 0, 0, 0, false)]
	[InlineData (1, 0, 1, 0, 0, false)]
	[InlineData (1, 9, 0, 0, 0, false)]
	[InlineData (1, 0, 9, 0, 0, false)]
	public void MouseCoordinatesTest_NoFrames (int offset, int clickX, int clickY, int expectedX, int expectedY, bool expectedClicked)
	{
		var size = new Size (10, 10);
		var pos = new Point (offset, offset);
		
		var clicked = false;
		Application.Top.X = pos.X;
		Application.Top.Y = pos.Y;
		Application.Top.Width = size.Width;
		Application.Top.Height = size.Height;

		var mouseEvent = new MouseEvent () {
			X = clickX,
			Y = clickY,
			Flags = MouseFlags.Button1Pressed
		};
		var mouseEventArgs = new MouseEventEventArgs (mouseEvent);

		Application.Top.MouseClick += (s, e) => {
			Assert.Equal (expectedX, e.MouseEvent.X);
			Assert.Equal (expectedY, e.MouseEvent.Y);
			clicked = true;
		};

		Application.OnMouseEvent (mouseEventArgs);
		Assert.Equal (expectedClicked, clicked);
	}
	
	/// <summary>
	/// Tests that the mouse coordinates passed to the focused view are correct when the mouse is clicked.
	/// No frames; Frame != Bounds
	/// </summary>
	[AutoInitShutdown]
	[Theory]
	// click inside view tests
	[InlineData (0, 0, 0, 0, 0, true)]
	[InlineData (0, 1, 0, 1, 0, true)]
	[InlineData (0, 0, 1, 0, 1, true)]
	[InlineData (0, 9, 0, 9, 0, true)]
	[InlineData (0, 0, 9, 0, 9, true)]

	// view is offset from origin ; click is inside view 
	[InlineData (1, 1, 1, 0, 0, true)]
	[InlineData (1, 2, 1, 1, 0, true)]
	[InlineData (1, 1, 2, 0, 1, true)]
	[InlineData (1, 9, 1, 8, 0, true)]
	[InlineData (1, 1, 9, 0, 8, true)]

	// click outside view tests
	[InlineData (0, -1, -1, 0, 0, false)]
	[InlineData (0, 0, -1, 0, 0, false)]
	[InlineData (0, -1, 0, 0, 0, false)]
	[InlineData (0, 0, 10, 0, 0, false)]
	[InlineData (0, 10, 0, 0, 0, false)]
	[InlineData (0, 10, 10, 0, 0, false)]

	// view is offset from origin ; click is outside view 
	[InlineData (1, 0, 0, 0, 0, false)]
	[InlineData (1, 1, 0, 0, 0, false)]
	[InlineData (1, 0, 1, 0, 0, false)]
	[InlineData (1, 9, 0, 0, 0, false)]
	[InlineData (1, 0, 9, 0, 0, false)]
	public void MouseCoordinatesTest_Border (int offset, int clickX, int clickY, int expectedX, int expectedY, bool expectedClicked)
	{
		var size = new Size (10, 10);
		var pos = new Point (offset, offset);

		var clicked = false;
		Application.Top.X = pos.X;
		Application.Top.Y = pos.Y;
		Application.Top.Width = size.Width;
		Application.Top.Height = size.Height;

		// Give the view a border. With PR #2920, mouse clicks are only passed if they are inside the view's Bounds.
		Application.Top.BorderStyle = LineStyle.Single;

		var mouseEvent = new MouseEvent () {
			X = clickX,
			Y = clickY,
			Flags = MouseFlags.Button1Pressed
		};
		var mouseEventArgs = new MouseEventEventArgs (mouseEvent);

		Application.Top.MouseClick += (s, e) => {
			Assert.Equal (expectedX, e.MouseEvent.X);
			Assert.Equal (expectedY, e.MouseEvent.Y);
			clicked = true;
		};

		Application.OnMouseEvent (mouseEventArgs);
		Assert.Equal (expectedClicked, clicked);
	}
	#endregion mouse coordinate tests 

	#region mouse grab tests
	[Fact, AutoInitShutdown]
	public void MouseGrabView_WithNullMouseEventView ()
	{
		var tf = new TextField () { Width = 10 };
		var sv = new ScrollView () {
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			ContentSize = new Size (100, 100)
		};

		sv.Add (tf);
		Application.Top.Add (sv);

		var iterations = -1;

		Application.Iteration += (s, a) => {
			iterations++;
			if (iterations == 0) {
				Assert.True (tf.HasFocus);
				Assert.Null (Application.MouseGrabView);

				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () {
					X = 5,
					Y = 5,
					Flags = MouseFlags.ReportMousePosition
				}));

				Assert.Equal (sv, Application.MouseGrabView);

				MessageBox.Query ("Title", "Test", "Ok");

				Assert.Null (Application.MouseGrabView);
			} else if (iterations == 1) {
				// Application.MouseGrabView is null because
				// another toplevel (Dialog) was opened
				Assert.Null (Application.MouseGrabView);

				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () {
					X = 5,
					Y = 5,
					Flags = MouseFlags.ReportMousePosition
				}));

				Assert.Null (Application.MouseGrabView);

				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () {
					X = 40,
					Y = 12,
					Flags = MouseFlags.ReportMousePosition
				}));

				Assert.Null (Application.MouseGrabView);

				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () {
					X = 0,
					Y = 0,
					Flags = MouseFlags.Button1Pressed
				}));

				Assert.Null (Application.MouseGrabView);

				Application.RequestStop ();
			} else if (iterations == 2) {
				Assert.Null (Application.MouseGrabView);

				Application.RequestStop ();
			}
		};

		Application.Run ();
	}

	[Fact, AutoInitShutdown]
	public void MouseGrabView_GrabbedMouse_UnGrabbedMouse ()
	{
		View grabView = null;
		var count = 0;

		var view1 = new View ();
		var view2 = new View ();

		Application.GrabbedMouse += Application_GrabbedMouse;
		Application.UnGrabbedMouse += Application_UnGrabbedMouse;

		Application.GrabMouse (view1);
		Assert.Equal (0, count);
		Assert.Equal (grabView, view1);
		Assert.Equal (view1, Application.MouseGrabView);

		Application.UngrabMouse ();
		Assert.Equal (1, count);
		Assert.Equal (grabView, view1);
		Assert.Null (Application.MouseGrabView);

		Application.GrabbedMouse += Application_GrabbedMouse;
		Application.UnGrabbedMouse += Application_UnGrabbedMouse;

		Application.GrabMouse (view2);
		Assert.Equal (1, count);
		Assert.Equal (grabView, view2);
		Assert.Equal (view2, Application.MouseGrabView);

		Application.UngrabMouse ();
		Assert.Equal (2, count);
		Assert.Equal (grabView, view2);
		Assert.Null (Application.MouseGrabView);

		void Application_GrabbedMouse (object sender, ViewEventArgs e)
		{
			if (count == 0) {
				Assert.Equal (view1, e.View);
				grabView = view1;
			} else {
				Assert.Equal (view2, e.View);
				grabView = view2;
			}

			Application.GrabbedMouse -= Application_GrabbedMouse;
		}

		void Application_UnGrabbedMouse (object sender, ViewEventArgs e)
		{
			if (count == 0) {
				Assert.Equal (view1, e.View);
				Assert.Equal (grabView, e.View);
			} else {
				Assert.Equal (view2, e.View);
				Assert.Equal (grabView, e.View);
			}
			count++;

			Application.UnGrabbedMouse -= Application_UnGrabbedMouse;
		}
	}
	#endregion
}