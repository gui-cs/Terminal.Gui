﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "BackgroundWorker Collection", Description: "A persisting multi Toplevel BackgroundWorker threading")]
	[ScenarioCategory ("Threading")]
	[ScenarioCategory ("TopLevel")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Controls")]
	class BackgroundWorkerCollection : Scenario {
		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			Application.Top.Dispose ();

			Application.Run<MdiMain> ();

			Application.Top.Dispose ();
		}

		public override void Run ()
		{
		}

		class MdiMain : Toplevel {
			private WorkerApp workerApp;
			private bool canOpenWorkerApp;
			MenuBar menu;

			public MdiMain ()
			{
				Data = "MdiMain";

				IsMdiContainer = true;

				workerApp = new WorkerApp () { Visible = false };

				menu = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_Options", new MenuItem [] {
						new MenuItem ("_Run Worker", "", () => workerApp.RunWorker(), null, null, Key.CtrlMask | Key.R),
						new MenuItem ("_Cancel Worker", "", () => workerApp.CancelWorker(), null, null, Key.CtrlMask | Key.C),
						null,
						new MenuItem ("_Quit", "", () => Quit(), null, null, Key.CtrlMask | Key.Q)
					}),
					new MenuBarItem ("_View", new MenuItem [] { }),
					new MenuBarItem ("_Window", new MenuItem [] { })
				});
				menu.MenuOpening += Menu_MenuOpening;
				Add (menu);

				var statusBar = new StatusBar (new [] {
					new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Exit", () => Quit()),
					new StatusItem(Key.CtrlMask | Key.R, "~^R~ Run Worker", () => workerApp.RunWorker()),
					new StatusItem(Key.CtrlMask | Key.C, "~^C~ Cancel Worker", () => workerApp.CancelWorker())
				});
				Add (statusBar);

				Activate += MdiMain_Activate;
				Deactivate += MdiMain_Deactivate;

				Closed += MdiMain_Closed;

				Application.Iteration += () => {
					if (canOpenWorkerApp && !workerApp.Running && Application.MdiTop.Running) {
						Application.Run (workerApp);
					}
				};
			}

			private void MdiMain_Closed (Toplevel obj)
			{
				workerApp.Dispose ();
				Dispose ();
			}

			private void Menu_MenuOpening (MenuOpeningEventArgs menu)
			{
				if (!canOpenWorkerApp) {
					canOpenWorkerApp = true;
					return;
				}
				if (menu.CurrentMenu.Title == "_Window") {
					menu.NewMenuBarItem = OpenedWindows ();
				} else if (menu.CurrentMenu.Title == "_View") {
					menu.NewMenuBarItem = View ();
				}
			}

			private void MdiMain_Deactivate (Toplevel top)
			{
				workerApp.WriteLog ($"{top.Data} deactivate.");
			}

			private void MdiMain_Activate (Toplevel top)
			{
				workerApp.WriteLog ($"{top.Data} activate.");
			}

			private MenuBarItem View ()
			{
				List<MenuItem> menuItems = new List<MenuItem> ();
				var item = new MenuItem () {
					Title = "WorkerApp",
					CheckType = MenuItemCheckStyle.Checked
				};
				var top = Application.MdiChildes?.Find ((x) => x.Data.ToString () == "WorkerApp");
				if (top != null) {
					item.Checked = top.Visible;
				}
				item.Action += () => {
					var top = Application.MdiChildes.Find ((x) => x.Data.ToString () == "WorkerApp");
					item.Checked = top.Visible = !item.Checked;
					if (top.Visible) {
						top.ShowChild ();
					} else {
						Application.MdiTop.SetNeedsDisplay ();
					}
				};
				menuItems.Add (item);
				return new MenuBarItem ("_View",
					new List<MenuItem []> () { menuItems.Count == 0 ? new MenuItem [] { } : menuItems.ToArray () });
			}

			private MenuBarItem OpenedWindows ()
			{
				var index = 1;
				List<MenuItem> menuItems = new List<MenuItem> ();
				var sortedChildes = Application.MdiChildes;
				sortedChildes.Sort (new ToplevelComparer ());
				foreach (var top in sortedChildes) {
					if (top.Data.ToString () == "WorkerApp" && !top.Visible) {
						continue;
					}
					var item = new MenuItem ();
					item.Title = top is Window ? $"{index} {((Window)top).Title}" : $"{index} {top.Data}";
					index++;
					item.CheckType |= MenuItemCheckStyle.Checked;
					var topTitle = top is Window ? ((Window)top).Title : top.Data.ToString ();
					var itemTitle = item.Title.Substring (index.ToString ().Length + 1);
					if (top == top.GetTopMdiChild () && topTitle == itemTitle) {
						item.Checked = true;
					} else {
						item.Checked = false;
					}
					item.Action += () => {
						top.ShowChild ();
					};
					menuItems.Add (item);
				}
				if (menuItems.Count == 0) {
					return new MenuBarItem ("_Window", "", null);
				} else {
					return new MenuBarItem ("_Window", new List<MenuItem []> () { menuItems.ToArray () });
				}
			}

			private void Quit ()
			{
				RequestStop ();
			}
		}

		class WorkerApp : Toplevel {
			private List<string> log = new List<string> ();
			private ListView listLog;
			private Dictionary<Staging, BackgroundWorker> stagingWorkers;
			private List<StagingUIController> stagingsUI;

			public WorkerApp ()
			{
				Data = "WorkerApp";

				Width = Dim.Percent (80);
				Height = Dim.Percent (50);

				ColorScheme = Colors.Base;

				var label = new Label ("Worker collection Log") {
					X = Pos.Center (),
					Y = 0
				};
				Add (label);

				listLog = new ListView (log) {
					X = 0,
					Y = Pos.Bottom (label),
					Width = Dim.Fill (),
					Height = Dim.Fill ()
				};
				Add (listLog);
			}

			public void RunWorker ()
			{
				var stagingUI = new StagingUIController () { Modal = true };

				Staging staging = null;
				var worker = new BackgroundWorker () { WorkerSupportsCancellation = true };

				worker.DoWork += (s, e) => {
					var stageResult = new List<string> ();
					for (int i = 0; i < 500; i++) {
						stageResult.Add (
							$"Worker {i} started at {DateTime.Now}");
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
						WriteLog ($"Exception occurred {e.Error.Message} on Worker {staging.StartStaging}.{staging.StartStaging:fff} at {DateTime.Now}");
					} else if (e.Cancelled) {
						// Canceled
						WriteLog ($"Worker {staging.StartStaging}.{staging.StartStaging:fff} was canceled at {DateTime.Now}!");
					} else {
						// Passed
						WriteLog ($"Worker {staging.StartStaging}.{staging.StartStaging:fff} was completed at {DateTime.Now}.");
						Application.Refresh ();

						var stagingUI = new StagingUIController (staging, e.Result as List<string>) {
							Modal = false,
							Title = $"Worker started at {staging.StartStaging}.{staging.StartStaging:fff}",
							Data = $"{staging.StartStaging}.{staging.StartStaging:fff}"
						};

						stagingUI.ReportClosed += StagingUI_ReportClosed;

						if (stagingsUI == null) {
							stagingsUI = new List<StagingUIController> ();
						}
						stagingsUI.Add (stagingUI);
						stagingWorkers.Remove (staging);

						stagingUI.Run ();
					}
				};

				Application.Run (stagingUI);

				if (stagingUI.Staging != null && stagingUI.Staging.StartStaging != null) {
					staging = new Staging (stagingUI.Staging.StartStaging);
					WriteLog ($"Worker is started at {staging.StartStaging}.{staging.StartStaging:fff}");
					if (stagingWorkers == null) {
						stagingWorkers = new Dictionary<Staging, BackgroundWorker> ();
					}
					stagingWorkers.Add (staging, worker);
					worker.RunWorkerAsync ();
					stagingUI.Dispose ();
				}
			}

			private void StagingUI_ReportClosed (StagingUIController obj)
			{
				WriteLog ($"Report {obj.Staging.StartStaging}.{obj.Staging.StartStaging:fff} closed.");
				stagingsUI.Remove (obj);
			}

			public void CancelWorker ()
			{
				if (stagingWorkers == null || stagingWorkers.Count == 0) {
					WriteLog ($"Worker is not running at {DateTime.Now}!");
					return;
				}

				foreach (var sw in stagingWorkers) {
					var key = sw.Key;
					var value = sw.Value;
					if (!key.Completed) {
						value.CancelAsync ();
					}
					WriteLog ($"Worker {key.StartStaging}.{key.StartStaging:fff} is canceling at {DateTime.Now}!");

					stagingWorkers.Remove (sw.Key);
				}
			}

			public void WriteLog (string msg)
			{
				log.Add (msg);
				listLog.MoveEnd ();
			}
		}

		class StagingUIController : Window {
			private Label label;
			private ListView listView;
			private Button start;
			private Button close;
			public Staging Staging { get; private set; }

			public event Action<StagingUIController> ReportClosed;

			public StagingUIController (Staging staging, List<string> list) : this ()
			{
				Staging = staging;
				label.Text = "Work list:";
				listView.SetSource (list);
				start.Visible = false;
				Id = "";
			}

			public StagingUIController ()
			{
				X = Pos.Center ();
				Y = Pos.Center ();
				Width = Dim.Percent (85);
				Height = Dim.Percent (85);

				ColorScheme = Colors.Dialog;

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
					Staging = new Staging (DateTime.Now);
					RequestStop ();
				};
				Add (start);

				close = new Button ("Close");
				close.Clicked += OnReportClosed;
				Add (close);

				KeyPress += (e) => {
					if (e.KeyEvent.Key == Key.Esc) {
						OnReportClosed ();
					}
				};

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

			private void OnReportClosed ()
			{
				if (Staging.StartStaging != null) {
					ReportClosed?.Invoke (this);
				}
				RequestStop ();
			}

			public void Run ()
			{
				Application.Run (this);
			}
		}

		class Staging {
			public DateTime? StartStaging { get; private set; }
			public bool Completed { get; }

			public Staging (DateTime? startStaging, bool completed = false)
			{
				StartStaging = startStaging;
				Completed = completed;
			}
		}
	}
}
