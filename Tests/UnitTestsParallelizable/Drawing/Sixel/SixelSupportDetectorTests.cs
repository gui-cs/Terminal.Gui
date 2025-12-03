#nullable enable
using Moq;

namespace DrawingTests;

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

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Detect_SetsSupported_WhenIsVirtualTerminalIsTrueAndResponseContain4OrFalse (bool isVirtualTerminal)
    {
        // Arrange
        var responseReceived = false;
        var output = new FakeOutput ();
        output.IsVirtualTerminal = isVirtualTerminal;

        Mock<DriverImpl> driverMock = new (
                                           MockBehavior.Strict,
                                           new FakeInputProcessor (null!),
                                           new OutputBufferImpl (),
                                           output,
                                           new AnsiRequestScheduler (new AnsiResponseParser ()),
                                           new SizeMonitorImpl (output)
                                          );
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                        {
                                                            if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                            {
                                                                responseReceived = true;

                                                                if (isVirtualTerminal)
                                                                {
                                                                    // Response does contain "4" (so DAR indicates has sixel)
                                                                    req.ResponseReceived.Invoke ("?1;4;0;7c");
                                                                }
                                                                else
                                                                {
                                                                    // Response does NOT contain "4" (so DAR indicates no sixel)
                                                                    req.ResponseReceived.Invoke ("");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // Abandon all requests
                                                                req.Abandoned?.Invoke ();
                                                            }
                                                        })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);
        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.Equal (isVirtualTerminal, driverMock.Object.IsVirtualTerminal);
        Assert.NotNull (final);

        if (isVirtualTerminal)
        {
            Assert.True (final.IsSupported);
        }
        else
        {
            // Not a real VT, so should be supported
            Assert.False (final.IsSupported);
        }
        Assert.True (responseReceived);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (1));
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Detect_SetsSupported_WhenIsVirtualTerminalIsTrueOrFalse_With_Response (bool isVirtualTerminal)
    {
        // Arrange
        var responseReceived = false;
        var output = new FakeOutput ();
        output.IsVirtualTerminal = isVirtualTerminal;

        Mock<DriverImpl> driverMock = new (
                                           MockBehavior.Strict,
                                           new FakeInputProcessor (null!),
                                           new OutputBufferImpl (),
                                           output,
                                           new AnsiRequestScheduler (new AnsiResponseParser ()),
                                           new SizeMonitorImpl (output)
                                          );

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                        {
                                                            if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                            {
                                                                responseReceived = true;

                                                                // Respond to the SendDeviceAttributes request with a value that indicates support (contains "4")
                                                                // Respond to the SendDeviceAttributes request with an empty value that indicates non-support
                                                                req.ResponseReceived.Invoke (driverMock.Object.IsVirtualTerminal ? "1;4;7c" : "");
                                                            }

                                                            // Abandon all requests
                                                            req.Abandoned?.Invoke ();
                                                        })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);
        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.Equal (isVirtualTerminal, driverMock.Object.IsVirtualTerminal);
        Assert.NotNull (final);

        if (isVirtualTerminal)
        {
            Assert.True (final.IsSupported);
            Assert.False (final.SupportsTransparency);
        }
        else
        {
            // Not a real VT, so shouldn't be supported
            Assert.False (final.IsSupported);
            Assert.False (final.SupportsTransparency);
        }

        Assert.True (responseReceived);
    }
}
