using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Terminal.Gui;
public partial class View {

	void AddCommands ()
	{
		// By default, the accept command is bound to the HotKey enabling focus
		AddCommand (Command.Accept, () => {
			if (CanFocus) {
				SuperView.SetFocus (this);
				return true;
			}
			return false;
		});
	}

	#region HotKey Support
	/// <summary>
	/// Invoked when the <see cref="HotKey"/> is changed.
	/// </summary>
	public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

	Key _hotKey = Key.Null;

	void TextFormatter_HotKeyChanged (object sender, KeyChangedEventArgs e)
	{
		HotKeyChanged?.Invoke (this, e);
	}

	/// <summary>
	/// Gets or sets the hot key defined for this view. Pressing the hot key on the keyboard while this view has
	/// focus will invoke the <see cref="Command.Accept"/> command. By default, the HotKey is automatically set to the first
	/// character of <see cref="Text"/> that is prefixed with with <see cref="HotKeySpecifier"/>.
	/// <para>
	/// A HotKey is a keypress that selects a visible UI item. For selecting items across <see cref="View"/>`s
	/// (e.g.a <see cref="Button"/> in a <see cref="Dialog"/>) the keypress must include the <see cref="Key.AltMask"/> modifier.
	/// For selecting items within a View that are not Views themselves, the keypress can be key without the Alt modifier.
	/// For example, in a Dialog, a Button with the text of "_Text" can be selected with Alt-T.
	/// Or, in a <see cref="Menu"/> with "_File _Edit", Alt-F will select (show) the "_File" menu.
	/// If the "_File" menu has a sub-menu of "_New" `Alt-N` or `N` will ONLY select the "_New" sub-menu if the "_File" menu is already opened.
	/// </para>
	/// </summary>
	/// <remarks>
	/// <para>
	/// See <see href="../docs/keyboard.md"/> for an overview of Terminal.Gui keyboard APIs.
	/// </para>
	/// <para>
	/// This is a helper API for configuring a key binding for the hot key. By default, this property is set whenever <see cref="Text"/> changes.
	/// </para>
	/// <para>
	/// By default, when the Hot Key is set, key bindings are added for both the base key (e.g. <see cref="Key.D3"/>) and the Alt-shifted key (e.g. <see cref="Key.D3"/> | <see cref="Key.AltMask"/>).
	/// This behavior can be overriden by overriding <see cref="HotKey"/>.
	/// </para>
	/// <para>
	/// By default, when the HotKey is set to <see cref="Key.A"/> through <see cref="Key.Z"/> key bindings will be added for both the un-shifted and shifted
	/// versions. This means if the HotKey is <see cref="Key.A"/>, key bindings for <see cref="Key.A"/> and <see cref="Key.A"/> | <see cref="Key.ShiftMask"/>
	/// will be added. This behavior can be overriden by overriding <see cref="HotKey"/>.
	/// </para>
	/// <para>
	/// If the hot key is changed, the <see cref="HotKeyChanged"/> event is fired.
	/// </para>
	/// </remarks>
	public virtual Key HotKey {
		get => _hotKey;
		set {
			if (_hotKey != value) {
				var newKey = value == Key.Unknown ? Key.Null : value;

				var baseKey = newKey & ~Key.CtrlMask & ~Key.AltMask & ~Key.ShiftMask;
				if (newKey != baseKey) {
					if ((newKey & Key.CtrlMask) != 0) {
						throw new ArgumentException (@$"HotKey does not support CtrlMask ({value}).");
					}
					// Strip off the shift mask if it's A...Z
					if (baseKey is >= Key.A and <= Key.Z && (newKey & Key.ShiftMask) != 0) {
						newKey &= ~Key.ShiftMask;
					}
					// Strip off the Alt mask
					newKey &= ~Key.AltMask;
				}

				// Remove base version
				if (TryGetKeyBinding (_hotKey, out _)) {
					ClearKeyBinding (_hotKey);
				}

				// Remove the Alt version
				if (TryGetKeyBinding (_hotKey | Key.AltMask, out _)) {
					ClearKeyBinding (_hotKey | Key.AltMask);
				}

				if (_hotKey is >= Key.A and <= Key.Z) {
					// Remove base and shift version
					if (TryGetKeyBinding (_hotKey | Key.ShiftMask, out _)) {
						ClearKeyBinding (_hotKey | Key.ShiftMask);
					}
				}

				// Add the new 
				if (newKey != Key.Null) {
					// Add the base and Alt key
					AddKeyBinding (newKey, Command.Accept);
					AddKeyBinding (newKey | Key.AltMask, Command.Accept);

					// If the Key is A..Z, add ShiftMask
					if (newKey is >= Key.A and <= Key.Z) {
						AddKeyBinding (newKey | Key.ShiftMask, Command.Accept);
					}

					AddKeyBinding (newKey, Command.Accept);
				}
				// This will cause TextFormatter_HotKeyChanged to be called, firing HotKeyChanged
				_hotKey = TextFormatter.HotKey = value;
			}
		}
	}

