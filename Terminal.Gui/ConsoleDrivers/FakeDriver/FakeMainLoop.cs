using System;

namespace Terminal.Gui {
	internal class FakeMainLoop : IMainLoopDriver {

		public Action<ConsoleKeyInfo> KeyPressed;

		public FakeMainLoop (ConsoleDriver consoleDriver = null)
		{
			// consoleDriver is not needed/used in FakeConsole
		}

		public void Setup (MainLoop mainLoop)
		{
		}

		public void Wakeup ()
		{
			// No implementation needed for FakeMainLoop
		}

		public bool EventsPending (bool wait)
		{
			// Always return true for FakeMainLoop
			return true;
		}

		public void MainIteration ()
		{
			if (FakeConsole.MockKeyPresses.Count > 0) {
				KeyPressed?.Invoke (FakeConsole.MockKeyPresses.Pop ());
			}
		}
	}
}