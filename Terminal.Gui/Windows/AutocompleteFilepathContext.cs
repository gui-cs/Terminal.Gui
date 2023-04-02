using NStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Terminal.Gui.FileServices;

namespace Terminal.Gui {
	internal class AutocompleteFilepathContext : AutocompleteContext {
		public FileDialogState State { get; set; }

		public AutocompleteFilepathContext (FileDialogState state)
            : base(new System.Collections.Generic.List<System.Rune>(),0)
		{
			this.State = state;
		}
	}


	internal class FilepathSuggestionGenerator : ISuggestionGenerator {

		List<Suggestion> cachedSuggestions = new List<Suggestion>();

		public IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context)
		{
			if(context is not AutocompleteFilepathContext fileState) {
				return cachedSuggestions;
			}
			var state = fileState.State;
			var path = ustring.Make (fileState.CurrentLine).ToString();
			var last = path.LastIndexOfAny (FileDialog.Separators);

			var term = path.Substring (last + 1);

			if (term.Equals (state?.Directory?.Name)) {
				// Clear suggestions
				return cachedSuggestions = new List<Suggestion> ();
			}

			bool isWindows = RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

			var suggestions = state.Children.Select (
				e => e.FileSystemInfo is DirectoryInfo d
					? d.Name + System.IO.Path.DirectorySeparatorChar
					: e.FileSystemInfo.Name)
				.ToArray ();

			var validSuggestions = suggestions
				.Where (s => s.StartsWith (term, isWindows ?
					StringComparison.InvariantCultureIgnoreCase :
					StringComparison.InvariantCulture))
				.OrderBy (m => m.Length)
				.ToArray ();


			// nothing to suggest
			if (validSuggestions.Length == 0 || validSuggestions [0].Length == term.Length) {
				return cachedSuggestions = new List<Suggestion> ();
			}

			return cachedSuggestions = validSuggestions.Select (f => new Suggestion(0,f.Substring (term.Length),f)).ToList ();
		}

		public bool IsWordChar (Rune rune)
		{
			if(rune == '\n') {
				return false;
			}


			return true;
		}


	}
}