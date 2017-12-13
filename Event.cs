namespace Terminal {

    public class Event {
        public class Key : Event {
            public int Code { get; private set; }

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