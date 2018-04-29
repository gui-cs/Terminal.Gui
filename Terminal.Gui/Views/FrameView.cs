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
	/// The FrameView is a container frame that draws a frame around the contents
	/// </summary>
	public class FrameView : View {
		View contentView;
		ustring title;

		/// <summary>
		/// The title to be displayed for this window.
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
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Gui.FrameView"/> class with
		/// an absolute position and a title.
		/// </summary>
		/// <param name="frame">Frame.</param>
		/// <param name="title">Title.</param>
		public FrameView (Rect frame, ustring title) : base (frame)
		{
			var cFrame = new Rect (1, 1 , frame.Width - 2, frame.Height - 2);
			contentView = new ContentView (cFrame);
			base.Add (contentView);
			Title = title;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Gui.FrameView"/> class with
		/// a title and the result is suitable to have its X, Y, Width and Height properties computed.
		/// </summary>
		/// <param name="title">Title.</param>
		public FrameView (ustring title)
		{
			contentView = new ContentView () {
				X = 1,
				Y = 1,
				Width = Dim.Fill (2),
				Height = Dim.Fill (2)
			};
			base.Add (contentView);
			Title = title;
		}


		void DrawFrame ()
		{
			DrawFrame (new Rect (0, 0, Frame.Width, Frame.Height), 0, fill: true);
		}

		/// <summary>
		/// Add the specified view to the ContentView.
		/// </summary>
		/// <param name="view">View to add to the window.</param>
		public override void Add (View view)
		{
			contentView.Add (view);
			if (view.CanFocus)
				CanFocus = true;
		}


		/// <summary>
		///   Removes a widget from this container.
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

			if (contentView.Subviews.Count < 1)
				this.CanFocus = false;
		}

		public override void Redraw (Rect bounds)
		{
			if (!NeedDisplay.IsEmpty) {
				Driver.SetAttribute (ColorScheme.Normal);
				DrawFrame ();
				if (HasFocus)
					Driver.SetAttribute (ColorScheme.Normal);
				var width = Frame.Width;
				if (Title != null && width > 4) {
					Move (1, 0);
					Driver.AddRune (' ');
					var str = Title.Length > width ? Title [0, width - 4] : Title;
					Driver.AddStr (str);
					Driver.AddRune (' ');
				}
				Driver.SetAttribute (ColorScheme.Normal);
			}
			contentView.Redraw (contentView.Bounds);
			ClearNeedsDisplay ();
		}
	}
}
