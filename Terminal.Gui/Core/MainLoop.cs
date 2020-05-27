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
		/// The interation function.
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

		IMainLoopDriver driver;

		/// <summary>
		/// The current IMainLoopDriver in use.
		/// </summary>
		/// <value>The driver.</value>
		public IMainLoopDriver Driver => driver;

		/// <summary>
		///  Creates a new Mainloop, to run it you must provide a driver, and choose
		///  one of the implementations UnixMainLoop, NetMainLoop or WindowsMainLoop.
		/// </summary>
		public MainLoop (IMainLoopDriver driver)
		{
			this.driver = driver;
			driver.Setup (this);
		}

		/// <summary>
		///   Runs @action on the thread that is processing events
		/// </summary>
		public void Invoke (Action action)
		{
			AddIdle (() => {
				action ();
				return false;
			});
		}

		/// <summary>
		///   Executes the specified @idleHandler on the idle loop.  The return value is a token to remove it.
		/// </summary>
		public Func<bool> AddIdle (Func<bool> idleHandler)
		{
			lock (idleHandlers)
				idleHandlers.Add (idleHandler);

			return idleHandler;
		}

		/// <summary>
		///   Removes the specified idleHandler from processing.
		/// </summary>
		public void RemoveIdle (Func<bool> idleHandler)
		{
			lock (idleHandler)
				idleHandlers.Remove (idleHandler);
		}

		void AddTimeout (TimeSpan time, Timeout timeout)
		{
			timeouts.Add ((DateTime.UtcNow + time).Ticks, timeout);
		}

		/// <summary>
		///   Adds a timeout to the mainloop.
		/// </summary>
		/// <remarks>
		///   When time time specified passes, the callback will be invoked.
		///   If the callback returns true, the timeout will be reset, repeating
		///   the invocation. If it returns false, the timeout will stop.
		///
		///   The returned value is a token that can be used to stop the timeout
		///   by calling RemoveTimeout.
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
		public void RemoveTimeout (object token)
		{
			var idx = timeouts.IndexOfValue (token as Timeout);
			if (idx == -1)
				return;
			timeouts.RemoveAt (idx);
		}

		void RunTimers ()
		{
			long now = DateTime.UtcNow.Ticks;
			var copy = timeouts;
			timeouts = new SortedList<long, Timeout> ();
			foreach (var k in copy.Keys) {
				var timeout = copy [k];
				if (k < now) {
					if (timeout.Callback (this))
						AddTimeout (timeout.Span, timeout);
				} else
					timeouts.Add (k, timeout);
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
			driver.Wakeup ();
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
			return driver.EventsPending (wait);
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

			driver.MainIteration ();

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