	/// <summary>
	/// Gets or sets the specifier character for the hot key (e.g. '_'). Set to '\xffff' to disable automatic hot key setting
	/// support for this View instance. The default is '\xffff'. 
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

	void SetHotKey ()
	{
		if (TextFormatter == null || HotKeySpecifier == new Rune ('\xFFFF')) {
			return; // throw new InvalidOperationException ("Can't set HotKey unless a TextFormatter has been created");
		}
		if (TextFormatter.FindHotKey (_text, HotKeySpecifier, true, out _, out var hk)) {
			if (_hotKey != hk && hk != Key.Unknown) {
				HotKey = hk;
			}
		} else {
			HotKey = Key.Null;
		}
	}

	#endregion HotKey Support

	#region Tab/Focus Handling
	// This is null, and allocated on demand.
	List<View> _tabIndexes;

	/// <summary>
	/// Gets a list of the subviews that are <see cref="TabStop"/>s.
	/// </summary>
	/// <value>The tabIndexes.</value>
	public IList<View> TabIndexes => _tabIndexes?.AsReadOnly () ?? _empty;

	int _tabIndex = -1;
	int _oldTabIndex;

	/// <summary>
	/// Indicates the index of the current <see cref="View"/> from the <see cref="TabIndexes"/> list. See also: <seealso cref="TabStop"/>.
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
	/// Gets or sets whether the view is a stop-point for keyboard navigation of focus. Will be
	/// <see langword="true"/> only if the <see cref="CanFocus"/> is also <see langword="true"/>.
	/// Set to <see langword="false"/> to prevent the view from being a stop-point for keyboard navigation.
	/// </summary>
	/// <remarks>
	/// The default keyboard navigation keys are <see cref="Key.Tab"/> and <see cref="Key.ShiftMask"/>|<see cref="Key.Tab"/>.
	/// These can be changed by modifying the key bindings (see <see cref="AddKeyBinding(Key, Command[])"/>) of the SuperView.
	/// </remarks>
	public bool TabStop {
		get => _tabStop;
		set {
			if (_tabStop == value) {
				return;
			}
			_tabStop = CanFocus && value;
		}
	}

	#endregion Tab/Focus Handling

	#region Low-level Key handling

	#region Key Down Event
	/// <summary>
	/// If the view is enabled, processes a key down event and returns <see langword="true"/> if the event was handled.
	/// Called before <see cref="ProcessKeyPressEvent"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// If the view has a sub view that is focused, <see cref="ProcessKeyDownEvent"/> will be called on the focused view first.
	/// </para>
	/// <para>
	/// If the focused sub view does not handle the key press, this method calls <see cref="OnKeyDown"/>
	/// then <see cref="OnProcessKeyDown"/>, both of which are cancellable.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	/// <param name="keyEvent"></param>
	/// <returns><see langword="true"/> if the event was handled.</returns>
	public bool ProcessKeyDownEvent (KeyEventArgs keyEvent)
	{
		if (!Enabled) {
			return false;
		}

		if (Focused?.ProcessKeyDownEvent (keyEvent) == true) {
			return true;
		}

		// Before (fire the cancellable event KeyPress)
		if (OnKeyDown (keyEvent)) {
			return true;
		}

		// During (this is what can be cancelled)
		if (OnProcessKeyDown (keyEvent)) {
			return true;
		}

		// After (fire the cancellable event InvokingKeyBindings)
		// TODO: Until there's a clear use-case, we will not define an 'after' event (e.g. OnAfterKeyDown). 

		return false;
	}
	/// <summary>
	/// Invoked when a key is pressed down. This is called before <see cref="OnKeyPress"/>.
	/// </summary>
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPress"/>.
	/// <para>
	/// Overrides must call into the base and return <see langword="true"/> if the base returns  <see langword="true"/>.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="OnKeyPress"/>.
	/// </para>
	/// </remarks>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key stroke was not handled. <see langword="true"/> if no
	/// other view should see it.</returns>
	public virtual bool OnKeyDown (KeyEventArgs keyEvent)
	{
		if (!Enabled) {
			return false;
		}

		// fire event
		KeyDown?.Invoke (this, keyEvent);
		if (keyEvent.Handled) {
			return true;
		}

		if (Focused?.OnKeyDown (keyEvent) == true) {
			return true;
		}

		return false;
	}

