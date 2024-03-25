using System.Reflection;
using Xunit.Abstractions;

namespace UICatalog.Tests;

public class ScenarioTests
{
    private readonly ITestOutputHelper _output;

    public ScenarioTests (ITestOutputHelper output)
    {
#if DEBUG_IDISPOSABLE
        Responder.Instances.Clear ();
#endif
        _output = output;
    }

    /// <summary>
    ///     <para>This runs through all Scenarios defined in UI Catalog, calling Init, Setup, and Run.</para>
    ///     <para>Should find any Scenarios which crash on load or do not respond to <see cref="Application.RequestStop()"/>.</para>
    /// </summary>
    [Fact]
    public void Run_All_Scenarios ()
    {
        List<Scenario> scenarios = Scenario.GetScenarios ();
        Assert.NotEmpty (scenarios);

        foreach (Scenario scenario in scenarios)
        {
            _output.WriteLine ($"Running Scenario '{scenario.GetName ()}'");

            Application.Init (new FakeDriver ());

            // Press QuitKey 
            Assert.Empty (FakeConsole.MockKeyPresses);

            // BUGBUG: (#2474) For some reason ReadKey is not returning the QuitKey for some Scenarios
            // by adding this Space it seems to work.
            //FakeConsole.PushMockKeyPress (Key.Space);
            FakeConsole.PushMockKeyPress ((KeyCode)Application.QuitKey);

            // The only key we care about is the QuitKey
            Application.KeyDown += (sender, args) =>
                                       {
                                           _output.WriteLine ($"  Keypress: {args.KeyCode}");

                                           // BUGBUG: (#2474) For some reason ReadKey is not returning the QuitKey for some Scenarios
                                           // by adding this Space it seems to work.
                                           // See #2474 for why this is commented out
                                           Assert.Equal (Application.QuitKey.KeyCode, args.KeyCode);
                                       };

            uint abortTime = 500;

            // If the scenario doesn't close within 500ms, this will force it to quit
            Func<bool> forceCloseCallback = () =>
                                            {
                                                if (Application.Top.Running && FakeConsole.MockKeyPresses.Count == 0)
                                                {
                                                    Application.RequestStop ();

                                                    // See #2474 for why this is commented out
                                                    Assert.Fail (
                                                                 $"'{
                                                                     scenario.GetName ()
                                                                 }' failed to Quit with {
                                                                     Application.QuitKey
                                                                 } after {
                                                                     abortTime
                                                                 }ms. Force quit."
                                                                );
                                                }

                                                return false;
                                            };

            //output.WriteLine ($"  Add timeout to force quit after {abortTime}ms");
            _ = Application.AddTimeout (TimeSpan.FromMilliseconds (abortTime), forceCloseCallback);

            Application.Iteration += (s, a) =>
                                     {
                                         //output.WriteLine ($"  iteration {++iterations}");
                                         if (Application.Top.Running && FakeConsole.MockKeyPresses.Count == 0)
                                         {
                                             Application.RequestStop ();
                                             Assert.Fail ($"'{scenario.GetName ()}' failed to Quit with {Application.QuitKey}. Force quit.");
                                         }
                                     };

            scenario.Init ();
            scenario.Setup ();
            scenario.Run ();
            scenario.Dispose ();

            Application.Shutdown ();
#if DEBUG_IDISPOSABLE
            Assert.Empty (Responder.Instances);
#endif
        }
#if DEBUG_IDISPOSABLE
        Assert.Empty (Responder.Instances);
