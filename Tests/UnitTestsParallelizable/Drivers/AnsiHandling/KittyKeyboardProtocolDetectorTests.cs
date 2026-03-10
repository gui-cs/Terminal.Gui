#nullable enable
using Moq;

namespace DriverTests.AnsiHandling;

public class KittyKeyboardProtocolDetectorTests
{
    [Fact]
    public void Detect_QueuesKittyQuery_AndReturnsSupportedResult_WhenTerminalResponds ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (request =>
                                                       {
                                                           Assert.Equal (EscSeqUtils.CSI_QueryKittyKeyboardFlags.Request, request.Request);
                                                           Assert.Equal (EscSeqUtils.CSI_QueryKittyKeyboardFlags.Terminator, request.Terminator);
                                                           Assert.Equal (EscSeqUtils.CSI_QueryKittyKeyboardFlags.Value, request.Value);
                                                           request.ResponseReceived ("\u001B[?31u");
                                                       });

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        KittyKeyboardProtocolResult? result = null;

        detector.Detect (r => result = r);

        Assert.NotNull (result);
        Assert.True (result.IsSupported);
        Assert.Equal (31, result.SupportedFlags);
        Assert.Equal (EscSeqUtils.KittyKeyboardPhase1Flags, result.EnabledFlags);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.Once);
    }

    [Fact]
    public void Detect_ReturnsUnsupportedResult_WhenTerminalDoesNotRespond ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (request => request.Abandoned?.Invoke ());

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        KittyKeyboardProtocolResult? result = null;

        detector.Detect (r => result = r);

        Assert.NotNull (result);
        Assert.False (result.IsSupported);
        Assert.Equal (0, result.SupportedFlags);
        Assert.Equal (0, result.EnabledFlags);
    }

    [Fact]
    public void Detect_SkipsLegacyConsole ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (true);

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        KittyKeyboardProtocolResult? result = null;

        detector.Detect (r => result = r);

        Assert.NotNull (result);
        Assert.False (result.IsSupported);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.Never);
    }

    [Theory]
    [InlineData ("\u001B[?0u", true, 0)]
    [InlineData ("\u001B[?31u", true, 31)]
    [InlineData ("[?1u", true, 1)]
    [InlineData ("\u001B[1;5u", false, 0)]
    [InlineData ("", false, 0)]
    public void ParseResponse_ReturnsExpectedResult (string response, bool isSupported, int supportedFlags)
    {
        KittyKeyboardProtocolResult result = KittyKeyboardProtocolDetector.ParseResponse (response);

        Assert.Equal (isSupported, result.IsSupported);
        Assert.Equal (supportedFlags, result.SupportedFlags);
    }
}
