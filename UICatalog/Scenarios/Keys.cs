using NStack;
using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Keys", Description: "Shows how to handle keyboard input")]
	[ScenarioCategory ("Input")]
	class Keys : Scenario {

		static List<string> _processKeyList = new List<string> ();
		static List<string> _processHotKeyList = new List<string> ();
		static List<string> _processColdKeyList = new List<string> ();

		class TestWindow : Window {
			public TestWindow (ustring title = null) : base (title)
			{
			}

			public TestWindow (Rect frame, ustring title = null) : base (frame, title)
			{
			}

			public TestWindow (ustring title = null, int padding = 0) : base (title, padding)
			{
			}

			public TestWindow (Rect frame, ustring title = null, int padding = 0) : base (frame, title, padding)
			{
			}

			public override bool ProcessKey (KeyEvent keyEvent)
			{
				_processKeyList.Add (keyEvent.ToString ());
				return base.ProcessKey (keyEvent);
			}

			public override bool ProcessHotKey (KeyEvent keyEvent)
			{
				_processHotKeyList.Add (keyEvent.ToString ());
				return base.ProcessHotKey (keyEvent);
			}

			public override bool ProcessColdKey (KeyEvent keyEvent)
			{
				_processColdKeyList.Add (keyEvent.ToString ());

				return base.ProcessColdKey (keyEvent);
			}
		}

		public override void Init (Toplevel top)
		{
			Application.Init ();
			Top = top;

			Win = new TestWindow ($"CTRL-Q to Close - Scenario: {GetName ()}") {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			Top.Add (Win);
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
			var keyPressedLabel = new Label ("Last KeyPress:") {
				X = Pos.Left (editLabel),
				Y = Pos.Top (editLabel) + 2,
			};
			Win.Add (keyPressedLabel);
			// BUGBUG: Label is not positioning right with Pos, so using TextField instead
			var labelKeypress = new TextField ("") {
				X = Pos.Right (keyPressedLabel) + 1,
				Y = Pos.Top (keyPressedLabel),
				Width = 20,
				//TextAlignment = Terminal.Gui.TextAlignment.Left,
				ColorScheme = Colors.Error,
			};
			Win.Add (labelKeypress);

			Win.KeyPress += (sender, a) => labelKeypress.Text = a.KeyEvent.ToString ();

			// Key stroke log:
			var keyLogLabel = new Label ("Key stroke log:") {
				X = Pos.Left (editLabel),
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (keyLogLabel);

			var yOffset = (Top == Application.Top ? 1 : 6);
			var keyStrokelist = new List<string> ();
			var keyStrokeListView = new ListView (keyStrokelist) {
				X = 0,
				Y = Pos.Top (keyLogLabel) + yOffset,
				Width = 25,
				Height = Dim.Fill (),
			};
			keyStrokeListView.ColorScheme = Colors.TopLevel;
			Win.Add (keyStrokeListView);

			void KeyDownPressUp (KeyEvent keyEvent, string updown)
			{
				var msg = $"Key{updown,-5}: {keyEvent.ToString ()}";
				keyStrokelist.Add (msg);
				keyStrokeListView.MoveDown ();
			}

			Win.KeyDown += (sender, a) => KeyDownPressUp (a.KeyEvent, "Down");
			Win.KeyPress += (sender, a) => KeyDownPressUp (a.KeyEvent, "Press");
			Win.KeyUp += (sender, a) => KeyDownPressUp (a.KeyEvent, "Up");

			// ProcessKey log:
			// BUGBUG: Label is not positioning right with Pos, so using TextField instead
			var processKeyLogLabel = new Label ("ProcessKey log:") {
				X = Pos.Right (keyStrokeListView) + 1,
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (processKeyLogLabel);

			yOffset = (Top == Application.Top ? 1 : 6);
			var processKeyListView = new ListView (_processKeyList) {
				X = Pos.Left (processKeyLogLabel),
				Y = Pos.Top (processKeyLogLabel) + yOffset,
				Width = 25,
				Height = Dim.Fill (),
			};
			processKeyListView.ColorScheme = Colors.TopLevel;
			Win.Add (processKeyListView);

			// ProcessHotKey log:
			// BUGBUG: Label is not positioning right with Pos, so using TextField instead
			var processHotKeyLogLabel = new Label ("ProcessHotKey log:") {
				X = Pos.Right (processKeyListView) + 1,
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (processHotKeyLogLabel);

			yOffset = (Top == Application.Top ? 1 : 6);
			var processHotKeyListView = new ListView (_processHotKeyList) {
				X = Pos.Left (processHotKeyLogLabel),
				Y = Pos.Top (processHotKeyLogLabel) + yOffset,
				Width = 25,
				Height = Dim.Fill (),
			};
			processHotKeyListView.ColorScheme = Colors.TopLevel;
			Win.Add (processHotKeyListView);

			// ProcessColdKey log:
			// BUGBUG: Label is not positioning right with Pos, so using TextField instead
			var processColdKeyLogLabel = new Label ("ProcessColdKey log:") {
				X = Pos.Right (processHotKeyListView) + 1,
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (processColdKeyLogLabel);

			yOffset = (Top == Application.Top ? 1 : 6);
			var processColdKeyListView = new ListView (_processColdKeyList) {
				X = Pos.Left (processColdKeyLogLabel),
				Y = Pos.Top (processColdKeyLogLabel) + yOffset,
				Width = 25,
				Height = Dim.Fill (),
			};
			processColdKeyListView.ColorScheme = Colors.TopLevel;
			Win.Add (processColdKeyListView);
		}
	}
}