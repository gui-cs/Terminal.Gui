//
// mainloop.cs: Simple managed mainloop implementation.
//
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Terminal.Gui {
	/// <summary>
	/// Unix main loop, suitable for using on Posix systems
	/// </summary>
	/// <remarks>
	/// In addition to the general functions of the mainloop, the Unix version
	/// can watch file descriptors using the AddWatch methods.
	/// </remarks>
	internal class UnixMainLoop : IMainLoopDriver {
		public UnixMainLoop (ConsoleDriver consoleDriver = null)
		{
			// UnixDriver doesn't use the consoleDriver parameter, but the WindowsDriver does.
		}

		public const int KEY_RESIZE = unchecked((int)0xffffffffffffffff);

		[StructLayout (LayoutKind.Sequential)]
		struct Pollfd {
			public int fd;
			public short events, revents;
		}

		/// <summary>
		///	Condition on which to wake up from file descriptor activity.  These match the Linux/BSD poll definitions.
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

		Dictionary<int, Watch> _descriptorWatchers = new Dictionary<int, Watch> ();

		[DllImport ("libc")]
		extern static int poll ([In, Out] Pollfd [] ufds, uint nfds, int timeout);

		[DllImport ("libc")]
		extern static int pipe ([In, Out] int [] pipes);

		[DllImport ("libc")]
		extern static int read (int fd, IntPtr buf, IntPtr n);

		[DllImport ("libc")]
		extern static int write (int fd, IntPtr buf, IntPtr n);

		Pollfd [] _pollmap;
		bool _poll_dirty = true;
		int [] _wakeupPipes = new int [2];
		static IntPtr _ignore = Marshal.AllocHGlobal (1);
		MainLoop _mainLoop;
		bool _winChanged;

		public Action WinChanged;

		void IMainLoopDriver.Wakeup ()
		{
			if (!ConsoleDriver.RunningUnitTests) {
				write (_wakeupPipes [1], _ignore, (IntPtr)1);
			}
		}

		void IMainLoopDriver.Setup (MainLoop mainLoop)
		{
			this._mainLoop = mainLoop;
			if (ConsoleDriver.RunningUnitTests) {
				return;
			}

			try {
				pipe (_wakeupPipes);
				AddWatch (_wakeupPipes [0], Condition.PollIn, ml => {
					read (_wakeupPipes [0], _ignore, (IntPtr)1);
					return true;
				});
			} catch (DllNotFoundException e) {
				throw new NotSupportedException ("liblibc not found", e);
			}
		}

		/// <summary>
		///	Removes an active watch from the mainloop.
		/// </summary>
		/// <remarks>
		///	The token parameter is the value returned from AddWatch
		/// </remarks>
		public void RemoveWatch (object token)
		{
			if (!ConsoleDriver.RunningUnitTests) {
				var watch = token as Watch;
				if (watch == null)
					return;
				_descriptorWatchers.Remove (watch.File);
			}
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
			if (callback == null) {
				throw new ArgumentNullException (nameof (callback));
			}

			var watch = new Watch () { Condition = condition, Callback = callback, File = fileDescriptor };
			_descriptorWatchers [fileDescriptor] = watch;
			_poll_dirty = true;
			return watch;
		}

		void UpdatePollMap ()
		{
			if (!_poll_dirty) {
				return;
			}
			_poll_dirty = false;

			_pollmap = new Pollfd [_descriptorWatchers.Count];
			int i = 0;
			foreach (var fd in _descriptorWatchers.Keys) {
				_pollmap [i].fd = fd;
				_pollmap [i].events = (short)_descriptorWatchers [fd].Condition;
				i++;
			}
		}

		bool IMainLoopDriver.EventsPending (bool wait)
		{
			UpdatePollMap ();

			bool checkTimersResult = _mainLoop.CheckTimers (wait, out var pollTimeout);

			var n = poll (_pollmap, (uint)_pollmap.Length, pollTimeout);

			if (n == KEY_RESIZE) {
				_winChanged = true;
			}

			return checkTimersResult || n >= KEY_RESIZE;
		}

		void IMainLoopDriver.Iteration ()
		{
			if (_winChanged) {
				_winChanged = false;
				WinChanged?.Invoke ();
			}
			if (_pollmap != null) {
				foreach (var p in _pollmap) {
					Watch watch;

					if (p.revents == 0)
						continue;

					if (!_descriptorWatchers.TryGetValue (p.fd, out watch))
						continue;
					if (!watch.Callback (this._mainLoop))
						_descriptorWatchers.Remove (p.fd);
				}
			}
		}
	}
}
