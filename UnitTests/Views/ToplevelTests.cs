using System;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ToplevelTests {
	readonly ITestOutputHelper _output;

	public ToplevelTests (ITestOutputHelper output) => _output = output;

	[Fact]
	[AutoInitShutdown]
	public void Constructor_Default ()
	{
		var top = new Toplevel ();

		Assert.Equal (Colors.ColorSchemes ["TopLevel"], top.ColorScheme);
		Assert.Equal ("Fill(0)", top.Width.ToString ());
		Assert.Equal ("Fill(0)", top.Height.ToString ());
		Assert.False (top.Running);
		Assert.False (top.Modal);
		Assert.Null (top.MenuBar);
		Assert.Null (top.StatusBar);
		Assert.False (top.IsOverlappedContainer);
		Assert.False (top.IsOverlapped);
	}


#if BROKE_IN_2927
		// BUGBUG: The name of this test does not match what it does. 
		[Fact]
		[AutoInitShutdown]
		public void Application_Top_GetLocationThatFits_To_Driver_Rows_And_Cols ()
		{
			var iterations = 0;

			Application.Iteration += (s, a) => {
				switch (iterations) {
				case 0:
					Assert.False (Application.Top.AutoSize);
					Assert.Equal ("Top1", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.OnKeyPressed (new (Key.CtrlMask | Key.R));
					break;
				case 1:
					Assert.Equal ("Top2", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.OnKeyPressed (new (Key.CtrlMask | Key.C));
					break;
				case 3:
					Assert.Equal ("Top1", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.OnKeyPressed (new (Key.CtrlMask | Key.R));
					break;
				case 4:
					Assert.Equal ("Top2", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.OnKeyPressed (new (Key.CtrlMask | Key.C));
					break;
				case 6:
					Assert.Equal ("Top1", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.OnKeyPressed (new (Key.CtrlMask | Key.Q));
					break;
				}
				iterations++;
			};

			Application.Run (Top1 ());

			Toplevel Top1 ()
			{
				var top = Application.Top;
				top.Text = "Top1";
				var menu = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_Options", new MenuItem [] {
						new MenuItem ("_Run Top2", "", () => Application.Run (Top2 ()), null, null, Key.CtrlMask | Key.R),
						new MenuItem ("_Quit", "", () => Application.RequestStop(), null, null, Key.CtrlMask | Key.Q)
					})
				});
				top.Add (menu);

				var statusBar = new StatusBar (new [] {
					new StatusItem(Key.CtrlMask | Key.R, "~^R~ Run Top2", () => Application.Run (Top2 ())),
					new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Application.RequestStop())
				});
				top.Add (statusBar);

				var t1 = new Toplevel ();
				top.Add (t1);

				return top;
			}

			Toplevel Top2 ()
			{
				var top = new Toplevel (Application.Top.Frame);
				top.Text = "Top2";
				var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
				var menu = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_Stage", new MenuItem [] {
						new MenuItem ("_Close", "", () => Application.RequestStop(), null, null, Key.CtrlMask | Key.C)
					})
				});
				top.Add (menu);

				var statusBar = new StatusBar (new [] {
					new StatusItem(Key.CtrlMask | Key.C, "~^C~ Close", () => Application.RequestStop()),
				});
				top.Add (statusBar);

				win.Add (new ListView () {
					X = 0,
					Y = 0,
					Width = Dim.Fill (),
					Height = Dim.Fill ()
				});
				top.Add (win);

				return top;
			}
		}
