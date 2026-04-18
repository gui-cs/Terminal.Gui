// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

#nullable disable
using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>
///     Interface for all non-generic members of <see cref="TreeView{T}"/>.
///     <a href="../docs/treeview.md">See TreeView Deep Dive for more information</a>.
/// </summary>
public interface ITreeView
{
    /// <summary>Contains options for changing how the tree is rendered.</summary>
    TreeStyle Style { get; set; }

    /// <summary>Removes all objects from the tree and clears selection.</summary>
    void ClearObjects ();

    /// <summary>Sets a flag indicating this view needs to be drawn because its state has changed.</summary>
    void SetNeedsDraw ();
}

/// <summary>
///     Convenience implementation of generic <see cref="TreeView{T}"/> for any tree were all nodes implement
///     <see cref="ITreeNode"/>. <a href="../docs/treeview.md">See TreeView Deep Dive for more information</a>.
/// </summary>
public class TreeView : TreeView<ITreeNode>, IDesignable
{
    /// <summary>
    ///     Creates a new instance of the tree control with absolute positioning and initialises
    ///     <see cref="TreeBuilder{T}"/> with default <see cref="ITreeNode"/> based builder.
    /// </summary>
    public TreeView ()
    {
        CanFocus = true;

        TreeBuilder = new TreeNodeBuilder ();
        AspectGetter = o => o is null ? "Null" : o.Text ?? o.ToString () ?? "Unnamed Node";
    }

    bool IDesignable.EnableForDesign ()
    {
        var root1 = new TreeNode ("Root1");
        root1.Children.Add (new TreeNode ("Child1.1"));
        root1.Children.Add (new TreeNode ("Child1.2"));

        var root2 = new TreeNode ("Root2");
        root2.Children.Add (new TreeNode ("Child2.1"));
        root2.Children.Add (new TreeNode ("Child2.2"));

        AddObject (root1);
        AddObject (root2);

        ExpandAll ();

        return true;
    }
}

/// <summary>
///     Hierarchical tree view with expandable branches. Branch objects are dynamically determined when expanded using
///     a user defined <see cref="ITreeBuilder{T}"/>.
///     <a href="../docs/treeview.md">See TreeView Deep Dive for more information</a>.
/// </summary>
/// <remarks>
///     <para>
///         Key bindings are configurable via <see cref="DefaultKeyBindings"/> (TreeView-specific commands)
///         and <see cref="View.DefaultKeyBindings"/> (shared navigation commands). The instance-dependent
///         <see cref="ObjectActivationKey"/> binding is added directly in the constructor.
///     </para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Up</term> <description>Moves up one node.</description>
///         </item>
///         <item>
///             <term>Down</term> <description>Moves down one node.</description>
///         </item>
///         <item>
///             <term>Right</term> <description>Expands the current branch.</description>
///         </item>
///         <item>
///             <term>Ctrl+Right</term> <description>Expands the current branch and all sub-branches.</description>
///         </item>
///         <item>
///             <term>Left</term> <description>Collapses the current branch.</description>
///         </item>
///         <item>
///             <term>Ctrl+Left</term> <description>Collapses the current branch and all sub-branches.</description>
///         </item>
///         <item>
///             <term>Ctrl+Up</term> <description>Moves up to the first branch at the same level.</description>
///         </item>
///         <item>
///             <term>Ctrl+Down</term> <description>Moves down to the last branch at the same level.</description>
///         </item>
///         <item>
///             <term>PageUp / PageDown</term> <description>Moves one page up or down.</description>
///         </item>
///         <item>
///             <term>Shift+Up / Shift+Down</term> <description>Extends the selection up or down.</description>
///         </item>
///         <item>
///             <term>Shift+PageUp / Shift+PageDown</term> <description>Extends the selection by one page.</description>
///         </item>
///         <item>
///             <term>Home / End</term> <description>Moves to the first or last node.</description>
///         </item>
///         <item>
///             <term>Ctrl+A</term> <description>Selects all nodes.</description>
///         </item>
///     </list>
/// </remarks>
public partial class TreeView<T> : View, ITreeView where T : class
{
    /// <summary>
    ///     Error message to display when the control is not properly initialized at draw time (nodes added but no tree
    ///     builder set).
    /// </summary>
    public static string NoBuilderError = "ERROR: TreeBuilder Not Set";

    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="TreeView{T}"/>. These are layered on top of
    ///     <see cref="View.DefaultKeyBindings"/> when the view is created.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only single-command bindings are included here. The instance-dependent
    ///         <see cref="ObjectActivationKey"/> binding is added directly in the constructor.
    ///     </para>
    ///     <para>
    ///         This property is not decorated with <see cref="ConfigurationPropertyAttribute"/> because
    ///         <see cref="TreeView{T}"/> is a generic type. Use <see cref="View.ViewKeyBindings"/> to
    ///         override key bindings for TreeView via configuration.
    ///     </para>
    /// </remarks>
    public new static Dictionary<Command, PlatformKeyBinding> DefaultKeyBindings { get; set; } = new ()
    {
        // Tree-specific expand/collapse
        [Command.Expand] = Bind.All (Key.CursorRight),
        [Command.ExpandAll] = Bind.All (Key.CursorRight.WithCtrl),
        [Command.Collapse] = Bind.All (Key.CursorLeft),
        [Command.CollapseAll] = Bind.All (Key.CursorLeft.WithCtrl),

        // Branch navigation
        [Command.LineUpToFirstBranch] = Bind.All (Key.CursorUp.WithCtrl),
        [Command.LineDownToLastBranch] = Bind.All (Key.CursorDown.WithCtrl),

        // TreeView adds Home/End as additional Start/End bindings (the base layer also provides Ctrl+Home/Ctrl+End)
        [Command.Start] = Bind.All (Key.Home),
        [Command.End] = Bind.All (Key.End)
    };

