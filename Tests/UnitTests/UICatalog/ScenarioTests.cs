#nullable enable
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using UICatalog;
using Timeout = System.Threading.Timeout;

namespace UnitTests.UICatalog;

public class ScenarioTests : TestsAllViews
{
    public ScenarioTests (ITestOutputHelper output)
    {
#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
        View.Instances.Clear ();
#endif
        _output = output;
    }

    private readonly ITestOutputHelper _output;

    /// <summary>
    ///     <para>This runs through all Scenarios defined in UI Catalog, calling Init, Setup, and Run.</para>
    ///     <para>Should find any Scenarios which crash on load or do not respond to <see cref="Application.RequestStop()"/>.</para>
    /// </summary>
    [Theory]
    [MemberData (nameof (AllScenarioTypes))]
    public void All_Scenarios_Quit_And_Init_Shutdown_Properly (Type scenarioType)
    {
        // Disable on Mac due to random failures related to timing issues
        if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            _output.WriteLine ($"Skipping Scenario '{scenarioType}' on macOS due to random timeout failures.");

            return;
        }

        // Force a complete reset - use ResetModelUsageTracking to allow both models in tests
        ApplicationImpl.ResetModelUsageTracking ();
        CM.Disable (true);

        _output.WriteLine ($"Running Scenario '{scenarioType}'");
        Scenario? scenario = null;
        var scenarioName = string.Empty;

        // Do not use Application.AddTimer for out-of-band watchdogs as
        // they will be stopped by Shutdown/ResetState.
        Timer? watchdogTimer = null;
        var timeoutFired = false;

        // Increase timeout for macOS - it's consistently slower
        uint abortTime = 5000;

        if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            abortTime = 10000;
        }

        var initialized = false;
        var shutdownGracefully = false;
        var iterationCount = 0;
        Key quitKey = Application.QuitKey;

        // Track the current application instance for the modern model
        IApplication? currentApp = null;

        // Track if we've already unsubscribed to prevent double-removal
        var iterationHandlerRemoved = false;

        Exception? scenarioException = null;

        try
        {
            scenario = Activator.CreateInstance (scenarioType) as Scenario;
            scenarioName = scenario!.GetName ();

            // Use thread-local events for modern instance-based model
            Application.InstanceInitialized += OnInstanceInitialized;
            Application.InstanceDisposed += OnInstanceDisposed;

            Application.ForceDriver = DriverRegistry.Names.ANSI;
            scenario!.Main ();
            Application.ForceDriver = string.Empty;
        }
        catch (Exception ex)
        {
            // Catch exceptions to prevent test host crashes
            scenarioException = ex;
            _output.WriteLine ($"Scenario '{scenarioName}' threw exception: {ex}");
        }
        finally
        {
            // Ensure cleanup happens regardless of how we exit
            Application.InstanceInitialized -= OnInstanceInitialized;
            Application.InstanceDisposed -= OnInstanceDisposed;

            // Remove iteration handler if it wasn't removed
            if (!iterationHandlerRemoved && currentApp is { })
            {
                currentApp.Iteration -= OnApplicationOnIteration;
                iterationHandlerRemoved = true;
            }

            watchdogTimer?.Dispose ();

            scenario?.Dispose ();
            scenario = null;

            ConfigurationManager.Disable (true);
        }

        Assert.True (initialized, $"Scenario '{scenarioName}' failed to initialize.");

        if (timeoutFired)
        {
            _output.WriteLine ($"WARNING: Scenario '{scenarioName}' timed out after {abortTime}ms. This may indicate a performance issue on this runner.");
        }

        Assert.True (shutdownGracefully,
                     $"Scenario '{scenarioName}' failed to quit with {quitKey} after {abortTime}ms and {iterationCount} iterations. "
                     + $"TimeoutFired={timeoutFired}");

        // Fail the test if an exception was thrown (but don't crash the test host)
        Assert.Null (scenarioException);

#if DEBUG_IDISPOSABLE
        if (View.Instances.Count > 0)
        {
            foreach (View inst in View.Instances)
            {
                _output.WriteLine ($"Not Disposed: {inst.ToDebugString ()}");
            }
            Assert.Fail ("Views were not disposed properly.");
        }
