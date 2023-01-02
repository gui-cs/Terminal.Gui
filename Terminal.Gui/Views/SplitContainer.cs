using NStack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Terminal.Gui.Graphs;

namespace Terminal.Gui {

	/// <summary>
	/// A <see cref="View"/> consisting of a moveable bar that divides
	/// the display area into 2 resizeable panels.
	/// </summary>
	public class SplitContainer : FrameView {

		private SplitContainerLineView splitterLine;
		private Pos splitterDistance = Pos.Percent (50);
		private Orientation orientation = Orientation.Vertical;
		private SplitterPanel [] splitterPanels = { new SplitterPanel(), new SplitterPanel() };

		/// <summary>
		/// Creates a new instance of the SplitContainer class.
		/// </summary>
		public SplitContainer ()
		{
			splitterLine = new SplitContainerLineView (this);

			this.Add (splitterPanels [0]);
			this.Add (splitterLine);
			this.Add (splitterPanels [1]);

			LayoutStarted += (e) => Setup ();

			CanFocus = false;
		}

		/// <summary>
		/// Gets the list of panels. Currently only supports 2 panels.
		/// <remarks>
		/// <para>
		/// The first item in the list is either the leftmost or topmost panel;
		/// the second item is either the rightmost or bottom panel 
		/// (depending on <see cref="Orientation"/>)
		/// </para>
		/// <para>
		/// Add panel contents to the <see cref="SplitterPanel"/>s using <see cref="View.Add(View)"/>.
		/// </para>
		/// </remarks>
		/// </summary>
		public List<SplitterPanel> Panels { get { return splitterPanels.ToList(); } }

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
				Setup ();
				LayoutSubviews ();
			}
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
				Setup ();
				OnSplitterMoved ();
				LayoutSubviews ();
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
			Driver.SetAttribute (ColorScheme.Normal);
			Clear ();
			base.Redraw (bounds);

			// Draw Splitter over Border (to get Ts)
			if (splitterLine.Visible) {
				splitterLine.Redraw (bounds);
			}

