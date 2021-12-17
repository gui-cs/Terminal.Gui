using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	public sealed class KeyBinding {
		public KeyBinding (Type view, Key inKey, Key outKey, string description = "", bool enabled = true)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null", nameof (view));
			}
			View = view.Name;
			InKey = inKey;
			OutKey = outKey;
			Description = description;
			Enabled = enabled;
		}

		public string View { get; }
		public Key InKey { get; }
		public Key OutKey { get; }
		public string Description { get; }
		public bool Enabled { get; set; } = true;
	}

	public sealed class KeyBindings {
		private class KeyBindingEqualityComparer : IEqualityComparer<KeyBinding> {
			public bool Equals (KeyBinding x, KeyBinding y)
			{
				if (x.View == y.View && (x.InKey == y.InKey || x.OutKey == y.OutKey))
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

		//private class ViewBindingStatus {
		//	public ViewBindingStatus (string view, bool enabled)
		//	{
		//		View = view;
		//		Enabled = enabled;
		//	}

		//	public string View { get; set; }
		//	public bool Enabled { get; set; } = true;
		//}

		public KeyBindings ()
		{
		}

		public KeyBindings (Type view, Key inKey, Key outKey, string description = "", bool enabled = true)
		{
			AddKey (view, inKey, outKey, description, enabled);
		}

		public KeyBindings (KeyBinding keyBinding)
		{
			AddKey (keyBinding);
		}

		public List<(string View, bool Enabled)> Views { get; private set; } = new List<(string View, bool Enabled)> ();

		public List<KeyBinding> Keys { get; private set; } = new List<KeyBinding> ();

		public bool Enabled { get; set; } = false;

		public Key EnableKey { get; set; } = Key.Enter;

		public Key DisableKey { get; set; } = Key.Esc;

		public int Count => Views.Count;

		public bool AddKey (Type view, Key inKey, Key outKey, string description = "", bool enabled = true)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null", nameof (view));
			}
			var kb = new KeyBinding (view, inKey, outKey, description, enabled);
			var viewName = view.Name;
			if (!Views.Any (x => x.View == viewName)) {
				Views.Add ((viewName, true));
			}
			if (Keys.Contains (kb, new KeyBindingEqualityComparer ())) {
				throw new ArgumentException ("One of the keys already exists.", nameof (view));
			}
			Keys.Add (kb);
			return true;
		}

		public bool AddKey (KeyBinding keyBinding)
		{
			Type view = GetInstance (keyBinding.View).GetType ();
			return AddKey (view, keyBinding.InKey, keyBinding.OutKey, keyBinding.Description, keyBinding.Enabled);
		}

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
				Views.Remove (Views.FirstOrDefault (x => x.View == viewName));
			}
			if (count == Keys.Count) {
				return false;
			}
			return true;
		}

		public bool RemoveKey (string view, Key inKey, Key outKey)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null", nameof (view));
			}
			Type v = GetInstance (view).GetType ();
			return RemoveKey (v, inKey, outKey);
		}

		public bool RemoveKey (KeyBinding keyBinding)
		{
			Type view = GetInstance (keyBinding.View).GetType ();
			return RemoveKey (view, keyBinding.InKey, keyBinding.OutKey);
		}

		public bool RemoveAll (Type view)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null", nameof (view));
			}
			var viewName = view.Name;
			var result = Keys.RemoveAll (x => x.View == viewName) > 0;
			if (result) {
				Views.Remove (Views.FirstOrDefault (x => x.View == viewName));
			}
			return result;
		}

		public bool RemoveAll ()
		{
			if (Views.Count > 0) {
				Views = new List<(string View, bool Enabled)> ();
				Keys = new List<KeyBinding> ();
				return true;
			}
			return false;
		}

		public bool EnableDisableKeyBinding (Type view, Key inKey, Key outKey, bool toReplace)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null", nameof (view));
			}
			var viewName = view.Name;
			var kb = Keys.FirstOrDefault (x => x.View == viewName && x.InKey == inKey && x.OutKey == outKey);
			if (kb.Enabled != toReplace) {
				kb.Enabled = toReplace;
				return true;
			}
			return false;
		}

		public bool EnableDisableKeyBinding (string view, Key inKey, Key outKey, bool toReplace)
		{
			if (view == null) {
				throw new ArgumentNullException ("View cannot be null", nameof (view));
			}
			Type v = GetInstance (view).GetType ();
			return EnableDisableKeyBinding (v, inKey, outKey, toReplace);
		}

		public bool EnableDisableKeyBinding (KeyBinding keyBinding, bool toReplace)
		{
			if (keyBinding == null) {
				throw new ArgumentNullException ("KeyBinding cannot be null", nameof (keyBinding));
			}
			Type view = GetInstance (keyBinding.View).GetType ();
			return EnableDisableKeyBinding (view, keyBinding.InKey, keyBinding.OutKey, toReplace);
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
