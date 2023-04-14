using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// View is the base class for all views on the screen and represents a visible element that can render itself and 
	/// contains zero or more nested views, called SubViews.
	/// </summary>
	/// <remarks>
	/// <para>
	///    The View defines the base functionality for user interface elements in Terminal.Gui. Views
	///    can contain one or more subviews, can respond to user input and render themselves on the screen.
	/// </para>
	/// <para>
	///    Views that are focusable should implement the <see cref="PositionCursor"/> to make sure that
	///    the cursor is placed in a location that makes sense. Unix terminals do not have
	///    a way of hiding the cursor, so it can be distracting to have the cursor left at
	///    the last focused view. So views should make sure that they place the cursor
	///    in a visually sensible place.
	/// </para>
	/// <para>
	///     Views can also opt-in to more sophisticated initialization
	///     by implementing overrides to <see cref="ISupportInitialize.BeginInit"/> and
	///     <see cref="ISupportInitialize.EndInit"/> which will be called
	///     when the view is added to a <see cref="SuperView"/>. 
	/// </para>
	/// <para>
	///     If first-run-only initialization is preferred, overrides to <see cref="ISupportInitializeNotification"/>
	///     can be implemented, in which case the <see cref="ISupportInitialize"/>
	///     methods will only be called if <see cref="ISupportInitializeNotification.IsInitialized"/>
	///     is <see langword="false"/>. This allows proper <see cref="View"/> inheritance hierarchies
	///     to override base class layout code optimally by doing so only on first run,
	///     instead of on every run.
	///   </para>
	/// </remarks>
	public partial class View : Responder, ISupportInitializeNotification {

		#region Constructors and Initialization

		/// <summary>
		/// Initializes a new instance of a <see cref="Terminal.Gui.LayoutStyle.Absolute"/> <see cref="View"/> class with the absolute
		/// dimensions specified in the <paramref name="frame"/> parameter. 
		/// </summary>
		/// <param name="frame">The region covered by this view.</param>
		/// <remarks>
		/// This constructor initialize a View with a <see cref="LayoutStyle"/> of <see cref="Terminal.Gui.LayoutStyle.Absolute"/>.
		/// Use <see cref="View"/> to initialize a View with  <see cref="LayoutStyle"/> of <see cref="Terminal.Gui.LayoutStyle.Computed"/> 
		/// </remarks>
		public View (Rect frame) : this (frame, null) { }

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="Terminal.Gui.LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   Use <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties to dynamically control the size and location of the view.
		///   The <see cref="View"/> will be created using <see cref="Terminal.Gui.LayoutStyle.Computed"/>
		///   coordinates. The initial size (<see cref="View.Frame"/>) will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   If <see cref="Height"/> is greater than one, word wrapping is provided.
		/// </para>
		/// <para>
		///   This constructor initialize a View with a <see cref="LayoutStyle"/> of <see cref="Terminal.Gui.LayoutStyle.Computed"/>. 
		///   Use <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties to dynamically control the size and location of the view.
		/// </para>
		/// </remarks>
		public View () : this (text: string.Empty, direction: TextDirection.LeftRight_TopBottom) { }

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="Terminal.Gui.LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="View"/> will be created at the given
		///   coordinates with the given string. The size (<see cref="View.Frame"/>) will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   No line wrapping is provided.
		/// </para>
		/// </remarks>
		/// <param name="x">column to locate the View.</param>
		/// <param name="y">row to locate the View.</param>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		public View (int x, int y, ustring text) : this (TextFormatter.CalcRect (x, y, text), text) { }

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="Terminal.Gui.LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="View"/> will be created at the given
		///   coordinates with the given string. The initial size (<see cref="View.Frame"/>) will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   If <c>rect.Height</c> is greater than one, word wrapping is provided.
		/// </para>
		/// </remarks>
		/// <param name="rect">Location.</param>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		public View (Rect rect, ustring text)
		{
			SetInitialProperties (text, rect, LayoutStyle.Absolute, TextDirection.LeftRight_TopBottom);
		}

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="Terminal.Gui.LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="View"/> will be created using <see cref="Terminal.Gui.LayoutStyle.Computed"/>
		///   coordinates with the given string. The initial size (<see cref="View.Frame"/>) will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   If <see cref="Height"/> is greater than one, word wrapping is provided.
		/// </para>
		/// </remarks>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		/// <param name="direction">The text direction.</param>
		public View (ustring text, TextDirection direction = TextDirection.LeftRight_TopBottom)
		{
			SetInitialProperties (text, Rect.Empty, LayoutStyle.Computed, direction);
		}

		// TODO: v2 - Remove constructors with parameters
		/// <summary>
		/// Private helper to set the initial properties of the View that were provided via constructors.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="rect"></param>
		/// <param name="layoutStyle"></param>
		/// <param name="direction"></param>
		void SetInitialProperties (ustring text, Rect rect, LayoutStyle layoutStyle = LayoutStyle.Computed,
		    TextDirection direction = TextDirection.LeftRight_TopBottom)
		{
			TextFormatter = new TextFormatter ();
			TextFormatter.HotKeyChanged += TextFormatter_HotKeyChanged;
			TextDirection = direction;

			_shortcutHelper = new ShortcutHelper ();
			CanFocus = false;
			TabIndex = -1;
			TabStop = false;
			LayoutStyle = layoutStyle;

			Text = text == null ? ustring.Empty : text;
			LayoutStyle = layoutStyle;
			Frame = rect.IsEmpty ? TextFormatter.CalcRect (0, 0, text, direction) : rect;
			OnResizeNeeded ();

			CreateFrames ();

			LayoutFrames ();
		}

		/// <summary>
		/// Get or sets if  the <see cref="View"/> has been initialized (via <see cref="ISupportInitialize.BeginInit"/> 
		/// and <see cref="ISupportInitialize.EndInit"/>).
		/// </summary>
		/// <para>
		///     If first-run-only initialization is preferred, overrides to <see cref="ISupportInitializeNotification.IsInitialized"/>
		///     can be implemented, in which case the <see cref="ISupportInitialize"/>
		///     methods will only be called if <see cref="ISupportInitializeNotification.IsInitialized"/>
		///     is <see langword="false"/>. This allows proper <see cref="View"/> inheritance hierarchies
		///     to override base class layout code optimally by doing so only on first run,
		///     instead of on every run.
		///   </para>
		public virtual bool IsInitialized { get; set; }

		/// <summary>
		///  Signals the View that initialization is starting. See <see cref="ISupportInitialize"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		///     Views can opt-in to more sophisticated initialization
		///     by implementing overrides to <see cref="ISupportInitialize.BeginInit"/> and
		///     <see cref="ISupportInitialize.EndInit"/> which will be called
		///     when the view is added to a <see cref="SuperView"/>. 
		/// </para>
		/// <para>
		///     If first-run-only initialization is preferred, overrides to <see cref="ISupportInitializeNotification"/>
		///     can be implemented too, in which case the <see cref="ISupportInitialize"/>
		///     methods will only be called if <see cref="ISupportInitializeNotification.IsInitialized"/>
		///     is <see langword="false"/>. This allows proper <see cref="View"/> inheritance hierarchies
		///     to override base class layout code optimally by doing so only on first run,
		///     instead of on every run.
		///   </para>
		/// </remarks>
		public virtual void BeginInit ()
		{
			if (!IsInitialized) {
				_oldCanFocus = CanFocus;
				_oldTabIndex = _tabIndex;

				UpdateTextDirection (TextDirection);
				UpdateTextFormatterText ();
				SetHotKey ();

				// TODO: Figure out why ScrollView and other tests fail if this call is put here 
				// instead of the constructor.
				//InitializeFrames ();

			} else {
				//throw new InvalidOperationException ("The view is already initialized.");

			}

			if (_subviews?.Count > 0) {
				foreach (var view in _subviews) {
					if (!view.IsInitialized) {
						view.BeginInit ();
					}
				}
			}
		}

		/// <summary>
		///  Signals the View that initialization is ending. See <see cref="ISupportInitialize"/>.
		/// </summary>
		public void EndInit ()
		{
			IsInitialized = true;
			OnResizeNeeded ();
			if (_subviews != null) {
				foreach (var view in _subviews) {
					if (!view.IsInitialized) {
						view.EndInit ();
					}
				}
			}
			Initialized?.Invoke (this, EventArgs.Empty);
		}
		#endregion Constructors and Initialization

		/// <summary>
		/// Points to the current driver in use by the view, it is a convenience property
		/// for simplifying the development of new views.
		/// </summary>
		public static ConsoleDriver Driver => Application.Driver;

		/// <summary>
		/// Gets or sets arbitrary data for the view.
		/// </summary>
		/// <remarks>This property is not used internally.</remarks>
		public object Data { get; set; }

		/// <summary>
		/// Gets or sets an identifier for the view;
		/// </summary>
		/// <value>The identifier.</value>
		/// <remarks>The id should be unique across all Views that share a SuperView.</remarks>
		public string Id { get; set; } = "";

		ustring _title = ustring.Empty;
		/// <summary>
		/// The title to be displayed for this <see cref="View"/>. The title will be displayed if <see cref="Border"/>.<see cref="Thickness.Top"/>
		/// is greater than 0.
		/// </summary>
		/// <value>The title.</value>
		public ustring Title {
			get => _title;
			set {
				if (!OnTitleChanging (_title, value)) {
					var old = _title;
					_title = value;
					SetNeedsDisplay ();
#if DEBUG
					if (_title != null && string.IsNullOrEmpty (Id)) {
						Id = _title.ToString ();
					}
#endif // DEBUG
					OnTitleChanged (old, _title);
				}
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
			TitleChanging?.Invoke (this, args);
			return args.Cancel;
		}

		/// <summary>
		/// Event fired when the <see cref="View.Title"/> is changing. Set <see cref="TitleEventArgs.Cancel"/> to 
		/// `true` to cancel the Title change.
		/// </summary>
		public event EventHandler<TitleEventArgs> TitleChanging;

		/// <summary>
		/// Called when the <see cref="View.Title"/> has been changed. Invokes the <see cref="TitleChanged"/> event.
		/// </summary>
		/// <param name="oldTitle">The <see cref="View.Title"/> that is/has been replaced.</param>
		/// <param name="newTitle">The new <see cref="View.Title"/> to be replaced.</param>
		public virtual void OnTitleChanged (ustring oldTitle, ustring newTitle)
		{
			var args = new TitleEventArgs (oldTitle, newTitle);
			TitleChanged?.Invoke (this, args);
		}

		/// <summary>
		/// Event fired after the <see cref="View.Title"/> has been changed. 
		/// </summary>
		public event EventHandler<TitleEventArgs> TitleChanged;

		/// <summary>
		/// Event fired when the <see cref="Enabled"/> value is being changed.
		/// </summary>
		public event EventHandler EnabledChanged;

		/// <inheritdoc/>
		public override void OnEnabledChanged () => EnabledChanged?.Invoke (this, EventArgs.Empty);

		bool _oldEnabled;

		/// <inheritdoc/>
		public override bool Enabled {
			get => base.Enabled;
			set {
				if (base.Enabled != value) {
					if (value) {
						if (SuperView == null || SuperView?.Enabled == true) {
							base.Enabled = value;
						}
					} else {
						base.Enabled = value;
					}
					if (!value && HasFocus) {
						SetHasFocus (false, this);
					}
					OnEnabledChanged ();
					SetNeedsDisplay ();

					if (_subviews != null) {
						foreach (var view in _subviews) {
							if (!value) {
								view._oldEnabled = view.Enabled;
								view.Enabled = false;
							} else {
								view.Enabled = view._oldEnabled;
								view._addingView = false;
							}
						}
					}
				}
			}
		}
		
		/// <summary>
		/// Event fired when the <see cref="Visible"/> value is being changed.
		/// </summary>
		public event EventHandler VisibleChanged;

		/// <inheritdoc/>
		public override void OnVisibleChanged () => VisibleChanged?.Invoke (this, EventArgs.Empty);

		/// <summary>
		/// Gets or sets whether a view is cleared if the <see cref="Visible"/> property is <see langword="false"/>.
		/// </summary>
		public bool ClearOnVisibleFalse { get; set; } = true;

		/// <inheritdoc/>>
		public override bool Visible {
			get => base.Visible;
			set {
				if (base.Visible != value) {
					base.Visible = value;
					if (!value) {
						if (HasFocus) {
							SetHasFocus (false, this);
						}
						if (ClearOnVisibleFalse) {
							Clear ();
						}
					}
					OnVisibleChanged ();
					SetNeedsDisplay ();
				}
			}
		}

		bool CanBeVisible (View view)
		{
			if (!view.Visible) {
				return false;
			}
			for (var c = view.SuperView; c != null; c = c.SuperView) {
				if (!c.Visible) {
					return false;
				}
			}

			return true;
		}
		#region Super and Sub Views
		static readonly IList<View> _empty = new List<View> (0).AsReadOnly ();

		View _superView = null;
		/// <summary>
		/// Returns the container for this view, or null if this view has not been added to a container.
		/// </summary>
		/// <value>The super view.</value>
		public virtual View SuperView {
			get {
				return _superView;
			}
			set {
				throw new NotImplementedException ();
			}
		}

		List<View> _subviews; // This is null, and allocated on demand.
		/// <summary>
		/// This returns a list of the subviews contained by this view.
		/// </summary>
		/// <value>The subviews.</value>
		public IList<View> Subviews => _subviews?.AsReadOnly () ?? _empty;

		// Internally, we use InternalSubviews rather than subviews, as we do not expect us
		// to make the same mistakes our users make when they poke at the Subviews.
		internal IList<View> InternalSubviews => _subviews ?? _empty;

		/// <summary>
		/// Returns a value indicating if this View is currently on Top (Active)
		/// </summary>
		public bool IsCurrentTop => Application.Current == this;

		/// <summary>
		/// Event fired when this view is added to another.
		/// </summary>
		public event EventHandler<SuperViewChangedEventArgs> Added;

		internal bool _addingView;

		/// <summary>
		///   Adds a subview (child) to this view.
		/// </summary>
		/// <remarks>
		/// The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. 
		/// See also <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/> 
		/// </remarks>
		public virtual void Add (View view)
		{
			if (view == null) {
				return;
			}
			if (_subviews == null) {
				_subviews = new List<View> ();
			}
			if (_tabIndexes == null) {
				_tabIndexes = new List<View> ();
			}
			_subviews.Add (view);
			_tabIndexes.Add (view);
			view._superView = this;
			if (view.CanFocus) {
				_addingView = true;
				if (SuperView?.CanFocus == false) {
					SuperView._addingView = true;
					SuperView.CanFocus = true;
					SuperView._addingView = false;
				}
				CanFocus = true;
				view._tabIndex = _tabIndexes.IndexOf (view);
				_addingView = false;
			}
			if (view.Enabled && !Enabled) {
				view._oldEnabled = true;
				view.Enabled = false;
			}
			SetNeedsLayout ();
			SetNeedsDisplay ();

			OnAdded (new SuperViewChangedEventArgs (this, view));
			if (IsInitialized && !view.IsInitialized) {
				view.BeginInit ();
				view.EndInit ();
			}
		}

		/// <summary>
		/// Adds the specified views (children) to the view.
		/// </summary>
		/// <param name="views">Array of one or more views (can be optional parameter).</param>
		/// <remarks>
		/// The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. 
		/// See also <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/> 
		/// </remarks>
		public void Add (params View [] views)
		{
			if (views == null) {
				return;
			}
			foreach (var view in views) {
				Add (view);
			}
		}

		/// <summary>
		/// Method invoked when a subview is being added to this view.
		/// </summary>
		/// <param name="e">Event where <see cref="ViewEventArgs.View"/> is the subview being added.</param>
		public virtual void OnAdded (SuperViewChangedEventArgs e)
		{
			var view = e.Child;
			view.IsAdded = true;
			view.OnResizeNeeded ();
			view._x ??= view._frame.X;
			view._y ??= view._frame.Y;
			view._width ??= view._frame.Width;
			view._height ??= view._frame.Height;

			view.Added?.Invoke (this, e);
		}

		/// <summary>
		/// Gets information if the view was already added to the <see cref="SuperView"/>.
		/// </summary>
		public bool IsAdded { get; private set; }

		/// <summary>
		/// Event fired when this view is removed from another.
		/// </summary>
		public event EventHandler<SuperViewChangedEventArgs> Removed;

		/// <summary>
		///   Removes all subviews (children) added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		public virtual void RemoveAll ()
		{
			if (_subviews == null) {
				return;
			}

			while (_subviews.Count > 0) {
				Remove (_subviews [0]);
			}
		}

		/// <summary>
		///   Removes a subview added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Remove (View view)
		{
			if (view == null || _subviews == null) return;

			var touched = view.Frame;
			_subviews.Remove (view);
			_tabIndexes.Remove (view);
			view._superView = null;
			view._tabIndex = -1;
			SetNeedsLayout ();
			SetNeedsDisplay ();

			foreach (var v in _subviews) {
				if (v.Frame.IntersectsWith (touched))
					view.SetNeedsDisplay ();
			}
			OnRemoved (new SuperViewChangedEventArgs (this, view));
			if (_focused == view) {
				_focused = null;
			}
		}

		/// <summary>
		/// Method invoked when a subview is being removed from this view.
		/// </summary>
		/// <param name="e">Event args describing the subview being removed.</param>
		public virtual void OnRemoved (SuperViewChangedEventArgs e)
		{
			var view = e.Child;
			view.IsAdded = false;
			view.Removed?.Invoke (this, e);
		}


		void PerformActionForSubview (View subview, Action<View> action)
		{
			if (_subviews.Contains (subview)) {
				action (subview);
			}

			SetNeedsDisplay ();
			subview.SetNeedsDisplay ();
		}

		/// <summary>
		/// Brings the specified subview to the front so it is drawn on top of any other views.
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="SendSubviewToBack"/>.
		/// </remarks>
		public void BringSubviewToFront (View subview)
		{
			PerformActionForSubview (subview, x => {
				_subviews.Remove (x);
				_subviews.Add (x);
			});
		}

		/// <summary>
		/// Sends the specified subview to the front so it is the first view drawn
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="BringSubviewToFront(View)"/>.
		/// </remarks>
		public void SendSubviewToBack (View subview)
		{
			PerformActionForSubview (subview, x => {
				_subviews.Remove (x);
				_subviews.Insert (0, subview);
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void SendSubviewBackwards (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = _subviews.IndexOf (x);
				if (idx > 0) {
					_subviews.Remove (x);
					_subviews.Insert (idx - 1, x);
				}
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void BringSubviewForward (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = _subviews.IndexOf (x);
				if (idx + 1 < _subviews.Count) {
					_subviews.Remove (x);
					_subviews.Insert (idx + 1, x);
				}
			});
		}

		/// <summary>
		/// Get the top superview of a given <see cref="View"/>.
		/// </summary>
		/// <returns>The superview view.</returns>
		public View GetTopSuperView (View view = null, View superview = null)
		{
			View top = superview ?? Application.Top;
			for (var v = view?.SuperView ?? (this?.SuperView); v != null; v = v.SuperView) {
				top = v;
				if (top == superview) {
					break;
				}
			}

			return top;
		}



		#region Focus
		View _focused = null;

		internal enum Direction {
			Forward,
			Backward
		}

		/// <summary>
		/// Event fired when the view gets focus.
		/// </summary>
		public event EventHandler<FocusEventArgs> Enter;

		/// <summary>
		/// Event fired when the view looses focus.
		/// </summary>
		public event EventHandler<FocusEventArgs> Leave;

		Direction _focusDirection;
		internal Direction FocusDirection {
			get => SuperView?.FocusDirection ?? _focusDirection;
			set {
				if (SuperView != null)
					SuperView.FocusDirection = value;
				else
					_focusDirection = value;
			}
		}


		// BUGBUG: v2 - Seems weird that this is in View and not Responder.
		bool _hasFocus;

		/// <inheritdoc/>
		public override bool HasFocus => _hasFocus;
		
		void SetHasFocus (bool value, View view, bool force = false)
		{
			if (_hasFocus != value || force) {
				_hasFocus = value;
				if (value) {
					OnEnter (view);
				} else {
					OnLeave (view);
				}
				SetNeedsDisplay ();
			}

			// Remove focus down the chain of subviews if focus is removed
			if (!value && _focused != null) {
				var f = _focused;
				f.OnLeave (view);
				f.SetHasFocus (false, view);
				_focused = null;
			}
		}

		/// <summary>
		/// Event fired when the <see cref="CanFocus"/> value is being changed.
		/// </summary>
		public event EventHandler CanFocusChanged;

		/// <inheritdoc/>
		public override void OnCanFocusChanged () => CanFocusChanged?.Invoke (this, EventArgs.Empty);

		bool _oldCanFocus;
		/// <inheritdoc/>
		public override bool CanFocus {
			get => base.CanFocus;
			set {
				if (!_addingView && IsInitialized && SuperView?.CanFocus == false && value) {
					throw new InvalidOperationException ("Cannot set CanFocus to true if the SuperView CanFocus is false!");
				}
				if (base.CanFocus != value) {
					base.CanFocus = value;

					switch (value) {
					case false when _tabIndex > -1:
						TabIndex = -1;
						break;
					case true when SuperView?.CanFocus == false && _addingView:
						SuperView.CanFocus = true;
						break;
					}

					if (value && _tabIndex == -1) {
						TabIndex = SuperView != null ? SuperView._tabIndexes.IndexOf (this) : -1;
					}
					TabStop = value;

					if (!value && SuperView?.Focused == this) {
						SuperView._focused = null;
					}
					if (!value && HasFocus) {
						SetHasFocus (false, this);
						SuperView?.EnsureFocus ();
						if (SuperView != null && SuperView.Focused == null) {
							SuperView.FocusNext ();
							if (SuperView.Focused == null) {
								Application.Current.FocusNext ();
							}
							Application.EnsuresTopOnFront ();
						}
					}
					if (_subviews != null && IsInitialized) {
						foreach (var view in _subviews) {
							if (view.CanFocus != value) {
								if (!value) {
									view._oldCanFocus = view.CanFocus;
									view._oldTabIndex = view._tabIndex;
									view.CanFocus = false;
									view._tabIndex = -1;
								} else {
									if (_addingView) {
										view._addingView = true;
									}
									view.CanFocus = view._oldCanFocus;
									view._tabIndex = view._oldTabIndex;
									view._addingView = false;
								}
							}
						}
					}
					OnCanFocusChanged ();
					SetNeedsDisplay ();
				}
			}
		}


		/// <inheritdoc/>
		public override bool OnEnter (View view)
		{
			var args = new FocusEventArgs (view);
			Enter?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (base.OnEnter (view))
				return true;

			return false;
		}

		/// <inheritdoc/>
		public override bool OnLeave (View view)
		{
			var args = new FocusEventArgs (view);
			Leave?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (base.OnLeave (view))
				return true;

			return false;
		}

		/// <summary>
		/// Returns the currently focused view inside this view, or null if nothing is focused.
		/// </summary>
		/// <value>The focused.</value>
		public View Focused => _focused;

		/// <summary>
		/// Returns the most focused view in the chain of subviews (the leaf view that has the focus).
		/// </summary>
		/// <value>The most focused View.</value>
		public View MostFocused {
			get {
				if (Focused == null)
					return null;
				var most = Focused.MostFocused;
				if (most != null)
					return most;
				return Focused;
			}
		}

		/// <summary>
		/// Causes the specified subview to have focus.
		/// </summary>
		/// <param name="view">View.</param>
		void SetFocus (View view)
		{
			if (view == null) {
				return;
			}
			//Console.WriteLine ($"Request to focus {view}");
			if (!view.CanFocus || !view.Visible || !view.Enabled) {
				return;
			}
			if (_focused?._hasFocus == true && _focused == view) {
				return;
			}
			if ((_focused?._hasFocus == true && _focused?.SuperView == view) || view == this) {

				if (!view._hasFocus) {
					view._hasFocus = true;
				}
				return;
			}
			// Make sure that this view is a subview
			View c;
			for (c = view._superView; c != null; c = c._superView)
				if (c == this)
					break;
			if (c == null)
				throw new ArgumentException ("the specified view is not part of the hierarchy of this view");

			if (_focused != null)
				_focused.SetHasFocus (false, view);

			var f = _focused;
			_focused = view;
			_focused.SetHasFocus (true, f);
			_focused.EnsureFocus ();

			// Send focus upwards
			if (SuperView != null) {
				SuperView.SetFocus (this);
			} else {
				SetFocus (this);
			}
		}

		/// <summary>
		/// Causes the specified view and the entire parent hierarchy to have the focused order updated.
		/// </summary>
		public void SetFocus ()
		{
			if (!CanBeVisible (this) || !Enabled) {
				if (HasFocus) {
					SetHasFocus (false, this);
				}
				return;
			}

			if (SuperView != null) {
				SuperView.SetFocus (this);
			} else {
				SetFocus (this);
			}
		}

		/// <summary>
		/// Finds the first view in the hierarchy that wants to get the focus if nothing is currently focused, otherwise, does nothing.
		/// </summary>
		public void EnsureFocus ()
		{
			if (_focused == null && _subviews?.Count > 0) {
				if (FocusDirection == Direction.Forward) {
					FocusFirst ();
				} else {
					FocusLast ();
				}
			}
		}

		/// <summary>
		/// Focuses the first focusable subview if one exists.
		/// </summary>
		public void FocusFirst ()
		{
			if (!CanBeVisible (this)) {
				return;
			}

			if (_tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			foreach (var view in _tabIndexes) {
				if (view.CanFocus && view._tabStop && view.Visible && view.Enabled) {
					SetFocus (view);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the last focusable subview if one exists.
		/// </summary>
		public void FocusLast ()
		{
			if (!CanBeVisible (this)) {
				return;
			}

			if (_tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			for (var i = _tabIndexes.Count; i > 0;) {
				i--;

				var v = _tabIndexes [i];
				if (v.CanFocus && v._tabStop && v.Visible && v.Enabled) {
					SetFocus (v);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the previous view.
		/// </summary>
		/// <returns><see langword="true"/> if previous was focused, <see langword="false"/> otherwise.</returns>
		public bool FocusPrev ()
		{
			if (!CanBeVisible (this)) {
				return false;
			}

			FocusDirection = Direction.Backward;
			if (_tabIndexes == null || _tabIndexes.Count == 0)
				return false;

			if (_focused == null) {
				FocusLast ();
				return _focused != null;
			}

			var focusedIdx = -1;
			for (var i = _tabIndexes.Count; i > 0;) {
				i--;
				var w = _tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusPrev ())
						return true;
					focusedIdx = i;
					continue;
				}
				if (w.CanFocus && focusedIdx != -1 && w._tabStop && w.Visible && w.Enabled) {
					_focused.SetHasFocus (false, w);

					if (w.CanFocus && w._tabStop && w.Visible && w.Enabled)
						w.FocusLast ();

					SetFocus (w);
					return true;
				}
			}
			if (_focused != null) {
				_focused.SetHasFocus (false, this);
				_focused = null;
			}
			return false;
		}

		/// <summary>
		/// Focuses the next view.
		/// </summary>
		/// <returns><see langword="true"/> if next was focused, <see langword="false"/> otherwise.</returns>
		public bool FocusNext ()
		{
			if (!CanBeVisible (this)) {
				return false;
			}

			FocusDirection = Direction.Forward;
			if (_tabIndexes == null || _tabIndexes.Count == 0)
				return false;

			if (_focused == null) {
				FocusFirst ();
				return _focused != null;
			}
			var focusedIdx = -1;
			for (var i = 0; i < _tabIndexes.Count; i++) {
				var w = _tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusNext ())
						return true;
					focusedIdx = i;
					continue;
				}
				if (w.CanFocus && focusedIdx != -1 && w._tabStop && w.Visible && w.Enabled) {
					_focused.SetHasFocus (false, w);

					if (w.CanFocus && w._tabStop && w.Visible && w.Enabled)
						w.FocusFirst ();

					SetFocus (w);
					return true;
				}
			}
			if (_focused != null) {
				_focused.SetHasFocus (false, this);
				_focused = null;
			}
			return false;
		}

		View GetMostFocused (View view)
		{
			if (view == null) {
				return null;
			}

			return view._focused != null ? GetMostFocused (view._focused) : view;
		}

		/// <summary>
		///   Positions the cursor in the right position based on the currently focused view in the chain.
		/// </summary>
		///    Views that are focusable should override <see cref="PositionCursor"/> to ensure
		///    the cursor is placed in a location that makes sense. Unix terminals do not have
		///    a way of hiding the cursor, so it can be distracting to have the cursor left at
		///    the last focused view. Views should make sure that they place the cursor
		///    in a visually sensible place.
		public virtual void PositionCursor ()
		{
			if (!CanBeVisible (this) || !Enabled) {
				return;
			}

			// BUGBUG: v2 - This needs to support children of Frames too

			if (_focused == null && SuperView != null) {
				SuperView.EnsureFocus ();
			} else if (_focused?.Visible == true && _focused?.Enabled == true && _focused?.Frame.Width > 0 && _focused.Frame.Height > 0) {
				_focused.PositionCursor ();
			} else if (_focused?.Visible == true && _focused?.Enabled == false) {
				_focused = null;
			} else if (CanFocus && HasFocus && Visible && Frame.Width > 0 && Frame.Height > 0) {
				Move (TextFormatter.HotKeyPos == -1 ? 0 : TextFormatter.CursorPosition, 0);
			} else {
				Move (_frame.X, _frame.Y);
			}
		}
		#endregion Focus

		#endregion Super and Sub Views

		/// <summary>
		/// Pretty prints the View
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return $"{GetType ().Name}({Id})({Frame})";
		}

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			Margin?.Dispose ();
			Margin = null;
			Border?.Dispose ();
			Border = null;
			Padding?.Dispose ();
			Padding = null;

			for (var i = InternalSubviews.Count - 1; i >= 0; i--) {
				var subview = InternalSubviews [i];
				Remove (subview);
				subview.Dispose ();
			}
			base.Dispose (disposing);
		}
	}
}
