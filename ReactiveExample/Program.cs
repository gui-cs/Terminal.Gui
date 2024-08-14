using System.Reactive.Concurrency;
using ReactiveUI;
using Terminal.Gui;

namespace ReactiveExample;

public static class Program
{
    private static void Main (string [] args)
    {
        Application.Init ();
        RxApp.MainThreadScheduler = TerminalScheduler.Default;
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;
        Application.Run (new LoginView (new LoginViewModel ()));
        Application.Top.Dispose ();
        Application.Shutdown ();
    }
}