#endif
	[Fact]
	[AutoInitShutdown]
	public void Internal_Tests ()
	{
		var top = new Toplevel ();

		var eventInvoked = "";

		top.ChildUnloaded += (s, e) => eventInvoked = "ChildUnloaded";
		top.OnChildUnloaded (top);
		Assert.Equal ("ChildUnloaded", eventInvoked);
		top.ChildLoaded += (s, e) => eventInvoked = "ChildLoaded";
		top.OnChildLoaded (top);
		Assert.Equal ("ChildLoaded", eventInvoked);
		top.Closed += (s, e) => eventInvoked = "Closed";
		top.OnClosed (top);
		Assert.Equal ("Closed", eventInvoked);
		top.Closing += (s, e) => eventInvoked = "Closing";
		top.OnClosing (new ToplevelClosingEventArgs (top));
		Assert.Equal ("Closing", eventInvoked);
		top.AllChildClosed += (s, e) => eventInvoked = "AllChildClosed";
		top.OnAllChildClosed ();
		Assert.Equal ("AllChildClosed", eventInvoked);
		top.ChildClosed += (s, e) => eventInvoked = "ChildClosed";
		top.OnChildClosed (top);
		Assert.Equal ("ChildClosed", eventInvoked);
		top.Deactivate += (s, e) => eventInvoked = "Deactivate";
		top.OnDeactivate (top);
		Assert.Equal ("Deactivate", eventInvoked);
		top.Activate += (s, e) => eventInvoked = "Activate";
		top.OnActivate (top);
		Assert.Equal ("Activate", eventInvoked);
		top.Loaded += (s, e) => eventInvoked = "Loaded";
		top.OnLoaded ();
		Assert.Equal ("Loaded", eventInvoked);
		top.Ready += (s, e) => eventInvoked = "Ready";
		top.OnReady ();
		Assert.Equal ("Ready", eventInvoked);
		top.Unloaded += (s, e) => eventInvoked = "Unloaded";
		top.OnUnloaded ();
		Assert.Equal ("Unloaded", eventInvoked);

		top.AddMenuStatusBar (new MenuBar ());
		Assert.NotNull (top.MenuBar);
		top.AddMenuStatusBar (new StatusBar ());
		Assert.NotNull (top.StatusBar);
		top.RemoveMenuStatusBar (top.MenuBar);
		Assert.Null (top.MenuBar);
		top.RemoveMenuStatusBar (top.StatusBar);
		Assert.Null (top.StatusBar);

		Application.Begin (top);
		Assert.Equal (top, Application.Top);

		// Application.Top without menu and status bar.
		var supView = top.GetLocationThatFits (top, 2, 2, out var nx, out var ny, out var mb, out var sb);
		Assert.Equal (Application.Top, supView);
		Assert.Equal (0, nx);
		Assert.Equal (0, ny);
		Assert.Null (mb);
		Assert.Null (sb);

		top.AddMenuStatusBar (new MenuBar ());
		Assert.NotNull (top.MenuBar);

		// Application.Top with a menu and without status bar.
		top.GetLocationThatFits (top, 2, 2, out nx, out ny, out mb, out sb);
		Assert.Equal (0, nx);
		Assert.Equal (1, ny);
		Assert.NotNull (mb);
		Assert.Null (sb);

		top.AddMenuStatusBar (new StatusBar ());
		Assert.NotNull (top.StatusBar);

		// Application.Top with a menu and status bar.
		top.GetLocationThatFits (top, 2, 2, out nx, out ny, out mb, out sb);
		Assert.Equal (0, nx);
		// The available height is lower than the Application.Top height minus
		// the menu bar and status bar, then the top can go beyond the bottom
		Assert.Equal (2, ny);
		Assert.NotNull (mb);
		Assert.NotNull (sb);

		top.RemoveMenuStatusBar (top.MenuBar);
		Assert.Null (top.MenuBar);

		// Application.Top without a menu and with a status bar.
		top.GetLocationThatFits (top, 2, 2, out nx, out ny, out mb, out sb);
		Assert.Equal (0, nx);
		// The available height is lower than the Application.Top height minus
		// the status bar, then the top can go beyond the bottom
		Assert.Equal (2, ny);
		Assert.Null (mb);
		Assert.NotNull (sb);

		top.RemoveMenuStatusBar (top.StatusBar);
		Assert.Null (top.StatusBar);
		Assert.Null (top.MenuBar);

		var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
		top.Add (win);
		top.LayoutSubviews ();

		// The SuperView is always the same regardless of the caller.
		supView = top.GetLocationThatFits (win, 0, 0, out nx, out ny, out mb, out sb);
		Assert.Equal (Application.Top, supView);
		supView = win.GetLocationThatFits (win, 0, 0, out nx, out ny, out mb, out sb);
		Assert.Equal (Application.Top, supView);

		// Application.Top without menu and status bar.
		top.GetLocationThatFits (win, 0, 0, out nx, out ny, out mb, out sb);
		Assert.Equal (0, nx);
		Assert.Equal (0, ny);
		Assert.Null (mb);
		Assert.Null (sb);

		top.AddMenuStatusBar (new MenuBar ());
		Assert.NotNull (top.MenuBar);

		// Application.Top with a menu and without status bar.
		top.GetLocationThatFits (win, 2, 2, out nx, out ny, out mb, out sb);
		Assert.Equal (0, nx);
		Assert.Equal (1, ny);
		Assert.NotNull (mb);
		Assert.Null (sb);

		top.AddMenuStatusBar (new StatusBar ());
		Assert.NotNull (top.StatusBar);

		// Application.Top with a menu and status bar.
		top.GetLocationThatFits (win, 30, 20, out nx, out ny, out mb, out sb);
		Assert.Equal (0, nx);
		// The available height is lower than the Application.Top height minus
		// the menu bar and status bar, then the top can go beyond the bottom
		Assert.Equal (20, ny);
		Assert.NotNull (mb);
		Assert.NotNull (sb);

		top.RemoveMenuStatusBar (top.MenuBar);
		top.RemoveMenuStatusBar (top.StatusBar);
		Assert.Null (top.StatusBar);
		Assert.Null (top.MenuBar);

		top.Remove (win);

		win = new Window { Width = 60, Height = 15 };
		top.Add (win);

		// Application.Top without menu and status bar.
		top.GetLocationThatFits (win, 0, 0, out nx, out ny, out mb, out sb);
		Assert.Equal (0, nx);
		Assert.Equal (0, ny);
		Assert.Null (mb);
		Assert.Null (sb);

		top.AddMenuStatusBar (new MenuBar ());
		Assert.NotNull (top.MenuBar);

		// Application.Top with a menu and without status bar.
		top.GetLocationThatFits (win, 2, 2, out nx, out ny, out mb, out sb);
		Assert.Equal (2, nx);
		Assert.Equal (2, ny);
		Assert.NotNull (mb);
		Assert.Null (sb);

		top.AddMenuStatusBar (new StatusBar ());
		Assert.NotNull (top.StatusBar);

		// Application.Top with a menu and status bar.
		top.GetLocationThatFits (win, 30, 20, out nx, out ny, out mb, out sb);
		Assert.Equal (20, nx); // 20+60=80
		Assert.Equal (9, ny); // 9+15+1(mb)=25
		Assert.NotNull (mb);
		Assert.NotNull (sb);

		top.PositionToplevels ();
		Assert.Equal (new Rect (0, 1, 60, 15), win.Frame);

		Assert.Null (Toplevel._dragPosition);
		win.MouseEvent (new MouseEvent { X = 6, Y = 0, Flags = MouseFlags.Button1Pressed });
		Assert.Equal (new Point (6, 0), Toplevel._dragPosition);
		win.MouseEvent (new MouseEvent { X = 6, Y = 0, Flags = MouseFlags.Button1Released });
		Assert.Null (Toplevel._dragPosition);
		win.CanFocus = false;
		win.MouseEvent (new MouseEvent { X = 6, Y = 0, Flags = MouseFlags.Button1Pressed });
		Assert.Null (Toplevel._dragPosition);
	}

	[Fact]
	[AutoInitShutdown]
	public void KeyBindings_Command ()
	{
		var isRunning = false;

		var win1 = new Window { Id = "win1", Width = Dim.Percent (50f), Height = Dim.Fill () };
		var lblTf1W1 = new Label ("Enter text in TextField on Win1:") { Id = "lblTf1W1" };
		var tf1W1 = new TextField ("Text1 on Win1") { Id = "tf1W1", X = Pos.Right (lblTf1W1) + 1, Width = Dim.Fill () };
		var lblTvW1 = new Label ("Enter text in TextView on Win1:") { Id = "lblTvW1", Y = Pos.Bottom (lblTf1W1) + 1 };
		var tvW1 = new TextView { Id = "tvW1", X = Pos.Left (tf1W1), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win1" };
		var lblTf2W1 = new Label ("Enter text in TextField on Win1:") { Id = "lblTf2W1", Y = Pos.Bottom (lblTvW1) + 1 };
		var tf2W1 = new TextField ("Text2 on Win1") { Id = "tf2W1", X = Pos.Left (tf1W1), Width = Dim.Fill () };
		win1.Add (lblTf1W1, tf1W1, lblTvW1, tvW1, lblTf2W1, tf2W1);

		var win2 = new Window { Id = "win2", X = Pos.Right (win1) + 1, Width = Dim.Percent (50f), Height = Dim.Fill () };
		var lblTf1W2 = new Label ("Enter text in TextField on Win2:") { Id = "lblTf1W2" };
		var tf1W2 = new TextField ("Text1 on Win2") { Id = "tf1W2", X = Pos.Right (lblTf1W2) + 1, Width = Dim.Fill () };
		var lblTvW2 = new Label ("Enter text in TextView on Win2:") { Id = "lblTvW2", Y = Pos.Bottom (lblTf1W2) + 1 };
		var tvW2 = new TextView { Id = "tvW2", X = Pos.Left (tf1W2), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win2" };
		var lblTf2W2 = new Label ("Enter text in TextField on Win2:") { Id = "lblTf2W2", Y = Pos.Bottom (lblTvW2) + 1 };
		var tf2W2 = new TextField ("Text2 on Win2") { Id = "tf2W2", X = Pos.Left (tf1W2), Width = Dim.Fill () };
		win2.Add (lblTf1W2, tf1W2, lblTvW2, tvW2, lblTf2W2, tf2W2);

		var top = Application.Top;
		top.Add (win1, win2);
		top.Loaded += (s, e) => isRunning = true;
		top.Closing += (s, e) => isRunning = false;
		Application.Begin (top);
		top.Running = true;

		Assert.Equal (new Rect (0, 0, 40, 25), win1.Frame);
		Assert.Equal (new Rect (41, 0, 40, 25), win2.Frame);
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf1W1, top.MostFocused);

		Assert.True (isRunning);
		Assert.True (Application.OnKeyDown (Application.QuitKey));
		Assert.False (isRunning);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Z | KeyCode.CtrlMask)));

		Assert.True (Application.OnKeyDown (new Key (KeyCode.F5))); // refresh

		Assert.True (Application.OnKeyDown (new Key (KeyCode.Tab)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tvW1, top.MostFocused);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Tab)));
		Assert.Equal ($"\tFirst line Win1{Environment.NewLine}Second line Win1", tvW1.Text);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Tab | KeyCode.ShiftMask)));
		Assert.Equal ($"First line Win1{Environment.NewLine}Second line Win1", tvW1.Text);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Tab | KeyCode.CtrlMask)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf2W1, top.MostFocused);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Tab)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf1W1, top.MostFocused);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.CursorRight)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf1W1, top.MostFocused);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.CursorDown)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tvW1, top.MostFocused);
