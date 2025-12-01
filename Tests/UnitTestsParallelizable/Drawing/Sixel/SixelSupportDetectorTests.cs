#nullable enable
using Moq;

namespace UnitTests_Parallelizable.DrawingTests;

public class SixelSupportDetectorTests
{
    [Fact]
    public void Detect_SetsSupportedAndResolution_WhenDeviceAttributesContain4_AndResolutionResponds()
    {
        // Arrange
        var driverMock = new Mock<IDriver>(MockBehavior.Strict);

        // Expect QueueAnsiRequest to be called at least twice:
        // 1) CSI_SendDeviceAttributes (terminator "c")
        // 2) CSI_RequestSixelResolution (terminator "t")
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          // Respond to the SendDeviceAttributes request with a value that indicates support (contains "4")
                                                          if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                          {
                                                              req.ResponseReceived.Invoke ("1;4;7c");
                                                          }
                                                          else if (req.Request == EscSeqUtils.CSI_RequestSixelResolution.Request)
                                                          {
                                                              // Reply with a resolution response matching regex "\[\d+;(\d+);(\d+)t$"
                                                              // Group 1 -> ry, Group 2 -> rx. The detector constructs resolution as new(rx, ry)
                                                              req.ResponseReceived.Invoke ("[6;20;10t");
                                                          }
                                                          else
                                                          {
                                                              // Any other request - call abandoned to avoid hanging
                                                              req.Abandoned?.Invoke ();
                                                          }
                                                      })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);

        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.NotNull (final);
        Assert.True (final.IsSupported); // Response contained "4"
        // Resolution should be constructed as new(rx, ry) where rx=10, ry=20 from our reply "[6;20;10t"
        Assert.Equal (10, final.Resolution.Width);
        Assert.Equal (20, final.Resolution.Height);

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (2));
    }

    [Fact]
    public void Detect_DoesNotSetSupported_WhenDeviceAttributesDoNotContain4()
    {
        // Arrange
        var driverMock = new Mock<IDriver>(MockBehavior.Strict);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          // SendDeviceAttributes -> reply without "4"
                                                          if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                          {
                                                              req.ResponseReceived.Invoke ("1;0;7c");
                                                          }
                                                          else
                                                          {
                                                              // Any other requests should be abandoned
                                                              req.Abandoned?.Invoke ();
                                                          }
                                                      })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);

        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.NotNull (final);
        Assert.False (final.IsSupported);
        // On no support, the direct resolution request path isn't followed so resolution remains the default
        Assert.Equal (10, final.Resolution.Width);
        Assert.Equal (20, final.Resolution.Height);

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (1));
    }

    [Fact]
    public void Detect_SetsSupported_WhenIsVirtualTerminalIsTrue ()
    {
        // Arrange
        var abandoned = false;
        var driverMock = new Mock<IDriver> (MockBehavior.Strict);
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                        {
                                                            // Abandon all requests
                                                            req.Abandoned?.Invoke ();
                                                            abandoned = true;
                                                        })
                  .Verifiable ();
        var detector = new SixelSupportDetector (driverMock.Object);
        // Mock IsVirtualTerminal to return true
        var isVirtualTerminalMethod = typeof (SixelSupportDetector).GetMethod ("IsVirtualTerminal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull (isVirtualTerminalMethod);
        isVirtualTerminalMethod!.Invoke (detector, null);
        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.NotNull (final);
        // Not a real VT, so should be supported
        Assert.False (final.IsSupported);
        Assert.True (abandoned);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (1));
    }
}