	/// <summary>
	/// Invoked when a key is pressed down. Set <see cref="KeyEventArgs.Handled"/> to true to stop the key from being processed by other views.
	/// </summary>
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPress"/>.
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	public event EventHandler<KeyEventArgs> KeyDown;

	/// <summary>
	/// Low-level API allowing views to process key down events. This is called before <see cref="OnProcessKeyPress"/>
	/// and before <see cref="OnInvokeKeyBindings(KeyEventArgs)"/>.
	/// </summary>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	/// <remarks>
	/// <para>
	/// Fires the <see cref="ProcessKeyDown"/> event. 
	/// </para>
	/// <para>
	/// Called before <see cref="OnProcessKeyPress"/>.
	/// </para>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="OnProcessKeyPress"/>.
	/// </para>
	/// </remarks>
	public virtual bool OnProcessKeyDown (KeyEventArgs keyEvent)
	{
		// fire event
		ProcessKeyPress?.Invoke (this, keyEvent);
		return keyEvent.Handled;
	}
	#endregion

	#region Key Press Event
	/// <summary>
	/// If the view is enabled, processes a key press event and returns <see langword="true"/> if the event was handled.
	/// </summary>
	/// <remarks>
	/// <para>
	/// If the view has a sub view that is focused, <see cref="ProcessKeyPressEvent"/> will be called on the focused view first.
	/// </para>
	/// <para>
	/// If the focused sub view does not handle the key press, this method calls <see cref="OnKeyPress"/>
	/// then <see cref="OnProcessKeyPress"/> and
	/// then <see cref="OnInvokeKeyBindings(KeyEventArgs)"/>, all of which are cancellable.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	/// <param name="keyEvent"></param>
	/// <returns><see langword="true"/> if the event was handled.</returns>
	public bool ProcessKeyPressEvent (KeyEventArgs keyEvent)
	{
		if (!Enabled) {
			return false;
		}

		if (Focused?.ProcessKeyPressEvent (keyEvent) == true) {
			return true;
		}

		// Before (fire the cancellable event KeyPress)
		if (OnKeyPress (keyEvent)) {
			return true;
		}

		// During (this is what can be cancelled)
		// Overridable method that can be used to process a key press.
		// (fires the cancellable event ProcessKeyPress)
		var handled = OnInvokeKeyBindings (keyEvent);
		if (handled != null && (bool)handled) {
			return true;
		}
		
		if (OnProcessKeyPress (keyEvent)) {
			return true;
		}

		// After (fire the cancellable event InvokingKeyBindings)


		return false;
	}

	/// <summary>
	/// Low-level API called when the user presses a key. In most cases this is where the users sees results of the key press
	/// (the character appears, a command runs, etc...).
	/// This is called before <see cref="OnInvokeKeyBindings(KeyEventArgs)"/>.
	/// </summary>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	/// <remarks>
	/// <para>
	/// For processing <see cref="HotKey"/>s and commands, use <see cref="Command"/> and <see cref="AddKeyBinding(Key, Command[])"/>instead.
	/// </para>
	/// <para>
	/// Fires the <see cref="KeyPress"/> event. 
	/// </para>
	/// <para>
	/// Called after <see cref="OnKeyDown"/> and before <see cref="OnKeyUp"/>.
	/// </para>
	/// <para>
	/// SubViews can use the <see cref="KeyPress"/> of their super view to intercept key presses.
	/// </para>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="OnKeyPress"/>.
	/// </para>
	/// </remarks>
	public virtual bool OnKeyPress (KeyEventArgs keyEvent)
	{
		// fire event
		KeyPress?.Invoke (this, keyEvent);
		return keyEvent.Handled;
	}

