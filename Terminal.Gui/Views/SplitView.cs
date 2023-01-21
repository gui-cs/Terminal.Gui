using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	/// <summary>
	/// A <see cref="View"/> consisting of a moveable bar that divides
	/// the display area into resizeable views.
	/// </summary>
	public class SplitView : View {

		SplitView parentSplitView;

		/// TODO: Might be able to make Border virtual and override here
		/// To make this more API friendly

		/// <summary>
		/// Use this field instead of Border to create an integrated
		/// Border in which lines connect with subviews and splitters
		/// seamlessly
		/// </summary>
		public BorderStyle IntegratedBorder { get; set; }

		public class Tile {
			public View View { get; }
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
		private List<SplitContainerLineView> splitterLines;

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
		/// Creates a new instance of the SplitContainer class.
		/// </summary>
		public SplitView () : this (2)
		{
		}

		public SplitView (int tiles)
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
			splitterLines = new List<SplitContainerLineView> ();

			RemoveAll ();
			tiles.Clear ();
			splitterDistances.Clear ();

			if (count == 0) {
				return;
			}

			for (int i = 0; i < count; i++) {
				var tile = new Tile ();
				tiles.Add (tile);
				Add (tile.View);

				if (i > 0) {
					var currentPos = Pos.Percent ((100 / count) * i);
					splitterDistances.Add (currentPos);
					var line = new SplitContainerLineView (this, i-1);
					Add (line);
					splitterLines.Add (line);
				}
			}

			LayoutSubviews ();
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
			} else if (HasAnyTitles () && IsRootSplitContainer ()) {
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
			GetRootSplitContainer ().LayoutSubviews ();
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

			var allLines = GetAllChildSplitContainerLineViewRecursively (this);

			if (IsRootSplitContainer ()) {
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
		/*
		/// <summary>
		/// Converts <see cref="View1"/> from a regular <see cref="View"/>
		/// container to a new nested <see cref="SplitView"/>.  If <see cref="View1"/>
		/// is already a <see cref="SplitView"/> then returns false.
		/// </summary>
		/// <remarks>After successful splitting, the returned container's <see cref="View1"/> 
		/// will contain the original content and <see cref="View1Title"/> (if any) while
		/// <see cref="View2"/> will be empty and available for adding to.
		/// for adding to.</remarks>
		/// <param name="result">The new <see cref="SplitView"/> now showing in 
		/// <see cref="View1"/> or the existing one if it was already been converted before.</param>
		/// <returns><see langword="true"/> if a <see cref="View"/> was converted to a new nested
		/// <see cref="SplitView"/>.  <see langword="false"/> if it was already a nested
		/// <see cref="SplitView"/></returns>
		public bool TrySplitView1(out SplitView result)
		{
			// when splitting a view into 2 sub views we will need to migrate
			// the title too
			var title = View1Title;

			bool returnValue = TrySplit (
				this.View1,
				(newSplitContainer) => {
					this.View1 = newSplitContainer;
					
					// Move title to new container
					View1Title = string.Empty;
					newSplitContainer.View1Title = title;
				},
				out result);
			
			return returnValue;
		}

		/// <summary>
		/// Converts <see cref="View2"/> from a regular <see cref="View"/>
		/// container to a new nested <see cref="SplitView"/>.  If <see cref="View2"/>
		/// is already a <see cref="SplitView"/> then returns false.
		/// </summary>
		/// <remarks>After successful splitting, the returned container's <see cref="View1"/> 
		/// will contain the original content and <see cref="View2Title"/> (if any) while
		/// <see cref="View2"/> will be empty and available for adding to.
		/// for adding to.</remarks>
		/// <param name="result">The new <see cref="SplitView"/> now showing in 
		/// <see cref="View2"/> or the existing one if it was already been converted before.</param>
		/// <returns><see langword="true"/> if a <see cref="View"/> was converted to a new nested
		/// <see cref="SplitView"/>.  <see langword="false"/> if it was already a nested
		/// <see cref="SplitView"/></returns>
		public bool TrySplitView2 (out SplitView result)
		{
			// when splitting a view into 2 sub views we will need to migrate
			// the title too
			var title = View2Title;

			bool returnValue = TrySplit (
				this.View2,
				(newSplitContainer) => {
					this.View2 = newSplitContainer;

					// Move title to new container
					View2Title = string.Empty;

					// Content always goes into View1 of the new container
					// so that is where the title goes too
					newSplitContainer.View1Title = title;
				},
				out result);

			return returnValue;
		}
		private bool TrySplit(
			View toMove,
			Action<SplitView> newSplitContainerSetter,
			out SplitView result)
		{
			if (toMove is SplitView existing) {
				result = existing;
				return false;
			}

			var newContainer = new SplitView {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				parentSplitView = this,
			};
			
			// Take everything out of the View we are moving
			var childViews = toMove.Subviews.ToArray();
			toMove.RemoveAll ();

			// Remove the view itself and replace it with the new SplitContainer
			Remove (toMove);
			Add (newContainer);
			newSplitContainerSetter(newContainer);

			// Add the original content into the first view of the new container
			foreach(var childView in childViews) {
				newContainer.View1.Add (childView);
			}

			result = newContainer;
			return true;
		}*/


		private List<SplitContainerLineView> GetAllChildSplitContainerLineViewRecursively (View v)
		{
			var lines = new List<SplitContainerLineView> ();

			foreach (var sub in v.Subviews) {
				if (sub is SplitContainerLineView s) {
					if (s.Parent.GetRootSplitContainer () == this) {
						lines.Add (s);
					}
				} else {
					lines.AddRange (GetAllChildSplitContainerLineViewRecursively (sub));
				}
			}

			return lines;
		}

		private bool IsRootSplitContainer ()
		{
			// TODO: don't want to layout subviews since the parent recursively lays them all out
			return parentSplitView == null;
		}
		private SplitView GetRootSplitContainer ()
		{
			SplitView root = this;

			while (root.parentSplitView != null) {
				root = root.parentSplitView;
			}

			return root;
		}
		private void Setup (Rect bounds)
		{
			if (bounds.IsEmpty) {
				return;
			}

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
				}
				else {
					line.Y = splitterDistances [i];
				}

			}

			RespectMinimumTileSizes ();

			for (int i = 0; i < tiles.Count; i++) {
				var tile = tiles [i];

				// TODO: Deal with lines being Visibility false

				if (Orientation == Orientation.Vertical) {
					tile.View.X = i == 0 ? 0 : Pos.Right (splitterLines [i - 1]);
					tile.View.Y = bounds.Y;
					tile.View.Height = bounds.Height;

					// Take a copy so it is const for the lamda
					int i2 = i;

					// if it is not the last tile then fill horizontally right to next line
					tile.View.Width = i + 1 < tiles.Count
						? new Dim.DimFunc (
							() => splitterDistances [i2].Anchor (bounds.Width))
									: Dim.Fill (HasBorder () ? 1 : 0);
				} else {
					tile.View.X = bounds.X;
					tile.View.Y = i == 0 ? 0 : Pos.Bottom (splitterLines [i - 1]);
					tile.View.Width = bounds.Width;

					// Take a copy so it is const for the lamda
					int i2 = i;

					// if it is not the last tile then fill vertically down to next line
					tile.View.Height = i + 1 < tiles.Count
						? new Dim.DimFunc (
							() => splitterDistances [i2].Anchor (bounds.Height))
										: Dim.Fill (HasBorder () ? 1 : 0);
				}
			}
		}

		private void RespectMinimumTileSizes ()
		{
			// TODO: implement this
			/* 
			// if we are not yet initialized then we don't know
			// how big we are and therefore cannot sensibly calculate
			// how big the views will be with a given SplitterDistance
			if (!IsInitialized) {
				return pos;
			}

			var view1MinSize = View1MinSize;
			var view2MinSize = View2MinSize;

			// how much space is there?
			var availableSpace = Orientation == Orientation.Horizontal 
				? this.Bounds.Height 
				: this.Bounds.Width;

			// if there is a border then there is less space
			// for the views so we need to make size restrictions
			// tighter.
			if (HasBorder ()) {
				view1MinSize++;
				view2MinSize++;
			}



			// we probably haven't finished layout even if IsInitialized is true :(
			if (availableSpace <= 0) {
				return pos;
			}

			var idealPosition = pos.Anchor (availableSpace);

			// bad position because not enough space for View1
			if (idealPosition < view1MinSize) {

				// TODO: we should preserve Absolute/Percent status here not just force it to absolute
				return (Pos)Math.Min (view1MinSize, availableSpace);
			}

			// bad position because not enough space for View2
			if (availableSpace - idealPosition <= view2MinSize) {

				// TODO: we should preserve Absolute/Percent status here not just force it to absolute

				// +1 is to allow space for the splitter
				return (Pos)Math.Max (availableSpace - (view2MinSize + 1), 0);
			}

			// this splitter position is fine, there is enough space for everyone
			return pos;*/
		}

		private class SplitContainerLineView : LineView {
			public SplitView Parent { get; private set; }
			public int Idx { get; }

			Point? dragPosition;
			Pos dragOrignalPos;
			public Point? moveRuneRenderLocation;

			public SplitContainerLineView (SplitView parent, int idx)
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
			/// Moves <see cref="Parent"/> <see cref="SplitView.SplitterDistance"/> to 
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
						Parent.splitterDistances [Idx] = ConvertToPosFactor (newValue, Parent.Bounds.Height);
					} else {
						Parent.splitterDistances [Idx] = ConvertToPosFactor (newValue, Parent.Bounds.Width);
					}
				} else {
					Parent.splitterDistances [Idx] = newValue;
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

			readonly SplitContainerLineView currentLine;
			internal ChildSplitterLine (SplitContainerLineView currentLine)
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
	///  Provides data for <see cref="SplitContainer"/> events.
	/// </summary>
	public class SplitterEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="SplitterEventArgs"/> class.
		/// </summary>
		/// <param name="splitContainer"></param>
		/// <param name="splitterDistance"></param>
		public SplitterEventArgs (SplitView splitContainer, int idx, Pos splitterDistance)
		{
			SplitterDistance = splitterDistance;
			SplitContainer = splitContainer;
			Idx = idx;
		}

		/// <summary>
		/// New position of the <see cref="SplitView.SplitterDistance"/>
		/// </summary>
		public Pos SplitterDistance { get; }

		/// <summary>
		/// Container (sender) of the event.
		/// </summary>
		public SplitView SplitContainer { get; }

		/// <summary>
		/// The splitter that is being moved (use when <see cref="SplitContainer"/>
		/// has more than 2 panels).
		/// </summary>
		public int Idx { get; }
	}

	/// <summary>
	///  Represents a method that will handle splitter events.
	/// </summary>
	public delegate void SplitterEventHandler (object sender, SplitterEventArgs e);
}
