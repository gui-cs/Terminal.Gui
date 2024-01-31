//
// TextValidateField.cs: single-line text editor with validation through providers.
//
// Authors:
//	José Miguel Perricone (jmperricone@hotmail.com)
//

#region

using System.ComponentModel;
using System.Text.RegularExpressions;
using Terminal.Gui.TextValidateProviders;

#endregion

namespace Terminal.Gui {
    namespace TextValidateProviders {
        /// <summary>
        /// TextValidateField Providers Interface.
        /// All TextValidateField are created with a ITextValidateProvider.
        /// </summary>
        public interface ITextValidateProvider {
            /// <summary>
            /// Set that this provider uses a fixed width.
            /// e.g. Masked ones are fixed.
            /// </summary>
            bool Fixed { get; }

            /// <summary>
            /// Set Cursor position to <paramref name="pos"/>.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns>Return first valid position.</returns>
            int Cursor (int pos);

            /// <summary>
            /// First valid position before <paramref name="pos"/>.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns>New cursor position if any, otherwise returns <paramref name="pos"/></returns>
            int CursorLeft (int pos);

            /// <summary>
            /// First valid position after <paramref name="pos"/>.
            /// </summary>
            /// <param name="pos">Current position.</param>
            /// <returns>New cursor position if any, otherwise returns <paramref name="pos"/></returns>
            int CursorRight (int pos);

            /// <summary>
            /// Find the first valid character position.
            /// </summary>
            /// <returns>New cursor position.</returns>
            int CursorStart ();

            /// <summary>
            /// Find the last valid character position.
            /// </summary>
            /// <returns>New cursor position.</returns>
            int CursorEnd ();

            /// <summary>
            /// Deletes the current character in <paramref name="pos"/>.
            /// </summary>
            /// <param name="pos"></param>
            /// <returns>true if the character was successfully removed, otherwise false.</returns>
            bool Delete (int pos);

            /// <summary>
            /// Insert character <paramref name="ch"/> in position <paramref name="pos"/>.
            /// </summary>
            /// <param name="ch"></param>
            /// <param name="pos"></param>
            /// <returns>true if the character was successfully inserted, otherwise false.</returns>
            bool InsertAt (char ch, int pos);

            /// <summary>
            /// True if the input is valid, otherwise false.
            /// </summary>
            bool IsValid { get; }

            /// <summary>
            /// Set the input text and get the current value.
            /// </summary>
            string Text { get; set; }

            /// <summary>
            /// Gets the formatted string for display.
            /// </summary>
            string DisplayText { get; }

            /// <summary>
            /// Method that invoke the <see cref="TextChanged"/> event if it's defined.
            /// </summary>
            /// <param name="oldValue">The previous text before replaced.</param>
            /// <returns>Returns the <see cref="TextChangedEventArgs"/></returns>
            void OnTextChanged (TextChangedEventArgs oldValue);

            /// <summary>
            /// Changed event, raised when the text has changed.
            /// <remarks>
            /// This event is raised when the <see cref="Text"/> changes.
            /// The passed <see cref="EventArgs"/> is a <see cref="string"/> containing the old value.
            /// </remarks>
            /// </summary>
            event EventHandler<TextChangedEventArgs> TextChanged;
        }

        //////////////////////////////////////////////////////////////////////////////
        // PROVIDERS
        //////////////////////////////////////////////////////////////////////////////

        #region NetMaskedTextProvider

        /// <summary>
        /// .Net MaskedTextProvider Provider for TextValidateField.
        /// <para></para>
        /// <para>
        ///     <a href="https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.maskedtextprovider?view=net-5.0">
        ///     Wrapper
        ///     around MaskedTextProvider
        ///     </a>
        /// </para>
        /// <para>
        ///     <a href="https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.maskedtextbox.mask?view=net-5.0">
        ///     Masking
        ///     elements
        ///     </a>
        /// </para>
        /// </summary>
        public class NetMaskedTextProvider : ITextValidateProvider {
            MaskedTextProvider _provider;

            /// <inheritdoc/>
            public event EventHandler<TextChangedEventArgs> TextChanged;

            /// <summary>
            /// Empty Constructor
            /// </summary>
            public NetMaskedTextProvider (string mask) { Mask = mask; }

            /// <summary>
            /// Mask property
            /// </summary>
            public string Mask {
                get { return _provider?.Mask; }
                set {
                    var current = _provider != null ? _provider.ToString (false, false) : string.Empty;
                    _provider = new MaskedTextProvider (value == string.Empty ? "&&&&&&" : value);
                    if (!string.IsNullOrEmpty (current)) {
                        _provider.Set (current);
                    }
                }
            }

            ///<inheritdoc/>
            public string Text { get { return _provider.ToString (); } set { _provider.Set (value); } }

