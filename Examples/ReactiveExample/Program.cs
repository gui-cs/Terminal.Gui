using System.Reactive.Concurrency;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Terminal.Gui.Configuration;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;

namespace ReactiveExample;

public static class Program
{
    private static void Main (string [] args)
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        Application.Init ();
        RxApp.MainThreadScheduler = TerminalScheduler.Default;
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
        Application.Run (new LoginView (new LoginViewModel ()));
        Application.Top.Dispose ();
        Application.Shutdown ();
    }
}
