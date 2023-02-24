using System;
using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// Represents the state of an ANSI escape sequence request.
	/// </summary>
	/// <remarks>
	/// This is needed because there are some escape sequence requests responses that are equal
	/// with some normal escape sequences and thus, will be only considered the responses to the
	/// requests that were registered with this object.
	/// </remarks>
	public class EscSequenceRequestState {
		/// <summary>
		/// Gets the terminating.
		/// </summary>
		public string Terminating { get; }
		/// <summary>
		/// Gets the number of requests.
		/// </summary>
		public int NumRequests { get; }
		/// <summary>
		/// Gets information about unfinished requests.
		/// </summary>
		public int NumOutstanding { get; set; }

		/// <summary>
		/// Creates a new state of escape sequence request.
		/// </summary>
		/// <param name="terminating">The terminating.</param>
		/// <param name="numOfReq">The number of requests.</param>
		public EscSequenceRequestState (string terminating, int numOfReq)
		{
			Terminating = terminating;
			NumRequests = NumOutstanding = numOfReq;
		}
	}

	/// <summary>
	/// Manages a list of <see cref="EscSequenceRequestState"/>.
	/// </summary>
	public class EscSeqReqProc {
		/// <summary>
		/// Gets the <see cref="EscSequenceRequestState"/> list.
		/// </summary>
		public List<EscSequenceRequestState> EscSeqReqStats { get; } = new List<EscSequenceRequestState> ();

		/// <summary>
		/// Adds a new <see cref="EscSequenceRequestState"/> instance to the <see cref="EscSeqReqStats"/> list.
		/// </summary>
		/// <param name="terminating">The terminating.</param>
		/// <param name="numOfReq">The number of requests.</param>
		public void Add (string terminating, int numOfReq = 1)
		{
			lock (EscSeqReqStats) {
				var found = EscSeqReqStats.Find (x => x.Terminating == terminating);
				if (found == null) {
					EscSeqReqStats.Add (new EscSequenceRequestState (terminating, numOfReq));
				} else if (found != null && found.NumOutstanding < found.NumRequests) {
					found.NumOutstanding = Math.Min (found.NumOutstanding + numOfReq, found.NumRequests);
				}
			}
		}

		/// <summary>
		/// Removes a <see cref="EscSequenceRequestState"/> instance from the <see cref="EscSeqReqStats"/> list.
		/// </summary>
		/// <param name="terminating">The terminating string.</param>
		public void Remove (string terminating)
		{
			lock (EscSeqReqStats) {
				var found = EscSeqReqStats.Find (x => x.Terminating == terminating);
				if (found == null) {
					return;
				}
				if (found != null && found.NumOutstanding == 0) {
					EscSeqReqStats.Remove (found);
				} else if (found != null && found.NumOutstanding > 0) {
					found.NumOutstanding--;
					if (found.NumOutstanding == 0) {
						EscSeqReqStats.Remove (found);
					}
				}
			}
		}

		/// <summary>
		/// Indicates if a <see cref="EscSequenceRequestState"/> with the <paramref name="terminating"/> exist
		/// in the <see cref="EscSeqReqStats"/> list.
		/// </summary>
		/// <param name="terminating"></param>
		/// <returns><see langword="true"/> if exist, <see langword="false"/> otherwise.</returns>
		public bool Requested (string terminating)
		{
			lock (EscSeqReqStats) {
				var found = EscSeqReqStats.Find (x => x.Terminating == terminating);
				if (found == null) {
					return false;
				}
				if (found != null && found.NumOutstanding > 0) {
					return true;
				} else {
					EscSeqReqStats.Remove (found);
				}
				return false;
			}
		}
	}
}
