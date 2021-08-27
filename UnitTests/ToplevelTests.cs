using System;
using Xunit;

namespace Terminal.Gui.Core {
	public class ToplevelTests {
		[Fact]
		[AutoInitShutdown]
		public void Constructor_Default ()
		{
			var top = new Toplevel ();

			Assert.Equal (Colors.TopLevel, top.ColorScheme);
			Assert.Equal ("Dim.Fill(margin=0)", top.Width.ToString ());
			Assert.Equal ("Dim.Fill(margin=0)", top.Height.ToString ());
			Assert.False (top.Running);
			Assert.False (top.Modal);
			Assert.Null (top.MenuBar);
			Assert.Null (top.StatusBar);
			Assert.False (top.IsMdiContainer);
			Assert.False (top.IsMdiChild);
		}

		[Fact]
		[AutoInitShutdown]
		public void Create_Toplevel ()
		{
			var top = Toplevel.Create ();
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Bounds);
		}


		[Fact]
		[AutoInitShutdown]
		public void Application_Top_EnsureVisibleBounds_To_Driver_Rows_And_Cols ()
		{
			var iterations = 0;

			Application.Iteration += () => {
				if (iterations == 0) {
					Assert.Equal ("Top1", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.R, new KeyModifiers ()));
				} else if (iterations == 1) {
					Assert.Equal ("Top2", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.C, new KeyModifiers ()));
				} else if (iterations == 3) {
					Assert.Equal ("Top1", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.R, new KeyModifiers ()));
				} else if (iterations == 4) {
					Assert.Equal ("Top2", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.C, new KeyModifiers ()));
				} else if (iterations == 6) {
					Assert.Equal ("Top1", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.Q, new KeyModifiers ()));
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
					new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Application.RequestStop())
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

		[Fact]
		[AutoInitShutdown]
		public void Internal_Tests ()
		{
			var top = new Toplevel ();
			var eventInvoked = "";

			top.ChildUnloaded += (e) => eventInvoked = "ChildUnloaded";
			top.OnChildUnloaded (top);
			Assert.Equal ("ChildUnloaded", eventInvoked);
			top.ChildLoaded += (e) => eventInvoked = "ChildLoaded";
			top.OnChildLoaded (top);
			Assert.Equal ("ChildLoaded", eventInvoked);
			top.Closed += (e) => eventInvoked = "Closed";
			top.OnClosed (top);
			Assert.Equal ("Closed", eventInvoked);
			top.Closing += (e) => eventInvoked = "Closing";
			top.OnClosing (new ToplevelClosingEventArgs (top));
			Assert.Equal ("Closing", eventInvoked);
			top.AllChildClosed += () => eventInvoked = "AllChildClosed";
			top.OnAllChildClosed ();
			Assert.Equal ("AllChildClosed", eventInvoked);
			top.ChildClosed += (e) => eventInvoked = "ChildClosed";
			top.OnChildClosed (top);
			Assert.Equal ("ChildClosed", eventInvoked);
			top.Deactivate += (e) => eventInvoked = "Deactivate";
			top.OnDeactivate (top);
			Assert.Equal ("Deactivate", eventInvoked);
			top.Activate += (e) => eventInvoked = "Activate";
			top.OnActivate (top);
			Assert.Equal ("Activate", eventInvoked);
			top.Loaded += () => eventInvoked = "Loaded";
			top.OnLoaded ();
			Assert.Equal ("Loaded", eventInvoked);
			top.Ready += () => eventInvoked = "Ready";
			top.OnReady ();
			Assert.Equal ("Ready", eventInvoked);
			top.Unloaded += () => eventInvoked = "Unloaded";
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

			// top is Application.Top without menu and status bar.
			var supView = top.EnsureVisibleBounds (top, 2, 2, out int nx, out int ny, out View mb, out View sb);
			Assert.Equal (Application.Top, supView);
			Assert.Equal (0, nx);
			Assert.Equal (0, ny);
			Assert.Null (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new MenuBar ());
			Assert.NotNull (top.MenuBar);

			// top is Application.Top with a menu and without status bar.
			top.EnsureVisibleBounds (top, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (1, ny);
			Assert.NotNull (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new StatusBar ());
			Assert.NotNull (top.StatusBar);

			// top is Application.Top with a menu and status bar.
			top.EnsureVisibleBounds (top, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (1, ny);
			Assert.NotNull (mb);
			Assert.NotNull (sb);

			top.RemoveMenuStatusBar (top.MenuBar);
			Assert.Null (top.MenuBar);

			// top is Application.Top without a menu and with a status bar.
			top.EnsureVisibleBounds (top, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (0, ny);
			Assert.Null (mb);
			Assert.NotNull (sb);

			top.RemoveMenuStatusBar (top.StatusBar);
			Assert.Null (top.StatusBar);
			Assert.Null (top.MenuBar);

			var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
			top.Add (win);
			top.LayoutSubviews ();

			// The SuperView is always the same regardless of the caller.
			supView = top.EnsureVisibleBounds (win, 0, 0, out nx, out ny, out mb, out sb);
			Assert.Equal (Application.Top, supView);
			supView = win.EnsureVisibleBounds (win, 0, 0, out nx, out ny, out mb, out sb);
			Assert.Equal (Application.Top, supView);

			// top is Application.Top without menu and status bar.
			top.EnsureVisibleBounds (win, 0, 0, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (0, ny);
			Assert.Null (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new MenuBar ());
			Assert.NotNull (top.MenuBar);

			// top is Application.Top with a menu and without status bar.
			top.EnsureVisibleBounds (win, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (1, ny);
			Assert.NotNull (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new StatusBar ());
			Assert.NotNull (top.StatusBar);

			// top is Application.Top with a menu and status bar.
			top.EnsureVisibleBounds (win, 30, 20, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (1, ny);
			Assert.NotNull (mb);
			Assert.NotNull (sb);

			top.RemoveMenuStatusBar (top.MenuBar);
			top.RemoveMenuStatusBar (top.StatusBar);
			Assert.Null (top.StatusBar);
			Assert.Null (top.MenuBar);

			top.Remove (win);

			win = new Window () { Width = 60, Height = 15 };
			top.Add (win);

			// top is Application.Top without menu and status bar.
			top.EnsureVisibleBounds (win, 0, 0, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (0, ny);
			Assert.Null (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new MenuBar ());
			Assert.NotNull (top.MenuBar);

			// top is Application.Top with a menu and without status bar.
			top.EnsureVisibleBounds (win, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (2, nx);
			Assert.Equal (2, ny);
			Assert.NotNull (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new StatusBar ());
			Assert.NotNull (top.StatusBar);

			// top is Application.Top with a menu and status bar.
			top.EnsureVisibleBounds (win, 30, 20, out nx, out ny, out mb, out sb);
			Assert.Equal (20, nx); // 20+60=80
			Assert.Equal (9, ny); // 9+15+1(mb)=25
			Assert.NotNull (mb);
			Assert.NotNull (sb);

			top.PositionToplevels ();
			Assert.Equal (new Rect (0, 1, 60, 15), win.Frame);

			Assert.Null (Toplevel.dragPosition);
			win.MouseEvent (new MouseEvent () { X = 6, Y = 0, Flags = MouseFlags.Button1Pressed });
			Assert.Equal (new Point (6, 0), Toplevel.dragPosition);
			win.MouseEvent (new MouseEvent () { X = 6, Y = 0, Flags = MouseFlags.Button1Released });
			Assert.Null (Toplevel.dragPosition);
			win.CanFocus = false;
			win.MouseEvent (new MouseEvent () { X = 6, Y = 0, Flags = MouseFlags.Button1Pressed });
			Assert.Null (Toplevel.dragPosition);
		}
	}
}
