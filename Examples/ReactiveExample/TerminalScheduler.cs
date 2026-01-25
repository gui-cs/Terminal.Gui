#nullable enable
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Terminal.Gui.App;

namespace ReactiveExample;

public class TerminalScheduler : LocalScheduler
{
    public TerminalScheduler (IApplication? application) { _application = application; }

    private readonly IApplication? _application = null;

    public override IDisposable Schedule<TState> (
        TState state,
        TimeSpan dueTime,
        Func<IScheduler, TState, IDisposable> action
    )
    {
        IDisposable PostOnMainLoop ()
        {
            var composite = new CompositeDisposable (2);
            var cancellation = new CancellationDisposable ();

            _application?.Invoke (
                                 (_) =>
                                 {
                                     if (!cancellation.Token.IsCancellationRequested)
                                     {
                                         composite.Add (action (this, state));
                                     }
                                 }
                                );
            composite.Add (cancellation);

            return composite;
        }

        IDisposable PostOnMainLoopAsTimeout ()
        {
            var composite = new CompositeDisposable (2);

            object? timeout = _application?.AddTimeout (
                                                      dueTime,
                                                      () =>
                                                      {
                                                          composite.Add (action (this, state));

                                                          return false;
                                                      }
                                                     );
            composite.Add (Disposable.Create (() =>
                                              {
                                                  if (timeout is { })
                                                  {
                                                      _application?.RemoveTimeout (timeout);
                                                  }
                                              }));

            return composite;
        }

        return dueTime == TimeSpan.Zero
                   ? PostOnMainLoop ()
                   : PostOnMainLoopAsTimeout ();
    }
}
