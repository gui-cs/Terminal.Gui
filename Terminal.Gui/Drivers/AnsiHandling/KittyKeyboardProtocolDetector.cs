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
    ///     Enables kitty keyboard progressive enhancement flags for the active terminal,
    ///     then performs a follow-up detect request to confirm and store the flags that were actually enabled.
    /// </summary>
    /// <param name="flags">The kitty keyboard flags to enable.</param>
    internal void Enable (KittyKeyboardFlags flags)
    {
        if (_driver is { IsLegacyConsole: true })
        {
            Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Enable", "Skipping kitty keyboard probe for legacy console");

            return;
        }

        if (flags == KittyKeyboardFlags.None)
        {
            return;
        }

        Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Enable", $"Writing enable sequence for flags {flags}");
        _driver?.GetOutput ().Write (EscSeqUtils.CSI_EnableKittyKeyboardFlags (flags));

        Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Enable", "Running Detector again, to get reported flags...");

        Detect (result =>
                {
                    if (!result.IsSupported || _driver?.KittyKeyboardCapabilities is null)
                    {
                        Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector),
                                         "Enable",
                                         $"Post-enable detect did not update flags. IsSupported={
                                             result.IsSupported
                                         }, HasCapabilities={
                                             _driver?.KittyKeyboardCapabilities is { }
                                         }");

                        return;
                    }

                    _driver.KittyKeyboardCapabilities.Flags = result.Flags;
                    Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Enable", $"Post-enable detect confirmed kitty flags {result.Flags}");
                });
    }

    /// <summary>
    ///     Sends the kitty keyboard disable sequence to restore the terminal keyboard protocol mode.
    /// </summary>
    internal void Disable ()
    {
        if (_driver is { IsLegacyConsole: true })
        {
            Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Disable", "Skipping kitty keyboard probe for legacy console");

            return;
        }

        Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector), "Disable", "Writing disable sequence");
        _driver?.GetOutput ().Write (EscSeqUtils.CSI_DisableKittyKeyboardFlags);
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

                          Trace.Lifecycle (nameof (KittyKeyboardProtocolDetector),
                                           "Detect",
                                           $"Kitty keyboard response '{response}' => IsSupported={result.IsSupported}, Flags={result.Flags}");
                          _driver?.KittyKeyboardCapabilities = result;

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

    /// <summary>
    ///     Parses a <see cref="EscSeqUtils.CSI_QueryKittyKeyboardFlags"/> response, returning
    ///     the parsed result. If the response indicates Kitty support, <see cref="KittyKeyboardCapabilities.IsSupported"/>
    ///     will be <see langword="true"/>. If <see cref="EscSeqUtils.CSI_EnableKittyKeyboardFlags"/> has been sent,
    ///     enabling Kitty support <see cref="KittyKeyboardCapabilities.Flags"/> will indicate
    ///     which flags have been enabled.
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
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

        return new KittyKeyboardCapabilities { IsSupported = true, Flags = (KittyKeyboardFlags)supportedFlags };
    }
}
