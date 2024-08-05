using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using JetBrains.Annotations;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Buttons", "Demonstrates all sorts of Buttons.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
public class Buttons : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window main = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        // Add a label & text field so we can demo IsDefault
        var editLabel = new Label { X = 0, Y = 0, Text = "TextField (to demo IsDefault):" };
        main.Add (editLabel);

        // Add a TextField using Absolute layout. 
        var edit = new TextField { X = 31, Width = 15, HotKey = Key.Y.WithAlt };
        main.Add (edit);

        // This is the default button (IsDefault = true); if user presses ENTER in the TextField
        // the scenario will quit
        var defaultButton = new Button { X = Pos.Center (), Y = Pos.AnchorEnd (), IsDefault = true, Text = "_Quit" };
        defaultButton.Accept += (s, e) => Application.RequestStop ();
        main.Add (defaultButton);

        var swapButton = new Button
        {
            X = 50,
            Width = 45,
            Height = 3,
            Text = "S_wap Default (Size = 45, 3)",
            ColorScheme = Colors.ColorSchemes ["Error"]
        };

        swapButton.Accept += (s, e) =>
                             {
                                 defaultButton.IsDefault = !defaultButton.IsDefault;
                                 swapButton.IsDefault = !swapButton.IsDefault;
                             };
        main.Add (swapButton);

        static void DoMessage (Button button, string txt)
        {
            button.Accept += (s, e) =>
                             {
                                 string btnText = button.Text;
                                 MessageBox.Query ("Message", $"Did you click {txt}?", "Yes", "No");
                             };
        }

        var colorButtonsLabel = new Label { X = 0, Y = Pos.Bottom (swapButton) + 1, Text = "Color Buttons: " };
        main.Add (colorButtonsLabel);

        View prev = colorButtonsLabel;

        foreach (KeyValuePair<string, ColorScheme> colorScheme in Colors.ColorSchemes)
        {
            var colorButton = new Button
            {
                X = Pos.Right (prev),
                Y = Pos.Y (colorButtonsLabel),
                Text = $"_{colorScheme.Key}",
                ColorScheme = colorScheme.Value,
            };
            DoMessage (colorButton, colorButton.Text);
            main.Add (colorButton);
            prev = colorButton;
        }

        Button button;

        main.Add (
                  button = new ()
                  {
                      X = 2,
                      Y = Pos.Bottom (colorButtonsLabel) + 1,
                      Text =
                          "A super l_öng Button that will probably expose a bug in clipping or wrapping of text. Will it?"
                  }
                 );
        DoMessage (button, button.Text);

        // Note the 'N' in 'Newline' will be the hotkey
        main.Add (
                  button = new () { X = 2, Y = Pos.Bottom (button) + 1, Height = 2, Text = "a Newline\nin the button" }
                 );
        button.Accept += (s, e) => MessageBox.Query ("Message", "Question?", "Yes", "No");

        var textChanger = new Button { X = 2, Y = Pos.Bottom (button) + 1, Text = "Te_xt Changer" };
        main.Add (textChanger);
        textChanger.Accept += (s, e) => textChanger.Text += "!";

        main.Add (
                  button = new ()
                  {
                      X = Pos.Right (textChanger) + 2,
                      Y = Pos.Y (textChanger),
                      Text = "Lets see if this will move as \"Text Changer\" grows"
                  }
                 );

        var removeButton = new Button
        {
            X = 2, Y = Pos.Bottom (button) + 1,
            ColorScheme = Colors.ColorSchemes ["Error"], Text = "Remove this button"
        };
        main.Add (removeButton);

        // This in interesting test case because `moveBtn` and below are laid out relative to this one!
        removeButton.Accept += (s, e) =>
                               {
                                   removeButton.Visible = false;
                               };

        var computedFrame = new FrameView
        {
            X = 0,
            Y = Pos.Bottom (removeButton) + 1,
            Width = Dim.Percent (50),
            Height = 6,
            Title = "Computed Layout"
        };
        main.Add (computedFrame);

        // Demonstrates how changing the View.Frame property can move Views
        var moveBtn = new Button
        {
            X = 0,
            Y = Pos.Center () - 1,
            Width = 30,
            ColorScheme = Colors.ColorSchemes ["Error"],
            Text = "Move This \u263b Button v_ia Pos"
        };

        moveBtn.Accept += (s, e) =>
                          {
                              moveBtn.X = moveBtn.Frame.X + 5;
                          };
        computedFrame.Add (moveBtn);

        // Demonstrates how changing the View.Frame property can SIZE Views (#583)
        var sizeBtn = new Button
        {
            Y = Pos.Center () + 1,
            X = 0,
            Width = 30,
            Text = "Grow This \u263a Button _via Pos",
            ColorScheme = Colors.ColorSchemes ["Error"],
        };

        sizeBtn.Accept += (s, e) =>
                          {
                              sizeBtn.Width = sizeBtn.Frame.Width + 5;
                          };
        computedFrame.Add (sizeBtn);

        var absoluteFrame = new FrameView
        {
            X = Pos.Right (computedFrame),
            Y = Pos.Bottom (removeButton) + 1,
            Width = Dim.Fill (),
            Height = 6,
            Title = "Absolute Layout"
        };
        main.Add (absoluteFrame);

        // Demonstrates how changing the View.Frame property can move Views
        var moveBtnA = new Button { ColorScheme = Colors.ColorSchemes ["Error"], Text = "Move This Button via Frame" };

        moveBtnA.Accept += (s, e) =>
                           {
                               moveBtnA.Frame = new (
                                                     moveBtnA.Frame.X + 5,
                                                     moveBtnA.Frame.Y,
                                                     moveBtnA.Frame.Width,
                                                     moveBtnA.Frame.Height
                                                    );
                           };
        absoluteFrame.Add (moveBtnA);

        // Demonstrates how changing the View.Frame property can SIZE Views (#583)
        var sizeBtnA = new Button
        {
            Y = 2, ColorScheme = Colors.ColorSchemes ["Error"], Text = " ~  s  gui.cs   master ↑_10 = Сохранить"
        };

        sizeBtnA.Accept += (s, e) =>
                           {
                               sizeBtnA.Frame = new (
                                                     sizeBtnA.Frame.X,
                                                     sizeBtnA.Frame.Y,
                                                     sizeBtnA.Frame.Width + 5,
                                                     sizeBtnA.Frame.Height
                                                    );
                           };
        absoluteFrame.Add (sizeBtnA);

        var label = new Label
        {
            X = 2, Y = Pos.Bottom (computedFrame) + 1, 
            Text = "Text Alignment (changes the four buttons above): "
        };
        main.Add (label);

        var radioGroup = new RadioGroup
        {
            X = 4,
            Y = Pos.Bottom (label) + 1,
            SelectedItem = 2,
            RadioLabels = new [] { "Start", "End", "Center", "Fill" }
        };
        main.Add (radioGroup);

        // Demo changing hotkey
        string MoveHotkey (string txt)
        {
            // Remove the '_'
            List<Rune> runes = txt.ToRuneList ();

            int i = runes.IndexOf ((Rune)'_');
            var start = "";

            if (i > -1)
            {
                start = StringExtensions.ToString (runes.GetRange (0, i));
            }

            txt = start + StringExtensions.ToString (runes.GetRange (i + 1, runes.Count - (i + 1)));

            runes = txt.ToRuneList ();

            // Move over one or go to start
            i++;

            if (i >= runes.Count)
            {
                i = 0;
            }

            // Slip in the '_'
            start = StringExtensions.ToString (runes.GetRange (0, i));

            return start + '_' + StringExtensions.ToString (runes.GetRange (i, runes.Count - i));
        }

        var mhkb = "Click to Change th_is Button's Hotkey";

        var moveHotKeyBtn = new Button
        {
            X = 2,
            Y = Pos.Bottom (radioGroup) + 1,
            Width = Dim.Width (computedFrame) - 2,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Text = mhkb
        };
        moveHotKeyBtn.Accept += (s, e) => { moveHotKeyBtn.Text = MoveHotkey (moveHotKeyBtn.Text); };
        main.Add (moveHotKeyBtn);

        var muhkb = " ~  s  gui.cs   master ↑10 = Сохранить";

        var moveUnicodeHotKeyBtn = new Button
        {
            X = Pos.Left (absoluteFrame) + 1,
            Y = Pos.Bottom (radioGroup) + 1,
            Width = Dim.Width (absoluteFrame) - 2,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Text = muhkb
        };
        moveUnicodeHotKeyBtn.Accept += (s, e) => { moveUnicodeHotKeyBtn.Text = MoveHotkey (moveUnicodeHotKeyBtn.Text); };
        main.Add (moveUnicodeHotKeyBtn);

        radioGroup.SelectedItemChanged += (s, args) =>
                                          {
                                              switch (args.SelectedItem)
                                              {
                                                  case 0:
                                                      moveBtn.TextAlignment = Alignment.Start;
                                                      sizeBtn.TextAlignment = Alignment.Start;
                                                      moveBtnA.TextAlignment = Alignment.Start;
                                                      sizeBtnA.TextAlignment = Alignment.Start;
                                                      moveHotKeyBtn.TextAlignment = Alignment.Start;
                                                      moveUnicodeHotKeyBtn.TextAlignment = Alignment.Start;

                                                      break;
                                                  case 1:
                                                      moveBtn.TextAlignment = Alignment.End;
                                                      sizeBtn.TextAlignment = Alignment.End;
                                                      moveBtnA.TextAlignment = Alignment.End;
                                                      sizeBtnA.TextAlignment = Alignment.End;
                                                      moveHotKeyBtn.TextAlignment = Alignment.End;
                                                      moveUnicodeHotKeyBtn.TextAlignment = Alignment.End;

                                                      break;
                                                  case 2:
                                                      moveBtn.TextAlignment = Alignment.Center;
                                                      sizeBtn.TextAlignment = Alignment.Center;
                                                      moveBtnA.TextAlignment = Alignment.Center;
                                                      sizeBtnA.TextAlignment = Alignment.Center;
                                                      moveHotKeyBtn.TextAlignment = Alignment.Center;
                                                      moveUnicodeHotKeyBtn.TextAlignment = Alignment.Center;

                                                      break;
                                                  case 3:
                                                      moveBtn.TextAlignment = Alignment.Fill;
                                                      sizeBtn.TextAlignment = Alignment.Fill;
                                                      moveBtnA.TextAlignment = Alignment.Fill;
                                                      sizeBtnA.TextAlignment = Alignment.Fill;
                                                      moveHotKeyBtn.TextAlignment = Alignment.Fill;
                                                      moveUnicodeHotKeyBtn.TextAlignment = Alignment.Fill;

                                                      break;
                                              }
                                          };

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (moveUnicodeHotKeyBtn) + 1,
            Title = "_Numeric Up/Down (press-and-hold):",
        };

        var numericUpDown = new NumericUpDown<int>
        {
            Value = 69,
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = 5,
            Height = 1
        };
        numericUpDown.ValueChanged += NumericUpDown_ValueChanged;

        void NumericUpDown_ValueChanged (object sender, EventArgs<int> e) { }

        main.Add (label, numericUpDown);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (numericUpDown) + 1,
            Title = "_No Repeat:"
        };
        var noRepeatAcceptCount = 0;

        var noRepeatButton = new Button
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Title = $"Accept Cou_nt: {noRepeatAcceptCount}",
            WantContinuousButtonPressed = false
        };
        noRepeatButton.Accept += (s, e) => { noRepeatButton.Title = $"Accept Cou_nt: {++noRepeatAcceptCount}"; };
        main.Add (label, noRepeatButton);

        label = new ()
        {
            X = 0,
            Y = Pos.Bottom (label) + 1,
            Title = "_Repeat (press-and-hold):"
        };
        var acceptCount = 0;

        var repeatButton = new Button
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Title = $"Accept Co_unt: {acceptCount}",
            WantContinuousButtonPressed = true
        };
        repeatButton.Accept += (s, e) => { repeatButton.Title = $"Accept Co_unt: {++acceptCount}"; };

        var enableCB = new CheckBox
        {
            X = Pos.Right (repeatButton) + 1,
            Y = Pos.Top (repeatButton),
            Title = "Enabled",
            State = CheckState.Checked
        };
        enableCB.Toggle += (s, e) => { repeatButton.Enabled = !repeatButton.Enabled; };
        main.Add (label, repeatButton, enableCB);

        main.Ready += (s, e) => radioGroup.Refresh ();
        Application.Run (main);
        main.Dispose ();
        Application.Shutdown ();
    }

    /// <summary>
    /// Enables the user to increase or decrease a value by clicking on the up or down buttons.
    /// </summary>
    /// <remarks>
    ///     Supports the following types: <see cref="int"/>, <see cref="long"/>, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>.
    ///     Supports only one digit of precision.
    /// </remarks>
    public class NumericUpDown<T> : View
    {
        private readonly Button _down;
        // TODO: Use a TextField instead of a Label
        private readonly View _number;
        private readonly Button _up;

        public NumericUpDown ()
        {
            Type type = typeof (T);
            if (!(type == typeof (int) || type == typeof (long) || type == typeof (float) || type == typeof (double) || type == typeof (decimal)))
            {
                throw new InvalidOperationException ("T must be a numeric type that supports addition and subtraction.");
            }

            Width = Dim.Auto (DimAutoStyle.Content); //Dim.Function (() => Digits + 2); // button + 3 for number + button
            Height = Dim.Auto (DimAutoStyle.Content);

            _down = new ()
            {
                Height = 1,
                Width = 1,
                NoPadding = true,
                NoDecorations = true,
                Title = $"{CM.Glyphs.DownArrow}",
                WantContinuousButtonPressed = true,
                CanFocus = false,
                ShadowStyle = ShadowStyle.None,
            };

            _number = new ()
            {
                Text = Value.ToString (),
                X = Pos.Right (_down),
                Y = Pos.Top (_down),
                Width = Dim.Func (() => Digits),
                Height = 1,
                TextAlignment = Alignment.Center,
                CanFocus = true
            };

            _up = new ()
            {
                X = Pos.AnchorEnd (),
                Y = Pos.Top (_number),
                Height = 1,
                Width = 1,
                NoPadding = true,
                NoDecorations = true,
                Title = $"{CM.Glyphs.UpArrow}",
                WantContinuousButtonPressed = true,
                CanFocus = false,
                ShadowStyle = ShadowStyle.None,
            };

            CanFocus = true;

            _down.Accept += OnDownButtonOnAccept;
            _up.Accept += OnUpButtonOnAccept;

            Add (_down, _number, _up);


            AddCommand (Command.ScrollUp, () =>
                                          {
                                              Value = (dynamic)Value + 1;
                                              _number.Text = Value.ToString ();

                                              return true;
                                          });
            AddCommand (Command.ScrollDown, () =>
                                            {
                                                Value = (dynamic)Value - 1;
                                                _number.Text = Value.ToString ();

                                                return true;
                                            });

            KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
            KeyBindings.Add (Key.CursorDown, Command.ScrollDown);

            return;

            void OnDownButtonOnAccept (object s, HandledEventArgs e)
            {
                InvokeCommand (Command.ScrollDown);
            }

            void OnUpButtonOnAccept (object s, HandledEventArgs e)
            {
                InvokeCommand (Command.ScrollUp);
            }
        }

        private void _up_Enter (object sender, FocusEventArgs e)
        {
            throw new NotImplementedException ();
        }

        private T _value;

        /// <summary>
        /// The value that will be incremented or decremented.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (_value.Equals (value))
                {
                    return;
                }

                T oldValue = value;
                CancelEventArgs<T> args = new (ref _value, ref value);
                ValueChanging?.Invoke (this, args);

                if (args.Cancel)
                {
                    return;
                }

                _value = value;
                _number.Text = _value.ToString ()!;
                ValueChanged?.Invoke (this, new (in _value));
            }
        }

        /// <summary>
        /// Fired when the value is about to change. Set <see cref="CancelEventArgs{T}.Cancel"/> to true to prevent the change.
        /// </summary>
        [CanBeNull]
        public event EventHandler<CancelEventArgs<T>> ValueChanging;

        /// <summary>
        /// Fired when the value has changed.
        /// </summary>
        [CanBeNull]
        public event EventHandler<EventArgs<T>> ValueChanged;

        /// <summary>
        /// The number of digits to display. The <see cref="View.Viewport"/> will be resized to fit this number of characters plus the buttons. The default is 3.
        /// </summary>
        public int Digits { get; set; } = 3;
    }
}

