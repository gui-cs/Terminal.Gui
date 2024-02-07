using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Keys", "Shows keyboard input handling.")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Keys : Scenario {
    public override void Setup () {
        List<string> keyPressedList = new List<string> ();
        List<string> invokingKeyBindingsList = new List<string> ();

        var editLabel = new Label {
                                      X = 0,
                                      Y = 0,
                                      Text = "Type text here:"
                                  };
        Win.Add (editLabel);

        var edit = new TextField {
                                     X = Pos.Right (editLabel) + 1,
                                     Y = Pos.Top (editLabel),
                                     Width = Dim.Fill (2)
                                 };
        Win.Add (edit);

        edit.KeyDown += (s, a) => { keyPressedList.Add (a.ToString ()); };

        edit.InvokingKeyBindings += (s, a) => {
            if (edit.KeyBindings.TryGet (a, out KeyBinding binding)) {
                invokingKeyBindingsList.Add ($"{a}: {string.Join (",", binding.Commands)}");
            }
        };

        // Last KeyPress: ______
        var keyPressedLabel = new Label {
                                            X = Pos.Left (editLabel), Y = Pos.Top (editLabel) + 1,
                                            Text = "Last TextView.KeyPressed:"
                                        };
        Win.Add (keyPressedLabel);
        var labelTextViewKeypress = new Label { X = Pos.Right (keyPressedLabel) + 1, Y = Pos.Top (keyPressedLabel) };
        Win.Add (labelTextViewKeypress);

        edit.KeyDown += (s, e) => labelTextViewKeypress.Text = e.ToString ();

        keyPressedLabel = new Label {
                                        X = Pos.Left (keyPressedLabel),
                                        Y = Pos.Bottom (keyPressedLabel),
                                        Text = "Last Application.KeyDown:"
                                    };
        Win.Add (keyPressedLabel);
        var labelAppKeypress = new Label { X = Pos.Right (keyPressedLabel) + 1, Y = Pos.Top (keyPressedLabel) };
        Win.Add (labelAppKeypress);

        Application.KeyDown += (s, e) => labelAppKeypress.Text = e.ToString ();

        // Key stroke log:
        var keyLogLabel = new Label {
                                        X = Pos.Left (editLabel), Y = Pos.Top (editLabel) + 4,
                                        Text = "Application Key Events:"
                                    };
        Win.Add (keyLogLabel);
        int maxKeyString = Key.CursorRight.WithAlt.WithCtrl.WithShift.ToString ().Length;
        var yOffset = 1;
        List<string> keyEventlist = new List<string> ();
        var keyEventListView = new ListView {
                                                X = 0,
                                                Y = Pos.Top (keyLogLabel) + yOffset,
                                                Width = "Key Down:".Length + maxKeyString,
                                                Height = Dim.Fill (),
                                                Source = new ListWrapper (keyEventlist)
                                            };
        keyEventListView.ColorScheme = Colors.ColorSchemes["TopLevel"];
        Win.Add (keyEventListView);

        // OnKeyPressed
        var onKeyPressedLabel = new Label {
                                              X = Pos.Right (keyEventListView) + 1,
                                              Y = Pos.Top (editLabel) + 4,
                                              Text = "TextView KeyDown:"
                                          };
        Win.Add (onKeyPressedLabel);

        yOffset = 1;
        var onKeyPressedListView = new ListView {
                                                    X = Pos.Left (onKeyPressedLabel),
                                                    Y = Pos.Top (onKeyPressedLabel) + yOffset,
                                                    Width = maxKeyString,
                                                    Height = Dim.Fill (),
                                                    Source = new ListWrapper (keyPressedList)
                                                };
        onKeyPressedListView.ColorScheme = Colors.ColorSchemes["TopLevel"];
        Win.Add (onKeyPressedListView);

        // OnInvokeKeyBindings
        var onInvokingKeyBindingsLabel = new Label {
                                                       X = Pos.Right (onKeyPressedListView) + 1,
                                                       Y = Pos.Top (editLabel) + 4,
                                                       Text = "TextView InvokingKeyBindings:"
                                                   };
        Win.Add (onInvokingKeyBindingsLabel);
        var onInvokingKeyBindingsListView = new ListView {
                                                             X = Pos.Left (onInvokingKeyBindingsLabel),
                                                             Y = Pos.Top (onInvokingKeyBindingsLabel) + yOffset,
                                                             Width = Dim.Fill (1),
                                                             Height = Dim.Fill (),
                                                             Source = new ListWrapper (invokingKeyBindingsList)
                                                         };
        onInvokingKeyBindingsListView.ColorScheme = Colors.ColorSchemes["TopLevel"];
        Win.Add (onInvokingKeyBindingsListView);

        //Application.KeyDown += (s, a) => KeyDownPressUp (a, "Down");
        Application.KeyDown += (s, a) => KeyDownPressUp (a, "Down");
        Application.KeyUp += (s, a) => KeyDownPressUp (a, "Up");

        void KeyDownPressUp (Key args, string updown) {
            // BUGBUG: KeyEvent.ToString is badly broken
            var msg = $"Key{updown,-7}: {args}";
            keyEventlist.Add (msg);
            keyEventListView.MoveDown ();
            onKeyPressedListView.MoveDown ();
            onInvokingKeyBindingsListView.MoveDown ();
        }
    }
}
