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
    public EscSeqReqStatus (AnsiEscapeSequenceRequest ansiRequest)
    {
        AnsiRequest = ansiRequest;
        NumRequests = NumOutstanding = 1;
    }

    /// <summary>Gets the number of unfinished requests.</summary>
    public int NumOutstanding { get; set; }

    /// <summary>Gets the number of requests.</summary>
    public int NumRequests { get; set; }

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
    /// <summary>Gets the <see cref="EscSeqReqStatus"/> list.</summary>
    public List<EscSeqReqStatus> Statuses { get; } = new ();

    /// <summary>
    ///     Adds a new request for the ANSI Escape Sequence defined by <paramref name="ansiRequest"/>. Adds a
    ///     <see cref="EscSeqReqStatus"/> instance to <see cref="Statuses"/> list.
    /// </summary>
    /// <param name="ansiRequest">The <see cref="AnsiEscapeSequenceRequest"/> object.</param>
    public void Add (AnsiEscapeSequenceRequest ansiRequest)
    {
        lock (Statuses)
        {
            EscSeqReqStatus? found = Statuses.Find (x => x.AnsiRequest.Terminator == ansiRequest.Terminator);

            if (found is null)
            {
                Statuses.Add (new (ansiRequest));
            }
            else if (found.NumOutstanding < found.NumRequests)
            {
                found.NumOutstanding = Math.Min (found.NumOutstanding + 1, found.NumRequests);
            }
            else
            {
                found.NumRequests++;
                found.NumOutstanding++;
            }
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
            EscSeqReqStatus? found = Statuses.Find (x => x.AnsiRequest.Terminator == terminator);
            seqReqStatus = found;

            return found is { NumOutstanding: > 0 };
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
            EscSeqReqStatus? found = Statuses.Find (x => x == seqReqStatus);

            if (found is null)
            {
                return;
            }

            if (found is { NumOutstanding: 0 })
            {
                Statuses.Remove (found);
            }
            else if (found is { NumOutstanding: > 0 })
            {
                found.NumOutstanding--;

                if (found.NumOutstanding == 0)
                {
                    Statuses.Remove (found);
                }
            }
        }
    }
}
