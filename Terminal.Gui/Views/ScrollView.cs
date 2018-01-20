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
		public ScrollView (Rect frame) : base (frame)
		{
		}

		Rect contentSize;
		Point contentOffset;
		bool showHorizontalScrollIndicator;
		bool showVerticalScrollIndicator;

		/// <summary>
		/// Represents the contents of the data shown inside the scrolview
		/// </summary>
		/// <value>The size of the content.</value>
		public Rect ContentSize {
			get {
				return contentSize;
			}
			set {
				contentSize = value;
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
				contentOffset = value;
			}
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
			base.Redraw(region);
		}
	}
}
