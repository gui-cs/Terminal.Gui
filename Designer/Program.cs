using Terminal.Gui;

namespace Designer {
	class MainClass {
		public static void Main (string [] args)
		{
			Application.Init ();
			var top = Application.Top;

			var scroll = new ScrollView (new Rect (5, 5, 20, 20));
			scroll.Add (new Label (0, 0, "Name:"));
			scroll.Add (new Label (0, 1, "Addr:"));
			scroll.Add (new TextField (5, 0, 5, "-"));
			scroll.Add (new TextField (5, 1, 5, "-"));
			top.Add (scroll);
			top.SetFocus (scroll);

			Application.Run ();
		}
	}
}
