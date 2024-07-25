using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("BackgroundWorker Collection", "A persisting multi Toplevel BackgroundWorker threading")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Top Level Windows")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Controls")]
public class BackgroundWorkerCollection : Scenario
{
    public override void Main ()
    {
        Application.Run<OverlappedMain> ().Dispose ();

#if DEBUG_IDISPOSABLE
        if (ApplicationOverlapped.OverlappedChildren is { })
        {
            Debug.Assert (ApplicationOverlapped.OverlappedChildren?.Count == 0);
            Debug.Assert (Application.Top == ApplicationOverlapped.OverlappedTop);
        }
#endif

        Application.Shutdown ();
    }

    private class OverlappedMain : Toplevel
    {
        private readonly MenuBar _menu;
        private WorkerApp _workerApp;

        public OverlappedMain ()
        {
            Arrangement = ViewArrangement.Movable;
            Data = "OverlappedMain";

            IsOverlappedContainer = true;

            _workerApp = new WorkerApp { Visible = false };
            _workerApp.Border.Thickness = new (0, 1, 0, 0);
            _workerApp.Border.LineStyle = LineStyle.Dashed;

            _menu = new MenuBar
            {
                Menus =
                [
                    new MenuBarItem (
                                     "_Options",
                                     new MenuItem []
                                     {
                                         new (
                                              "_Run Worker",
                                              "",
                                              () => _workerApp.RunWorker (),
                                              null,
                                              null,
                                              KeyCode.CtrlMask | KeyCode.R
                                             ),
                                         new (
                                              "_Cancel Worker",
                                              "",
                                              () => _workerApp.CancelWorker (),
                                              null,
                                              null,
                                              KeyCode.CtrlMask | KeyCode.C
                                             ),
                                         null,
                                         new (
                                              "_Quit",
                                              "",
                                              () => Quit (),
                                              null,
                                              null,
                                              (KeyCode)Application.QuitKey
                                             )
                                     }
                                    ),
                    new MenuBarItem ("_View", new MenuItem [] { }),
                    new MenuBarItem ("_Window", new MenuItem [] { })
                ]
            };
            ;
            _menu.MenuOpening += Menu_MenuOpening;
            Add (_menu);
            var statusBar = new StatusBar (
                                           new []
                                           {
                                               new Shortcut (Application.QuitKey, $"Quit", Quit),
                                               new Shortcut (
                                                             Key.R.WithCtrl,
                                                             "Run Worker",
                                                             () => _workerApp.RunWorker ()
                                                            ),
                                               new Shortcut (
                                                             Key.C.WithCtrl,
                                                             "Cancel Worker",
                                                             () => _workerApp.CancelWorker ()
                                                            )
                                           }
                                          );
            Add (statusBar);
            Ready += OverlappedMain_Ready;
            Activate += OverlappedMain_Activate;
            Deactivate += OverlappedMain_Deactivate;
        }

        private void OverlappedMain_Ready (object sender, EventArgs e)
        {
            if (_workerApp?.Running == false)
            {
                Application.Run (_workerApp);
            }
        }

        private void Menu_MenuOpening (object sender, MenuOpeningEventArgs menu)
        {
            if (menu.CurrentMenu.Title == "_Window")
            {
                menu.NewMenuBarItem = OpenedWindows ();
            }
            else if (menu.CurrentMenu.Title == "_View")
            {
                menu.NewMenuBarItem = View ();
            }
        }

        private MenuBarItem OpenedWindows ()
        {
            var index = 1;
            List<MenuItem> menuItems = new ();
            List<Toplevel> sortedChildren = ApplicationOverlapped.OverlappedChildren;
            sortedChildren.Sort (new ToplevelComparer ());

            foreach (Toplevel top in sortedChildren)
            {
                if (top.Data.ToString () == "WorkerApp" && !top.Visible)
                {
                    continue;
                }

                var item = new MenuItem ();
                item.Title = top is Window ? $"{index} {((Window)top).Title}" : $"{index} {top.Data}";
                index++;
                item.CheckType |= MenuItemCheckStyle.Checked;
                string topTitle = top is Window ? ((Window)top).Title : top.Data.ToString ();
                string itemTitle = item.Title.Substring (index.ToString ().Length + 1);

                if (top == ApplicationOverlapped.GetTopOverlappedChild () && topTitle == itemTitle)
                {
                    item.Checked = true;
                }
                else
                {
                    item.Checked = false;
                }

                item.Action += () => { ApplicationOverlapped.MoveToOverlappedChild (top); };
                menuItems.Add (item);
            }

            if (menuItems.Count == 0)
            {
                return new MenuBarItem ("_Window", "", null);
            }

            return new MenuBarItem ("_Window", new List<MenuItem []> { menuItems.ToArray () });
        }

