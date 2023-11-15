using System.Text;
using System;
namespace Terminal.Gui {
	/// <summary>
	/// Specifies the style that a <see cref="ProgressBar"/> uses to indicate the progress of an operation.
	/// </summary>
	public enum ProgressBarStyle {
		/// <summary>
		/// Indicates progress by increasing the number of segmented blocks in a <see cref="ProgressBar"/>.
		/// </summary>
		Blocks,
		/// <summary>
		/// Indicates progress by increasing the size of a smooth, continuous bar in a <see cref="ProgressBar"/>.
		/// </summary>
		Continuous,
		/// <summary>
		/// Indicates progress by continuously scrolling a block across a <see cref="ProgressBar"/> in a marquee fashion.
		/// </summary>
		MarqueeBlocks,
		/// <summary>
		/// Indicates progress by continuously scrolling a block across a <see cref="ProgressBar"/> in a marquee fashion.
		/// </summary>
		MarqueeContinuous
	}

	/// <summary>
	///Specifies the format that a <see cref="ProgressBar"/> uses to indicate the visual presentation.
	/// </summary>
	public enum ProgressBarFormat {
		/// <summary>
		/// A simple visual presentation showing only the progress bar.
		/// </summary>
		Simple,
		/// <summary>
		/// A simple visual presentation showing the progress bar overlaid with the percentage.
		/// </summary>
		SimplePlusPercentage,
	}

	/// <summary>
	/// A Progress Bar view that can indicate progress of an activity visually.
	/// </summary>
	/// <remarks>
	///   <para>
	///     <see cref="ProgressBar"/> can operate in two modes, percentage mode, or
	///     activity mode. The progress bar starts in percentage mode and
	///     setting the Fraction property will reflect on the UI the progress 
	///     made so far. Activity mode is used when the application has no 
	///     way of knowing how much time is left, and is started when the <see cref="Pulse"/> method is called.  
	///     Call <see cref="Pulse"/> repeatedly as progress is made.
	///   </para>
	/// </remarks>
	public class ProgressBar : View {
<<<<<<< Updated upstream
		bool isActivity;
		int [] activityPos;
		int delta;
		View progress;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressBar"/> class, starts in percentage mode with an absolute position and size.
		/// </summary>
		/// <param name="rect">Rect.</param>
		public ProgressBar (Rect rect) : base (rect)
		{
			Initialize (rect);
		}
=======
		bool _isActivity;
		int [] _activityPos;
		int _delta;
>>>>>>> Stashed changes

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressBar"/> class, starts in percentage mode and uses relative layout.
		/// </summary>
		public ProgressBar () 
		{
			SetInitialProperties ();
		}

		void SetInitialProperties ()
		{
			CanFocus = false;
<<<<<<< Updated upstream
			fraction = 0;
			ColorScheme = new ColorScheme () {
				Normal = new Attribute (Color.BrightGreen, Color.Gray),
				HotNormal = Colors.Base.Normal
			};
			if (rect.IsEmpty) {
				Height = 1;
			}
			progress = new View () {
				Width = Dim.Fill (),
				Height = 1
			};
			base.Add (progress);
=======
			_fraction = 0;
			LayoutStarted += ProgressBar_LayoutStarted;
			Initialized += ProgressBar_Initialized;
>>>>>>> Stashed changes
		}

		void ProgressBar_Initialized (object sender, EventArgs e)
		{
			ColorScheme = new ColorScheme (ColorScheme ?? SuperView.ColorScheme) {
				HotNormal = new Attribute (Color.BrightGreen, Color.Gray)
			};
		}

		void ProgressBar_LayoutStarted (object sender, EventArgs e)
		{
			Bounds = new Rect (Bounds.Location, new Size (Bounds.Width, 1));
		}

		float _fraction;

		/// <summary>
		/// Gets or sets the <see cref="ProgressBar"/> fraction to display, must be a value between 0 and 1.
		/// </summary>
		/// <value>The fraction representing the progress.</value>
		public float Fraction {
			get => _fraction;
			set {
				_fraction = Math.Min (value, 1);
				_isActivity = false;
				SetNeedsDisplay ();
			}
		}

		ProgressBarStyle _progressBarStyle;

