using System;
using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// Represents a state of escape sequence request.
	/// </summary>
	/// <remarks>
	/// This is needed because there are some escape sequence requests responses that are equal
	/// with some normal escape sequences and thus, will be only considered the responses to the
	/// requests that were registered with this object.
	/// </remarks>
	public class EscSeqReqStat {
		/// <summary>
		/// Gets the terminating.
		/// </summary>
		public string Terminating { get; }
		/// <summary>
		/// Gets the number of requests.
		/// </summary>
		public int NumOfReq { get; }
		/// <summary>
		/// Gets information about unfinished requests.
		/// </summary>
		public int Unfinished { get; set; }

		/// <summary>
		/// Creates a new state of escape sequence request.
		/// </summary>
		/// <param name="terminating">The terminating.</param>
		/// <param name="numOfReq">The number of requests.</param>
		public EscSeqReqStat (string terminating, int numOfReq)
		{
			Terminating = terminating;
			NumOfReq = Unfinished = numOfReq;
		}
	}

	/// <summary>
	/// Manages a <see cref="EscSeqReqStat"/> list of escape sequence requests.
	/// </summary>
	public class EscSeqReqProc {
		/// <summary>
		/// Gets the <see cref="EscSeqReqStat"/> list.
		/// </summary>
		public List<EscSeqReqStat> EscSeqReqStats { get; } = new List<EscSeqReqStat> ();

		/// <summary>
		/// Adds a new <see cref="EscSeqReqStat"/> instance to the <see cref="EscSeqReqStats"/> list.
		/// </summary>
		/// <param name="terminating">The terminating.</param>
		/// <param name="numOfReq">The number of requests.</param>
		public void Add (string terminating, int numOfReq = 1)
		{
			lock (EscSeqReqStats) {
				var found = EscSeqReqStats.Find (x => x.Terminating == terminating);
				if (found == null) {
					EscSeqReqStats.Add (new EscSeqReqStat (terminating, numOfReq));
				} else if (found != null && found.Unfinished < found.NumOfReq) {
					found.Unfinished = Math.Min (found.Unfinished + numOfReq, found.NumOfReq);
				}
			}
		}

		/// <summary>
		/// Removes a <see cref="EscSeqReqStat"/> instance from the <see cref="EscSeqReqStats"/> list.
		/// </summary>
		/// <param name="terminating">The terminating.</param>
		public void Remove (string terminating)
		{
			lock (EscSeqReqStats) {
				var found = EscSeqReqStats.Find (x => x.Terminating == terminating);
				if (found == null) {
					return;
				}
				if (found != null && found.Unfinished == 0) {
					EscSeqReqStats.Remove (found);
				} else if (found != null && found.Unfinished > 0) {
					found.Unfinished--;
					if (found.Unfinished == 0) {
						EscSeqReqStats.Remove (found);
					}
				}
			}
		}

		/// <summary>
		/// Indicates if a <see cref="EscSeqReqStat"/> with the <paramref name="terminating"/> exist
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
				if (found != null && found.Unfinished > 0) {
					return true;
				} else {
					EscSeqReqStats.Remove (found);
				}
				return false;
			}
		}
	}
}
