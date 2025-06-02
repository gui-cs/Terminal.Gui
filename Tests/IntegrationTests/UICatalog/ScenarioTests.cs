using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using UICatalog;
using UnitTests;
using Xunit.Abstractions;

namespace IntegrationTests.UICatalog;

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

    private object? _timeoutLock;

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

        ConfigurationManager.Disable (resetToHardCodedDefaults: true);

        // If a previous test failed, this will ensure that the Application is in a clean state
        Application.ResetState (true);

        _output.WriteLine ($"Running Scenario '{scenarioType}'");
        var scenario = Activator.CreateInstance (scenarioType) as Scenario;

        uint abortTime = 2000;
        object? timeout = null;
        var initialized = false;
        var shutdownGracefully = false;
        var iterationCount = 0;
        Key quitKey = Application.QuitKey;

        Application.InitializedChanged += OnApplicationOnInitializedChanged;

        Application.ForceDriver = "FakeDriver";
        scenario!.Main ();
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


        Assert.True (shutdownGracefully, $"Scenario Failed to Quit with {quitKey} after {abortTime}ms and {iterationCount} iterations. Force quit.");

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif

        lock (_timeoutLock)
        {
            _timeoutLock = null;
        }

        ConfigurationManager.Disable (resetToHardCodedDefaults: true);

        return;

        void OnApplicationOnInitializedChanged (object? s, EventArgs<bool> a)
        {
            if (a.Value)
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
                shutdownGracefully = true;
            }

            _output.WriteLine ($"Initialized == {a.Value}; shutdownGracefully == {shutdownGracefully}.");
        }

        // If the scenario doesn't close within abortTime ms, this will force it to quit
        bool ForceCloseCallback ()
        {
            lock (_timeoutLock)
            {
                if (timeout is { })
                {
                    timeout = null;
                }
            }

            ConfigurationManager.Disable (resetToHardCodedDefaults: true);

            Application.ResetState (true);

            return false;
        }

        void OnApplicationOnIteration (object? s, IterationEventArgs a)
        {
            iterationCount++;

            if (Application.Initialized)
            {
                // Press QuitKey 
                quitKey = Application.QuitKey;
                _output.WriteLine ($"Attempting to quit with {quitKey} after {iterationCount} iterations.");
                Application.RaiseKeyDownEvent (quitKey);
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
        ConfigurationManager.Disable (resetToHardCodedDefaults: true);

        View? curView = null;

        // Settings
        var xVal = 0;
        var yVal = 0;

        var wVal = 0;
        var hVal = 0;
        List<string> posNames = ["Percent", "AnchorEnd", "Center", "Absolute"];
        List<string> dimNames = ["Auto", "Percent", "Fill", "Absolute"];

        Application.Init (new FakeDriver ());

        var top = new Toplevel ();

        Dictionary<string, Type> viewClasses = GetAllViewClasses ().ToDictionary (t => t.Name);

        Window leftPane = new ()
        {
            Title = "Classes",
            X = 0,
            Y = 0,
            Width = 15,
            Height = Dim.Fill (1), // for status bar
            CanFocus = false,
            SchemeName = "TopLevel"
        };

        ListView classListView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            AllowsMarking = false,
            SchemeName = "TopLevel",
            Source = new ListWrapper<string> (new (viewClasses.Keys.ToList ()))
        };
        leftPane.Add (classListView);

        FrameView settingsPane = new ()
        {
            X = Pos.Right (leftPane),
            Y = 0, // for menu
            Width = Dim.Fill (),
            Height = 10,
            CanFocus = false,
            SchemeName = "TopLevel",
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
        RadioGroup xRadioGroup = new () { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        TextField xText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{xVal}" };
        locationFrame.Add (xText);

        locationFrame.Add (xRadioGroup);

        radioItems = new [] { "Percent(y)", "AnchorEnd(y)", "Center", "Absolute(y)" };
        label = new () { X = Pos.Right (xRadioGroup) + 1, Y = 0, Text = "y:" };
        locationFrame.Add (label);
        TextField yText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{yVal}" };
        locationFrame.Add (yText);
        RadioGroup yRadioGroup = new () { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        locationFrame.Add (yRadioGroup);

        FrameView sizeFrame = new ()
        {
            X = Pos.Right (locationFrame),
            Y = Pos.Y (locationFrame),
            Height = 3 + radioItems.Length,
            Width = 40,
            Title = "Size (Dim)"
        };

        radioItems = new [] { "Auto()", "Percent(width)", "Fill(width)", "Absolute(width)" };
        label = new () { X = 0, Y = 0, Text = "width:" };
        sizeFrame.Add (label);
        RadioGroup wRadioGroup = new () { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        TextField wText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{wVal}" };
        sizeFrame.Add (wText);
        sizeFrame.Add (wRadioGroup);

        radioItems = new [] { "Auto()", "Percent(height)", "Fill(height)", "Absolute(height)" };
        label = new () { X = Pos.Right (wRadioGroup) + 1, Y = 0, Text = "height:" };
        sizeFrame.Add (label);
        TextField hText = new () { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{hVal}" };
        sizeFrame.Add (hText);

        RadioGroup hRadioGroup = new () { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        sizeFrame.Add (hRadioGroup);

        settingsPane.Add (sizeFrame);

        FrameView hostPane = new ()
        {
            X = Pos.Right (leftPane),
            Y = Pos.Bottom (settingsPane),
            Width = Dim.Fill (),
            Height = Dim.Fill (1), // + 1 for status bar
            SchemeName = "Dialog"
        };

        classListView.OpenSelectedItem += (s, a) => { settingsPane.SetFocus (); };

        classListView.SelectedItemChanged += (s, args) =>
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

                                                  curView = CreateClass (viewClasses.Values.ToArray () [classListView.SelectedItem]);
                                              };

        xRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (curView);

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

        yRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (curView);

        wRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (curView);

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

        hRadioGroup.SelectedItemChanged += (s, selected) => DimPosChanged (curView);

        top.Add (leftPane, settingsPane, hostPane);

        top.LayoutSubViews ();

        curView = CreateClass (viewClasses.First ().Value);

        var iterations = 0;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations < viewClasses.Count)
                                     {
                                         classListView.MoveDown ();

                                         if (curView is { })
                                         {
                                             Assert.Equal (
                                                           curView.GetType ().Name,
                                                           viewClasses.Values.ToArray () [classListView.SelectedItem].Name
                                                          );
                                         }
                                     }
                                     else
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        Application.Run (top);

        Assert.Equal (viewClasses.Count, iterations);

        top.Dispose ();
        Application.Shutdown ();
        ConfigurationManager.Disable (resetToHardCodedDefaults: true);

        void DimPosChanged (View? view)
        {
            if (view == null)
            {
                return;
            }

            try
            {
                switch (xRadioGroup.SelectedItem)
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

                switch (yRadioGroup.SelectedItem)
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

                switch (wRadioGroup.SelectedItem)
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

                switch (hRadioGroup.SelectedItem)
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
                xRadioGroup.SelectedItem = posNames.IndexOf (posNames.First (s => x.Contains (s)));
                yRadioGroup.SelectedItem = posNames.IndexOf (posNames.First (s => y.Contains (s)));
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

            wRadioGroup.SelectedItem = dimNames.IndexOf (dimNames.First (s => w.Contains (s)));
            hRadioGroup.SelectedItem = dimNames.IndexOf (dimNames.First (s => h.Contains (s)));

            wText.Text = $"{view.Frame.Width}";
            hText.Text = $"{view.Frame.Height}";
        }

        void UpdateTitle (View? view) { hostPane.Title = $"{view!.GetType ().Name} - {view.X}, {view.Y}, {view.Width}, {view.Height}"; }

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
                    MessageBox.ErrorQuery ("Exception", e.InnerException!.Message, "Ok");
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

        void LayoutCompleteHandler (object? sender, LayoutEventArgs args) { UpdateTitle (curView); }
    }

    [Fact]
    public void Run_Generic ()
    {
        ConfigurationManager.Disable (resetToHardCodedDefaults: true);
        Assert.Equal (Key.Esc, Application.QuitKey);

        ObservableCollection<Scenario> scenarios = Scenario.GetScenarios ();
        Assert.NotEmpty (scenarios);

        int item = scenarios.IndexOf (s => s.GetName ().Equals ("Generic", StringComparison.OrdinalIgnoreCase));
        Scenario generic = scenarios [item];

        Application.Init (new FakeDriver ());

        // BUGBUG: (#2474) For some reason ReadKey is not returning the QuitKey for some Scenarios
        // by adding this Space it seems to work.

        Assert.Equal (Key.Esc, Application.QuitKey);
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
        object? token = null;

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

        Application.KeyDown += (sender, args) => { Assert.Equal (Application.QuitKey, args); };

        generic.Main ();

        Assert.Equal (0, abortCount);

        // # of key up events should match # of iterations
        Assert.Equal (1, iterations);

        generic.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
        ConfigurationManager.Disable (resetToHardCodedDefaults: true);

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif
    }
}