#endif

        return;

        void OnInstanceInitialized (object? s, EventArgs<IApplication> a)
        {
            currentApp = a.Value;
            currentApp.Iteration += OnApplicationOnIteration;
            initialized = true;

            // Use a System.Threading.Timer for the watchdog to ensure it's not affected by Application.StopAllTimers
            watchdogTimer = new Timer (_ => ForceCloseCallback (), null, (int)abortTime, Timeout.Infinite);

            _output.WriteLine ($"Initialized; shutdownGracefully == {shutdownGracefully}.");
        }

        void OnInstanceDisposed (object? s, EventArgs<IApplication> a)
        {
            // Unsubscribe from Iteration before shutdown assertions
            if (!iterationHandlerRemoved && currentApp is { })
            {
                currentApp.Iteration -= OnApplicationOnIteration;
                iterationHandlerRemoved = true;
            }

            shutdownGracefully = true;
            _output.WriteLine ($"Disposed; shutdownGracefully == {shutdownGracefully}.");
        }

        // If the scenario doesn't close within abortTime ms, this will force it to quit
        void ForceCloseCallback ()
        {
            timeoutFired = true;

            _output.WriteLine ($"TIMEOUT FIRED for {scenarioName} after {abortTime}ms. Attempting graceful shutdown.");

            // Don't call ResetState here - let the finally block handle cleanup
            // Just try to stop the application gracefully
            try
            {
                if (currentApp?.Initialized == true)
                {
                    currentApp.RequestStop ();
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine ($"Exception during timeout callback: {ex.Message}");
            }
        }

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterationCount++;

            if (currentApp?.Initialized == true)
            {
                // Press QuitKey
                quitKey = currentApp.Keyboard.QuitKey;
                _output.WriteLine ($"Attempting to quit with {quitKey} after {iterationCount} iterations.");

                try
                {
                    currentApp.Keyboard.RaiseKeyDownEvent (quitKey);
                }
                catch (Exception ex)
                {
                    _output.WriteLine ($"Exception raising quit key: {ex.Message}");
                }

                currentApp.Iteration -= OnApplicationOnIteration;
                iterationHandlerRemoved = true;
            }
        }
    }

    public static IEnumerable<object []> AllScenarioTypes =>
        typeof (Scenario).Assembly.GetTypes ()
                         .Where (type => type.IsClass && !type.IsAbstract && type.IsSubclassOf (typeof (Scenario)))
                         .Select (type => new object [] { type });

    [Fact]
    public void Run_All_Views_Tester_Scenario ()
    {
        // Reset model usage tracking to allow legacy static model in this test
        ApplicationImpl.ResetModelUsageTracking ();

        // Disable any UIConfig settings
        ConfigurationManager.Disable (true);

        View? curView = null;

        // Settings
        var xVal = 0;
        var yVal = 0;

        var wVal = 0;
        var hVal = 0;
        List<string> posNames = ["Percent", "AnchorEnd", "Center", "Absolute"];
        List<string> dimNames = ["Auto", "Percent", "Fill", "Absolute"];

        Application.Init (DriverRegistry.Names.ANSI);

        var top = new Runnable ();

        Dictionary<string, Type> viewClasses = GetAllViewClasses ().ToDictionary (t => t.Name);

        Window leftPane = new ()
        {
            Title = "Classes",
            X = 0,
            Y = 0,
            Width = 15,
            Height = Dim.Fill (1), // for status bar
            CanFocus = false,
            SchemeName = "Runnable"
        };

        ListView classListView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ShowMarks = false,
            SchemeName = "Runnable",
            Source = new ListWrapper<string> (new ObservableCollection<string> (viewClasses.Keys.ToList ()))
        };
        leftPane.Add (classListView);

        FrameView settingsPane = new ()
        {
            X = Pos.Right (leftPane),
            Y = 0, // for menu
            Width = Dim.Fill (),
            Height = 10,
            CanFocus = false,
            SchemeName = "Runnable",
            Title = "Settings"
        };

        var radioItems = new [] { "Percent(x)", "AnchorEnd(x)", "Center", "Absolute(x)" };

        FrameView locationFrame = new ()
        {
            X = 0,
            Y = 0,
            Height = 3 + radioItems.Length,
            Width = 36,
            Title = "Location (Pos)"
        };
        settingsPane.Add (locationFrame);

        var label = new Label { X = 0, Y = 0, Text = "x:" };
        locationFrame.Add (label);
        OptionSelector xOptionSelector = new () { X = 0, Y = Pos.Bottom (label), Labels = radioItems };
        TextField xText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{xVal}" };
        locationFrame.Add (xText);

        locationFrame.Add (xOptionSelector);

        radioItems = new [] { "Percent(y)", "AnchorEnd(y)", "Center", "Absolute(y)" };
        label = new Label { X = Pos.Right (xOptionSelector) + 1, Y = 0, Text = "y:" };
        locationFrame.Add (label);
        TextField yText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{yVal}" };
        locationFrame.Add (yText);
        OptionSelector yOptionSelector = new () { X = Pos.X (label), Y = Pos.Bottom (label), Labels = radioItems };
        locationFrame.Add (yOptionSelector);

        FrameView sizeFrame = new ()
        {
            X = Pos.Right (locationFrame),
            Y = Pos.Y (locationFrame),
            Height = 3 + radioItems.Length,
            Width = 40,
            Title = "Size (Dim)"
        };

        radioItems = new [] { "Auto()", "Percent(width)", "Fill(width)", "Absolute(width)" };
        label = new Label { X = 0, Y = 0, Text = "width:" };
        sizeFrame.Add (label);
        OptionSelector wOptionSelector = new () { X = 0, Y = Pos.Bottom (label), Labels = radioItems };
        TextField wText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{wVal}" };
        sizeFrame.Add (wText);
        sizeFrame.Add (wOptionSelector);

        radioItems = new [] { "Auto()", "Percent(height)", "Fill(height)", "Absolute(height)" };
        label = new Label { X = Pos.Right (wOptionSelector) + 1, Y = 0, Text = "height:" };
        sizeFrame.Add (label);
        TextField hText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{hVal}" };
        sizeFrame.Add (hText);

        OptionSelector hOptionSelector = new () { X = Pos.X (label), Y = Pos.Bottom (label), Labels = radioItems };
        sizeFrame.Add (hOptionSelector);

        settingsPane.Add (sizeFrame);

        FrameView hostPane = new ()
        {
            X = Pos.Right (leftPane),
            Y = Pos.Bottom (settingsPane),
            Width = Dim.Fill (),
            Height = Dim.Fill (1), // + 1 for status bar
            SchemeName = "Dialog"
        };

        classListView.Accepting += (s, a) =>
                                   {
                                       settingsPane.SetFocus ();
                                       a.Handled = true;
                                   };

        classListView.ValueChanged += (_, _) =>
                                      {
                                          // Remove existing class, if any
                                          if (curView is { })
                                          {
                                              curView.SubViewsLaidOut -= LayoutCompleteHandler;
                                              hostPane.Remove (curView);
                                              curView.Dispose ();
                                              curView = null;
                                              hostPane.FillRect (hostPane.Viewport);
                                          }

                                          curView = CreateClass (viewClasses.Values.ToArray () [classListView.SelectedItem!.Value]);
                                      };

        xOptionSelector.ValueChanged += (_, _) => DimPosChanged (curView);

        xText.TextChanged += (s, args) =>
                             {
                                 try
                                 {
                                     xVal = int.Parse (xText.Text);
                                     DimPosChanged (curView);
                                 }
                                 catch
                                 { }
                             };

        yText.TextChanged += (s, e) =>
                             {
                                 try
                                 {
                                     yVal = int.Parse (yText.Text);
                                     DimPosChanged (curView);
                                 }
                                 catch
                                 { }
                             };

        yOptionSelector.ValueChanged += (_, _) => DimPosChanged (curView);

        wOptionSelector.ValueChanged += (_, _) => DimPosChanged (curView);

        wText.TextChanged += (s, args) =>
                             {
                                 try
                                 {
                                     wVal = int.Parse (wText.Text);
                                     DimPosChanged (curView);
                                 }
                                 catch
                                 { }
                             };

        hText.TextChanged += (s, args) =>
                             {
                                 try
                                 {
                                     hVal = int.Parse (hText.Text);
                                     DimPosChanged (curView);
                                 }
                                 catch
                                 { }
                             };

        hOptionSelector.ValueChanged += (_, _) => DimPosChanged (curView);

        top.Add (leftPane, settingsPane, hostPane);

        top.LayoutSubViews ();

        curView = CreateClass (viewClasses.First ().Value);

        var iterations = 0;

        Application.Iteration += OnApplicationOnIteration;
        Application.Run (top);
        Application.Iteration -= OnApplicationOnIteration;

        Assert.Equal (viewClasses.Count, iterations);

        top.Dispose ();
        Application.Shutdown ();
        ConfigurationManager.Disable (true);

        return;

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterations++;

            if (iterations < viewClasses.Count)
            {
                classListView.MoveDown ();

                if (curView is { })
                {
                    Assert.Equal (curView.GetType ().Name, viewClasses.Values.ToArray () [classListView.SelectedItem!.Value].Name);
                }
            }
            else
            {
                a.Value?.RequestStop ();
            }
        }

        void DimPosChanged (View? view)
        {
            if (view == null)
            {
                return;
            }

            try
            {
                switch (xOptionSelector.Value)
                {
                    case 0:
                        view.X = Pos.Percent (xVal);

                        break;

                    case 1:
                        view.X = Pos.AnchorEnd (xVal);

                        break;

                    case 2:
                        view.X = Pos.Center ();

                        break;

                    case 3:
                        view.X = Pos.Absolute (xVal);

                        break;
                }

                switch (yOptionSelector.Value)
                {
                    case 0:
                        view.Y = Pos.Percent (yVal);

                        break;

                    case 1:
                        view.Y = Pos.AnchorEnd (yVal);

                        break;

                    case 2:
                        view.Y = Pos.Center ();

                        break;

                    case 3:
                        view.Y = Pos.Absolute (yVal);

                        break;
                }

                switch (wOptionSelector.Value)
                {
                    case 0:
                        view.Width = Dim.Percent (wVal);

                        break;

                    case 1:
                        view.Width = Dim.Fill (wVal);

                        break;

                    case 2:
                        view.Width = Dim.Absolute (wVal);

                        break;
                }

                switch (hOptionSelector.Value)
                {
                    case 0:
                        view.Height = Dim.Percent (hVal);

                        break;

                    case 1:
                        view.Height = Dim.Fill (hVal);

                        break;

                    case 2:
                        view.Height = Dim.Absolute (hVal);

                        break;
                }
            }
            catch (Exception e)
            {
                MessageBox.ErrorQuery (ApplicationImpl.Instance, "Exception", e.Message, "Ok");
            }

            UpdateTitle (view);
        }

        void UpdateSettings (View view)
        {
            var x = view.X.ToString ();
            var y = view.Y.ToString ();

            try
            {
                xOptionSelector.Value = posNames.IndexOf (posNames.First (s => x.Contains (s)));
                yOptionSelector.Value = posNames.IndexOf (posNames.First (s => y.Contains (s)));
            }
            catch (InvalidOperationException e)
            {
                // This is a hack to work around the fact that the Pos enum doesn't have an "Align" value yet
                Debug.WriteLine ($"{e}");
            }

            xText.Text = $"{view.Frame.X}";
            yText.Text = $"{view.Frame.Y}";

            var w = view.Width!.ToString ();
            var h = view.Height!.ToString ();

            wOptionSelector.Value = dimNames.IndexOf (dimNames.First (s => w.Contains (s)));
            hOptionSelector.Value = dimNames.IndexOf (dimNames.First (s => h.Contains (s)));

            wText.Text = $"{view.Frame.Width}";
            hText.Text = $"{view.Frame.Height}";
        }

        void UpdateTitle (View? view) => hostPane.Title = $"{view!.GetType ().Name} - {view.X}, {view.Y}, {view.Width}, {view.Height}";

        View? CreateClass (Type type)
        {
            // If we are to create a generic Type
            if (type.IsGenericType)
            {
                // For each of the <T> arguments
                List<Type> typeArguments = new ();

                // use <object> or the original type if applicable
                foreach (Type arg in type.GetGenericArguments ())
                {
                    if (arg.IsValueType && Nullable.GetUnderlyingType (arg) == null)
                    {
                        typeArguments.Add (arg);
                    }
                    else
                    {
                        typeArguments.Add (typeof (object));
                    }
                }

                // Ensure the type does not contain any generic parameters
                if (type.ContainsGenericParameters)
                {
                    Logging.Warning ($"Cannot create an instance of {type} because it contains generic parameters.");

                    //throw new ArgumentException ($"Cannot create an instance of {type} because it contains generic parameters.");
                    return null;
                }

                // And change what type we are instantiating from MyClass<T> to MyClass<object>
                type = type.MakeGenericType (typeArguments.ToArray ());
            }

            // Instantiate view
            var view = Activator.CreateInstance (type) as View;

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
            if (!view.HasScheme)
            {
                view.SchemeName = "Base";
            }

            // If the view supports a Text property, set it so we have something to look at
            if (view.GetType ().GetProperty ("Text") != null)
            {
                try
                {
                    view.GetType ().GetProperty ("Text")?.GetSetMethod ()?.Invoke (view, new [] { "Test Text" });
                }
                catch (TargetInvocationException e)
                {
                    MessageBox.ErrorQuery (ApplicationImpl.Instance, "Exception", e.InnerException!.Message, "Ok");
                    view = null;
                }
            }

            // If the view supports a Title property, set it so we have something to look at
            if (view != null && view.GetType ().GetProperty ("Title") != null)
            {
                if (view.GetType ().GetProperty ("Title")!.PropertyType == typeof (string))
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
                && view.GetType ().GetProperty ("Source")!.PropertyType == typeof (IListDataSource))
            {
                ListWrapper<string> source = new (["Test Text #1", "Test Text #2", "Test Text #3"]);
                view?.GetType ().GetProperty ("Source")?.GetSetMethod ()?.Invoke (view, new [] { source });
            }

            // Add
            hostPane.Add (view);

            //DimPosChanged ();
            hostPane.LayoutSubViews ();
            hostPane.ClearViewport ();
            hostPane.SetNeedsDraw ();
            UpdateSettings (view!);
            UpdateTitle (view);

            view!.SubViewsLaidOut += LayoutCompleteHandler;

            return view;
        }

        void LayoutCompleteHandler (object? sender, LayoutEventArgs args) => UpdateTitle (curView);
    }
}