            ///<inheritdoc/>
            public bool IsValid => _provider.MaskCompleted;

            ///<inheritdoc/>
            public bool Fixed => true;

            ///<inheritdoc/>
            public string DisplayText => _provider.ToDisplayString ();

            ///<inheritdoc/>
            public int Cursor (int pos) {
                if (pos < 0) {
                    return CursorStart ();
                } else if (pos > _provider.Length) {
                    return CursorEnd ();
                } else {
                    var p = _provider.FindEditPositionFrom (pos, false);
                    if (p == -1) p = _provider.FindEditPositionFrom (pos, true);

                    return p;
                }
            }

            ///<inheritdoc/>
            public int CursorStart () {
                return
                    _provider.IsEditPosition (0)
                        ? 0
                        : _provider.FindEditPositionFrom (0, true);
            }

            ///<inheritdoc/>
            public int CursorEnd () {
                return
                    _provider.IsEditPosition (_provider.Length - 1)
                        ? _provider.Length - 1
                        : _provider.FindEditPositionFrom (_provider.Length, false);
            }

            ///<inheritdoc/>
            public int CursorLeft (int pos) {
                var c = _provider.FindEditPositionFrom (pos - 1, false);

                return c == -1 ? pos : c;
            }

            ///<inheritdoc/>
            public int CursorRight (int pos) {
                var c = _provider.FindEditPositionFrom (pos + 1, true);

                return c == -1 ? pos : c;
            }

            ///<inheritdoc/>
            public bool Delete (int pos) {
                var oldValue = Text;
                var result = _provider.Replace (' ', pos); // .RemoveAt (pos);
                if (result) {
                    OnTextChanged (new TextChangedEventArgs (oldValue));
                }

                return result;
            }

            ///<inheritdoc/>
            public bool InsertAt (char ch, int pos) {
                var oldValue = Text;
                var result = _provider.Replace (ch, pos);
                if (result) {
                    OnTextChanged (new TextChangedEventArgs (oldValue));
                }

                return result;
            }

            /// <inheritdoc/>
            public void OnTextChanged (TextChangedEventArgs oldValue) => TextChanged?.Invoke (this, oldValue);
        }

        #endregion

        #region TextRegexProvider

        /// <summary>
        /// Regex Provider for TextValidateField.
        /// </summary>
        public class TextRegexProvider : ITextValidateProvider {
            Regex _regex;
            List<Rune> _text;
            List<Rune> _pattern;

            /// <inheritdoc/>
            public event EventHandler<TextChangedEventArgs> TextChanged;

            /// <summary>
            /// Empty Constructor.
            /// </summary>
            public TextRegexProvider (string pattern) { Pattern = pattern; }

            /// <summary>
            /// Regex pattern property.
            /// </summary>
            public string Pattern {
                get { return StringExtensions.ToString (_pattern); }
                set {
                    _pattern = value.ToRuneList ();
                    CompileMask ();
                    SetupText ();
                }
            }

            ///<inheritdoc/>
            public string Text {
                get { return StringExtensions.ToString (_text); }
                set {
                    _text = value != string.Empty ? value.ToRuneList () : null;
                    SetupText ();
                }
            }

            ///<inheritdoc/>
            public string DisplayText => Text;

            ///<inheritdoc/>
            public bool IsValid { get { return Validate (_text); } }

            ///<inheritdoc/>
            public bool Fixed => false;

            /// <summary>
            /// When true, validates with the regex pattern on each input, preventing the input if it's not valid.
            /// </summary>
            public bool ValidateOnInput { get; set; } = true;

            bool Validate (List<Rune> text) {
                var match = _regex.Match (StringExtensions.ToString (text));

                return match.Success;
            }

            ///<inheritdoc/>
            public int Cursor (int pos) {
                if (pos < 0) {
                    return CursorStart ();
                } else if (pos >= _text.Count) {
                    return CursorEnd ();
                } else {
                    return pos;
                }
            }

            ///<inheritdoc/>
            public int CursorStart () { return 0; }

            ///<inheritdoc/>
            public int CursorEnd () { return _text.Count; }

            ///<inheritdoc/>
            public int CursorLeft (int pos) {
                if (pos > 0) {
                    return pos - 1;
                }

                return pos;
            }

            ///<inheritdoc/>
            public int CursorRight (int pos) {
                if (pos < _text.Count) {
                    return pos + 1;
                }

                return pos;
            }

            ///<inheritdoc/>
            public bool Delete (int pos) {
                if (_text.Count > 0 && pos < _text.Count) {
                    var oldValue = Text;
                    _text.RemoveAt (pos);
                    OnTextChanged (new TextChangedEventArgs (oldValue));
                }

                return true;
            }

