namespace Terminal.Gui;

internal class Branch<T> where T : class
{
    private readonly TreeView<T> tree;

    /// <summary>
    ///     Declares a new branch of <paramref name="tree"/> in which the users object <paramref name="model"/> is
    ///     presented.
    /// </summary>
    /// <param name="tree">The UI control in which the branch resides.</param>
    /// <param name="parentBranchIfAny">Pass null for root level branches, otherwise pass the parent.</param>
    /// <param name="model">The user's object that should be displayed.</param>
    public Branch (TreeView<T> tree, Branch<T> parentBranchIfAny, T model)
    {
        this.tree = tree;
        Model = model;

        if (parentBranchIfAny is { })
        {
            Depth = parentBranchIfAny.Depth + 1;
            Parent = parentBranchIfAny;
        }
    }

    /// <summary>
    ///     The children of the current branch.  This is null until the first call to <see cref="FetchChildren"/> to avoid
    ///     enumerating the entire underlying hierarchy.
    /// </summary>
    public Dictionary<T, Branch<T>> ChildBranches { get; set; }

    /// <summary>The depth of the current branch.  Depth of 0 indicates root level branches.</summary>
    public int Depth { get; }

    /// <summary>True if the branch is expanded to reveal child branches.</summary>
    public bool IsExpanded { get; set; }

    /// <summary>The users object that is being displayed by this branch of the tree.</summary>
    public T Model { get; private set; }

    /// <summary>The parent <see cref="Branch{T}"/> or null if it is a root.</summary>
    public Branch<T> Parent { get; }

    /// <summary>
    ///     Returns true if the current branch can be expanded according to the <see cref="TreeBuilder{T}"/> or cached
    ///     children already fetched.
    /// </summary>
    /// <returns></returns>
    public bool CanExpand ()
    {
        // if we do not know the children yet
        if (ChildBranches is null)
        {
            //if there is a rapid method for determining whether there are children
            if (tree.TreeBuilder.SupportsCanExpand)
            {
                return tree.TreeBuilder.CanExpand (Model);
            }

            //there is no way of knowing whether we can expand without fetching the children
            FetchChildren ();
        }

        //we fetched or already know the children, so return whether we have any
        return ChildBranches.Any ();
    }

    /// <summary>Marks the branch as collapsed (<see cref="IsExpanded"/> false).</summary>
    public void Collapse () { IsExpanded = false; }

    /// <summary>Renders the current <see cref="Model"/> on the specified line <paramref name="y"/>.</summary>
    /// <param name="driver"></param>
    /// <param name="colorScheme"></param>
    /// <param name="y"></param>
    /// <param name="availableWidth"></param>
    public virtual void Draw (ConsoleDriver driver, ColorScheme colorScheme, int y, int availableWidth)
    {
        List<Cell> cells = new ();
        int? indexOfExpandCollapseSymbol = null;
        int indexOfModelText;

        // true if the current line of the tree is the selected one and control has focus
        bool isSelected = tree.IsSelected (Model);

        Attribute textColor =
            isSelected ? tree.HasFocus ? colorScheme.Focus : colorScheme.HotNormal : colorScheme.Normal;
        Attribute symbolColor = tree.Style.HighlightModelTextOnly ? colorScheme.Normal : textColor;

        // Everything on line before the expansion run and branch text
        Rune [] prefix = GetLinePrefix (driver).ToArray ();
        Rune expansion = GetExpandableSymbol (driver);
        string lineBody = tree.AspectGetter (Model) ?? "";

        tree.Move (0, y);

        // if we have scrolled to the right then bits of the prefix will have disappeared off the screen
        int toSkip = tree.ScrollOffsetHorizontal;
        Attribute attr = symbolColor;

        // Draw the line prefix (all parallel lanes or whitespace and an expand/collapse/leaf symbol)
        foreach (Rune r in prefix)
        {
            if (toSkip > 0)
            {
                toSkip--;
            }
            else
            {
                cells.Add (NewCell (attr, r));
                availableWidth -= r.GetColumns ();
            }
        }

        // pick color for expanded symbol
        if (tree.Style.ColorExpandSymbol || tree.Style.InvertExpandSymbolColors)
        {
            Attribute color = symbolColor;

            if (tree.Style.ColorExpandSymbol)
            {
                if (isSelected)
                {
                    color = tree.Style.HighlightModelTextOnly ? colorScheme.HotNormal :
                            tree.HasFocus ? tree.ColorScheme.HotFocus : tree.ColorScheme.HotNormal;
                }
                else
                {
                    color = tree.ColorScheme.HotNormal;
                }
            }
            else
            {
                color = symbolColor;
            }

            if (tree.Style.InvertExpandSymbolColors)
            {
                color = new Attribute (color.Background, color.Foreground);
            }

            attr = color;
        }

        if (toSkip > 0)
        {
            toSkip--;
        }
        else
        {
            indexOfExpandCollapseSymbol = cells.Count;
            cells.Add (NewCell (attr, expansion));
            availableWidth -= expansion.GetColumns ();
        }

        // horizontal scrolling has already skipped the prefix but now must also skip some of the line body
        if (toSkip > 0)
        {
            // For the event record a negative location for where model text starts since it
            // is pushed off to the left because of scrolling
            indexOfModelText = -toSkip;

            if (toSkip > lineBody.Length)
            {
                lineBody = "";
            }
            else
            {
                lineBody = lineBody.Substring (toSkip);
            }
        }
        else
        {
            indexOfModelText = cells.Count;
        }

        // If body of line is too long
        if (lineBody.EnumerateRunes ().Sum (l => l.GetColumns ()) > availableWidth)
        {
            // remaining space is zero and truncate the line
            lineBody = new string (
                                   lineBody.TakeWhile (c => (availableWidth -= ((Rune)c).GetColumns ()) >= 0)
                                           .ToArray ()
                                  );
            availableWidth = 0;
        }
        else
        {
            // line is short so remaining width will be whatever comes after the line body
            availableWidth -= lineBody.Length;
        }

        // default behaviour is for model to use the color scheme
        // of the tree view
        Attribute modelColor = textColor;

        // if custom color delegate invoke it
        if (tree.ColorGetter is { })
        {
            ColorScheme modelScheme = tree.ColorGetter (Model);

            // if custom color scheme is defined for this Model
            if (modelScheme is { })
            {
                // use it
                modelColor = isSelected ? modelScheme.Focus : modelScheme.Normal;
            }
            else
            {
                modelColor = new Attribute ();
            }
        }

        attr = modelColor;
        cells.AddRange (lineBody.Select (r => NewCell (attr, new Rune (r))));

        if (availableWidth > 0)
        {
            attr = symbolColor;

            cells.AddRange (
                            Enumerable.Repeat (
                                               NewCell (attr, new Rune (' ')),
                                               availableWidth
                                              )
                           );
        }

        DrawTreeViewLineEventArgs<T> e = new ()
        {
            Model = Model,
            Y = y,
            Cells = cells,
            Tree = tree,
            IndexOfExpandCollapseSymbol =
                indexOfExpandCollapseSymbol,
            IndexOfModelText = indexOfModelText
        };
        tree.OnDrawLine (e);

        if (!e.Handled)
        {
            foreach (Cell cell in cells)
            {
                driver.SetAttribute ((Attribute)cell.Attribute!);
                driver.AddRune (cell.Rune);
            }
        }

        driver.SetAttribute (colorScheme.Normal);
    }

