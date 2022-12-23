using System;
using Terminal.Gui.Graphs;
using static Terminal.Gui.Dim;

namespace Terminal.Gui {
	public class SplitContainer : View {

		private LineView splitterLine = new LineView ();
		private bool panel1Collapsed;
		private bool panel2Collapsed;
		private Pos splitterDistance = Pos.Percent (50);
		private Orientation orientation = Orientation.Vertical;

		public SplitContainer ()
		{
			// Default to a border of 1 so that View looks nice
			Border = new Border ();

			this.Add (splitterLine);
			this.Add (Panel1);
			this.Add (Panel2);

			Setup ();

			// TODO: Actually respect collapsed statuses
		}

		private void Setup ()
		{
			// TODO: Enforce minimum sizes

			splitterLine.Orientation = Orientation;

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

		/// <summary>
		/// The left or top panel of the <see cref="SplitContainer"/>
		/// (depending on <see cref="Orientation"/>).  Add panel contents
		/// to this <see cref="View"/> using <see cref="View.Add(View)"/>.
		/// </summary>
		public View Panel1 { get; } = new View ();

		/// <summary>
		/// TODO: not implemented yet
		/// </summary>
		public int Panel1MinSize { get; set; }

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
		/// TODO: not implemented yet
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

		public Orientation Orientation {
			get { return orientation; }
			set {
				orientation = value;
				Setup ();
			}
		}

		public Pos SplitterDistance {
			get { return splitterDistance; }
			set {
				splitterDistance = value;
				Setup ();
			}
		}
	}
}
