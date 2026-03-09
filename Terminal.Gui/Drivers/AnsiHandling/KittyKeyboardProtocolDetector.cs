using System.Text.RegularExpressions;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes the kitty keyboard protocol state discovered from the active terminal.
/// </summary>
public class KittyKeyboardProtocolResult
{
    /// <summary>
    ///     Gets or sets whether the active terminal responded to the kitty keyboard protocol query.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    ///     Gets or sets the kitty keyboard flags reported by the terminal.
    /// </summary>
    public int SupportedFlags { get; set; }

    /// <summary>
    ///     Gets or sets the kitty keyboard flags Terminal.Gui intends to enable.
    /// </summary>
    public int EnabledFlags { get; set; }
}

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
    public void Detect (Action<KittyKeyboardProtocolResult> resultCallback)
    {
        ArgumentNullException.ThrowIfNull (resultCallback);

        if (_driver is { IsLegacyConsole: true })
        {
            Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Detect", "Skipping kitty keyboard probe for legacy console");
            resultCallback (new KittyKeyboardProtocolResult ());

            return;
        }

        Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Detect", $"Queueing kitty keyboard probe '{EscSeqUtils.CSI_QueryKittyKeyboardFlags.Request}'");
        QueueRequest (EscSeqUtils.CSI_QueryKittyKeyboardFlags,
                      response =>
                      {
                          KittyKeyboardProtocolResult result = ParseResponse (response);
                          result.EnabledFlags = result.IsSupported ? EscSeqUtils.KittyKeyboardRequestedFlags : 0;
                          Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector),
                                           "Detect",
                                           $"Kitty keyboard response '{response}' => Supported={result.IsSupported}, SupportedFlags={result.SupportedFlags}, EnabledFlags={result.EnabledFlags}");
                          resultCallback (result);
                      },
                      () =>
                      {
                          Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Detect", "Kitty keyboard probe abandoned");
                          resultCallback (new KittyKeyboardProtocolResult ());
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

    internal static KittyKeyboardProtocolResult ParseResponse (string? response)
    {
        if (string.IsNullOrWhiteSpace (response))
        {
            return new KittyKeyboardProtocolResult ();
        }

        Match match = Regex.Match (response, @"(?:\x1B)?\[\?(\d+)u$");

        if (!match.Success || !int.TryParse (match.Groups [1].Value, out int supportedFlags))
        {
            return new KittyKeyboardProtocolResult ();
        }

        return new KittyKeyboardProtocolResult
        {
            IsSupported = true,
            SupportedFlags = supportedFlags
        };
    }
}
