using System.Text;
using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Keys", Description: "Shows keyboard input handling.")]
	[ScenarioCategory ("Mouse and Keyboard")]
	public class Keys : Scenario {

		class TestWindow : Window {
			public List<string> _onKeyPressedList = new List<string> ();
			public List<string> _processHotKeyList = new List<string> ();
			public List<string> _processColdKeyList = new List<string> ();

			public override bool OnKeyPressed (KeyEventArgs arg)
			{
				_onKeyPressedList.Add (arg.ToString ());
				return base.OnKeyPressed (arg);
			}

			public override bool ProcessHotKey (KeyEventArgs keyEvent)
			{
				_processHotKeyList.Add (keyEvent.ToString ());
				return base.ProcessHotKey (keyEvent);
			}

			public override bool ProcessColdKey (KeyEventArgs keyEvent)
			{
				_processColdKeyList.Add (keyEvent.ToString ());

				return base.ProcessColdKey (keyEvent);
			}
		}

		public override void Init ()
		{
			Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();

			Win = new TestWindow () {
				Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = Colors.ColorSchemes [TopLevelColorScheme],
			};
			Application.Top.Add (Win);
		}

		public override void Setup ()
		{
			// Type text here: ______
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

			// Last KeyPress: ______
			var appKeyPressedLabel = new Label ("Last Application.KeyPress:") {
				X = Pos.Left (editLabel),
				Y = Pos.Top (editLabel) + 2,
			};
			Win.Add (appKeyPressedLabel);
			var labelAppKeyPressed = new Label ("") {
				X = Pos.Right (appKeyPressedLabel) + 1,
				Y = Pos.Top (appKeyPressedLabel),
				TextAlignment = Terminal.Gui.TextAlignment.Centered,
				ColorScheme = Colors.Error,
				AutoSize = true
			};
			Win.Add (labelAppKeyPressed);

			Application.KeyPressed += (s, e) => labelAppKeyPressed.Text = e.ToString ();

			// Key event log (for Application):

			var keyLogLabel = new Label ("All events:") {
				X = Pos.Left (editLabel),
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (keyLogLabel);
			var fakeKeyPress = new KeyEventArgs (Key.CtrlMask | Key.A, new KeyModifiers () {
				Alt = true,
				Ctrl = true,
				Shift = true
			});
			var maxLogEntry = $"Key{"",-5}: {fakeKeyPress}".Length;
			var yOffset = (Application.Top == Application.Top ? 1 : 6);
			var keyEventlist = new List<string> ();
			var keyEventListView = new ListView (keyEventlist) {
				X = 0,
				Y = Pos.Top (keyLogLabel) + yOffset,
				Width = Dim.Percent (30),
				Height = Dim.Fill (),
			};
			keyEventListView.ColorScheme = Colors.TopLevel;
			Win.Add (keyEventListView);

			// ProcessKey log:
			var processKeyLogLabel = new Label ("Win.OnKeyPressed:") {
				X = Pos.Right (keyEventListView) + 1,
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (processKeyLogLabel);

			maxLogEntry = $"{fakeKeyPress}".Length;
			yOffset = (Application.Top == Application.Top ? 1 : 6);
			var processKeyListView = new ListView (((TestWindow)Win)._onKeyPressedList) {
				X = Pos.Left (processKeyLogLabel),
				Y = Pos.Top (processKeyLogLabel) + yOffset,
				Width = Dim.Percent (30),
				Height = Dim.Fill (),
			};
			processKeyListView.ColorScheme = Colors.TopLevel;
			Win.Add (processKeyListView);

			// ProcessHotKey log:
			// BUGBUG: Label is not positioning right with Pos, so using TextField instead
			var processHotKeyLogLabel = new Label ("Win.ProcessHotKey:") {
				X = Pos.Right (processKeyListView) + 1,
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (processHotKeyLogLabel);

			yOffset = (Application.Top == Application.Top ? 1 : 6);
			var processHotKeyListView = new ListView (((TestWindow)Win)._processHotKeyList) {
				X = Pos.Left (processHotKeyLogLabel),
				Y = Pos.Top (processHotKeyLogLabel) + yOffset,
				Width = Dim.Percent (20),
				Height = Dim.Fill (),
			};
			processHotKeyListView.ColorScheme = Colors.TopLevel;
			Win.Add (processHotKeyListView);

			// ProcessColdKey log:
			// BUGBUG: Label is not positioning right with Pos, so using TextField instead
			var processColdKeyLogLabel = new Label ("Win.ProcessColdKey:") {
				X = Pos.Right (processHotKeyListView) + 1,
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (processColdKeyLogLabel);

			yOffset = (Application.Top == Application.Top ? 1 : 6);
			var processColdKeyListView = new ListView (((TestWindow)Win)._processColdKeyList) {
				X = Pos.Left (processColdKeyLogLabel),
				Y = Pos.Top (processColdKeyLogLabel) + yOffset,
				Width = Dim.Percent (20),
				Height = Dim.Fill (),
			};

			void KeyDownPressUp (KeyEventArgs args, string updown)
			{
				// BUGBUG: KeyEvent.ToString is badly broken
				var msg = $"Key{updown,-5}: {args}";
				keyEventlist.Add (msg);
				keyEventListView.MoveDown ();
				processKeyListView.MoveDown ();
				processColdKeyListView.MoveDown ();
				processHotKeyListView.MoveDown ();
			}

			Application.KeyDown += (s, a) => KeyDownPressUp (a, "Down");
			Application.KeyPressed += (s, a) => KeyDownPressUp (a, "Press");
			Application.KeyUp += (s, a) => KeyDownPressUp (a, "Up");

			processColdKeyListView.ColorScheme = Colors.TopLevel;
			Win.Add (processColdKeyListView);
		}
	}
}