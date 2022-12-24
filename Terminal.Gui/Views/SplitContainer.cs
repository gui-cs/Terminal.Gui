using System;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {
	public class SplitContainer : View {

		private LineView splitterLine;
		private bool panel1Collapsed;
		private bool panel2Collapsed;
		private Pos splitterDistance = Pos.Percent (50);
		private Orientation orientation = Orientation.Vertical;
		private Pos panel1MinSize = 0;
		private Pos panel2MinSize = 0;

		/// <summary>
		/// Creates a new instance of the SplitContainer class.
		/// </summary>
		public SplitContainer ()
		{
			splitterLine = new SplitContainerLineView (this);

			this.Add (Panel1);
			this.Add (splitterLine);
			this.Add (Panel2);

			Setup ();

			CanFocus = false;
		}

		/// <summary>
		/// The left or top panel of the <see cref="SplitContainer"/>
		/// (depending on <see cref="Orientation"/>).  Add panel contents
		/// to this <see cref="View"/> using <see cref="View.Add(View)"/>.
		/// </summary>
		public View Panel1 { get; } = new View ();

		/// <summary>
		/// The minimum size <see cref="Panel1"/> can be when adjusting
		/// <see cref="SplitterDistance"/>.
		/// </summary>
		public Pos Panel1MinSize {
			get { return panel1MinSize; }
			set {
				panel1MinSize = value;
				Setup ();
			}
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
			SplitterMoved?.Invoke (this,new SplitterEventArgs(this,splitterDistance));
		}


		/// <summary>
		/// This determines if <see cref="Panel1"/> is collapsed.
		/// </summary>
		public bool Panel1Collapsed {
			get { return panel1Collapsed; }
			set {
				panel1Collapsed = value;
				if (value && panel2Collapsed) {
					panel2Collapsed = false;
				}

				Setup ();
			}

		}

		/// <summary>
		/// The right or bottom panel of the <see cref="SplitContainer"/>
		/// (depending on <see cref="Orientation"/>).  Add panel contents
		/// to this <see cref="View"/> using <see cref="View.Add(View)"/>
		/// </summary>
		public View Panel2 { get; } = new View ();

		/// <summary>
		/// The minimum size <see cref="Panel2"/> can be when adjusting
		/// <see cref="SplitterDistance"/>.
		/// </summary>
		public Pos Panel2MinSize {
			get {
				return panel2MinSize;
			}

			set {
				panel2MinSize = value;
				Setup ();
			}
		}

		/// <summary>
		/// This determines if <see cref="Panel2"/> is collapsed.
		/// </summary>
		public bool Panel2Collapsed {
			get { return panel2Collapsed; }
			set {
				panel2Collapsed = value;
				if (value && panel1Collapsed) {
					panel1Collapsed = false;
				}
				Setup ();
			}
		}

		/// <summary>
		/// Orientation of the dividing line (Horizontal or Vertical).
		/// </summary>
		public Orientation Orientation {
			get { return orientation; }
			set {
				orientation = value;
				Setup ();
			}
		}


		/// <summary>
		/// Distance Horizontally or Vertically to the splitter line when
		/// neither panel is collapsed.
		/// </summary>
		public Pos SplitterDistance {
			get { return splitterDistance; }
			set {
				splitterDistance = value;
				Setup ();
				OnSplitterMoved ();
			}
		}

		public override bool OnEnter (View view)
		{
			Driver.SetCursorVisibility (CursorVisibility.Invisible);
			return base.OnEnter (view);
		}

		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (ColorScheme.Normal);
			Clear ();
			base.Redraw (bounds);
		}

		private void Setup ()
		{
			splitterLine.Orientation = Orientation;

			if (panel1Collapsed || panel2Collapsed) {
				SetupForCollapsedPanel ();
			} else {
				SetupForNormal ();
			}
		}

		private void SetupForNormal ()
		{
			// Ensure all our component views are here
			// (e.g. if we are transitioning from a collapsed state)

			if (!this.Subviews.Contains (splitterLine)) {
				this.Add (splitterLine);
			}
			if (!this.Subviews.Contains (Panel1)) {
				this.Add (Panel1);
			}
			if (!this.Subviews.Contains (Panel2)) {
				this.Add (Panel2);
			}

			splitterDistance = BoundByMinimumSizes (splitterDistance);

			switch (Orientation) {
			case Orientation.Horizontal:
				splitterLine.X = 0;
				splitterLine.Y = splitterDistance;
				splitterLine.Width = Dim.Fill ();
				splitterLine.Height = 1;
				splitterLine.LineRune = Driver.HLine;

				this.Panel1.X = 0;
				this.Panel1.Y = 0;
				this.Panel1.Width = Dim.Fill ();
				this.Panel1.Height = new Dim.DimFunc (() =>
					splitterDistance.Anchor (Bounds.Height));

				this.Panel2.Y = Pos.Bottom (splitterLine);
				this.Panel2.X = 0;
				this.Panel2.Width = Dim.Fill ();
				this.Panel2.Height = Dim.Fill ();
				break;

			case Orientation.Vertical:
				splitterLine.X = splitterDistance;
				splitterLine.Y = 0;
				splitterLine.Width = 1;
				splitterLine.Height = Dim.Fill ();
				splitterLine.LineRune = Driver.VLine;

				this.Panel1.X = 0;
				this.Panel1.Y = 0;
				this.Panel1.Height = Dim.Fill ();
				this.Panel1.Width = new Dim.DimFunc (() =>
					splitterDistance.Anchor (Bounds.Width));

				this.Panel2.X = Pos.Right (splitterLine);
				this.Panel2.Y = 0;
				this.Panel2.Width = Dim.Fill ();
				this.Panel2.Height = Dim.Fill ();
				break;

			default: throw new ArgumentOutOfRangeException (nameof (orientation));
			};

			this.LayoutSubviews ();
		}

		/// <summary>
		/// Considers <paramref name="pos"/> as a candidate for <see cref="splitterDistance"/>
		/// then either returns (if valid) or returns adjusted if invalid with respect to
		/// <see cref="Panel1MinSize"/> or <see cref="Panel2MinSize"/>.
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

			var availableSpace = Orientation == Orientation.Horizontal ? this.Bounds.Height : this.Bounds.Width;

			var idealPosition = pos.Anchor (availableSpace);
			var panel1MinSizeAbs = panel1MinSize.Anchor (availableSpace);
			var panel2MinSizeAbs = panel2MinSize.Anchor (availableSpace);

			// bad position because not enough space for panel1
			if (idealPosition < panel1MinSizeAbs) {

				// TODO: we should preserve Absolute/Percent status here not just force it to absolute
				return (Pos)Math.Min (panel1MinSizeAbs, availableSpace);
			}
			
			// bad position because not enough space for panel2
			if(availableSpace - idealPosition <= panel2MinSizeAbs) {

				// TODO: we should preserve Absolute/Percent status here not just force it to absolute

				// +1 is to allow space for the splitter
				return (Pos)Math.Max (availableSpace - (panel2MinSizeAbs+1), 0);
			}

			// this splitter position is fine, there is enough space for everyone
			return pos;
		}

		private void SetupForCollapsedPanel ()
		{
			View toRemove = panel1Collapsed ? Panel1 : Panel2;
			View toFullSize = panel1Collapsed ? Panel2 : Panel1;

			if (this.Subviews.Contains (splitterLine)) {
				this.Remove(splitterLine);
			}
			if (this.Subviews.Contains (toRemove)) {
				this.Remove (toRemove);
			}
			if (!this.Subviews.Contains (toFullSize)) {
				this.Add (toFullSize);
			}

			toFullSize.X = 0;
			toFullSize.Y = 0;
			toFullSize.Width = Dim.Fill ();
			toFullSize.Height = Dim.Fill ();
		}

		private class SplitContainerLineView : LineView {
			private SplitContainer parent;

			Point? dragPosition;
			Pos dragOrignalPos;
			Point? moveRuneRenderLocation;

			// TODO: Make focusable and allow moving with keyboard
			public SplitContainerLineView (SplitContainer parent)
			{
				CanFocus = true;
				TabStop = true;

				this.parent = parent;

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


			///<inheritdoc/>
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
				Move (this.Bounds.Width / 2, this.Bounds.Height / 2);
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

				if (CanFocus && HasFocus) {

					var location = moveRuneRenderLocation ??
						new Point (Bounds.Width / 2, Bounds.Height / 2);

					AddRune (location.X, location.Y, Driver.Diamond);
				}

			}

			///<inheritdoc/>
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
					}

					return true;
				} else if (
					dragPosition.HasValue &&
					(mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))) {

					// Continue Drag

					// how far has user dragged from original location?						
					if (Orientation == Orientation.Horizontal) {
						int dy = mouseEvent.Y - dragPosition.Value.Y;
						parent.SplitterDistance = Offset (Y, dy);
						moveRuneRenderLocation = new Point (mouseEvent.X, 0);
					} else {
						int dx = mouseEvent.X - dragPosition.Value.X;
						parent.SplitterDistance = Offset (X, dx);
						moveRuneRenderLocation = new Point (0, mouseEvent.Y);
					}

					parent.SetNeedsDisplay ();
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
					parent.Bounds.Height : parent.Bounds.Width);

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
						parent.SplitterDistance = ConvertToPosFactor (newValue, parent.Bounds.Height);
					} else {
						parent.SplitterDistance = ConvertToPosFactor (newValue, parent.Bounds.Width);
					}
				} else {
					parent.SplitterDistance = newValue;
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
