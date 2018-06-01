//
// mainloop.cs: Simple managed mainloop implementation.
//
// Authors:
//   Miguel de Icaza (miguel.de.icaza@gmail.com)
//
// Copyright (C) 2011 Novell (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mono.Terminal {

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
		void MainIteration ();
	}

	/// <summary>
	/// Unix main loop, suitable for using on Posix systems
	/// </summary>
	/// <remarks>
	/// In addition to the general functions of the mainloop, the Unix version
	/// can watch file descriptors using the AddWatch methods.
	/// </remarks>
	public class UnixMainLoop : IMainLoopDriver {
		[StructLayout (LayoutKind.Sequential)]
		struct Pollfd {
			public int fd;
			public short events, revents;
		}

		/// <summary>
		///   Condition on which to wake up from file descriptor activity.  These match the Linux/BSD poll definitions.
		/// </summary>
		[Flags]
		public enum Condition : short {
			/// <summary>
			/// There is data to read
			/// </summary>
			PollIn = 1,
			/// <summary>
			/// Writing to the specified descriptor will not block
			/// </summary>
			PollOut = 4,
			/// <summary>
			/// There is urgent data to read
			/// </summary>
			PollPri = 2,
			/// <summary>
			///  Error condition on output
			/// </summary>
			PollErr = 8,
			/// <summary>
			/// Hang-up on output
			/// </summary>
			PollHup = 16,
			/// <summary>
			/// File descriptor is not open.
			/// </summary>
			PollNval = 32
		}

		class Watch {
			public int File;
			public Condition Condition;
			public Func<MainLoop, bool> Callback;
		}

		Dictionary<int, Watch> descriptorWatchers = new Dictionary<int, Watch> ();

		[DllImport ("libc")]
		extern static int poll ([In, Out]Pollfd [] ufds, uint nfds, int timeout);

		[DllImport ("libc")]
		extern static int pipe ([In, Out]int [] pipes);

		[DllImport ("libc")]
		extern static int read (int fd, IntPtr buf, IntPtr n);

		[DllImport ("libc")]
		extern static int write (int fd, IntPtr buf, IntPtr n);

		Pollfd [] pollmap;
		bool poll_dirty = true;
		int [] wakeupPipes = new int [2];
		static IntPtr ignore = Marshal.AllocHGlobal (1);
		MainLoop mainLoop;

		void IMainLoopDriver.Wakeup ()
		{
			write (wakeupPipes [1], ignore, (IntPtr) 1);
		}

		void IMainLoopDriver.Setup (MainLoop mainLoop) {
			this.mainLoop = mainLoop;
			pipe (wakeupPipes);
			AddWatch (wakeupPipes [0], Condition.PollIn, ml => {
				read (wakeupPipes [0], ignore, (IntPtr)1);
				return true;
			});			
		}

		/// <summary>
		///   Removes an active watch from the mainloop.
		/// </summary>
		/// <remarks>
		///   The token parameter is the value returned from AddWatch
		/// </remarks>
		public void RemoveWatch (object token)
		{
			var watch = token as Watch;
			if (watch == null)
				return;
			descriptorWatchers.Remove (watch.File);
		}

		/// <summary>
		///  Watches a file descriptor for activity.
		/// </summary>
		/// <remarks>
		///  When the condition is met, the provided callback
		///  is invoked.  If the callback returns false, the
		///  watch is automatically removed.
		///
		///  The return value is a token that represents this watch, you can
		///  use this token to remove the watch by calling RemoveWatch.
		/// </remarks>
		public object AddWatch (int fileDescriptor, Condition condition, Func<MainLoop, bool> callback)
		{
			if (callback == null)
				throw new ArgumentNullException (nameof(callback));

			var watch = new Watch () { Condition = condition, Callback = callback, File = fileDescriptor };
			descriptorWatchers [fileDescriptor] = watch;
			poll_dirty = true;
			return watch;
		}

		void UpdatePollMap ()
		{
			if (!poll_dirty)
				return;
			poll_dirty = false;

			pollmap = new Pollfd [descriptorWatchers.Count];
			int i = 0;
			foreach (var fd in descriptorWatchers.Keys) {
				pollmap [i].fd = fd;
				pollmap [i].events = (short)descriptorWatchers [fd].Condition;
				i++;
			}
		}

		bool IMainLoopDriver.EventsPending (bool wait) 
		{
			long now = DateTime.UtcNow.Ticks;

			int pollTimeout, n;
			if (mainLoop.timeouts.Count > 0) {
				pollTimeout = (int)((mainLoop.timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
				if (pollTimeout < 0)
					return true;

			} else
				pollTimeout = -1;

			if (!wait)
				pollTimeout = 0;

			UpdatePollMap ();

			n = poll (pollmap, (uint)pollmap.Length, pollTimeout);
			int ic;
			lock (mainLoop.idleHandlers)
				ic = mainLoop.idleHandlers.Count;
			return n > 0 || mainLoop.timeouts.Count > 0 && ((mainLoop.timeouts.Keys [0] - DateTime.UtcNow.Ticks) < 0) || ic > 0;			
		}

		void IMainLoopDriver.MainIteration () 
		{
			if (pollmap != null) {
				foreach (var p in pollmap) {
					Watch watch;

					if (p.revents == 0)
						continue;

					if (!descriptorWatchers.TryGetValue (p.fd, out watch))
						continue;
					if (!watch.Callback (this.mainLoop))
						descriptorWatchers.Remove (p.fd);
				}
			}			
		}
	}

	/// <summary>
	/// Mainloop intended to be used with the .NET System.Console API, and can
	/// be used on Windows and Unix, it is cross platform but lacks things like
	/// file descriptor monitoring.
	/// </summary>
	class NetMainLoop : IMainLoopDriver {
		AutoResetEvent keyReady = new AutoResetEvent (false);
		AutoResetEvent waitForProbe = new AutoResetEvent (false);
		ConsoleKeyInfo? windowsKeyResult = null;
		public Action<ConsoleKeyInfo> WindowsKeyPressed;
		MainLoop mainLoop;

		public NetMainLoop () 
		{
		}

		void WindowsKeyReader ()
		{
			while (true) {
				waitForProbe.WaitOne ();
				windowsKeyResult = Console.ReadKey (true);
				keyReady.Set ();
			}
		}		

		void IMainLoopDriver.Setup (MainLoop mainLoop)
		{
			this.mainLoop = mainLoop;
			Thread readThread = new Thread (WindowsKeyReader);
			readThread.Start ();			
		}

		void IMainLoopDriver.Wakeup () 
		{
		}

		bool IMainLoopDriver.EventsPending (bool wait)
		{
			long now = DateTime.UtcNow.Ticks;

			int waitTimeout;
			if (mainLoop.timeouts.Count > 0) {
				waitTimeout = (int)((mainLoop.timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
				if (waitTimeout < 0)
					return true;
			} else
				waitTimeout = -1;

			if (!wait)
				waitTimeout = 0;

			windowsKeyResult = null;
			waitForProbe.Set ();
			keyReady.WaitOne (waitTimeout);
			return windowsKeyResult.HasValue;
		}

		void IMainLoopDriver.MainIteration ()
		{
			if (windowsKeyResult.HasValue) {
				if (WindowsKeyPressed!= null)
					WindowsKeyPressed (windowsKeyResult.Value);
				windowsKeyResult = null;
			}			
		}
	}

	/// <summary>
	///   Simple main loop implementation that can be used to monitor
	///   file descriptor, run timers and idle handlers.
	/// </summary>
	/// <remarks>
	///   Monitoring of file descriptors is only available on Unix, there
	///   does not seem to be a way of supporting this on Windows.
	/// </remarks>
	public class 	MainLoop {
		internal class Timeout {
			public TimeSpan Span;
			public Func<MainLoop,bool> Callback;
		}

		internal SortedList <long, Timeout> timeouts = new SortedList<long,Timeout> ();
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
			AddIdle (()=> {
				action ();
				return false;
			});
			driver.Wakeup ();
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
		public object AddTimeout (TimeSpan time, Func<MainLoop,bool> callback)
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
			timeouts = new SortedList<long,Timeout> ();
			foreach (var k in copy.Keys){
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
			lock (idleHandlers){
				iterate = idleHandlers;
				idleHandlers = new List<Func<bool>> ();
			}

			foreach (var idle in iterate){
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

			lock (idleHandlers){
				if (idleHandlers.Count > 0)
					RunIdle();
			}
		}
		
		/// <summary>
		///   Runs the mainloop.
		/// </summary>
		public void Run ()
		{
			bool prev = running;
			running = true;
			while (running){
				EventsPending (true);
				MainIteration ();
			}
			running = prev;
		}
	}
}
