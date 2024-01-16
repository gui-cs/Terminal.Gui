using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static Unix.Terminal.Delegates;

namespace Terminal.Gui;

/// <summary>
/// Like a <see cref="Label"/>, but where the <see cref="View.Text"/> is formatted to highlight
/// the <see cref="Shortcut"/>.
/// <code>
/// 
/// </code>
/// </summary>
public class Shortcut : View {
	Key _key;
	KeyBindingScope _keyBindingScope;
	Command? _command;

	readonly View _container;
	View _commandView;
	bool _autoSize;

	public View HelpView { get; }

	public View KeyView { get; }

	public Shortcut ()
	{
		CanFocus = true;
		Height = 1;
		AutoSize = true;

		AddCommand (Gui.Command.Default, () => {
			//SetFocus ();
			//SuperView?.FocusNext ();
			return true;
		});
		AddCommand (Gui.Command.Accept, () => OnAccept ());
		KeyBindings.Add (KeyCode.Space, Gui.Command.Accept);

		_container = new View () { Id = "_container", CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
		CommandView = new View () { Id = "_commandView", CanFocus = false, AutoSize = true, X = 0, Y = Pos.Center (), HotKeySpecifier = new Rune ('_') };
		HelpView = new View () { Id = "_helpView", CanFocus = false, AutoSize = true, Y = Pos.Center () };
		HelpView.TextAlignment = TextAlignment.Left;
		KeyView = new View () { Id = "_keyView", CanFocus = false, AutoSize = true, Y = Pos.Center () };

		HelpView.TextAlignment = TextAlignment.Right;

		_container.MouseClick += Container_MouseClick;
		//_commandView.MouseClick += SubView_MouseClick;
		HelpView.MouseClick += SubView_MouseClick;
		KeyView.MouseClick += SubView_MouseClick;

		LayoutStarted += Shortcut_LayoutStarted;

		TitleChanged += Shortcut_TitleChanged;

		_container.Add (_commandView, HelpView, KeyView);
		Add (_container);
	}

	private void Shortcut_LayoutStarted (object sender, LayoutEventArgs e) => SetSubViewLayout ();

	void SetSubViewLayout ()
	{
		if (!IsInitialized) {
			return;
		}

		var cs = new ColorScheme (ColorScheme) {
			Normal = ColorScheme.HotNormal,
			HotNormal = ColorScheme.Normal
		};
		KeyView.ColorScheme = cs;

		HelpView.X = Pos.Right (CommandView) + 2;
		KeyView.X = Pos.AnchorEnd (KeyView.Text.GetColumns());
		if (AutoSize) {
			var thickness = GetAdornmentsThickness ();
			_container.Width = _commandView.Frame.Width +
			                   (HelpView.Visible && HelpView.Text.Length > 0 ? HelpView.Frame.Width + 2 : 0) +
			                   (KeyView.Visible && KeyView.Text.Length > 0 ? KeyView.Frame.Width + 2 : 0);
			Width = _container.Width + thickness.Horizontal;
		} else {
			//Width = Dim.Fill ();
			//Height = 1;
		}
	}

	private void Shortcut_TitleChanged (object sender, TitleEventArgs e)
	{
		_commandView.Text = Title;
	}

	private void Container_MouseClick (object sender, MouseEventEventArgs e)
	{
		e.Handled = OnAccept ();
	}

	private void SubView_MouseClick (object sender, MouseEventEventArgs e)
	{
		e.Handled = OnAccept ();
	}

	/// <summary>
	/// If <see langword="true"/> the Shortcut will be sized to fit the available space (the Bounds of the
	/// the SuperView).
	/// </summary>
	/// <remarks>
	/// </remarks>
	public override bool AutoSize {
		get => _autoSize;
		set {
			_autoSize = value;
			SetSubViewLayout ();
		}
	}

	public override string Text {
		get => base.Text;
		set {
			//base.Text = value;
			if (HelpView != null) {
				HelpView.Text = value;
			}
		}
	}

	/// <summary>
	/// The shortcut key.
	/// </summary>
	public Key Key {
		get => _key;
		set {
			if (value == null) {
				throw new ArgumentNullException ();
			}
			_key = value;
			if (Command != null) {
				UpdateKeyBinding ();
			}
			KeyView.Text = $"{Key}";
			KeyView.Visible = Key != Key.Empty;
		}
	}

	public KeyBindingScope KeyBindingScope {
		get => _keyBindingScope;
		set {
			_keyBindingScope = value;
			if (Command != null) {
				UpdateKeyBinding ();
			}
		}
	}

	public Command? Command {
		get => _command;
		set {
			if (value != null) {
				_command = value.Value;
				UpdateKeyBinding ();
			}
		}
	}

	void UpdateKeyBinding ()
	{
		if (this.KeyBindingScope == KeyBindingScope.Application) {
			return;
		}

		if (Command != null && Key != null && Key != Key.Empty) {
			// Add a command and key binding for this command to this Shortcut
			if (!GetSupportedCommands ().Contains (Command.Value)) {
				// The action that will be taken will be to fire the OnClicked
				// event. 
				AddCommand (Command.Value, () => OnAccept ());
			}
			KeyBindings.Remove (Key);
			KeyBindings.Add (Key, this.KeyBindingScope, Command.Value);
		}

	}


	/// <summary>
	/// The event fired when the <see cref="Command.Accept"/> command is received. This
	/// occurs if the user clicks on the Bar with the mouse or presses the key bound to
	/// Command.Accept (Space by default).
	/// </summary>
	/// <remarks>
	/// Client code can hook up to this event, it is
	/// raised when the button is activated either with
	/// the mouse or the keyboard.
	/// </remarks>
	public event EventHandler<HandledEventArgs> Accept;

	/// <summary>
	/// Called when the <see cref="Command.Accept"/> command is received. This
	/// occurs if the user clicks on the Bar with the mouse or presses the key bound to
	/// Command.Accept (Space by default).
	/// </summary>
	public virtual bool OnAccept ()
	{
		if (Key == null || Key == Key.Empty) {
			return false;
		}

		bool handled = false;
		var keyCopy = new Key (Key);

		switch (KeyBindingScope) {
		case KeyBindingScope.Application:
			// Simulate a key down to invoke the Application scoped key binding
			handled = Application.OnKeyDown (keyCopy);
			break;
		case KeyBindingScope.Focused:
			//throw new InvalidOperationException ();
			handled = false;
			break;
		case KeyBindingScope.HotKey:
			handled = _commandView.InvokeCommand (Gui.Command.Accept) == true;
			break;
		}
		if (handled == false) {
			var args = new HandledEventArgs ();
			Accept?.Invoke (this, args);
			handled = args.Handled;
		}
		return handled;
	}

	public View CommandView {
		get => _commandView;
		set {
			if (value == null) {
				throw new ArgumentNullException ();
			}
			if (_commandView != null) {
				_container.Remove (_commandView);
				_commandView?.Dispose ();
			}
			_commandView = value;
			_commandView.Id = "_commandView";
			_commandView.CanFocus = true;
			_commandView.AutoSize = true;
			_commandView.X = 0;
			_commandView.Y = Pos.Center ();
			_commandView.CanFocus = false;
			_commandView.MouseClick += SubView_MouseClick;

			_commandView.HotKeyChanged += (s, e) => {
				if (_commandView.HotKey != Key.Empty) {
					// Add it 
					AddKeyBindingsForHotKey (Key.Empty, _commandView.HotKey);
				}
			};

			_commandView.HotKeySpecifier = new Rune ('_');
			_container.Add (_commandView);
			SetSubViewLayout ();
		}
	}

	public override bool CanFocus {
		get {
			if (KeyView != null) {
				return KeyView.Visible && CommandView is Shortcut;
			}
			return base.CanFocus;
		}
		set {
			base.CanFocus = value;
		}
	}

	public override bool OnEnter (View view)
	{
		Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

		var cs = new ColorScheme (ColorScheme) {
			Normal = ColorScheme.Focus,
			HotNormal = ColorScheme.HotFocus
		};

		_container.ColorScheme = cs;

		cs = new ColorScheme (ColorScheme) {
			Normal = ColorScheme.HotFocus,
			HotNormal = ColorScheme.Focus
		};
		KeyView.ColorScheme = cs;

		return base.OnEnter (view);
	}

	public override bool OnLeave (View view)
	{
		var cs = new ColorScheme (ColorScheme) {
			Normal = ColorScheme.Normal,
			HotNormal = ColorScheme.HotNormal
		};

		_container.ColorScheme = cs;

		cs = new ColorScheme (ColorScheme) {
			Normal = ColorScheme.HotNormal,
			HotNormal = ColorScheme.Normal
		};
		KeyView.ColorScheme = cs;

		return base.OnLeave (view);
	}
}

/// <summary>
/// The Bar <see cref="View"/> provides a container for other views to be used as a toolbar or status bar.
/// </summary>
/// <remarks>
/// Views added to a Bar will be positioned horizontally from left to right.
/// </remarks>
public class Bar : View {
	/// <inheritdoc/>
	public Bar () => SetInitialProperties ();

