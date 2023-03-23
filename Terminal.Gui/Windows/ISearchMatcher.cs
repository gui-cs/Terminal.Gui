using System;
using System.IO;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Defines whether a given file/directory matches a set of
	/// search terms.
	/// </summary>
	public interface ISearchMatcher {
		/// <summary>
		/// Called once for each new search. Defines the string
		/// the user has provided as search terms.
		/// </summary>
		void Initialize (string terms);

		/// <summary>
		/// Return true if <paramref name="f"/> is a match to the
		/// last provided search terms
		/// </summary>
		bool IsMatch (FileSystemInfo f);
	}
	
	class DefaultSearchMatcher : ISearchMatcher {
		string [] terms;

		public void Initialize (string terms)
		{
			this.terms = terms.Split (new string [] { " " }, StringSplitOptions.RemoveEmptyEntries);
		}

		public bool IsMatch (FileSystemInfo f)
		{
			//Contains overload with StringComparison is not available in (net472) or (netstandard2.0)
			//return f.Name.Contains (terms, StringComparison.OrdinalIgnoreCase);

			return
				// At least one term must match the file name only e.g. "my" in "myfile.csv"
				terms.Any (t => f.Name.IndexOf (t, StringComparison.OrdinalIgnoreCase) >= 0)
				&&
				// All terms must exist in full path e.g. "dos my" can match "c:\documents\myfile.csv"
				terms.All (t => f.FullName.IndexOf (t, StringComparison.OrdinalIgnoreCase) >= 0);
		}
	}

}