#endif
    }

    [Fact]
    public void Run_All_Views_Tester_Scenario ()
    {
        Window _leftPane;
        ListView _classListView;
        FrameView _hostPane;

        Dictionary<string, Type> _viewClasses;
        View _curView = null;

        // Settings
        FrameView _settingsPane;
        CheckBox _computedCheckBox;
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
        List<string> posNames = new () { "Factor", "AnchorEnd", "Center", "Absolute" };
        List<string> dimNames = new () { "Factor", "Fill", "Absolute" };

        Application.Init (new FakeDriver ());

        Toplevel Top = new Toplevel ();

        _viewClasses = GetAllViewClassesCollection ()
                       .OrderBy (t => t.Name)
                       .Select (t => new KeyValuePair<string, Type> (t.Name, t))
                       .ToDictionary (t => t.Key, t => t.Value);

        _leftPane = new Window
        {
            Title = "Classes",
            X = 0,
            Y = 0,
            Width = 15,
            Height = Dim.Fill (1), // for status bar
            CanFocus = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"]
        };

        _classListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            AllowsMarking = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Source = new ListWrapper (_viewClasses.Keys.ToList ())
        };
        _leftPane.Add (_classListView);

        _settingsPane = new FrameView
        {
            X = Pos.Right (_leftPane),
            Y = 0, // for menu
            Width = Dim.Fill (),
            Height = 10,
            CanFocus = false,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            Title = "Settings"
        };
        _computedCheckBox = new CheckBox { X = 0, Y = 0, Text = "Computed Layout", Checked = true };
        _settingsPane.Add (_computedCheckBox);

        var radioItems = new [] { "Percent(x)", "AnchorEnd(x)", "Center", "At(x)" };

        _locationFrame = new FrameView
        {
            X = Pos.Left (_computedCheckBox),
            Y = Pos.Bottom (_computedCheckBox),
            Height = 3 + radioItems.Length,
            Width = 36,
            Title = "Location (Pos)"
        };
        _settingsPane.Add (_locationFrame);

        var label = new Label { X = 0, Y = 0, Text = "x:" };
        _locationFrame.Add (label);
        _xRadioGroup = new RadioGroup { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        _xText = new TextField { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_xVal}" };
        _locationFrame.Add (_xText);

        _locationFrame.Add (_xRadioGroup);

        radioItems = new [] { "Percent(y)", "AnchorEnd(y)", "Center", "At(y)" };
        label = new Label { X = Pos.Right (_xRadioGroup) + 1, Y = 0, Text = "y:" };
        _locationFrame.Add (label);
        _yText = new TextField { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_yVal}" };
        _locationFrame.Add (_yText);
        _yRadioGroup = new RadioGroup { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        _locationFrame.Add (_yRadioGroup);

        _sizeFrame = new FrameView
        {
            X = Pos.Right (_locationFrame),
            Y = Pos.Y (_locationFrame),
            Height = 3 + radioItems.Length,
            Width = 40,
            Title = "Size (Dim)"
        };

        radioItems = new [] { "Percent(width)", "Fill(width)", "Sized(width)" };
        label = new Label { X = 0, Y = 0, Text = "width:" };
        _sizeFrame.Add (label);
        _wRadioGroup = new RadioGroup { X = 0, Y = Pos.Bottom (label), RadioLabels = radioItems };
        _wText = new TextField { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_wVal}" };
        _sizeFrame.Add (_wText);
        _sizeFrame.Add (_wRadioGroup);

        radioItems = new [] { "Percent(height)", "Fill(height)", "Sized(height)" };
        label = new Label { X = Pos.Right (_wRadioGroup) + 1, Y = 0, Text = "height:" };
        _sizeFrame.Add (label);
        _hText = new TextField { X = Pos.Right (label) + 1, Y = 0, Width = 4, Text = $"{_hVal}" };
        _sizeFrame.Add (_hText);

        _hRadioGroup = new RadioGroup { X = Pos.X (label), Y = Pos.Bottom (label), RadioLabels = radioItems };
        _sizeFrame.Add (_hRadioGroup);

        _settingsPane.Add (_sizeFrame);

        _hostPane = new FrameView
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
                                                      _curView.LayoutComplete -= LayoutCompleteHandler;
                                                      _hostPane.Remove (_curView);
                                                      _curView.Dispose ();
                                                      _curView = null;
                                                      _hostPane.Clear (_hostPane.Bounds);
                                                  }

                                                  _curView = CreateClass (_viewClasses.Values.ToArray () [_classListView.SelectedItem]);
                                              };

        _computedCheckBox.Toggled += (s, e) =>
                                     {
                                         if (_curView != null)
                                         {
                                             //_curView.LayoutStyle = e.OldValue == true ? LayoutStyle.Absolute : LayoutStyle.Computed;
                                             _hostPane.LayoutSubviews ();
                                         }
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

        Top.Add (_leftPane, _settingsPane, _hostPane);

        Top.LayoutSubviews ();

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

        Application.Run (Top);

        Assert.Equal (_viewClasses.Count, iterations);

        Top.Dispose ();
        Application.Shutdown ();

        void DimPosChanged (View view)
        {
            if (view == null)
            {
                return;
            }

            LayoutStyle layout = view.LayoutStyle;

            try
            {
                //view.LayoutStyle = LayoutStyle.Absolute;

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
                        view.X = Pos.At (_xVal);

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
                        view.Y = Pos.At (_yVal);

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
                        view.Width = Dim.Sized (_wVal);

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
                        view.Height = Dim.Sized (_hVal);

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
            _xRadioGroup.SelectedItem = posNames.IndexOf (posNames.Where (s => x.Contains (s)).First ());
            _yRadioGroup.SelectedItem = posNames.IndexOf (posNames.Where (s => y.Contains (s)).First ());
            _xText.Text = $"{view.Frame.X}";
            _yText.Text = $"{view.Frame.Y}";

            var w = view.Width.ToString ();
            var h = view.Height.ToString ();
            _wRadioGroup.SelectedItem = dimNames.IndexOf (dimNames.Where (s => w.Contains (s)).First ());
            _hRadioGroup.SelectedItem = dimNames.IndexOf (dimNames.Where (s => h.Contains (s)).First ());
            _wText.Text = $"{view.Frame.Width}";
            _hText.Text = $"{view.Frame.Height}";
        }

        void UpdateTitle (View view) { _hostPane.Title = $"{view.GetType ().Name} - {view.X}, {view.Y}, {view.Width}, {view.Height}"; }

        List<Type> GetAllViewClassesCollection ()
        {
            List<Type> types = new ();

            foreach (Type type in typeof (View).Assembly.GetTypes ()
                                               .Where (
                                                       myType =>
                                                           myType.IsClass && !myType.IsAbstract && myType.IsPublic && myType.IsSubclassOf (typeof (View))
                                                      ))
            {
                types.Add (type);
            }

            return types;
        }

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

            //_curView.X = Pos.Center ();
            //_curView.Y = Pos.Center ();
            if (!view.AutoSize)
            {
                view.Width = Dim.Percent (75);
                view.Height = Dim.Percent (75);
            }

            // Set the colorscheme to make it stand out if is null by default
            if (view.ColorScheme == null)
            {
                view.ColorScheme = Colors.ColorSchemes ["Base"];
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
                var source = new ListWrapper (new List<string> { "Test Text #1", "Test Text #2", "Test Text #3" });
                view?.GetType ().GetProperty ("Source")?.GetSetMethod ()?.Invoke (view, new [] { source });
            }

            // Set Settings
            _computedCheckBox.Checked = view.LayoutStyle == LayoutStyle.Computed;

            // Add
            _hostPane.Add (view);

            //DimPosChanged ();
            _hostPane.LayoutSubviews ();
            _hostPane.Clear ();
            _hostPane.SetNeedsDisplay ();
            UpdateSettings (view);
            UpdateTitle (view);

            view.LayoutComplete += LayoutCompleteHandler;

            return view;
        }

        void LayoutCompleteHandler (object sender, LayoutEventArgs args) { UpdateTitle (_curView); }
    }

    [Fact]
    public void Run_Generic ()
    {
        List<Scenario> scenarios = Scenario.GetScenarios ();
        Assert.NotEmpty (scenarios);

        int item = scenarios.FindIndex (s => s.GetName ().Equals ("Generic", StringComparison.OrdinalIgnoreCase));
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
                                         token = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (ms), abortCallback);
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
                                       // See #2474 for why this is commented out
                                       Assert.Equal (KeyCode.CtrlMask | KeyCode.Q, args.KeyCode);
                                   };

        generic.Init ();
        generic.Setup ();
        generic.Run ();

        Assert.Equal (0, abortCount);

        // # of key up events should match # of iterations
        Assert.Equal (1, iterations);

        generic.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();

#if DEBUG_IDISPOSABLE
        Assert.Empty (Responder.Instances);
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
