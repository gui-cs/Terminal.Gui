using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Terminal.Gui.App;

namespace ReactiveExample;

public class TerminalScheduler : LocalScheduler
{
    public static readonly TerminalScheduler Default = new ();
    private TerminalScheduler () { }

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

            Application.Invoke (
                                () =>
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

            object timeout = Application.AddTimeout (
                                                     dueTime,
                                                     () =>
                                                     {
                                                         composite.Add (action (this, state));

                                                         return false;
                                                     }
                                                    );
            composite.Add (Disposable.Create (() => Application.RemoveTimeout (timeout)));

            return composite;
        }

        return dueTime == TimeSpan.Zero
                   ? PostOnMainLoop ()
                   : PostOnMainLoopAsTimeout ();
    }
}
