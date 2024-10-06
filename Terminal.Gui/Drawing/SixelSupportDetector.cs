using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Terminal.Gui;

/// <summary>
///     Uses ANSII escape sequences to detect whether sixel is supported
///     by the terminal.
/// </summary>
public class SixelSupportDetector
{
    public SixelSupport Detect ()
    {
        var result = new SixelSupport ();

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
                // TODO: Try pixel/window resolution getting
            }
        }

        return result;
    }
}

public class SixelSupport
{
    /// <summary>
    ///     Whether the current driver supports sixel graphic format.
    ///     Defaults to false.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    ///     The number of pixels of sixel that corresponds to each Col (<see cref="Size.Width"/>)
    ///     and each Row (<see cref="Size.Height"/>.  Defaults to 10x20.
    /// </summary>
    public Size Resolution { get; set; } = new (10, 20);

    /// <summary>
    ///     The maximum number of colors that can be included in a sixel image. Defaults
    ///     to 256.
    /// </summary>
    public int MaxPaletteColors { get; set; } = 256;
}
