using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Terminal.Gui;
public partial class View {
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
	/// focus will cause the <see cref="MouseClick"/> event to fire.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This API is a helper API for configuring a key binding for the hot key. It uses the <see cref="TextFormatter.HotKey"/>
	/// to determine the hot key from <see cref="Title"/> by looking for the first character prefixed with <see cref="HotKeySpecifier"/>.
	/// </para>
	/// <para>
	/// If the hot key is changed, the <see cref="HotKeyChanged"/> event is fired.
	/// </para>
	/// </remarks>
	public virtual Key HotKey {
		get => _hotKey;
		set {
			if (_hotKey != value) {
				var v = value == Key.Unknown ? Key.Null : value;
				// Force upper case
				var mask = v & Key.CharMask;
				if (mask >= Key.a && mask <= Key.z) {
					v = (Key)((int)v - 32);
				}
				if (_hotKey != Key.Null && ContainsKeyBinding (Key.Space | _hotKey)) {
					if (v == Key.Null) {
						ClearKeyBinding (Key.Space | _hotKey);
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
	/// Gets or sets the specifier character for the hot key (e.g. '_'). Set to '\xffff' to disable hot key support for this View instance.
	/// The default is '\xffff'. 
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
		if (TextFormatter == null || HotKeySpecifier == new Rune('\xFFFF')) {
			return; // throw new InvalidOperationException ("Can't set HotKey unless a TextFormatter has been created");
		}
		TextFormatter.FindHotKey (_text, HotKeySpecifier, true, out _, out var hk);
		if (_hotKey != hk) {
			HotKey = hk;
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
	/// The default keyboard navigation keys are <see cref="Key.Tab"/> and  <see cref="Key.BackTab"/>. These can be
	/// changed by modifying the key bindings (see <see cref="AddKeyBinding(Key, Command[])"/>) of the SuperView.
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
	/// <summary>
	/// A low-level method to support hot keys (e.g. Alt-X). Can be overridden to provide accelerator functionality.
	/// Typical apps will use <see cref="Command"/> instead.
	/// </summary>
	/// <remarks>
	///   <para>
	///     Before keys are sent to the subview on the
	///     current view, all the views are
	///     processed and the key is passed to the widgets
	///     to allow some of them to process the keystroke
	///     as a hot-key. </para>
	///  <para>
	///     For example, if you implement a button that
	///     has a hotkey ok "o", you would catch the
	///     combination Alt-o here.  If the event is
	///     caught, you must return true to stop the
	///     keystroke from being dispatched to other
	///     views.
	///  </para>
	/// </remarks>
	public virtual bool OnHotKey (KeyEventArgs keyEvent)
	{

		if (!Enabled) {
			return false;
		}

		if (MostFocused?.ProcessKeyPressed (keyEvent) == true) {
			return true;
		}

		if (_subviews == null || _subviews.Count == 0) {
			return false;
		}

		foreach (var view in _subviews) {
			if (view.OnHotKey (keyEvent))
				return true;
		}
		return false;
	}
	
	/// <summary>
	/// Invoked when a key is depressed.
	/// </summary>
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPressed"/>.
	/// <para>
	/// Overrides must call into the base and return <see langword="true"/> if the base returns  <see langword="true"/>.
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
	/// Invoked when a key is depressed. Set <see cref="KeyEventArgs.Handled"/> to true to stop the key from being processed by other views.
	/// </summary>
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPressed"/>.
	/// </remarks>
	public event EventHandler<KeyEventArgs> KeyDown;

	/// <summary>
	/// Method invoked when a key is released. This method will be called after <see cref="OnKeyPressed"/>.
	/// </summary>
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPressed"/>.
	/// <para>
	/// Overrides must call into the base and return <see langword="true"/> if the base returns  <see langword="true"/>.
	/// </para>
	/// </remarks>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key stroke was not handled. <see langword="true"/> if no
	/// other view should see it.</returns>
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
	/// Invoked when a key is released. Set <see cref="KeyEventArgs.Handled"/> to true to stop the key from being processed by other views.
	/// </summary>
	/// <remarks>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPressed"/>.
	/// </remarks>
	public event EventHandler<KeyEventArgs> KeyUp;

	/// <summary>
	/// If the view is enabled, processes a key pressed event and returns <see langword="true"/> if the event was handled.
	/// </summary>
	/// <remarks>
	/// <para>Calls <see cref="OnKeyPressed(KeyEventArgs)"/> and <see cref="OnInvokeKeyBindings(KeyEventArgs)"/>.</para>
	/// </remarks>
	/// <param name="keyEvent"></param>
	/// <returns><see langword="true"/> if the event was handled.</returns>
	public bool ProcessKeyPressed (KeyEventArgs keyEvent)
	{
		if (!Enabled) {
			return false;
		}

		// TODO: Figure out how to enable a view to see the key event regardless of whether a child view is focused or not.
		if (Focused?.ProcessKeyPressed (keyEvent) == true) {
			return true;
		}

		if (OnKeyPressed (keyEvent)) {
			return true;
		}

		if (OnInvokeKeyBindings (keyEvent)) {
			return true;
		}

		return false;
	}

	/// <summary>
	/// Low-level API called when a key is pressed. This is called before <see cref="OnInvokeKeyBindings(KeyEventArgs)"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// For processing shortcuts, hot-keys, and commands, use <see cref="Command"/> and <see cref="AddKeyBinding(Key, Command[])"/>instead.
	/// </para>
	/// <para>
	/// Fires the <see cref="KeyPressed"/> event.
	/// </para>
	/// <para>
	/// Called after <see cref="OnKeyDown"/> and before <see cref="OnKeyUp"/>.
	/// </para>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPressed"/>.
	/// </para>
	/// </remarks>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	public virtual bool OnKeyPressed (KeyEventArgs keyEvent)
	{
		// fire event
		KeyPressed?.Invoke (this, keyEvent);
		return keyEvent.Handled;
	}

	/// <summary>
	/// Invoked when a key is pressed. Set <see cref="KeyEventArgs.Handled"/> to true to stop the key from
	/// being processed by other views. Invoked after <see cref="KeyDown"/> and before <see cref="KeyUp"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Not all terminals support key distinct down/up notifications, Applications should avoid
	/// depending on distinct KeyDown and KeyUp events and instead should use <see cref="KeyPressed"/>.
	/// </para>
	/// </remarks>
	public event EventHandler<KeyEventArgs> KeyPressed;
	#endregion Low-level Key handling

	#region Key Bindings
	/// <summary>
	/// Gets the key bindings for this view.
	/// </summary>
	private Dictionary<Key, Command []> KeyBindings { get; set; } = new Dictionary<Key, Command []> ();
	private Dictionary<Command, Func<bool?>> CommandImplementations { get; set; } = new Dictionary<Command, Func<bool?>> ();

	/// <summary>
	/// Low-level API called when a key is pressed to invoke any key bindings set on the view.
	/// This is called after <see cref="OnKeyPressed(KeyEventArgs)"/>. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// Fires the <see cref="InvokingKeyBindings"/> event.
	/// </para>
	/// </remarks>
	/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
	/// <returns><see langword="false"/> if the key press was not handled. <see langword="true"/> if
	/// the keypress was handled and no other view should see it.</returns>
	public virtual bool OnInvokeKeyBindings (KeyEventArgs keyEvent)
	{
		// fire event
		InvokingKeyBindings?.Invoke (this, keyEvent);
		if (keyEvent.Handled) {
			return true;
		}
		var ret = InvokeKeyBindings (keyEvent);
		if (ret != null && (bool)ret) {
			return true;
		}

		foreach (var view in Subviews.Where (v => v is not Toplevel && v.Enabled && !v.HasFocus && v.KeyBindings.Count > 0)) {
			if (view.OnInvokeKeyBindings (keyEvent)) {
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

		// Force upper case
		var key = keyEvent.Key;
		var mask = key & Key.CharMask;
		if (mask >= Key.a && mask <= Key.z) {
			key = (Key)((int)key - 32);
		}

		if (KeyBindings.TryGetValue (key, out var binding)) {

			foreach (var command in binding) {

				if (!CommandImplementations.ContainsKey (command)) {
					throw new NotSupportedException ($"A KeyBinding was set up for the command {command} ({keyEvent.Key}) but that command is not supported by this View ({GetType ().Name})");
				}

				// each command has its own return value
				var thisReturn = InvokeCommand(command);

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

		// Force upper case
		var mask = key & Key.CharMask;
		if (mask >= Key.a && mask <= Key.z) {
			key = (Key)((int)key - 32);
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
		// Force upper case
		var mask = toKey & Key.CharMask;
		if (mask is >= Key.a and <= Key.z) {
			toKey = (Key)((int)toKey - 32);
		}
		mask = fromKey & Key.CharMask;
		if (mask is >= Key.a and <= Key.z) {
			fromKey = (Key)((int)fromKey - 32);
		}
		if (ContainsKeyBinding (fromKey)) {
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
		// Force upper case
		var mask = key & Key.CharMask;
		if (mask is >= Key.a and <= Key.z) {
			key = (Key)((int)key - 32);
		}
		return KeyBindings.ContainsKey (key);
	}

	/// <summary>
	/// Gets the array of <see cref="Command"/>s bound to <paramref name="key"/> if it exists.
	/// </summary>
	/// <param name=""></param>
	/// <returns>The array of <see cref="Command"/>s if <paramref name="key"/> is bound. An empty <see cref="Command"/> array if not.</returns>
	public Command [] GetKeyBindings (Key key)
	{
		// Force upper case
		var mask = key & Key.CharMask;
		if (mask is >= Key.a and <= Key.z) {
			key = (Key)((int)key - 32);
		}
		if (KeyBindings.TryGetValue (key, out var bindings)) {
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
	/// Clears the existing keybinding (if any) for the given <paramref name="key"/>.
	/// </summary>
	/// <param name="key"></param>
	public void ClearKeyBinding (Key key)
	{
		KeyBindings.Remove (key);
	}

	/// <summary>
	/// Removes all key bindings that trigger the given command. Views can have multiple different
	/// keys bound to the same command and this method will clear all of them.
	/// </summary>
	/// <param name="command"></param>
	public void ClearKeyBinding (params Command [] command)
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
		return KeyBindings.First (a => a.Value.SequenceEqual (command)).Key;
	}

	#endregion Key Bindings

}
