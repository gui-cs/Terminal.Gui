﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NStack;


namespace Terminal.Gui {
	/// <summary>
	/// Determines the LayoutStyle for a <see cref="View"/>, if Absolute, during <see cref="View.LayoutSubviews"/>, the
	/// value from the <see cref="View.Frame"/> will be used, if the value is Computed, then <see cref="View.Frame"/>
	/// will be updated from the X, Y <see cref="Pos"/> objects and the Width and Height <see cref="Dim"/> objects.
	/// </summary>
	public enum LayoutStyle {
		/// <summary>
		/// The position and size of the view are based <see cref="View.Frame"/>. 
		/// </summary>
		Absolute,

		/// <summary>
		/// The position and size of the view will be computed based on 
		/// <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/>. <see cref="View.Frame"/> will
		/// provide the absolute computed values.
		/// </summary>
		Computed
	}

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
	///    Views supports two layout styles: <see cref="LayoutStyle.Absolute"/> or <see cref="LayoutStyle.Computed"/>. 
	///    The choice as to which layout style is used by the View 
	///    is determined when the View is initialized. To create a View using Absolute layout, call a constructor that takes a
	///    Rect parameter to specify the absolute position and size (the View.<see cref="View.Frame "/>). To create a View 
	///    using Computed layout use a constructor that does not take a Rect parameter and set the X, Y, Width and Height 
	///    properties on the view. Both approaches use coordinates that are relative to the container they are being added to. 
	/// </para>
	/// <para>
	///    To switch between Absolute and Computed layout, use the <see cref="LayoutStyle"/> property. 
	/// </para>
	/// <para>
	///    Computed layout is more flexible and supports dynamic console apps where controls adjust layout
	///    as the terminal resizes or other Views change size or position. The X, Y, Width and Height 
	///    properties are Dim and Pos objects that dynamically update the position of a view.
	///    The X and Y properties are of type <see cref="Pos"/>
	///    and you can use either absolute positions, percentages or anchor
	///    points. The Width and Height properties are of type
	///    <see cref="Dim"/> and can use absolute position,
	///    percentages and anchors. These are useful as they will take
	///    care of repositioning views when view's frames are resized or
	///    if the terminal size changes.
	/// </para>
	/// <para>
	///    Absolute layout requires specifying coordinates and sizes of Views explicitly, and the
	///    View will typically stay in a fixed position and size. To change the position and size use the
	///    <see cref="Frame"/> property.
	/// </para>
	/// <para>
	///    Subviews (child views) can be added to a View by calling the <see cref="Add(View)"/> method. 
	///    The container of a View can be accessed with the <see cref="SuperView"/> property.
	/// </para>
	/// <para>
	///    To flag a region of the View's <see cref="Bounds"/> to be redrawn call <see cref="SetNeedsDisplay(Rect)"/>. 
	///    To flag the entire view for redraw call <see cref="SetNeedsDisplay()"/>.
	/// </para>
	/// <para>
	///    Views have a <see cref="ColorScheme"/> property that defines the default colors that subviews
	///    should use for rendering. This ensures that the views fit in the context where
	///    they are being used, and allows for themes to be plugged in. For example, the
	///    default colors for windows and toplevels uses a blue background, while it uses
	///    a white background for dialog boxes and a red background for errors.
	/// </para>
	/// <para>
	///    Subclasses should not rely on <see cref="ColorScheme"/> being
	///    set at construction time. If a <see cref="ColorScheme"/> is not set on a view, the view will inherit the
	///    value from its <see cref="SuperView"/> and the value might only be valid once a view has been
	///    added to a SuperView. 
	/// </para>
	/// <para>
	///    By using  <see cref="ColorScheme"/> applications will work both
	///    in color as well as black and white displays.
	/// </para>
	/// <para>
	///    Views that are focusable should implement the <see cref="PositionCursor"/> to make sure that
	///    the cursor is placed in a location that makes sense. Unix terminals do not have
	///    a way of hiding the cursor, so it can be distracting to have the cursor left at
	///    the last focused view. So views should make sure that they place the cursor
	///    in a visually sensible place.
	/// </para>
	/// <para>
	///    The <see cref="LayoutSubviews"/> method is invoked when the size or layout of a view has
	///    changed. The default processing system will keep the size and dimensions
	///    for views that use the <see cref="LayoutStyle.Absolute"/>, and will recompute the
	///    frames for the vies that use <see cref="LayoutStyle.Computed"/>.
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
		internal enum Direction {
			Forward,
			Backward
		}

		View _superView = null;
		View _focused = null;
		Direction _focusDirection;
		bool _autoSize;
		ShortcutHelper _shortcutHelper;

		/// <summary>
		/// Event fired when this view is added to another.
		/// </summary>
		public event EventHandler<SuperViewChangedEventArgs> Added;

		/// <summary>
		/// Event fired when this view is removed from another.
		/// </summary>
		public event EventHandler<SuperViewChangedEventArgs> Removed;

		/// <summary>
		/// Event fired when the view gets focus.
		/// </summary>
		public event EventHandler<FocusEventArgs> Enter;

		/// <summary>
		/// Event fired when the view looses focus.
		/// </summary>
		public event EventHandler<FocusEventArgs> Leave;

		/// <summary>
		/// Event fired when the view receives the mouse event for the first time.
		/// </summary>
		public event EventHandler<MouseEventEventArgs> MouseEnter;

		/// <summary>
		/// Event fired when the view receives a mouse event for the last time.
		/// </summary>
		public event EventHandler<MouseEventEventArgs> MouseLeave;

		/// <summary>
		/// Event fired when a mouse event is generated.
		/// </summary>
		public event EventHandler<MouseEventEventArgs> MouseClick;

		/// <summary>
		/// Event fired when the <see cref="CanFocus"/> value is being changed.
		/// </summary>
		public event EventHandler CanFocusChanged;

		/// <summary>
		/// Event fired when the <see cref="Enabled"/> value is being changed.
		/// </summary>
		public event EventHandler EnabledChanged;

		/// <summary>
		/// Event fired when the <see cref="Visible"/> value is being changed.
		/// </summary>
		public event EventHandler VisibleChanged;

		/// <summary>
		/// Event invoked when the <see cref="HotKey"/> is changed.
		/// </summary>
		public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

		Key _hotKey = Key.Null;

		/// <summary>
		/// Gets or sets the HotKey defined for this view. A user pressing HotKey on the keyboard while this view has focus will cause the Clicked event to fire.
		/// </summary>
		public virtual Key HotKey {
			get => _hotKey;
			set {
				if (_hotKey != value) {
					var v = value == Key.Unknown ? Key.Null : value;
					if (_hotKey != Key.Null && ContainsKeyBinding (Key.Space | _hotKey)) {
						if (v == Key.Null) {
							ClearKeybinding (Key.Space | _hotKey);
						} else {
							ReplaceKeyBinding (Key.Space | _hotKey, Key.Space | v);
						}
					} else if (v != Key.Null) {
						AddKeyBinding (Key.Space | v, Command.Accept);
					}
					_hotKey = TextFormatter.HotKey = v;
				}
			}
		}

		/// <summary>
		/// Gets or sets the specifier character for the hotkey (e.g. '_'). Set to '\xffff' to disable hotkey support for this View instance. The default is '\xffff'. 
		/// </summary>
		public virtual Rune HotKeySpecifier {
			get {
				if (TextFormatter != null) {
					return TextFormatter.HotKeySpecifier;
				} else {
					return new Rune ('\xFFFF');
				}
			}
			set {
				TextFormatter.HotKeySpecifier = value;
				SetHotKey ();
			}
		}

		/// <summary>
		/// This is the global setting that can be used as a global shortcut to invoke an action if provided.
		/// </summary>
		public Key Shortcut {
			get => _shortcutHelper.Shortcut;
			set {
				if (_shortcutHelper.Shortcut != value && (ShortcutHelper.PostShortcutValidation (value) || value == Key.Null)) {
					_shortcutHelper.Shortcut = value;
				}
			}
		}

		/// <summary>
		/// The keystroke combination used in the <see cref="Shortcut"/> as string.
		/// </summary>
		public ustring ShortcutTag => ShortcutHelper.GetShortcutTag (_shortcutHelper.Shortcut);

		/// <summary>
		/// The action to run if the <see cref="Shortcut"/> is defined.
		/// </summary>
		public virtual Action ShortcutAction { get; set; }

		/// <summary>
		/// Gets or sets arbitrary data for the view.
		/// </summary>
		/// <remarks>This property is not used internally.</remarks>
		public object Data { get; set; }

		internal Direction FocusDirection {
			get => SuperView?.FocusDirection ?? _focusDirection;
			set {
				if (SuperView != null)
					SuperView.FocusDirection = value;
				else
					_focusDirection = value;
			}
		}

		/// <summary>
		/// Points to the current driver in use by the view, it is a convenience property
		/// for simplifying the development of new views.
		/// </summary>
		public static ConsoleDriver Driver => Application.Driver;

		static readonly IList<View> _empty = new List<View> (0).AsReadOnly ();

		// This is null, and allocated on demand.
		List<View> _subviews;

		/// <summary>
		/// This returns a list of the subviews contained by this view.
		/// </summary>
		/// <value>The subviews.</value>
		public IList<View> Subviews => _subviews?.AsReadOnly () ?? _empty;

		// Internally, we use InternalSubviews rather than subviews, as we do not expect us
		// to make the same mistakes our users make when they poke at the Subviews.
		internal IList<View> InternalSubviews => _subviews ?? _empty;

		// This is null, and allocated on demand.
		List<View> _tabIndexes;

		/// <summary>
		/// Configurable keybindings supported by the control
		/// </summary>
		private Dictionary<Key, Command []> KeyBindings { get; set; } = new Dictionary<Key, Command []> ();
		private Dictionary<Command, Func<bool?>> CommandImplementations { get; set; } = new Dictionary<Command, Func<bool?>> ();

		/// <summary>
		/// This returns a tab index list of the subviews contained by this view.
		/// </summary>
		/// <value>The tabIndexes.</value>
		public IList<View> TabIndexes => _tabIndexes?.AsReadOnly () ?? _empty;

		int _tabIndex = -1;

		/// <summary>
		/// Indicates the index of the current <see cref="View"/> from the <see cref="TabIndexes"/> list.
		/// </summary>
		public int TabIndex {
			get { return _tabIndex; }
			set {
				if (!CanFocus) {
					_tabIndex = -1;
					return;
				} else if (SuperView?._tabIndexes == null || SuperView?._tabIndexes.Count == 1) {
					_tabIndex = 0;
					return;
				} else if (_tabIndex == value) {
					return;
				}
				_tabIndex = value > SuperView._tabIndexes.Count - 1 ? SuperView._tabIndexes.Count - 1 : value < 0 ? 0 : value;
				_tabIndex = GetTabIndex (_tabIndex);
				if (SuperView._tabIndexes.IndexOf (this) != _tabIndex) {
					SuperView._tabIndexes.Remove (this);
					SuperView._tabIndexes.Insert (_tabIndex, this);
					SetTabIndex ();
				}
			}
		}

