// Copilot - Claude Sonnet 4.6

namespace DrawingTests;

public class KittyGraphicsSupportDetectorTests
{
    [Fact]
    public void Detect_WhenKittyWindowIdSet_ReturnsSupported ()
    {
        string? previous = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", "1");
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", null);

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previous);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_WhenTermProgramIsKitty_ReturnsSupported ()
    {
        string? previous = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", "kitty");

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previous);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_WhenTermProgramIsGhostty_ReturnsSupported ()
    {
        string? previous = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", "ghostty");

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previous);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_WhenTermProgramIsGhostty_CaseInsensitive ()
    {
        string? previous = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", "Ghostty");

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previous);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_WhenNoEnvVarsSet_ReturnsNotSupported ()
    {
        string? previousWindowId = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", "xterm");

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.False (result.IsSupported);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previousWindowId);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_NotSupported_HasDefaultResolution ()
    {
        string? previousWindowId = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", null);
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", null);

        try
        {
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.Equal (new Size (10, 20), result.Resolution);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previousWindowId);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }

    [Fact]
    public void Detect_Supported_NoDriver_HasDefaultResolution ()
    {
        string? previousWindowId = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");
        string? previousTermProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", "1");
        Environment.SetEnvironmentVariable ("TERM_PROGRAM", null);

        try
        {
            // Detector without a driver cannot query for resolution — defaults to 10×20
            KittyGraphicsSupportDetector detector = new ();
            KittyGraphicsSupportResult? result = null;

            detector.Detect (r => result = r);

            Assert.NotNull (result);
            Assert.True (result.IsSupported);
            Assert.Equal (new Size (10, 20), result.Resolution);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("KITTY_WINDOW_ID", previousWindowId);
            Environment.SetEnvironmentVariable ("TERM_PROGRAM", previousTermProgram);
        }
    }
}
