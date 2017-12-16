
using System;

namespace Terminal {
    public enum TextAlignment {
        Left, Right, Centered, Justified
    }

    /// <summary>
    ///   Label widget, displays a string at a given position, can include multiple lines.
    /// </summary>
    public class Label : View {
        string text;
        TextAlignment textAlignment;

        static Rect CalcRect (int x, int y, string s)
        {
            int mw = 0;
            int ml = 1;

            int cols = 0;
            foreach (var c in s) {
                if (c == '\n'){
                    ml++;
                    if (cols > mw)
                        mw = cols;
                    cols = 0;
                } else
                    cols++;
            }
            return new Rect (x, y, cols, ml);
        }

        /// <summary>
        ///   Public constructor: creates a label at the given
        ///   coordinate with the given string, computes the bounding box
        ///   based on the size of the string, assumes that the string contains
        ///   newlines for multiple lines, no special breaking rules are used.
        /// </summary>
        public Label (int x, int y, string text) : this (CalcRect (x, y, text), text)
        {
        }

        /// <summary>
        ///   Public constructor: creates a label at the given
        ///   coordinate with the given string and uses the specified
        ///   frame for the string.
        /// </summary>
        public Label (Rect rect, string text) : base (rect)
        {
            this.text = text;
        }

        public override void Redraw ()
        {
            if (TextColor != -1)
                Driver.SetColor (TextColor);
            else
                Driver.SetColor(Colors.Base.Normal);

            Clear ();
            Move (Frame.X, Frame.Y);
            Driver.AddStr (text);
        }

        /// <summary>
        ///   The text displayed by this widget.
        /// </summary>
        public virtual string Text {
            get => text;
            set {
                text = value;
                SetNeedsDisplay ();
            }
        }

        public TextAlignment TextAlignment {
            get => textAlignment;
            set {
                textAlignment = value;
                SetNeedsDisplay ();
            }
        }

        /// <summary>
        ///   The color used for the label
        /// </summary>        
        Color textColor = -1;
        public Color TextColor {
            get => textColor;
            set {
                textColor = value;
                SetNeedsDisplay ();
            }
        }
    }

}