		int GetTabIndex (int idx)
		{
			var i = 0;
			foreach (var v in SuperView._tabIndexes) {
				if (v._tabIndex == -1 || v == this) {
					continue;
				}
				i++;
			}
			return Math.Min (i, idx);
		}

		void SetTabIndex ()
		{
			var i = 0;
			foreach (var v in SuperView._tabIndexes) {
				if (v._tabIndex == -1) {
					continue;
				}
				v._tabIndex = i;
				i++;
			}
		}

		bool _tabStop = true;

		/// <summary>
		/// This only be <see langword="true"/> if the <see cref="CanFocus"/> is also <see langword="true"/> 
		/// and the focus can be avoided by setting this to <see langword="false"/>
		/// </summary>
		public bool TabStop {
			get => _tabStop;
			set {
				if (_tabStop == value) {
					return;
				}
				_tabStop = CanFocus && value;
			}
		}

		bool _oldCanFocus;
		int _oldTabIndex;

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

		// The frame for the object. Superview relative.
		Rect _frame;

		/// <summary>
		/// Gets or sets an identifier for the view;
		/// </summary>
		/// <value>The identifier.</value>
		/// <remarks>The id should be unique across all Views that share a SuperView.</remarks>
		public string Id { get; set; } = "";

		/// <summary>
		/// Returns a value indicating if this View is currently on Top (Active)
		/// </summary>
		public bool IsCurrentTop => Application.Current == this;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> wants mouse position reports.
		/// </summary>
		/// <value><see langword="true"/> if want mouse position reports; otherwise, <see langword="false"/>.</value>
		public virtual bool WantMousePositionReports { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> want continuous button pressed event.
		/// </summary>
		public virtual bool WantContinuousButtonPressed { get; set; }

		/// <summary>
		/// Gets or sets the frame for the view. The frame is relative to the view's container (<see cref="SuperView"/>).
		/// </summary>
		/// <value>The frame.</value>
		/// <remarks>
		/// <para>
		///    Change the Frame when using the <see cref="Terminal.Gui.LayoutStyle.Absolute"/> layout style to move or resize views. 
		/// </para>
		/// <para>
		///    Altering the Frame of a view will trigger the redrawing of the
		///    view as well as the redrawing of the affected regions of the <see cref="SuperView"/>.
		/// </para>
		/// </remarks>
		public virtual Rect Frame {
			get => _frame;
			set {
				_frame = new Rect (value.X, value.Y, Math.Max (value.Width, 0), Math.Max (value.Height, 0));
				if (IsInitialized || LayoutStyle == LayoutStyle.Absolute) {
					LayoutFrames ();
					TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
					SetNeedsLayout ();
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// The Thickness that separates a View from other SubViews of the same SuperView. 
		/// The Margin is not part of the View's content and is not clipped by the View's Clip Area. 
		/// </summary>
		public Frame Margin { get; private set; }

		/// <summary>
		///  Thickness where a visual border (drawn using line-drawing glyphs) and the Title are drawn. 
		///  The Border expands inward; in other words if `Border.Thickness.Top == 2` the border and 
		///  title will take up the first row and the second row will be filled with spaces. 
		///  The Border is not part of the View's content and is not clipped by the View's `ClipArea`.
		/// </summary>
		/// <remarks>
		/// <see cref="BorderStyle"/> provides a simple helper for turning a simple border frame on or off.
		/// </remarks>
		public Frame Border { get; private set; }

		/// <summary>
		/// Means the Thickness inside of an element that offsets the `Content` from the Border. 
		/// Padding is `{0, 0, 0, 0}` by default. Padding is not part of the View's content and is not clipped by the View's `ClipArea`.
		/// </summary>
		/// <remarks>
		/// (NOTE: in v1 `Padding` is OUTSIDE of the `Border`). 
		/// </remarks>
		public Frame Padding { get; private set; }

		/// <summary>
		/// Helper to get the total thickness of the <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>. 
		/// </summary>
		/// <returns>A thickness that describes the sum of the Frames' thicknesses.</returns>
		public Thickness GetFramesThickness ()
		{
			var left = Margin.Thickness.Left + Border.Thickness.Left + Padding.Thickness.Left;
			var top = Margin.Thickness.Top + Border.Thickness.Top + Padding.Thickness.Top;
			var right = Margin.Thickness.Right + Border.Thickness.Right + Padding.Thickness.Right;
			var bottom = Margin.Thickness.Bottom + Border.Thickness.Bottom + Padding.Thickness.Bottom;
			return new Thickness (left, top, right, bottom);
		}

		/// <summary>
		/// Helper to get the X and Y offset of the Bounds from the Frame. This is the sum of the Left and Top properties of
		/// <see cref="Margin"/>, <see cref="Border"/> and <see cref="Padding"/>.
		/// </summary>
		public Point GetBoundsOffset () => new Point (Padding?.Thickness.GetInside (Padding.Frame).X ?? 0, Padding?.Thickness.GetInside (Padding.Frame).Y ?? 0);

		/// <summary>
		/// Creates the view's <see cref="Frame"/> objects. This internal method is overridden by Frame to do nothing
		/// to prevent recursion during View construction.
		/// </summary>
		internal virtual void CreateFrames ()
		{
			void ThicknessChangedHandler (object sender, EventArgs e)
			{
				LayoutFrames ();
				SetNeedsLayout ();
				SetNeedsDisplay ();
			}

			if (Margin != null) {
				Margin.ThicknessChanged -= ThicknessChangedHandler;
				Margin.Dispose ();
			}
			Margin = new Frame () { Id = "Margin", Thickness = new Thickness (0) };
			Margin.ThicknessChanged += ThicknessChangedHandler;
			Margin.Parent = this;

			if (Border != null) {
				Border.ThicknessChanged -= ThicknessChangedHandler;
				Border.Dispose ();
			}
			Border = new Frame () { Id = "Border", Thickness = new Thickness (0) };
			Border.ThicknessChanged += ThicknessChangedHandler;
			Border.Parent = this;

			// TODO: Create View.AddAdornment

			if (Padding != null) {
				Padding.ThicknessChanged -= ThicknessChangedHandler;
				Padding.Dispose ();
			}
			Padding = new Frame () { Id = "Padding", Thickness = new Thickness (0) };
			Padding.ThicknessChanged += ThicknessChangedHandler;
			Padding.Parent = this;
		}

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


		LayoutStyle _layoutStyle;

		/// <summary>
		/// Controls how the View's <see cref="Frame"/> is computed during the LayoutSubviews method, if the style is set to
		/// <see cref="Terminal.Gui.LayoutStyle.Absolute"/>, 
		/// LayoutSubviews does not change the <see cref="Frame"/>. If the style is <see cref="Terminal.Gui.LayoutStyle.Computed"/>
		/// the <see cref="Frame"/> is updated using
		/// the <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties.
		/// </summary>
		/// <value>The layout style.</value>
		public LayoutStyle LayoutStyle {
			get => _layoutStyle;
			set {
				_layoutStyle = value;
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// The View-relative rectangle where View content is displayed. SubViews are positioned relative to 
		/// Bounds.<see cref="Rect.Location">Location</see> (which is always (0, 0)) and <see cref="Redraw(Rect)"/> clips drawing to 
		/// Bounds.<see cref="Rect.Size">Size</see>.
		/// </summary>
		/// <value>The bounds.</value>
		/// <remarks>
		/// <para>
		/// The <see cref="Rect.Location"/> of Bounds is always (0, 0). To obtain the offset of the Bounds from the Frame use 
		/// <see cref="GetBoundsOffset"/>.
		/// </para>
		/// </remarks>
		public virtual Rect Bounds {
			get {
#if DEBUG
				if (LayoutStyle == LayoutStyle.Computed && !IsInitialized) {
					Debug.WriteLine ($"WARNING: Bounds is being accessed before the View has been initialized. This is likely a bug. View: {this}");
				}
#endif // DEBUG
				var frameRelativeBounds = Padding?.Thickness.GetInside (Padding.Frame) ?? new Rect (default, Frame.Size);
				return new Rect (default, frameRelativeBounds.Size);
			}
			set {
				// BUGBUG: Margin etc.. can be null (if typeof(Frame))
				Frame = new Rect (Frame.Location,
					new Size (
						value.Size.Width + Margin.Thickness.Horizontal + Border.Thickness.Horizontal + Padding.Thickness.Horizontal,
						value.Size.Height + Margin.Thickness.Vertical + Border.Thickness.Vertical + Padding.Thickness.Vertical
						)
					);
			}
		}

		// Diagnostics to highlight when X or Y is read before the view has been initialized
		private Pos VerifyIsIntialized (Pos pos)
		{
#if DEBUG
			if (LayoutStyle == LayoutStyle.Computed && (!IsInitialized)) {
				Debug.WriteLine ($"WARNING: \"{this}\" has not been initialized; position is indeterminate {pos}. This is likely a bug.");
			}
#endif // DEBUG
			return pos;
		}

		// Diagnostics to highlight when Width or Height is read before the view has been initialized
		private Dim VerifyIsIntialized (Dim dim)
		{
#if DEBUG
			if (LayoutStyle == LayoutStyle.Computed && (!IsInitialized)) {
				Debug.WriteLine ($"WARNING: \"{this}\" has not been initialized; dimension is indeterminate: {dim}. This is likely a bug.");
			}
#endif // DEBUG		
			return dim;
		}

		Pos _x, _y;

		/// <summary>
		/// Gets or sets the X position for the view (the column). Only used if the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The X Position.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Pos X {
			get => VerifyIsIntialized (_x);
			set {
				if (ForceValidatePosDim && !ValidatePosDim (_x, value)) {
					throw new ArgumentException ();
				}

				_x = value;

				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets the Y position for the view (the row). Only used if the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The y position (line).</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Pos Y {
			get => VerifyIsIntialized (_y);
			set {
				if (ForceValidatePosDim && !ValidatePosDim (_y, value)) {
					throw new ArgumentException ();
				}

				_y = value;

				OnResizeNeeded ();
			}
		}
		Dim _width, _height;

		/// <summary>
		/// Gets or sets the width of the view. Only used the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The width.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Dim Width {
			get => VerifyIsIntialized (_width);
			set {
				if (ForceValidatePosDim && !ValidatePosDim (_width, value)) {
					throw new ArgumentException ("ForceValidatePosDim is enabled", nameof (Width));
				}

				_width = value;

				if (ForceValidatePosDim) {
					var isValidNewAutSize = AutoSize && IsValidAutoSizeWidth (_width);

					if (IsAdded && AutoSize && !isValidNewAutSize) {
						throw new InvalidOperationException ("Must set AutoSize to false before set the Width.");
					}
				}
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets the height of the view. Only used the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The height.</value>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		public Dim Height {
			get => VerifyIsIntialized (_height);
			set {
				if (ForceValidatePosDim && !ValidatePosDim (_height, value)) {
					throw new ArgumentException ("ForceValidatePosDim is enabled", nameof (Height));
				}

				_height = value;

				if (ForceValidatePosDim) {
					var isValidNewAutSize = AutoSize && IsValidAutoSizeHeight (_height);

					if (IsAdded && AutoSize && !isValidNewAutSize) {
						throw new InvalidOperationException ("Must set AutoSize to false before set the Height.");
					}
				}
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Forces validation with <see cref="Terminal.Gui.LayoutStyle.Computed"/> layout
		///  to avoid breaking the <see cref="Pos"/> and <see cref="Dim"/> settings.
		/// </summary>
		public bool ForceValidatePosDim { get; set; }

		bool ValidatePosDim (object oldValue, object newValue)
		{
			if (!IsInitialized || _layoutStyle == LayoutStyle.Absolute || oldValue == null || oldValue.GetType () == newValue.GetType () || this is Toplevel) {
				return true;
			}
			if (_layoutStyle == LayoutStyle.Computed) {
				if (oldValue.GetType () != newValue.GetType () && !(newValue is Pos.PosAbsolute || newValue is Dim.DimAbsolute)) {
					return true;
				}
			}
			return false;
		}

		// BUGBUG: This API is broken - It should be renamed to `GetMinimumBoundsForFrame and 
		// should not assume Frame.Height == Bounds.Height
		/// <summary>
		/// Gets the minimum dimensions required to fit the View's <see cref="Text"/>, factoring in <see cref="TextDirection"/>.
		/// </summary>
		/// <param name="size">The minimum dimensions required.</param>
		/// <returns><see langword="true"/> if the dimensions fit within the View's <see cref="Bounds"/>, <see langword="false"/> otherwise.</returns>
		/// <remarks>
		/// Always returns <see langword="false"/> if <see cref="AutoSize"/> is <see langword="true"/> or
		/// if <see cref="Height"/> (Horizontal) or <see cref="Width"/> (Vertical) are not not set or zero.
		/// Does not take into account word wrapping.
		/// </remarks>
		public bool GetMinimumBounds (out Size size)
		{
			size = Bounds.Size;

			if (!AutoSize && !ustring.IsNullOrEmpty (TextFormatter.Text)) {
				switch (TextFormatter.IsVerticalDirection (TextDirection)) {
				case true:
					var colWidth = TextFormatter.GetSumMaxCharWidth (new List<ustring> { TextFormatter.Text }, 0, 1);
					// TODO: v2 - This uses frame.Width; it should only use Bounds
					if (_frame.Width < colWidth &&
						(Width == null ||
							(Bounds.Width >= 0 &&
								Width is Dim.DimAbsolute &&
								Width.Anchor (0) >= 0 &&
								Width.Anchor (0) < colWidth))) {
						size = new Size (colWidth, Bounds.Height);
						return true;
					}
					break;
				default:
					if (_frame.Height < 1 &&
						(Height == null ||
							(Height is Dim.DimAbsolute &&
								Height.Anchor (0) == 0))) {
						size = new Size (Bounds.Width, 1);
						return true;
					}
					break;
				}
			}
			return false;
		}

		// BUGBUG - v2 - Should be renamed "SetBoundsToFitFrame"
		/// <summary>
		/// Sets the size of the View to the minimum width or height required to fit <see cref="Text"/> (see <see cref="GetMinimumBounds(out Size)"/>.
		/// </summary>
		/// <returns><see langword="true"/> if the size was changed, <see langword="false"/> if <see cref="Text"/>
		/// will not fit.</returns>
		public bool SetMinWidthHeight ()
		{
			if (GetMinimumBounds (out Size size)) {
				_frame = new Rect (_frame.Location, size);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets or sets the <see cref="Gui.TextFormatter"/> used to format <see cref="Text"/>.
		/// </summary>
		public TextFormatter TextFormatter { get; set; }

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
		/// Can be overridden if the <see cref="Terminal.Gui.TextFormatter.Text"/> has
		///  different format than the default.
		/// </summary>
		protected virtual void UpdateTextFormatterText ()
		{
			if (TextFormatter != null) {
				TextFormatter.Text = _text;
			}
		}

		/// <summary>
		/// Called whenever the view needs to be resized. Sets <see cref="Frame"/> and
		/// triggers a <see cref="LayoutSubviews()"/> call.		/// 
		/// </summary>
		/// <remarks>
		/// Can be overridden if the view resize behavior is different than the default.
		/// </remarks>
		protected virtual void OnResizeNeeded ()
		{
			var actX = _x is Pos.PosAbsolute ? _x.Anchor (0) : _frame.X;
			var actY = _y is Pos.PosAbsolute ? _y.Anchor (0) : _frame.Y;

			if (AutoSize) {
				//if (TextAlignment == TextAlignment.Justified) {
				//	throw new InvalidOperationException ("TextAlignment.Justified cannot be used with AutoSize");
				//}
				var s = GetAutoSize ();
				var w = _width is Dim.DimAbsolute && _width.Anchor (0) > s.Width ? _width.Anchor (0) : s.Width;
				var h = _height is Dim.DimAbsolute && _height.Anchor (0) > s.Height ? _height.Anchor (0) : s.Height;
				_frame = new Rect (new Point (actX, actY), new Size (w, h)); // Set frame, not Frame!
			} else {
				var w = _width is Dim.DimAbsolute ? _width.Anchor (0) : _frame.Width;
				var h = _height is Dim.DimAbsolute ? _height.Anchor (0) : _frame.Height;
				// BUGBUG: v2 - ? - If layoutstyle is absolute, this overwrites the current frame h/w with 0. Hmmm...
				// This is needed for DimAbsolute values by setting the frame before LayoutSubViews.
				_frame = new Rect (new Point (actX, actY), new Size (w, h)); // Set frame, not Frame!
			}
			//// BUGBUG: I think these calls are redundant or should be moved into just the AutoSize case
			if (IsInitialized || LayoutStyle == LayoutStyle.Absolute) {
				SetMinWidthHeight ();
				LayoutFrames ();
				TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
				SetNeedsLayout ();
				SetNeedsDisplay ();
			}
		}

		void TextFormatter_HotKeyChanged (object sender, KeyChangedEventArgs e)
		{
			HotKeyChanged?.Invoke (this, e);
		}

		internal bool LayoutNeeded { get; private set; } = true;

		internal void SetNeedsLayout ()
		{
			if (LayoutNeeded)
				return;
			LayoutNeeded = true;
			if (SuperView == null)
				return;
			SuperView.SetNeedsLayout ();
			foreach (var view in Subviews) {
				view.SetNeedsLayout ();
			}
			TextFormatter.NeedsFormat = true;
		}

		/// <summary>
		/// Removes the <see cref="SetNeedsLayout"/> setting on this view.
		/// </summary>
		protected void ClearLayoutNeeded ()
		{
			LayoutNeeded = false;
		}

		// The view-relative region that needs to be redrawn
		internal Rect _needsDisplay { get; private set; } = Rect.Empty;

		/// <summary>
		/// Sets a flag indicating this view needs to be redisplayed because its state has changed.
		/// </summary>
		public void SetNeedsDisplay ()
		{
			if (!IsInitialized) return;
			SetNeedsDisplay (Bounds);
		}

		/// <summary>
		/// Flags the view-relative region on this View as needing to be redrawn.
		/// </summary>
		/// <param name="region">The view-relative region that needs to be redrawn.</param>
		public void SetNeedsDisplay (Rect region)
		{
			if (_needsDisplay.IsEmpty)
				_needsDisplay = region;
			else {
				var x = Math.Min (_needsDisplay.X, region.X);
				var y = Math.Min (_needsDisplay.Y, region.Y);
				var w = Math.Max (_needsDisplay.Width, region.Width);
				var h = Math.Max (_needsDisplay.Height, region.Height);
				_needsDisplay = new Rect (x, y, w, h);
			}
			_superView?.SetSubViewNeedsDisplay ();

			if (_subviews == null)
				return;

			foreach (var view in _subviews)
				if (view.Frame.IntersectsWith (region)) {
					var childRegion = Rect.Intersect (view.Frame, region);
					childRegion.X -= view.Frame.X;
					childRegion.Y -= view.Frame.Y;
					view.SetNeedsDisplay (childRegion);
				}
		}

		private Rect GetNeedsDisplayRectScreen (Rect containerBounds)
		{
			Rect rect = ViewToScreen (_needsDisplay);
			if (!containerBounds.IsEmpty) {
				rect.Width = Math.Min (_needsDisplay.Width, containerBounds.Width);
				rect.Height = Math.Min (_needsDisplay.Height, containerBounds.Height);
			}

			return rect;
		}


		internal bool _childNeedsDisplay { get; private set; }

		/// <summary>
		/// Indicates that any Subviews (in the <see cref="Subviews"/> list) need to be repainted.
		/// </summary>
		public void SetSubViewNeedsDisplay ()
		{
			if (_childNeedsDisplay) {
				return;
			}
			_childNeedsDisplay = true;
			if (_superView != null && !_superView._childNeedsDisplay)
				_superView.SetSubViewNeedsDisplay ();
		}

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
		///   Clears the view region with the current color.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This clears the entire region used by this view.
		///   </para>
		/// </remarks>
		public void Clear ()
		{
			var h = Frame.Height;
			var w = Frame.Width;
			for (var line = 0; line < h; line++) {
				Move (0, line);
				for (var col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
		}


		// BUGBUG: Stupid that this takes screen-relative. We should have a tenet that says 
		// "View APIs only deal with View-relative coords". 
		/// <summary>
		///   Clears the specified region with the current color. 
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="regionScreen">The screen-relative region to clear.</param>
		public void Clear (Rect regionScreen)
		{
			var h = regionScreen.Height;
			var w = regionScreen.Width;
			for (var line = regionScreen.Y; line < regionScreen.Y + h; line++) {
				Driver.Move (regionScreen.X, line);
				for (var col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
		}

		/// <summary>
		/// Converts a point from screen-relative coordinates to view-relative coordinates.
		/// </summary>
		/// <returns>The mapped point.</returns>
		/// <param name="x">X screen-coordinate point.</param>
		/// <param name="y">Y screen-coordinate point.</param>
		public Point ScreenToView (int x, int y)
		{
			if (SuperView == null) {
				return new Point (x - Frame.X, y - _frame.Y);
			} else {
				var parent = SuperView.ScreenToView (x, y);
				return new Point (parent.X - _frame.X, parent.Y - _frame.Y);
			}
		}

		/// <summary>
		/// Converts a point from screen-relative coordinates to bounds-relative coordinates.
		/// </summary>
		/// <returns>The mapped point.</returns>
		/// <param name="x">X screen-coordinate point.</param>
		/// <param name="y">Y screen-coordinate point.</param>
		public Point ScreenToBounds (int x, int y)
		{
			if (SuperView == null) {
				var boundsOffset = GetBoundsOffset ();
				return new Point (x - Frame.X + boundsOffset.X, y - Frame.Y + boundsOffset.Y);
			} else {
				var parent = SuperView.ScreenToView (x, y);
				return new Point (parent.X - _frame.X, parent.Y - _frame.Y);
			}
		}

		/// <summary>
		/// Converts a view-relative location to a screen-relative location (col,row). The output is optionally clamped to the screen dimensions.
		/// </summary>
		/// <param name="col">View-relative column.</param>
		/// <param name="row">View-relative row.</param>
		/// <param name="rcol">Absolute column; screen-relative.</param>
		/// <param name="rrow">Absolute row; screen-relative.</param>
		/// <param name="clamped">If <see langword="true"/>, <paramref name="rcol"/> and <paramref name="rrow"/> will be clamped to the 
		/// screen dimensions (they never be negative and will always be less than to <see cref="ConsoleDriver.Cols"/> and
		/// <see cref="ConsoleDriver.Rows"/>, respectively.</param>
		public virtual void ViewToScreen (int col, int row, out int rcol, out int rrow, bool clamped = true)
		{
			var boundsOffset = GetBoundsOffset ();
			rcol = col + Frame.X + boundsOffset.X;
			rrow = row + Frame.Y + boundsOffset.Y;

			var super = SuperView;
			while (super != null) {
				boundsOffset = super.GetBoundsOffset ();
				rcol += super.Frame.X + boundsOffset.X;
				rrow += super.Frame.Y + boundsOffset.Y;
				super = super.SuperView;
			}

			// The following ensures that the cursor is always in the screen boundaries.
			if (clamped) {
				rrow = Math.Min (rrow, Driver.Rows - 1);
				rcol = Math.Min (rcol, Driver.Cols - 1);
			}
		}

		/// <summary>
		/// Converts a region in view-relative coordinates to screen-relative coordinates.
		/// </summary>
		internal Rect ViewToScreen (Rect region)
		{
			ViewToScreen (region.X, region.Y, out var x, out var y, clamped: false);
			return new Rect (x, y, region.Width, region.Height);
		}

		// Clips a rectangle in screen coordinates to the dimensions currently available on the screen
		internal Rect ScreenClip (Rect regionScreen)
		{
			var x = regionScreen.X < 0 ? 0 : regionScreen.X;
			var y = regionScreen.Y < 0 ? 0 : regionScreen.Y;
			var w = regionScreen.X + regionScreen.Width >= Driver.Cols ? Driver.Cols - regionScreen.X : regionScreen.Width;
			var h = regionScreen.Y + regionScreen.Height >= Driver.Rows ? Driver.Rows - regionScreen.Y : regionScreen.Height;

			return new Rect (x, y, w, h);
		}

		/// <summary>
		/// Sets the <see cref="ConsoleDriver"/>'s clip region to <see cref="Bounds"/>.
		/// </summary>
		/// <returns>The current screen-relative clip region, which can be then re-applied by setting <see cref="ConsoleDriver.Clip"/>.</returns>
		/// <remarks>
		/// <para>
		/// <see cref="Bounds"/> is View-relative.
		/// </para>
		/// <para>
		/// If <see cref="ConsoleDriver.Clip"/> and <see cref="Bounds"/> do not intersect, the clip region will be set to <see cref="Rect.Empty"/>.
		/// </para>
		/// </remarks>
		public Rect ClipToBounds ()
		{
			var clip = Bounds;


			return SetClip (clip);
		}

		// BUGBUG: v2 - SetClip should return VIEW-relative so that it can be used to reset it; using Driver.Clip directly should not be necessary. 
		/// <summary>
		/// Sets the clip region to the specified view-relative region.
		/// </summary>
		/// <returns>The current screen-relative clip region, which can be then re-applied by setting <see cref="ConsoleDriver.Clip"/>.</returns>
		/// <param name="region">View-relative clip region.</param>
		/// <remarks>
		/// If <see cref="ConsoleDriver.Clip"/> and <paramref name="region"/> do not intersect, the clip region will be set to <see cref="Rect.Empty"/>.
		/// </remarks>
		public Rect SetClip (Rect region)
		{
			var previous = Driver.Clip;
			Driver.Clip = Rect.Intersect (previous, ViewToScreen (region));
			return previous;
		}

		/// <summary>
		/// Utility function to draw strings that contain a hotkey.
		/// </summary>
		/// <param name="text">String to display, the hotkey specifier before a letter flags the next letter as the hotkey.</param>
		/// <param name="hotColor">Hot color.</param>
		/// <param name="normalColor">Normal color.</param>
		/// <remarks>
		/// <para>The hotkey is any character following the hotkey specifier, which is the underscore ('_') character by default.</para>
		/// <para>The hotkey specifier can be changed via <see cref="HotKeySpecifier"/></para>
		/// </remarks>
		public void DrawHotString (ustring text, Attribute hotColor, Attribute normalColor)
		{
			var hotkeySpec = HotKeySpecifier == (Rune)0xffff ? (Rune)'_' : HotKeySpecifier;
			Application.Driver.SetAttribute (normalColor);
			foreach (var rune in text) {
				if (rune == hotkeySpec) {
					Application.Driver.SetAttribute (hotColor);
					continue;
				}
				Application.Driver.AddRune (rune);
				Application.Driver.SetAttribute (normalColor);
			}
		}

		/// <summary>
		/// Utility function to draw strings that contains a hotkey using a <see cref="ColorScheme"/> and the "focused" state.
		/// </summary>
		/// <param name="text">String to display, the underscore before a letter flags the next letter as the hotkey.</param>
		/// <param name="focused">If set to <see langword="true"/> this uses the focused colors from the color scheme, otherwise the regular ones.</param>
		/// <param name="scheme">The color scheme to use.</param>
		public void DrawHotString (ustring text, bool focused, ColorScheme scheme)
		{
			if (focused)
				DrawHotString (text, scheme.HotFocus, scheme.Focus);
			else
				DrawHotString (text, Enabled ? scheme.HotNormal : scheme.Disabled, Enabled ? scheme.Normal : scheme.Disabled);
		}

		/// <summary>
		/// This moves the cursor to the specified column and row in the view.
		/// </summary>
		/// <returns>The move.</returns>
		/// <param name="col">The column to move to, in view-relative coordinates.</param>
		/// <param name="row">the row to move to, in view-relative coordinates.</param>
		/// <param name="clipped">Whether to clip the result of the ViewToScreen method,
		///  If  <see langword="true"/>, the <paramref name="col"/> and <paramref name="row"/> values are clamped to the screen (terminal) dimensions (0..TerminalDim-1).</param>
		public void Move (int col, int row, bool clipped = true)
		{
			if (Driver.Rows == 0) {
				return;
			}

			ViewToScreen (col, row, out var rCol, out var rRow, clipped);
			Driver.Move (rCol, rRow);
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
		/// Method invoked when a subview is being removed from this view.
		/// </summary>
		/// <param name="e">Event args describing the subview being removed.</param>
		public virtual void OnRemoved (SuperViewChangedEventArgs e)
		{
			var view = e.Child;
			view.IsAdded = false;
			view.Removed?.Invoke (this, e);
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

		ColorScheme _colorScheme;

		/// <summary>
		/// The color scheme for this view, if it is not defined, it returns the <see cref="SuperView"/>'s
		/// color scheme.
		/// </summary>
		public virtual ColorScheme ColorScheme {
			get {
				if (_colorScheme == null) {
					return SuperView?.ColorScheme;
				}
				return _colorScheme;
			}
			set {
				if (_colorScheme != value) {
					_colorScheme = value;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Displays the specified character in the specified column and row of the View.
		/// </summary>
		/// <param name="col">Column (view-relative).</param>
		/// <param name="row">Row (view-relative).</param>
		/// <param name="ch">Ch.</param>
		public void AddRune (int col, int row, Rune ch)
		{
			if (row < 0 || col < 0)
				return;
			if (row > _frame.Height - 1 || col > _frame.Width - 1)
				return;
			Move (col, row);
			Driver.AddRune (ch);
		}

		/// <summary>
		/// Removes the <see cref="_needsDisplay"/> and the <see cref="_childNeedsDisplay"/> setting on this view.
		/// </summary>
		protected void ClearNeedsDisplay ()
		{
			_needsDisplay = Rect.Empty;
			_childNeedsDisplay = false;
		}

		/// <summary>
		/// The canvas that any line drawing that is to be shared by subviews of this view should add lines to.
		/// </summary>
		/// <remarks><see cref="Border"/> adds border lines to this LineCanvas.</remarks>
		public virtual LineCanvas LineCanvas { get; set; } = new LineCanvas ();

		/// <summary>
		/// Gets or sets whether this View will use it's SuperView's <see cref="LineCanvas"/> for
		/// rendering any border lines. If <see langword="true"/> the rendering of any borders drawn
		/// by this Frame will be done by it's parent's SuperView. If <see langword="false"/> (the default)
		/// this View's <see cref="OnDrawFrames()"/> method will be called to render the borders.
		/// </summary>
		public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

		// TODO: Make this cancelable
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public virtual bool OnDrawFrames ()
		{
			if (!IsInitialized) {
				return false;
			}

			var prevClip = Driver.Clip;
			var screenBounds = ViewToScreen (Frame);

			// TODO: Figure out what we should do if we have no superview
			//if (SuperView != null) {
			// TODO: Clipping is disabled for now to ensure we see errors
			Driver.Clip = new Rect (0, 0, Driver.Cols, Driver.Rows);// screenBounds;// SuperView.ClipToBounds ();
										//}

			// Each of these renders lines to either this View's LineCanvas 
			// Those lines will be finally rendered in OnRenderLineCanvas
			Margin?.Redraw (Margin.Frame);
			Border?.Redraw (Border.Frame);
			Padding?.Redraw (Padding.Frame);

			Driver.Clip = prevClip;

			return true;
		}

		/// <summary>
		/// Redraws this view and its subviews; only redraws the views that have been flagged for a re-display.
		/// </summary>
		/// <param name="bounds">The bounds (view-relative region) to redraw.</param>
		/// <remarks>
		/// <para>
		///    Always use <see cref="Bounds"/> (view-relative) when calling <see cref="Redraw(Rect)"/>, NOT <see cref="Frame"/> (superview-relative).
		/// </para>
		/// <para>
		///    Views should set the color that they want to use on entry, as otherwise this will inherit
		///    the last color that was set globally on the driver.
		/// </para>
		/// <para>
		///    Overrides of <see cref="Redraw"/> must ensure they do not set <c>Driver.Clip</c> to a clip region
		///    larger than the <ref name="bounds"/> parameter, as this will cause the driver to clip the entire region.
		/// </para>
		/// </remarks>
		public virtual void Redraw (Rect bounds)
		{
			if (!CanBeVisible (this)) {
				return;
			}

			var prevClip = ClipToBounds ();

			OnDrawFrames ();

			// TODO: Implement complete event
			// OnDrawFramesComplete (Frame)

			if (ColorScheme != null) {
				//Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
				Driver.SetAttribute (GetNormalColor ());
			}

			if (SuperView != null) {
				Clear (ViewToScreen (bounds));
			}

			// Invoke DrawContentEvent
			OnDrawContent (bounds);

			// Draw subviews
			// TODO: Implement OnDrawSubviews (cancelable);
			if (_subviews != null) {
				foreach (var view in _subviews) {
					if (view.Visible) { //!view._needsDisplay.IsEmpty || view._childNeedsDisplay || view.LayoutNeeded) {
						if (true) { //view.Frame.IntersectsWith (bounds)) { // && (view.Frame.IntersectsWith (bounds) || bounds.X < 0 || bounds.Y < 0)) {
							if (view.LayoutNeeded) {
								view.LayoutSubviews ();
							}

							// Draw the subview
							// Use the view's bounds (view-relative; Location will always be (0,0)
							//if (view.Visible && view.Frame.Width > 0 && view.Frame.Height > 0) {
							view.Redraw (view.Bounds);
							//}
						}
						view.ClearNeedsDisplay ();
					}
				}
			}

			// Invoke DrawContentCompleteEvent
			OnDrawContentComplete (bounds);
			Driver.Clip = prevClip;

			OnRenderLineCanvas ();




			// BUGBUG: v2 - We should be able to use View.SetClip here and not have to resort to knowing Driver details.
			ClearLayoutNeeded ();
			ClearNeedsDisplay ();
		}

		private void OnRenderLineCanvas ()
		{
			//Driver.SetAttribute (new Attribute(Color.White, Color.Black));

			// If we have a SuperView, it'll render our frames.
			if (!SuperViewRendersLineCanvas && LineCanvas.Bounds != Rect.Empty) {
				foreach (var p in LineCanvas.GetCellMap ()) { // Get the entire map
					Driver.SetAttribute (p.Value.Attribute?.Value ?? ColorScheme.Normal);
					Driver.Move (p.Key.X, p.Key.Y);
					Driver.AddRune (p.Value.Rune.Value);
				}
				LineCanvas.Clear ();
			}

			if (Subviews.Select (s => s.SuperViewRendersLineCanvas).Count () > 0) {
				foreach (var subview in Subviews.Where (s => s.SuperViewRendersLineCanvas)) {
					// Combine the LineCavas'
					LineCanvas.Merge (subview.LineCanvas);
					subview.LineCanvas.Clear ();
				}

				foreach (var p in LineCanvas.GetCellMap ()) { // Get the entire map
					Driver.SetAttribute (p.Value.Attribute?.Value ?? ColorScheme.Normal);
					Driver.Move (p.Key.X, p.Key.Y);
					Driver.AddRune (p.Value.Rune.Value);
				}
				LineCanvas.Clear ();
			}
		}

		/// <summary>
		/// Event invoked when the content area of the View is to be drawn.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.
		/// </para>
		/// <para>
		/// Rect provides the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.
		/// </para>
		/// </remarks>
		public event EventHandler<DrawEventArgs> DrawContent;

		/// <summary>
		/// Enables overrides to draw infinitely scrolled content and/or a background behind added controls. 
		/// </summary>
		/// <param name="contentArea">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called before any subviews added with <see cref="Add(View)"/> have been drawn. 
		/// </remarks>
		public virtual void OnDrawContent (Rect contentArea)
		{
			// TODO: Make DrawContent a cancelable event
			// if (!DrawContent?.Invoke(this, new DrawEventArgs (viewport)) {
			DrawContent?.Invoke (this, new DrawEventArgs (contentArea));

			if (!ustring.IsNullOrEmpty (TextFormatter.Text)) {
				if (TextFormatter != null) {
					TextFormatter.NeedsFormat = true;
				}
				// This should NOT clear 
				TextFormatter?.Draw (ViewToScreen (contentArea), HasFocus ? GetFocusColor () : GetNormalColor (),
				    HasFocus ? ColorScheme.HotFocus : GetHotNormalColor (),
				    Rect.Empty, false);
				SetSubViewNeedsDisplay ();
			}
		}

		/// <summary>
		/// Event invoked when the content area of the View is completed drawing.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Will be invoked after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
		/// </para>
		/// <para>
		/// Rect provides the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.
		/// </para>
		/// </remarks>
		public event EventHandler<DrawEventArgs> DrawContentComplete;

		/// <summary>
		/// Enables overrides after completed drawing infinitely scrolled content and/or a background behind removed controls.
		/// </summary>
		/// <param name="viewport">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
		/// </remarks>
		public virtual void OnDrawContentComplete (Rect viewport)
		{
			DrawContentComplete?.Invoke (this, new DrawEventArgs (viewport));
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
		/// Invoked when a character key is pressed and occurs after the key up event.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyPress;

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (Focused?.Enabled == true) {
				Focused?.KeyPress?.Invoke (this, args);
				if (args.Handled)
					return true;
			}

			return Focused?.Enabled == true && Focused?.ProcessKey (keyEvent) == true;
		}

		/// <summary>
		/// Invokes any binding that is registered on this <see cref="View"/>
		/// and matches the <paramref name="keyEvent"/>
		/// </summary>
		/// <param name="keyEvent">The key event passed.</param>
		protected bool? InvokeKeybindings (KeyEvent keyEvent)
		{
			bool? toReturn = null;

			if (KeyBindings.ContainsKey (keyEvent.Key)) {

				foreach (var command in KeyBindings [keyEvent.Key]) {

					if (!CommandImplementations.ContainsKey (command)) {
						throw new NotSupportedException ($"A KeyBinding was set up for the command {command} ({keyEvent.Key}) but that command is not supported by this View ({GetType ().Name})");
					}

					// each command has its own return value
					var thisReturn = CommandImplementations [command] ();

					// if we haven't got anything yet, the current command result should be used
					if (toReturn == null) {
						toReturn = thisReturn;
					}

					// if ever see a true then that's what we will return
					if (thisReturn ?? false) {
						toReturn = true;
					}
				}
			}

			return toReturn;
		}


		/// <summary>
		/// <para>Adds a new key combination that will trigger the given <paramref name="command"/>
		/// (if supported by the View - see <see cref="GetSupportedCommands"/>)
		/// </para>
		/// <para>If the key is already bound to a different <see cref="Command"/> it will be
		/// rebound to this one</para>
		/// <remarks>Commands are only ever applied to the current <see cref="View"/>(i.e. this feature
		/// cannot be used to switch focus to another view and perform multiple commands there) </remarks>
		/// </summary>
		/// <param name="key"></param>
		/// <param name="command">The command(s) to run on the <see cref="View"/> when <paramref name="key"/> is pressed.
		/// When specifying multiple commands, all commands will be applied in sequence. The bound <paramref name="key"/> strike
		/// will be consumed if any took effect.</param>
		public void AddKeyBinding (Key key, params Command [] command)
		{
			if (command.Length == 0) {
				throw new ArgumentException ("At least one command must be specified", nameof (command));
			}

			if (KeyBindings.ContainsKey (key)) {
				KeyBindings [key] = command;
			} else {
				KeyBindings.Add (key, command);
			}
		}

		/// <summary>
		/// Replaces a key combination already bound to <see cref="Command"/>.
		/// </summary>
		/// <param name="fromKey">The key to be replaced.</param>
		/// <param name="toKey">The new key to be used.</param>
		protected void ReplaceKeyBinding (Key fromKey, Key toKey)
		{
			if (KeyBindings.ContainsKey (fromKey)) {
				var value = KeyBindings [fromKey];
				KeyBindings.Remove (fromKey);
				KeyBindings [toKey] = value;
			}
		}

		/// <summary>
		/// Checks if the key binding already exists.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns><see langword="true"/> If the key already exist, <see langword="false"/> otherwise.</returns>
		public bool ContainsKeyBinding (Key key)
		{
			return KeyBindings.ContainsKey (key);
		}

		/// <summary>
		/// Removes all bound keys from the View and resets the default bindings.
		/// </summary>
		public void ClearKeybindings ()
		{
			KeyBindings.Clear ();
		}

		/// <summary>
		/// Clears the existing keybinding (if any) for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key"></param>
		public void ClearKeybinding (Key key)
		{
			KeyBindings.Remove (key);
		}

		/// <summary>
		/// Removes all key bindings that trigger the given command. Views can have multiple different
		/// keys bound to the same command and this method will clear all of them.
		/// </summary>
		/// <param name="command"></param>
		public void ClearKeybinding (params Command [] command)
		{
			foreach (var kvp in KeyBindings.Where (kvp => kvp.Value.SequenceEqual (command)).ToArray ()) {
				KeyBindings.Remove (kvp.Key);
			}
		}

		/// <summary>
		/// <para>States that the given <see cref="View"/> supports a given <paramref name="command"/>
		/// and what <paramref name="f"/> to perform to make that command happen
		/// </para>
		/// <para>If the <paramref name="command"/> already has an implementation the <paramref name="f"/>
		/// will replace the old one</para>
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="f">The function.</param>
		protected void AddCommand (Command command, Func<bool?> f)
		{
			// if there is already an implementation of this command
			if (CommandImplementations.ContainsKey (command)) {
				// replace that implementation
				CommandImplementations [command] = f;
			} else {
				// else record how to perform the action (this should be the normal case)
				CommandImplementations.Add (command, f);
			}
		}

		/// <summary>
		/// Returns all commands that are supported by this <see cref="View"/>.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Command> GetSupportedCommands ()
		{
			return CommandImplementations.Keys;
		}

		/// <summary>
		/// Gets the key used by a command.
		/// </summary>
		/// <param name="command">The command to search.</param>
		/// <returns>The <see cref="Key"/> used by a <see cref="Command"/></returns>
		public Key GetKeyFromCommand (params Command [] command)
		{
			return KeyBindings.First (kb => kb.Value.SequenceEqual (command)).Key;
		}

		/// <inheritdoc/>
		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			if (MostFocused?.Enabled == true) {
				MostFocused?.KeyPress?.Invoke (this, args);
				if (args.Handled)
					return true;
			}
			if (MostFocused?.Enabled == true && MostFocused?.ProcessKey (keyEvent) == true)
				return true;
			if (_subviews == null || _subviews.Count == 0)
				return false;

			foreach (var view in _subviews)
				if (view.Enabled && view.ProcessHotKey (keyEvent))
					return true;
			return false;
		}

		/// <inheritdoc/>
		public override bool ProcessColdKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (MostFocused?.Enabled == true) {
				MostFocused?.KeyPress?.Invoke (this, args);
				if (args.Handled)
					return true;
			}
			if (MostFocused?.Enabled == true && MostFocused?.ProcessKey (keyEvent) == true)
				return true;
			if (_subviews == null || _subviews.Count == 0)
				return false;

			foreach (var view in _subviews)
				if (view.Enabled && view.ProcessColdKey (keyEvent))
					return true;
			return false;
		}

		/// <summary>
		/// Invoked when a key is pressed.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyDown;

		/// <inheritdoc/>
		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyDown?.Invoke (this, args);
			if (args.Handled) {
				return true;
			}
			if (Focused?.Enabled == true) {
				Focused.KeyDown?.Invoke (this, args);
				if (args.Handled) {
					return true;
				}
				if (Focused?.OnKeyDown (keyEvent) == true) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Invoked when a key is released.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyUp;

		/// <inheritdoc/>
		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyUp?.Invoke (this, args);
			if (args.Handled) {
				return true;
			}
			if (Focused?.Enabled == true) {
				Focused.KeyUp?.Invoke (this, args);
				if (args.Handled) {
					return true;
				}
				if (Focused?.OnKeyUp (keyEvent) == true) {
					return true;
				}
			}

			return false;
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
		/// Sets the View's <see cref="Frame"/> to the frame-relative coordinates if its container. The
		/// container size and location are specified by <paramref name="superviewFrame"/> and are relative to the
		/// View's superview.
		/// </summary>
		/// <param name="superviewFrame">The supserview-relative rectangle describing View's container (nominally the 
		/// same as <c>this.SuperView.Frame</c>).</param>
		internal void SetRelativeLayout (Rect superviewFrame)
		{
			int newX, newW, newY, newH;
			var autosize = Size.Empty;

			if (AutoSize) {
				// Note this is global to this function and used as such within the local functions defined
				// below. In v2 AutoSize will be re-factored to not need to be dealt with in this function.
				autosize = GetAutoSize ();
			}

			// Returns the new dimension (width or height) and location (x or y) for the View given
			//   the superview's Frame.X or Frame.Y
			//   the superview's width or height
			//   the current Pos (View.X or View.Y)
			//   the current Dim (View.Width or View.Height)
			(int newLocation, int newDimension) GetNewLocationAndDimension (int superviewLocation, int superviewDimension, Pos pos, Dim dim, int autosizeDimension)
			{
				int newDimension, newLocation;

				switch (pos) {
				case Pos.PosCenter:
					if (dim == null) {
						newDimension = AutoSize ? autosizeDimension : superviewDimension;
					} else {
						newDimension = dim.Anchor (superviewDimension);
						newDimension = AutoSize && autosizeDimension > newDimension ? autosizeDimension : newDimension;
					}
					newLocation = pos.Anchor (superviewDimension - newDimension);
					break;

				case Pos.PosCombine combine:
					int left, right;
					(left, newDimension) = GetNewLocationAndDimension (superviewLocation, superviewDimension, combine.left, dim, autosizeDimension);
					(right, newDimension) = GetNewLocationAndDimension (superviewLocation, superviewDimension, combine.right, dim, autosizeDimension);
					if (combine.add) {
						newLocation = left + right;
					} else {
						newLocation = left - right;
					}
					newDimension = Math.Max (CalculateNewDimension (dim, newLocation, superviewDimension, autosizeDimension), 0);
					break;

				case Pos.PosAbsolute:
				case Pos.PosAnchorEnd:
				case Pos.PosFactor:
				case Pos.PosFunc:
				case Pos.PosView:
				default:
					newLocation = pos?.Anchor (superviewDimension) ?? 0;
					newDimension = Math.Max (CalculateNewDimension (dim, newLocation, superviewDimension, autosizeDimension), 0);
					break;
				}
				return (newLocation, newDimension);
			}

			// Recursively calculates the new dimension (width or height) of the given Dim given:
			//   the current location (x or y)
			//   the current dimension (width or height)
			int CalculateNewDimension (Dim d, int location, int dimension, int autosize)
			{
				int newDimension;
				switch (d) {
				case null:
					newDimension = AutoSize ? autosize : dimension;
					break;
				case Dim.DimCombine combine:
					int leftNewDim = CalculateNewDimension (combine.left, location, dimension, autosize);
					int rightNewDim = CalculateNewDimension (combine.right, location, dimension, autosize);
					if (combine.add) {
						newDimension = leftNewDim + rightNewDim;
					} else {
						newDimension = leftNewDim - rightNewDim;
					}
					newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;
					break;

				case Dim.DimFactor factor when !factor.IsFromRemaining ():
					newDimension = d.Anchor (dimension);
					newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;
					break;

				case Dim.DimFill:
				default:
					newDimension = Math.Max (d.Anchor (dimension - location), 0);
					newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;
					break;
				}

				return newDimension;
			}


			// horizontal
			(newX, newW) = GetNewLocationAndDimension (superviewFrame.X, superviewFrame.Width, _x, _width, autosize.Width);

			// vertical
			(newY, newH) = GetNewLocationAndDimension (superviewFrame.Y, superviewFrame.Height, _y, _height, autosize.Height);

			var r = new Rect (newX, newY, newW, newH);
			if (Frame != r) {
				Frame = r;
				// BUGBUG: Why is this AFTER setting Frame? Seems duplicative.
				if (!SetMinWidthHeight ()) {
					TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
				}
			}
		}

		/// <summary>
		/// Fired after the View's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public event EventHandler<LayoutEventArgs> LayoutStarted;

		/// <summary>
		/// Raises the <see cref="LayoutStarted"/> event. Called from  <see cref="LayoutSubviews"/> before any subviews have been laid out.
		/// </summary>
		internal virtual void OnLayoutStarted (LayoutEventArgs args)
		{
			LayoutStarted?.Invoke (this, args);
		}

		/// <summary>
		/// Fired after the View's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public event EventHandler<LayoutEventArgs> LayoutComplete;

		/// <summary>
		/// Event called only once when the <see cref="View"/> is being initialized for the first time.
		/// Allows configurations and assignments to be performed before the <see cref="View"/> being shown.
		/// This derived from <see cref="ISupportInitializeNotification"/> to allow notify all the views that are being initialized.
		/// </summary>
		public event EventHandler Initialized;

		/// <summary>
		/// Raises the <see cref="LayoutComplete"/> event. Called from  <see cref="LayoutSubviews"/> before all sub-views have been laid out.
		/// </summary>
		internal virtual void OnLayoutComplete (LayoutEventArgs args)
		{
			LayoutComplete?.Invoke (this, args);
		}

		internal void CollectPos (Pos pos, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
		{
			switch (pos) {
			case Pos.PosView pv:
				// See #2461
				//if (!from.InternalSubviews.Contains (pv.Target)) {
				//	throw new InvalidOperationException ($"View {pv.Target} is not a subview of {from}");
				//}
				if (pv.Target != this) {
					nEdges.Add ((pv.Target, from));
				}
				return;
			case Pos.PosCombine pc:
				CollectPos (pc.left, from, ref nNodes, ref nEdges);
				CollectPos (pc.right, from, ref nNodes, ref nEdges);
				break;
			}
		}

		internal void CollectDim (Dim dim, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
		{
			switch (dim) {
			case Dim.DimView dv:
				// See #2461
				//if (!from.InternalSubviews.Contains (dv.Target)) {
				//	throw new InvalidOperationException ($"View {dv.Target} is not a subview of {from}");
				//}
				if (dv.Target != this) {
					nEdges.Add ((dv.Target, from));
				}
				return;
			case Dim.DimCombine dc:
				CollectDim (dc.left, from, ref nNodes, ref nEdges);
				CollectDim (dc.right, from, ref nNodes, ref nEdges);
				break;
			}
		}

		internal void CollectAll (View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
		{
			foreach (var v in from.InternalSubviews) {
				nNodes.Add (v);
				if (v._layoutStyle != LayoutStyle.Computed) {
					continue;
				}
				CollectPos (v.X, v, ref nNodes, ref nEdges);
				CollectPos (v.Y, v, ref nNodes, ref nEdges);
				CollectDim (v.Width, v, ref nNodes, ref nEdges);
				CollectDim (v.Height, v, ref nNodes, ref nEdges);
			}
		}

		// https://en.wikipedia.org/wiki/Topological_sorting
		internal static List<View> TopologicalSort (View superView, IEnumerable<View> nodes, ICollection<(View From, View To)> edges)
		{
			var result = new List<View> ();

			// Set of all nodes with no incoming edges
			var noEdgeNodes = new HashSet<View> (nodes.Where (n => edges.All (e => !e.To.Equals (n))));


			while (noEdgeNodes.Any ()) {
				//  remove a node n from S
				var n = noEdgeNodes.First ();
				noEdgeNodes.Remove (n);

				// add n to tail of L
				if (n != superView)
					result.Add (n);

				// for each node m with an edge e from n to m do
				foreach (var e in edges.Where (e => e.From.Equals (n)).ToArray ()) {
					var m = e.To;

					// remove edge e from the graph
					edges.Remove (e);

					// if m has no other incoming edges then
					if (edges.All (me => !me.To.Equals (m)) && m != superView) {
						// insert m into S
						noEdgeNodes.Add (m);
					}
				}
			}

			if (edges.Any ()) {
				foreach ((var from, var to) in edges) {
					if (from == to) {
						// if not yet added to the result, add it and remove from edge
						if (result.Find (v => v == from) == null) {
							result.Add (from);
						}
						edges.Remove ((from, to));
					} else if (from.SuperView == to.SuperView) {
						// if 'from' is not yet added to the result, add it
						if (result.Find (v => v == from) == null) {
							result.Add (from);
						}
						// if 'to' is not yet added to the result, add it
						if (result.Find (v => v == to) == null) {
							result.Add (to);
						}
						// remove from edge
						edges.Remove ((from, to));
					} else if (from != superView?.GetTopSuperView (to, from) && !ReferenceEquals (from, to)) {
						if (ReferenceEquals (from.SuperView, to)) {
							throw new InvalidOperationException ($"ComputedLayout for \"{superView}\": \"{to}\" references a SubView (\"{from}\").");
						} else {
							throw new InvalidOperationException ($"ComputedLayout for \"{superView}\": \"{from}\" linked with \"{to}\" was not found. Did you forget to add it to {superView}?");
						}
					}
				}
			}
			// return L (a topologically sorted order)
			return result;
		} // TopologicalSort

		/// <summary>
		/// Overriden by <see cref="Frame"/> to do nothing, as the <see cref="Frame"/> does not have frames.
		/// </summary>
		internal virtual void LayoutFrames ()
		{
			if (Margin == null) return; // CreateFrames() has not been called yet

			if (Margin.Frame.Size != Frame.Size) {
				Margin._frame = new Rect (Point.Empty, Frame.Size);
				Margin.X = 0;
				Margin.Y = 0;
				Margin.Width = Frame.Size.Width;
				Margin.Height = Frame.Size.Height;
				Margin.SetNeedsLayout ();
				Margin.LayoutSubviews ();
				Margin.SetNeedsDisplay ();
			}

			var border = Margin.Thickness.GetInside (Margin.Frame);
			if (border != Border.Frame) {
				Border._frame = new Rect (new Point (border.Location.X, border.Location.Y), border.Size);
				Border.X = border.Location.X;
				Border.Y = border.Location.Y;
				Border.Width = border.Size.Width;
				Border.Height = border.Size.Height;
				Border.SetNeedsLayout ();
				Border.LayoutSubviews ();
				Border.SetNeedsDisplay ();
			}

			var padding = Border.Thickness.GetInside (Border.Frame);
			if (padding != Padding.Frame) {
				Padding._frame = new Rect (new Point (padding.Location.X, padding.Location.Y), padding.Size);
				Padding.X = padding.Location.X;
				Padding.Y = padding.Location.Y;
				Padding.Width = padding.Size.Width;
				Padding.Height = padding.Size.Height;
				Padding.SetNeedsLayout ();
				Padding.LayoutSubviews ();
				Padding.SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Invoked when a view starts executing or when the dimensions of the view have changed, for example in
		/// response to the container view or terminal resizing.
		/// </summary>
		/// <remarks>
		/// Calls <see cref="OnLayoutComplete"/> (which raises the <see cref="LayoutComplete"/> event) before it returns.
		/// </remarks>
		public virtual void LayoutSubviews ()
		{
			if (!LayoutNeeded) {
				return;
			}

			LayoutFrames ();

			var oldBounds = Bounds;
			OnLayoutStarted (new LayoutEventArgs () { OldBounds = oldBounds });

			TextFormatter.Size = GetSizeNeededForTextAndHotKey ();

			// Sort out the dependencies of the X, Y, Width, Height properties
			var nodes = new HashSet<View> ();
			var edges = new HashSet<(View, View)> ();
			CollectAll (this, ref nodes, ref edges);
			var ordered = View.TopologicalSort (SuperView, nodes, edges);
			foreach (var v in ordered) {
				LayoutSubview (v, new Rect (GetBoundsOffset (), Bounds.Size));
			}

			// If the 'to' is rooted to 'from' and the layoutstyle is Computed it's a special-case.
			// Use LayoutSubview with the Frame of the 'from' 
			if (SuperView != null && GetTopSuperView () != null && LayoutNeeded && edges.Count > 0) {
				foreach ((var from, var to) in edges) {
					LayoutSubview (to, from.Frame);
				}
			}

			LayoutNeeded = false;

			OnLayoutComplete (new LayoutEventArgs () { OldBounds = oldBounds });
		}

		private void LayoutSubview (View v, Rect contentArea)
		{
			if (v.LayoutStyle == LayoutStyle.Computed) {
				v.SetRelativeLayout (contentArea);
			}

			v.LayoutSubviews ();
			v.LayoutNeeded = false;
		}

		ustring _text;

		/// <summary>
		///   The text displayed by the <see cref="View"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		///  The text will be drawn before any subviews are drawn.
		/// </para>
		/// <para>
		///  The text will be drawn starting at the view origin (0, 0) and will be formatted according
		///  to <see cref="TextAlignment"/> and <see cref="TextDirection"/>. 
		/// </para>
		/// <para>
		///  The text will word-wrap to additional lines if it does not fit horizontally. If <see cref="Bounds"/>'s height
		///  is 1, the text will be clipped.	
		///  </para>
		/// <para>
		///  Set the <see cref="HotKeySpecifier"/> to enable hotkey support. To disable hotkey support set <see cref="HotKeySpecifier"/> to
		///  <c>(Rune)0xffff</c>.
		/// </para>
		/// </remarks>
		public virtual ustring Text {
			get => _text;
			set {
				_text = value;
				SetHotKey ();
				UpdateTextFormatterText ();
				//TextFormatter.Format ();
				OnResizeNeeded ();

#if DEBUG
				if (_text != null && string.IsNullOrEmpty (Id)) {
					Id = _text.ToString ();
				}
#endif
			}
		}

		/// <summary>
		/// Gets or sets a flag that determines whether the View will be automatically resized to fit the <see cref="Text"/> 
		/// within <see cref="Bounds"/>
		/// <para>
		/// The default is <see langword="false"/>. Set to <see langword="true"/> to turn on AutoSize. If <see langword="true"/> then
		/// <see cref="Width"/> and <see cref="Height"/> will be used if <see cref="Text"/> can fit; 
		/// if <see cref="Text"/> won't fit the view will be resized as needed.
		/// </para>
		/// <para>
		/// In addition, if <see cref="ForceValidatePosDim"/> is <see langword="true"/> the new values of <see cref="Width"/> and
		/// <see cref="Height"/> must be of the same types of the existing one to avoid breaking the <see cref="Dim"/> settings.
		/// </para>
		/// </summary>
		public virtual bool AutoSize {
			get => _autoSize;
			set {
				var v = ResizeView (value);
				TextFormatter.AutoSize = v;
				if (_autoSize != v) {
					_autoSize = v;
					TextFormatter.NeedsFormat = true;
					UpdateTextFormatterText ();
					OnResizeNeeded ();
				}
			}
		}

		/// <summary>
		/// Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
		/// or not when <see cref="TextFormatter.WordWrap"/> is enabled. 
		/// If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when 
		/// <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
		/// </summary>
		public virtual bool PreserveTrailingSpaces {
			get => TextFormatter.PreserveTrailingSpaces;
			set {
				if (TextFormatter.PreserveTrailingSpaces != value) {
					TextFormatter.PreserveTrailingSpaces = value;
					TextFormatter.NeedsFormat = true;
				}
			}
		}

		/// <summary>
		/// Gets or sets how the View's <see cref="Text"/> is aligned horizontally when drawn. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual TextAlignment TextAlignment {
			get => TextFormatter.Alignment;
			set {
				TextFormatter.Alignment = value;
				UpdateTextFormatterText ();
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets how the View's <see cref="Text"/> is aligned vertically when drawn. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual VerticalTextAlignment VerticalTextAlignment {
			get => TextFormatter.VerticalAlignment;
			set {
				TextFormatter.VerticalAlignment = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the direction of the View's <see cref="Text"/>. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual TextDirection TextDirection {
			get => TextFormatter.Direction;
			set {
				UpdateTextDirection (value);
				TextFormatter.Direction = value;
			}
		}

		private void UpdateTextDirection (TextDirection newDirection)
		{
			var directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction)
			    != TextFormatter.IsHorizontalDirection (newDirection);
			TextFormatter.Direction = newDirection;

			var isValidOldAutoSize = AutoSize && IsValidAutoSize (out var _);

			UpdateTextFormatterText ();

			if ((!ForceValidatePosDim && directionChanged && AutoSize)
			    || (ForceValidatePosDim && directionChanged && AutoSize && isValidOldAutoSize)) {
				OnResizeNeeded ();
			} else if (directionChanged && IsAdded) {
				SetWidthHeight (Bounds.Size);
				SetMinWidthHeight ();
			} else {
				SetMinWidthHeight ();
			}
			TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
			SetNeedsDisplay ();
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
		/// Gets information if the view was already added to the <see cref="SuperView"/>.
		/// </summary>
		public bool IsAdded { get; private set; }

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

		/// <summary>
		/// Gets or sets whether the view has a one row/col thick border.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is a helper for manipulating the view's <see cref="Border"/>. Setting this property to any value other than
		/// <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s <see cref="Frame.Thickness"/> 
		/// to `1` and <see cref="BorderStyle"/> to the value. 
		/// </para>
		/// <para>
		/// Setting this property to <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s <see cref="Frame.Thickness"/> 
		/// to `0` and <see cref="BorderStyle"/> to <see cref="LineStyle.None"/>. 
		/// </para>
		/// <para>
		/// For more advanced customization of the view's border, manipulate see <see cref="Border"/> directly.
		/// </para>
		/// </remarks>
		public LineStyle BorderStyle {
			get {
				return Border?.BorderStyle ?? LineStyle.None;
			}
			set {
				if (Border == null) {
					throw new InvalidOperationException ("Border is null; this is likely a bug.");
				}
				if (value != LineStyle.None) {
					Border.Thickness = new Thickness (1);
				} else {
					Border.Thickness = new Thickness (0);
				}
				Border.BorderStyle = value;
				LayoutFrames ();
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// Pretty prints the View
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return $"{GetType ().Name}({Id})({Frame})";
		}

		void SetHotKey ()
		{
			if (TextFormatter == null) {
				return; // throw new InvalidOperationException ("Can't set HotKey unless a TextFormatter has been created");
			}
			TextFormatter.FindHotKey (_text, HotKeySpecifier, true, out _, out var hk);
			if (_hotKey != hk) {
				HotKey = hk;
			}
		}

		bool ResizeView (bool autoSize)
		{
			if (!autoSize) {
				return false;
			}

			var aSize = true;
			var nBoundsSize = GetAutoSize ();
			if (nBoundsSize != Bounds.Size) {
				if (ForceValidatePosDim) {
					aSize = SetWidthHeight (nBoundsSize);
				} else {
					Height = nBoundsSize.Height;
					Width = nBoundsSize.Width; // = new Rect (Bounds.X, Bounds.Y, nBoundsSize.Width, nBoundsSize.Height);
				}
			}
			// BUGBUG: This call may be redundant
			TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
			return aSize;
		}

		/// <summary>
		/// Resizes the View to fit the specified <see cref="Bounds"/> size.
		/// </summary>
		/// <param name="nBounds"></param>
		/// <returns></returns>
		bool SetWidthHeight (Size nBounds)
		{
			var aSize = false;
			var canSizeW = TrySetWidth (nBounds.Width - GetHotKeySpecifierLength (), out var rW);
			var canSizeH = TrySetHeight (nBounds.Height - GetHotKeySpecifierLength (false), out var rH);
			if (canSizeW) {
				aSize = true;
				_width = rW;
			}
			if (canSizeH) {
				aSize = true;
				_height = rH;
			}
			if (aSize) {
				Bounds = new Rect (Bounds.X, Bounds.Y, canSizeW ? rW : Bounds.Width, canSizeH ? rH : Bounds.Height);
			}

			return aSize;
		}

		/// <summary>
		/// Gets the Frame dimensions required to fit <see cref="Text"/> using the text <see cref="Direction"/> specified by the
		/// <see cref="TextFormatter"/> property and accounting for any <see cref="HotKeySpecifier"/> characters.
		/// </summary>
		/// <returns>The <see cref="Size"/> required to fit the text.</returns>
		public Size GetAutoSize ()
		{
			var rect = TextFormatter.CalcRect (Bounds.X, Bounds.Y, TextFormatter.Text, TextFormatter.Direction);
			var newWidth = rect.Size.Width - GetHotKeySpecifierLength () + Margin.Thickness.Horizontal + Border.Thickness.Horizontal + Padding.Thickness.Horizontal;
			var newHeight = rect.Size.Height - GetHotKeySpecifierLength (false) + Margin.Thickness.Vertical + Border.Thickness.Vertical + Padding.Thickness.Vertical;
			return new Size (newWidth, newHeight);
		}

		bool IsValidAutoSize (out Size autoSize)
		{
			var rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
			autoSize = new Size (rect.Size.Width - GetHotKeySpecifierLength (),
			    rect.Size.Height - GetHotKeySpecifierLength (false));
			return !(ForceValidatePosDim && (!(Width is Dim.DimAbsolute) || !(Height is Dim.DimAbsolute))
			    || _frame.Size.Width != rect.Size.Width - GetHotKeySpecifierLength ()
			    || _frame.Size.Height != rect.Size.Height - GetHotKeySpecifierLength (false));
		}

		bool IsValidAutoSizeWidth (Dim width)
		{
			var rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
			var dimValue = width.Anchor (0);
			return !(ForceValidatePosDim && (!(width is Dim.DimAbsolute)) || dimValue != rect.Size.Width
			    - GetHotKeySpecifierLength ());
		}

		bool IsValidAutoSizeHeight (Dim height)
		{
			var rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
			var dimValue = height.Anchor (0);
			return !(ForceValidatePosDim && (!(height is Dim.DimAbsolute)) || dimValue != rect.Size.Height
			    - GetHotKeySpecifierLength (false));
		}

		/// <summary>
		/// Gets the width or height of the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/> characters 
		/// in the <see cref="Text"/> property.
		/// </summary>
		/// <remarks>
		/// Only the first hotkey specifier found in <see cref="Text"/> is supported.
		/// </remarks>
		/// <param name="isWidth">If <see langword="true"/> (the default) the width required for the hotkey specifier is returned. Otherwise the height is returned.</param>
		/// <returns>The number of characters required for the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/>. If the text direction specified
		/// by <see cref="TextDirection"/> does not match the <paramref name="isWidth"/> parameter, <c>0</c> is returned.</returns>
		public int GetHotKeySpecifierLength (bool isWidth = true)
		{
			if (isWidth) {
				return TextFormatter.IsHorizontalDirection (TextDirection) &&
				    TextFormatter.Text?.Contains (HotKeySpecifier) == true
				    ? Math.Max (Rune.ColumnWidth (HotKeySpecifier), 0) : 0;
			} else {
				return TextFormatter.IsVerticalDirection (TextDirection) &&
				    TextFormatter.Text?.Contains (HotKeySpecifier) == true
				    ? Math.Max (Rune.ColumnWidth (HotKeySpecifier), 0) : 0;
			}
		}

		/// <summary>
		/// Gets the dimensions required for <see cref="Text"/> ignoring a <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/>.
		/// </summary>
		/// <returns></returns>
		public Size GetSizeNeededForTextWithoutHotKey ()
		{
			return new Size (TextFormatter.Size.Width - GetHotKeySpecifierLength (),
			    TextFormatter.Size.Height - GetHotKeySpecifierLength (false));
		}

		/// <summary>
		/// Gets the dimensions required for <see cref="Text"/> accounting for a <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/> .
		/// </summary>
		/// <returns></returns>
		public Size GetSizeNeededForTextAndHotKey ()
		{
			if (ustring.IsNullOrEmpty (TextFormatter.Text)) {

				if (!IsInitialized) return Size.Empty;

				return Bounds.Size;
			}

			// BUGBUG: This IGNORES what Text is set to, using on only the current View size. This doesn't seem to make sense.
			// BUGBUG: This uses Frame; in v2 it should be Bounds
			return new Size (_frame.Size.Width + GetHotKeySpecifierLength (),
					 _frame.Size.Height + GetHotKeySpecifierLength (false));
		}

		/// <inheritdoc/>
		public override bool OnMouseEnter (MouseEvent mouseEvent)
		{
			if (!Enabled) {
				return true;
			}

			if (!CanBeVisible (this)) {
				return false;
			}

			var args = new MouseEventEventArgs (mouseEvent);
			MouseEnter?.Invoke (this, args);

			return args.Handled || base.OnMouseEnter (mouseEvent);
		}

		/// <inheritdoc/>
		public override bool OnMouseLeave (MouseEvent mouseEvent)
		{
			if (!Enabled) {
				return true;
			}

			if (!CanBeVisible (this)) {
				return false;
			}

			var args = new MouseEventEventArgs (mouseEvent);
			MouseLeave?.Invoke (this, args);

			return args.Handled || base.OnMouseLeave (mouseEvent);
		}

		/// <summary>
		/// Method invoked when a mouse event is generated
		/// </summary>
		/// <param name="mouseEvent"></param>
		/// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
		public virtual bool OnMouseEvent (MouseEvent mouseEvent)
		{
			if (!Enabled) {
				return true;
			}

			if (!CanBeVisible (this)) {
				return false;
			}

			var args = new MouseEventEventArgs (mouseEvent);
			if (OnMouseClick (args))
				return true;
			if (MouseEvent (mouseEvent))
				return true;

			if (mouseEvent.Flags == MouseFlags.Button1Clicked) {
				if (CanFocus && !HasFocus && SuperView != null) {
					SuperView.SetFocus (this);
					SetNeedsDisplay ();
				}

				return true;
			}
			return false;
		}

		/// <summary>
		/// Invokes the MouseClick event.
		/// </summary>
		protected bool OnMouseClick (MouseEventEventArgs args)
		{
			if (!Enabled) {
				return true;
			}

			MouseClick?.Invoke (this, args);
			return args.Handled;
		}

		/// <inheritdoc/>
		public override void OnCanFocusChanged () => CanFocusChanged?.Invoke (this, EventArgs.Empty);

		/// <inheritdoc/>
		public override void OnEnabledChanged () => EnabledChanged?.Invoke (this, EventArgs.Empty);

		/// <inheritdoc/>
		public override void OnVisibleChanged () => VisibleChanged?.Invoke (this, EventArgs.Empty);

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

		/// <summary>
		/// Determines if the View's <see cref="Width"/> can be set to a new value.
		/// </summary>
		/// <param name="desiredWidth"></param>
		/// <param name="resultWidth">Contains the width that would result if <see cref="Width"/> were set to <paramref name="desiredWidth"/>"/> </param>
		/// <returns><see langword="true"/> if the View's <see cref="Width"/> can be changed to the specified value. False otherwise.</returns>
		internal bool TrySetWidth (int desiredWidth, out int resultWidth)
		{
			var w = desiredWidth;
			bool canSetWidth;
			switch (Width) {
			case Dim.DimCombine _:
			case Dim.DimView _:
			case Dim.DimFill _:
				// It's a Dim.DimCombine and so can't be assigned. Let it have it's Width anchored.
				w = Width.Anchor (w);
				canSetWidth = !ForceValidatePosDim;
				break;
			case Dim.DimFactor factor:
				// Tries to get the SuperView Width otherwise the view Width.
				var sw = SuperView != null ? SuperView.Frame.Width : w;
				if (factor.IsFromRemaining ()) {
					sw -= Frame.X;
				}
				w = Width.Anchor (sw);
				canSetWidth = !ForceValidatePosDim;
				break;
			default:
				canSetWidth = true;
				break;
			}
			resultWidth = w;

			return canSetWidth;
		}

		/// <summary>
		/// Determines if the View's <see cref="Height"/> can be set to a new value.
		/// </summary>
		/// <param name="desiredHeight"></param>
		/// <param name="resultHeight">Contains the width that would result if <see cref="Height"/> were set to <paramref name="desiredHeight"/>"/> </param>
		/// <returns><see langword="true"/> if the View's <see cref="Height"/> can be changed to the specified value. False otherwise.</returns>
		internal bool TrySetHeight (int desiredHeight, out int resultHeight)
		{
			var h = desiredHeight;
			bool canSetHeight;
			switch (Height) {
			case Dim.DimCombine _:
			case Dim.DimView _:
			case Dim.DimFill _:
				// It's a Dim.DimCombine and so can't be assigned. Let it have it's height anchored.
				h = Height.Anchor (h);
				canSetHeight = !ForceValidatePosDim;
				break;
			case Dim.DimFactor factor:
				// Tries to get the SuperView height otherwise the view height.
				var sh = SuperView != null ? SuperView.Frame.Height : h;
				if (factor.IsFromRemaining ()) {
					sh -= Frame.Y;
				}
				h = Height.Anchor (sh);
				canSetHeight = !ForceValidatePosDim;
				break;
			default:
				canSetHeight = true;
				break;
			}
			resultHeight = h;

			return canSetHeight;
		}

		/// <summary>
		/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
		/// </summary>
		/// <returns><see cref="Terminal.Gui.ColorScheme.Normal"/> if <see cref="Enabled"/> is <see langword="true"/>
		/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
		/// If it's overridden can return other values.</returns>
		public virtual Attribute GetNormalColor ()
		{
			return Enabled ? ColorScheme.Normal : ColorScheme.Disabled;
		}

		/// <summary>
		/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
		/// </summary>
		/// <returns><see cref="Terminal.Gui.ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/>
		/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
		/// If it's overridden can return other values.</returns>
		public virtual Attribute GetFocusColor ()
		{
			return Enabled ? ColorScheme.Focus : ColorScheme.Disabled;
		}

		/// <summary>
		/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
		/// </summary>
		/// <returns><see cref="Terminal.Gui.ColorScheme.HotNormal"/> if <see cref="Enabled"/> is <see langword="true"/>
		/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
		/// If it's overridden can return other values.</returns>
		public virtual Attribute GetHotNormalColor ()
		{
			return Enabled ? ColorScheme.HotNormal : ColorScheme.Disabled;
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

		/// <summary>
		/// Finds which view that belong to the <paramref name="start"/> superview at the provided location.
		/// </summary>
		/// <param name="start">The superview where to look for.</param>
		/// <param name="x">The column location in the superview.</param>
		/// <param name="y">The row location in the superview.</param>
		/// <param name="resx">The found view screen relative column location.</param>
		/// <param name="resy">The found view screen relative row location.</param>
		/// <returns>
		///  The view that was found at the <praramref name="x"/> and <praramref name="y"/> coordinates.
		///  <see langword="null"/> if no view was found.
		/// </returns>
		public static View FindDeepestView (View start, int x, int y, out int resx, out int resy)
		{
			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}
			if (start.InternalSubviews != null) {
				int count = start.InternalSubviews.Count;
				if (count > 0) {
					var boundsOffset = start.GetBoundsOffset ();
					var rx = x - (startFrame.X + boundsOffset.X);
					var ry = y - (startFrame.Y + boundsOffset.Y);
					for (int i = count - 1; i >= 0; i--) {
						View v = start.InternalSubviews [i];
						if (v.Visible && v.Frame.Contains (rx, ry)) {
							var deep = FindDeepestView (v, rx, ry, out resx, out resy);
							if (deep == null)
								return v;
							return deep;
						}
					}
				}
			}
			resx = x - startFrame.X;
			resy = y - startFrame.Y;
			return start;
		}
	}
}
