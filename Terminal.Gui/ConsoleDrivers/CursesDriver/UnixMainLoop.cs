//
// mainloop.cs: Linux/Curses MainLoop implementation.
//

#region

using System.Runtime.InteropServices;

#endregion

namespace Terminal.Gui {
    /// <summary>
    /// Unix main loop, suitable for using on Posix systems
    /// </summary>
    /// <remarks>
    /// In addition to the general functions of the MainLoop, the Unix version
    /// can watch file descriptors using the AddWatch methods.
    /// </remarks>
    internal class UnixMainLoop : IMainLoopDriver {
        private CursesDriver _cursesDriver;

        public UnixMainLoop (ConsoleDriver consoleDriver = null) {
            // UnixDriver doesn't use the consoleDriver parameter, but the WindowsDriver does.
            _cursesDriver = (CursesDriver)Application.Driver;
        }

        public const int KEY_RESIZE = unchecked ((int)0xffffffffffffffff);

        [StructLayout (LayoutKind.Sequential)]
        struct Pollfd {
            public int fd;
            public short events, revents;
        }

        /// <summary>
        /// Condition on which to wake up from file descriptor activity.  These match the Linux/BSD poll definitions.
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
            /// Error condition on output
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

        readonly Dictionary<int, Watch> _descriptorWatchers = new Dictionary<int, Watch> ();
        [DllImport ("libc")] extern static int poll ([In, Out] Pollfd[] ufds, uint nfds, int timeout);
        [DllImport ("libc")] extern static int pipe ([In, Out] int[] pipes);
        [DllImport ("libc")] extern static int read (int fd, IntPtr buf, IntPtr n);
        [DllImport ("libc")] extern static int write (int fd, IntPtr buf, IntPtr n);
        Pollfd[] _pollMap;
        bool _pollDirty = true;
        readonly int[] _wakeUpPipes = new int [2];
        static readonly IntPtr _ignore = Marshal.AllocHGlobal (1);
        MainLoop _mainLoop;
        bool _winChanged;

        void IMainLoopDriver.Wakeup () {
            if (!ConsoleDriver.RunningUnitTests) {
                write (_wakeUpPipes[1], _ignore, (IntPtr)1);
            }
        }

        void IMainLoopDriver.Setup (MainLoop mainLoop) {
            this._mainLoop = mainLoop;
            if (ConsoleDriver.RunningUnitTests) {
                return;
            }

            try {
                pipe (_wakeUpPipes);
                AddWatch (
                          _wakeUpPipes[0],
                          Condition.PollIn,
                          ml => {
                              read (_wakeUpPipes[0], _ignore, (IntPtr)1);

                              return true;
                          });
            }
            catch (DllNotFoundException e) {
                throw new NotSupportedException ("libc not found", e);
            }
        }

        /// <summary>
        /// Removes an active watch from the mainloop.
        /// </summary>
        /// <remarks>
        /// The token parameter is the value returned from AddWatch
        /// </remarks>
        internal void RemoveWatch (object token) {
            if (!ConsoleDriver.RunningUnitTests) {
                if (token is not Watch watch) {
                    return;
                }

                _descriptorWatchers.Remove (watch.File);
            }
        }

        /// <summary>
        /// Watches a file descriptor for activity.
        /// </summary>
        /// <remarks>
        /// When the condition is met, the provided callback
        /// is invoked.  If the callback returns false, the
        /// watch is automatically removed.
        /// 
        /// The return value is a token that represents this watch, you can
        /// use this token to remove the watch by calling RemoveWatch.
        /// </remarks>
        internal object AddWatch (int fileDescriptor, Condition condition, Func<MainLoop, bool> callback) {
            if (callback == null) {
                throw new ArgumentNullException (nameof (callback));
            }

            var watch = new Watch () { Condition = condition, Callback = callback, File = fileDescriptor };
            _descriptorWatchers[fileDescriptor] = watch;
            _pollDirty = true;

            return watch;
        }

        void UpdatePollMap () {
            if (!_pollDirty) {
                return;
            }

            _pollDirty = false;

            _pollMap = new Pollfd [_descriptorWatchers.Count];
            var i = 0;
            foreach (var fd in _descriptorWatchers.Keys) {
                _pollMap[i].fd = fd;
                _pollMap[i].events = (short)_descriptorWatchers[fd].Condition;
                i++;
            }
        }

        bool IMainLoopDriver.EventsPending () {
            UpdatePollMap ();

            var checkTimersResult = _mainLoop.CheckTimersAndIdleHandlers (out var pollTimeout);

            var n = poll (_pollMap, (uint)_pollMap.Length, pollTimeout);

            if (n == KEY_RESIZE) {
                _winChanged = true;
            }

            return checkTimersResult || n >= KEY_RESIZE;
        }

        void IMainLoopDriver.Iteration () {
            if (_winChanged) {
                _winChanged = false;
                _cursesDriver.ProcessInput ();

                // This is needed on the mac. See https://github.com/gui-cs/Terminal.Gui/pull/2922#discussion_r1365992426
                _cursesDriver.ProcessWinChange ();
            }

            if (_pollMap == null) return;

            foreach (var p in _pollMap) {
                Watch watch;

                if (p.revents == 0) {
                    continue;
                }

                if (!_descriptorWatchers.TryGetValue (p.fd, out watch)) {
                    continue;
                }

                if (!watch.Callback (this._mainLoop)) {
                    _descriptorWatchers.Remove (p.fd);
                }
            }
        }

        void IMainLoopDriver.TearDown () {
            _descriptorWatchers?.Clear ();

            _mainLoop = null;
        }
    }
}