    /// <summary>
    ///     Interface for filtering which lines of the tree are displayed e.g. to provide text searching.  Defaults to
    ///     <see langword="null"/> (no filtering).
    /// </summary>
    public ITreeViewFilter<T> Filter = null;

    /// <summary>Secondary selected regions of tree when <see cref="MultiSelect"/> is true.</summary>
    private readonly Stack<TreeSelection<T>> _multiSelectedRegions = new ();

    /// <summary>Cached result of <see cref="BuildLineMap"/></summary>
    private IReadOnlyCollection<Branch<T>> _cachedLineMap;

    private KeyCode _objectActivationKey = KeyCode.Enter;
    private int _scrollOffsetHorizontal;
    private int _scrollOffsetVertical;

    /// <summary>private variable for <see cref="SelectedObject"/></summary>
    private T _selectedObject;

    /// <summary>
    ///     Creates a new tree view with absolute positioning. Use <see cref="AddObjects(IEnumerable{T})"/> to set
    ///     root objects for the tree. Children will not be rendered until you set <see cref="TreeBuilder"/>.
    /// </summary>
    public TreeView ()
    {
        CanFocus = true;

        // Things this view knows how to do
        AddCommand (Command.PageUp,
                    () =>
                    {
                        MovePageUp ();

                        return true;
                    });

        AddCommand (Command.PageDown,
                    () =>
                    {
                        MovePageDown ();

                        return true;
                    });

        AddCommand (Command.PageUpExtend,
                    () =>
                    {
                        MovePageUp (true);

                        return true;
                    });

        AddCommand (Command.PageDownExtend,
                    () =>
                    {
                        MovePageDown (true);

                        return true;
                    });

        AddCommand (Command.Expand,
                    () =>
                    {
                        Expand ();

                        return true;
                    });

        AddCommand (Command.ExpandAll,
                    () =>
                    {
                        ExpandAll (SelectedObject);

                        return true;
                    });

        AddCommand (Command.Collapse,
                    () =>
                    {
                        CursorLeft (false);

                        return true;
                    });

        AddCommand (Command.CollapseAll,
                    () =>
                    {
                        CursorLeft (true);

                        return true;
                    });

        AddCommand (Command.Up,
                    () =>
                    {
                        AdjustSelection (-1);

                        return true;
                    });

        AddCommand (Command.UpExtend,
                    () =>
                    {
                        AdjustSelection (-1, true);

                        return true;
                    });

        AddCommand (Command.LineUpToFirstBranch,
                    () =>
                    {
                        AdjustSelectionToBranchStart ();

                        return true;
                    });

        AddCommand (Command.Down,
                    () =>
                    {
                        AdjustSelection (1);

                        return true;
                    });

        AddCommand (Command.DownExtend,
                    () =>
                    {
                        AdjustSelection (1, true);

                        return true;
                    });

        AddCommand (Command.LineDownToLastBranch,
                    () =>
                    {
                        AdjustSelectionToBranchEnd ();

                        return true;
                    });

        AddCommand (Command.Start,
                    () =>
                    {
                        GoToFirst ();

                        return true;
                    });

        AddCommand (Command.End,
                    () =>
                    {
                        GoToEnd ();

                        return true;
                    });

        AddCommand (Command.SelectAll,
                    () =>
                    {
                        SelectAll ();

                        return true;
                    });

        AddCommand (Command.ScrollUp,
                    () =>
                    {
                        ScrollUp ();

                        return true;
                    });

        AddCommand (Command.ScrollDown,
                    () =>
                    {
                        ScrollDown ();

                        return true;
                    });

        // Apply configurable key bindings (TreeView-specific layer + base layer)
        ApplyKeyBindings (DefaultKeyBindings, View.DefaultKeyBindings);

        // Instance-dependent activation key binding (not part of DefaultKeyBindings)
        KeyBindings.Remove (ObjectActivationKey);
        KeyBindings.Add (ObjectActivationKey, Command.Activate);

        KeystrokeNavigator.Matcher = new TreeViewCollectionNavigatorMatcher<T> (this);
    }

