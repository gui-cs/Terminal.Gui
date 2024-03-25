using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Single BackgroundWorker", "A single BackgroundWorker threading opening another Toplevel")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Top Level Windows")]
public class SingleBackgroundWorker : Scenario
{
    public override void Init ()
    {
        Application.Run<MainApp> ();

        Application.Top.Dispose ();
    }

    public override void Run () { }

    public class MainApp : Toplevel
    {
        private readonly ListView _listLog;
        private readonly List<string> _log = new ();
        private DateTime? _startStaging;
        private BackgroundWorker _worker;

        public MainApp ()
        {
            var menu = new MenuBar
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
                                              KeyCode.CtrlMask | KeyCode.Q
                                             )
                                     }
                                    )
                ]
            };
            Add (menu);

            var statusBar = new StatusBar (
                                           new []
                                           {
                                               new StatusItem (
                                                               Application.QuitKey,
                                                               $"{Application.QuitKey} to Quit",
                                                               () => Application.RequestStop ()
                                                              ),
                                               new StatusItem (
                                                               KeyCode.CtrlMask | KeyCode.P,
                                                               "~^R~ Run Worker",
                                                               () => RunWorker ()
                                                              )
                                           }
                                          );
            Add (statusBar);

            var workerLogTop = new Toplevel () { Title = "Worker Log Top"};

            workerLogTop.Add (
                     new Label { X = Pos.Center (), Y = 0, Text = "Worker Log" }
                    );

            _listLog = new ListView
            {
                X = 0,
                Y = 2,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Source = new ListWrapper (_log)
            };
            workerLogTop.Add (_listLog);
            Add (workerLogTop);
            Title = "MainApp";
        }

        private void RunWorker ()
        {
            _worker = new BackgroundWorker { WorkerSupportsCancellation = true };

            var cancel = new Button { Text = "Cancel Worker" };

            cancel.Accept += (s, e) =>
                              {
                                  if (_worker == null)
                                  {
                                      _log.Add ($"Worker is not running at {DateTime.Now}!");
                                      _listLog.SetNeedsDisplay ();

                                      return;
                                  }

                                  _log.Add (
                                            $"Worker {_startStaging}.{_startStaging:fff} is canceling at {DateTime.Now}!"
                                           );
                                  _listLog.SetNeedsDisplay ();
                                  _worker.CancelAsync ();
                              };

            _startStaging = DateTime.Now;
            _log.Add ($"Worker is started at {_startStaging}.{_startStaging:fff}");
            _listLog.SetNeedsDisplay ();

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

                                  for (var i = 0; i < 500; i++)
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
                                                            $"Exception occurred {
                                                                e.Error.Message
                                                            } on Worker {
                                                                _startStaging
                                                            }.{
                                                                _startStaging
                                                                :fff} at {
                                                                DateTime.Now
                                                            }"
                                                           );
                                                  _listLog.SetNeedsDisplay ();
                                              }
                                              else if (e.Cancelled)
                                              {
                                                  // Canceled
                                                  _log.Add (
                                                            $"Worker {_startStaging}.{_startStaging:fff} was canceled at {DateTime.Now}!"
                                                           );
                                                  _listLog.SetNeedsDisplay ();
                                              }
                                              else
                                              {
                                                  // Passed
                                                  _log.Add (
                                                            $"Worker {_startStaging}.{_startStaging:fff} was completed at {DateTime.Now}."
                                                           );
                                                  _listLog.SetNeedsDisplay ();
                                                  Application.Refresh ();

                                                  var builderUI =
                                                      new StagingUIController (_startStaging, e.Result as List<string>);
                                                  var top = Application.Top;
                                                  top.Visible = false;
                                                  Application.Current.Visible = false;
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

        public StagingUIController (DateTime? start, List<string> list)
        {
            _top = new Toplevel
            {
                Title = "_top", Width = Dim.Fill (), Height = Dim.Fill ()
            };

            _top.KeyDown += (s, e) =>
                            {
                                // Prevents Ctrl+Q from closing this.
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
                    new MenuBarItem (
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
                                           new []
                                           {
                                               new StatusItem (
                                                               KeyCode.CtrlMask | KeyCode.C,
                                                               "~^C~ Close",
                                                               () =>
                                                               {
                                                                   if (Close ())
                                                                   {
                                                                       Application.RequestStop ();
                                                                   }
                                                               }
                                                              )
                                           }
                                          );
            _top.Add (statusBar);

            Title = $"Worker started at {start}.{start:fff}";
            ColorScheme = Colors.ColorSchemes ["Base"];

            Add (
                 new ListView
                 {
                     X = 0,
                     Y = 0,
                     Width = Dim.Fill (),
                     Height = Dim.Fill (),
                     Source = new ListWrapper (list)
                 }
                );

            _top.Add (this);
        }

        public void Load () {
            Application.Run (_top);
            _top.Dispose ();
            _top = null;
        }

        ///// <inheritdoc />
        //protected override void Dispose (bool disposing)
        //{
        //    _top?.Dispose ();
        //    _top = null;
        //    base.Dispose (disposing);
        //}
    }
}
