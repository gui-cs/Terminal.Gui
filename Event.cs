namespace Terminal {

    /// <summary>
    /// The Key enumeration contains special encoding for some keys, but can also
    /// encode all the unicode values that can be passed.   
    /// </summary>
    /// <remarks>
    /// <para>
    ///   If the SpecialMask is set, then the value is that of the special mask,
    ///   otherwise, the value is the one of the lower bits (as extracted by CharMask)
    /// </para>
    /// <para>
    ///   Control keys are the values between 1 and 26 corresponding to Control-A to Control-Z
    /// </para>
    /// </remarks>
    public enum Key : uint {
        CharMask = 0xfffff,
        SpecialMask = 0xfff00000,
        ControlA = 1,
        ControlB,
        ControlC,
        ControlD,
        ControlE,
        ControlF,
        ControlG,
        ControlH,
        ControlI,
        Tab = ControlI,
        ControlJ,
        ControlK,
        ControlL,
        ControlM,
        ControlN,
        ControlO,
        ControlP,
        ControlQ,
        ControlR,
        ControlS,
        ControlT,
        ControlU,
        ControlV,
        ControlW,
        ControlX,
        ControlY,
        ControlZ,
        Esc = 27,
        Space = 32,
        Delete = 127,

        AltMask = 0x80000000,

        Backspace = 0x100000,
        CursorUp,
        CursorDown,
        CursorLeft,
        CursorRight,
        PageUp,
        PageDown,
        Home,
        End,
        DeleteChar,
        InsertChar,
        F1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        BackTab,
        Unknown
    }

    public struct KeyEvent {
        public Key Key;
        public int KeyValue => (int)Key;
        public bool IsAlt => (Key & Key.AltMask) != 0;
        public bool IsCtrl => ((uint)Key >= 1) && ((uint)Key <= 26);

        public KeyEvent (Key k)
        {
            Key = k;
        }
    }

    public class Event {
        public class Key : Event {
            public int Code { get; private set; }
            public bool Alt { get; private set; }
            public Key (int code)
            {
                Code = code;
            }
        }

        public class Mouse : Event {
        }

        public static Event CreateMouseEvent ()
        {
            return new Mouse ();
        }

        public static Event CreateKeyEvent (int code)
        {
            return new Key (code);
        }

    }

}