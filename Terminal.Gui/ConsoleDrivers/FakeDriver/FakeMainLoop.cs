using System;
using System.Threading;
using System.Threading.Tasks;

namespace Terminal.Gui;
public class FakeMainLoop : IMainLoopDriver {
	private MainLoop _mainLoop;

	public Action<ConsoleKeyInfo> KeyPressed;

	public FakeMainLoop (ConsoleDriver consoleDriver = null)
	{
		// consoleDriver is not needed/used in FakeConsole
	}
	
	public void Setup (MainLoop mainLoop)
	{
		_mainLoop = mainLoop;
	}

	public void Wakeup ()
	{
		// No implementation needed for FakeMainLoop
	}

	public bool EventsPending (bool wait)
	{
		//if (CheckTimers (wait, out var waitTimeout)) {
		//	return true;
		//}

		// Always return true for FakeMainLoop
		return true;
	}

	//private bool CheckTimers (bool wait, out int waitTimeout)
	//{
	//	long now = DateTime.UtcNow.Ticks;

	//	if (_mainLoop.timeouts.Count > 0) {
	//		waitTimeout = (int)((_mainLoop.timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
	//		if (waitTimeout < 0)
	//			return true;
	//	} else {
	//		waitTimeout = -1;
	//	}

	//	if (!wait) {
	//		waitTimeout = 0;
	//	}

	//	return _mainLoop.idleHandlers.Count > 0;
	//}

	public void Iteration ()
	{
		if (FakeConsole.MockKeyPresses.Count > 0) {
			KeyPressed?.Invoke (FakeConsole.MockKeyPresses.Pop ());
		}
	}
}