            ///<inheritdoc/>
            public bool InsertAt (char ch, int pos) {
                var aux = _text.ToList ();
                aux.Insert (pos, (Rune)ch);
                if (Validate (aux) || ValidateOnInput == false) {
                    var oldValue = Text;
                    _text.Insert (pos, (Rune)ch);
                    OnTextChanged (new TextChangedEventArgs (oldValue));

                    return true;
                }

                return false;
            }

            void SetupText () {
                if (_text != null && IsValid) {
                    return;
                }

                _text = new List<Rune> ();
            }

            /// <summary>
            /// Compiles the regex pattern for validation./>
            /// </summary>
            private void CompileMask () {
                _regex = new Regex (StringExtensions.ToString (_pattern), RegexOptions.Compiled);
            }

            /// <inheritdoc/>
            public void OnTextChanged (TextChangedEventArgs oldValue) => TextChanged?.Invoke (this, oldValue);
        }

        #endregion
    }

    /// <summary>
    /// Text field that validates input through a  <see cref="ITextValidateProvider"/>
    /// </summary>
    public class TextValidateField : View {
        ITextValidateProvider _provider;
        int _cursorPosition;
        int _defaultLength = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextValidateField"/> class using <see cref="LayoutStyle.Computed"/>
        /// positioning.
        /// </summary>
        public TextValidateField () : this (null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextValidateField"/> class using <see cref="LayoutStyle.Computed"/>
        /// positioning.
        /// </summary>
        public TextValidateField (ITextValidateProvider provider) {
            if (provider != null) {
                Provider = provider;
            }

            SetInitialProperties ();
        }

        void SetInitialProperties () {
            Height = 1;
            CanFocus = true;

            // Things this view knows how to do
            AddCommand (
                        Command.LeftHome,
                        () => {
                            HomeKeyHandler ();

                            return true;
                        });
            AddCommand (
                        Command.RightEnd,
                        () => {
                            EndKeyHandler ();

                            return true;
                        });
            AddCommand (
                        Command.DeleteCharRight,
                        () => {
                            DeleteKeyHandler ();

                            return true;
                        });
            AddCommand (
                        Command.DeleteCharLeft,
                        () => {
                            BackspaceKeyHandler ();

                            return true;
                        });
            AddCommand (
                        Command.Left,
                        () => {
                            CursorLeft ();

                            return true;
                        });
            AddCommand (
                        Command.Right,
                        () => {
                            CursorRight ();

                            return true;
                        });

            // Default keybindings for this view
            KeyBindings.Add (KeyCode.Home, Command.LeftHome);
            KeyBindings.Add (KeyCode.End, Command.RightEnd);

            KeyBindings.Add (KeyCode.Delete, Command.DeleteCharRight);
            KeyBindings.Add (KeyCode.Delete, Command.DeleteCharRight);

            KeyBindings.Add (KeyCode.Backspace, Command.DeleteCharLeft);
            KeyBindings.Add (KeyCode.CursorLeft, Command.Left);
            KeyBindings.Add (KeyCode.CursorRight, Command.Right);
        }

        /// <summary>
        /// Provider
        /// </summary>
        public ITextValidateProvider Provider {
            get => _provider;
            set {
                _provider = value;
                if (_provider.Fixed) {
                    this.Width = _provider.DisplayText == string.Empty ? _defaultLength : _provider.DisplayText.Length;
                }

                // HomeKeyHandler already call SetNeedsDisplay
                HomeKeyHandler ();
            }
        }

        ///<inheritdoc/>
        public override bool MouseEvent (MouseEvent mouseEvent) {
            if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)) {
                var c = _provider.Cursor (mouseEvent.X - GetMargins (Bounds.Width).left);
                if (_provider.Fixed == false && TextAlignment == TextAlignment.Right && Text.Length > 0) {
                    c++;
                }

                _cursorPosition = c;
                SetFocus ();
                SetNeedsDisplay ();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Text
        /// </summary>
        public new string Text {
            get {
                if (_provider == null) {
                    return string.Empty;
                }

                return _provider.Text;
            }
            set {
                if (_provider == null) {
                    return;
                }

                _provider.Text = value;

                SetNeedsDisplay ();
            }
        }

        ///<inheritdoc/>
        public override void PositionCursor () {
            var (left, _) = GetMargins (Bounds.Width);

            // Fixed = true, is for inputs that have fixed width, like masked ones.
            // Fixed = false, is for normal input.
            // When it's right-aligned and it's a normal input, the cursor behaves differently.
            int curPos;
            if (_provider?.Fixed == false && TextAlignment == TextAlignment.Right) {
                curPos = _cursorPosition + left - 1;
                Move (curPos, 0);
            } else {
                curPos = _cursorPosition + left;
                Move (curPos, 0);
            }

            if (curPos < 0 || curPos >= Bounds.Width) {
                Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);
            } else {
                Application.Driver.SetCursorVisibility (CursorVisibility.Default);
            }
        }

