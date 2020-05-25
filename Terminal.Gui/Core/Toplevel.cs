//
// Toplevel.cs: Toplevel views can be modally executed
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.ComponentModel;

namespace Terminal.Gui {
	/// <summary>
	/// Toplevel views can be modally executed.
	/// </summary>
	/// <remarks>
	///   <para>
	///     Toplevels can be modally executing views, and they return control
	///     to the caller when the "Running" property is set to false, or
	///     by calling <see cref="M:Terminal.Gui.Application.RequestStop()"/>
	///   </para>
	///   <para>
	///     There will be a toplevel created for you on the first time use
	///     and can be accessed from the property <see cref="P:Terminal.Gui.Application.Top"/>,
	///     but new toplevels can be created and ran on top of it.   To run, create the
	///     toplevel and then invoke <see cref="M:Terminal.Gui.Application.Run"/> with the
	///     new toplevel.
	///   </para>
	///   <para>
	///     TopLevels can also opt-in to more sophisticated initialization
	///     by implementing <see cref="ISupportInitialize"/>. When they do
	///     so, the <see cref="ISupportInitialize.BeginInit"/> and
	///     <see cref="ISupportInitialize.EndInit"/> methods will be called
	///     before running the view.
	///     If first-run-only initialization is preferred, the <see cref="ISupportInitializeNotification"/>
	///     can be implemented too, in which case the <see cref="ISupportInitialize"/>
	///     methods will only be called if <see cref="ISupportInitializeNotification.IsInitialized"/>
	///     is <see langword="false"/>. This allows proper View inheritance hierarchies
	///     to override base class layout code optimally by doing so only on first run,
	///     instead of on every run.
	///   </para>
	/// </remarks>
	public class Toplevel : View {
		/// <summary>
		/// Gets or sets whether the Mainloop for this <see cref="Toplevel"/> is running or not. Setting
		/// this property to false will cause the MainLoop to exit. 
		/// </summary>
		public bool Running { get; set; }

		/// <summary>
		/// Fired once the Toplevel's MainLoop has started it's first iteration. 
		/// Subscribe to this event to perform tasks when the <see cref="Toplevel"/> has been laid out and focus has been set.
		/// changes. A Ready event handler is a good place to finalize initialization after calling `<see cref="Application.Run()"/>(topLevel)`. 
		/// </summary>
		public event EventHandler Ready;

