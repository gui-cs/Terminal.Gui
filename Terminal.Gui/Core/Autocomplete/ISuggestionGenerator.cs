using System.Collections.Generic;
using Rune = System.Rune;

namespace Terminal.Gui {
	/// <summary>
	/// Generates autocomplete <see cref="Suggestion"/> based on a given cursor location within a string
	/// </summary>
	public interface ISuggestionGenerator {

		/// <summary>
		/// Generates autocomplete <see cref="Suggestion"/> based on a given <paramref name="context"/>
		/// </summary>
		IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context);

		bool IsWordChar (Rune rune);

	}
}

