//
// FrameView.cs: Frame control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// The FrameView is a container frame that draws a frame around the contents. It is similar to
	/// a GroupBox in Windows.
	/// </summary>
	public class FrameView : View {
		View contentView;
		ustring title;

		/// <summary>
		/// The title to be displayed for this <see cref="FrameView"/>.
		/// </summary>
		/// <value>The title.</value>
		public ustring Title {
			get => title;
			set {
				title = value;
				SetNeedsDisplay ();
			}
		}

		class ContentView : View {
			public ContentView (Rect frame) : base (frame) { }
			public ContentView () : base () { }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.FrameView"/> class with
		/// an absolute position and a title.
		/// </summary>
		/// <param name="frame">Frame.</param>
		/// <param name="title">Title.</param>
		public FrameView (Rect frame, ustring title) : base (frame)
		{
			var cFrame = new Rect (1, 1 , frame.Width - 2, frame.Height - 2);
			this.title = title;
			contentView = new ContentView (cFrame);
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.FrameView"/> class with
		/// an absolute position, a title and <see cref="View"/>s.
		/// </summary>
		/// <param name="frame">Frame.</param>
		/// <param name="title">Title.</param>
		/// /// <param name="views">Views.</param>
		public FrameView (Rect frame, ustring title, View[] views) : this (frame, title)
		{
			foreach (var view in views) {
				contentView.Add (view);
			}
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.FrameView"/> class with
		/// a title and the result is suitable to have its X, Y, Width and Height properties computed.
		/// </summary>
		/// <param name="title">Title.</param>
		public FrameView (ustring title)
		{
			this.title = title;
			contentView = new ContentView () {
				X = 1,
				Y = 1,
				Width = Dim.Fill (1),
				Height = Dim.Fill (1)
			};
			Initialize ();
		}

		void Initialize ()
		{
			base.Add (contentView);
		}

		void DrawFrame ()
		{
			DrawFrame (new Rect (0, 0, Frame.Width, Frame.Height), 0, fill: true);
		}

		/// <summary>
		/// Add the specified <see cref="View"/> to this container.
		/// </summary>
		/// <param name="view"><see cref="View"/> to add to this container</param>
		public override void Add (View view)
		{
			contentView.Add (view);
			if (view.CanFocus)
				CanFocus = true;
		}


		/// <summary>
		///   Removes a <see cref="View"/> from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override void Remove (View view)
		{
			if (view == null)
				return;

			SetNeedsDisplay ();
			var touched = view.Frame;
			contentView.Remove (view);

			if (contentView.InternalSubviews.Count < 1)
				this.CanFocus = false;
		}

		/// <summary>
		///   Removes all <see cref="View"/>s from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override void RemoveAll()
		{
			contentView.RemoveAll();
		}

		///<inheritdoc cref="Redraw(Rect)"/>
		public override void Redraw (Rect bounds)
		{
			var padding = 0;
			Application.CurrentView = this;
			var scrRect = RectToScreen (new Rect (0, 0, Frame.Width, Frame.Height));

			if (NeedDisplay != null && !NeedDisplay.IsEmpty) {
				Driver.SetAttribute (ColorScheme.Normal);
				Driver.DrawFrame (scrRect, padding, true);
			}

			if (Driver.Clip.IsEmpty || Driver.Clip.Contains (contentView.RectToScreen (contentView.Frame))) {
				var savedClip = ClipToBounds (); 
				contentView.Redraw (contentView.Bounds);
				Driver.Clip = savedClip;
			} else {
				contentView.Redraw (contentView.Bounds);
			}
			ClearNeedsDisplay ();
			Driver.SetAttribute (ColorScheme.Normal);
			Driver.DrawFrame (scrRect, padding, false);

			if (HasFocus)
				Driver.SetAttribute (ColorScheme.HotNormal);
			Driver.DrawWindowTitle (scrRect, Title, padding, padding, padding, padding);
			Driver.SetAttribute (ColorScheme.Normal);
		}
	}
}
