using System;
using System.Threading;

namespace Terminal.Gui {
	/// <summary>
	/// Mainloop intended to be used with the .NET System.Console API, and can
	/// be used on Windows and Unix, it is cross platform but lacks things like
	/// file descriptor monitoring.
	/// </summary>
	/// <remarks>
	/// This implementation is used for FakeDriver.
	/// </remarks>
	public class FakeMainLoop : IMainLoopDriver {
		AutoResetEvent _keyReady = new AutoResetEvent (false);
		AutoResetEvent _waitForProbe = new AutoResetEvent (false);
		ConsoleKeyInfo? _keyResult = null;
		MainLoop _mainLoop;
		Thread _readThread;
		//Func<ConsoleKeyInfo> consoleKeyReaderFn = () => ;

		/// <summary>
		/// Invoked when a Key is pressed.
		/// </summary>
		public Action<ConsoleKeyInfo> KeyPressed;

		/// <summary>
		/// Creates an instance of the FakeMainLoop. <paramref name="consoleDriver"/> is not used.
		/// </summary>
		/// <param name="consoleDriver"></param>
		public FakeMainLoop (ConsoleDriver consoleDriver = null)
		{
			// consoleDriver is not needed/used in FakeConsole
		}

		void MockKeyReader ()
		{
			while (_mainLoop != null && _waitForProbe != null) {
				_waitForProbe?.WaitOne ();
				_keyResult = FakeConsole.ReadKey (true);
				_keyReady?.Set ();
			}
		}

		void IMainLoopDriver.Setup (MainLoop mainLoop)
		{
			this._mainLoop = mainLoop;
			_readThread = new Thread (MockKeyReader);
			// BUGBUG: This thread never gets cleaned up. This causes unit tests to never exit.
			_readThread.Start ();
		}

		void IMainLoopDriver.Wakeup ()
		{
		}

		bool IMainLoopDriver.EventsPending (bool wait)
		{
			_keyResult = null;
			_waitForProbe.Set ();

			if (CheckTimers (wait, out var waitTimeout)) {
				return true;
			}

			_keyReady.WaitOne (waitTimeout);
			return _keyResult.HasValue;
		}

		bool CheckTimers (bool wait, out int waitTimeout)
		{
			long now = DateTime.UtcNow.Ticks;

			if (_mainLoop.timeouts.Count > 0) {
				waitTimeout = (int)((_mainLoop.timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
				if (waitTimeout < 0)
					return true;
			} else {
				waitTimeout = -1;
			}

			if (!wait) {
				waitTimeout = 0;
			}

			int ic;
			lock (_mainLoop.idleHandlers) {
				ic = _mainLoop.idleHandlers.Count;
			}

			return ic > 0;
		}

		void IMainLoopDriver.Iteration ()
		{
			if (_keyResult.HasValue) {
				KeyPressed?.Invoke (_keyResult.Value);
				_keyResult = null;
			}
		}
		public void TearDown ()
		{
			_waitForProbe.Close ();
			_waitForProbe.Dispose();
			_waitForProbe = null;
			_keyReady.Dispose ();
			_keyReady = null;
			_mainLoop = null;
		}
	}
}