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
			new RadioGroup (3, 8, new [] { "_Personal", "_Company" }),
			new Button (3, 14, "Ok"),
			new Button (10, 14, "Cancel"),
			new Label (3, 18, "Press ESC and 9 to activate the menubar")
		);
	}

	public static Label ml2;

	static void NewFile ()
	{
		var d = new Dialog (
			"New File", 50, 20,
			new Button ("Ok", is_default: true ) { Clicked = () => { Application.RequestStop (); } },
			new Button ("Cancel") { Clicked = () => { Application.RequestStop (); } });
		ml2 = new Label (1, 1, "Mouse Debug Line");
		d.Add (ml2);
		Application.Run (d);
	}

	static bool Quit ()
	{
		var n = MessageBox.Query (50, 5, "Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
		return n == 0;
	}

	public static Label ml;
	static void Main ()
	{
		Application.Init ();
		var top = Application.Top;
		var tframe = top.Frame;

		var win = new Window (new Rect (0, 1, tframe.Width, tframe.Height-1), "Hello");
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_New", "Creates new file", NewFile),
				new MenuItem ("_Open", "", null),
				new MenuItem ("_Close", "", null),
				new MenuItem ("_Quit", "", () => { if (Quit ()) top.Running = false; })
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", null),
				new MenuItem ("C_ut", "", null),
				new MenuItem ("_Paste", "", null)
			})
		});

		ShowEntries (win);
		int count = 0;
		ml = new Label (new Rect (3, 16, 50, 1), "Mouse: ");
		Application.RootMouseEvent += delegate (MouseEvent me) {

			ml.Text = $"Mouse: ({me.X},{me.Y}) - {me.Flags} {count++}";
		};

		win.Add (ml);

		// ShowTextAlignments (win);
		top.Add (win);
		top.Add (menu);
		Application.Run ();
	}
}