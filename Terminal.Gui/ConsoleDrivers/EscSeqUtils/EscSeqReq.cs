#nullable enable

namespace Terminal.Gui;

/// <summary>
///     Represents the status of an ANSI escape sequence request made to the terminal using
///     <see cref="EscSeqRequests"/>.
/// </summary>
/// <remarks></remarks>
public class EscSeqReqStatus
{
    /// <summary>Creates a new state of escape sequence request.</summary>
    /// <param name="ansiRequest">The <see cref="AnsiEscapeSequenceRequest"/> object.</param>
    public EscSeqReqStatus (AnsiEscapeSequenceRequest ansiRequest) { AnsiRequest = ansiRequest; }

    /// <summary>Gets the Escape Sequence Terminator (e.g. ESC[8t ... t is the terminator).</summary>
    public AnsiEscapeSequenceRequest AnsiRequest { get; }
}

// TODO: This class is a singleton. It should use the singleton pattern.
/// <summary>
///     Manages ANSI Escape Sequence requests and responses. The list of <see cref="EscSeqReqStatus"/> contains the
///     status of the request. Each request is identified by the terminator (e.g. ESC[8t ... t is the terminator).
/// </summary>
public class EscSeqRequests
{
    /// <summary>
    ///     Adds a new request for the ANSI Escape Sequence defined by <paramref name="ansiRequest"/>. Adds a
    ///     <see cref="EscSeqReqStatus"/> instance to <see cref="Statuses"/> list.
    /// </summary>
    /// <param name="ansiRequest">The <see cref="AnsiEscapeSequenceRequest"/> object.</param>
    public void Add (AnsiEscapeSequenceRequest ansiRequest)
    {
        lock (Statuses)
        {
            Statuses.Enqueue (new (ansiRequest));
            Console.Out.Write (ansiRequest.Request);
            Console.Out.Flush ();
            Thread.Sleep (100); // Allow time for the terminal to respond
        }
    }

    /// <summary>
    ///     Indicates if a <see cref="EscSeqReqStatus"/> with the <paramref name="terminator"/> exists in the
    ///     <see cref="Statuses"/> list.
    /// </summary>
    /// <param name="terminator"></param>
    /// <param name="seqReqStatus"></param>
    /// <returns><see langword="true"/> if exist, <see langword="false"/> otherwise.</returns>
    public bool HasResponse (string terminator, out EscSeqReqStatus? seqReqStatus)
    {
        lock (Statuses)
        {
            Statuses.TryPeek (out seqReqStatus);

            var result = seqReqStatus?.AnsiRequest.Terminator == terminator;

            if (result)
            {
                return true;
            }

            seqReqStatus = null;

            return false;
        }
    }

    /// <summary>
    ///     Removes a request defined by <paramref name="seqReqStatus"/>. If a matching <see cref="EscSeqReqStatus"/> is
    ///     found and the number of outstanding requests is greater than 0, the number of outstanding requests is decremented.
    ///     If the number of outstanding requests is 0, the <see cref="EscSeqReqStatus"/> is removed from
    ///     <see cref="Statuses"/>.
    /// </summary>
    /// <param name="seqReqStatus">The <see cref="EscSeqReqStatus"/> object.</param>
    public void Remove (EscSeqReqStatus? seqReqStatus)
    {
        lock (Statuses)
        {
            Statuses.TryDequeue (out var request);

            if (request != seqReqStatus)
            {
                throw new InvalidOperationException ("Both EscSeqReqStatus objects aren't equals.");
            }
        }
    }

    /// <summary>Gets the <see cref="EscSeqReqStatus"/> list.</summary>
    public Queue<EscSeqReqStatus> Statuses { get; } = new ();
}
