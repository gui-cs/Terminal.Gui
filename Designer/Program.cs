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

			surface.Add (login, password);
			Application.Top.Add (menu, surface);
			Application.Run ();
		}
	}
#endif
	class MainClass {
		public static void Main(string[] args)
		{
			Application.Init();

			Window window = new Window("Repaint Issue") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
			RadioGroup radioGroup = new RadioGroup(1, 1, new[] { "Short", "Longer Text  --> Will not be repainted <--", "Short" });

			Button replaceButtonLonger = new Button(1, 10, "Replace Texts above Longer") {
				Clicked = () => { radioGroup.RadioLabels = new string[] { "Longer than before", "Shorter Text", "Longer than before" }; }
			};

			Button replaceButtonSmaller = new Button(35, 10, "Replace Texts above Smaller") {
				Clicked = () => { radioGroup.RadioLabels = new string[] { "Short", "Longer Text  --> Will not be repainted <--", "Short" }; }
			};

			window.Add(radioGroup, replaceButtonLonger, replaceButtonSmaller);
			Application.Top.Add(window);
			Application.Run();
		}
	}

}
