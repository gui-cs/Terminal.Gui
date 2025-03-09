using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Terminal.Gui;
using UnitTests;
using UICatalog;
using Xunit.Abstractions;

namespace IntegrationTests.UICatalog;

public class ScenarioTests : TestsAllViews
{
    public ScenarioTests (ITestOutputHelper output)
    {
#if DEBUG_IDISPOSABLE
        View.DebugIDisposable = true;
        View.Instances.Clear ();
#endif
        _output = output;
    }

    private readonly ITestOutputHelper _output;

    private object _timeoutLock;

    /// <summary>
    ///     <para>This runs through all Scenarios defined in UI Catalog, calling Init, Setup, and Run.</para>
    ///     <para>Should find any Scenarios which crash on load or do not respond to <see cref="Application.RequestStop()"/>.</para>
    /// </summary>
    [Theory]
    [MemberData (nameof (AllScenarioTypes))]
    public void All_Scenarios_Quit_And_Init_Shutdown_Properly (Type scenarioType)
    {
        Assert.Null (_timeoutLock);
        _timeoutLock = new ();

        // Disable any UIConfig settings
        ConfigLocations savedConfigLocations = ConfigurationManager.Locations;
        ConfigurationManager.Locations = ConfigLocations.Default;

        // If a previous test failed, this will ensure that the Application is in a clean state
        Application.ResetState (true);

        _output.WriteLine ($"Running Scenario '{scenarioType}'");
        var scenario = (Scenario)Activator.CreateInstance (scenarioType);

        uint abortTime = 1500;
        object timeout = null;
        var initialized = false;
        var shutdown = false;
        int iterationCount = 0;

        Application.InitializedChanged += OnApplicationOnInitializedChanged;

        Application.ForceDriver = "FakeDriver";
        scenario.Main ();
        scenario.Dispose ();
        scenario = null;
        Application.ForceDriver = string.Empty;

        Application.InitializedChanged -= OnApplicationOnInitializedChanged;

        lock (_timeoutLock)
        {
            if (timeout is { })
            {
                timeout = null;
            }
        }

        Assert.True (initialized);
        Assert.True (shutdown);

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif

        lock (_timeoutLock)
        {
            _timeoutLock = null;
        }

        // Restore the configuration locations
        ConfigurationManager.Locations = savedConfigLocations;
        ConfigurationManager.Reset ();
        return;

        void OnApplicationOnInitializedChanged (object s, EventArgs<bool> a)
        {
            if (a.CurrentValue)
            {
                Application.Iteration += OnApplicationOnIteration;
                initialized = true;

                lock (_timeoutLock)
                {
                    timeout = Application.AddTimeout (TimeSpan.FromMilliseconds (abortTime), ForceCloseCallback);
                }

            }
            else
            {
                Application.Iteration -= OnApplicationOnIteration;
                shutdown = true;
            }
            _output.WriteLine ($"Initialized == {a.CurrentValue}");
        }

        // If the scenario doesn't close within 500ms, this will force it to quit
        bool ForceCloseCallback ()
        {
            lock (_timeoutLock)
            {
                if (timeout is { })
                {
                    timeout = null;
                }
            }

            Assert.Fail (
                         $"'{scenario.GetName ()}' failed to Quit with {Application.QuitKey} after {abortTime}ms and {iterationCount} iterations. Force quit.");

            // Restore the configuration locations
            ConfigurationManager.Locations = savedConfigLocations;
            ConfigurationManager.Reset ();

            Application.ResetState (true);

            return false;
        }

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            iterationCount++;
            if (Application.Initialized)
            {
                // Press QuitKey 
                _output.WriteLine ($"Attempting to quit with {Application.QuitKey}");
                Application.RaiseKeyDownEvent (Application.QuitKey);
            }
        }
    }

    
    public static IEnumerable<object []> AllScenarioTypes =>
        typeof (Scenario).Assembly
                         .GetTypes ()
                         .Where (type => type.IsClass && !type.IsAbstract && type.IsSubclassOf (typeof (Scenario)))
                         .Select (type => new object [] { type });

    [Fact]
    public void Run_All_Views_Tester_Scenario ()
    {
        // Disable any UIConfig settings
        ConfigLocations savedConfigLocations = ConfigurationManager.Locations;
        ConfigurationManager.Locations = ConfigLocations.Default;

        Window _leftPane;
        ListView _classListView;
        FrameView _hostPane;

        Dictionary<string, Type> _viewClasses;
        View _curView = null;

        // Settings
        FrameView _settingsPane;
        FrameView _locationFrame;
        RadioGroup _xRadioGroup;
        TextField _xText;
        var _xVal = 0;
        RadioGroup _yRadioGroup;
        TextField _yText;
        var _yVal = 0;

        FrameView _sizeFrame;
        RadioGroup _wRadioGroup;
        TextField _wText;
        var _wVal = 0;
        RadioGroup _hRadioGroup;
        TextField _hText;
        var _hVal = 0;
        List<string> posNames = new () { "Percent", "AnchorEnd", "Center", "Absolute" };
        List<string> dimNames = new () { "Auto", "Percent", "Fill", "Absolute" };

        Application.Init (new FakeDriver ());

        var top = new Toplevel ();

        _viewClasses = ViewTestHelpers.GetAllViewClasses ().ToDictionary (t => t.Name);

        _leftPane = new ()
        {
            Title = "Classes",
            X = 0,
            Y = 0,
            Width = 15,
            Height = Dim.Fill (1), // for status bar
            CanFocus = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"]
        };

        _classListView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            AllowsMarking = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper<string> (new (_viewClasses.Keys.ToList ()))
        };
        _leftPane.Add (_classListView);

        _settingsPane = new ()
        {
            X = Pos.Right (_leftPane),
            Y = 0, // for menu
            Width = Dim.Fill (),
            Height = 10,
            CanFocus = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Title = "Settings"
        };

        var radioItems = new [] { "Percent(x)", "AnchorEnd(x)", "Center", "Absolute(x)" };

        _locationFrame = new ()
        {
            X = 0,
            Y = 0,
            Height = 3 + radioItems.Length,
            Width = 36,
            Title = "Location (Pos)"
        };
        _settingsPane.Add (_locationFrame);

        var label = new Label { X = 0, Y = 0, Text = "x:" };
        _locationFrame.Add (label);
        _xRadioGroup = new () { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        _xText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_xVal}" };
        _locationFrame.Add (_xText);

        _locationFrame.Add (_xRadioGroup);

        radioItems = new [] { "Percent(y)", "AnchorEnd(y)", "Center", "Absolute(y)" };
        label = new () { X = Pos.Right (_xRadioGroup) + 1, Y = 0, Text = "y:" };
        _locationFrame.Add (label);
        _yText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_yVal}" };
        _locationFrame.Add (_yText);
        _yRadioGroup = new () { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        _locationFrame.Add (_yRadioGroup);

        _sizeFrame = new ()
        {
            X = Pos.Right (_locationFrame),
            Y = Pos.Y (_locationFrame),
            Height = 3 + radioItems.Length,
            Width = 40,
            Title = "Size (Dim)"
        };

        radioItems = new [] { "Auto()", "Percent(width)", "Fill(width)", "Absolute(width)" };
        label = new () { X = 0, Y = 0, Text = "width:" };
        _sizeFrame.Add (label);
        _wRadioGroup = new () { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        _wText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_wVal}" };
        _sizeFrame.Add (_wText);
        _sizeFrame.Add (_wRadioGroup);

        radioItems = new [] { "Auto()", "Percent(height)", "Fill(height)", "Absolute(height)" };
        label = new () { X = Pos.Right (_wRadioGroup) + 1, Y = 0, Text = "height:" };
        _sizeFrame.Add (label);
        _hText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_hVal}" };
        _sizeFrame.Add (_hText);

        _hRadioGroup = new () { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        _sizeFrame.Add (_hRadioGroup);

        _settingsPane.Add (_sizeFrame);

        _hostPane = new ()
        {
            X = Pos.Right (_leftPane),
            Y = Pos.Bottom (_settingsPane),
            Width = Dim.Fill (),
            Height = Dim.Fill (1), // + 1 for status bar
            ColorScheme = Colors.ColorSchemes ["Dialog"]
        };

        _classListView.OpenSelectedItem += (s, a) => { _settingsPane.SetFocus (); };

        _classListView.SelectedItemChanged += (s, args) =>
        {
            // Remove existing class, if any
            if (_curView != null)
            {
                _curView.SubviewsLaidOut -= LayoutCompleteHandler;
                _hostPane.Remove (_curView);
                _curView.Dispose ();
                _curView = null;
                _hostPane.FillRect (_hostPane.Viewport);
            }

            _curView = CreateClass (_viewClasses.Values.ToArray () [_classListView.SelectedItem]);
        };

        _xRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);

        _xText.TextChanged += (s, args) =>
        {
            try
            {
                _xVal = int.Parse (_xText.Text);
                DimPosChanged (_curView);
            }
            catch
            { }
        };

        _yText.TextChanged += (s, e) =>
        {
            try
            {
                _yVal = int.Parse (_yText.Text);
                DimPosChanged (_curView);
            }
            catch
            { }
        };

        _yRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);

        _wRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);

        _wText.TextChanged += (s, args) =>
        {
            try
            {
                _wVal = int.Parse (_wText.Text);
                DimPosChanged (_curView);
            }
            catch
            { }
        };

        _hText.TextChanged += (s, args) =>
        {
            try
            {
                _hVal = int.Parse (_hText.Text);
                DimPosChanged (_curView);
            }
            catch
            { }
        };

        _hRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (_curView);

        top.Add (_leftPane, _settingsPane, _hostPane);

        top.LayoutSubviews ();

        _curView = CreateClass (_viewClasses.First ().Value);

        var iterations = 0;

        Application.Iteration += (s, a) =>
        {
            iterations++;

            if (iterations < _viewClasses.Count)
            {
                _classListView.MoveDown ();

                Assert.Equal (
                              _curView.GetType ().Name,
                              _viewClasses.Values.ToArray () [_classListView.SelectedItem].Name
                             );
            }
            else
            {
                Application.RequestStop ();
            }
        };

        Application.Run (top);

        Assert.Equal (_viewClasses.Count, iterations);

        top.Dispose ();
        Application.Shutdown ();

        // Restore the configuration locations
        ConfigurationManager.Locations = savedConfigLocations;
        ConfigurationManager.Reset ();

        void DimPosChanged (View view)
        {
            if (view == null)
            {
                return;
            }

            try
            {
                switch (_xRadioGroup.SelectedItem)
                {
                    case 0:
                        view.X = Pos.Percent (_xVal);

                        break;
                    case 1:
                        view.X = Pos.AnchorEnd (_xVal);

                        break;
                    case 2:
                        view.X = Pos.Center ();

                        break;
                    case 3:
                        view.X = Pos.Absolute (_xVal);

                        break;
                }

                switch (_yRadioGroup.SelectedItem)
                {
                    case 0:
                        view.Y = Pos.Percent (_yVal);

                        break;
                    case 1:
                        view.Y = Pos.AnchorEnd (_yVal);

                        break;
                    case 2:
                        view.Y = Pos.Center ();

                        break;
                    case 3:
                        view.Y = Pos.Absolute (_yVal);

                        break;
                }

                switch (_wRadioGroup.SelectedItem)
                {
                    case 0:
                        view.Width = Dim.Percent (_wVal);

                        break;
                    case 1:
                        view.Width = Dim.Fill (_wVal);

                        break;
                    case 2:
                        view.Width = Dim.Absolute (_wVal);

                        break;
                }

                switch (_hRadioGroup.SelectedItem)
                {
                    case 0:
                        view.Height = Dim.Percent (_hVal);

                        break;
                    case 1:
                        view.Height = Dim.Fill (_hVal);

                        break;
                    case 2:
                        view.Height = Dim.Absolute (_hVal);

                        break;
                }
            }
            catch (Exception e)
            {
                MessageBox.ErrorQuery ("Exception", e.Message, "Ok");
            }

            UpdateTitle (view);
        }

        void UpdateSettings (View view)
        {
            var x = view.X.ToString ();
            var y = view.Y.ToString ();

            try
            {
                _xRadioGroup.SelectedItem = posNames.IndexOf (posNames.First (s => x.Contains (s)));
                _yRadioGroup.SelectedItem = posNames.IndexOf (posNames.First (s => y.Contains (s)));
            }
            catch (InvalidOperationException e)
            {
                // This is a hack to work around the fact that the Pos enum doesn't have an "Align" value yet
                Debug.WriteLine ($"{e}");
            }

            _xText.Text = $"{view.Frame.X}";
            _yText.Text = $"{view.Frame.Y}";

            var w = view.Width.ToString ();
            var h = view.Height.ToString ();

            _wRadioGroup.SelectedItem = dimNames.IndexOf (dimNames.First (s => w.Contains (s)));
            _hRadioGroup.SelectedItem = dimNames.IndexOf (dimNames.First (s => h.Contains (s)));

            _wText.Text = $"{view.Frame.Width}";
            _hText.Text = $"{view.Frame.Height}";
        }

        void UpdateTitle (View view) { _hostPane.Title = $"{view.GetType ().Name} - {view.X}, {view.Y}, {view.Width}, {view.Height}"; }

        View CreateClass (Type type)
        {
            // If we are to create a generic Type
            if (type.IsGenericType)
            {
                // For each of the <T> arguments
                List<Type> typeArguments = new ();

                // use <object>
                foreach (Type arg in type.GetGenericArguments ())
                {
                    typeArguments.Add (typeof (object));
                }

                // And change what type we are instantiating from MyClass<T> to MyClass<object>
                type = type.MakeGenericType (typeArguments.ToArray ());
            }

            // Instantiate view
            var view = (View)Activator.CreateInstance (type);

            if (view is null)
            {
                return null;
            }

            if (view.Width is not DimAuto)
            {
                view.Width = Dim.Percent (75);
            }

            if (view.Height is not DimAuto)
            {
                view.Height = Dim.Percent (75);
            }

            // Set the colorscheme to make it stand out if is null by default
            view.ColorScheme ??= Colors.ColorSchemes ["Base"];

            // If the view supports a Text property, set it so we have something to look at
            if (view.GetType ().GetProperty ("Text") != null)
            {
                try
                {
                    view.GetType ().GetProperty ("Text")?.GetSetMethod ()?.Invoke (view, new [] { "Test Text" });
                }
                catch (TargetInvocationException e)
                {
                    MessageBox.ErrorQuery ("Exception", e.InnerException.Message, "Ok");
                    view = null;
                }
            }

            // If the view supports a Title property, set it so we have something to look at
            if (view != null && view.GetType ().GetProperty ("Title") != null)
            {
                if (view.GetType ().GetProperty ("Title").PropertyType == typeof (string))
                {
                    view?.GetType ().GetProperty ("Title")?.GetSetMethod ()?.Invoke (view, new [] { "Test Title" });
                }
                else
                {
                    view?.GetType ().GetProperty ("Title")?.GetSetMethod ()?.Invoke (view, new [] { "Test Title" });
                }
            }

            // If the view supports a Source property, set it so we have something to look at
            if (view != null
                && view.GetType ().GetProperty ("Source") != null
                && view.GetType ().GetProperty ("Source").PropertyType == typeof (IListDataSource))
            {
                ListWrapper<string> source = new (["Test Text #1", "Test Text #2", "Test Text #3"]);
                view?.GetType ().GetProperty ("Source")?.GetSetMethod ()?.Invoke (view, new [] { source });
            }

            // Add
            _hostPane.Add (view);

            //DimPosChanged ();
            _hostPane.LayoutSubviews ();
            _hostPane.ClearViewport ();
            _hostPane.SetNeedsDraw ();
            UpdateSettings (view);
            UpdateTitle (view);

            view.SubviewsLaidOut += LayoutCompleteHandler;

            return view;
        }

        void LayoutCompleteHandler (object sender, LayoutEventArgs args) { UpdateTitle (_curView); }
    }

    [Fact]
    public void Run_Generic ()
    {
        // Disable any UIConfig settings
        ConfigLocations savedConfigLocations = ConfigurationManager.Locations;
        ConfigurationManager.Locations = ConfigLocations.Default;

        ObservableCollection<Scenario> scenarios = Scenario.GetScenarios ();
        Assert.NotEmpty (scenarios);

        int item = scenarios.IndexOf (s => s.GetName ().Equals ("Generic", StringComparison.OrdinalIgnoreCase));
        Scenario generic = scenarios [item];

        Application.Init (new FakeDriver ());

        // BUGBUG: (#2474) For some reason ReadKey is not returning the QuitKey for some Scenarios
        // by adding this Space it seems to work.

        FakeConsole.PushMockKeyPress ((KeyCode)Application.QuitKey);

        var ms = 100;
        var abortCount = 0;

        Func<bool> abortCallback = () =>
        {
            abortCount++;
            _output.WriteLine ($"'Generic' abortCount {abortCount}");
            Application.RequestStop ();

            return false;
        };

        var iterations = 0;
        object token = null;

        Application.Iteration += (s, a) =>
        {
            if (token == null)
            {
                // Timeout only must start at first iteration
                token = Application.AddTimeout (TimeSpan.FromMilliseconds (ms), abortCallback);
            }

            iterations++;
            _output.WriteLine ($"'Generic' iteration {iterations}");

            // Stop if we run out of control...
            if (iterations == 10)
            {
                _output.WriteLine ("'Generic' had to be force quit!");
                Application.RequestStop ();
            }
        };

        Application.KeyDown += (sender, args) =>
        {
            Assert.Equal (Application.QuitKey, args.KeyCode);
        };

        generic.Main ();

        Assert.Equal (0, abortCount);

        // # of key up events should match # of iterations
        Assert.Equal (1, iterations);

        generic.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();

        // Restore the configuration locations
        ConfigurationManager.Locations = savedConfigLocations;
        ConfigurationManager.Reset ();

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif
    }

    private int CreateInput (string input)
    {
        FakeConsole.MockKeyPresses.Clear ();

        // Put a QuitKey in at the end
        FakeConsole.PushMockKeyPress ((KeyCode)Application.QuitKey);

        foreach (char c in input.Reverse ())
        {
            var key = KeyCode.Null;

            if (char.IsLetter (c))
            {
                key = (KeyCode)char.ToUpper (c) | (char.IsUpper (c) ? KeyCode.ShiftMask : 0);
            }
            else
            {
                key = (KeyCode)c;
            }

            FakeConsole.PushMockKeyPress (key);
        }

        return FakeConsole.MockKeyPresses.Count;
    }
}