		/// <summary>
		/// Called from Application.RunLoop after the <see cref="Toplevel"/> has entered it's first iteration of the loop. 
		/// </summary>
		internal virtual void OnReady ()
		{
			Ready?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Toplevel"/> class with the specified absolute layout.
		/// </summary>
		/// <param name="frame">Frame.</param>
		public Toplevel (Rect frame) : base (frame)
		{
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Toplevel"/> class with Computed layout, defaulting to <see langword="async"/> full screen.
		/// </summary>
		public Toplevel () : base ()
		{
			Initialize ();
			Width = Dim.Fill ();
			Height = Dim.Fill ();
		}

		void Initialize ()
		{
			ColorScheme = Colors.Base;
		}

		/// <summary>
		/// Convenience factory method that creates a new toplevel with the current terminal dimensions.
		/// </summary>
		/// <returns>The create.</returns>
		public static Toplevel Create ()
		{
			return new Toplevel (new Rect (0, 0, Driver.Cols, Driver.Rows));
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Toplevel"/> can focus.
		/// </summary>
		/// <value><c>true</c> if can focus; otherwise, <c>false</c>.</value>
		public override bool CanFocus {
			get => true;
		}

		/// <summary>
		/// Determines whether the <see cref="Toplevel"/> is modal or not.
		/// Causes <see cref="ProcessKey(KeyEvent)"/> to propagate keys upwards
		/// by default unless set to <see langword="true"/>.
		/// </summary>
		public bool Modal { get; set; }

		/// <summary>
		/// Check id current toplevel has menu bar
		/// </summary>
		public MenuBar MenuBar { get; set; }

		/// <summary>
		/// Check id current toplevel has status bar
		/// </summary>
		public StatusBar StatusBar { get; set; }

		///<inheritdoc cref="ProcessKey"/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (base.ProcessKey (keyEvent))
				return true;

			switch (keyEvent.Key) {
			case Key.ControlQ:
				// FIXED: stop current execution of this container
				Application.RequestStop ();
				break;
			case Key.ControlZ:
				Driver.Suspend ();
				return true;

#if false
			case Key.F5:
				Application.DebugDrawBounds = !Application.DebugDrawBounds;
				SetNeedsDisplay ();
				return true;
#endif
			case Key.Tab:
			case Key.CursorRight:
			case Key.CursorDown:
			case Key.ControlI: // Unix
				var old = Focused;
				if (!FocusNext ())
					FocusNext ();
				if (old != Focused) {
					old?.SetNeedsDisplay ();
					Focused?.SetNeedsDisplay ();
				}
				return true;
			case Key.CursorLeft:
			case Key.CursorUp:
			case Key.BackTab:
				old = Focused;
				if (!FocusPrev ())
					FocusPrev ();
				if (old != Focused) {
					old?.SetNeedsDisplay ();
					Focused?.SetNeedsDisplay ();
				}
				return true;

			case Key.ControlL:
				Application.Refresh ();
				return true;
			}
			return false;
		}

		///<inheritdoc cref="Add"/>
		public override void Add (View view)
		{
			if (this == Application.Top) {
				if (view is MenuBar)
					MenuBar = view as MenuBar;
				if (view is StatusBar)
					StatusBar = view as StatusBar;
			}
			base.Add (view);
		}

		///<inheritdoc cref="Remove"/>
		public override void Remove (View view)
		{
			if (this == Application.Top) {
				if (view is MenuBar)
					MenuBar = null;
				if (view is StatusBar)
					StatusBar = null;
			}
			base.Remove (view);
		}

		///<inheritdoc cref="RemoveAll"/>
		public override void RemoveAll ()
		{
			if (this == Application.Top) {
				MenuBar = null;
				StatusBar = null;
			}
			base.RemoveAll ();
		}

		internal void EnsureVisibleBounds (Toplevel top, int x, int y, out int nx, out int ny)
		{
			nx = Math.Max (x, 0);
			nx = nx + top.Frame.Width > Driver.Cols ? Math.Max (Driver.Cols - top.Frame.Width, 0) : nx;
			bool m, s;
			if (SuperView == null || SuperView.GetType () != typeof (Toplevel))
				m = Application.Top.MenuBar != null;
			else
				m = ((Toplevel)SuperView).MenuBar != null;
			int l = m ? 1 : 0;
			ny = Math.Max (y, l);
			if (SuperView == null || SuperView.GetType () != typeof (Toplevel))
				s = Application.Top.StatusBar != null;
			else
				s = ((Toplevel)SuperView).StatusBar != null;
			l = s ? Driver.Rows - 1 : Driver.Rows;
			ny = Math.Min (ny, l);
			ny = ny + top.Frame.Height > l ? Math.Max (l - top.Frame.Height, m ? 1 : 0) : ny;
		}

		internal void PositionToplevels ()
		{
			if (this != Application.Top) {
				EnsureVisibleBounds (this, Frame.X, Frame.Y, out int nx, out int ny);
				if ((nx != Frame.X || ny != Frame.Y) && LayoutStyle != LayoutStyle.Computed) {
					X = nx;
					Y = ny;
				}
			} else {
				foreach (var top in Subviews) {
					if (top is Toplevel) {
						EnsureVisibleBounds ((Toplevel)top, top.Frame.X, top.Frame.Y, out int nx, out int ny);
						if ((nx != top.Frame.X || ny != top.Frame.Y) && top.LayoutStyle != LayoutStyle.Computed) {
							top.X = nx;
							top.Y = ny;
						}
						if (StatusBar != null) {
							if (ny + top.Frame.Height > Driver.Rows - 1) {
								if (top.Height is Dim.DimFill)
									top.Height = Dim.Fill () - 1;
							}
							if (StatusBar.Frame.Y != Driver.Rows - 1) {
								StatusBar.Y = Driver.Rows - 1;
								SetNeedsDisplay ();
							}
						}
					}
				}
			}
		}

		///<inheritdoc cref="Redraw"/>
		public override void Redraw (Rect region)
		{
			Application.CurrentView = this;

			if (IsCurrentTop) {
				if (NeedDisplay != null && !NeedDisplay.IsEmpty) {
					Driver.SetAttribute (Colors.TopLevel.Normal);
					Clear (region);
					Driver.SetAttribute (Colors.Base.Normal);
				}
				foreach (var view in Subviews) {
					if (view.Frame.IntersectsWith (region)) {
						view.SetNeedsLayout ();
						view.SetNeedsDisplay (view.Bounds);
					}
				}

				ClearNeedsDisplay ();
			}

			base.Redraw (base.Bounds);
		}

		/// <summary>
		/// This method is invoked by Application.Begin as part of the Application.Run after
		/// the views have been laid out, and before the views are drawn for the first time.
		/// </summary>
		public virtual void WillPresent ()
		{
			FocusFirst ();
		}
	}
}
