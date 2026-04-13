using UnitTests;

namespace DriverTests.AnsiHandling;

public class ProgressIndicatorTests : TestDriverBase
{
    // Copilot
    [Fact]
    public void Clear_WithoutPriorWrite_DoesNotWriteAnything ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        ProgressIndicator progressIndicator = new (driver);

        progressIndicator.Clear ();

        Assert.DoesNotContain (EscSeqUtils.OSC_ClearProgress (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void SetValue_ThenClear_WritesExpectedSequences ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        ProgressIndicator progressIndicator = new (driver);

        progressIndicator.SetValue (42);
        progressIndicator.Clear ();

        string output = driver.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_SetProgressValue (42), output, StringComparison.Ordinal);
        Assert.Contains (EscSeqUtils.OSC_ClearProgress (), output, StringComparison.Ordinal);
    }

    [Fact]
    public void SetValue_DuplicateValue_WritesOnce ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        ProgressIndicator progressIndicator = new (driver);

        progressIndicator.SetValue (42);
        int outputLength = driver.GetOutput ().GetLastOutput ().Length;

        progressIndicator.SetValue (42);

        Assert.Equal (outputLength, driver.GetOutput ().GetLastOutput ().Length);
    }

    [Fact]
    public void SetIndeterminate_WritesExpectedSequence ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        ProgressIndicator progressIndicator = new (driver);

        progressIndicator.SetIndeterminate ();

        Assert.Contains (EscSeqUtils.OSC_SetProgressIndeterminate (), driver.GetOutput ().GetLastOutput (), StringComparison.Ordinal);
    }

    [Fact]
    public void WriteMethods_SkipLegacyConsole ()
    {
        DriverImpl driver = (DriverImpl)CreateTestDriver ();
        ProgressIndicator progressIndicator = new (driver);
        driver.IsLegacyConsole = true;

        progressIndicator.SetValue (42);
        progressIndicator.SetIndeterminate ();
        progressIndicator.Clear ();

        string output = driver.GetOutput ().GetLastOutput ();
        Assert.DoesNotContain (EscSeqUtils.OSC_SetProgressValue (42), output, StringComparison.Ordinal);
        Assert.DoesNotContain (EscSeqUtils.OSC_SetProgressIndeterminate (), output, StringComparison.Ordinal);
        Assert.DoesNotContain (EscSeqUtils.OSC_ClearProgress (), output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData (true, false, "xterm-256color", true)]
    [InlineData (false, false, "xterm-256color", false)]
    [InlineData (true, true, "xterm-256color", false)]
    [InlineData (true, false, "dumb", false)]
    public void IsSupportedOutput_ReturnsExpectedResult (bool outputAttached, bool outputRedirected, string term, bool expected)
    {
        Assert.Equal (expected, ProgressIndicator.IsSupportedOutput (outputAttached, outputRedirected, term));
    }
}