	public bool StatusBarStyle { get; set; } = true;

	void SetInitialProperties ()
	{
		AutoSize = false;
		ColorScheme = Colors.Menu;
		CanFocus = true;

		LayoutStarted += Bar_LayoutStarted;
	}

	private void Bar_LayoutStarted (object sender, LayoutEventArgs e)
	{
		View prevSubView = null;

		switch (Orientation) {
		case Orientation.Horizontal:
			for (var index = 0; index < Subviews.Count; index++) {
				var subview = Subviews [index];
				if (!subview.Visible) {
					continue;
				}
				if (prevSubView == null) {
					subview.X = 0;
				} else {
					// Make view to right be autosize
					//Subviews [^1].AutoSize = true;

					// Align the view to the right of the previous view
					subview.X = Pos.Right (prevSubView);
				}
				subview.Y = Pos.Center ();
				prevSubView = subview;
			}
			break;
		case Orientation.Vertical:
			int maxSubviewWidth = 0;
			for (var index = 0; index < Subviews.Count; index++) {
				var subview = Subviews [index];
				if (!subview.Visible) {
					continue;
				}
				if (prevSubView == null) {
					subview.Y = 0;
				} else {
					// Make view to right be autosize
					//Subviews [^1].AutoSize = true;

					// Align the view to the bottom of the previous view
					subview.Y = Pos.Bottom (prevSubView);
				}
				prevSubView = subview;

				subview.AutoSize = true;
				subview.SetRelativeLayout (Driver.Bounds);
				Width = maxSubviewWidth + GetAdornmentsThickness ().Horizontal;
				subview.SetRelativeLayout (Bounds);

				subview.AutoSize = false;
				subview.X = 0;
				subview.Width = Dim.Fill ();

			}
			Height = Subviews.Count + GetAdornmentsThickness ().Vertical; 
			break;
		}
	}