#if UNIX_KEY_BINDINGS
			Assert.True (Application.OnKeyDown (new (Key.I | Key.CtrlMask)));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf2W1, top.MostFocused);
#endif
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Tab | KeyCode.ShiftMask)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tvW1, top.MostFocused);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.CursorLeft)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf1W1, top.MostFocused);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.CursorUp)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf2W1, top.MostFocused);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Tab | KeyCode.CtrlMask)));
		Assert.Equal (win2, top.Focused);
		Assert.Equal (tf1W2, top.MostFocused);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.Tab | KeyCode.CtrlMask | KeyCode.ShiftMask)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf2W1, top.MostFocused);
		Assert.True (Application.OnKeyDown (Application.AlternateForwardKey));
		Assert.Equal (win2, top.Focused);
		Assert.Equal (tf1W2, top.MostFocused);
		Assert.True (Application.OnKeyDown (Application.AlternateBackwardKey));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf2W1, top.MostFocused);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.CursorUp)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tvW1, top.MostFocused);
#if UNIX_KEY_BINDINGS
			Assert.True (Application.OnKeyDown (new (Key.B | Key.CtrlMask)));
#else
		Assert.True (Application.OnKeyDown (new Key (KeyCode.CursorLeft)));
#endif
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf1W1, top.MostFocused);

		Assert.True (Application.OnKeyDown (new Key (KeyCode.CursorDown)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tvW1, top.MostFocused);
		Assert.Equal (new Point (0, 0), tvW1.CursorPosition);
		Assert.True (Application.OnKeyDown (new Key (KeyCode.End | KeyCode.CtrlMask)));
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tvW1, top.MostFocused);
		Assert.Equal (new Point (16, 1), tvW1.CursorPosition);
#if UNIX_KEY_BINDINGS
			Assert.True (Application.OnKeyDown (new (Key.F | Key.CtrlMask)));
#else
		Assert.True (Application.OnKeyDown (new Key (KeyCode.CursorRight)));
#endif
		Assert.Equal (win1, top.Focused);
		Assert.Equal (tf2W1, top.MostFocused);

#if UNIX_KEY_BINDINGS
			Assert.True (Application.OnKeyDown (new (Key.L | Key.CtrlMask)));
#else
		Assert.True (Application.OnKeyDown (new Key (KeyCode.F5)));
#endif
	}

	[Fact]
	[AutoInitShutdown]
	public void KeyBindings_Command_With_OverlappedTop ()
	{
		var top = Application.Top;
		Assert.Null (Application.OverlappedTop);
		top.IsOverlappedContainer = true;
		Application.Begin (top);
		Assert.Equal (Application.Top, Application.OverlappedTop);

		var isRunning = true;

		var win1 = new Window { Id = "win1", Width = Dim.Percent (50f), Height = Dim.Fill () };
		var lblTf1W1 = new Label ("Enter text in TextField on Win1:");
		var tf1W1 = new TextField ("Text1 on Win1") { X = Pos.Right (lblTf1W1) + 1, Width = Dim.Fill () };
		var lblTvW1 = new Label ("Enter text in TextView on Win1:") { Y = Pos.Bottom (lblTf1W1) + 1 };
		var tvW1 = new TextView { X = Pos.Left (tf1W1), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win1" };
		var lblTf2W1 = new Label ("Enter text in TextField on Win1:") { Y = Pos.Bottom (lblTvW1) + 1 };
		var tf2W1 = new TextField ("Text2 on Win1") { X = Pos.Left (tf1W1), Width = Dim.Fill () };
		win1.Add (lblTf1W1, tf1W1, lblTvW1, tvW1, lblTf2W1, tf2W1);

		var win2 = new Window { Id = "win2", Width = Dim.Percent (50f), Height = Dim.Fill () };
		var lblTf1W2 = new Label ("Enter text in TextField on Win2:");
		var tf1W2 = new TextField ("Text1 on Win2") { X = Pos.Right (lblTf1W2) + 1, Width = Dim.Fill () };
		var lblTvW2 = new Label ("Enter text in TextView on Win2:") { Y = Pos.Bottom (lblTf1W2) + 1 };
		var tvW2 = new TextView { X = Pos.Left (tf1W2), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win2" };
		var lblTf2W2 = new Label ("Enter text in TextField on Win2:") { Y = Pos.Bottom (lblTvW2) + 1 };
		var tf2W2 = new TextField ("Text2 on Win2") { X = Pos.Left (tf1W2), Width = Dim.Fill () };
		win2.Add (lblTf1W2, tf1W2, lblTvW2, tvW2, lblTf2W2, tf2W2);

		win1.Closing += (s, e) => isRunning = false;
		Assert.Null (top.Focused);
		Assert.Equal (top, Application.Current);
		Assert.True (top.IsCurrentTop);
		Assert.Equal (top, Application.OverlappedTop);
		Application.Begin (win1);
		Assert.Equal (new Rect (0, 0, 40, 25), win1.Frame);
		Assert.NotEqual (top, Application.Current);
		Assert.False (top.IsCurrentTop);
		Assert.Equal (win1, Application.Current);
		Assert.True (win1.IsCurrentTop);
		Assert.True (win1.IsOverlapped);
		Assert.Null (top.Focused);
		Assert.Null (top.MostFocused);
		Assert.Equal (tf1W1, win1.MostFocused);
		Assert.True (win1.IsOverlapped);
		Assert.Single (Application.OverlappedChildren);
		Application.Begin (win2);
		Assert.Equal (new Rect (0, 0, 40, 25), win2.Frame);
		Assert.NotEqual (top, Application.Current);
		Assert.False (top.IsCurrentTop);
		Assert.Equal (win2, Application.Current);
		Assert.True (win2.IsCurrentTop);
		Assert.True (win2.IsOverlapped);
		Assert.Null (top.Focused);
		Assert.Null (top.MostFocused);
		Assert.Equal (tf1W2, win2.MostFocused);
		Assert.Equal (2, Application.OverlappedChildren.Count);

		Application.MoveToOverlappedChild (win1);
		Assert.Equal (win1, Application.Current);
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		win1.Running = true;
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (Application.QuitKey));
		Assert.False (isRunning);
		Assert.False (win1.Running);
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Z | KeyCode.CtrlMask)));

		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.F5))); // refresh

		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Tab)));
		Assert.True (win1.IsCurrentTop);
		Assert.Equal (tvW1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Tab)));
		Assert.Equal ($"\tFirst line Win1{Environment.NewLine}Second line Win1", tvW1.Text);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Tab | KeyCode.ShiftMask)));
		Assert.Equal ($"First line Win1{Environment.NewLine}Second line Win1", tvW1.Text);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Tab | KeyCode.CtrlMask)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf2W1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Tab)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf1W1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf1W1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.CursorDown)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tvW1, win1.MostFocused);
