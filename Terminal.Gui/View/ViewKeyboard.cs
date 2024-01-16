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

	Key _hotKey = new Key ();

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
	/// (e.g.a <see cref="Button"/> in a <see cref="Dialog"/>) the keypress must include the <see cref="Key.WithAlt"/> modifier.
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
	/// By default, when the Hot Key is set, key bindings are added for both the base key (e.g. <see cref="KeyCode.D3"/>) and
	/// the Alt-shifted key (e.g. <see cref="KeyCode.D3"/> | <see cref="KeyCode.AltMask"/>).
	/// This behavior can be overriden by overriding <see cref="AddKeyBindingsForHotKey"/>.
	/// </para>
	/// <para>
	/// By default, when the HotKey is set to <see cref="Key.A"/> through <see cref="KeyCode.Z"/> key bindings will be added for both the un-shifted and shifted
	/// versions. This means if the HotKey is <see cref="Key.A"/>, key bindings for <c>Key.A</c> and <c>Key.A.WithShift</c>
	/// will be added. This behavior can be overriden by overriding <see cref="AddKeyBindingsForHotKey"/>.
	/// </para>
	/// <para>
	/// If the hot key is changed, the <see cref="HotKeyChanged"/> event is fired.
	/// </para>
	/// <para>
	/// Set to <see cref="KeyCode.Null"/> to disable the hot key.
	/// </para>
	/// </remarks>
	public virtual Key HotKey {
		get => _hotKey;
		set {
			if (value is null) {
				throw new ArgumentException (@"HotKey must not be null. Use Key.Empty to clear the HotKey.", nameof (value));
			}
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
	/// By default key bindings are added for both the base key (e.g. <see cref="Key.D3"/>) and
	/// the Alt-shifted key (e.g. <c>Key.D3.WithAlt</c>
	/// This behavior can be overriden by overriding <see cref="AddKeyBindingsForHotKey"/>.
	/// </para>
	/// <para>
	/// By default, when <paramref name="hotKey"/> is <see cref="Key.A"/> through <see cref="Key.Z"/> key bindings will be added for both the un-shifted and shifted
	/// versions. This means if the HotKey is <see cref="Key.A"/>, key bindings for <c>Key.A</c> and <c>Key.A.WithShift</c>
	/// will be added. This behavior can be overriden by overriding <see cref="AddKeyBindingsForHotKey"/>.
	/// </para>
	/// <para>
	/// For each of the bound keys <see cref="Command.Default"/> causes the view to be focused and <see cref="Command.Accept"/> does nothing.
	/// </para>
	/// </remarks>
	/// <param name="prevHotKey">The HotKey <paramref name="hotKey"/> is replacing. Key bindings for this key will be removed.</param>
	/// <param name="hotKey">The new HotKey. If <see cref="Key.Empty"/> <paramref name="prevHotKey"/> bindings will be removed.</param>
	/// <returns><see langword="true"/> if the HotKey bindings were added.</returns>
	/// <exception cref="ArgumentException"></exception>
	public virtual bool AddKeyBindingsForHotKey (Key prevHotKey, Key hotKey)
	{
		if ((KeyCode)_hotKey == hotKey) {
			return false;
		}

		var newKey = hotKey;

		var baseKey = newKey.NoAlt.NoShift.NoCtrl;
		if (newKey != Key.Empty && (baseKey == Key.Space || Rune.IsControl (baseKey.AsRune))) {
			throw new ArgumentException (@$"HotKey must be a printable (and non-space) key ({hotKey}).");
		}

		if (newKey != baseKey) {
			if (newKey.IsCtrl) {
				throw new ArgumentException (@$"HotKey does not support CtrlMask ({hotKey}).");
			}
			// Strip off the shift mask if it's A...Z
			if (baseKey.IsKeyCodeAtoZ) {
				newKey = newKey.NoShift;
			}
			// Strip off the Alt mask
			newKey = newKey.NoAlt;
		}

		// Remove base version
		if (KeyBindings.TryGet (prevHotKey, out _)) {
			KeyBindings.Remove (prevHotKey);
		}

		// Remove the Alt version
		if (KeyBindings.TryGet (prevHotKey.WithAlt, out _)) {
			KeyBindings.Remove (prevHotKey.WithAlt);
		}

		if (_hotKey.KeyCode is >= KeyCode.A and <= KeyCode.Z) {
			// Remove the shift version
			if (KeyBindings.TryGet (prevHotKey.WithShift, out _)) {
				KeyBindings.Remove (prevHotKey.WithShift);
			}
			// Remove alt | shift version
			if (KeyBindings.TryGet (prevHotKey.WithShift.WithAlt, out _)) {
				KeyBindings.Remove (prevHotKey.WithShift.WithAlt);
			}
		}

		// Add the new 
		if (newKey != KeyCode.Null) {
			// Add the base and Alt key
			KeyBindings.Add (newKey, KeyBindingScope.HotKey, Command.Default, Command.Accept);
			KeyBindings.Add (newKey.WithAlt, KeyBindingScope.HotKey, Command.Default, Command.Accept);

			// If the Key is A..Z, add ShiftMask and AltMask | ShiftMask
			if (newKey.IsKeyCodeAtoZ) {
				KeyBindings.Add (newKey.WithShift, KeyBindingScope.HotKey, Command.Default, Command.Accept);
				KeyBindings.Add (newKey.WithShift.WithAlt, KeyBindingScope.HotKey, Command.Default, Command.Accept);
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
		if (TextFormatter.FindHotKey (_text, HotKeySpecifier, out _, out var hk)) {
			if (_hotKey.KeyCode != hk) {
				HotKey = hk;
			}
		} else {
			HotKey = KeyCode.Null;
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
	/// The default keyboard navigation keys are <c>Key.Tab</c> and <c>Key>Tab.WithShift</c>.
	/// These can be changed by modifying the key bindings (see <see cref="KeyBindings.Add(Key, Command[])"/>) of the SuperView.
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
	/// If the view is enabled, processes a new key down event and returns <see langword="true"/> if the event was handled.
	/// </summary>
	/// <remarks>
	/// <para>
	/// If the view has a sub view that is focused, <see cref="NewKeyDownEvent"/> will be called on the focused view first.
	/// </para>
	/// <para>
	/// If the focused sub view does not handle the key press, this method calls <see cref="OnKeyDown"/> to allow the view
	/// to pre-process the key press. If <see cref="OnKeyDown"/> returns <see langword="false"/>, this method then calls
	/// <see cref="OnInvokingKeyBindings"/> to invoke any key bindings. Then, only if no key bindings are handled,
	/// <see cref="OnProcessKeyDown"/> will be called allowing the view to process the key press.
	/// </para>
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	/// <param name="keyEvent"></param>
	/// <returns><see langword="true"/> if the event was handled.</returns>
	public bool NewKeyDownEvent (Key keyEvent)
	{
		if (!Enabled) {
			return false;
		}

		// By default the KeyBindingScope is View

		if (Focused?.NewKeyDownEvent (keyEvent) == true) {
			return true;
		}

		// Before (fire the cancellable event)
		if (OnKeyDown (keyEvent)) {
			return true;
		}

		// During (this is what can be cancelled)
		InvokingKeyBindings?.Invoke (this, keyEvent);
		if (keyEvent.Handled) {
			return true;
		}
		var handled = OnInvokingKeyBindings (keyEvent);
		if (handled != null && (bool)handled) {
			return true;
		}

		// TODO: The below is not right. OnXXX handlers are supposed to fire the events.
		// TODO: But I've moved it outside of the v-function to test something.
		// After (fire the cancellable event)
		// fire event
		ProcessKeyDown?.Invoke (this, keyEvent);
		if (!keyEvent.Handled && OnProcessKeyDown (keyEvent)) {
			return true;
		}


		return keyEvent.Handled;
	}

	/// <summary>
	/// Low-level API called when the user presses a key, allowing a view to pre-process the key down event.
	/// This is called from <see cref="NewKeyDownEvent"/> before <see cref="OnInvokingKeyBindings"/>.
	/// </summary>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	/// <remarks>
	/// <para>
	/// For processing <see cref="HotKey"/>s and commands, use <see cref="Command"/> and <see cref="KeyBindings.Add(Key, Command[])"/>instead.
	/// </para>
	/// <para>
	/// Fires the <see cref="KeyDown"/> event. 
	/// </para>
	/// </remarks>
	public virtual bool OnKeyDown (Key keyEvent)
	{
		// fire event
		KeyDown?.Invoke (this, keyEvent);
		return keyEvent.Handled;
	}

	/// <summary>
	/// Invoked when the user presses a key, allowing subscribers to pre-process the key down event.
	/// This is fired from <see cref="OnKeyDown"/> before <see cref="OnInvokingKeyBindings"/>.
	/// Set <see cref="Key.Handled"/> to true to stop the key from
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
	public event EventHandler<Key> KeyDown;

	/// <summary>
	/// Low-level API called when the user presses a key, allowing views do things during key down events.
	/// This is called from <see cref="NewKeyDownEvent"/> after <see cref="OnInvokingKeyBindings"/>. 
	/// </summary>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	/// <remarks>
	/// <para>
	/// Override <see cref="OnProcessKeyDown"/> to override the behavior of how the base class processes key down events.
	/// </para>
	/// <para>
	/// For processing <see cref="HotKey"/>s and commands, use <see cref="Command"/> and <see cref="KeyBindings.Add(Key, Command[])"/>instead.
	/// </para>
	/// <para>
	/// Fires the <see cref="ProcessKeyDown"/> event. 
	/// </para>
	/// <para>
	/// Not all terminals support distinct key up notifications; applications should avoid
	/// depending on distinct KeyUp events.
	/// </para>
	/// </remarks>
	public virtual bool OnProcessKeyDown (Key keyEvent)
	{
		//ProcessKeyDown?.Invoke (this, keyEvent);
		return keyEvent.Handled;
	}

	/// <summary>
	/// Invoked when the users presses a key, allowing subscribers to do things during key down events.
	/// Set <see cref="Key.Handled"/> to true to stop the key from
	/// being processed by other views. Invoked after <see cref="KeyDown"/> and before <see cref="InvokingKeyBindings"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// SubViews can use the <see cref="ProcessKeyDown"/> of their super view override the default behavior of
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
	public event EventHandler<Key> ProcessKeyDown;

	#endregion KeyDown Event

	#region KeyUp Event
	/// <summary>
	/// If the view is enabled, processes a new key up event and returns <see langword="true"/> if the event was handled.
	/// Called before <see cref="NewKeyDownEvent"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyUp events.
	/// </para>
	/// <para>
	/// If the view has a sub view that is focused, <see cref="NewKeyUpEvent"/> will be called on the focused view first.
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
	public bool NewKeyUpEvent (Key keyEvent)
	{
		if (!Enabled) {
			return false;
		}

		if (Focused?.NewKeyUpEvent (keyEvent) == true) {
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
	/// Method invoked when a key is released. This method is called from <see cref="NewKeyUpEvent"/>.
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
	public virtual bool OnKeyUp (Key keyEvent)
	{
		// fire event
		KeyUp?.Invoke (this, keyEvent);
		if (keyEvent.Handled) {
			return true;
		}

		return false;
	}

	/// <summary>
	/// Invoked when a key is released. Set <see cref="Key.Handled"/> to true to stop the key up event from being processed by other views.
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyDown"/>.
	/// <para>
	/// See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see>
	/// </para>
	/// </remarks>
	/// </summary>
	public event EventHandler<Key> KeyUp;

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
	/// This is called during <see cref="NewKeyDownEvent"/> after <see cref="OnKeyDown"/> has returned.
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
	public virtual bool? OnInvokingKeyBindings (Key keyEvent)
	{
		// fire event
		// BUGBUG: KeyEventArgs doesn't include scope, so the event never sees it.
		if (keyEvent.Scope == KeyBindingScope.Application || keyEvent.Scope == KeyBindingScope.HotKey) {
			InvokingKeyBindings?.Invoke (this, keyEvent);
			if (keyEvent.Handled) {
				return true;
			}
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

		// Now, process any key bindings in the subviews that are tagged to KeyBindingScope.HotKey.
		foreach (var view in Subviews.Where (v => v.KeyBindings.TryGet (keyEvent.KeyCode, KeyBindingScope.HotKey, out var _))) {
			// TODO: I think this TryGet is not needed due to the one in the lambda above. Use `Get` instead?
			if (view.KeyBindings.TryGet (keyEvent.KeyCode, KeyBindingScope.HotKey, out var binding)) {
				keyEvent.Scope = KeyBindingScope.HotKey;
				handled = view.OnInvokingKeyBindings (keyEvent);
				if (handled != null && (bool)handled) {
					return true;
				}
			}
		}

		return handled;
	}

	/// <summary>
	/// Invoked when a key is pressed that may be mapped to a key binding. Set <see cref="Key.Handled"/>
	/// to true to stop the key from being processed by other views. 
	/// </summary>
	public event EventHandler<Key> InvokingKeyBindings;

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
	protected bool? InvokeKeyBindings (Key keyEvent)
	{
		bool? toReturn = null;
		var key = keyEvent.KeyCode;
		if (!KeyBindings.TryGet (key, out var binding)) {
			return null;
		}
		foreach (var command in binding.Commands) {

			if (!CommandImplementations.ContainsKey (command)) {
				throw new NotSupportedException (@$"A KeyBinding was set up for the command {command} ({keyEvent.KeyCode}) but that command is not supported by this View ({GetType ().Name})");
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
