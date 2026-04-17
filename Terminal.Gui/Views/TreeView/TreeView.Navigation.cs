// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

#nullable disable

namespace Terminal.Gui.Views;

public partial class TreeView<T>
{
    /// <summary>
    ///     This event is raised when an object is activated e.g. by double clicking or pressing
    ///     <see cref="ObjectActivationKey"/>.
    /// </summary>

    // TODO: Refactor to use CWP
    public event EventHandler<ObjectActivatedEventArgs<T>> ObjectActivated;

    /// <summary>Called when the <see cref="SelectedObject"/> changes.</summary>

    // TODO: Refactor to use CWP
    public event EventHandler<SelectionChangedEventArgs<T>> SelectionChanged;

    /// <summary>
    ///     The number of screen lines to move the currently selected object by. Supports negative values.
    ///     <paramref name="offset"/>. Each branch occupies 1 line on screen.
    /// </summary>
    /// <remarks>
    ///     If nothing is currently selected or the selected object is no longer in the tree then the first object in the
    ///     tree is selected instead.
    /// </remarks>
    /// <param name="offset">Positive to move the selection down the screen, negative to move it up</param>
    /// <param name="expandSelection">
    ///     True to expand the selection (assuming <see cref="MultiSelect"/> is enabled). False to
    ///     replace.
    /// </param>
    public void AdjustSelection (int offset, bool expandSelection = false)
    {
        // if it is not a shift click, or we don't allow multi select
        if (!expandSelection || !MultiSelect)
        {
            _multiSelectedRegions.Clear ();
        }

        if (SelectedObject is null)
        {
            SelectedObject = roots.Keys.FirstOrDefault ();
        }
        else
        {
            IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

            int idx = map.IndexOf (b => b.Model.Equals (SelectedObject));

            if (idx == -1)
            {
                // The current selection has disappeared!
                SelectedObject = roots.Keys.FirstOrDefault ();
            }
            else
            {
                int newIdx = Math.Min (Math.Max (0, idx + offset), map.Count - 1);

                Branch<T> newBranch = map.ElementAt (newIdx);

                // If it is a multi selection
                if (expandSelection && MultiSelect)
                {
                    if (_multiSelectedRegions.Any ())
                    {
                        // expand the existing head selection
                        TreeSelection<T> head = _multiSelectedRegions.Pop ();
                        _multiSelectedRegions.Push (new TreeSelection<T> (head.Origin, newIdx, map));
                    }
                    else
                    {
                        // or start a new multi selection region
                        _multiSelectedRegions.Push (new TreeSelection<T> (map.ElementAt (idx), newIdx, map));
                    }
                }

                SelectedObject = newBranch.Model;

                EnsureVisible (SelectedObject);
            }
        }

        UpdateCursor ();

        SetNeedsDraw ();
    }

    /// <summary>Moves the selection to the last child in the currently selected level.</summary>
    public void AdjustSelectionToBranchEnd ()
    {
        T o = SelectedObject;

        if (o is null)
        {
            return;
        }

        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        int currentIdx = map.IndexOf (b => Equals (b.Model, o));

        if (currentIdx == -1)
        {
            return;
        }

        Branch<T> currentBranch = map.ElementAt (currentIdx);
        Branch<T> next = currentBranch;

        for (; currentIdx < map.Count; currentIdx++)
        {
            //if it is the end of the current depth of branch
            if (currentBranch.Depth != next.Depth)
            {
                SelectedObject = currentBranch.Model;
                EnsureVisible (currentBranch.Model);
                SetNeedsDraw ();

                return;
            }

            // look at next branch for consideration
            currentBranch = next;
            next = map.ElementAt (currentIdx);
        }

        GoToEnd ();
    }

    /// <summary>Moves the selection to the first child in the currently selected level.</summary>
    public void AdjustSelectionToBranchStart ()
    {
        T o = SelectedObject;

        if (o is null)
        {
            return;
        }

        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        int currentIdx = map.IndexOf (b => Equals (b.Model, o));

        if (currentIdx == -1)
        {
            return;
        }

        Branch<T> currentBranch = map.ElementAt (currentIdx);
        Branch<T> next = currentBranch;

        for (; currentIdx >= 0; currentIdx--)
        {
            //if it is the beginning of the current depth of branch
            if (currentBranch.Depth != next.Depth)
            {
                SelectedObject = currentBranch.Model;
                EnsureVisible (currentBranch.Model);
                SetNeedsDraw ();

                return;
            }

            // look at next branch up for consideration
            currentBranch = next;
            next = map.ElementAt (currentIdx);
        }

        // We ran all the way to top of tree
        GoToFirst ();
    }