			// Draw Titles over Border
			var screen = ViewToScreen (bounds);
			if (splitterPanels[0].Visible && splitterPanels[0].Title.Length > 0) {
				Driver.SetAttribute (splitterPanels[0].HasFocus ? ColorScheme.HotNormal : ColorScheme.Normal);
				Driver.DrawWindowTitle (new Rect (screen.X, screen.Y, splitterPanels[0].Frame.Width, 1), splitterPanels[0].Title, 0, 0, 0, 0);
			}
			if (splitterLine.Visible) {
				screen = ViewToScreen (splitterLine.Frame);
			} else {
				screen.X--;
				screen.Y--;
			}
			if (Orientation == Orientation.Horizontal) {
				if (splitterPanels[1].Visible && splitterPanels[1].Title.Length > 0) {

					Driver.SetAttribute (splitterPanels[1].HasFocus ? ColorScheme.HotNormal : ColorScheme.Normal);
					Driver.DrawWindowTitle (new Rect (screen.X + 1, screen.Y + 1, splitterPanels[1].Bounds.Width, 1), splitterPanels[1].Title, 0, 0, 0, 0);
				}
			} else {
				if (splitterPanels[1].Visible && splitterPanels[1].Title.Length > 0) {
					Driver.SetAttribute (splitterPanels[1].HasFocus ? ColorScheme.HotNormal : ColorScheme.Normal);
					Driver.DrawWindowTitle (new Rect (screen.X + 1, screen.Y + 1, splitterPanels[1].Bounds.Width, 1), splitterPanels[1].Title, 0, 0, 0, 0);
				}
			}
		}

		private void Setup ()
		{
			splitterLine.Orientation = Orientation;
			splitterLine.Text = splitterPanels[1].Title;

			if (!splitterPanels[0].Visible || !splitterPanels[1].Visible) {
				View toFullSize = !splitterPanels[0].Visible ? splitterPanels[1] : splitterPanels[0];

				splitterLine.Visible = false;

				toFullSize.X = 0;
				toFullSize.Y = 0;
				toFullSize.Width = Dim.Fill ();
				toFullSize.Height = Dim.Fill ();
			} else {
				splitterLine.Visible = true;

				splitterDistance = BoundByMinimumSizes (splitterDistance);

				splitterPanels[0].X = 0;
				splitterPanels[0].Y = 0;

				splitterPanels[1].Width = Dim.Fill ();
				splitterPanels[1].Height = Dim.Fill ();

				switch (Orientation) {
				case Orientation.Horizontal:
					splitterLine.X = -1;
					splitterLine.Y = splitterDistance;
					splitterLine.Width = Dim.Fill () + 1;
					splitterLine.Height = 1;
					splitterLine.LineRune = Driver.HLine;

					splitterPanels[0].Width = Dim.Fill ();
					splitterPanels[0].Height = new Dim.DimFunc (() =>
					splitterDistance.Anchor (Bounds.Height)) - 1;

					splitterPanels[1].Y = Pos.Bottom (splitterLine);
					splitterPanels[1].X = 0;
					break;

				case Orientation.Vertical:
					splitterLine.X = splitterDistance;
					splitterLine.Y = -1;
					splitterLine.Width = 1;
					splitterLine.Height = Dim.Fill () + 1;
					splitterLine.LineRune = Driver.VLine;

					splitterPanels[0].Height = Dim.Fill ();
					splitterPanels[0].Width = new Dim.DimFunc (() =>
					splitterDistance.Anchor (Bounds.Width));

					splitterPanels[1].X = Pos.Right (splitterLine);
					splitterPanels[1].Y = 0;
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

			var availableSpace = Orientation == Orientation.Horizontal ? this.Bounds.Height : this.Bounds.Width;

			var idealPosition = pos.Anchor (availableSpace);

			// bad position because not enough space for splitterPanels[0]
			if (idealPosition < splitterPanels [0].MinSize.Anchor (availableSpace)) {

				// TODO: we should preserve Absolute/Percent status here not just force it to absolute
				return (Pos)Math.Min (splitterPanels [0].MinSize.Anchor (availableSpace), availableSpace);
			}

			// bad position because not enough space for splitterPanels[1]
			if (availableSpace - idealPosition <= splitterPanels [1].MinSize.Anchor (availableSpace)) {

				// TODO: we should preserve Absolute/Percent status here not just force it to absolute

				// +1 is to allow space for the splitter
				return (Pos)Math.Max (availableSpace - (splitterPanels [1].MinSize.Anchor (availableSpace) + 1), 0);
			}

			// this splitter position is fine, there is enough space for everyone
			return pos;
		}

		/// <summary>
		/// A panel within a <see cref="SplitterPanel"/>. 
		/// </summary>
		public class SplitterPanel : View {
			Pos minSize = 1;

			/// <summary>
			/// Gets or sets the minimum size for the panel.
			/// </summary>
			public Pos MinSize { get => minSize;
				set { 
					minSize = value;
					SuperView?.SetNeedsLayout ();
				} 
			}

			ustring title = ustring.Empty;
			/// <summary>
			/// The title to be displayed for this <see cref="SplitterPanel"/>. The title will be rendered 
			/// on the top border aligned to the left of the panel.
			/// </summary>
			/// <value>The title.</value>
			public ustring Title {
				get => title;
				set {
					title = value;
					SetNeedsDisplay ();
				}
			}

			/// <inheritdoc/>
			public override void Redraw (Rect bounds)
			{
				Driver.SetAttribute (ColorScheme.Normal);
				base.Redraw (bounds);
			}

			/// <inheritdoc/>
			public override void OnVisibleChanged ()
			{
				base.OnVisibleChanged ();
				SuperView?.SetNeedsLayout ();
			}
		}

		private class SplitContainerLineView : LineView {
			private SplitContainer parent;

			Point? dragPosition;
			Pos dragOrignalPos;
			Point? moveRuneRenderLocation;

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

				LayoutStarted += (e) => {
					moveRuneRenderLocation = null;
					if (Orientation == Orientation.Horizontal) {
						StartingAnchor = ParentHasBorder () ? Driver.LeftTee : (Rune?)null;
						EndingAnchor = ParentHasBorder () ? Driver.RightTee : (Rune?)null;
					} else {
						StartingAnchor = ParentHasBorder () ? Driver.TopTee : (Rune?)null;
						EndingAnchor = ParentHasBorder () ? Driver.BottomTee : (Rune?)null;
					}
				};
			}

			private bool ParentHasBorder ()
			{
				return parent.Border != null && parent.Border.BorderStyle != BorderStyle.None;
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
						parent.SplitterDistance = Offset (Y, dy);
						moveRuneRenderLocation = new Point (mouseEvent.X, 0);
					} else {
						int dx = mouseEvent.X - dragPosition.Value.X;
						parent.SplitterDistance = Offset (X, dx);
						moveRuneRenderLocation = new Point (0, Math.Max (1, Math.Min (Bounds.Height - 2, mouseEvent.Y)));
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
					//moveRuneRenderLocation = null;
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