    /// <summary>Expands the current branch if possible.</summary>
    public void Expand ()
    {
        if (ChildBranches is null)
        {
            FetchChildren ();
        }

        if (ChildBranches.Any ())
        {
            IsExpanded = true;
        }
    }

    /// <summary>Fetch the children of this branch. This method populates <see cref="ChildBranches"/>.</summary>
    public virtual void FetchChildren ()
    {
        if (tree.TreeBuilder is null)
        {
            return;
        }

        IEnumerable<T> children;

        if (Depth >= tree.MaxDepth)
        {
            children = Enumerable.Empty<T> ();
        }
        else
        {
            children = tree.TreeBuilder.GetChildren (Model) ?? Enumerable.Empty<T> ();
        }

        ChildBranches = children.ToDictionary (k => k, val => new Branch<T> (tree, this, val));
    }

    /// <summary>
    ///     Returns an appropriate symbol for displaying next to the string representation of the <see cref="Model"/>
    ///     object to indicate whether it <see cref="IsExpanded"/> or not (or it is a leaf).
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public Rune GetExpandableSymbol (ConsoleDriver driver)
    {
        Rune leafSymbol = tree.Style.ShowBranchLines ? Glyphs.HLine : (Rune)' ';

        if (IsExpanded)
        {
            return tree.Style.CollapseableSymbol ?? leafSymbol;
        }

        if (CanExpand ())
        {
            return tree.Style.ExpandableSymbol ?? leafSymbol;
        }

        return leafSymbol;
    }

    /// <summary>
    ///     Returns the width of the line including prefix and the results of <see cref="TreeView{T}.AspectGetter"/> (the
    ///     line body).
    /// </summary>
    /// <returns></returns>
    public virtual int GetWidth (ConsoleDriver driver)
    {
        return
            GetLinePrefix (driver).Sum (r => r.GetColumns ()) + GetExpandableSymbol (driver).GetColumns () + (tree.AspectGetter (Model) ?? "").Length;
    }

