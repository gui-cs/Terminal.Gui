using Moq;

namespace DriverTests.AnsiHandling;

public class KittyKeyboardProtocolDetectorTests
{
    [Fact]
    public void Detect_QueuesKittyQuery_AndReturnsSupportedResult_WhenTerminalResponds ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);
        driverMock.SetupSet (d => d.KittyKeyboardCapabilities = It.IsAny<KittyKeyboardCapabilities?> ());

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (request =>
                                                        {
                                                            Assert.Equal (EscSeqUtils.CSI_QueryKittyKeyboardFlags.Request, request.Request);
                                                            Assert.Equal (EscSeqUtils.CSI_QueryKittyKeyboardFlags.Terminator, request.Terminator);
                                                            Assert.Equal (EscSeqUtils.CSI_QueryKittyKeyboardFlags.Value, request.Value);
                                                            request.ResponseReceived ("\u001B[?31u");
                                                        });

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        KittyKeyboardCapabilities? result = null;

        detector.Detect (r => result = r);

        Assert.NotNull (result);
        Assert.True (result.IsSupported);
        Assert.Equal (EscSeqUtils.KittyKeyboardRequestedFlags, result.Flags);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.Once);

        driverMock.VerifySet (d => d.KittyKeyboardCapabilities =
                                       It.Is<KittyKeyboardCapabilities?> (k => k != null
                                                                               && k.IsSupported
                                                                               && k.Flags == EscSeqUtils.KittyKeyboardRequestedFlags),
                              Times.Once);
    }

    [Fact]
    public void Detect_ReturnsUnsupportedResult_WhenTerminalDoesNotRespond ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (request => request.Abandoned?.Invoke ());

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        KittyKeyboardCapabilities? result = null;

        detector.Detect (r => result = r);

        Assert.NotNull (result);
        Assert.False (result.IsSupported);
        Assert.Equal (0, (int)result.Flags);
        driverMock.VerifySet (d => d.KittyKeyboardCapabilities = It.IsAny<KittyKeyboardCapabilities?> (), Times.Never);
    }

    [Fact]
    public void Detect_SkipsLegacyConsole ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (true);

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        KittyKeyboardCapabilities? result = null;

        detector.Detect (r => result = r);

        Assert.NotNull (result);
        Assert.False (result.IsSupported);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.Never);
        driverMock.VerifySet (d => d.KittyKeyboardCapabilities = It.IsAny<KittyKeyboardCapabilities?> (), Times.Never);
    }

    [Theory]
    [InlineData ("\u001B[?0u", true, 0)]
    [InlineData ("\u001B[?31u", true, 31)]
    [InlineData ("[?1u", true, 1)]
    [InlineData ("\u001B[1;5u", false, 0)]
    [InlineData ("", false, 0)]
    public void ParseResponse_ReturnsExpectedResult (string response, bool isSupported, int supportedFlags)
    {
        KittyKeyboardCapabilities result = KittyKeyboardProtocolDetector.ParseResponse (response);

        Assert.Equal (isSupported, result.IsSupported);
        Assert.Equal ((KittyKeyboardFlags)supportedFlags, result.Flags);
    }
}
