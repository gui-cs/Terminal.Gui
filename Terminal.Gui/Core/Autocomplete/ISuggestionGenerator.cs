using System.Collections.Generic;
using Rune = System.Rune;

namespace Terminal.Gui {
	/// <summary>
	/// Generates autocomplete <see cref="Suggestion"/> based on a given cursor location within a string
	/// </summary>
	public interface ISuggestionGenerator {

		/// <summary>
		/// Generates autocomplete <see cref="Suggestion"/> based on a given cursor location <paramref name="idx"/>
		/// within a <paramref name="currentLine"/>
		/// </summary>
		IEnumerable<Suggestion> GenerateSuggestions (List<Rune> currentLine, int idx);

		bool IsWordChar (Rune rune);

	}
}

