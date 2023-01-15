using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	/// <summary>
	/// A <see cref="View"/> consisting of a moveable bar that divides
	/// the display area into 2 resizeable panels.
	/// </summary>
	public class SplitContainer : View {

		private SplitContainerLineView splitterLine;
		SplitContainer parentSplitPanel;
		
		/// TODO: Might be able to make Border virtual and override here
		/// To make this more API friendly

		/// <summary>
		/// Use this field instead of Border to create an integrated
		/// Border in which lines connect with subpanels and splitters
		/// seamlessly
		/// </summary>
		public BorderStyle IntegratedBorder {get;set;}

		/// <summary>
		/// The <see cref="View"/> showing in the left hand pane of a
		/// <see cref="Orientation.Vertical"/>  or top of an
		/// <see cref="Orientation.Horizontal"/> pane.  May be another
		/// <see cref="SplitContainer"/> if further splitter subdivisions are
		/// desired (e.g. to create a resizeable grid.
		/// </summary>
		public View Panel1 { get; set; } // TODO: Should not be public set, should be helpers for this


		public int Panel1MinSize { get; set; } = 1;
		public ustring Panel1Title { get; set; } = string.Empty;

		/// <summary>
		/// The <see cref="View"/> showing in the right hand pane of a
		/// <see cref="Orientation.Vertical"/>  or bottom of an
		/// <see cref="Orientation.Horizontal"/> pane.  May be another
		/// <see cref="SplitContainer"/> if further splitter subdivisions are
		/// desired (e.g. to create a resizeable grid.
		/// </summary>
		public View Panel2 { get; set; } // TODO: Should not be public set, should be helpers for this

		public int Panel2MinSize { get; set; } = 1;
		public ustring Panel2Title { get; set; } = string.Empty;

		private Pos splitterDistance = Pos.Percent (50);
		private Orientation orientation = Orientation.Vertical;

		/// <summary>
		/// Creates a new instance of the SplitContainer class.
		/// </summary>
		public SplitContainer ()
		{
			splitterLine = new SplitContainerLineView (this);
			Panel1 = new View () { Width = Dim.Fill (), Height = Dim.Fill() };
			Panel2 = new View () { Width = Dim.Fill (), Height = Dim.Fill () };

			this.Add (Panel1);
			this.Add (splitterLine);
			this.Add (Panel2);

			CanFocus = true;
		}

		/// <summary>
		/// Invoked when the <see cref="SplitterDistance"/> is changed
		/// </summary>
		public event SplitterEventHandler SplitterMoved;

		/// <summary>
		/// Raises the <see cref="SplitterMoved"/> event
		/// </summary>
		protected virtual void OnSplitterMoved ()
		{
			SplitterMoved?.Invoke (this, new SplitterEventArgs (this, splitterDistance));
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
			if(this.IsRootSplitContainer()) {

				var contentArea = Bounds;

				if(HasBorder())
				{
					// TODO: Bound with Max/Min
					contentArea = new Rect(
						contentArea.X + 1,
						contentArea.Y + 1,
						Math.Max (0, contentArea.Width - 2),
						Math.Max (0, contentArea.Height - 2));
				}
				else if(HasAnyTitles() && IsRootSplitContainer())
				{
					// TODO: Bound with Max/Min
					contentArea = new Rect(
						contentArea.X,
						contentArea.Y + 1,
						contentArea.Width,
						Math.Max(0,contentArea.Height - 1));
				}

				Setup (contentArea);
			}			

			base.LayoutSubviews ();
		}

		/// <summary>
		/// <para>Distance Horizontally or Vertically to the splitter line when
		/// neither panel is collapsed.
		/// </para>
		/// <para>Only absolute values (e.g. 10) and percent values (i.e. <see cref="Pos.Percent(float)"/>)
		/// are supported for this property.</para>
		/// </summary>
		public Pos SplitterDistance {
			get { return splitterDistance; }
			set {
				if (!(value is Pos.PosAbsolute) && !(value is Pos.PosFactor)) {
					throw new ArgumentException ($"Only Percent and Absolute values are supported for {nameof (SplitterDistance)} property.  Passed value was {value.GetType ().Name}");
				}

				splitterDistance = value;
				GetRootSplitContainer ().LayoutSubviews ();
				OnSplitterMoved ();
			}
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

			var lc = new LineCanvas(Application.Driver);

			var allLines = GetAllChildSplitContainerLineViewRecursively (this);

			if (IsRootSplitContainer())
			{
				if(HasBorder ()) {

					lc.AddLine (new Point (0, 0), bounds.Width - 1, Orientation.Horizontal, IntegratedBorder);
					lc.AddLine (new Point (0, 0), bounds.Height - 1, Orientation.Vertical, IntegratedBorder);

					lc.AddLine (new Point (bounds.Width - 1, bounds.Height - 1), -bounds.Width + 1, Orientation.Horizontal, IntegratedBorder);
					lc.AddLine (new Point (bounds.Width - 1, bounds.Height - 1), -bounds.Height + 1, Orientation.Vertical, IntegratedBorder);
				}

				foreach (var line in allLines.Where(l=>l.Visible))
				{
					bool isRoot = line == splitterLine;

					line.ViewToScreen(0,0,out var x1,out var y1);
					var origin = ScreenToView(x1,y1);
					var length = line.Orientation == Orientation.Horizontal ?
							line.Frame.Width - 1 :
							line.Frame.Height - 1;

					if(!isRoot) {
						if(line.Orientation == Orientation.Horizontal) {
							origin.X -= 1;
						} else {
							origin.Y -= 1;
						}
						length += 2;

						childTitles.Add (
							new ChildSplitterLine(line));
						
					}

					lc.AddLine(origin,length,line.Orientation,IntegratedBorder);
				}
			}

			Driver.SetAttribute (ColorScheme.Normal);
			lc.Draw(this,bounds);

			// Redraw the lines so that focus/drag symbol renders
			foreach(var line in allLines) {
				line.DrawSplitterSymbol ();
			}

			foreach(var child in childTitles) {
				child.DrawTitles ();
			}

			// Draw Titles over Border
			var screen = ViewToScreen (new Rect(0,0,bounds.Width,1));
			if (Panel1.Visible && Panel1Title.Length > 0) {
				Driver.SetAttribute (Panel1.HasFocus ? ColorScheme.HotNormal : ColorScheme.Normal);
				Driver.DrawWindowTitle (new Rect (screen.X, screen.Y, Panel1.Frame.Width, 0), Panel1Title, 0, 0, 0, 0);
			}

			if (splitterLine.Visible) {
				screen = ViewToScreen (splitterLine.Frame);
			} else {
				
				screen.X--;
				//screen.Y--;
			}

			if (Orientation == Orientation.Horizontal) {
				if (Panel2.Visible && Panel2Title?.Length > 0) {

					Driver.SetAttribute (Panel2.HasFocus ? ColorScheme.HotNormal : ColorScheme.Normal);
					Driver.DrawWindowTitle (new Rect (screen.X, screen.Y, Panel2.Bounds.Width, 1), Panel2Title, 0, 0, 0, 0);
				}
			} else {
				if (Panel2.Visible && Panel2Title?.Length > 0) {
					Driver.SetAttribute (Panel2.HasFocus ? ColorScheme.HotNormal : ColorScheme.Normal);
					Driver.DrawWindowTitle (new Rect (screen.X, screen.Y, Panel2.Bounds.Width, 1), Panel2Title, 0, 0, 0, 0);
				}
			}
		}

		/// <summary>
		/// Converts <see cref="Panel1"/> from a regular <see cref="View"/>
		/// container to a new nested <see cref="SplitContainer"/>.  If <see cref="Panel1"/>
		/// is already a <see cref="SplitContainer"/> then returns false.
		/// </summary>
		/// <remarks>After successful splitting, the returned container's <see cref="Panel1"/> 
		/// will contain the original content (if any) while <see cref="Panel2"/> will be empty and available
		/// for adding to.</remarks>
		/// <param name="result">The new <see cref="SplitContainer"/> now showing in 
		/// <see cref="Panel1"/> or the existing one if it was already been converted before.</param>
		/// <returns><see langword="true"/> if a <see cref="View"/> was converted to a new nested
		/// <see cref="SplitContainer"/>.  <see langword="false"/> if it was already a nested
		/// <see cref="SplitContainer"/></returns>
		public bool TrySplitPanel1(out SplitContainer result)
		{
			return TrySplit (
				() => this.Panel1,
				(n) => this.Panel1 = n,
				out result);
		}

		/// <summary>
		/// Converts <see cref="Panel2"/> from a regular <see cref="View"/>
		/// container to a new nested <see cref="SplitContainer"/>.  If <see cref="Panel2"/>
		/// is already a <see cref="SplitContainer"/> then returns false.
		/// </summary>
		/// <remarks>After successful splitting, the returned container's <see cref="Panel1"/> 
		/// will contain the original content (if any) while <see cref="Panel2"/> will be empty and available
		/// for adding to.</remarks>
		/// <param name="result">The new <see cref="SplitContainer"/> now showing in 
		/// <see cref="Panel2"/> or the existing one if it was already been converted before.</param>
		/// <returns><see langword="true"/> if a <see cref="View"/> was converted to a new nested
		/// <see cref="SplitContainer"/>.  <see langword="false"/> if it was already a nested
		/// <see cref="SplitContainer"/></returns>
		public bool TrySplitPanel2 (out SplitContainer result)
		{
			return TrySplit (
				() => this.Panel2,
				(n) => this.Panel2 = n,
				out result);
		}
		private bool TrySplit(
			Func<View> getter,
			Action<SplitContainer> newSplitContainerSetter,
			out SplitContainer result)
		{
			// Get the current panel contents (Panel1 or Panel2)
			var toMove = getter();

			if (toMove is SplitContainer existing) {
				result = existing;
				return false;
			}

			var newContainer = new SplitContainer {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			// Replace current child contents 
			Remove (toMove);
			Add (newContainer);

			// Set Panel (1 or 2) to the new container
			newSplitContainerSetter(newContainer);

			// Set the original content into the first panel of the new container
			newContainer.Add (toMove);
			newContainer.Panel1 = toMove;

			result = newContainer;
			return true;
		}

		
		private List<SplitContainerLineView> GetAllChildSplitContainerLineViewRecursively (View v)
		{
			var lines = new List<SplitContainerLineView>();

			foreach(var sub in v.Subviews)
			{
				if(sub is SplitContainerLineView s)
				{
					lines.Add(s);
				}
				else {
					lines.AddRange(GetAllChildSplitContainerLineViewRecursively(sub));
				}
			}

			return lines;
		}

		private bool IsRootSplitContainer ()
		{
			// TODO: don't want to layout subviews since the parent recursively lays them all out
			return parentSplitPanel == null;
		}
		private SplitContainer GetRootSplitContainer ()
		{
			SplitContainer root = this;

			while (root.parentSplitPanel != null) {
				root = root.parentSplitPanel;
			}

			return root;
		}
		private void Setup (Rect bounds)
		{
			splitterLine.Orientation = Orientation;
			// splitterLine.Text = Panel2.Title;

			// TODO: Recursion

			if (!Panel1.Visible || !Panel2.Visible) {
				View toFullSize = !Panel1.Visible ? Panel2 : Panel1;

				splitterLine.Visible = false;

				toFullSize.X = bounds.X;
				toFullSize.Y = bounds.Y;
				toFullSize.Width = bounds.Width;
				toFullSize.Height = bounds.Height;
			} else {
				splitterLine.Visible = true;

				splitterDistance = BoundByMinimumSizes (splitterDistance);

				Panel1.X = bounds.X;
				Panel1.Y = bounds.Y;

				switch (Orientation) {
				case Orientation.Horizontal:
					splitterLine.X = 0;
					splitterLine.Y = splitterDistance;
					splitterLine.Width = Dim.Fill ();
					splitterLine.Height = 1;
					splitterLine.LineRune = Driver.HLine;

					Panel1.Width = Dim.Fill (HasBorder()? 1:0);
					Panel1.Height = new Dim.DimFunc (() =>
					splitterDistance.Anchor (bounds.Height));

					Panel2.Y = Pos.Bottom (splitterLine);
					Panel2.X = bounds.X;
					Panel2.Width = bounds.Width;
					Panel2.Height = Dim.Fill(HasBorder () ? 1 : 0);
					break;

				case Orientation.Vertical:
					splitterLine.X = splitterDistance;
					splitterLine.Y = 0;
					splitterLine.Width = 1;
					splitterLine.Height = Dim.Fill ();
					splitterLine.LineRune = Driver.VLine;

					Panel1.Height = Dim.Fill();
					Panel1.Width = new Dim.DimFunc (() =>
					splitterDistance.Anchor (bounds.Width));

					Panel2.X = Pos.Right (splitterLine);
					Panel2.Y = bounds.Y;
					Panel2.Height = bounds.Height;
					Panel2.Width = Dim.Fill(HasBorder()? 1:0);
					break;

				default: throw new ArgumentOutOfRangeException (nameof (orientation));
				};
			}
		}

		/// <summary>
		/// Considers <paramref name="pos"/> as a candidate for <see cref="splitterDistance"/>
		/// then either returns (if valid) or returns adjusted if invalid with respect to the 
		/// <see cref="SplitterPanel.MinSize"/> of the panels.
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		private Pos BoundByMinimumSizes (Pos pos)
		{
			// if we are not yet initialized then we don't know
			// how big we are and therefore cannot sensibly calculate
			// how big the panels will be with a given SplitterDistance
			if (!IsInitialized) {
				return pos;
			}
			
			var panel1MinSize = Panel1MinSize;
			var panel2MinSize = Panel2MinSize;


			// if there is a border then there is less space
			// for the panels so we need to make size restrictions
			// tighter.
			if(HasBorder()) {
				panel1MinSize++;
				panel2MinSize++;
			}

			var availableSpace = Orientation == Orientation.Horizontal ? this.Bounds.Height : this.Bounds.Width;

			// we probably haven't finished layout even if IsInitialized is true :(
			if(availableSpace <= 0) {
				return pos;
			}

			var idealPosition = pos.Anchor (availableSpace);

			// bad position because not enough space for Panel1
			if (idealPosition < panel1MinSize) {

				// TODO: we should preserve Absolute/Percent status here not just force it to absolute
				return (Pos)Math.Min (panel1MinSize, availableSpace);
			}

			// bad position because not enough space for Panel2
			if (availableSpace - idealPosition <= panel2MinSize) {

				// TODO: we should preserve Absolute/Percent status here not just force it to absolute

				// +1 is to allow space for the splitter
				return (Pos)Math.Max (availableSpace - (panel2MinSize + 1), 0);
			}

			// this splitter position is fine, there is enough space for everyone
			return pos;
		}
		private class SplitContainerLineView : LineView {
			public SplitContainer Parent { get; private set; }

			Point? dragPosition;
			Pos dragOrignalPos;
			public Point? moveRuneRenderLocation;

			public SplitContainerLineView (SplitContainer parent)
			{
				CanFocus = true;
				TabStop = true;

				this.Parent = parent;

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

			public void DrawSplitterSymbol()
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
						Parent.SplitterDistance = Offset (Y, dy);
						moveRuneRenderLocation = new Point (mouseEvent.X, 0);
					} else {
						int dx = mouseEvent.X - dragPosition.Value.X;
						Parent.SplitterDistance = Offset (X, dx);
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
			/// Moves <see cref="parent"/> <see cref="SplitContainer.SplitterDistance"/> to 
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
						Parent.SplitterDistance = ConvertToPosFactor (newValue, Parent.Bounds.Height);
					} else {
						Parent.SplitterDistance = ConvertToPosFactor (newValue, Parent.Bounds.Width);
					}
				} else {
					Parent.SplitterDistance = newValue;
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
		private bool HasAnyTitles()
		{
			return Panel1Title.Length > 0 || Panel2Title.Length > 0;

		}

		private class ChildSplitterLine {

			readonly SplitContainerLineView currentLine;
			internal ChildSplitterLine (SplitContainerLineView currentLine)
			{
				this.currentLine = currentLine;
			}

			internal void DrawTitles ()
			{
				if(currentLine.Orientation == Orientation.Horizontal) 
				{
					var screenRect = currentLine.ViewToScreen (
						new Rect(0,0,currentLine.Frame.Width,currentLine.Frame.Height));
					Driver.DrawWindowTitle (screenRect, currentLine.Parent.Panel2Title, 0, 0, 0, 0);
				}
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
		public SplitterEventArgs (SplitContainer splitContainer, Pos splitterDistance)
		{
			SplitterDistance = splitterDistance;
			SplitContainer = splitContainer;
		}

		/// <summary>
		/// New position of the <see cref="SplitContainer.SplitterDistance"/>
		/// </summary>
		public Pos SplitterDistance { get; }

		/// <summary>
		/// Container (sender) of the event.
		/// </summary>
		public SplitContainer SplitContainer { get; }
	}

	/// <summary>
	///  Represents a method that will handle splitter events.
	/// </summary>
	public delegate void SplitterEventHandler (object sender, SplitterEventArgs e);
}
