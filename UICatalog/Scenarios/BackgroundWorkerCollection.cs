using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("BackgroundWorker Collection", "A persisting multi Toplevel BackgroundWorker threading")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Top Level Windows")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Controls")]
public class BackgroundWorkerCollection : Scenario {
    public override void Run () => Application.Run<OverlappedMain> ();

    class OverlappedMain : Toplevel {
        readonly MenuBar _menu;
        readonly WorkerApp _workerApp;
        bool _canOpenWorkerApp;

        public OverlappedMain () {
            Data = "OverlappedMain";

            IsOverlappedContainer = true;

            _workerApp = new WorkerApp { Visible = false };

            _menu = new MenuBar {
                Menus = [
                    new MenuBarItem ("_Options", new MenuItem [] {
                        new("_Run Worker", "", () => _workerApp.RunWorker (), null, null,
                            KeyCode.CtrlMask | KeyCode.R),
                        new("_Cancel Worker", "", () => _workerApp.CancelWorker (), null, null,
                            KeyCode.CtrlMask | KeyCode.C),
                        null,
                        new("_Quit", "", () => Quit (), null, null,
                            (KeyCode)Application.QuitKey)
                    }),
                    new MenuBarItem ("_View", new MenuItem [] { }),
                    new MenuBarItem ("_Window", new MenuItem [] { })
                ]
            };
            ;
            _menu.MenuOpening += Menu_MenuOpening;
            Add (_menu);

            var statusBar = new StatusBar (new[] {
                new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit ()),
                new StatusItem (KeyCode.CtrlMask | KeyCode.R, "~^R~ Run Worker",
                    () => _workerApp.RunWorker ()),
                new StatusItem (KeyCode.CtrlMask | KeyCode.C, "~^C~ Cancel Worker",
                    () => _workerApp.CancelWorker ())
            });
            Add (statusBar);

            Activate += OverlappedMain_Activate;
            Deactivate += OverlappedMain_Deactivate;

            Closed += OverlappedMain_Closed;

            Application.Iteration += (s, a) => {
                if (_canOpenWorkerApp && !_workerApp.Running && Application.OverlappedTop.Running) {
                    Application.Run (_workerApp);
                }
            };
        }

        void OverlappedMain_Closed (object sender, ToplevelEventArgs e) {
            _workerApp.Dispose ();
            Dispose ();
        }

        void Menu_MenuOpening (object sender, MenuOpeningEventArgs menu) {
            if (!_canOpenWorkerApp) {
                _canOpenWorkerApp = true;
                return;
            }

            if (menu.CurrentMenu.Title == "_Window") {
                menu.NewMenuBarItem = OpenedWindows ();
            } else if (menu.CurrentMenu.Title == "_View") {
                menu.NewMenuBarItem = View ();
            }
        }

        void OverlappedMain_Deactivate (object sender, ToplevelEventArgs top) =>
            _workerApp.WriteLog ($"{top.Toplevel.Data} deactivate.");

        void OverlappedMain_Activate (object sender, ToplevelEventArgs top) =>
            _workerApp.WriteLog ($"{top.Toplevel.Data} activate.");

        MenuBarItem View () {
            var menuItems = new List<MenuItem> ();
            var item = new MenuItem {
                Title = "WorkerApp",
                CheckType = MenuItemCheckStyle.Checked
            };
            var top = Application.OverlappedChildren?.Find (x => x.Data.ToString () == "WorkerApp");
            if (top != null) {
                item.Checked = top.Visible;
            }

            item.Action += () => {
                var top = Application.OverlappedChildren.Find (x => x.Data.ToString () == "WorkerApp");
                item.Checked = top.Visible = (bool)!item.Checked;
                if (top.Visible) {
                    Application.MoveToOverlappedChild (top);
                } else {
                    Application.OverlappedTop.SetNeedsDisplay ();
                }
            };
            menuItems.Add (item);
            return new MenuBarItem ("_View",
                new List<MenuItem[]>
                    { menuItems.Count == 0 ? new MenuItem [] { } : menuItems.ToArray () });
        }

