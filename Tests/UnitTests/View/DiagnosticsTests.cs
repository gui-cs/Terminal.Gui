#nullable enable
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
///     Tests <see cref="View.Diagnostics"/> static property and <see cref="ViewDiagnosticFlags"/> enum.
/// </summary>
/// <param name="output"></param>
[Trait ("Category", "Output")]
public class DiagnosticTests ()
{
    /// <summary>
    ///     /// Tests <see cref="View.Diagnostics"/> static property and <see cref="ViewDiagnosticFlags"/> enum.
    ///     ///
    /// </summary>
    [Fact]
    public void Diagnostics_Sets ()
    {
        // View.Diagnostics is a static property that returns the current diagnostic flags.
        Assert.Equal (ViewDiagnosticFlags.Off, View.Diagnostics);

        // View.Diagnostics can be set to a new value.
        View.Diagnostics = ViewDiagnosticFlags.Thickness;
        Assert.Equal (ViewDiagnosticFlags.Thickness, View.Diagnostics);

        // Ensure we turn off at the end of the test
        View.Diagnostics = ViewDiagnosticFlags.Off;
    }
}
