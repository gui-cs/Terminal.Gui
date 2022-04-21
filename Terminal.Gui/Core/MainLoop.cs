//
// MainLoop.cs: IMainLoopDriver and MainLoop for Terminal.Gui
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// Public interface to create your own platform specific main loop driver.
	/// </summary>
	public interface IMainLoopDriver {
		/// <summary>
		/// Initializes the main loop driver, gets the calling main loop for the initialization.
		/// </summary>
		/// <param name="mainLoop">Main loop.</param>
		void Setup (MainLoop mainLoop);

		/// <summary>
		/// Wakes up the mainloop that might be waiting on input, must be thread safe.
		/// </summary>
		void Wakeup ();

		/// <summary>
		/// Must report whether there are any events pending, or even block waiting for events.
		/// </summary>
		/// <returns><c>true</c>, if there were pending events, <c>false</c> otherwise.</returns>
		/// <param name="wait">If set to <c>true</c> wait until an event is available, otherwise return immediately.</param>
		bool EventsPending (bool wait);

		/// <summary>
		/// The iteration function.
		/// </summary>
		void MainIteration ();
	}

	/// <summary>
	///   Simple main loop implementation that can be used to monitor
	///   file descriptor, run timers and idle handlers.
	/// </summary>
	/// <remarks>
	///   Monitoring of file descriptors is only available on Unix, there
	///   does not seem to be a way of supporting this on Windows.
	/// </remarks>
	public class MainLoop {
		internal class Timeout {
			public TimeSpan Span;
			public Func<MainLoop, bool> Callback;
		}

		internal SortedList<long, Timeout> timeouts = new SortedList<long, Timeout> ();
		internal List<Func<bool>> idleHandlers = new List<Func<bool>> ();

		/// <summary>
		/// The current IMainLoopDriver in use.
		/// </summary>
		/// <value>The driver.</value>
		public IMainLoopDriver Driver { get; }

		/// <summary>
		///  Creates a new Mainloop. 
		/// </summary>
		/// <param name="driver">Should match the <see cref="ConsoleDriver"/> (one of the implementations UnixMainLoop, NetMainLoop or WindowsMainLoop).</param>
		public MainLoop (IMainLoopDriver driver)
		{
			Driver = driver;
			driver.Setup (this);
		}

		/// <summary>
		///   Runs <c>action</c> on the thread that is processing events
		/// </summary>
		/// <param name="action">the action to be invoked on the main processing thread.</param>
		public void Invoke (Action action)
		{
			AddIdle (() => {
				action ();
				return false;
			});
		}

		/// <summary>
		///   Adds specified idle handler function to mainloop processing. The handler function will be called once per iteration of the main loop after other events have been handled.
		/// </summary>
		/// <remarks>
		/// <para>
		///   Remove an idle hander by calling <see cref="RemoveIdle(Func{bool})"/> with the token this method returns.
		/// </para>
		/// <para>
		///   If the <c>idleHandler</c> returns <c>false</c> it will be removed and not called subsequently.
		/// </para>
		/// </remarks>
		/// <param name="idleHandler">Token that can be used to remove the idle handler with <see cref="RemoveIdle(Func{bool})"/> .</param>
		public Func<bool> AddIdle (Func<bool> idleHandler)
		{
			lock (idleHandlers) {
				idleHandlers.Add (idleHandler);
			}

			Driver.Wakeup ();
			return idleHandler;
		}

		/// <summary>
		///   Removes an idle handler added with <see cref="AddIdle(Func{bool})"/> from processing.
		/// </summary>
		/// <param name="token">A token returned by <see cref="AddIdle(Func{bool})"/></param>
		/// Returns <c>true</c>if the idle handler is successfully removed; otherwise, <c>false</c>.
		///  This method also returns <c>false</c> if the idle handler is not found.
		public bool RemoveIdle (Func<bool> token)
		{
			lock (token)
				return idleHandlers.Remove (token);
		}

		void AddTimeout (TimeSpan time, Timeout timeout)
		{
			lock (timeouts) {
				var k = (DateTime.UtcNow + time).Ticks;
				while (timeouts.ContainsKey (k)) {
					k = (DateTime.UtcNow + time).Ticks;
				}
				timeouts.Add (k, timeout);
			}
		}

		/// <summary>
		///   Adds a timeout to the mainloop.
		/// </summary>
		/// <remarks>
		///   When time specified passes, the callback will be invoked.
		///   If the callback returns true, the timeout will be reset, repeating
		///   the invocation. If it returns false, the timeout will stop and be removed.
		///
		///   The returned value is a token that can be used to stop the timeout
		///   by calling <see cref="RemoveTimeout(object)"/>.
		/// </remarks>
		public object AddTimeout (TimeSpan time, Func<MainLoop, bool> callback)
		{
			if (callback == null)
				throw new ArgumentNullException (nameof (callback));
			var timeout = new Timeout () {
				Span = time,
				Callback = callback
			};
			AddTimeout (time, timeout);
			return timeout;
		}

		/// <summary>
		///   Removes a previously scheduled timeout
		/// </summary>
		/// <remarks>
		///   The token parameter is the value returned by AddTimeout.
		/// </remarks>
		/// Returns <c>true</c>if the timeout is successfully removed; otherwise, <c>false</c>.
		/// This method also returns <c>false</c> if the timeout is not found.
		public bool RemoveTimeout (object token)
		{
			lock (timeouts) {
				var idx = timeouts.IndexOfValue (token as Timeout);
				if (idx == -1)
					return false;
				timeouts.RemoveAt (idx);
			}
			return true;
		}

		void RunTimers ()
		{
			long now = DateTime.UtcNow.Ticks;
			var copy = timeouts;
			timeouts = new SortedList<long, Timeout> ();
			foreach (var t in copy) {
				var k = t.Key;
				var timeout = t.Value;
				if (k < now) {
					if (timeout.Callback (this))
						AddTimeout (timeout.Span, timeout);
				} else {
					lock (timeouts) {
						timeouts.Add (k, timeout);
					}
				}
			}
		}

		void RunIdle ()
		{
			List<Func<bool>> iterate;
			lock (idleHandlers) {
				iterate = idleHandlers;
				idleHandlers = new List<Func<bool>> ();
			}

			foreach (var idle in iterate) {
				if (idle ())
					lock (idleHandlers)
						idleHandlers.Add (idle);
			}
		}

		bool running;

		/// <summary>
		///   Stops the mainloop.
		/// </summary>
		public void Stop ()
		{
			running = false;
			Driver.Wakeup ();
		}

		/// <summary>
		///   Determines whether there are pending events to be processed.
		/// </summary>
		/// <remarks>
		///   You can use this method if you want to probe if events are pending.
		///   Typically used if you need to flush the input queue while still
		///   running some of your own code in your main thread.
		/// </remarks>
		public bool EventsPending (bool wait = false)
		{
			return Driver.EventsPending (wait);
		}

		/// <summary>
		///   Runs one iteration of timers and file watches
		/// </summary>
		/// <remarks>
		///   You use this to process all pending events (timers, idle handlers and file watches).
		///
		///   You can use it like this:
		///     while (main.EvensPending ()) MainIteration ();
		/// </remarks>
		public void MainIteration ()
		{
			if (timeouts.Count > 0)
				RunTimers ();

			Driver.MainIteration ();

			lock (idleHandlers) {
				if (idleHandlers.Count > 0)
					RunIdle ();
			}
		}

		/// <summary>
		///   Runs the mainloop.
		/// </summary>
		public void Run ()
		{
			bool prev = running;
			running = true;
			while (running) {
				EventsPending (true);
				MainIteration ();
			}
			running = prev;
		}
	}
}
