#nullable enable
namespace Terminal.Gui;

public partial class View
{
    /// <summary>Gets or sets whether diagnostic information will be drawn. This is a bit-field of <see cref="ViewDiagnosticFlags"/>.e <see cref="View"/> diagnostics.</summary>
    /// <remarks>
    /// <para>
    ///     <see cref="Adornment.Diagnostics"/> gets set to this property by default, enabling <see cref="ViewDiagnosticFlags.Ruler"/> and <see cref="ViewDiagnosticFlags.Thickness"/>.
    /// </para>
    /// <para>
    ///     <see cref="ViewDiagnosticFlags.Hover"/> and <see cref="ViewDiagnosticFlags.DrawIndicator"/> are enabled for all Views independently of Adornments.
    /// </para>
    /// </remarks>
    public static ViewDiagnosticFlags Diagnostics { get; set; }
}
