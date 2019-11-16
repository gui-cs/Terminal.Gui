using System;
using Terminal.Gui;

namespace Designer {
#if false
	class Surface : Window {
		public Surface () : base ("Designer")
		{
		}
	}

	class MainClass {
		public static void Main (string [] args)
		{
			Application.Init ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => { Application.RequestStop (); })
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				}),
			});

			var login = new Label ("Login: ") { X = 3, Y = 6 };
			var password = new Label ("Password: ") {
				X = Pos.Left (login),
				Y = Pos.Bottom (login) + 1
			};

			var surface = new Surface () {
				X = 0,
				Y = 1,
				Width = Dim.Percent (80),
				Height = Dim.Fill ()
			};

			//Application.Top.Add (menu);
			Application.Top.Add (login, password);
			Application.Run ();
		}
	}
#elif true
	class MainClass {
		public static void Main (string [] args)
		{

			string [] radioLabels = { "First", "Second" };
			Application.Init ();

			Window window = new Window ("Redraw issue when setting coordinates of label") { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

			Label radioLabel = new Label ("Radio selection: ") { X = 1, Y = 1 };
			Label otherLabel = new Label ("Other label: ") { X = Pos.Left (radioLabel), Y = Pos.Top (radioLabel) + radioLabels.Length };

			RadioGroup radioGroup = new RadioGroup (radioLabels) { X = Pos.Right (radioLabel), Y = Pos.Top (radioLabel) };
			RadioGroup radioGroup2 = new RadioGroup (new [] { "Option 1 of the second radio group", "Option 2 of the second radio group" }) { X = Pos.Right (radioLabel), Y = Pos.Top (otherLabel) };

			Button replaceButton = new Button (1, 10, "Add radio labels") {
				Clicked = () => {
					radioGroup.RadioLabels = new [] { "First", "Second", "Third                             <- Third ->", "Fourth                            <- Fourth ->" };
					otherLabel.Y = Pos.Top (radioLabel) + radioGroup.RadioLabels.Length;
					//Application.Refresh(); // Even this won't redraw the app correctly, only a terminal resize will re-render the view.
					//typeof(Application).GetMethod("TerminalResized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
				}
			};

			window.Add (radioLabel, otherLabel, radioGroup, radioGroup2, replaceButton);
			Application.Top.Add (window);
			Application.Top.Add (window);
			Application.Run ();
			Application.Run ();
		}
	}
#endif
}
