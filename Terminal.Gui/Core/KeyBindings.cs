using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Class that implement the keys used by the <see cref="KeyBindings"/> for any <see cref="View"/>.
	/// </summary>
	public sealed class KeyBinding {
		/// <summary>
		/// Initializes a new key binding for a view. If <see cref="View"/> is used will applied to all views.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <param name="description">The description.</param>
		/// <param name="enabled"><c>true</c> if enabled, <c>false</c>otherwise.</param>
		public KeyBinding (Type view, Key inKey, Key outKey, string description = "", bool enabled = true)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null.", nameof (view));
			}
			View = view.Name;
			InKey = inKey;
			OutKey = outKey;
			Description = description;
			Enabled = enabled;
		}

		/// <summary>
		/// Get the view name.
		/// </summary>
		public string View { get; }
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

	}

	/// <summary>
	/// Manage all the <see cref="KeyBinding"/> collection used by the <see cref="Application.KeyBindings"/>
	/// </summary>
	public sealed class KeyBindings {
		private class KeyBindingEqualityComparer : IEqualityComparer<KeyBinding> {
			public bool Equals (KeyBinding x, KeyBinding y)
			{
				if (x.View == y.View && (x.InKey == y.InKey || x.OutKey == y.OutKey))
					return true;
				return false;
			}

			public bool ReferenceEquals (KeyBinding x, KeyBinding y)
			{
				if (x.View == y.View && x.InKey == y.InKey && x.OutKey == y.OutKey && x.Description == y.Description && x.Enabled == y.Enabled)
					return true;
				return false;
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
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <param name="description">The description.</param>
		/// <param name="enabled"><c>true</c> if enabled, <c>false</c>otherwise.</param>
		public KeyBindings (Type view, Key inKey, Key outKey, string description = "", bool enabled = true)
		{
			AddKey (view, inKey, outKey, description, enabled);
		}

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
		public Dictionary<string, bool> Views { get; private set; } = new Dictionary<string, bool> ();

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

		/// <summary>
		/// Gets the total of views with key bindings.
		/// </summary>
		public int Count => Views.Count;

		/// <summary>
		/// Add a new <see cref="KeyBinding"/> to the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <param name="description">The description.</param>
		/// <param name="enabled"><c>true</c> if enabled, <c>false</c>otherwise.</param>
		/// <returns><c>true</c> if the key binding was successfully added, <c>false</c>otherwise.</returns>
		public bool AddKey (Type view, Key inKey, Key outKey, string description = "", bool enabled = true)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null.", nameof (view));
			}
			var kb = new KeyBinding (view, inKey, outKey, description, enabled);
			var viewName = view.Name;
			if (!Views.Keys.Contains (viewName)) {
				Views.Add (viewName, true);
			}
			if (Keys.Contains (kb, new KeyBindingEqualityComparer ())) {
				throw new ArgumentException ("One of the keys already exists.", nameof (view));
			}
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
			Type view = GetInstance (keyBinding.View).GetType ();
			return AddKey (view, keyBinding.InKey, keyBinding.OutKey, keyBinding.Description, keyBinding.Enabled);
		}

		/// <summary>
		/// Removes a <see cref="KeyBinding"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="inKey">The input key pressed.</param>
		/// <param name="outKey">The desired output key.</param>
		/// <returns><c>true</c> if the key binding was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveKey (Type view, Key inKey, Key outKey)
		{
			var kb = new KeyBinding (view, inKey, outKey);
			var viewName = view.Name;
			var idx = Keys.FindIndex (x => x.View == viewName && x.InKey == inKey && x.OutKey == outKey);
			if (idx == -1) {
				return false;
			}
			var count = Keys.Count;
			Keys.RemoveAt (idx);
			if (Keys.FirstOrDefault (x => x.View == viewName) == null) {
				Views.Remove (viewName);
			}
			if (count == Keys.Count) {
				return false;
			}
			return true;
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
			Type v = GetInstance (view).GetType ();
			return RemoveKey (v, inKey, outKey);
		}

		/// <summary>
		/// Removes a <see cref="KeyBinding"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="keyBinding">The key binding.</param>
		/// <returns><c>true</c> if the key binding was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveKey (KeyBinding keyBinding)
		{
			Type view = GetInstance (keyBinding.View).GetType ();
			return RemoveKey (view, keyBinding.InKey, keyBinding.OutKey);
		}

		/// <summary>
		/// Removes all the <see cref="KeyBinding"/> of <see cref="View"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <returns><c>true</c> if all key binding of the related view was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveAll (Type view)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null.", nameof (view));
			}
			var viewName = view.Name;
			var result = Keys.RemoveAll (x => x.View == viewName) > 0;
			if (result) {
				Views.Remove (viewName);
			}
			return result;
		}

		/// <summary>
		/// Removes all the <see cref="KeyBinding"/> from the <see cref="Keys"/> collection.
		/// </summary>
		/// <returns><c>true</c> if all key binding was successfully removed, <c>false</c>otherwise.</returns>
		public bool RemoveAll ()
		{
			if (Views.Count > 0) {
				Views = new Dictionary<string, bool> ();
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
		public bool EnableDisableKeyBinding (Type view, Key inKey, Key outKey, bool toReplace)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null.", nameof (view));
			}
			var viewName = view.Name;
			var kb = Keys.FirstOrDefault (x => x.View == viewName && x.InKey == inKey && x.OutKey == outKey);
			if (kb.Enabled != toReplace) {
				kb.Enabled = toReplace;
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
		public bool EnableDisableKeyBinding (string view, Key inKey, Key outKey, bool toReplace)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null.", nameof (view));
			}
			Type v = GetInstance (view).GetType ();
			return EnableDisableKeyBinding (v, inKey, outKey, toReplace);
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
			Type view = GetInstance (keyBinding.View).GetType ();
			return EnableDisableKeyBinding (view, keyBinding.InKey, keyBinding.OutKey, toReplace);
		}

		/// <summary>
		/// Replaces the match <see cref="KeyBinding"/> to the new binding sets.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
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
			var viewName = GetInstance (from.View).GetType ().Name;
			var idxFrom = Keys.FindIndex (x => x.View == viewName && x.InKey == from.InKey && x.OutKey == from.OutKey);
			if (idxFrom == -1) {
				throw new ArgumentException ("KeyBinding not found.", nameof (idxFrom));
			}
			var idxTo = Keys.FindIndex (x => x.View == viewName && (x.InKey == to.InKey || x.OutKey == to.OutKey));
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

		private View GetInstance (string strFullyQualifiedName)
		{
			foreach (Type type in typeof (View).Assembly.GetTypes ()
				.Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsPublic && myType.IsSubclassOf (typeof (View)))) {
				if (type != null && type.Name == strFullyQualifiedName)
					return (View)Activator.CreateInstance (type);
			}
			return null;
		}
	}
}