    /// <summary>
    ///     Initialises <see cref="TreeBuilder"/>.Creates a new tree view with absolute positioning. Use
    ///     <see cref="AddObjects(IEnumerable{T})"/> to set root objects for the tree.
    /// </summary>
    public TreeView (ITreeBuilder<T> builder) : this () => TreeBuilder = builder;

    /// <summary>True makes a letter key press navigate to the next visible branch that begins with that letter/digit.</summary>
    /// <value></value>
    public bool AllowLetterBasedNavigation { get; set; } = true;

    /// <summary>
    ///     Returns the string representation of model objects hosted in the tree. Default implementation is to call
    ///     <see cref="object.ToString"/>.
    /// </summary>
    /// <value></value>
    public AspectGetterDelegate<T> AspectGetter { get; set; } = o => o.ToString () ?? "";

    /// <summary>
    ///     Delegate for multi-colored tree views. Return the <see cref="Scheme"/> to use for each passed object or
    ///     null to use the default.
    /// </summary>
    public Func<T, Scheme> ColorGetter { get; set; }

    /// <summary>The current number of rows in the tree (ignoring the controls bounds).</summary>
    public int ContentHeight => BuildLineMap ().Count ();

    /// <summary>
    ///     Gets the <see cref="CollectionNavigator"/> that searches the <see cref="Objects"/> collection as the user
    ///     types.
    /// </summary>
    public IListCollectionNavigator KeystrokeNavigator { get; } = new CollectionNavigator ();

    /// <summary>Maximum number of nodes that can be expanded in any given branch.</summary>
    public int MaxDepth { get; set; } = 100;

    /// <summary>True to allow multiple objects to be selected at once.</summary>
    /// <value></value>
    public bool MultiSelect { get; set; } = true;

    /// <summary>
    ///     Mouse event to trigger <see cref="TreeView{T}.ObjectActivated"/>. Defaults to double click (
    ///     <see cref="MouseFlags.LeftButtonDoubleClicked"/>). Set to null to disable this feature.
    /// </summary>
    /// <value></value>
    public MouseFlags? ObjectActivationButton { get; set; } = MouseFlags.LeftButtonDoubleClicked;

    // TODO: Update to use Key instead of KeyCode
    /// <summary>Key which when pressed triggers <see cref="TreeView{T}.ObjectActivated"/>. Defaults to Enter.</summary>
    public KeyCode ObjectActivationKey
    {
        get => _objectActivationKey;
        set
        {
            if (_objectActivationKey == value)
            {
                return;
            }

            KeyBindings.Replace (ObjectActivationKey, value);
            _objectActivationKey = value;
            SetNeedsDraw ();
        }
    }

    /// <summary>The root objects in the tree, note that this collection is of root objects only.</summary>
    public IEnumerable<T> Objects => roots.Keys;

    /// <summary>The amount of tree view that has been scrolled to the right (horizontally).</summary>
    /// <remarks>
    ///     Setting a value of less than 0 will result in a offset of 0. To see changes in the UI call
    ///     <see cref="View.SetNeedsDraw()"/>.
    /// </remarks>
    public int ScrollOffsetHorizontal
    {
        get => _scrollOffsetHorizontal;
        set
        {
            _scrollOffsetHorizontal = Math.Max (0, value);
            SetNeedsDraw ();
        }
    }

