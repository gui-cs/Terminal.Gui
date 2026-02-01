using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;


using IApplication app = Application.Create ().Init ();
var userName = app.Run<ExampleWindow> ();
public sealed class ExampleWindow : Window
{
    public ExampleWindow ()
    {
        Title = "Text View Scrollbars Example";
        var textView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ScrollBars = true,
        };


        Add (textView);
    }
}
