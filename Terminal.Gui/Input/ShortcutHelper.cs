using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Terminal.Gui;
/// <summary>
/// Represents a helper to manipulate shortcut keys used on views.
/// </summary>
public class ShortcutHelper {
	private ConsoleDriverKey shortcut;

	/// <summary>
	/// This is the global setting that can be used as a global shortcut to invoke the action on the view.
	/// </summary>
	public virtual ConsoleDriverKey Shortcut {
		get => shortcut;
		set {
			if (shortcut != value && (PostShortcutValidation (value) || value == ConsoleDriverKey.Null)) {
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
	static string GetKeyToString (ConsoleDriverKey key, out ConsoleDriverKey knm)
	{
		if (key == ConsoleDriverKey.Null) {
			knm = ConsoleDriverKey.Null;
			return "";
		}

		knm = key;
		var mK = key & (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.ShiftMask);
		knm &= ~mK;
		for (uint i = (uint)ConsoleDriverKey.F1; i < (uint)ConsoleDriverKey.F12; i++) {
			if (knm == (ConsoleDriverKey)i) {
				mK |= (ConsoleDriverKey)i;
			}
		}
		knm &= ~mK;
		uint.TryParse (knm.ToString (), out uint c);
		var s = mK == ConsoleDriverKey.Null ? "" : mK.ToString ();
		if (s != "" && (knm != ConsoleDriverKey.Null || c > 0)) {
			s += ",";
		}
		s += c == 0 ? knm == ConsoleDriverKey.Null ? "" : knm.ToString () : ((char)c).ToString ();
		return s;
	}

	/// <summary>
	/// Allows to retrieve a <see cref="ConsoleDriverKey"/> from a <see cref="ShortcutTag"/>
	/// </summary>
	/// <param name="tag">The key as string.</param>
	/// <param name="delimiter">The delimiter string.</param>
	public static ConsoleDriverKey GetShortcutFromTag (string tag, Rune delimiter = default)
	{
		var sCut = tag;
		if (string.IsNullOrEmpty (sCut)) {
			return default;
		}

		ConsoleDriverKey key = ConsoleDriverKey.Null;
		//var hasCtrl = false;
		if (delimiter == default) {
			delimiter = MenuBar.ShortcutDelimiter;
		}

		string [] keys = sCut.Split (delimiter.ToString());
		for (int i = 0; i < keys.Length; i++) {
			var k = keys [i];
			if (k == "Ctrl") {
				//hasCtrl = true;
				key |= ConsoleDriverKey.CtrlMask;
			} else if (k == "Shift") {
				key |= ConsoleDriverKey.ShiftMask;
			} else if (k == "Alt") {
				key |= ConsoleDriverKey.AltMask;
			} else if (k.StartsWith ("F") && k.Length > 1) {
				int.TryParse (k.Substring (1).ToString (), out int n);
				for (uint j = (uint)ConsoleDriverKey.F1; j <= (uint)ConsoleDriverKey.F12; j++) {
					int.TryParse (((ConsoleDriverKey)j).ToString ().Substring (1), out int f);
					if (f == n) {
						key |= (ConsoleDriverKey)j;
					}
				}
			} else {
				key |= (ConsoleDriverKey)Enum.Parse (typeof (ConsoleDriverKey), k.ToString ());
			}
		}

		return key;
	}

	/// <summary>
	/// Lookup for a <see cref="ConsoleDriverKey"/> on range of keys.
	/// </summary>
	/// <param name="key">The source key.</param>
	/// <param name="first">First key in range.</param>
	/// <param name="last">Last key in range.</param>
	public static bool CheckKeysFlagRange (ConsoleDriverKey key, ConsoleDriverKey first, ConsoleDriverKey last)
	{
		for (uint i = (uint)first; i < (uint)last; i++) {
			if ((key | (ConsoleDriverKey)i) == key) {
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
	public static bool PreShortcutValidation (ConsoleDriverKey key)
	{
		if ((key & (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask)) == 0 && !CheckKeysFlagRange (key, ConsoleDriverKey.F1, ConsoleDriverKey.F12)) {
			return false;
		}

		return true;
	}

	/// <summary>
	/// Used at key up validation.
	/// </summary>
	/// <param name="key">The key to validate.</param>
	/// <returns><c>true</c> if is valid.<c>false</c>otherwise.</returns>
	public static bool PostShortcutValidation (ConsoleDriverKey key)
	{
		GetKeyToString (key, out ConsoleDriverKey knm);

		if (CheckKeysFlagRange (key, ConsoleDriverKey.F1, ConsoleDriverKey.F12) ||
			((key & (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask)) != 0 && knm != ConsoleDriverKey.Null && knm != ConsoleDriverKey.Unknown)) {
			return true;
		}
		Debug.WriteLine ($"WARNING: {KeyEventArgs.ToString (key)} is not a valid shortcut key.");
		return false;
	}
}