#if UNIX_KEY_BINDINGS
			Assert.True (Application.OverlappedChildren [0].ProcessKeyDown (new (Key.I | Key.CtrlMask)));
			Assert.Equal (win1, Application.OverlappedChildren [0]);
			Assert.Equal (tf2W1, win1.MostFocused);
#endif
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Tab | KeyCode.ShiftMask)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tvW1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.CursorLeft)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf1W1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.CursorUp)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf2W1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Tab)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf1W1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Tab | KeyCode.CtrlMask)));
		Assert.Equal (win2, Application.OverlappedChildren [0]);
		Assert.Equal (tf1W2, win2.MostFocused);
		tf2W2.SetFocus ();
		Assert.True (tf2W2.HasFocus);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.Tab | KeyCode.CtrlMask | KeyCode.ShiftMask)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf1W1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (Application.AlternateForwardKey));
		Assert.Equal (win2, Application.OverlappedChildren [0]);
		Assert.Equal (tf2W2, win2.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (Application.AlternateBackwardKey));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf1W1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.CursorDown)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tvW1, win1.MostFocused);
#if UNIX_KEY_BINDINGS
			Assert.True (Application.OverlappedChildren [0].ProcessKeyDown (new (Key.B | Key.CtrlMask)));
#else
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.CursorLeft)));
#endif
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf1W1, win1.MostFocused);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.CursorDown)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tvW1, win1.MostFocused);
		Assert.Equal (new Point (0, 0), tvW1.CursorPosition);
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.End | KeyCode.CtrlMask)));
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tvW1, win1.MostFocused);
		Assert.Equal (new Point (16, 1), tvW1.CursorPosition);
#if UNIX_KEY_BINDINGS
			Assert.True (Application.OverlappedChildren [0].ProcessKeyDown (new (Key.F | Key.CtrlMask)));
#else
		Assert.True (Application.OverlappedChildren [0].NewKeyDownEvent (new Key (KeyCode.CursorRight)));
#endif
		Assert.Equal (win1, Application.OverlappedChildren [0]);
		Assert.Equal (tf2W1, win1.MostFocused);

#if UNIX_KEY_BINDINGS
			Assert.True (Application.OverlappedChildren [0].ProcessKeyDown (new (Key.L | Key.CtrlMask)));
