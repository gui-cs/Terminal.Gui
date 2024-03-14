using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// Determines the LayoutStyle for a <see cref="View"/>, if Absolute, during <see cref="View.LayoutSubviews"/>, the
	/// value from the <see cref="View.Frame"/> will be used, if the value is Computed, then <see cref="View.Frame"/>
	/// will be updated from the X, Y <see cref="Pos"/> objects and the Width and Height <see cref="Dim"/> objects.
	/// </summary>
	public enum LayoutStyle {
		/// <summary>
		/// The position and size of the view are based on the Frame value.
		/// </summary>
		Absolute,

		/// <summary>
		/// The position and size of the view will be computed based on the
		/// X, Y, Width and Height properties and set on the Frame.
		/// </summary>
		Computed
	}

	/// <summary>
	/// View is the base class for all views on the screen and represents a visible element that can render itself and 
	/// contains zero or more nested views.
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
	/// </remarks>
	public partial class View : Responder, ISupportInitializeNotification {

		internal enum Direction {
			Forward,
			Backward
		}

		// container == SuperView
		View container = null;
		View focused = null;
		Direction focusDirection;
		bool autoSize;

		ShortcutHelper shortcutHelper;

		/// <summary>
		/// Event fired when a subview is being added to this view.
		/// </summary>
		public event Action<View> Added;

		/// <summary>
		/// Event fired when a subview is being removed from this view.
		/// </summary>
		public event Action<View> Removed;

		/// <summary>
		/// Event fired when the view gets focus.
		/// </summary>
		public event Action<FocusEventArgs> Enter;

		/// <summary>
		/// Event fired when the view looses focus.
		/// </summary>
		public event Action<FocusEventArgs> Leave;

		/// <summary>
		/// Event fired when the view receives the mouse event for the first time.
		/// </summary>
		public event Action<MouseEventArgs> MouseEnter;

		/// <summary>
		/// Event fired when the view receives a mouse event for the last time.
		/// </summary>
		public event Action<MouseEventArgs> MouseLeave;

		/// <summary>
		/// Event fired when a mouse event is generated.
		/// </summary>
		public event Action<MouseEventArgs> MouseClick;

		/// <summary>
		/// Event fired when the <see cref="CanFocus"/> value is being changed.
		/// </summary>
		public event Action CanFocusChanged;

		/// <summary>
		/// Event fired when the <see cref="Enabled"/> value is being changed.
		/// </summary>
		public event Action EnabledChanged;

		/// <summary>
		/// Event fired when the <see cref="Visible"/> value is being changed.
		/// </summary>
		public event Action VisibleChanged;

		/// <summary>
		/// Event invoked when the <see cref="HotKey"/> is changed.
		/// </summary>
		public event Action<Key> HotKeyChanged;

		Key hotKey = Key.Null;

		/// <summary>
		/// Gets or sets the HotKey defined for this view. A user pressing HotKey on the keyboard while this view has focus will cause the Clicked event to fire.
		/// </summary>
		public virtual Key HotKey {
			get => hotKey;
			set {
				if (hotKey != value) {
					hotKey = TextFormatter.HotKey = (value == Key.Unknown ? Key.Null : value);
				}
			}
		}

		/// <summary>
		/// Gets or sets the specifier character for the hotkey (e.g. '_'). Set to '\xffff' to disable hotkey support for this View instance. The default is '\xffff'. 
		/// </summary>
		public virtual Rune HotKeySpecifier {
			get => TextFormatter.HotKeySpecifier;
			set {
				TextFormatter.HotKeySpecifier = value;
				SetHotKey ();
			}
		}

		/// <summary>
		/// This is the global setting that can be used as a global shortcut to invoke an action if provided.
		/// </summary>
		public Key Shortcut {
			get => shortcutHelper.Shortcut;
			set {
				if (shortcutHelper.Shortcut != value && (ShortcutHelper.PostShortcutValidation (value) || value == Key.Null)) {
					shortcutHelper.Shortcut = value;
				}
			}
		}

		/// <summary>
		/// The keystroke combination used in the <see cref="Shortcut"/> as string.
		/// </summary>
		public ustring ShortcutTag => ShortcutHelper.GetShortcutTag (shortcutHelper.Shortcut);

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
			get => SuperView?.FocusDirection ?? focusDirection;
			set {
				if (SuperView != null)
					SuperView.FocusDirection = value;
				else
					focusDirection = value;
			}
		}

		/// <summary>
		/// Points to the current driver in use by the view, it is a convenience property
		/// for simplifying the development of new views.
		/// </summary>
		public static ConsoleDriver Driver => Application.Driver;

		static readonly IList<View> empty = new List<View> (0).AsReadOnly ();

		// This is null, and allocated on demand.
		List<View> subviews;

		/// <summary>
		/// This returns a list of the subviews contained by this view.
		/// </summary>
		/// <value>The subviews.</value>
		public IList<View> Subviews => subviews?.AsReadOnly () ?? empty;

		// Internally, we use InternalSubviews rather than subviews, as we do not expect us
		// to make the same mistakes our users make when they poke at the Subviews.
		internal IList<View> InternalSubviews => subviews ?? empty;

		// This is null, and allocated on demand.
		List<View> tabIndexes;

		/// <summary>
		/// Configurable keybindings supported by the control
		/// </summary>
		private Dictionary<Key, Command []> KeyBindings { get; set; } = new Dictionary<Key, Command []> ();
		private Dictionary<Command, Func<bool?>> CommandImplementations { get; set; } = new Dictionary<Command, Func<bool?>> ();

		/// <summary>
		/// This returns a tab index list of the subviews contained by this view.
		/// </summary>
		/// <value>The tabIndexes.</value>
		public IList<View> TabIndexes => tabIndexes?.AsReadOnly () ?? empty;

		int tabIndex = -1;

		/// <summary>
		/// Indicates the index of the current <see cref="View"/> from the <see cref="TabIndexes"/> list.
		/// </summary>
		public int TabIndex {
			get { return tabIndex; }
			set {
				if (!CanFocus) {
					tabIndex = -1;
					return;
				} else if (SuperView?.tabIndexes == null || SuperView?.tabIndexes.Count == 1) {
					tabIndex = 0;
					return;
				} else if (tabIndex == value) {
					return;
				}
				tabIndex = value > SuperView.tabIndexes.Count - 1 ? SuperView.tabIndexes.Count - 1 : value < 0 ? 0 : value;
				tabIndex = GetTabIndex (tabIndex);
				if (SuperView.tabIndexes.IndexOf (this) != tabIndex) {
					SuperView.tabIndexes.Remove (this);
					SuperView.tabIndexes.Insert (tabIndex, this);
					SetTabIndex ();
				}
			}
		}

		int GetTabIndex (int idx)
		{
			var i = 0;
			foreach (var v in SuperView.tabIndexes) {
				if (v.tabIndex == -1 || v == this) {
					continue;
				}
				i++;
			}
			return Math.Min (i, idx);
		}

		void SetTabIndex ()
		{
			var i = 0;
			foreach (var v in SuperView.tabIndexes) {
				if (v.tabIndex == -1) {
					continue;
				}
				v.tabIndex = i;
				i++;
			}
		}

		bool tabStop = true;

		/// <summary>
		/// This only be <see langword="true"/> if the <see cref="CanFocus"/> is also <see langword="true"/> 
		/// and the focus can be avoided by setting this to <see langword="false"/>
		/// </summary>
		public bool TabStop {
			get => tabStop;
			set {
				if (tabStop == value) {
					return;
				}
				tabStop = CanFocus && value;
			}
		}

		bool oldCanFocus;
		int oldTabIndex;

		/// <inheritdoc/>
		public override bool CanFocus {
			get => base.CanFocus;
			set {
				if (!addingView && IsInitialized && SuperView?.CanFocus == false && value) {
					throw new InvalidOperationException ("Cannot set CanFocus to true if the SuperView CanFocus is false!");
				}
				if (base.CanFocus != value) {
					base.CanFocus = value;

					switch (value) {
					case false when tabIndex > -1:
						TabIndex = -1;
						break;
					case true when SuperView?.CanFocus == false && addingView:
						SuperView.CanFocus = true;
						break;
					}

					if (value && tabIndex == -1) {
						TabIndex = SuperView != null ? SuperView.tabIndexes.IndexOf (this) : -1;
					}
					TabStop = value;

					if (!value && SuperView?.Focused == this) {
						SuperView.focused = null;
					}
					if (!value && HasFocus) {
						SetHasFocus (false, this);
						SuperView?.EnsureFocus ();
						if (SuperView != null && SuperView.Focused == null) {
							SuperView.FocusNext ();
							if (SuperView.Focused == null && Application.Current != null) {
								Application.Current.FocusNext ();
							}
							Application.EnsuresTopOnFront ();
						}
					}
					if (subviews != null && IsInitialized) {
						foreach (var view in subviews) {
							if (view.CanFocus != value) {
								if (!value) {
									view.oldCanFocus = view.CanFocus;
									view.oldTabIndex = view.tabIndex;
									view.CanFocus = false;
									view.tabIndex = -1;
								} else {
									if (addingView) {
										view.addingView = true;
									}
									view.CanFocus = view.oldCanFocus;
									view.tabIndex = view.oldTabIndex;
									view.addingView = false;
								}
							}
						}
					}
					OnCanFocusChanged ();
					SetNeedsDisplay ();
				}
			}
		}

		internal Rect NeedDisplay { get; private set; } = Rect.Empty;

		// The frame for the object. Superview relative.
		Rect frame;

		/// <summary>
		/// Gets or sets an identifier for the view;
		/// </summary>
		/// <value>The identifier.</value>
		/// <remarks>The id should be unique across all Views that share a SuperView.</remarks>
		public ustring Id { get; set; } = "";

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
			get => frame;
			set {
				frame = value;
				TextFormatter.Size = GetBoundsTextFormatterSize ();
				SetNeedsLayout ();
				SetNeedsDisplay ();
			}
		}

		///// <summary>
		///// Gets an enumerator that enumerates the subviews in this view.
		///// </summary>
		///// <returns>The enumerator.</returns>
		//public IEnumerator GetEnumerator ()
		//{
		//	foreach (var v in InternalSubviews)
		//		yield return v;
		//}

		LayoutStyle layoutStyle;

		/// <summary>
		/// Controls how the View's <see cref="Frame"/> is computed during the LayoutSubviews method, if the style is set to
		/// <see cref="Terminal.Gui.LayoutStyle.Absolute"/>, 
		/// LayoutSubviews does not change the <see cref="Frame"/>. If the style is <see cref="Terminal.Gui.LayoutStyle.Computed"/>
		/// the <see cref="Frame"/> is updated using
		/// the <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties.
		/// </summary>
		/// <value>The layout style.</value>
		public LayoutStyle LayoutStyle {
			get => layoutStyle;
			set {
				layoutStyle = value;
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// The bounds represent the View-relative rectangle used for this view; the area inside of the view.
		/// </summary>
		/// <value>The bounds.</value>
		/// <remarks>
		/// <para>
		/// Updates to the Bounds update the <see cref="Frame"/>,
		/// and has the same side effects as updating the <see cref="Frame"/>.
		/// </para>
		/// <para>
		/// Because <see cref="Bounds"/> coordinates are relative to the upper-left corner of the <see cref="View"/>, 
		/// the coordinates of the upper-left corner of the rectangle returned by this property are (0,0). 
		/// Use this property to obtain the size and coordinates of the client area of the 
		/// control for tasks such as drawing on the surface of the control.
		/// </para>
		/// </remarks>
		public Rect Bounds {
			get => new Rect (Point.Empty, Frame.Size);
			set => Frame = new Rect (frame.Location, value.Size);
		}

		Pos x, y;

		/// <summary>
		/// Gets or sets the X position for the view (the column). Only used if the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The X Position.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Pos X {
			get => x;
			set {
				if (ForceValidatePosDim && !ValidatePosDim (x, value)) {
					throw new ArgumentException ();
				}

				x = value;

				ProcessResizeView ();
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
			get => y;
			set {
				if (ForceValidatePosDim && !ValidatePosDim (y, value)) {
					throw new ArgumentException ();
				}

				y = value;

				ProcessResizeView ();
			}
		}
		Dim width, height;

		/// <summary>
		/// Gets or sets the width of the view. Only used the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The width.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Dim Width {
			get => width;
			set {
				if (ForceValidatePosDim && !ValidatePosDim (width, value)) {
					throw new ArgumentException ("ForceValidatePosDim is enabled", nameof (Width));
				}

				width = value;

				if (ForceValidatePosDim) {
					var isValidNewAutSize = autoSize && IsValidAutoSizeWidth (width);

					if (IsAdded && autoSize && !isValidNewAutSize) {
						throw new InvalidOperationException ("Must set AutoSize to false before set the Width.");
					}
				}
				ProcessResizeView ();
			}
		}

		/// <summary>
		/// Gets or sets the height of the view. Only used the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The height.</value>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		public Dim Height {
			get => height;
			set {
				if (ForceValidatePosDim && !ValidatePosDim (height, value)) {
					throw new ArgumentException ("ForceValidatePosDim is enabled", nameof (Height));
				}

				height = value;

				if (ForceValidatePosDim) {
					var isValidNewAutSize = autoSize && IsValidAutoSizeHeight (height);

					if (IsAdded && autoSize && !isValidNewAutSize) {
						throw new InvalidOperationException ("Must set AutoSize to false before set the Height.");
					}
				}
				ProcessResizeView ();
			}
		}

		/// <summary>
		/// Forces validation with <see cref="Terminal.Gui.LayoutStyle.Computed"/> layout
		///  to avoid breaking the <see cref="Pos"/> and <see cref="Dim"/> settings.
		/// </summary>
		public bool ForceValidatePosDim { get; set; }

		bool ValidatePosDim (object oldValue, object newValue)
		{
			if (!IsInitialized || layoutStyle == LayoutStyle.Absolute || oldValue == null || oldValue.GetType () == newValue.GetType () || this is Toplevel) {
				return true;
			}
			if (layoutStyle == LayoutStyle.Computed) {
				if (oldValue.GetType () != newValue.GetType () && !(newValue is Pos.PosAbsolute || newValue is Dim.DimAbsolute)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Verifies if the minimum width or height can be sets in the view.
		/// </summary>
		/// <param name="size">The size.</param>
		/// <returns><see langword="true"/> if the size can be set, <see langword="false"/> otherwise.</returns>
		public bool GetMinWidthHeight (out Size size)
		{
			size = Size.Empty;

			if (!AutoSize && !ustring.IsNullOrEmpty (TextFormatter.Text)) {
				switch (TextFormatter.IsVerticalDirection (TextDirection)) {
				case true:
					var colWidth = TextFormatter.GetSumMaxCharWidth (new List<ustring> { TextFormatter.Text }, 0, 1);
					if (frame.Width < colWidth && (Width == null || (Bounds.Width >= 0 && Width is Dim.DimAbsolute
						&& Width.Anchor (0) >= 0 && Width.Anchor (0) < colWidth))) {
						size = new Size (colWidth, Bounds.Height);
						return true;
					}
					break;
				default:
					if (frame.Height < 1 && (Height == null || (Height is Dim.DimAbsolute && Height.Anchor (0) == 0))) {
						size = new Size (Bounds.Width, 1);
						return true;
					}
					break;
				}
			}
			return false;
		}

		/// <summary>
		/// Sets the minimum width or height if the view can be resized.
		/// </summary>
		/// <returns><see langword="true"/> if the size can be set, <see langword="false"/> otherwise.</returns>
		public bool SetMinWidthHeight ()
		{
			if (GetMinWidthHeight (out Size size)) {
				Bounds = new Rect (Bounds.Location, size);
				TextFormatter.Size = GetBoundsTextFormatterSize ();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets or sets the <see cref="Terminal.Gui.TextFormatter"/> which can be handled differently by any derived class.
		/// </summary>
		public TextFormatter TextFormatter { get; set; }

		/// <summary>
		/// Returns the container for this view, or null if this view has not been added to a container.
		/// </summary>
		/// <value>The super view.</value>
		public View SuperView => container;

		/// <summary>
		/// Initializes a new instance of a <see cref="Terminal.Gui.LayoutStyle.Absolute"/> <see cref="View"/> class with the absolute
		/// dimensions specified in the <see langword="frame"/> parameter. 
		/// </summary>
		/// <param name="frame">The region covered by this view.</param>
		/// <remarks>
		/// This constructor initialize a View with a <see cref="LayoutStyle"/> of <see cref="Terminal.Gui.LayoutStyle.Absolute"/>.
		/// Use <see cref="View"/> to initialize a View with  <see cref="LayoutStyle"/> of <see cref="Terminal.Gui.LayoutStyle.Computed"/> 
		/// </remarks>
		public View (Rect frame)
		{
			Initialize (ustring.Empty, frame, LayoutStyle.Absolute, TextDirection.LeftRight_TopBottom);
		}

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
		/// <param name="border">The <see cref="Border"/>.</param>
		public View (Rect rect, ustring text, Border border = null)
		{
			Initialize (text, rect, LayoutStyle.Absolute, TextDirection.LeftRight_TopBottom, border);
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
		/// <param name="border">The <see cref="Border"/>.</param>
		public View (ustring text, TextDirection direction = TextDirection.LeftRight_TopBottom, Border border = null)
		{
			Initialize (text, Rect.Empty, LayoutStyle.Computed, direction, border);
		}

		void Initialize (ustring text, Rect rect, LayoutStyle layoutStyle = LayoutStyle.Computed,
		    TextDirection direction = TextDirection.LeftRight_TopBottom, Border border = null)
		{
			TextFormatter = new TextFormatter ();
			TextFormatter.HotKeyChanged += TextFormatter_HotKeyChanged;
			TextDirection = direction;
			Border = border;
			if (Border != null) {
				Border.Child = this;
			}
			shortcutHelper = new ShortcutHelper ();
			CanFocus = false;
			TabIndex = -1;
			TabStop = false;
			LayoutStyle = layoutStyle;
			// BUGBUG: CalcRect doesn't account for line wrapping

			var r = rect.IsEmpty ? TextFormatter.CalcRect (0, 0, text, direction) : rect;
			Frame = r;

			Text = text;
			UpdateTextFormatterText ();
			ProcessResizeView ();
		}

		/// <summary>
		/// Can be overridden if the <see cref="Terminal.Gui.TextFormatter.Text"/> has
		///  different format than the default.
		/// </summary>
		protected virtual void UpdateTextFormatterText ()
		{
			TextFormatter.Text = text;
		}

		/// <summary>
		/// Can be overridden if the view resize behavior is
		///  different than the default.
		/// </summary>
		protected virtual void ProcessResizeView ()
		{
			var actX = x is Pos.PosAbsolute ? x.Anchor (0) : frame.X;
			var actY = y is Pos.PosAbsolute ? y.Anchor (0) : frame.Y;
			Rect oldFrame = frame;

			if (AutoSize) {
				var s = GetAutoSize ();
				var w = width is Dim.DimAbsolute && width.Anchor (0) > s.Width ? width.Anchor (0) : s.Width;
				var h = height is Dim.DimAbsolute && height.Anchor (0) > s.Height ? height.Anchor (0) : s.Height;
				frame = new Rect (new Point (actX, actY), new Size (w, h));
			} else {
				var w = width is Dim.DimAbsolute ? width.Anchor (0) : frame.Width;
				var h = height is Dim.DimAbsolute ? height.Anchor (0) : frame.Height;
				frame = new Rect (new Point (actX, actY), new Size (w, h));
				SetMinWidthHeight ();
			}
			TextFormatter.Size = GetBoundsTextFormatterSize ();
			SetNeedsLayout ();
			SetNeedsDisplay ();
		}

		void TextFormatter_HotKeyChanged (Key obj)
		{
			HotKeyChanged?.Invoke (obj);
		}

		/// <summary>
		/// Sets a flag indicating this view needs to be redisplayed because its state has changed.
		/// </summary>
		public void SetNeedsDisplay ()
		{
			SetNeedsDisplay (Bounds);
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

		/// <summary>
		/// Flags the view-relative region on this View as needing to be repainted.
		/// </summary>
		/// <param name="region">The view-relative region that must be flagged for repaint.</param>
		public void SetNeedsDisplay (Rect region)
		{
			if (NeedDisplay.IsEmpty)
				NeedDisplay = region;
			else {
				var x = Math.Min (NeedDisplay.X, region.X);
				var y = Math.Min (NeedDisplay.Y, region.Y);
				var w = Math.Max (NeedDisplay.Width, region.Width);
				var h = Math.Max (NeedDisplay.Height, region.Height);
				NeedDisplay = new Rect (x, y, w, h);
			}
			container?.SetChildNeedsDisplay ();

			if (subviews == null)
				return;

			foreach (var view in subviews)
				if (view.Frame.IntersectsWith (region)) {
					var childRegion = Rect.Intersect (view.Frame, region);
					childRegion.X -= view.Frame.X;
					childRegion.Y -= view.Frame.Y;
					view.SetNeedsDisplay (childRegion);
				}
		}

		internal bool ChildNeedsDisplay { get; private set; }

		/// <summary>
		/// Indicates that any child views (in the <see cref="Subviews"/> list) need to be repainted.
		/// </summary>
		public void SetChildNeedsDisplay ()
		{
			ChildNeedsDisplay = true;
			if (container != null)
				container.SetChildNeedsDisplay ();
		}

		internal bool addingView;

		/// <summary>
		///   Adds a subview (child) to this view.
		/// </summary>
		/// <remarks>
		/// The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. 
		/// See also <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/> 
		/// </remarks>
		public virtual void Add (View view)
		{
			if (view == null)
				return;
			if (subviews == null) {
				subviews = new List<View> ();
			}
			if (tabIndexes == null) {
				tabIndexes = new List<View> ();
			}
			subviews.Add (view);
			tabIndexes.Add (view);
			view.container = this;
			if (view.CanFocus) {
				addingView = true;
				if (SuperView?.CanFocus == false) {
					SuperView.addingView = true;
					SuperView.CanFocus = true;
					SuperView.addingView = false;
				}
				CanFocus = true;
				view.tabIndex = tabIndexes.IndexOf (view);
				addingView = false;
			}
			if (view.Enabled && !Enabled) {
				view.oldEnabled = true;
				view.Enabled = false;
			}
			SetNeedsLayout ();
			SetNeedsDisplay ();
			OnAdded (view);
			if (IsInitialized) {
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
			if (views == null)
				return;
			foreach (var view in views)
				Add (view);
		}

		/// <summary>
		///   Removes all subviews (children) added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		public virtual void RemoveAll ()
		{
			if (subviews == null)
				return;

			while (subviews.Count > 0) {
				Remove (subviews [0]);
			}
		}

		/// <summary>
		///   Removes a subview added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Remove (View view)
		{
			if (view == null || subviews == null)
				return;

			var touched = view.Frame;
			subviews.Remove (view);
			tabIndexes.Remove (view);
			view.container = null;
			view.tabIndex = -1;
			SetNeedsLayout ();
			SetNeedsDisplay ();
			foreach (var v in subviews) {
				if (v.Frame.IntersectsWith (touched))
					view.SetNeedsDisplay ();
			}
			OnRemoved (view);
			if (focused == view) {
				focused = null;
			}
		}

		void PerformActionForSubview (View subview, Action<View> action)
		{
			if (subviews.Contains (subview)) {
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
				subviews.Remove (x);
				subviews.Add (x);
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
				subviews.Remove (x);
				subviews.Insert (0, subview);
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
				var idx = subviews.IndexOf (x);
				if (idx > 0) {
					subviews.Remove (x);
					subviews.Insert (idx - 1, x);
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
				var idx = subviews.IndexOf (x);
				if (idx + 1 < subviews.Count) {
					subviews.Remove (x);
					subviews.Insert (idx + 1, x);
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
		/// Converts a view-relative (col,row) position to a screen-relative position (col,row). The values are optionally clamped to the screen dimensions.
		/// </summary>
		/// <param name="col">View-relative column.</param>
		/// <param name="row">View-relative row.</param>
		/// <param name="rcol">Absolute column; screen-relative.</param>
		/// <param name="rrow">Absolute row; screen-relative.</param>
		/// <param name="clipped">Whether to clip the result of the ViewToScreen method, if set to <see langword="true"/>, the rcol, rrow values are clamped to the screen (terminal) dimensions (0..TerminalDim-1).</param>
		internal void ViewToScreen (int col, int row, out int rcol, out int rrow, bool clipped = false)
		{
			// Computes the real row, col relative to the screen.
			rrow = row + frame.Y;
			rcol = col + frame.X;

			var curContainer = container;
			while (curContainer != null) {
				rrow += curContainer.frame.Y;
				rcol += curContainer.frame.X;
				curContainer = curContainer.container;
			}

			// The following ensures that the cursor is always in the screen boundaries.
			if (clipped) {
				rrow = Math.Min (rrow, Driver.Rows - 1);
				rcol = Math.Min (rcol, Driver.Cols - 1);
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
				return new Point (x - Frame.X, y - frame.Y);
			} else {
				var parent = SuperView.ScreenToView (x, y);
				return new Point (parent.X - frame.X, parent.Y - frame.Y);
			}
		}

		/// <summary>
		/// Converts a region in view-relative coordinates to screen-relative coordinates.
		/// </summary>
		internal Rect ViewToScreen (Rect region)
		{
			ViewToScreen (region.X, region.Y, out var x, out var y, clipped: false);
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
		/// Sets the <see cref="ConsoleDriver"/>'s clip region to the current View's <see cref="Bounds"/>.
		/// </summary>
		/// <returns>The existing driver's clip region, which can be then re-applied by setting <c><see cref="Driver"/>.Clip</c> (<see cref="ConsoleDriver.Clip"/>).</returns>
		/// <remarks>
		/// <see cref="Bounds"/> is View-relative.
		/// </remarks>
		public Rect ClipToBounds ()
		{
			return SetClip (Bounds);
		}

		/// <summary>
		/// Sets the clip region to the specified view-relative region.
		/// </summary>
		/// <returns>The previous screen-relative clip region.</returns>
		/// <param name="region">View-relative clip region.</param>
		public Rect SetClip (Rect region)
		{
			var previous = Driver.Clip;
			Driver.Clip = Rect.Intersect (previous, ViewToScreen (region));
			return previous;
		}

		/// <summary>
		/// Draws a frame in the current view, clipped by the boundary of this view
		/// </summary>
		/// <param name="region">View-relative region for the frame to be drawn.</param>
		/// <param name="padding">The padding to add around the outside of the drawn frame.</param>
		/// <param name="fill">If set to <see langword="true"/> it fill will the contents.</param>
		public void DrawFrame (Rect region, int padding = 0, bool fill = false)
		{
			var scrRect = ViewToScreen (region);
			var savedClip = ClipToBounds ();
			Driver.DrawWindowFrame (scrRect, padding + 1, padding + 1, padding + 1, padding + 1, border: true, fill: fill);
			Driver.Clip = savedClip;
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
		/// <param name="col">Col.</param>
		/// <param name="row">Row.</param>
		/// <param name="clipped">Whether to clip the result of the ViewToScreen method,
		///  if set to <see langword="true"/>, the col, row values are clamped to the screen (terminal) dimensions (0..TerminalDim-1).</param>
		public void Move (int col, int row, bool clipped = false)
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

			if (focused == null && SuperView != null) {
				SuperView.EnsureFocus ();
			} else if (focused?.Visible == true && focused?.Enabled == true && focused?.Frame.Width > 0 && focused.Frame.Height > 0) {
				focused.PositionCursor ();
			} else if (focused?.Visible == true && focused?.Enabled == false) {
				focused = null;
			} else if (CanFocus && HasFocus && Visible && Frame.Width > 0 && Frame.Height > 0) {
				Move (TextFormatter.HotKeyPos == -1 ? 0 : TextFormatter.CursorPosition, 0);
			} else {
				Move (frame.X, frame.Y);
			}
		}

		bool hasFocus;

		/// <inheritdoc/>
		public override bool HasFocus => hasFocus;

		void SetHasFocus (bool value, View view, bool force = false)
		{
			if (hasFocus != value || force) {
				hasFocus = value;
				if (value) {
					OnEnter (view);
				} else {
					OnLeave (view);
				}
				SetNeedsDisplay ();
			}

			// Remove focus down the chain of subviews if focus is removed
			if (!value && focused != null) {
				var f = focused;
				f.OnLeave (view);
				f.SetHasFocus (false, view);
				focused = null;
			}
		}

		/// <summary>
		/// Defines the event arguments for <see cref="SetFocus(View)"/>
		/// </summary>
		public class FocusEventArgs : EventArgs {
			/// <summary>
			/// Constructs.
			/// </summary>
			/// <param name="view">The view that gets or loses focus.</param>
			public FocusEventArgs (View view) { View = view; }
			/// <summary>
			/// Indicates if the current focus event has already been processed and the driver should stop notifying any other event subscriber.
			/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
			/// </summary>
			public bool Handled { get; set; }
			/// <summary>
			/// Indicates the current view that gets or loses focus.
			/// </summary>
			public View View { get; set; }
		}

		/// <summary>
		/// Method invoked when a subview is being added to this view.
		/// </summary>
		/// <param name="view">The subview being added.</param>
		public virtual void OnAdded (View view)
		{
			view.IsAdded = true;
			view.x = view.x ?? view.frame.X;
			view.y = view.y ?? view.frame.Y;
			view.width = view.width ?? view.frame.Width;
			view.height = view.height ?? view.frame.Height;

			view.Added?.Invoke (this);
		}

		/// <summary>
		/// Method invoked when a subview is being removed from this view.
		/// </summary>
		/// <param name="view">The subview being removed.</param>
		public virtual void OnRemoved (View view)
		{
			view.IsAdded = false;
			view.Removed?.Invoke (this);
		}

		/// <inheritdoc/>
		public override bool OnEnter (View view)
		{
			var args = new FocusEventArgs (view);
			Enter?.Invoke (args);
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
			Leave?.Invoke (args);
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
		public View Focused => focused;

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

		ColorScheme colorScheme;

		/// <summary>
		/// The color scheme for this view, if it is not defined, it returns the <see cref="SuperView"/>'s
		/// color scheme.
		/// </summary>
		public virtual ColorScheme ColorScheme {
			get {
				if (colorScheme == null) {
					return SuperView?.ColorScheme;
				}
				return colorScheme;
			}
			set {
				if (colorScheme != value) {
					colorScheme = value;
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
			if (row > frame.Height - 1 || col > frame.Width - 1)
				return;
			Move (col, row);
			Driver.AddRune (ch);
		}

		/// <summary>
		/// Removes the <see cref="SetNeedsDisplay()"/> and the <see cref="ChildNeedsDisplay"/> setting on this view.
		/// </summary>
		protected void ClearNeedsDisplay ()
		{
			NeedDisplay = Rect.Empty;
			ChildNeedsDisplay = false;
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

			var clipRect = new Rect (Point.Empty, frame.Size);

			if (ColorScheme != null) {
				Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
			}

			if (!IgnoreBorderPropertyOnRedraw && Border != null) {
				Border.DrawContent (this);
			} else if (ustring.IsNullOrEmpty (TextFormatter.Text) &&
				(GetType ().IsNestedPublic && !IsOverridden (this, "Redraw") || GetType ().Name == "View") &&
				(!NeedDisplay.IsEmpty || ChildNeedsDisplay || LayoutNeeded)) {

				if (ColorScheme != null) {
					Driver.SetAttribute (GetNormalColor ());
					Clear ();
					SetChildNeedsDisplay ();
				}
			}

			if (!ustring.IsNullOrEmpty (TextFormatter.Text)) {
				Rect containerBounds = GetContainerBounds ();
				Clear (ViewToScreen (GetNeedDisplay (containerBounds)));
				SetChildNeedsDisplay ();
				// Draw any Text
				if (TextFormatter != null) {
					TextFormatter.NeedsFormat = true;
				}
				TextFormatter?.Draw (ViewToScreen (Bounds), HasFocus ? GetFocusColor () : GetNormalColor (),
				    HasFocus ? ColorScheme.HotFocus : GetHotNormalColor (),
				    containerBounds);
			}

			// Invoke DrawContentEvent
			OnDrawContent (bounds);

			if (subviews != null) {
				foreach (var view in subviews) {
					if (!view.NeedDisplay.IsEmpty || view.ChildNeedsDisplay || view.LayoutNeeded) {
						if (view.Frame.IntersectsWith (clipRect) && (view.Frame.IntersectsWith (bounds) || bounds.X < 0 || bounds.Y < 0)) {
							if (view.LayoutNeeded)
								view.LayoutSubviews ();

							// Draw the subview
							// Use the view's bounds (view-relative; Location will always be (0,0)
							if (view.Visible && view.Frame.Width > 0 && view.Frame.Height > 0) {
								var rect = view.Bounds;
								view.OnDrawContent (rect);
								view.Redraw (rect);
								view.OnDrawContentComplete (rect);
							}
						}
						view.NeedDisplay = Rect.Empty;
						view.ChildNeedsDisplay = false;
					}
				}
			}

			// Invoke DrawContentCompleteEvent
			OnDrawContentComplete (bounds);

			ClearLayoutNeeded ();
			ClearNeedsDisplay ();
		}

		Rect GetNeedDisplay (Rect containerBounds)
		{
			Rect rect = NeedDisplay;
			if (!containerBounds.IsEmpty) {
				rect.Width = Math.Min (NeedDisplay.Width, containerBounds.Width);
				rect.Height = Math.Min (NeedDisplay.Height, containerBounds.Height);
			}

			return rect;
		}

		Rect GetContainerBounds ()
		{
			var containerBounds = SuperView == null ? default : SuperView.ViewToScreen (SuperView.Bounds);
			var driverClip = Driver == null ? Rect.Empty : Driver.Clip;
			containerBounds.X = Math.Max (containerBounds.X, driverClip.X);
			containerBounds.Y = Math.Max (containerBounds.Y, driverClip.Y);
			var lenOffset = (driverClip.X + driverClip.Width) - (containerBounds.X + containerBounds.Width);
			if (containerBounds.X + containerBounds.Width > driverClip.X + driverClip.Width) {
				containerBounds.Width = Math.Max (containerBounds.Width + lenOffset, 0);
			} else {
				containerBounds.Width = Math.Min (containerBounds.Width, driverClip.Width);
			}
			lenOffset = (driverClip.Y + driverClip.Height) - (containerBounds.Y + containerBounds.Height);
			if (containerBounds.Y + containerBounds.Height > driverClip.Y + driverClip.Height) {
				containerBounds.Height = Math.Max (containerBounds.Height + lenOffset, 0);
			} else {
				containerBounds.Height = Math.Min (containerBounds.Height, driverClip.Height);
			}
			return containerBounds;
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
		public event Action<Rect> DrawContent;

		/// <summary>
		/// Enables overrides to draw infinitely scrolled content and/or a background behind added controls. 
		/// </summary>
		/// <param name="viewport">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called before any subviews added with <see cref="Add(View)"/> have been drawn. 
		/// </remarks>
		public virtual void OnDrawContent (Rect viewport)
		{
			DrawContent?.Invoke (viewport);
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
		public event Action<Rect> DrawContentComplete;

		/// <summary>
		/// Enables overrides after completed drawing infinitely scrolled content and/or a background behind removed controls.
		/// </summary>
		/// <param name="viewport">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
		/// </remarks>
		public virtual void OnDrawContentComplete (Rect viewport)
		{
			DrawContentComplete?.Invoke (viewport);
		}

		/// <summary>
		/// Causes the specified subview to have focus.
		/// </summary>
		/// <param name="view">View.</param>
		void SetFocus (View view)
		{
			if (view == null)
				return;
			//Console.WriteLine ($"Request to focus {view}");
			if (!view.CanFocus || !view.Visible || !view.Enabled)
				return;
			if (focused?.hasFocus == true && focused == view)
				return;

			// Make sure that this view is a subview
			View c;
			for (c = view.container; c != null; c = c.container)
				if (c == this)
					break;
			if (c == null)
				throw new ArgumentException ("the specified view is not part of the hierarchy of this view");

			if (focused != null)
				focused.SetHasFocus (false, view);

			var f = focused;
			focused = view;
			focused.SetHasFocus (true, f);
			focused.EnsureFocus ();

			// Send focus upwards
			SuperView?.SetFocus (this);
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

			SuperView?.SetFocus (this);
		}

		/// <summary>
		/// Defines the event arguments for <see cref="KeyEvent"/>
		/// </summary>
		public class KeyEventEventArgs : EventArgs {
			/// <summary>
			/// Constructs.
			/// </summary>
			/// <param name="ke"></param>
			public KeyEventEventArgs (KeyEvent ke) => KeyEvent = ke;
			/// <summary>
			/// The <see cref="KeyEvent"/> for the event.
			/// </summary>
			public KeyEvent KeyEvent { get; set; }
			/// <summary>
			/// Indicates if the current Key event has already been processed and the driver should stop notifying any other event subscriber.
			/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
			/// </summary>
			public bool Handled { get; set; } = false;
		}

		/// <summary>
		/// Invoked when a character key is pressed and occurs after the key up event.
		/// </summary>
		public event Action<KeyEventEventArgs> KeyPress;

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (args);
			if (args.Handled)
				return true;
			if (Focused?.Enabled == true) {
				Focused?.KeyPress?.Invoke (args);
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
				MostFocused?.KeyPress?.Invoke (args);
				if (args.Handled)
					return true;
			}
			if (MostFocused?.Enabled == true && MostFocused?.ProcessKey (keyEvent) == true)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;

			foreach (var view in subviews)
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
			KeyPress?.Invoke (args);
			if (args.Handled)
				return true;
			if (MostFocused?.Enabled == true) {
				MostFocused?.KeyPress?.Invoke (args);
				if (args.Handled)
					return true;
			}
			if (MostFocused?.Enabled == true && MostFocused?.ProcessKey (keyEvent) == true)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;

			foreach (var view in subviews)
				if (view.Enabled && view.ProcessColdKey (keyEvent))
					return true;
			return false;
		}

		/// <summary>
		/// Invoked when a key is pressed.
		/// </summary>
		public event Action<KeyEventEventArgs> KeyDown;

		/// <inheritdoc/>
		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyDown?.Invoke (args);
			if (args.Handled) {
				return true;
			}
			if (Focused?.Enabled == true) {
				Focused.KeyDown?.Invoke (args);
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
		public event Action<KeyEventEventArgs> KeyUp;

		/// <inheritdoc/>
		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyUp?.Invoke (args);
			if (args.Handled) {
				return true;
			}
			if (Focused?.Enabled == true) {
				Focused.KeyUp?.Invoke (args);
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
			if (focused == null && subviews?.Count > 0) {
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

			if (tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			foreach (var view in tabIndexes) {
				if (view.CanFocus && view.tabStop && view.Visible && view.Enabled) {
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

			if (tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			for (var i = tabIndexes.Count; i > 0;) {
				i--;

				var v = tabIndexes [i];
				if (v.CanFocus && v.tabStop && v.Visible && v.Enabled) {
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
			if (tabIndexes == null || tabIndexes.Count == 0)
				return false;

			if (focused == null) {
				FocusLast ();
				return focused != null;
			}

			var focusedIdx = -1;
			for (var i = tabIndexes.Count; i > 0;) {
				i--;
				var w = tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusPrev ())
						return true;
					focusedIdx = i;
					continue;
				}
				if (w.CanFocus && focusedIdx != -1 && w.tabStop && w.Visible && w.Enabled) {
					focused.SetHasFocus (false, w);

					if (w.CanFocus && w.tabStop && w.Visible && w.Enabled)
						w.FocusLast ();

					SetFocus (w);
					return true;
				}
			}
			if (focused != null) {
				focused.SetHasFocus (false, this);
				focused = null;
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
			if (tabIndexes == null || tabIndexes.Count == 0)
				return false;

			if (focused == null) {
				FocusFirst ();
				return focused != null;
			}
			var n = tabIndexes.Count;
			var focusedIdx = -1;
			for (var i = 0; i < n; i++) {
				var w = tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusNext ())
						return true;
					focusedIdx = i;
					continue;
				}
				if (w.CanFocus && focusedIdx != -1 && w.tabStop && w.Visible && w.Enabled) {
					focused.SetHasFocus (false, w);

					if (w.CanFocus && w.tabStop && w.Visible && w.Enabled)
						w.FocusFirst ();

					SetFocus (w);
					return true;
				}
			}
			if (focused != null) {
				focused.SetHasFocus (false, this);
				focused = null;
			}
			return false;
		}

		View GetMostFocused (View view)
		{
			if (view == null) {
				return null;
			}

			return view.focused != null ? GetMostFocused (view.focused) : view;
		}

		/// <summary>
		/// Sets the View's <see cref="Frame"/> to the relative coordinates if its container, given the <see cref="Frame"/> for its container.
		/// </summary>
		/// <param name="hostFrame">The screen-relative frame for the host.</param>
		/// <remarks>
		/// Reminder: <see cref="Frame"/> is superview-relative; <see cref="Bounds"/> is view-relative.
		/// </remarks>
		internal void SetRelativeLayout (Rect hostFrame)
		{
			int actW, actH, actX, actY;
			var s = Size.Empty;

			if (AutoSize) {
				s = GetAutoSize ();
			}

			if (x is Pos.PosCenter) {
				if (width == null) {
					actW = AutoSize ? s.Width : hostFrame.Width;
				} else {
					actW = width.Anchor (hostFrame.Width);
					actW = AutoSize && s.Width > actW ? s.Width : actW;
				}
				actX = x.Anchor (hostFrame.Width - actW);
			} else {
				actX = x?.Anchor (hostFrame.Width) ?? 0;

				actW = Math.Max (CalculateActualWidth (width, hostFrame, actX, s), 0);
			}

			if (y is Pos.PosCenter) {
				if (height == null) {
					actH = AutoSize ? s.Height : hostFrame.Height;
				} else {
					actH = height.Anchor (hostFrame.Height);
					actH = AutoSize && s.Height > actH ? s.Height : actH;
				}
				actY = y.Anchor (hostFrame.Height - actH);
			} else {
				actY = y?.Anchor (hostFrame.Height) ?? 0;

				actH = Math.Max (CalculateActualHeight (height, hostFrame, actY, s), 0);
			}

			var r = new Rect (actX, actY, actW, actH);
			if (Frame != r) {
				Frame = r;
				if (!SetMinWidthHeight ())
					TextFormatter.Size = GetBoundsTextFormatterSize ();
			}
		}

		private int CalculateActualWidth (Dim width, Rect hostFrame, int actX, Size s)
		{
			int actW;
			switch (width) {
			case null:
				actW = AutoSize ? s.Width : hostFrame.Width;
				break;
			case Dim.DimCombine combine:
				int leftActW = CalculateActualWidth (combine.left, hostFrame, actX, s);
				int rightActW = CalculateActualWidth (combine.right, hostFrame, actX, s);
				if (combine.add) {
					actW = leftActW + rightActW;
				} else {
					actW = leftActW - rightActW;
				}
				actW = AutoSize && s.Width > actW ? s.Width : actW;
				break;
			case Dim.DimFactor factor when !factor.IsFromRemaining ():
				actW = width.Anchor (hostFrame.Width);
				actW = AutoSize && s.Width > actW ? s.Width : actW;
				break;
			default:
				actW = Math.Max (width.Anchor (hostFrame.Width - actX), 0);
				actW = AutoSize && s.Width > actW ? s.Width : actW;
				break;
			}

			return actW;
		}

		private int CalculateActualHeight (Dim height, Rect hostFrame, int actY, Size s)
		{
			int actH;
			switch (height) {
			case null:
				actH = AutoSize ? s.Height : hostFrame.Height;
				break;
			case Dim.DimCombine combine:
				int leftActH = CalculateActualHeight (combine.left, hostFrame, actY, s);
				int rightActH = CalculateActualHeight (combine.right, hostFrame, actY, s);
				if (combine.add) {
					actH = leftActH + rightActH;
				} else {
					actH = leftActH - rightActH;
				}
				actH = AutoSize && s.Height > actH ? s.Height : actH;
				break;
			case Dim.DimFactor factor when !factor.IsFromRemaining ():
				actH = height.Anchor (hostFrame.Height);
				actH = AutoSize && s.Height > actH ? s.Height : actH;
				break;
			default:
				actH = Math.Max (height.Anchor (hostFrame.Height - actY), 0);
				actH = AutoSize && s.Height > actH ? s.Height : actH;
				break;
			}

			return actH;
		}

		// https://en.wikipedia.org/wiki/Topological_sorting
		List<View> TopologicalSort (IEnumerable<View> nodes, ICollection<(View From, View To)> edges)
		{
			var result = new List<View> ();

			// Set of all nodes with no incoming edges
			var noEdgeNodes = new HashSet<View> (nodes.Where (n => edges.All (e => !e.To.Equals (n))));

			while (noEdgeNodes.Any ()) {
				//  remove a node n from S
				var n = noEdgeNodes.First ();
				noEdgeNodes.Remove (n);

				// add n to tail of L
				if (n != this?.SuperView)
					result.Add (n);

				// for each node m with an edge e from n to m do
				foreach (var e in edges.Where (e => e.From.Equals (n)).ToArray ()) {
					var m = e.To;

					// remove edge e from the graph
					edges.Remove (e);

					// if m has no other incoming edges then
					if (edges.All (me => !me.To.Equals (m)) && m != this?.SuperView) {
						// insert m into S
						noEdgeNodes.Add (m);
					}
				}
			}

			if (edges.Any ()) {
				(var from, var to) = edges.First ();
				if (from != Application.Top) {
					if (!ReferenceEquals (from, to)) {
						throw new InvalidOperationException ($"TopologicalSort (for Pos/Dim) cannot find {from} linked with {to}. Did you forget to add it to {this}?");
					} else {
						throw new InvalidOperationException ("TopologicalSort encountered a recursive cycle in the relative Pos/Dim in the views of " + this);
					}
				}
			}

			// return L (a topologically sorted order)
			return result;
		}

		/// <summary>
		/// Event arguments for the <see cref="LayoutComplete"/> event.
		/// </summary>
		public class LayoutEventArgs : EventArgs {
			/// <summary>
			/// The view-relative bounds of the <see cref="View"/> before it was laid out.
			/// </summary>
			public Rect OldBounds { get; set; }
		}

		/// <summary>
		/// Fired after the View's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public event Action<LayoutEventArgs> LayoutStarted;

		/// <summary>
		/// Raises the <see cref="LayoutStarted"/> event. Called from  <see cref="LayoutSubviews"/> before any subviews have been laid out.
		/// </summary>
		internal virtual void OnLayoutStarted (LayoutEventArgs args)
		{
			LayoutStarted?.Invoke (args);
		}

		/// <summary>
		/// Fired after the View's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public event Action<LayoutEventArgs> LayoutComplete;

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
			LayoutComplete?.Invoke (args);
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

			var oldBounds = Bounds;
			OnLayoutStarted (new LayoutEventArgs () { OldBounds = oldBounds });

			TextFormatter.Size = GetBoundsTextFormatterSize ();


			// Sort out the dependencies of the X, Y, Width, Height properties
			var nodes = new HashSet<View> ();
			var edges = new HashSet<(View, View)> ();

			void CollectPos (Pos pos, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
			{
				switch (pos) {
				case Pos.PosView pv:
					if (pv.Target != this) {
						nEdges.Add ((pv.Target, from));
					}
					foreach (var v in from.InternalSubviews) {
						CollectAll (v, ref nNodes, ref nEdges);
					}
					return;
				case Pos.PosCombine pc:
					foreach (var v in from.InternalSubviews) {
						CollectPos (pc.left, from, ref nNodes, ref nEdges);
						CollectPos (pc.right, from, ref nNodes, ref nEdges);
					}
					break;
				}
			}

			void CollectDim (Dim dim, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
			{
				switch (dim) {
				case Dim.DimView dv:
					if (dv.Target != this) {
						nEdges.Add ((dv.Target, from));
					}
					foreach (var v in from.InternalSubviews) {
						CollectAll (v, ref nNodes, ref nEdges);
					}
					return;
				case Dim.DimCombine dc:
					foreach (var v in from.InternalSubviews) {
						CollectDim (dc.left, from, ref nNodes, ref nEdges);
						CollectDim (dc.right, from, ref nNodes, ref nEdges);
					}
					break;
				}
			}

			void CollectAll (View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
			{
				foreach (var v in from.InternalSubviews) {
					nNodes.Add (v);
					if (v.layoutStyle != LayoutStyle.Computed) {
						continue;
					}
					CollectPos (v.X, v, ref nNodes, ref nEdges);
					CollectPos (v.Y, v, ref nNodes, ref nEdges);
					CollectDim (v.Width, v, ref nNodes, ref nEdges);
					CollectDim (v.Height, v, ref nNodes, ref nEdges);
				}
			}

			CollectAll (this, ref nodes, ref edges);

			var ordered = TopologicalSort (nodes, edges);

			foreach (var v in ordered) {
				if (v.LayoutStyle == LayoutStyle.Computed) {
					v.SetRelativeLayout (v?.SuperView.Frame ?? Frame);
				}

				v.LayoutSubviews ();
				v.LayoutNeeded = false;
			}

			if (SuperView != null && SuperView == Application.Top && LayoutNeeded
			    && ordered.Count == 0 && LayoutStyle == LayoutStyle.Computed) {
				SetRelativeLayout (SuperView.Frame);
			}

			LayoutNeeded = false;

			OnLayoutComplete (new LayoutEventArgs () { OldBounds = oldBounds });
		}

		ustring text;

		/// <summary>
		///   The text displayed by the <see cref="View"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		///  If provided, the text will be drawn before any subviews are drawn.
		/// </para>
		/// <para>
		///  The text will be drawn starting at the view origin (0, 0) and will be formatted according
		///  to the <see cref="TextAlignment"/> property. If the view's height is greater than 1, the
		///  text will word-wrap to additional lines if it does not fit horizontally. If the view's height
		///  is 1, the text will be clipped.
		/// </para>
		/// <para>
		///  Set the <see cref="HotKeySpecifier"/> to enable hotkey support. To disable hotkey support set <see cref="HotKeySpecifier"/> to
		///  <c>(Rune)0xffff</c>.
		/// </para>
		/// </remarks>
		public virtual ustring Text {
			get => text;
			set {
				text = value;
				SetHotKey ();
				UpdateTextFormatterText ();
				ProcessResizeView ();
			}
		}

		/// <summary>
		/// Gets or sets a flag that determines whether the View will be automatically resized to fit the <see cref="Text"/>.
		/// The default is <see langword="false"/>. Set to <see langword="true"/> to turn on AutoSize. If <see cref="AutoSize"/> is <see langword="true"/> the <see cref="Width"/>
		/// and <see cref="Height"/> will always be used if the text size is lower. If the text size is higher the bounds will
		/// be resized to fit it.
		/// In addition, if <see cref="ForceValidatePosDim"/> is <see langword="true"/> the new values of <see cref="Width"/> and
		/// <see cref="Height"/> must be of the same types of the existing one to avoid breaking the <see cref="Dim"/> settings.
		/// </summary>
		public virtual bool AutoSize {
			get => autoSize;
			set {
				var v = ResizeView (value);
				TextFormatter.AutoSize = v;
				if (autoSize != v) {
					autoSize = v;
					TextFormatter.NeedsFormat = true;
					UpdateTextFormatterText ();
					ProcessResizeView ();
				}
			}
		}

		/// <summary>
		/// Gets or sets a flag that determines whether <see cref="Terminal.Gui.TextFormatter.Text"/> will have trailing spaces preserved
		/// or not when <see cref="Terminal.Gui.TextFormatter.WordWrap"/> is enabled. If <see langword="true"/> 
		/// any trailing spaces will be trimmed when either the <see cref="Text"/> property is changed or 
		/// when <see cref="Terminal.Gui.TextFormatter.WordWrap"/> is set to <see langword="true"/>.
		/// The default is <see langword="false"/>.
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
				ProcessResizeView ();
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
				if (TextFormatter.Direction != value) {
					var isValidOldAutSize = autoSize && IsValidAutoSize (out var _);
					var directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction)
					    != TextFormatter.IsHorizontalDirection (value);

					TextFormatter.Direction = value;
					UpdateTextFormatterText ();

					if ((!ForceValidatePosDim && directionChanged && AutoSize)
					    || (ForceValidatePosDim && directionChanged && AutoSize && isValidOldAutSize)) {
						ProcessResizeView ();
					} else if (directionChanged && IsAdded) {
						SetWidthHeight (Bounds.Size);
						SetMinWidthHeight ();
					} else {
						SetMinWidthHeight ();
					}
					TextFormatter.Size = GetBoundsTextFormatterSize ();
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Get or sets if  the <see cref="View"/> was already initialized.
		/// This derived from <see cref="ISupportInitializeNotification"/> to allow notify all the views that are being initialized.
		/// </summary>
		public virtual bool IsInitialized { get; set; }

		/// <summary>
		/// Gets information if the view was already added to the <see cref="SuperView"/>.
		/// </summary>
		public bool IsAdded { get; private set; }

		bool oldEnabled;

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

					if (subviews != null) {
						foreach (var view in subviews) {
							if (!value) {
								view.oldEnabled = view.Enabled;
								view.Enabled = false;
							} else {
								view.Enabled = view.oldEnabled;
								view.addingView = false;
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

		Border border;

		/// <inheritdoc/>
		public virtual Border Border {
			get => border;
			set {
				if (border != value) {
					border = value;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Get or sets whether the view will use <see cref="Terminal.Gui.Border"/> (if <see cref="Border"/> is set) to draw 
		/// a border. If <see langword="false"/> (the default),
		/// <see cref="View.Redraw(Rect)"/> will call <see cref="Border.DrawContent(View, bool)"/>
		/// to draw the view's border. If <see langword="true"/> no border is drawn (and the view is expected to draw the border
		/// itself).
		/// </summary>
		public virtual bool IgnoreBorderPropertyOnRedraw { get; set; }

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
			TextFormatter.FindHotKey (text, HotKeySpecifier, true, out _, out var hk);
			if (hotKey != hk) {
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
					Bounds = new Rect (Bounds.X, Bounds.Y, nBoundsSize.Width, nBoundsSize.Height);
				}
			}
			TextFormatter.Size = GetBoundsTextFormatterSize ();
			return aSize;
		}

		bool SetWidthHeight (Size nBounds)
		{
			var aSize = false;
			var canSizeW = SetWidth (nBounds.Width - GetHotKeySpecifierLength (), out var rW);
			var canSizeH = SetHeight (nBounds.Height - GetHotKeySpecifierLength (false), out var rH);
			if (canSizeW) {
				aSize = true;
				width = rW;
			}
			if (canSizeH) {
				aSize = true;
				height = rH;
			}
			if (aSize) {
				Bounds = new Rect (Bounds.X, Bounds.Y, canSizeW ? rW : Bounds.Width, canSizeH ? rH : Bounds.Height);
				TextFormatter.Size = GetBoundsTextFormatterSize ();
			}

			return aSize;
		}

		/// <summary>
		/// Gets the size to fit all text if <see cref="AutoSize"/> is true.
		/// </summary>
		/// <returns>The <see cref="Size"/></returns>
		public Size GetAutoSize ()
		{
			var rect = TextFormatter.CalcRect (Bounds.X, Bounds.Y, TextFormatter.Text, TextFormatter.Direction);
			return new Size (rect.Size.Width - GetHotKeySpecifierLength (),
			    rect.Size.Height - GetHotKeySpecifierLength (false));
		}

		bool IsValidAutoSize (out Size autoSize)
		{
			var rect = TextFormatter.CalcRect (frame.X, frame.Y, TextFormatter.Text, TextDirection);
			autoSize = new Size (rect.Size.Width - GetHotKeySpecifierLength (),
			    rect.Size.Height - GetHotKeySpecifierLength (false));
			return !(ForceValidatePosDim && (!(Width is Dim.DimAbsolute) || !(Height is Dim.DimAbsolute))
			    || frame.Size.Width != rect.Size.Width - GetHotKeySpecifierLength ()
			    || frame.Size.Height != rect.Size.Height - GetHotKeySpecifierLength (false));
		}

		bool IsValidAutoSizeWidth (Dim width)
		{
			var rect = TextFormatter.CalcRect (frame.X, frame.Y, TextFormatter.Text, TextDirection);
			var dimValue = width.Anchor (0);
			return !(ForceValidatePosDim && (!(width is Dim.DimAbsolute)) || dimValue != rect.Size.Width
			    - GetHotKeySpecifierLength ());
		}

		bool IsValidAutoSizeHeight (Dim height)
		{
			var rect = TextFormatter.CalcRect (frame.X, frame.Y, TextFormatter.Text, TextDirection);
			var dimValue = height.Anchor (0);
			return !(ForceValidatePosDim && (!(height is Dim.DimAbsolute)) || dimValue != rect.Size.Height
			    - GetHotKeySpecifierLength (false));
		}

		/// <summary>
		/// Get the width or height of the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/> length.
		/// </summary>
		/// <param name="isWidth"><see langword="true"/> if is the width (default) <see langword="false"/> if is the height.</param>
		/// <returns>The length of the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/>.</returns>
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
		/// Gets the bounds size from a <see cref="Terminal.Gui.TextFormatter.Size"/>.
		/// </summary>
		/// <returns>The bounds size minus the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/> length.</returns>
		public Size GetTextFormatterBoundsSize ()
		{
			return new Size (TextFormatter.Size.Width - GetHotKeySpecifierLength (),
			    TextFormatter.Size.Height - GetHotKeySpecifierLength (false));
		}

		/// <summary>
		/// Gets the text formatter size from a <see cref="Bounds"/> size.
		/// </summary>
		/// <returns>The text formatter size more the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/> length.</returns>
		public Size GetBoundsTextFormatterSize ()
		{
			if (ustring.IsNullOrEmpty (TextFormatter.Text))
				return Bounds.Size;

			return new Size (frame.Size.Width + GetHotKeySpecifierLength (),
			    frame.Size.Height + GetHotKeySpecifierLength (false));
		}

		/// <summary>
		/// Specifies the event arguments for <see cref="MouseEvent"/>. This is a higher-level construct
		/// than the wrapped <see cref="MouseEvent"/> class and is used for the events defined on <see cref="View"/>
		/// and subclasses of View (e.g. <see cref="View.MouseEnter"/> and <see cref="View.MouseClick"/>).
		/// </summary>
		public class MouseEventArgs : EventArgs {
			/// <summary>
			/// Constructs.
			/// </summary>
			/// <param name="me"></param>
			public MouseEventArgs (MouseEvent me) => MouseEvent = me;
			/// <summary>
			/// The <see cref="MouseEvent"/> for the event.
			/// </summary>
			public MouseEvent MouseEvent { get; set; }

			/// <summary>
			/// Indicates if the current mouse event has already been processed and the driver should stop notifying any other event subscriber.
			/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
			/// </summary>
			/// <remarks>This property forwards to the <see cref="MouseEvent.Handled"/> property and is provided as a convenience and for
			/// backwards compatibility</remarks>
			public bool Handled {
				get => MouseEvent.Handled;
				set => MouseEvent.Handled = value;
			}
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

			var args = new MouseEventArgs (mouseEvent);
			MouseEnter?.Invoke (args);

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

			var args = new MouseEventArgs (mouseEvent);
			MouseLeave?.Invoke (args);

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

			if ((mouseEvent.Flags & MouseFlags.Button1Clicked) != 0 || (mouseEvent.Flags & MouseFlags.Button2Clicked) != 0
				|| (mouseEvent.Flags & MouseFlags.Button3Clicked) != 0 || (mouseEvent.Flags & MouseFlags.Button4Clicked) != 0) {

				var args = new MouseEventArgs (mouseEvent);
				if (OnMouseClick (args)) {
					return true;
				}
			}
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
		protected bool OnMouseClick (MouseEventArgs args)
		{
			if (!Enabled) {
				return true;
			}

			MouseClick?.Invoke (args);
			return args.Handled;
		}

		/// <inheritdoc/>
		public override void OnCanFocusChanged () => CanFocusChanged?.Invoke ();

		/// <inheritdoc/>
		public override void OnEnabledChanged () => EnabledChanged?.Invoke ();

		/// <inheritdoc/>
		public override void OnVisibleChanged () => VisibleChanged?.Invoke ();

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			height = null;
			width = null;
			x = null;
			y = null;
			for (var i = InternalSubviews.Count - 1; i >= 0; i--) {
				var subview = InternalSubviews [i];
				Remove (subview);
				subview.Dispose ();
			}
			base.Dispose (disposing);
			System.Diagnostics.Debug.Assert (InternalSubviews.Count == 0);
		}

		/// <summary>
		/// This derived from <see cref="ISupportInitializeNotification"/> to allow notify all the views that are beginning initialized.
		/// </summary>
		public void BeginInit ()
		{
			if (!IsInitialized) {
				oldCanFocus = CanFocus;
				oldTabIndex = tabIndex;
			}
			if (subviews?.Count > 0) {
				foreach (var view in subviews) {
					if (!view.IsInitialized) {
						view.BeginInit ();
					}
				}
			}
		}

		/// <summary>
		/// This derived from <see cref="ISupportInitializeNotification"/> to allow notify all the views that are ending initialized.
		/// </summary>
		public void EndInit ()
		{
			IsInitialized = true;
			if (subviews?.Count > 0) {
				foreach (var view in subviews) {
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

		bool CanSetWidth (int desiredWidth, out int resultWidth)
		{
			var w = desiredWidth;
			bool canSetWidth;
			if (Width is Dim.DimCombine || Width is Dim.DimView || Width is Dim.DimFill) {
				// It's a Dim.DimCombine and so can't be assigned. Let it have it's width anchored.
				w = Width.Anchor (w);
				canSetWidth = !ForceValidatePosDim;
			} else if (Width is Dim.DimFactor factor) {
				// Tries to get the SuperView width otherwise the view width.
				var sw = SuperView != null ? SuperView.Frame.Width : w;
				if (factor.IsFromRemaining ()) {
					sw -= Frame.X;
				}
				w = Width.Anchor (sw);
				canSetWidth = !ForceValidatePosDim;
			} else {
				canSetWidth = true;
			}
			resultWidth = w;

			return canSetWidth;
		}

		bool CanSetHeight (int desiredHeight, out int resultHeight)
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
		/// Calculate the width based on the <see cref="Width"/> settings.
		/// </summary>
		/// <param name="desiredWidth">The desired width.</param>
		/// <param name="resultWidth">The real result width.</param>
		/// <returns><see langword="true"/> if the width can be directly assigned, <see langword="false"/> otherwise.</returns>
		public bool SetWidth (int desiredWidth, out int resultWidth)
		{
			return CanSetWidth (desiredWidth, out resultWidth);
		}

		/// <summary>
		/// Calculate the height based on the <see cref="Height"/> settings.
		/// </summary>
		/// <param name="desiredHeight">The desired height.</param>
		/// <param name="resultHeight">The real result height.</param>
		/// <returns><see langword="true"/> if the height can be directly assigned, <see langword="false"/> otherwise.</returns>
		public bool SetHeight (int desiredHeight, out int resultHeight)
		{
			return CanSetHeight (desiredHeight, out resultHeight);
		}

		/// <summary>
		/// Gets the current width based on the <see cref="Width"/> settings.
		/// </summary>
		/// <param name="currentWidth">The real current width.</param>
		/// <returns><see langword="true"/> if the width can be directly assigned, <see langword="false"/> otherwise.</returns>
		public bool GetCurrentWidth (out int currentWidth)
		{
			SetRelativeLayout (SuperView?.frame ?? frame);
			currentWidth = frame.Width;

			return CanSetWidth (0, out _);
		}

		/// <summary>
		/// Calculate the height based on the <see cref="Height"/> settings.
		/// </summary>
		/// <param name="currentHeight">The real current height.</param>
		/// <returns><see langword="true"/> if the height can be directly assigned, <see langword="false"/> otherwise.</returns>
		public bool GetCurrentHeight (out int currentHeight)
		{
			SetRelativeLayout (SuperView?.frame ?? frame);
			currentHeight = frame.Height;

			return CanSetHeight (0, out _);
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
		public View GetTopSuperView ()
		{
			View top = Application.Top;
			for (var v = this?.SuperView; v != null; v = v.SuperView) {
				top = v;
			}

			return top;
		}
	}
}
