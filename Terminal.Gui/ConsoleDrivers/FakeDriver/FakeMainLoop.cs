﻿using System;

namespace Terminal.Gui;

internal class FakeMainLoop : IMainLoopDriver {

	public Action<ConsoleKeyInfo> KeyPressed;

	public FakeMainLoop (ConsoleDriver consoleDriver = null)
	{
		// No implementation needed for FakeMainLoop
	}

	public void Setup (MainLoop mainLoop)
	{
		// No implementation needed for FakeMainLoop
	}

	public void Wakeup ()
	{
		// No implementation needed for FakeMainLoop
	}

	public bool EventsPending ()
	{
		// Always return true for FakeMainLoop
		return true;
	}

	public void Iteration ()
	{
		if (FakeConsole.MockKeyPresses.Count > 0) {
			KeyPressed?.Invoke (FakeConsole.MockKeyPresses.Pop ());
		}
	}
}

