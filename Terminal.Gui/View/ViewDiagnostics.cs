﻿
namespace Terminal.Gui;

/// <summary>Enables diagnostic functions for <see cref="View"/>.</summary>
[Flags]
public enum ViewDiagnosticFlags : uint
{
    /// <summary>All diagnostics off</summary>
    Off = 0b_0000_0000,

    /// <summary>
    ///     When enabled, <see cref="View.OnDrawAdornments"/> will draw a ruler in the Thickness.
    /// </summary>
    Ruler = 0b_0000_0001,

    /// <summary>
    ///     When enabled, <see cref="View.OnDrawAdornments"/> will draw the first letter of the Adornment name ('M', 'B', or 'P')
    ///     in the Thickness.
    /// </summary>
    Padding = 0b_0000_0010
}

public partial class View
{
    /// <summary>Flags to enable/disable <see cref="View"/> diagnostics.</summary>
    public static ViewDiagnosticFlags Diagnostics { get; set; }
}
