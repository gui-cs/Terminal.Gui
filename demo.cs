using Terminal;

class Demo {
    static void Main ()
    {
        Application.Init ();
        var top = Application.Top;
        var win = new Window (new Rect (0, 0, 80, 24), "Hello") {
            new Label (new Rect (0, 0, 40, 3), "1-Hello world, how are you doing today") { TextAlignment = TextAlignment.Left },
            new Label (new Rect (0, 4, 40, 3), "2-Hello world, how are you doing today") { TextAlignment = TextAlignment.Right},
            new Label (new Rect (0, 8, 40, 3), "3-Hello world, how are you doing today") { TextAlignment = TextAlignment.Centered },
            new Label (new Rect (0, 12, 40, 3), "4-Hello world, how are you doing today") { TextAlignment = TextAlignment.Justified},
            new Label (3, 14, "Login: "),
            new TextField (10, 14, 40, ""),
            new Button (3, 16, "Ok")
        };
        top.Add (win);
        Application.Run ();
    }
}