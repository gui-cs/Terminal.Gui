﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests; 

public class ConsoleDriverTests {
	readonly ITestOutputHelper output;

	public ConsoleDriverTests (ITestOutputHelper output)
	{
		ConsoleDriver.RunningUnitTests = true;
		this.output = output;
	}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		[InlineData (typeof (NetDriver))]
		[InlineData (typeof (ANSIDriver))]
		[InlineData (typeof (WindowsDriver))]
		[InlineData (typeof (CursesDriver))]
		public void Init_Inits (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			var ml = driver.Init ();
			Assert.NotNull (ml);
			Assert.NotNull (driver.Clipboard);
			Console.ForegroundColor = ConsoleColor.Red;
			Assert.Equal (ConsoleColor.Red, Console.ForegroundColor);
			Console.BackgroundColor = ConsoleColor.Green;
			Assert.Equal (ConsoleColor.Green, Console.BackgroundColor);

		driver.End ();
	}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		[InlineData (typeof (NetDriver))]
		[InlineData (typeof (ANSIDriver))]
		[InlineData (typeof (WindowsDriver))]
		[InlineData (typeof (CursesDriver))]
		public void End_Cleans_Up (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			driver.Init ();
			driver.End ();
		}

	[Theory]
	[InlineData (typeof (FakeDriver))]
	public void FakeDriver_Only_Sends_Keystrokes_Through_MockKeyPresses (Type driverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		Application.Init (driver);

		var top = Application.Top;
		var view = new View () {
			CanFocus = true
		};
		int count = 0;
		bool wasKeyPressed = false;

		view.KeyDown += (s, e) => {
			wasKeyPressed = true;
		};
		top.Add (view);

		Application.Iteration += (s, a) => {
			count++;
			if (count == 10) {
				Application.RequestStop ();
			}
		};

		Application.Run ();

		Assert.False (wasKeyPressed);

		// Shutdown must be called to safely clean up Application if Init has been called
		Application.Shutdown ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver))]
	public void FakeDriver_MockKeyPresses (Type driverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		Application.Init (driver);

		string text = "MockKeyPresses";
		var mKeys = new Stack<ConsoleKeyInfo> ();
		foreach (char r in text.Reverse ()) {
			var ck = char.IsLetter (r) ? (ConsoleKey)char.ToUpper (r) : (ConsoleKey)r;
			var cki = new ConsoleKeyInfo (r, ck, false, false, false);
			mKeys.Push (cki);
		}
		Console.MockKeyPresses = mKeys;

		var top = Application.Top;
		var view = new View () {
			CanFocus = true
		};
		string rText = "";
		int idx = 0;

		view.KeyDown += (s, e) => {
			Assert.Equal (text [idx], (char)e.KeyCode);
			rText += (char)e.KeyCode;
			Assert.Equal (rText, text.Substring (0, idx + 1));
			e.Handled = true;
			idx++;
		};
		top.Add (view);

		Application.Iteration += (s, a) => {
			if (mKeys.Count == 0) {
				Application.RequestStop ();
			}
		};

		Application.Run ();

		Assert.Equal ("MockKeyPresses", rText);

		// Shutdown must be called to safely clean up Application if Init has been called
		Application.Shutdown ();
	}

	//[Theory]
	//[InlineData (typeof (FakeDriver))]
	//public void FakeDriver_MockKeyPresses_Press_AfterTimeOut (Type driverType)
	//{
	//	var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
	//	Application.Init (driver);

	//	// Simulating pressing of QuitKey after a short period of time
	//	uint quitTime = 100;
	//	Func<MainLoop, bool> closeCallback = (MainLoop loop) => {
	//		// Prove the scenario is using Application.QuitKey correctly
	//		output.WriteLine ($"  {quitTime}ms elapsed; Simulating keypresses...");
	//		FakeConsole.PushMockKeyPress (Key.F);
	//		FakeConsole.PushMockKeyPress (Key.U);
	//		FakeConsole.PushMockKeyPress (Key.C);
	//		FakeConsole.PushMockKeyPress (Key.K);
	//		return false;
	//	};
	//	output.WriteLine ($"Add timeout to simulate key presses after {quitTime}ms");
	//	_ = Application.AddTimeout (TimeSpan.FromMilliseconds (quitTime), closeCallback);

	//	// If Top doesn't quit within abortTime * 5 (500ms), this will force it
	//	uint abortTime = quitTime * 5;
	//	Func<MainLoop, bool> forceCloseCallback = (MainLoop loop) => {
	//		Application.RequestStop ();
	//		Assert.Fail ($"  failed to Quit after {abortTime}ms. Force quit.");
	//		return false;
	//	};
	//	output.WriteLine ($"Add timeout to force quit after {abortTime}ms");
	//	_ = Application.AddTimeout (TimeSpan.FromMilliseconds (abortTime), forceCloseCallback);

	//	Key key = Key.Unknown;

	//	Application.Top.KeyPress += (e) => {
	//		key = e.Key;
	//		output.WriteLine ($"  Application.Top.KeyPress: {key}");
	//		e.Handled = true;

	//	};

	//	int iterations = 0;
	//	Application.Iteration += (s, a) => {
	//		output.WriteLine ($"  iteration {++iterations}");

	//		if (Console.MockKeyPresses.Count == 0) {
	//			output.WriteLine ($"    No more MockKeyPresses; RequestStop");
	//			Application.RequestStop ();
	//		}
	//	};

	//	Application.Run ();

		//	// Shutdown must be called to safely clean up Application if Init has been called
		//	Application.Shutdown ();
		//}
		
		[Theory]
		[InlineData (typeof (FakeDriver))]
		[InlineData (typeof (NetDriver))]
		[InlineData (typeof (ANSIDriver))]
		[InlineData (typeof (WindowsDriver))]
		[InlineData (typeof (CursesDriver))]
		public void TerminalResized_Simulation (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			driver?.Init ();
			driver.Cols = 80;
			driver.Rows = 25;
			
			var wasTerminalResized = false;
			driver.SizeChanged += (s, e) => {
				wasTerminalResized = true;
				Assert.Equal (120, e.Size.Width);
				Assert.Equal (40, e.Size.Height);
			};

		Assert.Equal (80, driver.Cols);
		Assert.Equal (25, driver.Rows);
		Assert.False (wasTerminalResized);

		driver.Cols = 120;
		driver.Rows = 40;
		driver.OnSizeChanged (new SizeChangedEventArgs (new Size (driver.Cols, driver.Rows)));
		Assert.Equal (120, driver.Cols);
		Assert.Equal (40, driver.Rows);
		Assert.True (wasTerminalResized);
		driver.End ();
	}

	// Disabled due to test error - Change Task.Delay to an await
	//		[Fact, AutoInitShutdown]
	//		public void Write_Do_Not_Change_On_ProcessKey ()
	//		{
	//			var win = new Window ();
	//			Application.Begin (win);
	//			((FakeDriver)Application.Driver).SetBufferSize (20, 8);

	//			System.Threading.Tasks.Task.Run (() => {
	//				System.Threading.Tasks.Task.Delay (500).Wait ();
	//				Application.Invoke (() => {
	//					var lbl = new Label ("Hello World") { X = Pos.Center () };
	//					var dlg = new Dialog ();
	//					dlg.Add (lbl);
	//					Application.Begin (dlg);

	//					var expected = @"
	//┌──────────────────┐
	//│┌───────────────┐ │
	//││  Hello World  │ │
	//││               │ │
	//││               │ │
	//││               │ │
	//│└───────────────┘ │
	//└──────────────────┘
	//";

	//					var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
	//					Assert.Equal (new Rect (0, 0, 20, 8), pos);

	//					Assert.True (dlg.ProcessKey (new (Key.Tab)));
	//					dlg.Draw ();

	//					expected = @"
	//┌──────────────────┐
	//│┌───────────────┐ │
	//││  Hello World  │ │
	//││               │ │
	//││               │ │
	//││               │ │
	//│└───────────────┘ │
	//└──────────────────┘
	//";

	//					pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
	//					Assert.Equal (new Rect (0, 0, 20, 8), pos);

	//					win.RequestStop ();
	//				});
	//			});

	//			Application.Run (win);
	//			Application.Shutdown ();
	//		}
}