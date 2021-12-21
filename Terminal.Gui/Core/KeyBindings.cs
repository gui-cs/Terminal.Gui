using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Terminal.Gui {
	/// <summary>
	/// All keybindable operations supported by Terminal.Gui views.  Concepts should be kept
	/// broad enough to apply to all views e.g. LineScrollDown.  Not all <see cref="View"/> implement
	/// all <see cref="Command"/>
	/// </summary>
	public enum Command {

		/// <summary>
		/// When bound to keys performs the standard platform behavior.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Moves the caret down one line.
		/// </summary>
		LineDown,

		/// <summary>
		/// Scrolls down one line.
		/// </summary>
		LineScrollDown,

		/// <summary>
		/// Moves the caret down one page.
		/// </summary>
		PageDown,

		/// <summary>
		/// Extends the selection down one page.
		/// </summary>
		PageDownExtend,
	}

	/// <summary>
	/// Class that implement the keys used by the <see cref="KeyBindings"/> for any <see cref="View"/>.
	/// </summary>
	public sealed class KeyBinding : ICloneable {
		/// <summary>
		/// Initializes a new key binding for a view. If <see cref="View"/> is used will applied to all views.
		/// </summary>
		/// <param name="view">The view type.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <param name="description">The description.</param>
		/// <param name="enabled"><c>true</c> if enabled, <c>false</c>otherwise.</param>
		public KeyBinding (Key inKey, Key outKey, Command command = default, Action action = null, string description = "", bool enabled = true)
		{
			//if (view == null) {
			//	throw new ArgumentNullException ("View cannot be null.", nameof (view));
			//}
			//if (!typeof (View).IsAssignableFrom (view)) {
			//	throw new ArgumentException ("Type is not assignable from View.", nameof (view));
			//}
			Initialize (inKey, outKey, command, action, description, enabled);
		}

		/// <summary>
		/// Initializes a new key binding for a view. If <see cref="View"/> is used will applied to all views.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <param name="description">The description.</param>
		/// <param name="enabled"><c>true</c> if enabled, <c>false</c>otherwise.</param>
		//public KeyBinding (Key inKey, Key outKey, Command command = default, Action action = null, string description = "", bool enabled = true)
		//{
		//	Initialize (inKey, outKey, command, action, description, enabled);
		//}

		private void Initialize (Key inKey, Key outKey, Command command, Action action, string description, bool enabled)
		{
			//if (string.IsNullOrEmpty (view)) {
			//	throw new ArgumentNullException ("View cannot be null or empty.", nameof (view));
			//}
			//View = view;
			InKey = inKey;
			OutKey = outKey;
			Description = description;
			Enabled = enabled;
			Command = command;
			Action = action;
		}

		/// <summary>
		/// Get the view name.
		/// </summary>
		public string View { get; internal set; }
		/// <summary>
		/// Get or sets the input key pressed.
		/// </summary>
		public Key InKey { get; set; }
		/// <summary>
		/// Gets or sets the desired output key.
		/// </summary>
		public Key OutKey { get; set; }
		/// <summary>
		/// Gets or sets the description for this key.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Gets or sets the enable status.
		/// </summary>
		public bool Enabled { get; set; } = true;

		public Command Command;

		public Action Action { get; set; }

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		public object Clone ()
		{
			return this.MemberwiseClone ();
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString ()
		{
			return $"{View}({InKey})({OutKey})";
		}
	}

	/// <summary>
	/// Manage all the <see cref="KeyBinding"/> collection used by the <see cref="Application.KeyBindings"/>
	/// </summary>
	public sealed class KeyBindings {
		private class KeyBindingEqualityComparer : IEqualityComparer<KeyBinding> {
			public bool Equals (KeyBinding x, KeyBinding y)
			{
				if (x.InKey == y.InKey || x.OutKey == y.OutKey && x.Action == null && y.Action == null
					|| (x.OutKey == default && y.OutKey == default && x.Action == y.Action))
					return true;
				return false;
			}

			public bool ReferenceEquals (KeyBinding x, KeyBinding y)
			{
				PropertyInfo [] xProps = x.GetType ()
					.GetProperties (BindingFlags.Public | BindingFlags.Instance);
				PropertyInfo [] yProps = y.GetType ()
					.GetProperties (BindingFlags.Public | BindingFlags.Instance);

				for (int i = 0; i < xProps.Length; i++) {
					PropertyInfo piX = xProps [i];
					PropertyInfo piY = yProps [i];
					if (piX.Name != piY.Name) {
						throw new InvalidOperationException (
							$"The type {nameof (piX)} doesn't have the same hierarchy of type {nameof (piY)}");
					}
					if (!piX.GetValue (x, null).Equals (piY.GetValue (y, null))) {
						return false;
					}
				}
				return true;
			}

			public int GetHashCode (KeyBinding obj)
			{
				if (obj == null)
					throw new ArgumentNullException ();

				int hCode = 0;
				foreach (var c in obj.View) {
					hCode += c;
				}
				return hCode.GetHashCode ();
			}
		}

		/// <summary>
		/// Initializes a default instance.
		/// </summary>
		public KeyBindings ()
		{
		}

		/// <summary>
		/// Initializes a instance with the necessary settings.
		/// </summary>
		/// <param name="view">The view type.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <param name="description">The description.</param>
		/// <param name="enabled"><c>true</c> if enabled, <c>false</c>otherwise.</param>
		public KeyBindings (Key inKey, Key outKey, Command command = default, Action action = null, string description = "", bool enabled = true)
		{
			AddKey (inKey, outKey, command, action, description, enabled);
		}

		/// <summary>
		/// Initializes a instance with the necessary settings.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <param name="description">The description.</param>
		/// <param name="enabled"><c>true</c> if enabled, <c>false</c>otherwise.</param>
		//public KeyBindings (Key inKey, Key outKey, Command command = default, Action action = null, string description = "", bool enabled = true)
		//{
		//	AddKey (inKey, outKey, action, description, enabled);
		//}

		/// <summary>
		/// Initializes a instance of a <see cref="KeyBinding"/> instance.
		/// </summary>
		/// <param name="keyBinding">The key binding.</param>
		public KeyBindings (KeyBinding keyBinding)
		{
			AddKey (keyBinding);
		}

		/// <summary>
		/// The views name with the enabled status.
		/// </summary>
		//public Dictionary<string, bool> Views { get; private set; } = new Dictionary<string, bool> ();

		/// <summary>
		/// The <see cref="KeyBinding"/> collection.
		/// </summary>
		public List<KeyBinding> Keys { get; private set; } = new List<KeyBinding> ();

		/// <summary>
		/// Get or sets the enabled status.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Gets or sets the enable key to leave insert mode returning to the keys bindings.
		/// </summary>
		public Key EnableKey { get; set; } = Key.Esc;

		/// <summary>
		/// Get or sets the disable key to enter insert mode leaving the keys bindings.
		/// </summary>
		public Key DisableKey { get; set; } = Key.Enter;

		public bool EnabledDisabledKeyStatus { get; set; } = true;

		/// <summary>
		/// Gets the total of key bindings.
		/// </summary>
		public int Count => Keys.Count;

		/// <summary>
		/// Add a new <see cref="KeyBinding"/> to the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="view">The view type.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <param name="description">The description.</param>
		/// <param name="enabled"><c>true</c> if enabled, <c>false</c>otherwise.</param>
		/// <returns><c>true</c> if the key binding was successfully added, <c>false</c>otherwise.</returns>
		//public bool AddKey (Key inKey, Key outKey, Command command = default, Action action = null, string description = "", bool enabled = true)
		//{
		//	//if (view == null) {
		//	//	throw new ArgumentNullException ("View cannot be null.", nameof (view));
		//	//}
		//	//if (!typeof (View).IsAssignableFrom (view)) {
		//	//	throw new ArgumentException ("Type is not assignable from View.", nameof (view));
		//	//}
		//	return AddKey (inKey, outKey, command, action, description, enabled);
		//}

		/// <summary>
		/// Add a new <see cref="KeyBinding"/> to the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <param name="description">The description.</param>
		/// <param name="enabled"><c>true</c> if enabled, <c>false</c>otherwise.</param>
		/// <returns><c>true</c> if the key binding was successfully added, <c>false</c>otherwise.</returns>
		public bool AddKey (Key inKey, Key outKey, Command command = default, Action action = null, string description = "", bool enabled = true)
		{
			//if (view == null) {
			//	throw new ArgumentNullException ("View cannot be null.", nameof (view));
			//}
			var kb = new KeyBinding (inKey, outKey, command, action, description, enabled);
			//if (!Views.ContainsKey (view)) {
			//	Views.Add (view, true);
			//}
			//if (Keys.Contains (kb, new KeyBindingEqualityComparer ())) {
			//	throw new ArgumentException ("One of the keys already exists.", nameof (view));
			//}
			Keys.Add (kb);
			return true;
		}

		/// <summary>
		/// Add a new <see cref="KeyBinding"/> to the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="keyBinding">The key binding.</param>
		/// <returns><c>true</c> if the key binding was successfully added, <c>false</c>otherwise.</returns>
		public bool AddKey (KeyBinding keyBinding)
		{
			return AddKey (keyBinding.InKey, keyBinding.OutKey, keyBinding.Command, keyBinding.Action, keyBinding.Description, keyBinding.Enabled);
		}

		/// <summary>
		/// Removes a <see cref="KeyBinding"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <returns><c>true</c> if the key binding was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveKey (Key inKey, Key outKey)
		{
			//if (view == null) {
			//	throw new ArgumentNullException ("View cannot be null.", nameof (view));
			//}
			return RemoveKey (inKey, outKey);
		}

		/// <summary>
		/// Removes a <see cref="KeyBinding"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <returns><c>true</c> if the key binding was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveKey (string view, Key inKey, Key outKey)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null.", nameof (view));
			}
			var kb = new KeyBinding (inKey, outKey);
			var idx = Keys.FindIndex (x => x.View == view && x.InKey == inKey && x.OutKey == outKey);
			if (idx == -1) {
				return false;
			}
			var count = Keys.Count;
			Keys.RemoveAt (idx);
			//if (Keys.FirstOrDefault (x => x.View == view) == null) {
			//	Views.Remove (view);
			//}
			if (count == Keys.Count) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Removes a <see cref="KeyBinding"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="keyBinding">The key binding.</param>
		/// <returns><c>true</c> if the key binding was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveKey (KeyBinding keyBinding)
		{
			return RemoveKey (keyBinding.View, keyBinding.InKey, keyBinding.OutKey);
		}

		/// <summary>
		/// Removes all the <see cref="KeyBinding"/> of <see cref="View"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="view">The view type.</param>
		/// <returns><c>true</c> if all key binding of the related view was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveAll (Type view)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null.", nameof (view));
			}
			return RemoveAll (view.Name);
		}

		/// <summary>
		/// Removes all the <see cref="KeyBinding"/> of <see cref="View"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <returns><c>true</c> if all key binding of the related view was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveAll (string view)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null.", nameof (view));
			}
			var result = Keys.RemoveAll (x => x.View == view) > 0;
			//if (result) {
			//	Views.Remove (view);
			//}
			return result;
		}

		/// <summary>
		/// Removes all the <see cref="KeyBinding"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <returns><c>true</c> if all key binding was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveAll ()
		{
			if (Keys.Count > 0) {
				//Views = new Dictionary<string, bool> ();
				Keys = new List<KeyBinding> ();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Enables or disables the <see cref="KeyBinding.Enabled"/> of the match key binding.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The current output key.</param>
		/// <param name="toReplace">The enabled state to replace.</param>
		/// <returns><c>true</c> if enabled status was successfully sets, <c>false</c>otherwise.</returns>
		//public bool EnableDisableKeyBinding (Key inKey, Key outKey, bool toReplace)
		//{
		//	//if (view == null) {
		//	//	throw new ArgumentNullException ("View cannot be null.", nameof (view));
		//	//}
		//	return EnableDisableKeyBinding (inKey, outKey, toReplace);
		//}

		/// <summary>
		/// Enables or disables the <see cref="KeyBinding.Enabled"/> of the match key binding.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The current output key.</param>
		/// <param name="toReplace">The enabled state to replace.</param>
		/// <returns><c>true</c> if enabled status was successfully sets, <c>false</c>otherwise.</returns>
		public bool EnableDisableKeyBinding (Key inKey, Key outKey, bool toReplace)
		{
			//if (view == null) {
			//	throw new ArgumentNullException ("View cannot be null.", nameof (view));
			//}
			var kb = Keys.FirstOrDefault (x => x.InKey == inKey && x.OutKey == outKey);
			if (kb.Enabled != toReplace) {
				kb.Enabled = toReplace;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Enables or disables the <see cref="KeyBinding.Enabled"/> of the match key binding.
		/// </summary>
		/// <param name="keyBinding">The key binding.</param>
		/// <param name="toReplace">The enabled state to replace.</param>
		/// <returns><c>true</c> if enabled status was successfully sets, <c>false</c>otherwise.</returns>
		public bool EnableDisableKeyBinding (KeyBinding keyBinding, bool toReplace)
		{
			if (keyBinding == null) {
				throw new ArgumentNullException ("KeyBinding cannot be null.", nameof (keyBinding));
			}
			return EnableDisableKeyBinding (keyBinding.InKey, keyBinding.OutKey, toReplace);
		}

		/// <summary>
		/// Replaces the match <see cref="KeyBinding"/> to the new binding sets.
		/// </summary>
		/// <param name="from">The key binding to be replaced.</param>
		/// <param name="to">The key binding being replaced.</param>
		/// <returns><c>true</c> if the key binding was successfully replaced, <c>false</c>otherwise.</returns>
		public bool ReplaceViewKey (KeyBinding from, KeyBinding to)
		{
			if (from == null) {
				throw new ArgumentNullException ("KeyBinding cannot be null.", nameof (from));
			}
			if (to == null) {
				throw new ArgumentNullException ("KeyBinding cannot be null.", nameof (to));
			}
			if (from.View != to.View) {
				throw new InvalidOperationException ("Both views must be the same.");
			}
			if (new KeyBindingEqualityComparer ().ReferenceEquals (from, to)) {
				throw new ArgumentException ("Both KeyBinding are equal.");
			}
			var idxFrom = Keys.FindIndex (x => x.View == from.View && x.InKey == from.InKey && x.OutKey == from.OutKey);
			if (idxFrom == -1) {
				throw new ArgumentException ("KeyBinding not found.", nameof (idxFrom));
			}
			var idxTo = Keys.FindIndex (x => x.View == from.View && (x.InKey == to.InKey || x.OutKey == to.OutKey));
			if (idxTo > -1 && idxTo != idxFrom) {
				throw new ArgumentException ("One of the keys already exists.", nameof (idxTo));
			}
			var kb = Keys [idxFrom];
			kb.InKey = to.InKey;
			kb.OutKey = to.OutKey;
			kb.Description = to.Description;
			kb.Enabled = to.Enabled;
			if (new KeyBindingEqualityComparer ().ReferenceEquals (kb, to)) {
				return true;
			}
			return false;
		}

		/// <summary>
		/// Replaces all keys from the matched view to the destiny view.
		/// </summary>
		/// <param name="fromView">The view type to be replaced.</param>
		/// <param name="toView">The view type being replaced.</param>
		/// <param name="force">Used to delete the destiny if already exists.</param>
		/// <returns><c>true</c> if the view keys was successfully replaced, <c>false</c>otherwise.</returns>
		//public bool ReplaceAllKeysFromView (Type fromView, Type toView, bool force = false)
		//{
		//	if (fromView == null) {
		//		throw new ArgumentNullException ("View cannot be null.", nameof (fromView));
		//	}
		//	if (toView == null) {
		//		throw new ArgumentNullException ("View cannot be null.", nameof (toView));
		//	}
		//	return ReplaceAllKeysFromView (fromView.Name, toView.Name, force);
		//}

		/// <summary>
		/// Replaces all keys from the matched view to the destiny view.
		/// </summary>
		/// <param name="fromView">The view to be replaced.</param>
		/// <param name="toView">The view being replaced.</param>
		/// <param name="force">Used to delete the destiny if already exists.</param>
		/// <returns><c>true</c> if the view keys was successfully replaced, <c>false</c>otherwise.</returns>
		//public bool ReplaceAllKeysFromView (string fromView, string toView, bool force = false)
		//{
		//	if (string.IsNullOrEmpty (fromView)) {
		//		throw new ArgumentNullException ("View cannot be null.", nameof (fromView));
		//	}
		//	if (string.IsNullOrEmpty (toView)) {
		//		throw new ArgumentNullException ("View cannot be null.", nameof (toView));
		//	}
		//	if (fromView == toView) {
		//		throw new ArgumentException ("The source and destiny are the same.", nameof (fromView));
		//	}
		//	if (!Views.ContainsKey (fromView)) {
		//		throw new ArgumentException ("View not found.", nameof (fromView));
		//	}
		//	if (Views.ContainsKey (toView) && !force) {
		//		throw new InvalidOperationException ("Destiny View already exists. Set force = true to delete it before.");
		//	} else if (Views.ContainsKey (toView) && force) {
		//		var enable = Views [toView];
		//		RemoveAll (toView);
		//		Views.Add (toView, enable);
		//	} else {
		//		Views.Add (toView, Views [fromView]);
		//	}
		//	try {
		//		Views.Remove (fromView);
		//		foreach (var kb in Keys.Where (x => x.View == fromView)) {
		//			kb.View = toView;
		//		}
		//	} catch (Exception) {
		//		return false;
		//	}
		//	return true;
		//}

		/// <summary>
		/// Copy the match <paramref name="from"/> <see cref="KeyBinding"/> to the new <paramref name="toView"/>.
		/// </summary>
		/// <param name="from">The key binding to be copied.</param>
		/// <param name="toView">The view type being copied.</param>
		/// <returns><c>true</c> if the view keys was successfully copied, <c>false</c>otherwise.</returns>
		public bool CopyViewKey (KeyBinding from, Type toView)
		{
			if (from == null) {
				throw new ArgumentNullException ("KeyBinding cannot be null.", nameof (from));
			}
			if (toView == null) {
				throw new ArgumentNullException ("KeyBinding cannot be null.", nameof (toView));
			}
			return CopyViewKey (from, toView.Name);
		}

		/// <summary>
		/// Copy the match <paramref name="from"/> <see cref="KeyBinding"/> to the new <paramref name="toView"/>.
		/// </summary>
		/// <param name="from">The key binding to be copied.</param>
		/// <param name="toView">The view being copied.</param>
		/// <returns><c>true</c> if the view keys was successfully copied, <c>false</c>otherwise.</returns>
		public bool CopyViewKey (KeyBinding from, string toView)
		{
			if (from == null) {
				throw new ArgumentNullException ("KeyBinding cannot be null.", nameof (from));
			}
			if (toView == null) {
				throw new ArgumentNullException ("KeyBinding cannot be null.", nameof (toView));
			}
			if (from.View == toView) {
				throw new InvalidOperationException ("Both views cannot be the same.");
			}
			if (Keys.Find (x => x.View == from.View && x.InKey == from.InKey && x.OutKey == from.OutKey) == default) {
				throw new ArgumentException ("KeyBinding not found.", nameof (from));
			}
			var to = (KeyBinding)from.Clone ();
			to.View = toView;
			return AddKey (to);
		}

		/// <summary>
		/// Copy all keys from the matched view to the destiny view.
		/// </summary>
		/// <param name="fromView">The view type to be copied.</param>
		/// <param name="toView">The view type being copied.</param>
		/// <param name="force">Used to delete the destiny if already exists.</param>
		/// <returns><c>true</c> if the view keys was successfully copied, <c>false</c>otherwise.</returns>
		//public bool CopyAllKeysFromView (Type fromView, Type toView, bool force = false)
		//{
		//	if (fromView == null) {
		//		throw new ArgumentNullException ("View cannot be null.", nameof (fromView));
		//	}
		//	if (toView == null) {
		//		throw new ArgumentNullException ("View cannot be null.", nameof (toView));
		//	}
		//	return CopyAllKeysFromView (fromView.Name, toView.Name, force);
		//}

		/// <summary>
		/// Copy all keys from the matched view to the destiny view.
		/// </summary>
		/// <param name="fromView">The view to be copied.</param>
		/// <param name="toView">The view being copied.</param>
		/// <param name="force">Used to delete the destiny if already exists.</param>
		/// <returns><c>true</c> if the view keys was successfully copied, <c>false</c>otherwise.</returns>
		//public bool CopyAllKeysFromView (string fromView, string toView, bool force = false)
		//{
		//	if (string.IsNullOrEmpty (fromView)) {
		//		throw new ArgumentNullException ("View cannot be null.", nameof (fromView));
		//	}
		//	if (string.IsNullOrEmpty (toView)) {
		//		throw new ArgumentNullException ("View cannot be null.", nameof (toView));
		//	}
		//	if (fromView == toView) {
		//		throw new ArgumentException ("The source and destiny are the same.", nameof (fromView));
		//	}
		//	if (!Views.ContainsKey (fromView)) {
		//		throw new ArgumentException ("View not found.", nameof (fromView));
		//	}
		//	if (Views.ContainsKey (toView) && !force) {
		//		throw new InvalidOperationException ("Destiny View already exists. Set force = true to delete it before.");
		//	} else if (Views.ContainsKey (toView) && force) {
		//		RemoveAll (toView);
		//		Views.Add (toView, Views [fromView]);
		//	} else {
		//		Views.Add (toView, Views [fromView]);
		//	}
		//	try {
		//		var keys = new List<KeyBinding> ();
		//		foreach (var kb in Keys.Where (x => x.View == fromView)) {
		//			KeyBinding newKb = (KeyBinding)kb.Clone ();
		//			newKb.View = toView;
		//			keys.Add (newKb);
		//		}
		//		Keys.AddRange (keys);
		//	} catch (Exception) {
		//		return false;
		//	}
		//	return true;
		//}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString ()
		{
			return $"Views({Count})Keys({Keys.Count})";
		}
	}
}