	/// <summary>
	/// Invoked when the user presses a key. In most cases this is where the users sees results of the key press
	/// (the character appears, a command runs, etc...).
	/// Set <see cref="KeyEventArgs.Handled"/> to true to stop the key from
	/// being processed by other views. Invoked after <see cref="KeyDown"/> and before <see cref="KeyUp"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// SubViews can use the <see cref="KeyPress"/> of their super view to intercept key presses.
	/// </para>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPress"/>.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	public event EventHandler<KeyEventArgs> KeyPress;

	/// <summary>
	/// Low-level API allowing views to process key press events. This is called after <see cref="OnKeyPress"/>
	/// and before <see cref="OnInvokeKeyBindings(KeyEventArgs)"/>.
	/// </summary>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	/// <remarks>
	/// <para>
	/// Override <see cref="OnProcessKeyPress"/> to override the default behavior of when key bindings are invoked.
	/// </para>
	/// <para>
	/// For processing <see cref="HotKey"/>s and commands, use <see cref="Command"/> and <see cref="AddKeyBinding(Key, Command[])"/>instead.
	/// </para>
	/// <para>
	/// Fires the <see cref="ProcessKeyPress"/> event. 
	/// </para>
	/// <para>
	/// Called after <see cref="OnKeyDown"/> and before <see cref="OnKeyUp"/>.
	/// </para>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPress"/>.
	/// </para>
	/// </remarks>
	public virtual bool OnProcessKeyPress (KeyEventArgs keyEvent)
	{
		// fire event
		ProcessKeyPress?.Invoke (this, keyEvent);
		return keyEvent.Handled;
	}

	/// <summary>
	/// Invoked when the users presses a key to allow processing of the key press event.
	/// Set <see cref="KeyEventArgs.Handled"/> to true to stop the key from
	/// being processed by other views. Invoked after <see cref="KeyDown"/> and before <see cref="InvokingKeyBindings"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// SubViews can use the <see cref="ProcessKeyPress"/> of their super view override the default behavior of
	/// when key bindings are invoked.
	/// </para>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPress"/>.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	public event EventHandler<KeyEventArgs> ProcessKeyPress;

	#endregion KeyPress Event

	#region Key Up Event
	/// <summary>
	/// Method invoked when a key is released. This method will be called after <see cref="OnProcessKeyPress"/>.
	/// </summary>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key stroke was not handled. <see langword="true"/> if no
	/// other view should see it.</returns>
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPress"/>.
	/// <para>
	/// Overrides must call into the base and return <see langword="true"/> if the base returns  <see langword="true"/>.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	public virtual bool OnKeyUp (KeyEventArgs keyEvent)
	{
		if (!Enabled) {
			return false;
		}

		// fire event
		KeyUp?.Invoke (this, keyEvent);
		if (keyEvent.Handled) {
			return true;
		}

		if (Focused?.OnKeyUp (keyEvent) == true) {
			return true;
		}

		return false;

	}

	/// <summary>
	/// Invoked when a key is released. Set <see cref="KeyEventArgs.Handled"/> to true to stop the key up event from being processed by other views.
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPress"/>.
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	/// </summary>
	public event EventHandler<KeyEventArgs> KeyUp;

	#endregion Key Up Event
	#endregion Low-level Key handling

	#region Key Bindings
	/// <summary>
	/// Gets the key bindings for this view.
	/// </summary>
	private Dictionary<Key, Command []> KeyBindings { get; set; } = new Dictionary<Key, Command []> ();
	private Dictionary<Command, Func<bool?>> CommandImplementations { get; set; } = new Dictionary<Command, Func<bool?>> ();