		/// <summary>
		/// Gets/Sets the progress bar style based on the <see cref="Terminal.Gui.ProgressBarStyle"/>
		/// </summary>
		public ProgressBarStyle ProgressBarStyle {
			get => _progressBarStyle;
			set {
				_progressBarStyle = value;
				switch (value) {
				case ProgressBarStyle.Blocks:
					SegmentCharacter = CM.Glyphs.BlocksMeterSegment;
					break;
				case ProgressBarStyle.Continuous:
					SegmentCharacter = CM.Glyphs.ContinuousMeterSegment;
					break;
				case ProgressBarStyle.MarqueeBlocks:
					SegmentCharacter = CM.Glyphs.BlocksMeterSegment;
					break;
				case ProgressBarStyle.MarqueeContinuous:
					SegmentCharacter = CM.Glyphs.ContinuousMeterSegment;
					break;
				}
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Specifies the format that a <see cref="ProgressBar"/> uses to indicate the visual presentation.
		/// </summary>
<<<<<<< Updated upstream
		public ProgressBarFormat ProgressBarFormat {
			get => progressBarFormat;
			set {
				progressBarFormat = value;
				switch (progressBarFormat) {
				case ProgressBarFormat.Simple:
					Height = 1;
					progress.Height = 1;
					break;
				case ProgressBarFormat.SimplePlusPercentage:
					Height = 2;
					progress.Height = 1;
					break;
				case ProgressBarFormat.Framed:
					Height = 3;
					progress.Height = 1;
					break;
				case ProgressBarFormat.FramedPlusPercentage:
					Height = 4;
					progress.Height = 1;
					break;
				case ProgressBarFormat.FramedProgressPadded:
					Height = 6;
					progress.Height = 3;
					break;
				}
				SetNeedsDisplay ();
			}
		}
=======
		public ProgressBarFormat ProgressBarFormat { get; set; }
>>>>>>> Stashed changes

		private Rune _segmentCharacter = CM.Glyphs.BlocksMeterSegment;

		/// <summary>
		/// Segment indicator for meter views.
		/// </summary>
		public Rune SegmentCharacter {
			get => _segmentCharacter;
			set {
				_segmentCharacter = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the text displayed on the progress bar. If set to an empty string and <see cref="ProgressBarFormat"/> is
		/// <see cref="ProgressBarFormat.SimplePlusPercentage"/> the percentage will be displayed.
		/// </summary>
		public override string Text {
			get => string.IsNullOrEmpty(base.Text) ? $"{_fraction * 100:F0}%" : base.Text;
			set => base.Text = value;
		}

		bool _bidirectionalMarquee = true;

		/// <summary>
		/// Specifies if the <see cref="ProgressBarStyle.MarqueeBlocks"/> or the
		///  <see cref="ProgressBarStyle.MarqueeContinuous"/> styles is unidirectional
		///  or bidirectional.
		/// </summary>
		public bool BidirectionalMarquee {
			get => _bidirectionalMarquee;
			set {
				_bidirectionalMarquee = value;
				SetNeedsDisplay ();
			}
		}
		
		/// <summary>
		/// Notifies the <see cref="ProgressBar"/> that some progress has taken place.
		/// </summary>
		/// <remarks>
		/// If the <see cref="ProgressBar"/> is percentage mode, it switches to activity
		/// mode. If is in activity mode, the marker is moved.
		/// </remarks>
		public void Pulse ()
		{
			if (_activityPos == null) {
				PopulateActivityPos ();
			}
			if (!_isActivity) {
				_isActivity = true;
				_delta = 1;
			} else {
				for (var i = 0; i < _activityPos.Length; i++) {
					_activityPos [i] += _delta;
				}
<<<<<<< Updated upstream
				int fWidth = progress.Bounds.Width;
				if (activityPos [activityPos.Length - 1] < 0) {
					for (int i = 0; i < activityPos.Length; i++) {
						activityPos [i] = i - activityPos.Length + 2;
=======
				if (_activityPos [^1] < 0) {
					for (var i = 0; i < _activityPos.Length; i++) {
						_activityPos [i] = i - _activityPos.Length + 2;
>>>>>>> Stashed changes
					}
					_delta = 1;
				} else if (_activityPos [0] >= Bounds.Width) {
					if (_bidirectionalMarquee) {
						for (var i = 0; i < _activityPos.Length; i++) {
							_activityPos [i] = Bounds.Width + i - 2;
						}
						_delta = -1;
					} else {
						PopulateActivityPos ();
					}
				}
			}

			SetNeedsDisplay ();
		}

		///<inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			Driver.SetAttribute (GetHotNormalColor());

<<<<<<< Updated upstream
			Driver.SetAttribute (GetNormalColor ());

			int fWidth = progress.Bounds.Width;
			if (isActivity) {
				progress.Move (0, 0);
				for (int i = 0; i < fWidth; i++)
					if (Array.IndexOf (activityPos, i) != -1)
=======
			Move (0, 0);
			if (_isActivity) {
				for (int i = 0; i < Bounds.Width; i++)
					if (Array.IndexOf (_activityPos, i) != -1) {
>>>>>>> Stashed changes
						Driver.AddRune (SegmentCharacter);
					} else {
						Driver.AddRune ((Rune)' ');
					}
			} else {
<<<<<<< Updated upstream
				progress.Move (0, 0);
				int mid = (int)(fraction * fWidth);
=======
				int mid = (int)(_fraction * Bounds.Width);
>>>>>>> Stashed changes
				int i;
				for (i = 0; i < mid & i < Bounds.Width; i++) {
					Driver.AddRune (SegmentCharacter);
				}
				for (; i < Bounds.Width; i++) {
					Driver.AddRune ((Rune)' ');
				}
			}

<<<<<<< Updated upstream
			progress.OnRenderLineCanvas ();

			DrawText (GetFrameWidth ());
		}

		int GetFrameWidth ()
		{
			switch (progressBarFormat) {
			case ProgressBarFormat.Simple:
			case ProgressBarFormat.SimplePlusPercentage:
				break;
			case ProgressBarFormat.Framed:
			case ProgressBarFormat.FramedPlusPercentage:
			case ProgressBarFormat.FramedProgressPadded:
				return Frame.Width - 2;
			}

			return Frame.Width;
		}

		void DrawText (int fWidth)
		{
			switch (progressBarFormat) {
			case ProgressBarFormat.Simple:
			case ProgressBarFormat.Framed:
				break;
			case ProgressBarFormat.SimplePlusPercentage:
			case ProgressBarFormat.FramedPlusPercentage:
			case ProgressBarFormat.FramedProgressPadded:
=======
			if (ProgressBarFormat != ProgressBarFormat.Simple && !_isActivity) {
>>>>>>> Stashed changes
				var tf = new TextFormatter () {
					Alignment = TextAlignment.Centered,
					Text = Text
				};
<<<<<<< Updated upstream
				var row = progressBarFormat == ProgressBarFormat.FramedProgressPadded ? 3 : 1;
				Move (0, row);
				var rect = new Rect (0, row, fWidth, 1);
				tf?.Draw (ViewToScreen (rect), ColorScheme.HotNormal, ColorScheme.HotNormal,
					SuperView == null ? default : SuperView.ViewToScreen (SuperView.Bounds));
				break;
			}
		}

		void DrawFrame ()
		{
			switch (progressBarFormat) {
			case ProgressBarFormat.Simple:
			case ProgressBarFormat.SimplePlusPercentage:
				Border.BorderStyle = LineStyle.None;
				Border.Thickness = new Thickness (0);
				progress.Border.BorderStyle = LineStyle.None;
				progress.Border.Thickness = new Thickness (0);
				break;
			case ProgressBarFormat.Framed:
			case ProgressBarFormat.FramedPlusPercentage:
				Border.BorderStyle = LineStyle.Single;
				Border.Thickness = new Thickness (1);
				progress.Border.BorderStyle = LineStyle.None;
				progress.Border.Thickness = new Thickness (0);
				break;
			case ProgressBarFormat.FramedProgressPadded:
				Border.BorderStyle = LineStyle.Single;
				Border.Thickness = new Thickness (1);
				progress.Border.BorderStyle = LineStyle.Single;
				progress.Border.Thickness = new Thickness (1);
				break;
=======
				Attribute attr = new Attribute (ColorScheme.HotNormal.Foreground, ColorScheme.HotNormal.Background); 
				if (_fraction > .5) {
					attr = new Attribute (ColorScheme.HotNormal.Background, ColorScheme.HotNormal.Foreground);
				}
				tf?.Draw (ViewToScreen (Bounds), 
					attr, 
					ColorScheme.Normal,
					SuperView?.ViewToScreen (SuperView.Bounds) ?? default, 
					fillRemaining: false);

>>>>>>> Stashed changes
			}

			progress.OnDrawFrames ();
		}

		void PopulateActivityPos ()
		{
			_activityPos = new int [Math.Min (Frame.Width / 3, 5)];
			for (var i = 0; i < _activityPos.Length; i++) {
				_activityPos [i] = i - _activityPos.Length + 1;
			}
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);
			return base.OnEnter (view);
		}

		///<inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			progress.Dispose ();
			progress = null;

			base.Dispose (disposing);
		}
	}
}
