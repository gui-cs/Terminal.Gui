﻿using System.Diagnostics;

namespace Terminal.Gui;

// TODO: Nuke when #2975 is completed
/// <summary>Represents a helper to manipulate shortcut keys used on views.</summary>
public class ShortcutHelper
{
    // TODO: Update this to use Key, not KeyCode
    private KeyCode shortcut;

    /// <summary>This is the global setting that can be used as a global shortcut to invoke the action on the view.</summary>
    public virtual KeyCode Shortcut
    {
        get => shortcut;
        set
        {
            if (shortcut != value && (PostShortcutValidation (value) || value is KeyCode.Null))
            {
                shortcut = value;
            }
        }
    }

    /// <summary>The keystroke combination used in the <see cref="Shortcut"/> as string.</summary>
    public virtual string ShortcutTag => Key.ToString (shortcut, MenuBar.ShortcutDelimiter);

    /// <summary>Lookup for a <see cref="KeyCode"/> on range of keys.</summary>
    /// <param name="key">The source key.</param>
    /// <param name="first">First key in range.</param>
    /// <param name="last">Last key in range.</param>
    public static bool CheckKeysFlagRange (KeyCode key, KeyCode first, KeyCode last)
    {
        for (var i = (uint)first; i < (uint)last; i++)
        {
            if ((key | (KeyCode)i) == key)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Allows to retrieve a <see cref="KeyCode"/> from a <see cref="ShortcutTag"/></summary>
    /// <param name="tag">The key as string.</param>
    /// <param name="delimiter">The delimiter string.</param>
    public static KeyCode GetShortcutFromTag (string tag, Rune delimiter = default)
    {
        string sCut = tag;

        if (string.IsNullOrEmpty (sCut))
        {
            return default (KeyCode);
        }

        var key = KeyCode.Null;

        //var hasCtrl = false;
        if (delimiter == default (Rune))
        {
            delimiter = MenuBar.ShortcutDelimiter;
        }

        string [] keys = sCut.Split (delimiter.ToString ());

        for (var i = 0; i < keys.Length; i++)
        {
            string k = keys [i];

            if (k == "Ctrl")
            {
                //hasCtrl = true;
                key |= KeyCode.CtrlMask;
            }
            else if (k == "Shift")
            {
                key |= KeyCode.ShiftMask;
            }
            else if (k == "Alt")
            {
                key |= KeyCode.AltMask;
            }
            else if (k.StartsWith ("F") && k.Length > 1)
            {
                int.TryParse (k.Substring (1), out int n);

                for (var j = (uint)KeyCode.F1; j <= (uint)KeyCode.F12; j++)
                {
                    int.TryParse (((KeyCode)j).ToString ().Substring (1), out int f);

                    if (f == n)
                    {
                        key |= (KeyCode)j;
                    }
                }
            }
            else
            {
                key |= (KeyCode)Enum.Parse (typeof (KeyCode), k);
            }
        }

        return key;
    }

    /// <summary>Used at key up validation.</summary>
    /// <param name="key">The key to validate.</param>
    /// <returns><c>true</c> if is valid.<c>false</c>otherwise.</returns>
    public static bool PostShortcutValidation (KeyCode key)
    {
        GetKeyToString (key, out KeyCode knm);

        if (CheckKeysFlagRange (key, KeyCode.F1, KeyCode.F12) || ((key & (KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask)) != 0 && knm != KeyCode.Null))
        {
            return true;
        }

        return false;
    }

    /// <summary>Used at key down or key press validation.</summary>
    /// <param name="key">The key to validate.</param>
    /// <returns><c>true</c> if is valid.<c>false</c>otherwise.</returns>
    public static bool PreShortcutValidation (KeyCode key)
    {
        if ((key & (KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask)) == 0
            && !CheckKeysFlagRange (key, KeyCode.F1, KeyCode.F12))
        {
            return false;
        }

        return true;
    }

    /// <summary>Return key as string.</summary>
    /// <param name="key">The key to extract.</param>
    /// <param name="knm">Correspond to the non modifier key.</param>
    private static string GetKeyToString (KeyCode key, out KeyCode knm)
    {
        if (key == KeyCode.Null)
        {
            knm = KeyCode.Null;

            return "";
        }

        knm = key;
        KeyCode mK = key & (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.ShiftMask);
        knm &= ~mK;

        for (var i = (uint)KeyCode.F1; i < (uint)KeyCode.F12; i++)
        {
            if (knm == (KeyCode)i)
            {
                mK |= (KeyCode)i;
            }
        }

        knm &= ~mK;
        uint.TryParse (knm.ToString (), out uint c);
        string s = mK == KeyCode.Null ? "" : mK.ToString ();

        if (s != "" && (knm != KeyCode.Null || c > 0))
        {
            s += ",";
        }

        s += c == 0 ? knm == KeyCode.Null ? "" : knm.ToString () : ((char)c).ToString ();

        return s;
    }
}
