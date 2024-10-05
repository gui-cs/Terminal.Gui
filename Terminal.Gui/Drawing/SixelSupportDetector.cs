using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui;

/// <summary>
/// Uses ANSII escape sequences to detect whether sixel is supported
/// by the terminal.
/// </summary>
public class SixelSupportDetector
{
    public SixelSupport Detect ()
    {
        var darResponse = AnsiEscapeSequenceRequest.ExecuteAnsiRequest (EscSeqUtils.CSI_SendDeviceAttributes);
        var result = new SixelSupport ();
        result.IsSupported = darResponse.Response.Split (';').Contains ("4");

        return result;
    }
}


public class SixelSupport
{
    /// <summary>
    /// Whether the current driver supports sixel graphic format.
    /// Defaults to false.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    /// The number of pixels of sixel that corresponds to each Col (<see cref="Size.Width"/>)
    /// and each Row (<see cref="Size.Height"/>.  Defaults to 10x20.
    /// </summary>
    public Size Resolution { get; set; } = new Size (10, 20);

    /// <summary>
    /// The maximum number of colors that can be included in a sixel image. Defaults
    /// to 256.
    /// </summary>
    public int MaxPaletteColors { get; set; } = 256;
}