        MenuBarItem OpenedWindows () {
            var index = 1;
            var menuItems = new List<MenuItem> ();
            var sortedChildren = Application.OverlappedChildren;
            sortedChildren.Sort (new ToplevelComparer ());
            foreach (var top in sortedChildren) {
                if (top.Data.ToString () == "WorkerApp" && !top.Visible) {
                    continue;
                }

                var item = new MenuItem ();
                item.Title = top is Window ? $"{index} {((Window)top).Title}" : $"{index} {top.Data}";
                index++;
                item.CheckType |= MenuItemCheckStyle.Checked;
                var topTitle = top is Window ? ((Window)top).Title : top.Data.ToString ();
                var itemTitle = item.Title.Substring (index.ToString ().Length + 1);
                if (top == Application.GetTopOverlappedChild () && topTitle == itemTitle) {
                    item.Checked = true;
                } else {
                    item.Checked = false;
                }

                item.Action += () => {
                    Application.MoveToOverlappedChild (top);
                };
                menuItems.Add (item);
            }

            if (menuItems.Count == 0) {
                return new MenuBarItem ("_Window", "", null);
            }

            return new MenuBarItem ("_Window", new List<MenuItem[]> { menuItems.ToArray () });
        }

        void Quit () => RequestStop ();
    }

    class WorkerApp : Toplevel {
        readonly ListView _listLog;
        readonly List<string> _log = [];
        List<StagingUIController> _stagingsUi;
        Dictionary<Staging, BackgroundWorker> _stagingWorkers;

        public WorkerApp () {
            Data = "WorkerApp";

            Width = Dim.Percent (80);
            Height = Dim.Percent (50);

            ColorScheme = Colors.ColorSchemes["Base"];

            var label = new Label { X = Pos.Center (), Y = 0, Text = "Worker collection Log" };
            Add (label);

            _listLog = new ListView {
                X = 0,
                Y = Pos.Bottom (label),
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Source = new ListWrapper (_log)
            };
            Add (_listLog);
        }

        public void RunWorker () {
            var stagingUI = new StagingUIController { Modal = true };

            Staging staging = null;
            var worker = new BackgroundWorker { WorkerSupportsCancellation = true };

            worker.DoWork += (s, e) => {
                var stageResult = new List<string> ();
                for (var i = 0; i < 500; i++) {
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
                    WriteLog (
                        $"Exception occurred {e.Error.Message} on Worker {staging.StartStaging}.{staging.StartStaging:fff} at {DateTime.Now}");
                } else if (e.Cancelled) {
                    // Canceled
                    WriteLog (
                        $"Worker {staging.StartStaging}.{staging.StartStaging:fff} was canceled at {DateTime.Now}!");
                } else {
                    // Passed
                    WriteLog (
                        $"Worker {staging.StartStaging}.{staging.StartStaging:fff} was completed at {DateTime.Now}.");
                    Application.Refresh ();

                    var stagingUI = new StagingUIController (staging, e.Result as List<string>) {
                        Modal = false,
                        Title =
                            $"Worker started at {staging.StartStaging}.{staging.StartStaging:fff}",
                        Data = $"{staging.StartStaging}.{staging.StartStaging:fff}"
                    };

                    stagingUI.ReportClosed += StagingUI_ReportClosed;

                    if (_stagingsUi == null) {
                        _stagingsUi = new List<StagingUIController> ();
                    }

                    _stagingsUi.Add (stagingUI);
                    _stagingWorkers.Remove (staging);

                    stagingUI.Run ();
                }
            };

            Application.Run (stagingUI);

            if (stagingUI.Staging != null && stagingUI.Staging.StartStaging != null) {
                staging = new Staging (stagingUI.Staging.StartStaging);
                WriteLog ($"Worker is started at {staging.StartStaging}.{staging.StartStaging:fff}");
                if (_stagingWorkers == null) {
                    _stagingWorkers = new Dictionary<Staging, BackgroundWorker> ();
                }

                _stagingWorkers.Add (staging, worker);
                worker.RunWorkerAsync ();
                stagingUI.Dispose ();
            }
        }

        void StagingUI_ReportClosed (StagingUIController obj) {
            WriteLog ($"Report {obj.Staging.StartStaging}.{obj.Staging.StartStaging:fff} closed.");
            _stagingsUi.Remove (obj);
        }

        public void CancelWorker () {
            if (_stagingWorkers == null || _stagingWorkers.Count == 0) {
                WriteLog ($"Worker is not running at {DateTime.Now}!");
                return;
            }

            foreach (var sw in _stagingWorkers) {
                var key = sw.Key;
                var value = sw.Value;
                if (!key.Completed) {
                    value.CancelAsync ();
                }

                WriteLog (
                    $"Worker {key.StartStaging}.{key.StartStaging:fff} is canceling at {DateTime.Now}!");

                _stagingWorkers.Remove (sw.Key);
            }
        }

        public void WriteLog (string msg) {
            _log.Add (msg);
            _listLog.MoveDown ();
        }
    }

    class StagingUIController : Window {
        readonly Button _close;
        readonly Label _label;
        readonly ListView _listView;
        readonly Button _start;

        public StagingUIController (Staging staging, List<string> list) : this () {
            Staging = staging;
            _label.Text = "Work list:";
            _listView.SetSource (list);
            _start.Visible = false;
            Id = "";
        }

        public StagingUIController () {
            X = Pos.Center ();
            Y = Pos.Center ();
            Width = Dim.Percent (85);
            Height = Dim.Percent (85);

            ColorScheme = Colors.ColorSchemes["Dialog"];

            Title = "Run Worker";

            _label = new Label {
                X = Pos.Center (),
                Y = 1,
                ColorScheme = Colors.ColorSchemes["Dialog"],
                Text = "Press start to do the work or close to quit."
            };
            Add (_label);

            _listView = new ListView {
                X = 0,
                Y = 2,
                Width = Dim.Fill (),
                Height = Dim.Fill (2)
            };
            Add (_listView);

            _start = new Button { Text = "Start", IsDefault = true, ClearOnVisibleFalse = false };
            _start.Clicked += (s, e) => {
                Staging = new Staging (DateTime.Now);
                RequestStop ();
            };
            Add (_start);

            _close = new Button { Text = "Close" };
            _close.Clicked += OnReportClosed;
            Add (_close);

            KeyDown += (s, e) => {
                if (e.KeyCode == KeyCode.Esc) {
                    OnReportClosed (this, EventArgs.Empty);
                }
            };

            LayoutStarted += (s, e) => {
                var btnsWidth = _start.Frame.Width + _close.Frame.Width + 2 - 1;
                var shiftLeft = Math.Max (((Bounds.Width - btnsWidth) / 2) - 2, 0);

                shiftLeft += _close.Frame.Width + 1;
                _close.X = Pos.AnchorEnd (shiftLeft);
                _close.Y = Pos.AnchorEnd (1);

                shiftLeft += _start.Frame.Width + 1;
                _start.X = Pos.AnchorEnd (shiftLeft);
                _start.Y = Pos.AnchorEnd (1);
            };
        }

        public Staging Staging { get; private set; }

        public event Action<StagingUIController> ReportClosed;

        void OnReportClosed (object sender, EventArgs e) {
            if (Staging?.StartStaging != null) {
                ReportClosed?.Invoke (this);
            }

            RequestStop ();
        }

        public void Run () => Application.Run (this);
    }

    class Staging {
        public Staging (DateTime? startStaging, bool completed = false) {
            StartStaging = startStaging;
            Completed = completed;
        }

        public DateTime? StartStaging { get; }
        public bool Completed { get; }
    }
}