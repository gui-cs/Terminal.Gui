using Terminal.Gui;

namespace UICatalog.Scenarios;
[ScenarioMetadata (Name: "Bars", Description: "Illustrates Bar views (e.g. StatusBar)")]
[ScenarioCategory ("Controls")]
public class bars : Scenario {
	public override void Init ()
	{
		Application.Init ();
		ConfigurationManager.Themes.Theme = Theme;
		ConfigurationManager.Apply ();
		Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
		Application.Top.Loaded += Top_Initialized;
	}

	// Setting everything up in Initialized handler because we change the
	// QuitKey and it only sticks if changed after init
	void Top_Initialized (object sender, System.EventArgs e)
	{
		Application.QuitKey = Key.Z.WithCtrl;

		SetupMenuBar ();
		SetupDemoBar ();
		SetupStatusBar ();
	}

	void Button_Clicked (object sender, System.EventArgs e) => MessageBox.Query ("Hi", $"You clicked {sender}");

	void SetupMenuBar ()
	{
		var bar = new Bar () {
			X = 0,
			Y = 0,
			Width = Dim.Fill (),
			Height = 1,
			StatusBarStyle = true
		};

		var fileMenu = new Shortcut () {
			Title = $"_File",
			Key = Key.F.WithAlt,
			KeyBindingScope = KeyBindingScope.HotKey,
			Command = Command.Accept,
		};
		fileMenu.HelpView.Visible = false;
		fileMenu.KeyView.Visible = false;

		fileMenu.Accept += (s, e) => {
			fileMenu.SetFocus ();

			if (s is View view) {
				var menuWindow = new Window () {
					X = view.Frame.X + 1,
					Y = view.Frame.Y + 1,
					Width = 40,
					Height = 10,
					ColorScheme = view.ColorScheme
				};

				menuWindow.KeyBindings.Add (Key.Esc, Command.QuitToplevel);

				var menu = new Bar () {
					Orientation = Orientation.Vertical,
					StatusBarStyle = false,
					X = 0,
					Y = 0,
					Width = Dim.Fill (),
					Height = Dim.Fill (),
				};

				var newMenu = new Shortcut () {
					Title = "_New...",
					Text = "Create a new file",
					Key = Key.N.WithCtrl
				};
				var open = new Shortcut () {
					Title = "_Open...",
					Text = "Show the File Open Dialog",
					Key = Key.O.WithCtrl
				};
				var save = new Shortcut () {
					Title = "_Save...",
					Text = "Save",
					Key = Key.S.WithCtrl
				};

				menu.Add (newMenu, open, save);
				menuWindow.Add (menu);

				Application.Run (menuWindow);
				Application.Refresh ();
			}
		};
		var editMenu = new Shortcut () {
			Title = $"_Edit",
			//Key = Key.E.WithAlt,
			KeyBindingScope = KeyBindingScope.HotKey,
			Command = Command.Accept,
		};

		editMenu.Accept += (s, e) => {

		};
		editMenu.HelpView.Visible = false;
		editMenu.KeyView.Visible = false;

		bar.Add (fileMenu, editMenu);
		Application.Top.Add (bar);
	}

	void SetupDemoBar ()
	{
		var menu = new Bar () {
			Orientation = Orientation.Vertical,
			StatusBarStyle = false,
			X = 2,
			Y = 2,
			AutoSize = true,
			BorderStyle = LineStyle.Single
		};

		var newMenu = new Shortcut () {
			Title = "_New...",
			Text = "Create a new file",
			Key = Key.N.WithCtrl,
			AutoSize = true,
			Width = Dim.Fill ()
		};
		var open = new Shortcut () {
			Title = "_Open...",
			Text = "Show the File Open Dialog",
			Key = Key.O.WithCtrl,
			AutoSize = true,
			Width = Dim.Fill ()
		};
		var save = new Shortcut () {
			Title = "_Save...",
			Text = "Save",
			Key = Key.S.WithCtrl,
			AutoSize = true,
			Width = Dim.Fill ()
		};

		menu.Add (newMenu, open, save);
		Application.Top.Add (menu);

		var shortcut1 = new Shortcut () {
			Title = $"_Zigzag",
			Key = Key.Z.WithAlt,
			Text = "Gonna zig zag",
			KeyBindingScope = KeyBindingScope.HotKey,
			Command = Command.Accept,
			X = Pos.Center (),
			Y = Pos.Center (),
			Height = 1,
		};

		var shortcut2 = new Shortcut () {
			Title = $"Za_G",
			Text = "Gonna zag",
			Key = Key.G.WithAlt,
			KeyBindingScope = KeyBindingScope.HotKey,
			Command = Command.Accept,
			X = Pos.Left (shortcut1),
			Y = Pos.Bottom (shortcut1),
			Height = 1,
		};

		Application.Top.Add (shortcut1, shortcut2);
		shortcut1.SetFocus ();
	}

	void SetupStatusBar ()
	{
		var bar = new Bar () {
			X = 0,
			Y = Pos.AnchorEnd (1),
			Width = Dim.Fill(),
			Height = 1
		};
		var shortcut = new Shortcut () {
			Text = "Quit Application",
			Title = $"Q_uit",
			Key = Application.QuitKey,
			KeyBindingScope = KeyBindingScope.Application,
			Command = Command.QuitToplevel
		};

		bar.Add (shortcut);

		var button1 = new Button ("Press me!") {
			AutoSize = true,
			Visible = false
		};
		button1.Clicked += Button_Clicked;

		bar.Add (button1);

		shortcut = new Shortcut () {
			Title = $"_Show/Hide",
			Key = Key.F10,
			KeyBindingScope = KeyBindingScope.HotKey,
			Command = Command.ToggleChecked,
			CommandView = new CheckBox () {
				Text = $"_Show/Hide"
			}
		};

		((CheckBox)shortcut.CommandView).Toggled += (s, e) => {
			button1.Visible = !button1.Visible;
			button1.Enabled = button1.Visible;
		};

		bar.Add (shortcut);

		bar.Add (new Label () { HotKeySpecifier = new System.Text.Rune ('_'), Text = "Fo_cusLabel", CanFocus = true });

		var button2 = new Button ("Or me!") {
			AutoSize = true,
		};
		button2.Clicked += (s, e) => Application.RequestStop ();

		bar.Add (button2);

		Application.Top.Add (bar);

	}
}
