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

			IDisposable PostOnMainLoopAsTimeout () {
				object timeout = null;
				var composite = new CompositeDisposable (2) {
					Disposable.Create (() => Application.MainLoop.RemoveTimeout (timeout))
				};
				timeout = Application.MainLoop.AddTimeout (dueTime, args => {
					composite.Add(action (this, state));
					Application.MainLoop.RemoveTimeout (timeout);
					return true;
				});
				return composite;
			}

			return dueTime == TimeSpan.Zero 
				? PostOnMainLoop ()
				: PostOnMainLoopAsTimeout ();
		}
	}
}