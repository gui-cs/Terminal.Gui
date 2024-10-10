using System.Text.RegularExpressions;

namespace Terminal.Gui;
/* TODO : Depends on https://github.com/gui-cs/Terminal.Gui/pull/3768
/// <summary>
///     Uses Ansi escape sequences to detect whether sixel is supported
///     by the terminal.
/// </summary>
public class SixelSupportDetector : ISixelSupportDetector
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

        result.IsSupported = IsSixelSupportedByDar ();

        if (result.IsSupported)
        {
            if (TryGetResolutionDirectly (out var res))
            {
                result.Resolution = res;
            }
            else if(TryComputeResolution(out res))
            {
                result.Resolution = res;
            }

            result.SupportsTransparency = IsWindowsTerminal () || IsXtermWithTransparency ();
        }

        return result;
    }


    private bool TryGetResolutionDirectly (out Size resolution)
    {
        // Expect something like:
        //<esc>[6;20;10t

        if (AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (EscSeqUtils.CSI_RequestSixelResolution, out var response))
        {
            // Terminal supports directly responding with resolution
            var match = Regex.Match (response.Response, @"\[\d+;(\d+);(\d+)t$");

            if (match.Success)
            {
                if (int.TryParse (match.Groups [1].Value, out var ry) &&
                    int.TryParse (match.Groups [2].Value, out var rx))
                {
                    resolution = new Size (rx, ry);

                    return true;
                }
            }
        }

        resolution = default;
        return false;
    }


    private bool TryComputeResolution (out Size resolution)
    {
        // Fallback to window size in pixels and characters
        if (AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (EscSeqUtils.CSI_RequestWindowSizeInPixels, out var pixelSizeResponse)
            && AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (EscSeqUtils.CSI_ReportTerminalSizeInChars, out var charSizeResponse))
        {
            // Example [4;600;1200t
            var pixelMatch = Regex.Match (pixelSizeResponse.Response, @"\[\d+;(\d+);(\d+)t$");

            // Example [8;30;120t
            var charMatch = Regex.Match (charSizeResponse.Response, @"\[\d+;(\d+);(\d+)t$");

            if (pixelMatch.Success && charMatch.Success)
            {
                // Extract pixel dimensions
                if (int.TryParse (pixelMatch.Groups [1].Value, out var pixelHeight)
                    && int.TryParse (pixelMatch.Groups [2].Value, out var pixelWidth)
                    &&

                    // Extract character dimensions
                    int.TryParse (charMatch.Groups [1].Value, out var charHeight)
                    && int.TryParse (charMatch.Groups [2].Value, out var charWidth)
                    && charWidth != 0
                    && charHeight != 0) // Avoid divide by zero
                {
                    // Calculate the character cell size in pixels
                    var cellWidth = (int)Math.Round ((double)pixelWidth / charWidth);
                    var cellHeight = (int)Math.Round ((double)pixelHeight / charHeight);

                    // Set the resolution based on the character cell size
                    resolution = new Size (cellWidth, cellHeight);

                    return true;
                }
            }
        }

        resolution = default;
        return false;
    }
    private bool IsSixelSupportedByDar ()
    {
        return AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (EscSeqUtils.CSI_SendDeviceAttributes, out AnsiEscapeSequenceResponse darResponse)
            ? darResponse.Response.Split (';').Contains ("4")
            : false;
    }

    private bool IsWindowsTerminal ()
    {
        return  !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable ("WT_SESSION"));;
    }
    private bool IsXtermWithTransparency ()
    {
        // Check if running in real xterm (XTERM_VERSION is more reliable than TERM)
        var xtermVersionStr = Environment.GetEnvironmentVariable ("XTERM_VERSION");

        // If XTERM_VERSION exists, we are in a real xterm
        if (!string.IsNullOrWhiteSpace (xtermVersionStr) && int.TryParse (xtermVersionStr, out var xtermVersion) && xtermVersion >= 370)
        {
            return true;
        }

        return false;
    }
}*/