	/// <summary>
	/// Low-level API called when a user presses a key; invokes any key bindings set on the view.
	/// This is called during <see cref="OnProcessKeyPress"/> after <see cref="OnKeyPress"/> has returned,
	/// and before <see cref="OnKeyUp"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Fires the <see cref="InvokingKeyBindings"/> event.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	public virtual bool? OnInvokeKeyBindings (KeyEventArgs keyEvent)
	{
		// fire event
		InvokingKeyBindings?.Invoke (this, keyEvent);
		if (keyEvent.Handled) {
			return true;
		}

		// * If no key binding was found, `InvokeKeyBindings` returns `null`.
		//   Continue passing the event (return `false` from `OnInvokeKeyBindings`).
		// * If key bindings were found, but none handled the key (all `Command`s returned `false`),
		//   `InvokeKeyBindings` returns `false`. Continue passing the event (return `false` from `OnInvokeKeyBindings`)..
		// * If key bindings were found, and any handled the key (at least one `Command` returned `true`),
		//   `InvokeKeyBindings` returns `true`. Continue passing the event (return `false` from `OnInvokeKeyBindings`).
		var handled = InvokeKeyBindings (keyEvent);
		if (handled != null && (bool)handled) {
			return true;
		}
		
		// TODO: Refactor this per https://github.com/gui-cs/Terminal.Gui/pull/2927#discussion_r1420415162
		// View base class doesn't have to knowing about a derived class TopLevel.
		// The isolation principle is affected. If a derived class has a different behavior the it must overridden
		// the related method and deal with the right actions. I know you want to get rid of the TopLevel but this derived view has specific
		// functionalities that only have to belong to him.
		//
		// Another problem with this design is that are a lot of views that have KeyBindings that only must be run if they are focused.
		// TextView is one of that and this call will invoke an action even if it isn't focused and thus having unexpected behavior.
		// With this design force all the TextView methods called by KeyBindings to check if it's focused which is a nightmare.
		foreach (var view in Subviews.Where (v => v is not Toplevel && v.Enabled && !v.HasFocus && v.KeyBindings.Count > 0)) {
			handled = view.OnInvokeKeyBindings (keyEvent);
			if (handled != null && (bool)handled) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Invoked when a key is pressed that may be mapped to a key binding. Set <see cref="KeyEventArgs.Handled"/>
	/// to true to stop the key from being processed by other views. 
	/// </summary>
	public event EventHandler<KeyEventArgs> InvokingKeyBindings;

	/// <summary>
	/// Invokes any binding that is registered on this <see cref="View"/>
	/// and matches the <paramref name="keyEvent"/>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </summary>
	/// <param name="keyEvent">The key event passed.</param>
	/// <returns>
	/// <see langword="null"/> if no command was bound the <paramref name="keyEvent"/>.
	/// <see langword="true"/> if commands were invoked and at least one handled the command.
	/// <see langword="false"/> if commands were invoked and at none handled the command.
	/// </returns>	
	protected bool? InvokeKeyBindings (KeyEventArgs keyEvent)
	{
		bool? toReturn = null;
		var key = keyEvent.Key;
		if (TryGetKeyBinding (key, out var commands)) {

			foreach (var command in commands) {

				if (!CommandImplementations.ContainsKey (command)) {
					throw new NotSupportedException ($"A KeyBinding was set up for the command {command} ({keyEvent.Key}) but that command is not supported by this View ({GetType ().Name})");
				}

				// each command has its own return value
				var thisReturn = InvokeCommand (command);

				// if we haven't got anything yet, the current command result should be used
				toReturn ??= thisReturn;

				// if ever see a true then that's what we will return
				if (thisReturn ?? false) {
					toReturn = true;
				}
			}
		}

		return toReturn;
	}

	/// <summary>
	/// Invokes the specified command.
	/// </summary>
	/// <param name="command"></param>
	/// <returns>
	/// <see langword="null"/> if no command was found.
	/// <see langword="true"/> if the command was invoked and it handled the command.
	/// <see langword="false"/> if the command was invoked and it did not handle the command.
	/// </returns>		
	public bool? InvokeCommand (Command command)
	{
		if (!CommandImplementations.ContainsKey (command)) {
			return null;
		}
		return CommandImplementations [command] ();
	}

	/// <summary>
	/// <para>
	/// Adds a new key combination that will trigger the commands in <paramref name="commands"/>
	/// (if supported by the View - see <see cref="GetSupportedCommands"/>).
	/// </para>
	/// <para>
	/// If the key is already bound to a different array of <see cref="Command"/>s it will be
	/// rebound <paramref name="commands"/>.</para>
	/// </summary>
	/// <remarks>
	/// Commands are only ever applied to the current <see cref="View"/> (i.e. this feature
	/// cannot be used to switch focus to another view and perform multiple commands there).
	/// </remarks>
	/// <param name="key">
	/// The key to check.
	/// </param>
	/// <param name="commands">The command to invoked on the <see cref="View"/> when <paramref name="key"/> is pressed.
	/// When multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> strike
	/// will be consumed if any took effect.</param>
	public void AddKeyBinding (Key key, params Command [] commands)
	{
		if (commands.Length == 0) {
			throw new ArgumentException ("At least one command must be specified", nameof (commands));
		}

		if (TryGetKeyBinding (key, out _)) {
			KeyBindings [key] = commands;
		} else {
			KeyBindings.Add (key, commands);
		}
	}

	/// <summary>
	/// Replaces a key combination already bound to a set of <see cref="Command"/>s.
	/// </summary>
	/// <remarks>
	/// </remarks>
	/// <param name="fromKey">The key to be replaced.</param>
	/// <param name="toKey">The new key to be used.</param>
	protected void ReplaceKeyBinding (Key fromKey, Key toKey)
	{
		if (TryGetKeyBinding (fromKey, out var commands)) {
			var value = KeyBindings [fromKey];
			KeyBindings.Remove (fromKey);
			KeyBindings [toKey] = value;
		}
	}

	/// <summary>
	/// Gets the commands bound with the specified Key.
	/// </summary>
	/// <remarks>
	/// </remarks>
	/// <param name="key">
	/// The key to check.
	/// </param>
	/// <param name="commands">
	/// When this method returns, contains the commands bound with the specified Key, if the Key is found;
	/// otherwise, null. This parameter is passed uninitialized.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the Key is bound; otherwise <see langword="false"/>.
	/// </returns>
	public bool TryGetKeyBinding (Key key, out Command [] commands)
	{
		return KeyBindings.TryGetValue (key, out commands);
	}

	/// <summary>
	/// Gets the array of <see cref="Command"/>s bound to <paramref name="key"/> if it exists.
	/// </summary>
	/// <param name="key">
	/// The key to check.
	/// </param>
	/// <returns>The array of <see cref="Command"/>s if <paramref name="key"/> is bound. An empty <see cref="Command"/> array if not.</returns>
	public Command [] GetKeyBinding (Key key)
	{
		if (TryGetKeyBinding (key, out var bindings)) {
			return bindings;
		}
		return Array.Empty<Command> ();
	}

	/// <summary>
	/// Removes all bound keys from the View and resets the default bindings.
	/// </summary>
	public void ClearKeyBindings ()
	{
		KeyBindings.Clear ();
	}

	/// <summary>
	/// Clears the key binding (if any) for the given <paramref name="key"/>.
	/// </summary>
	/// <param name="key">
	/// </param>
	public void ClearKeyBinding (Key key)
	{
		KeyBindings.Remove (key);
	}

	/// <summary>
	/// Removes all key bindings that trigger the given command set. Views can have multiple different
	/// keys bound to the same command sets and this method will clear all of them.
	/// </summary>
	/// <param name="command"></param>
	public void ClearKeyBinding (params Command [] command)
	{
		foreach (var kvp in KeyBindings.Where (kvp => kvp.Value.SequenceEqual (command)).ToArray ()) {
			KeyBindings.Remove (kvp.Key);
		}
	}

	/// <summary>
	/// <para>
	/// Sets the function that will be invoked for a <see cref="Command"/>. Views should call <see cref="AddCommand"/>
	/// for each command they support. 
	/// </para>
	/// <para>
	/// If <see cref="AddCommand"/> has already been called for <paramref name="command"/> <paramref name="f"/> will replace the old one.</para>
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
	/// Gets the Key used by a set of commands.
	/// </summary>
	/// <remarks>
	/// </remarks>
	/// <param name="commands">The set of commands to search.</param>
	/// <returns>The <see cref="Key"/> used by a <see cref="Command"/></returns>
	/// <exception cref="InvalidOperationException">If no matching set of commands was found.</exception>
	public Key GetKeyFromCommands (params Command [] commands)
	{
		return KeyBindings.First (a => a.Value.SequenceEqual (commands)).Key;
	}

	// TODO: Add GetKeysBoundToCommand() - given a Command, return all Keys that would invoke it

	#endregion Key Bindings
}
