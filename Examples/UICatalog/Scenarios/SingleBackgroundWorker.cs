using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Single BackgroundWorker", "A single BackgroundWorker threading opening another Toplevel")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Arrangement")]
[ScenarioCategory ("Runnable")]
public class SingleBackgroundWorker : Scenario
{
    public override void Main ()
    {
        Application.Run<MainApp> ().Dispose ();
        Application.Shutdown ();
    }

    public class MainApp : Toplevel
    {
        private readonly ListView _listLog;
        private readonly ObservableCollection<string> _log = [];
        private DateTime? _startStaging;
        private BackgroundWorker _worker;

        public MainApp ()
        {
            var menu = new MenuBar
            {
                Menus =
                [
                    new (
                         "_Options",
                         new MenuItem []
                         {
                             new (
                                  "_Run Worker",
                                  "",
                                  () => RunWorker (),
                                  null,
                                  null,
                                  KeyCode.CtrlMask | KeyCode.R
                                 ),
                             null,
                             new (
                                  "_Quit",
                                  "",
                                  () => Application.RequestStop (),
                                  null,
                                  null,
                                  Application.QuitKey
                                 )
                         }
                        )
                ]
            };

            var statusBar = new StatusBar (
                                           [
                                               new (Application.QuitKey, "Quit", () => Application.RequestStop ()),
                                               new (Key.R.WithCtrl, "Run Worker", RunWorker)
                                           ]);

            var workerLogTop = new Toplevel
            {
                Title = "Worker Log Top"
            };

            workerLogTop.Add (
                              new Label { X = Pos.Center (), Y = 0, Text = "Worker Log" }
                             );

            _listLog = new ()
            {
                X = 0,
                Y = 2,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Source = new ListWrapper<string> (_log)
            };
            workerLogTop.Add (_listLog);

            workerLogTop.Y = 1;
            workerLogTop.Height = Dim.Fill (Dim.Func (() => statusBar.Frame.Height));

            Add (menu, workerLogTop, statusBar);
            Title = "MainApp";
        }

        private void RunWorker ()
        {
            _worker = new () { WorkerSupportsCancellation = true };

            var cancel = new Button { Text = "Cancel Worker" };

            cancel.Accepting += (s, e) =>
                                {
                                    if (_worker == null)
                                    {
                                        _log.Add ($"Worker is not running at {DateTime.Now}!");
                                        _listLog.SetNeedsDraw ();

                                        return;
                                    }

                                    _log.Add (
                                              $"Worker {_startStaging}.{_startStaging:fff} is canceling at {DateTime.Now}!"
                                             );
                                    _listLog.SetNeedsDraw ();
                                    _worker.CancelAsync ();
                                };

            _startStaging = DateTime.Now;
            _log.Add ($"Worker is started at {_startStaging}.{_startStaging:fff}");
            _listLog.SetNeedsDraw ();

            var md = new Dialog
            {
                Title = $"Running Worker started at {_startStaging}.{_startStaging:fff}", Buttons = [cancel]
            };

            md.Add (
                    new Label { X = Pos.Center (), Y = Pos.Center (), Text = "Wait for worker to finish..." }
                   );

            _worker.DoWork += (s, e) =>
                              {
                                  List<string> stageResult = new ();

                                  for (var i = 0; i < 200; i++)
                                  {
                                      stageResult.Add ($"Worker {i} started at {DateTime.Now}");
                                      e.Result = stageResult;
                                      Thread.Sleep (1);

                                      if (_worker.CancellationPending)
                                      {
                                          e.Cancel = true;

                                          return;
                                      }
                                  }
                              };

            _worker.RunWorkerCompleted += (s, e) =>
                                          {
                                              if (md.IsCurrentTop)
                                              {
                                                  //Close the dialog
                                                  Application.RequestStop ();
                                              }

                                              if (e.Error != null)
                                              {
                                                  // Failed
                                                  _log.Add (
                                                            $"Exception occurred {e.Error.Message} on Worker {_startStaging}.{_startStaging:fff} at {DateTime.Now}"
                                                           );
                                                  _listLog.SetNeedsDraw ();
                                              }
                                              else if (e.Cancelled)
                                              {
                                                  // Canceled
                                                  _log.Add (
                                                            $"Worker {_startStaging}.{_startStaging:fff} was canceled at {DateTime.Now}!"
                                                           );
                                                  _listLog.SetNeedsDraw ();
                                              }
                                              else
                                              {
                                                  // Passed
                                                  _log.Add (
                                                            $"Worker {_startStaging}.{_startStaging:fff} was completed at {DateTime.Now}."
                                                           );
                                                  _listLog.SetNeedsDraw ();
                                                  Application.LayoutAndDraw ();

                                                  var builderUI =
                                                      new StagingUIController (_startStaging, e.Result as ObservableCollection<string>);
                                                  Toplevel top = Application.Top;
                                                  top.Visible = false;
                                                  Application.Top.Visible = false;
                                                  builderUI.Load ();
                                                  builderUI.Dispose ();
                                                  top.Visible = true;
                                              }

                                              _worker = null;
                                          };
            _worker.RunWorkerAsync ();
            Application.Run (md);
            md.Dispose ();
        }
    }

    public class StagingUIController : Window
    {
        private Toplevel _top;

        public StagingUIController (DateTime? start, ObservableCollection<string> list)
        {
            _top = new ()
            {
                Title = "_top", Width = Dim.Fill (), Height = Dim.Fill (), Modal = true
            };

            _top.KeyDown += (s, e) =>
                            {
                                // Prevents App.QuitKey from closing this.
                                // Only Ctrl+C is allowed.
                                if (e == Application.QuitKey)
                                {
                                    e.Handled = true;
                                }
                            };

            bool Close ()
            {
                int n = MessageBox.Query (
                                          50,
                                          7,
                                          "Close Window.",
                                          "Are you sure you want to close this window?",
                                          "Yes",
                                          "No"
                                         );

                return n == 0;
            }

            var menu = new MenuBar
            {
                Menus =
                [
                    new (
                         "_Stage",
                         new MenuItem []
                         {
                             new (
                                  "_Close",
                                  "",
                                  () =>
                                  {
                                      if (Close ())
                                      {
                                          Application.RequestStop ();
                                      }
                                  },
                                  null,
                                  null,
                                  KeyCode.CtrlMask | KeyCode.C
                                 )
                         }
                        )
                ]
            };
            _top.Add (menu);

            var statusBar = new StatusBar (
                                           [
                                               new (
                                                    Key.C.WithCtrl,
                                                    "Close",
                                                    () =>
                                                    {
                                                        if (Close ())
                                                        {
                                                            Application.RequestStop ();
                                                        }
                                                    }
                                                   )
                                           ]);
            _top.Add (statusBar);

            Y = 1;
            Height = Dim.Fill (1);
            Title = $"Worker started at {start}.{start:fff}";
            ColorScheme = Colors.ColorSchemes ["Base"];

            Add (
                 new ListView
                 {
                     X = 0,
                     Y = 0,
                     Width = Dim.Fill (),
                     Height = Dim.Fill (),
                     Source = new ListWrapper<string> (list)
                 }
                );

            _top.Add (this);
        }

        public void Load ()
        {
            Application.Run (_top);
            _top.Dispose ();
            _top = null;
        }
    }
}