        private void OverlappedMain_Activate (object sender, ToplevelEventArgs top)
        {
            _workerApp?.WriteLog ($"{(top.Toplevel is null ? ((Toplevel)sender).Data : top.Toplevel.Data)} activate.");
        }

        private void OverlappedMain_Deactivate (object sender, ToplevelEventArgs top)
        {
            _workerApp?.WriteLog ($"{top.Toplevel.Data} deactivate.");
        }

        private void Quit () { RequestStop (); }

        private MenuBarItem View ()
        {
            List<MenuItem> menuItems = new ();
            var item = new MenuItem { Title = "WorkerApp", CheckType = MenuItemCheckStyle.Checked };
            Toplevel top = ApplicationOverlapped.OverlappedChildren?.Find (x => x.Data.ToString () == "WorkerApp");

            if (top != null)
            {
                item.Checked = top.Visible;
            }

            item.Action += () =>
                           {
                               Toplevel top = ApplicationOverlapped.OverlappedChildren.Find (x => x.Data.ToString () == "WorkerApp");
                               item.Checked = top.Visible = (bool)!item.Checked;

                               if (top.Visible)
                               {
                                   ApplicationOverlapped.MoveToOverlappedChild (top);
                               }
                               else
                               {
                                   ApplicationOverlapped.OverlappedTop!.SetNeedsDisplay ();
                               }
                           };
            menuItems.Add (item);

            return new MenuBarItem (
                                    "_View",
                                    new List<MenuItem []> { menuItems.Count == 0 ? new MenuItem [] { } : menuItems.ToArray () }
                                   );
        }

        /// <inheritdoc />
        protected override void Dispose (bool disposing)
        {
            _workerApp?.Dispose ();
            _workerApp = null;

            base.Dispose (disposing);
        }
    }

    private class Staging
    {
        public Staging (DateTime? startStaging, bool completed = false)
        {
            StartStaging = startStaging;
            Completed = completed;
        }

        public bool Completed { get; }
        public DateTime? StartStaging { get; }
    }

    private class StagingUIController : Window
    {
        private readonly Button _close;
        private readonly Label _label;
        private readonly ListView _listView;
        private readonly Button _start;

        public StagingUIController (Staging staging, ObservableCollection<string> list) : this ()
        {
            Staging = staging;
            _label.Text = "Work list:";
            _listView.Enabled = true;
            _listView.SetSource (list);
            _start.Visible = false;
            Id = "";
        }

