using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "BackgroundWorker", Description: "A persisting multi Toplevel BackgroundWorker threading")]
	[ScenarioCategory ("Threading")]
	[ScenarioCategory ("TopLevel")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Controls")]
	class BackgroundWorkerSample : Scenario {
		public override void Run ()
		{
			Top.Dispose ();

			Application.Run<MainApp> ();

			Top.Dispose ();
		}
	}

	public class MainApp : Toplevel {
		private List<string> log = new List<string> ();
		private ListView listLog;
		private Dictionary<StagingUIController, BackgroundWorker> stagingWorkers;

		public MainApp ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_Options", new MenuItem [] {
					new MenuItem ("_Run Worker", "", () => RunWorker(), null, null, Key.CtrlMask | Key.R),
					new MenuItem ("_Cancel Worker", "", () => CancelWorker(), null, null, Key.CtrlMask | Key.C),
					null,
					new MenuItem ("_Quit", "", () => Application.RequestStop (), null, null, Key.CtrlMask | Key.Q)
				})
			});
			Add (menu);

			var statusBar = new StatusBar (new [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Exit", () => Application.RequestStop()),
				new StatusItem(Key.CtrlMask | Key.P, "~^R~ Run Worker", () => RunWorker()),
				new StatusItem(Key.CtrlMask | Key.P, "~^C~ Cancel Worker", () => CancelWorker())
			});
			Add (statusBar);

			var top = new Toplevel ();

			top.Add (new Label ("Worker Log") {
				X = Pos.Center (),
				Y = 0
			});

			listLog = new ListView (log) {
				X = 0,
				Y = 2,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			top.Add (listLog);
			Add (top);
		}

		private void RunWorker ()
		{
			var stagingUI = new StagingUIController ();

			var worker = new BackgroundWorker () { WorkerSupportsCancellation = true };

			worker.DoWork += (s, e) => {
				var stageResult = new List<string> ();
				for (int i = 0; i < 500; i++) {
					stageResult.Add (
						$"Worker {i} started at {DateTime.UtcNow}");
					e.Result = stageResult;
					Thread.Sleep (1);
					if (worker.CancellationPending) {
						e.Cancel = true;
						return;
					}
				}
			};

			worker.RunWorkerCompleted += (s, e) => {
				if (e.Error != null) {
					// Failed
					log.Add ($"Exception occurred {e.Error.Message} on Worker {stagingUI.StartStaging}.{stagingUI.StartStaging:fff} at {DateTime.UtcNow}");
					listLog.SetNeedsDisplay ();
				} else if (e.Cancelled) {
					// Canceled
					log.Add ($"Worker {stagingUI.StartStaging}.{stagingUI.StartStaging:fff} was canceled at {DateTime.UtcNow}!");
					listLog.SetNeedsDisplay ();
				} else {
					// Passed
					log.Add ($"Worker {stagingUI.StartStaging}.{stagingUI.StartStaging:fff} was completed at {DateTime.UtcNow}.");
					listLog.SetNeedsDisplay ();
					Application.Refresh ();
					stagingUI.Load (e.Result as List<string>);
				}
				stagingWorkers.Remove (stagingUI);
			};

			Application.Run (stagingUI);

			if (stagingUI.StartStaging != null) {
				log.Add ($"Worker is started at {stagingUI.StartStaging}.{stagingUI.StartStaging:fff}");
				listLog.SetNeedsDisplay ();
				if (stagingWorkers == null) {
					stagingWorkers = new Dictionary<StagingUIController, BackgroundWorker> ();
				}
				stagingWorkers.Add (stagingUI, worker);
				worker.RunWorkerAsync ();
			}
		}

		private void CancelWorker ()
		{
			if (stagingWorkers.Count == 0) {
				log.Add ($"Worker is not running at {DateTime.UtcNow}!");
				listLog.SetNeedsDisplay ();
				return;
			}

			var eStaging = stagingWorkers.GetEnumerator ();
			eStaging.MoveNext ();
			var fStaging = eStaging.Current;
			var stagingUI = fStaging.Key;
			var worker = fStaging.Value;
			worker.CancelAsync ();
			log.Add ($"Worker {stagingUI.StartStaging}.{stagingUI.StartStaging:fff} is canceling at {DateTime.UtcNow}!");
			listLog.SetNeedsDisplay ();
		}
	}

	public class StagingUIController : Window {
		private Label label;
		private ListView listView;
		private Button start;
		private Button close;

		public DateTime? StartStaging { get; private set; }

		public StagingUIController ()
		{
			X = Pos.Center ();
			Y = Pos.Center ();
			Width = Dim.Percent (85);
			Height = Dim.Percent (85);

			ColorScheme = Colors.Dialog;
			Modal = true;

			Title = "Run Worker";

			label = new Label ("Press start to do the work or close to exit.") {
				X = Pos.Center (),
				Y = 1,
				ColorScheme = Colors.Dialog
			};
			Add (label);

			listView = new ListView () {
				X = 0,
				Y = 2,
				Width = Dim.Fill (),
				Height = Dim.Fill (2)
			};
			Add (listView);

			start = new Button ("Start") { IsDefault = true };
			start.Clicked += () => {
				StartStaging = DateTime.UtcNow;
				Application.RequestStop ();
			};
			Add (start);

			close = new Button ("Close");
			close.Clicked += () => Application.RequestStop ();
			Add (close);

			LayoutStarted += (_) => {
				var btnsWidth = start.Bounds.Width + close.Bounds.Width + 2 - 1;
				var shiftLeft = Math.Max ((Bounds.Width - btnsWidth) / 2 - 2, 0);

				shiftLeft += close.Bounds.Width + 1;
				close.X = Pos.AnchorEnd (shiftLeft);
				close.Y = Pos.AnchorEnd (1);

				shiftLeft += start.Bounds.Width + 1;
				start.X = Pos.AnchorEnd (shiftLeft);
				start.Y = Pos.AnchorEnd (1);
			};

		}

		public void Load (List<string> list)
		{
			var stagingUI = new StagingUIController ();
			stagingUI.Title = $"Worker started at {StartStaging}.{StartStaging:fff}";
			stagingUI.label.Text = "Work list:";
			stagingUI.listView.SetSource (list);
			stagingUI.start.Visible = false;

			Application.Run (stagingUI);
		}
	}
}
