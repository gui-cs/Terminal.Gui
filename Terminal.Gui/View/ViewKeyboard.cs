using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminal.Gui;
public partial class View {

	void AddCommands ()
	{
		// By default, the Default command is bound to the HotKey enabling focus
		AddCommand (Command.Default, () => {
			if (CanFocus) {
				SetFocus ();
				return true;
			}
			return false;
		});

		// By default the Accept command does nothing
		AddCommand (Command.Accept, () => false);
	}

	#region HotKey Support
	/// <summary>
	/// Invoked when the <see cref="HotKey"/> is changed.
	/// </summary>
	public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

	ConsoleDriverKey _hotKey = ConsoleDriverKey.Null;

	void TextFormatter_HotKeyChanged (object sender, KeyChangedEventArgs e)
	{
		HotKeyChanged?.Invoke (this, e);
	}

	/// <summary>
	/// Gets or sets the hot key defined for this view. Pressing the hot key on the keyboard while this view has
	/// focus will invoke the <see cref="Command.Default"/> and <see cref="Command.Accept"/> commands. <see cref="Command.Default"/>
	/// causes the view to be focused and <see cref="Command.Accept"/> does nothing.
	/// By default, the HotKey is automatically set to the first
	/// character of <see cref="Text"/> that is prefixed with with <see cref="HotKeySpecifier"/>.
	/// <para>
	/// A HotKey is a keypress that selects a visible UI item. For selecting items across <see cref="View"/>`s
	/// (e.g.a <see cref="Button"/> in a <see cref="Dialog"/>) the keypress must include the <see cref="ConsoleDriverKey.AltMask"/> modifier.
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
	/// By default, when the Hot Key is set, key bindings are added for both the base key (e.g. <see cref="ConsoleDriverKey.D3"/>) and
	/// the Alt-shifted key (e.g. <see cref="ConsoleDriverKey.D3"/> | <see cref="ConsoleDriverKey.AltMask"/>).
	/// This behavior can be overriden by overriding <see cref="KeyBindings.AddsForHotKey"/>.
	/// </para>
	/// <para>
	/// By default, when the HotKey is set to <see cref="ConsoleDriverKey.A"/> through <see cref="ConsoleDriverKey.Z"/> key bindings will be added for both the un-shifted and shifted
	/// versions. This means if the HotKey is <see cref="ConsoleDriverKey.A"/>, key bindings for <see cref="ConsoleDriverKey.A"/> and <see cref="ConsoleDriverKey.A"/> | <see cref="ConsoleDriverKey.ShiftMask"/>
	/// will be added. This behavior can be overriden by overriding <see cref="KeyBindings.AddsForHotKey"/>.
	/// </para>
	/// <para>
	/// If the hot key is changed, the <see cref="HotKeyChanged"/> event is fired.
	/// </para>
	/// </remarks>
	public virtual ConsoleDriverKey HotKey {
		get => _hotKey;
		set {
			if (AddKeyBindingsForHotKey (_hotKey, value)) {
				// This will cause TextFormatter_HotKeyChanged to be called, firing HotKeyChanged
				_hotKey = TextFormatter.HotKey = value;
			}
		}
	}

