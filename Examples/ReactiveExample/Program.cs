using System.Reactive.Concurrency;
using ReactiveUI.Builder;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;

namespace ReactiveExample;

public static class Program
{
    internal static ReactiveUIBuilder _rxApp;

    private static void Main (string [] _)
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        _rxApp = RxAppBuilder.CreateReactiveUIBuilder ();
        _rxApp.WithMainThreadScheduler (new TerminalScheduler (app));
        _rxApp.WithTaskPoolScheduler (TaskPoolScheduler.Default);
        var loginView = new LoginView (new LoginViewModel ());
        app.Run (loginView);
        loginView.Dispose ();
    }
}
