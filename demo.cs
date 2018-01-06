using Terminal;

class Demo {
	static void ShowTextAlignments (View container)
	{
		container.Add (
			new Label (new Rect (0, 0, 40, 3), "1-Hello world, how are you doing today") { TextAlignment = TextAlignment.Left },
			new Label (new Rect (0, 4, 40, 3), "2-Hello world, how are you doing today") { TextAlignment = TextAlignment.Right },
			new Label (new Rect (0, 8, 40, 3), "3-Hello world, how are you doing today") { TextAlignment = TextAlignment.Centered },
			new Label (new Rect (0, 12, 40, 3), "4-Hello world, how are you doing today") { TextAlignment = TextAlignment.Justified });
	}

	static void ShowEntries (View container)
	{
		container.Add (
			new Label (3, 2, "Login: "),
			new TextField (14, 2, 40, ""),
			new Label (3, 4, "Password: "),
			new TextField (14, 4, 40, "") { Secret = true },
			new CheckBox (3, 6, "Remember me"),
			new Button (3, 8, "Ok"),
			new Button (10, 8, "Cancel"),
			new Label (3, 18, "Press ESC and 9 to activate the menubar")
		);
	}

	static void Main ()
	{
		Application.Init ();
		var top = Application.Top;
		var tframe = top.Frame;

		var win = new Window (new Rect (0, 1, tframe.Width, tframe.Height-1), "Hello");
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_New", "Creates new file", () => System.Console.WriteLine ("foo")),
				new MenuItem ("_Open", "", null),
				new MenuItem ("_Close", "", null),
				new MenuItem ("_Quit", "", null)
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", null),
				new MenuItem ("C_ut", "", null),
				new MenuItem ("_Paste", "", null)
			})
		});

		ShowEntries (win);

		// ShowTextAlignments (win);
		top.Add (win);
		top.Add (menu);
		Application.Run ();
	}
}