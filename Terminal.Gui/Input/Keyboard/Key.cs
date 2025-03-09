#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Terminal.Gui;

/// <summary>
///     Provides an abstraction for common keyboard operations and state. Used for processing keyboard input and
///     raising keyboard events.
/// </summary>
/// <remarks>
///     <para>
///         This class provides a high-level abstraction with helper methods and properties for common keyboard
///         operations. Use this class instead of the <see cref="Terminal.Gui.KeyCode"/> enumeration for keyboard input
///         whenever possible.
///     </para>
///     <para></para>
///     <para>
///         The default value for <see cref="Key"/> is <see cref="KeyCode.Null"/> and can be tested using
///         <see cref="Key.Empty"/>.
///     </para>
///     <para>
///         <list type="table">
///             <listheader>
///                 <term>Concept</term><description>Definition</description>
///             </listheader>
///             <item>
///                 <term>Testing Shift State</term>
///                 <description>
///                     The <c>Is</c> properties (<see cref="IsShift"/>,<see cref="IsCtrl"/>, <see cref="IsAlt"/>)
///                     test for shift state; whether the key press was modified by a shift key.
///                 </description>
///             </item>
///             <item>
///                 <term>Adding Shift State</term>
///                 <description>
///                     The <c>With</c> properties (<see cref="WithShift"/>,<see cref="WithCtrl"/>,
///                     <see cref="WithAlt"/>) return a copy of the Key with the shift modifier applied. This is useful for
///                     specifying a key that requires a shift modifier (e.g.
///                     <c>var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;</c>.
///                 </description>
///             </item>
///             <item>
///                 <term>Removing Shift State</term>
///                 <description>
///                     The <c>No</c> properties (<see cref="NoShift"/>,<see cref="NoCtrl"/>, <see cref="NoAlt"/>)
///                     return a copy of the Key with the shift modifier removed. This is useful for specifying a key that
///                     does not require a shift modifier (e.g. <c>var ControlDelete = ControlAltDelete.NoCtrl;</c>.
///                 </description>
///             </item>
///             <item>
///                 <term>Encoding of A..Z</term>
///                 <description>
///                     Lowercase alpha keys are encoded (in <see cref="Key.KeyCode"/>) as values between 65 and
///                     90 corresponding to the un-shifted A to Z keys on a keyboard. Properties are provided for these
///                     (e.g. <see cref="Key.A"/>, <see cref="Key.B"/>, etc.). Even though the encoded values are the same
///                     as the ASCII values for uppercase characters, these enum values represent *lowercase*, un-shifted
///                     characters.
///                 </description>
///             </item>
///             <item>
///                 <term>Persistence as strings</term>
///                 <description>
///                     Keys are persisted as <c>"[Modifiers]+[Key]</c>. For example
///                     <c>new Key(Key.Delete).WithAlt.WithDel</c> is persisted as <c>"Ctrl+Alt+Delete"</c>. See
///                     <see cref="ToString()"/> and <see cref="TryParse(string, out Terminal.Gui.Key)"/> for more
///                     information.
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public class Key : EventArgs, IEquatable<Key>
{
    /// <summary>Constructs a new <see cref="Key"/></summary>
    public Key () : this (KeyCode.Null) { }

    /// <summary>Constructs a new <see cref="Key"/> from the provided Key value</summary>
    /// <param name="k">The key</param>
    public Key (KeyCode k) { KeyCode = k; }

    /// <summary>
    ///     Copy constructor.
    /// </summary>
    /// <param name="key">The Key to copy</param>
    public Key (Key key)
    {
        KeyCode = key.KeyCode;
        Handled = key.Handled;
    }

    /// <summary>Constructs a new <see cref="Key"/> from a char.</summary>
    /// <remarks>
    ///     <para>
    ///         The key codes for the A..Z keys are encoded as values between 65 and 90 (<see cref="KeyCode.A"/> through
    ///         <see cref="KeyCode.Z"/>). While these are the same as the ASCII values for uppercase characters, they represent
    ///         *keys*, not characters. Therefore, this constructor will store 'A'..'Z' as <see cref="KeyCode.A"/>..
    ///         <see cref="KeyCode.Z"/> with the <see cref="KeyCode.ShiftMask"/> set and will store `a`..`z` as
    ///         <see cref="KeyCode.A"/>..<see cref="KeyCode.Z"/>.
    ///     </para>
    /// </remarks>
    /// <param name="ch"></param>
    public Key (char ch)
    {
        switch (ch)
        {
            case >= 'A' and <= 'Z':
                // Upper case A..Z mean "Shift-char" so we need to add Shift
                KeyCode = (KeyCode)ch | KeyCode.ShiftMask;

                break;
            case >= 'a' and <= 'z':
                // Lower case a..z mean no shift, so we need to store as Key.A...Key.Z
                KeyCode = (KeyCode)(ch - 32);

                return;
            default:
                KeyCode = (KeyCode)ch;

                break;
        }
    }

    /// <summary>
    ///     Constructs a new Key from a string describing the key. See
    ///     <see cref="TryParse(string, out Terminal.Gui.Key)"/> for information on the format of the string.
    /// </summary>
    /// <param name="str">The string describing the key.</param>
    public Key (string str)
    {
        bool result = TryParse (str, out Key key);

        if (!result)
        {
            throw new ArgumentException (@$"Invalid key string: {str}", nameof (str));
        }

        KeyCode = key.KeyCode;
    }

    /// <summary>
    ///     The key value as a Rune. This is the actual value of the key pressed, and is independent of the modifiers.
    ///     Useful for determining if a key represents is a printable character.
    /// </summary>
    /// <remarks>
    ///     <para>Keys with Ctrl or Alt modifiers will return <see langword="default"/>.</para>
    ///     <para>
    ///         If the key is a letter key (A-Z), the Rune will be the upper or lower case letter depending on whether
    ///         <see cref="KeyCode.ShiftMask"/> is set.
    ///     </para>
    ///     <para>
    ///         If the key is outside of the <see cref="KeyCode.CharMask"/> range, the returned Rune will be
    ///         <see langword="default"/>.
    ///     </para>
    /// </remarks>
    public Rune AsRune => ToRune (KeyCode);

    /// <summary>
    ///     Indicates if the current Key event has already been processed and the driver should stop notifying any other
    ///     event subscriber. It's important to set this value to true specially when updating any View's layout from inside
    ///     the
    ///     subscriber method.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>Gets a value indicating whether the Alt key was pressed (real or synthesized)</summary>
    /// <value><see langword="true"/> if is alternate; otherwise, <see langword="false"/>.</value>
    public bool IsAlt => (KeyCode & KeyCode.AltMask) != 0;

    /// <summary>Gets a value indicating whether the Ctrl key was pressed.</summary>
    /// <value><see langword="true"/> if is ctrl; otherwise, <see langword="false"/>.</value>
    public bool IsCtrl => (KeyCode & KeyCode.CtrlMask) != 0;

    /// <summary>
    ///     Gets a value indicating whether the key represents a key in the range of <see cref="KeyCode.A"/> to
    ///     <see cref="KeyCode.Z"/>, regardless of the <see cref="KeyCode.ShiftMask"/>. This is useful for testing if a key is
    ///     based on these keys which are special cased.
    /// </summary>
    /// <remarks>
    ///     IMPORTANT: Lowercase alpha keys are encoded in <see cref="Key.KeyCode"/> as values between 65 and 90
    ///     corresponding to the un-shifted A to Z keys on a keyboard. Helper properties are provided these (e.g.
    ///     <see cref="Key.A"/>, <see cref="Key.B"/>, etc.). Even though the values are the same as the ASCII values for
    ///     uppercase characters, these enum values represent *lowercase*, un-shifted characters.
    /// </remarks>
    public bool IsKeyCodeAtoZ => GetIsKeyCodeAtoZ (KeyCode);

    /// <summary>Gets a value indicating whether the Shift key was pressed.</summary>
    /// <value><see langword="true"/> if is shift; otherwise, <see langword="false"/>.</value>
    public bool IsShift => (KeyCode & KeyCode.ShiftMask) != 0;

    /// <summary>
    ///     Indicates whether the <see cref="Key"/> is valid or not. Invalid keys are <see cref="Key.Empty"/>, and keys
    ///     with only shift modifiers.
    /// </summary>
    public bool IsValid => this != Empty && NoAlt.NoShift.NoCtrl != Empty;

    private readonly KeyCode _keyCode;

    /// <summary>The encoded key value.</summary>
    /// <para>
    ///     IMPORTANT: Lowercase alpha keys are encoded (in <see cref="Gui.KeyCode"/>) as values between 65 and 90
    ///     corresponding to the un-shifted A to Z keys on a keyboard. Enum values are provided for these (e.g.
    ///     <see cref="KeyCode.A"/>, <see cref="KeyCode.B"/>, etc.). Even though the values are the same as the ASCII values
    ///     for uppercase characters, these enum values represent *lowercase*, un-shifted characters.
    /// </para>
    /// <remarks>This property is the backing data for the <see cref="Key"/>. It is a <see cref="KeyCode"/> enum value.</remarks>
    public KeyCode KeyCode
    {
        get => _keyCode;
        init
        {
#if DEBUG
            if (GetIsKeyCodeAtoZ (value) && (value & KeyCode.Space) != 0)
            {
                throw new ArgumentException (@$"Invalid KeyCode: {value} is invalid.", nameof (value));
            }

#endif
            _keyCode = value;
        }
    }

    /// <summary>
    ///     Helper for removing a shift modifier from a <see cref="Key"/>.
    ///     <code>
    /// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
    /// var AltDelete = ControlAltDelete.NoCtrl;
    /// </code>
    /// </summary>
    public Key NoAlt => new (this) { KeyCode = KeyCode & ~KeyCode.AltMask };

    /// <summary>
    ///     Helper for removing a shift modifier from a <see cref="Key"/>.
    ///     <code>
    /// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
    /// var AltDelete = ControlAltDelete.NoCtrl;
    /// </code>
    /// </summary>
    public Key NoCtrl => new (this) { KeyCode = KeyCode & ~KeyCode.CtrlMask };

    /// <summary>
    ///     Helper for removing a shift modifier from a <see cref="Key"/>.
    ///     <code>
    /// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
    /// var AltDelete = ControlAltDelete.NoCtrl;
    /// </code>
    /// </summary>
    public Key NoShift => new (this) { KeyCode = KeyCode & ~KeyCode.ShiftMask };

    /// <summary>
    ///     Helper for specifying a shifted <see cref="Key"/>.
    ///     <code>
    /// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
    /// </code>
    /// </summary>
    public Key WithAlt => new (this) { KeyCode = KeyCode | KeyCode.AltMask };

    /// <summary>
    ///     Helper for specifying a shifted <see cref="Key"/>.
    ///     <code>
    /// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
    /// </code>
    /// </summary>
    public Key WithCtrl => new (this) { KeyCode = KeyCode | KeyCode.CtrlMask };

    /// <summary>
    ///     Helper for specifying a shifted <see cref="Key"/>.
    ///     <code>
    /// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
    /// </code>
    /// </summary>
    public Key WithShift => new (this) { KeyCode = KeyCode | KeyCode.ShiftMask };

    /// <summary>
    ///     Tests if a KeyCode represents a key in the range of <see cref="KeyCode.A"/> to <see cref="KeyCode.Z"/>,
    ///     regardless of the <see cref="KeyCode.ShiftMask"/>. This is useful for testing if a key is based on these keys which
    ///     are special cased.
    /// </summary>
    /// <remarks>
    ///     IMPORTANT: Lowercase alpha keys are encoded in <see cref="Key.KeyCode"/> as values between 65 and 90
    ///     corresponding to the un-shifted A to Z keys on a keyboard. Helper properties are provided these (e.g.
    ///     <see cref="Key.A"/>, <see cref="Key.B"/>, etc.). Even though the values are the same as the ASCII values for
    ///     uppercase characters, these enum values represent *lowercase*, un-shifted characters.
    /// </remarks>
    public static bool GetIsKeyCodeAtoZ (KeyCode keyCode)
    {
        if ((keyCode & KeyCode.AltMask) != 0 || (keyCode & KeyCode.CtrlMask) != 0)
        {
            return false;
        }

        if ((keyCode & ~KeyCode.Space & ~KeyCode.ShiftMask) is >= KeyCode.A and <= KeyCode.Z)
        {
            return true;
        }

        return (keyCode & KeyCode.CharMask) is >= KeyCode.A and <= KeyCode.Z;
    }

    /// <summary>
    ///     Converts a <see cref="KeyCode"/> to a <see cref="Rune"/>. Useful for determining if a key represents is a
    ///     printable character.
    /// </summary>
    /// <remarks>
    ///     <para>Keys with Ctrl or Alt modifiers will return <see langword="default"/>.</para>
    ///     <para>
    ///         If the key is a letter key (A-Z), the Rune will be the upper or lower case letter depending on whether
    ///         <see cref="KeyCode.ShiftMask"/> is set.
    ///     </para>
    ///     <para>
    ///         If the key is outside of the <see cref="KeyCode.CharMask"/> range, the returned Rune will be
    ///         <see langword="default"/>.
    ///     </para>
    /// </remarks>
    /// <param name="key"></param>
    /// <returns>The key converted to a Rune. <see langword="default"/> if conversion is not possible.</returns>
    public static Rune ToRune (KeyCode key)
    {
        if (key is KeyCode.Null or KeyCode.SpecialMask
            || key.HasFlag (KeyCode.CtrlMask)
            || key.HasFlag (KeyCode.AltMask))
        {
            return default (Rune);
        }

        // Extract the base key code
        KeyCode baseKey = key;

        if (baseKey.HasFlag (KeyCode.ShiftMask))
        {
            baseKey &= ~KeyCode.ShiftMask;
        }

        switch (baseKey)
        {
            case >= KeyCode.A and <= KeyCode.Z when !key.HasFlag (KeyCode.ShiftMask):
                return new ((uint)(baseKey + 32));
            case >= KeyCode.A and <= KeyCode.Z when key.HasFlag (KeyCode.ShiftMask):
                return new ((uint)baseKey);
            case > KeyCode.Null and < KeyCode.A:
                return new ((uint)baseKey);
        }

        if (Enum.IsDefined (typeof (KeyCode), baseKey))
        {
            return default (Rune);
        }

        return new ((uint)baseKey);
    }

    #region Operators

    /// <summary>
    ///     Explicitly cast a <see cref="Key"/> to a <see cref="Rune"/>. The conversion is lossy because properties such
    ///     as <see cref="Handled"/> are not encoded in <see cref="KeyCode"/>.
    /// </summary>
    /// <remarks>Uses <see cref="AsRune"/>.</remarks>
    /// <param name="kea"></param>
    public static explicit operator Rune (Key kea) { return kea.AsRune; }

    // BUGBUG: (Tig) I do not think this cast operator is really needed. 
    /// <summary>
    ///     Explicitly cast <see cref="Key"/> to a <see langword="uint"/>. The conversion is lossy because properties such
    ///     as <see cref="Handled"/> are not encoded in <see cref="KeyCode"/>.
    /// </summary>
    /// <param name="kea"></param>
    public static explicit operator uint (Key kea) { return (uint)kea.KeyCode; }

    /// <summary>
    ///     Explicitly cast <see cref="Key"/> to a <see cref="KeyCode"/>. The conversion is lossy because properties such
    ///     as <see cref="Handled"/> are not encoded in <see cref="KeyCode"/>.
    /// </summary>
    /// <param name="key"></param>
    public static explicit operator KeyCode (Key key) { return key.KeyCode; }

    /// <summary>Cast <see cref="KeyCode"/> to a <see cref="Key"/>.</summary>
    /// <param name="keyCode"></param>
    public static implicit operator Key (KeyCode keyCode) { return new (keyCode); }

    /// <summary>Cast <see langword="char"/> to a <see cref="Key"/>.</summary>
    /// <remarks>See <see cref="Key(char)"/> for more information.</remarks>
    /// <param name="ch"></param>
    public static implicit operator Key (char ch) { return new (ch); }

    /// <summary>Cast <see langword="string"/> to a <see cref="Key"/>.</summary>
    /// <remarks>See <see cref="Key(string)"/> for more information.</remarks>
    /// <param name="str"></param>
    public static implicit operator Key (string str) { return new (str); }

    /// <summary>Cast a <see cref="Key"/> to a <see langword="string"/>.</summary>
    /// <remarks>See <see cref="Key(string)"/> for more information.</remarks>
    /// <param name="key"></param>
    public static implicit operator string (Key key) { return key.ToString (); }

    /// <inheritdoc/>
    public override bool Equals (object? obj)
    {
        if (obj is Key other)
        {
            return other._keyCode == _keyCode && other.Handled == Handled;
        }
        return false;
    }

    bool IEquatable<Key>.Equals (Key? other) { return Equals (other); }

    /// <inheritdoc/>
    public override int GetHashCode () { return _keyCode.GetHashCode (); }

    /// <summary>Compares two <see cref="Key"/>s for equality.</summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator == (Key a, Key b) { return a!.Equals (b); }

    /// <summary>Compares two <see cref="Key"/>s for not equality.</summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator != (Key? a, Key? b) { return !a!.Equals (b); }

    /// <summary>Compares two <see cref="Key"/>s for less-than.</summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator < (Key a, Key b) { return a?.KeyCode < b?.KeyCode; }

    /// <summary>Compares two <see cref="Key"/>s for greater-than.</summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator > (Key a, Key b) { return a?.KeyCode > b?.KeyCode; }

    /// <summary>Compares two <see cref="Key"/>s for greater-than-or-equal-to.</summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator <= (Key a, Key b) { return a?.KeyCode <= b?.KeyCode; }

    /// <summary>Compares two <see cref="Key"/>s for greater-than-or-equal-to.</summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator >= (Key a, Key b) { return a?.KeyCode >= b?.KeyCode; }

    #endregion Operators

    #region String conversion

    /// <summary>Pretty prints the Key.</summary>
    /// <returns></returns>
    public override string ToString () { return ToString (KeyCode, Separator); }

    private static string GetKeyString (KeyCode key)
    {
        if (key is KeyCode.Null or KeyCode.SpecialMask)
        {
            return string.Empty;
        }

        // Extract the base key (removing modifier flags)
        KeyCode baseKey = key & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask;

        if (!key.HasFlag (KeyCode.ShiftMask) && baseKey is >= KeyCode.A and <= KeyCode.Z)
        {
            return ((Rune)(uint)(key + 32)).ToString ();
        }

        if (key is > KeyCode.Space and < KeyCode.A)
        {
            return ((Rune)(uint)key).ToString ();
        }

        string? keyName = Enum.GetName (typeof (KeyCode), key);

        return !string.IsNullOrEmpty (keyName) ? keyName : ((Rune)(uint)key).ToString ();
    }

    /// <summary>Formats a <see cref="KeyCode"/> as a string using the default separator of '+'</summary>
    /// <param name="key">The key to format.</param>
    /// <returns>
    ///     The formatted string. If the key is a printable character, it will be returned as a string. Otherwise, the key
    ///     name will be returned.
    /// </returns>
    public static string ToString (KeyCode key) { return ToString (key, Separator); }

    /// <summary>Formats a <see cref="KeyCode"/> as a string.</summary>
    /// <param name="key">The key to format.</param>
    /// <param name="separator">The character to use as a separator between modifier keys and the key itself.</param>
    /// <returns>
    ///     The formatted string. If the key is a printable character, it will be returned as a string. Otherwise, the key
    ///     name will be returned.
    /// </returns>
    public static string ToString (KeyCode key, Rune separator)
    {
        if (key is KeyCode.Null)
        {
            // Same as Key.IsValid
            return @"Null";
        }

        var sb = new StringBuilder ();

        // Extract the base key (removing modifier flags)
        KeyCode baseKey = key & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask;

        // Extract and handle modifiers
        var hasModifiers = false;

        if ((key & KeyCode.CtrlMask) != 0)
        {
            sb.Append ($"Ctrl{separator}");
            hasModifiers = true;
        }

        if ((key & KeyCode.AltMask) != 0)
        {
            sb.Append ($"Alt{separator}");
            hasModifiers = true;
        }

        if ((key & KeyCode.ShiftMask) != 0 && !GetIsKeyCodeAtoZ (key))
        {
            sb.Append ($"Shift{separator}");
            hasModifiers = true;
        }

        // Handle special cases and modifiers on their own
        if (key != KeyCode.SpecialMask && (baseKey != KeyCode.Null || hasModifiers))
        {
            if ((key & KeyCode.SpecialMask) != 0 && (baseKey & ~KeyCode.Space) is >= KeyCode.A and <= KeyCode.Z)
            {
                sb.Append (baseKey & ~KeyCode.Space);
            }
            else
            {
                // Append the actual key name
                sb.Append (GetKeyString (baseKey));
            }
        }

        return TrimEndSeparator (sb.ToString (), separator);
    }

    private static string TrimEndSeparator (string input, Rune separator)
    {
        // Trim the trailing separator (+). Unless there are two separators at the end.
        // "+" (don't trim)
        // "Ctrl+" (trim)
        // "Ctrl++" (trim)

        if (input.Length > 1 && new Rune (input [^1]) == separator && new Rune (input [^2]) != separator)
        {
            return input [..^1];
        }

        return input;
    }

    private static readonly Dictionary<string, KeyCode> _modifierDict =
        new (StringComparer.InvariantCultureIgnoreCase)
        {
            { "Shift", KeyCode.ShiftMask }, { "Ctrl", KeyCode.CtrlMask }, { "Alt", KeyCode.AltMask }
        };

    /// <summary>Converts the provided string to a new <see cref="Key"/> instance.</summary>
    /// <param name="text">
    ///     The text to analyze. Formats supported are "Ctrl+X", "Alt+X", "Shift+X", "Ctrl+Alt+X",
    ///     "Ctrl+Shift+X", "Alt+Shift+X", "Ctrl+Alt+Shift+X", "X", and "120" (Unicode codepoint).
    ///     <para>
    ///         The separator can be any character, not just <see cref="Key.Separator"/> (e.g. "Ctrl@Alt@X").
    ///     </para>
    /// </param>
    /// <param name="key">The parsed value.</param>
    /// <returns>A boolean value indicating whether parsing was successful.</returns>
    /// <remarks></remarks>
    public static bool TryParse (string text, out Key key)
    {
        if (string.IsNullOrEmpty (text))
        {
            key = Empty;

            return true;
        }

        switch (text)
        {
            case "Ctrl":
                key = KeyCode.CtrlMask;

                return true;
            case "Alt":
                key = KeyCode.AltMask;

                return true;
            case "Shift":
                key = KeyCode.ShiftMask;

                return true;
        }

        key = null!;

        Rune separator = Separator;

        // Perhaps the separator was written using a different Key.Separator? Does the string
        // start with "Ctrl", "Alt" or "Shift"? If so, get the char after the modifier string and use that as the separator.
        if (text.StartsWith ("Ctrl", StringComparison.InvariantCultureIgnoreCase))
        {
            separator = (Rune)text [4];
        }
        else if (text.StartsWith ("Alt", StringComparison.InvariantCultureIgnoreCase))
        {
            separator = (Rune)text [3];
        }
        else if (text.StartsWith ("Shift", StringComparison.InvariantCultureIgnoreCase))
        {
            separator = (Rune)text [5];
        }
        else if (text.EndsWith ("Ctrl", StringComparison.InvariantCultureIgnoreCase))
        {
            separator = (Rune)text [^5];
        }
        else if (text.EndsWith ("Alt", StringComparison.InvariantCultureIgnoreCase))
        {
            separator = (Rune)text [^4];
        }
        else if (text.EndsWith ("Shift", StringComparison.InvariantCultureIgnoreCase))
        {
            separator = (Rune)text [^6];
        }

        // Split the string into parts using the set Separator
        string [] parts = text.Split ((char)separator.Value);

        if (parts.Length is > 4)
        {
            // Invalid
            return false;
        }

        // e.g. "Ctrl++"
        if ((Rune)text [^1] != separator && parts.Any (string.IsNullOrEmpty))
        {
            // Invalid
            return false;
        }

        if ((Rune)text [^1] == separator)
        {
            parts [^1] = separator.Value.ToString ();
            key = (char)separator.Value;
        }

        if (separator != Separator && (parts.Length is 1 || (key is { } && parts.Length is 2)))
        {
            parts = text.Split ((char)separator.Value);

            if (parts.Length is 0 or > 4 || parts.Any (string.IsNullOrEmpty))
            {
                // Invalid
                return false;
            }
        }

        var modifiers = KeyCode.Null;

        for (var index = 0; index < parts.Length; index++)
        {
            if (_modifierDict.TryGetValue (parts [index].ToLowerInvariant (), out KeyCode modifier))
            {
                modifiers |= modifier;
                parts [index] = string.Empty; // eat it
            }
        }

        // we now have the modifiers

        string partNotFound = parts.FirstOrDefault (p => !string.IsNullOrEmpty (p), string.Empty);
        var parsedKeyCode = KeyCode.Null;
        var parsedInt = 0;

        if (partNotFound.Length == 1)
        {
            var keyCode = (KeyCode)partNotFound [0];

            // if it's a single digit int, treat it as such
            if (int.TryParse (
                              partNotFound,
                              NumberStyles.Integer,
                              CultureInfo.InvariantCulture,
                              out parsedInt
                             ))
            {
                keyCode = (KeyCode)((int)KeyCode.D0 + parsedInt);
            }
            else if (Enum.TryParse (partNotFound, false, out parsedKeyCode))
            {
                if (parsedKeyCode != KeyCode.Null)
                {
                    if (parsedKeyCode is >= KeyCode.A and <= KeyCode.Z && modifiers == 0)
                    {
                        key = new (parsedKeyCode | KeyCode.ShiftMask);

                        return true;
                    }

                    key = new (parsedKeyCode | modifiers);

                    return true;
                }
            }

            if (GetIsKeyCodeAtoZ (keyCode) && (keyCode & KeyCode.Space) != 0)
            {
                keyCode &= ~KeyCode.Space;
            }

            key = new (keyCode | modifiers);

            return true;
        }

        if (Enum.TryParse (partNotFound, true, out parsedKeyCode))
        {
            if (parsedKeyCode != KeyCode.Null)
            {
                if (parsedKeyCode is >= KeyCode.A and <= KeyCode.Z && modifiers == 0)
                {
                    key = new (parsedKeyCode | KeyCode.ShiftMask);

                    return true;
                }

                if (GetIsKeyCodeAtoZ (parsedKeyCode) && (parsedKeyCode & KeyCode.Space) != 0)
                {
                    parsedKeyCode = parsedKeyCode & ~KeyCode.Space;
                }

                key = new (parsedKeyCode | modifiers);

                return true;
            }
        }

        // if it's a number int, treat it as a unicode value
        if (int.TryParse (
                          partNotFound,
                          NumberStyles.Number,
                          CultureInfo.InvariantCulture,
                          out parsedInt
                         ))
        {
            if (!Rune.IsValid (parsedInt))
            {
                return false;
            }

            if ((KeyCode)parsedInt is >= KeyCode.A and <= KeyCode.Z && modifiers == 0)
            {
                key = new ((KeyCode)parsedInt | KeyCode.ShiftMask);

                return true;
            }

            key = new ((KeyCode)parsedInt);

            return true;
        }

        if (!Enum.TryParse (partNotFound, true, out parsedKeyCode))
        {
            return false;
        }

        if (GetIsKeyCodeAtoZ (parsedKeyCode))
        {
            key = new (parsedKeyCode | (modifiers & ~KeyCode.Space));

            return true;
        }

        return false;
    }

    #endregion

    #region Standard Key Definitions

    /// <summary>An uninitialized The <see cref="Key"/> object.</summary>
    public new static Key Empty => new ();

    /// <summary>The <see cref="Key"/> object for the Backspace key.</summary>
    public static Key Backspace => new (KeyCode.Backspace);

    /// <summary>The <see cref="Key"/> object for the tab key (forwards tab key).</summary>
    public static Key Tab => new (KeyCode.Tab);

    /// <summary>The <see cref="Key"/> object for the return key.</summary>
    public static Key Enter => new (KeyCode.Enter);

    /// <summary>The <see cref="Key"/> object for the clear key.</summary>
    public static Key Clear => new (KeyCode.Clear);

    /// <summary>The <see cref="Key"/> object for the Escape key.</summary>
    public static Key Esc => new (KeyCode.Esc);

    /// <summary>The <see cref="Key"/> object for the Space bar key.</summary>
    public static Key Space => new (KeyCode.Space);

    /// <summary>The <see cref="Key"/> object for 0 key.</summary>
    public static Key D0 => new (KeyCode.D0);

    /// <summary>The <see cref="Key"/> object for 1 key.</summary>
    public static Key D1 => new (KeyCode.D1);

    /// <summary>The <see cref="Key"/> object for 2 key.</summary>
    public static Key D2 => new (KeyCode.D2);

    /// <summary>The <see cref="Key"/> object for 3 key.</summary>
    public static Key D3 => new (KeyCode.D3);

    /// <summary>The <see cref="Key"/> object for 4 key.</summary>
    public static Key D4 => new (KeyCode.D4);

    /// <summary>The <see cref="Key"/> object for 5 key.</summary>
    public static Key D5 => new (KeyCode.D5);

    /// <summary>The <see cref="Key"/> object for 6 key.</summary>
    public static Key D6 => new (KeyCode.D6);

    /// <summary>The <see cref="Key"/> object for 7 key.</summary>
    public static Key D7 => new (KeyCode.D7);

    /// <summary>The <see cref="Key"/> object for 8 key.</summary>
    public static Key D8 => new (KeyCode.D8);

    /// <summary>The <see cref="Key"/> object for 9 key.</summary>
    public static Key D9 => new (KeyCode.D9);

    /// <summary>The <see cref="Key"/> object for the A key (un-shifted). Use <c>Key.A.WithShift</c> for uppercase 'A'.</summary>
    public static Key A => new (KeyCode.A);

    /// <summary>The <see cref="Key"/> object for the B key (un-shifted). Use <c>Key.B.WithShift</c> for uppercase 'B'.</summary>
    public static Key B => new (KeyCode.B);

    /// <summary>The <see cref="Key"/> object for the C key (un-shifted). Use <c>Key.C.WithShift</c> for uppercase 'C'.</summary>
    public static Key C => new (KeyCode.C);

    /// <summary>The <see cref="Key"/> object for the D key (un-shifted). Use <c>Key.D.WithShift</c> for uppercase 'D'.</summary>
    public static Key D => new (KeyCode.D);

    /// <summary>The <see cref="Key"/> object for the E key (un-shifted). Use <c>Key.E.WithShift</c> for uppercase 'E'.</summary>
    public static Key E => new (KeyCode.E);

    /// <summary>The <see cref="Key"/> object for the F key (un-shifted). Use <c>Key.F.WithShift</c> for uppercase 'F'.</summary>
    public static Key F => new (KeyCode.F);

    /// <summary>The <see cref="Key"/> object for the G key (un-shifted). Use <c>Key.G.WithShift</c> for uppercase 'G'.</summary>
    public static Key G => new (KeyCode.G);

    /// <summary>The <see cref="Key"/> object for the H key (un-shifted). Use <c>Key.H.WithShift</c> for uppercase 'H'.</summary>
    public static Key H => new (KeyCode.H);

    /// <summary>The <see cref="Key"/> object for the I key (un-shifted). Use <c>Key.I.WithShift</c> for uppercase 'I'.</summary>
    public static Key I => new (KeyCode.I);

    /// <summary>The <see cref="Key"/> object for the J key (un-shifted). Use <c>Key.J.WithShift</c> for uppercase 'J'.</summary>
    public static Key J => new (KeyCode.J);

    /// <summary>The <see cref="Key"/> object for the K key (un-shifted). Use <c>Key.K.WithShift</c> for uppercase 'K'.</summary>
    public static Key K => new (KeyCode.K);

    /// <summary>The <see cref="Key"/> object for the L key (un-shifted). Use <c>Key.L.WithShift</c> for uppercase 'L'.</summary>
    public static Key L => new (KeyCode.L);

    /// <summary>The <see cref="Key"/> object for the M key (un-shifted). Use <c>Key.M.WithShift</c> for uppercase 'M'.</summary>
    public static Key M => new (KeyCode.M);

    /// <summary>The <see cref="Key"/> object for the N key (un-shifted). Use <c>Key.N.WithShift</c> for uppercase 'N'.</summary>
    public static Key N => new (KeyCode.N);

    /// <summary>The <see cref="Key"/> object for the O key (un-shifted). Use <c>Key.O.WithShift</c> for uppercase 'O'.</summary>
    public static Key O => new (KeyCode.O);

    /// <summary>The <see cref="Key"/> object for the P key (un-shifted). Use <c>Key.P.WithShift</c> for uppercase 'P'.</summary>
    public static Key P => new (KeyCode.P);

    /// <summary>The <see cref="Key"/> object for the Q key (un-shifted). Use <c>Key.Q.WithShift</c> for uppercase 'Q'.</summary>
    public static Key Q => new (KeyCode.Q);

    /// <summary>The <see cref="Key"/> object for the R key (un-shifted). Use <c>Key.R.WithShift</c> for uppercase 'R'.</summary>
    public static Key R => new (KeyCode.R);

    /// <summary>The <see cref="Key"/> object for the S key (un-shifted). Use <c>Key.S.WithShift</c> for uppercase 'S'.</summary>
    public static Key S => new (KeyCode.S);

    /// <summary>The <see cref="Key"/> object for the T key (un-shifted). Use <c>Key.T.WithShift</c> for uppercase 'T'.</summary>
    public static Key T => new (KeyCode.T);

    /// <summary>The <see cref="Key"/> object for the U key (un-shifted). Use <c>Key.U.WithShift</c> for uppercase 'U'.</summary>
    public static Key U => new (KeyCode.U);

    /// <summary>The <see cref="Key"/> object for the V key (un-shifted). Use <c>Key.V.WithShift</c> for uppercase 'V'.</summary>
    public static Key V => new (KeyCode.V);

    /// <summary>The <see cref="Key"/> object for the W key (un-shifted). Use <c>Key.W.WithShift</c> for uppercase 'W'.</summary>
    public static Key W => new (KeyCode.W);

    /// <summary>The <see cref="Key"/> object for the X key (un-shifted). Use <c>Key.X.WithShift</c> for uppercase 'X'.</summary>
    public static Key X => new (KeyCode.X);

    /// <summary>The <see cref="Key"/> object for the Y key (un-shifted). Use <c>Key.Y.WithShift</c> for uppercase 'Y'.</summary>
    public static Key Y => new (KeyCode.Y);

    /// <summary>The <see cref="Key"/> object for the Z key (un-shifted). Use <c>Key.Z.WithShift</c> for uppercase 'Z'.</summary>
    public static Key Z => new (KeyCode.Z);

    /// <summary>The <see cref="Key"/> object for the Delete key.</summary>
    public static Key Delete => new (KeyCode.Delete);

    /// <summary>The <see cref="Key"/> object for the Cursor up key.</summary>
    public static Key CursorUp => new (KeyCode.CursorUp);

    /// <summary>The <see cref="Key"/> object for Cursor down key.</summary>
    public static Key CursorDown => new (KeyCode.CursorDown);

    /// <summary>The <see cref="Key"/> object for Cursor left key.</summary>
    public static Key CursorLeft => new (KeyCode.CursorLeft);

    /// <summary>The <see cref="Key"/> object for Cursor right key.</summary>
    public static Key CursorRight => new (KeyCode.CursorRight);

    /// <summary>The <see cref="Key"/> object for Page Up key.</summary>
    public static Key PageUp => new (KeyCode.PageUp);

    /// <summary>The <see cref="Key"/> object for Page Down key.</summary>
    public static Key PageDown => new (KeyCode.PageDown);

    /// <summary>The <see cref="Key"/> object for Home key.</summary>
    public static Key Home => new (KeyCode.Home);

    /// <summary>The <see cref="Key"/> object for End key.</summary>
    public static Key End => new (KeyCode.End);

    /// <summary>The <see cref="Key"/> object for Insert Character key.</summary>
    public static Key InsertChar => new (KeyCode.Insert);

    /// <summary>The <see cref="Key"/> object for Delete Character key.</summary>
    public static Key DeleteChar => new (KeyCode.Delete);

    /// <summary>The <see cref="Key"/> object for Print Screen key.</summary>
    public static Key PrintScreen => new (KeyCode.PrintScreen);

    /// <summary>The <see cref="Key"/> object for F1 key.</summary>
    public static Key F1 => new (KeyCode.F1);

    /// <summary>The <see cref="Key"/> object for F2 key.</summary>
    public static Key F2 => new (KeyCode.F2);

    /// <summary>The <see cref="Key"/> object for F3 key.</summary>
    public static Key F3 => new (KeyCode.F3);

    /// <summary>The <see cref="Key"/> object for F4 key.</summary>
    public static Key F4 => new (KeyCode.F4);

    /// <summary>The <see cref="Key"/> object for F5 key.</summary>
    public static Key F5 => new (KeyCode.F5);

    /// <summary>The <see cref="Key"/> object for F6 key.</summary>
    public static Key F6 => new (KeyCode.F6);

    /// <summary>The <see cref="Key"/> object for F7 key.</summary>
    public static Key F7 => new (KeyCode.F7);

    /// <summary>The <see cref="Key"/> object for F8 key.</summary>
    public static Key F8 => new (KeyCode.F8);

    /// <summary>The <see cref="Key"/> object for F9 key.</summary>
    public static Key F9 => new (KeyCode.F9);

    /// <summary>The <see cref="Key"/> object for F10 key.</summary>
    public static Key F10 => new (KeyCode.F10);

    /// <summary>The <see cref="Key"/> object for F11 key.</summary>
    public static Key F11 => new (KeyCode.F11);

    /// <summary>The <see cref="Key"/> object for F12 key.</summary>
    public static Key F12 => new (KeyCode.F12);

    /// <summary>The <see cref="Key"/> object for F13 key.</summary>
    public static Key F13 => new (KeyCode.F13);

    /// <summary>The <see cref="Key"/> object for F14 key.</summary>
    public static Key F14 => new (KeyCode.F14);

    /// <summary>The <see cref="Key"/> object for F15 key.</summary>
    public static Key F15 => new (KeyCode.F15);

    /// <summary>The <see cref="Key"/> object for F16 key.</summary>
    public static Key F16 => new (KeyCode.F16);

    /// <summary>The <see cref="Key"/> object for F17 key.</summary>
    public static Key F17 => new (KeyCode.F17);

    /// <summary>The <see cref="Key"/> object for F18 key.</summary>
    public static Key F18 => new (KeyCode.F18);

    /// <summary>The <see cref="Key"/> object for F19 key.</summary>
    public static Key F19 => new (KeyCode.F19);

    /// <summary>The <see cref="Key"/> object for F20 key.</summary>
    public static Key F20 => new (KeyCode.F20);

    /// <summary>The <see cref="Key"/> object for F21 key.</summary>
    public static Key F21 => new (KeyCode.F21);

    /// <summary>The <see cref="Key"/> object for F22 key.</summary>
    public static Key F22 => new (KeyCode.F22);

    /// <summary>The <see cref="Key"/> object for F23 key.</summary>
    public static Key F23 => new (KeyCode.F23);

    /// <summary>The <see cref="Key"/> object for F24 key.</summary>
    public static Key F24 => new (KeyCode.F24);

    #endregion

    private static Rune _separator = new ('+');

    /// <summary>Gets or sets the separator character used when parsing and printing Keys. E.g. Ctrl+A. The default is '+'.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Rune Separator
    {
        get => _separator;
        set
        {
            if (_separator != value)
            {
                _separator = value == default (Rune) ? new ('+') : value;
            }
        }
    }
}
