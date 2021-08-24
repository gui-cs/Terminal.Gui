using System;

namespace Terminal.Gui {
	/// <summary>
	/// A container for single <see cref="Child"/> that will allow to drawn <see cref="Border"/> in
	///  two ways. If <see cref="UsePanelFrame"/> the borders and the child will be accommodated in the available
	///  panel size, otherwise the panel will be resized based on the child and borders thickness sizes.
	/// </summary>
	public class PanelView : View {
		ChildContentView childContentView;

		private class ChildContentView : View { }

		private class SavedPosDim {
			public Pos X;
			public Pos Y;
			public Dim Width;
			public Dim Height;
		}

		private SavedPosDim savedPanel;
		private SavedPosDim savedChild;

		private View child;
		private bool usePanelFrame;

		/// <summary>
		/// Initializes a panel with a null child.
		/// </summary>
		public PanelView () : this (null) { }

		/// <summary>
		/// Initializes a panel with a valid child.
		/// </summary>
		/// <param name="child"></param>
		public PanelView (View child)
		{
			childContentView = new ChildContentView ();
			base.Add (childContentView);
			CanFocus = false;
			Child = child;
			if (child != null) {
				Visible = child.Visible;
			}
		}

		/// <summary>
		/// Gets or sets if the panel size will used, otherwise the child size.
		/// </summary>
		public bool UsePanelFrame {
			get => usePanelFrame;
			set {
				usePanelFrame = value;
				AdjustContainer ();
			}
		}

		/// <summary>
		/// The child that will use this panel.
		/// </summary>
		public View Child {
			get => child;
			set {
				if (child != null && value == null) {
					childContentView.Remove (child);
					child = value;
					return;
				}
				child = value;
				savedChild = new SavedPosDim () {
					X = child?.X,
					Y = child?.Y,
					Width = child?.Width,
					Height = child?.Height
				};
				if (child == null) {
					Visible = false;
					return;
				}
				child.X = 0;
				child.Y = 0;
				AdjustContainer ();
				if (child?.Border != null) {
					child.Border.BorderChanged += Border_BorderChanged;
					Border = child.Border;
					Border.Child = childContentView;
				} else {
					if (Border == null) {
						Border = new Border ();
					}
					Border.BorderChanged += Border_BorderChanged;
					Border.Child = childContentView;
				}
				if (!child.IsInitialized) {
					child.Initialized += Child_Initialized;
				}
				childContentView.Add (Child);
			}
		}

		private void Child_Initialized (object sender, EventArgs e)
		{
			savedPanel = new SavedPosDim () {
				X = X,
				Y = Y,
				Width = Width,
				Height = Height
			};
			AdjustContainer ();
			Child.Initialized -= Child_Initialized;
		}

		private void Border_BorderChanged (Border obj)
		{
			AdjustContainer ();
		}

		private void AdjustContainer ()
		{
			if (Child?.IsInitialized == true) {
				var borderLength = Child.Border != null
					? Child.Border.DrawMarginFrame ? 1 : 0
					: 0;
				var sumPadding = Child.Border != null
					? Child.Border.GetSumThickness ()
					: new Thickness ();
				if (!UsePanelFrame) {
					X = savedChild.X;
					childContentView.X = borderLength + sumPadding.Left;
					Y = savedChild.Y;
					childContentView.Y = borderLength + sumPadding.Top;
					if (savedChild.Width is Dim.DimFill) {
						var margin = -savedChild.Width.Anchor (0);
						Width = Dim.Fill (margin);
						childContentView.Width = Dim.Fill (margin + borderLength + sumPadding.Right);
					} else {
						Width = savedChild.Width + (2 * borderLength) + sumPadding.Right + sumPadding.Left;
						childContentView.Width = Dim.Fill (borderLength + sumPadding.Right);
					}
					if (savedChild.Height is Dim.DimFill) {
						var margin = -savedChild.Height.Anchor (0);
						Height = Dim.Fill (margin);
						childContentView.Height = Dim.Fill (margin + borderLength + sumPadding.Bottom);
					} else {
						Height = savedChild.Height + (2 * borderLength) + sumPadding.Bottom + sumPadding.Top;
						childContentView.Height = Dim.Fill (borderLength + sumPadding.Bottom);
					}
				} else {
					X = savedPanel.X;
					childContentView.X = borderLength + sumPadding.Left;
					Y = savedPanel.Y;
					childContentView.Y = borderLength + sumPadding.Top;
					Width = savedPanel.Width;
					Height = savedPanel.Height;
					if (Width is Dim.DimFill) {
						var margin = -savedPanel.Width.Anchor (0);
						childContentView.Width = Dim.Fill (margin + borderLength + sumPadding.Right);
					} else {
						childContentView.Width = Dim.Fill (borderLength + sumPadding.Right);
					}
					if (Height is Dim.DimFill) {
						var margin = -savedPanel.Height.Anchor (0);
						childContentView.Height = Dim.Fill (margin + borderLength + sumPadding.Bottom);
					} else {
						childContentView.Height = Dim.Fill (borderLength + sumPadding.Bottom);
					}
				}
				Visible = Child.Visible;
			} else {
				Visible = false;
			}
		}

		/// <inheritdoc/>
		public override void Add (View view)
		{
			if (Child != null) {
				Child = null;
			}
			Child = view;
		}

		/// <inheritdoc/>
		public override void Remove (View view)
		{
			if (view == childContentView) {
				base.Remove (view);
				return;
			}
			childContentView.Remove (view);
			if (Child != null) {
				Child = null;
			}
		}

		/// <inheritdoc/>
		public override void RemoveAll ()
		{
			if (Child != null) {
				Child = null;
			}
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (!NeedDisplay.IsEmpty) {
				Driver.SetAttribute (Child.GetNormalColor ());
				Border.DrawContent ();
			}
			var savedClip = childContentView.ClipToBounds ();
			childContentView.Redraw (childContentView.Bounds);
			Driver.Clip = savedClip;

			ClearLayoutNeeded ();
			ClearNeedsDisplay ();
		}
	}
}