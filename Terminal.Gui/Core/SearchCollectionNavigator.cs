using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Changes the index in a collection based on keys pressed
	/// and the current state
	/// </summary>
	class SearchCollectionNavigator {
		string state = "";
		DateTime lastKeystroke = DateTime.MinValue;
		const int TypingDelay = 250;
		public StringComparer Comparer { get; set; } = StringComparer.InvariantCultureIgnoreCase;
		private IEnumerable<object> Collection { get => _collection; set => _collection = value; }

		private IEnumerable<object> _collection;

		public SearchCollectionNavigator (IEnumerable<object> collection) { _collection = collection; }


		public int CalculateNewIndex (IEnumerable<object> collection, int currentIndex, char keyStruck)
		{
			// if user presses a key
			if (!char.IsControl(keyStruck)) {//char.IsLetterOrDigit (keyStruck) || char.IsPunctuation (keyStruck) || char.IsSymbol(keyStruck)) {

				// maybe user pressed 'd' and now presses 'd' again.
				// a candidate search is things that begin with "dd"
				// but if we find none then we must fallback on cycling
				// d instead and discard the candidate state
				string candidateState = "";

				// is it a second or third (etc) keystroke within a short time
				if (state.Length > 0 && DateTime.Now - lastKeystroke < TimeSpan.FromMilliseconds (TypingDelay)) {
					// "dd" is a candidate
					candidateState = state + keyStruck;
				} else {
					// its a fresh keystroke after some time
					// or its first ever key press
					state = new string (keyStruck, 1);
				}

				var idxCandidate = GetNextIndexMatching (collection, currentIndex, candidateState,
					// prefer not to move if there are multiple characters e.g. "ca" + 'r' should stay on "car" and not jump to "cart"
					candidateState.Length > 1);

				if (idxCandidate != -1) {
					// found "dd" so candidate state is accepted
					lastKeystroke = DateTime.Now;
					state = candidateState;
					return idxCandidate;
				}


				// nothing matches "dd" so discard it as a candidate
				// and just cycle "d" instead
				lastKeystroke = DateTime.Now;
				idxCandidate = GetNextIndexMatching (collection, currentIndex, state);

				// if no changes to current state manifested
				if (idxCandidate == currentIndex || idxCandidate == -1) {
					// clear history and treat as a fresh letter
					ClearState ();

					// match on the fresh letter alone
					state = new string (keyStruck, 1);
					idxCandidate = GetNextIndexMatching (collection, currentIndex, state);
					return idxCandidate == -1 ? currentIndex : idxCandidate;
				}

				// Found another "d" or just leave index as it was
				return idxCandidate;

			} else {
				// clear state because keypress was non letter
				ClearState ();

				// no change in index for non letter keystrokes
				return currentIndex;
			}
		}

		public int CalculateNewIndex (int currentIndex, char keyStruck)
		{
			return CalculateNewIndex (Collection, currentIndex, keyStruck);
		}

		private int GetNextIndexMatching (IEnumerable<object> collection, int currentIndex, string search, bool preferNotToMoveToNewIndexes = false)
		{
			if (string.IsNullOrEmpty (search)) {
				return -1;
			}

			// find indexes of items that start with the search text
			int [] matchingIndexes = collection.Select ((item, idx) => (item, idx))
				  .Where (k => k.item?.ToString().StartsWith (search, StringComparison.InvariantCultureIgnoreCase) ?? false)
				  .Select (k => k.idx)
				  .ToArray ();

			// if there are items beginning with search
			if (matchingIndexes.Length > 0) {
				// is one of them currently selected?
				var currentlySelected = Array.IndexOf (matchingIndexes, currentIndex);

				if (currentlySelected == -1) {
					// we are not currently selecting any item beginning with the search
					// so jump to first item in list that begins with the letter
					return matchingIndexes [0];
				} else {

					// the current index is part of the matching collection
					if (preferNotToMoveToNewIndexes) {
						// if we would rather not jump around (e.g. user is typing lots of text to get this match)
						return matchingIndexes [currentlySelected];
					}

					// cycle to next (circular)
					return matchingIndexes [(currentlySelected + 1) % matchingIndexes.Length];
				}
			}

			// nothing starts with the search
			return -1;
		}

		private void ClearState ()
		{
			state = "";
			lastKeystroke = DateTime.MinValue;

		}

		/// <summary>
		/// Returns true if <paramref name="kb"/> is a searchable key
		/// (e.g. letters, numbers etc) that is valid to pass to to this
		/// class for search filtering
		/// </summary>
		/// <param name="kb"></param>
		/// <returns></returns>
		public static bool IsCompatibleKey (KeyEvent kb)
		{
			return !kb.IsAlt && !kb.IsCapslock && !kb.IsCtrl && !kb.IsScrolllock && !kb.IsNumlock;
		}
	}
}
