//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// NOTE: Window is functionally identical to FrameView with the following exceptions. 
//  - Window is a Toplevel
//  - FrameView Does not support padding (but should)
//  - FrameView Does not support mouse dragging
//  - FrameView Does not support IEnumerable
// Any updates done here should probably be done in FrameView as well; TODO: Merge these classes

using System;
using System.Collections;
using System.Text.Json.Serialization;
using NStack;
using Terminal.Gui.Configuration;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui {
	
	/// <summary>
	/// A <see cref="Toplevel"/> <see cref="View"/> that draws a border around its <see cref="View.Frame"/> with a Title at the top.
	/// </summary>
	/// <remarks>
	/// The 'client area' of a <see cref="Window"/> is a rectangle deflated by one or more rows/columns from <see cref="View.Bounds"/>. A this time there is no
	/// API to determine this rectangle.
	/// </remarks>
	public class Window : Toplevel {
		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.Window"/> class with an optional title using <see cref="LayoutStyle.Absolute"/> positioning.
		/// </summary>
		/// <param name="frame">Superview-relative rectangle specifying the location and size</param>
		/// <param name="title">Title</param>
		/// <remarks>
		/// This constructor initializes a Window with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Absolute"/>. Use constructors
		/// that do not take <c>Rect</c> parameters to initialize a Window with <see cref="LayoutStyle.Computed"/>. 
		/// </remarks>
		public Window (Rect frame, ustring title = null) : this (frame, title, padding: 0, border: null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class with an optional title using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <remarks>
		///   This constructor initializes a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/>. 
		///   Use <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> properties to dynamically control the size and location of the view.
		/// </remarks>
		public Window (ustring title = null) : this (title, padding: 0, border: null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public Window () : this (title: null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> using <see cref="LayoutStyle.Absolute"/> positioning with the specified frame for its location, with the specified frame padding,
		/// and an optional title.
		/// </summary>
		/// <param name="frame">Superview-relative rectangle specifying the location and size</param>
		/// <param name="title">Title</param>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		/// <remarks>
		/// This constructor initializes a Window with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Absolute"/>. Use constructors
		/// that do not take <c>Rect</c> parameters to initialize a Window with  <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/> 
		/// </remarks>
		public Window (Rect frame, ustring title = null, int padding = 0, Border border = null) : base (frame)
		{
			Initialize (title, frame, padding, border);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> using <see cref="LayoutStyle.Computed"/> positioning,
		/// and an optional title.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		/// <remarks>
		///   This constructor initializes a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/>. 
		///   Use <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> properties to dynamically control the size and location of the view.
		/// </remarks>
		public Window (ustring title = null, int padding = 0, Border border = null) : base ()
		{
			Initialize (title, Rect.Empty, padding, border);
		}

		/// <summary>
		/// The default <see cref="BorderStyle"/> for <see cref="FrameView"/>. The default is <see cref="BorderStyle.Single"/>.
		/// </summary>
		/// <remarks>
		/// This property can be set in a Theme to change the default <see cref="BorderStyle"/> for all <see cref="Window"/>s. 
		/// </remarks>
		///[SerializableConfigurationProperty (Scope = typeof (ThemeScope)), JsonConverter (typeof (JsonStringEnumConverter))]
		public static BorderStyle DefaultBorderStyle { get; set; } = BorderStyle.Single;

		void Initialize (ustring title, Rect frame, int padding = 0, Border border = null)
		{
			CanFocus = true;
			ColorScheme = Colors.Base;
			if (title == null) title = ustring.Empty;
			Title = title;
			if (border == null) {
				Border = new Border () {
					BorderStyle = DefaultBorderStyle,
					Padding = new Thickness (padding),
					//Title = title
				};
			} else {
				Border = border;
			}
		}

		/// <inheritdoc/>
		public override void Add (View view)
		{
			base.Add (view);
			if (view.CanFocus) {
				CanFocus = true;
			}
			AddMenuStatusBar (view);
		}


		/// <inheritdoc/>
		public override void Remove (View view)
		{
			if (view == null) {
				return;
			}

			SetNeedsDisplay ();
			base.Remove (view);
			RemoveMenuStatusBar (view);

		}

		///// <inheritdoc/>
		//public override void RemoveAll ()
		//{
		//	contentView.RemoveAll ();
		//}

		/////<inheritdoc/>
		//public override void Redraw (Rect bounds)
		//{
		//	var padding = Border.GetSumThickness ();
		//	var scrRect = ViewToScreen (new Rect (0, 0, Frame.Width, Frame.Height));

		//	if (!NeedDisplay.IsEmpty || ChildNeedsDisplay || LayoutNeeded) {
		//		Driver.SetAttribute (GetNormalColor ());
		//		Clear ();
		//		contentView.SetNeedsDisplay ();
		//	}
		//	var savedClip = contentView.ClipToBounds ();

		//	// Redraw our contentView
		//	// DONE: smartly constrict contentView.Bounds to just be what intersects with the 'bounds' we were passed
		//	contentView.Redraw (!NeedDisplay.IsEmpty || ChildNeedsDisplay || LayoutNeeded ? contentView.Bounds : bounds);
		//	Driver.Clip = savedClip;

		//	ClearLayoutNeeded ();
		//	ClearNeedsDisplay ();

		//	Driver.SetAttribute (GetNormalColor ());
		//	//Driver.DrawWindowFrame (scrRect, padding.Left + borderLength, padding.Top + borderLength, padding.Right + borderLength, padding.Bottom + borderLength,
		//	//	Border.BorderStyle != BorderStyle.None, fill: true, Border.BorderStyle);
		//	Border.DrawContent (this, false);
		//	if (HasFocus)
		//		Driver.SetAttribute (ColorScheme.HotNormal);
		//	if (Border.DrawMarginFrame)
		//		Driver.DrawWindowTitle (scrRect, Title, padding.Left, padding.Top, padding.Right, padding.Bottom);
		//	Driver.SetAttribute (GetNormalColor ());
		//}

		///// <inheritdoc/>
		//public override void OnCanFocusChanged ()
		//{
		//	if (contentView != null) {
		//		contentView.CanFocus = CanFocus;
		//	}
		//	base.OnCanFocusChanged ();
		//}

		///// <summary>
		/////   The text displayed by the <see cref="Label"/>.
		///// </summary>
		//public override ustring Text {
		//	get => contentView?.Text;
		//	set {
		//		base.Text = value;
		//		if (contentView != null) {
		//			contentView.Text = value;
		//		}
		//	}
		//}

		///// <summary>
		///// Controls the text-alignment property of the label, changing it will redisplay the <see cref="Label"/>.
		///// </summary>
		///// <value>The text alignment.</value>
		//public override TextAlignment TextAlignment {
		//	get => contentView.TextAlignment;
		//	set {
		//		base.TextAlignment = contentView.TextAlignment = value;
		//	}
		//}

		/// <summary>
		/// Event arguments for <see cref="View.Title"/> change events.
		/// </summary>
		public class TitleEventArgs : EventArgs {
			/// <summary>
			/// The new Window Title.
			/// </summary>
			public ustring NewTitle { get; set; }

			/// <summary>
			/// The old Window Title.
			/// </summary>
			public ustring OldTitle { get; set; }

			/// <summary>
			/// Flag which allows canceling the Title change.
			/// </summary>
			public bool Cancel { get; set; }

			/// <summary>
			/// Initializes a new instance of <see cref="TitleEventArgs"/>
			/// </summary>
			/// <param name="oldTitle">The <see cref="View.Title"/> that is/has been replaced.</param>
			/// <param name="newTitle">The new <see cref="View.Title"/> to be replaced.</param>
			public TitleEventArgs (ustring oldTitle, ustring newTitle)
			{
				OldTitle = oldTitle;
				NewTitle = newTitle;
			}
		}
		/// <summary>
		/// Called before the <see cref="View.Title"/> changes. Invokes the <see cref="TitleChanging"/> event, which can be cancelled.
		/// </summary>
		/// <param name="oldTitle">The <see cref="View.Title"/> that is/has been replaced.</param>
		/// <param name="newTitle">The new <see cref="View.Title"/> to be replaced.</param>
		/// <returns>`true` if an event handler canceled the Title change.</returns>
		public virtual bool OnTitleChanging (ustring oldTitle, ustring newTitle)
		{
			var args = new TitleEventArgs (oldTitle, newTitle);
			TitleChanging?.Invoke (args);
			return args.Cancel;
		}

		/// <summary>
		/// Event fired when the <see cref="View.Title"/> is changing. Set <see cref="TitleEventArgs.Cancel"/> to 
		/// `true` to cancel the Title change.
		/// </summary>
		public event Action<TitleEventArgs> TitleChanging;

		/// <summary>
		/// Called when the <see cref="View.Title"/> has been changed. Invokes the <see cref="TitleChanged"/> event.
		/// </summary>
		/// <param name="oldTitle">The <see cref="View.Title"/> that is/has been replaced.</param>
		/// <param name="newTitle">The new <see cref="View.Title"/> to be replaced.</param>
		public virtual void OnTitleChanged (ustring oldTitle, ustring newTitle)
		{
			var args = new TitleEventArgs (oldTitle, newTitle);
			TitleChanged?.Invoke (args);
		}

		/// <summary>
		/// Event fired after the <see cref="View.Title"/> has been changed. 
		/// </summary>
		public event Action<TitleEventArgs> TitleChanged;
	}
}
