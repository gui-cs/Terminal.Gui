using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Threading", "Demonstration of how to use threading in different ways")]
[ScenarioCategory ("Threading")]
public class Threading : Scenario
{
    private readonly List<string> _log = [];
    private Action _action;
    private Button _btnActionCancel;
    private CancellationTokenSource _cancellationTokenSource;
    private EventHandler _handler;
    private ListView _itemsList;
    private Action _lambda;
    private ListView _logJob;
    private Action _sync;

    public override void Setup ()
    {
        _action = LoadData;

        _lambda = async () =>
                  {
                      _itemsList.Source = null;
                      LogJob ("Loading task lambda");
                      List<string> items = await LoadDataAsync ();
                      LogJob ("Returning from task lambda");
                      await _itemsList.SetSourceAsync (items);
                  };

        _handler = async (s, e) =>
                   {
                       _itemsList.Source = null;
                       LogJob ("Loading task handler");
                       List<string> items = await LoadDataAsync ();
                       LogJob ("Returning from task handler");
                       await _itemsList.SetSourceAsync (items);
                   };

        _sync = () =>
                {
                    _itemsList.Source = null;
                    LogJob ("Loading task synchronous");

                    List<string> items =
                        ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"];
                    LogJob ("Returning from task synchronous");
                    _itemsList.SetSource (items);
                };

        _btnActionCancel = new Button { X = 1, Y = 1, Text = "Cancelable Load Items" };
        _btnActionCancel.Accept += (s, e) => Application.Invoke (CallLoadItemsAsync);

        Win.Add (new Label { X = Pos.X (_btnActionCancel), Y = Pos.Y (_btnActionCancel) + 4, Text = "Data Items:" });

        _itemsList = new ListView
        {
            X = Pos.X (_btnActionCancel),
            Y = Pos.Y (_btnActionCancel) + 6,
            Width = 10,
            Height = 10,
            ColorScheme = Colors.ColorSchemes ["TopLevel"]
        };

        Win.Add (new Label { X = Pos.Right (_itemsList) + 10, Y = Pos.Y (_btnActionCancel) + 4, Text = "Task Logs:" });

        _logJob = new ListView
        {
            X = Pos.Right (_itemsList) + 10,
            Y = Pos.Y (_itemsList),
            Width = 50,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper (_log)
        };

        var text = new TextField { X = 1, Y = 3, Width = 100, Text = "Type anything after press the button" };

        var btnAction = new Button { X = 80, Y = 10, Text = "Load Data Action" };
        btnAction.Accept += (s, e) => _action.Invoke ();
        var btnLambda = new Button { X = 80, Y = 12, Text = "Load Data Lambda" };
        btnLambda.Accept += (s, e) => _lambda.Invoke ();
        var btnHandler = new Button { X = 80, Y = 14, Text = "Load Data Handler" };
        btnHandler.Accept += (s, e) => _handler.Invoke (null, EventArgs.Empty);
        var btnSync = new Button { X = 80, Y = 16, Text = "Load Data Synchronous" };
        btnSync.Accept += (s, e) => _sync.Invoke ();
        var btnMethod = new Button { X = 80, Y = 18, Text = "Load Data Method" };
        btnMethod.Accept += async (s, e) => await MethodAsync ();
        var btnClearData = new Button { X = 80, Y = 20, Text = "Clear Data" };

        btnClearData.Accept += (s, e) =>
                                {
                                    _itemsList.Source = null;
                                    LogJob ("Cleaning Data");
                                };
        var btnQuit = new Button { X = 80, Y = 22, Text = "Quit" };
        btnQuit.Accept += (s, e) => Application.RequestStop ();

        Win.Add (
                 _itemsList,
                 _btnActionCancel,
                 _logJob,
                 text,
                 btnAction,
                 btnLambda,
                 btnHandler,
                 btnSync,
                 btnMethod,
                 btnClearData,
                 btnQuit
                );

        void Top_Loaded (object sender, EventArgs args)
        {
            _btnActionCancel.SetFocus ();
            Top.Loaded -= Top_Loaded;
        }

        Top.Loaded += Top_Loaded;
    }

    private async void CallLoadItemsAsync ()
    {
        _cancellationTokenSource = new CancellationTokenSource ();
        _itemsList.Source = null;
        LogJob ("Clicked the button");

        if (_btnActionCancel.Text != "Cancel")
        {
            _btnActionCancel.Text = "Cancel";
        }
        else
        {
            _btnActionCancel.Text = "Cancelable Load Items";
            await _cancellationTokenSource.CancelAsync ();
        }

        try
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested ();
            }

            LogJob ($"Calling task Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");
            List<string> items = await Task.Run (LoadItemsAsync, _cancellationTokenSource.Token);

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                LogJob (
                        $"Returned from task Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}"
                       );
                await _itemsList.SetSourceAsync (items);

                LogJob (
                        $"Finished populate list view Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}"
                       );
                _btnActionCancel.Text = "Cancelable Load Items";
            }
            else
            {
                LogJob ("Task was canceled!");
            }
        }
        catch (OperationCanceledException ex)
        {
            LogJob (ex.Message);
        }
    }

    private async void LoadData ()
    {
        _itemsList.Source = null;
        LogJob ("Loading task");
        List<string> items = await LoadDataAsync ();
        LogJob ("Returning from task");
        await _itemsList.SetSourceAsync (items);
    }

    private async Task<List<string>> LoadDataAsync ()
    {
        _itemsList.Source = null;
        LogJob ("Starting delay");
        await Task.Delay (3000);
        LogJob ("Finished delay");

        return
        [
            "One",
            "Two",
            "Three",
            "Four",
            "Five",
            "Six",
            "Seven",
            "Eight",
            "Nine",
            "Ten",
            "Four",
            "Five",
            "Six",
            "Seven",
            "Eight",
            "Nine",
            "Ten"
        ];
    }

    private async Task<List<string>> LoadItemsAsync ()
    {
        // Do something that takes lot of times.
        LogJob ($"Starting delay Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");
        await Task.Delay (5000);
        LogJob ($"Finished delay Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");

        return ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"];
    }

    private void LogJob (string job)
    {
        _log.Add (job);
        _logJob.MoveDown ();
    }

    private async Task MethodAsync ()
    {
        _itemsList.Source = null;
        LogJob ("Loading task method");
        List<string> items = ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"];
        await Task.Delay (3000);
        LogJob ("Returning from task method");
        await _itemsList.SetSourceAsync (items);
        _itemsList.SetNeedsDisplay ();
    }
}