	public override void Add (View view)
	{
		if (Orientation == Orientation.Horizontal) {
			view.AutoSize = true;
		}

		if (StatusBarStyle) {
			view.Margin.Thickness = new Thickness (1, 0, 0, 0);
			// Light up right border
			view.BorderStyle = LineStyle.Single;
			view.Border.Thickness = new Thickness (0, 0, 1, 0);
			view.Padding.Thickness = new Thickness (0, 0, 1, 0);
		}
		//view.Padding.ColorScheme = Colors.Menu;

		// Add any HotKey keybindings to our bindings
		var bindings = view.KeyBindings.Bindings.Where (b => b.Value.Scope == KeyBindingScope.HotKey);
		foreach (var binding in bindings) {
			AddCommand (binding.Value.Commands [0], () => {
				if (view is Shortcut shortcut) {
					return shortcut.CommandView.InvokeCommands (binding.Value.Commands);
				}
				return false;
			});
			KeyBindings.Add (binding.Key, binding.Value);
		}
		base.Add (view);
	}

	/// <summary>
	/// Gets or sets the <see cref="Orientation"/> for this <see cref="Bar"/>. The default is <see cref="Orientation.Horizontal"/>.
	/// </summary>
	public Orientation Orientation { get; set; } = Orientation.Horizontal;
	private bool _autoSize;

	/// <summary>
	/// If <see langword="true"/> the Shortcut will be sized to fit the available space (the Bounds of the
	/// the SuperView).
	/// </summary>
	/// <remarks>
	/// </remarks>
	public override bool AutoSize {
		get => _autoSize;
		set {
			_autoSize = value;
			Bar_LayoutStarted (null, null);
		}
	}

}