        public StagingUIController ()
        {
            Arrangement = ViewArrangement.Movable;

            X = Pos.Center ();
            Y = Pos.Center ();
            Width = Dim.Percent (85);
            Height = Dim.Percent (85);

            ColorScheme = Colors.ColorSchemes ["Dialog"];

            Title = "Run Worker";

            _label = new Label
            {
                X = Pos.Center (),
                Y = 1,
                ColorScheme = Colors.ColorSchemes ["Dialog"],
                Text = "Press start to do the work or close to quit."
            };
            Add (_label);

            _listView = new ListView { X = 0, Y = 2, Width = Dim.Fill (), Height = Dim.Fill (2), Enabled = false };
            Add (_listView);

            _start = new Button { Text = "Start", IsDefault = true, ClearOnVisibleFalse = false };

            _start.Accept += (s, e) =>
                              {
                                  Staging = new Staging (DateTime.Now);
                                  RequestStop ();
                              };
            Add (_start);

            _close = new Button { Text = "Close" };
            _close.Accept += OnReportClosed;
            Add (_close);

            KeyDown += (s, e) =>
                       {
                           if (e == Application.QuitKey)
                           {
                               OnReportClosed (this, EventArgs.Empty);
                           }
                       };

            LayoutStarted += (s, e) =>
                             {
                                 int btnsWidth = _start.Frame.Width + _close.Frame.Width + 2 - 1;
                                 int shiftLeft = Math.Max ((Viewport.Width - btnsWidth) / 2 - 2, 0);

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

        private void OnReportClosed (object sender, EventArgs e)
        {
            if (Staging?.StartStaging != null)
            {
                ReportClosed?.Invoke (this);
            }

            RequestStop ();
        }
    }

    private class WorkerApp : Toplevel
    {
        private readonly ListView _listLog;
        private readonly ObservableCollection<string> _log = [];
        private List<StagingUIController> _stagingsUi;
        private Dictionary<Staging, BackgroundWorker> _stagingWorkers;

        public WorkerApp ()
        {
            Arrangement = ViewArrangement.Movable;

            Data = "WorkerApp";
            Title = "Worker collection Log";

            Width = Dim.Percent (80);
            Height = Dim.Percent (50);

            ColorScheme = Colors.ColorSchemes ["Base"];

            _listLog = new ListView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Source = new ListWrapper<string> (_log)
            };
            Add (_listLog);

            // We don't want WorkerApp to respond to the quitkey
            KeyBindings.Remove (Application.QuitKey);

            Closing += WorkerApp_Closing;
            Closed += WorkerApp_Closed;
        }

        private void WorkerApp_Closed (object sender, ToplevelEventArgs e)
        {
            CancelWorker ();
        }
        private void WorkerApp_Closing (object sender, ToplevelClosingEventArgs e)
        {
            Toplevel top = ApplicationOverlapped.OverlappedChildren!.Find (x => x.Data.ToString () == "WorkerApp");

            if (Visible && top == this)
            {
                Visible = false;
                e.Cancel = true;

                ApplicationOverlapped.OverlappedMoveNext ();
            }
        }

        public void CancelWorker ()
        {
            if (_stagingWorkers == null || _stagingWorkers.Count == 0)
            {
                WriteLog ($"Worker is not running at {DateTime.Now}!");

                return;
            }

            foreach (KeyValuePair<Staging, BackgroundWorker> sw in _stagingWorkers)
            {
                Staging key = sw.Key;
                BackgroundWorker value = sw.Value;

                if (!key.Completed)
                {
                    value.CancelAsync ();
                }

                WriteLog (
                          $"Worker {key.StartStaging}.{key.StartStaging:fff} is canceling at {DateTime.Now}!"
                         );

                _stagingWorkers.Remove (sw.Key);
            }
        }

        public void RunWorker ()
        {
            var stagingUI = new StagingUIController { Modal = true };

            Staging staging = null;
            var worker = new BackgroundWorker { WorkerSupportsCancellation = true };

            worker.DoWork += (s, e) =>
                             {
                                 List<string> stageResult = new ();

                                 for (var i = 0; i < 500; i++)
                                 {
                                     stageResult.Add (
                                                      $"Worker {i} started at {DateTime.Now}"
                                                     );
                                     e.Result = stageResult;
                                     Thread.Sleep (1);

                                     if (worker.CancellationPending)
                                     {
                                         e.Cancel = true;

                                         return;
                                     }
                                 }
                             };

            worker.RunWorkerCompleted += (s, e) =>
                                         {
                                             if (e.Error != null)
                                             {
                                                 // Failed
                                                 WriteLog (
                                                           $"Exception occurred {e.Error.Message} on Worker {staging.StartStaging}.{staging.StartStaging:fff} at {DateTime.Now}"
                                                          );
                                             }
                                             else if (e.Cancelled)
                                             {
                                                 // Canceled
                                                 WriteLog (
                                                           $"Worker {staging.StartStaging}.{staging.StartStaging:fff} was canceled at {DateTime.Now}!"
                                                          );
                                             }
                                             else
                                             {
                                                 // Passed
                                                 WriteLog (
                                                           $"Worker {staging.StartStaging}.{staging.StartStaging:fff} was completed at {DateTime.Now}."
                                                          );
                                                 Application.Refresh ();

                                                 var stagingUI = new StagingUIController (staging, e.Result as ObservableCollection<string>)
                                                 {
                                                     Modal = false,
                                                     Title =
                                                         $"Worker started at {staging.StartStaging}.{staging.StartStaging:fff}",
                                                     Data = $"{staging.StartStaging}.{staging.StartStaging:fff}"
                                                 };

                                                 stagingUI.ReportClosed += StagingUI_ReportClosed;

                                                 if (_stagingsUi == null)
                                                 {
                                                     _stagingsUi = new List<StagingUIController> ();
                                                 }

                                                 _stagingsUi.Add (stagingUI);
                                                 _stagingWorkers.Remove (staging);
#if DEBUG_IDISPOSABLE
                                                 if (ApplicationOverlapped.OverlappedTop is null)
                                                 {
                                                     stagingUI.Dispose ();
                                                     return;
                                                 }
#endif
                                                 Application.Run (stagingUI);
                                             }
                                         };

            Application.Run (stagingUI);

            if (stagingUI.Staging != null && stagingUI.Staging.StartStaging != null)
            {
                staging = new Staging (stagingUI.Staging.StartStaging);
                stagingUI.Dispose ();
                WriteLog ($"Worker is started at {staging.StartStaging}.{staging.StartStaging:fff}");

                if (_stagingWorkers == null)
                {
                    _stagingWorkers = new Dictionary<Staging, BackgroundWorker> ();
                }

                _stagingWorkers.Add (staging, worker);
                worker.RunWorkerAsync ();
            }
            else
            {
                stagingUI.Dispose ();
            }
        }

        public void WriteLog (string msg)
        {
            _log.Add (msg);
            _listLog.MoveDown ();
        }

        private void StagingUI_ReportClosed (StagingUIController obj)
        {
            WriteLog ($"Report {obj.Staging.StartStaging}.{obj.Staging.StartStaging:fff} closed.");
            _stagingsUi.Remove (obj);
            obj.Dispose ();
        }
    }
}
