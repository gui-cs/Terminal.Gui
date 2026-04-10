using System.Text.RegularExpressions;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Detects whether the active terminal supports the kitty keyboard protocol.
/// </summary>
public class KittyKeyboardProtocolDetector
{
    private readonly IDriver? _driver;

    /// <summary>
    ///     Creates a new detector that sends its query through the provided <paramref name="driver"/>.
    /// </summary>
    /// <param name="driver">The driver to send ANSI requests through.</param>
    public KittyKeyboardProtocolDetector (IDriver? driver)
    {
        ArgumentNullException.ThrowIfNull (driver);
        _driver = driver;
    }

    /// <summary>
    ///     Detects kitty keyboard protocol support asynchronously through the ANSI request scheduler.
    /// </summary>
    /// <param name="resultCallback">Called when detection completes.</param>
    public void Detect (Action<KittyKeyboardCapabilities> resultCallback)
    {
        ArgumentNullException.ThrowIfNull (resultCallback);

        if (_driver is { IsLegacyConsole: true })
        {
            Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Detect", "Skipping kitty keyboard probe for legacy console");
            resultCallback (new KittyKeyboardCapabilities ());

            return;
        }

        Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector),
                         "Detect",
                         $"Queueing kitty keyboard probe '{EscSeqUtils.CSI_QueryKittyKeyboardFlags.Request}'");

        QueueRequest (EscSeqUtils.CSI_QueryKittyKeyboardFlags,
                      response =>
                      {
                          KittyKeyboardCapabilities result = ParseResponse (response);
                          result.EnabledFlags = result.IsSupported ? EscSeqUtils.KittyKeyboardRequestedFlags : KittyKeyboardFlags.None;

                          Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector),
                                           "Detect",
                                           $"Kitty keyboard response '{
                                               response
                                           }' => Supported={
                                               result.IsSupported
                                           }, SupportedFlags={
                                               result.SupportedFlags
                                           }, EnabledFlags={
                                               result.EnabledFlags
                                           }");
                          resultCallback (result);
                      },
                      () =>
                      {
                          Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Detect", "Kitty keyboard probe abandoned");
                          resultCallback (new KittyKeyboardCapabilities ());
                      });
    }

    private void QueueRequest (AnsiEscapeSequence req, Action<string> responseCallback, Action abandoned)
    {
        AnsiEscapeSequenceRequest request = new ()
        {
            Request = req.Request,
            Value = req.Value,
            Terminator = req.Terminator,
            ResponseReceived = r => responseCallback (r ?? string.Empty),
            Abandoned = abandoned
        };

        _driver?.QueueAnsiRequest (request);
    }

    internal static KittyKeyboardCapabilities ParseResponse (string? response)
    {
        if (string.IsNullOrWhiteSpace (response))
        {
            return new KittyKeyboardCapabilities ();
        }

        Match match = Regex.Match (response, @"(?:\x1B)?\[\?(\d+)u$");

        if (!match.Success || !int.TryParse (match.Groups [1].Value, out int supportedFlags))
        {
            return new KittyKeyboardCapabilities ();
        }

        return new KittyKeyboardCapabilities { IsSupported = true, SupportedFlags = (KittyKeyboardFlags)supportedFlags };
    }
}