        /// <summary>
        /// Margins for text alignment.
        /// </summary>
        /// <param name="width">Total width</param>
        /// <returns>Left and right margins</returns>
        (int left, int right) GetMargins (int width) {
            var count = Text.Length;
            var total = width - count;
            switch (TextAlignment) {
                case TextAlignment.Left:
                    return (0, total);
                case TextAlignment.Centered:
                    return (total / 2, (total / 2) + (total % 2));
                case TextAlignment.Right:
                    return (total, 0);
                default:
                    return (0, total);
            }
        }

        ///<inheritdoc/>
        public override void OnDrawContent (Rect contentArea) {
            if (_provider == null) {
                Move (0, 0);
                Driver.AddStr ("Error: ITextValidateProvider not set!");

                return;
            }

            var bgcolor = !IsValid ? new Color (Color.BrightRed) : ColorScheme.Focus.Background;
            var textColor = new Attribute (ColorScheme.Focus.Foreground, bgcolor);

            var (margin_left, margin_right) = GetMargins (Bounds.Width);

            Move (0, 0);

            // Left Margin
            Driver.SetAttribute (textColor);
            for (int i = 0; i < margin_left; i++) {
                Driver.AddRune ((Rune)' ');
            }

            // Content
            Driver.SetAttribute (textColor);

            // Content
            for (int i = 0; i < _provider.DisplayText.Length; i++) {
                Driver.AddRune ((Rune)_provider.DisplayText[i]);
            }

            // Right Margin
            Driver.SetAttribute (textColor);
            for (int i = 0; i < margin_right; i++) {
                Driver.AddRune ((Rune)' ');
            }
        }

        /// <summary>
        /// Try to move the cursor to the left.
        /// </summary>
        /// <returns>True if moved.</returns>
        bool CursorLeft () {
            var current = _cursorPosition;
            _cursorPosition = _provider.CursorLeft (_cursorPosition);
            SetNeedsDisplay ();

            return current != _cursorPosition;
        }

        /// <summary>
        /// Try to move the cursor to the right.
        /// </summary>
        /// <returns>True if moved.</returns>
        bool CursorRight () {
            var current = _cursorPosition;
            _cursorPosition = _provider.CursorRight (_cursorPosition);
            SetNeedsDisplay ();

            return current != _cursorPosition;
        }

        /// <summary>
        /// Delete char at cursor position - 1, moving the cursor.
        /// </summary>
        /// <returns></returns>
        bool BackspaceKeyHandler () {
            if (_provider.Fixed == false && TextAlignment == TextAlignment.Right && _cursorPosition <= 1) {
                return false;
            }

            _cursorPosition = _provider.CursorLeft (_cursorPosition);
            _provider.Delete (_cursorPosition);
            SetNeedsDisplay ();

            return true;
        }

        /// <summary>
        /// Deletes char at current position.
        /// </summary>
        /// <returns></returns>
        bool DeleteKeyHandler () {
            if (_provider.Fixed == false && TextAlignment == TextAlignment.Right) {
                _cursorPosition = _provider.CursorLeft (_cursorPosition);
            }

            _provider.Delete (_cursorPosition);
            SetNeedsDisplay ();

            return true;
        }

        /// <summary>
        /// Moves the cursor to first char.
        /// </summary>
        /// <returns></returns>
        bool HomeKeyHandler () {
            _cursorPosition = _provider.CursorStart ();
            SetNeedsDisplay ();

            return true;
        }

        /// <summary>
        /// Moves the cursor to the last char.
        /// </summary>
        /// <returns></returns>
        bool EndKeyHandler () {
            _cursorPosition = _provider.CursorEnd ();
            SetNeedsDisplay ();

            return true;
        }

        ///<inheritdoc/>
        public override bool OnProcessKeyDown (Key a) {
            if (_provider == null) {
                return false;
            }

            if (a.AsRune == default) {
                return false;
            }

            var key = a.AsRune;

            var inserted = _provider.InsertAt ((char)key.Value, _cursorPosition);

            if (inserted) {
                CursorRight ();
            }

            return false;
        }

        /// <summary>
        /// This property returns true if the input is valid.
        /// </summary>
        public virtual bool IsValid {
            get {
                if (_provider == null) {
                    return false;
                }

                return _provider.IsValid;
            }
        }

        ///<inheritdoc/>
        public override bool OnEnter (View view) {
            Application.Driver.SetCursorVisibility (CursorVisibility.Default);

            return base.OnEnter (view);
        }

        ///<inheritdoc/>
        public override bool OnLeave (View view) {
            Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

            return base.OnLeave (view);
        }
    }
}
