using System.Reactive.Concurrency;
using ReactiveUI;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;

namespace ReactiveExample;

public static class Program
{
    private static void Main (string [] args)
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        RxApp.MainThreadScheduler = new TerminalScheduler (app);
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
        var loginView = new LoginView (new ());
        app.Run (loginView);
        loginView.Dispose ();
    }
}
