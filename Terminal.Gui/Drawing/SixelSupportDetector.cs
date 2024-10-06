using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Terminal.Gui;

/// <summary>
///     Uses Ansi escape sequences to detect whether sixel is supported
///     by the terminal.
/// </summary>
public class SixelSupportDetector
{
    /// <summary>
    /// Sends Ansi escape sequences to the console to determine whether
    /// sixel is supported (and <see cref="SixelSupportResult.Resolution"/>
    /// etc).
    /// </summary>
    /// <returns>Description of sixel support, may include assumptions where
    /// expected response codes are not returned by console.</returns>
    public SixelSupportResult Detect ()
    {
        var result = new SixelSupportResult ();

        result.IsSupported =
            AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (EscSeqUtils.CSI_SendDeviceAttributes, out AnsiEscapeSequenceResponse darResponse)
                ? darResponse.Response.Split (';').Contains ("4")
                : false;

        if (result.IsSupported)
        {
            // Expect something like:
            //<esc>[6;20;10t

            bool gotResolutionDirectly = false;

            if (AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (EscSeqUtils.CSI_RequestSixelResolution, out var resolution))
            {
                // Terminal supports directly responding with resolution
                var match = Regex.Match (resolution.Response, @"\[\d+;(\d+);(\d+)t$");

                if (match.Success)
                {
                    if (int.TryParse (match.Groups [1].Value, out var ry) &&
                        int.TryParse (match.Groups [2].Value, out var rx))
                    {
                        result.Resolution = new Size (rx, ry);
                        gotResolutionDirectly = true;
                    }
                }
            }

            if (!gotResolutionDirectly)
            {
                // Fallback to window size in pixels and characters
                if (AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (EscSeqUtils.CSI_RequestWindowSizeInPixels, out var pixelSizeResponse) &&
                    AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (EscSeqUtils.CSI_ReportTerminalSizeInChars, out var charSizeResponse))
                {
                    // Example [4;600;1200t
                    var pixelMatch = Regex.Match (pixelSizeResponse.Response, @"\[\d+;(\d+);(\d+)t$");

                    // Example [8;30;120t
                    var charMatch = Regex.Match (charSizeResponse.Response, @"\[\d+;(\d+);(\d+)t$");

                    if (pixelMatch.Success && charMatch.Success)
                    {
                        // Extract pixel dimensions
                        if (int.TryParse (pixelMatch.Groups [1].Value, out var pixelHeight) &&
                            int.TryParse (pixelMatch.Groups [2].Value, out var pixelWidth) &&
                            // Extract character dimensions
                            int.TryParse (charMatch.Groups [1].Value, out var charHeight) &&
                            int.TryParse (charMatch.Groups [2].Value, out var charWidth) &&
                            charWidth != 0 && charHeight != 0) // Avoid divide by zero
                        {
                            // Calculate the character cell size in pixels
                            var cellWidth = pixelWidth / charWidth;
                            var cellHeight = pixelHeight / charHeight;

                            // Set the resolution based on the character cell size
                            result.Resolution = new Size (cellWidth, cellHeight);
                        }
                    }
                }
            }
        }

        return result;
    }
}