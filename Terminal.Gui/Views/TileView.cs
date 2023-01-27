using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	/// <summary>
	/// A <see cref="View"/> consisting of a moveable bar that divides
	/// the display area into resizeable <see cref="Tiles"/>.
	/// </summary>
	public class TileView : View {

		TileView parentTileView;

		/// TODO: Might be able to make Border virtual and override here
		/// To make this more API friendly

		/// <summary>
		/// Use this field instead of Border to create an integrated
		/// Border in which lines connect with subviews and splitters
		/// seamlessly
		/// </summary>
		public BorderStyle IntegratedBorder { get; set; }

		public class Tile {
			public View View { get; internal set; }
			public int MinSize { get; set; }
			public string Title { get; set; }

			public Tile ()
			{
				View = new View () { Width = Dim.Fill (), Height = Dim.Fill () };
				Title = string.Empty;
				MinSize = 0;
			}
		}

		List<Tile> tiles;
		private List<Pos> splitterDistances;
		private List<TileViewLineView> splitterLines;

		/// <summary>
		/// The sub sections hosted by the view
		/// </summary>
		public IReadOnlyCollection<Tile> Tiles => tiles.AsReadOnly ();

		/// <summary>
		/// The splitter locations.  Note that there will be N-1 splitters where
		/// N is the number of <see cref="Tiles"/>.
		/// </summary>
		public IReadOnlyCollection<Pos> SplitterDistances => splitterDistances.AsReadOnly ();

		private Orientation orientation = Orientation.Vertical;

		/// <summary>
		/// Creates a new instance of the TileView class.
		/// </summary>
		public TileView () : this (2)
		{
		}

		public TileView (int tiles)
		{
			CanFocus = true;
			RebuildForTileCount (tiles);
		}

		/// <summary>
		/// Invoked when the <see cref="SplitterDistance"/> is changed
		/// </summary>
		public event SplitterEventHandler SplitterMoved;

		/// <summary>
		/// Raises the <see cref="SplitterMoved"/> event
		/// </summary>
		protected virtual void OnSplitterMoved (int idx)
		{
			SplitterMoved?.Invoke (this, new SplitterEventArgs (this, idx, splitterDistances [idx]));
		}

		/// <summary>
		/// Scraps all <see cref="Tiles"/>  and creates <paramref name="count"/> new tiles
		/// in orientation <see cref="Orientation"/>
		/// </summary>
		/// <param name="count"></param>
		public void RebuildForTileCount (int count)
		{
			tiles = new List<Tile> ();
			// TODO: keep these if growing
			splitterDistances = new List<Pos> ();
			splitterLines = new List<TileViewLineView> ();

			RemoveAll ();
			tiles.Clear ();
			splitterDistances.Clear ();

			if (count == 0) {
				return;
			}

			for (int i = 0; i < count; i++) {


				if (i > 0) {
					var currentPos = Pos.Percent ((100 / count) * i);
					splitterDistances.Add (currentPos);
					var line = new TileViewLineView (this, i - 1);
					Add (line);
					splitterLines.Add (line);
				}

				var tile = new Tile ();
				tiles.Add (tile);
				Add (tile.View);
			}

			LayoutSubviews ();
		}

		/// <summary>
		/// Adds a new <see cref="Tile"/> to the collection at <paramref name="idx"/>.
		/// This will also add another splitter line
		/// </summary>
		/// <param name="idx"></param>
		/// <exception cref="NotImplementedException"></exception>
		public Tile InsertTile (int idx)
		{
			var oldTiles = Tiles.ToArray ();
			RebuildForTileCount (oldTiles.Length + 1);

			Tile toReturn = null;

			for(int i=0;i<tiles.Count;i++) {
				
				if(i != idx) {
					var oldTile = oldTiles [i > idx ? i - 1 : i];

					// remove the new empty View
					Remove (tiles [i].View);
					
					// restore old Tile and View
					tiles [i] = oldTile;
					Add (tiles [i].View);
				}
				else
				{
					toReturn = tiles[i];
				}
			}
			SetNeedsDisplay ();
			LayoutSubviews ();

			return toReturn;
		}
		public void RemoveTile (int idx)
		{
			var oldTiles = Tiles.ToArray ();
			
			if (idx < 0 || idx >= oldTiles.Length) {
				return;
			}

			RebuildForTileCount (oldTiles.Length - 1);

			for (int i = 0; i < tiles.Count; i++) {

				int oldIdx = i >= idx ? i + 1: i;
				var oldTile = oldTiles [oldIdx];

				// remove the new empty View
				Remove (tiles [i].View);

				// restore old Tile and View
				tiles [i] = oldTile;
				Add (tiles [i].View);
				
			}
			SetNeedsDisplay ();
			LayoutSubviews ();
		}

		///<summary>
		/// Returns the index of the first <see cref="Tile"/> in
		/// <see cref="Tiles"/> which contains <paramref name="view"/>.
		///</summary>
		public int IndexOf(View view)
		{
			// TODO: Could be recursive (i.e. search nested Subviews)
			return tiles.IndexOf((t)=>t.View == view || t.View.Subviews.Contains(view));
		}

		/// <summary>
		/// Orientation of the dividing line (Horizontal or Vertical).
		/// </summary>
		public Orientation Orientation {
			get { return orientation; }
			set {
				orientation = value;
				LayoutSubviews ();
			}
		}

		public override void LayoutSubviews ()
		{
			var contentArea = Bounds;

			if (HasBorder ()) {
				// TODO: Bound with Max/Min
				contentArea = new Rect (
					contentArea.X + 1,
					contentArea.Y + 1,
					Math.Max (0, contentArea.Width - 2),
					Math.Max (0, contentArea.Height - 2));
			} else if (HasAnyTitles () && IsRootTileView ()) {
				// TODO: Bound with Max/Min
				contentArea = new Rect (
					contentArea.X,
					contentArea.Y + 1,
					contentArea.Width,
					Math.Max (0, contentArea.Height - 1));
			}

			Setup (contentArea);


			base.LayoutSubviews ();
		}

		/// <summary>
		/// <para>Distance Horizontally or Vertically to the splitter line when
		/// neither view is collapsed.
		/// </para>
		/// <para>Only absolute values (e.g. 10) and percent values (i.e. <see cref="Pos.Percent(float)"/>)
		/// are supported for this property.</para>
		/// </summary>
		public void SetSplitterPos (int idx, Pos value)
		{
			if (!(value is Pos.PosAbsolute) && !(value is Pos.PosFactor)) {
				throw new ArgumentException ($"Only Percent and Absolute values are supported.  Passed value was {value.GetType ().Name}");
			}

			splitterDistances [idx] = value;
			GetRootTileView ().LayoutSubviews ();
			OnSplitterMoved (idx);
		}


		/// <inheritdoc/>
		public override bool OnEnter (View view)
		{
			Driver.SetCursorVisibility (CursorVisibility.Invisible);
			return base.OnEnter (view);
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			var childTitles = new List<ChildSplitterLine> ();

			Driver.SetAttribute (ColorScheme.Normal);
			Clear ();
			base.Redraw (bounds);

			var lc = new LineCanvas ();

			var allLines = GetAllChildTileViewLineViewRecursively (this);

			if (IsRootTileView ()) {
				if (HasBorder ()) {

					lc.AddLine (new Point (0, 0), bounds.Width - 1, Orientation.Horizontal, IntegratedBorder);
					lc.AddLine (new Point (0, 0), bounds.Height - 1, Orientation.Vertical, IntegratedBorder);

					lc.AddLine (new Point (bounds.Width - 1, bounds.Height - 1), -bounds.Width + 1, Orientation.Horizontal, IntegratedBorder);
					lc.AddLine (new Point (bounds.Width - 1, bounds.Height - 1), -bounds.Height + 1, Orientation.Vertical, IntegratedBorder);
				}

				foreach (var line in allLines.Where (l => l.Visible)) {
					bool isRoot = splitterLines.Contains (line);

					line.ViewToScreen (0, 0, out var x1, out var y1);
					var origin = ScreenToView (x1, y1);
					var length = line.Orientation == Orientation.Horizontal ?
							line.Frame.Width - 1 :
							line.Frame.Height - 1;

					if (!isRoot) {
						if (line.Orientation == Orientation.Horizontal) {
							origin.X -= 1;
						} else {
							origin.Y -= 1;
						}
						length += 2;

						childTitles.Add (
							new ChildSplitterLine (line));

					}

					lc.AddLine (origin, length, line.Orientation, IntegratedBorder);
				}
			}

			Driver.SetAttribute (ColorScheme.Normal);
			lc.Draw (this, bounds);

			// Redraw the lines so that focus/drag symbol renders
			foreach (var line in allLines) {
				line.DrawSplitterSymbol ();
			}

			foreach (var child in childTitles) {
				child.DrawTitles ();
			}

			// Draw Titles over Border


			for (int i = 0; i < tiles.Count; i++) {

				var tile = tiles [i];

				if (tile.View.Visible && tile.Title.Length > 0) {

					var screen = i == 0 ?
						ViewToScreen (new Rect (0, 0, bounds.Width, 1)) :
						ViewToScreen (splitterLines [i - 1].Frame);


					Driver.SetAttribute (tile.View.HasFocus ? ColorScheme.HotNormal : ColorScheme.Normal);
					Driver.DrawWindowTitle (new Rect (screen.X, screen.Y, tile.View.Frame.Width, 0), tile.Title, 0, 0, 0, 0);
				}
			}
		}
		
		/// <summary>
		/// Converts <see cref="View1"/> from a regular <see cref="View"/>
		/// container to a new nested <see cref="TileView"/>.  If <see cref="View1"/>
		/// is already a <see cref="TileView"/> then returns false.
		/// </summary>
		/// <remarks>After successful splitting, the returned container's <see cref="View1"/> 
		/// will contain the original content and <see cref="View1Title"/> (if any) while
		/// <see cref="View2"/> will be empty and available for adding to.
		/// for adding to.</remarks>
		/// <param name="result">The new <see cref="TileView"/> now showing in 
		/// <see cref="View1"/> or the existing one if it was already been converted before.</param>
		/// <returns><see langword="true"/> if a <see cref="View"/> was converted to a new nested
		/// <see cref="TileView"/>.  <see langword="false"/> if it was already a nested
		/// <see cref="TileView"/></returns>
		public bool TrySplitTile(int idx, int panels, out TileView result)
		{
			// when splitting a view into 2 sub views we will need to migrate
			// the title too
			var tile = tiles [idx];
			var title = tile.Title;
			View toMove = tile.View;

			if (toMove is TileView existing) {
				result = existing;
				return false;
			}

			var newContainer = new TileView(panels) {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				parentTileView = this,
			};
			
			// Take everything out of the View we are moving
			var childViews = toMove.Subviews.ToArray();
			toMove.RemoveAll ();

			// Remove the view itself and replace it with the new TileView
			Remove (toMove);
			Add (newContainer);

			tile.View = newContainer;

			var newTileView1 = newContainer.tiles [0].View;
			// Add the original content into the first view of the new container
			foreach (var childView in childViews) {
				newTileView1.Add (childView);
			}

			result = newContainer;
			return true;
		}


		private List<TileViewLineView> GetAllChildTileViewLineViewRecursively (View v)
		{
			var lines = new List<TileViewLineView> ();

			foreach (var sub in v.Subviews) {
				if (sub is TileViewLineView s) {
					if (s.Parent.GetRootTileView () == this) {
						lines.Add (s);
					}
				} else {
					lines.AddRange (GetAllChildTileViewLineViewRecursively (sub));
				}
			}

			return lines;
		}

		/// <summary>
		/// <para>
		/// <see langword="true"/> if <see cref="TileView"/> is nested within a parent <see cref="TileView"/>
		/// e.g. via the <see cref="TrySplitTile"/>.  <see langword="false"/> if it is a root level <see cref="TileView"/>.
		/// </para>
		/// </summary>
		/// <remarks>Note that manually adding one <see cref="TileView"/> to another will not result in a parent/child
		/// relationship and both will still be considered 'root' containers.  Always use
		/// <see cref="TrySplitTile(int, int, out TileView)"/> if you want to subdivide a <see cref="TileView"/>.</remarks>
		/// <returns></returns>
		public bool IsRootTileView ()
		{
			// TODO: don't want to layout subviews since the parent recursively lays them all out
			return parentTileView == null;
		}

		public TileView GetParentTileView ()
		{
			return this.parentTileView;
		}
		private TileView GetRootTileView ()
		{
			TileView root = this;

			while (root.parentTileView != null) {
				root = root.parentTileView;
			}

			return root;
		}
		private void Setup (Rect bounds)
		{
			if (bounds.IsEmpty) {
				return;
			}

			RespectMinimumTileSizes ();

			for (int i = 0; i < splitterLines.Count; i++) {
				var line = splitterLines[i];

				line.Orientation = Orientation;
				line.Width = orientation == Orientation.Vertical
					? 1 : Dim.Fill ();
				line.Height = orientation == Orientation.Vertical
					? Dim.Fill () : 1;
				line.LineRune = orientation == Orientation.Vertical ?
					Driver.VLine : Driver.HLine;

				if (orientation == Orientation.Vertical) {
					line.X = splitterDistances [i];
					line.Y = 0;
				}
				else {
					line.Y = splitterDistances [i];
					line.X = 0;
				}

			}

			for (int i = 0; i < tiles.Count; i++) {
				var tile = tiles [i];

				// TODO: Deal with lines being Visibility false

				if (Orientation == Orientation.Vertical) {
					tile.View.X = i == 0 ? bounds.X : Pos.Right (splitterLines [i - 1]);
					tile.View.Y = bounds.Y;
					tile.View.Height = bounds.Height;					
					tile.View.Width = GetTileWidthOrHeight(i, Bounds.Width);
				} else {
					tile.View.X = bounds.X;
					tile.View.Y = i == 0 ? 0 : Pos.Bottom (splitterLines [i - 1]);
					tile.View.Width = bounds.Width;
					tile.View.Height = GetTileWidthOrHeight(i, Bounds.Height);
				}
			}
		}

		private Dim GetTileWidthOrHeight (int i, int space)
		{
			// last tile
			if(i + 1 >= tiles.Count)
			{
				return Dim.Fill (HasBorder () ? 1 : 0);
			}
			var nextSplitter = splitterDistances [i].Anchor (space);
			var lastSplitter = i >= 1 ? splitterDistances [i-1].Anchor (space) : 0;

			var distance = nextSplitter - lastSplitter;

			if(i>0) {
				return distance - 1;
			}

			return distance - (HasBorder() ? 1 : 0);
		}

		private void RespectMinimumTileSizes ()
		{
			// if we are not yet initialized then we don't know
			// how big we are and therefore cannot sensibly calculate
			// how big the views will be with a given SplitterDistance
			if (!IsInitialized) {
				return;
			}

			// how much space is there?
			var availableSpace = Orientation == Orientation.Horizontal 
				? this.Bounds.Height 
				: this.Bounds.Width;
			
			var fullSpace = availableSpace;

			var lastSplitterLocation = 0;

			for(int i=0;i< splitterDistances.Count; i++) {
				var splitterLocation = splitterDistances [i].Anchor(fullSpace);

				var availableLeft = splitterLocation - lastSplitterLocation;
				// Border steals space
				availableLeft -= HasBorder () && i == 0 ? 1 : 0;

				var availableRight = fullSpace - splitterLocation;
				// Border steals space
				availableRight -= HasBorder () && i == 0 ? 1 : 0;
				// Splitter line steals space
				availableRight--;

				// TODO: Test 3+ panel max/mins because this calculation is probably wrong

				var requiredLeft = tiles [i].MinSize;
				var requiredRight = tiles [i+1].MinSize;

				if (availableLeft < requiredLeft) {

					// There is not enough space for panel on left
					var insteadTake = requiredLeft + (HasBorder() ? 1 :0);

					// Don't take more than the available space in view
					insteadTake = Math.Max(0,Math.Min (fullSpace, insteadTake));
					splitterDistances [i] = insteadTake;
					splitterLocation = insteadTake;
				}
				else if (availableRight < requiredRight) {
					// There is not enough space for panel on right
					var insteadTake = fullSpace - (requiredRight + (HasBorder()?1:0));

					// leave 1 space for the splitter
					insteadTake --;

					insteadTake = Math.Max (0, Math.Min (fullSpace, insteadTake));
					splitterDistances [i] = insteadTake;
					splitterLocation = insteadTake;
				}

				availableSpace -= splitterLocation;
				lastSplitterLocation = splitterLocation;
			}
		}

		private class TileViewLineView : LineView {
			public TileView Parent { get; private set; }
			public int Idx { get; }

			Point? dragPosition;
			Pos dragOrignalPos;
			public Point? moveRuneRenderLocation;

			public TileViewLineView (TileView parent, int idx)
			{
				CanFocus = true;
				TabStop = true;

				this.Parent = parent;
				Idx = idx;
				base.AddCommand (Command.Right, () => {
					return MoveSplitter (1, 0);
				});

				base.AddCommand (Command.Left, () => {
					return MoveSplitter (-1, 0);
				});

				base.AddCommand (Command.LineUp, () => {
					return MoveSplitter (0, -1);
				});

				base.AddCommand (Command.LineDown, () => {
					return MoveSplitter (0, 1);
				});

				AddKeyBinding (Key.CursorRight, Command.Right);
				AddKeyBinding (Key.CursorLeft, Command.Left);
				AddKeyBinding (Key.CursorUp, Command.LineUp);
				AddKeyBinding (Key.CursorDown, Command.LineDown);
			}

			public override bool ProcessKey (KeyEvent kb)
			{
				if (!CanFocus || !HasFocus) {
					return base.ProcessKey (kb);
				}

				var result = InvokeKeybindings (kb);
				if (result != null)
					return (bool)result;

				return base.ProcessKey (kb);
			}

			public override void PositionCursor ()
			{
				base.PositionCursor ();
				var location = moveRuneRenderLocation ??
					new Point (Bounds.Width / 2, Bounds.Height / 2);
				Move (location.X, location.Y);
			}

			public override bool OnEnter (View view)
			{
				Driver.SetCursorVisibility (CursorVisibility.Default);
				PositionCursor ();

				return base.OnEnter (view);
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				DrawSplitterSymbol ();
			}

			public void DrawSplitterSymbol ()
			{
				if (CanFocus && HasFocus) {
					var location = moveRuneRenderLocation ??
						new Point (Bounds.Width / 2, Bounds.Height / 2);

					AddRune (location.X, location.Y, Driver.Diamond);
				}
			}

			public override bool MouseEvent (MouseEvent mouseEvent)
			{
				if (!CanFocus) {
					return true;
				}

				if (!dragPosition.HasValue && (mouseEvent.Flags == MouseFlags.Button1Pressed)) {

					// Start a Drag
					SetFocus ();
					Application.EnsuresTopOnFront ();

					if (mouseEvent.Flags == MouseFlags.Button1Pressed) {
						dragPosition = new Point (mouseEvent.X, mouseEvent.Y);
						dragOrignalPos = Orientation == Orientation.Horizontal ? Y : X;
						Application.GrabMouse (this);

						if (Orientation == Orientation.Horizontal) {

						} else {
							moveRuneRenderLocation = new Point (0, Math.Max (1, Math.Min (Bounds.Height - 2, mouseEvent.Y)));
						}
					}

					return true;
				} else if (
					dragPosition.HasValue &&
					(mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))) {

					// Continue Drag

					// how far has user dragged from original location?						
					if (Orientation == Orientation.Horizontal) {
						int dy = mouseEvent.Y - dragPosition.Value.Y;
						Parent.splitterDistances [Idx] = Offset (Y, dy);
						moveRuneRenderLocation = new Point (mouseEvent.X, 0);
					} else {
						int dx = mouseEvent.X - dragPosition.Value.X;
						Parent.splitterDistances [Idx] = Offset (X, dx);
						moveRuneRenderLocation = new Point (0, Math.Max (1, Math.Min (Bounds.Height - 2, mouseEvent.Y)));
					}

					Parent.SetNeedsDisplay ();
					return true;
				}

				if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && dragPosition.HasValue) {

					// End Drag

					Application.UngrabMouse ();
					Driver.UncookMouse ();
					FinalisePosition (
						dragOrignalPos,
						Orientation == Orientation.Horizontal ? Y : X);
					dragPosition = null;
					moveRuneRenderLocation = null;
				}

				return false;
			}

			private bool MoveSplitter (int distanceX, int distanceY)
			{
				if (Orientation == Orientation.Vertical) {

					// Cannot move in this direction
					if (distanceX == 0) {
						return false;
					}

					var oldX = X;
					FinalisePosition (oldX, (Pos)Offset (X, distanceX));
					return true;
				} else {

					// Cannot move in this direction
					if (distanceY == 0) {
						return false;
					}

					var oldY = Y;
					FinalisePosition (oldY, (Pos)Offset (Y, distanceY));
					return true;
				}
			}

			private Pos Offset (Pos pos, int delta)
			{
				var posAbsolute = pos.Anchor (Orientation == Orientation.Horizontal ?
					Parent.Bounds.Height : Parent.Bounds.Width);

				return posAbsolute + delta;
			}

			/// <summary>
			/// <para>
			/// Moves <see cref="Parent"/> <see cref="TileView.SplitterDistance"/> to 
			/// <see cref="Pos"/> <paramref name="newValue"/> preserving <see cref="Pos"/> format
			/// (absolute / relative) that <paramref name="oldValue"/> had.
			/// </para>
			/// <remarks>This ensures that if splitter location was e.g. 50% before and you move it
			/// to absolute 5 then you end up with 10% (assuming a parent had 50 width). </remarks>
			/// </summary>
			/// <param name="oldValue"></param>
			/// <param name="newValue"></param>
			private void FinalisePosition (Pos oldValue, Pos newValue)
			{
				if (oldValue is Pos.PosFactor) {
					if (Orientation == Orientation.Horizontal) {
						Parent.SetSplitterPos(Idx, ConvertToPosFactor (newValue, Parent.Bounds.Height));
					} else {
						Parent.SetSplitterPos (Idx, ConvertToPosFactor (newValue, Parent.Bounds.Width));
					}
				} else {
					Parent.SetSplitterPos (Idx, newValue);
				}
			}

			/// <summary>
			/// <para>
			/// Determines the absolute position of <paramref name="p"/> and
			/// returns a <see cref="Pos.PosFactor"/> that describes the percentage of that.
			/// </para>
			/// <para>Effectively turning any <see cref="Pos"/> into a <see cref="Pos.PosFactor"/>
			/// (as if created with <see cref="Pos.Percent(float)"/>)</para>
			/// </summary>
			/// <param name="p">The <see cref="Pos"/> to convert to <see cref="Pos.Percent(float)"/></param>
			/// <param name="parentLength">The Height/Width that <paramref name="p"/> lies within</param>
			/// <returns></returns>
			private Pos ConvertToPosFactor (Pos p, int parentLength)
			{
				// calculate position in the 'middle' of the cell at p distance along parentLength
				float position = p.Anchor (parentLength) + 0.5f;

				return new Pos.PosFactor (position / parentLength);
			}
		}

		private bool HasBorder ()
		{
			return IntegratedBorder != BorderStyle.None;
		}
		private bool HasAnyTitles ()
		{
			return tiles.Any (t => t.Title.Length > 0);

		}


		private class ChildSplitterLine {

			readonly TileViewLineView currentLine;
			internal ChildSplitterLine (TileViewLineView currentLine)
			{
				this.currentLine = currentLine;
			}

			internal void DrawTitles ()
			{
				//TODO: Implement this
				/*if(currentLine.Orientation == Orientation.Horizontal) 
				{
					var screenRect = currentLine.ViewToScreen (
						new Rect(0,0,currentLine.Frame.Width,currentLine.Frame.Height));
					Driver.DrawWindowTitle (screenRect, currentLine.Parent.View2Title, 0, 0, 0, 0);
				}*/
			}
		}
	}

	/// <summary>
	///  Provides data for <see cref="TileView"/> events.
	/// </summary>
	public class SplitterEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="SplitterEventArgs"/> class.
		/// </summary>
		/// <param name="tileView"></param>
		/// <param name="splitterDistance"></param>
		public SplitterEventArgs (TileView tileView, int idx, Pos splitterDistance)
		{
			SplitterDistance = splitterDistance;
			TileView = tileView;
			Idx = idx;
		}

		/// <summary>
		/// New position of the <see cref="TileView.SplitterDistance"/>
		/// </summary>
		public Pos SplitterDistance { get; }

		/// <summary>
		/// Container (sender) of the event.
		/// </summary>
		public TileView TileView { get; }

		/// <summary>
		/// The splitter that is being moved (use when <see cref="TileView"/>
		/// has more than 2 panels).
		/// </summary>
		public int Idx { get; }
	}

	/// <summary>
	///  Represents a method that will handle splitter events.
	/// </summary>
	public delegate void SplitterEventHandler (object sender, SplitterEventArgs e);
}
