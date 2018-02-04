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
using Mono.Unix.Native;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace Mono.Terminal {

	/// <summary>
	///   Simple main loop implementation that can be used to monitor
	///   file descriptor, run timers and idle handlers.
	/// </summary>
	public class MainLoop {
		/// <summary>
		///   Condition on which to wake up from file descriptor activity
		/// </summary>
		[Flags]
		public enum Condition {
			/// <summary>
			/// There is data to read
			/// </summary>
			PollIn = 1,
			/// <summary>
			/// Writing to the specified descriptor will not block
			/// </summary>
			PollOut = 2,
			/// <summary>
			/// There is urgent data to read
			/// </summary>
			PollPri = 4,
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
			public Func<MainLoop,bool> Callback;
		}

		class Timeout {
			public TimeSpan Span;
			public Func<MainLoop,bool> Callback;
		}

		Dictionary <int, Watch> descriptorWatchers = new Dictionary<int,Watch>();
		SortedList <double, Timeout> timeouts = new SortedList<double,Timeout> ();
		List<Func<bool>> idleHandlers = new List<Func<bool>> ();
		
		Pollfd [] pollmap;
		bool poll_dirty = true;
		int [] wakeupPipes = new int [2];
		static IntPtr ignore = Marshal.AllocHGlobal (1);
		
		/// <summary>
		///  Default constructor
		/// </summary>
		public MainLoop ()
		{
			Syscall.pipe (wakeupPipes);
			AddWatch (wakeupPipes [0], Condition.PollIn, ml => {
				Syscall.read (wakeupPipes [0], ignore, 1);
				return true;
			});
		}

		void Wakeup ()
		{
			Syscall.write (wakeupPipes [1], ignore, 1);
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
			Wakeup ();
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
		public object AddWatch (int fileDescriptor, Condition condition, Func<MainLoop,bool> callback)
		{
			if (callback == null)
				throw new ArgumentNullException ("callback");

			var watch = new Watch () { Condition = condition, Callback = callback, File = fileDescriptor };
			descriptorWatchers [fileDescriptor] = watch;
			poll_dirty = true;
			return watch;
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
				throw new ArgumentNullException ("callback");
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

		static PollEvents MapCondition (Condition condition)
		{
			PollEvents ret = 0;
			if ((condition & Condition.PollIn) != 0)
				ret |= PollEvents.POLLIN;
			if ((condition & Condition.PollOut) != 0)
				ret |= PollEvents.POLLOUT;
			if ((condition & Condition.PollPri) != 0)
				ret |= PollEvents.POLLPRI;
			if ((condition & Condition.PollErr) != 0)
				ret |= PollEvents.POLLERR;
			if ((condition & Condition.PollHup) != 0)
				ret |= PollEvents.POLLHUP;
			if ((condition & Condition.PollNval) != 0)
				ret |= PollEvents.POLLNVAL;
			return ret;
		}
		
		void UpdatePollMap ()
		{
			if (!poll_dirty)
				return;
			poll_dirty = false;

			pollmap = new Pollfd [descriptorWatchers.Count];
			int i = 0;
			foreach (var fd in descriptorWatchers.Keys){
				pollmap [i].fd = fd;
				pollmap [i].events = MapCondition (descriptorWatchers [fd].Condition);
				i++;
			}
		}

		void RunTimers ()
		{
			long now = DateTime.UtcNow.Ticks;
			var copy = timeouts;
			timeouts = new SortedList<double,Timeout> ();
			foreach (var k in copy.Keys){
				if (k >= now)
					break;

				var timeout = copy [k];
				if (timeout.Callback (this))
					AddTimeout (timeout.Span, timeout);
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
			Wakeup ();
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
			long now = DateTime.UtcNow.Ticks;
			int pollTimeout, n;
			if (timeouts.Count > 0)
				pollTimeout = (int) ((timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
			else
				pollTimeout = -1;
			
			if (!wait)
				pollTimeout = 0;
			
			UpdatePollMap ();

			n = Syscall.poll (pollmap, (uint) pollmap.Length, pollTimeout);
			int ic;
			lock (idleHandlers)
				ic = idleHandlers.Count;
			return n > 0 || timeouts.Count > 0 && ((timeouts.Keys [0] - DateTime.UtcNow.Ticks) < 0) || ic > 0;
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
			
			foreach (var p in pollmap){
				Watch watch;

				if (p.revents == 0)
					continue;

				if (!descriptorWatchers.TryGetValue (p.fd, out watch))
					continue;
				if (!watch.Callback (this))
					descriptorWatchers.Remove (p.fd);
			}
			if (idleHandlers.Count > 0)
				RunIdle ();
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
