using System.Collections.Generic;
using Rune = System.Rune;

namespace Terminal.Gui {
	public interface ISuggestionGenerator
	{

		/// <summary>
		/// Populates <see cref="Suggestions"/> with all strings in <see cref="AllSuggestions"/> that
		/// match with the current cursor position/text in the <see cref="HostControl"/>.
		/// </summary>
		/// <param name="columnOffset">The column offset. Current (zero - default), left (negative), right (positive).</param>
		IEnumerable<Suggestion> GenerateSuggestions (List<Rune> currentLine, int idx);

		bool IsWordChar (Rune rune);

	}
}

