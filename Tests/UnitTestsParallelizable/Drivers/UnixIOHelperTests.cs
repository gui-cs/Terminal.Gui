using System.Runtime.InteropServices;
using Terminal.Gui.Drivers;

namespace DriverTests;

/// <summary>
///     Tests for <see cref="UnixIOHelper"/> polling semantics on Unix-like platforms.
/// </summary>
[Collection ("Driver Tests")]
public class UnixIOHelperTests
{
    [DllImport ("libc", SetLastError = true)]
    private static extern int pipe (int [] pipefd);

    [DllImport ("libc", SetLastError = true)]
    private static extern int close (int fd);

    [Fact]
    // Copilot
    public void IsInputAvailable_ReturnsTrue_WhenPollReportsReadableData ()
    {
        if (!OperatingSystem.IsLinux () && !OperatingSystem.IsMacOS () && !OperatingSystem.IsFreeBSD ())
        {
            return;
        }

        int [] pipeFds = new int [2];
        Assert.Equal (0, pipe (pipeFds));

        try
        {
            byte [] payload = [0x41];
            Assert.Equal (1, UnixIOHelper.write (pipeFds [1], payload, payload.Length));

            UnixIOHelper.Pollfd [] pollMap =
            [
                new ()
                {
                    fd = pipeFds [0],
                    events = (short)UnixIOHelper.Condition.PollIn
                }
            ];

            Assert.True (UnixIOHelper.IsInputAvailable (pollMap, 0));
        }
        finally
        {
            close (pipeFds [0]);
            close (pipeFds [1]);
        }
    }

    [Fact]
    // Copilot
    public void IsInputAvailable_ReturnsFalse_WhenPollReportsNonReadableEvent ()
    {
        if (!OperatingSystem.IsLinux () && !OperatingSystem.IsMacOS () && !OperatingSystem.IsFreeBSD ())
        {
            return;
        }

        int [] pipeFds = new int [2];
        Assert.Equal (0, pipe (pipeFds));

        try
        {
            Assert.Equal (0, close (pipeFds [0]));

            UnixIOHelper.Pollfd [] pollMap =
            [
                new ()
                {
                    fd = pipeFds [0],
                    events = (short)UnixIOHelper.Condition.PollIn
                }
            ];

            Assert.False (UnixIOHelper.IsInputAvailable (pollMap, 0));
        }
        finally
        {
            close (pipeFds [1]);
        }
    }
}
