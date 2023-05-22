using System;
using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// The execution state for a <see cref="Toplevel"/> view.
	/// </summary>
	public class ApplicationRunState : IDisposable {
		/// <summary>
		/// Initializes a new <see cref="ApplicationRunState"/> class.
		/// </summary>
		/// <param name="view"></param>
		public ApplicationRunState (Toplevel view)
		{
			Toplevel = view;
		}
		/// <summary>
		/// The <see cref="Toplevel"/> belonging to this <see cref="ApplicationRunState"/>.
		/// </summary>
		public Toplevel Toplevel { get; internal set; }

#if DEBUG_IDISPOSABLE
		/// <summary>
		/// For debug (see DEBUG_IDISPOSABLE define) purposes to verify objects are being disposed properly
		/// </summary>
		public bool WasDisposed = false;

		/// <summary>
		/// For debug (see DEBUG_IDISPOSABLE define) purposes to verify objects are being disposed properly
		/// </summary>
		public int DisposedCount = 0;

		/// <summary>
		/// For debug (see DEBUG_IDISPOSABLE define) purposes; the runstate instances that have been created
		/// </summary>
		public static List<ApplicationRunState> Instances = new List<ApplicationRunState> ();

		/// <summary>
		/// Creates a new RunState object.
		/// </summary>
		public ApplicationRunState ()
		{
			Instances.Add (this);
		}
#endif

		/// <summary>
		/// Releases all resource used by the <see cref="Application.ApplicationRunState"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose()"/> when you are finished using the <see cref="Application.ApplicationRunState"/>. 
		/// </remarks>
		/// <remarks>
		/// <see cref="Dispose()"/> method leaves the <see cref="Application.ApplicationRunState"/> in an unusable state. After
		/// calling <see cref="Dispose()"/>, you must release all references to the
		/// <see cref="Application.ApplicationRunState"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Application.ApplicationRunState"/> was occupying.
		/// </remarks>
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
#if DEBUG_IDISPOSABLE
			WasDisposed = true;
#endif
		}

		/// <summary>
		/// Releases all resource used by the <see cref="Application.ApplicationRunState"/> object.
		/// </summary>
		/// <param name="disposing">If set to <see langword="true"/> we are disposing and should dispose held objects.</param>
		protected virtual void Dispose (bool disposing)
		{
			if (Toplevel != null && disposing) {
				throw new InvalidOperationException ("You must clean up (Dispose) the Toplevel before calling Application.RunState.Dispose");
			}
		}
	}
}
