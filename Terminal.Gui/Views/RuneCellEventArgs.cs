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
		/// The col index position in the line.
		/// </summary>
		public int IdxCol { get; set; }

		/// <summary>
		/// The row index position in the text.
		/// </summary>
		public int IdxRow { get; set; }

		/// <summary>
		/// Creates a new instance of the <see cref="RuneCellEventArgs"/> class.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <param name="idxCol">The col index.</param>
		/// <param name="idxRow">The row index.</param>
		public RuneCellEventArgs (List<RuneCell> line, int idxCol, int idxRow)
		{
			Line = line;
			IdxCol = idxCol;
			IdxRow = idxRow;
		}
	}
}
