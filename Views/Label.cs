
using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal {
    public enum TextAlignment {
        Left, Right, Centered, Justified
    }

    /// <summary>
    ///   Label widget, displays a string at a given position, can include multiple lines.
    /// </summary>
    public class Label : View {
        List<string> lines = new List<string> ();
        bool recalcPending = true;
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

        static char [] whitespace = new char [] { ' ', '\t' };

        string ClipAndJustify (string str)
        {
            int slen = str.Length;
            if (slen > Frame.Width)
                return str.Substring (0, Frame.Width);
            else {
                if (textAlignment == TextAlignment.Justified) {
                    var words = str.Split (whitespace, StringSplitOptions.RemoveEmptyEntries);
                    int textCount = words.Sum ((arg) => arg.Length);

                    var spaces = (Frame.Width - textCount) / (words.Length - 1);
                    var extras = (Frame.Width - textCount) % words.Length;
                    var s = new System.Text.StringBuilder ();
                    //s.Append ($"tc={textCount} sp={spaces},x={extras} - ");
                    for (int w = 0; w < words.Length; w++) {
                        var x = words [w];
                        s.Append (x);
                        if (w + 1 < words.Length)
                            for (int i = 0; i < spaces; i++)
                                s.Append (' ');
                        if (extras > 0) {
                            s.Append ('_');
                            extras--;
                        }
                    }
                    return s.ToString ();
                }
                return str;
            }
        }

        void Recalc ()
        {
            lines.Clear ();
            if (text.IndexOf ('\n') == -1) {
                lines.Add (ClipAndJustify (text));
                return;
            }
            int textLen = text.Length;
            int lp = 0;
            for (int i = 0; i < textLen; i++) {
                char c = text [i];

                if (c == '\n') {
                    lines.Add (ClipAndJustify (text.Substring (lp, i - lp)));
                    lp = i + 1;
                }
            }
            recalcPending = false;
        }

        public override void Redraw (Rect region)
        {
            if (recalcPending)
                Recalc ();
            
            if (TextColor != -1)
                Driver.SetColor (TextColor);
            else
                Driver.SetColor(Colors.Base.Normal);

            Clear ();
            Move (Frame.X, Frame.Y);
            for (int line = 0; line < lines.Count; line++) {
                if (line < region.Top || line >= region.Bottom)
                    continue;
                var str = lines [line];
                int x;
                switch (textAlignment) {
                case TextAlignment.Left:
                case TextAlignment.Justified:
                    x = 0;
                    break;
                case TextAlignment.Right:
                    x = Frame.Right - str.Length;
                    break;
                case TextAlignment.Centered:
                    x = Frame.Left + (Frame.Width - str.Length) / 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException ();
                }
                Move (x, line);
                Driver.AddStr (str);
            }
        }

        /// <summary>
        ///   The text displayed by this widget.
        /// </summary>
        public virtual string Text {
            get => text;
            set {
                text = value;
                recalcPending = true;
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
