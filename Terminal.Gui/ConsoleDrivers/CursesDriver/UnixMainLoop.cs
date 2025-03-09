#nullable enable
//
// mainloop.cs: Linux/Curses MainLoop implementation.
//

using System.Runtime.InteropServices;

namespace Terminal.Gui;

/// <summary>Unix main loop, suitable for using on Posix systems</summary>
/// <remarks>
///     In addition to the general functions of the MainLoop, the Unix version can watch file descriptors using the
///     AddWatch methods.
/// </remarks>
internal class UnixMainLoop : IMainLoopDriver
{
    /// <summary>Condition on which to wake up from file descriptor activity.  These match the Linux/BSD poll definitions.</summary>
    [Flags]
    public enum Condition : short
    {
        /// <summary>There is data to read</summary>
        PollIn = 1,

        /// <summary>Writing to the specified descriptor will not block</summary>
        PollOut = 4,

        /// <summary>There is urgent data to read</summary>
        PollPri = 2,

        /// <summary>Error condition on output</summary>
        PollErr = 8,

        /// <summary>Hang-up on output</summary>
        PollHup = 16,

        /// <summary>File descriptor is not open.</summary>
        PollNval = 32
    }

    public const int KEY_RESIZE = unchecked((int)0xffffffffffffffff);
    private static readonly nint _ignore = Marshal.AllocHGlobal (1);

    private readonly CursesDriver _cursesDriver;
    private readonly Dictionary<int, Watch> _descriptorWatchers = new ();
    private readonly int [] _wakeUpPipes = new int [2];
    private MainLoop? _mainLoop;
    private bool _pollDirty = true;
    private Pollfd []? _pollMap;
    private bool _winChanged;

    public UnixMainLoop (IConsoleDriver IConsoleDriver)
    {
        ArgumentNullException.ThrowIfNull (IConsoleDriver);

        _cursesDriver = (CursesDriver)IConsoleDriver;
    }

    void IMainLoopDriver.Wakeup ()
    {
        if (!ConsoleDriver.RunningUnitTests)
        {
            write (_wakeUpPipes [1], _ignore, 1);
        }
    }

    void IMainLoopDriver.Setup (MainLoop mainLoop)
    {
        _mainLoop = mainLoop;

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        try
        {
            pipe (_wakeUpPipes);

            AddWatch (
                      _wakeUpPipes [0],
                      Condition.PollIn,
                      _ =>
                      {
                          read (_wakeUpPipes [0], _ignore, 1);

                          return true;
                      }
                     );
        }
        catch (DllNotFoundException e)
        {
            throw new NotSupportedException ("libc not found", e);
        }
    }

    bool IMainLoopDriver.EventsPending ()
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return true;
        }

        UpdatePollMap ();

        bool checkTimersResult = _mainLoop!.TimedEvents.CheckTimersAndIdleHandlers (out int pollTimeout);

        int n = poll (_pollMap!, (uint)_pollMap!.Length, pollTimeout);

        if (n == KEY_RESIZE)
        {
            _winChanged = true;
        }

        return checkTimersResult || n >= KEY_RESIZE;
    }

    void IMainLoopDriver.Iteration ()
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        if (_winChanged)
        {
            _winChanged = false;
            _cursesDriver.ProcessInput ();

            // This is needed on the mac. See https://github.com/gui-cs/Terminal.Gui/pull/2922#discussion_r1365992426
            _cursesDriver.ProcessWinChange ();
        }

        if (_pollMap is null)
        {
            return;
        }

        foreach (Pollfd p in _pollMap)
        {
            if (p.revents == 0)
            {
                continue;
            }

            if (!_descriptorWatchers.TryGetValue (p.fd, out Watch? watch))
            {
                continue;
            }

            if (!watch.Callback (_mainLoop!))
            {
                _descriptorWatchers.Remove (p.fd);
            }
        }
    }

    void IMainLoopDriver.TearDown ()
    {
        _descriptorWatchers.Clear ();

        _mainLoop = null;
    }

    /// <summary>Watches a file descriptor for activity.</summary>
    /// <remarks>
    ///     When the condition is met, the provided callback is invoked.  If the callback returns false, the watch is
    ///     automatically removed. The return value is a token that represents this watch, you can use this token to remove the
    ///     watch by calling RemoveWatch.
    /// </remarks>
    internal object AddWatch (int fileDescriptor, Condition condition, Func<MainLoop, bool> callback)
    {
        ArgumentNullException.ThrowIfNull (callback);

        var watch = new Watch { Condition = condition, Callback = callback, File = fileDescriptor };
        _descriptorWatchers [fileDescriptor] = watch;
        _pollDirty = true;

        return watch;
    }

    /// <summary>Removes an active watch from the mainloop.</summary>
    /// <remarks>The token parameter is the value returned from AddWatch</remarks>
    internal void RemoveWatch (object token)
    {
        if (!ConsoleDriver.RunningUnitTests)
        {
            if (token is not Watch watch)
            {
                return;
            }

            _descriptorWatchers.Remove (watch.File);
        }
    }

    private void UpdatePollMap ()
    {
        if (!_pollDirty)
        {
            return;
        }

        _pollDirty = false;

        _pollMap = new Pollfd [_descriptorWatchers.Count];
        var i = 0;

        foreach (int fd in _descriptorWatchers.Keys)
        {
            _pollMap [i].fd = fd;
            _pollMap [i].events = (short)_descriptorWatchers [fd].Condition;
            i++;
        }
    }

    internal void WriteRaw (string ansiRequest)
    {
        // Write to stdout (fd 1)
        write (STDOUT_FILENO, ansiRequest, ansiRequest.Length);
    }

    [DllImport ("libc")]
    private static extern int pipe ([In][Out] int [] pipes);

    [DllImport ("libc")]
    private static extern int poll ([In] [Out] Pollfd [] ufds, uint nfds, int timeout);

    [DllImport ("libc")]
    private static extern int read (int fd, nint buf, nint n);

    [DllImport ("libc")]
    private static extern int write (int fd, nint buf, nint n);

    // File descriptor for stdout
    private const int STDOUT_FILENO = 1;

    [DllImport ("libc")]
    private static extern int write (int fd, string buf, int n);

    [StructLayout (LayoutKind.Sequential)]
    private struct Pollfd
    {
        public int fd;
        public short events;
        public readonly short revents;
    }

    private class Watch
    {
        // BUGBUG: Fix this nullable issue.
        public Func<MainLoop, bool> Callback;
        public Condition Condition;
        public int File;
    }
}