    /// <summary>
    ///     <para>Moves the <see cref="SelectedObject"/> to the next item that begins with <paramref name="character"/>.</para>
    ///     <para>This method will loop back to the start of the tree if reaching the end without finding a match.</para>
    /// </summary>
    /// <param name="character">The first character of the next item you want selected.</param>
    /// <param name="caseSensitivity">Case sensitivity of the search.</param>
    public void AdjustSelectionToNextItemBeginningWith (char character, StringComparison caseSensitivity = StringComparison.CurrentCultureIgnoreCase)
    {
        // search for next branch that begins with that letter
        var characterAsStr = character.ToString ();
        AdjustSelectionToNext (b => AspectGetter (b.Model).StartsWith (characterAsStr, caseSensitivity));
    }

    /// <summary>
    ///     Returns true if the given object <paramref name="o"/> is exposed in the tree and can be expanded otherwise
    ///     false.
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public bool CanExpand (T o) => ObjectToBranch (o)?.CanExpand () ?? false;

    /// <summary>Collapses the <see cref="SelectedObject"/></summary>
    public void Collapse () => Collapse (_selectedObject);

    /// <summary>Collapses the supplied object if it is currently expanded .</summary>
    /// <param name="toCollapse">The object to collapse.</param>
    public void Collapse (T toCollapse) => CollapseImpl (toCollapse, false);

    /// <summary>
    ///     Collapses the supplied object if it is currently expanded. Also collapses all children branches (this will
    ///     only become apparent when/if the user expands it again).
    /// </summary>
    /// <param name="toCollapse">The object to collapse.</param>
    public void CollapseAll (T toCollapse) => CollapseImpl (toCollapse, true);

