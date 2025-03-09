#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui;

/// <summary>
///     Manages <see cref="AnsiEscapeSequenceRequest"/> made to an <see cref="IAnsiResponseParser"/>.
///     Ensures there are not 2+ outstanding requests with the same terminator, throttles request sends
///     to prevent console becoming unresponsive and handles evicting ignored requests (no reply from
///     terminal).
/// </summary>
public class AnsiRequestScheduler
{
    private readonly IAnsiResponseParser _parser;

    /// <summary>
    ///     Function for returning the current time. Use in unit tests to
    ///     ensure repeatable tests.
    /// </summary>
    internal Func<DateTime> Now { get; set; }

    private readonly HashSet<Tuple<AnsiEscapeSequenceRequest, DateTime>> _queuedRequests = new ();

    internal IReadOnlyCollection<AnsiEscapeSequenceRequest> QueuedRequests => _queuedRequests.Select (r => r.Item1).ToList ();

    /// <summary>
    ///     <para>
    ///         Dictionary where key is ansi request terminator and value is when we last sent a request for
    ///         this terminator. Combined with <see cref="_throttle"/> this prevents hammering the console
    ///         with too many requests in sequence which can cause console to freeze as there is no space for
    ///         regular screen drawing / mouse events etc to come in.
    ///     </para>
    ///     <para>
    ///         When user exceeds the throttle, new requests accumulate in <see cref="_queuedRequests"/> (i.e. remain
    ///         queued).
    ///     </para>
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTime> _lastSend = new ();

    /// <summary>
    ///     Number of milliseconds after sending a request that we allow
    ///     another request to go out.
    /// </summary>
    private readonly TimeSpan _throttle = TimeSpan.FromMilliseconds (100);

    private readonly TimeSpan _runScheduleThrottle = TimeSpan.FromMilliseconds (100);

    /// <summary>
    ///     If console has not responded to a request after this period of time, we assume that it is never going
    ///     to respond. Only affects when we try to send a new request with the same terminator - at which point
    ///     we tell the parser to stop expecting the old request and start expecting the new request.
    /// </summary>
    private readonly TimeSpan _staleTimeout = TimeSpan.FromSeconds (1);

    private readonly DateTime _lastRun;

    /// <summary>
    ///     Creates a new instance.
    /// </summary>
    /// <param name="parser"></param>
    /// <param name="now"></param>
    public AnsiRequestScheduler (IAnsiResponseParser parser, Func<DateTime>? now = null)
    {
        _parser = parser;
        Now = now ?? (() => DateTime.Now);
        _lastRun = Now ();
    }

    /// <summary>
    ///     Sends the <paramref name="request"/> immediately or queues it if there is already
    ///     an outstanding request for the given <see cref="AnsiEscapeSequence.Terminator"/>.
    /// </summary>
    /// <param name="request"></param>
    /// <returns><see langword="true"/> if request was sent immediately. <see langword="false"/> if it was queued.</returns>
    public bool SendOrSchedule (AnsiEscapeSequenceRequest request) { return SendOrSchedule (request, true); }

    private bool SendOrSchedule (AnsiEscapeSequenceRequest request, bool addToQueue)
    {
        if (CanSend (request, out ReasonCannotSend reason))
        {
            Send (request);

            return true;
        }

        if (reason == ReasonCannotSend.OutstandingRequest)
        {
            // If we can evict an old request (no response from terminal after ages)
            if (EvictStaleRequests (request.Terminator))
            {
                // Try again after evicting
                if (CanSend (request, out _))
                {
                    Send (request);

                    return true;
                }
            }
        }

        if (addToQueue)
        {
            _queuedRequests.Add (Tuple.Create (request, Now ()));
        }

        return false;
    }

    private void EvictStaleRequests ()
    {
        foreach (string stale in _lastSend.Where (v => IsStale (v.Value)).Select (k => k.Key))
        {
            EvictStaleRequests (stale);
        }
    }

    private bool IsStale (DateTime dt) { return Now () - dt > _staleTimeout; }

    /// <summary>
    ///     Looks to see if the last time we sent <paramref name="withTerminator"/>
    ///     is a long time ago. If so we assume that we will never get a response and
    ///     can proceed with a new request for this terminator (returning <see langword="true"/>).
    /// </summary>
    /// <param name="withTerminator"></param>
    /// <returns></returns>
    private bool EvictStaleRequests (string withTerminator)
    {
        if (_lastSend.TryGetValue (withTerminator, out DateTime dt))
        {
            if (IsStale (dt))
            {
                _parser.StopExpecting (withTerminator, false);

                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Identifies and runs any <see cref="_queuedRequests"/> that can be sent based on the
    ///     current outstanding requests of the parser.
    /// </summary>
    /// <param name="force">
    ///     Repeated requests to run the schedule over short period of time will be ignored.
    ///     Pass <see langword="true"/> to override this behaviour and force evaluation of outstanding requests.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if a request was found and run. <see langword="false"/>
    ///     if no outstanding requests or all have existing outstanding requests underway in parser.
    /// </returns>
    public bool RunSchedule (bool force = false)
    {
        if (!force && Now () - _lastRun < _runScheduleThrottle)
        {
            return false;
        }

        // Get oldest request
        Tuple<AnsiEscapeSequenceRequest, DateTime>? opportunity = _queuedRequests.MinBy (r => r.Item2);

        if (opportunity != null)
        {
            // Give it another go
            if (SendOrSchedule (opportunity.Item1, false))
            {
                _queuedRequests.Remove (opportunity);

                return true;
            }
        }

        EvictStaleRequests ();

        return false;
    }

    private void Send (AnsiEscapeSequenceRequest r)
    {
        _lastSend.AddOrUpdate (r.Terminator, _ => Now (), (_, _) => Now ());
        _parser.ExpectResponse (r.Terminator, r.ResponseReceived, r.Abandoned, false);
        r.Send ();
    }

    private bool CanSend (AnsiEscapeSequenceRequest r, out ReasonCannotSend reason)
    {
        if (ShouldThrottle (r))
        {
            reason = ReasonCannotSend.TooManyRequests;

            return false;
        }

        if (_parser.IsExpecting (r.Terminator))
        {
            reason = ReasonCannotSend.OutstandingRequest;

            return false;
        }

        reason = default (ReasonCannotSend);

        return true;
    }

    private bool ShouldThrottle (AnsiEscapeSequenceRequest r)
    {
        if (_lastSend.TryGetValue (r.Terminator, out DateTime value))
        {
            return Now () - value < _throttle;
        }

        return false;
    }
}
