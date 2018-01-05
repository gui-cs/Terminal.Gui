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
			new Button (3, 6, "Ok"),
			new Button (10, 6, "Cancel")
		);
	}

	static void Main ()
	{
		Application.Init ();
		var top = Application.Top;
		var win = new Window (new Rect (0, 0, 80, 24), "Hello");

		ShowEntries (win);
		// ShowTextAlignments (win);
		top.Add (win);
		Application.Run ();
	}
}