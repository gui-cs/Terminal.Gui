﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests;

public class MainLoopDriverTests {

	public MainLoopDriverTests (ITestOutputHelper output)
	{
		ConsoleDriver.RunningUnitTests = true;
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_Constructs_Disposes (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);

		// Check default values
		Assert.NotNull (mainLoop);
		Assert.Equal (mainLoopDriver, mainLoop.MainLoopDriver);
		Assert.Empty (mainLoop.IdleHandlers);
		Assert.Empty (mainLoop.Timeouts);
		Assert.False (mainLoop.Running);

		// Clean up
		mainLoop.Dispose ();
		// TODO: It'd be nice if we could really verify IMainLoopDriver.TearDown was called
		// and that it was actually cleaned up.
		Assert.Null (mainLoop.MainLoopDriver);
		Assert.Empty (mainLoop.IdleHandlers);
		Assert.Empty (mainLoop.Timeouts);
		Assert.False (mainLoop.Running);
	}
	
	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_AddTimeout_ValidParameters_ReturnsToken (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);
		var callbackInvoked = false;

		var token = mainLoop.AddTimeout (TimeSpan.FromMilliseconds (100), () => {
			callbackInvoked = true;
			return false;
		});

		Assert.NotNull (token);
		mainLoop.RunIteration (); // Run an iteration to process the timeout
		Assert.False (callbackInvoked); // Callback should not be invoked immediately
		Thread.Sleep (200); // Wait for the timeout
		mainLoop.RunIteration (); // Run an iteration to process the timeout
		Assert.True (callbackInvoked); // Callback should be invoked after the timeout
		mainLoop.Dispose ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_RemoveTimeout_ValidToken_ReturnsTrue (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);

		var token = mainLoop.AddTimeout (TimeSpan.FromMilliseconds (100), () => false);
		var result = mainLoop.RemoveTimeout (token);

		Assert.True (result);
		mainLoop.Dispose ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_RemoveTimeout_InvalidToken_ReturnsFalse (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);

		var result = mainLoop.RemoveTimeout (new object ());

		Assert.False (result);
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_AddIdle_ValidIdleHandler_ReturnsToken (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);
		var idleHandlerInvoked = false;

		bool IdleHandler ()
		{
			idleHandlerInvoked = true;
			return false;
		}

		Func<bool> token = mainLoop.AddIdle (IdleHandler);

		Assert.NotNull (token);
		Assert.False (idleHandlerInvoked); // Idle handler should not be invoked immediately
		mainLoop.RunIteration (); // Run an iteration to process the idle handler
		Assert.True (idleHandlerInvoked); // Idle handler should be invoked after processing
		mainLoop.Dispose ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_RemoveIdle_ValidToken_ReturnsTrue (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);

		bool IdleHandler () => false;
		Func<bool> token = mainLoop.AddIdle (IdleHandler);
		var result = mainLoop.RemoveIdle (token);

		Assert.True (result);
		mainLoop.Dispose ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_RemoveIdle_InvalidToken_ReturnsFalse (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);

		var result = mainLoop.RemoveIdle (() => false);

		Assert.False (result);
		mainLoop.Dispose ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_RunIteration_ValidIdleHandler_CallsIdleHandler (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);
		var idleHandlerInvoked = false;

		Func<bool> idleHandler = () => {
			idleHandlerInvoked = true;
			return false;
		};

		mainLoop.AddIdle (idleHandler);
		mainLoop.RunIteration (); // Run an iteration to process the idle handler

		Assert.True (idleHandlerInvoked);
		mainLoop.Dispose ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_CheckTimersAndIdleHandlers_NoTimersOrIdleHandlers_ReturnsFalse (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);

		var result = mainLoop.CheckTimersAndIdleHandlers (out var waitTimeout);

		Assert.False (result);
		Assert.Equal (-1, waitTimeout);
		mainLoop.Dispose ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_CheckTimersAndIdleHandlers_TimersActive_ReturnsTrue (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);

		mainLoop.AddTimeout (TimeSpan.FromMilliseconds (100), () => false);
		var result = mainLoop.CheckTimersAndIdleHandlers (out var waitTimeout);

		Assert.True (result);
		Assert.True (waitTimeout >= 0);
		mainLoop.Dispose ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//[InlineData (typeof (ANSIDriver), typeof (AnsiMainLoopDriver))]
	public void MainLoop_CheckTimersAndIdleHandlers_IdleHandlersActive_ReturnsTrue (Type driverType, Type mainLoopDriverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
		var mainLoop = new MainLoop (mainLoopDriver);

		mainLoop.AddIdle (() => false);
		var result = mainLoop.CheckTimersAndIdleHandlers (out var waitTimeout);

		Assert.True (result);
		Assert.Equal (-1, waitTimeout);
		mainLoop.Dispose ();
	}

	//[Theory]
	//[InlineData (typeof (FakeDriver), typeof (FakeMainLoop))]
	//[InlineData (typeof (NetDriver), typeof (NetMainLoop))]
	//[InlineData (typeof (CursesDriver), typeof (UnixMainLoop))]
	//[InlineData (typeof (WindowsDriver), typeof (WindowsMainLoop))]
	//public void MainLoop_Invoke_ValidAction_RunsAction (Type driverType, Type mainLoopDriverType)
	//{
	//	var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
	//	var mainLoopDriver = (IMainLoopDriver)Activator.CreateInstance (mainLoopDriverType, new object [] { driver });
	//	var mainLoop = new MainLoop (mainLoopDriver);
	//	var actionInvoked = false;

	//	mainLoop.Invoke (() => { actionInvoked = true; });
	//	mainLoop.RunIteration (); // Run an iteration to process the action.

	//	Assert.True (actionInvoked);
	//	mainLoop.Dispose ();
	//}
}
