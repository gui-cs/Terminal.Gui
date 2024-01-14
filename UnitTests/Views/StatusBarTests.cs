﻿using System;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class StatusBarTests {
		readonly ITestOutputHelper output;

		public StatusBarTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void StatusItem_Constructor ()
		{
			Application.Init ();
			var si = new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit", null);
			Assert.Equal (KeyCode.CtrlMask | KeyCode.Q, si.Shortcut);
			Assert.Equal ($"{Application.QuitKey} to Quit", si.Title);
			Assert.Null (si.Action);
			si = new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit", () => { });
			Assert.NotNull (si.Action);
			Application.Shutdown ();
		}

		[Fact]
		public void StatusBar_Constructor_Default ()
		{
			var sb = new StatusBar ();

			Assert.Empty (sb.Items);
			Assert.False (sb.CanFocus);
			Assert.Equal (Colors.Menu, sb.ColorScheme);
			Assert.Equal (0, sb.X);
			Assert.Equal ("AnchorEnd(1)", sb.Y.ToString ());
			Assert.Equal (Dim.Fill (), sb.Width);
			Assert.Equal (1, sb.Height);

			var driver = new FakeDriver ();
			Application.Init (driver);

			sb = new StatusBar ();

			driver.SetCursorVisibility (CursorVisibility.Default);
			driver.GetCursorVisibility (out CursorVisibility cv);
			Assert.Equal (CursorVisibility.Default, cv);
			Assert.True (FakeConsole.CursorVisible);

			Application.Iteration += (s, a) => {
				Assert.Equal (24, sb.Frame.Y);

				driver.SetWindowSize (driver.Cols, 15);

				Assert.Equal (14, sb.Frame.Y);

				sb.OnEnter (null);
				driver.GetCursorVisibility (out cv);
				Assert.Equal (CursorVisibility.Invisible, cv);
				Assert.False (FakeConsole.CursorVisible);

				Application.RequestStop ();
			};

			Application.Top.Add (sb);

			Application.Run ();

			Application.Shutdown ();
		}

		[Fact]
		[AutoInitShutdown]
		public void Run_Action_With_Key_And_Mouse ()
		{
			var msg = "";
			var sb = new StatusBar (new StatusItem [] { new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit", () => msg = "Quiting...") });
			Application.Top.Add (sb);

			var iteration = 0;

			Application.Iteration += (s, a) => {
				if (iteration == 0) {
					Assert.Equal ("", msg);
					sb.NewKeyDownEvent (new (KeyCode.CtrlMask | KeyCode.Q));
				} else if (iteration == 1) {
					Assert.Equal ("Quiting...", msg);
					msg = "";
					sb.MouseEvent (new MouseEvent () { X = 1, Y = 24, Flags = MouseFlags.Button1Clicked });
				} else {
					Assert.Equal ("Quiting...", msg);

					Application.RequestStop ();
				}
				iteration++;
			};

			Application.Run ();
		}

		[Fact]
		[AutoInitShutdown]
		public void Redraw_Output ()
		{
			var sb = new StatusBar (new StatusItem [] {
				new StatusItem (KeyCode.CtrlMask | KeyCode.O, "~^O~ Open", null),
				new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit!", null)
			});
			Application.Top.Add (sb);

			sb.OnDrawContent (sb.Bounds);

			string expected = @$"
^O Open {CM.Glyphs.VLine} Ctrl+Q to Quit!
";
			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact]
		[AutoInitShutdown]
		public void Redraw_Output_CTRLQ ()
		{
			var sb = new StatusBar (new StatusItem [] {
				new StatusItem (KeyCode.CtrlMask | KeyCode.O, "~CTRL-O~ Open", null),
				new StatusItem (KeyCode.CtrlMask | KeyCode.Q, "~CTRL-Q~ Quit", null)
			});
			Application.Top.Add (sb);
			sb.OnDrawContent (sb.Bounds);

			string expected = @$"
CTRL-O Open {CM.Glyphs.VLine} CTRL-Q Quit
";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact]
		public void AddItemAt_RemoveItem_Replacing ()
		{
			var sb = new StatusBar (new StatusItem [] {
				new StatusItem (KeyCode.CtrlMask | KeyCode.Q, "~^O~ Open", null),
				new StatusItem (KeyCode.CtrlMask | KeyCode.Q, "~^S~ Save", null),
				new StatusItem (KeyCode.CtrlMask | KeyCode.Q, "~^Q~ Quit", null)
			});

			sb.AddItemAt (2, new StatusItem (KeyCode.CtrlMask | KeyCode.Q, "~^C~ Close", null));

			Assert.Equal ("~^O~ Open", sb.Items [0].Title);
			Assert.Equal ("~^S~ Save", sb.Items [1].Title);
			Assert.Equal ("~^C~ Close", sb.Items [2].Title);
			Assert.Equal ("~^Q~ Quit", sb.Items [^1].Title);

			Assert.Equal ("~^S~ Save", sb.RemoveItem (1).Title);

			Assert.Equal ("~^O~ Open", sb.Items [0].Title);
			Assert.Equal ("~^C~ Close", sb.Items [1].Title);
			Assert.Equal ("~^Q~ Quit", sb.Items [^1].Title);

			sb.Items [1] = new StatusItem (KeyCode.CtrlMask | KeyCode.A, "~^A~ Save As", null);

			Assert.Equal ("~^O~ Open", sb.Items [0].Title);
			Assert.Equal ("~^A~ Save As", sb.Items [1].Title);
			Assert.Equal ("~^Q~ Quit", sb.Items [^1].Title);
		}

		[Fact, AutoInitShutdown]
		public void CanExecute_ProcessHotKey ()
		{
			Window win = null;
			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem (KeyCode.CtrlMask | KeyCode.N, "~^N~ New", New, CanExecuteNew),
				new StatusItem (KeyCode.CtrlMask | KeyCode.C, "~^C~ Close", Close, CanExecuteClose)
			});
			var top = Application.Top;
			top.Add (statusBar);

			bool CanExecuteNew () => win == null;

			void New ()
			{
				win = new Window ();
			}

			bool CanExecuteClose () => win != null;

			void Close ()
			{
				win = null;
			}

			Application.Begin (top);

			Assert.Null (win);
			Assert.True (CanExecuteNew ());
			Assert.False (CanExecuteClose ());

			Assert.True (top.NewKeyDownEvent (new (KeyCode.N | KeyCode.CtrlMask)));
			Application.MainLoop.RunIteration ();
			Assert.NotNull (win);
			Assert.False (CanExecuteNew ());
			Assert.True (CanExecuteClose ());
		}
	}
}