    /// <summary>The amount of tree view that has been scrolled off the top of the screen (by the user scrolling down).</summary>
    /// <remarks>
    ///     Setting a value of less than 0 will result in an offset of 0. To see changes in the UI call
    ///     <see cref="View.SetNeedsDraw()"/>.
    /// </remarks>
    public int ScrollOffsetVertical
    {
        get => _scrollOffsetVertical;
        set
        {
            _scrollOffsetVertical = Math.Max (0, value);
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     The currently selected object in the tree. When <see cref="MultiSelect"/> is true this is the object at which
    ///     the cursor is at.
    /// </summary>
    public T SelectedObject
    {
        get => _selectedObject;
        set
        {
            T oldValue = _selectedObject;
            _selectedObject = value;

            if (!ReferenceEquals (oldValue, value))
            {
                OnSelectionChanged (new SelectionChangedEventArgs<T> (this, oldValue, value));
            }
        }
    }

    /// <summary>Determines how sub-branches of the tree are dynamically built at runtime as the user expands root nodes.</summary>
    /// <value></value>
    public ITreeBuilder<T> TreeBuilder { get; set; }

    /// <summary>
    ///     Map of root objects to the branches under them. All objects have a <see cref="Branch{T}"/> even if that branch
    ///     has no children.
    /// </summary>
    internal Dictionary<T, Branch<T>> roots { get; set; } = new ();

    /// <summary>Contains options for changing how the tree is rendered.</summary>
    public TreeStyle Style { get; set; } = new ();

    /// <summary>Removes all objects from the tree and clears <see cref="SelectedObject"/>.</summary>
    public void ClearObjects ()
    {
        SelectedObject = default (T);
        _multiSelectedRegions.Clear ();
        roots = new Dictionary<T, Branch<T>> ();
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext ctx)
    {
        base.OnActivated (ctx);

        T o = SelectedObject;

        if (o is null)
        {
            return;
        }

        ObjectActivatedEventArgs<T> e = new (this, o);
        OnObjectActivated (e);
    }

    /// <inheritdoc/>
    protected override void OnAccepted (ICommandContext ctx)
    {
        base.OnAccepted (ctx);

        T o = SelectedObject;

        if (o is null)
        {
            return;
        }

        ObjectActivatedEventArgs<T> e = new (this, o);
        OnObjectActivated (e);
    }

    /// <summary>
    ///     <para>Triggers the <see cref="ObjectActivated"/> event with the <see cref="SelectedObject"/>.</para>
    ///     <para>This method also ensures that the selected object is visible.</para>
    /// </summary>
    /// <returns><see langword="true"/> if <see cref="ObjectActivated"/> was fired.</returns>
    public bool? ActivateSelectedObjectIfAny (ICommandContext commandContext)
    {
        T o = SelectedObject;

        if (o is null)
        {
            return false;
        }

        ObjectActivatedEventArgs<T> e = new (this, o);
        OnObjectActivated (e);

        return true;
    }

    /// <summary>Adds a new root level object unless it is already a root of the tree.</summary>
    /// <param name="o"></param>
    public void AddObject (T o)
    {
        if (roots.ContainsKey (o))
        {
            return;
        }

        roots.Add (o, new Branch<T> (this, null, o));
        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>Adds many new root level objects. Objects that are already root objects are ignored.</summary>
    /// <param name="collection">Objects to add as new root level objects.</param>
    /// .\
    public void AddObjects (IEnumerable<T> collection)
    {
        var objectsAdded = false;

        foreach (T o in collection)
        {
            if (roots.ContainsKey (o))
            {
                continue;
            }

            roots.Add (o, new Branch<T> (this, null, o));
            objectsAdded = true;
        }

        if (!objectsAdded)
        {
            return;
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Rebuilds the tree structure for all exposed objects starting with the root objects. Call this method when you
    ///     know there are changes to the tree but don't know which objects have changed (otherwise use
    ///     <see cref="RefreshObject(T, bool)"/>).
    /// </summary>
    public void RebuildTree ()
    {
        foreach (Branch<T> branch in roots.Values)
        {
            branch.Rebuild ();
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Refreshes the state of the object <paramref name="o"/> in the tree. This will recompute children, string
    ///     representation etc.
    /// </summary>
    /// <remarks>This has no effect if the object is not exposed in the tree.</remarks>
    /// <param name="o"></param>
    /// <param name="startAtTop">
    ///     True to also refresh all ancestors of the objects branch (starting with the root). False to
    ///     refresh only the passed node.
    /// </param>
    public void RefreshObject (T o, bool startAtTop = false)
    {
        Branch<T> branch = ObjectToBranch (o);

        if (branch is null)
        {
            return;
        }

        branch.Refresh (startAtTop);
        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>Removes the given root object from the tree</summary>
    /// <remarks>If <paramref name="o"/> is the currently <see cref="SelectedObject"/> then the selection is cleared</remarks>
    /// .
    /// <param name="o"></param>
    public void Remove (T o)
    {
        if (!roots.ContainsKey (o))
        {
            return;
        }

        roots.Remove (o);
        InvalidateLineMap ();
        SetNeedsDraw ();

        if (Equals (SelectedObject, o))
        {
            SelectedObject = default (T);
        }
    }

    /// <summary>Clears any cached results of the tree state.</summary>
    public void InvalidateLineMap () => _cachedLineMap = null;

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        ColorGetter = null;
    }

    /// <summary>
    ///     Calculates all currently visible/expanded branches (including leafs) and outputs them by index from the top of
    ///     the screen.
    /// </summary>
    /// <remarks>
    ///     Index 0 of the returned array is the first item that should be visible in the top of the control, index 1 is
    ///     the next etc.
    /// </remarks>
    /// <returns></returns>
    internal IReadOnlyCollection<Branch<T>> BuildLineMap ()
    {
        if (_cachedLineMap is { })
        {
            return _cachedLineMap;
        }

        List<Branch<T>> toReturn = new ();

        foreach (Branch<T> root in roots.Values)
        {
            IEnumerable<Branch<T>> toAdd = AddToLineMap (root, false, out bool isMatch);

            if (isMatch)
            {
                toReturn.AddRange (toAdd);
            }
        }

        _cachedLineMap = new ReadOnlyCollection<Branch<T>> (toReturn);

        // Update the collection used for search-typing
        KeystrokeNavigator.Collection = _cachedLineMap.Select (b => b.Model).ToArray ();

        return _cachedLineMap;
    }

    private IEnumerable<Branch<T>> AddToLineMap (Branch<T> currentBranch, bool parentMatches, out bool match)
    {
        bool weMatch = IsFilterMatch (currentBranch);
        var anyChildMatches = false;

        List<Branch<T>> toReturn = new ();
        List<Branch<T>> children = new ();

        if (currentBranch.IsExpanded && currentBranch.ChildBranches is not null)
        {
            foreach (Branch<T> subBranch in currentBranch.ChildBranches)
            {
                foreach (Branch<T> sub in AddToLineMap (subBranch, weMatch, out bool childMatch))
                {
                    if (!childMatch)
                    {
                        continue;
                    }

                    children.Add (sub);
                    anyChildMatches = true;
                }
            }
        }

        if (parentMatches || weMatch || anyChildMatches)
        {
            match = true;
            toReturn.Add (currentBranch);
        }
        else
        {
            match = false;
        }

        toReturn.AddRange (children);

        return toReturn;
    }

    private bool IsFilterMatch (Branch<T> branch) => Filter?.IsMatch (branch.Model) ?? true;

    /// <summary>
    ///     Returns the corresponding <see cref="Branch{T}"/> in the tree for <paramref name="toFind"/>. This will not
    ///     work for objects hidden by their parent being collapsed.
    /// </summary>
    /// <param name="toFind"></param>
    /// <returns>The branch for <paramref name="toFind"/> or null if it is not currently exposed in the tree.</returns>
    private Branch<T> ObjectToBranch (T toFind) => BuildLineMap ().FirstOrDefault (o => o.Model.Equals (toFind));
}

internal class TreeSelection<T> where T : class
{
    private readonly HashSet<T> _included = new ();

    /// <summary>Creates a new selection between two branches in the tree</summary>
    /// <param name="from"></param>
    /// <param name="toIndex"></param>
    /// <param name="map"></param>
    public TreeSelection (Branch<T> from, int toIndex, IReadOnlyCollection<Branch<T>> map)
    {
        Origin = from;
        _included.Add (Origin.Model);

        int oldIdx = map.IndexOf (from);

        int lowIndex = Math.Min (oldIdx, toIndex);
        int highIndex = Math.Max (oldIdx, toIndex);

        // Select everything between the old and new indexes
        foreach (Branch<T> alsoInclude in map.Skip (lowIndex).Take (highIndex - lowIndex))
        {
            _included.Add (alsoInclude.Model);
        }
    }

    public Branch<T> Origin { get; }
    public bool Contains (T model) => _included.Contains (model);
}
