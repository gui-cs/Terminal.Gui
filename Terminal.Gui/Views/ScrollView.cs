using System;
namespace Terminal.Gui {
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// <para>
	///   The subviews that are added to this scrollview are offset by the
	///   ContentOffset property.   The view itself is a window into the 
	///   space represented by the ContentSize.
	/// </para>
	/// <para>
	///   
	/// </para>
	/// </remarks>
	public class ScrollView : View {
		View contentView;

		public ScrollView (Rect frame) : base (frame)
		{
			contentView = new View (frame);
			base.Add (contentView);
		}

		Size contentSize;
		Point contentOffset;
		bool showHorizontalScrollIndicator;
		bool showVerticalScrollIndicator;

		/// <summary>
		/// Represents the contents of the data shown inside the scrolview
		/// </summary>
		/// <value>The size of the content.</value>
		public Size ContentSize {
			get {
				return contentSize;
			}
			set {
				contentSize = value;
				contentView.Frame = new Rect (contentOffset, value);
			}
		}

		/// <summary>
		/// Represents the top left corner coordinate that is displayed by the scrollview
		/// </summary>
		/// <value>The content offset.</value>
		public Point ContentOffset {
			get {
				return contentOffset;
			}
			set {
				contentOffset = new Point (-value.X, -value.Y);
				contentView.Frame = new Rect (contentOffset, contentSize);
			}
		}

		/// <summary>
		/// Adds the view to the scrollview.
		/// </summary>
		/// <param name="view">The view to add to the scrollview.</param>
		public override void Add (View view)
		{
			contentView.Add (view);
		}

		/// <summary>
		/// Gets or sets the visibility for the horizontal scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show vertical scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowHorizontalScrollIndicator {
			get => showHorizontalScrollIndicator;
			set {
				showHorizontalScrollIndicator = value;
				SetNeedsDisplay ();
			}
		}


		/// <summary>
		/// /// Gets or sets the visibility for the vertical scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show vertical scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowVerticalScrollIndicator {
			get => showVerticalScrollIndicator;
			set {
				showVerticalScrollIndicator = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// This event is raised when the contents have scrolled
		/// </summary>
		public event Action<ScrollView> Scrolled;

		public override void Redraw(Rect region)
		{
			var oldClip = ClipToBounds ();
			base.Redraw(region);
			Attribute last = ColorScheme.Normal;
			Driver.SetAttribute (last);

			void SetColor (Attribute a)
			{
				if (a != last)
					Driver.SetAttribute (a);
				last = a;
			}

			Driver.Clip = oldClip;

			if (true || ShowVerticalScrollIndicator) {
				var bh = Bounds.Height;
				var by1 = -contentOffset.Y * bh/ contentSize.Height;
				var by2 = (-contentOffset.Y+bh) * bh/ contentSize.Height;

				for (int y = 0; y < bh; y++) {
					Move (Bounds.Width - 1, y);
					SpecialChar special;

					if (y < by1 || y > by2)
						special = SpecialChar.Stipple;
					else {
						if (by2 - by1 == 0)
							special = SpecialChar.Diamond;
						else {
							if (y == by1)
								special = SpecialChar.TopTee;
							else if (y == by2)
								special = SpecialChar.BottomTee;
							else
								special = SpecialChar.VLine;
						}
					}
					Driver.AddSpecial (special);
				}
			}
			if (true || ShowHorizontalScrollIndicator){
				var bw = Bounds.Width;
				var bx1 = -contentOffset.X * bw / contentSize.Width;
				var bx2 = (-contentOffset.X + bw) * bw / contentSize.Width;

				Move (0, Bounds.Height - 1);
				for (int x = 0; x < bw; x++) {
					SpecialChar special;

					if (x < bx1 || x > bx2){
						special = SpecialChar.Stipple;
					} else {
						if (bx2 - bx1 == 0)
							special = SpecialChar.Diamond;
						else {
							if (x == bx1)
								special = SpecialChar.LeftTee;
							else if (x == bx2)
								special = SpecialChar.RightTee;
							else
								special = SpecialChar.HLine;
						}
					}
					Driver.AddSpecial (special);
				}
			}
		}
	}
}