    /// <summary>Collapses all root nodes in the tree.</summary>
    public void CollapseAll ()
    {
        foreach (KeyValuePair<T, Branch<T>> item in roots)
        {
            item.Value.Collapse ();
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Adjusts the <see cref="ScrollOffsetVertical"/> to ensure the given <paramref name="model"/> is visible. Has no
    ///     effect if already visible.
    /// </summary>
    public void EnsureVisible (T model)
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        int idx = map.IndexOf (b => Equals (b.Model, model));

        if (idx == -1)
        {
            return;
        }

        /*this -1 allows for possible horizontal scroll bar in the last row of the control*/
        int leaveSpace = Style.LeaveLastRow ? 1 : 0;

        if (idx < ScrollOffsetVertical)
        {
            //if user has scrolled up too far to see their selection
            ScrollOffsetVertical = idx;
        }
        else if (idx >= ScrollOffsetVertical + Viewport.Height - leaveSpace)
        {
            //if user has scrolled off bottom of visible tree
            ScrollOffsetVertical = Math.Max (0, idx + 1 - (Viewport.Height - leaveSpace));
        }
    }

    /// <summary>Expands the currently <see cref="SelectedObject"/>.</summary>
    public void Expand () => Expand (SelectedObject);

    /// <summary>
    ///     Expands the supplied object if it is contained in the tree (either as a root object or as an exposed branch
    ///     object).
    /// </summary>
    /// <param name="toExpand">The object to expand.</param>
    public void Expand (T toExpand)
    {
        if (toExpand is null)
        {
            return;
        }

        ObjectToBranch (toExpand)?.Expand ();
        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>Expands the supplied object and all child objects.</summary>
    /// <param name="toExpand">The object to expand.</param>
    public void ExpandAll (T toExpand)
    {
        if (toExpand is null)
        {
            return;
        }

        ObjectToBranch (toExpand)?.ExpandAll ();
        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Fully expands all nodes in the tree, if the tree is very big and built dynamically this may take a while (e.g.
    ///     for file system).
    /// </summary>
    public void ExpandAll ()
    {
        foreach (KeyValuePair<T, Branch<T>> item in roots)
        {
            item.Value.ExpandAll ();
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Returns <see cref="SelectedObject"/> (if not null) and all multi selected objects if <see cref="MultiSelect"/>
    ///     is true
    /// </summary>
    /// <returns></returns>
    public IEnumerable<T> GetAllSelectedObjects ()
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        // To determine multi selected objects, start with the line map, that avoids yielding 
        // hidden nodes that were selected then the parent collapsed e.g. programmatically or
        // with mouse click
        if (MultiSelect)
        {
            foreach (T m in map.Select (b => b.Model).Where (IsSelected))
            {
                yield return m;
            }
        }
        else
        {
            if (SelectedObject is { })
            {
                yield return SelectedObject;
            }
        }
    }

    /// <summary>
    ///     Returns the currently expanded children of the passed object. Returns an empty collection if the branch is not
    ///     exposed or not expanded.
    /// </summary>
    /// <param name="o">An object in the tree.</param>
    /// <returns></returns>
    public IEnumerable<T> GetChildren (T o)
    {
        Branch<T> branch = ObjectToBranch (o);

        if (branch is null || !branch.IsExpanded)
        {
            return new T [0];
        }

        return branch.ChildBranches?.Select (b => b.Model).ToArray () ?? new T [0];
    }

    /// <summary>
    ///     Returns the parent object of <paramref name="o"/> in the tree. Returns null if the object is not exposed in
    ///     the tree.
    /// </summary>
    /// <param name="o">An object in the tree.</param>
    /// <returns></returns>
    public T GetParent (T o) => ObjectToBranch (o)?.Parent?.Model;

    /// <summary>
    ///     Returns the index of the object <paramref name="o"/> if it is currently exposed (it's parent(s) have been
    ///     expanded). This can be used with <see cref="ScrollOffsetVertical"/> and <see cref="View.SetNeedsDraw()"/> to
    ///     scroll to a specific object.
    /// </summary>
    /// <remarks>Uses the Equals method and returns the first index at which the object is found or -1 if it is not found.</remarks>
    /// <param name="o">An object that appears in your tree and is currently exposed.</param>
    /// <returns>The index the object was found at or -1 if it is not currently revealed or not in the tree at all.</returns>
    public int GetScrollOffsetOf (T o)
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        for (var i = 0; i < map.Count; i++)
        {
            if (map.ElementAt (i).Model.Equals (o))
            {
                return i;
            }
        }

        //object not found
        return -1;
    }

    /// <summary>
    ///     Changes the <see cref="SelectedObject"/> to <paramref name="toSelect"/> and scrolls to ensure it is visible.
    ///     Has no effect if <paramref name="toSelect"/> is not exposed in the tree (e.g. its parents are collapsed).
    /// </summary>
    /// <param name="toSelect"></param>
    public void GoTo (T toSelect)
    {
        if (ObjectToBranch (toSelect) is null)
        {
            return;
        }

        SelectedObject = toSelect;
        EnsureVisible (toSelect);
        SetNeedsDraw ();
    }

    /// <summary>Changes the <see cref="SelectedObject"/> to the last object in the tree and scrolls so that it is visible.</summary>
    public void GoToEnd ()
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();
        ScrollOffsetVertical = Math.Max (0, map.Count - Viewport.Height + 1);
        SelectedObject = map.LastOrDefault ()?.Model;

        SetNeedsDraw ();
    }

    /// <summary>
    ///     Changes the <see cref="SelectedObject"/> to the first root object and resets the
    ///     <see cref="ScrollOffsetVertical"/> to 0.
    /// </summary>
    public void GoToFirst ()
    {
        ScrollOffsetVertical = 0;
        SelectedObject = roots.Keys.FirstOrDefault ();

        SetNeedsDraw ();
    }

    /// <summary>Returns true if the given object <paramref name="o"/> is exposed in the tree and expanded otherwise false.</summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public bool IsExpanded (T o) => ObjectToBranch (o)?.IsExpanded ?? false;

    /// <summary>
    ///     Returns true if the <paramref name="model"/> is either the <see cref="SelectedObject"/> or part of a
    ///     <see cref="MultiSelect"/>.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public bool IsSelected (T model) => Equals (SelectedObject, model) || (MultiSelect && _multiSelectedRegions.Any (s => s.Contains (model)));

    /// <summary>Moves the selection down by the height of the control (1 page).</summary>
    /// <param name="expandSelection">True if the navigation should add the covered nodes to the selected current selection.</param>
    /// <exception cref="NotImplementedException"></exception>
    public void MovePageDown (bool expandSelection = false) => AdjustSelection (Viewport.Height, expandSelection);

    /// <summary>Moves the selection up by the height of the control (1 page).</summary>
    /// <param name="expandSelection">True if the navigation should add the covered nodes to the selected current selection.</param>
    /// <exception cref="NotImplementedException"></exception>
    public void MovePageUp (bool expandSelection = false) => AdjustSelection (-Viewport.Height, expandSelection);

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        if (!Enabled)
        {
            return false;
        }

        // If the key was bound to key command, let normal KeyDown processing happen. This enables overriding the default handling.
        // See: https://github.com/gui-cs/Terminal.Gui/issues/3950#issuecomment-2807350939
        if (KeyBindings.TryGet (key, out _))
        {
            return false;
        }

        // If not a keybinding, is the key a searchable key press?
        if (!KeystrokeNavigator.Matcher.IsCompatibleKey (key) || !AllowLetterBasedNavigation || _selectedObject is null)
        {
            return false;
        }

        // If there has been a call to InvalidateMap since the last time
        // we need a new one to reflect the new exposed tree state
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        // Find the current selected object within the tree
        int current = map.IndexOf (b => b.Model == SelectedObject);

        // The currently selected object is no longer in line map somehow
        if (current < 0)
        {
            return false;
        }

        int? newIndex = KeystrokeNavigator?.GetNextMatchingItem (current, (char)key);

        if (newIndex is not int || newIndex == -1)
        {
            return false;
        }

        SelectedObject = map.ElementAt ((int)newIndex).Model;
        EnsureVisible (_selectedObject);
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Scrolls the view area down a single line without changing the current selection.</summary>
    public void ScrollDown ()
    {
        if (ScrollOffsetVertical <= ContentHeight - 2)
        {
            ScrollOffsetVertical++;
            SetNeedsDraw ();
        }
    }

    /// <summary>Scrolls the view area up a single line without changing the current selection.</summary>
    public void ScrollUp ()
    {
        if (_scrollOffsetVertical > 0)
        {
            ScrollOffsetVertical--;
            SetNeedsDraw ();
        }
    }

    /// <summary>Selects all objects in the tree when <see cref="MultiSelect"/> is enabled otherwise does nothing.</summary>
    public void SelectAll ()
    {
        if (!MultiSelect)
        {
            return;
        }

        _multiSelectedRegions.Clear ();

        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        if (map.Count == 0)
        {
            return;
        }

        _multiSelectedRegions.Push (new TreeSelection<T> (map.ElementAt (0), map.Count, map));
        SetNeedsDraw ();
        UpdateCursor ();

        OnSelectionChanged (new SelectionChangedEventArgs<T> (this, SelectedObject, SelectedObject));
    }

    /// <summary>
    ///     Implementation of <see cref="Collapse(T)"/> and <see cref="CollapseAll(T)"/>. Performs operation and updates
    ///     selection if disappeared.
    /// </summary>
    /// <param name="toCollapse"></param>
    /// <param name="all"></param>
    protected void CollapseImpl (T toCollapse, bool all)
    {
        if (toCollapse is null)
        {
            return;
        }

        Branch<T> branch = ObjectToBranch (toCollapse);

        // Nothing to collapse
        if (branch is null)
        {
            return;
        }

        if (all)
        {
            branch.CollapseAll ();
        }
        else
        {
            branch.Collapse ();
        }

        if (SelectedObject is { } && ObjectToBranch (SelectedObject) is null)
        {
            // If the old selection suddenly became invalid then clear it
            SelectedObject = null;
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Determines systems behaviour when the left arrow key is pressed. Default behaviour is to collapse the current
    ///     tree node if possible otherwise changes selection to current branches parent.
    /// </summary>
    protected virtual void CursorLeft (bool ctrl)
    {
        if (!IsExpanded (SelectedObject))
        {
            T parent = GetParent (SelectedObject);

            if (parent is null)
            {
                return;
            }

            SelectedObject = parent;
            AdjustSelection (0);
            SetNeedsDraw ();

            return;
        }

        if (ctrl)
        {
            CollapseAll (SelectedObject);
        }
        else
        {
            Collapse (SelectedObject);
        }
    }

    /// <summary>Raises the <see cref="ObjectActivated"/> event.</summary>
    /// <param name="e"></param>

    // TODO: Refactor to use CWP
    protected virtual void OnObjectActivated (ObjectActivatedEventArgs<T> e) => ObjectActivated?.Invoke (this, e);

    /// <summary>Raises the SelectionChanged event.</summary>
    /// <param name="e"></param>

    // TODO: Refactor to use CWP
    protected virtual void OnSelectionChanged (SelectionChangedEventArgs<T> e) => SelectionChanged?.Invoke (this, e);

    /// <summary>Sets the selection to the next branch that matches the <paramref name="predicate"/>.</summary>
    /// <param name="predicate"></param>
    private void AdjustSelectionToNext (Func<Branch<T>, bool> predicate)
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        // empty map means we can't select anything anyway
        if (map.Count == 0)
        {
            return;
        }

        // Start searching from the first element in the map
        var idxStart = 0;

        // or the current selected branch
        if (SelectedObject is not null)
        {
            idxStart = map.IndexOf (b => Equals (b.Model, SelectedObject));
        }

        // if currently selected object mysteriously vanished, search from beginning
        if (idxStart == -1)
        {
            idxStart = 0;
        }

        // loop around all indexes and back to first index
        for (int idxCur = (idxStart + 1) % map.Count; idxCur != idxStart; idxCur = (idxCur + 1) % map.Count)
        {
            if (!predicate (map.ElementAt (idxCur)))
            {
                continue;
            }

            SelectedObject = map.ElementAt (idxCur).Model;
            EnsureVisible (map.ElementAt (idxCur).Model);
            SetNeedsDraw ();

            return;
        }
    }
}
