using System;
using System.Linq;
using System.Text;

namespace Terminal.Gui;
/// <summary>
/// Represents a helper to manipulate shortcut keys used on views.
/// </summary>
public class ShortcutHelper {
	private Key shortcut;

	/// <summary>
	/// This is the global setting that can be used as a global shortcut to invoke the action on the view.
	/// </summary>
	public virtual Key Shortcut {
		get => shortcut;
		set {
			if (shortcut != value && (PostShortcutValidation (value) || value == Key.Null)) {
				shortcut = value;
			}
		}
	}

	/// <summary>
	/// The keystroke combination used in the <see cref="Shortcut"/> as string.
	/// </summary>
	public virtual string ShortcutTag => KeyEventArgs.ToString (shortcut, MenuBar.ShortcutDelimiter);
	
	/// <summary>
	/// Return key as string.
	/// </summary>
	/// <param name="key">The key to extract.</param>
	/// <param name="knm">Correspond to the non modifier key.</param>
	static string GetKeyToString (Key key, out Key knm)
	{
		if (key == Key.Null) {
			knm = Key.Null;
			return "";
		}

		knm = key;
		var mK = key & (Key.AltMask | Key.CtrlMask | Key.ShiftMask);
		knm &= ~mK;
		for (uint i = (uint)Key.F1; i < (uint)Key.F12; i++) {
			if (knm == (Key)i) {
				mK |= (Key)i;
			}
		}
		knm &= ~mK;
		uint.TryParse (knm.ToString (), out uint c);
		var s = mK == Key.Null ? "" : mK.ToString ();
		if (s != "" && (knm != Key.Null || c > 0)) {
			s += ",";
		}
		s += c == 0 ? knm == Key.Null ? "" : knm.ToString () : ((char)c).ToString ();
		return s;
	}

	/// <summary>
	/// Allows to retrieve a <see cref="Key"/> from a <see cref="ShortcutTag"/>
	/// </summary>
	/// <param name="tag">The key as string.</param>
	/// <param name="delimiter">The delimiter string.</param>
	public static Key GetShortcutFromTag (string tag, Rune delimiter = default)
	{
		var sCut = tag;
		if (string.IsNullOrEmpty (sCut)) {
			return default;
		}

		Key key = Key.Null;
		//var hasCtrl = false;
		if (delimiter == default) {
			delimiter = MenuBar.ShortcutDelimiter;
		}

		string [] keys = sCut.Split (delimiter.ToString());
		for (int i = 0; i < keys.Length; i++) {
			var k = keys [i];
			if (k == "Ctrl") {
				//hasCtrl = true;
				key |= Key.CtrlMask;
			} else if (k == "Shift") {
				key |= Key.ShiftMask;
			} else if (k == "Alt") {
				key |= Key.AltMask;
			} else if (k.StartsWith ("F") && k.Length > 1) {
				int.TryParse (k.Substring (1).ToString (), out int n);
				for (uint j = (uint)Key.F1; j <= (uint)Key.F12; j++) {
					int.TryParse (((Key)j).ToString ().Substring (1), out int f);
					if (f == n) {
						key |= (Key)j;
					}
				}
			} else {
				key |= (Key)Enum.Parse (typeof (Key), k.ToString ());
			}
		}

		return key;
	}

	/// <summary>
	/// Lookup for a <see cref="Key"/> on range of keys.
	/// </summary>
	/// <param name="key">The source key.</param>
	/// <param name="first">First key in range.</param>
	/// <param name="last">Last key in range.</param>
	public static bool CheckKeysFlagRange (Key key, Key first, Key last)
	{
		for (uint i = (uint)first; i < (uint)last; i++) {
			if ((key | (Key)i) == key) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Used at key down or key press validation.
	/// </summary>
	/// <param name="key">The key to validate.</param>
	/// <returns><c>true</c> if is valid.<c>false</c>otherwise.</returns>
	public static bool PreShortcutValidation (Key key)
	{
		if ((key & (Key.CtrlMask | Key.ShiftMask | Key.AltMask)) == 0 && !CheckKeysFlagRange (key, Key.F1, Key.F12)) {
			return false;
		}

		return true;
	}

	/// <summary>
	/// Used at key up validation.
	/// </summary>
	/// <param name="key">The key to validate.</param>
	/// <returns><c>true</c> if is valid.<c>false</c>otherwise.</returns>
	public static bool PostShortcutValidation (Key key)
	{
		GetKeyToString (key, out Key knm);

		if (CheckKeysFlagRange (key, Key.F1, Key.F12) ||
			((key & (Key.CtrlMask | Key.ShiftMask | Key.AltMask)) != 0 && knm != Key.Null && knm != Key.Unknown)) {
			return true;
		}
		return false;
	}
}

