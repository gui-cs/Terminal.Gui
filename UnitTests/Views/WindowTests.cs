﻿using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests; 

public class WindowTests {
	readonly ITestOutputHelper _output;

	public WindowTests (ITestOutputHelper output) => this._output = output;

	[Fact]
	public void New_Initializes ()
	{
		// Parameterless
		var r = new Window ();
		Assert.NotNull (r);
		Assert.Equal (string.Empty,         r.Title);
		Assert.Equal (LayoutStyle.Computed, r.LayoutStyle);
		Assert.Equal ("Window()(0,0,0,0)",  r.ToString ());
		Assert.True (r.CanFocus);
		Assert.False (r.HasFocus);
		Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
		Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
		Assert.Null (r.Focused);
		Assert.NotNull (r.ColorScheme);
		Assert.Equal (Dim.Fill (), r.Width);
		Assert.Equal (Dim.Fill (), r.Height);
		Assert.Null (r.X);
		Assert.Null (r.Y);
		Assert.False (r.IsCurrentTop);
		Assert.Empty (r.Id);
		Assert.False (r.WantContinuousButtonPressed);
		Assert.False (r.WantMousePositionReports);
		Assert.Null (r.SuperView);
		Assert.Null (r.MostFocused);
		Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

		// Empty Rect
		r = new Window (Rect.Empty) { Title = "title" };
		Assert.NotNull (r);
		Assert.Equal ("title",                  r.Title);
		Assert.Equal (LayoutStyle.Absolute,     r.LayoutStyle);
		Assert.Equal ("Window(title)(0,0,0,0)", r.ToString ());
		Assert.True (r.CanFocus);
		Assert.False (r.HasFocus);
		Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
		Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
		Assert.Null (r.Focused);
		Assert.NotNull (r.ColorScheme);
		Assert.Null (r.Width);  // All view Dim are initialized now in the IsAdded setter,
		Assert.Null (r.Height); // avoiding Dim errors.
		Assert.Null (r.X);      // All view Pos are initialized now in the IsAdded setter,
		Assert.Null (r.Y);      // avoiding Pos errors.
		Assert.False (r.IsCurrentTop);
		Assert.Equal (r.Title, r.Id);
		Assert.False (r.WantContinuousButtonPressed);
		Assert.False (r.WantMousePositionReports);
		Assert.Null (r.SuperView);
		Assert.Null (r.MostFocused);
		Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

		// Rect with values
		r = new Window (new Rect (1, 2, 3, 4)) { Title = "title" };
		Assert.Equal ("title", r.Title);
		Assert.NotNull (r);
		Assert.Equal (LayoutStyle.Absolute,     r.LayoutStyle);
		Assert.Equal ("Window(title)(1,2,3,4)", r.ToString ());
		Assert.True (r.CanFocus);
		Assert.False (r.HasFocus);
		Assert.Equal (new Rect (0, 0, 1, 2), r.Bounds);
		Assert.Equal (new Rect (1, 2, 3, 4), r.Frame);
		Assert.Null (r.Focused);
		Assert.NotNull (r.ColorScheme);
		Assert.Null (r.Width);
		Assert.Null (r.Height);
		Assert.Null (r.X);
		Assert.Null (r.Y);
		Assert.False (r.IsCurrentTop);
		Assert.Equal (r.Title, r.Id);
		Assert.False (r.WantContinuousButtonPressed);
		Assert.False (r.WantMousePositionReports);
		Assert.Null (r.SuperView);
		Assert.Null (r.MostFocused);
		Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
		r.Dispose ();
	}

	[Fact] [AutoInitShutdown]
	public void MenuBar_And_StatusBar_Inside_Window ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new ("File", new MenuItem [] {
				new ("Open", "", null),
				new ("Quit", "", null)
			}),
			new ("Edit", new MenuItem [] {
				new ("Copy", "", null)
			})
		});

		var sb = new StatusBar (new StatusItem [] {
			new (KeyCode.CtrlMask | KeyCode.Q, "~^Q~ Quit", null),
			new (KeyCode.CtrlMask | KeyCode.O, "~^O~ Open", null),
			new (KeyCode.CtrlMask | KeyCode.C, "~^C~ Copy", null)
		});

		var fv = new FrameView ("Frame View") {
			Y = 1,
			Width = Dim.Fill (),
			Height = Dim.Fill (1)
		};
		var win = new Window ();
		win.Add (menu, sb, fv);
		var top = Application.Top;
		top.Add (win);
		Application.Begin (top);
		((FakeDriver)Application.Driver).SetBufferSize (20, 10);

		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│ File  Edit       │
│┌┤Frame View├────┐│
││                ││
││                ││
││                ││
││                ││
│└────────────────┘│
│ ^Q Quit │ ^O Open│
└──────────────────┘", _output);

		((FakeDriver)Application.Driver).SetBufferSize (40, 20);

		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌┤Frame View├────────────────────────┐│
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
│└────────────────────────────────────┘│
│ ^Q Quit │ ^O Open │ ^C Copy          │
└──────────────────────────────────────┘", _output);

		((FakeDriver)Application.Driver).SetBufferSize (20, 10);

		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│ File  Edit       │
│┌┤Frame View├────┐│
││                ││
││                ││
││                ││
││                ││
│└────────────────┘│
│ ^Q Quit │ ^O Open│
└──────────────────┘", _output);
	}

	[Fact] [AutoInitShutdown]
	public void OnCanFocusChanged_Only_Must_ContentView_Forces_SetFocus_After_IsInitialized_Is_True ()
	{
		var win1 = new Window { Id = "win1", Width = 10, Height = 1 };
		var view1 = new View { Id = "view1", Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
		var win2 = new Window { Id = "win2", Y = 6, Width = 10, Height = 1 };
		var view2 = new View { Id = "view2", Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
		win2.Add (view2);
		win1.Add (view1, win2);

		Application.Begin (win1);

		Assert.True (win1.HasFocus);
		Assert.True (view1.HasFocus);
		Assert.False (win2.HasFocus);
		Assert.False (view2.HasFocus);
	}

	[Fact] [AutoInitShutdown]
	public void Activating_MenuBar_By_Alt_Key_Does_Not_Throw ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new ("Child", new MenuItem [] {
				new ("_Create Child", "", null)
			})
		});
		var win = new Window ();
		win.Add (menu);
		Application.Top.Add (win);
		Application.Begin (Application.Top);

		var exception = Record.Exception (() => win.NewKeyDownEvent (new Key (KeyCode.AltMask)));
		Assert.Null (exception);
	}

	public static TheoryData<Toplevel> ButtonContainers =>
		new () {
			{ new Window () },
			{ new Dialog () }
		};


	[Theory, AutoInitShutdown]
	[MemberData (nameof (ButtonContainers))]
	public void With_Default_Button_Enter_Invokes_Accept_Action (Toplevel container)
	{
		var view = new View () { CanFocus = true };
		var btnOk = new Button ("Accept") { IsDefault = true };
		btnOk.Clicked += (s, e) => view.Text = "Test";
		var btnCancel = new Button ("Cancel");
		btnCancel.Clicked += (s, e) => view.Text = "";

		container.Add (view, btnOk, btnCancel);
		var rs = Application.Begin (container);

		Assert.True (view.HasFocus);
		Assert.Equal ("", view.Text);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Enter)));
		Assert.True (view.HasFocus);
		Assert.Equal ("Test", view.Text);

		btnOk.IsDefault = false;
		btnCancel.IsDefault = true;
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Enter)));
		Assert.True (view.HasFocus);
		Assert.Equal ("", view.Text);
		Application.End (rs);
	}
}