using NStack;
using System;
using System.Threading;
using Terminal.Gui;
using System.Linq;

namespace UICatalog {
	// 
	// This would be a great scenario to show of threading (Issue #471)
	//
	[ScenarioMetadata (Name: "Progress", Description: "Shows off ProgressBar and Threading")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Threading")]
	class Progress : Scenario {

		class ProgressDemo : FrameView, IDisposable {
			internal ProgressBar ActivityProgressBar { get; private set; }
			internal ProgressBar PulseProgressBar { get; private set; }
			bool _disposedValue;
			const int _verticalSpace = 1;

			internal Action StartBtnClick;
			internal Action StopBtnClick;
			internal Action PulseBtnClick;

			internal ProgressDemo (ustring title) : base (title)
			{
				ColorScheme = Colors.Dialog;

				var leftFrame = new FrameView ("Settings") {
					X = 0,
					Y = 0,
					Height = Dim.Percent (100),
					Width = Dim.Percent (30)
				};
				Add (leftFrame);

				var startButton = new Button ("Start Timer") {
					X = Pos.Right (leftFrame) + 1,
					Y = 0,
					Clicked = () => StartBtnClick?.Invoke ()
				};
				var pulseButton = new Button ("Pulse") {
					X = Pos.Right (startButton) + 2,
					Y = Pos.Y (startButton),
					Clicked = () => PulseBtnClick.Invoke ()
				};
				var stopbutton = new Button ("Stop Timer") {
					X = Pos.Right (pulseButton) + 2,
					Y = Pos.Top (pulseButton),
					Clicked = () => StopBtnClick.Invoke ()
				};

				Add (startButton);
				Add (pulseButton);
				Add (stopbutton);

				ActivityProgressBar = new ProgressBar () {
					X = Pos.Right (leftFrame) + 1,
					Y = Pos.Bottom (startButton) + 1,
					Width = Dim.Fill (),
					Height = 1,
					Fraction = 0.25F,
					ColorScheme = Colors.Error
				};
				Add (ActivityProgressBar);

				PulseProgressBar = new ProgressBar () {
					X = Pos.Right (leftFrame) + 1,
					Y = Pos.Bottom (ActivityProgressBar) + 1,
					Width = Dim.Fill (),
					Height = 1,
					ColorScheme = Colors.Error
				};
				Add (PulseProgressBar);

				// Set height to height of controls + spacing + frame
				Height = 2 + _verticalSpace + Dim.Height (startButton) + _verticalSpace + Dim.Height (ActivityProgressBar) + _verticalSpace + Dim.Height (PulseProgressBar) + _verticalSpace;

			}

			protected virtual void Dispose (bool disposing)
			{
				if (!_disposedValue) {
					if (disposing) {

					}
					_disposedValue = true;
				}
			}

			public void Dispose ()
			{
				// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
				Dispose (disposing: true);
				GC.SuppressFinalize (this);
			}
		}

		private Timer _systemTimer = null;
		private int _systemTimerTick = 100; // ms
		private object _mainLoopTimeout = null;
		private int _mainLooopTimeoutTick = 1000; // ms
		public override void Setup ()
		{
			// Demo #1 - Use System.Timer (and threading)
			var systemTimerDemo = new ProgressDemo ("System.Timer (threads)") {
				X = 0,
				Y = 0,
				Width = Dim.Percent (100),
			};
			systemTimerDemo.StartBtnClick = () => {
				_systemTimer?.Dispose ();
				_systemTimer = null;

				systemTimerDemo.ActivityProgressBar.Fraction = 0F;
				systemTimerDemo.PulseProgressBar.Fraction = 0F;

				_systemTimer = new Timer ((o) => {
					// Note the check for Mainloop being valid. System.Timers can run after they are Disposed.
					// This code must be defensive for that. 
					Application.MainLoop?.Invoke (() => systemTimerDemo.PulseBtnClick ());
				}, null, 0, _systemTimerTick);
			};

			systemTimerDemo.PulseBtnClick = () => {
				if (systemTimerDemo.ActivityProgressBar.Fraction + 0.01F >= 1) {
					systemTimerDemo.ActivityProgressBar.Fraction = 0F;
				} else {
					systemTimerDemo.ActivityProgressBar.Fraction += 0.01F;
				}
				systemTimerDemo.PulseProgressBar.Pulse ();
			};
			systemTimerDemo.StopBtnClick = () => {
				_systemTimer?.Dispose ();
				_systemTimer = null;

				systemTimerDemo.ActivityProgressBar.Fraction = 1F;
				systemTimerDemo.PulseProgressBar.Fraction = 1F;
			};


			Win.Add (systemTimerDemo);

			// Demo #2 - Use Application.MainLoop.AddTimeout (no threads)
			var mainLoopTimeoutDemo = new ProgressDemo ("Application.AddTimer (no threads)") {
				X = 0,
				Y = Pos.Bottom (systemTimerDemo),
				Width = Dim.Percent (100),
			};
			mainLoopTimeoutDemo.StartBtnClick = () => {
				mainLoopTimeoutDemo.StopBtnClick ();

				mainLoopTimeoutDemo.ActivityProgressBar.Fraction = 0F;
				mainLoopTimeoutDemo.PulseProgressBar.Fraction = 0F;

				Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (_mainLooopTimeoutTick), (loop) => {
					mainLoopTimeoutDemo?.PulseBtnClick ();
					return true;
				});
			};
			mainLoopTimeoutDemo.PulseBtnClick = () => {
				if (mainLoopTimeoutDemo.ActivityProgressBar.Fraction + 0.01F >= 1) {
					mainLoopTimeoutDemo.ActivityProgressBar.Fraction = 0F;
				} else {
					mainLoopTimeoutDemo.ActivityProgressBar.Fraction += 0.01F;
				}
				mainLoopTimeoutDemo.PulseProgressBar.Pulse ();
			};
			mainLoopTimeoutDemo.StopBtnClick = () => {
				if (_mainLoopTimeout != null) {
					Application.MainLoop.RemoveTimeout (_mainLoopTimeout);
					_mainLoopTimeout = null;
				}

				mainLoopTimeoutDemo.ActivityProgressBar.Fraction = 1F;
				mainLoopTimeoutDemo.PulseProgressBar.Fraction = 1F;
			};
			Win.Add (mainLoopTimeoutDemo);

		}

		protected override void Dispose (bool disposing)
		{
			Win.GetEnumerator ().Reset ();
			while (Win.GetEnumerator ().MoveNext ()) {
				var cur = (ProgressDemo)Win.GetEnumerator ().Current;
				cur?.StopBtnClick ();
				cur.Dispose ();
			}
			base.Dispose (disposing);
		}
	}
}