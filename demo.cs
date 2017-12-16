using Terminal;

class Demo {
    static void Main ()
    {
        Application.Init ();
        var top = Application.Top;
        top.Add (new Window (new Rect (10, 10, 20, 10), "Hello"));
        Application.Run ();
    }
}