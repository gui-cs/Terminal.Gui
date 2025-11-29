#nullable enable
using Moq;

namespace UnitTests_Parallelizable.DrawingTests;

public class SixelToRenderTests
{
    [Fact]
    public void SixelToRender_Properties_AreGettableAndSettable ()
    {
        var s = new SixelToRender ();

        s.SixelData = "SIXEL-DATA";
        s.ScreenPosition = new Point (3, 5);

        Assert.Equal ("SIXEL-DATA", s.SixelData);
        Assert.Equal (3, s.ScreenPosition.X);
        Assert.Equal (5, s.ScreenPosition.Y);
    }

    [Fact]
    public void SixelSupportResult_DefaultValues_AreExpected ()
    {
        var r = new SixelSupportResult ();

        Assert.False (r.IsSupported);
        Assert.Equal (10, r.Resolution.Width);
        Assert.Equal (20, r.Resolution.Height);
        Assert.Equal (256, r.MaxPaletteColors);
        Assert.False (r.SupportsTransparency);
    }

    [Fact]
    public void Detect_WhenDeviceAttributesIndicateSupport_GetsResolutionDirectly ()
    {
        // Arrange
        var driverMock = new Mock<IDriver> (MockBehavior.Strict);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                          {
                                                              // Response contains "4" -> indicates sixel support
                                                              req.ResponseReceived.Invoke ("?1;4;7c");
                                                          }
                                                          else if (req.Request == EscSeqUtils.CSI_RequestSixelResolution.Request)
                                                          {
                                                              // Return resolution: "[6;20;10t" (group1=20 -> ry, group2=10 -> rx)
                                                              req.ResponseReceived.Invoke ("[6;20;10t");
                                                          }
                                                          else
                                                          {
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
        Assert.True (final.IsSupported);
        Assert.Equal (10, final.Resolution.Width);
        Assert.Equal (20, final.Resolution.Height);

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (2));
    }

    [Fact]
    public void Detect_WhenDirectResolutionFails_ComputesResolutionFromWindowSizes ()
    {
        // Arrange
        var driverMock = new Mock<IDriver> (MockBehavior.Strict);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          switch (req.Request)
                                                          {
                                                              case var r when r == EscSeqUtils.CSI_SendDeviceAttributes.Request:
                                                                  // Indicate sixel support so flow continues to try resolution
                                                                  req.ResponseReceived.Invoke ("?1;4;7c");
                                                                  break;

                                                              case var r when r == EscSeqUtils.CSI_RequestSixelResolution.Request:
                                                                  // Simulate failure to return resolution directly
                                                                  req.Abandoned?.Invoke ();
                                                                  break;

                                                              case var r when r == EscSeqUtils.CSI_RequestWindowSizeInPixels.Request:
                                                                  // Pixel dimensions reply: [4;600;1200t  -> pixelHeight=600; pixelWidth=1200
                                                                  req.ResponseReceived.Invoke ("[4;600;1200t");
                                                                  break;

                                                              case var r when r == EscSeqUtils.CSI_ReportWindowSizeInChars.Request:
                                                                  // Character dimensions reply: [8;30;120t -> charHeight=30; charWidth=120
                                                                  req.ResponseReceived.Invoke ("[8;30;120t");
                                                                  break;

                                                              default:
                                                                  req.Abandoned?.Invoke ();
                                                                  break;
                                                          }
                                                      })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);

        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.NotNull (final);
        Assert.True (final.IsSupported);
        // Expect cell width = round(1200 / 120) = 10, cell height = round(600 / 30) = 20
        Assert.Equal (10, final.Resolution.Width);
        Assert.Equal (20, final.Resolution.Height);

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (3));
    }

    [Fact]
    public void Detect_WhenDeviceAttributesDoNotIndicateSupport_ReturnsNotSupported ()
    {
        // Arrange
        var driverMock = new Mock<IDriver> (MockBehavior.Strict);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                          {
                                                              // Response does NOT contain "4"
                                                              req.ResponseReceived.Invoke ("?1;0;7c");
                                                          }
                                                          else
                                                          {
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

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeastOnce ());
    }

    [Fact]
    public void Detect_WhenXtermEnvironmentIndicatesTransparency_SupportsTransparencyEvenIfDAReturnsNo4 ()
    {
        // Arrange - set XTERM_VERSION env var to indicate real xterm with transparency
        string? prev = Environment.GetEnvironmentVariable ("XTERM_VERSION");
        Environment.SetEnvironmentVariable ("XTERM_VERSION", "370");

        try
        {
            var driverMock = new Mock<IDriver> (MockBehavior.Strict);

            driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                      .Callback<AnsiEscapeSequenceRequest> (req =>
                                                          {
                                                              if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                              {
                                                                  // Response does NOT contain "4" (so DAR indicates no sixel)
                                                                  req.ResponseReceived.Invoke ("?1;0;7c");
                                                              }
                                                              else
                                                              {
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

            // DAR did not indicate sixel support
            Assert.False (final.IsSupported);

            // But because XTERM_VERSION >= 370 we expect SupportsTransparency to have been initially true and remain true
            Assert.True (final.SupportsTransparency);

            driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeastOnce ());
        }
        finally
        {
            // Restore environment
            Environment.SetEnvironmentVariable ("XTERM_VERSION", prev);
        }
    }
}