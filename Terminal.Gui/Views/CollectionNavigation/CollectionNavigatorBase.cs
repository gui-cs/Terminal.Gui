#nullable enable


namespace Terminal.Gui.Views;

/// <inheritdoc/>
internal abstract class CollectionNavigatorBase : ICollectionNavigator
{
    private DateTime _lastKeystroke = DateTime.Now;
    private string _searchString = "";

    /// <inheritdoc/>
    public ICollectionNavigatorMatcher Matcher { get; set; } = new DefaultCollectionNavigatorMatcher ();

    /// <inheritdoc/>
    public string SearchString
    {
        get => _searchString;
        private set
        {
            _searchString = value;
            OnSearchStringChanged (new (value));
        }
    }

    /// <inheritdoc/>
    public int TypingDelay { get; set; } = 500;

    /// <inheritdoc/>
    public int GetNextMatchingItem (int currentIndex, char keyStruck)
    {
        if (!char.IsControl (keyStruck))
        {
            // maybe user pressed 'd' and now presses 'd' again.
            // a candidate search is things that begin with "dd"
            // but if we find none then we must fallback on cycling
            // d instead and discard the candidate state
            var candidateState = "";
            var elapsedTime = DateTime.Now - _lastKeystroke;

            Logging.Debug ($"CollectionNavigator began processing '{keyStruck}', it has been {elapsedTime} since last keystroke");

            // is it a second or third (etc) keystroke within a short time
            if (SearchString.Length > 0 && elapsedTime < TimeSpan.FromMilliseconds (TypingDelay))
            {
                // "dd" is a candidate
                candidateState = SearchString + keyStruck;
                Logging.Debug ($"Appending, search is now for '{candidateState}'");
            }
            else
            {
                // its a fresh keystroke after some time
                // or its first ever key press
                SearchString = new string (keyStruck, 1);
                Logging.Debug ($"It has been too long since last key press so beginning new search");
            }

            int idxCandidate = GetNextMatchingItem (
                                                    currentIndex,
                                                    candidateState,

                                                    // prefer not to move if there are multiple characters e.g. "ca" + 'r' should stay on "car" and not jump to "cart"
                                                    candidateState.Length > 1
                                                   );

            Logging.Debug ($"CollectionNavigator searching (preferring minimum movement) matched:{idxCandidate}");
            if (idxCandidate != -1)
            {
                // found "dd" so candidate search string is accepted
                _lastKeystroke = DateTime.Now;
                SearchString = candidateState;

                Logging.Debug ($"Found collection item that matched search:{idxCandidate}");
                return idxCandidate;
            }

            //// nothing matches "dd" so discard it as a candidate
            //// and just cycle "d" instead
            _lastKeystroke = DateTime.Now;
            idxCandidate = GetNextMatchingItem (currentIndex, candidateState);

            Logging.Debug ($"CollectionNavigator searching (any match) matched:{idxCandidate}");

            // if a match wasn't found, the user typed a 'wrong' key in their search ("can" + 'z'
            // instead of "can" + 'd').
            if (SearchString.Length > 1 && idxCandidate == -1)
            {
                Logging.Debug ("CollectionNavigator ignored key and returned existing index");
                // ignore it since we're still within the typing delay
                // don't add it to SearchString either
                return currentIndex;
            }

            // if no changes to current state manifested
            if (idxCandidate == currentIndex || idxCandidate == -1)
            {
                Logging.Debug ("CollectionNavigator found no changes to current index, so clearing search");

                // clear history and treat as a fresh letter
                ClearSearchString ();

                // match on the fresh letter alone
                SearchString = new string (keyStruck, 1);
                idxCandidate = GetNextMatchingItem (currentIndex, SearchString);

                Logging.Debug ($"CollectionNavigator new SearchString {SearchString} matched index:{idxCandidate}");

                return idxCandidate == -1 ? currentIndex : idxCandidate;
            }

            Logging.Debug ($"CollectionNavigator final answer was:{idxCandidate}");
            // Found another "d" or just leave index as it was
            return idxCandidate;
        }

        Logging.Debug ("CollectionNavigator found key press was not actionable so clearing search and returning -1");

        // clear state because keypress was a control char
        ClearSearchString ();

        // control char indicates no selection
        return -1;
    }



    /// <summary>
    ///     Raised when the <see cref="SearchString"/> is changed. Useful for debugging. Raises the
    ///     <see cref="SearchStringChanged"/> event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnSearchStringChanged (KeystrokeNavigatorEventArgs e) { SearchStringChanged?.Invoke (this, e); }

    /// <summary>This event is raised when <see cref="SearchString"/> is changed. Useful for debugging.</summary>
    public event EventHandler<KeystrokeNavigatorEventArgs>? SearchStringChanged;

    /// <summary>Returns the collection being navigated element at <paramref name="idx"/>.</summary>
    /// <returns></returns>
    protected abstract object ElementAt (int idx);

    /// <summary>Return the number of elements in the collection</summary>
    protected abstract int GetCollectionLength ();

    /// <summary>Gets the index of the next item in the collection that matches <paramref name="search"/>.</summary>
    /// <param name="currentIndex">The index in the collection to start the search from.</param>
    /// <param name="search">The search string to use.</param>
    /// <param name="minimizeMovement">
    ///     Set to <see langword="true"/> to stop the search on the first match if there are
    ///     multiple matches for <paramref name="search"/>. e.g. "ca" + 'r' should stay on "car" and not jump to "cart". If
    ///     <see langword="false"/> (the default), the next matching item will be returned, even if it is above in the
    ///     collection.
    /// </param>
    /// <returns>The index of the next matching item or <see langword="-1"/> if no match was found.</returns>
    internal int GetNextMatchingItem (int currentIndex, string search, bool minimizeMovement = false)
    {
        if (string.IsNullOrEmpty (search))
        {
            return -1;
        }

        int collectionLength = GetCollectionLength ();

        if (currentIndex != -1 && currentIndex < collectionLength && Matcher.IsMatch (search, ElementAt (currentIndex)))
        {
            // we are already at a match
            if (minimizeMovement)
            {
                // if we would rather not jump around (e.g. user is typing lots of text to get this match)
                return currentIndex;
            }

            for (var i = 1; i < collectionLength; i++)
            {
                //circular
                int idxCandidate = (i + currentIndex) % collectionLength;

                if (Matcher.IsMatch (search, ElementAt (idxCandidate)))
                {
                    return idxCandidate;
                }
            }

            // nothing else starts with the search term
            return currentIndex;
        }

        // search terms no longer match the current selection or there is none
        for (var i = 0; i < collectionLength; i++)
        {
            if (Matcher.IsMatch (search, ElementAt (i)))
            {
                return i;
            }
        }

        // Nothing matches
        return -1;
    }

    private void ClearSearchString ()
    {
        SearchString = "";
        _lastKeystroke = DateTime.Now;
    }
}