#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes an ongoing ANSI request sent to the console.
///     Use <see cref="ResponseReceived"/> to handle the response
///     when console answers the request.
/// </summary>
public class AnsiEscapeSequenceRequest
{
    /// <summary>
    ///     Execute an ANSI escape sequence escape which may return a response or error.
    /// </summary>
    /// <param name="ansiRequest">The ANSI escape sequence to request.</param>
    /// <returns>A <see cref="AnsiEscapeSequenceResponse"/> with the response, error, terminator and value.</returns>
    public static AnsiEscapeSequenceResponse ExecuteAnsiRequest (AnsiEscapeSequenceRequest ansiRequest)
    {
        var response = new StringBuilder ();
        var error = new StringBuilder ();
        var savedIsReportingMouseMoves = false;

        try
        {
            switch (Application.Driver)
            {
                case NetDriver netDriver:
                    savedIsReportingMouseMoves = netDriver.IsReportingMouseMoves;

                    if (savedIsReportingMouseMoves)
                    {
                        netDriver.StopReportingMouseMoves ();
                    }

                    break;
                case CursesDriver cursesDriver:
                    savedIsReportingMouseMoves = cursesDriver.IsReportingMouseMoves;

                    if (savedIsReportingMouseMoves)
                    {
                        cursesDriver.StopReportingMouseMoves ();
                    }

                    break;
            }

            Thread.Sleep (100); // Allow time for mouse stopping and to flush the input buffer

            // Flush the input buffer to avoid reading stale input
            while (Console.KeyAvailable)
            {
                Console.ReadKey (true);
            }

            // Send the ANSI escape sequence
            Console.Write (ansiRequest.Request);
            Console.Out.Flush (); // Ensure the request is sent

            // Read the response from stdin (response should come back as input)
            Thread.Sleep (100); // Allow time for the terminal to respond

            // Read input until no more characters are available or the terminator is encountered
            while (Console.KeyAvailable)
            {
                // Peek the next key
                ConsoleKeyInfo keyInfo = Console.ReadKey (true); // true to not display on the console

                // Append the current key to the response
                response.Append (keyInfo.KeyChar);

                if (keyInfo.KeyChar == ansiRequest.Terminator [^1]) // Check if the key is terminator (ANSI escape sequence ends)
                {
                    // Break out of the loop when terminator is found
                    break;
                }
            }

            if (!response.ToString ().EndsWith (ansiRequest.Terminator [^1]))
            {
                throw new InvalidOperationException ($"Terminator doesn't ends with: {ansiRequest.Terminator [^1]}");
            }
        }
        catch (Exception ex)
        {
            error.AppendLine ($"Error executing ANSI request: {ex.Message}");
        }
        finally
        {
            if (savedIsReportingMouseMoves)
            {
                switch (Application.Driver)
                {
                    case NetDriver netDriver:
                        netDriver.StartReportingMouseMoves ();

                        break;
                    case CursesDriver cursesDriver:
                        cursesDriver.StartReportingMouseMoves ();

                        break;
                }
            }
        }

        var values = new string? [] { null };

        if (string.IsNullOrEmpty (error.ToString ()))
        {
            (string? c1Control, string? code, values, string? terminator) = EscSeqUtils.GetEscapeResult (response.ToString ().ToCharArray ());
        }

        AnsiEscapeSequenceResponse ansiResponse = new ()
            { Response = response.ToString (), Error = error.ToString (), Terminator = response.ToString () [^1].ToString (), Value = values [0] };

        // Invoke the event if it's subscribed
        ansiRequest.ResponseReceived?.Invoke (ansiRequest, ansiResponse);

        return ansiResponse;
    }

    /// <summary>
    ///     Request to send e.g. see
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///     </see>
    /// </summary>
    public required string Request { get; init; }

    /// <summary>
    ///     Invoked when the console responds with an ANSI response code that matches the
    ///     <see cref="Terminator"/>
    /// </summary>
    public event EventHandler<AnsiEscapeSequenceResponse>? ResponseReceived;

    /// <summary>
    ///     <para>
    ///         The terminator that uniquely identifies the type of response as responded
    ///         by the console. e.g. for
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///         </see>
    ///         the terminator is
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Terminator</cref>
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         After sending a request, the first response with matching terminator will be matched
    ///         to the oldest outstanding request.
    ///     </para>
    /// </summary>
    public required string Terminator { get; init; }

    /// <summary>
    ///     The value expected in the response e.g.
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_ReportTerminalSizeInChars.Value</cref>
    ///     </see>
    ///     which will have a 't' as terminator but also other different request may return the same terminator with a
    ///     different value.
    /// </summary>
    public string? Value { get; init; }
}
