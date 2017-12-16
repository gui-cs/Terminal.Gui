using Terminal;

class Demo {
    static void Main ()
    {
        Application.Init ();
        var top = Application.Top;
        top.Add (new Window (new Rect (0, 0, 80, 24), "Hello"));
        Application.Run ();
    }
}