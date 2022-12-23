using System;
using Terminal.Gui.Graphs;
using static Terminal.Gui.Dim;
using static Terminal.Gui.Pos;

namespace Terminal.Gui {
	public class SplitContainer : View {

		private LineView splitterLine;
		private bool panel1Collapsed;
		private bool panel2Collapsed;
		private Pos splitterDistance = Pos.Percent (50);
		private Orientation orientation = Orientation.Vertical;
		private int panel1MinSize;

		/// <summary>
		/// Creates a new instance of the SplitContainer class.
		/// </summary>
		public SplitContainer ()
		{
			// Default to a border of 1 so that View looks nice
			Border = new Border ();
			splitterLine = new SplitContainerLineView (this);

			this.Add (splitterLine);
			this.Add (Panel1);
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
		public int Panel1MinSize {
			get { return panel1MinSize; }
			set {
				panel1MinSize = value;
				Setup();
			}
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
		public int Panel2MinSize { get; set; }

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
			}
		}

		public override bool OnEnter (View view)
		{
			Driver.SetCursorVisibility (CursorVisibility.Invisible);
			return base.OnEnter (view);
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
				this.Panel1.Height = new DimFunc (() =>
					splitterDistance.Anchor (Bounds.Height) - 1);

				this.Panel2.Y = Pos.Bottom (splitterLine) + 1;
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
				this.Panel1.Width = new DimFunc (() =>
					splitterDistance.Anchor (Bounds.Width) - 1);

				this.Panel2.X = Pos.Right (splitterLine) + 1;
				this.Panel2.Y = 0;
				this.Panel2.Width = Dim.Fill ();
				this.Panel2.Height = Dim.Fill ();
				break;

			default: throw new ArgumentOutOfRangeException (nameof (orientation));
			};
		}

		private void SetupForCollapsedPanel ()
		{
			View toRemove = panel1Collapsed ? Panel1 : Panel2;
			View toFullSize = panel1Collapsed ? Panel2 : Panel1;

			if (this.Subviews.Contains (splitterLine)) {
				this.Subviews.Remove (splitterLine);
			}
			if (this.Subviews.Contains (toRemove)) {
				this.Subviews.Remove (toRemove);
			}
			if (!this.Subviews.Contains (toFullSize)) {
				this.Add (toFullSize);
			}

			toFullSize.X = 0;
			toFullSize.Y = 0;
			toFullSize.Width = Dim.Fill ();
			toFullSize.Height = Dim.Fill ();
		}

		private class SplitContainerLineView : LineView
		{
			private SplitContainer parent;

			Point? dragPosition;
			Pos dragOrignalPos;

			// TODO: Make focusable and allow moving with keyboard
			public SplitContainerLineView(SplitContainer parent)
			{
				CanFocus = true;
				this.parent = parent;

				base.AddCommand (Command.Right, () => {
					if (Orientation == Orientation.Vertical) {
						parent.SplitterDistance = Offset (X, 1);
						return true;
					}
					return false;
				});

				base.AddCommand (Command.Left, () => {
					if (Orientation == Orientation.Vertical) {
						parent.SplitterDistance = Offset (X, -1);
						return true;
					}
					return false;
				});

				base.AddCommand (Command.LineUp, () => {
					if (Orientation == Orientation.Horizontal) {
						parent.SplitterDistance = Offset (Y, -1);
						return true;
					}
					return false;
				});

				base.AddCommand (Command.LineDown, () => {
					if (Orientation == Orientation.Horizontal) {
						parent.SplitterDistance = Offset (Y, 1);
						return true;
					}
					return false;
				});

				AddKeyBinding (Key.CursorRight, Command.Right);
				AddKeyBinding (Key.CursorLeft, Command.Left);
				AddKeyBinding (Key.CursorUp, Command.LineUp);
				AddKeyBinding (Key.CursorDown, Command.LineDown);
			}


			///<inheritdoc/>
			public override bool ProcessKey (KeyEvent kb)
			{
				var result = InvokeKeybindings (kb);
				if (result != null)
					return (bool)result;

				return base.ProcessKey (kb);
			}

			///<inheritdoc/>
			public override bool MouseEvent (MouseEvent mouseEvent)
			{
				if (!CanFocus) {
					return true;
				}

				// Start a drag
				if (!dragPosition.HasValue && (mouseEvent.Flags == MouseFlags.Button1Pressed)) {

					SetFocus ();
					Application.EnsuresTopOnFront ();

					if (mouseEvent.Flags == MouseFlags.Button1Pressed) {
						dragPosition = new Point (mouseEvent.X, mouseEvent.Y);
						dragOrignalPos = Orientation == Orientation.Horizontal ? Y : X;
						Application.GrabMouse (this);
					}

					return true;
				} else if (mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) 
				{
					if (dragPosition.HasValue) {
						
						// how far has user dragged from original location?						
						if(Orientation == Orientation.Horizontal)
						{
							int dy = mouseEvent.Y - dragPosition.Value.Y;
							parent.SplitterDistance = Offset(Y , dy);
						}
						else
						{
							int dx = mouseEvent.X - dragPosition.Value.X;
							parent.SplitterDistance = Offset(X , dx);
						}

						parent.SetNeedsDisplay ();
						return true;
					}
				}

				if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && dragPosition.HasValue) {
					Application.UngrabMouse ();
					Driver.UncookMouse ();
					FinalisePosition ();
					dragPosition = null;
				}

				return false;
			}

			private Pos Offset (Pos pos, int delta)
			{
				var posAbsolute = pos.Anchor (Orientation == Orientation.Horizontal ?
					parent.Bounds.Width : parent.Bounds.Height);

				return posAbsolute + delta;
			}
			private void FinalisePosition ()
			{
				// if before dragging we were a proportional position
				// then preserve that when the mouse is released so that
				// resizing continues to work as intended
				if(dragOrignalPos is PosFactor) {
					if(Orientation == Orientation.Horizontal) {
						Y = ToPosFactor (Y, parent.Bounds.Height); 
					} else {
						X = ToPosFactor (X, parent.Bounds.Width);
					}
				}
			}

			private Pos ToPosFactor (Pos y, int parentLength)
			{
				int position = y.Anchor (parentLength);
				return new PosFactor (position / (float)parentLength);
			}
		}
	}
}