	/// <summary>
	/// Adds key bindings for the specified HotKey. Useful for views that contain multiple items that each have their own HotKey
	/// such as <see cref="RadioGroup"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// By default key bindings are added for both the base key (e.g. <see cref="ConsoleDriverKey.D3"/>) and
	/// the Alt-shifted key (e.g. <see cref="ConsoleDriverKey.D3"/> | <see cref="ConsoleDriverKey.AltMask"/>).
	/// This behavior can be overriden by overriding <see cref="KeyBindings.AddsForHotKey"/>.
	/// </para>
	/// <para>
	/// By default, when <paramref name="hotKey"/> is <see cref="ConsoleDriverKey.A"/> through <see cref="ConsoleDriverKey.Z"/> key bindings will be added for both the un-shifted and shifted
	/// versions. This means if the HotKey is <see cref="ConsoleDriverKey.A"/>, key bindings for <see cref="ConsoleDriverKey.A"/> and <see cref="ConsoleDriverKey.A"/> | <see cref="ConsoleDriverKey.ShiftMask"/>
	/// will be added. This behavior can be overriden by overriding <see cref="KeyBindings.AddsForHotKey"/>.
	/// </para>
	/// <para>
	/// For each of the bound keys <see cref="Command.Default"/> causes the view to be focused and <see cref="Command.Accept"/> does nothing.
	/// </para>
	/// </remarks>
	/// <param name="prevHotKey">The HotKey <paramref name="hotKey"/> is replacing. Key bindings for this key will be removed.</param>
	/// <param name="hotKey">The new HotKey. If <see cre="Key.Null"/> <paramref name="prevHotKey"/> bindings will be removed.</param>
	/// <returns><see langword="true"/> if the HotKey bindings were added.</returns>
	/// <exception cref="ArgumentException"></exception>
	public virtual bool AddKeyBindingsForHotKey (ConsoleDriverKey prevHotKey, ConsoleDriverKey hotKey)
	{
		if (_hotKey == hotKey) {
			return false;
		}

		var newKey = hotKey == ConsoleDriverKey.Unknown ? ConsoleDriverKey.Null : hotKey;

		var baseKey = newKey & ~ConsoleDriverKey.CtrlMask & ~ConsoleDriverKey.AltMask & ~ConsoleDriverKey.ShiftMask;
		if (newKey != ConsoleDriverKey.Null && (baseKey == ConsoleDriverKey.Space || Rune.IsControl (KeyEventArgs.ToRune (baseKey)))) {
			throw new ArgumentException (@$"HotKey must be a printable (and non-space) key ({hotKey}).");
		}

		if (newKey != baseKey) {
			if ((newKey & ConsoleDriverKey.CtrlMask) != 0) {
				throw new ArgumentException (@$"HotKey does not support CtrlMask ({hotKey}).");
			}
			// Strip off the shift mask if it's A...Z
			if (baseKey is >= ConsoleDriverKey.A and <= ConsoleDriverKey.Z && (newKey & ConsoleDriverKey.ShiftMask) != 0) {
				newKey &= ~ConsoleDriverKey.ShiftMask;
			}
			// Strip off the Alt mask
			newKey &= ~ConsoleDriverKey.AltMask;
		}

		// Remove base version
		if (KeyBindings.TryGet (prevHotKey, out _)) {
			KeyBindings.Remove (prevHotKey);
		}

		// Remove the Alt version
		if (KeyBindings.TryGet (prevHotKey | ConsoleDriverKey.AltMask, out _)) {
			KeyBindings.Remove (prevHotKey | ConsoleDriverKey.AltMask);
		}

		if (_hotKey is >= ConsoleDriverKey.A and <= ConsoleDriverKey.Z) {
			// Remove the shift version
			if (KeyBindings.TryGet (prevHotKey | ConsoleDriverKey.ShiftMask, out _)) {
				KeyBindings.Remove (prevHotKey | ConsoleDriverKey.ShiftMask);
			}
			// Remove alt | shift version
			if (KeyBindings.TryGet (prevHotKey | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask, out _)) {
				KeyBindings.Remove (prevHotKey | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask);
			}
		}

		// Add the new 
		if (newKey != ConsoleDriverKey.Null) {
			// Add the base and Alt key
			KeyBindings.Add (newKey, KeyBindingScope.HotKey, Command.Default, Command.Accept);
			KeyBindings.Add (newKey | ConsoleDriverKey.AltMask, KeyBindingScope.HotKey, Command.Default, Command.Accept);

			// If the Key is A..Z, add ShiftMask and AltMask | ShiftMask
			if (newKey is >= ConsoleDriverKey.A and <= ConsoleDriverKey.Z) {
				KeyBindings.Add (newKey | ConsoleDriverKey.ShiftMask, KeyBindingScope.HotKey, Command.Default, Command.Accept);
				KeyBindings.Add (newKey | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask, KeyBindingScope.HotKey, Command.Default, Command.Accept);
			}
		}
		return true;
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
			if (_hotKey != hk && hk != ConsoleDriverKey.Unknown) {
				HotKey = hk;
			}
		} else {
			HotKey = ConsoleDriverKey.Null;
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
	/// The default keyboard navigation keys are <see cref="ConsoleDriverKey.Tab"/> and <see cref="ConsoleDriverKey.ShiftMask"/>|<see cref="ConsoleDriverKey.Tab"/>.
	/// These can be changed by modifying the key bindings (see <see cref="KeyBindings.Add(ConsoleDriverKey, Command[])"/>) of the SuperView.
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
	/// </summary>
	/// <remarks>
	/// <para>
	/// If the view has a sub view that is focused, <see cref="ProcessKeyDown"/> will be called on the focused view first.
	/// </para>
	/// <para>
	/// If the focused sub view does not handle the key press, this method calls <see cref="OnKeyDown"/> to allow the view
	/// to pre-process the key press. If <see cref="OnKeyDown"/> returns <see langword="false"/>, this method then calls
	/// <see cref="OnInvokingKeyBindings"/> to invoke any key bindings. Then, only if no key bindings are handled,
	/// <see cref="OnKeyPressed"/> will be called allowing the view to process the key press.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	/// <param name="keyEvent"></param>
	/// <returns><see langword="true"/> if the event was handled.</returns>
	public bool ProcessKeyDown (KeyEventArgs keyEvent)
	{
		if (!Enabled) {
			return false;
		}

		// By default the KeyBindingScope is View

		if (Focused?.ProcessKeyDown (keyEvent) == true) {
			return true;
		}

		// Before (fire the cancellable event)
		if (OnKeyDown (keyEvent)) {
			return true;
		}

		// During (this is what can be cancelled)
		var handled = OnInvokingKeyBindings (keyEvent);
		if (handled != null && (bool)handled) {
			return true;
		}

		// After (fire the cancellable event)
		if (OnKeyPressed (keyEvent)) {
			return true;
		}

		return false;
	}

	/// <summary>
	/// Low-level API called when the user presses a key, allowing a view to preprocess the key down event.
	/// This is called from <see cref="ProcessKeyDown"/> before <see cref="OnInvokingKeyBindings"/>.
	/// </summary>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	/// <remarks>
	/// <para>
	/// For processing <see cref="HotKey"/>s and commands, use <see cref="Command"/> and <see cref="KeyBindings.Add(ConsoleDriverKey, Command[])"/>instead.
	/// </para>
	/// <para>
	/// Fires the <see cref="KeyDown"/> event. 
	/// </para>
	/// </remarks>
	public virtual bool OnKeyDown (KeyEventArgs keyEvent)
	{
		// fire event
		KeyDown?.Invoke (this, keyEvent);
		return keyEvent.Handled;
	}

	/// <summary>
	/// Invoked when the user presses a key, allowing subscribers to preprocess the key down event.
	/// This is fired from <see cref="OnKeyDown"/> before <see cref="OnInvokingKeyBindings"/>.
	/// Set <see cref="KeyEventArgs.Handled"/> to true to stop the key from
	/// being processed by other views. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// Not all terminals support key distinct up notifications, Applications should avoid
	/// depending on distinct KeyUp events.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	public event EventHandler<KeyEventArgs> KeyDown;

	/// <summary>
	/// Low-level API called when the user presses a key, allowing views do things during key press events.
	/// This is called from <see cref="ProcessKeyDown"/> after <see cref="OnInvokingKeyBindings"/>. 
	/// </summary>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	/// <remarks>
	/// <para>
	/// Override <see cref="OnKeyPressed"/> to override the behavior of how the base class processes key down events.
	/// </para>
	/// <para>
	/// For processing <see cref="HotKey"/>s and commands, use <see cref="Command"/> and <see cref="KeyBindings.Add(ConsoleDriverKey, Command[])"/>instead.
	/// </para>
	/// <para>
	/// Fires the <see cref="KeyPressed"/> event. 
	/// </para>
	/// <para>
	/// Not all terminals support distinct key up notifications; applications should avoid
	/// depending on distinct KeyUp events.
	/// </para>
	/// </remarks>
	public virtual bool OnKeyPressed (KeyEventArgs keyEvent)
	{
		// fire event
		KeyPressed?.Invoke (this, keyEvent);
		return keyEvent.Handled;
	}

	/// <summary>
	/// Invoked when the users presses a key, allowing subscribers to do things during key down events.
	/// Set <see cref="KeyEventArgs.Handled"/> to true to stop the key from
	/// being processed by other views. Invoked after <see cref="KeyDown"/> and before <see cref="InvokingKeyBindings"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// SubViews can use the <see cref="KeyPressed"/> of their super view override the default behavior of
	/// when key bindings are invoked.
	/// </para>
	/// <para>
	/// Not all terminals support distinct key up notifications; applications should avoid
	/// depending on distinct KeyUp events.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	public event EventHandler<KeyEventArgs> KeyPressed;

	#endregion KeyDown Event

	#region KeyUp Event
	/// <summary>
	/// If the view is enabled, processes a key up event and returns <see langword="true"/> if the event was handled.
	/// Called before <see cref="ProcessKeyDown"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyUp events.
	/// </para>
	/// <para>
	/// If the view has a sub view that is focused, <see cref="ProcessKeyUp"/> will be called on the focused view first.
	/// </para>
	/// <para>
	/// If the focused sub view does not handle the key press, this method calls <see cref="OnKeyUp"/>, which is cancellable.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	/// <param name="keyEvent"></param>
	/// <returns><see langword="true"/> if the event was handled.</returns>
	public bool ProcessKeyUp (KeyEventArgs keyEvent)
	{
		if (!Enabled) {
			return false;
		}

		if (Focused?.ProcessKeyUp (keyEvent) == true) {
			return true;
		}

		// Before (fire the cancellable event)
		if (OnKeyUp (keyEvent)) {
			return true;
		}

		// During (this is what can be cancelled)
		// TODO: Until there's a clear use-case, we will not define 'during' event (e.g. OnDuringKeyUp). 

		// After (fire the cancellable event InvokingKeyBindings)
		// TODO: Until there's a clear use-case, we will not define an 'after' event (e.g. OnAfterKeyUp). 

		return false;
	}

	/// <summary>
	/// Method invoked when a key is released. This method is called from <see cref="ProcessKeyUp"/>.
	/// </summary>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key stroke was not handled. <see langword="true"/> if no
	/// other view should see it.</returns>
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyUp events.
	/// <para>
	/// Overrides must call into the base and return <see langword="true"/> if the base returns <see langword="true"/>.
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
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyDown"/>.
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	/// </summary>
	public event EventHandler<KeyEventArgs> KeyUp;

	#endregion KeyUp Event

	#endregion Low-level Key handling

	#region Key Bindings

	/// <summary>
	/// Gets the key bindings for this view.
	/// </summary>
	public KeyBindings KeyBindings { get; } = new ();
	private Dictionary<Command, Func<bool?>> CommandImplementations { get; } = new ();

	/// <summary>
	/// Low-level API called when a user presses a key; invokes any key bindings set on the view.
	/// This is called during <see cref="ProcessKeyDown"/> after <see cref="OnKeyDown"/> has returned.
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
	public virtual bool? OnInvokingKeyBindings (KeyEventArgs keyEvent)
	{
		// fire event
		// BUGBUG: KeyEventArgs doesn't include scope, so the event never sees it.
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
			// Stop processing if any key binding handled the key.
			// DO NOT stop processing if there are no matching key bindings or none of the key bindings handled the key
			return true;
		}

		foreach (var view in Subviews.Where (v => v.KeyBindings.TryGet (keyEvent.Key, KeyBindingScope.HotKey, out var _))) {
			if (view.KeyBindings.TryGet (keyEvent.Key, KeyBindingScope.HotKey, out var binding)) {
				keyEvent.Scope = KeyBindingScope.HotKey;
				handled = view.OnInvokingKeyBindings (keyEvent);
				if (handled != null && (bool)handled) {
					return true;
				}
			}
		}

		//// TODO: Refactor this per https://github.com/gui-cs/Terminal.Gui/pull/2927#discussion_r1420415162
		//// TODO: The problem that needs to be solved is:
		//// TODO: - How to enable a view to define global key bindings like StatusBar and MenuBar?
		//// see https://github.com/gui-cs/Terminal.Gui/issues/3042
		//foreach (var view in Subviews.Where (v => v is StatusBar or RadioGroup || keyEvent.BareKey == v.HotKey)) {
		//	handled = view.OnInvokingKeyBindings (keyEvent);
		//	if (handled != null && (bool)handled) {
		//		return true;
		//	}
		//}

		//foreach (var view in Subviews.Where (v => v is MenuBar)) {
		//	if (view.KeyBindings.TryGet (keyEvent.Key, KeyBindingScope.SuperView, out var binding)) {
		//		handled = view.OnInvokingKeyBindings (keyEvent);
		//		if (handled != null && (bool)handled) {
		//			return true;
		//		}
		//	}
		//}

		return handled;
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
		if (!KeyBindings.TryGet (key, out var binding)) {
			return null;
		}
		foreach (var command in binding.Commands) {

			if (!CommandImplementations.ContainsKey (command)) {
				throw new NotSupportedException (@$"A KeyBinding was set up for the command {command} ({keyEvent.Key}) but that command is not supported by this View ({GetType ().Name})");
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
		// replace that implementation
		// else record how to perform the action (this should be the normal case)
		if (CommandImplementations != null) {
			CommandImplementations [command] = f;
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

	// TODO: Add GetKeysBoundToCommand() - given a Command, return all Keys that would invoke it

	#endregion Key Bindings
}