#endif
	}

	[Fact]
	public void Added_Event_Should_Not_Be_Used_To_Initialize_Toplevel_Events ()
	{
		Key alternateForwardKey = default;
		Key alternateBackwardKey = default;
		Key quitKey = default;
		var wasAdded = false;

		var view = new View ();
		view.Added += View_Added;

		void View_Added (object sender, SuperViewChangedEventArgs e)
		{
			Assert.Throws<NullReferenceException> (() => Application.Top.AlternateForwardKeyChanged += (s, e) => alternateForwardKey = (KeyCode)e.OldKey);
			Assert.Throws<NullReferenceException> (() => Application.Top.AlternateBackwardKeyChanged += (s, e) => alternateBackwardKey = (KeyCode)e.OldKey);
			Assert.Throws<NullReferenceException> (() => Application.Top.QuitKeyChanged += (s, e) => quitKey = (KeyCode)e.OldKey);
			Assert.False (wasAdded);
			wasAdded = true;
			view.Added -= View_Added;
		}

		var win = new Window ();
		win.Add (view);
		Application.Init (new FakeDriver ());
		var top = Application.Top;
		top.Add (win);

		Assert.True (wasAdded);

		Application.Shutdown ();
	}

	[Fact]
	[AutoInitShutdown]
	public void AlternateForwardKeyChanged_AlternateBackwardKeyChanged_QuitKeyChanged_Events ()
	{
		Key alternateForwardKey = KeyCode.Null;
		Key alternateBackwardKey = KeyCode.Null;
		Key quitKey = KeyCode.Null;

		var view = new View ();
		view.Initialized += View_Initialized;

		void View_Initialized (object sender, EventArgs e)
		{
			Application.Top.AlternateForwardKeyChanged += (s, e) => alternateForwardKey = e.OldKey;
			Application.Top.AlternateBackwardKeyChanged += (s, e) => alternateBackwardKey = e.OldKey;
			Application.Top.QuitKeyChanged += (s, e) => quitKey = e.OldKey;
		}

		var win = new Window ();
		win.Add (view);
		var top = Application.Top;
		top.Add (win);
		Application.Begin (top);

		Assert.Equal (KeyCode.Null, alternateForwardKey);
		Assert.Equal (KeyCode.Null, alternateBackwardKey);
		Assert.Equal (KeyCode.Null, quitKey);

		Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, Application.AlternateForwardKey);
		Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, Application.AlternateBackwardKey);
		Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, Application.QuitKey);

		Application.AlternateForwardKey = KeyCode.A;
		Application.AlternateBackwardKey = KeyCode.B;
		Application.QuitKey = KeyCode.C;

		Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, alternateForwardKey);
		Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, alternateBackwardKey);
		Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, quitKey);

		Assert.Equal (KeyCode.A, Application.AlternateForwardKey);
		Assert.Equal (KeyCode.B, Application.AlternateBackwardKey);
		Assert.Equal (KeyCode.C, Application.QuitKey);

		// Replacing the defaults keys to avoid errors on others unit tests that are using it.
		Application.AlternateForwardKey = KeyCode.PageDown | KeyCode.CtrlMask;
		Application.AlternateBackwardKey = KeyCode.PageUp | KeyCode.CtrlMask;
		Application.QuitKey = KeyCode.Q | KeyCode.CtrlMask;

		Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, Application.AlternateForwardKey);
		Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, Application.AlternateBackwardKey);
		Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, Application.QuitKey);
	}

	[Fact]
	[AutoInitShutdown]
	public void Mouse_Drag_On_Top_With_Superview_Null ()
	{
		var win = new Window ();
		var top = Application.Top;
		top.Add (win);
		var iterations = -1;
		Window testWindow;

		Application.Iteration += (s, a) => {
			iterations++;
			if (iterations == 0) {
				((FakeDriver)Application.Driver).SetBufferSize (15, 7);
				// Don't use MessageBox here; it's too complicated for this unit test; just use Window
				testWindow = new Window {
					Text = "Hello",
					X = 2,
					Y = 2,
					Width = 10,
					Height = 3
				};
				Application.Run (testWindow);

			} else if (iterations == 1) {
				TestHelpers.AssertDriverContentsWithFrameAre (@"
┌─────────────┐
│             │
│ ┌────────┐  │
│ │Hello   │  │
│ └────────┘  │
│             │
└─────────────┘
", _output);
			} else if (iterations == 2) {
				Assert.Null (Application.MouseGrabView);
				// Grab the mouse
				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
					X = 3,
					Y = 2,
					Flags = MouseFlags.Button1Pressed
				}));

				Assert.Equal (Application.Current, Application.MouseGrabView);
				Assert.Equal (new Rect (2, 2, 10, 3), Application.MouseGrabView.Frame);

			} else if (iterations == 3) {
				Assert.Equal (Application.Current, Application.MouseGrabView);
				// Drag to left
				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
					X = 2,
					Y = 2,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				}));
				Application.Refresh ();

				Assert.Equal (Application.Current, Application.MouseGrabView);
				Assert.Equal (new Rect (1, 2, 10, 3), Application.MouseGrabView.Frame);

			} else if (iterations == 4) {
				Assert.Equal (Application.Current, Application.MouseGrabView);

				TestHelpers.AssertDriverContentsWithFrameAre (@"
┌─────────────┐
│             │
│┌────────┐   │
││Hello   │   │
│└────────┘   │
│             │
└─────────────┘", _output);

				Assert.Equal (Application.Current, Application.MouseGrabView);
			} else if (iterations == 5) {
				Assert.Equal (Application.Current, Application.MouseGrabView);
				// Drag up
				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
					X = 2,
					Y = 1,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				}));
				Application.Refresh ();

				Assert.Equal (Application.Current, Application.MouseGrabView);
				Assert.Equal (new Rect (1, 1, 10, 3), Application.MouseGrabView.Frame);

			} else if (iterations == 6) {
				Assert.Equal (Application.Current, Application.MouseGrabView);

				TestHelpers.AssertDriverContentsWithFrameAre (@"
┌─────────────┐
│┌────────┐   │
││Hello   │   │
│└────────┘   │
│             │
│             │
└─────────────┘", _output);

				Assert.Equal (Application.Current, Application.MouseGrabView);
				Assert.Equal (new Rect (1, 1, 10, 3), Application.MouseGrabView.Frame);

			} else if (iterations == 7) {
				Assert.Equal (Application.Current, Application.MouseGrabView);
				// Ungrab the mouse
				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
					X = 2,
					Y = 1,
					Flags = MouseFlags.Button1Released
				}));
				Application.Refresh ();

				Assert.Null (Application.MouseGrabView);

			} else if (iterations == 8) {
				Application.RequestStop ();
			} else if (iterations == 9) {
				Application.RequestStop ();
			}
		};

		Application.Run ();
	}

	[Fact]
	[AutoInitShutdown]
	public void Mouse_Drag_On_Top_With_Superview_Not_Null ()
	{
		var win = new Window {
			X = 3,
			Y = 2,
			Width = 10,
			Height = 5
		};
		var top = Application.Top;
		top.Add (win);

		var iterations = -1;

		var movex = 0;
		var movey = 0;

		var location = new Rect (win.Frame.X, win.Frame.Y, 7, 3);

		Application.Iteration += (s, a) => {
			iterations++;
			if (iterations == 0) {
				((FakeDriver)Application.Driver).SetBufferSize (30, 10);
			} else if (iterations == 1) {
				location = win.Frame;

				Assert.Null (Application.MouseGrabView);
				// Grab the mouse
				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
					X = win.Frame.X,
					Y = win.Frame.Y,
					Flags = MouseFlags.Button1Pressed
				}));

				Assert.Equal (win, Application.MouseGrabView);
				Assert.Equal (location, Application.MouseGrabView.Frame);
			} else if (iterations == 2) {
				Assert.Equal (win, Application.MouseGrabView);
				// Drag to left
				movex = 1;
				movey = 0;
				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
					X = win.Frame.X + movex,
					Y = win.Frame.Y + movey,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				}));

				Assert.Equal (win, Application.MouseGrabView);

			} else if (iterations == 3) {
				// we should have moved +1, +0
				Assert.Equal (win, Application.MouseGrabView);
				Assert.Equal (win, Application.MouseGrabView);
				location.Offset (movex, movey);
				Assert.Equal (location, Application.MouseGrabView.Frame);

			} else if (iterations == 4) {
				Assert.Equal (win, Application.MouseGrabView);
				// Drag up
				movex = 0;
				movey = -1;
				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
					X = win.Frame.X + movex,
					Y = win.Frame.Y + movey,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				}));

				Assert.Equal (win, Application.MouseGrabView);

			} else if (iterations == 5) {
				// we should have moved +0, -1
				Assert.Equal (win, Application.MouseGrabView);
				location.Offset (movex, movey);
				Assert.Equal (location, Application.MouseGrabView.Frame);

			} else if (iterations == 6) {
				Assert.Equal (win, Application.MouseGrabView);
				// Ungrab the mouse
				movex = 0;
				movey = 0;
				Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
					X = win.Frame.X + movex,
					Y = win.Frame.Y + movey,
					Flags = MouseFlags.Button1Released
				}));

				Assert.Null (Application.MouseGrabView);
			} else if (iterations == 7) {
				Application.RequestStop ();
			}
		};

		Application.Run ();
	}

	[Fact]
	[AutoInitShutdown]
	public void GetLocationThatFits_With_Border_Null_Not_Throws ()
	{
		var top = new Toplevel ();
		Application.Begin (top);

		var exception = Record.Exception (() => ((FakeDriver)Application.Driver).SetBufferSize (0, 10));
		Assert.Null (exception);

		exception = Record.Exception (() => ((FakeDriver)Application.Driver).SetBufferSize (10, 0));
		Assert.Null (exception);
	}

	[Fact]
	[AutoInitShutdown]
	public void OnEnter_OnLeave_Triggered_On_Application_Begin_End ()
	{
		var isEnter = false;
		var isLeave = false;
		var v = new View ();
		v.Enter += (s, _) => isEnter = true;
		v.Leave += (s, _) => isLeave = true;
		var top = Application.Top;
		top.Add (v);

		Assert.False (v.CanFocus);
		var exception = Record.Exception (() => top.OnEnter (top));
		Assert.Null (exception);
		exception = Record.Exception (() => top.OnLeave (top));
		Assert.Null (exception);

		v.CanFocus = true;
		Application.Begin (top);

		Assert.True (isEnter);
		Assert.False (isLeave);

		isEnter = false;
		var d = new Dialog ();
		var rs = Application.Begin (d);

		Assert.False (isEnter);
		Assert.True (isLeave);

		isLeave = false;
		Application.End (rs);

		Assert.True (isEnter);
		Assert.False (isLeave); // Leave event cannot be trigger because it v.Enter was performed and v is focused
		Assert.True (v.HasFocus);
	}

	[Fact]
	[AutoInitShutdown]
	public void OnEnter_OnLeave_Triggered_On_Application_Begin_End_With_More_Toplevels ()
	{
		var iterations = 0;
		var steps = new int [5];
		var isEnterTop = false;
		var isLeaveTop = false;
		var vt = new View ();
		var top = Application.Top;
		var diag = new Dialog ();

		vt.Enter += (s, e) => {
			iterations++;
			isEnterTop = true;
			if (iterations == 1) {
				steps [0] = iterations;
				Assert.Null (e.View);
			} else {
				steps [4] = iterations;
				Assert.Equal (diag, e.View);
			}
		};
		vt.Leave += (s, e) => {
			iterations++;
			steps [1] = iterations;
			isLeaveTop = true;
			Assert.Equal (diag, e.View);
		};
		top.Add (vt);

		Assert.False (vt.CanFocus);
		var exception = Record.Exception (() => top.OnEnter (top));
		Assert.Null (exception);
		exception = Record.Exception (() => top.OnLeave (top));
		Assert.Null (exception);

		vt.CanFocus = true;
		Application.Begin (top);

		Assert.True (isEnterTop);
		Assert.False (isLeaveTop);

		isEnterTop = false;
		var isEnterDiag = false;
		var isLeaveDiag = false;
		var vd = new View ();
		vd.Enter += (s, e) => {
			iterations++;
			steps [2] = iterations;
			isEnterDiag = true;
			Assert.Null (e.View);
		};
		vd.Leave += (s, e) => {
			iterations++;
			steps [3] = iterations;
			isLeaveDiag = true;
			Assert.Equal (top, e.View);
		};
		diag.Add (vd);

		Assert.False (vd.CanFocus);
		exception = Record.Exception (() => diag.OnEnter (diag));
		Assert.Null (exception);
		exception = Record.Exception (() => diag.OnLeave (diag));
		Assert.Null (exception);

		vd.CanFocus = true;
		var rs = Application.Begin (diag);

		Assert.True (isEnterDiag);
		Assert.False (isLeaveDiag);
		Assert.False (isEnterTop);
		Assert.True (isLeaveTop);

		isEnterDiag = false;
		isLeaveTop = false;
		Application.End (rs);

		Assert.False (isEnterDiag);
		Assert.True (isLeaveDiag);
		Assert.True (isEnterTop);
		Assert.False (isLeaveTop); // Leave event cannot be trigger because it v.Enter was performed and v is focused
		Assert.True (vt.HasFocus);
		Assert.Equal (1, steps [0]);
		Assert.Equal (2, steps [1]);
		Assert.Equal (3, steps [2]);
		Assert.Equal (4, steps [3]);
		Assert.Equal (5, steps [^1]);
	}

	[Fact]
	[AutoInitShutdown]
	public void PositionCursor_SetCursorVisibility_To_Invisible_If_Focused_Is_Null ()
	{
		var tf = new TextField ("test") { Width = 5 };
		var view = new View { Width = 10, Height = 10 };
		view.Add (tf);
		Application.Top.Add (view);
		Application.Begin (Application.Top);

		Assert.True (tf.HasFocus);
		Application.Driver.GetCursorVisibility (out var cursor);
		Assert.Equal (CursorVisibility.Default, cursor);

		view.Enabled = false;
		Assert.False (tf.HasFocus);
		Application.Refresh ();
		Application.Driver.GetCursorVisibility (out cursor);
		Assert.Equal (CursorVisibility.Invisible, cursor);
	}

	[Fact]
	[AutoInitShutdown]
	public void IsLoaded_Application_Begin ()
	{
		var top = Application.Top;
		Assert.False (top.IsLoaded);

		Application.Begin (top);
		Assert.True (top.IsLoaded);
	}

	[Fact]
	[AutoInitShutdown]
	public void IsLoaded_With_Sub_Toplevel_Application_Begin_NeedDisplay ()
	{
		var top = Application.Top;
		var subTop = new Toplevel ();
		var view = new View (new Rect (0, 0, 20, 10));
		subTop.Add (view);
		top.Add (subTop);

		Assert.False (top.IsLoaded);
		Assert.False (subTop.IsLoaded);
		Assert.Equal (new Rect (0, 0, 20, 10), view.Frame);

		view.LayoutStarted += view_LayoutStarted;

		void view_LayoutStarted (object sender, LayoutEventArgs e)
		{
			Assert.Equal (new Rect (0, 0, 20, 10), view._needsDisplayRect);
			view.LayoutStarted -= view_LayoutStarted;
		}

		Application.Begin (top);

		Assert.True (top.IsLoaded);
		Assert.True (subTop.IsLoaded);
		Assert.Equal (new Rect (0, 0, 20, 10), view.Frame);

		view.Frame = new Rect (1, 3, 10, 5);
		Assert.Equal (new Rect (1, 3, 10, 5), view.Frame);
		Assert.Equal (new Rect (0, 0, 10, 5), view._needsDisplayRect);

		view.OnDrawContent (view.Bounds);
		view.Frame = new Rect (1, 3, 10, 5);
		Assert.Equal (new Rect (1, 3, 10, 5), view.Frame);
		Assert.Equal (new Rect (0, 0, 10, 5), view._needsDisplayRect);
	}

	// BUGBUG: Broke this test with #2483 - @bdisp I need your help figuring out why
	[Fact]
	[AutoInitShutdown]
	public void Toplevel_Inside_ScrollView_MouseGrabView ()
	{
		var scrollView = new ScrollView {
			X = 3,
			Y = 3,
			Width = 40,
			Height = 16,
			ContentSize = new Size (200, 100)
		};
		var win = new Window { X = 3, Y = 3, Width = Dim.Fill (3), Height = Dim.Fill (3) };
		scrollView.Add (win);
		var top = Application.Top;
		top.Add (scrollView);
		Application.Begin (top);

		Assert.Equal (new Rect (0, 0, 80, 25), top.Frame);
		Assert.Equal (new Rect (3, 3, 40, 16), scrollView.Frame);
		Assert.Equal (new Rect (0, 0, 200, 100), scrollView.Subviews [0].Frame);
		Assert.Equal (new Rect (3, 3, 194, 94), win.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
                                          ▲
                                          ┬
                                          │
      ┌───────────────────────────────────┴
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ▼
   ◄├──────┤░░░░░░░░░░░░░░░░░░░░░░░░░░░░░► ", _output);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 6,
			Y = 6,
			Flags = MouseFlags.Button1Pressed
		}));
		Assert.Equal (win, Application.MouseGrabView);
		Assert.Equal (new Rect (3, 3, 194, 94), win.Frame);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 9,
			Y = 9,
			Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
		}));
		Assert.Equal (win, Application.MouseGrabView);
		top.SetNeedsLayout ();
		top.LayoutSubviews ();
		Assert.Equal (new Rect (6, 6, 191, 91), win.Frame);
		Application.Refresh ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
                                          ▲
                                          ┬
                                          │
                                          ┴
                                          ░
                                          ░
         ┌────────────────────────────────░
         │                                ░
         │                                ░
         │                                ░
         │                                ░
         │                                ░
         │                                ░
         │                                ░
         │                                ▼
   ◄├──────┤░░░░░░░░░░░░░░░░░░░░░░░░░░░░░► ", _output);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 5,
			Y = 5,
			Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
		}));
		Assert.Equal (win, Application.MouseGrabView);
		top.SetNeedsLayout ();
		top.LayoutSubviews ();
		Assert.Equal (new Rect (2, 2, 195, 95), win.Frame);
		Application.Refresh ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
                                          ▲
                                          ┬
     ┌────────────────────────────────────│
     │                                    ┴
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ▼
   ◄├──────┤░░░░░░░░░░░░░░░░░░░░░░░░░░░░░► ", _output);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 5,
			Y = 5,
			Flags = MouseFlags.Button1Released
		}));
		Assert.Null (Application.MouseGrabView);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 4,
			Y = 4,
			Flags = MouseFlags.ReportMousePosition
		}));
		Assert.Equal (scrollView, Application.MouseGrabView);
	}

	[Fact]
	[AutoInitShutdown]
	public void Window_Bounds_Bigger_Than_Driver_Cols_And_Rows_Allow_Drag_Beyond_Left_Right_And_Bottom ()
	{
		var top = Application.Top;
		var window = new Window { Width = 20, Height = 3 };
		Application.Begin (top);
		((FakeDriver)Application.Driver).SetBufferSize (40, 10);
		Application.Begin (window);
		Application.Refresh ();
		Assert.Equal (new Rect (0, 0, 40, 10), top.Frame);
		Assert.Equal (new Rect (0, 0, 20, 3), window.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│                  │
└──────────────────┘
", _output);

		Assert.Null (Application.MouseGrabView);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 0,
			Y = 0,
			Flags = MouseFlags.Button1Pressed
		}));

		Assert.Equal (window, Application.MouseGrabView);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = -11,
			Y = -4,
			Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
		}));

		Application.Refresh ();
		Assert.Equal (new Rect (0, 0, 40, 10), top.Frame);
		Assert.Equal (new Rect (0, 0, 20, 3), window.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│                  │
└──────────────────┘
", _output);

		// Changes Top size to same size as Dialog more menu and scroll bar
		((FakeDriver)Application.Driver).SetBufferSize (20, 3);
		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = -1,
			Y = -1,
			Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
		}));

		Application.Refresh ();
		Assert.Equal (new Rect (0, 0, 20, 3), top.Frame);
		Assert.Equal (new Rect (0, 0, 20, 3), window.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│                  │
└──────────────────┘
", _output);

		// Changes Top size smaller than Dialog size
		((FakeDriver)Application.Driver).SetBufferSize (19, 2);
		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = -1,
			Y = -1,
			Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
		}));

		Application.Refresh ();
		Assert.Equal (new Rect (0, 0, 19, 2), top.Frame);
		Assert.Equal (new Rect (-1, 0, 20, 3), window.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
──────────────────┐
                  │
", _output);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 18,
			Y = 1,
			Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
		}));

		Application.Refresh ();
		Assert.Equal (new Rect (0, 0, 19, 2), top.Frame);
		Assert.Equal (new Rect (18, 1, 20, 3), window.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
                  ┌", _output);

		// On a real app we can't go beyond the SuperView bounds
		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 19,
			Y = 2,
			Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
		}));

		Application.Refresh ();
		Assert.Equal (new Rect (0, 0, 19, 2), top.Frame);
		Assert.Equal (new Rect (19, 2, 20, 3), window.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"", _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Modal_As_Top_Will_Drag_Cleanly ()
	{
		// Don't use Dialog as a Top, use a Window instead - dialog has complex layout behavior that is not needed here.
		var window = new Window { Width = 10, Height = 3 };
		window.Add (new Label (
			"Test") {
			X = Pos.Center (),
			Y = Pos.Center (),
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			TextAlignment = TextAlignment.Centered,
			VerticalTextAlignment = VerticalTextAlignment.Middle,
			AutoSize = false
		});

		var rs = Application.Begin (window);

		Assert.Null (Application.MouseGrabView);
		Assert.Equal (new Rect (0, 0, 10, 3), window.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│  Test  │
└────────┘", _output);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 0,
			Y = 0,
			Flags = MouseFlags.Button1Pressed
		}));

		var firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);
		Assert.Equal (window, Application.MouseGrabView);

		Assert.Equal (new Rect (0, 0, 10, 3), window.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│  Test  │
└────────┘", _output);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 1,
			Y = 1,
			Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
		}));

		firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);
		Assert.Equal (window, Application.MouseGrabView);
		Assert.Equal (new Rect (1, 1, 10, 3), window.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
 ┌────────┐
 │  Test  │
 └────────┘", _output);

		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void Begin_With_Window_Sets_Size_Correctly ()
	{
		var top = Application.Top;
		Application.Begin (top);
		((FakeDriver)Application.Driver).SetBufferSize (20, 20);

		var testWindow = new Window { X = 2, Y = 1, Width = 15, Height = 10 };
		Assert.Equal (new Rect (2, 1, 15, 10), testWindow.Frame);

		var rs = Application.Begin (testWindow);
		Assert.Equal (new Rect (2, 1, 15, 10), testWindow.Frame);
	}

	// Don't use Dialog as a Top, use a Window instead - dialog has complex layout behavior that is not needed here.
	[Fact]
	[AutoInitShutdown]
	public void Draw_A_Top_Subview_On_A_Window ()
	{
		var top = Application.Top;
		var win = new Window ();
		top.Add (win);
		Application.Begin (top);
		((FakeDriver)Application.Driver).SetBufferSize (20, 20);

		Assert.Equal (new Rect (0, 0, 20, 20), win.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘", _output);

		var btnPopup = new Button ("Popup");
		var testWindow = new Window { X = 2, Y = 1, Width = 15, Height = 10 };
		testWindow.Add (btnPopup);
		btnPopup.Clicked += (s, e) => {
			var viewToScreen = btnPopup.BoundsToScreen (top.Frame);
			var viewAddedToTop = new View {
				Text = "viewAddedToTop",
				X = 1,
				Y = viewToScreen.Y + 1,
				Width = 18,
				Height = 16,
				BorderStyle = LineStyle.Single
			};
			Assert.Equal (testWindow, Application.Current);
			Application.Current.DrawContentComplete += testWindow_DrawContentComplete;
			top.Add (viewAddedToTop);

			void testWindow_DrawContentComplete (object sender, DrawEventArgs e)
			{
				Assert.Equal (new Rect (1, 3, 18, 16), viewAddedToTop.Frame);

				var savedClip = Application.Driver.Clip;
				Application.Driver.Clip = top.Frame;
				viewAddedToTop.Draw ();
				top.Move (2, 15);
				View.Driver.AddStr ("One");
				top.Move (2, 16);
				View.Driver.AddStr ("Two");
				top.Move (2, 17);
				View.Driver.AddStr ("Three");
				Application.Driver.Clip = savedClip;

				Application.Current.DrawContentComplete -= testWindow_DrawContentComplete;
			}
		};
		var rs = Application.Begin (testWindow);

		Assert.Equal (new Rect (2, 1, 15, 10), testWindow.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@$"
┌──────────────────┐
│ ┌─────────────┐  │
│ │{CM.Glyphs.LeftBracket} Popup {CM.Glyphs.RightBracket}    │  │
│ │             │  │
│ │             │  │
│ │             │  │
│ │             │  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘", _output);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent {
			X = 5,
			Y = 2,
			Flags = MouseFlags.Button1Clicked
		}));
		Application.Top.Draw ();

		var firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);
		TestHelpers.AssertDriverContentsWithFrameAre (@$"
┌──────────────────┐
│ ┌─────────────┐  │
│ │{CM.Glyphs.LeftBracket} Popup {CM.Glyphs.RightBracket}    │  │
│┌────────────────┐│
││viewAddedToTop  ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││                ││
││One             ││
││Two             ││
││Three           ││
│└────────────────┘│
└──────────────────┘", _output);

		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void Activating_MenuBar_By_Alt_Key_Does_Not_Throw ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new ("Child", new MenuItem [] {
				new ("_Create Child", "", null)
			})
		});
		var topChild = new Toplevel ();
		topChild.Add (menu);
		Application.Top.Add (topChild);
		Application.Begin (Application.Top);

		var exception = Record.Exception (() => topChild.NewKeyDownEvent (new Key (KeyCode.AltMask)));
		Assert.Null (exception);
	}


	[Fact]
	[TestRespondersDisposed]
	public void Multi_Thread_Toplevels ()
	{
		Application.Init (new FakeDriver ());

		var t = Application.Top;
		var w = new Window ();
		t.Add (w);

		int count = 0, count1 = 0, count2 = 0;
		bool log = false, log1 = false, log2 = false;
		var fromTopStillKnowFirstIsRunning = false;
		var fromTopStillKnowSecondIsRunning = false;
		var fromFirstStillKnowSecondIsRunning = false;

		Application.AddTimeout (TimeSpan.FromMilliseconds (100), () => {
			count++;
			if (count1 == 5) {
				log1 = true;
			}
			if (count1 == 14 && count2 == 10 && count == 15) {
				// count2 is already stopped
				fromTopStillKnowFirstIsRunning = true;
			}
			if (count1 == 7 && count2 == 7 && count == 8) {
				fromTopStillKnowSecondIsRunning = true;
			}
			if (count == 30) {
				Assert.Equal (30, count);
				Assert.Equal (20, count1);
				Assert.Equal (10, count2);

				Assert.True (log);
				Assert.True (log1);
				Assert.True (log2);

				Assert.True (fromTopStillKnowFirstIsRunning);
				Assert.True (fromTopStillKnowSecondIsRunning);
				Assert.True (fromFirstStillKnowSecondIsRunning);

				Application.RequestStop ();
				return false;
			}
			return true;
		});

		t.Ready += FirstWindow;

		void FirstWindow (object sender, EventArgs args)
		{
			var firstWindow = new Window ();
			firstWindow.Ready += SecondWindow;

			Application.AddTimeout (TimeSpan.FromMilliseconds (100), () => {
				count1++;
				if (count2 == 5) {
					log2 = true;
				}
				if (count2 == 4 && count1 == 5 && count == 5) {
					fromFirstStillKnowSecondIsRunning = true;
				}
				if (count1 == 20) {
					Assert.Equal (20, count1);
					Application.RequestStop ();
					return false;
				}
				return true;
			});

			Application.Run (firstWindow);
		}

		void SecondWindow (object sender, EventArgs args)
		{
			var testWindow = new Window ();

			Application.AddTimeout (TimeSpan.FromMilliseconds (100), () => {
				count2++;
				if (count < 30) {
					log = true;
				}
				if (count2 == 10) {
					Assert.Equal (10, count2);
					Application.RequestStop ();
					return false;
				}
				return true;
			});

			Application.Run (testWindow);
		}

		Application.Run ();
		Application.Shutdown ();
	}
}