using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// Args for events that relate to a specific <see cref="RuneCell"/>.
	/// </summary>
	public class RuneCellEventArgs {
		/// <summary>
		/// The line that is currently drawn.
		/// </summary>
		public List<RuneCell> Line { get; set; }

		/// <summary>
		/// The index position in the line.
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		/// Creates a new instance of the <see cref="RuneCellEventArgs"/> class.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <param name="index">The index.</param>
		public RuneCellEventArgs (List<RuneCell> line, int index)
		{
			Line = line;
			Index = index;
		}
	}
}
