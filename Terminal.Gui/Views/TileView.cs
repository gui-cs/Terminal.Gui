namespace Terminal.Gui;

/// <summary>
///     A <see cref="View"/> consisting of a moveable bar that divides the display area into resizeable
///     <see cref="Tiles"/>.
/// </summary>
public class TileView : View
{
    private Orientation _orientation = Orientation.Vertical;
    private List<Pos> _splitterDistances;
    private List<TileViewLineView> _splitterLines;
    private List<Tile> _tiles;
    private TileView _parentTileView;

    /// <summary>Creates a new instance of the <see cref="TileView"/> class with 2 tiles (i.e. left and right).</summary>
    public TileView () : this (2)
    {
    }

    /// <summary>Creates a new instance of the <see cref="TileView"/> class with <paramref name="tiles"/> number of tiles.</summary>
    /// <param name="tiles"></param>
    public TileView (int tiles)
    {
        CanFocus = true;
        RebuildForTileCount (tiles);
    }

    /// <summary>The line style to use when drawing the splitter lines.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.None;

    /// <summary>Orientation of the dividing line (Horizontal or Vertical).</summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;

            if (IsInitialized)
            {
                LayoutSubviews ();
            }
        }
    }

    /// <summary>The splitter locations. Note that there will be N-1 splitters where N is the number of <see cref="Tiles"/>.</summary>
    public IReadOnlyCollection<Pos> SplitterDistances => _splitterDistances.AsReadOnly ();

    /// <summary>The sub sections hosted by the view</summary>
    public IReadOnlyCollection<Tile> Tiles => _tiles.AsReadOnly ();

    // TODO: Update to use Key instead of KeyCode
    /// <summary>
    ///     The keyboard key that the user can press to toggle resizing of splitter lines.  Mouse drag splitting is always
    ///     enabled.
    /// </summary>
    public KeyCode ToggleResizable { get; set; } = KeyCode.CtrlMask | KeyCode.F10;

    /// <summary>
    ///     Returns the immediate parent <see cref="TileView"/> of this. Note that in case of deep nesting this might not
    ///     be the root <see cref="TileView"/>. Returns null if this instance is not a nested child (created with
    ///     <see cref="TrySplitTile(int, int, out TileView)"/>)
    /// </summary>
    /// <remarks>Use <see cref="IsRootTileView"/> to determine if the returned value is the root.</remarks>
    /// <returns></returns>
    public TileView GetParentTileView () { return _parentTileView; }

    /// <summary>
    ///     Returns the index of the first <see cref="Tile"/> in <see cref="Tiles"/> which contains
    ///     <paramref name="toFind"/>.
    /// </summary>
    public int IndexOf (View toFind, bool recursive = false)
    {
        for (var i = 0; i < _tiles.Count; i++)
        {
            View v = _tiles [i].ContentView;

            if (v == toFind)
            {
                return i;
            }

            if (v.Subviews.Contains (toFind))
            {
                return i;
            }

            if (recursive)
            {
                if (RecursiveContains (v.Subviews, toFind))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    ///     Adds a new <see cref="Tile"/> to the collection at <paramref name="idx"/>. This will also add another splitter
    ///     line
    /// </summary>
    /// <param name="idx"></param>
    public Tile InsertTile (int idx)
    {
        Tile [] oldTiles = Tiles.ToArray ();
        RebuildForTileCount (oldTiles.Length + 1);

        Tile toReturn = null;

        for (var i = 0; i < _tiles.Count; i++)
        {
            if (i != idx)
            {
                Tile oldTile = oldTiles [i > idx ? i - 1 : i];

                // remove the new empty View
                Remove (_tiles [i].ContentView);
                _tiles [i].ContentView.Dispose ();
                _tiles [i].ContentView = null;

                // restore old Tile and View
                _tiles [i] = oldTile;
                _tiles [i].ContentView.TabStop = TabStop;
                Add (_tiles [i].ContentView);
            }
            else
            {
                toReturn = _tiles [i];
            }
        }

        SetNeedsDisplay ();

        if (IsInitialized)
        {
            LayoutSubviews ();
        }

        return toReturn;
    }

    /// <summary>
    ///     <para>
    ///         <see langword="true"/> if <see cref="TileView"/> is nested within a parent <see cref="TileView"/> e.g. via
    ///         the <see cref="TrySplitTile"/>. <see langword="false"/> if it is a root level <see cref="TileView"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Note that manually adding one <see cref="TileView"/> to another will not result in a parent/child relationship
    ///     and both will still be considered 'root' containers. Always use <see cref="TrySplitTile(int, int, out TileView)"/>
    ///     if you want to subdivide a <see cref="TileView"/>.
    /// </remarks>
    /// <returns></returns>
    public bool IsRootTileView () { return _parentTileView == null; }

    /// <inheritdoc/>
    public override void LayoutSubviews ()
    {
        if (!IsInitialized)
        {
            return;
        }

        Rectangle viewport = Viewport;

        if (HasBorder ())
        {
            viewport = new (
                            viewport.X + 1,
                            viewport.Y + 1,
                            Math.Max (0, viewport.Width - 2),
                            Math.Max (0, viewport.Height - 2)
                           );
        }

        Setup (viewport);
        base.LayoutSubviews ();
    }

    // BUG: v2 fix this hack
    // QUESTION: Does this need to be fixed before events are refactored?
    /// <summary>Overridden so no Frames get drawn</summary>
    /// <returns></returns>
    public override bool OnDrawAdornments () { return false; }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        Driver.SetAttribute (ColorScheme.Normal);

        Clear ();

        base.OnDrawContent (viewport);

        var lc = new LineCanvas ();

        List<TileViewLineView> allLines = GetAllLineViewsRecursively (this);
        List<TileTitleToRender> allTitlesToRender = GetAllTitlesToRenderRecursively (this);

        if (IsRootTileView ())
        {
            if (HasBorder ())
            {
                lc.AddLine (Point.Empty, Viewport.Width, Orientation.Horizontal, LineStyle);
                lc.AddLine (Point.Empty, Viewport.Height, Orientation.Vertical, LineStyle);

                lc.AddLine (
                            new Point (Viewport.Width - 1, Viewport.Height - 1),
                            -Viewport.Width,
                            Orientation.Horizontal,
                            LineStyle
                           );

                lc.AddLine (
                            new Point (Viewport.Width - 1, Viewport.Height - 1),
                            -Viewport.Height,
                            Orientation.Vertical,
                            LineStyle
                           );
            }

            foreach (TileViewLineView line in allLines)
            {
                bool isRoot = _splitterLines.Contains (line);

                Rectangle screen = line.ViewportToScreen (Rectangle.Empty);
                Point origin = ScreenToFrame (screen.Location);
                int length = line.Orientation == Orientation.Horizontal ? line.Frame.Width : line.Frame.Height;

                if (!isRoot)
                {
                    if (line.Orientation == Orientation.Horizontal)
                    {
                        origin.X -= 1;
                    }
                    else
                    {
                        origin.Y -= 1;
                    }

                    length += 2;
                }

                lc.AddLine (origin, length, line.Orientation, LineStyle);
            }
        }

        Driver.SetAttribute (ColorScheme.Normal);

        foreach (KeyValuePair<Point, Rune> p in lc.GetMap (Viewport))
        {
            AddRune (p.Key.X, p.Key.Y, p.Value);
        }

        // Redraw the lines so that focus/drag symbol renders
        foreach (TileViewLineView line in allLines)
        {
            line.DrawSplitterSymbol ();
        }

        // Draw Titles over Border

        foreach (TileTitleToRender titleToRender in allTitlesToRender)
        {
            Point renderAt = titleToRender.GetLocalCoordinateForTitle (this);

            if (renderAt.Y < 0)
            {
                // If we have no border then root level tiles
                // have nowhere to render their titles.
                continue;
            }

            // TODO: Render with focus color if focused

            string title = titleToRender.GetTrimmedTitle ();

            for (var i = 0; i < title.Length; i++)
            {
                AddRune (renderAt.X + i, renderAt.Y, (Rune)title [i]);
            }
        }
    }

    //// BUGBUG: Why is this not handled by a key binding???
    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        var focusMoved = false;

        if (key.KeyCode == ToggleResizable)
        {
            foreach (TileViewLineView l in _splitterLines)
            {
                bool iniBefore = l.IsInitialized;
                l.IsInitialized = false;
                l.CanFocus = !l.CanFocus;
                l.IsInitialized = iniBefore;

                if (l.CanFocus && !focusMoved)
                {
                    l.SetFocus ();
                    focusMoved = true;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Scraps all <see cref="Tiles"/> and creates <paramref name="count"/> new tiles in orientation
    ///     <see cref="Orientation"/>
    /// </summary>
    /// <param name="count"></param>
    public void RebuildForTileCount (int count)
    {
        _tiles = new List<Tile> ();
        _splitterDistances = new List<Pos> ();

        if (_splitterLines is { })
        {
            foreach (TileViewLineView sl in _splitterLines)
            {
                sl.Dispose ();
            }
        }

        _splitterLines = new List<TileViewLineView> ();

        RemoveAll ();

        foreach (Tile tile in _tiles)
        {
            tile.ContentView.Dispose ();
            tile.ContentView = null;
        }

        _tiles.Clear ();
        _splitterDistances.Clear ();

        if (count == 0)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                Pos currentPos = Pos.Percent (100 / count * i);
                _splitterDistances.Add (currentPos);
                var line = new TileViewLineView (this, i - 1);
                Add (line);
                _splitterLines.Add (line);
            }

            var tile = new Tile ();
            _tiles.Add (tile);
            tile.ContentView.Id = $"Tile.ContentView {i}";
            Add (tile.ContentView);
            tile.TitleChanged += (s, e) => SetNeedsDisplay ();
        }

        if (IsInitialized)
        {
            LayoutSubviews ();
        }
    }

    /// <summary>
    ///     Removes a <see cref="Tiles"/> at the provided <paramref name="idx"/> from the view. Returns the removed tile
    ///     or null if already empty.
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public Tile RemoveTile (int idx)
    {
        Tile [] oldTiles = Tiles.ToArray ();

        if (idx < 0 || idx >= oldTiles.Length)
        {
            return null;
        }

        Tile removed = Tiles.ElementAt (idx);

        RebuildForTileCount (oldTiles.Length - 1);

        for (var i = 0; i < _tiles.Count; i++)
        {
            int oldIdx = i >= idx ? i + 1 : i;
            Tile oldTile = oldTiles [oldIdx];

            // remove the new empty View
            Remove (_tiles [i].ContentView);
            _tiles [i].ContentView.Dispose ();
            _tiles [i].ContentView = null;

            // restore old Tile and View
            _tiles [i] = oldTile;
            Add (_tiles [i].ContentView);
        }

        SetNeedsDisplay ();
        LayoutSubviews ();

        return removed;
    }

    /// <summary>
    ///     <para>
    ///         Attempts to update the <see cref="SplitterDistances"/> of line at <paramref name="idx"/> to the new
    ///         <paramref name="value"/>. Returns false if the new position is not allowed because of
    ///         <see cref="Tile.MinSize"/>, location of other splitters etc.
    ///     </para>
    ///     <para>
    ///         Only absolute values (e.g. 10) and percent values (i.e. <see cref="Pos.Percent(int)"/>) are supported for
    ///         this property.
    ///     </para>
    /// </summary>
    public bool SetSplitterPos (int idx, Pos value)
    {
        if (!(value is PosAbsolute) && !(value is PosPercent))
        {
            throw new ArgumentException (
                                         $"Only Percent and Absolute values are supported. Passed value was {value.GetType ().Name}"
                                        );
        }

        int fullSpace = _orientation == Orientation.Vertical ? Viewport.Width : Viewport.Height;

        if (fullSpace != 0 && !IsValidNewSplitterPos (idx, value, fullSpace))
        {
            return false;
        }

        _splitterDistances [idx] = value;
        GetRootTileView ().LayoutSubviews ();
        OnSplitterMoved (idx);

        return true;
    }

    /// <summary>Invoked when any of the <see cref="SplitterDistances"/> is changed.</summary>
    public event SplitterEventHandler SplitterMoved;

    /// <summary>
    ///     Converts of <see cref="Tiles"/> element <paramref name="idx"/> from a regular <see cref="View"/> to a new
    ///     nested <see cref="TileView"/> the specified <paramref name="numberOfPanels"/>. Returns false if the element already
    ///     contains a nested view.
    /// </summary>
    /// <remarks>
    ///     After successful splitting, the old contents will be moved to the <paramref name="result"/>
    ///     <see cref="TileView"/> 's first tile.
    /// </remarks>
    /// <param name="idx">The element of <see cref="Tiles"/> that is to be subdivided.</param>
    /// <param name="numberOfPanels">The number of panels that the <see cref="Tile"/> should be split into</param>
    /// <param name="result">The new nested <see cref="TileView"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if a <see cref="View"/> was converted to a new nested <see cref="TileView"/>.
    ///     <see langword="false"/> if it was already a nested <see cref="TileView"/>
    /// </returns>
    public bool TrySplitTile (int idx, int numberOfPanels, out TileView result)
    {
        // when splitting a view into 2 sub views we will need to migrate
        // the title too
        Tile tile = _tiles [idx];

        string title = tile.Title;
        View toMove = tile.ContentView;

        if (toMove is TileView existing)
        {
            result = existing;

            return false;
        }

        var newContainer = new TileView (numberOfPanels)
        {
            Width = Dim.Fill (), Height = Dim.Fill (), _parentTileView = this
        };

        // Take everything out of the View we are moving
        View [] childViews = toMove.Subviews.ToArray ();
        toMove.RemoveAll ();

        // Remove the view itself and replace it with the new TileView
        Remove (toMove);
        toMove.Dispose ();
        toMove = null;

        Add (newContainer);

        tile.ContentView = newContainer;

        View newTileView1 = newContainer._tiles [0].ContentView;

        // Add the original content into the first view of the new container
        foreach (View childView in childViews)
        {
            newTileView1.Add (childView);
        }

        // Move the title across too
        newContainer._tiles [0].Title = title;
        tile.Title = string.Empty;

        result = newContainer;

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        foreach (Tile tile in Tiles)
        {
            Remove (tile.ContentView);
            tile.ContentView.Dispose ();
        }

        base.Dispose (disposing);
    }

    /// <summary>Raises the <see cref="SplitterMoved"/> event</summary>
    protected virtual void OnSplitterMoved (int idx) { SplitterMoved?.Invoke (this, new SplitterEventArgs (this, idx, _splitterDistances [idx])); }

    private List<TileViewLineView> GetAllLineViewsRecursively (View v)
    {
        List<TileViewLineView> lines = new ();

        foreach (View sub in v.Subviews)
        {
            if (sub is TileViewLineView s)
            {
                if (s.Visible && s.Parent.GetRootTileView () == this)
                {
                    lines.Add (s);
                }
            }
            else
            {
                if (sub.Visible)
                {
                    lines.AddRange (GetAllLineViewsRecursively (sub));
                }
            }
        }

        return lines;
    }

    private List<TileTitleToRender> GetAllTitlesToRenderRecursively (TileView v, int depth = 0)
    {
        List<TileTitleToRender> titles = new ();

        foreach (Tile sub in v.Tiles)
        {
            // Don't render titles for invisible stuff!
            if (!sub.ContentView.Visible)
            {
                continue;
            }

            if (sub.ContentView is TileView subTileView)
            {
                // Panels with sub split tiles in them can never
                // have their Titles rendered. Instead we dive in
                // and pull up their children as titles
                titles.AddRange (GetAllTitlesToRenderRecursively (subTileView, depth + 1));
            }
            else
            {
                if (sub.Title.Length > 0)
                {
                    titles.Add (new TileTitleToRender (v, sub, depth));
                }
            }
        }

        return titles;
    }

    private TileView GetRootTileView ()
    {
        TileView root = this;

        while (root._parentTileView is { })
        {
            root = root._parentTileView;
        }

        return root;
    }

    private Dim GetTileWidthOrHeight (int i, int space, Tile [] visibleTiles, TileViewLineView [] visibleSplitterLines)
    {
        // last tile
        if (i + 1 >= visibleTiles.Length)
        {
            return Dim.Fill (HasBorder () ? 1 : 0);
        }

        TileViewLineView nextSplitter = visibleSplitterLines [i];
        Pos nextSplitterPos = Orientation == Orientation.Vertical ? nextSplitter.X : nextSplitter.Y;
        int nextSplitterDistance = nextSplitterPos.GetAnchor (space);

        TileViewLineView lastSplitter = i >= 1 ? visibleSplitterLines [i - 1] : null;
        Pos lastSplitterPos = Orientation == Orientation.Vertical ? lastSplitter?.X : lastSplitter?.Y;
        int lastSplitterDistance = lastSplitterPos?.GetAnchor (space) ?? 0;

        int distance = nextSplitterDistance - lastSplitterDistance;

        if (i > 0)
        {
            return distance - 1;
        }

        return distance - (HasBorder () ? 1 : 0);
    }

    private bool HasBorder () { return LineStyle != LineStyle.None; }

    private void HideSplittersBasedOnTileVisibility ()
    {
        if (_splitterLines.Count == 0)
        {
            return;
        }

        foreach (TileViewLineView line in _splitterLines)
        {
            line.Visible = true;
        }

        for (var i = 0; i < _tiles.Count; i++)
        {
            if (!_tiles [i].ContentView.Visible)
            {
                // when a tile is not visible, prefer hiding
                // the splitter on it's left
                TileViewLineView candidate = _splitterLines [Math.Max (0, i - 1)];

                // unless that splitter is already hidden
                // e.g. when hiding panels 0 and 1 of a 3 panel 
                // container
                if (candidate.Visible)
                {
                    candidate.Visible = false;
                }
                else
                {
                    _splitterLines [Math.Min (i, _splitterLines.Count - 1)].Visible = false;
                }
            }
        }
    }

    private bool IsValidNewSplitterPos (int idx, Pos value, int fullSpace)
    {
        int newSize = value.GetAnchor (fullSpace);
        bool isGettingBigger = newSize > _splitterDistances [idx].GetAnchor (fullSpace);
        int lastSplitterOrBorder = HasBorder () ? 1 : 0;
        int nextSplitterOrBorder = HasBorder () ? fullSpace - 1 : fullSpace;

        // Cannot move off screen right
        if (newSize >= fullSpace - (HasBorder () ? 1 : 0))
        {
            if (isGettingBigger)
            {
                return false;
            }
        }

        // Cannot move off screen left
        if (newSize < (HasBorder () ? 1 : 0))
        {
            if (!isGettingBigger)
            {
                return false;
            }
        }

        // Do not allow splitter to move left of the one before
        if (idx > 0)
        {
            int posLeft = _splitterDistances [idx - 1].GetAnchor (fullSpace);

            if (newSize <= posLeft)
            {
                return false;
            }

            lastSplitterOrBorder = posLeft;
        }

        // Do not allow splitter to move right of the one after
        if (idx + 1 < _splitterDistances.Count)
        {
            int posRight = _splitterDistances [idx + 1].GetAnchor (fullSpace);

            if (newSize >= posRight)
            {
                return false;
            }

            nextSplitterOrBorder = posRight;
        }

        if (isGettingBigger)
        {
            int spaceForNext = nextSplitterOrBorder - newSize;

            // space required for the last line itself
            if (idx > 0)
            {
                spaceForNext--;
            }

            // don't grow if it would take us below min size of right panel
            if (spaceForNext < _tiles [idx + 1].MinSize)
            {
                return false;
            }
        }
        else
        {
            int spaceForLast = newSize - lastSplitterOrBorder;

            // space required for the line itself
            if (idx > 0)
            {
                spaceForLast--;
            }

            // don't shrink if it would take us below min size of left panel
            if (spaceForLast < _tiles [idx].MinSize)
            {
                return false;
            }
        }

        return true;
    }

    private bool RecursiveContains (IEnumerable<View> haystack, View needle)
    {
        foreach (View v in haystack)
        {
            if (v == needle)
            {
                return true;
            }

            if (RecursiveContains (v.Subviews, needle))
            {
                return true;
            }
        }

        return false;
    }

    private void Setup (Rectangle viewport)
    {
        if (viewport.IsEmpty || viewport.Height <= 0 || viewport.Width <= 0)
        {
            return;
        }

        for (var i = 0; i < _splitterLines.Count; i++)
        {
            TileViewLineView line = _splitterLines [i];

            line.Orientation = Orientation;

            line.Width = _orientation == Orientation.Vertical
                             ? 1
                             : Dim.Fill ();

            line.Height = _orientation == Orientation.Vertical
                              ? Dim.Fill ()
                              : 1;
            line.LineRune = _orientation == Orientation.Vertical ? Glyphs.VLine : Glyphs.HLine;

            if (_orientation == Orientation.Vertical)
            {
                line.X = _splitterDistances [i];
                line.Y = 0;
            }
            else
            {
                line.Y = _splitterDistances [i];
                line.X = 0;
            }
        }

        HideSplittersBasedOnTileVisibility ();

        Tile [] visibleTiles = _tiles.Where (t => t.ContentView.Visible).ToArray ();
        TileViewLineView [] visibleSplitterLines = _splitterLines.Where (l => l.Visible).ToArray ();

        for (var i = 0; i < visibleTiles.Length; i++)
        {
            Tile tile = visibleTiles [i];

            if (Orientation == Orientation.Vertical)
            {
                tile.ContentView.X = i == 0 ? viewport.X : Pos.Right (visibleSplitterLines [i - 1]);
                tile.ContentView.Y = viewport.Y;
                tile.ContentView.Height = viewport.Height;
                tile.ContentView.Width = GetTileWidthOrHeight (i, Viewport.Width, visibleTiles, visibleSplitterLines);
            }
            else
            {
                tile.ContentView.X = viewport.X;
                tile.ContentView.Y = i == 0 ? viewport.Y : Pos.Bottom (visibleSplitterLines [i - 1]);
                tile.ContentView.Width = viewport.Width;
                tile.ContentView.Height = GetTileWidthOrHeight (i, Viewport.Height, visibleTiles, visibleSplitterLines);
            }
            //  BUGBUG: This should not be needed. If any of the pos/dim setters above actually changed values, NeedsDisplay should have already been set. 
            tile.ContentView.SetNeedsDisplay ();
        }
    }

    private class TileTitleToRender
    {
        public TileTitleToRender (TileView parent, Tile tile, int depth)
        {
            Parent = parent;
            Tile = tile;
            Depth = depth;
        }

        public int Depth { get; }
        public TileView Parent { get; }
        public Tile Tile { get; }

        /// <summary>
        ///     Translates the <see cref="Tile"/> title location from its local coordinate space
        ///     <paramref name="intoCoordinateSpace"/>.
        /// </summary>
        public Point GetLocalCoordinateForTitle (TileView intoCoordinateSpace)
        {
            Rectangle screen = Tile.ContentView.ViewportToScreen (Rectangle.Empty);
            return intoCoordinateSpace.ScreenToFrame (new (screen.X, screen.Y - 1));
        }

        internal string GetTrimmedTitle ()
        {
            Dim spaceDim = Tile.ContentView.Width;

            int spaceAbs = spaceDim.GetAnchor (Parent.Viewport.Width);

            var title = $" {Tile.Title} ";

            if (title.Length > spaceAbs)
            {
                return title.Substring (0, spaceAbs);
            }

            return title;
        }
    }

    private class TileViewLineView : LineView
    {
        public Point? moveRuneRenderLocation;

        private Pos dragOrignalPos;
        private Point? dragPosition;

        public TileViewLineView (TileView parent, int idx)
        {
            CanFocus = false;
            TabStop = TabBehavior.TabStop;

            Parent = parent;
            Idx = idx;
            AddCommand (Command.Right, () => { return MoveSplitter (1, 0); });

            AddCommand (Command.Left, () => { return MoveSplitter (-1, 0); });

            AddCommand (Command.Up, () => { return MoveSplitter (0, -1); });

            AddCommand (Command.Down, () => { return MoveSplitter (0, 1); });

            KeyBindings.Add (Key.CursorRight, Command.Right);
            KeyBindings.Add (Key.CursorLeft, Command.Left);
            KeyBindings.Add (Key.CursorUp, Command.Up);
            KeyBindings.Add (Key.CursorDown, Command.Down);
        }

        public int Idx { get; }
        public TileView Parent { get; }

        public void DrawSplitterSymbol ()
        {
            if (dragPosition is { } || CanFocus)
            {
                Point location = moveRuneRenderLocation ?? new Point (Viewport.Width / 2, Viewport.Height / 2);

                AddRune (location.X, location.Y, Glyphs.Diamond);
            }
        }

        protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
        {
            if (!dragPosition.HasValue && mouseEvent.Flags == MouseFlags.Button1Pressed)
            {
                // Start a Drag
                SetFocus ();

                if (mouseEvent.Flags == MouseFlags.Button1Pressed)
                {
                    dragPosition = mouseEvent.Position;
                    dragOrignalPos = Orientation == Orientation.Horizontal ? Y : X;
                    Application.GrabMouse (this);

                    if (Orientation == Orientation.Horizontal)
                    { }
                    else
                    {
                        moveRuneRenderLocation = new Point (
                                                            0,
                                                            Math.Max (1, Math.Min (Viewport.Height - 2, mouseEvent.Position.Y))
                                                           );
                    }
                }

                return true;
            }

            if (
                dragPosition.HasValue && mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
            {
                // Continue Drag

                // how far has user dragged from original location?
                if (Orientation == Orientation.Horizontal)
                {
                    int dy = mouseEvent.Position.Y - dragPosition.Value.Y;
                    Parent.SetSplitterPos (Idx, Offset (Y, dy));
                    moveRuneRenderLocation = new Point (mouseEvent.Position.X, 0);
                }
                else
                {
                    int dx = mouseEvent.Position.X - dragPosition.Value.X;
                    Parent.SetSplitterPos (Idx, Offset (X, dx));
                    moveRuneRenderLocation = new Point (0, Math.Max (1, Math.Min (Viewport.Height - 2, mouseEvent.Position.Y)));
                }

                Parent.SetNeedsDisplay ();

                return true;
            }

            if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && dragPosition.HasValue)
            {
                // End Drag

                Application.UngrabMouse ();

                //Driver.UncookMouse ();
                FinalisePosition (
                                  dragOrignalPos,
                                  Orientation == Orientation.Horizontal ? Y : X
                                 );
                dragPosition = null;
                moveRuneRenderLocation = null;
            }

            return false;
        }

        public override void OnDrawContent (Rectangle viewport)
        {
            base.OnDrawContent (viewport);

            DrawSplitterSymbol ();
        }

        public override Point? PositionCursor ()
        {
            base.PositionCursor ();

            Point location = moveRuneRenderLocation ?? new Point (Viewport.Width / 2, Viewport.Height / 2);
            Move (location.X, location.Y);

            return null; // Hide cursor
        }

        /// <summary>
        ///     <para>
        ///         Determines the absolute position of <paramref name="p"/> and returns a <see cref="PosPercent"/> that
        ///         describes the percentage of that.
        ///     </para>
        ///     <para>
        ///         Effectively turning any <see cref="Pos"/> into a <see cref="PosPercent"/> (as if created with
        ///         <see cref="Pos.Percent(int)"/>)
        ///     </para>
        /// </summary>
        /// <param name="p">The <see cref="Pos"/> to convert to <see cref="Pos.Percent(int)"/></param>
        /// <param name="parentLength">The Height/Width that <paramref name="p"/> lies within</param>
        /// <returns></returns>
        private Pos ConvertToPosPercent (Pos p, int parentLength)
        {
            // Calculate position in the 'middle' of the cell at p distance along parentLength
            float position = p.GetAnchor (parentLength) + 0.5f;

            // Calculate the percentage
            int percent = (int)Math.Round ((position / parentLength) * 100);

            // Return a new PosPercent object
            return Pos.Percent (percent);
        }

        /// <summary>
        ///     <para>
        ///         Moves <see cref="Parent"/> <see cref="TileView.SplitterDistances"/> to <see cref="Pos"/>
        ///         <paramref name="newValue"/> preserving <see cref="Pos"/> format (absolute / relative) that
        ///         <paramref name="oldValue"/> had.
        ///     </para>
        ///     <remarks>
        ///         This ensures that if splitter location was e.g. 50% before and you move it to absolute 5 then you end up
        ///         with 10% (assuming a parent had 50 width).
        ///     </remarks>
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private bool FinalisePosition (Pos oldValue, Pos newValue)
        {
            if (oldValue is PosPercent)
            {
                if (Orientation == Orientation.Horizontal)
                {
                    return Parent.SetSplitterPos (Idx, ConvertToPosPercent (newValue, Parent.Viewport.Height));
                }

                return Parent.SetSplitterPos (Idx, ConvertToPosPercent (newValue, Parent.Viewport.Width));
            }

            return Parent.SetSplitterPos (Idx, newValue);
        }

        private bool MoveSplitter (int distanceX, int distanceY)
        {
            if (Orientation == Orientation.Vertical)
            {
                // Cannot move in this direction
                if (distanceX == 0)
                {
                    return false;
                }

                Pos oldX = X;

                return FinalisePosition (oldX, Offset (X, distanceX));
            }

            // Cannot move in this direction
            if (distanceY == 0)
            {
                return false;
            }

            Pos oldY = Y;

            return FinalisePosition (oldY, Offset (Y, distanceY));
        }

        private Pos Offset (Pos pos, int delta)
        {
            int posAbsolute = pos.GetAnchor (
                                          Orientation == Orientation.Horizontal
                                              ? Parent.Viewport.Height
                                              : Parent.Viewport.Width
                                         );

            return posAbsolute + delta;
        }
    }
}

/// <summary>Represents a method that will handle splitter events.</summary>
public delegate void SplitterEventHandler (object sender, SplitterEventArgs e);