    /// <summary>Refreshes cached knowledge in this branch e.g. what children an object has.</summary>
    /// <param name="startAtTop">True to also refresh all <see cref="Parent"/> branches (starting with the root).</param>
    public void Refresh (bool startAtTop)
    {
        // if we must go up and refresh from the top down
        if (startAtTop)
        {
            Parent?.Refresh (true);
        }

        // we don't want to lose the state of our children so lets be selective about how we refresh
        //if we don't know about any children yet just use the normal method
        if (ChildBranches is null)
        {
            FetchChildren ();
        }
        else
        {
            // we already knew about some children so preserve the state of the old children

            // first gather the new Children
            IEnumerable<T> newChildren = tree.TreeBuilder?.GetChildren (Model) ?? Enumerable.Empty<T> ();

            // Children who no longer appear need to go
            foreach (T toRemove in ChildBranches.Keys.Except (newChildren).ToArray ())
            {
                ChildBranches.Remove (toRemove);

                //also if the user has this node selected (its disappearing) so lets change selection to us (the parent object) to be helpful
                if (Equals (tree.SelectedObject, toRemove))
                {
                    tree.SelectedObject = Model;
                }
            }

            // New children need to be added
            foreach (T newChild in newChildren)
            {
                // If we don't know about the child, yet we need a new branch
                if (!ChildBranches.ContainsKey (newChild))
                {
                    ChildBranches.Add (newChild, new Branch<T> (tree, this, newChild));
                }
                else
                {
                    //we already have this object but update the reference anyway in case Equality match but the references are new
                    ChildBranches [newChild].Model = newChild;
                }
            }
        }
    }

    /// <summary>
    ///     Collapses the current branch and all children branches (even though those branches are no longer visible they
    ///     retain collapse/expansion state).
    /// </summary>
    internal void CollapseAll ()
    {
        Collapse ();

        if (ChildBranches is { })
        {
            foreach (KeyValuePair<T, Branch<T>> child in ChildBranches)
            {
                child.Value.CollapseAll ();
            }
        }
    }

    /// <summary>Expands the current branch and all children branches.</summary>
    internal void ExpandAll ()
    {
        Expand ();

        if (ChildBranches is { })
        {
            foreach (KeyValuePair<T, Branch<T>> child in ChildBranches)
            {
                child.Value.ExpandAll ();
            }
        }
    }

    /// <summary>
    ///     Gets all characters to render prior to the current branches line.  This includes indentation whitespace and
    ///     any tree branches (if enabled).
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    internal IEnumerable<Rune> GetLinePrefix (ConsoleDriver driver)
    {
        // If not showing line branches or this is a root object.
        if (!tree.Style.ShowBranchLines)
        {
            for (var i = 0; i < Depth; i++)
            {
                yield return new Rune (' ');
            }

            yield break;
        }

        // yield indentations with runes appropriate to the state of the parents
        foreach (Branch<T> cur in GetParentBranches ().Reverse ())
        {
            if (cur.IsLast ())
            {
                yield return new Rune (' ');
            }
            else
            {
                yield return Glyphs.VLine;
            }

            yield return new Rune (' ');
        }

        if (IsLast ())
        {
            yield return Glyphs.LLCorner;
        }
        else
        {
            yield return Glyphs.LeftTee;
        }
    }

    /// <summary>
    ///     Returns true if the given x offset on the branch line is the +/- symbol.  Returns false if not showing
    ///     expansion symbols or leaf node etc.
    /// </summary>
    /// <param name="driver"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    internal bool IsHitOnExpandableSymbol (ConsoleDriver driver, int x)
    {
        // if leaf node then we cannot expand
        if (!CanExpand ())
        {
            return false;
        }

        // if we could theoretically expand
        if (!IsExpanded && tree.Style.ExpandableSymbol != default (Rune?))
        {
            return x == GetLinePrefix (driver).Count ();
        }

        // if we could theoretically collapse
        if (IsExpanded && tree.Style.CollapseableSymbol != default (Rune?))
        {
            return x == GetLinePrefix (driver).Count ();
        }

        return false;
    }

    /// <summary>Calls <see cref="Refresh(bool)"/> on the current branch and all expanded children.</summary>
    internal void Rebuild ()
    {
        Refresh (false);

        // if we know about our children
        if (ChildBranches is { })
        {
            if (IsExpanded)
            {
                // if we are expanded we need to update the visible children
                foreach (KeyValuePair<T, Branch<T>> child in ChildBranches)
                {
                    child.Value.Rebuild ();
                }
            }
            else
            {
                // we are not expanded so should forget about children because they may not exist anymore
                ChildBranches = null;
            }
        }
    }

    /// <summary>Returns all parents starting with the immediate parent and ending at the root.</summary>
    /// <returns></returns>
    private IEnumerable<Branch<T>> GetParentBranches ()
    {
        Branch<T> cur = Parent;

        while (cur is { })
        {
            yield return cur;

            cur = cur.Parent;
        }
    }

    /// <summary>
    ///     Returns true if this branch has parents, and it is the last node of its parents branches (or last root of the
    ///     tree).
    /// </summary>
    /// <returns></returns>
    private bool IsLast ()
    {
        if (Parent is null)
        {
            return this == tree.roots.Values.LastOrDefault ();
        }

        return Parent.ChildBranches.Values.LastOrDefault () == this;
    }

    private static Cell NewCell (Attribute attr, Rune r) { return new Cell { Rune = r, Attribute = new (attr) }; }
}
