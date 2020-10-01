using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Terminal.Gui;

namespace ReactiveExample {
	public class TerminalScheduler : LocalScheduler {
		public static readonly TerminalScheduler Default = new TerminalScheduler();
		TerminalScheduler () { }

		public override IDisposable Schedule<TState> (
			TState state, TimeSpan dueTime,
			Func<IScheduler, TState, IDisposable> action) {
			
			IDisposable PostOnMainLoop() {
				var composite = new CompositeDisposable(2);
				var cancellation = new CancellationDisposable();
				Application.MainLoop.Invoke (() => {
					if (!cancellation.Token.IsCancellationRequested)
						composite.Add(action(this, state));
				});
				composite.Add(cancellation);
				return composite;
			}

			IDisposable PostAsTimeout () {
				var composite = new CompositeDisposable(2);
				var token = Application.MainLoop.AddTimeout (dueTime, args => {
					composite.Add(action (this, state));
					return true;
				});
				composite.Add (Disposable.Create (() => Application.MainLoop.RemoveTimeout (token)));
				return composite;
			}

			return dueTime == TimeSpan.Zero 
				? PostOnMainLoop ()
				: PostAsTimeout ();
		}
	}
}