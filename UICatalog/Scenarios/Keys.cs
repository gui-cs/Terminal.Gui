using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata (Name: "Keys", Description: "Shows keyboard input handling.")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Keys : Scenario {

	public override void Setup ()
	{
		List<string> onKeyPressedList = new List<string> ();
		List<string> onInvokingKeyBindingsList = new List<string> ();

		var editLabel = new Label ("Type text here:") {
			X = 0,
			Y = 0,
		};
		Win.Add (editLabel);

		var edit = new TextField ("") {
			X = Pos.Right (editLabel) + 1,
			Y = Pos.Top (editLabel),
			Width = Dim.Fill (2),
		};
		Win.Add (edit);

		edit.KeyPressed += (s, a) => {
			onKeyPressedList.Add (a.ToString ());
		};


		edit.InvokingKeyBindings += (s, a) => {
			if (edit.ContainsKeyBinding (a.Key)) {
				var cmds = edit.GetKeyBindings (a.Key);
				onInvokingKeyBindingsList.Add ($"{a}: {string.Join (",", cmds)}");
			}
		};

		// Last KeyPress: ______
		var keyPressedLabel = new Label ("Last Application.KeyPressed:") {
			X = Pos.Left (editLabel),
			Y = Pos.Top (editLabel) + 2,
		};
		Win.Add (keyPressedLabel);
		var labelKeypress = new Label ("") {
			X = Pos.Right (keyPressedLabel) + 1,
			Y = Pos.Top (keyPressedLabel),
			TextAlignment = Terminal.Gui.TextAlignment.Centered,
			ColorScheme = Colors.Error,
			AutoSize = true
		};
		Win.Add (labelKeypress);

		Win.KeyPressed += (s, e) => labelKeypress.Text = e.ToString ();

		// Key stroke log:
		var keyLogLabel = new Label ("Key Events:") {
			X = Pos.Left (editLabel),
			Y = Pos.Top (editLabel) + 4,
		};
		Win.Add (keyLogLabel);
		var maxKeyString = KeyEventArgs.ToString (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.CursorRight, MenuBar.ShortcutDelimiter).Length;
		var yOffset = (Application.Top == Application.Top ? 1 : 6);
		var keyEventlist = new List<string> ();
		var keyEventListView = new ListView (keyEventlist) {
			X = 0,
			Y = Pos.Top (keyLogLabel) + yOffset,
			Width = "Key Pressed:".Length + maxKeyString,
			Height = Dim.Fill (),
		};
		keyEventListView.ColorScheme = Colors.TopLevel;
		Win.Add (keyEventListView);

		// OnKeyPressed
		var onKeyPressedLabel = new Label ("OnKeyPressed:") {
			X = Pos.Right (keyEventListView) + 1,
			Y = Pos.Top (editLabel) + 4,
		};
		Win.Add (onKeyPressedLabel);

		yOffset = (Application.Top == Application.Top ? 1 : 6);
		var onKeyPressedListView = new ListView (onKeyPressedList) {
			X = Pos.Left (onKeyPressedLabel),
			Y = Pos.Top (onKeyPressedLabel) + yOffset,
			Width = maxKeyString,
			Height = Dim.Fill (),
		};
		onKeyPressedListView.ColorScheme = Colors.TopLevel;
		Win.Add (onKeyPressedListView);


		// OnInvokeKeyBindings
		var onInvokingKeyBindingsLabel = new Label ("OnInvokingKeyBindings:") {
			X = Pos.Right (onKeyPressedListView) + 1,
			Y = Pos.Top (editLabel) + 4,
		};
		Win.Add (onInvokingKeyBindingsLabel);
		var onInvokingKeyBindingsListView = new ListView (onInvokingKeyBindingsList) {
			X = Pos.Left (onInvokingKeyBindingsLabel),
			Y = Pos.Top (onInvokingKeyBindingsLabel) + yOffset,
			Width = Dim.Fill(1),
			Height = Dim.Fill (),
		};
		onInvokingKeyBindingsListView.ColorScheme = Colors.TopLevel;
		Win.Add (onInvokingKeyBindingsListView);

		Application.KeyDown += (s, a) => KeyDownPressUp (a, "Down");
		Application.KeyPressed += (s, a) => KeyDownPressUp (a, "Pressed");
		Application.KeyUp += (s, a) => KeyDownPressUp (a, "Up");

		void KeyDownPressUp (KeyEventArgs args, string updown)
		{
			// BUGBUG: KeyEvent.ToString is badly broken
			var msg = $"Key{updown,-7}: {args}";
			keyEventlist.Add (msg);
			keyEventListView.MoveDown ();
			onKeyPressedListView.MoveDown ();
			onInvokingKeyBindingsListView.MoveDown ();
		}
	}
}