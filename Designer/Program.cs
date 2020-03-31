using System;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Designer {
	class Surface : Window {
		public Surface () : base ("Designer")
		{
		}
	}

	class MainClass {
		static void Close ()
		{
			MessageBox.ErrorQuery (50, 7, "Error", "There is nothing to close", "Ok");
		}

		public static void Main (string [] args)
		{
			Application.Init ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Close", "", () => Close ()),
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

			var loginText = new TextField("") {
				X = Pos.Right(password),
				Y = Pos.Top(login),
				Width = 40,
				ColorScheme = new ColorScheme() {
					Focus = Attribute.Make(Color.BrightYellow, Color.DarkGray),
					Normal = Attribute.Make(Color.Green, Color.BrightYellow),
					HotFocus = Attribute.Make(Color.BrightBlue, Color.Brown),
					HotNormal = Attribute.Make(Color.Red, Color.BrightRed),
				},
			};

			var passText = new TextField ("") {
				Secret = true,
				X = Pos.Left (loginText),
				Y = Pos.Top (password),
				Width = Dim.Width (loginText)
			};

			surface.Add (login, password, loginText, passText);
			Application.Top.Add (menu, surface);
			Application.Run ();
		}
	}
}
