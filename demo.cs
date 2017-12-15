using Terminal;

class Demo {
    static void Main ()
    {
        Application.Init ();
        var top = Application.Top;
        top.Add (new Window (new Rect (10, 10, 20, 20), "Hello"));
        Application.Run ();
    }
}